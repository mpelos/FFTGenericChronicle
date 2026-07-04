#!/usr/bin/env python3
"""Exact call-site anchors for the 4 reaction Brave-gate rolls (roll(100, Brave) -> 0x278EE0).

For each site 0x30BE86/0x30BEDC/0x30BF32/0x30BF72 dump a window to read off:
  - the exact `call 0x278EE0` instruction RVA + bytes (hook anchor, ExecuteFirst + `mov edx,N`)
  - the edx load (movzx edx, [reg+0x2B]) and which reg holds the defender
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

SITES = [0x30BE86, 0x30BEDC, 0x30BF32, 0x30BF72]


def off(rva):
    return pe.get_offset_from_rva(rva)


def hb(b):
    return " ".join(f"{x:02X}" for x in b)


for site in SITES:
    start = site - 0x20
    print(f"\n=== window around 0x{site:X} ===")
    b = data[off(start): off(start) + 0x40]
    for ins in md.disasm(b, base + start):
        r = ins.address - base
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        mark = ""
        if "0x2b]" in t.lower():
            mark = "  <-- Brave load (edx)"
        elif ins.mnemonic == "call" and "0x140278ee0" in t.lower():
            mark = "  <-- RNG call (hook anchor)"
        print(f"  {r:08X}: {hb(ins.bytes):<20} {t}{mark}")
