#!/usr/bin/env python3
"""Byte-pattern candidate scan for disp32 = 0x1C0 / 0x360 / 0x15C / 0x344, then
ALIGNED-verify each candidate by re-disassembling from its function head.
This is the rigorous methodology: raw scan only proposes; find_head+aligned confirms."""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, disasm_win, off, DATA, hb, BASE, REAL_MAX, MD, EXSECS)

def candidates_for_disp(disp):
    """Find every file position whose 4 bytes little-endian == disp (a plausible disp32).
    Also catch disp8 form for small disps. Returns list of RVAs (approx instr-containing)."""
    d32 = disp.to_bytes(4,'little')
    out=[]
    for va, praw, sz in EXSECS:
        end = min(sz, (REAL_MAX - va)) if va < REAL_MAX else 0
        blob = DATA[praw:praw+end]
        i=0
        while True:
            j = blob.find(d32, i)
            if j<0: break
            out.append(va+j)   # this is where the disp32 bytes sit; instr starts a few bytes earlier
            i=j+1
    return out

def aligned_instr_at_disp(disp, disp_pos_rva, window=24):
    """Given the RVA where the disp32 bytes appear, find the enclosing instruction by
    aligned disasm from function head, and return the instruction whose operand string
    contains '+ 0x{disp:x}]'. Confirms it's real & the disp belongs to a modrm."""
    # the instruction starts within ~10 bytes before disp_pos
    approx = disp_pos_rva - 8
    h = find_head(approx)
    if h is None:
        h = find_head(disp_pos_rva)
    if h is None: return None
    o = off(h); end = off(disp_pos_rva) + 16
    if end - o > 0x8000:
        # head too far; try nearer synthetic head
        h2 = disp_pos_rva - 0x40
        o = off(h2); end = off(disp_pos_rva)+16
        base_va = h2
    else:
        base_va = h
    target = f"0x{disp:x}]"
    for ins in MD.disasm(DATA[o:end], BASE+base_va):
        r = ins.address - BASE
        # instruction that consumes the disp bytes: disp_pos is within [r, r+len)
        if r <= disp_pos_rva < r + ins.size:
            ops = ins.op_str.lower()
            if target in ops and "0x1"[:0] or (f"+ {target}" in ops or f"- {target}" in ops or target in ops):
                first = ops.split(",")[0].strip()
                is_write = (f"+ {target}" in first or target in first) and ("ptr" in first)
                return (r, is_write, f"{ins.mnemonic} {ins.op_str}", find_head(r))
            return None
        if r > disp_pos_rva:
            return None
    return None

def enumerate_disp(disp, label):
    print("\n"+"="*84)
    print(f"{label} :: ALIGNED-verified refs to [reg + 0x{disp:X}]")
    print("="*84)
    seen=set(); results=[]
    for pos in candidates_for_disp(disp):
        res = aligned_instr_at_disp(disp, pos)
        if res is None: continue
        r,isw,txt,h = res
        if r in seen: continue
        seen.add(r)
        results.append((r,isw,txt,h))
    for r,isw,txt,h in sorted(results):
        tag = "WRITE" if isw else "read "
        print(f"  0x{r:X} [{tag}] fn=0x{h if h else 0:X}  {txt}")
    print(f"  ({len(results)} aligned-verified sites)")
    return results

r1c0 = enumerate_disp(0x1C0, "Q1 +0x1C0")
r360 = enumerate_disp(0x360, "Q1 +0x360")
r15c = enumerate_disp(0x15C, "Q2 +0x15C")
r344 = enumerate_disp(0x344, "Q3 +0x344")

# For each +0x1C0 site, show a short aligned window for context classification
print("\n"+"="*84)
print("Q1 :: context windows for each aligned +0x1C0 site")
print("="*84)
for r,isw,txt,h in sorted(r1c0):
    print(f"\n--- site 0x{r:X} (fn 0x{h:X}) [{'W' if isw else 'R'}] ---")
    print(disasm_win(r, 0x14, 0x30, {"+ 0x1c0":"1C0","+ 0x360":"360","+ 0x15c":"15C"}))

print("\n"+"="*84)
print("Q1 :: context windows for each aligned +0x360 site")
print("="*84)
for r,isw,txt,h in sorted(r360):
    print(f"\n--- site 0x{r:X} (fn 0x{h:X}) [{'W' if isw else 'R'}] ---")
    print(disasm_win(r, 0x14, 0x30, {"+ 0x360":"360","+ 0x1c0":"1C0"}))

print("\n"+"="*84)
print("Q2 :: context windows for each aligned +0x15C site (looking for WRITES of bit2)")
print("="*84)
for r,isw,txt,h in sorted(r15c):
    print(f"\n--- site 0x{r:X} (fn 0x{h:X}) [{'W' if isw else 'R'}] ---")
    print(disasm_win(r, 0x18, 0x34, {"+ 0x15c":"15C","or ":"OR-set","and ":"AND-clr","test":"test"}))

print("\nDONE.")
