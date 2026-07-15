#!/usr/bin/env python3
"""Verify current-build native carriers and remaining DCL multistrike boundaries."""
from __future__ import annotations

import argparse
import csv
import hashlib
import struct
import time
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

import analyze_dcl_reaction_dispatch as dispatch
import report_dcl_ability_classification as ability_classification


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE
DEFAULT_CATALOG = ROOT / "work" / "wotl_ability_action_baseline.csv"
DEFAULT_TABLEDATA = Path(r"C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData")
DEFAULT_ABILITY_TYPE = DEFAULT_TABLEDATA / "AbilityTypeData.xml"
DEFAULT_EFFECT_FILTER = DEFAULT_TABLEDATA / "AbilityEffectNumberFilterData.xml"
DEFAULT_ABILITY_DATA = DEFAULT_TABLEDATA / "AbilityData.xml"
DEFAULT_JOB_COMMAND = DEFAULT_TABLEDATA / "JobCommandData.xml"
NATIVE_REPEAT_SOURCE = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "DclNativeRepeat.cs"
MOD_SOURCE = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "Mod.cs"
DISPATCH_TABLE_RVA = 0x682BC8
BARRAGE_POSTPROCESSOR_RVA = 0x3067CC
BARRAGE_POSTPROCESSOR_TRACE_RVA = 0x101AE0E0
RANDOM_FIRE_REPEAT_COUNT_RVA = 0x7B0762
RANDOM_FIRE_REPEAT_INDEX_RVA = 0x7B0763
RANDOM_FIRE_TRUTH_WEIGHTS_RVA = 0x9069D0
RANDOM_FIRE_TRUTH_WEIGHTS = (5, 5, 10, 10, 20, 20, 10, 10, 5, 5)
FORMULA_TARGETS = {
    0x1E: 0x307C0C,
    0x1F: 0x307C24,
    0x32: 0x30877C,
    0x5E: 0x307C0C,
    0x5F: 0x307C0C,
    0x60: 0x30930C,
    0x6A: 0x30D71C,
}
EXPECTED_RANDOM_FIRE_IDS = {
    169, 170, 171, 172, 173, 174, 175, 176,
    177, 178, 179, 180, 255, 342, 343, 344,
}


@dataclass(frozen=True)
class AbilityExpectation:
    ability_id: int
    name: str
    formula: int
    x: int
    y: int
    aoe: int
    random_fire: bool
    boundary: str


