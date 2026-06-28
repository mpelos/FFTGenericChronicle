#!/usr/bin/env python3
"""Offline RE: WHERE is the displayed attack hit-% computed/stored, and can we
overwrite it (display-only)?

Built on the byte-pattern + clean-window technique from disasm_reaction_scan.py
(capstone linear sweep DESYNCS on this binary), extending find_forecast_writer.py.

Real code = .xcode (RVA 0x1000..0x611000, image base 0x140000000). Anything that
jmp/calls into RVA > 0x610000 is the Denuvo VM (a thunk) -> stop there.

Investigations:
  S1. Byte-scan real code for READS of the target evade displacements
      (+0x46,+0x47,+0x4A,+0x4B,+0x4E) and dump surrounding math (accuracy-evade?).
  S2. Byte-scan for the +0x1D8 "charge/forecast" word access.
  S3. Disasm key forecast/UI anchors: selector 0x205210, status exporter
      0x226EBC/0x226F39, and the 0x26Axxx forecast callers (resolve their call
      targets: real vs VM thunk).
  S4. Find readers/writers of the scratch buffer 0x7832E6 region (lea/mov rip-rel
      into [0x7832E6 .. 0x783400]) and classify.
  S5. Hunt for a "clamp to 0..100" store: scan real code for `mov [...], r/imm`
      immediately preceded by a cmp/min against 0x64 (100) -> a hit% formatter.
"""
from __future__ import annotations

from pathlib import Path

import pefile
from capstone import CS_ARCH_X86, CS_MODE_64, Cs
from capstone.x86 import X86_OP_IMM, X86_OP_MEM, X86_OP_REG

EXE_CANDIDATES = [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
]
REAL_MAX = 0x610000
IMAGE_BASE = 0x140000000

EVADE_DISPS = {0x46, 0x47, 0x4A, 0x4B, 0x4E}
SCRATCH_LO = 0x7832E6
SCRATCH_HI = 0x783400


def hb(b: bytes) -> str:
    return " ".join(f"{x:02X}" for x in b)


def modrm_ok_disp8(mod_byte: int) -> bool:
    mod = mod_byte >> 6
    rm = mod_byte & 0x07
    return mod == 0x01 and rm not in (0x04, 0x05)


def load():
    exe = next((p for p in EXE_CANDIDATES if p.exists()), None)
    if exe is None:
        raise SystemExit("exe not found")
    pe = pefile.PE(str(exe), fast_load=True)
    data = exe.read_bytes()
    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = True
    exsecs = []
    for sec in pe.sections:
        if sec.Characteristics & 0x20000000:
            exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))
    return exe, pe, data, md, exsecs


def real_exsecs(exsecs):
    # only the real-code section (.xcode) - first exec section, RVA < REAL_MAX
    return [(va, praw, sz) for (va, praw, sz) in exsecs if va < REAL_MAX]


def off(pe, rva):
    return pe.get_offset_from_rva(rva)


def disasm_window(md, data, pe, rva, back=0, forward=24):
    """Disasm a clean window starting `back` bytes before rva (after a REX guard)."""
    start = rva - back
    fo = off(pe, start)
    blob = data[fo: fo + back + forward]
    out = []
    for ins in md.disasm(blob, IMAGE_BASE + start):
        out.append(ins)
    return out


def is_vm(target_rva):
    return target_rva > REAL_MAX


def call_target(rva, b):
    """If b[0]==E8 (call rel32) or E9 (jmp rel32), return target RVA else None."""
    if not b:
        return None
    if b[0] in (0xE8, 0xE9):
        rel = int.from_bytes(b[1:5], "little", signed=True)
        return (rva + 5 + rel) & 0xFFFFFFFFFFFF
    return None


