#!/usr/bin/env python3
"""Verify DCL Fear pre-confirm actor, native target builder, and managed authority contract."""
from __future__ import annotations

import argparse
import hashlib
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE
TARGET_BUILDER_RVA = 0x282754
TARGET_HELPER_RVA = 0x281C24
CALC_ENTRY_RVA = 0x3099AC
MOD_SOURCE = ROOT / "codemod" / "fftivc.generic.chronicle.codemod" / "Mod.cs"

SOURCE_CONTRACT = (
    "TryAssessDclFearExpandedTargets(",
    "TryResolveVoluntaryCasterIndex(",
    "DclFearPlayerConfirmEnforcementEnabled",
    "casterIndex == turnOwner",
    "casterSource=",
    "actorUnitIdx=",
    "listAuthoritative=",
    "privateListAuthoritative",
)
RETIRED_SOURCE_CONTRACT = (
    "DclFearForecastDecision",
    "_dclFearForecastDecision",
    "_dclFearForecastGate",
)


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("confirm-actor-resolver", 0x20C341, "E8 7A 44 05 00", "resolve the current battle actor"),
    Anchor("confirm-actor-save", 0x20C346, "48 8B D8", "retain the current actor in rbx"),
    Anchor("confirm-transition-call", 0x20C55F, "E8 94 AD FF FF", "accepted voluntary confirmation"),
    Anchor("transition-state-write", 0xD84440B, "C7 05 B7 6D 42 F3 1B 00 00 00", "advance state to 0x1B"),
    Anchor("execution-target-helper", 0x281E1A, "E8 05 FE FF FF", "validate/derive execution target state"),
    Anchor("execution-selected-target", 0x281E36, "0F B6 15 55 E9 52 00", "load selected target byte from RVA 0x7B0792"),
    Anchor("execution-target-builder", 0x281EC3, "E8 8C 08 00 00", "expand the affected-target list"),
    Anchor("forecast-entry", 0xEF53DE0, "48 89 5C 24 10 48 89 6C 24 18", "player forecast implementation"),
    Anchor("forecast-order-address", 0xEF53DF6, "41 B9 A0 01 B4 DE", "derive caster unit+0x1A0 under protected constants"),
    Anchor("forecast-target-helper", 0xEF53E92, "E8 8D DD 32 F1", "validate forecast target state"),
    Anchor("forecast-target-index", 0xEF53EFA, "8A 97 BC 01 00 00", "load primary target unit index"),
    Anchor("forecast-calc-call", 0xEF53F0F, "E8 98 5A 3B F1", "calculate the primary forecast target"),
    Anchor("selected-target-copy", 0x307179, "88 05 13 96 4A 00", "copy calc target index to RVA 0x7B0792"),
    Anchor("actor-target-count-consumer", 0x20CCCD, "8A 83 A9 01 00 00", "read actor target count during cleanup"),
    Anchor("actor-target-list-consumer", 0x20CCDA, "48 8D B3 AA 01 00 00", "address actor target list during cleanup"),
    Anchor("target-builder-thunk", 0x282754, "E9 38 47 C7 0E", "enter the protected affected-target builder"),
    Anchor("target-builder-output-base", 0xEEF6EC5, "49 89 CD", "retain the caller-owned output buffer"),
    Anchor("target-builder-actor-base", 0xEEF6EC2, "48 89 D3", "retain the caller-owned actor"),
    Anchor("target-builder-output-store", 0xEEF6FD5, "47 88 1C 28", "write one expanded unit index to the private output buffer"),
    Anchor("target-builder-output-fill", 0xEEF7002, "E8 19 34 6D F1", "fill remaining output slots with 0xFF"),
    Anchor("secondary-builder-call", 0xEF48AD6, "E8 79 9C 33 F1", "reuse target builder in protected reaction evaluation"),
    Anchor("secondary-first-target", 0xEF48AF5, "0F B6 74 24 20", "consume the expanded list's first target"),
    Anchor("secondary-reaction-order", 0xEF48BF9, "E8 82 A6 33 F1", "materialize a reaction order after target selection"),
)


def parse_hex(value: str) -> bytes:
    return bytes.fromhex(value)


def hex_bytes(value: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in value)


def all_direct_callers(pe: pefile.PE, raw: bytes, target_rva: int) -> list[int]:
    """Find direct rel32 calls across every executable section, including protected trace code."""
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


