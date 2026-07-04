#!/usr/bin/env python3
"""Exact post-copy hook anchors for the 3 evade-byte copiers (Writer A/B/twin).

We must hook AFTER the equipment->struct evade copy completes, so ExecuteFirst at the
instruction FOLLOWING the last evade store. Dump each tail so we can read off the exact
hook RVA + expected bytes, and confirm which register holds the unit ptr at that point.
"""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())

pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64)


def off(rva):
    return pe.get_offset_from_rva(rva)


def hb(b):
    return " ".join(f"{x:02X}" for x in b)


def dump(name, start, end_after, unit_reg):
    print(f"\n=== {name}  (unit ptr = {unit_reg})  window 0x{start:X}..0x{start+end_after:X} ===")
    b = data[off(start): off(start) + end_after]
    for ins in md.disasm(b, base + start):
        r = ins.address - base
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        mark = ""
        low = t.lower()
        # flag the evade store range 0x44..0x4f and the first non-store tail instr
        if any(f"0x{d:x}]" in low for d in range(0x44, 0x50)) and ins.mnemonic == "mov":
            mark = "  <-- evade store"
        print(f"  {r:08X}: {hb(ins.bytes):<24} {t}{mark}")


# Writer A: last evade store at 0x59F927 (mov [rbx+0x4F],al). Dump through the tail.
dump("Writer A  fn 0x59F550", 0x59F900, 0x50, "RBX")
# Writer B: last evade store at 0x285550 (mov [rbx+0x47],al).
dump("Writer B  fn 0x285394", 0x285532, 0x40, "RBX")
# Writer B twin: around 0x39672D..0x39674B.
dump("Writer B twin fn 0x3965B0", 0x396720, 0x50, "RSI")
