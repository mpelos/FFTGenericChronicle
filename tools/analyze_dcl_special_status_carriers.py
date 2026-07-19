#!/usr/bin/env python3
"""Classify the remaining real-code formula families that call the native status finalizer."""
from __future__ import annotations

import argparse
import csv
import hashlib
import time
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

import pefile

import analyze_dcl_status_transactions as status_tx


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = status_tx.DEFAULT_EXE
DEFAULT_CATALOG = status_tx.DEFAULT_CATALOG


@dataclass(frozen=True)
class Anchor:
    formula: int
    handler_rva: int
    anchor_rva: int
    expected_hex: str
    label: str
    classification: str
    next_owner: str

    @property
    def expected(self) -> bytes:
        return bytes.fromhex(self.expected_hex)


FAMILIES = (
    Anchor(0x29, 0x308204, 0x308237,
           "4C 8B 05 32 2D 56 01 45 33 C9 45 38 08 74 32 48 8B 05 1B 2D 56 01 8A 48 06 48 8B 05 21 2D 56 01 8A 50 06 32 D1 F6 C2 E0 75 12 45 88 08 41 C6 40 02 07 66 45 89 48 2C 48 83 C4 28 C3 E8 10 E7 FF FF",
           "opposite-sex Charm", "conditional status-only", "exact-action roll plus gender/team eligibility"),
    Anchor(0x2A, 0x308280, 0x308369,
           "48 8B 15 00 2C 56 01 40 38 32 74 69 0F B7 0D FC 83 4A 00 83 E9 75 74 55 83 E9 01 74 43 83 E9 01 74 34 83 E9 01 74 22 83 E9 01 74 13 83 F9 02 74 07 E8 E9 E5 FF FF EB 3D",
           "Speechcraft status branch", "mixed conditional handler", "exact-id roll, Earplugs/eligibility, and non-status sibling routing"),
    Anchor(0x33, 0x30881C, 0x308859,
           "48 8B 05 10 27 56 01 80 38 00 74 05 E8 1E E1 FF FF",
           "Purification cure", "conditional multi-remove", "exact-action success producer before finalizer"),
    Anchor(0x3D, 0x308A20, 0x308A20,
           "48 83 EC 28 E8 43 E3 FF FF 85 C0 75 0E E8 52 E6 FF FF 85 C0 75 05 E8 4D DF FF FF 48 83 C4 28 C3",
           "random monster status", "conditional random-choice", "one roll plus one native/random status choice per target"),
    Anchor(0x3F, 0x308A6C, 0x308A9F,
           "48 8B 05 CA 24 56 01 80 38 00 74 05 E8 D8 DE FF FF",
           "Leg/Arm Shot", "conditional status-only", "exact-action Speed contest producer"),
    Anchor(0x40, 0x308AB8, 0x308AEB,
           "48 8B 0D 7E 24 56 01 33 D2 38 11 74 21 48 8B 05 69 24 56 01 F6 40 61 10 75 0F 88 11 C6 41 02 07 66 89 51 2C 48 83 C4 28 C3 E8 6F DE FF FF",
           "Seal Evil", "conditional Undead-only", "target-Undead eligibility plus exact-action roll"),
    Anchor(0x41, 0x308B20, 0x308B20,
           "48 83 EC 28 E8 5B E5 FF FF 45 33 C0 85 C0 75 40 48 8B 05 31 24 56 01 0F B7 48 08 48 8B 05 36 24 56 01 0F B7 50 08 B8 00 F0 00 00 66 33 D1 66 85 D0 75 18 48 8B 05 16 24 56 01 44 88 00 C6 40 02 07 66 44 89 40 2C 48 83 C4 28 C3 E8 18 DE FF FF 48 83 C4 28 C3",
           "Celestial Stasis", "conditional multi-add", "one per-target roll plus team/identity eligibility"),
    Anchor(0x50, 0x308E4C, 0x308E4C,
           "48 83 EC 28 E8 37 DE FF FF 85 C0 75 2E 48 8B 05 18 21 56 01 0F B6 48 3F 0F B6 05 32 79 4A 00 66 89 05 00 79 4A 00 66 89 0D F7 78 4A 00 E8 62 E2 FF FF 85 C0 75 05 E8 01 DB FF FF 48 83 C4 28 C3",
           "monster status attack", "conditional status-only/random bundle", "exact-action roll and random-bundle ownership"),
    Anchor(0x51, 0x308E8C, 0x308EC9,
           "48 8B 05 A0 20 56 01 80 38 00 74 05 E8 AE DA FF FF",
           "monster cure/buff", "conditional add/remove", "exact-action producer with complete packet ownership"),
    Anchor(0x57, 0x30905C, 0x30908D,
           "80 7F 29 63 72 11 33 C0 C6 43 02 07 88 43 27 88 03 66 89 43 2C EB 3F B8 80 00 00 00 C6 43 27 01 66 89 43 12 48 8B 05 C0 1E 56 01 48 89 05 A9 1E 56 01 48 8B 05 9A 1E 56 01 48 89 05 A3 1E 56 01 C6 00 01 E8 B3 D8 FF FF",
           "Bequeath Bacon", "dedicated lifecycle", "preserve native level gain and caster Crystal finalization"),
    Anchor(0x5A, 0x3091D4, 0x3091D8,
           "48 8B 05 89 1D 56 01 33 D2 8A 88 8E 01 00 00 48 8B 05 82 1D 56 01 80 E9 0F 80 F9 01 76 0A 88 10 C6 40 02 07 66 89 50 2C 38 10 74 05 E8 7F D7 FF FF",
           "Dragon's Charm", "conditional species-only", "dragon-species eligibility plus exact-action roll policy"),
)


