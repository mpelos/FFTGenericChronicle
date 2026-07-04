#!/usr/bin/env python3
"""Trace: the sole +0x1C0 WRITE at 0x205B38 -- who reaches it, and is it in the
compute path 0x309A44 (before our hook) or elsewhere. Also dump compute fn 0x309A44
to see if it (or its callees) write +0x1C0, and dump the write's function head."""
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

    def find_func_start(rva, back=0x400):
        """Heuristic: walk back to a 'int3;int3' pad or a std prologue."""
        b = data[off(rva-back): off(rva)]
        # find last occurrence of CC CC (int3 pad) before rva
        idx = b.rfind(b"\xCC\xCC")
        if idx >= 0:
            return rva - back + idx + 2
        return rva - back

    def dump(start, length, note=""):
        print(f"\n--- 0x{start:X} ({note}) ---")
        b = data[off(start): off(start) + length]
        for ins in md.disasm(b, base + start):
            r = ins.address - base
            t = f"{ins.mnemonic} {ins.op_str}".strip(); low = t.lower(); mk = ""
            if "0x1c0" in low: mk = "  <== +1C0"
            elif ins.mnemonic == "call":
                mk = "  (call)"
            elif ins.mnemonic == "ret": mk = "  (ret)"
            print(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")

    # 1) function containing the write 0x205B38
    fs = find_func_start(0x205B38)
    print(f"[write 0x205B38] heuristic func start ~0x{fs:X}")
    dump(fs, (0x205B60 - fs), "func containing +1C0 WRITE (0x205B38)")
    # who calls this function?
    xs = xrefs_to(fs)
    print(f"\n  xrefs to func-start 0x{fs:X}: {xs}")
    # also try the real prologue near 0x205AF0-0x205B00
    dump(0x205AE0, 0x60, "pre-write window (find prologue/callers)")

    # 2) compute fn 0x309A44: dump head + all its direct call targets, to see if
    #    any callee is the +0x1C0 writer's function or the selector.
    print("\n\n==== COMPUTE FN 0x309A44 (called at sweep 0x281F85) ====")
    dump(0x309A44, 0x120, "compute fn head")

    # 3) Does 0x309A44 (or 0x281xxx driver) transitively call the selector 0x205210
    #    or the write-func? Show all E8 targets inside 0x309A44..+0x300.
    print("\n  direct call targets inside 0x309A44..+0x300:")
    b = data[off(0x309A44): off(0x309A44)+0x300]
    for ins in md.disasm(b, base + 0x309A44):
        if ins.mnemonic == "call":
            for opn in ins.operands:
                if opn.type == 3:
                    tgt = opn.imm
                    print(f"    0x{ins.address-base:X}: call 0x{tgt-base if tgt> base else tgt:X}")
        if ins.address - base - 0x309A44 > 0x2E0:
            break

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
