#!/usr/bin/env python3
"""Byte-pattern (desync-proof) hunt for the evade-TYPE machinery at unit+0x1C0.

Question: what produces evade-type 0x01 (cloak/accessory evade) and 0x06 (plain
miss), and can we select them via memory?

Linear capstone sweep desyncs on data/jump-tables, so we BYTE-SCAN exact encodings
and disasm a clean window at each hit (technique from disasm_reaction_scan.py).

Targets:
  (1) every REAL-code WRITE to displacement 0x1C0 (the evade-type byte):
        88 /r  d32=C0 01 00 00   mov  [reg+0x1C0], r8       (modrm mod==10)
        88 /r  d8 ...            (no: 0x1C0 needs disp32)
        C6 /0  d32=C0 01 00 00 imm8   mov byte [reg+0x1C0], imm8
      with optional REX prefix. Also disp via SIB are caught loosely.
  (2) every REAL-code READ of displacement 0x1C0 (movzx/movsx/mov r8) to see who
      consumes the type (e.g. the selector path).
  (3) reads of plausible ACCESSORY-evade displacements that might drive type 0x01:
        0x48,0x49,0x4C,0x4D and 0x52..0x5F  (the gaps around the 5 known evade bytes
        0x46/0x47 weapon, 0x4A/0x4E shield, 0x4B class).
  (4) the selector 0x205210: dump a generous window to expose the cl(=type) switch
      / jump table and any immediate comparisons to 0x01..0x06.
  (5) immediate compares to the evade-type constants (cmp ..., 0x01 / 0x04 / 0x06)
      that sit near a 0x1C0 read or near the selector, to find the 0x04-vs-0x06 branch.
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
SELECTOR = 0x205210
PRODUCER = 0x30F0C4
ROLL = 0x30FA34

DISP_TYPE = 0x1C0  # the evade-type byte
# accessory-evade candidate displacements (gaps around the 5 known evade bytes)
ACC_DISPS = [0x48, 0x49, 0x4C, 0x4D] + list(range(0x52, 0x60))
KNOWN_EVADE = {0x46: "weapon", 0x47: "weapon", 0x4A: "shield", 0x4B: "class", 0x4E: "shield"}


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

    print(f"exe={exe}\nimage_base=0x{base:X}\nREAL_MAX=0x{REAL_MAX:X}\n")

    def disasm_at(start, length=10):
        b = data[off(start): off(start) + length]
        return next(md.disasm(b, base + start), None)

    def window(rva, before, after, marks=None):
        """Disasm a clean window [rva-before, rva+after], align by stepping."""
        marks = marks or {}
        b = data[off(rva - before): off(rva) + after]
        out = []
        for ins in md.disasm(b, base + (rva - before)):
            r = ins.address - base
            if r > rva + after:
                break
            t = f"{ins.mnemonic} {ins.op_str}".strip()
            low = t.lower()
            mk = ""
            for needle, label in marks.items():
                if needle in low:
                    mk = "  " + label
                    break
            out.append(f"    {r:08X}: {hb(ins.bytes):<22} {t}{mk}")
        return out

    # ---------- helpers to test a disp32 == 0x1C0 little-endian ----------
    def is_disp32_1c0(b, i):
        # bytes at i..i+3 == C0 01 00 00
        return b[i] == 0xC0 and b[i + 1] == 0x01 and b[i + 2] == 0x00 and b[i + 3] == 0x00

    # ============================================================
    # (1) WRITES to +0x1C0
    # ============================================================
    print("=" * 70)
    print("(1) REAL-code WRITES to displacement +0x1C0 (evade-type byte)")
    print("=" * 70)
    write_hits = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 8):
            rva = va + i
            if rva >= REAL_MAX:
                break
            # optional REX (40..4F) then opcode
            j = i
            rex = None
            if 0x40 <= blob[j] <= 0x4F:
                rex = blob[j]
                j += 1
            op = blob[j]
            modrm = blob[j + 1] if j + 1 < n else 0
            mod = modrm >> 6
            rm = modrm & 0x07
            kind = None
            disp_at = None
            if op == 0x88:  # mov r/m8, r8
                if mod == 0x02 and rm != 0x04:  # disp32, no SIB
                    disp_at = j + 2
                    kind = "mov [r+d32],r8"
                elif mod == 0x02 and rm == 0x04:  # SIB then disp32
                    disp_at = j + 3
                    kind = "mov [sib+d32],r8"
            elif op == 0xC6:  # mov r/m8, imm8 (modrm.reg must be /0)
                if ((modrm >> 3) & 0x07) == 0:
                    if mod == 0x02 and rm != 0x04:
                        disp_at = j + 2
                        kind = "mov byte [r+d32],imm8"
                    elif mod == 0x02 and rm == 0x04:
                        disp_at = j + 3
                        kind = "mov byte [sib+d32],imm8"
            if kind and disp_at and disp_at + 4 <= n and is_disp32_1c0(blob, disp_at):
                write_hits.append((rva, kind, rex))
    write_hits = sorted(set((r, k) for r, k, _ in write_hits))
    for rva, kind in write_hits:
        ins = disasm_at(rva, 12)
        txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
        print(f"\n  0x{rva:X}: {txt}   [{kind}]")
        for line in window(rva, 0x20, 6, marks={
            "0x1c0": "<== +1C0 WRITE", "0x1be": "(+1BE)", "0x1c4": "(+1C4)",
            "0x1e5": "(+1E5)",
        }):
            print(line)
    print(f"\n  total +0x1C0 WRITE sites: {len(write_hits)}")

    # ============================================================
    # (2) READS of +0x1C0
    # ============================================================
    print("\n" + "=" * 70)
    print("(2) REAL-code READS of displacement +0x1C0 (who consumes the type)")
    print("=" * 70)
    read_hits = []
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 8):
            rva = va + i
            if rva >= REAL_MAX:
                break
            j = i
            if 0x40 <= blob[j] <= 0x4F:
                j += 1
            b0 = blob[j]
            kind = None
            disp_at = None
            if b0 == 0x0F and blob[j + 1] in (0xB6, 0xBE):  # movzx/movsx r32, r/m8
                modrm = blob[j + 2]
                mod = modrm >> 6
                rm = modrm & 0x07
                if mod == 0x02 and rm != 0x04:
                    disp_at = j + 3
                    kind = "movzx/movsx r32,[r+d32]"
            elif b0 == 0x8A:  # mov r8, r/m8
                modrm = blob[j + 1]
                mod = modrm >> 6
                rm = modrm & 0x07
                if mod == 0x02 and rm != 0x04:
                    disp_at = j + 2
                    kind = "mov r8,[r+d32]"
            elif b0 == 0x38 or b0 == 0x3A:  # cmp r/m8,r8 / cmp r8,r/m8
                modrm = blob[j + 1]
                mod = modrm >> 6
                rm = modrm & 0x07
                if mod == 0x02 and rm != 0x04:
                    disp_at = j + 2
                    kind = "cmp [r+d32] involving +1C0"
            elif b0 == 0x80:  # cmp/and/... byte [r/m],imm8 ; reg field selects op
                modrm = blob[j + 1]
                mod = modrm >> 6
                rm = modrm & 0x07
                if mod == 0x02 and rm != 0x04:
                    disp_at = j + 2
                    kind = "grp1 byte [r+d32],imm8"
            if kind and disp_at and disp_at + 4 <= n and is_disp32_1c0(blob, disp_at):
                read_hits.append((rva, kind))
    read_hits = sorted(set(read_hits))
    for rva, kind in read_hits:
        ins = disasm_at(rva, 12)
        txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
        print(f"\n  0x{rva:X}: {txt}   [{kind}]")
        for line in window(rva, 6, 0x24, marks={
            "0x1c0": "<== +1C0 READ", ", 1": "(=1?)", ", 4": "(=4?)", ", 6": "(=6?)",
            "jmp": "(jmp)", "je": "(je)", "jne": "(jne)",
        }):
            print(line)
    print(f"\n  total +0x1C0 READ sites: {len(read_hits)}")

    # ============================================================
    # (3) reads of accessory-evade candidate displacements
    # ============================================================
    print("\n" + "=" * 70)
    print("(3) REAL-code READS of accessory-evade candidate disps")
    print(f"    candidates: {[hex(d) for d in ACC_DISPS]}  (gaps around 0x46/47/4A/4B/4E)")
    print("=" * 70)
    acc_hits = {d: [] for d in ACC_DISPS}
    accset = set(ACC_DISPS)
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 5):
            rva = va + i
            if rva >= REAL_MAX:
                break
            j = i
            if 0x40 <= blob[j] <= 0x4F:
                j += 1
            b0 = blob[j]
            disp_at = None
            if b0 == 0x0F and blob[j + 1] in (0xB6, 0xBE):
                modrm = blob[j + 2]
                if (modrm >> 6) == 0x01 and (modrm & 0x07) not in (0x04, 0x05):
                    disp_at = j + 3
            elif b0 == 0x8A:
                modrm = blob[j + 1]
                if (modrm >> 6) == 0x01 and (modrm & 0x07) not in (0x04, 0x05):
                    disp_at = j + 2
            if disp_at is not None and disp_at < n:
                d = blob[disp_at]
                if d in accset:
                    acc_hits[d].append(rva)
    for d in ACC_DISPS:
        hs = sorted(set(acc_hits[d]))
        if not hs:
            continue
        print(f"\n  --- disp +0x{d:02X}: {len(hs)} read(s) ---")
        for rva in hs[:12]:
            start = rva - 1 if (data[off(rva) - 1] & 0xF0) == 0x40 else rva
            ins = disasm_at(start, 8)
            txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
            print(f"    0x{rva:X}: {txt}")
        if len(hs) > 12:
            print(f"    ... (+{len(hs) - 12} more)")
    total_acc = sum(len(set(v)) for v in acc_hits.values())
    print(f"\n  total accessory-disp reads: {total_acc}")

    # ============================================================
    # (4) the selector 0x205210 — dump the type switch / jump table
    # ============================================================
    print("\n" + "=" * 70)
    print(f"(4) SELECTOR 0x{SELECTOR:X} — wide dump to expose the cl(=+1C0) switch")
    print("=" * 70)
    b = data[off(SELECTOR): off(SELECTOR) + 0x420]
    for ins in md.disasm(b, base + SELECTOR):
        r = ins.address - base
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        low = t.lower()
        mk = ""
        if "0x1c0" in low:
            mk = "  <== +1C0"
        elif "0x1be" in low:
            mk = "  <== +1BE (dmg/evade switch)"
        elif "0x1c4" in low:
            mk = "  (+1C4 dmg)"
        elif "0x1e5" in low:
            mk = "  (+1E5)"
        elif ins.mnemonic in ("cmp", "sub") and any(
            x in low for x in (", 1", ", 2", ", 3", ", 4", ", 5", ", 6", ", 7")
        ):
            mk = "  *type-cmp?*"
        elif ins.mnemonic.startswith("j") and ins.mnemonic != "jmp":
            mk = "  (branch)"
        elif ins.mnemonic == "jmp" and "qword" in low:
            mk = "  <== JUMP TABLE"
        print(f"    {r:08X}: {hb(ins.bytes):<22} {t}{mk}")
        if r - SELECTOR > 0x400:
            break

    # ============================================================
    # (5) producer pre-roll window (context for 0x06 vs 0x04 "miss" branch)
    # ============================================================
    print("\n" + "=" * 70)
    print(f"(5) PRODUCER 0x{PRODUCER:X} around roll 0x{ROLL:X} (miss vs hit branch)")
    print("=" * 70)
    b = data[off(PRODUCER): off(PRODUCER) + 0x520]
    for ins in md.disasm(b, base + PRODUCER):
        r = ins.address - base
        t = f"{ins.mnemonic} {ins.op_str}".strip()
        low = t.lower()
        mk = ""
        # mark the call to the VM roll
        if ins.mnemonic in ("call", "jmp"):
            for opnd in ins.operands:
                if opnd.type == 3:  # imm
                    tgt = opnd.imm - base if opnd.imm > base else opnd.imm
            if "0x30fa34" in low or "0x150" in low:
                mk = "  <== VM ROLL"
        if "0x300" in low:
            mk = "  <== or 0x300 (stage hit)"
        elif "0x1c0" in low:
            mk = "  <== +1C0"
        elif "0x41" in low and ins.mnemonic == "mov":
            mk = "  (+0x41 rand scratch)"
        elif "0x4b" in low:
            mk = "  (+0x4B class evade)"
        if mk or ins.mnemonic in ("test", "je", "jne", "call"):
            print(f"    {r:08X}: {hb(ins.bytes):<22} {t}{mk}")
        if r - PRODUCER > 0x500:
            break

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
