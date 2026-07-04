#!/usr/bin/env python3
"""Refined: byte-pattern scan for stores to +0x1C0 (avoid linear-disasm desync),
plus trace the selector-caller functions 0x26A580/0x26A704 (what calls THEM,
are they per-execution?), and the computeActionResult caller functions
0x281D60 (loop/AI) vs 0x307E90 (execution)."""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, callers_of, jmps_to, disasm_win,
                             off, DATA, hb, is_vm_thunk, BASE, REAL_MAX, MD, EXSECS)

def disasm_at(rva, length):
    """Disasm starting EXACTLY at rva (aligned)."""
    return disasm_win(rva, 0, length)

# ---- byte-pattern scan for stores whose disp32 == 0x1C0 ----
# mov [reg+disp32], r8/16/32 forms. We look for the 4-byte little-endian 0x000001C0
# appearing as a displacement in a mov store. Simpler: scan for known encodings:
#  88 xx C0 01 00 00        mov [reg+0x1C0], r8   (ModRM reg field varies)
#  44 88 xx C0 01 00 00     mov [reg+0x1C0], r8..r15 (REX.R)
#  66 89 xx C0 01 00 00     mov [reg+0x1C0], r16
#  89 xx C0 01 00 00        mov [reg+0x1C0], r32
# We scan for the displacement tail "C0 01 00 00" then verify capstone decodes a
# store to +0x1c0 at a plausible instruction start (back up to 7 bytes).
print("="*80)
print("Q1(a) byte-scan :: any instruction touching [reg+0x1C0]")
print("="*80)
tail = bytes([0xC0,0x01,0x00,0x00])
seen=set()
for va, praw, sz in EXSECS:
    end = min(sz, (REAL_MAX - va)) if va < REAL_MAX else 0
    if end<=0: continue
    blob = DATA[praw:praw+end]
    idx=0
    while True:
        j = blob.find(tail, idx)
        if j<0: break
        idx=j+1
        # try instruction starts from j-6 .. j-2 (disp32 sits at end of modrm+disp)
        for back in range(2,8):
            st = j-back
            if st<0: continue
            try:
                for ins in MD.disasm(DATA[praw+st:praw+st+16], BASE+va+st):
                    disp_ok = f"+ 0x1c0]" in ins.op_str.lower()
                    # ensure the tail we found is THIS instruction's disp
                    if disp_ok and (va+st) not in seen:
                        seen.add(va+st)
                        r=va+st; h=find_head(r)
                        print(f"  0x{r:X} (fn 0x{h:X})  {hb(ins.bytes):<22} {ins.mnemonic} {ins.op_str}")
                    break
            except Exception:
                pass
print("(scan complete)")

# ---- selector-caller functions: what invokes 0x26A580 / 0x26A704? ----
print("\n" + "="*80)
print("Q1(b) :: callers of the render fns that call the selector")
print("="*80)
for fn in (0x26A580, 0x26A704):
    cs = callers_of(fn); js=jmps_to(fn)
    print(f"\nfn 0x{fn:X}: E8 callers={[hex(c) for c in cs]}  E9 jumps={[hex(c) for c in js]}")
    for c in cs[:6]:
        ch=find_head(c)
        print(f"  caller 0x{c:X} in fn 0x{ch:X}")

# ---- computeActionResult callers: distinguish exec vs preview/AI ----
print("\n" + "="*80)
print("Q2/Q1 :: computeActionResult callers — fn heads 0x281D60 (loop) vs 0x307E90")
print("="*80)
for fn in (0x281D60, 0x307E90):
    cs=callers_of(fn); js=jmps_to(fn)
    print(f"\nfn 0x{fn:X}: E8 callers={[hex(c) for c in cs]}  E9 jumps={[hex(c) for c in js]}")

# ---- exact realign of 0x2055FC teardown fn (the +0x1C0 writer) ----
print("\n" + "="*80)
print("Q1(a) :: fn 0x2055FC (0x205B38 teardown, sole real writer of +0x1C0) — aligned")
print("="*80)
# disasm from head so alignment is correct, then find the 0x1C0 line
h=0x2055FC
code=DATA[off(h):off(h)+0x600]
for ins in MD.disasm(code, BASE+h):
    r=ins.address-BASE
    if r>=REAL_MAX: break
    ops=ins.op_str.lower()
    if "+ 0x1c0]" in ops or "+ 0x15c]" in ops or (ins.mnemonic=="test" and "0x15c" in ops):
        print(f"  0x{r:X}  {hb(ins.bytes):<22} {ins.mnemonic} {ins.op_str}")
    if r>0x205C00: break
