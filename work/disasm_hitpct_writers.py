#!/usr/bin/env python3
"""Definitive writer scan for 0x7B079D and 0x7B07AC. For every byte offset, test
ONLY store opcodes (88/C6/89/C7 with optional 66/REX) whose rip-relative target is
one of the two globals, then validate by re-decoding and confirming operand target.
Also report reads by mov/movzx to see who consumes them (context of the roll)."""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
BASE = 0x140000000
REAL_MAX = 0x610000
TARGETS = {0x7B079D: "Faith/magic chance", 0x7B07AC: "Blind/status chance"}
STORE = {0x88, 0xC6, 0x89, 0xC7, 0xFE, 0xFF, 0x00, 0x01, 0x08, 0x09, 0x30, 0x31}


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]

    writers = {t: [] for t in TARGETS}
    readers = {t: [] for t in TARGETS}
    for va, praw, sz in exsecs:
        end = min(va+sz, REAL_MAX)
        if va >= REAL_MAX: continue
        blob = data[praw: praw+(end-va)]
        n = len(blob)
        for i in range(n-2):
            b0 = blob[i]
            # allow prefixes
            j = i
            if b0 == 0x66: j += 1
            if 0x40 <= blob[j] <= 0x4F: j += 1
            opc = blob[j]
            ins = next(md.disasm(blob[i:i+15], BASE+va+i), None)
            if ins is None or not ins.operands: continue
            for opidx, op in enumerate(ins.operands):
                if op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP and op.mem.index == 0:
                    tgt = (ins.address-BASE + ins.size + op.mem.disp) & 0xFFFFFFFF
                    if tgt in TARGETS:
                        is_w = (opidx == 0 and ins.mnemonic not in ("lea","cmp","test"))
                        rec = (ins.address-BASE, f"{ins.mnemonic} {ins.op_str}")
                        if is_w and opc in STORE:
                            writers[tgt].append(rec)
                        elif not is_w:
                            readers[tgt].append(rec)

    for t, name in TARGETS.items():
        print("=" * 60)
        print(f"0x{BASE+t:X} ({name})")
        print("=" * 60)
        ws = sorted(set(writers[t]))
        rs = sorted(set(readers[t]))
        print(f"  candidate WRITES: {len(ws)}")
        for rva, txt in ws:
            print(f"    0x{rva:X}: {txt}")
        print(f"  candidate reads (dedup): {len(rs)}")
        for rva, txt in rs[:8]:
            print(f"    0x{rva:X}: {txt}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
