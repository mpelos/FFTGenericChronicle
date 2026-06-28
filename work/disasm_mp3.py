#!/usr/bin/env python3
"""Resolve the two RIP-relative globals feeding the apply block, and decode the
field layout: is rbp (result-rec ptr) == defender_base + 0x1C0?  That would map
rbp+6/+8/+a/+c <-> defender+0x1C6/0x1C8/0x1CA/0x1CC, reconciling with the
documented staged +0x1C4/+0x1C6.  Also dump fn 0x34E8E2 area (the staged writer)."""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True

    def off(rva): return pe.get_offset_from_rva(rva)

    # Resolve mov r64,[rip+disp32] at the two sites
    for site in (0x30A661, 0x30A668):
        o = off(site)
        b = data[o:o+7]
        # REX.W 48, 8B, modrm(=2D for rbp / 3D for rdi), disp32
        disp = int.from_bytes(b[3:7], "little", signed=True)
        tgt = site + 7 + disp  # RVA of the global qword that holds the pointer
        print(f"  {site:08X}: {hb(b)}  -> global @ RVA 0x{tgt:X} (VA 0x{base+tgt:X})")

    # The two globals are adjacent? 0x1560908 vs 0x15608f9 -> sites differ by 7,
    # disp differ by (0x1560908-0x15608f9)=0xF ... compute both global RVAs:
    g_rbp = 0x30A661 + 7 + 0x1560908
    g_rdi = 0x30A668 + 7 + 0x15608F9
    print(f"\n  g_rbp(result-rec ptr) global RVA = 0x{g_rbp:X}")
    print(f"  g_rdi(defender ptr)   global RVA = 0x{g_rdi:X}")
    print(f"  same global? {g_rbp == g_rdi}; delta = 0x{abs(g_rbp-g_rdi):X}")

    # Dump 0x34E8C0..0x34E990 : the staged +0x1C4 writer region
    print("\n=== staged-rec writer region 0x34E8C0..0x34E990 ===")
    start = 0x34E8C0
    blob = data[off(start): off(start) + 0xD0]
    for ins in md.disasm(blob, base+start):
        r = ins.address - base
        if r > 0x34E990: break
        t = f"{ins.mnemonic} {ins.op_str}"
        mark = ""
        for d,l in ((0x1c4,"dmg"),(0x1c6,"heal"),(0x1c8,"?"),(0x1ca,"?"),(0x1cc,"?"),(0x1e5,"kind")):
            if f"0x{d:x}]" in t.lower(): mark += f"  <== rec+0x{d:X} {l}"
        print(f"  {r:08X}: {hb(ins.bytes):<24} {t}{mark}")

    # Also dump 0x2050E0..0x205300 (the cluster of word reads of +0x1C4/+0x1C6)
    print("\n=== reader cluster 0x2050E0..0x2052D0 ===")
    start = 0x2050E0
    blob = data[off(start): off(start) + 0x1F0]
    for ins in md.disasm(blob, base+start):
        r = ins.address - base
        if r > 0x2052D0: break
        t = f"{ins.mnemonic} {ins.op_str}"
        mark = ""
        for d,l in ((0x1c4,"dmg"),(0x1c6,"heal"),(0x1c8,"?"),(0x1ca,"?"),(0x1cc,"?"),(0x1e5,"kind")):
            if f"0x{d:x}]" in t.lower(): mark += f"  <== rec+0x{d:X} {l}"
        print(f"  {r:08X}: {hb(ins.bytes):<24} {t}{mark}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
