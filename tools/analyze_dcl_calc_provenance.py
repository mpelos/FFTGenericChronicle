#!/usr/bin/env python3
"""Verify calc-entry callers, nested formula re-entry, and phase-signal boundaries."""
from __future__ import annotations

import argparse
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
CALC_ENTRY_RVA = 0x3099AC
OUTER_SWEEP_ENTRY_RVA = 0x281CE8
OUTER_CALL_RVA = 0x281F0D
NESTED_FORMULA_ENTRY_RVA = 0x307E70
NESTED_CALL_RVA = 0x307ED0
FORECAST_TRACE_CALL_RVA = 0xEF53F0F
FORMULA_DISPATCH_TABLE_RVA = 0x682BC8
NESTED_FORMULA_ID = 0x25
FORECAST_GLOBAL_RVA = 0x2FF3CF8
BATTLE_STATE_GLOBAL_RVA = 0xC6B1CC


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("calc-entry", CALC_ENTRY_RVA, "48 89 5C 24 18 55 56 57", "computeActionResult prologue"),
    Anchor("outer-sweep-entry", OUTER_SWEEP_ENTRY_RVA, "48 89 5C 24 18 55 56 57", "affected-target sweep"),
    Anchor("outer-calc-call", OUTER_CALL_RVA, "E8 9A 7A 08 00", "ordinary per-target calc call"),
    Anchor("nested-save-type", 0x307EA7, "40 8A B1 A1 01 00 00", "save outer order type"),
    Anchor("nested-save-id", 0x307EAE, "0F B7 B9 A2 01 00 00", "save outer ability id"),
    Anchor("nested-force-attack-type", 0x307EB5, "C6 81 A1 01 00 00 01", "temporarily write type 1"),
    Anchor("nested-force-attack-id", 0x307EBC, "66 89 A9 A2 01 00 00", "temporarily write ability id 0"),
    Anchor("nested-calc-call", NESTED_CALL_RVA, "E8 D7 1A 00 00", "formula-internal calc re-entry"),
    Anchor("forecast-trace-calc-call", FORECAST_TRACE_CALL_RVA, "E8 98 5A 3B F1", "forecast calc call in executable .trace code"),
    Anchor("nested-restore-id", 0x307EE2, "66 89 B8 A2 01 00 00", "restore outer ability id"),
    Anchor("nested-restore-type", 0x307EE9, "40 88 B0 A1 01 00 00", "restore outer order type"),
    Anchor("forecast-compute-call", 0x229EA5, "E8 B6 B3 0D 00", "UI builder computes forecast before publishing pointer"),
    Anchor("forecast-object-address", 0x229F1B, "48 8D 87 BE 01 00 00", "derive target+0x1BE"),
    Anchor("forecast-pointer-publish", 0x229F22, "48 89 05 CF 9D DC 02", "publish UI forecast pointer"),
    Anchor("state-15-apply-call", 0x211FCB, "E8 04 A0 FF FF", "state 0x15 enters apply handler"),
    Anchor("state-15-check", 0x211FD0, "83 3D F5 91 A5 00 15", "dispatcher checks shared state 0x15"),
    Anchor("pre-clamp-execution", 0x30A5D7, "0F BF 45 06 0F B7 57 30", "execution-only staged HP apply surface"),
    Anchor("result-selector-execution", 0x205210, "48 89 5C 24 08 48 89 6C", "execution-only result selector"),
)


def all_direct_callers(pe: pefile.PE, raw: bytes, target_rva: int) -> list[int]:
    """Find direct rel32 calls across every executable section, including .trace."""
    result: list[int] = []
    for section in dispatch.executable_sections(pe):
        blob = raw[section.PointerToRawData : section.PointerToRawData + section.SizeOfRawData]
        cursor = 0
        while True:
            index = blob.find(b"\xE8", cursor)
            if index < 0 or index + 5 > len(blob):
                break
            caller = section.VirtualAddress + index
            relative = int.from_bytes(blob[index + 1 : index + 5], "little", signed=True)
            if caller + 5 + relative == target_rva:
                result.append(caller)
            cursor = index + 1
    return sorted(result)


def parse_hex(value: str) -> bytes:
    return bytes.fromhex(value)


def hex_bytes(value: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in value)


