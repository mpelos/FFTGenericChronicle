#!/usr/bin/env python3
"""Verify the current-build mechanics behind formulas 0x65..0x6A."""
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
TARGETS = {
    0x65: 0x30D53C,
    0x66: 0x30D5A8,
    0x67: 0x30D614,
    0x68: 0x30D61C,
    0x69: 0x30D6DC,
    0x6A: 0x30D71C,
}


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("hp-drain-80-percent", 0x30D57F,
           "B8 67 66 66 66 41 0F BF 49 06 C1 E1 03 F7 E9 C1 FA 02 8B C2 C1 E8 1F 03 D0 66 41 89 51 06 E8 4A 8D FF FF",
           "formula 0x65 replaces staged debit with trunc(debit*8/10) and calls HP-drain helper 0x3062EC"),
    Anchor("ordinary-hp-drain-helper", 0x3077DF,
           "66 89 50 06 E8 04 EB FF FF",
           "ordinary formula 0x10 calls the same HP-drain helper"),
    Anchor("mp-drain-80-percent", 0x30D5EB,
           "B8 67 66 66 66 41 0F BF 49 06 C1 E1 03 F7 E9 C1 FA 02 8B C2 C1 E8 1F 03 D0 66 41 89 51 06 E8 A6 8D FF FF",
           "formula 0x66 replaces staged debit with trunc(debit*8/10) and calls MP-drain helper 0x3063B4"),
    Anchor("ordinary-mp-drain-helper", 0x307783,
           "66 89 50 0A 66 89 50 06 C6 40 27 20 E8 20 EC FF FF",
           "ordinary formula 0x0F calls the same MP-drain helper"),
    Anchor("formula-67-alias", 0x30D614,
           "E9 A7 AE FF FF",
           "formula 0x67 jumps directly to the formula-0x2D handler at 0x3084C0"),
    Anchor("formula-68-position-read", 0x30D640,
           "41 0F B6 49 4F 41 0F B6 40 4F 2B C1 41 0F B6 49 50 99 44 8B D0 41 0F B6 40 50",
           "formula 0x68 compares the two unit-position byte pairs"),
    Anchor("formula-68-power-branches", 0x30D66E,
           "41 83 EA 01 74 24 41 83 FA 01 74 15 0F BF 05 EF 30 4A 00 99 2B C2 D1 F8 66 89 05 E3 30 4A 00",
           "distance band halves, increments, or adds three to the power term"),
    Anchor("formula-68-output-chain", 0x30D6A0,
           "E8 0F 85 FF FF E8 86 97 FF FF E8 01 88 FF FF E8 50 88 FF FF 8A 0D F4 30 4A 00 E8 91 7E FF FF",
           "formula 0x68 uses the ordinary damage/status output chain"),
    Anchor("formula-69-power", 0x30D6E5,
           "41 0F B6 41 3F 66 89 05 7F 30 4A 00 41 0F B6 49 3F 41 0F B7 41 32 44 8D 04 89 41 0F B6 49 3E 8D 04 40 45 03 C0 41 F7 F0 66 03 C1",
           "builds Y = PA + floor((3*MaxHP)/(10*MA))"),
    Anchor("formula-69-formula-4e-tail", 0x30D710,
           "66 89 05 5B 30 4A 00 E9 2C 98 FF FF",
           "stores derived Y and enters the formula-0x4E non-Faith MA damage pipeline"),
    Anchor("formula-6a-dynamic-dispatch", 0x30D720,
           "0F B6 05 83 30 4A 00 48 8D 0D A2 54 37 00 FF 54 C1 F8",
           "reads equipped-weapon formula id and calls that entry in the ordinary dispatch table"),
    Anchor("formula-6a-normal-attack-tail", 0x30D732,
           "48 83 C4 28 E9 91 90 FF FF",
           "enters the native normal-attack postprocessor after delegated weapon calculation"),
)


def target_for(pe: pefile.PE, raw: bytes, formula_id: int) -> int:
    data = dispatch.rva_bytes(pe, raw, DISPATCH_TABLE_RVA + formula_id * 8, 8)
    return struct.unpack("<Q", data)[0] - pe.OPTIONAL_HEADER.ImageBase


