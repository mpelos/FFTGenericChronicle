#!/usr/bin/env python3
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
md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail=True
def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)
def find_head(rva):
    o=off(rva)
    for k in range(1,0x3000):
        if data[o-k]==0xCC and data[o-k-1]==0xCC:
            h=o-k
            while data[h]==0xCC: h+=1
            return rva-(o-h)
    return rva
def dumpfn(rva, span, title):
    h=find_head(rva)
    b=data[off(h):off(h)+span]
    print("#"*76); print(f"# {title}  callsite 0x{rva:X} head 0x{h:X}"); print("#"*76)
    for ins in md.disasm(b, base+h):
        r=ins.address-base
        t=f"{ins.mnemonic} {ins.op_str}".strip(); low=t.lower(); mk=""
        if "1853ce0" in low: mk="  <== UNIT TABLE 0x141853CE0"
        elif "0x200" in low and ins.mnemonic in ("add","lea","cmp","sub","imul"): mk="  <== stride 0x200?"
        elif ins.mnemonic=="call": mk="  <== CALL"
        if r==rva: mk += "  <<< OUR CALLSITE"
        if mk or ins.mnemonic in ("ret",):
            print(f"    {r:08X}: {hb(ins.bytes):<22} {t}{mk}")
for rva,span,title in [
    (0x59C0B0,0x400,"F_shield caller 0x59C0B0 (calls 0x59C... -> 0x59F550 chain)"),
    (0x33E428,0x300,"F_wpn root 0x33E428"),
    (0x210214,0x300,"F_wpn root 0x210214"),
    (0x3932E4,0x300,"F_wpn root 0x3932E4"),
    (0x396D9C,0x200,"F_wpn caller 0x396D9C"),
]:
    dumpfn(rva,span,title); print()
