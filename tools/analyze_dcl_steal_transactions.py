#!/usr/bin/env python3
"""Verify native equipment/Gil/EXP Steal transaction surfaces in the current executable."""
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
FORMULA_TARGETS = {0x26: 0x307FC8, 0x27: 0x308094, 0x28: 0x3080F4}


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("equipment-selector-entry", 0x3065C0,
           "40 53 48 83 EC 20 4C 8B 05 A3 49 56 01 4C 8B 0D 94 49 56 01",
           "shared exact-id equipment selector entry"),
    Anchor("equipment-selector-id-base", 0x3065E0,
           "0F B7 0D 91 A1 4A 00 B8 A0 00 00 00 3B C8 77 61 0F 84 0F 01 00 00 83 E9 6E",
           "loads exact action id and begins the Steal/Break/Plunder switch at id 110"),
    Anchor("accessory-slot", 0x306693,
           "41 BA FF 00 00 00 66 45 39 51 1E 74 A3 41 8D 42 06 66 41 3B 41 1E 76 98 41 C6 40 1B 20",
           "Accessory reads target+0x1E and stages slot mask 0x20"),
    Anchor("shield-slots", 0x3066BA,
           "B8 05 01 00 00 44 8D 50 FA 66 45 39 51 22 74 16 66 41 3B 41 22 76 0F 41 C6 40 1B 08",
           "Shield checks target+0x22 then target+0x26 and stages the matching hand mask"),
    Anchor("armor-slot", 0x306705,
           "41 BA FF 00 00 00 66 45 39 51 1C 0F 84 2D FF FF FF 41 8D 42 06 66 41 3B 41 1C",
           "Armor reads target+0x1C and stages slot mask 0x40"),
    Anchor("weapon-slots", 0x306734,
           "41 0F B6 49 20 41 83 CB FF 41 0B DB 41 BA FF 00 00 00 41 3A CA",
           "Weapon evaluates target+0x20 and target+0x24 before choosing a hand mask"),
    Anchor("helm-slot", 0x306793,
           "41 BA FF 00 00 00 66 45 39 51 1A 0F 84 9F FE FF FF 41 8D 42 06 66 41 3B 41 1A",
           "Helm reads target+0x1A and stages slot mask 0x80"),
    Anchor("selected-item-write", 0x3067BD,
           "66 41 89 40 04 33 C0 E9 82 FE FF FF",
           "writes selected item id to result+0x04"),
    Anchor("equipment-steal-success", 0x308085,
           "B8 10 00 00 00 66 41 89 42 12",
           "formula 0x26 success writes result+0x12 = 0x0010"),
    Anchor("gil-paired-stage", 0x305FFD,
           "48 8B 05 5C 4F 56 01 66 89 58 0E 66 F7 DB C6 00 01 C6 40 27 01 48 8B 05 57 4F 56 01 66 89 58 0E",
           "formula 0x27 helper stages equal/opposite result+0x0E values"),
    Anchor("exp-paired-stage", 0x3081C8,
           "48 8B 05 91 2D 56 01 88 48 2A 80 C1 80 C6 00 01 C6 40 27 01 41 88 48 2A 41 C6 40 27 01",
           "formula 0x28 stages paired result+0x2A values with the target subtraction tag"),
    Anchor("state-apply-gil-exp", 0x30A6F0,
           "66 39 58 0E 75 05 38 58 2A 74 03 41 0B F5 0F BF 50 0E 45 33 C0 48 8B 0D 5C 08 56 01 E8 23 28 00 00",
           "state apply forwards result+0x0E and then applies result+0x2A"),
    Anchor("exp-add-path", 0x30A725,
           "0F B6 50 2A 75 5D 84 D2 78 41 0F B6 4F 28 B8 FF 00 00 00 66 03 CA",
           "clear high bit adds EXP and bounds at 255"),
    Anchor("exp-subtract-path", 0x30A770,
           "80 E2 7F 8B CB 0F B6 C2 0F B6 57 28 66 2B D0 0F B6 C2 0F 49 C8 88 4F 28",
           "high bit selects bounded EXP subtraction"),
    Anchor("post-calc-boundary", 0x281F0D,
           "E8 9A 7A 08 00 48 FF C3 48 83 FB 15",
           "computeActionResult returns immediately before the existing target-sweep hook boundary"),
)


SLOT_ROWS = (
    ("Helm", "110/361", "target+0x1A", "0x80"),
    ("Armor", "111/362", "target+0x1C", "0x40"),
    ("Shield", "112/363", "target+0x22 or +0x26", "0x08 or 0x02"),
    ("Weapon", "113/364", "target+0x20 or +0x24", "0x10 or 0x04"),
    ("Accessory", "114/365", "target+0x1E", "0x20"),
)


