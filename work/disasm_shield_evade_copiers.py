#!/usr/bin/env python3
"""Dump the candidate SNAPSHOT/COPY functions that read the whole evade block
(0x46..0x4E) plus the accuracy-preview functions, to classify each precisely."""
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
import sys
def dump(fn, span, note=""):
    print("#"*78); print(f"# fn 0x{fn:X}   {note}"); print("#"*78)
    b=data[off(fn):off(fn)+span]
    for ins in md.disasm(b,base+fn):
        r=ins.address-base; t=f"{ins.mnemonic} {ins.op_str}".strip(); low=t.lower(); mk=""
        for d in (0x46,0x47,0x48,0x49,0x4A,0x4B,0x4E):
            if f"+ 0x{d:x}]" in low or f"+ 0x{d:X}]" in low: mk=f"  <== +0x{d:02X} READ"
        if not mk:
            if ins.mnemonic=="call": mk="  (call)"
            elif ins.mnemonic in ("ret","int3"): mk="  --"
        print(f"  {r:08X}: {hb(ins.bytes):<22} {t}{mk}")
    print()

fns = eval(sys.argv[1]) if len(sys.argv)>1 else [(0x226EBC,0x120,"all-7 evade -> rip globals? SNAPSHOT candidate")]
for fn,span,note in fns:
    dump(fn,span,note)
