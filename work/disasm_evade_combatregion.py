#!/usr/bin/env python3
"""Check the combat/avoidance region 0x309000..0x310000 for:
  - any WRITE to evade bytes 0x44..0x4F (would be the pre-roll restore)
  - how the producer 0x30F0C4 READS the evade bytes (fresh from struct?)
Also list ALL writers to 0x46/0x47/0x4A/0x4B/0x4E that fall in combat region.
"""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
pe = pefile.PE(str(EXE), fast_load=True)
base=pe.OPTIONAL_HEADER.ImageBase
data=EXE.read_bytes()
md=Cs(CS_ARCH_X86,CS_MODE_64); md.detail=True
def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)
KEY={0x46,0x47,0x4A,0x4B,0x4E}
REGS=["rax","rcx","rdx","rbx","rsp","rbp","rsi","rdi","r8","r9","r10","r11","r12","r13","r14","r15"]

# scan writers in combat region only
LO,HI=0x309000,0x310000
sec=[(s.VirtualAddress,s.PointerToRawData,s.SizeOfRawData) for s in pe.sections if s.Characteristics&0x20000000]
print("Writers to evade bytes 0x44..0x4F in combat region 0x309000..0x310000:")
found=0
for va,praw,sz in sec:
    blob=data[praw:praw+sz]; n=len(blob)
    for i in range(n-8):
        rva=va+i
        if rva<LO: continue
        if rva>=HI: break
        j=i
        if 0x40<=blob[j]<=0x4F: j+=1
        op=blob[j]; modrm=blob[j+1] if j+1<n else 0
        mod=modrm>>6; rm=modrm&7
        if op not in (0x88,0xC6): continue
        if op==0xC6 and ((modrm>>3)&7)!=0: continue
        disp=None
        if mod==1 and rm not in (4,): disp=blob[j+2]
        elif mod==1 and rm==4: disp=blob[j+3]
        if disp is None or disp not in range(0x44,0x50): continue
        found+=1
        b=data[off(rva):off(rva)+8]
        ins=next(md.disasm(b,base+rva),None)
        print(f"  0x{rva:X}: {ins.mnemonic} {ins.op_str}")
print(f"  total: {found}\n")

# dump producer 0x30F0C4 window, marking reads of 0x44..0x4F
print("Producer 0x30F0C4: how it READS evade bytes")
b=data[off(0x30F0C4):off(0x30F0C4)+0x300]
for ins in md.disasm(b,base+0x30F0C4):
    r=ins.address-base
    t=f"{ins.mnemonic} {ins.op_str}".strip(); low=t.lower(); mk=""
    if any(f"+ 0x{d:02x}]" in low for d in range(0x44,0x50)): mk="  <-- EVADE BYTE READ/USE"
    elif ins.mnemonic=="call": mk="  <== CALL"
    if mk:
        print(f"    {r:08X}: {hb(ins.bytes):<20} {t}{mk}")
