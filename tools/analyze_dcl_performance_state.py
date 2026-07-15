#!/usr/bin/env python3
"""Verify native Song/Dance effect routing and persistent performance-state ownership."""
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


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE
DEFAULT_CATALOG = ROOT / "work" / "wotl_ability_action_baseline.csv"
FORMULA_DISPATCH_TABLE_RVA = 0x682BC8
SONG_FORMULA_ID = 0x1C
DANCE_FORMULA_ID = 0x1D
SONG_HANDLER_RVA = 0x307A6C
DANCE_HANDLER_RVA = 0x307B40


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("song-entry", SONG_HANDLER_RVA,
           "40 53 48 83 EC 20 48 8B 05 EF 34 56 01", "formula 0x1C handler entry"),
    Anchor("song-sleep-and-eligibility", 0x307A82,
           "F6 40 65 10 74 0A 88 19 C6 41 02 07 66 89 59 2C 38 19 0F 84 A0 00 00 00 E8 B9 D2 FF FF",
           "Sleep gate followed by the common target eligibility/hit helper"),
    Anchor("song-id-switch", 0x307AD7,
           "83 E9 56 74 52 83 E9 01 74 3F 83 E9 01 74 30 83 E9 01 74 25 83 E9 01 74 1A 83 E9 01 74 0B 83 F9 01",
           "exact action-id switch over 86..92"),
    Anchor("song-status-finalizer", 0x307B00, "48 83 C4 20 5B E9 7E EE FF FF",
           "Nameless Song delegates to the shared status-result finalizer"),
    Anchor("song-stat-deltas", 0x307B0A,
           "C6 40 17 81 EB 0A C6 40 16 81 EB 04 C6 40 14 81 C6 40 27 01",
           "MA, PA, and Speed use +1 sign-magnitude staged deltas"),
    Anchor("song-hp-credit", 0x307B20,
           "66 41 03 D0 C6 40 27 40 66 89 50 08", "Life's Anthem stages HP credit at result+0x08"),
    Anchor("song-mp-credit", 0x307B2E,
           "66 41 03 D0 C6 40 27 10 66 89 50 0C", "Seraph Song stages MP credit at result+0x0C"),
    Anchor("song-ct-credit", 0x307AFA, "C6 40 15 FF", "Finale stages +127 CT at result+0x15"),
    Anchor("dance-entry", DANCE_HANDLER_RVA,
           "40 53 48 83 EC 20 48 8B 05 1B 34 56 01", "formula 0x1D handler entry"),
    Anchor("dance-sleep-and-eligibility", 0x307B56,
           "F6 40 65 10 74 0A 88 19 C6 41 02 07 66 89 59 2C 38 19 0F 84 97 00 00 00 E8 E5 D1 FF FF",
           "Sleep gate followed by the common target eligibility/hit helper"),
    Anchor("dance-id-switch", 0x307B8E,
           "83 E9 5D 74 5C 83 E9 01 74 3F 83 E9 01 74 30 83 E9 01 74 25 83 E9 01 74 1A 83 E9 01 74 0B 83 F9 01",
           "exact action-id switch over 93..99"),
    Anchor("dance-status-finalizer", 0x307BB7, "48 83 C4 20 5B E9 C7 ED FF FF",
           "Forbidden Dance delegates to the shared status-result finalizer"),
    Anchor("dance-stat-deltas", 0x307BC1,
           "C6 40 17 01 EB 0A C6 40 16 01 EB 04 C6 40 14 01 C6 40 27 01",
           "MA, PA, and Speed use -1 sign-magnitude staged deltas"),
    Anchor("dance-hp-debit", 0x307BD7,
           "0F B7 0D 94 8B 4A 00 66 03 0D 8B 8B 4A 00 66 89 48 06 C6 40 27 80",
           "Mincing Minuet stages HP debit at result+0x06"),
    Anchor("dance-mp-debit", 0x307BEF,
           "0F B7 0D 7C 8B 4A 00 66 03 0D 73 8B 4A 00 66 89 48 0A C6 40 27 20",
           "Witch Hunt stages MP debit at result+0x0A"),
    Anchor("dance-ct-debit", 0x307BB1, "C6 40 15 7F", "Last Waltz stages -127 CT at result+0x15"),
    Anchor("ui-performance-action", 0x229C5D,
           "F6 40 61 09 74 07 0F BF 98 A2 01 00 00",
           "active Performing/Charging state retains exact action id at unit+0x1A2"),
    Anchor("ui-performance-action-second", 0x2E0863,
           "F6 40 61 09 74 07 0F BF 98 A2 01 00 00",
           "independent path reads the same persistent action identity"),
    Anchor("scheduler-result-apply", 0x38A659,
           "81 FA 00 03 00 00 75 07 E8 1E FE F7 FF",
           "event class 0x300 calls the ordinary battle-unit state-apply routine"),
    Anchor("scheduler-performance-cleanup", 0x38A687,
           "80 64 2B 61 F6 80 A4 2B EF 01 00 00 F6 44 88 A4 2B 8D 01 00 00",
           "event class 0x200 clears Performing/Charging mirrors and records the pending timer"),
    Anchor("scheduler-active-scan", 0x38A6C9,
           "44 38 64 28 01 74 07 F6 44 28 61 09 75 10 03 CE 48 05 00 02 00 00 48 3D 00 2A 00 00 7C E2",
           "scheduler scans all 21 unit slots for live Performing/Charging state"),
)


