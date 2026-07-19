#!/usr/bin/env python3
"""Verify native boundaries needed to implement DCL Fear without suppressing reactions."""
from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

import analyze_dcl_control_status_dispatch as control


DEFAULT_EXE = control.DEFAULT_EXE


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor(
        "battle-state-19-input-call",
        0x212173,
        "E8 94 A1 FF FF E9 11 15 00 00",
        "battle state 0x19 calls the voluntary target-input handler",
    ),
    Anchor(
        "voluntary-confirm-call",
        0x20C55F,
        "E8 94 AD FF FF",
        "accepted player input calls the confirmation transition",
    ),
    Anchor(
        "confirm-transition-thunk",
        0x2072F8,
        "E9 F3 D0 63 0D",
        "confirmation thunk tail-jumps to the real transition body",
    ),
    Anchor(
        "confirm-state-write",
        0xD84440B,
        "C7 05 B7 6D 42 F3 1B 00 00 00",
        "the real confirmation body advances battle state to 0x1B",
    ),
    Anchor(
        "affected-target-builder-setup",
        0x281EBC,
        "49 8B D6 48 8D 4D D8 E8 8C 08 00 00",
        "the universal action-result path builds its affected-target list",
    ),
    Anchor(
        "affected-target-builder-call",
        0x281EC3,
        "E8 8C 08 00 00",
        "the complete five-byte builder call is the safe pre-return hook boundary",
    ),
    Anchor(
        "affected-target-list-return",
        0x281EC8,
        "44 8A 65 D0 0F 10 45 D8 88 05 B9 E8 52 00",
        "the complete target list is available before publication",
    ),
    Anchor(
        "affected-target-publication",
        0x281EEA,
        "F3 0F 7F 05 3E 91 5E 01",
        "the action-result path publishes the target list",
    ),
    Anchor(
        "affected-target-skip-sentinel",
        0x281EFA,
        "8A 54 1D D8 80 FA FF 74 0F",
        "target index 0xFF is skipped before per-target result calculation",
    ),
    Anchor(
        "affected-target-output-filter",
        0x281F78,
        "41 8A 0C 12 41 8D 40 01 80 F9 FF 88 0A 41 0F 44 C0",
        "target index 0xFF is excluded from the affected-target output count",
    ),
)


DIRECT_TARGETS = {
    0x212173: 0x20C30C,
    0x20C55F: 0x2072F8,
    0x281EC3: 0x282754,
    0x281DF7: 0x281ECC,
    0x281F0D: 0x3099AC,
}


def read_rva(exe: Path, pe: pefile.PE, rva: int, size: int) -> bytes:
    with exe.open("rb") as handle:
        handle.seek(pe.get_offset_from_rva(rva))
        return handle.read(size)


def direct_target(rva: int, encoded: bytes) -> int:
    if len(encoded) != 5 or encoded[0] not in (0xE8, 0xE9):
        raise ValueError(f"RVA 0x{rva:X} is not a direct near call/jump")
    return rva + 5 + int.from_bytes(encoded[1:], "little", signed=True)


def verify(exe: Path) -> tuple[pefile.PE, list[str]]:
    pe = pefile.PE(str(exe), fast_load=True)
    errors: list[str] = []
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = read_rva(exe, pe, anchor.rva, len(expected))
        if actual != expected:
            errors.append(
                f"{anchor.name} RVA 0x{anchor.rva:X}: expected "
                f"{expected.hex(' ').upper()}, found {actual.hex(' ').upper()}"
            )
    for rva, expected_target in DIRECT_TARGETS.items():
        encoded = read_rva(exe, pe, rva, 5)
        try:
            actual_target = direct_target(rva, encoded)
        except ValueError as error:
            errors.append(str(error))
            continue
        if actual_target != expected_target:
            errors.append(
                f"branch RVA 0x{rva:X}: expected target 0x{expected_target:X}, "
                f"found 0x{actual_target:X}"
            )
    return pe, errors


def disassemble(exe: Path, pe: pefile.PE, rva: int, size: int) -> list[str]:
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    base = pe.OPTIONAL_HEADER.ImageBase
    rows: list[str] = []
    for instruction in md.disasm(read_rva(exe, pe, rva, size), base + rva):
        here = instruction.address - base
        encoded = " ".join(f"{byte:02X}" for byte in instruction.bytes)
        rendered = f"{instruction.mnemonic} {instruction.op_str}".strip()
        rows.append(f"{here:08X}: {encoded:<28} {rendered}")
    return rows


def report(exe: Path, pe: pefile.PE) -> str:
    return "\n".join(
        [
            "# DCL Fear native-boundary analysis",
            "",
            f"Executable: `{exe}`",
            "",
            "## Strong static boundary",
            "",
            "Battle state `0x19` calls the voluntary target-input handler at `0x20C30C`.",
            "Only its accepted-confirm path reaches the call at `0x20C55F`; that call enters",
            "thunk `0x2072F8`, whose real body writes battle state `0x1B`. A guarded replacement",
            "of this single call can therefore reject a Fear-invalid player confirmation while",
            "leaving the target-selection state active. Reaction queue/delivery states `0x29` and",
            "`0x2C` do not traverse this voluntary confirmation boundary.",
            "",
            "The universal action-result path calls the affected-target builder at `0x281EC3`.",
            "The complete native target list is available at `0x281EC8`, before publication.",
            "The return itself is not a safe inline-hook site: a separate branch at `0x281DF7`",
            "targets its second instruction at `0x281ECC`. The safe bridge replaces only the",
            "five-byte builder call, invokes that native builder first, inspects the completed",
            "list, and then returns to the untouched `0x281EC8` continuation.",
            "Entries replaced with `0xFF` are skipped both by the per-target result call and by",
            "the affected-target output copy. This boundary sees unit, tile and area targeting",
            "after native target expansion rather than guessing from cursor coordinates.",
            "",
            "The implementable Fear policy is therefore: inspect the completed list; reject the",
            "whole candidate when any affected unit is opposing; leave self, ally, empty-tile and",
            "defensive candidates intact; apply invalidation during AI evaluation and fail-closed",
            "execution, but never during reaction delivery. Player forecast records the decision",
            "and the voluntary-confirm hook enforces it without touching reactions.",
            "",
            "## Voluntary confirmation disassembly",
            "",
            "```text",
            *disassemble(exe, pe, 0x20C520, 0x58),
            "```",
            "",
            "## Affected-target sweep disassembly",
            "",
            "```text",
            *disassemble(exe, pe, 0x281EBC, 0xD5),
            "```",
            "",
        ]
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    if not args.exe.exists():
        print(f"ERROR: executable not found: {args.exe}")
        return 1
    pe, errors = verify(args.exe)
    for error in errors:
        print(f"ERROR: {error}")
    if errors:
        print("DCL Fear native-boundary analysis FAIL")
        return 1
    if args.output is not None and not args.check_only:
        args.output.write_text(report(args.exe, pe), encoding="utf-8", newline="\n")
        print(f"wrote {args.output.resolve()}")
    print("DCL Fear native-boundary analysis PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
