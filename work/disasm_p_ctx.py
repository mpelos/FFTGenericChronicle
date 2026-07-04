#!/usr/bin/env python3
"""Targeted context for the record-relevant +0x1C0 byte readers, +0x360, +0x15C,
plus the digit-render popup fn 0x2667E0 (Q3) and its use of +0x1c0 vs the number."""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, disasm_win, off, DATA, hb, BASE, REAL_MAX, MD, EXSECS, callers_of, jmps_to)

def sec(t): print("\n"+"="*84); print(t); print("="*84)

MARKS={"+ 0x1c0":"1C0(kind)","+ 0x360":"360(mirror)","+ 0x15c":"15C(gate)",
       "+ 0x1c4":"1C4(dmg)","+ 0x1c6":"1C6(heal)","+ 0x344":"344(popupval)",
       "+ 0x1be":"1BE(flag)","+ 0x1e5":"1E5(effect)","+ 0x1ec":"1EC","0x205210":"CALL-selector",
       "test al":"use-ret","+ 0x1bc":"1BC(tgtidx)"}

sec("Q1 :: readers of record+0x1C0 — context per site")
for site in [0x1FAB3F,0x2061C3,0x266DE1,0x26A4E5]:
    h=find_head(site)
    print(f"\n--- reader 0x{site:X} (fn head 0x{h:X}) ---")
    print(disasm_win(site, 0x16, 0x40, MARKS))

sec("Q1 :: fn 0x2060F4 (contains 0x2061C3 reader) full head — what is this fn?")
print(disasm_win(0x2060F4, 0, 0x90, MARKS))
print("callers of 0x2060F4:", [hex(c) for c in callers_of(0x2060F4)])

sec("Q1 :: fn 0x1FA9E4 (contains 0x1FAB3F reader) — what is this fn?")
print(disasm_win(0x1FA9E4, 0, 0x40, MARKS))
print("callers of 0x1FA9E4:", [hex(c) for c in callers_of(0x1FA9E4)])

sec("Q1 :: fn 0x26A4B8 (contains 0x26A4E5 reader)")
print(disasm_win(0x26A4B8, 0, 0x60, MARKS))
print("callers of 0x26A4B8:", [hex(c) for c in callers_of(0x26A4B8)])

# --- +0x360 aligned refs (re-run the enumeration inline, byte candidates) ---
def cands(disp):
    d=disp.to_bytes(4,'little'); out=[]
    for va,praw,sz in EXSECS:
        end=min(sz,(REAL_MAX-va)) if va<REAL_MAX else 0
        blob=DATA[praw:praw+end]; i=0
        while True:
            j=blob.find(d,i)
            if j<0: break
            out.append(va+j); i=j+1
    return out
def aln(disp,pos):
    approx=pos-8; h=find_head(approx) or find_head(pos)
    if h is None: return None
    o=off(h); end=off(pos)+16
    if end-o>0x8000:
        h=pos-0x40; o=off(h); end=off(pos)+16
    tgt=f"0x{disp:x}]"
    for ins in MD.disasm(DATA[o:end],BASE+h):
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

sec("Q1 :: +0x360 mirror — BYTE-sized aligned refs off record-like registers")
seen=set()
for pos in cands(0x360):
    res=aln(0x360,pos)
    if not res: continue
    r,isw,txt,h=res
    if r in seen: continue
    seen.add(r)
    tl=txt.lower()
    if "byte" in tl and ("rsp" not in tl and "rbp" not in tl):
        print(f"  0x{r:X} [{'W' if isw else 'R'}] fn=0x{h if h else 0:X}  {txt}")
        print(disasm_win(r,0x10,0x24,MARKS))

sec("Q2 :: +0x15C — ALL aligned refs; hunt WRITES (or/and/mov byte) that set bit2")
seen=set()
for pos in cands(0x15C):
    res=aln(0x15C,pos)
    if not res: continue
    r,isw,txt,h=res
    if r in seen: continue
    seen.add(r)
    tl=txt.lower()
    if "rsp" in tl or "rbp" in tl: continue
    marker = "  <<< WRITE" if isw else ""
    print(f"  0x{r:X} [{'W' if isw else 'R'}] fn=0x{h if h else 0:X}  {txt}{marker}")

sec("Q2 :: context around each +0x15C WRITE candidate (bit-set detection)")
seen=set()
for pos in cands(0x15C):
    res=aln(0x15C,pos)
    if not res: continue
    r,isw,txt,h=res
    if r in seen: continue
    seen.add(r)
    tl=txt.lower()
    if "rsp" in tl or "rbp" in tl: continue
    if isw or "or " in tl or "and " in tl or "test" in tl:
        print(f"\n--- 0x{r:X} (fn 0x{h:X}) {txt} ---")
        print(disasm_win(r,0x1c,0x30,MARKS))

# --- Q3: digit render fn 0x2667E0: where +0x1c0 gates the number ---
sec("Q3 :: digit-render fn 0x2667E0 — how +0x1c0 (0x266DE1/0x266E10) gates number vs Miss")
print(disasm_win(0x266DC0, 0, 0xB0, MARKS))

sec("Q3 :: +0x344 popup value — writers/readers aligned")
seen=set()
for pos in cands(0x344):
    res=aln(0x344,pos)
    if not res: continue
    r,isw,txt,h=res
    if r in seen: continue
    seen.add(r)
    tl=txt.lower()
    if "rsp" in tl or "rbp" in tl: continue
    print(f"  0x{r:X} [{'W' if isw else 'R'}] fn=0x{h if h else 0:X}  {txt}")

print("\nDONE.")
