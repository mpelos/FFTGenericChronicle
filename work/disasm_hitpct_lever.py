#!/usr/bin/env python3
"""Correctly resolve rip-relative WRITERS/READERS of the true chance-source globals:
   0x1407B079D (magic/Faith) and 0x1407B07AC (Blind/status chance).
Uses capstone operand resolution (not raw displacement)."""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
BASE = 0x140000000
REAL_MAX = 0x610000
TARGETS = {0x7B079D: "magic/Faith chance", 0x7B07AC: "Blind/status chance"}


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(rva): return pe.get_offset_from_rva(rva)
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]

    refs = {t: [] for t in TARGETS}
    for va, praw, sz in exsecs:
        blob = data[praw: praw+sz]
        i = 0
        # linear-ish but disasm each valid start; to be robust do sliding disasm
        for i in range(len(blob)-1):
            rva = va+i
            if rva >= REAL_MAX: break
            ins = next(md.disasm(blob[i:i+15], BASE+rva), None)
            if ins is None or not ins.operands: continue
            for opidx, op in enumerate(ins.operands):
                if op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP and op.mem.index == 0:
                    tgt = (rva + ins.size + op.mem.disp) & 0xFFFFFFFF
                    if tgt in TARGETS:
                        w = (opidx == 0 and ins.mnemonic not in ("lea","cmp","test"))
                        refs[tgt].append((rva, ins.size, "W" if w else "r", f"{ins.mnemonic} {ins.op_str}"))

    def window(rva, before, after):
        b = data[off(rva-before): off(rva)+after]
        for ins in md.disasm(b, BASE+(rva-before)):
            r = ins.address - BASE
            if r > rva+after: break
            mk = " <==" if r == rva else ""
            print(f"      {r:08X}: {hb(ins.bytes):<22} {ins.mnemonic} {ins.op_str}{mk}")

    for t, name in TARGETS.items():
        # dedup by rva keeping the longest/first
        seen = {}
        for rva, sz, k, txt in refs[t]:
            if rva not in seen:
                seen[rva] = (k, txt)
        print("=" * 72)
        print(f"0x{BASE+t:X} ({name}) — {len(seen)} unique-rva refs")
        print("=" * 72)
        for rva in sorted(seen):
            k, txt = seen[rva]
            print(f"\n  0x{rva:X} [{k}] {txt}")
            window(rva, 0x1E, 6)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
