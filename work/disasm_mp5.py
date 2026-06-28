#!/usr/bin/env python3
"""Final confirmation:
 (a) re-print apply block with CORRECTED absolute field map (rbp=base+0x1BE).
 (b) Find the staging writer of rec+0x1C8 / rec+0x1CA (the MP debit/credit terms)
     analogous to the +0x1C4 writer at 0x34E8E2 / 0x34E96A.
 (c) confirm 0x30A4A4 -> 0x30A51C path (the apply entry)."""
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
REAL_MAX = 0x610000


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(rva): return pe.get_offset_from_rva(rva)

    RBP_BASE = 0x1BE  # g_rbp = defender_base + 0x1BE  (proven at 0x271B9F)
    print(f"NOTE: rbp = defender_base + 0x{RBP_BASE:X}  => rbp+N maps to defender+0x{RBP_BASE:X}+N\n")
    print("=== APPLY block 0x30A66F..0x30A6CC with ABSOLUTE field map ===")
    start = 0x30A66F
    blob = data[off(start): off(start)+(0x30A6CC-start)+6]
    for ins in md.disasm(blob, base+start):
        r=ins.address-base
        if r>0x30A6CC: break
        t=f"{ins.mnemonic} {ins.op_str}"; low=t.lower(); note=""
        import re
        for m in re.finditer(r"rbp \+ 0x([0-9a-f]+)", low):
            d=int(m.group(1),16); note+=f"  [rec+0x{RBP_BASE:X}+0x{d:X} = DEF+0x{RBP_BASE+d:X}]"
        if "rbp + 6" in low and "0x" not in low.split("rbp + ")[1][:3]:
            note+=f"  [rec+6 = DEF+0x{RBP_BASE+6:X}]"
        # handle the small disp forms rbp+6/+8 (capstone prints 'rbp + 6')
        for sd in (6,8,0xa,0xc,0x14,0x15,0x16,0x17):
            if f"rbp + {sd}" in low or f"rbp + 0x{sd:x}" in low:
                note=f"  [rec+0x{sd:X} = DEF+0x{RBP_BASE+sd:X}]"
        for sd,lab in ((0x30,"HP"),(0x32,"MP?cur2"),(0x34,"MP"),(0x36,"MaxMP")):
            if f"rdi + 0x{sd:x}" in low: note+=f"  [DEF+0x{sd:X} {lab}]"
        print(f"  {r:08X}: {hb(ins.bytes):<24} {t}{note}")

    # (b) staging writers of rec+0x1C8 / rec+0x1CA  (absolute disp32 stores via 66 89 ... )
    for disp in (0x1C8, 0x1CA, 0x1CC):
        print(f"\n=== writers of rec+0x{disp:X} (66 89 store w/ disp32) ===")
        lo=disp&0xFF; b2=(disp>>8)&0xFF
        hits=[]
        for sec in pe.sections:
            if not (sec.Characteristics & 0x20000000): continue
            blob=data[sec.PointerToRawData:sec.PointerToRawData+sec.SizeOfRawData]; n=len(blob)
            va=sec.VirtualAddress
            for i in range(n-7):
                rva=va+i
                if rva>=REAL_MAX: break
                # 66 89 modrm(mod=10) disp32 ; or 66 C7 (imm). also movzx forms (read)
                if blob[i]==0x66 and blob[i+1] in (0x89,0x8B) and (blob[i+2]>>6)==0x02 and (blob[i+2]&7) not in (4,5) and blob[i+3]==lo and blob[i+4]==b2 and blob[i+5]==0 and blob[i+6]==0:
                    hits.append((rva,blob[i+1]))
        for rva,opc in hits[:20]:
            ins=next(md.disasm(data[off(rva):off(rva)+10],base+rva),None)
            print(f"  0x{rva:X}: [{'STORE' if opc==0x89 else 'load'}] {ins.mnemonic+' '+ins.op_str if ins else '?'}")
        print(f"  total: {len(hits)}")

    # (c) 0x30A4A4 head -> does it tail to 0x30A51C?
    print("\n=== 0x30A4A4 head (apply dispatcher?) ===")
    blob=data[off(0x30A4A4):off(0x30A4A4)+0x80]
    for ins in md.disasm(blob, base+0x30A4A4):
        r=ins.address-base
        if r>0x30A4A4+0x78: break
        t=f"{ins.mnemonic} {ins.op_str}"
        mark="  <== calls HP/MP-apply 0x30A51C" if "0x30a51c" in t.lower() else ""
        print(f"  {r:08X}: {hb(ins.bytes):<20} {t}{mark}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
