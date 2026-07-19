#!/usr/bin/env python3
"""Fail-closed static map of the current-build charged-action cancellation boundary."""
from __future__ import annotations

import argparse
import hashlib
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_AC_READ, CS_AC_WRITE, CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM

from analyze_dcl_reaction_dispatch import (
    DEFAULT_EXE,
    disassembly,
    hex_bytes,
    rva_bytes,
    scan_aligned_instructions,
)


ROOT = Path(__file__).resolve().parents[1]


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor(
        "pending predicate",
        0x271C54,
        "F6 41 61 08 74 12 80 B9 8D 01 00 00 FF 74 09 80 B9 A1 01 00 00 08 74 20",
        "Charging bit 0x08 plus a timer other than 0xFF identifies a live pending record; action type 8 has a dedicated continuation.",
    ),
    Anchor(
        "charge timer authored at action start",
        0x2818BC,
        "45 85 F6 74 36 88 8E 8D 01 00 00 44 84 F3 74 2B",
        "A confirmed delayed action stores its calculated timer at unit+0x18D.",
    ),
    Anchor(
        "incapacitation cancellation sentinel",
        0x30A966,
        "B8 00 40 00 00 66 41 85 40 12 74 2B 33 D2 48 8B CF E8 D4 72 F6 FF 48 8B 3D E5 05 56 01 85 C0 74 16 48 8B 05 E2 05 56 01 80 48 22 08 B8 FF 00 00 00 88 87 8D 01 00 00",
        "A native result carrying bit 0x4000 checks for a pending action, marks cancellation metadata, and writes timer 0xFF before status/result application continues.",
    ),
    Anchor(
        "normal charged-action resolution cleanup",
        0x30D38B,
        "44 88 BF 8D 01 00 00 8A 8F EF 01 00 00 80 E1 F2 88 8F EF 01 00 00 0A 4F 57 88 4F 61",
        "After native charged-action computation, the engine writes timer 0xFF, clears the mutually-exclusive 0x01/0x04/0x08 state bits from durable state, then rebuilds the effective byte with source state.",
    ),
    Anchor(
        "scheduler readiness timer gate",
        0x30F2CB,
        "48 8B CB E8 C5 08 00 00 A8 0F 75 0D 40 38 B3 8D 01 00 00 0F 84 45 05 00 00",
        "The scheduler admits a ready unit only when its status gate is clear and unit+0x18D equals zero.",
    ),
    Anchor(
        "scheduler countdown excludes 0xFF",
        0x30F315,
        "41 8A 0A 8A C1 41 2A C3 3C FD 77 06 41 2A CB 41 88 0A",
        "The countdown decrements values 1..254 but leaves the 0xFF cancellation sentinel unchanged.",
    ),
    Anchor(
        "scheduler timer reconstruction",
        0x30F829,
        "44 84 5B 61 74 1B 0F BF 8B A2 01 00 00 E8 21 B8 FA FF 44 8A 70 0C 41 80 E6 7F 44 88 B3 BD 01 00 00 44 88 B3 8D 01 00 00",
        "Charging bit 0x08 reloads duration from action metadata; without it the preloaded 0xFF value is retained and stored as the timer.",
    ),
    Anchor(
        "performance event cleanup",
        0x38A668,
        "81 FA 00 02 00 00 75 2E 48 8D 8D A0 01 00 00 48 03 CB E8 A5 04 00 00 85 C0 0F 84 40 01 00 00 80 64 2B 61 F6 80 A4 2B EF 01 00 00 F6 44 88 A4 2B 8D 01 00 00",
        "Scheduler event class 0x200 validates the retained order, clears 0x01/0x08 in both status mirrors, and stores the scheduler's 0xFF sentinel.",
    ),
)


TIMER_WRITER_MEANINGS = {
    0x24166D: "battle/order initialization from native timing data or 0xFF",
    0x2818C1: "confirmed delayed-action timer authoring",
    0x28192C: "alternate delayed-action timer authoring",
    0x283358: "temporary timer restore around result calculation",
    0x30A997: "incapacitation cancellation sentinel 0xFF",
    0x30D38B: "normal charged-action resolution sentinel 0xFF",
    0x30F84A: "scheduler timer reconstruction",
    0x38A694: "performance cleanup sentinel 0xFF",
}


WINDOWS = (
    ("Pending predicate", 0x271C50, 0x58),
    ("Incapacitation cancellation", 0x30A966, 0x3A),
    ("Normal resolution cleanup", 0x30D363, 0x54),
    ("Scheduler readiness/countdown", 0x30F2CB, 0x65),
    ("Scheduler timer reconstruction", 0x30F829, 0x42),
    ("Performance cleanup", 0x38A659, 0x4B),
)


def parse_hex(value: str) -> bytes:
    return bytes.fromhex(value)


def access_text(value: int) -> str:
    result = ""
    if value & CS_AC_READ:
        result += "R"
    if value & CS_AC_WRITE:
        result += "W"
    return result or "?"


