#!/usr/bin/env python3
"""Final confirmations:
- fn 0x30C798 (distinct apply-caller = the reaction/counter apply staging?) head+context.
- fn 0x38A4FC result dispatcher: does the SAME queue carry a reaction record with
  attacker/target/ability? examine the record fields it reads.
- 0x281D60 sweep: what array/flag distinguishes candidate-eval from execution.
- selector 0x205210: read its +0x1BE/+0x1C4 gate to confirm it's render-only (both
  preview & execute fire it) — i.e. NOT a clean once-per-execution consumption signal.
- The 0x15C gate on the +0x1C0 writer 0x2055FC — what is bit 4 of +0x15C?
"""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, callers_of, jmps_to, disasm_win,
                             off, DATA, hb, is_vm_thunk, BASE, REAL_MAX, MD, EXSECS, pe)

print("="*80)
print("(A) fn 0x30C798 — the distinct apply-caller (counter/reaction staging?)")
print("="*80)
print(disasm_win(0x30C798,0,0x90,{"+ 0x1bc":"target idx","+ 0x1c4":"dmg","+ 0x1a0":"order rec",
      "+ 0x142":"executor","0x30a51c":"APPLY","1853ce0":"unit tbl","+ 0x1a2":"ability"}))
cs=callers_of(0x30C798); js=jmps_to(0x30C798)
print(f"\n0x30C798 callers: {[hex(c) for c in cs]}  jumps: {[hex(c) for c in js]}")
for c in cs[:6]:
    print(f"  caller 0x{c:X} in fn 0x{find_head(c):X}")

print("\n" + "="*80)
print("(B) 0x2055FC +0x1C0 writer — the +0x15C bit-4 gate + what value goes to +0x1C0")
print("="*80)
# dump around 0x205B00..0x205B60 aligned from head to see r12b source
h=0x2055FC
code=DATA[off(h):off(h)+0x600]
prev=[]
for ins in MD.disasm(code, BASE+h):
    r=ins.address-BASE
    if 0x205AF0<=r<=0x205B60:
        print(f"  0x{r:X}  {hb(ins.bytes):<20} {ins.mnemonic} {ins.op_str}")

print("\n" + "="*80)
print("(C) selector 0x205210 — the render gate (+0x1BE / +0x1C4 read) confirming")
print("    it is render-side and fires for both preview and execution")
print("="*80)
print(disasm_win(0x205210,0,0x120,{"+ 0x148":"record","+ 0x1e5":"effect","+ 0x1c4":"dmg",
      "+ 0x1c0":"evade in","+ 0x1be":"1be flag","+ 0x1bb":"1bb"}))

print("\n" + "="*80)
print("(D) 0x281D60 sweep discriminator — where the per-target loop lives (0x281F72)")
print("="*80)
print(disasm_win(0x281F45,0,0x60,{"0x309a44":"computeAR","0x15":"count21","+ 0x28":"targetlist",
      "52e807":"g_targetidx","52e841":"g"}))
