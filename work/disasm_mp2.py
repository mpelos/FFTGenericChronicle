#!/usr/bin/env python3
"""Focused: clamp block 0x30A66F..0x30A6D3 + who writes rbp/rdi there;
   plus confirm the +0x1C4/+0x1C6/+0x1E5 result-record fields and find the
   record-staging writer (where staged dmg/heal words are SET before apply).
"""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
REAL_MAX = 0x610000


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True

    def off(rva): return pe.get_offset_from_rva(rva)

    exsecs = []
    for sec in pe.sections:
        if sec.Characteristics & 0x20000000:
            exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))

    # 1. Tight disasm of the apply/clamp block with rbp/rdi annotation
    print("=== APPLY/CLAMP block 0x30A661..0x30A6E0 ===")
    start = 0x30A661
    blob = data[off(start): off(start) + (0x30A6E0 - start) + 8]
    for ins in md.disasm(blob, base + start):
        r = ins.address - base
        if r > 0x30A6E0: break
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        note = ""
        low = t.lower()
        if "rbp +" in low: note += "  [rbp=RESULT-REC]"
        if "rdi +" in low or "[rdi]" in low: note += "  [rdi=DEFENDER]"
        print(f"  {r:08X}: {hb(ins.bytes):<26} {t}{note}")

    # 2. Where do rbp and rdi get loaded just before (0x30A661, 0x30A668)?
    print("\n=== loads feeding the block (0x30A661 rbp <- [rip+0x1560908], 0x30A668 rdi <- [rip+0x15608f9]) ===")
    for site in (0x30A661, 0x30A668):
        o = off(site)
        ins = next(md.disasm(data[o:o+8], base+site))
        # compute the RIP-relative global address
        # mov r64, [rip+disp32]; disp at bytes 3..7 for REX+8B+modrm
        print(f"  {site:08X}: {ins.mnemonic} {ins.op_str}")

    # 3. Confirm rec fields +0x1C4/+0x1C6/+0x1E5 are referenced somewhere (byte-scan)
    for disp in (0x1C4, 0x1C6, 0x1E5):
        print(f"\n=== real-code refs to rec+0x{disp:X} ===")
        lo = disp & 0xFF; b2 = (disp >> 8) & 0xFF; b3 = (disp >> 16) & 0xFF; b4 = (disp >> 24) & 0xFF
        hits = []
        for va, praw, sz in exsecs:
            blob = data[praw:praw+sz]; n=len(blob)
            for i in range(n-7):
                rva = va+i
                if rva >= REAL_MAX: break
                # any modrm with disp32 == disp: look for the 4-byte LE sequence preceded by a mod=10 modrm
                if blob[i+3]==lo and blob[i+4]==b2 and blob[i+5]==b3 and blob[i+6]==b4:
                    mb = blob[i+2]
                    if (mb>>6)==0x02 and (mb&7) not in (0x04,0x05):
                        hits.append(rva)
        hits = sorted(set(hits))
        for rva in hits[:40]:
            o=off(rva)
            s = rva-1 if (data[o-1]&0xF0)==0x40 else rva
            s = s-1 if (data[off(s)-1]==0x66 or (data[off(s)-1]&0xF0)==0x40) else s
            ins = next(md.disasm(data[off(s):off(s)+12], base+s), None)
            print(f"  0x{rva:X}: {ins.mnemonic+' '+ins.op_str if ins else '?'}")
        print(f"  total: {len(hits)}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
