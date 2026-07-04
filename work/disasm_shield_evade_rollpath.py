#!/usr/bin/env python3
"""Trace the avoidance ROLL PATH: dump the known anchors and mark every read of an
evade byte (0x46/47/48/49/4A/4B/4E) and every equipment-pointer deref, so we can see
what actually feeds the shield leg vs the class leg into the VM roll."""
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
EV={0x46,0x47,0x48,0x49,0x4A,0x4B,0x4E}
def dump(fn, span, note=""):
    print("#"*78); print(f"# fn 0x{fn:X}   {note}"); print("#"*78)
    b=data[off(fn):off(fn)+span]
    for ins in md.disasm(b,base+fn):
        r=ins.address-base; t=f"{ins.mnemonic} {ins.op_str}".strip(); low=t.lower(); mk=""
        for d in EV:
            if f"+ 0x{d:x}]" in low: mk+=f"  <== +0x{d:02X}"
        if "0x1be" in low: mk+="  <FORECAST+1BE>"
        if "0x1ea" in low or "0x2c]" in low: mk+="  <PREVIEW#>"
        if not mk and ins.mnemonic=="call": mk="  (call)"
        if ins.mnemonic in ("ret","int3"): mk+="  --"
        print(f"  {r:08X}: {hb(ins.bytes):<22} {t}{mk}")
    print()

for fn,span,note in [
    (0x30F0C4,0x200,"PRODUCER"),
    (0x30FA34,0x100,"ROLL"),
    (0x30FC30,0x180,"GATHER"),
    (0x309A44,0x120,"COMPUTE-ENTRY"),
    (0x205210,0x220,"SELECTOR"),
]:
    dump(fn,span,note)
