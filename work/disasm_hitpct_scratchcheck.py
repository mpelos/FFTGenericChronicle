#!/usr/bin/env python3
"""Characterize the chance-source globals: which section, initialized value, and ANY
real-code reference by brute rip-relative scan (all opcodes). Also check 0x1407B079D
(known Faith global) refs, and dump the staging-object globals 0x14186AF60..AF80."""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
BASE = 0x140000000
REAL_MAX = 0x610000
CHECK = [0x4AA179, 0x4AB995, 0x7B079D, 0x186AF60, 0x186AF68, 0x186AF70, 0x186AF78]


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(rva): return pe.get_offset_from_rva(rva)

    print("Section map + raw bytes at each target:")
    for rva in CHECK:
        sec = None
        for s in pe.sections:
            if s.VirtualAddress <= rva < s.VirtualAddress + max(s.Misc_VirtualSize, s.SizeOfRawData):
                sec = s.Name.rstrip(b"\x00").decode(errors="replace"); break
        try:
            raw = data[off(rva): off(rva)+8]
            raws = " ".join(f"{b:02X}" for b in raw)
        except Exception:
            raws = "(uninit/bss)"
        print(f"  0x{BASE+rva:X} RVA 0x{rva:06X}  sec={sec:<8} bytes={raws}")

    # brute scan ALL rip-relative refs to these targets
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]
    tset = set(CHECK)
    hits = {t: [] for t in CHECK}
    for va, praw, sz in exsecs:
        blob = data[praw: praw+sz]
        for i in range(len(blob)-1):
            rva = va+i
            if rva >= REAL_MAX: break
            ins = next(md.disasm(blob[i:i+15], BASE+rva), None)
            if ins is None or not ins.operands: continue
            for opidx, op in enumerate(ins.operands):
                if op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP and op.mem.index == 0:
                    tgt = (rva + ins.size + op.mem.disp) & 0xFFFFFFFF
                    if tgt in tset:
                        w = (opidx == 0 and ins.mnemonic not in ("lea","cmp","test"))
                        hits[tgt].append((rva, "W" if w else "r", f"{ins.mnemonic} {ins.op_str}"))
    print("\nAll rip-relative refs (any opcode):")
    for t in CHECK:
        print(f"\n  0x{BASE+t:X}: {len(hits[t])} refs")
        for rva, k, txt in sorted(set(hits[t])):
            print(f"    0x{rva:X} [{k}] {txt}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
