#!/usr/bin/env python3
"""Deep dive:
(1) fn 0x2055FC — the +0x1C0 writer: full body head, its callers, the 0x15C gate.
(2) fn 0x31BEC0 — the render/animation dispatcher calling the selector-render fns.
(3) computeActionResult callers 0x281D60 / 0x307E90 — how are they entered (indirect)?
    Scan for the function-pointer references (their VA appearing as data / lea).
(4) Q2 counter path: fn 0x30B410 reaction dispatcher full body; find where a
    counter's damage result is computed (a call reaching pre-clamp 0x30A66F region
    or a sibling of computeActionResult).
"""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, callers_of, jmps_to, disasm_win,
                             off, DATA, hb, is_vm_thunk, BASE, REAL_MAX, MD, EXSECS, pe)

def refs_to_va(target_rva):
    """Find lea/mov rip-relative refs and absolute-imm refs to target VA in real code
    (i.e. someone taking the address of this function -> stored in a dispatch table)."""
    tv = BASE + target_rva
    res=[]
    # rip-relative lea: 48 8D 05 xx xx xx xx (lea rax,[rip+d]); ModRM /r varies.
    # Simpler: linear disasm real code, check any op referencing tv.
    tvhex = f"0x{tv:x}"
    for va, praw, sz in EXSECS:
        end = min(sz, (REAL_MAX - va)) if va < REAL_MAX else 0
        if end<=0: continue
        for ins in MD.disasm(DATA[praw:praw+end], BASE+va):
            r=ins.address-BASE
            if r>=REAL_MAX: break
            if tvhex in ins.op_str.lower():
                res.append((r, f"{ins.mnemonic} {ins.op_str}"))
    return res

def data_refs_to_va(target_rva):
    """Find the 8-byte little-endian VA appearing anywhere (dispatch/vtable data)."""
    tv = (BASE + target_rva).to_bytes(8,'little')
    res=[]; idx=0
    while True:
        j=DATA.find(tv, idx)
        if j<0: break
        idx=j+1
        try: rva=pe.get_rva_from_offset(j)
        except Exception: rva=None
        res.append((j,rva))
    return res

print("="*80)
print("(1) fn 0x2055FC — +0x1C0 writer: prologue + callers")
print("="*80)
print(disasm_win(0x2055FC, 0, 0x40, {"+ 0x15c":"gate","+ 0x1c0":"WRITE evade"}))
cs=callers_of(0x2055FC); js=jmps_to(0x2055FC)
print(f"\nE8 callers: {[hex(c) for c in cs]}  E9 jumps: {[hex(c) for c in js]}")
for c in cs[:8]:
    print(f"\n--- caller 0x{c:X} (fn 0x{find_head(c):X}) ---")
    print(disasm_win(c,0x30,0x40,{"+ 0x15c":"gate","0x309a44":"computeAR","0x205210":"selector"}))

print("\n" + "="*80)
print("(2) fn 0x31BEC0 — render/animation dispatcher (calls 0x26A704 render)")
print("="*80)
print(disasm_win(0x31C830-0x30,0x0,0x80,{"0x26a704":"render","+ 0x1c0":"evade","+ 0x1c4":"dmg"}))
cs=callers_of(0x31BEC0)
print(f"\n0x31BEC0 E8 callers: {[hex(c) for c in cs]}")

print("\n" + "="*80)
print("(3) how computeActionResult callers are entered — refs to 0x281D60 / 0x307E90")
print("="*80)
for fn in (0x281D60,0x307E90):
    print(f"\nfn 0x{fn:X}:")
    cr=refs_to_va(fn)
    print("  code refs:", [(hex(r),t) for r,t in cr][:10])
    dr=data_refs_to_va(fn)
    print("  data (dispatch-table) refs:", [(hex(off_),hex(rva) if rva else None) for off_,rva in dr][:10])

print("\n" + "="*80)
print("(4) Q2 :: reaction dispatcher 0x30B410 — full call graph, hunt result-compute")
print("="*80)
h=0x30B410
code=DATA[off(h):off(h)+0xB00]
calls=[]
for ins in MD.disasm(code, BASE+h):
    r=ins.address-BASE
    if r>=REAL_MAX: break
    if ins.mnemonic=="call" and ins.op_str.startswith("0x"):
        tgt=int(ins.op_str,16)-BASE
        thunk=is_vm_thunk(tgt)
        calls.append((r,tgt,thunk[0],thunk[1]))
    if ins.mnemonic in ("ret",) and r-h>0x900: break
print(f"reaction dispatch 0x{h:X} calls:")
for r,tgt,th,dst in calls:
    print(f"  @0x{r:X} -> 0x{tgt:X} [{'VMthunk->0x%X'%dst if th else 'real'}]")

# 0x2BB0D4 was the skillset resolver; trace it if real
print("\n--- what calls into the pre-clamp/apply region for counters? ---")
# find callers of computeActionResult sibling candidates near 0x307E90 exec fn:
# also: does anything call 0x30A66F's function? find its head
ph=find_head(0x30A66F)
print(f"pre-clamp 0x30A66F in fn head 0x{ph:X}")
pcs=callers_of(ph); pjs=jmps_to(ph)
print(f"  pre-clamp fn E8 callers: {[hex(c) for c in pcs]}  E9: {[hex(c) for c in pjs]}")
