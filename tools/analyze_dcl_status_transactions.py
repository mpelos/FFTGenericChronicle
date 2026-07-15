#!/usr/bin/env python3
"""Verify the current-build native status packet, validator, and apply transaction."""
from __future__ import annotations

import argparse
import csv
import hashlib
import struct
import time
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

import pefile

import build_neuter_data as neuter


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = Path(
    r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"
)
DEFAULT_CATALOG = ROOT / "work" / "wotl_ability_action_baseline.csv"
DISPATCH_TABLE_RVA = 0x682BC8
FORMULA_COUNT = 162
COMMON_STATUS_FINALIZER_RVA = 0x306988
HOSTILE_HP_STATUS_IDS = {
    80, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137,
    155, 156, 157, 158, 159, 173, 179, 187, 202, 203, 204, 209, 210,
    211, 219, 277, 331, 344, 352,
}


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected_hex: str
    meaning: str

    @property
    def expected(self) -> bytes:
        return bytes.fromhex(self.expected_hex)


ANCHORS = (
    Anchor(
        "status-finalizer-thunk",
        0x306988,
        "E9 43 4E F0 0F",
        "enters the protected common status-packet finalizer",
    ),
    Anchor(
        "status-finalizer-packet-scan",
        0x1020B9E2,
        "44 88 E2 48 8D 4B 1D 8A 41 05 0A 01 48 FF C1 08 C2 48 83 ED 01 75 F0",
        "scans the paired five-byte packet at result+0x1D and result+0x22",
    ),
    Anchor(
        "status-packet-validator-thunk",
        0x3052D8,
        "E9 2B 5F CD 0F",
        "enters the protected immunity/current-state packet validator",
    ),
    Anchor(
        "status-packet-validator-add-remove",
        0x0FFDB329,
        "4A 8D 04 19 48 83 F8 03 7D 06 8A 44 11 44 EB 04 8A 44 0A 3A 0A 44 11 3F F6 D0 20 01",
        "filters add bits against effective/immunity and remove bits against source state",
    ),
    Anchor(
        "status-commit-thunk",
        0x30C878,
        "E9 03 8B 0E 10",
        "enters the native per-target status commit transaction",
    ),
    Anchor(
        "ordinary-apply-calls-status-commit",
        0x30A99D,
        "33 C9 E8 34 A9 FF FF 8B 47 61 33 D2 0F 10 47 66 89 44 24 68 41 8B CE 8A 47 65 88 44 24 6C F3 0F 7F 44 24 20 E8 B2 1E 00 00",
        "validates the staged packet and unconditionally calls status commit in ordinary apply",
    ),
    Anchor(
        "status-remove-thunk",
        0x30C9FC,
        "E9 F9 D8 0F 10",
        "enters the native remove-bit consumer",
    ),
    Anchor(
        "status-remove-master-write",
        0x1040A32B,
        "89 F8 89 F9 83 E1 07 48 C1 E8 03 BA 80 00 00 00 D3 FA 84 94 18 E0 01 00 00 74 39 F6 D2 41 B8 01 00 00 00 20 94 18 EF 01 00 00",
        "consumes unit+0x1E0 remove bits and clears durable unit+0x1EF bits",
    ),
    Anchor(
        "status-add-thunk",
        0x30CAA4,
        "E9 A8 26 10 10",
        "enters the native add-bit consumer",
    ),
    Anchor(
        "status-add-master-write",
        0x1040F1DD,
        "89 FD 89 F9 83 E1 07 48 C1 ED 03 BE 80 00 00 00 89 FB D3 FE 40 84 74 2A 1D 0F 84 9C 01 00 00",
        "consumes unit+0x1DB add bits before OR-ing durable unit+0x1EF",
    ),
)


def rva_bytes(pe: pefile.PE, raw: bytes, rva: int, size: int) -> bytes:
    offset = pe.get_offset_from_rva(rva)
    return raw[offset : offset + size]


def dispatch_targets(pe: pefile.PE, raw: bytes) -> dict[int, int]:
    base = pe.OPTIONAL_HEADER.ImageBase
    return {
        formula_id: struct.unpack(
            "<Q", rva_bytes(pe, raw, DISPATCH_TABLE_RVA + formula_id * 8, 8)
        )[0]
        - base
        for formula_id in range(FORMULA_COUNT)
    }


def direct_callers(pe: pefile.PE, target_rva: int) -> list[int]:
    callers: list[int] = []
    for section in pe.sections:
        section_rva = section.VirtualAddress
        data = section.get_data()
        cursor = 0
        while True:
            cursor = data.find(b"\xE8", cursor)
            if cursor < 0 or cursor + 5 > len(data):
                break
            relative = struct.unpack_from("<i", data, cursor + 1)[0]
            if section_rva + cursor + 5 + relative == target_rva:
                callers.append(section_rva + cursor)
            cursor += 1
    return sorted(callers)


def formulas_calling(pe: pefile.PE, raw: bytes, target_rva: int) -> dict[int, int]:
    targets = dispatch_targets(pe, raw)
    starts = sorted(set(targets.values()))
    result: dict[int, int] = {}
    for caller in direct_callers(pe, target_rva):
        # Protected traces can call back through the real-code thunk. Formula ownership is derived
        # only from the compact real-code dispatch region, not from those high-RVA callbacks.
        if caller >= 0x610000:
            continue
        candidates = [start for start in starts if start <= caller]
        if not candidates:
            continue
        start = candidates[-1]
        end = next((value for value in starts if value > start), None)
        if end is None or caller >= end:
            continue
        for formula_id, handler in targets.items():
            if handler == start:
                result[formula_id] = caller
    return result