EXPECTED_COUNTS = {
    0x29: 4,
    0x2A: 4,
    0x33: 1,
    0x3D: 3,
    0x3F: 2,
    0x40: 1,
    0x41: 1,
    0x50: 9,
    0x51: 3,
    0x57: 1,
    0x5A: 1,
}


def load_rows(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def render(exe: Path, catalog_path: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    targets = status_tx.dispatch_targets(pe, raw)
    rows = load_rows(catalog_path)
    formula_rows = {
        formula: [row for row in rows if row["formula_hex"].upper() == f"0X{formula:02X}" and row["inflict_statuses"].strip()]
        for formula in EXPECTED_COUNTS
    }
    counts = {formula: len(values) for formula, values in formula_rows.items()}
    counts_ok = counts == EXPECTED_COUNTS

    anchor_results = []
    for family in FAMILIES:
        actual = status_tx.rva_bytes(pe, raw, family.anchor_rva, len(family.expected))
        anchor_results.append((family, actual == family.expected, actual))
    dispatch_ok = all(targets[family.formula] == family.handler_rva for family in FAMILIES)

    direct_finalizer_formulas = status_tx.formulas_calling(pe, raw, status_tx.COMMON_STATUS_FINALIZER_RVA)
    finalizer_ok = all(family.formula in direct_finalizer_formulas for family in FAMILIES)

    bequeath = formula_rows[0x57]
    lifecycle_ok = (
        len(bequeath) == 1
        and int(bequeath[0]["id_dec"]) == 314
        and bequeath[0]["inflict_statuses"] == "Crystal"
    )

    lines = [
        "# DCL special status-carrier family analysis",
        "",
        "Generated by `tools/analyze_dcl_special_status_carriers.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Ability catalog: `{catalog_path}`",
        f"- Output: `{output}`",
        "",
        "## Formula-family anchors",
        "",
        "| Formula | Handler | Anchor | Result | Family | Classification |",
        "| --- | ---: | ---: | --- | --- | --- |",
    ]
    for family, passed, actual in anchor_results:
        result = "PASS" if passed else f"FAIL (`{actual.hex(' ').upper()}`)"
        lines.append(
            f"| `0x{family.formula:02X}` | `0x{family.handler_rva:X}` | `0x{family.anchor_rva:X}` | "
            f"{result} | {family.label} | {family.classification} |"
        )

    lines.extend([
        "",
        f"Dispatch table targets: **{'PASS' if dispatch_ok else 'FAIL'}**.",
        f"All eleven families call the common status finalizer: **{'PASS' if finalizer_ok else 'FAIL'}**.",
        "",
        "## Catalog inventory and required owner",
        "",
        "| Formula | Status actions | Ability ids | Required DCL/native owner |",
        "| --- | ---: | --- | --- |",
    ])
    for family in FAMILIES:
        family_catalog = formula_rows[family.formula]
        ids = ", ".join(row["id_dec"] for row in family_catalog)
        lines.append(
            f"| `0x{family.formula:02X}` | {len(family_catalog)} | `{ids}` | {family.next_owner} |"
        )

    lines.extend([
        "",
        f"Exact per-family catalog counts: **{'PASS' if counts_ok else 'FAIL'}**.",
        f"Bequeath Bacon id/status lifecycle identity: **{'PASS' if lifecycle_ok else 'FAIL'}**.",
        "",
        "## Closure",
        "",
        "- Ten of the eleven families are status-only conditional producers. They call the common",
        "  packet finalizer only after family-specific success and eligibility logic. As with",
        "  formulas `0x0A/0x0B`, pre-clamp packet replacement cannot resurrect a native failure.",
        "- Their packet commit mechanism is known. The execution-only post-calc producer supplies the",
        "  missing result after the conditional handler returns. Random-choice families cache one chosen",
        "  status per target; multi-status families require complete catalog-derived bit ownership;",
        "  Speechcraft preserves its non-status stat branches and Earplugs gate.",
        "- Formula `0x57` is not a generic status carrier. It couples target level gain to the caster's",
        "  Crystal transition and must remain under its dedicated native lifecycle owner.",
        "- No family in this report is added to `retained-as-carrier`; conditional actions use",
        "  post-calc replacement, which clears every catalog-owned native packet bit before staging",
        "  the DCL result. A reskin additionally declares a distinct source bit per output bit.",
        "",
        "The remaining integration dependencies are live carrier proof and authored forecast/AI",
        "probability. Execution roll consumption is gated by outer-sweep provenance plus state `0x2A`.",
        "",
    ])
    ok = (
        all(passed for _, passed, _ in anchor_results)
        and dispatch_ok
        and finalizer_ok
        and counts_ok
        and lifecycle_ok
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
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-special-status-carrier-analysis.md"
    report, ok = render(args.exe, args.catalog, output)
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("all special status-carrier checks PASS" if ok else "one or more special status-carrier checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
