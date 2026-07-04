#!/usr/bin/env python3
"""Find callers of Writer A (0x59F550) and the equipment-refresh path, and check
whether the equipment shield-evade byte [item+0x12] is read directly near the roll
(i.e. shield leg recomputed from the item, bypassing +0x4A)."""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
pe=pefile.PE(str(EXE),fast_load=True); base=pe.OPTIONAL_HEADER.ImageBase
data=EXE.read_bytes(); md=Cs(CS_ARCH_X86,CS_MODE_64); md.detail=True
REAL_MAX=0x610000
def off(r): return pe.get_offset_from_rva(r)
def hb(b): return ' '.join(f'{x:02X}' for x in b)
exsecs=[(s.VirtualAddress,s.PointerToRawData,s.SizeOfRawData) for s in pe.sections if s.Characteristics & 0x20000000]

def find_callers(target):
    hits=[]
    for va,praw,sz in exsecs:
        blob=data[praw:praw+sz]; n=len(blob)
        for i in range(n-5):
            rva=va+i
            if rva>=REAL_MAX: break
            if blob[i]==0xE8:
                rel=int.from_bytes(blob[i+1:i+5],'little',signed=True)
                dst=(rva+5+rel)&0xFFFFFFFFFFFFFFFF
                if dst==target:
                    hits.append(rva)
    return hits

for tgt in (0x59F550,0x285394,0x3965B0):
    cs=find_callers(tgt)
    print(f"callers of 0x{tgt:X}: {[hex(c) for c in cs]}")
print()

# Also: who READS [reg+0x12] AND [reg+0x13] AND [reg+0x16]/[reg+0x17] as an equipment
# evade block (Writer A signature) — to find all equipment-derived refresh sites.
print("Writer-A-like equipment->evade copiers (read [src+0x12] store [dst+0x4A]):")
for va,praw,sz in exsecs:
    blob=data[praw:praw+sz]; n=len(blob)
    for i in range(n-4):
        rva=va+i
        if rva>=REAL_MAX: break
        # movzx eax,[reg+0x12] ; mov [reg2+0x4A],al  -> 0F B6 47 12 ... 88 43 4A pattern loosely
        if blob[i]==0x0F and blob[i+1]==0xB6 and blob[i+3]==0x12:
            # look ahead 8 bytes for 88 .. 4A
            w=blob[i+4:i+12]
            if 0x88 in w:
                k=w.index(0x88)
                if k+2<len(w) and w[k+2]==0x4A:
                    print(f"  0x{rva:X}: equip+0x12 -> +0x4A store nearby")
