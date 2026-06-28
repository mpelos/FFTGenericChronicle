#!/usr/bin/env python3
"""Find REAL-code refs (read/write) to the proven displayed-hit% address 0x1407832C0.

The differential memory scan (mem_scan.py) located the on-screen attack hit% at
static address 0x1407832C0 (RVA 0x7832C0) plus 3 heap mirrors. We can read AND
write it externally, but writing while the panel is already drawn does NOT refresh
the text (retained-mode UI: it redraws only when the panel is dirtied, and dirtying
recomputes the value). To control the DISPLAY deterministically we must know who
writes 0x7832C0 (to hook and substitute our value at compute time) and who reads it
(the render path).

The previous preview scan windowed 0x7832E6..0x783400 and MISSED 0x7832C0 (it sits
just below). Here we scan a window centered on 0x7832C0 and classify each ref.

REAL code = RVA < 0x610000 (image base 0x140000000). Refs from above are Denuvo VM.
"""
from __future__ import annotations

from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_OP_REG, X86_OP_IMM, X86_REG_RIP

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]
REAL_MAX = 0x610000
IMAGE_BASE = 0x140000000

# Window around the proven hit% address. 0x7832C0 is the value; widen to the whole
# forecast scratch block so we see the base-pointer lea + every neighbouring field.
TGT_LO = 0x782000
TGT_HI = 0x784000
HITPCT = 0x7832C0


def hb(b: bytes) -> str:
    return " ".join(f"{x:02X}" for x in b)


def load():
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True
    exsecs = []
    for sec in pe.sections:
        if sec.Characteristics & 0x20000000:  # IMAGE_SCN_MEM_EXECUTE
            exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))
    return exe, pe, data, md, exsecs


def off(pe, rva):
    return pe.get_offset_from_rva(rva)


def main() -> int:
    exe, pe, data, md, exsecs = load()
    rsec = [(va, praw, sz) for (va, praw, sz) in exsecs if va < REAL_MAX]
    print(f"exe={exe}")
    print(f"image_base=0x{IMAGE_BASE:X}  hit%@0x{HITPCT:X}  window 0x{TGT_LO:X}..0x{TGT_HI:X}\n")

    # REX prefixes 0x40-0x4F plus common opcode/prefix starts for RIP-relative forms.
    rip_op_starts = set(range(0x40, 0x50)) | {0x8B, 0x89, 0x0F, 0x66, 0xC7, 0x88, 0x8A, 0xC6,
                                              0xFE, 0xFF, 0x00, 0x01, 0x03, 0x2B, 0x3B, 0x38, 0x39, 0xF3}
    refs = []  # (rva, tgt, is_write, text)
    seen = set()
    for va, praw, sz in rsec:
        blob = data[praw: praw + sz]
        for i in range(len(blob) - 1):
            if blob[i] not in rip_op_starts:
                continue
            rva = va + i
            if rva >= REAL_MAX:
                break
            ins = next(md.disasm(blob[i:i + 15], IMAGE_BASE + rva), None)
            if ins is None or not ins.operands:
                continue
            for opidx, op in enumerate(ins.operands):
                if op.type != X86_OP_MEM or op.mem.index != 0:
                    continue
                if op.mem.base == X86_REG_RIP:
                    tgt = (rva + ins.size + op.mem.disp) & 0xFFFFFFFFFFFF
                elif op.mem.base == 0:
                    tgt = op.mem.disp & 0xFFFFFFFFFFFF  # absolute disp32
                else:
                    continue
                if TGT_LO <= tgt < TGT_HI and rva not in seen:
                    seen.add(rva)
                    # write if mem operand is the destination (operand 0) for a store mnemonic
                    is_write = (opidx == 0 and ins.mnemonic not in ("lea", "cmp", "test"))
                    refs.append((rva, tgt, is_write, f"{ins.mnemonic} {ins.op_str}", ins.size))
    refs.sort()
    print(f"== {len(refs)} real-code RIP-relative refs into window ==\n")
    for rva, tgt, is_write, t, _sz in refs:
        kind = "WRITE" if is_write else "read "
        star = "  <<< HIT% (0x7832C0)" if tgt == HITPCT else ""
        print(f"  0x{rva:06X} -> 0x{tgt:X}  [{kind}]  {t}{star}")

    # Context windows around each ref that touches the exact hit% address (writers first)
    targets = [r for r in refs if r[1] == HITPCT] or refs
    print("\n" + "=" * 70)
    print("CONTEXT around refs to the exact hit% address 0x7832C0")
    print("=" * 70)
    for rva, tgt, is_write, t, _sz in targets:
        print(f"\n### 0x{rva:06X}  [{'WRITE' if is_write else 'read'}] -> 0x{tgt:X} :: {t} ###")
        back = 0x28
        fo = off(pe, rva - back)
        win = data[fo: fo + back + 0x20]
        for ins in md.disasm(win, IMAGE_BASE + (rva - back)):
            r = ins.address - IMAGE_BASE
            if r > rva + 0x18:
                break
            mark = " <==" if r == rva else ""
            # resolve call/jmp rel32
            if ins.bytes and ins.bytes[0] in (0xE8, 0xE9):
                rel = int.from_bytes(ins.bytes[1:5], "little", signed=True)
                ct = (r + 5 + rel) & 0xFFFFFFFFFFFF
                mark += f"  -> 0x{ct:X}{' [VM]' if ct > REAL_MAX else ' [real]'}"
            print(f"    {r:06X}: {hb(ins.bytes):<26} {ins.mnemonic} {ins.op_str}{mark}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
