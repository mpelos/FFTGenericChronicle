#!/usr/bin/env python3
"""Verify the central battle-unit position commit and enumerate every direct caller."""
from __future__ import annotations

import argparse
import hashlib
import time
from dataclasses import dataclass
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

import analyze_dcl_calc_provenance as provenance
import analyze_dcl_reaction_dispatch as dispatch


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_EXE = dispatch.DEFAULT_EXE

POSITION_THUNK_RVA = 0x27192C
POSITION_BODY_RVA = 0xE7D721B
POSITION_BODY_END_RVA = 0xE7D73F4
POSITION_WRITE_RVA = 0xE7D735A
UNIT_TABLE_RVA = 0x1853CE0
UNIT_STRIDE = 0x200
UNIT_X_OFFSET = 0x4F
UNIT_Y_OFFSET = 0x50
UNIT_LAYER_OFFSET = 0x51


@dataclass(frozen=True)
class Anchor:
    name: str
    rva: int
    expected: str
    meaning: str


ANCHORS = (
    Anchor(
        "position-thunk",
        POSITION_THUNK_RVA,
        "E9 EA 58 56 0E",
        "native thunk tail-jumps to the complete position writer",
    ),
    Anchor(
        "position-body-entry",
        POSITION_BODY_RVA,
        "48 89 5C 24 08 44 88 44 24 18 88 54 24 10 55 56",
        "callee saves the index/X/Y arguments before its prologue",
    ),
    Anchor(
        "destination-validation-call",
        0xE7D7272,
        "E8 05 B8 AA F1",
        "position writer asks the map/unit resolver about the destination",
    ),
    Anchor(
        "unit-x-write",
        0xE7D735A,
        "46 88 94 1B 2F 3D 85 01",
        "writes destination X to unit-table field +0x4F",
    ),
    Anchor(
        "unit-y-write",
        0xE7D736C,
        "46 88 8C 1B 30 3D 85 01",
        "writes destination Y to unit-table field +0x50",
    ),
    Anchor(
        "unit-layer-write",
        0xE7D7377,
        "42 88 84 1B 31 3D 85 01",
        "writes the combined layer/facing byte to unit-table field +0x51",
    ),
    Anchor(
        "linked-unit-x-write",
        0xE7D73B0,
        "46 88 94 18 2F 3D 85 01",
        "mirrors X when the position record names a linked unit",
    ),
    Anchor(
        "position-return",
        0xE7D73DF,
        "48 8B 5C 24 60 48 83 C4 20 41 5F 41 5E 41 5D 41 5C 5F 5E 5D C3",
        "balanced epilogue returns directly to the thunk caller",
    ),
)


EXPECTED_CALLERS = (
    0x1F1323,
    0x1F1405,
    0x1F14EF,
    0x1F15DC,
    0x1F1686,
    0x1F24AB,
    0x1F3022,
    0x204E1F,
    0x204E77,
    0x20B6BA,
    0x20BC4F,
    0x20BCE2,
    0x27303B,
    0x303A7A,
    0x303AD0,
    0xD43CF29,
    0xD43CF82,
    0xD737999,
    0xD7763BD,
    0xD77641F,
    0xD7E245B,
    0xD8C7D18,
    0xD8C7DD3,
    0xE46AD63,
)


# These labels deliberately stop at what static code establishes. In particular, none of the
# callers is called "walk" until a live return-address trace separates committed movement from
# setup, preview, state restoration, scripted relocation, and presentation synchronization.
CALLER_CLASS = {
    0x1F1323: "selection/state coordinate restore",
    0x1F1405: "selection/state coordinate restore",
    0x1F14EF: "selection/state coordinate restore",
    0x1F15DC: "selection/state coordinate restore",
    0x1F1686: "selection/state coordinate restore",
    0x303A7A: "two-unit position copy/swap",
    0x303AD0: "two-unit position copy/swap",
    0xE46AD63: "presentation/result coordinate synchronization",
}


def _hex(raw: bytes) -> str:
    return " ".join(f"{byte:02X}" for byte in raw)


def _owner_map(pe: pefile.PE, callers: tuple[int, ...]) -> dict[int, tuple[int, int] | None]:
    pe.parse_data_directories(
        directories=[pefile.DIRECTORY_ENTRY["IMAGE_DIRECTORY_ENTRY_EXCEPTION"]]
    )
    functions = sorted(
        (entry.struct.BeginAddress, entry.struct.EndAddress)
        for entry in pe.DIRECTORY_ENTRY_EXCEPTION
    )
    result: dict[int, tuple[int, int] | None] = {}
    for caller in callers:
        result[caller] = next(
            ((begin, end) for begin, end in functions if begin <= caller < end),
            None,
        )
    return result


def _jump_target(pe: pefile.PE, raw: bytes, rva: int) -> int | None:
    encoded = dispatch.rva_bytes(pe, raw, rva, 5)
    if len(encoded) != 5 or encoded[0] != 0xE9:
        return None
    return rva + 5 + int.from_bytes(encoded[1:], "little", signed=True)


