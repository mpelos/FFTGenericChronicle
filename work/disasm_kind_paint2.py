#!/usr/bin/env python3
"""Follow-up: clean-window disasm of suspect readers + selector caller context.
Establish who reads +0x1C0 into cl for the selector, and confirm which of the 9
byte-scan hits are REAL instructions vs mid-stream false positives.
"""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    def off(rva): return pe.get_offset_from_rva(rva)

    def dump(start, length, note=""):
        print(f"\n--- disasm from 0x{start:X} ({note}) ---")
        b = data[off(start): off(start) + length]
        for ins in md.disasm(b, base + start):
            r = ins.address - base
            t = f"{ins.mnemonic} {ins.op_str}".strip()
            low = t.lower()
            mk = ""
            if "0x1c0" in low: mk = "  <== +1C0"
            elif "0x1be" in low: mk = "  <== +1BE"
            elif "0x1c4" in low: mk = "  <== +1C4"
            elif "0x1e5" in low: mk = "  <== +1E5"
            elif "0x205210" in low: mk = "  <== SELECTOR"
            elif ins.mnemonic == "call": mk = "  (call)"
            print(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")

    # Suspect readers -- disasm from a slightly earlier aligned point.
    # 0x2061C3 : preceded by 'sub al,0x38' at 2061B3 -> likely mid-stream. Align earlier.
    dump(0x2061A0, 0x40, "reader 0x2061C3 context")
    # 0x266DE1/0x266E10 : jump table area? align from 266DE0-ish predecessor
    dump(0x266DA0, 0x80, "readers 0x266DE1/0x266E10 context")

    # The three selector-caller readers: 0x26A67D, 0x26A7AB, 0x26A8F1 are the
    # 'mov cl,[reg+0x1c0]; call 0x205210' pattern. Confirm proper alignment by
    # disasm from the function start-ish before each.
    dump(0x26A650, 0x40, "selector caller A (0x26A67D->0x26A683 call)")
    dump(0x26A780, 0x40, "selector caller B (0x26A7AB->0x26A7B1 call)")
    dump(0x26A8C0, 0x80, "selector caller C (0x26A8F1->0x26A92F call)")

    # reader 0x1FAB3F -- the big switch on +0x1C0 (0..6). Show its call context:
    # who calls the function containing 0x1FAB3F? And does it feed a string/anim?
    dump(0x1FAB00, 0xC0, "reader 0x1FAB3F switch + downstream")

    # reader 0x26A4E5 -- switch on (+1C0)-2 in {0,1} or ==7 ; context
    dump(0x26A4C0, 0x40, "reader 0x26A4E5 context")

    # reader 0x26A98B -- switch on +1C0 (0..7) after the selector call at 0x26A92F.
    # Show what it does with the kind (anim/message dispatch?).
    dump(0x26A980, 0xE0, "reader 0x26A98B post-selector kind switch")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
