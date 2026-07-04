#!/usr/bin/env python3
"""Tighter classifier: for each real-code READ of an evade byte, group by enclosing
function and report which offsets (0x46/47/4A/4B/4E/48/49) that function touches.

Goal: find the function(s) that read shield (0x4A/0x4E) and CONTRAST with class (0x4B).
We keep only functions that read AT LEAST one evade byte with a legit disp8 pointer
register (rax..r15, not rsp-relative frame temporaries), and dedupe by function head.
"""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
REAL_MAX = 0x610000
UNIT_TABLE = 0x141853CE0
EVADE = {0x46:"wpn",0x47:"wpn",0x48:"acc",0x49:"acc",0x4A:"shd",0x4B:"cls",0x4E:"shd"}

pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)

exsecs=[]
for sec in pe.sections:
    if sec.Characteristics & 0x20000000:
        exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))

def find_func_head(rva, back=0x800):
    start = rva - back
    b = data[off(start): off(rva)]
    last=None; i=0
    while i < len(b):
        if b[i]==0xCC:
            j=i
            while j<len(b) and b[j]==0xCC: j+=1
            last=start+j; i=j
        else: i+=1
    return last if last else start

# Reg-relative READ scan; skip rsp/rbp-frame (rm==4 sib esp, or base=rbp) to cut noise.
# We require the modrm base register to be a pointer reg (not rsp).
def scan():
    hits=[]
    for va,praw,sz in exsecs:
        blob=data[praw:praw+sz]; n=len(blob)
        for i in range(n-8):
            rva=va+i
            if rva>=REAL_MAX: break
            j=i; rex=0
            if 0x40<=blob[j]<=0x4F: rex=blob[j]; j+=1
            b0=blob[j]; disp_at=None; base_rm=None
            if b0==0x0F and blob[j+1] in (0xB6,0xBE):
                modrm=blob[j+2]; mod=modrm>>6; rm=modrm&7
                if mod==1 and rm not in (4,5): disp_at=j+3; base_rm=rm
            elif b0==0x8A:
                modrm=blob[j+1]; mod=modrm>>6; rm=modrm&7
                if mod==1 and rm not in (4,5): disp_at=j+2; base_rm=rm
            elif b0 in (0x38,0x3A):
                modrm=blob[j+1]; mod=modrm>>6; rm=modrm&7
                if mod==1 and rm not in (4,5): disp_at=j+2; base_rm=rm
            elif b0==0x84:
                modrm=blob[j+1]; mod=modrm>>6; rm=modrm&7
                if mod==1 and rm not in (4,5): disp_at=j+2; base_rm=rm
            elif b0==0x80:
                modrm=blob[j+1]; mod=modrm>>6; rm=modrm&7
                if mod==1 and rm not in (4,5): disp_at=j+2; base_rm=rm
            if disp_at is None or disp_at>=n: continue
            d=blob[disp_at]
            if d in EVADE:
                # base reg number (with REX.B)
                reg = base_rm + (8 if (rex & 1) else 0)
                # skip rbp(5) frame though excluded; keep pointer regs
                hits.append((rva,d,reg))
    return hits

hits=scan()
# group by function head
funcs={}
for rva,d,reg in hits:
    h=find_func_head(rva)
    funcs.setdefault(h,{"offs":set(),"sites":[]})
    funcs[h]["offs"].add(d)
    funcs[h]["sites"].append((rva,d,reg))

REGN=["rax","rcx","rdx","rbx","rsp","rbp","rsi","rdi","r8","r9","r10","r11","r12","r13","r14","r15"]

# Only report functions that read a SHIELD (0x4A or 0x4E) OR class (0x4B) byte,
# and touch >=2 distinct evade offsets (real recompute/gather signature) OR are the
# lone shield/class reader.
print("Functions touching shield(0x4A/4E) or class(0x4B) evade bytes:\n")
interesting=[]
for h,info in funcs.items():
    offs=info["offs"]
    if offs & {0x4A,0x4E,0x4B}:
        interesting.append((h,info))
interesting.sort()
for h,info in interesting:
    offs=sorted(info["offs"])
    tag="+".join(f"{o:02X}({EVADE[o]})" for o in offs)
    multi = len(offs)>=2
    print(f"fn 0x{h:X}   offsets: {tag}   {'[MULTI]' if multi else ''}")
    for rva,d,reg in sorted(info["sites"]):
        print(f"    0x{rva:X}  +0x{d:02X}({EVADE[d]})  base={REGN[reg]}")
print(f"\ntotal functions: {len(interesting)}")
