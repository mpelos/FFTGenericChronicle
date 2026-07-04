#!/usr/bin/env python3
"""Q2: classify all xrefs to computeActionResult 0x309A44, and probe the
reaction dispatch cluster 0x30BE86..0x30BF72 for a sibling result computer."""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, callers_of, jmps_to, disasm_win,
                             off, DATA, hb, is_vm_thunk, BASE, REAL_MAX, MD, pe)

TARGET = 0x309A44
print("="*80)
print(f"Q2 :: xrefs to computeActionResult 0x{TARGET:X}")
print("="*80)

# Is 0x309A44 a function head, or mid-function?
head = find_head(TARGET)
print(f"\nfind_head(0x{TARGET:X}) = 0x{head:X}  (delta {TARGET-head:#x})")
thunk,dst = is_vm_thunk(head)
print(f"head is VM thunk: {thunk} (dst {hex(dst) if dst else None})")
print("\n--- head prologue ---")
print(disasm_win(head, 0, 0x30, {"1a0":"caster order rec","+ 0x142":"executor copy"}))

callers = callers_of(head)
print(f"\ndirect E8 callers of head 0x{head:X}: {[hex(c) for c in callers]}")
tj = jmps_to(head)
print(f"tail E9 jumps to head 0x{head:X}: {[hex(c) for c in tj]}")

# also callers of the exact TARGET (in case mods hook mid-func or there's a
# call straight to 0x309A44)
if head != TARGET:
    c2 = callers_of(TARGET); j2 = jmps_to(TARGET)
    print(f"direct E8 callers of 0x{TARGET:X}: {[hex(c) for c in c2]}")
    print(f"tail E9 jumps to 0x{TARGET:X}: {[hex(c) for c in j2]}")

for c in callers:
    ch = find_head(c)
    print("\n" + "#"*76)
    print(f"# CALLER 0x{c:X}  (in fn head 0x{ch:X})")
    print("#"*76)
    print(disasm_win(c, 0x40, 0x60, {"1a0":"order rec","reaction":"react",
          "+ 0x94":"react bitfield","+ 0x142":"executor","+ 0x1a2":"pending"}))

# --- reaction cluster ---
print("\n\n" + "="*80)
print("Q2 :: reaction-roll cluster 0x30BE86..0x30BF72")
print("="*80)
for site in (0x30BE86,0x30BEDC,0x30BF32,0x30BF72,0x30BE8B,0x30B584):
    h = find_head(site)
    print(f"\n--- site 0x{site:X} in fn head 0x{h:X} ---")
    print(disasm_win(site, 0x20, 0x40, {"278ee0":"shared RNG","+ 0x2b":"Brave",
          "+ 0x94":"react bits","14186aff0":"react-eval id"}))

# What function contains the reaction cluster, and what does it call?
print("\n\n" + "="*80)
print("Q2 :: reaction dispatcher fn 0x30B584 body — find its result-compute calls")
print("="*80)
rh = find_head(0x30B584)
print(f"reaction dispatch head 0x{rh:X}")
# walk the whole function, collect E8 call targets
o = off(rh); end = o + 0x900
calls=[]
for ins in MD.disasm(DATA[o:end], BASE+rh):
    r=ins.address-BASE
    if r>=REAL_MAX: break
    if ins.mnemonic=="call" and ins.op_str.startswith("0x"):
        tgt=int(ins.op_str,16)-BASE
        calls.append((r,tgt))
    if ins.mnemonic=="ret" and (ins.address-BASE-rh)>0x40:
        # keep going a bit past first ret in case of multiple blocks
        pass
print(f"\ncall targets from reaction dispatch fn (first 40):")
for r,tgt in calls[:40]:
    th=is_vm_thunk(tgt)
    tag = "VM-thunk" if th[0] else "real"
    print(f"    call @0x{r:X} -> 0x{tgt:X}  [{tag}]  {'==0x309A44!' if tgt==TARGET or tgt==head else ''}")
