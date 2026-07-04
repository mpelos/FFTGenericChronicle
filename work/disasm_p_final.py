#!/usr/bin/env python3
"""Final pieces: Q3 kind->glyph mapping in digit-render, +0x344 write source,
[rbp+6] source for the number store, and the 0x266E10 branch tail."""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, disasm_win, off, DATA, hb, BASE, REAL_MAX, MD, EXSECS, callers_of, jmps_to)

def sec(t): print("\n"+"="*84); print(t); print("="*84)
MARKS={"+ 0x1c0":"1C0","+ 0x344":"344(val)","+ 0x342":"342","+ 0x1d8":"1D8bits",
       "+ 0x1c4":"1C4dmg","+ 0x1c6":"1C6heal","0x266ae0":"digitrender","+ 6]":"[rbp+6]"}

sec("Q3 :: digit-render 0x266E10 branch tail — kind byte -> glyph id (Miss/evade strings)")
print(disasm_win(0x266E10, 0, 0xB0, MARKS))

sec("Q3 :: how digit-render 0x266AE0 reaches +0x344 (the number) — find the +0x344 read/[rdi+0x344]")
# scan the whole digit-render fn body for +0x344 and 0x266AE0's number path
h=0x2667E0
o=off(h); end=o+0x18C0
for ins in MD.disasm(DATA[o:o+0x18C0], BASE+h):
    r=ins.address-BASE
    if r>=REAL_MAX: break
    ops=ins.op_str.lower()
    if "0x344]" in ops or "0x342]" in ops:
        print(f"    {r:08X}: {hb(ins.bytes):<26} {ins.mnemonic} {ins.op_str}")

sec("Q3 :: writers of +0x344 (who fills the popup number)")
def cands(disp):
    d=disp.to_bytes(4,'little'); out=[]
    for va,praw,sz in EXSECS:
        e=min(sz,(REAL_MAX-va)) if va<REAL_MAX else 0
        blob=DATA[praw:praw+e]; i=0
        while True:
            j=blob.find(d,i)
            if j<0:break
            out.append(va+j); i=j+1
    return out
def aln(disp,pos):
    approx=pos-8; hh=find_head(approx) or find_head(pos)
    if hh is None: return None
    o=off(hh); e=off(pos)+16
    if e-o>0x8000: hh=pos-0x40; o=off(hh); e=off(pos)+16
    tgt=f"0x{disp:x}]"
    for ins in MD.disasm(DATA[o:e],BASE+hh):
        r=ins.address-BASE
        if r<=pos<r+ins.size:
            ops=ins.op_str.lower()
            if tgt in ops:
                first=ops.split(",")[0].strip()
                isw=(tgt in first and "ptr" in first)
                return (r,isw,f"{ins.mnemonic} {ins.op_str}",find_head(r))
            return None
        if r>pos: return None
    return None
seen=set()
for pos in cands(0x344):
    res=aln(0x344,pos)
    if not res: continue
    r,isw,txt,hh=res
    if r in seen: continue
    seen.add(r)
    tl=txt.lower()
    if "rsp" in tl or "rbp" in tl: continue
    print(f"  0x{r:X} [{'W' if isw else 'R'}] fn=0x{hh if hh else 0:X}  {txt}")

sec("Q3 :: number-store fn 0x2060F4 — what is [rbp+6] source (the 0x22802F branch reads it)?")
print("  (fn 0x2060F4 builds a struct: [rbp-1c]=0, [rbp-18]=byte8, [rbp+0]=word[+1c4],")
print("   [rbp+2]=word[+1c6], [rbp+4]=word[+1c5]. So the DISPLAY [rbp+6] in fn 0x227xxx is")
print("   a DIFFERENT rbp frame. Let me find who writes the popup value mirror 0x3740200 / +0x344.")
# find writers to abs 0x3740200 (rip-relative) — the popup value mirror
sec("Q3 :: fn containing 0x22802F format dispatch — head + what feeds [rbp+6]")
fh=find_head(0x22802F)
print(f"format-dispatch fn head 0x{fh:X}")
print(disasm_win(fh, 0, 0x70, {"+ 6]":"[rbp+6]","+ 0x148":"rec","228488":"store"}))

sec("Q1 :: does presentation read +0x1c0 at DRAW time? callers of digit-render 0x266AE0 and selector-fn 0x204FF0")
print("callers of digit-render fn 0x2667E0:", [hex(c) for c in callers_of(0x2667E0)])
print("callers of digit-render entry 0x266AE0:", [hex(c) for c in callers_of(0x266AE0)])
print("callers of selector fn 0x204FF0:", [hex(c) for c in callers_of(0x204FF0)])
print("callers of popup-region fn 0x26A580:", [hex(c) for c in callers_of(0x26A580)])
print("callers of popup-region fn 0x26A704:", [hex(c) for c in callers_of(0x26A704)])
print("callers of fn 0x2060F4 (num builder):", [hex(c) for c in callers_of(0x2060F4)])
print("callers of fn 0x2055FC (0x1c0 writer):", [hex(c) for c in callers_of(0x2055FC)])

print("\nDONE.")
