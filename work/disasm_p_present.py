#!/usr/bin/env python3
"""Presentation-consumer RE pass. Q1/Q2/Q3.

Q1: who READS record+0x1C0 / +0x360 to drive presentation (popup number vs Miss vs anim).
Q2: who WRITES record+0x15C bit2 (the test ...,4 gate for the 0x205B38 store).
Q3: where the damage-NUMBER popup value comes from + is there a separate "draw Miss vs number" lever.
"""
from __future__ import annotations
import sys
sys.path.insert(0, r"D:/Projects/FFTGenericChronicle/work")
from disasm_q_common import (find_head, callers_of, jmps_to, disasm_win,
                             off, DATA, hb, is_vm_thunk, BASE, REAL_MAX, MD, EXSECS)

def scan_mem_refs(disp, kinds=("read","write","any")):
    """Aligned-ish linear scan: find any instruction whose op_str references
    [reg + disp32] with the given disp. Return (rva, is_write, text, bytes).
    NOTE: linear disasm can desync on data; caller MUST re-verify via find_head+aligned.
    """
    hits=[]
    dstr = f"+ 0x{disp:x}]"
    for va, praw, sz in EXSECS:
        end = min(sz, (REAL_MAX - va)) if va < REAL_MAX else 0
        if end <= 0: continue
        code = DATA[praw:praw+end]
        for ins in MD.disasm(code, BASE+va):
            r = ins.address - BASE
            if r >= REAL_MAX: break
            ops = ins.op_str.lower()
            if dstr in ops:
                first = ops.split(",")[0].strip()
                is_write = dstr in first  # dest operand first for mov-family
                hits.append((r, is_write, f"{ins.mnemonic} {ins.op_str}", hb(ins.bytes)))
    return hits

def aligned_verify(rva, disp):
    """Re-disasm ALIGNED from the function head; return the aligned instruction text
    at rva (or None if desync means rva isn't a real instruction boundary)."""
    h = find_head(rva)
    if h is None: return None, None
    o = off(h)
    end = off(rva) + 16
    seen=None
    for ins in MD.disasm(DATA[o:end], BASE+h):
        r = ins.address - BASE
        if r == rva:
            seen = f"{ins.mnemonic} {ins.op_str}"
            break
        if r > rva:  # rva was mid-instruction => desync / false boundary
            return h, "<<DESYNC: rva not an instruction boundary>>"
    return h, seen

def dump(title):
    print("\n"+"="*84); print(title); print("="*84)

# ---------------------------------------------------------------------------
dump("Q1 :: enumerate ALL real-code refs to [reg+0x1C0] then ALIGNED-verify each")
refs_1c0 = scan_mem_refs(0x1C0)
print(f"raw linear-scan candidates referencing +0x1c0: {len(refs_1c0)}")
verified=[]
for r,isw,txt,by in refs_1c0:
    h, aln = aligned_verify(r, 0x1C0)
    ok = aln is not None and "DESYNC" not in (aln or "") and "0x1c0]" in (aln or "").lower()
    tag = "WRITE" if isw else "read "
    status = "ALIGNED-OK" if ok else "raw-only/desync"
    print(f"  0x{r:X} [{tag}] fn=0x{h if h else 0:X} {status}")
    print(f"         linear: {txt}")
    print(f"         aligned:{aln}")
    if ok: verified.append((r,isw,aln,h))
print(f"\nALIGNED-VERIFIED +0x1c0 sites: {[(hex(r),'W' if w else 'R') for r,w,a,h in verified]}")

dump("Q1 :: same for mirror [reg+0x360]")
refs_360 = scan_mem_refs(0x360)
v360=[]
for r,isw,txt,by in refs_360:
    h, aln = aligned_verify(r, 0x360)
    ok = aln is not None and "DESYNC" not in (aln or "") and "0x360]" in (aln or "").lower()
    if not ok: continue
    tag = "WRITE" if isw else "read "
    print(f"  0x{r:X} [{tag}] fn=0x{h if h else 0:X}  {aln}")
    v360.append((r,isw,aln,h))
print(f"ALIGNED-VERIFIED +0x360 sites: {[(hex(r),'W' if w else 'R') for r,w,a,h in v360]}")

# ---------------------------------------------------------------------------
dump("Q1 :: SELECTOR 0x205210 body (reads it does)")
sh=find_head(0x205210)
print(f"selector fn head 0x{sh:X}; entry 0x205210 delta {0x205210-sh:#x}")
print(disasm_win(0x205210, 0, 0xA0, {"+ 0x1be":"result flag","+ 0x1e5":"effect-kind",
     "+ 0x1c4":"staged dmg","+ 0x1c0":"evade byte","+ 0x1d0":"staged","+ 0x30":"descr+30"}))

dump("Q1 :: the 3 selector callers — full bodies from head")
for c,cf in [(0x26A683,0x26A580),(0x26A7B1,0x26A704),(0x26A92F,0x26A704)]:
    ch=find_head(c)
    print(f"\n--- caller-site 0x{c:X} (fn head 0x{ch:X}) ---")
    print(disasm_win(cf, 0, 0x120, {"+ 0x1c0":"EVADE byte","0x205210":"CALL selector",
         "test al":"use ret","+ 0x1c4":"dmg","+ 0x344":"popup val","+ 0x1e5":"effect"}))

# ---------------------------------------------------------------------------
dump("Q2 :: refs to [reg+0x15C] — writers of bit2 (test ,4 gate) ALIGNED-verified")
refs_15c = scan_mem_refs(0x15C)
for r,isw,txt,by in refs_15c:
    h, aln = aligned_verify(r, 0x15C)
    ok = aln is not None and "DESYNC" not in (aln or "") and "0x15c]" in (aln or "").lower()
    if not ok: continue
    tag = "WRITE" if isw else "read "
    print(f"  0x{r:X} [{tag}] fn=0x{h if h else 0:X}  {aln}")

dump("Q2 :: context around the KNOWN gate at 0x205B2F and store 0x205B38")
print(disasm_win(0x205B38, 0x60, 0x60, {"+ 0x15c":"GATE 15C","+ 0x1c0":"store 1C0",
     "+ 0x360":"mirror 360","602592":"anim timer"}))

# ---------------------------------------------------------------------------
dump("Q3 :: digit render 0x266AE0 head + value source [rdi+0x344]")
dh=find_head(0x266AE0)
print(f"digit render fn head 0x{dh:X}")
print(disasm_win(0x266AE0, 0, 0x60, {"+ 0x344":"POPUP VAL","+ 0x1c0":"evade","+ 0x1c4":"dmg"}))

dump("Q3 :: format dispatch branch 0x22802F -> store 0x228488")
print(disasm_win(0x22802F, 0x10, 0x40, {"228488":"store","+ 6":"[rbp+6]"}))
print("---- store 0x228488 ----")
print(disasm_win(0x228488, 0x8, 0x20, {"7832be":"display buf","78 32":"disp"}))

dump("Q3 :: what refs +0x344 (popup value field) — find writers/readers")
for r,isw,txt,by in scan_mem_refs(0x344):
    h, aln = aligned_verify(r, 0x344)
    ok = aln is not None and "DESYNC" not in (aln or "") and "0x344]" in (aln or "").lower()
    if not ok: continue
    tag="WRITE" if isw else "read "
    print(f"  0x{r:X} [{tag}] fn=0x{h if h else 0:X}  {aln}")

print("\nDONE.")
