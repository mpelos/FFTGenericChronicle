#!/usr/bin/env python3
"""Find the FFT 'chicken' (panic / low-Brave) threshold in real code.

Forcing Brave (+0x2B) to 0 turned every unit into a chicken on its turn -> the
engine honors our live Brave write (good!), but the chicken mechanic is a
SEPARATE Brave check from the reaction roll. We need the threshold T so we can
set Brave = T (just above chicken) to suppress reactions without chickening.

Strategy:
  A) byte-scan for direct struct compares: cmp byte [reg+0x2B], imm8  (80 /7 .. 2B imm)
  B) for every +0x2B read site, dump a forward window and flag small-immediate
     cmp/sub (<= 20) + the conditional branch (the threshold test).
  C) byte-scan for +0x2B WRITES (mov byte [reg+0x2B], r8) - chicken often follows
     a Brave decrement; show context.
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


def hb(b: bytes) -> str:
    return " ".join(f"{x:02X}" for x in b)


def modrm_disp8(mb: int) -> bool:
    return (mb >> 6) == 1 and (mb & 7) not in (4, 5)


def main() -> int:
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    def off(rva):
        return pe.get_offset_from_rva(rva)

    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]

    print(f"exe={exe}\nimage_base=0x{base:X}\n")

    # ---- A) direct struct compares: cmp byte [reg+0x2B], imm8 ----
    print("=== A) cmp byte [reg+0x2B], imm8  (direct Brave threshold compares) ===")
    found_a = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        for i in range(len(blob) - 4):
            rva = va + i
            if rva >= REAL_MAX:
                break
            # 80 /7 modrm disp8 imm8  ; reg-field=7 -> modrm bits 0x38 set, mod=01
            if blob[i] == 0x80 and ((blob[i + 1] >> 3) & 7) == 7 and modrm_disp8(blob[i + 1]) and blob[i + 2] == 0x2B:
                imm = blob[i + 3]
                # disasm cleanly (handle optional REX prefix one back)
                start = rva
                if i > 0 and (blob[i - 1] & 0xF0) == 0x40:
                    start = rva - 1
                ins = next(md.disasm(data[off(start): off(start) + 8], base + start), None)
                txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
                found_a.append((rva, imm, txt))
    for rva, imm, txt in found_a:
        print(f"  0x{rva:X}: {txt}   (imm={imm})")
    print(f"  total: {len(found_a)}")

    # ---- B) forward context from each +0x2B read; flag small-imm cmp/sub + branch ----
    print("\n=== B) +0x2B reads -> nearby small-immediate compare (threshold test) ===")
    brave_reads = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        for i in range(len(blob) - 4):
            rva = va + i
            if rva >= REAL_MAX:
                break
            b0, b1, b2, b3 = blob[i:i + 4]
            if (b0 == 0x0F and b1 in (0xB6, 0xBE) and modrm_disp8(b2) and b3 == 0x2B) or \
               (b0 == 0x8A and modrm_disp8(b1) and b2 == 0x2B):
                brave_reads.append(rva)
    brave_reads = sorted(set(brave_reads))

    for site in brave_reads:
        start = site - 1 if (data[off(site) - 1] & 0xF0) == 0x40 else site
        win = data[off(start): off(start) + 0x30]
        hits = []
        for ins in md.disasm(win, base + start):
            m = ins.mnemonic
            ops = ins.op_str
            # small immediate compare/sub: e.g. cmp eax, 0xa ; sub eax, 5 ; cmp al, 9
            if m in ("cmp", "sub") and ("0x" in ops or ops.rstrip().endswith(tuple("0123456789"))):
                # extract trailing immediate if small
                tok = ops.split(",")[-1].strip()
                try:
                    val = int(tok, 16) if tok.startswith("0x") else int(tok)
                except ValueError:
                    val = None
                if val is not None and 0 < val <= 20:
                    hits.append((ins.address - base, f"{m} {ops}", val))
        if hits:
            print(f"\n  --- read 0x{site:X} ---")
            # reprint the window with marks
            for ins in md.disasm(win, base + start):
                r = ins.address - base
                t = f"{ins.mnemonic} {ins.op_str}"
                mark = ""
                if "0x2b" in t.lower():
                    mark = "  <== BRAVE read"
                elif any(r == h[0] for h in hits):
                    mark = "  <== SMALL-IMM CMP (threshold?)"
                elif ins.mnemonic.startswith("j") and ins.mnemonic != "jmp":
                    mark = "  *branch*"
                print(f"    {r:08X}: {hb(ins.bytes):<20} {t}{mark}")

    # ---- C) +0x2B writes (Brave decrement -> chicken follows) ----
    print("\n=== C) +0x2B WRITES: mov byte [reg+0x2B], r8 (88 /r .. 2B) ===")
    writes = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        for i in range(len(blob) - 3):
            rva = va + i
            if rva >= REAL_MAX:
                break
            if blob[i] == 0x88 and modrm_disp8(blob[i + 1]) and blob[i + 2] == 0x2B:
                writes.append(rva)
    writes = sorted(set(writes))
    for rva in writes:
        start = rva - 1 if (data[off(rva) - 1] & 0xF0) == 0x40 else rva
        ins = next(md.disasm(data[off(start): off(start) + 8], base + start), None)
        txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
        print(f"  0x{rva:X}: {txt}")
    print(f"  total writes: {len(writes)}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