EFFECTS = (
    (86, "Seraph Song", "MP credit", "result+0x0C"),
    (87, "Life's Anthem", "HP credit", "result+0x08"),
    (88, "Rousing Melody", "+1 raw Speed", "result+0x14 = 0x81"),
    (89, "Battle Chant", "+1 raw PA", "result+0x16 = 0x81"),
    (90, "Magickal Refrain", "+1 raw MA", "result+0x17 = 0x81"),
    (91, "Nameless Song", "beneficial status bundle", "shared status finalizer"),
    (92, "Finale", "+127 CT bounded", "result+0x15 = 0xFF"),
    (93, "Witch Hunt", "MP debit", "result+0x0A"),
    (94, "Mincing Minuet", "HP debit", "result+0x06"),
    (95, "Slow Dance", "-1 raw Speed", "result+0x14 = 0x01"),
    (96, "Polka", "-1 raw PA", "result+0x16 = 0x01"),
    (97, "Heathen Frolic", "-1 raw MA", "result+0x17 = 0x01"),
    (98, "Forbidden Dance", "harmful status bundle", "shared status finalizer"),
    (99, "Last Waltz", "-127 CT bounded", "result+0x15 = 0x7F"),
)

EXPECTED_STATUS_PACKETS = {
    91: ("Random", {"Reraise", "Regen", "Protect", "Shell", "Haste"}),
    98: ("Random", {"Darkness", "Confusion", "Silence", "Frog", "Poison", "Slow", "Stop", "Sleep"}),
}


