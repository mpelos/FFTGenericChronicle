#!/usr/bin/env python3
"""Offline RE: how the engine stores/applies/removes STATUS effects in the
0x200-byte unit battle struct, and whether status is controllable via memory.

Technique mirrors disasm_reaction_scan.py: byte-pattern scans over executable
section file-bytes (linear capstone sweep desyncs around data), then disasm a
clean window at each hit. Image base 0x140000000; real code RVA < 0x610000;
targets >= 0x610000 are Denuvo VM (.edata) -- note + stop.

KEY FINDINGS (see header report for the prose write-up):
  * Persistent status lives in the LOW block +0x60..+0x66; +0x61 is the master
    status byte. Confirmed +0x61 masks: 0x20=KO (anchor), 0x10 (control-flip),
    0x40 (removed-from-play sibling of KO), 0x08/0x04/0x01 (can't-act group),
    0x60/0x64 (composites). KO mirror at +0x1EF&0x20; team/charge 2-bit at
    +0x1EE bits4-5; transient action-flags dword at +0x1F8.
  * +0x61 is RECOMPUTED in real code at 0x30D42A-0x30D43C:
        cl = byte[+0x1EF]; cl &= 0xF2; byte[+0x1EF]=cl; cl |= byte[+0x57];
        byte[+0x61] = cl      <-- the mirror write
    i.e. +0x1EF (volatile) | +0x57 (innate/equip) -> +0x61 (effective).
  * The actual roll / per-stat apply / death are all Denuvo VM thunks:
        APPLY stat helper 0x30C114  -> jmp 0x150B88DF6 (VM)
        death fn          0x30C910  -> jmp 0x150BAEC80 (VM)
        avoidance roll    0x30FA34  -> jmp 0x150CFB562 (VM)
  * Producer 0x30F0C4 reads byte[+0x61]&0x04 (charge/defend status) BEFORE the
    VM avoidance roll; on hit it `or r12d,0x300`.
"""
from __future__ import annotations

from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_MEM, X86_OP_IMM

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]
REAL_MAX = 0x610000
IMG_BASE = 0x140000000

# real-code anchors of interest
ANCHORS = {
    "APPLY_30A51C": 0x30A51C,
    "PRECLAMP_30A66F": 0x30A66F,
    "SELECTOR_205210": 0x205210,
    "PRODUCER_30F0C4": 0x30F0C4,
    "AVOID_ROLL_30FA34": 0x30FA34,
    "STATAPPLY_30C114": 0x30C114,
    "DEATHFN_30C910": 0x30C910,
    "STATUS_RECOMPUTE_30D42A": 0x30D420,
}

# struct offsets we annotate
TAG = {0x2B: "Brave", 0x30: "HP", 0x57: "status-src(innate/equip)",
       0x61: "STATUS-effective", 0x62: "status2", 0x65: "status?",
       0x66: "status?", 0x1BB: "phase", 0x1BE: "staged-present",
       0x1C0: "evade-type", 0x1C4: "staged-dmg", 0x1C6: "staged-heal",
       0x1E5: "result-kind", 0x1EE: "team/charge(bits4-5)", 0x1EF: "STATUS-master",
       0x1F8: "action-flags(dword)"}


def hb(b: bytes) -> str:
    return " ".join(f"{x:02X}" for x in b)


def load():
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    base = pe.OPTIONAL_HEADER.ImageBase
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True
    exsecs = [(s.VirtualAddress, s.PointerToRawData, s.SizeOfRawData)
              for s in pe.sections if s.Characteristics & 0x20000000]
    return exe, pe, base, data, md, exsecs


def annotate(ins, base):
    m = []
    for op in ins.operands:
        if op.type == X86_OP_MEM and op.mem.base != 0 and op.mem.index == 0:
            if op.mem.disp in TAG:
                m.append(f"<<+0x{op.mem.disp:X} {TAG[op.mem.disp]}>>")
        if op.type == X86_OP_IMM and ins.mnemonic in (
                "or", "and", "xor", "test", "cmp", "bt", "bts", "btr"):
            m.append(f"m=0x{op.imm & 0xFFFFFFFF:X}")
    if ins.mnemonic in ("call", "jmp"):
        for op in ins.operands:
            if op.type == X86_OP_IMM:
                t = op.imm - base
                m.append(f"**VM->0x{t:X}**" if t >= REAL_MAX else f"->0x{t:X}")
    return "  " + " ".join(m) if m else ""