def main() -> int:
    exe, pe, data, md, exsecs = load()
    rsec = real_exsecs(exsecs)
    print(f"exe={exe}")
    print(f"image_base=0x{IMAGE_BASE:X}  real-code RVA < 0x{REAL_MAX:X}\n")

    # ===== S1: reads of evade displacements in real code =====
    print("=" * 78)
    print("S1. REAL-code reads of EVADE displacements +{0x46,0x47,0x4A,0x4B,0x4E}")
    print("=" * 78)
    hits = []
    for va, praw, sz in rsec:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 4):
            rva = va + i
            if rva >= REAL_MAX:
                break
            b0, b1, b2, b3 = blob[i], blob[i + 1], blob[i + 2], blob[i + 3]
            disp = None
            # 0F B6 /r d8 (movzx r32,r/m8) ; 0F BE /r d8 (movsx)
            if b0 == 0x0F and b1 in (0xB6, 0xBE) and modrm_ok_disp8(b2) and b3 in EVADE_DISPS:
                disp = b3
            # 8A /r d8 (mov r8, r/m8)
            elif b0 == 0x8A and modrm_ok_disp8(b1) and b2 in EVADE_DISPS:
                disp = b2
            # 0F B7 /r d8 (movzx r32, r/m16) - word read at that disp
            elif b0 == 0x0F and b1 == 0xB7 and modrm_ok_disp8(b2) and b3 in EVADE_DISPS:
                disp = b3
            if disp is not None:
                hits.append((rva, disp))
    hits = sorted(set(hits))
    for rva, disp in hits:
        # back up for a REX prefix to disasm cleanly
        prev = data[off(pe, rva) - 1]
        back = 1 if (prev & 0xF0) == 0x40 else 0
        ins = disasm_window(md, data, pe, rva, back=back, forward=8)
        txt = f"{ins[0].mnemonic} {ins[0].op_str}" if ins else "?"
        print(f"  0x{rva:06X}  +0x{disp:02X}  {txt}")
    print(f"  total evade-read sites: {len(hits)}")

    # For each evade-read site, dump a +/- window to see the accuracy-evade math
    print("\n--- context windows around each evade-read site ---")
    for rva, disp in hits:
        print(f"\n  ### site 0x{rva:06X} (+0x{disp:02X}) ###")
        win_back = 0x20
        fo = off(pe, rva - win_back)
        blob = data[fo: fo + win_back + 0x30]
        for ins in md.disasm(blob, IMAGE_BASE + (rva - win_back)):
            r = ins.address - IMAGE_BASE
            if r > rva + 0x28:
                break
            t = f"{ins.mnemonic} {ins.op_str}"
            low = t.lower()
            mark = ""
            if any(f"0x{d:x}]" in low for d in EVADE_DISPS) and ("ptr [" in low):
                mark = "  <== EVADE"
            elif "0x64" in low:
                mark = "  (=100)"
            elif ins.mnemonic in ("sub", "imul", "mul", "idiv", "div"):
                mark = "  *arith*"
            tgt = call_target(r, ins.bytes)
            if tgt is not None:
                mark += f"  -> 0x{tgt:X}{' [VM]' if is_vm(tgt) else ' [real]'}"
            print(f"    {r:06X}: {hb(ins.bytes):<22} {t}{mark}")

    # ===== S2: +0x1D8 forecast/charge word =====
    print("\n" + "=" * 78)
    print("S2. REAL-code accesses to +0x1D8 (charge/forecast word)")
    print("=" * 78)
    d = 0x1D8
    d_le = d.to_bytes(4, "little")
    s2 = []
    for va, praw, sz in rsec:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 7):
            rva = va + i
            if rva >= REAL_MAX:
                break
            # disp32 forms: modrm mod==10 (disp32), look for the 4-byte disp == 0x1D8
            # generic: scan for the little-endian disp bytes preceded by a plausible modrm
            if blob[i + 3:i + 7] == d_le and (blob[i + 2] >> 6) == 0x02 and (blob[i + 2] & 7) not in (4, 5):
                s2.append(rva)
    s2 = sorted(set(s2))
    qw = sum(1 for rva in s2 if data[off(pe, rva)-2] in (0x48, 0x4C, 0x49) or (data[off(pe, rva)-1] in (0x8B, 0x89)))
    print(f"  total +0x1D8 disp32 sites: {len(s2)} (most are qword ptr/object slots, not a 0..100 forecast)")

    # ===== S3: disasm the key forecast/UI anchors, resolving call targets =====
    print("\n" + "=" * 78)
    print("S3. Forecast/UI anchors (selector / status exporter / 0x26Axxx callers)")
    print("=" * 78)
    anchors = {
        "selector 0x205210 (result/anim)": (0x205210, 0x140),
        "status exporter head 0x226EBC": (0x226EBC, 0xC0),
        "status exporter 0x226F39": (0x226F39, 0x40),
        "forecast caller 0x26A600": (0x26A600, 0x320),
    }
    for label, (a, length) in anchors.items():
        print(f"\n  --- {label} ---")
        fo = off(pe, a)
        blob = data[fo: fo + length]
        for ins in md.disasm(blob, IMAGE_BASE + a):
            r = ins.address - IMAGE_BASE
            t = f"{ins.mnemonic} {ins.op_str}"
            low = t.lower()
            mark = ""
            tgt = call_target(r, ins.bytes)
            if tgt is not None:
                mark = f"  -> 0x{tgt:X}{' [VM]' if is_vm(tgt) else ' [real]'}"
            elif "0x64" in low:
                mark = "  (=100)"
            elif any(f"0x{d:x}]" in low for d in (0x1BE, 0x1C0, 0x1C4, 0x1E5)):
                mark = "  <== record field"
            print(f"    {r:06X}: {hb(ins.bytes):<22} {t}{mark}")

    # ===== S4: readers/writers of the scratch buffer 0x7832E6.. region =====
    print("\n" + "=" * 78)
    print(f"S4. RIP-relative refs into scratch [0x{SCRATCH_LO:X}..0x{SCRATCH_HI:X})")
    print("=" * 78)
    # lea/mov reg,[rip+disp] and mov [rip+disp],reg: instr forms with modrm rm==101 (RIP)
    # We brute-force: for every position, try to decode and check any mem operand RIP target in range.
    s4 = []
    for va, praw, sz in rsec:
        blob = data[praw: praw + sz]
        # disasm in chunks won't desync-proof; instead pattern: 48 8D 05 / 8B 05 / 89 05 / 0F B7 05 etc are common.
        # Simpler: scan for the 4-byte LE of any target in range as a disp following a RIP modrm (rm=101).
        for i in range(len(blob) - 7):
            rva = va + i
            if rva >= REAL_MAX:
                break
            modrm = blob[i]
            # crude: look for modrm byte with mod==00 rm==101 then disp32 (RIP+disp32)
            # but we don't know instr length; instead decode a window when disp matches
            disp = int.from_bytes(blob[i + 1:i + 5], "little", signed=True)
            # candidate target if THIS were a 7-byte rip instr ending at i+5 (e.g. lea r,[rip+d] = 48 8D 05 d)
            # try a few common ending offsets
            for instr_len in (7, 6):
                end = rva - 0 + instr_len  # next-instr addr if instruction starts somewhere; approximate via decode below
            # Defer to decode at a few likely starts
    # Robust approach: walk the section trying capstone at each byte that looks like a rip-rel opcode start
    rip_op_starts = {0x48: None, 0x4C: None, 0x8B: None, 0x89: None, 0x0F: None, 0x66: None, 0xC7: None, 0x88: None, 0x8A: None}
    seen = set()
    for va, praw, sz in rsec:
        blob = data[praw: praw + sz]
        for i in range(len(blob) - 1):
            if blob[i] not in rip_op_starts:
                continue
            rva = va + i
            if rva >= REAL_MAX:
                break
            ins = next(md.disasm(blob[i:i + 10], IMAGE_BASE + rva), None)
            if ins is None or not ins.operands:
                continue
            for op in ins.operands:
                if op.type == X86_OP_MEM and op.mem.base == 0 and op.mem.index == 0:
                    # RIP-relative: target = next_instr + disp
                    tgt = (rva + ins.size + op.mem.disp) & 0xFFFFFFFFFFFF
                    if SCRATCH_LO <= tgt < SCRATCH_HI and rva not in seen:
                        seen.add(rva)
                        s4.append((rva, tgt, f"{ins.mnemonic} {ins.op_str}"))
    for rva, tgt, t in sorted(s4):
        print(f"  0x{rva:06X} -> 0x{tgt:X}: {t}")
    print(f"  total scratch refs: {len(s4)}")

    # ===== S5: clamp-to-100 then store (a hit% formatter) =====
    print("\n" + "=" * 78)
    print("S5. 'min/cmp vs 0x64 (100)' followed by a STORE  (hit% formatter hunt)")
    print("=" * 78)
    # Heuristic byte scan: find `cmp r, 0x64` (83 F8 64 etc) or `mov r,0x64`/`cmovg`,
    # then within next 12 bytes a store to memory (mov [..],r). Report windows.
    s5 = 0
    for va, praw, sz in rsec:
        blob = data[praw: praw + sz]
        n = len(blob)
        for i in range(n - 16):
            rva = va + i
            if rva >= REAL_MAX:
                break
            # cmp r32, 0x64 : 83 /7 ib  -> 83 F8..FF 64 ; also cmovle/cmovg pattern; and direct 'mov r,100; cmp'
            is_cmp100 = (blob[i] == 0x83 and (blob[i + 1] & 0xF8) == 0xF8 and blob[i + 2] == 0x64)
            if not is_cmp100:
                continue
            # look ahead for a memory store of a 16/32-bit reg
            win = blob[i:i + 18]
            ins_list = list(md.disasm(win, IMAGE_BASE + rva))
            has_store = False
            store_txt = ""
            for ins in ins_list[1:6]:
                if ins.mnemonic.startswith("mov") and "ptr [" in ins.op_str and ins.op_str.split(",")[0].strip().endswith("]"):
                    has_store = True
                    store_txt = f"{ins.mnemonic} {ins.op_str}"
                    break
            if has_store:
                s5 += 1
                if s5 <= 40:
                    print(f"  0x{rva:06X}: cmp ...,0x64 -> {store_txt}")
    print(f"  total cmp-100->store windows: {s5}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