def render_report(exe: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)

    rows: list[str] = []
    anchors_ok = True
    for anchor in ANCHORS:
        expected = parse_hex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        ok = actual == expected
        anchors_ok &= ok
        rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | "
            f"{'PASS' if ok else f'FAIL (`{hex_bytes(actual)}`)'} | `{anchor.expected}` | {anchor.meaning} |"
        )

    builder_callers = all_direct_callers(pe, raw, TARGET_BUILDER_RVA)
    helper_callers = all_direct_callers(pe, raw, TARGET_HELPER_RVA)
    calc_callers = all_direct_callers(pe, raw, CALC_ENTRY_RVA)
    callers_ok = (
        builder_callers == [0x281EC3, 0xEF48AD6]
        and helper_callers == [0x281E1A, 0xEF53E92]
        and calc_callers == [0x281F0D, 0x307ED0, 0xEF53F0F]
    )
    mod_source = MOD_SOURCE.read_text(encoding="utf-8")
    source_rows = [
        (token, token in mod_source)
        for token in SOURCE_CONTRACT
    ] + [
        (f"retired: {token}", token not in mod_source)
        for token in RETIRED_SOURCE_CONTRACT
    ]
    source_ok = all(ok for _, ok in source_rows)

    lines = [
        "# DCL Fear pre-confirm target-source analysis",
        "",
        "Generated by `tools/analyze_dcl_fear_preconfirm.py`.",
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
        "## Exhaustive direct callers",
        "",
        "- target builder `0x282754`: " + ", ".join(f"`0x{x:X}`" for x in builder_callers),
        "- target helper `0x281C24`: " + ", ".join(f"`0x{x:X}`" for x in helper_callers),
        "- per-target calculation `0x3099AC`: " + ", ".join(f"`0x{x:X}`" for x in calc_callers),
        f"- Exact caller sets: **{'PASS' if callers_ok else 'FAIL'}**.",
        "",
        "## Managed authority contract",
        "",
        "| Contract | Result |",
        "| --- | --- |",
        *[f"| `{token}` | {'PASS' if ok else 'FAIL'} |" for token, ok in source_rows],
        "",
        "## Strong static interpretation",
        "",
        "- At voluntary confirmation, `rbx` still contains the actor returned by `0x2607C0`; the",
        "  guarded call at `0x20C55F` is therefore an exact observe-only surface for actor state",
        "  and the private native target-builder input. Its linked unit, action fields, primary",
        "  target, and `actor+0x1A9/+0x1AA` list are diagnostic rather than caster authority.",
        "  The live turn-owner slot owns caster identity and `unit+0x1A0` action identity.",
        "- The player forecast implementation temporarily derives caster `unit+0x1A0`, validates",
        "  target state, and calls the universal calculator only for the primary target unit. It does",
        "  not directly call the affected-target builder, so calc-entry aggregation is not an exact",
        "  AoE target set.",
        "- The target builder has one additional protected-code caller, but that caller consumes the",
        "  first expanded target and then invokes reaction-order materialization. It proves reusable",
        "  native expansion outside ordinary execution, not a player-forecast list publication.",
        "- The builder accepts a caller-owned output buffer in `rcx` and an actor in `rdx`, writes",
        "  expanded unit indices in distance order, and fills unused slots with `0xFF`. This makes a",
        "  synchronous private-buffer call with the pre-confirm actor the exact target-source.",
        "  Single-target completeness is proven separately by live evidence; AoE completeness and",
        "  the separate default-off enforcement arm remain the final player-confirm gates.",
        "",
        "## Confirmation window",
        "",
        *dispatch.disassembly(md, pe, raw, 0x20C33A, 0x22F),
        "",
        "## Native target-builder implementation",
        "",
        *dispatch.disassembly(md, pe, raw, 0xEEF6E91, 0x1B2),
        "",
        "## Forecast calculation window",
        "",
        *dispatch.disassembly(md, pe, raw, 0xEF53DE0, 0x15A),
        "",
        "## Secondary target-builder caller",
        "",
        *dispatch.disassembly(md, pe, raw, 0xEF48AA7, 0x15C),
        "",
    ]
    return "\n".join(lines), anchors_ok and callers_ok and source_ok


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    if not args.exe.exists():
        print(f"ERROR: executable not found: {args.exe}")
        return 1
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-fear-preconfirm-analysis.md"
    report, ok = render_report(args.exe, output)
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output.resolve()}")
    print("DCL Fear pre-confirm analysis PASS" if ok else "DCL Fear pre-confirm analysis FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
