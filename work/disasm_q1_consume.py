#!/usr/bin/env python3
"""Q1: miss-consumption signal.
(a) who writes evade-type byte +0x1C0 (mov [reg+0x1C0], ...) — enumerate all
    real-code stores to +0x1C0 and classify each site's context.
(b) selector 0x205210 callers.
(c) the evade-record store sites 0x284BEC/.../0x3964A5 as commit markers.
"""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, callers_of, jmps_to, disasm_win,
                             off, DATA, hb, is_vm_thunk, BASE, REAL_MAX, MD, EXSECS)

def scan_stores_to_disp(disp, want_mnem_prefix=("mov",)):
    """Find real-code instructions writing to [reg + disp] with a 32-bit disp.
    disp e.g. 0x1C0. We disassemble linearly per section and filter by operand."""
    hits=[]
    dstr = f"+ 0x{disp:x}]"
    for va, praw, sz in EXSECS:
        # linear disasm is expensive over whole sections; only real code
        end = min(sz, (REAL_MAX - va)) if va < REAL_MAX else 0
        if end <= 0: continue
        code = DATA[praw:praw+end]
        for ins in MD.disasm(code, BASE+va):
            r = ins.address - BASE
            if r >= REAL_MAX: break
            ops = ins.op_str.lower()
            if dstr in ops:
                # is it a write (dest = memory)? crude: op_str starts with the mem operand
                # capstone puts dest first for x86 mov
                first = ops.split(",")[0].strip()
                if dstr in first and ins.mnemonic.startswith("mov"):
                    hits.append((r, f"{ins.mnemonic} {ins.op_str}", hb(ins.bytes)))
    return hits

print("="*80)
print("Q1(a) :: real-code stores to [reg + 0x1C0] (evade-type byte writers)")
print("="*80)
for r,txt,by in scan_stores_to_disp(0x1C0):
    h=find_head(r)
    print(f"  0x{r:X}  (fn 0x{h:X})  {by:<20} {txt}")

print("\n" + "="*80)
print("Q1(a) context :: known teardown copy 0x205B38 and its function")
print("="*80)
h=find_head(0x205B38)
print(f"0x205B38 in fn head 0x{h:X}")
print(disasm_win(0x205B38, 0x40, 0x40, {"+ 0x1c0":"WRITE 1C0","test":"gate","+ 0x15c":"gate 15C"}))

print("\n" + "="*80)
print("Q1(b) :: selector 0x205210 — head, callers")
print("="*80)
sh=find_head(0x205210)
print(f"selector head 0x{sh:X}")
print(disasm_win(0x205210, 0, 0x40, {"+ 0x148":"record","+ 0x1e5":"effect-kind","+ 0x1c4":"staged dmg","+ 0x1c0":"evade byte"}))
cs=callers_of(0x205210); js=jmps_to(0x205210)
print(f"\ndirect callers of 0x205210: {[hex(c) for c in cs]}")
print(f"tail jumps to 0x205210: {[hex(c) for c in js]}")
for c in cs[:8]:
    ch=find_head(c)
    print(f"\n--- caller 0x{c:X} (fn 0x{ch:X}) ---")
    print(disasm_win(c, 0x38, 0x50, {"+ 0x148":"record","+ 0x1c0":"evade","cl":"evade arg"}))

print("\n" + "="*80)
print("Q1(c) :: evade-record store sites as commit markers")
print("="*80)
for site in (0x284BEC,0x284C00,0x284C28,0x3602D6,0x3602EA,0x360313,0x396468,0x39647C,0x3964A5):
    h=find_head(site)
    print(f"\n--- store site 0x{site:X} (fn 0x{h:X}) ---")
    print(disasm_win(site, 0x18, 0x28, {"+ 0x44":"rec+44","+ 0x46":"rec+46","+ 0x50":"rec+50"}))
