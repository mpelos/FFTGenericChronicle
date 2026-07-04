#!/usr/bin/env python3
"""Dump enclosing-function context for the top evade-writer groups.
For a list of RVAs, walk back to find the function head (align on a preceding
int3/ret pad or a common prologue), then linear-disasm a wide window, marking:
  - writes to 0x44..0x4F  (evade/equip cluster)
  - references to unit table 0x141853CE0 / stride 0x200 (loop over units)
  - calls (to see what feeds the source, and whether called per-attack)
  - equipment-table / item lookups (large data addresses)
"""
from __future__ import annotations
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
md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)

import sys
# target site + how far back to start disasm + total length
TARGETS = [
    ("GROUP0 [rbx] 0x48-0x4F", 0x59F8F6, 0x140, 0x260),
    ("GROUP1 [rdx] 0x48-0x4B", 0x25DC51, 0x120, 0x180),
    ("GROUP2 [rbx] 0x44-0x47", 0x28553B, 0x120, 0x160),
    ("GROUP4 [rsi] 0x44-0x47", 0x39672D, 0x140, 0x180),
    ("GROUP13 [rcx] 0x44-0x46", 0x558C0F, 0xC0, 0x140),
]
if len(sys.argv) > 1:
    # allow: python x.py RVA back len
    TARGETS = [("CUSTOM", int(sys.argv[1],16), int(sys.argv[2],16), int(sys.argv[3],16))]

def dump(label, site, back, length):
    start = site - back
    b = data[off(start): off(start)+length]
    print("#"*78)
    print(f"# {label}   site=0x{site:X}  window 0x{start:X}..0x{start+length:X}")
    print("#"*78)
    for ins in md.disasm(b, base+start):
        r = ins.address - base
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        low = t.lower()
        mk = ""
        if "1853ce0" in low or "1853ce" in low: mk = "  <== UNIT TABLE BASE"
        elif "0x200" in low and ins.mnemonic in ("add","lea","imul","cmp","sub"): mk = "  <== stride 0x200?"
        elif ins.mnemonic == "call": mk = "  <== CALL"
        elif any(f"+ 0x{d:02x}]" in low for d in range(0x44,0x50)): mk = "  <-- evade cluster"
        elif ins.mnemonic in ("ret","int3"): mk = "  ----"
        print(f"    {r:08X}: {hb(ins.bytes):<26} {t}{mk}")

for label, site, back, length in TARGETS:
    dump(label, site, back, length)
    print()
