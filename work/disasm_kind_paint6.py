#!/usr/bin/env python3
"""Final: is the +0x1C0 WRITE (fn 0x2055FC) in the COMPUTE chain (<= our hook) or
the PRESENTATION chain (after)? And confirm 0x30A66F apply is called from the
sweep frame and precedes presentation. Map: 0x205AAF & 0x212B82 call 0x2055FC."""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
REAL_MAX = 0x610000


def main():
    pe = pefile.PE(str(EXE), fast_load=True); base = pe.OPTIONAL_HEADER.ImageBase
    data = EXE.read_bytes(); md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(r): return pe.get_offset_from_rva(r)
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]

    def xrefs_to(target):
        hits = []
        for va, praw, sz in exsecs:
            blob = data[praw:praw+sz]; n = len(blob)
            for i in range(n-5):
                rva = va+i
                if rva >= REAL_MAX: break
                if blob[i] in (0xE8, 0xE9):
                    rel = int.from_bytes(blob[i+1:i+5], "little", signed=True)
                    if ((rva+5+rel) & 0xFFFFFFFFFFFFFFFF) == target:
                        hits.append(rva)
        return hits

    def find_func_start(rva, back=0x800):
        for bk in (back, 0x1000, 0x2000):
            b = data[off(rva-bk):off(rva)]; idx = b.rfind(b"\xCC\xCC")
            if idx >= 0:
                return rva-bk+idx+2
        return None

    def dump(s, l, note=""):
        print(f"--- 0x{s:X} ({note}) ---")
        b = data[off(s):off(s)+l]
        for ins in md.disasm(b, base+s):
            r = ins.address-base; t = f"{ins.mnemonic} {ins.op_str}"; mk=""
            if ins.mnemonic=="call": mk="  (call)"
            elif ins.mnemonic=="ret": mk="  (ret)"
            print(f"    {r:08X}: {t}{mk}")
        print()

    # callers of write-func 0x2055FC
    print("=== chain to +0x1C0 WRITE (fn 0x2055FC) ===")
    for c in (0x205AAF, 0x212B82):
        fs = find_func_start(c)
        fss = hex(fs) if fs else "?"
        xr = [hex(x) for x in xrefs_to(fs)][:16] if fs else "n/a"
        print(f"  call@0x{c:X} is inside fn {fss}; xrefs to it: {xr}")
    # 0x212B82's fn:
    fsB = find_func_start(0x212B82)
    print(f"  0x212B82 fn start = {hex(fsB) if fsB else None}")
    if fsB:
        print(f"    xrefs to 0x{fsB:X}: {[hex(x) for x in xrefs_to(fsB)][:16]}")

    # apply 0x30A66F: it had 0 direct E8 xrefs -> reached via jmp/table or is mid-fn.
    # Show its head + who reaches the sweep's compute region. Also confirm the sweep
    # driver fn start and whether apply/presentation are siblings after 0x281F85 loop.
    dump(0x30A660, 0x30, "apply 0x30A66F region")

    # sweep driver fn start (contains 0x281F85 loop + tail we already saw calling 0x282827cc etc)
    fsS = find_func_start(0x281F85, 0x600)
    print(f"sweep-driver fn start ~0x{fsS:X}; xrefs: {[hex(x) for x in xrefs_to(fsS)][:16]}")

    # Does compute fn 0x309A44 or the sweep tail call presentation fn 0x26A704 / builder 0x1FA9E4?
    for tgt, name in ((0x26A704, "presentation 0x26A704"), (0x1FA9E4, "anim-builder 0x1FA9E4"),
                      (0x30A66F, "apply 0x30A66F")):
        xs = xrefs_to(tgt)
        print(f"  xrefs to {name}: {[hex(x) for x in xs][:16]}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
