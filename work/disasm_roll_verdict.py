#!/usr/bin/env python3
"""Validate the roll-verdict-override hypothesis.

The resolution spine calls the single VM avoidance roll 0x30FA34 and then
branches on eax (test eax,eax; je miss). If eax is the verdict in REAL code,
an ExecuteFirst hook that forces eax flips hit<->miss WITHOUT touching the VM
or the data (defender evade is VM-internal, so input-control is dead). This
script proves the exact bytes/branch and looks for the +0x1C4 forecast write
so we know a forced hit has damage staged.
"""
from __future__ import annotations

from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_GRP_CALL, X86_GRP_JUMP, X86_OP_IMM

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]


def hb(data: bytes) -> str:
    return " ".join(f"{b:02X}" for b in data)


def main() -> int:
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found in candidates")
    pe = pefile.PE(str(exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    def off(rva: int) -> int:
        return pe.get_offset_from_rva(rva)

    print(f"exe={exe}")
    print(f"image_base=0x{base:X}\n")

    # --- Window: the verdict region around the roll call ---
    win_start, win_end = 0x30F3C0, 0x30F4E0
    print(f"=== SPINE 0x{win_start:X}..0x{win_end:X} (roll + verdict branch) ===")
    blob = data[off(win_start): off(win_end)]
    interesting = ("0x1c4", "0x1c6", "0x1be", "0x1c0", "0x300", "0x30fa34")
    for insn in md.disasm(blob, base + win_start):
        rva = insn.address - base
        text = f"{insn.mnemonic} {insn.op_str}".strip()
        mark = ""
        low = text.lower()
        if "test eax, eax" in low or (insn.mnemonic == "call"):
            mark = " <== "
        if any(t in low for t in interesting):
            mark = mark or " *field* "
        print(f"{rva:08X}: {hb(insn.bytes):<26} {text}{mark}")

    # --- Confirm single caller of the roll 0x30FA34 ---
    print("\n=== direct callers of roll 0x30FA34 ===")
    callers = []
    for sec in pe.sections:
        if not sec.Characteristics & 0x20000000:
            continue
        sblob = data[sec.PointerToRawData: sec.PointerToRawData + sec.SizeOfRawData]
        for insn in md.disasm(sblob, base + sec.VirtualAddress):
            if not (insn.group(X86_GRP_CALL) or insn.group(X86_GRP_JUMP)):
                continue
            if not insn.operands or insn.operands[0].type != X86_OP_IMM:
                continue
            if insn.operands[0].imm - base == 0x30FA34:
                callers.append((insn.address - base, insn.mnemonic))
    for rva, mn in callers:
        print(f"  0x{rva:X} ({mn})")
    if not callers:
        print("  (none)")

    # --- What is 0x30FA34 itself (thunk into VM?) ---
    print("\n=== roll target 0x30FA34 (first 6 insns) ===")
    blob = data[off(0x30FA34): off(0x30FA34) + 0x30]
    for i, insn in enumerate(md.disasm(blob, base + 0x30FA34)):
        if i >= 6:
            break
        rva = insn.address - base
        print(f"{rva:08X}: {hb(insn.bytes):<26} {insn.mnemonic} {insn.op_str}")

    # --- Scan the whole producer for +0x1C4 / +0x1C6 forecast writes ---
    print("\n=== +0x1C4 / +0x1C6 references in producer 0x30F000..0x30F600 ===")
    blob = data[off(0x30F000): off(0x30F600)]
    for insn in md.disasm(blob, base + 0x30F000):
        low = f"{insn.mnemonic} {insn.op_str}".lower()
        if "0x1c4" in low or "0x1c6" in low:
            rva = insn.address - base
            print(f"{rva:08X}: {hb(insn.bytes):<26} {insn.mnemonic} {insn.op_str}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