def render_report(exe: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    md = Cs(CS_ARCH_X86, CS_MODE_64)

    rows: list[str] = []
    anchors_ok = True
    for anchor in ANCHORS:
        expected = parse_hex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        ok = actual == expected
        anchors_ok &= ok
        result = "PASS" if ok else f"FAIL (`{hex_bytes(actual)}`)"
        rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | {result} | `{anchor.expected}` | {anchor.meaning} |"
        )

    real_code_calc_callers = dispatch.direct_callers(pe, raw, CALC_ENTRY_RVA)
    calc_callers = all_direct_callers(pe, raw, CALC_ENTRY_RVA)
    outer_callers = dispatch.direct_callers(pe, raw, OUTER_SWEEP_ENTRY_RVA)
    callers_ok = (
        real_code_calc_callers == [OUTER_CALL_RVA, NESTED_CALL_RVA]
        and calc_callers == [OUTER_CALL_RVA, NESTED_CALL_RVA, FORECAST_TRACE_CALL_RVA]
        and outer_callers == []
    )

    table_bytes = dispatch.rva_bytes(
        pe, raw, FORMULA_DISPATCH_TABLE_RVA + NESTED_FORMULA_ID * 8, 8
    )
    formula_target_va = struct.unpack("<Q", table_bytes)[0]
    formula_target_rva = formula_target_va - image_base
    formula_ok = formula_target_rva == NESTED_FORMULA_ENTRY_RVA

    lines = [
        "# DCL calc-entry provenance analysis",
        "",
        "Generated by `tools/analyze_dcl_calc_provenance.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Output: `{output}`",
        "",
        "## Byte anchors",
        "",
        "| Name | RVA | Result | Expected bytes | Meaning |",
        "| --- | ---: | --- | --- | --- |",
        *rows,
        "",
        "## Exhaustive executable call graph",
        "",
        f"- `computeActionResult 0x{CALC_ENTRY_RVA:X}` aligned real-code direct callers: "
        + (", ".join(f"`0x{rva:X}`" for rva in real_code_calc_callers) or "none") + ".",
        f"- `computeActionResult 0x{CALC_ENTRY_RVA:X}` direct callers: "
        + (", ".join(f"`0x{rva:X}`" for rva in calc_callers) or "none") + ".",
        f"- affected-target sweep `0x{OUTER_SWEEP_ENTRY_RVA:X}` direct callers: "
        + (", ".join(f"`0x{rva:X}`" for rva in outer_callers) or "none") + ".",
        f"- Expected two aligned real-code callers, one `.trace` forecast caller, and a VM-owned sweep entry: **{'PASS' if callers_ok else 'FAIL'}**.",
        "",
        "## Formula-dispatch ownership",
        "",
        f"- Dispatch table: RVA `0x{FORMULA_DISPATCH_TABLE_RVA:X}`.",
        f"- Formula `0x{NESTED_FORMULA_ID:02X}` target: RVA `0x{formula_target_rva:X}`.",
        f"- Expected nested handler `0x{NESTED_FORMULA_ENTRY_RVA:X}`: **{'PASS' if formula_ok else 'FAIL'}**.",
        "- The stock action catalog assigns formula `0x25` to Rend Helm, Rend Armor, Rend Shield,",
        "  and Rend Weapon. The handler saves the outer order type/id, writes Attack `(type=1,id=0)`,",
        "  re-enters the universal calc for the same target, and restores the outer order record.",
        "",
        "## Nested re-entry window",
        "",
        *dispatch.disassembly(md, pe, raw, 0x307EA7, 0x4B),
        "",
        "## Forecast publication ordering",
        "",
        *dispatch.disassembly(md, pe, raw, 0x229E9F, 0x8D),
        "",
        "## Static interpretation",
        "",
        "- **Proven:** calc entry has three direct callers across executable sections. `0x281F0D` is",
        "  the ordinary per-target sweep call, `0x307ED0` is a formula-internal re-entry owned by",
        "  formula `0x25`, and `.trace` RVA `0xEF53F0F` is the player-forecast calculation path.",
        "- **Strong:** the nested call presents `(type=1, abilityId=0)` even though the outer action",
        "  is one of the Rend abilities. A latest-per-target cache that records every calc entry can",
        "  therefore replace the outer Rend identity with the synthetic inner Attack identity.",
        "- **Strong:** the UI forecast global is not a safe synchronous calc-entry classifier. Its UI",
        "  builder calls the forecast computation first and publishes `target+0x1BE` afterward; the",
        "  same global is also explicitly cleared on an invalid/no-forecast branch.",
        "- **Strong:** state `0x15` staged apply, pre-clamp `0x30A5D7`, and result selector `0x205210`",
        "  remain execution-only downstream signals. They can commit/consume state, but they cannot",
        "  tell the earlier calc hook whether a fire came from preview, charge polling, or AI scoring.",
        "- **Proven:** LT28 captures the forecast caller returning at `0xEF53F14` in battle state",
        "  `0x19`, while confirmed execution returns from the ordinary sweep at `0x281F12` in state",
        "  `0x2A`. Charge polling, AI scoring, and nested Rend remain separate coverage rows.",
        "- **Implementation boundary:** do not mutate the DCL cache yet. The safe candidate is to",
        "  preserve the outer formula-`0x25` identity across the nested Attack re-entry, but a Rend",
        "  execution must first prove whether native fallback damage expects one outer decision or a",
        "  distinct inner decision.",
        "",
    ]
    return "\n".join(lines), anchors_ok and callers_ok and formula_ok


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-calc-provenance-analysis.md"
    report, ok = render_report(args.exe, output)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(report, encoding="utf-8", newline="\n")
    print(f"wrote {output}")
    print("all calc-provenance checks PASS" if ok else "one or more calc-provenance checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