def render_report(exe: Path, output: Path) -> tuple[str, bool]:
    raw = exe.read_bytes()
    pe = pefile.PE(data=raw, fast_load=True)
    md = Cs(CS_ARCH_X86, CS_MODE_64)

    anchor_rows: list[str] = []
    anchors_ok = True
    for anchor in ANCHORS:
        expected = bytes.fromhex(anchor.expected)
        actual = dispatch.rva_bytes(pe, raw, anchor.rva, len(expected))
        passed = actual == expected
        anchors_ok &= passed
        result = "PASS" if passed else f"FAIL (`{_hex(actual)}`)"
        anchor_rows.append(
            f"| `{anchor.name}` | `0x{anchor.rva:X}` | {result} | `{anchor.expected}` | {anchor.meaning} |"
        )

    jump_target = _jump_target(pe, raw, POSITION_THUNK_RVA)
    jump_ok = jump_target == POSITION_BODY_RVA
    callers = tuple(provenance.all_direct_callers(pe, raw, POSITION_THUNK_RVA))
    callers_ok = callers == EXPECTED_CALLERS
    owners = _owner_map(pe, callers)

    caller_rows: list[str] = []
    for caller in callers:
        owner = owners[caller]
        owner_text = "no unwind owner" if owner is None else f"`0x{owner[0]:X}..0x{owner[1]:X}`"
        classification = CALLER_CLASS.get(caller, "unclassified statically")
        caller_rows.append(f"| `0x{caller:X}` | {owner_text} | {classification} |")

    overall = anchors_ok and jump_ok and callers_ok
    lines = [
        "# DCL battle-position commit analysis",
        "",
        "Generated by `tools/analyze_dcl_position_commit.py`.",
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
        *anchor_rows,
        "",
        "## Thunk and calling convention",
        "",
        f"The native thunk at `0x{POSITION_THUNK_RVA:X}` resolves to "
        f"`0x{jump_target:X}` ({'PASS' if jump_ok else 'FAIL'}). Its Windows x64 arguments are:",
        "",
        "| Argument | Register/stack at entry | Meaning |",
        "| --- | --- | --- |",
        "| 1 | `ecx` | battle-unit table index |",
        "| 2 | `dl` | destination X |",
        "| 3 | `r8b` | destination Y |",
        "| 4 | `r9b` | high layer bit |",
        "| 5 | `[rsp+0x28]` | low nibble preserved in unit `+0x51` |",
        "",
        f"The body multiplies the index by `0x{UNIT_STRIDE:X}`, combines it with image-base-relative "
        f"unit table `0x{UNIT_TABLE_RVA:X}`, and writes `+0x{UNIT_X_OFFSET:X}`/"
        f"`+0x{UNIT_Y_OFFSET:X}`/`+0x{UNIT_LAYER_OFFSET:X}`. At the first write boundary "
        f"`0x{POSITION_WRITE_RVA:X}`, `rdi` still holds the unit index, `r10b` holds X, `r9b` "
        "holds Y, `r14b` holds the shifted high layer bit, and `r8b` holds the low nibble.",
        "",
        "Because the thunk tail-jumps instead of calling the body, the original caller return address "
        "remains on the body stack. At the first write, the seven pushes plus `sub rsp,0x20` place "
        "that return address at `[rsp+0x58]`. This gives a read-only probe both exact unit/destination "
        "identity and the direct caller that requested the commit.",
        "",
        "## Direct callers",
        "",
        f"Expected exact caller set: {len(EXPECTED_CALLERS)}. Actual: {len(callers)} "
        f"({'PASS' if callers_ok else 'FAIL'}).",
        "",
        "| Call RVA | Exception-directory owner | Static classification |",
        "| ---: | --- | --- |",
        *caller_rows,
        "",
        "## What static evidence settles",
        "",
        "- **Proven:** `0x27192C` is the shared synchronous battle-unit position writer for the "
        "current executable, with exact unit index and destination coordinates available at entry.",
        "- **Proven:** `0xE7D735A` is the first canonical unit-table write and is suitable for an "
        "observe-only hook; it runs before X/Y/layer have been overwritten.",
        "- **Proven:** the original direct caller can be read at stack offset `+0x58` at that hook.",
        "- **Proven:** some callers are setup/state restore, position copy/swap, or presentation "
        "synchronization, so firing this writer alone is not evidence of a real movement step.",
        "- **Unresolved offline:** which caller(s) represent accepted walking steps, whether the "
        "writer fires per traversed tile or only at the final tile, and which calls are UI forecast "
        "or rollback. A short caller-correlated live trace is required to separate them.",
        "",
        "## Position-writer body",
        "",
        *dispatch.disassembly(
            md,
            pe,
            raw,
            POSITION_BODY_RVA,
            POSITION_BODY_END_RVA - POSITION_BODY_RVA,
        ),
        "",
        f"Overall static gate: **{'PASS' if overall else 'FAIL'}**.",
        "",
    ]
    return "\n".join(lines), overall


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--output", type=Path)
    parser.add_argument(
        "--check-only",
        action="store_true",
        help="validate and print the gate without writing a work report",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    output = args.output or ROOT / "work" / f"{int(time.time())}-dcl-position-commit-analysis.md"
    report, ok = render_report(args.exe, output)
    if not args.check_only:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(report, encoding="utf-8", newline="\n")
        print(f"wrote {output}")
    print("position commit PASS" if ok else "position commit FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
