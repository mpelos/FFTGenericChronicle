#!/usr/bin/env python3
"""Static hunt for the FINAL computed hit% storage before the Denuvo VM roll.

Questions:
 (1) unit+0x1EA (word hit%): every REAL-code WRITE and READ.
 (2) fn 0x304DF0 magic-accuracy: what it reads, where it writes accuracy before
     roll 0x304E33 (call RNG 0x278EE0).
 (3) dispatch table RVA 0x682BC8 (162 handlers): read entries as 8-byte LE pointers,
     classify real-code vs trampoline.
 (4) any `mov edx,[reg+disp]` immediately preceding `call 0x278EE0` -> chance source.
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
BASE = 0x140000000

RNG = 0x278EE0
FN_MAGACC = 0x304DF0
ROLL_MAGACC = 0x304E33
DISPATCH = 0x682BC8
FAITH_GLOBAL = 0x7B079D


def hb(b): return " ".join(f"{x:02X}" for x in b)


def main():
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True

    def off(rva): return pe.get_offset_from_rva(rva)

    exsecs = []
    for sec in pe.sections:
        if sec.Characteristics & 0x20000000:
            exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))

    def in_real(rva): return 0x1000 <= rva < REAL_MAX

    def disasm_at(start, length=14):
        b = data[off(start): off(start) + length]
        return next(md.disasm(b, BASE + start), None)

    def window(rva, before, after, marks=None):
        marks = marks or {}
        b = data[off(rva - before): off(rva) + after]
        out = []
        for ins in md.disasm(b, BASE + (rva - before)):
            r = ins.address - BASE
            if r > rva + after:
                break
            t = f"{ins.mnemonic} {ins.op_str}".strip()
            low = t.lower()
            mk = ""
            for needle, label in marks.items():
                if needle in low:
                    mk = "  " + label
                    break
            out.append(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")
        return out

    print(f"exe={exe}\n")

    def is_disp32_1ea(b, i):
        return b[i] == 0xEA and b[i+1] == 0x01 and b[i+2] == 0x00 and b[i+3] == 0x00

    # (1) writes/reads of +0x1EA
    print("=" * 72)
    print("(1) REAL-code accesses of +0x1EA (hit% word). 66-prefix => 16-bit.")
    print("=" * 72)
    writes, reads = [], []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 10):
            rva = va + i
            if rva >= REAL_MAX:
                break
            j = i
            opsize16 = False
            if blob[j] == 0x66:
                opsize16 = True
                j += 1
            if 0x40 <= blob[j] <= 0x4F:
                j += 1
            op = blob[j]
            modrm = blob[j+1] if j+1 < n else 0
            mod = modrm >> 6
            rm = modrm & 0x07
            reg = (modrm >> 3) & 0x07
            disp_at = None
            kind = None
            is_write = None
            if op == 0x89:
                if mod == 0x02 and rm != 0x04:
                    disp_at = j+2; kind = "mov [r+d32],r"; is_write = True
            elif op == 0xC7 and reg == 0:
                if mod == 0x02 and rm != 0x04:
                    disp_at = j+2; kind = "mov [r+d32],imm"; is_write = True
            elif op == 0x88:
                if mod == 0x02 and rm != 0x04:
                    disp_at = j+2; kind = "mov byte [r+d32],r8"; is_write = True
            elif op == 0xC6 and reg == 0:
                if mod == 0x02 and rm != 0x04:
                    disp_at = j+2; kind = "mov byte [r+d32],imm8"; is_write = True
            elif op == 0x8B:
                if mod == 0x02 and rm != 0x04:
                    disp_at = j+2; kind = "mov r,[r+d32]"; is_write = False
            elif op == 0x8A:
                if mod == 0x02 and rm != 0x04:
                    disp_at = j+2; kind = "mov r8,[r+d32]"; is_write = False
            elif op == 0x0F and blob[j+1] in (0xB6, 0xB7, 0xBE, 0xBF):
                modrm = blob[j+2]; mod = modrm >> 6; rm = modrm & 0x07
                if mod == 0x02 and rm != 0x04:
                    disp_at = j+3; kind = f"movzx/sx r,[r+d32] (0F {blob[j+1]:02X})"; is_write = False
            elif op in (0x3B, 0x39, 0x3A, 0x38):
                if mod == 0x02 and rm != 0x04:
                    disp_at = j+2; kind = "cmp [r+d32]"; is_write = False
            if kind and disp_at and disp_at+4 <= n and is_disp32_1ea(blob, disp_at):
                rec = (rva, kind, opsize16)
                (writes if is_write else reads).append(rec)

    writes = sorted(set(writes)); reads = sorted(set(reads))
    print(f"\n--- WRITES to +0x1EA: {len(writes)} ---")
    for rva, kind, w16 in writes:
        ins = disasm_at(rva)
        txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
        print(f"\n  0x{rva:X}: {txt}   [{kind}{' 16b' if w16 else ''}]")
        for line in window(rva, 0x1C, 8, marks={"0x1ea": "<== +1EA WRITE", "0x1be": "(+1BE)"}):
            print(line)
    print(f"\n--- READS of +0x1EA: {len(reads)} ---")
    for rva, kind, w16 in reads:
        ins = disasm_at(rva)
        txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
        print(f"\n  0x{rva:X}: {txt}   [{kind}{' 16b' if w16 else ''}]")
        for line in window(rva, 6, 0x18, marks={"0x1ea": "<== +1EA READ", "0x278ee0": "<== RNG!"}):
            print(line)

    # (2) fn 0x304DF0
    print("\n" + "=" * 72)
    print(f"(2) fn 0x{FN_MAGACC:X} full trace (roll site 0x{ROLL_MAGACC:X})")
    print("=" * 72)
    b = data[off(FN_MAGACC): off(FN_MAGACC) + 0x150]
    for ins in md.disasm(b, BASE + FN_MAGACC):
        r = ins.address - BASE
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        low = t.lower()
        mk = ""
        if "0x278ee0" in low or "278ee0" in low:
            mk = "  <== RNG ROLL"
        elif "0x1ea" in low:
            mk = "  <== +1EA"
        elif "0x1be" in low:
            mk = "  <== +1BE"
        elif "7b079d" in low:
            mk = "  <== FAITH GLOBAL"
        elif "0x2d]" in low:
            mk = "  (+2D faith?)"
        elif ins.mnemonic == "call":
            mk = "  (call)"
        print(f"    {r:08X}: {hb(ins.bytes):<24} {t}{mk}")
        if r - FN_MAGACC > 0x130:
            break

    # (3) dispatch table
    print("\n" + "=" * 72)
    print(f"(3) dispatch table 0x{BASE+DISPATCH:X} (RVA 0x{DISPATCH:X}) — 170 entries as 8-byte LE")
    print("=" * 72)
    tbl = data[off(DISPATCH): off(DISPATCH) + 170*8]
    handlers = []
    for k in range(170):
        q = int.from_bytes(tbl[k*8:k*8+8], "little")
        handlers.append(q)
    real_count = 0
    for k, q in enumerate(handlers):
        rva = q - BASE if BASE <= q < BASE + 0x40000000 else None
        tag = ""
        if rva is None:
            tag = "(non-ptr)"
        elif in_real(rva):
            tag = "REAL"; real_count += 1
        elif 0x1000 <= rva:
            tag = "TRAMPOLINE/VM"
        print(f"    [{k:3}] 0x{q:016X}  {('rva=0x%X'%rva) if rva else '':<14} {tag}")
    print(f"\n  handlers in REAL code: {real_count}")

    # (4) RNG call sites
    print("\n" + "=" * 72)
    print(f"(4) call sites of RNG 0x{RNG:X}: show insns before to find edx(chance) source")
    print("=" * 72)
    call_sites = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 5):
            rva = va + i
            if rva >= REAL_MAX:
                break
            if blob[i] == 0xE8:
                rel = int.from_bytes(blob[i+1:i+5], "little", signed=True)
                tgt = (rva + 5 + rel) & 0xFFFFFFFF
                if tgt == RNG:
                    call_sites.append(rva)
    print(f"\n  RNG call sites (E8 rel32): {len(call_sites)} -> {[hex(x) for x in call_sites]}")
    for cs in call_sites:
        print(f"\n  --- call site 0x{cs:X} ---")
        for line in window(cs, 0x2E, 6, marks={
            "0x278ee0": "<== RNG", "edx": "(edx=chance)", "0x1ea": "<== +1EA!",
        }):
            print(line)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
