#!/usr/bin/env python3
"""Physical-attack chance = byte [src_unit+0x2B]. Identify src_unit (global ptr
0x1562c3a) and whether +0x2B is a writable per-unit field. Also enumerate ALL RNG
sites' chance-operand kind (global vs [ptr+disp]) for the final verdict table, and
show the single +0x1EA reader at 0x3B122F context (display path)."""
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")
BASE = 0x140000000


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    pe = pefile.PE(str(EXE), fast_load=True)
    data = EXE.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(rva): return pe.get_offset_from_rva(rva)

    def dump(rva, length, marks=None):
        marks = marks or {}
        for ins in md.disasm(data[off(rva):off(rva)+length], BASE+rva):
            r = ins.address-BASE
            t = f"{ins.mnemonic} {ins.op_str}"; low = t.lower(); mk = ""
            for k,v in marks.items():
                if k in low: mk = "  "+v; break
            print(f"    {r:08X}: {hb(ins.bytes):<22} {t}{mk}")

    # global 0x1562c3a load site is 0x308327: 48 8B 1D <disp32>. target of the qword.
    for site in (0x308327,):
        raw = data[off(site):off(site)+7]
        disp = int.from_bytes(raw[3:7],"little",signed=True)
        tgt = (site+7+disp)&0xFFFFFFFF
        print(f"src_unit ptr: 0x{site:X} loads qword [0x{BASE+tgt:X}] (RVA 0x{tgt:X})")
    print()

    # RNG sites chance-operand summary
    sites = {
        0x271D88:"edx=ebx (computed local, +0x9e flag path)",
        0x27223D:"edx=byte[rdi+r15*8+0x787f82] (table)",
        0x27CA8C:"edx=computed (lea/add local)",
        0x304E33:"edx=r8d=byte[0x7B079D Faith]  (MAGIC path fn0x304DF0)",
        0x306636:"edx=byte[0x7B07AC]  (BLIND/status)",
        0x3083AB:"edx=byte[src_unit+0x2B]  (PHYSICAL)",
        0x30946B:"edx=byte[r8+0x2B]  (physical variant)",
        0x30BE86:"edx=byte[unit+0x2B] (reaction?)",
        0x30BEDC:"edx=byte[unit+0x2B]",
        0x30BF32:"edx=byte[unit+0x2B]",
        0x30BF72:"edx=byte[unit+0x2B]",
    }
    print("RNG chance-operand per site:")
    for k,v in sites.items():
        print(f"  0x{k:X}: {v}")

    print("\n+0x1EA single reader 0x3B122F context (display/copy path):")
    dump(0x3B1200, 0x60, marks={"0x1ea":"<== +1EA","0x1eb":"(+1EB)","0x208":"(dst+208)","0xf8":"(dst+F8)"})

    print("\nphysical +0x2B site 0x308388 wider (is +0x2B per-unit accuracy?):")
    dump(0x308370, 0x50, marks={"0x2b]":"<== +2B chance","0x97]":"(+97 flag)","0x278ee0":"RNG"})

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
