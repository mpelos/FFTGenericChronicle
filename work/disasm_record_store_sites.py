#!/usr/bin/env python3
"""Exact store-site bytes for the packed evade record fields (+0x44 class, +0x46 shield/acc MAX,
+0x50 magic twin) in the 3 combat-input record builders. Hook = ExecuteFirst at the store
instruction, inject `mov eax, VALUE` so the original store writes our value."""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())

pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64)


def off(rva):
    return pe.get_offset_from_rva(rva)


def hb(b):
    return " ".join(f"{x:02X}" for x in b)


# The 3 builders' packing regions
for name, start, length in [
    ("builder 0x284A80 (accessory record)", 0x284BD0, 0x70),
    ("builder 0x3600DC (main combat record)", 0x3602C0, 0x70),
    ("builder 0x3962F0 (equip-refresh record)", 0x396450, 0x70),
]:
    print(f"\n=== {name} ===")
    b = data[off(start): off(start) + length]
    for ins in md.disasm(b, base + start):
        r = ins.address - base
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        low = t.lower()
        mark = ""
        if ins.mnemonic == "mov" and ("0x44]" in low or "0x46]" in low or "0x50]" in low) and ", ax" in low:
            mark = "  <== PACKED STORE"
        elif "0x4b]" in low:
            mark = "  (class src)"
        elif "0x4a]" in low or "0x49]" in low or "0x4d]" in low or "0x4e]" in low:
            mark = "  (equip evade src)"
        elif ins.mnemonic == "cmova":
            mark = "  (MAX)"
        print(f"  {r:08X}: {hb(ins.bytes):<20} {t}{mark}")
