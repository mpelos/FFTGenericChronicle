#!/usr/bin/env python3
"""Verify the native execution boundary for status-only effects and staged CT deltas."""
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
ABILITY_BASELINE = ROOT / "work" / "wotl_ability_action_baseline.csv"

FORMULA_DISPATCH_TABLE_RVA = 0x682BC8
STATUS_ONLY_FORMULA_ID = 0x22
STATUS_ONLY_HANDLER_RVA = 0x307DE4
STATUS_RESULT_BUILDER_RVA = 0x306558
STATUS_RESULT_FINALIZER_RVA = 0x306988
STATE_15_APPLY_CALL_RVA = 0x211FCB
EXECUTION_APPLY_CALL_RVA = 0x20C06E
STATE_APPLY_RVA = 0x30A484
PRE_CLAMP_RVA = 0x30A5D7
STAGED_CT_TAIL_RVA = 0x30A64D
SONG_SPEED_PLUS_ONE_RVA = 0x307B16
DANCE_SPEED_MINUS_ONE_RVA = 0x307BCD
FINALE_CT_PLUS_127_RVA = 0x307AFA
LAST_WALTZ_CT_MINUS_127_RVA = 0x307BB1


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor("formula-22-wrapper", STATUS_ONLY_HANDLER_RVA,
           "48 83 EC 28 E8 6B E7 FF FF 48 83 C4 28 E9 92 EB FF FF",
           "formula 0x22 delegates to the status-result builder and finalizer"),
    Anchor("status-result-header", STATUS_RESULT_BUILDER_RVA,
           "40 53 48 83 EC 20 48 8B 05 13 4A 56 01",
           "shared status-result builder entry"),
    Anchor("state-15-apply-call", STATE_15_APPLY_CALL_RVA,
           "E8 04 A0 FF FF",
           "execution dispatcher enters the state-0x15 apply handler"),
    Anchor("execution-apply-call", EXECUTION_APPLY_CALL_RVA,
           "E8 11 E4 0F 00",
           "execution handler calls the battle-unit state-apply routine"),
    Anchor("state-apply-entry", STATE_APPLY_RVA,
           "48 89 5C 24 08 48 89 6C 24 18",
           "battle-unit state-apply entry"),
    Anchor("pre-clamp", PRE_CLAMP_RVA,
           "0F BF 45 06 0F B7 57 30",
           "managed commit boundary before native HP/MP application"),
    Anchor("staged-ct-tail", STAGED_CT_TAIL_RVA,
           "8A 4D 15 48 8D 57 41 45 33 C9 41 B0 FF",
           "load result+0x15 and pass unit+0x41 with bounds 0..255 to the native delta helper"),
    Anchor("song-speed-plus-one", SONG_SPEED_PLUS_ONE_RVA,
           "C6 40 14 81 C6 40 27 01",
           "Rousing Melody stages +1 Speed as sign-magnitude byte 0x81"),
    Anchor("dance-speed-minus-one", DANCE_SPEED_MINUS_ONE_RVA,
           "C6 40 14 01 C6 40 27 01",
           "Slow Dance stages -1 Speed as sign-magnitude byte 0x01"),
    Anchor("finale-ct-plus-127", FINALE_CT_PLUS_127_RVA,
           "C6 40 15 FF",
           "Finale stages +127 CT as sign-magnitude byte 0xFF"),
    Anchor("last-waltz-ct-minus-127", LAST_WALTZ_CT_MINUS_127_RVA,
           "C6 40 15 7F",
           "Last Waltz stages -127 CT as sign-magnitude byte 0x7F"),
)


def parse_hex(value: str) -> bytes:
    return bytes.fromhex(value)


def hex_bytes(value: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in value)


