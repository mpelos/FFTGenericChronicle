#!/usr/bin/env python3
"""Byte-scan every REAL-code READ of the DEFENDER evade bytes and classify each.

Offsets on the unit struct (stride 0x200, table 0x141853CE0):
  +0x46 +0x47  weapon parry
  +0x4A +0x4E  shield block
  +0x4B        class evade
  (+0x48 +0x49 accessory for contrast)

Read encodings (each with optional REX 40..4F prefix), disp8 form (mod=01, rm!=4/5):
  movzx/movsx r32, byte [reg+disp8]   0F B6/BE  modrm  disp8
  mov  r8, byte [reg+disp8]           8A        modrm  disp8
  cmp  byte [reg+disp8], r8 / r8,[..]  38 / 3A   modrm  disp8
  cmp/test/and byte [reg+disp8], imm8  80        modrm  disp8 imm8
  test byte [reg+disp8], r8            84        modrm  disp8
Also disp32 forms (mod=10, disp = XX 00 00 00) caught.

For each hit: disasm a window; find enclosing function head (int3 pad / push rbp);
detect a unit-table loop; classify.
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
TARGET = {0x46: "weapon", 0x47: "weapon", 0x48: "accessory", 0x49: "accessory",
          0x4A: "shield", 0x4B: "class", 0x4E: "shield"}

pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
md = Cs(CS_ARCH_X86, CS_MODE_64); md.detail = True

def off(rva): return pe.get_offset_from_rva(rva)
def hb(b): return " ".join(f"{x:02X}" for x in b)

exsecs = []
for sec in pe.sections:
    if sec.Characteristics & 0x20000000:
        exsecs.append((sec.VirtualAddress, sec.PointerToRawData, sec.SizeOfRawData))

def disasm_at(start, length=12):
    b = data[off(start): off(start)+length]
    return next(md.disasm(b, base+start), None)

def find_func_head(rva, back=0x600):
    """Walk back to a plausible function head: first byte after int3 padding (CC),
    or a standard prologue. Returns best guess rva."""
    start = rva - back
    b = data[off(start): off(rva)]
    # find last run of CC (int3) padding before rva; head = first byte after it
    last_cc_end = None
    i = 0
    while i < len(b):
        if b[i] == 0xCC:
            j = i
            while j < len(b) and b[j] == 0xCC:
                j += 1
            last_cc_end = start + j
            i = j
        else:
            i += 1
    return last_cc_end if last_cc_end else start

def func_reads_table(head, span=0x800):
    """Does the function reference the unit table base or use stride 0x200 (imul/lea)?"""
    b = data[off(head): off(head)+span]
    txt = b
    # look for 0x141853CE0 as bytes (E0 3C 85 41 01 00 00 00) or stride 0x200 imm
    tb = UNIT_TABLE.to_bytes(8, "little")
    has_tb = tb[:4] in txt  # low dword E0 3C 85 41
    has_stride = b"\x00\x02\x00\x00" in txt  # 0x200 imm32
    return has_tb, has_stride

def window(rva, before, after, hi=None):
    hi = hi or []
    b = data[off(rva-before): off(rva)+after]
    out = []
    for ins in md.disasm(b, base+(rva-before)):
        r = ins.address - base
        if r > rva + after: break
        t = f"{ins.mnemonic} {ins.op_str}".strip(); low = t.lower(); mk = ""
        for d in TARGET:
            if f"0x{d:x}]" in low: mk = f"  <== +0x{d:02X} {TARGET[d]}"
        if not mk:
            if ins.mnemonic == "call": mk = "  (call)"
            elif ins.mnemonic in ("ret",): mk = "  --ret"
        out.append(f"      {r:08X}: {hb(ins.bytes):<22} {t}{mk}")
    return out

# ---- scan ----
hits = []  # (rva, disp, kind)
for va, praw, sz in exsecs:
    blob = data[praw: praw+sz]; n = len(blob)
    for i in range(n-8):
        rva = va + i
        if rva >= REAL_MAX: break
        j = i
        if 0x40 <= blob[j] <= 0x4F: j += 1
        b0 = blob[j]; kind = None; disp_at = None; disp32 = False
        if b0 == 0x0F and blob[j+1] in (0xB6, 0xBE):
            modrm = blob[j+2]; mod = modrm>>6; rm = modrm&7
            if mod == 1 and rm not in (4,5): disp_at=j+3; kind="movzx/movsx r,[r+d8]"
            elif mod == 1 and rm == 4: disp_at=j+4; kind="movzx [sib+d8]"
            elif mod == 2 and rm not in (4,5): disp_at=j+3; kind="movzx [r+d32]"; disp32=True
        elif b0 == 0x8A:
            modrm = blob[j+1]; mod=modrm>>6; rm=modrm&7
            if mod==1 and rm not in (4,5): disp_at=j+2; kind="mov r8,[r+d8]"
            elif mod==1 and rm==4: disp_at=j+3; kind="mov r8,[sib+d8]"
            elif mod==2 and rm not in (4,5): disp_at=j+2; kind="mov r8,[r+d32]"; disp32=True
        elif b0 in (0x38, 0x3A):
            modrm = blob[j+1]; mod=modrm>>6; rm=modrm&7
            if mod==1 and rm not in (4,5): disp_at=j+2; kind="cmp byte"
            elif mod==2 and rm not in (4,5): disp_at=j+2; kind="cmp byte d32"; disp32=True
        elif b0 == 0x84:
            modrm = blob[j+1]; mod=modrm>>6; rm=modrm&7
            if mod==1 and rm not in (4,5): disp_at=j+2; kind="test byte,r8"
        elif b0 == 0x80:
            modrm = blob[j+1]; mod=modrm>>6; rm=modrm&7
            if mod==1 and rm not in (4,5): disp_at=j+2; kind="grp1 byte,imm8"
            elif mod==2 and rm not in (4,5): disp_at=j+2; kind="grp1 byte,imm8 d32"; disp32=True
        if not kind or disp_at is None or disp_at >= n: continue
        if disp32:
            if disp_at+4 > n: continue
            if not (blob[disp_at+1]==0 and blob[disp_at+2]==0 and blob[disp_at+3]==0): continue
        d = blob[disp_at]
        if d in TARGET:
            hits.append((rva, d, kind))

hits = sorted(set(hits))
by_disp = {}
for rva, d, kind in hits:
    by_disp.setdefault(d, []).append((rva, kind))

print(f"exe={EXE.name}  READ sites total={len(hits)}\n")
for d in sorted(TARGET):
    lst = by_disp.get(d, [])
    print("="*74)
    print(f"+0x{d:02X} ({TARGET[d]}): {len(lst)} read site(s)")
    print("="*74)
    for rva, kind in lst:
        head = find_func_head(rva)
        has_tb, has_stride = func_reads_table(head)
        flags = []
        if has_tb: flags.append("TABLE-BASE-REF")
        if has_stride: flags.append("STRIDE0x200")
        ins = disasm_at(rva)
        txt = f"{ins.mnemonic} {ins.op_str}" if ins else "?"
        print(f"\n  0x{rva:X}  fn~0x{head:X}  [{kind}]  {' '.join(flags) if flags else '-'}")
        print(f"    {txt}")
        for line in window(rva, 0x14, 0x14):
            print(line)
