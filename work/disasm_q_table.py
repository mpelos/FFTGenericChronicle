#!/usr/bin/env python3
"""Nail Q2: the action-handler dispatch table @ data 0x682ce0 (holds 0x307E90),
its neighbors (sibling handlers incl. the counter/reaction one), and the
reaction siblings 0x30B234 / 0x30B30C / 0x41FB70. Also classify 0x281D60 vs 0x307E90."""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, callers_of, jmps_to, disasm_win,
                             off, DATA, hb, is_vm_thunk, BASE, REAL_MAX, MD, EXSECS, pe)

def read_va(fileoff):
    return int.from_bytes(DATA[fileoff:fileoff+8],'little')

# The data ref was at file offset 0x682ce0 -> a qword table of function pointers.
print("="*80)
print("(A) dispatch table around file-offset 0x682ce0 (holds ptr to 0x307E90)")
print("="*80)
# dump 24 qwords centered
start = 0x682ce0 - 8*8
for i in range(24):
    fo = start + i*8
    va = read_va(fo)
    rva = va - BASE if BASE <= va < BASE+0x1000000 else None
    tag=""
    if rva is not None and 0x1000 <= rva < REAL_MAX:
        th=is_vm_thunk(rva)
        tag = f"-> RVA 0x{rva:X} " + ("[VMthunk->0x%X]"%th[1] if th[0] else "[real]")
        if rva==0x307E90: tag+="  <== computeAR EXEC caller"
    marker = "  <==@0x682ce0" if fo==0x682ce0 else ""
    print(f"  fo 0x{fo:X}: {va:016X} {tag}{marker}")

# what reads this table? find code that references VA of table start region
print("\n" + "="*80)
print("(B) who indexes this table — code refs to the table VA")
print("="*80)
tbl_rva = pe.get_rva_from_offset(0x682ce0)
tbl_va = BASE + tbl_rva
print(f"table @ RVA 0x{tbl_rva:X} VA 0x{tbl_va:X}")
# scan real code for rip-relative lea/mov referencing near this VA (within table span)
for va, praw, sz in EXSECS:
    end = min(sz, (REAL_MAX - va)) if va < REAL_MAX else 0
    if end<=0: continue
    for ins in MD.disasm(DATA[praw:praw+end], BASE+va):
        r=ins.address-BASE
        if r>=REAL_MAX: break
        # rip-relative operand resolves to an absolute; capstone shows it
        for tv in (tbl_va, tbl_va-0x40, tbl_va-0x80):
            if f"0x{tv:x}" in ins.op_str.lower():
                print(f"  0x{r:X}: {ins.mnemonic} {ins.op_str}")
                break

print("\n" + "="*80)
print("(C) computeAR caller contexts — 0x281D60 (sweep) vs 0x307E90 (exec)")
print("="*80)
print("--- 0x281D60 head (the 21-unit loop caller) ---")
print(disasm_win(0x281D60,0,0x40,{"1853ce0":"unit table","1853e80":"unit+rec"}))
print("\n--- 0x307E90 head (exec caller) ---")
print(disasm_win(0x307E90,0,0x60,{"+ 0x1a1":"pending exec","+ 0x1a2":"ability","0x309a44":"computeAR"}))

print("\n" + "="*80)
print("(D) reaction siblings: 0x30B234, 0x30B30C, 0x41FB70")
print("="*80)
for fn in (0x30B234,0x30B30C,0x41FB70):
    print(f"\n--- fn 0x{fn:X} head ---")
    print(disasm_win(fn,0,0x50,{"0x309a44":"computeAR!","0x30a51c":"apply","+ 0x1c4":"staged dmg","+ 0x1c0":"evade","278ee0":"RNG"}))
    # does it call computeActionResult or the pre-clamp/apply fn?
    code=DATA[off(fn):off(fn)+0x600]
    innercalls=[]
    for ins in MD.disasm(code, BASE+fn):
        r=ins.address-BASE
        if r>=REAL_MAX: break
        if ins.mnemonic=="call" and ins.op_str.startswith("0x"):
            innercalls.append(int(ins.op_str,16)-BASE)
        if ins.mnemonic=="ret" and r-fn>0x40: break
    tagged=[]
    for t in innercalls:
        th=is_vm_thunk(t)
        s="0x%X"%t
        if t==0x309A44: s+="(computeAR)"
        if t==0x30A51C: s+="(apply)"
        if th[0]: s+="[VM]"
        tagged.append(s)
    print("  calls:", tagged)