ABILITIES = (
    AbilityExpectation(101, "Pummel", 0x32, 9, 0, 0, False, "aggregate-native"),
    AbilityExpectation(173, "Celestial Void", 0x1E, 10, 8, 1, True, "random-fire-live"),
    AbilityExpectation(179, "Corporeal Void", 0x1F, 10, 27, 1, True, "random-fire-live"),
    AbilityExpectation(344, "Dark Whisper", 0x5E, 5, 1, 1, True, "random-fire-live"),
    AbilityExpectation(349, "Nanoflare", 0x5F, 0, 5, 2, False, "single-hit-closed"),
    AbilityExpectation(358, "Barrage", 0x6A, 50, 0, 0, False, "native-four-repeat"),
)


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor(
        "common-ma-handler",
        0x307C0C,
        "48 83 EC 28 E8 57 F1 FF FF 85 C0 75 05 E8 7A F3 FF FF 48 83 C4 28 C3",
        "runs one ordinary MA result pipeline; formulas 0x1E, 0x5E, and 0x5F alias it",
    ),
    Anchor(
        "pummel-aggregate-count",
        0x3087D8,
        "E8 A7 48 00 00 0F B6 0D B9 7F 4A 00 0F AF C1 99 81 E2 FF 7F 00 00 03 C2 48 8B 15 79 27 56 01 C1 F8 0F FE C0 0F B6 C8 88 0D 88 7F 4A 00 8B C1 0F BF 4A 06 0F AF C8 C6 42 27 80 66 89 4A 06",
        "derives floor(random15*X/32768)+1 and multiplies one staged HP debit by it",
    ),
    Anchor(
        "barrage-weapon-delegate",
        0x30D71C,
        "48 83 EC 28 0F B6 05 83 30 4A 00 48 8D 0D A2 54 37 00 FF 54 C1 F8 48 83 C4 28 E9 91 90 FF FF",
        "dispatches the equipped-weapon formula and enters the normal-attack postprocessor",
    ),
    Anchor(
        "barrage-normal-attack-postprocessor-thunk",
        BARRAGE_POSTPROCESSOR_RVA,
        "E9 0F 79 EA 0F",
        "enters the protected normal-attack rider/postprocessing trace after the one weapon-formula call",
    ),
    Anchor(
        "barrage-normal-attack-postprocessor-trace",
        BARRAGE_POSTPROCESSOR_TRACE_RVA,
        "48 89 5C 24 18 57 48 83 EC 20 48 8B 05 7F CE 6B F1 31 FF 40 38 78 27 0F 8D 79 01 00 00",
        "begins the result/rider postprocessor; it does not wrap the formula dispatch in a strike loop",
    ),
    Anchor(
        "action-record-scratch-copy",
        0x309A12,
        "49 8D 80 BE 01 00 00 48 03 C1 88 1D 3F 6D 4A 00 44 39 3D 73 15 56 01 48 89 05 40 15 56 01",
        "binds the selected target result while the current action record is available to calculation",
    ),
    Anchor(
        "random-fire-flag-consumer",
        0xEEBC6ED,
        "40 F6 C6 08 74 1A 83 3D 86 E8 9A F2 00 75 11 E8 AF 5F 3C F1",
        "tests action byte 4 bit 0x08 and dispatches the dedicated random-tile selector",
    ),
    Anchor(
        "random-fire-target-selector",
        0x2826B0,
        "40 53 48 83 EC 20 B9 01 00 00 00 E8 E8 FD FF FF 33 DB 8B C8 85 C0 79 15",
        "chooses one eligible tile for the current repeat before clearing and setting the target map",
    ),
    Anchor(
        "random-fire-repeat-initializer",
        0xEED0D11,
        "E8 46 A3 3E F1 44 8A 40 03 8A 50 08 8A 48 09 44 88 05 04 B8 11 F4 8D 42 E2 3C 01 0F 87 C6 00 00 00",
        "reads formula and X; formulas 0x1E/0x1F enter the weighted repeat-count branch",
    ),
    Anchor(
        "random-fire-formula-5e-count",
        0xEED0DF8,
        "80 FA 5E 75 0D FE C1 88 0D 5D F9 8D F1 E9 9E 00 00 00",
        "stores X+1 repeats for formula 0x5E (three for Tri attacks and six for Dark Whisper)",
    ),
    Anchor(
        "barrage-formula-6a-count",
        0xEED0E0A,
        "80 FA 6A 75 0C C6 05 4C F9 8D F1 04 E9 8D 00 00 00",
        "stores fixed count four for formula 0x6A in the shared native repeat counter",
    ),
    Anchor(
        "random-fire-per-repeat-target-and-calc",
        0x281E0B,
        "49 8D 8C 24 80 3E 85 01 48 03 CE 48 8D 55 D0 E8 05 FE FF FF",
        "runs target selection inside the result producer before the selected-target calculation sweep",
    ),
    Anchor(
        "random-fire-selected-target-calc",
        0x281EFA,
        "8A 54 1D D8 80 FA FF 74 0F 49 8B CE 44 88 2D 7F E8 52 00 E8 9A 7A 08 00 48 FF C3 48 83 FB 15 7C DF",
        "calls the ordinary calculation once for every selected target; RandomFire selects one tile per repeat",
    ),
    Anchor(
        "random-fire-repeat-continuation",
        0x2821EC,
        "8A 05 71 E5 52 00 02 C3 3A 05 68 E5 52 00 88 05 63 E5 52 00 0F 92 C0 88 47 18",
        "increments repeat index, compares it with repeat count, and publishes whether another result event follows",
    ),
)


