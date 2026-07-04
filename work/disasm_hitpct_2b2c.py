#!/usr/bin/env python3
"""Reconcile forecast-object +0x2B/+0x2C with unit+0x1E9/+0x1EA and find the
staging pointer. Show every writer of word [reg+0x2c] and byte [reg+0x2b], and
who sets the base pointer used by the RNG sites (0x155f0xx globals)."""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
REAL_MAX = 0x610000
BASE = 0x140000000


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(rva): return pe.get_offset_from_rva(rva)
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]

    def window(rva, before, after, marks=None):
        marks = marks or {}
        b = data[off(rva - before): off(rva) + after]
        out = []
        for ins in md.disasm(b, BASE + (rva - before)):
            r = ins.address - BASE
            if r > rva + after: break
            t = f"{ins.mnemonic} {ins.op_str}".strip(); low = t.lower(); mk = ""
            for needle, label in marks.items():
                if needle in low: mk = "  " + label; break
            out.append(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")
        return out

    print(f"exe={EXE}\n")

    # word writes to [reg+0x2c]:  66 [rex] 89 /r modrm(mod=01 disp8=2C)
    print("=" * 72)
    print("(A) writers of WORD [reg+0x2C]  (== unit+0x1EA if base=unit+0x1BE)")
    print("=" * 72)
    hits = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw+sz]; n = len(blob)
        for i in range(n-6):
            rva = va+i
            if rva >= REAL_MAX: break
            if blob[i] != 0x66: continue
            j = i+1
            if 0x40 <= blob[j] <= 0x4F: j += 1
            if blob[j] != 0x89: continue
            modrm = blob[j+1]; mod = modrm >> 6; rm = modrm & 7
            if mod == 0x01 and rm != 4 and blob[j+2] == 0x2C:
                hits.append(rva)
    hits = sorted(set(hits))
    print(f"  word-write [reg+0x2C] sites: {len(hits)} -> {[hex(x) for x in hits]}")
    for h in hits:
        print(f"\n  --- 0x{h:X} ---")
        for line in window(h, 0x18, 6, marks={"0x2c]": "<== WRITE +2C", "0x64": "(=100)", "0x278ee0": "RNG"}):
            print(line)

    # byte writes to [reg+0x2b]
    print("\n" + "=" * 72)
    print("(B) writers of BYTE [reg+0x2B]  (== unit+0x1E9 if base=unit+0x1BE)")
    print("=" * 72)
    bhits = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw+sz]; n = len(blob)
        for i in range(n-5):
            rva = va+i
            if rva >= REAL_MAX: break
            j = i
            if 0x40 <= blob[j] <= 0x4F: j += 1
            op = blob[j]
            if op in (0x88, 0xC6):
                modrm = blob[j+1]; mod = modrm >> 6; rm = modrm & 7; reg = (modrm>>3)&7
                if op == 0xC6 and reg != 0: continue
                if mod == 0x01 and rm != 4 and blob[j+2] == 0x2B:
                    bhits.append(rva)
    bhits = sorted(set(bhits))
    print(f"  byte-write [reg+0x2B] sites: {len(bhits)}")
    for h in bhits[:20]:
        ins = next(md.disasm(data[off(h):off(h)+8], BASE+h), None)
        print(f"    0x{h:X}: {ins.mnemonic} {ins.op_str}" if ins else f"    0x{h:X}: ?")

    # who sets the forecast base ptr (global 0x1560CXX region loaded before RNG)
    # trace 0x306636 site: rax base -> where set? show wider window
    print("\n" + "=" * 72)
    print("(C) 0x306636 site wider — how the base ptr (rax) is established")
    print("=" * 72)
    for line in window(0x306636, 0x80, 6, marks={
        "0x2c]": "<== +2C", "0x2b]": "<== +2B", "0x1be": "<== +1BE",
        "0x1a8": "(+1A8)", "0x278ee0": "RNG"}):
        print(line)

    # 0x3083AB site wider — rbx base is the forecast obj
    print("\n" + "=" * 72)
    print("(D) 0x3083AB site wider — rbx base (physical?) how established")
    print("=" * 72)
    for line in window(0x3083AB, 0x90, 6, marks={
        "0x2c]": "<== +2C", "0x2b]": "<== +2B", "0x1be": "<== +1BE",
        "0x278ee0": "RNG"}):
        print(line)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
