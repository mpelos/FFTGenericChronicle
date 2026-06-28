#!/usr/bin/env python3
"""Find the MP debit/credit path during action resolution.

Strategy (desync-proof, byte-scan + clean-window disasm, like disasm_reaction_scan.py):
  1. Disasm the FULL body of the HP-apply fn 0x30A51C until ret. Look for:
       - the +0x30 (HP) clamp we already know,
       - a SECOND clamp on a nearby word offset (+0x32/+0x34/+0x36) => MP,
       - reads of result-record offsets near +0x1C4/+0x1C6 (the staged-MP words).
  2. Byte-scan ALL real code for word (16-bit, 66-prefixed) reads/writes of the
     candidate MP struct offsets (+0x32/+0x34/+0x36) to locate MP read/write sites.
  3. For each clamp-looking site, dump a clean window.
"""
from __future__ import annotations

from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]
REAL_MAX = 0x610000
HP_APPLY = 0x30A51C
HP_PRECLAMP = 0x30A66F


def hb(b: bytes) -> str:
    return " ".join(f"{x:02X}" for x in b)


def main() -> int:
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    def off(rva):
        return pe.get_offset_from_rva(rva)

    exsecs = []
    for sec in pe.sections:
        if sec.Characteristics & 0x20000000:
            exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))

    print(f"exe={exe}\nimage_base=0x{base:X}\n")

    # ---- 1. Full disasm of the HP-apply fn body ----
    print(f"=== FULL BODY of HP-apply fn 0x{HP_APPLY:X} (until ret/desync) ===")
    start = HP_APPLY
    blob = data[off(start): off(start) + 0x600]
    count = 0
    for ins in md.disasm(blob, base + start):
        r = ins.address - base
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        low = t.lower()
        mark = ""
        # struct word offsets of interest
        for disp, label in (
            (0x30, "HP"), (0x32, "?MP?"), (0x34, "?MP?"), (0x36, "?MP?"),
            (0x38, "?max?"), (0x3a, "?max?"), (0x3c, "?max?"),
        ):
            if f"0x{disp:x}]" in low or f"+ 0x{disp:x}]" in low:
                mark += f"  <== struct+0x{disp:X} {label}"
        # result-record offsets of interest
        for disp, label in (
            (0x1c4, "staged-dmg"), (0x1c6, "staged-heal"),
            (0x1c8, "?staged?"), (0x1ca, "?staged?"), (0x1cc, "?staged?"),
            (0x1e5, "result-kind"),
        ):
            if f"0x{disp:x}]" in low:
                mark += f"  <== rec+0x{disp:X} {label}"
        if ins.mnemonic in ("cmp",) or "cmov" in ins.mnemonic:
            mark += "  *clamp?*"
        if r == HP_PRECLAMP:
            mark += "  <<< HP PRE-CLAMP HOOK"
        print(f"  {r:08X}: {hb(ins.bytes):<24} {t}{mark}")
        count += 1
        if ins.mnemonic == "ret" and count > 8:
            break
        if count > 320:
            print("  ... (cap)")
            break

    # ---- 2. byte-scan reads/writes of candidate MP struct offsets ----
    # encodings to catch (reg+disp8 form, mod==01):
    #   66 89 /r d8   mov word [r+d8], r16   (store)
    #   66 8B /r d8   mov r16, word [r+d8]    (load)
    #   0F B7 /r d8   movzx r32, word [r+d8]
    #   0F BF /r d8   movsx r32, word [r+d8]
    #   66 0F B7 ...  (with 66 prefix variants)
    #   66 C7 /0 d8 iw mov word [r+d8], imm16
    def modrm_disp8_simple(mb):
        return (mb >> 6) == 0x01 and (mb & 0x07) not in (0x04, 0x05)

    for target_disp in (0x32, 0x34, 0x36, 0x38, 0x3A, 0x3C):
        print(f"\n=== REAL-code word access of struct+0x{target_disp:X} [byte-scan] ===")
        hits = []
        for va, praw, sz in exsecs:
            blob = data[praw: praw + sz]
            n = len(blob)
            for i in range(n - 6):
                rva = va + i
                if rva >= REAL_MAX:
                    break
                b = blob[i:i + 6]
                hit = False
                # 66-prefixed mov word
                if b[0] == 0x66 and b[1] in (0x89, 0x8B) and modrm_disp8_simple(b[2]) and b[3] == target_disp:
                    hit = True
                # 66 C7 store imm16
                elif b[0] == 0x66 and b[1] == 0xC7 and ((b[2] >> 6) == 0x01) and ((b[2] & 0x07) not in (0x04, 0x05)) and ((b[2] >> 3) & 7) == 0 and b[3] == target_disp:
                    hit = True
                # movzx/movsx word
                elif b[0] == 0x0F and b[1] in (0xB7, 0xBF) and modrm_disp8_simple(b[2]) and b[3] == target_disp:
                    hit = True
                # 66 0F B7/BF (rare)
                elif b[0] == 0x66 and b[1] == 0x0F and b[2] in (0xB7, 0xBF) and modrm_disp8_simple(b[3]) and b[4] == target_disp:
                    hit = True
                if hit:
                    hits.append(rva)
        hits = sorted(set(hits))
        for rva in hits[:60]:
            # back up for possible REX
            o = off(rva)
            start2 = rva - 1 if (data[o - 1] & 0xF0) == 0x40 else rva
            blob2 = data[off(start2): off(start2) + 10]
            ins = next(md.disasm(blob2, base + start2), None)
            txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
            print(f"  0x{rva:X}: {txt}")
        print(f"  total struct+0x{target_disp:X} sites: {len(hits)}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