def target_for(pe: pefile.PE, raw: bytes, formula_id: int) -> int:
    entry = dispatch.rva_bytes(pe, raw, DISPATCH_TABLE_RVA + formula_id * 8, 8)
    return struct.unpack("<Q", entry)[0] - pe.OPTIONAL_HEADER.ImageBase


def jump_target(pe: pefile.PE, raw: bytes, rva: int) -> int | None:
    data = dispatch.rva_bytes(pe, raw, rva, 5)
    if data[0] != 0xE9:
        return None
    return rva + 5 + struct.unpack_from("<i", data, 1)[0]


def load_catalog(path: Path) -> dict[int, dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return {int(row["id_dec"]): row for row in csv.DictReader(handle)}


def bool_field(value: str) -> bool:
    return value.strip().lower() in {"1", "true", "yes"}


def xml_rows(path: Path, tag: str) -> dict[int, ET.Element]:
    root = ET.parse(path).getroot()
    return {
        int(node.findtext("Id", "-1")): node
        for node in root.findall(f"./Entries/{tag}")
    }


def render(
    exe: Path,
    catalog_path: Path,
    output: Path,
    ability_type_path: Path,
    effect_filter_path: Path,
    ability_data_path: Path,
    job_command_path: Path,
) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    catalog = load_catalog(catalog_path)
    classified_rows, classification_errors = ability_classification.load_manifest(catalog_path)
    classified_by_id = {int(row["ability_id"]): row for row in classified_rows}

    dispatch_rows = [
        (formula_id, target_for(pe, raw, formula_id), expected)
        for formula_id, expected in FORMULA_TARGETS.items()
    ]
    anchor_rows = []
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        anchor_rows.append((anchor, actual == expected, actual))

    ability_rows = []
    for expected in ABILITIES:
        row = catalog[expected.ability_id]
        raw14 = bytes.fromhex(row["raw14_psp"])
        raw_random_fire = bool(raw14[4] & 0x08)
        actual = {
            "formula": int(row["formula_hex"], 16),
            "x": int(row["x"]),
            "y": int(row["y"]),
            "aoe": int(row["aoe"]),
            "random_fire": bool_field(row["RandomFire"]),
            "raw_random_fire": raw_random_fire,
        }
        passed = (
            actual["formula"] == expected.formula
            and actual["x"] == expected.x
            and actual["y"] == expected.y
            and actual["aoe"] == expected.aoe
            and actual["random_fire"] == expected.random_fire
            and actual["raw_random_fire"] == expected.random_fire
        )
        ability_rows.append((expected, row, actual, passed))

    random_fire_rows = []
    for ability_id, row in sorted(catalog.items()):
        raw14 = bytes.fromhex(row["raw14_psp"])
        declared = bool_field(row["RandomFire"])
        raw_flag = len(raw14) > 4 and bool(raw14[4] & 0x08)
        if declared or raw_flag:
            random_fire_rows.append((ability_id, row, declared, raw_flag))
    random_fire_ok = (
        {ability_id for ability_id, _, _, _ in random_fire_rows} == EXPECTED_RANDOM_FIRE_IDS
        and all(declared == raw_flag for _, _, declared, raw_flag in random_fire_rows)
        and all(int(row["formula_hex"], 16) in {0x1E, 0x1F, 0x5E}
                for _, row, _, _ in random_fire_rows)
    )
    random_fire_status_ids = {173, 179, 344}
    native_policy_ok = (
        not classification_errors
        and classified_by_id[101]["candidate_side_effect_policy"] == "managed_multistrike"
        and classified_by_id[358]["candidate_side_effect_policy"] == "native_multistrike"
        and all(
            classified_by_id[ability_id]["candidate_side_effect_policy"]
            == ("native_multistrike_status_rider" if ability_id in random_fire_status_ids else "native_multistrike")
            for ability_id in EXPECTED_RANDOM_FIRE_IDS
        )
        and all(
            classified_by_id[ability_id]["metadata_blocking_scope"] in {"closed", "design"}
            for ability_id in EXPECTED_RANDOM_FIRE_IDS | {101, 358}
        )
    )

    formula_alias_ok = (
        target_for(pe, raw, 0x1E)
        == target_for(pe, raw, 0x5E)
        == target_for(pe, raw, 0x5F)
        == 0x307C0C
    )
    formula_60_target = jump_target(pe, raw, 0x30930C)
    formula_60_ok = formula_60_target == 0x306F98
    barrage_postprocessor_target = jump_target(pe, raw, BARRAGE_POSTPROCESSOR_RVA)
    barrage_postprocessor_ok = barrage_postprocessor_target == BARRAGE_POSTPROCESSOR_TRACE_RVA
    truth_weights = tuple(
        dispatch.rva_bytes(
            pe,
            raw,
            RANDOM_FIRE_TRUTH_WEIGHTS_RVA,
            len(RANDOM_FIRE_TRUTH_WEIGHTS),
        )
    )
    truth_weights_ok = truth_weights == RANDOM_FIRE_TRUTH_WEIGHTS and sum(truth_weights) == 100
    native_repeat_source = NATIVE_REPEAT_SOURCE.read_text(encoding="utf-8")
    mod_source = MOD_SOURCE.read_text(encoding="utf-8")
    native_repeat_source_ok = all(token in native_repeat_source for token in (
        "RepeatCountRva = 0x7B0762",
        "RepeatIndexRva = 0x7B0763",
        "BarrageRepeatCount = 4",
        "[5, 5, 10, 10, 20, 20, 10, 10, 5, 5]",
        "HasMoreRepeats",
        "TruthRepeatCountFromPercentile",
        "Formula5ERepeatCount",
    ))
    random_fire_retention_ok = (
        "ShouldRetainDclHitDecisionForRandomFire" in mod_source
        and mod_source.count("ShouldRetainDclHitDecisionForRandomFire(") >= 6
        and "DclNativeRepeat.RepeatCountRva" in mod_source
        and "DclNativeRepeat.RepeatIndexRva" in mod_source
    )

    table_paths = (ability_type_path, effect_filter_path, ability_data_path, job_command_path)
    tabledata_available = all(path.exists() for path in table_paths)
    barrage_table_checks: list[tuple[str, str, str, bool]] = []
    if tabledata_available:
        ability_types = xml_rows(ability_type_path, "AbilityType")
        effects = xml_rows(effect_filter_path, "AbilityEffectNumberFilter")
        abilities = xml_rows(ability_data_path, "Ability")
        job_commands = xml_rows(job_command_path, "JobCommand")
        barrage_type = ability_types[358]
        pummel_type = ability_types[101]
        barrage_effect = effects[358]
        pummel_effect = effects[101]
        barrage_ability = abilities[358]
        piracy = job_commands[225]
        barrage_catalog = catalog[358]
        barrage_table_checks = [
            ("Barrage animation", barrage_type.findtext("AnimationId", ""), "0",
             barrage_type.findtext("AnimationId") == "0"),
            ("Barrage effect", barrage_effect.findtext("EffectId", ""), "-1",
             barrage_effect.findtext("EffectId") == "-1"),
            ("Barrage charge-effect type", barrage_type.findtext("ChargeEffectType", ""), "7",
             barrage_type.findtext("ChargeEffectType") == "7"),
            ("Barrage action flag", barrage_catalog["NormalAttack"], "NormalAttack=1",
             bool_field(barrage_catalog["NormalAttack"])),
            ("Barrage range source", barrage_catalog["WeaponRange"], "WeaponRange=1",
             bool_field(barrage_catalog["WeaponRange"])),
            ("Barrage AI family", barrage_ability.findtext("AIBehaviorFlags", ""),
             "PhysicalAttack + UsableByAI",
             {"PhysicalAttack", "UsableByAI"}.issubset(
                 set(barrage_ability.findtext("AIBehaviorFlags", "").replace(",", " ").split()))),
            ("Piracy slot", piracy.findtext("AbilityId4", ""), "358",
             piracy.findtext("AbilityId4") == "358"),
            ("Pummel comparison animation", pummel_type.findtext("AnimationId", ""), "104",
             pummel_type.findtext("AnimationId") == "104"),
            ("Pummel comparison effect", pummel_effect.findtext("EffectId", ""), "96",
             pummel_effect.findtext("EffectId") == "96"),
        ]
    barrage_tabledata_ok = tabledata_available and all(row[3] for row in barrage_table_checks)

    lines = [
        "# DCL multistrike native-carrier analysis",
        "",
        "Generated by `tools/analyze_dcl_multistrike_transactions.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Ability catalog: `{catalog_path}`",
        f"- Ability animation table: `{ability_type_path}`",
        f"- Ability effect table: `{effect_filter_path}`",
        f"- Ability metadata table: `{ability_data_path}`",
        f"- Job command table: `{job_command_path}`",
        f"- Output: `{output}`",
        "",
        "## Formula dispatch",
        "",
        "| Formula | Handler | Expected | Result |",
        "| --- | ---: | ---: | --- |",
    ]
    for formula_id, actual, expected in dispatch_rows:
        lines.append(
            f"| `0x{formula_id:02X}` | `0x{actual:X}` | `0x{expected:X}` | "
            f"{'PASS' if actual == expected else 'FAIL'} |"
        )
    lines.extend([
        "",
        f"- Formula aliases `0x1E = 0x5E = 0x5F = 0x307C0C`: "
        f"**{'PASS' if formula_alias_ok else 'FAIL'}**.",
        f"- Formula `0x60` thunk target `0x{formula_60_target:X}` "
        f"(expected common body `0x306F98`): **{'PASS' if formula_60_ok else 'FAIL'}**.",
        f"- Barrage postprocessor thunk target `0x{barrage_postprocessor_target:X}` "
        f"(expected `0x{BARRAGE_POSTPROCESSOR_TRACE_RVA:X}`): "
        f"**{'PASS' if barrage_postprocessor_ok else 'FAIL'}**.",
        f"- Truth/Untruth repeat weights `{','.join(map(str, truth_weights))}` "
        f"(expected `{','.join(map(str, RANDOM_FIRE_TRUTH_WEIGHTS))}`, total 100): "
        f"**{'PASS' if truth_weights_ok else 'FAIL'}**.",
        f"- Managed cadence helper mirrors the native counters and exact distributions: "
        f"**{'PASS' if native_repeat_source_ok else 'FAIL'}**.",
        f"- Runtime hit-decision consumers retain spell-level Magic Evade until the final repeat: "
        f"**{'PASS' if random_fire_retention_ok else 'FAIL'}**.",
        f"- Metadata separates aggregate managed Pummel from engine-owned RandomFire/Barrage repetition: "
        f"**{'PASS' if native_policy_ok else 'FAIL'}**.",
        "",
        "The handler alias proves that formula identity alone does not make an action repeated. The",
        "ability-action `RandomFire` flag is a distinct outer targeting/cadence input.",
        "",
        "## Catalog flags and native boundary",
        "",
        "| Id | Action | Formula | X/Y | AoE | RandomFire | raw byte 4 bit 0x08 | Boundary | Result |",
        "| ---: | --- | --- | --- | ---: | --- | --- | --- | --- |",
    ])
    for expected, row, actual, passed in ability_rows:
        name = row.get("name_ivc") or row.get("name_wotl") or expected.name
        lines.append(
            f"| {expected.ability_id} | {name} | `0x{actual['formula']:02X}` | "
            f"`{actual['x']}/{actual['y']}` | {actual['aoe']} | "
            f"{'yes' if actual['random_fire'] else 'no'} | "
            f"{'set' if actual['raw_random_fire'] else 'clear'} | `{expected.boundary}` | "
            f"{'PASS' if passed else 'FAIL'} |"
        )

    lines.extend([
        "",
        "## Complete RandomFire inventory",
        "",
        "| Id | Action | Formula | X/Y | AoE | Enemy use | Raw flag |",
        "| ---: | --- | --- | --- | ---: | --- | --- |",
    ])
    for ability_id, row, declared, raw_flag in random_fire_rows:
        name = row.get("name_ivc") or row.get("name_wotl") or "<unnamed>"
        lines.append(
            f"| {ability_id} | {name} | `{row['formula_hex']}` | `{row['x']}/{row['y']}` | "
            f"{row['aoe']} | {'yes' if bool_field(row['used_by_enemies']) else 'no'} | "
            f"{'set' if raw_flag else 'clear'} |"
        )
    lines.extend([
        "",
        f"The inventory is **{'PASS' if random_fire_ok else 'FAIL'}**: exactly 16 actions use the flag,",
        "the decoded column matches raw action byte 4 bit `0x08`, and every record uses formula",
        "`0x1E`, `0x1F`, or `0x5E`. All 16 catalog records are enemy-usable.",
    ])

    lines.extend(["", "## Barrage TableData carrier audit", ""])
    if tabledata_available:
        lines.extend([
            "| Check | Actual | Expected | Result |",
            "| --- | --- | --- | --- |",
        ])
        for name, actual, expected, passed in barrage_table_checks:
            lines.append(
                f"| {name} | `{actual}` | `{expected}` | {'PASS' if passed else 'FAIL'} |"
            )
        lines.extend([
            "",
            "Barrage is exposed through Piracy and uses the ordinary weapon-facing action path:",
            "`WeaponRange=1`, `NormalAttack=1`, animation `0`, and effect `-1`. Unlike Pummel's",
            "dedicated animation `104` and effect `96`, the editable TableData layer contains no",
            "Barrage-specific effect sequence that could own repetition. Formula `0x6A` instead initializes",
            "the shared native repeat count to four; the protected result producer owns continuation.",
        ])
    else:
        lines.extend([
            "Installed ModLoader TableData templates are unavailable; the carrier comparison is skipped.",
            "The executable/catalog checks remain valid, but a complete report requires the four paths",
            "listed under Inputs.",
        ])

    lines.extend([
        "",
        "## Byte anchors",
        "",
        "| Name | RVA | Result | Meaning |",
        "| --- | ---: | --- | --- |",
    ])
    for anchor, passed, actual in anchor_rows:
        shown = "PASS" if passed else f"FAIL (`{' '.join(f'{value:02X}' for value in actual)}`)"
        lines.append(f"| `{anchor.name}` | `0x{anchor.rva:X}` | {shown} | {anchor.meaning} |")

    lines.extend([
        "",
        "## DCL implementation boundary",
        "",
        "- **Pummel — Strong:** formula `0x32` does not commit N native strikes. It computes",
        "  `count = floor(random15 * X / 32768) + 1`, multiplies the one staged HP debit by that",
        "  count, and sets one HP-debit result flag. The vanilla carrier is aggregate. DCL Pummel",
        "  therefore needs an explicit managed strike loop to obtain per-strike contest and Guard",
        "  depletion while retaining the authored once-per-action reaction policy.",
        "- **RandomFire family — Strong:** all 16 flagged actions are mapped and enemy-usable. The",
        "  protected flag consumer dispatches a selector that marks exactly one eligible tile for the",
        "  current repeat. The result producer then calls the ordinary calculation for that selected",
        "  target, increments the repeat index at `0x7B0763`, compares it with the count at `0x7B0762`,",
        "  and publishes the continuation bit for the next result event. Truth/Untruth choose 1..10",
        "  repeats from the exact 5/5/10/10/20/20/10/10/5/5 distribution; formula `0x5E` uses `X+1`,",
        "  giving three Tri hits and six Dark Whisper hits. This proves per-repeat target selection and",
        "  calculation rather than one aggregate calculation. The runtime reads the same native count/index",
        "  pair to retain one Magic Evade decision per target across repeats, while each status-bearing",
        "  result receives a fresh packet plan. Celestial Void, Corporeal Void, and Dark Whisper additionally",
        "  carry hostile status riders.",
        "- **Nanoflare — Proven:** formula `0x5F` aliases the same single-result MA handler but the",
        "  action has `RandomFire` clear. It is a single-hit MA action, not a multistrike mechanism.",
        "- **Barrage — Strong:** formula `0x6A` initializes the shared native repeat count to exactly four,",
        "  delegates each calculation to the equipped-weapon formula, and enters the ordinary normal-attack",
        "  postprocessor. `RandomFire` is clear, so the dedicated random-tile selector is not dispatched and",
        "  the original single target remains selected. The same result producer increments the shared index",
        "  and publishes continuation after every result. Barrage is therefore a target-stable four-repeat",
        "  weapon transaction rather than an aggregate multiplier or TableData animation sequence.",
        "",
        "## Remaining live gates",
        "",
        "1. Live-confirm the statically mapped one-target/one-calculation result event cadence through",
        "   selector, pre-clamp, HP/status apply, and repeated selection of the same target.",
        "2. Live-confirm Barrage's statically mapped four-repeat target stability through pre-clamp/apply,",
        "   plus active-hand/weapon-formula and reaction cadence.",
        "3. Validate the managed Pummel carrier with one physical contest and Guard debit per authored",
        "   strike, but no more than one reaction per Pummel action.",
        "",
        "## Relevant native windows",
        "",
        "### Common MA handler",
        "",
        *dispatch.disassembly(md, pe, raw, 0x307C0C, 0x18),
        "",
        "### Pummel aggregate count",
        "",
        *dispatch.disassembly(md, pe, raw, 0x3087D8, 0x42),
        "",
        "### Barrage weapon delegate",
        "",
        *dispatch.disassembly(md, pe, raw, 0x30D71C, 0x1F),
        "",
        "### Barrage normal-attack postprocessor trace",
        "",
        *dispatch.disassembly(md, pe, raw, BARRAGE_POSTPROCESSOR_TRACE_RVA, 0x1A1),
        "",
    ])

    ok = (
        all(actual == expected for _, actual, expected in dispatch_rows)
        and all(passed for _, passed, _ in anchor_rows)
        and all(passed for _, _, _, passed in ability_rows)
        and formula_alias_ok
        and formula_60_ok
        and barrage_postprocessor_ok
        and truth_weights_ok
        and native_repeat_source_ok
        and random_fire_retention_ok
        and native_policy_ok
        and random_fire_ok
        and barrage_tabledata_ok
    )
    return "\n".join(lines), ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--ability-type", type=Path, default=DEFAULT_ABILITY_TYPE)
    parser.add_argument("--effect-filter", type=Path, default=DEFAULT_EFFECT_FILTER)
    parser.add_argument("--ability-data", type=Path, default=DEFAULT_ABILITY_DATA)
    parser.add_argument("--job-command", type=Path, default=DEFAULT_JOB_COMMAND)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true", help="Run anchors without writing a report.")
    args = parser.parse_args()
    if not args.exe.exists():
        raise SystemExit(f"executable not found: {args.exe}")
    if not args.catalog.exists():
        raise SystemExit(f"ability catalog not found: {args.catalog}")
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-multistrike-native-carrier-analysis.md"
    report, ok = render(
        args.exe,
        args.catalog,
        output,
        args.ability_type,
        args.effect_filter,
        args.ability_data,
        args.job_command,
    )
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("all multistrike carrier checks PASS" if ok else "one or more multistrike checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
