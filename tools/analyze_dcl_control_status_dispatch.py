#!/usr/bin/env python3
"""Map the real-code Chicken/Berserk forced-control dispatcher used by Fear/Taunt."""
from __future__ import annotations

import argparse
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs


DEFAULT_EXE = Path(
    r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"
)

ANCHORS = {
    0x38BBFC: bytes.fromhex("48 89 5C 24 08 48 89 6C 24 10 57 48 83 EC 20"),
    0x38BC37: bytes.fromhex("F6 47 63 04"),
    0x38BC3D: bytes.fromhex("E8 DA 24 00 00"),
    0x38BC42: bytes.fromhex("83 F8 FF 75 0F"),
    0x38BC6A: bytes.fromhex("BA 01 00 00 00 B9 FF 00 00 00 E8 17 57 F9 FF"),
    0x38BC84: bytes.fromhex("8A 47 62 A8 04"),
    0x38BCB2: bytes.fromhex("A8 10 0F 84 44 01 00 00"),
    0x38BDFE: bytes.fromhex("F6 47 63 08"),
    0x38BE08: bytes.fromhex("B9 02 00 00 00 E8 D2 1A 00 00"),
    0x38BE56: bytes.fromhex("48 8B CA E8 A2 46 F9 FF"),
    0x38BF1B: bytes.fromhex("33 C0 48 8B 5C 24 30"),
    0x38D8E4: bytes.fromhex("E9 4E AB 98 10"),
    0x321390: bytes.fromhex("E9 AB 82 6A 10"),
    0x32091C: bytes.fromhex("E9 16 AD 66 10"),
    0x1098B8AB: bytes.fromhex(
        "48 8D 35 76 77 EE F0 83 CB FF E8 42 03 A0 EF 39 D8 75 0E "
        "44 88 25 48 6A EE F0 89 D8 E9 90 00 00 00 85 C0 0F 85 86 00 00 00"
    ),
    0x10D6F81E: bytes.fromhex(
        "8A 05 AC 2C B0 F0 48 8B 0D 75 36 B0 F0 88 41 4F "
        "8A 05 9E 2C B0 F0 48 8B 0D 65 36 B0 F0 88 41 50 "
        "48 8B 15 5B 36 B0 F0 8A 0D 86 2C B0 F0 C0 E1 07 "
        "8A 42 51 24 7F 08 C1 88 4A 51 31 C0"
    ),
    0x109C9753: bytes.fromhex(
        "41 89 CB 45 89 CD 48 8D 0D 40 7F EA F0 "
        "88 94 B1 C4 0C 00 00 44 88 94 B1 C6 0C 00 00 "
        "40 88 AC B1 C5 0C 00 00 44 88 84 B1 C7 0C 00 00"
    ),
}


def read_rva(exe: Path, pe: pefile.PE, rva: int, size: int) -> bytes:
    with exe.open("rb") as handle:
        handle.seek(pe.get_offset_from_rva(rva))
        return handle.read(size)


def call_target(rva: int, encoded: bytes) -> int:
    if len(encoded) != 5 or encoded[0] != 0xE8:
        raise ValueError(f"RVA 0x{rva:X} is not a direct near call")
    return rva + 5 + int.from_bytes(encoded[1:], "little", signed=True)


def verify(exe: Path) -> tuple[pefile.PE, list[str]]:
    pe = pefile.PE(str(exe), fast_load=True)
    errors: list[str] = []
    for rva, expected in ANCHORS.items():
        actual = read_rva(exe, pe, rva, len(expected))
        if actual != expected:
            errors.append(
                f"RVA 0x{rva:X}: expected {expected.hex(' ').upper()}, "
                f"found {actual.hex(' ').upper()}"
            )

    calls = {
        0x38BC3D: 0x38E11C,
        0x38BC74: 0x321390,
        0x38BE0D: 0x38D8E4,
        0x38BE59: 0x320500,
        0x1098B8B5: 0x38BBFC,
    }
    for rva, expected_target in calls.items():
        encoded = read_rva(exe, pe, rva, 5)
        try:
            actual_target = call_target(rva, encoded)
        except ValueError as error:
            errors.append(str(error))
            continue
        if actual_target != expected_target:
            errors.append(
                f"call RVA 0x{rva:X}: expected target 0x{expected_target:X}, "
                f"found 0x{actual_target:X}"
            )
    return pe, errors


