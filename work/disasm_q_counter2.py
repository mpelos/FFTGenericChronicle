#!/usr/bin/env python3
"""Q2 final: trace how the reaction dispatcher 0x30B410 turns a detected counter
into an executed action, and whether that path re-enters computeActionResult
0x309A44 or bypasses it. Map the 5 apply-fn callers. Examine the tail of 0x30B410
(the 0x30BBD8 region calling 0x30B30C then recursing 0x30B410, then 0x41FB70)."""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, callers_of, jmps_to, disasm_win,
                             off, DATA, hb, is_vm_thunk, BASE, REAL_MAX, MD, EXSECS, pe)

print("="*80)
print("(A) reaction dispatcher 0x30B410 body — decision + enqueue region")
print("="*80)
print(disasm_win(0x30B410,0,0x120,{"+ 0x1a1":"pending","+ 0x1a2":"ability","+ 0x142":"executor",
      "1853ce0":"unit tbl","+ 0x94":"react bits","+ 0x65":"react65"}))

print("\n--- tail region 0x30BB80..0x30BC30 (enqueue / recurse) ---")
print(disasm_win(0x30BB80,0,0xB0,{"0x30b410":"RECURSE dispatch","0x41fb70":"util",
      "+ 0x1a1":"pending","+ 0x1a2":"ability","+ 0x142":"executor"}))

print("\n" + "="*80)
print("(B) apply fn 0x30A51C — all 5 callers classified")
print("="*80)
for c in (0x20452b,0x2047bf,0x20c06e,0x30c7dc,0x38a6f9):
    print(f"\n--- caller 0x{c:X} (fn 0x{find_head(c):X}) ---")
    print(disasm_win(c,0x28,0x30,{"0x30a51c":"APPLY","+ 0x1c4":"dmg","+ 0x300":"cat300"}))

print("\n" + "="*80)
print("(C) does the counter re-enter the EXEC caller 0x307E90? refs & the")
print("    action-executor global [0x15630d8] writers")
print("="*80)
# 0x307E90 read global [rip+0x15630d8] i.e. current actor. find its RVA/VA
# instruction was at 0x307E99: mov rax,[rip+0x15630d8]; next ins at 0x307EA0
g_va = 0x307EA0 + 0x15630d8
print(f"exec-actor global VA ~0x{g_va:X} (RVA 0x{g_va-BASE:X})")
# who writes it? scan real code for mov [rip+X], reg resolving to g_va
gv_hex=f"0x{g_va:x}"
writers=[]
for va, praw, sz in EXSECS:
    end = min(sz,(REAL_MAX-va)) if va<REAL_MAX else 0
    if end<=0: continue
    for ins in MD.disasm(DATA[praw:praw+end], BASE+va):
        r=ins.address-BASE
        if r>=REAL_MAX: break
        ol=ins.op_str.lower()
        if gv_hex in ol and ins.mnemonic.startswith("mov") and ol.split(",")[0].strip().startswith("qword ptr [rip"):
            writers.append((r,f"{ins.mnemonic} {ins.op_str}"))
print("writers of exec-actor global:", [(hex(r),t) for r,t in writers][:12])

print("\n" + "="*80)
print("(D) 0x307E90 fn — its callers (who invokes execution)?")
print("="*80)
# 0x307E90 was in a jump-table; find who jmp/calls near it. Also its own head siblings.
# Try: callers of 0x307E90 via E8/E9 already empty. Check indirect: the table at 0x682cf0 held OTHER handlers.
# The exec fn is reached by 'jmp [table+idx*8]'? Search for that dispatch.
# Instead, find what calls the fn 0x306XXX helpers 0x307E90 calls -> the pipeline.
# Simpler: dump the switch that reads global action-type and jmps into 0x307Exx family.
# Look at callers of 0x3065F0 (first call in 0x307E90) to find the shared pipeline entry.
for probe in (0x3065F0,0x305F9C):
    cs=callers_of(probe)
    print(f"  fn 0x{probe:X} callers: {[hex(c) for c in cs][:12]}")