def scan_offset_uses(pe: pefile.PE, raw: bytes, md: Cs, offsets: set[int]):
    image_base = pe.OPTIONAL_HEADER.ImageBase
    rows: list[tuple[int, int, str, str]] = []
    for instruction in scan_aligned_instructions(pe, raw, md):
        for operand in instruction.operands:
            if operand.type != X86_OP_MEM or operand.mem.disp not in offsets:
                continue
            rows.append(
                (
                    operand.mem.disp,
                    instruction.address - image_base,
                    access_text(operand.access),
                    f"{instruction.mnemonic} {instruction.op_str}".strip(),
                )
            )
    return rows


def validate_anchors(pe: pefile.PE, raw: bytes) -> list[str]:
    failures: list[str] = []
    for anchor in ANCHORS:
        expected = parse_hex(anchor.expected)
        actual = rva_bytes(pe, raw, anchor.rva, len(expected))
        if actual != expected:
            failures.append(
                f"{anchor.name} at RVA 0x{anchor.rva:X}: expected {anchor.expected}; got {hex_bytes(actual)}"
            )
    return failures


def render_report(exe: Path, raw: bytes, pe: pefile.PE, md: Cs, rows) -> str:
    lines = [
        "# DCL Pending-Action Cancellation Analysis",
        "",
        "Generated by `tools/analyze_dcl_pending_cancellation.py`.",
        "",
        "## Inputs",
        "",
        f"- Executable: `{exe}`",
        f"- SHA-256: `{hashlib.sha256(raw).hexdigest().upper()}`",
        "",
        "## Fail-closed anchors",
        "",
        "| Boundary | RVA | Current-build meaning |",
        "| --- | ---: | --- |",
    ]
    for anchor in ANCHORS:
        lines.append(f"| {anchor.name} | `0x{anchor.rva:X}` | {anchor.meaning} |")

    lines.extend(
        [
            "",
            "## Timer and state contract",
            "",
            "The current build uses `unit+0x18D = 0xFF` as the non-pending/cancelled sentinel. The",
            "scheduler neither decrements that value nor admits it as ready. A live charged action also",
            "requires bit `0x08` in the effective charging byte, while the action id at `unit+0x1A2` is",
            "retained after resolution and is therefore identity/history rather than liveness.",
            "",
            "Native incapacitation writes the timer sentinel but relies on the accompanying status/result",
            "transaction for the rest of its state transition. Normal resolution and performance cleanup",
            "also clear state bits, but their masks remove neighboring mutually-exclusive state families.",
            "A DCL-only Interrupt must therefore target bit `0x08` specifically in both mirrors and must",
            "not copy the broader `0xF2` or `0xF6` masks blindly.",
            "",
            "## Aligned real-code uses of pending fields",
            "",
            "| Offset | RVA | Access | Instruction | Interpretation |",
            "| ---: | ---: | --- | --- | --- |",
        ]
    )
    for offset, rva, access, rendered in sorted(rows):
        interpretation = ""
        if offset == 0x18D and "W" in access:
            interpretation = TIMER_WRITER_MEANINGS.get(rva, "other-structure or unclassified writer")
        lines.append(
            f"| `+0x{offset:X}` | `0x{rva:X}` | {access} | `{rendered}` | {interpretation} |"
        )

    lines.extend(["", "## Guarded disassembly windows", ""])
    for name, rva, size in WINDOWS:
        lines.extend([f"### {name}", ""])
        lines.extend(disassembly(md, pe, raw, rva, size))
        lines.append("")

    lines.extend(
        [
            "## Static conclusion",
            "",
            "**Strong:** a charged action can be made non-runnable by atomically setting `+0x18D` to",
            "`0xFF` and clearing only charging bit `0x08` from both `+0x61` and `+0x1EF`, while retaining",
            "`+0x1A1/+0x1A2` as historical identity. The scheduler's own reconstruction then preserves",
            "the `0xFF` sentinel because the charging bit is absent.",
            "",
            "Static evidence does not prove presentation cleanup, the exact safe managed write frame, or",
            "that no protected/VM-side queue mirrors the same action. Those remain a minimal live gate:",
            "observe a real pending action, perform the three-field transaction once, prove no resolution",
            "and no stuck Charging state, then verify a later ordinary action by the same unit.",
            "",
        ]
    )
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    raw = args.exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    failures = validate_anchors(pe, raw)
    if failures:
        for failure in failures:
            print(f"ERROR: {failure}")
        return 1

    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True
    rows = scan_offset_uses(pe, raw, md, {0x61, 0x18D, 0x1A1, 0x1A2, 0x1EF})
    if not rows:
        print("ERROR: no aligned pending-field uses found")
        return 1

    if args.check_only:
        print(
            "PASS: pending predicate, cancellation sentinel, resolution cleanup, scheduler countdown, "
            "timer reconstruction, and performance cleanup match the current executable"
        )
        return 0

    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-pending-cancellation-analysis.md"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(render_report(args.exe, raw, pe, md, rows), encoding="utf-8")
    print(f"wrote {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
