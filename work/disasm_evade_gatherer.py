#!/usr/bin/env python3
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
pe=pefile.PE(str(EXE),fast_load=True); base=pe.OPTIONAL_HEADER.ImageBase
data=EXE.read_bytes(); md=Cs(CS_ARCH_X86,CS_MODE_64); md.detail=True
def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)
for fn,span in [(0x30FC30,0x180),(0x30A4A4,0x120)]:
    print("#"*70); print(f"# fn 0x{fn:X}"); print("#"*70)
    b=data[off(fn):off(fn)+span]
    for ins in md.disasm(b,base+fn):
        r=ins.address-base
        t=f"{ins.mnemonic} {ins.op_str}".strip(); low=t.lower(); mk=""
        if any(f"+ 0x{d:02x}]" in low for d in range(0x44,0x50)): mk="  <-- EVADE BYTE"
        elif ins.mnemonic=="call": mk="  call"
        elif ins.mnemonic=="ret": mk=" --"
        print(f"    {r:08X}: {hb(ins.bytes):<20} {t}{mk}")
    print()