def dispatch_target(pe: pefile.PE, raw: bytes, formula_id: int) -> int:
    data = dispatch.rva_bytes(pe, raw, FORMULA_DISPATCH_TABLE_RVA + formula_id * 8, 8)
    return struct.unpack("<Q", data)[0] - pe.OPTIONAL_HEADER.ImageBase


def load_steal_catalog(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return [
            row for row in csv.DictReader(handle)
            if row.get("formula_hex", "").upper() in {"0X26", "0X27", "0X28"}
        ]


def render(exe: Path, catalog_path: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)

    dispatch_rows = [
        (formula_id, dispatch_target(pe, raw, formula_id), expected)
        for formula_id, expected in FORMULA_TARGETS.items()
    ]
    dispatch_ok = all(actual == expected for _, actual, expected in dispatch_rows)

    anchor_rows = []
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        anchor_rows.append((anchor, actual == expected, " ".join(f"{value:02X}" for value in actual)))

    catalog = load_steal_catalog(catalog_path)
    ids = sorted(int(row["id_dec"]) for row in catalog)
    expected_ids = [108, *range(110, 116), 307, 359, *range(361, 367)]
    catalog_ok = ids == expected_ids

    lines = [
        "# DCL native Steal transaction analysis", "",
        "Generated by `tools/analyze_dcl_steal_transactions.py`.", "",
        "## Inputs", "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Ability catalog: `{catalog_path}`",
        f"- Output: `{output}`", "",
        "## Dispatch and catalog checks", "",
        "| Formula | Actual handler | Expected | Result |", "| --- | ---: | ---: | --- |",
        *(f"| `0x{formula_id:02X}` | `0x{actual:X}` | `0x{expected:X}` | {'PASS' if actual == expected else 'FAIL'} |"
          for formula_id, actual, expected in dispatch_rows),
        "",
        f"Catalog is exactly the 15 formula-`0x26..0x28` Steal/Plunder records: **{'PASS' if catalog_ok else 'FAIL'}**.",
        "", "## Byte anchors", "",
        "| Name | RVA | Result | Meaning |", "| --- | ---: | --- | --- |",
    ]
    for anchor, passed, actual in anchor_rows:
        result = "PASS" if passed else f"FAIL (`{actual}`)"
        lines.append(f"| `{anchor.name}` | `0x{anchor.rva:X}` | {result} | {anchor.meaning} |")

    lines.extend([
        "", "## Equipment transaction map", "",
        "| Slot | Exact action ids | Equipment source | Staged mask |", "| --- | --- | --- | ---: |",
        *(f"| {slot} | `{ids_text}` | `{source}` | `{mask}` |"
          for slot, ids_text, source, mask in SLOT_ROWS),
        "", "## Static interpretation", "",
        "- **Strong:** formula `0x26` uses exact action identity to select a real equipped item,",
        "  writes its item id to `result+0x04`, writes the exact equipment-slot mask to",
        "  `result+0x1B`, and writes success gate `result+0x12 = 0x0010`.",
        "- **Strong:** formula `0x27` stages an equal/opposite `result+0x0E` pair; state apply",
        "  forwards that signed value to the native campaign-value bridge. The native transaction",
        "  remains the owner of Gil mutation.",
        "- **Strong:** formula `0x28` stages paired EXP bytes at `result+0x2A`. For this field only,",
        "  a clear high bit means add and `0x80 | magnitude` means subtract; state apply bounds live",
        "  `unit+0x28` to `0..255` and runs the ordinary progression refresh.",
        "- **Strong implementation surface:** `0x281F12` is immediately after the per-target",
        "  `computeActionResult` call and before application. The DCL can author success/failure at",
        "  this staged-result boundary while preserving native item/Gil/EXP commit ownership.",
        "- **Remaining live gate:** force one natural equipment-Steal failure to success and one",
        "  success to failure at the staged result, verifying exact item/slot transfer cardinality,",
        "  no duplicate inventory, and forecast/execution separation.",
        "", "## Relevant native windows", "", "### Equipment selector and formula 0x26", "",
        *dispatch.disassembly(md, pe, raw, 0x3065C0, 0x20C),
        "", *dispatch.disassembly(md, pe, raw, 0x307FC8, 0xCC),
        "", "### Gil/EXP staging and state apply", "",
        *dispatch.disassembly(md, pe, raw, 0x305FA4, 0x80),
        "", *dispatch.disassembly(md, pe, raw, 0x3080F4, 0x10E),
        "", *dispatch.disassembly(md, pe, raw, 0x30A6F0, 0x98), "",
    ])

    ok = dispatch_ok and catalog_ok and all(passed for _, passed, _ in anchor_rows)
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
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-steal-transaction-analysis.md"
    report, ok = render(args.exe, args.catalog, output)
    output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {output}")
    print("all Steal transaction checks PASS" if ok else "one or more Steal transaction checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
