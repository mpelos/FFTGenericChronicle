#!/usr/bin/env python3
"""Robust (byte-pattern, desync-proof) enumeration of the Brave-gated surface.

Linear capstone sweep of a whole section desyncs around data/jump-tables, so it
misses real instructions (it reported 0 Brave reads even though 0x271D4A clearly
reads +0x2B). Here we byte-scan for the exact encodings, then disasm a clean
window at each hit. We map:
  * every real-code read of +0x2B (Brave):  0F B6 /r d8=2B  (movzx r32)
                                            0F BE /r d8=2B  (movsx r32)
                                            8A /r    d8=2B  (mov r8)
                                            44 8A /r d8=2B  (mov r8b w/ REX.R) etc are covered by 8A scan loosely
  * every real-code call to the VM roll primitive 0x278EE0 (E8 rel32),
    with the ~10 instructions before it (to see the threshold = f(Brave)).
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
ROLL = 0x278EE0


def hb(b: bytes) -> str:
    return " ".join(f"{x:02X}" for x in b)


def modrm_ok_disp8(mod_byte: int) -> bool:
    # mod==01 (disp8) and rm not 100(SIB)/101 so the byte after modrm is the disp8
    mod = mod_byte >> 6
    rm = mod_byte & 0x07
    return mod == 0x01 and rm not in (0x04, 0x05)


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

    # executable section file-range(s)
    exsecs = []
    for sec in pe.sections:
        if sec.Characteristics & 0x20000000:
            exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))

    def rva_to_fileoff(rva):
        return off(rva)

    print(f"exe={exe}\nimage_base=0x{base:X}\n")

    # ---- 1. Brave (+0x2B) reads via byte scan ----
    print("=== REAL-code reads of +0x2B (Brave) [byte-scan] ===")
    brave_hits = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        for i in range(len(blob) - 4):
            rva = va + i
            if rva >= REAL_MAX:
                break
            b0, b1, b2, b3 = blob[i], blob[i + 1], blob[i + 2], blob[i + 3]
            hit = False
            # 0F B6 /r (movzx), 0F BE /r (movsx): need d8==2B at b3, modrm=b2
            if b0 == 0x0F and b1 in (0xB6, 0xBE) and modrm_ok_disp8(b2) and b3 == 0x2B:
                hit = True
            # 8A /r (mov r8, rm8): modrm=b1, d8 at b2
            elif b0 == 0x8A and modrm_ok_disp8(b1) and b2 == 0x2B:
                hit = True
            if hit:
                brave_hits.append(rva)
    # de-dup near-identical (a REX-prefixed variant can double-count by +1)
    brave_hits = sorted(set(brave_hits))
    for rva in brave_hits:
        # back up one for a possible REX prefix to disasm cleanly
        start = rva - 1 if (data[off(rva) - 1] & 0xF0) == 0x40 else rva
        blob = data[off(start): off(start) + 8]
        ins = next(md.disasm(blob, base + start), None)
        txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
        print(f"  0x{rva:X}: {txt}")
    print(f"  total Brave-read sites: {len(brave_hits)}")

    # ---- 2. callers of the VM roll 0x278EE0 (E8 rel32) ----
    print(f"\n=== REAL-code callers of VM roll 0x{ROLL:X} [byte-scan E8 rel32] ===")
    callers = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        for i in range(len(blob) - 5):
            rva = va + i
            if rva >= REAL_MAX:
                break
            if blob[i] != 0xE8:
                continue
            rel = int.from_bytes(blob[i + 1:i + 5], "little", signed=True)
            tgt = (rva + 5 + rel) & 0xFFFFFFFFFFFF
            if tgt == ROLL:
                callers.append(rva)
    for rva in callers:
        print(f"\n  --- call site 0x{rva:X}: preceding context ---")
        win = 0x2A
        blob = data[off(rva - win): off(rva) + 6]
        for ins in md.disasm(blob, base + (rva - win)):
            r = ins.address - base
            if r > rva:
                break
            t = f"{ins.mnemonic} {ins.op_str}".strip()
            low = t.lower()
            mark = ""
            if "0x2b" in low:
                mark = "  <== BRAVE"
            elif "0x64" in low or ", 0x64" in low:
                mark = "  (=100)"
            elif ins.mnemonic in ("sub", "cmp"):
                mark = "  *arith*"
            elif r == rva:
                mark = "  <== CALL ROLL"
            print(f"    {r:08X}: {hb(ins.bytes):<20} {t}{mark}")
    print(f"\n  total roll callers: {len(callers)}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
