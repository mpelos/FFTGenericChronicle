#!/usr/bin/env python3
"""Verify the REACTION (Blade Grasp / Hamedo / Arrow Guard) Brave%-gated roll.

Offline RE concluded reactions are NOT fully virtualized: the defender's Brave
(+0x2B) is read in REAL code and the success/fail BRANCH is real (only the roll
arithmetic is VM, via primitive 0x278EE0). The candidate reaction-roll fn is
0x271D20 (defender from [actor+0x148] in rcx; Brave read at 0x271D4A = 0F B6 57 2B).

This script confirms:
  1. fn 0x271D20 reads +0x2B (Brave) off the defender and how it derives the threshold.
  2. every REAL-code reader of +0x2B (the "6 Brave-gated roll sites").
  3. the VM roll primitive 0x278EE0 and its real-code callers.
  4. the COMPARISON DIRECTION around each Brave read -> tells us whether Brave=0
     suppresses a reaction (expected: trigger iff roll < Brave, so Brave 0 => never).
"""
from __future__ import annotations

from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_GRP_CALL, X86_GRP_JUMP, X86_OP_IMM

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]

REAL_MAX = 0x610000  # real code below this; Denuvo VM (.edata thunks) above


def hb(data: bytes) -> str:
    return " ".join(f"{b:02X}" for b in data)


def main() -> int:
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found in candidates")
    pe = pefile.PE(str(exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    def off(rva: int) -> int:
        return pe.get_offset_from_rva(rva)

    print(f"exe={exe}")
    print(f"image_base=0x{base:X}\n")

    def is_brave_read(insn) -> bool:
        # movzx/mov r, byte [reg+0x2b]  -> reads the Brave byte
        if insn.mnemonic not in ("movzx", "mov", "movsx"):
            return False
        for op in insn.operands:
            if op.type == 3 and op.mem.disp == 0x2B and op.mem.base != 0:  # X86_OP_MEM=3
                # exclude rip-relative (base==0 handled) and writes (mem must be source)
                return True
        return False

    # --- 1. The candidate reaction-roll fn 0x271D20 ---
    fn = 0x271D20
    win = 0x140
    print(f"=== reaction-roll fn 0x{fn:X}..0x{fn+win:X} ===")
    blob = data[off(fn): off(fn) + win]
    for insn in md.disasm(blob, base + fn):
        rva = insn.address - base
        text = f"{insn.mnemonic} {insn.op_str}".strip()
        low = text.lower()
        mark = ""
        if is_brave_read(insn):
            mark = "  <== BRAVE +0x2B"
        elif "0x148" in low:
            mark = "  <== defender [actor+0x148]"
        elif "0x2a" in low:
            mark = "  (maxbrave +0x2a?)"
        elif insn.mnemonic == "call":
            mark = "  <== CALL"
        elif insn.mnemonic in ("cmp", "test", "sub") or insn.mnemonic.startswith("j"):
            mark = "  *branch/cmp*"
        print(f"{rva:08X}: {hb(insn.bytes):<24} {text}{mark}")

    # --- 2. All REAL-code readers of +0x2B (Brave) ---
    print("\n=== REAL-code readers of +0x2B (Brave), RVA < 0x%X ===" % REAL_MAX)
    brave_sites = []
    for sec in pe.sections:
        if not sec.Characteristics & 0x20000000:  # executable
            continue
        sblob = data[sec.PointerToRawData: sec.PointerToRawData + sec.SizeOfRawData]
        for insn in md.disasm(sblob, base + sec.VirtualAddress):
            rva = insn.address - base
            if rva >= REAL_MAX:
                continue
            if is_brave_read(insn):
                brave_sites.append(rva)
    for rva in brave_sites:
        b = data[off(rva): off(rva) + 8]
        print(f"  0x{rva:X}: {hb(b)}")
    print(f"  total: {len(brave_sites)}")

    # --- 3. Comparison direction around each Brave read (window of 12 insns) ---
    print("\n=== context around each Brave read (direction of the trigger compare) ===")
    for site in brave_sites:
        print(f"\n--- 0x{site:X} ---")
        blob = data[off(site): off(site) + 0x40]
        n = 0
        for insn in md.disasm(blob, base + site):
            rva = insn.address - base
            text = f"{insn.mnemonic} {insn.op_str}".strip()
            low = text.lower()
            mark = ""
            if "0x2b" in low:
                mark = "  <== BRAVE"
            elif insn.mnemonic == "call":
                mark = "  <== CALL (roll?)"
            elif insn.mnemonic in ("cmp", "sub", "test"):
                mark = "  *cmp*"
            elif insn.mnemonic.startswith("j") and insn.mnemonic != "jmp":
                mark = "  *branch*"
            print(f"  {rva:08X}: {hb(insn.bytes):<22} {text}{mark}")
            n += 1
            if n >= 14:
                break

    # --- 4. VM roll primitive 0x278EE0 and its real-code callers ---
    target = 0x278EE0
    print(f"\n=== real-code callers of VM roll primitive 0x{target:X} ===")
    callers = []
    for sec in pe.sections:
        if not sec.Characteristics & 0x20000000:
            continue
        sblob = data[sec.PointerToRawData: sec.PointerToRawData + sec.SizeOfRawData]
        for insn in md.disasm(sblob, base + sec.VirtualAddress):
            if not (insn.group(X86_GRP_CALL) or insn.group(X86_GRP_JUMP)):
                continue
            if not insn.operands or insn.operands[0].type != X86_OP_IMM:
                continue
            tgt = insn.operands[0].imm - base
            rva = insn.address - base
            if tgt == target and rva < REAL_MAX:
                callers.append(rva)
    for rva in callers:
        print(f"  0x{rva:X}")
    print(f"  total: {len(callers)}")

    print(f"\n=== 0x{target:X} first 8 insns (thunk into VM?) ===")
    blob = data[off(target): off(target) + 0x30]
    for i, insn in enumerate(md.disasm(blob, base + target)):
        if i >= 8:
            break
        rva = insn.address - base
        print(f"  {rva:08X}: {hb(insn.bytes):<22} {insn.mnemonic} {insn.op_str}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