def disassemble(exe: Path, pe: pefile.PE, rva: int, size: int) -> list[str]:
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    base = pe.OPTIONAL_HEADER.ImageBase
    result: list[str] = []
    for instruction in md.disasm(read_rva(exe, pe, rva, size), base + rva):
        here = instruction.address - base
        encoded = " ".join(f"{byte:02X}" for byte in instruction.bytes)
        rendered = f"{instruction.mnemonic} {instruction.op_str}".strip()
        result.append(f"{here:08X}: {encoded:<28} {rendered}")
    return result


def report(exe: Path, pe: pefile.PE) -> str:
    lines = [
        "# DCL forced-control dispatcher analysis",
        "",
        f"Executable: `{exe}`",
        "",
        "## Proven static control split",
        "",
        "The real-code resolver at `0x38BBFC` reads the current unit pointer and gives native",
        "loss-of-control statuses explicit branches before ordinary control returns:",
        "",
        "- effective Chicken (`unit+0x63 & 0x04`) enters the branch at `0x38BC37`;",
        "- effective Confusion (`unit+0x62 & 0x04`) is tested at `0x38BC87`;",
        "- effective Charm (`unit+0x62 & 0x10`) is tested at `0x38BCB2`;",
        "- effective Berserk (`unit+0x63 & 0x08`) enters the branch at `0x38BDFE`;",
        "- no Berserk falls through to the zero return at `0x38BF1B`.",
        "",
        "The Chicken branch synchronously calls `0x38E11C`, treats `-1` as failure, clears a",
        "`0x240`-byte planning block, and calls planner thunk `0x321390` with `ecx=0xFF`, `edx=1`.",
        "The Berserk branch requests mode `2` through VM thunk `0x38D8E4`, selects/commits its",
        "forced target through common resolver `0x320500`, and continues through the same planning",
        "tail. These branches expose reusable native control surfaces; they do not prove that native",
        "Chicken preserves a later ally/self action, so Fear's support-action allowance and its",
        "enemy-target rejection remain separate gates.",
        "",
        "The outer planning function enters through thunk `0x32091C` and calls this resolver at",
        "trace RVA `0x1098B8B5`. It treats `-1` as failure, but any nonzero handled result jumps",
        "directly to the function's zero-return epilogue; only resolver result zero continues into",
        "the ordinary planning path. Chicken's selector thunk `0x38E11C` reaches trace function",
        "`0x10D6F6CD`, whose successful tail writes the selected x/y/layer tuple to active unit",
        "offsets `+0x4F/+0x50/+0x51`. Planner thunk `0x321390` then writes the winning route tuple",
        "into its four-byte selection record. Native Chicken therefore owns a complete forced-plan",
        "transaction and suppresses ordinary voluntary planning; it cannot be used unchanged as a",
        "Fear carrier that must allow a later self/ally/item/defensive action.",
        "",
        "The shippable Taunt fallback needs no new control branch: a one-target-turn DCL-owned",
        "Berserk status rule reaches the existing `0x38BDFE` forced-aggression branch. Its resistance",
        "formula is inverted on Brave and its duration remains owned by the generic target-turn",
        "status-duration tracker.",
        "",
        "## Disassembly",
        "",
        "```text",
        *disassemble(exe, pe, 0x38BBFC, 0xC5),
        "```",
        "",
        "```text",
        *disassemble(exe, pe, 0x38BDFE, 0x12A),
        "```",
        "",
        "```text",
        *disassemble(exe, pe, 0x1098B8AB, 0xB1),
        "```",
        "",
        "```text",
        *disassemble(exe, pe, 0x10D6F81E, 0x55),
        "```",
        "",
    ]
    return "\n".join(lines)


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
        print("DCL forced-control dispatcher analysis FAIL")
        return 1
    if args.output is not None and not args.check_only:
        args.output.write_text(report(args.exe, pe), encoding="utf-8", newline="\n")
        print(f"wrote {args.output.resolve()}")
    print("DCL forced-control dispatcher analysis PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