def load_catalog(path: Path) -> dict[int, tuple[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return {
            int(row["formula_hex"], 16): (
                row.get("name_ivc") or row.get("name_wotl") or "(unnamed)", row["id_dec"]
            )
            for row in csv.DictReader(handle)
            if row.get("formula_hex", "").upper() in {f"0X{value:02X}" for value in TARGETS}
        }


def render(exe: Path, catalog_path: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    catalog = load_catalog(catalog_path)
    with catalog_path.open(encoding="utf-8-sig", newline="") as handle:
        status_rows = {int(row["id_dec"]): row for row in csv.DictReader(handle) if row["inflict_statuses"].strip()}
    rider_catalog_ok = (
        status_rows[219]["formula_hex"].upper() == "0X67"
        and status_rows[219]["inflict_status_mode"] == "Separate"
        and status_rows[219]["inflict_statuses"] == "Stop"
        and status_rows[357]["formula_hex"].upper() == "0X69"
        and status_rows[357]["inflict_status_mode"] == "AllOrNothing"
        and status_rows[357]["inflict_statuses"] == "Slow"
    )
    rider_allowlist_ok = {219, 357}.issubset(neuter.DCL_STATUS_RIDER_ABILITY_IDS)

    dispatch_rows = [(formula, target_for(pe, raw, formula), expected) for formula, expected in TARGETS.items()]
    catalog_ok = sorted(catalog) == sorted(TARGETS)
    anchor_rows = []
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        anchor_rows.append((anchor, actual == expected, " ".join(f"{value:02X}" for value in actual)))

    lines = [
        "# DCL formulas 0x65..0x6A analysis", "",
        "Generated by `tools/analyze_dcl_dark_knight_formulas.py`.", "",
        "## Inputs", "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Ability catalog: `{catalog_path}`",
        f"- Output: `{output}`", "",
        "## Dispatch and catalog", "",
        "| Formula | Ability | Actual handler | Expected | Result |", "| --- | --- | ---: | ---: | --- |",
        *(f"| `0x{formula:02X}` | {catalog.get(formula, ('missing', '?'))[1]}:{catalog.get(formula, ('missing', '?'))[0]} | "
          f"`0x{actual:X}` | `0x{expected:X}` | {'PASS' if actual == expected else 'FAIL'} |"
          for formula, actual, expected in dispatch_rows),
        "", f"Catalog has exactly one member for each formula: **{'PASS' if catalog_ok else 'FAIL'}**.",
        "", "## Byte anchors", "",
        "| Name | RVA | Result | Meaning |", "| --- | ---: | --- | --- |",
    ]
    for anchor, passed, actual in anchor_rows:
        result = "PASS" if passed else f"FAIL (`{actual}`)"
        lines.append(f"| `{anchor.name}` | `0x{anchor.rva:X}` | {result} | {anchor.meaning} |")

    lines.extend([
        "", "## Static interpretation", "",
        "- **Strong:** `0x65` and `0x66` are 80%-scaled variants of the established HP- and MP-drain",
        "  transactions. Their final calls are the same helpers used by formulas `0x10` and `0x0F`.",
        "- **Strong:** `0x67` is not a new mechanism; it is a direct wrapper alias of formula `0x2D`.",
        "- **Strong:** `0x68` derives a distance-band power adjustment from two unit-position bytes,",
        "  then uses the ordinary damage/status staging chain. Its exact authored DCL identity and",
        "  power policy are design inputs, not an unknown commit mechanism.",
        "- **Strong:** `0x69` derives `Y = PA + floor((3*MaxHP)/(10*MA))` and enters the formula-`0x4E`",
        "  non-Faith MA damage pipeline. The catalog Slow rider is handled by the status contest.",
        "- **Strong:** `0x6A` dynamically dispatches the equipped weapon's formula and then enters the",
        "  normal-attack postprocessor. Its remaining gap is multistrike cardinality/integration, not",
        "  formula identity.",
        "", "## Data-suppressed status riders", "",
        f"- Crushing Blow `219` is the formula-`0x67` Stop rider and Unholy Sacrifice `357` is the formula-`0x69` Slow rider: **{'PASS' if rider_catalog_ok else 'FAIL'}**.",
        f"- Both exact ids are in the ordinary damage-rider data allowlist: **{'PASS' if rider_allowlist_ok else 'FAIL'}**.",
        "- Formula `0x67` inherits the same independent HP carrier as the already accepted `0x2D` riders.",
        "- Formula `0x69` derives its power and unconditionally enters the formula-`0x4E` non-Faith damage pipeline.",
        "  Clearing only `InflictStatus` preserves each HP result for managed packet replacement.",
        "", "## Relevant native window", "",
        *dispatch.disassembly(md, pe, raw, 0x30D53C, 0x204), "",
    ])

    ok = catalog_ok and rider_catalog_ok and rider_allowlist_ok and all(
        actual == expected for _, actual, expected in dispatch_rows
    ) and all(passed for _, passed, _ in anchor_rows)
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
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-formula-65-6a-analysis.md"
    report, ok = render(args.exe, args.catalog, output)
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("all formula-0x65..0x6A checks PASS" if ok else "one or more formula-0x65..0x6A checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
