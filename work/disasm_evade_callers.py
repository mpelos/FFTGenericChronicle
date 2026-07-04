#!/usr/bin/env python3
"""Find function head for a given RVA (scan back for prologue after int3 pad),
and enumerate direct callers (E8 rel32) of that head across real code.
Also, for GROUP2/4/13, dump their function heads/context.
"""
from __future__ import annotations
from pathlib import Path
import pefile, sys
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
REAL_MAX = 0x610000
pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)
exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
          for s in pe.sections if s.Characteristics & 0x20000000]

def find_head(rva):
    """Walk back to the previous int3-pad boundary; the byte after pad is head."""
    o = off(rva)
    # scan back up to 0x2000 for a run of int3 (CC CC) or ret+align
    for k in range(1, 0x2000):
        if data[o-k] == 0xCC and data[o-k-1] == 0xCC:
            # head is first non-CC after this
            h = o-k
            while data[h] == 0xCC:
                h += 1
            return rva - (o - h)
    return None

def callers_of(target_rva):
    res = []
    tv = base + target_rva
    for va, praw, sz in exsecs:
        blob = data[praw:praw+sz]; n=len(blob)
        for i in range(n-5):
            rva = va+i
            if rva >= REAL_MAX: break
            if blob[i] == 0xE8:
                rel = int.from_bytes(blob[i+1:i+5], 'little', signed=True)
                dst = (base+rva+5+rel)
                if dst == tv:
                    res.append(rva)
    return res

def dumpwin(rva, back, length, title):
    start = rva-back
    b = data[off(start):off(start)+length]
    print("#"*76); print(f"# {title}  head~0x{rva:X}"); print("#"*76)
    for ins in md.disasm(b, base+start):
        r = ins.address-base
        t = f"{ins.mnemonic} {ins.op_str}".strip(); low=t.lower(); mk=""
        if "1853ce0" in low: mk="  <== UNIT TABLE"
        elif ins.mnemonic=="call": mk="  <== CALL"
        elif any(f"+ 0x{d:02x}]" in low for d in range(0x44,0x50)): mk="  <-- cluster"
        elif ins.mnemonic in ("ret","int3"): mk="  --"
        print(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")

# --- GROUP0 function head + callers ---
site = 0x59F8F6
head = find_head(site)
print(f"GROUP0 site 0x{site:X} -> function head 0x{head:X}\n")
dumpwin(head, 0, 0x60, "GROUP0 HEAD")
cs = callers_of(head)
print(f"\nGROUP0 direct callers of 0x{head:X}: {[hex(c) for c in cs]}\n")
for c in cs[:8]:
    dumpwin(c, 0x30, 0x50, f"caller 0x{c:X}")

print("\n\n======== GROUP2 (0x28553B) ========")
h2 = find_head(0x28553B); print(f"head 0x{h2:X}")
dumpwin(0x28553B, 0x30, 0x60, "GROUP2 writes 0x44-0x47")

print("\n\n======== GROUP4 (0x39672D) ========")
h4 = find_head(0x39672D); print(f"head 0x{h4:X}")
dumpwin(0x39672D, 0x40, 0x60, "GROUP4 writes 0x44-0x47")

print("\n\n======== GROUP13 (0x558C0F) ========")
h13 = find_head(0x558C0F); print(f"head 0x{h13:X}")
dumpwin(0x558C0F, 0x40, 0x70, "GROUP13 writes 0x44-0x46")
