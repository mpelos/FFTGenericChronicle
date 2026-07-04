#!/usr/bin/env python3
from __future__ import annotations
from pathlib import Path
import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE = Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe")


def main():
    pe = pefile.PE(str(EXE), fast_load=True); base = pe.OPTIONAL_HEADER.ImageBase
    data = EXE.read_bytes(); md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True
    def off(r): return pe.get_offset_from_rva(r)
    def dump(s, l, note=""):
        print(f"--- 0x{s:X} ({note}) ---")
        b = data[off(s):off(s)+l]
        for ins in md.disasm(b, base+s):
            r = ins.address-base; t = f"{ins.mnemonic} {ins.op_str}"; low=t.lower(); mk=""
            if "0x1c0" in low: mk="  <1C0"
            elif ins.mnemonic=="call": mk="  (call)"
            elif ins.mnemonic=="ret": mk="  (ret)"
            print(f"    {r:08X}: {t}{mk}")
        print()
    dump(0x31C800, 0x80, "caller 0x31C830/0x31C892 of kind-switch fn 0x26A704")
    dump(0x26A704, 0x50, "fn 0x26A704 head (selector call + kind switch @0x26A98B)")
    # the message-id producing tail of 26A704: after switch it sets r14d = ids
    # 0x18,0x19,0x58,0x59,0x5A -> where do those go? show 0x26AAEA sink
    dump(0x26AAEA, 0x60, "26A704 sink (message-id r14 consumed)")
    # 0x1FA9E4 is the multi-target anim-code buffer builder; who calls it (0x20C4A5 etc)
    dump(0x20C490, 0x40, "caller of anim-code builder 0x1FA9E4")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
