#!/usr/bin/env python3
"""Find real-code references to the item-table data region.

1) absolute-disp lea pattern like fn 0x2B8CB8: lea r64,[r*4 + imm32] / [r*8 + imm32]
   (48/4C 8D modrm(mod=00,rm=100) sib(base=101) imm32) with imm32 in table region.
2) rip-relative lea/mov targeting rva IN [lo,hi).
Group by enclosing function (int3-pad walk-back)."""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
REAL_LO, REAL_HI = 0x1000, 0x610000
LO, HI = 0x67F000, 0x830000   # item table neighbourhood

pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)

xoff = off(REAL_LO)
blob = data[xoff: off(REAL_HI)]
hits = []  # (rva, kind, target_rva, text)

# --- pattern 1: 8D /r with SIB base=101 (abs disp32), any scale ---
i = 0
n = len(blob)
while i < n - 8:
    j = i
    if 0x40 <= blob[j] <= 0x4F: j += 1
    if blob[j] == 0x8D:
        modrm = blob[j+1]
        mod, rm = modrm >> 6, modrm & 7
        if mod == 0 and rm == 4:
            sib = blob[j+2]
            if (sib & 7) == 5:  # base=101 -> disp32 absolute
                disp = int.from_bytes(blob[j+3:j+7], "little")
                if LO <= disp < HI:
                    rva = REAL_LO + i
                    ins = next(md.disasm(data[off(rva):off(rva)+10], base+rva), None)
                    if ins and ins.mnemonic == "lea":
                        hits.append((rva, "lea-abs", disp, f"{ins.mnemonic} {ins.op_str}"))
    i += 1

# --- pattern 2: rip-relative operands into [LO,HI) ---
# decode instruction stream loosely: sliding window disasm at each candidate rel32
# cheaper: scan for rel32 values v such that rva+len+disp lands in region, via capstone over whole .xcode
for ins in md.disasm(blob, base + REAL_LO):
    for op in ins.operands:
        if op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP:
            tgt = ins.address + ins.size + op.mem.disp - base
            if LO <= tgt < HI:
                hits.append((ins.address - base, "rip", tgt, f"{ins.mnemonic} {ins.op_str}"))

def fn_head(rva, back=0x1000):
    start = rva - back
    b = data[off(start): off(rva)]
    last = None; i = 0
    while i < len(b):
        if b[i] == 0xCC:
            j = i
            while j < len(b) and b[j] == 0xCC: j += 1
            last = start + j; i = j
        else: i += 1
    return last or start

hits = sorted(set(hits))
print(f"{len(hits)} real-code refs into rva 0x{LO:X}..0x{HI:X}\n")
byfn = {}
for rva, kind, tgt, txt in hits:
    byfn.setdefault(fn_head(rva), []).append((rva, kind, tgt, txt))
for head in sorted(byfn):
    print(f"fn ~0x{head:X}:")
    for rva, kind, tgt, txt in byfn[head]:
        print(f"   0x{rva:X} [{kind}] -> rva 0x{tgt:X} : {txt}")
    print()
