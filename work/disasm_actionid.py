#!/usr/bin/env python3
"""Offline RE: where does real (non-VM) code read/write ability-id-sized fields?

Targets:
  A. Byte-scan real code (RVA 0x1000..0x610000) for mem operands with disp
     0x142 (actor action id), 0x1A2 (unit action id), 0x1A0/0x1A1 (boundary),
     0x18D (pending timer), 0x1D8 (charge word) -- 16-bit loads/stores and
     movzx forms. Validate each hit by disassembling at the candidate start.
  B. Disasm context windows around known sites:
     finalizers 0x30637E, 0x308D8F, 0x307DC4, 0x309664;
     pre-clamp 0x30A66F; display dispatch 0x228488 (+ branch 0x22802F);
     selector 0x205210.
"""
from __future__ import annotations

import bisect
from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
REAL_MIN = 0x1000
REAL_MAX = 0x610000
IMAGE_BASE = 0x140000000

DISPS = {0x142: "actor+0x142 actionId",
         0x1A2: "unit+0x1A2 actionId",
         0x1A0: "unit+0x1A0 boundary",
         0x18D: "unit+0x18D pendingTimer",
         0x1D8: "unit+0x1D8 charge",
         0x148: "actor+0x148 unitPtr"}

pe = pefile.PE(str(EXE), fast_load=True)
# build rva->raw mapping via sections
secs = []
for s in pe.sections:
    secs.append((s.VirtualAddress, s.VirtualAddress + s.Misc_VirtualSize,
                 s.PointerToRawData, s.SizeOfRawData, s.Name.rstrip(b"\0").decode()))
data = pe.__data__

def rva_to_off(rva):
    for va, vend, raw, rawsz, name in secs:
        if va <= rva < vend:
            off = rva - va
            if off < rawsz:
                return raw + off, name
    return None, None

md = Cs(CS_ARCH_X86, CS_MODE_64)
md.detail = True

def disasm_window(rva, before=0x30, after=0x30, sync_back=True):
    """Disassemble around an RVA; sync by starting a bit earlier and hoping
    alignment recovers (usually does within a few instrs)."""
    start = rva - before
    off, _ = rva_to_off(start)
    if off is None:
        return []
    code = data[off: off + before + after]
    out = []
    for i in md.disasm(code, IMAGE_BASE + start):
        out.append(i)
    return out

# ---------- A: displacement scan ----------
print("=" * 78)
print("A. Real-code accesses of action-id-ish displacements (disp32 forms)")
print("=" * 78)

import re
hits = {}
for disp, label in DISPS.items():
    dispbytes = disp.to_bytes(4, "little")
    found = []
    # scan every section that maps into real-code RVA range
    for va, vend, raw, rawsz, name in secs:
        lo = max(va, REAL_MIN)
        hi = min(vend, REAL_MAX)
        if lo >= hi:
            continue
        blob = data[raw + (lo - va): raw + min(hi - va, rawsz)]
        base_rva = lo
        idx = 0
        while True:
            j = blob.find(dispbytes, idx)
            if j < 0:
                break
            idx = j + 1
            # candidate: modrm byte right before disp32, mod==10
            if j < 2:
                continue
            modrm = blob[j - 1]
            if (modrm >> 6) != 0b10:
                continue
            rm = modrm & 7
            # skip rm=100 (SIB) handled separately: then modrm is at j-2
            # try to find instruction start: opcodes we care about
            # look back up to 4 bytes for opcode
            cands = []
            for back in range(2, 6):
                if j - back < 0:
                    break
                op = blob[j - back: j - 1]
                cands.append((back, op))
            # heuristic: try disasm from a few starts and accept if the insn
            # covers the disp bytes and has a mem operand with our disp
            best = None
            for back in range(2, 8):
                st = j - back
                if st < 0:
                    break
                insns = list(md.disasm(blob[st: j + 12], IMAGE_BASE + base_rva + st, 1))
                if not insns:
                    continue
                ins = insns[0]
                if ins.size < back + 4:
                    continue
                memops = [o for o in ins.operands if o.type == X86_OP_MEM and o.mem.disp == disp]
                if memops and ins.mnemonic not in ("nop",):
                    best = ins
                    break
            if best is not None:
                found.append(best)
    # dedupe by address
    seen = set()
    uniq = []
    for i in found:
        if i.address in seen:
            continue
        seen.add(i.address)
        uniq.append(i)
    hits[disp] = uniq
    print(f"\n--- disp 0x{disp:X} ({label}): {len(uniq)} candidate insns")
    for i in sorted(uniq, key=lambda x: x.address)[:40]:
        rva = i.address - IMAGE_BASE
        print(f"  RVA 0x{rva:06X}  {i.mnemonic:8s} {i.op_str}")

# ---------- B: context windows ----------
print()
print("=" * 78)
print("B. Context windows around known sites")
print("=" * 78)
SITES = {
    0x30637E: "finalizer (magic obj+6 store)",
    0x308D8F: "finalizer (physical obj+6 store)",
    0x307DC4: "finalizer",
    0x309664: "finalizer",
    0x30A66F: "pre-clamp staged debit",
    0x228488: "display number store",
    0x22802F: "display dispatch branch (basic/fire)",
    0x205210: "result/animation selector",
    0x227FEA: "hit% copy loader",
}
for rva, label in sorted(SITES.items()):
    print(f"\n--- 0x{rva:06X} {label}")
    for i in disasm_window(rva, before=0x40, after=0x50):
        mark = ">>" if i.address == IMAGE_BASE + rva else "  "
        print(f" {mark} 0x{i.address - IMAGE_BASE:06X}  {i.mnemonic:8s} {i.op_str}")
