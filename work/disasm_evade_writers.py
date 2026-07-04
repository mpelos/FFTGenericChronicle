#!/usr/bin/env python3
"""Byte-scan for REAL-code WRITES to the DEFENDER evade bytes:
   +0x46 +0x47 (weapon parry), +0x4A +0x4E (shield block), +0x4B (class evade).

Encodings (each with optional REX 40..4F prefix):
  mov  [reg+disp8], r8        88 /r, mod=01, disp8 in {46,47,4A,4B,4E}
  mov  byte [reg+disp8], imm8 C6 /0, mod=01, disp8
  (disp32 forms too: mod=10, disp = XX 00 00 00)
  also SIB variants (rm==4).

For each hit: disasm a window, find function head (push rbp / sub rsp / int3 pad),
detect unit-table loop (stride 0x200, cmp against table base 0x141853CE0), and
trace the source register feeding the stored byte.
"""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]
REAL_MAX = 0x610000
UNIT_TABLE = 0x141853CE0
STRIDE = 0x200
TARGET_DISPS = {0x46: "weapon-parry", 0x47: "weapon-parry",
                0x4A: "shield-block", 0x4B: "class-evade", 0x4E: "shield-block"}

def hb(b): return " ".join(f"{x:02X}" for x in b)

def main():
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True

    def off(rva): return pe.get_offset_from_rva(rva)

    exsecs = []
    for sec in pe.sections:
        if sec.Characteristics & 0x20000000:
            exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))

    print(f"exe={exe}\nimage_base=0x{base:X}\nREAL_MAX=0x{REAL_MAX:X}")
    print(f"unit_table=0x{UNIT_TABLE:X} stride=0x{STRIDE:X}\n")

    def disasm_at(start, length=12):
        b = data[off(start): off(start)+length]
        return next(md.disasm(b, base+start), None)

    hits = []  # (rva, disp, kind, rex)
    for va, praw, sz in exsecs:
        blob = data[praw: praw+sz]; n = len(blob)
        for i in range(n-8):
            rva = va + i
            if rva >= REAL_MAX: break
            j = i; rex = None
            if 0x40 <= blob[j] <= 0x4F:
                rex = blob[j]; j += 1
            op = blob[j]
            modrm = blob[j+1] if j+1 < n else 0
            mod = modrm >> 6; rm = modrm & 7
            kind = None; disp_at = None; disp32 = False
            if op == 0x88:  # mov r/m8, r8
                if mod == 1 and rm != 4: disp_at = j+2; kind = "mov [r+d8],r8"
                elif mod == 1 and rm == 4: disp_at = j+3; kind = "mov [sib+d8],r8"
                elif mod == 2 and rm != 4: disp_at = j+2; kind = "mov [r+d32],r8"; disp32 = True
                elif mod == 2 and rm == 4: disp_at = j+3; kind = "mov [sib+d32],r8"; disp32 = True
            elif op == 0xC6:  # mov r/m8, imm8, /0
                if ((modrm>>3)&7) == 0:
                    if mod == 1 and rm != 4: disp_at = j+2; kind = "mov byte [r+d8],imm8"
                    elif mod == 1 and rm == 4: disp_at = j+3; kind = "mov byte [sib+d8],imm8"
                    elif mod == 2 and rm != 4: disp_at = j+2; kind = "mov byte [r+d32],imm8"; disp32 = True
                    elif mod == 2 and rm == 4: disp_at = j+3; kind = "mov byte [sib+d32],imm8"; disp32 = True
            if not kind or disp_at is None or disp_at >= n:
                continue
            if disp32:
                if disp_at+4 > n: continue
                d = blob[disp_at]
                if not (blob[disp_at+1]==0 and blob[disp_at+2]==0 and blob[disp_at+3]==0):
                    continue
            else:
                d = blob[disp_at]
            if d in TARGET_DISPS:
                hits.append((rva, d, kind, rex))

    hits = sorted(set(hits))
    print("="*72)
    print(f"WRITE sites to defender evade bytes ({len(hits)} raw hits):")
    print("="*72)
    by_disp = {}
    for rva, d, kind, rex in hits:
        by_disp.setdefault(d, []).append((rva, kind))
    for d in sorted(TARGET_DISPS):
        lst = by_disp.get(d, [])
        print(f"\n+0x{d:02X} ({TARGET_DISPS[d]}): {len(lst)} site(s)")
        for rva, kind in lst:
            ins = disasm_at(rva)
            txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
            print(f"    0x{rva:X}: {txt}   [{kind}]")

    return hits

if __name__ == "__main__":
    main()