def load_formula_22_abilities(path: Path) -> list[tuple[int, str, str]]:
    rows: list[tuple[int, str, str]] = []
    with path.open(newline="", encoding="utf-8-sig") as handle:
        for row in csv.DictReader(handle):
            if row.get("formula_hex", "").upper() != "0X22":
                continue
            rows.append((int(row["id_dec"]), row["name_ivc"], row["inflict_statuses"]))
    return rows


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

    table_bytes = dispatch.rva_bytes(
        pe, raw, FORMULA_DISPATCH_TABLE_RVA + STATUS_ONLY_FORMULA_ID * 8, 8
    )
    formula_target_va = struct.unpack("<Q", table_bytes)[0]
    formula_target_rva = formula_target_va - image_base
    formula_ok = formula_target_rva == STATUS_ONLY_HANDLER_RVA

    apply_callers = dispatch.direct_callers(pe, raw, STATE_APPLY_RVA)
    caller_ok = EXECUTION_APPLY_CALL_RVA in apply_callers
    order_ok = PRE_CLAMP_RVA < STAGED_CT_TAIL_RVA
    formula_abilities = load_formula_22_abilities(ABILITY_BASELINE)
    expected_formula_abilities = [
        (81, "Kiyomori", "Protect|Shell"),
        (84, "Masamune", "Regen|Haste"),
    ]
    iaido_ok = formula_abilities == expected_formula_abilities

    lines = [
        "# DCL status-only / staged-CT transaction analysis",
        "",
        "Generated by `tools/analyze_dcl_support_transaction.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        f"- Ability baseline: `{ABILITY_BASELINE}`",
        f"- Output: `{output}`",
        "",
        "## Byte anchors",
        "",
        "| Name | RVA | Result | Expected bytes | Meaning |",
        "| --- | ---: | --- | --- | --- |",
        *rows,
        "",
        "## Formula and call-graph checks",
        "",
        f"- Formula `0x22` dispatch target: `0x{formula_target_rva:X}` "
        f"({'PASS' if formula_ok else 'FAIL'}; expected `0x{STATUS_ONLY_HANDLER_RVA:X}`).",
        "- Direct real-code callers of the state-apply entry: "
        + (", ".join(f"`0x{rva:X}`" for rva in apply_callers) or "none") + ".",
        f"- Execution apply caller `0x{EXECUTION_APPLY_CALL_RVA:X}` present: "
        f"**{'PASS' if caller_ok else 'FAIL'}**.",
        f"- Pre-clamp precedes staged CT application: **{'PASS' if order_ok else 'FAIL'}**.",
        "",
        "## Formula 0x22 catalog members",
        "",
        "| Ability id | Name | Native status payload |",
        "| ---: | --- | --- |",
        *(f"| {ability_id} | {name} | {statuses or 'none'} |"
          for ability_id, name, statuses in formula_abilities),
        "",
        f"Exact Kiyomori (`81`) / Masamune (`84`) carrier inventory: **{'PASS' if iaido_ok else 'FAIL'}**.",
        "",
        "## Relevant native windows",
        "",
        "### Formula 0x22 wrapper and status-result header",
        "",
        *dispatch.disassembly(md, pe, raw, STATUS_ONLY_HANDLER_RVA, 0x45),
        "",
        *dispatch.disassembly(md, pe, raw, STATUS_RESULT_BUILDER_RVA, 0x68),
        "",
        "### Execution-to-apply call",
        "",
        *dispatch.disassembly(md, pe, raw, EXECUTION_APPLY_CALL_RVA - 0x25, 0x38),
        "",
        "### Pre-clamp through staged CT tail",
        "",
        *dispatch.disassembly(md, pe, raw, PRE_CLAMP_RVA, STAGED_CT_TAIL_RVA - PRE_CLAMP_RVA + 0x1A),
        "",
        "## Static interpretation",
        "",
        "- **Strong:** formula `0x22` is the status-only family used by Kiyomori and Masamune. Its",
        "  visible wrapper delegates to the shared status-result builder/finalizer rather than an",
        "  HP/MP formula handler.",
        "- **Strong:** actual execution reaches the battle-unit state-apply routine through the",
        "  state-`0x15` handler. The routine reaches the existing managed pre-clamp without testing",
        "  whether staged HP/MP debit or credit is positive.",
        "- **Strong:** after pre-clamp, native code loads `result+0x15`, addresses `unit+0x41` (CT),",
        "  supplies the bounds `0..255`, and invokes the engine's ordinary bounded-delta helper.",
        "- **Strong:** staged one-byte stat/CT deltas use sign-magnitude encoding: bit `0x80` means",
        "  increase and bits `0x7F` hold the magnitude; a clear high bit means decrease. Song/Dance",
        "  provide symmetric native witnesses (`0x81` = +1, `0x01` = -1; `0xFF` = +127,",
        "  `0x7F` = -127). Therefore CT +8 is staged as `0x88`, not `0x08`.",
        "- **Implementation consequence:** a support action can stage a CT delta at `unit+0x1D3`",
        "  (`result+0x15`) inside the existing pre-clamp callback, then let the native tail apply it.",
        "  This preserves the same execution commit and avoids a late direct write to live CT.",
        "- **Remaining live gate:** confirm one all-zero HP/MP formula-`0x22` execution emits exactly",
        "  one managed pre-clamp callback and that staging byte `0x88` yields CT +8 with the expected",
        "  cap. Static control flow is sufficient for implementation behind a disabled setting, but",
        "  not for marking the complete vertical slice Proven.",
        "",
    ]
    ok = anchors_ok and formula_ok and caller_ok and order_ok and iaido_ok
    return "\n".join(lines), ok


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if not args.exe.exists():
        raise SystemExit(f"executable not found: {args.exe}")
    if not ABILITY_BASELINE.exists():
        raise SystemExit(f"ability baseline not found: {ABILITY_BASELINE}")
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-support-transaction-analysis.md"
    report, ok = render_report(args.exe, output)
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("all support-transaction checks PASS" if ok else "one or more support-transaction checks FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
