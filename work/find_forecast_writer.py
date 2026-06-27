#!/usr/bin/env python3
"""Find where the damage forecast +0x1C4 (debit) / +0x1C6 (credit) is written.

If the forecast is staged independently of the hit/miss verdict (e.g. during
target/skill setup), then forcing a hit on a would-be miss still has valid
damage to apply -> neutralization (force eax=1 at the roll) works end-to-end.
"""
from __future__ import annotations

from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_OP_REG

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]


def hb(data: bytes) -> str:
    return " ".join(f"{b:02X}" for b in data)


def main() -> int:
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    targets = (0x1C4, 0x1C6, 0x1BE, 0x1C0)
    print("Scanning .text for memory accesses with disp in {0x1C4,0x1C6,0x1BE,0x1C0}...\n")
    for sec in pe.sections:
        if not sec.Characteristics & 0x20000000:
            continue
        sblob = data[sec.PointerToRawData: sec.PointerToRawData + sec.SizeOfRawData]
        secrva = sec.VirtualAddress
        for insn in md.disasm(sblob, base + secrva):
            wrote = False
            disp = None
            for op in insn.operands:
                if op.type == X86_OP_MEM and op.mem.disp in targets:
                    disp = op.mem.disp
            if disp is None:
                continue
            rva = insn.address - base
            # classify: is the memory operand the destination (a write)?
            mn = insn.mnemonic
            is_store = mn.startswith("mov") and insn.op_str.strip().startswith(("byte", "word", "dword", "qword", "[")) or mn in ("add", "sub", "or", "and") and "ptr [" in insn.op_str.split(",")[0]
            tag = "STORE" if is_store else "read "
            print(f"{rva:08X} +0x{disp:03X} {tag}: {hb(insn.bytes):<24} {mn} {insn.op_str}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
