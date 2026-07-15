#!/usr/bin/env python3
"""Verify the current-build paired HP/MP drain result transactions."""
from __future__ import annotations

import argparse
import csv
import hashlib
import struct
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

import analyze_dcl_reaction_dispatch as dispatch
import build_neuter_data as neuter


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE
DEFAULT_CATALOG = ROOT / "work" / "wotl_ability_action_baseline.csv"
DISPATCH_TABLE_RVA = 0x682BC8
FORMULA_TARGETS = {
    0x0F: 0x307744,
    0x10: 0x30779C,
    0x2F: 0x308660,
    0x30: 0x3086A8,
    0x47: 0x308BE8,
    0x4D: 0x308D44,
    0x65: 0x30D53C,
    0x66: 0x30D5A8,
}
EXPECTED_CATALOG_IDS = {
    0x0F: (47, 235),
    0x10: (48, 236),
    0x2F: (164,),
    0x30: (165,),
    0x47: (200, 284),
    0x4D: (274, 298),
    0x65: (45,),
    0x66: (184,),
}
HP_HELPER_RVA = 0x3062EC
HP_HELPER_TRACE_RVA = 0x1014C978
NORMALIZER_RVA = 0x30A118
NORMALIZER_TRACE_RVA = 0x102EDEB0


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor(
        "source-result-clear",
        0x281D88,
        "48 8D 0D 31 92 5E 01 BB 08 00 00 00 42 88 9C 26 A0 3E 85 01 E8 6B 86 08 00",
        "selects and clears the source-result scratch buffer before the target calculation sweep",
    ),
    Anchor(
        "per-target-calculation-loop",
        0x281EFA,
        "8A 54 1D D8 80 FA FF 74 0F 49 8B CE 44 88 2D 7F E8 52 00 E8 9A 7A 08 00 48 FF C3 48 83 FB 15 7C DF",
        "calls the universal calculation entry once for each selected target id",
    ),
    Anchor(
        "source-result-rebind",
        0x281F31,
        "48 8D 05 88 90 5E 01 48 89 05 21 90 5E 01",
        "rebinds qword[0x14186AF60] to the source-result scratch after the sweep",
    ),
    Anchor(
        "target-result-pointer",
        0x309A12,
        "49 8D 80 BE 01 00 00 48 03 C1 88 1D 3F 6D 4A 00 44 39 3D 73 15 56 01 48 89 05 40 15 56 01",
        "binds qword[0x14186AF70] to selectedTarget+0x1BE",
    ),
    Anchor(
        "source-result-pointer",
        0x309A34,
        "48 8D 05 85 15 56 01 44 88 3D 76 6D 4A 00 48 89 05 17 15 56 01",
        "binds qword[0x14186AF60] to the source-result scratch at RVA 0x186AFC0",
    ),
    Anchor(
        "mp-drain-helper",
        0x3063B4,
        "40 53 48 83 EC 20 48 8B 0D AF 4B 56 01 33 DB 0F B7 41 06 66 89 41 0A 66 89 59 06 C6 41 27 20 33 C9 E8 3E 3D 00 00 48 8B 05 8F 4B 56 01 38 18 74 16 48 8B 0D 74 4B 56 01 0F B7 40 0A 66 89 41 0C C6 41 27 10 C6 01 01 48 83 C4 20 5B C3",
        "moves target input into MP debit, normalizes it, and mirrors the final amount as source MP credit",
    ),
    Anchor(
        "hp-drain-thunk",
        HP_HELPER_RVA,
        "E9 87 66 E4 0F",
        "current-build HP-drain thunk enters trace RVA 0x1014C978",
    ),
    Anchor(
        "hp-drain-trace",
        HP_HELPER_TRACE_RVA,
        "48 83 EC 28 48 8B 05 E5 E5 71 F1 31 C9 F6 40 61 10 74 40 E8 88 D7 1B F0 4C 8B 05 D9 E5 71 F1 31 D2 41 38 10 74 5C 41 0F B7 40 06 48 8B 0D B6 E5 71 F1 66 89 41 06 C6 41 27 80 C6 01 01 41 0F B7 40 06 66 41 89 40 08 66 41 89 50 06 41 C6 40 27 40 EB 2F E8 48 D7 1B F0 4C 8B 05 99 E5 71 F1 31 D2 41 38 10 74 1C 48 8B 0D 7B E5 71 F1 41 0F B7 40 06 66 89 41 08 C6 41 27 40 C6 01 01 41 C6 40 27 80 48 83 C4 28 C3",
        "implements normal HP debit/source credit and Undead reversal with paired result records",
    ),
    Anchor(
        "normalizer-thunk",
        NORMALIZER_RVA,
        "E9 93 3D FE 0F",
        "current-build result normalizer enters trace RVA 0x102EDEB0",
    ),
    Anchor(
        "normalizer-four-channel-cap",
        0x102EE0B7,
        "0F B7 42 06 B9 9F 7A C3 DB 03 0D 3E 43 34 01 66 39 C8 7E 06 66 89 4A 06 89 C8 66 39 4A 08 7E 04 66 89 4A 08 66 39 4A 0A 7E 04 66 89 4A 0A 66 39 4A 0C 7E 04 66 89 4A 0C",
        "caps HP debit/credit and MP debit/credit result words to the shared 999 ceiling",
    ),
    Anchor(
        "shared-hp-mp-apply",
        0x30A5D7,
        "0F BF 45 06 0F B7 57 30 44 0F B7 7F 32 0F BF 4D 08 44 0F B7 47 34 2B C8 8D 04 11 8B CB 85 C0 0F 49 C8 0F BF 45 0A 41 3B CF 44 0F 4E F9 0F BF 4D 0C 2B C8 42 8D 04 01 8B CB 85 C0 0F 49 C8 0F B7 47 36 3B C8 0F 4E C1",
        "applies debit/credit arithmetic and independently clamps live HP and MP",
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


def load_catalog(path: Path) -> dict[int, list[dict[str, str]]]:
    rows = {formula_id: [] for formula_id in FORMULA_TARGETS}
    with path.open(encoding="utf-8-sig", newline="") as handle:
        for row in csv.DictReader(handle):
            formula = row.get("formula_hex", "").upper()
            for formula_id in rows:
                if formula == f"0X{formula_id:02X}":
                    rows[formula_id].append(row)
                    break
    return rows


def render(exe: Path, catalog_path: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    catalog = load_catalog(catalog_path)
    blood_drain_rows = {int(row["id_dec"]): row for row in catalog[0x47]}
    blood_suck_catalog_ok = (
        set(blood_drain_rows) == {200, 284}
        and all(row["inflict_statuses"] == "BloodSuck" for row in blood_drain_rows.values())
    )
    blood_suck_allowlist_ok = {200, 284}.issubset(neuter.DCL_STATUS_RIDER_ABILITY_IDS)

    dispatch_rows = [
        (formula_id, target_for(pe, raw, formula_id), expected)
        for formula_id, expected in FORMULA_TARGETS.items()
    ]
    catalog_checks = []
    for formula_id, rows in catalog.items():
        ids = tuple(int(row["id_dec"]) for row in rows)
        single_target = bool(rows) and all(row.get("aoe") == "0" for row in rows)
        catalog_checks.append((formula_id, ids, ids == EXPECTED_CATALOG_IDS[formula_id], single_target))

    anchor_rows = []
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        anchor_rows.append((anchor, actual == expected, actual))

    hp_trace = jump_target(pe, raw, HP_HELPER_RVA)
    normalizer_trace = jump_target(pe, raw, NORMALIZER_RVA)
    obfuscated_cap = struct.unpack(
        "<I", dispatch.rva_bytes(pe, raw, 0x11632404, 4)
    )[0]
    cap = (0xDBC37A9F + obfuscated_cap) & 0xFFFFFFFF

    lines = [
        "# DCL paired HP/MP drain transaction analysis",
        "",
        "Generated by `tools/analyze_dcl_drain_transactions.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Ability catalog: `{catalog_path}`",
        f"- Output: `{output}`",
        "",
        "## Dispatch and catalog",
        "",
        "| Formula | Handler | Expected | Catalog ids | AoE=0 | Result |",
        "| --- | ---: | ---: | --- | --- | --- |",
    ]
    catalog_by_formula = {row[0]: row for row in catalog_checks}
    for formula_id, actual, expected in dispatch_rows:
        _, ids, ids_ok, single_target = catalog_by_formula[formula_id]
        passed = actual == expected and ids_ok and single_target
        lines.append(
            f"| `0x{formula_id:02X}` | `0x{actual:X}` | `0x{expected:X}` | `{ids}` | "
            f"{'yes' if single_target else 'no'} | {'PASS' if passed else 'FAIL'} |"
        )

    lines.extend([
        "",
        "The current 12 drain records are all single-target (`aoe=0`). Multi-target source-credit",
        "cardinality is therefore not an implementation blocker for any existing drain record.",
        "",
        "## Protected-target and cap checks",
        "",
        f"- HP helper trace target: `0x{hp_trace:X}` (expected `0x{HP_HELPER_TRACE_RVA:X}`): "
        f"**{'PASS' if hp_trace == HP_HELPER_TRACE_RVA else 'FAIL'}**.",
        f"- Result normalizer trace target: `0x{normalizer_trace:X}` "
        f"(expected `0x{NORMALIZER_TRACE_RVA:X}`): "
        f"**{'PASS' if normalizer_trace == NORMALIZER_TRACE_RVA else 'FAIL'}**.",
        f"- Decoded four-channel result cap: `{cap}` (expected `999`): "
        f"**{'PASS' if cap == 999 else 'FAIL'}**.",
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
        "## Native transaction",
        "",
        "The selected target owns `qword[0x14186AF70] = target+0x1BE`. The source side uses the",
        "scratch result at RVA `0x186AFC0`, reached through `qword[0x14186AF60]`. The source scratch",
        "is cleared before the target sweep and reused by formulas that stage a paired result.",
        "",
        "| Case | Target result | Source result |",
        "| --- | --- | --- |",
        "| HP drain, ordinary target | `+0x06=amount`, `+0x27=0x80` | `+0x08=amount`, `+0x27=0x40`, active |",
        "| HP drain, Undead target | `+0x08=amount`, `+0x06=0`, `+0x27=0x40` | `+0x06=amount`, `+0x27=0x80`, active |",
        "| MP drain | `+0x0A=amount`, `+0x06=0`, `+0x27=0x20` | `+0x0C=amount`, `+0x27=0x10`, active |",
        "",
        "The shared normalizer bounds each of the four numeric result words to `999`. Native state",
        "application then computes HP as `current + credit - debit` and MP the same way, clamping",
        "each participant independently to its own `0..MaxHP` or `0..MaxMP` range. Consequently the",
        "paired source amount is the normalized staged amount; it is not reduced to the target's",
        "remaining resource before the source-side clamp.",
        "",
        "## DCL implementation boundary",
        "",
        "- **Strong:** ordinary HP drain, Undead HP-drain reversal, and MP drain are expressible by",
        "  authoring the two paired result records at the post-calculation boundary and leaving native",
        "  HP/MP clamp, presentation, KO, and status lifecycle in control.",
        "- **Strong:** formulas `0x65` and `0x66` are amount producers for the same paired HP/MP",
        "  transactions; they do not require a separate commit mechanism.",
        "- **Implementation consequence:** the 12 existing drain records have no remaining offline",
        "  transaction-discovery gap. Their amount, avoidance, element, Undead policy, and source-cap",
        "  behavior are per-ability design decisions.",
        "- **Live integration gate:** execute one authored HP drain, one Undead reversal, and one MP",
        "  drain to verify exactly one target apply plus one source apply and coherent presentation.",
        "  This gate validates integration/cardinality; it does not block the mapped fields or helper",
        "  semantics.",
        "", "## Blood Suck rider ownership", "",
        f"- Formula `0x47` has exactly Blood Drain ids 200/284 and both carry BloodSuck: **{'PASS' if blood_suck_catalog_ok else 'FAIL'}**.",
        f"- Both ids are in the ordinary damage-rider data allowlist: **{'PASS' if blood_suck_allowlist_ok else 'FAIL'}**.",
        "- Clearing only `InflictStatus` preserves the mapped target HP debit and source HP credit transaction.",
        "  DCL can therefore stage Blood Suck byte 1 mask `0x04` independently in the target packet.",
        "",
        "## Relevant native windows",
        "",
        "### Source scratch and target sweep",
        "",
        *dispatch.disassembly(md, pe, raw, 0x281D7A, 0x1A0),
        "",
        "### MP helper",
        "",
        *dispatch.disassembly(md, pe, raw, 0x3063B4, 0x4D),
        "",
        "### HP helper trace",
        "",
        *dispatch.disassembly(md, pe, raw, HP_HELPER_TRACE_RVA, 0x87),
        "",
        "### Shared result cap",
        "",
        *dispatch.disassembly(md, pe, raw, 0x102EE0B7, 0x38),
        "",
    ])

    ok = (
        all(actual == expected for _, actual, expected in dispatch_rows)
        and all(ids_ok and single_target for _, _, ids_ok, single_target in catalog_checks)
        and all(passed for _, passed, _ in anchor_rows)
        and hp_trace == HP_HELPER_TRACE_RVA
        and normalizer_trace == NORMALIZER_TRACE_RVA
        and cap == 999
        and blood_suck_catalog_ok
        and blood_suck_allowlist_ok
    )
    return "\n".join(lines), ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    if not args.exe.exists():
        raise SystemExit(f"executable not found: {args.exe}")
    if not args.catalog.exists():
        raise SystemExit(f"ability catalog not found: {args.catalog}")
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-drain-transaction-analysis.md"
    report, ok = render(args.exe, args.catalog, output)
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("all drain transaction checks PASS" if ok else "one or more drain checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
