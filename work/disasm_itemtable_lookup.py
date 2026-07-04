#!/usr/bin/env python3
"""Disassemble the weapon/item lookup helpers and Writer A source chain.

Targets:
  fn 0x287410  - weapon/item lookup called by Writer B fn 0x285394
  fn 0x396C8C  - sibling lookup called by Writer B fn 0x3965B0
  fn 0x59F550  - Writer A (copies [rdi+0x10..0x17] -> [unit+0x48..0x4F])
  fn 0x59C0B0  - Writer A's dispatcher (call site 0x59CACD)
  fn 0x285394 / 0x3965B0 - Writer B bodies (context)

For every rip-relative memory operand we compute the absolute target,
report which section it lives in, and (for pointer-sized data) show the
file-time contents.
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
REAL_MAX = 0x610000

pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64)
md.detail = True

def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)

def sec_of(rva):
    for s in pe.sections:
        if s.VirtualAddress <= rva < s.VirtualAddress + max(s.Misc_VirtualSize, s.SizeOfRawData):
            return s.Name.rstrip(b"\x00").decode()
    return "?"

def read_qword(rva):
    try:
        return int.from_bytes(data[off(rva):off(rva)+8], "little")
    except Exception:
        return None

def disasm_func(head, max_len=0x1200, label=""):
    """Linear disasm from head until ret followed by int3, or max_len."""
    print("=" * 90)
    print(f"FUNCTION 0x{head:X} (VA 0x{base+head:X}) {label}")
    print("=" * 90)
    b = data[off(head): off(head) + max_len]
    globals_seen = []
    ended = False
    for ins in md.disasm(b, base + head):
        rva = ins.address - base
        note = ""
        for op in ins.operands:
            if op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP:
                tgt = ins.address + ins.size + op.mem.disp
                trva = tgt - base
                sec = sec_of(trva)
                q = read_qword(trva)
                note += f"   ; GLOBAL 0x{tgt:X} (rva 0x{trva:X}, {sec})"
                if q is not None:
                    note += f" file_q=0x{q:X}"
                globals_seen.append((rva, trva, sec, q, f"{ins.mnemonic} {ins.op_str}"))
        if ins.mnemonic == "call" and ins.op_str.startswith("0x"):
            note += f"   ; -> fn 0x{int(ins.op_str,16)-base:X}"
        print(f"  {rva:08X}: {hb(ins.bytes):<28} {ins.mnemonic} {ins.op_str}{note}")
        if ins.mnemonic == "ret":
            nxt = data[off(rva + ins.size)]
            if nxt == 0xCC:
                ended = True
                break
        if ins.mnemonic == "int3":
            ended = True
            break
        if ins.mnemonic == "jmp" and ins.op_str.startswith("0x"):
            # tail jmp out of function bounds followed by int3 => end
            nxt = data[off(rva + ins.size)]
            if nxt == 0xCC:
                ended = True
                break
    if not ended:
        print(f"  ... (truncated at +0x{max_len:X})")
    if globals_seen:
        print("\n  RIP-RELATIVE GLOBALS in this function:")
        for rva, trva, sec, q, txt in globals_seen:
            print(f"    at 0x{rva:X}: rva 0x{trva:X} ({sec}) file_q=0x{q:X}  [{txt}]")
    print()
    return globals_seen

targets = [
    (0x287410, "weapon/item lookup (Writer B leg 1)"),
    (0x396C8C, "weapon/item lookup (Writer B leg 2)"),
    (0x285394, "Writer B body 1", 0x800),
    (0x3965B0, "Writer B body 2", 0x800),
    (0x59F550, "Writer A (stat block copy)", 0x800),
]

for t in targets:
    head, label = t[0], t[1]
    mx = t[2] if len(t) > 2 else 0x1200
    disasm_func(head, mx, label)

# Dispatcher: show a window around the 0x59CACD call site
print("=" * 90)
print("DISPATCHER fn 0x59C0B0 - window around call site 0x59CACD")
print("=" * 90)
start = 0x59C9E0
b = data[off(start): off(start) + 0x200]
for ins in md.disasm(b, base + start):
    rva = ins.address - base
    if rva > 0x59CB60: break
    note = ""
    for op in ins.operands:
        if op.type == X86_OP_MEM and op.mem.base == X86_REG_RIP:
            tgt = ins.address + ins.size + op.mem.disp
            trva = tgt - base
            note += f"   ; GLOBAL rva 0x{trva:X} ({sec_of(trva)}) file_q=0x{read_qword(trva):X}"
    if ins.mnemonic == "call" and ins.op_str.startswith("0x"):
        note += f"   ; -> fn 0x{int(ins.op_str,16)-base:X}"
    mark = "  <<<< CALL SITE" if rva == 0x59CACD else ""
    print(f"  {rva:08X}: {hb(ins.bytes):<28} {ins.mnemonic} {ins.op_str}{note}{mark}")
