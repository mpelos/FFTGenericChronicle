#!/usr/bin/env python3
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

    def xrefs_to(target):
        hits = []
        for va, praw, sz in exsecs:
            blob = data[praw: praw + sz]; n = len(blob)
            for i in range(n - 5):
                rva = va + i
                if rva >= REAL_MAX: break
                if blob[i] in (0xE8, 0xE9):
                    rel = int.from_bytes(blob[i+1:i+5], "little", signed=True)
                    dst = (rva + 5 + rel) & 0xFFFFFFFFFFFFFFFF
                    if dst == target:
                        hits.append((rva, "call" if blob[i] == 0xE8 else "jmp"))
        return hits

    def find_func_start(rva, back=0x600):
        b = data[off(rva-back): off(rva)]
        idx = b.rfind(b"\xCC\xCC")
        return (rva - back + idx + 2) if idx >= 0 else None

    def dump(start, length, note=""):
        print(f"\n--- 0x{start:X} ({note}) ---")
        b = data[off(start): off(start) + length]
        for ins in md.disasm(b, base + start):
            r = ins.address - base
            t = f"{ins.mnemonic} {ins.op_str}".strip(); low = t.lower(); mk=""
            if "0x1c0" in low: mk="  <== +1C0"
            elif ins.mnemonic=="call": mk="  (call)"
            elif ins.mnemonic in("ret","int3"): mk=f"  ({ins.mnemonic})"
            print(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")

    # true function start of the +0x1C0 WRITE (0x205B38)
    for back in (0x100,0x200,0x300,0x500,0x800,0xC00):
        fs = find_func_start(0x205B38, back)
        if fs:
            print(f"write 0x205B38: func start (back {hex(back)}) = 0x{fs:X}")
            xs = xrefs_to(fs)
            print(f"   xrefs to 0x{fs:X}: {xs[:20]}")
            dump(fs, 0x30, f"write-func prologue 0x{fs:X}")
            break

    # Caller of reader 0x1FAB3F function (the 0..6 -> anim-code buffer writer).
    for back in (0x100,0x200,0x400,0x800):
        fs = find_func_start(0x1FAB3F, back)
        if fs:
            print(f"\nreader 0x1FAB3F: func start (back {hex(back)}) = 0x{fs:X}")
            xs = xrefs_to(fs)
            print(f"   xrefs to 0x{fs:X}: {xs[:20]}")
            dump(fs, 0x30, f"1FAB3F-func prologue")
            break

    # Caller of reader 0x26A98B function (post-selector kind switch -> message ids 0x18/0x19/0x58/0x5A...)
    for back in (0x200,0x400,0x800,0xC00):
        fs = find_func_start(0x26A98B, back)
        if fs:
            print(f"\nreader 0x26A98B: func start (back {hex(back)}) = 0x{fs:X}")
            xs = xrefs_to(fs)
            print(f"   xrefs to 0x{fs:X}: {xs[:20]}")
            break

    # Selector caller function starts (0x26A683 etc all share callers)
    for tgt in (0x26A67D, 0x26A8F1):
        for back in (0x200,0x400,0x800,0xE00):
            fs = find_func_start(tgt, back)
            if fs:
                xs = xrefs_to(fs)
                print(f"\nselector-caller 0x{tgt:X}: func start 0x{fs:X}; xrefs={xs[:12]}")
                break

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
