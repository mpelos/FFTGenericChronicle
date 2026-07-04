#!/usr/bin/env python3
"""Refine: cluster the evade-byte writers.

A real equipment->struct evade copy writes SEVERAL of 0x46/0x47/0x4A/0x4B/0x4E
(and neighbors 0x48/0x49/0x4C/0x4D) into the SAME base register within a short
window. We:
  1. collect writes to 0x44..0x4F (the whole evade/equip cluster) via [base+d8]
     with NO scaled index (real struct writes; drop rsp-local & scaled-array).
  2. group hits whose RVAs are within 0x60 of each other -> candidate copy loops.
  3. for each group, dump the enclosing function window, detect unit-table stride
     0x200 / base-0x141853CE0 loop, and note the source of each stored byte.
Also classify each group by which base register and whether rsp (stack local).
"""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
REAL_MAX = 0x610000
CLUSTER = set(range(0x44, 0x50))  # 0x44..0x4F
KEY = {0x46,0x47,0x4A,0x4B,0x4E}
REGS = ["rax","rcx","rdx","rbx","rsp","rbp","rsi","rdi",
        "r8","r9","r10","r11","r12","r13","r14","r15"]

def hb(b): return " ".join(f"{x:02X}" for x in b)

def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(rva): return pe.get_offset_from_rva(rva)
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]

    hits = []  # (rva, disp, basereg, kind)
    for va, praw, sz in exsecs:
        blob = data[praw:praw+sz]; n = len(blob)
        for i in range(n-8):
            rva = va+i
            if rva >= REAL_MAX: break
            j = i; rex = 0
            if 0x40 <= blob[j] <= 0x4F:
                rex = blob[j]; j += 1
            op = blob[j]; modrm = blob[j+1] if j+1<n else 0
            mod = modrm>>6; rm = modrm&7
            if op not in (0x88, 0xC6): continue
            if op == 0xC6 and ((modrm>>3)&7) != 0: continue
            if mod != 1: continue        # disp8 only (these disps fit)
            if rm == 4: continue          # skip SIB (scaled arrays / rsp locals)
            if rm == 5: continue          # rip-rel doesn't happen for mod1
            disp = blob[j+2]
            if disp not in CLUSTER: continue
            basereg = REGS[((rex&1)<<3) | rm]
            kind = "imm8" if op==0xC6 else "r8"
            hits.append((rva, disp, basereg, kind))

    hits.sort()
    # group by proximity
    groups = []
    cur = []
    for h in hits:
        if cur and h[0] - cur[-1][0] > 0x60:
            groups.append(cur); cur = []
        cur.append(h)
    if cur: groups.append(cur)

    def has_key(g): return any(d in KEY for _,d,_,_ in g)

    print(f"exe={EXE}\ntotal cluster writes (0x44..0x4F, [base+d8], no-SIB): {len(hits)}")
    print(f"groups: {len(groups)}\n")

    # Rank groups: those touching multiple KEY disps AND a non-rsp base first.
    def score(g):
        disps = {d for _,d,_,_ in g}
        keyn = len(disps & KEY)
        nonstack = any(br != "rsp" for _,_,br,_ in g)
        return (keyn, len(disps), nonstack)

    ranked = sorted([g for g in groups if has_key(g)],
                    key=score, reverse=True)

    for gi, g in enumerate(ranked[:20]):
        disps = sorted({d for _,d,_,_ in g})
        bregs = sorted({br for _,_,br,_ in g})
        start = g[0][0]
        print("="*72)
        print(f"GROUP {gi}: start=0x{start:X}  disps={[hex(x) for x in disps]}  bases={bregs}")
        for rva,d,br,k in g:
            b = data[off(rva-1):off(rva)+8]
            ins = next(md.disasm(b, base+rva-1), None) or next(md.disasm(data[off(rva):off(rva)+8], base+rva), None)
            print(f"    0x{rva:X}: +0x{d:02X} <- {k}  base={br}")
    return ranked

if __name__ == "__main__":
    main()
