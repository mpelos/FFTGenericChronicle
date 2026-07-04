#!/usr/bin/env python3
"""Find SNAPSHOT sites: an evade byte read from [src+0x4A/0x4E/0x4B] immediately
followed (within a few instrs) by a store to a DIFFERENT destination (another struct
field or a rip-global). These are the copy-into-forecast/context sites."""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
pe=pefile.PE(str(EXE),fast_load=True); base=pe.OPTIONAL_HEADER.ImageBase
data=EXE.read_bytes(); md=Cs(CS_ARCH_X86,CS_MODE_64); md.detail=True
REAL_MAX=0x610000
def off(r): return pe.get_offset_from_rva(r)
def hb(b): return ' '.join(f'{x:02X}' for x in b)
exsecs=[(s.VirtualAddress,s.PointerToRawData,s.SizeOfRawData) for s in pe.sections if s.Characteristics & 0x20000000]

SHIELD={0x4A,0x4E}; CLASS={0x4B}
def classify_window(rva):
    """Disasm forward from an evade READ; return the store target if the loaded value
    is copied elsewhere within 4 instrs. Detect 'mov r8,[src+0x4A]; mov [dst+k],r8'
    or store to rip global."""
    b=data[off(rva):off(rva)+0x20]
    ins=list(md.disasm(b,base+rva))
    if not ins: return None
    first=ins[0]
    # loaded into which reg? mov al/… ; movzx eax,…
    # Just look at next up-to-4 instrs for a byte store using same reg
    out=[]
    for k in ins[:5]:
        out.append((k.address-base,f'{k.mnemonic} {k.op_str}'))
    # detect store
    for a,t in out[1:]:
        if t.startswith('mov ') and '],' in t and ('rip' in t or '+ 0x' in t):
            # a store to a different memory
            return (out, (a,t))
    return (out, None)

results=[]
for va,praw,sz in exsecs:
    blob=data[praw:praw+sz]; n=len(blob)
    for i in range(n-6):
        rva=va+i
        if rva>=REAL_MAX: break
        j=i
        if 0x40<=blob[j]<=0x4F: j+=1
        b0=blob[j]; disp_at=None
        if b0==0x8A:
            modrm=blob[j+1]; mod=modrm>>6; rm=modrm&7
            if mod==1 and rm not in (4,5): disp_at=j+2
        elif b0==0x0F and blob[j+1] in (0xB6,0xBE):
            modrm=blob[j+2]; mod=modrm>>6; rm=modrm&7
            if mod==1 and rm not in (4,5): disp_at=j+3
        if disp_at is None or disp_at>=n: continue
        d=blob[disp_at]
        if d in SHIELD or d in CLASS:
            r=classify_window(rva)
            if r and r[1]:
                results.append((rva,d,r))

print("SNAPSHOT/COPY sites (evade byte read -> stored to different dest):\n")
for rva,d,(out,store) in results:
    kind='SHIELD' if d in SHIELD else 'CLASS'
    print(f"0x{rva:X}  +0x{d:02X} [{kind}]  -> store {store[1]} @0x{store[0]:X}")
    for a,t in out:
        print(f"      {a:08X}: {t}")
    print()
print(f"total copy sites: {len(results)}")
