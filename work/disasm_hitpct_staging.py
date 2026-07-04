#!/usr/bin/env python3
"""Identify the staging/result object (r11 = global 0x1564956 etc.) and whether it
aliases unit+0x1BE. Also dump fn 0x306608 (Blind/status handler entry) and
0x308318 (physical handler entry) to confirm which struct field each reads for the
chance, and whether +0x1EA/+0x2C is the writable pre-roll lever."""
from __future__ import annotations
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
        b = data[off(rva): off(rva)+length]
        for ins in md.disasm(b, BASE+rva):
            r = ins.address - BASE
            t = f"{ins.mnemonic} {ins.op_str}".strip(); low = t.lower(); mk = ""
            for needle, label in marks.items():
                if needle in low: mk = "  " + label; break
            print(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")

    # The staging-object globals seen: 0x1564956 (r11 in 0x306608), 0x1562c39 (r11 in 0x308327)
    # These are qword ptr [rip+disp]; resolve the absolute .data addresses they load.
    print("=" * 72)
    print("Resolve staging-object global pointers (rip-relative loads before RNG sites)")
    print("=" * 72)
    for site, note in [(0x306603, "306608 r11 load"), (0x308330, "308327 r11 load"),
                       (0x3065F6, "306608 rax load(src unit)"), (0x308327, "308327 rbx load(src unit)")]:
        b = data[off(site): off(site)+7]
        ins = next(md.disasm(b, BASE+site), None)
        if ins and ins.operands:
            for op in ins.operands:
                if op.type == 3:  # mem
                    if op.mem.base == 41:  # rip in capstone? just compute
                        pass
        # compute rip-rel target manually: 48 8B xx disp32
        # find disp
        raw = data[off(site):off(site)+7]
        # format REX 48, 8B, modrm, disp32
        disp = int.from_bytes(raw[3:7], "little", signed=True)
        tgt = (site + 7 + disp) & 0xFFFFFFFF
        print(f"  {note}: at 0x{site:X}  loads qword [0x{BASE+tgt:X}] (RVA 0x{tgt:X})")

    print("\n" + "=" * 72)
    print("Blind/status handler 0x306608 (dispatch entry near [56]=0x306A20? check)")
    print("=" * 72)
    dump(0x3065F0, 0x60, marks={"0x2c]": "<== +2C staged", "0x1a8": "(+1A8 src word)",
                                "0x4aa179": "<== chance byte src", "0x278ee0": "RNG"})

    print("\n" + "=" * 72)
    print("Physical handler region 0x308318 (dispatch [42]) full")
    print("=" * 72)
    dump(0x308318, 0xC0, marks={"0x2c]": "<== +2C staged", "0x2b]": "<== +2B src byte",
                                "0x278ee0": "RNG"})

    # what does 0x4aa179 hold near? it's the chance byte for Blind path. And 0x4ab995
    # (magic path Faith). Show the surrounding data addresses as RVAs.
    print("\n" + "=" * 72)
    print("Chance-source data addresses")
    print("=" * 72)
    print("  Blind path 0x306636: edx = byte [0x%X] (RVA 0x4AA179)" % (BASE+0x4AA179))
    print("  Magic path 0x304E33: edx = r8d = byte [0x%X] (RVA 0x4AB995)" % (BASE+0x4AB995))
    print("  Physical 0x3083AB:   edx = byte [src_unit+0x2B]")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
