#!/usr/bin/env python3
"""Alignment-respecting LINEAR disasm of the formula module (0x304000..0x320000) to get
an authoritative list of refs to 0x7B079D (Faith) and 0x7B07AC (Blind chance), plus a
scan of the whole real-code range for CLEAN writes to those two globals.
Linear decode from function starts avoids the overlapping-byte false positives."""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
BASE = 0x140000000
REAL_MAX = 0x610000
TARGETS = {0x7B079D: "Faith/magic chance", 0x7B07AC: "Blind/status chance"}


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(rva): return pe.get_offset_from_rva(rva)

    # Linear decode the whole .text-equivalent real code once, recording refs.
    # We decode from each executable section start; capstone auto-resyncs reasonably,
    # and to reduce overlap noise we take a single forward stream per section.
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]
    found = {t: {"W": [], "r": []} for t in TARGETS}
    for va, praw, sz in exsecs:
        end = min(va+sz, REAL_MAX)
        if va >= REAL_MAX: continue
        blob = data[praw: praw + (end-va)]
        for ins in md.disasm(blob, BASE+va):
            if not ins.operands: continue
            for opidx, op in enumerate(ins.operands):
                if op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP and op.mem.index == 0:
                    tgt = (ins.address - BASE + ins.size + op.mem.disp) & 0xFFFFFFFF
                    if tgt in TARGETS:
                        w = (opidx == 0 and ins.mnemonic not in ("lea","cmp","test"))
                        found[tgt]["W" if w else "r"].append(
                            (ins.address-BASE, f"{ins.mnemonic} {ins.op_str}"))

    for t, name in TARGETS.items():
        print("=" * 68)
        print(f"0x{BASE+t:X} ({name})")
        print("=" * 68)
        for kind in ("W", "r"):
            lst = sorted(set(found[t][kind]))
            label = "WRITES" if kind == "W" else "reads"
            print(f"  {label}: {len(lst)}")
            for rva, txt in lst:
                print(f"    0x{rva:X}: {txt}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
