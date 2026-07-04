#!/usr/bin/env python3
"""Disassemble arbitrary functions with rip-relative global annotation.
Usage: python disasm_itemtable_fetchers.py <rva_hex> [max_hex] ...
Default targets: 0x286D04 0x396A18 0x2B8CB8 (item stat fetchers + id->row helper)
"""
from __future__ import annotations
from pathlib import Path
import sys
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_REG_RIP

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())

pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64)
md.detail = True

def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)

SECS = [(s.VirtualAddress, s.VirtualAddress + max(s.Misc_VirtualSize, s.SizeOfRawData),
         s.Name.rstrip(b"\x00").decode(errors="replace"),
         bool(s.Characteristics & 0x80000000),  # writable
         bool(s.Characteristics & 0x20000000))  # exec
        for s in pe.sections]

def sec_of(rva):
    for lo, hi, nm, w, x in SECS:
        if lo <= rva < hi:
            return f"{nm}{'/W' if w else ''}{'/X' if x else ''}"
    return "?"

def read_q(rva):
    try: return int.from_bytes(data[off(rva):off(rva)+8], "little")
    except Exception: return None

def dump(head, max_len=0x900):
    print("=" * 92)
    print(f"FUNCTION 0x{head:X} (VA 0x{base+head:X})")
    print("=" * 92)
    b = data[off(head): off(head) + max_len]
    for ins in md.disasm(b, base + head):
        rva = ins.address - base
        note = ""
        for op in ins.operands:
            if op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP:
                tgt = ins.address + ins.size + op.mem.disp
                trva = tgt - base
                q = read_q(trva)
                note += f"   ; G rva=0x{trva:X} [{sec_of(trva)}]"
                if q is not None: note += f" fq=0x{q:X}"
        if ins.mnemonic == "call" and ins.op_str.startswith("0x"):
            note += f"   ; -> fn 0x{int(ins.op_str,16)-base:X}"
        print(f"  {rva:08X}: {hb(ins.bytes):<26} {ins.mnemonic} {ins.op_str}{note}")
        if ins.mnemonic in ("ret",) and data[off(rva+ins.size)] == 0xCC:
            break
        if ins.mnemonic == "jmp" and ins.op_str.startswith("0x") and data[off(rva+ins.size)] == 0xCC:
            tj = int(ins.op_str, 16) - base
            if not (head <= tj < head + max_len):
                break
    print()

if len(sys.argv) > 1:
    args = [int(a, 16) for a in sys.argv[1:]]
    i = 0
    while i < len(args):
        head = args[i]
        mx = 0x900
        if i + 1 < len(args) and args[i+1] < 0x10000:
            mx = args[i+1]; i += 1
        dump(head, mx)
        i += 1
else:
    print("SECTIONS:")
    for lo, hi, nm, w, x in SECS:
        print(f"  {nm:<12} rva 0x{lo:08X}..0x{hi:08X}  {'W' if w else '-'}{'X' if x else '-'}")
    print()
    for t in (0x286D04, 0x396A18, 0x2B8CB8):
        dump(t)
