#!/usr/bin/env python3
"""Trace caller chains upward for the evade-copy functions, and test whether any
lands near / on the path into the avoidance cluster.

Functions of interest (heads):
  F_shield = 0x59F550   (writes 0x48-0x4F incl class/shield evade)  caller 0x59CACD
  F_wpn2   = 0x285394   (writes 0x44-0x47 weapon parry)
  F_wpn4   = 0x3965B0   (writes 0x44-0x47 weapon parry)
Avoidance cluster: 0x30F0C4 producer, 0x30FA34 roll, 0x205210 selector,
  0x30A66F apply, 0x309A44 compute-entry.
We build a callers map (E8 rel32) and BFS up a few levels from each F, printing
the chain, and flag if any function contains a call to 0x30F0C4/0x30FA34/0x309A44
or is itself in [0x309000..0x310000].
"""
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
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64)
def off(rva): return pe.get_offset_from_rva(rva)
exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
          for s in pe.sections if s.Characteristics & 0x20000000]

# Build ALL call edges: caller_rva -> target_rva
edges = []
for va, praw, sz in exsecs:
    blob = data[praw:praw+sz]; n=len(blob)
    for i in range(n-5):
        rva = va+i
        if rva >= REAL_MAX: break
        if blob[i]==0xE8:
            rel = int.from_bytes(blob[i+1:i+5],'little',signed=True)
            dst = rva+5+rel
            if 0 <= dst < REAL_MAX:
                edges.append((rva, dst))

def find_head(rva):
    o = off(rva)
    for k in range(1, 0x3000):
        if data[o-k]==0xCC and data[o-k-1]==0xCC:
            h=o-k
            while data[h]==0xCC: h+=1
            return rva-(o-h)
    return rva

# map target-head -> list of caller-heads
from collections import defaultdict
callers = defaultdict(set)
for cr, dst in edges:
    callers[dst].add(find_head(cr))

def contains_call_to(fn_head, targets, span=0x1200):
    """does function starting fn_head call any of targets?"""
    lo, hi = fn_head, fn_head+span
    for cr, dst in edges:
        if lo <= cr < hi and dst in targets:
            return dst
    return None

AVOID = {0x30F0C4, 0x30FA34, 0x309A44, 0x30A66F, 0x205210}
# also find the heads of these
avoid_heads = {t: find_head(t) for t in AVOID}
print("avoidance-cluster function heads:")
for t,h in avoid_heads.items():
    print(f"  0x{t:X} -> head 0x{h:X}")
print()

def trace(name, head, depth=4):
    print("="*70)
    print(f"{name}: head 0x{head:X}")
    seen=set()
    frontier=[(head,[head])]
    for lvl in range(depth):
        nxt=[]
        for fn,path in frontier:
            for c in sorted(callers.get(fn,())):
                if c in seen: continue
                seen.add(c)
                # does this caller call into avoidance cluster?
                flag = contains_call_to(c, set(avoid_heads.values()) | AVOID)
                near = "  IN-AVOID-RANGE" if 0x309000 <= c <= 0x310000 else ""
                fl = f"  ==CALLS-AVOID 0x{flag:X}" if flag else ""
                print(f"  L{lvl+1} {'  '*lvl}0x{c:X}{near}{fl}")
                nxt.append((c, path+[c]))
        frontier=nxt
        if not frontier: break

for name, h in [("F_shield(0x48-4F)",0x59F550),
                ("F_wpn(0x285394)",0x285394),
                ("F_wpn(0x3965B0)",0x3965B0)]:
    trace(name, h, depth=4)
    print()

# Also: is any evade-copy function called from within the avoidance functions?
print("="*70)
print("Do avoidance functions call the evade-copy functions? (reverse check)")
for a,ah in avoid_heads.items():
    for tgt in (0x59F550,0x285394,0x3965B0,0x59CACD,0x285394):
        f = contains_call_to(ah, {tgt})
        if f: print(f"  avoid 0x{a:X}(head 0x{ah:X}) calls 0x{tgt:X}")
print("done")