def load_catalog(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def yes(value: str) -> bool:
    return value.strip().lower() in {"1", "true", "yes"}


def render(exe: Path, catalog_path: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    catalog = load_catalog(catalog_path)
    anchor_rows = []
    for anchor in ANCHORS:
        actual = rva_bytes(pe, raw, anchor.rva, len(anchor.expected))
        anchor_rows.append((anchor, actual == anchor.expected))

    formula_callers = formulas_calling(pe, raw, COMMON_STATUS_FINALIZER_RVA)
    expected_formula_callers = {
        0x0A, 0x0B, 0x29, 0x2A, 0x33, 0x3D, 0x3F,
        0x40, 0x41, 0x50, 0x51, 0x57, 0x5A,
    }
    formula_callers_ok = set(formula_callers) == expected_formula_callers

    status_rows = [row for row in catalog if row["inflict_statuses"].strip()]
    status_ids = {int(row["id_dec"]) for row in status_rows}
    hostile_hp_candidates = HOSTILE_HP_STATUS_IDS & status_ids
    ordinary_damage_riders = set(neuter.DCL_STATUS_RIDER_ABILITY_IDS) & status_ids
    catalog_ok = (
        len(catalog) == 512
        and len(status_ids) == 150
        and len(hostile_hp_candidates) == 32
        and len(ordinary_damage_riders) == 26
        and max(status_ids) <= 367
    )
    status_formula_counts = Counter(row["formula_hex"] for row in status_rows)

    lines = [
        "# DCL native status-transaction analysis",
        "",
        "Generated by `tools/analyze_dcl_status_transactions.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Ability catalog: `{catalog_path}`",
        f"- Output: `{output}`",
        "",
        "## Anchor verification",
        "",
        "| Anchor | RVA | Result | Meaning |",
        "| --- | ---: | --- | --- |",
    ]
    for anchor, passed in anchor_rows:
        lines.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | {'PASS' if passed else 'FAIL'} | {anchor.meaning} |"
        )

    lines.extend([
        "",
        "## Formula convergence",
        "",
        "Thirteen real-code formula handlers call the same protected status finalizer at",
        "`0x306988`:",
        "",
        "| Formula | Handler call site | Catalog status abilities |",
        "| --- | ---: | ---: |",
    ])
    for formula_id, caller in sorted(formula_callers.items()):
        count = status_formula_counts.get(f"0x{formula_id:02X}", 0)
        lines.append(f"| `0x{formula_id:02X}` | `0x{caller:X}` | {count} |")

    lines.extend([
        "",
        f"Formula-call inventory: **{'PASS' if formula_callers_ok else 'FAIL'}**.",
        "",
        "## Native packet contract",
        "",
        "The selected target owns two five-byte staged status masks immediately before the result",
        "flag byte:",
        "",
        "- `unit+0x1DB..+0x1DF`: add-status packet;",
        "- `unit+0x1E0..+0x1E4`: remove-status packet;",
        "- `unit+0x1E5` low bit `0x08`: status/effect presentation route.",
        "",
        "The protected validator filters add bits against the target's effective and immunity arrays",
        "and filters removals so equipment/innate source bits survive. The ordinary result apply path",
        "calls the status transaction at `0x30C878`; its remove consumer clears durable master bits and",
        "its add consumer ORs durable master bits, invokes native per-status side effects, then rebuilds",
        "the effective state. This is the native carrier DCL should preserve instead of directly",
        "recreating every status animation and lifecycle side effect.",
        "",
        "## Ability inventory",
        "",
        f"- Catalog records: **{len(catalog)}**.",
        f"- Abilities with one or more native status tokens: **{len(status_ids)}**.",
        f"- Status abilities carrying hostile HP AI metadata: **{len(hostile_hp_candidates)}**.",
        f"- Conservative ordinary single-result damage+rider subset: **{len(ordinary_damage_riders)}**.",
        f"- Status-only, cure, lifecycle, periodic, multihit, KO, or special carriers: **{len(status_ids - ordinary_damage_riders)}**.",
        f"- Highest status ability id: **{max(status_ids)}**; all fit the 368-row override table.",
        "",
        "The 26 ordinary single-result damage+rider abilities are:",
        "",
        "`" + ", ".join(str(value) for value in sorted(ordinary_damage_riders)) + "`.",
        "",
        "The wider 32-row HP metadata set also contains dedicated instant-KO, RandomFire,",
        "status-only, self/caster, and custom-formula records; HP AI metadata alone is therefore not",
        "accepted as proof of a safe independent damage carrier.",
        "",
        "Catalog inventory: **" + ("PASS" if catalog_ok else "FAIL") + "**.",
        "",
        "## Implementation boundary",
        "",
        "For the conservative 26-action subset, `InflictStatus=0` can suppress the native rider while the HP result",
        "still reaches the proven DCL apply window; the managed contest can then author the add/remove",
        "packet. RandomFire and other repeated carriers remain cardinality-gated.",
        "",
        "For a status-only or special action, deleting `InflictStatus` can erase the only native result carrier",
        "before apply. The remaining 124 actions need their dedicated lifecycle/cardinality owner or",
        "formula/calc provenance plus an output hook that stages",
        "the DCL packet while preserving native forecast, AI scoring, connect/miss, and presentation.",
        "The `0x30C878` transaction is the correct execution commit, but by itself it cannot resurrect",
        "an action whose formula produced no result. This is an offline-proven structural boundary, not",
        "a reason to convert status skills into fake zero-damage actions.",
        "",
    ])
    ok = all(passed for _, passed in anchor_rows) and formula_callers_ok and catalog_ok
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
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-status-transaction-analysis.md"
    report, ok = render(args.exe, args.catalog, output)
    if not args.check_only:
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("all native status-transaction checks PASS" if ok else "one or more status-transaction checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