def dump_fn(md, data, pe, base, name, rva, length=0x120, maxins=70):
    print(f"\n========== {name} (0x{rva:X}) ==========")
    blob = data[pe.get_offset_from_rva(rva): pe.get_offset_from_rva(rva) + length]
    c = 0
    for ins in md.disasm(blob, base + rva):
        r = ins.address - base
        print(f"  {r:08X}: {hb(ins.bytes):<24} {ins.mnemonic} {ins.op_str}{annotate(ins, base)}")
        c += 1
        if c > maxins or (ins.mnemonic == "ret" and c > 6):
            break


def scan_disp_masks(md, data, base, exsecs, disp_t, label, lo=0x200000, hi=0x340000):
    """All battle-code instructions accessing [reg+disp_t] with their imm mask."""
    print(f"\n===== {label}: accesses to +0x{disp_t:X} (battle code 0x{lo:X}..0x{hi:X}) =====")
    seen, rows = set(), []
    OPS1 = (0x08, 0x09, 0x20, 0x21, 0x22, 0x23, 0x30, 0x31, 0x32, 0x33, 0x80,
            0x81, 0x83, 0x84, 0x85, 0x88, 0x89, 0x8A, 0x8B, 0xC6, 0xF6, 0xF7,
            0x38, 0x3A, 0x0A, 0x02)
    for va, praw, sz in exsecs:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 10):
            rva = va + i
            if rva >= REAL_MAX:
                break
            if not (lo <= rva < hi):
                if rva >= hi:
                    break
                continue
            j = i
            if blob[j] in (0x66, 0xF2, 0xF3):
                j += 1
            if 0x40 <= blob[j] <= 0x4F:
                j += 1
            op = blob[j]
            if op == 0x0F:
                modrm_i = j + 2
            elif op in OPS1:
                modrm_i = j + 1
            else:
                continue
            if modrm_i + 1 > n:
                continue
            modrm = blob[modrm_i]
            mod, rm = modrm >> 6, modrm & 7
            if rm in (4, 5):
                continue
            if mod == 1:
                disp = blob[modrm_i + 1] if modrm_i + 1 < n else -1
            elif mod == 2:
                disp = int.from_bytes(blob[modrm_i + 1:modrm_i + 5], "little") if modrm_i + 5 <= n else -1
            else:
                continue
            if disp != disp_t:
                continue
            ins = next(md.disasm(blob[i:i + 14], base + rva), None)
            if ins is None or rva in seen:
                continue
            if not any(o.type == X86_OP_MEM and o.mem.disp == disp_t and o.mem.index == 0
                       for o in ins.operands):
                continue
            seen.add(rva)
            imm = ""
            for o in ins.operands:
                if o.type == X86_OP_IMM and ins.mnemonic in ("test", "and", "or", "xor", "cmp"):
                    imm = f"  m=0x{o.imm & 0xFF:X}"
            rows.append((rva, f"{ins.mnemonic} {ins.op_str}{imm}"))
    for rva, t in sorted(rows):
        print(f"  0x{rva:X}: {t}")
    return rows


def main() -> int:
    exe, pe, base, data, md, exsecs = load()
    print(f"exe={exe}\nimage_base=0x{base:X}\nreal_max=0x{REAL_MAX:X}")
    for name, rva in ANCHORS.items():
        dump_fn(md, data, pe, base, name, rva)
    scan_disp_masks(md, data, base, exsecs, 0x61, "+0x61 STATUS-effective")
    scan_disp_masks(md, data, base, exsecs, 0x57, "+0x57 status-source")
    scan_disp_masks(md, data, base, exsecs, 0x1EE, "+0x1EE team/charge", hi=0x3C0000)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
