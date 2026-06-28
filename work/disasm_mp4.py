#!/usr/bin/env python3
"""Confirm g_rbp(0x186AF70) == g_rdi(0x186AF68) + 0x1C0 by finding the writer(s)
   of these two cached globals. Scan for `mov [rip+disp]=...` (89 store) and
   `lea`/add patterns that set 0x186AF70 from (0x186AF68)+0x1C0."""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
REAL_MAX = 0x610000
G_RDI = 0x186AF68
G_RBP = 0x186AF70


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

    # Find any RIP-relative instruction whose target == G_RDI or G_RBP (store form 48 89 0D/05 ...).
    # We scan E8-style not needed; we look for 48 89 modrm(rip) disp32 and 48 8B too (loads) -> to find STORES.
    for label, G in (("g_rdi(defender@base)", G_RDI), ("g_rbp(rec@base+0x1C0)", G_RBP)):
        print(f"\n=== writers/refs of {label} = 0x{G:X} ===")
        hits = []
        for va, praw, sz in exsecs:
            blob = data[praw:praw+sz]; n=len(blob)
            for i in range(n-7):
                rva = va+i
                if rva >= REAL_MAX: break
                # REX.W (48) + opcode(89 store / 8B load) + modrm with mod=00 rm=101 (RIP) => 0x0D(rcx)/0x05(rax)/0x15(rdx)/0x1D(rbx)/0x2D(rbp)/0x3D(rdi)/0x35(rsi)/0x25
                if blob[i]==0x48 and blob[i+1] in (0x89,0x8B) and (blob[i+2] & 0xC7)==0x05:
                    disp = int.from_bytes(blob[i+3:i+7],"little",signed=True)
                    tgt = rva + 7 + disp
                    if tgt == G:
                        hits.append((rva, blob[i+1]))
        for rva, opc in hits[:30]:
            ins = next(md.disasm(data[off(rva):off(rva)+8], base+rva), None)
            kind = "STORE" if opc==0x89 else "load"
            print(f"  0x{rva:X}: [{kind}] {ins.mnemonic+' '+ins.op_str if ins else '?'}")
        print(f"  total: {len(hits)}")

    # Dump the writer that sets BOTH (look around first store of G_RDI) to see if G_RBP = G_RDI+0x1C0
    print("\n=== look for the routine that caches these (window around 0x16C200..0x16C260 candidates) ===")
    # Instead, find a store to G_RDI and dump +-0x40 around it
    store_sites = []
    for va, praw, sz in exsecs:
        blob = data[praw:praw+sz]; n=len(blob)
        for i in range(n-7):
            rva = va+i
            if rva >= REAL_MAX: break
            if blob[i]==0x48 and blob[i+1]==0x89 and (blob[i+2]&0xC7)==0x05:
                disp = int.from_bytes(blob[i+3:i+7],"little",signed=True)
                if rva+7+disp == G_RDI:
                    store_sites.append(rva)
    for s in store_sites[:4]:
        print(f"\n  --- store-to-g_rdi @ 0x{s:X}, window ---")
        start = s - 0x30
        blob = data[off(start): off(s)+0x40]
        for ins in md.disasm(blob, base+start):
            r = ins.address-base
            if r > s+0x38: break
            t=f"{ins.mnemonic} {ins.op_str}"
            mark=""
            if "0x1c0" in t.lower(): mark="  <== +0x1C0!"
            print(f"    {r:08X}: {hb(ins.bytes):<22} {t}{mark}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
