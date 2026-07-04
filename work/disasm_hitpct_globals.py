#!/usr/bin/env python3
"""Find real-code WRITERS and READERS of the two RNG chance-source globals:
   0x1404AA179 (Blind/status path) and 0x1404AB995 (magic path).
These are byte scratch globals the VM roll reads as `chance`. If real code writes a
COMPUTED hit% into them before the roll, THAT is the input lever."""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
BASE = 0x140000000
REAL_MAX = 0x610000
TARGETS = {0x4AA179: "Blind/status chance", 0x4AB995: "magic-path chance(Faith?)"}


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(rva): return pe.get_offset_from_rva(rva)
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]

    rip_starts = set(range(0x40, 0x50)) | {0x8B,0x89,0x0F,0x66,0xC7,0x88,0x8A,0xC6,0xFE,0xFF,0x00,0x01,0x03,0x2B,0x3B,0x38,0x39,0xF3,0x84,0x80,0x83}
    refs = {t: [] for t in TARGETS}
    for va, praw, sz in exsecs:
        blob = data[praw: praw+sz]
        for i in range(len(blob)-1):
            if blob[i] not in rip_starts: continue
            rva = va+i
            if rva >= REAL_MAX: break
            ins = next(md.disasm(blob[i:i+15], BASE+rva), None)
            if ins is None or not ins.operands: continue
            for opidx, op in enumerate(ins.operands):
                if op.type != X86_OP_MEM or op.mem.index != 0: continue
                if op.mem.base == X86_REG_RIP:
                    tgt = (rva + ins.size + op.mem.disp) & 0xFFFFFFFF
                else:
                    continue
                if tgt in TARGETS:
                    is_write = (opidx == 0 and ins.mnemonic not in ("lea","cmp","test"))
                    refs[tgt].append((rva, is_write, f"{ins.mnemonic} {ins.op_str}"))

    def window(rva, before, after):
        b = data[off(rva-before): off(rva)+after]
        for ins in md.disasm(b, BASE+(rva-before)):
            r = ins.address - BASE
            if r > rva+after: break
            mk = " <==" if r == rva else ""
            print(f"      {r:08X}: {hb(ins.bytes):<22} {ins.mnemonic} {ins.op_str}{mk}")

    for t, name in TARGETS.items():
        rs = sorted(set(refs[t]))
        print("=" * 72)
        print(f"global 0x{BASE+t:X} ({name}) — {len(rs)} refs")
        print("=" * 72)
        for rva, isw, txt in rs:
            print(f"\n  0x{rva:X} [{'WRITE' if isw else 'read'}] {txt}")
            window(rva, 0x22, 6)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
