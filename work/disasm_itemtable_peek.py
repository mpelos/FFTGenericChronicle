#!/usr/bin/env python3
"""Peek file-time bytes at the static tables found in fn 0x2B8CB8 / 0x286D04."""
from pathlib import Path
import pefile, sys

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
pe = pefile.PE(str(EXE), fast_load=True)
data = EXE.read_bytes()
def off(rva): return pe.get_offset_from_rva(rva)

def peek(rva, n, label, stride=None):
    b = data[off(rva): off(rva)+n]
    nz = sum(1 for x in b if x)
    print(f"--- {label} rva=0x{rva:X} ({n} bytes, {nz} nonzero) ---")
    if stride:
        for i in range(0, min(n, stride*16), stride):
            print(f"  row{i//stride:>3}: {' '.join(f'{x:02X}' for x in b[i:i+stride])}")
    else:
        for i in range(0, min(n, 256), 16):
            print(f"  +{i:03X}: {' '.join(f'{x:02X}' for x in b[i:i+16])}")
    print()

peek(0x80EA90, 0x100*0xC, "ItemData LOW (id<0x100), stride 0xC", stride=0xC)
peek(0x67F910 + 0x100*0xC, 0x60*0xC, "ItemData HIGH (id>=0x100) first rows at id=0x100", stride=0xC)
peek(0x80FEA0, 0x55*0x1A, "weapon-TYPE table stride 0x1A", stride=0x1A)
