#!/usr/bin/env python3
"""Shared helpers for the miss-consumption / counter-path RE pass."""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
REAL_MAX = 0x610000
pe = pefile.PE(str(EXE), fast_load=True)
BASE = pe.OPTIONAL_HEADER.ImageBase
DATA = EXE.read_bytes()
MD = Cs(CS_ARCH_X86, CS_MODE_64); MD.detail = True

def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)

EXSECS = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
          for s in pe.sections if s.Characteristics & 0x20000000]

def find_head(rva):
    """Walk back to previous int3 pad; head = first non-CC after."""
    o = off(rva)
    for k in range(1, 0x4000):
        if DATA[o-k] == 0xCC and DATA[o-k-1] == 0xCC:
            h = o-k
            while DATA[h] == 0xCC:
                h += 1
            return rva - (o - h)
    return None

def callers_of(target_rva):
    """Direct E8 rel32 callers across real code."""
    res = []
    tv = BASE + target_rva
    for va, praw, sz in EXSECS:
        blob = DATA[praw:praw+sz]; n=len(blob)
        for i in range(n-5):
            rva = va+i
            if rva >= REAL_MAX: break
            if blob[i] == 0xE8:
                rel = int.from_bytes(blob[i+1:i+5], 'little', signed=True)
                if (BASE+rva+5+rel) == tv:
                    res.append(rva)
    return res

def jmps_to(target_rva):
    """Direct E9 rel32 jumps to target (tail-call detection)."""
    res = []
    tv = BASE + target_rva
    for va, praw, sz in EXSECS:
        blob = DATA[praw:praw+sz]; n=len(blob)
        for i in range(n-5):
            rva = va+i
            if rva >= REAL_MAX: break
            if blob[i] == 0xE9:
                rel = int.from_bytes(blob[i+1:i+5], 'little', signed=True)
                if (BASE+rva+5+rel) == tv:
                    res.append(rva)
    return res

def disasm_win(rva, back, length, marks=None):
    marks = marks or {}
    start = rva-back
    b = DATA[off(start):off(start)+length]
    out=[]
    for ins in MD.disasm(b, BASE+start):
        r = ins.address-BASE
        t = f"{ins.mnemonic} {ins.op_str}".strip(); low=t.lower(); mk=""
        for needle,label in marks.items():
            if needle in low:
                mk = "  <== "+label; break
        if not mk:
            if ins.mnemonic=="call": mk="  ; call"
            elif ins.mnemonic in ("ret","int3"): mk="  ; --"
        out.append(f"    {r:08X}: {hb(ins.bytes):<26} {t}{mk}")
    return "\n".join(out)

def is_vm_thunk(rva):
    """True if head is E9 (jmp) into VM region (>= REAL_MAX)."""
    o = off(rva);
    if DATA[o]==0xE9:
        rel=int.from_bytes(DATA[o+1:o+5],'little',signed=True)
        dst=rva+5+rel
        return dst>=REAL_MAX or dst<0x1000, dst
    return False, None