def load_catalog(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def dispatch_target(pe: pefile.PE, raw: bytes, formula_id: int) -> int:
    encoded = dispatch.rva_bytes(pe, raw, FORMULA_DISPATCH_TABLE_RVA + formula_id * 8, 8)
    return struct.unpack("<Q", encoded)[0] - pe.OPTIONAL_HEADER.ImageBase


def render(exe: Path, catalog_path: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)

    checks: list[tuple[Anchor, bool, str]] = []
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        checks.append((anchor, actual == expected, " ".join(f"{value:02X}" for value in actual)))

    song_target = dispatch_target(pe, raw, SONG_FORMULA_ID)
    dance_target = dispatch_target(pe, raw, DANCE_FORMULA_ID)
    dispatch_ok = song_target == SONG_HANDLER_RVA and dance_target == DANCE_HANDLER_RVA

    catalog = load_catalog(catalog_path)
    family = {
        int(row["id_dec"]): row
        for row in catalog
        if row.get("formula_hex", "").upper() in {"0X1C", "0X1D"}
    }
    catalog_ok = sorted(family) == list(range(86, 100))
    names_ok = all(
        ability_id in family
        and (family[ability_id].get("name_ivc") or family[ability_id].get("name_wotl")) == name
        for ability_id, name, _, _ in EFFECTS
    )
    packets_ok = all(
        ability_id in family
        and family[ability_id].get("inflict_status_mode") == mode
        and set(family[ability_id].get("inflict_statuses", "").split("|")) == statuses
        for ability_id, (mode, statuses) in EXPECTED_STATUS_PACKETS.items()
    )

    lines = [
        "# DCL native performance-state analysis", "",
        "Generated by `tools/analyze_dcl_performance_state.py`.", "",
        "## Inputs", "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Ability catalog: `{catalog_path}`",
        f"- Output: `{output}`", "",
        "## Dispatch and catalog checks", "",
        f"- Formula `0x1C` dispatches to `0x{song_target:X}`: **{'PASS' if song_target == SONG_HANDLER_RVA else 'FAIL'}**.",
        f"- Formula `0x1D` dispatches to `0x{dance_target:X}`: **{'PASS' if dance_target == DANCE_HANDLER_RVA else 'FAIL'}**.",
        f"- Catalog family is exactly action ids `86..99`: **{'PASS' if catalog_ok else 'FAIL'}**.",
        f"- Catalog names match the decoded switch: **{'PASS' if names_ok else 'FAIL'}**.", "",
        f"- Nameless Song/Forbidden Dance packets and `Random` mode match exactly: **{'PASS' if packets_ok else 'FAIL'}**.", "",
        "## Byte anchors", "",
        "| Name | RVA | Result | Meaning |", "| --- | ---: | --- | --- |",
    ]
    for anchor, passed, actual in checks:
        result = "PASS" if passed else f"FAIL (`{actual}`)"
        lines.append(f"| `{anchor.name}` | `0x{anchor.rva:X}` | {result} | {anchor.meaning} |")

    lines.extend([
        "", "## Exact native effects", "",
        "| Action | Name | Native effect | Staged channel |", "| ---: | --- | --- | --- |",
        *(f"| {ability_id} | {name} | {effect} | `{channel}` |"
          for ability_id, name, effect, channel in EFFECTS),
        "", "## Static interpretation", "",
        "- **Strong:** formulas `0x1C` and `0x1D` are exact-id switches over Bardsong `86..92`",
        "  and Dance `93..99`. Every numeric effect uses an ordinary staged HP, MP, raw-stat, or",
        "  CT channel; the two random bundles use the shared status-result finalizer.",
        "- **Strong:** the persistent unit state tests `unit+0x61 & 0x09`, retains the exact action id",
        "  at `unit+0x1A2`, and is independently consumed by two action/UI paths.",
        "- **Strong:** the battle scheduler scans all 21 unit slots for those Performing/Charging",
        "  bits. Its `0x300` event calls the ordinary state-apply routine; its validated `0x200`",
        "  event clears both mirrors with mask `0xF6` and records the pending timer.",
        "- **Implementation consequence:** the DCL preserves the native performance scheduler, exact",
        "  action identity, and stop/cleanup ownership. Nameless Song and Forbidden Dance use the",
        "  execution-only post-calc packet producer as complete `random-one` transactions. Its native",
        "  caster-Sleep guard prevents the managed contest from resurrecting a stopped performer.",
        "- **Remaining live gate:** observe one Song and one Dance from start through at least two",
        "  ticks and one stop cause; correlate exact id, one result/apply callback per target/tick,",
        "  and cleanup. This validates runtime cardinality and stop behavior, not a missing native",
        "  mechanism.", "",
        "## Relevant native windows", "", "### Song and Dance handlers", "",
        *dispatch.disassembly(md, pe, raw, SONG_HANDLER_RVA, DANCE_HANDLER_RVA - SONG_HANDLER_RVA),
        "", *dispatch.disassembly(md, pe, raw, DANCE_HANDLER_RVA, 0xC6),
        "", "### Persistent action identity and scheduler", "",
        *dispatch.disassembly(md, pe, raw, 0x229C54, 0x21),
        "", *dispatch.disassembly(md, pe, raw, 0x38A650, 0xA0), "",
    ])

    ok = all(passed for _, passed, _ in checks) and dispatch_ok and catalog_ok and names_ok and packets_ok
    return "\n".join(lines), ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if not args.exe.exists():
        raise SystemExit(f"executable not found: {args.exe}")
    if not args.catalog.exists():
        raise SystemExit(f"ability catalog not found: {args.catalog}")
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-performance-state-analysis.md"
    report, ok = render(args.exe, args.catalog, output)
    output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {output}")
    print("all performance-state checks PASS" if ok else "one or more performance-state checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
