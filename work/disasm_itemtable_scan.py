#!/usr/bin/env python3
"""Verify ItemData table at RVA 0x80EA90 / 0x67F910 (stride 0xC) against item_catalog.csv,
and brute-force scan the exe image for the weapon/shield/armor/accessory sub-tables.

Known from real-code disasm:
  fn 0x2B8CB8: ItemData row = base + (id<0x100 ? 0x80EA90 : 0x67F910) + id*0xC
    row+5 = category (cmp 0x13), row+7 = weapon-type (idx into 0x80FEA0 stride 0x1A table)
  fn 0x286D04 field usage:
    weapon row (fn 0x2B8CE8):    +4 = weapon power, +5 = weapon evade
    shield row (fn 0x2B8D48):    +0 = phys evade,   +1 = magic evade
    armor  row (fn 0x2B8DB0):    +0 = hp bonus,     +1 = mp bonus
    accessory row (fn 0x2B8E14): +0 = phys evade,   +1 = magic evade
Sub-tables indexed (hypothesis) by additional_data_id from the catalog.
"""
from __future__ import annotations
from pathlib import Path
import csv
import pefile

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
CSV = Path(r"D:/Projects/FFTGenericChronicle/work/item_catalog.csv")

pe = pefile.PE(str(EXE), fast_load=True)
base = pe.OPTIONAL_HEADER.ImageBase
data = EXE.read_bytes()
def off(rva): return pe.get_offset_from_rva(rva)

rows = list(csv.DictReader(CSV.open(encoding="utf-8-sig")))
def ival(r, k):
    v = r.get(k, "").strip()
    return int(v) if v else None

# ---------- 1) dump ItemData rows (file-time) for sample items ----------
print("### ItemData table check (stride 0xC) ###")
for tbl, lo, hi in (("low  0x80EA90", 0x80EA90, 0x100), ("high 0x67F910", 0x67F910, None)):
    pass
def item_row_rva(iid):
    return (0x80EA90 if iid < 0x100 else 0x67F910) + iid * 0xC

samples = [0, 1, 2, 3, 0x49, 0x6E, 0x100, 0x101]
for iid in samples:
    rva = item_row_rva(iid)
    b = data[off(rva): off(rva)+12]
    r = rows[iid] if iid < len(rows) else None
    nm = r["name"] if r else "?"
    print(f"  id {iid:>4} rva 0x{rva:X}: {' '.join(f'{x:02X}' for x in b)}   csv: {nm}"
          f" cat={r['item_category'] if r else '?'} lvl={ival(r,'required_level') if r else '?'}"
          f" add_id={ival(r,'additional_data_id') if r else '?'}")

# ---------- 2) brute-force sub-table scan ----------
SCAN_LO = pe.get_offset_from_rva(0x611000)
SCAN_HI = pe.get_offset_from_rva(0x1800000)

def find_table(pairs, offs, name, strides=range(1, 0x40), max_idx=None):
    """pairs: list of (idx, byte_at_off0, byte_at_off1) with offs=(o0,o1).
    Find (stride, file_base_rva) such that for all: data[base + idx*stride + o0] == v0 etc.
    Scan restricted to rva 0x611000..0x1800000 (real data sections near known tables)."""
    o0, o1 = offs
    anchors = pairs[:2]
    rest = pairs[2:]
    n = len(data)
    results = []
    (i0, a0, b0), (i1, a1, b1) = anchors
    target0 = bytes([a0]) if b0 is None else bytes([a0, b0])
    # collect anchor positions once
    positions = []
    startpos = SCAN_LO
    while True:
        p = data.find(target0, startpos, SCAN_HI)
        if p < 0: break
        positions.append(p)
        startpos = p + 1
    for s in strides:
        for p in positions:
            fb = p - i0 * s - o0  # file offset of table base
            if fb < 0: continue
            q = fb + i1 * s
            if q + o1 + 1 >= n: continue
            if data[q + o0] != a1: continue
            if b1 is not None and data[q + o1] != b1: continue
            ok = True
            for (ii, va, vb) in rest:
                qq = fb + ii * s
                if qq + o1 + 1 >= n or data[qq + o0] != va or (vb is not None and data[qq + o1] != vb):
                    ok = False; break
            if ok:
                results.append((s, fb))
    out = []
    for s, fb in sorted(set(results)):
        # map file offset back to rva
        rva = None
        for sec in pe.sections:
            if sec.PointerToRawData <= fb < sec.PointerToRawData + sec.SizeOfRawData:
                rva = fb - sec.PointerToRawData + sec.VirtualAddress
                secn = sec.Name.rstrip(b"\x00").decode()
                break
        out.append((s, fb, rva, secn if rva else "?"))
    print(f"\n### {name}: {len(out)} candidate table(s) (checked {len(pairs)} constraints) ###")
    for s, fb, rva, secn in out:
        print(f"  stride 0x{s:X}  file_off 0x{fb:X}  rva 0x{rva:X} ({secn})  va 0x{base+rva:X}")
    return out

# Build constraint sets from CSV keyed by additional_data_id
weap, shld, accs, armr = [], [], [], []
seen_w, seen_s, seen_a, seen_r = set(), set(), set(), set()
for r in rows:
    aid = ival(r, "additional_data_id")
    if aid is None: continue
    kind = r["secondary_kind"].strip()
    if kind == "weapon":
        wp, wev = ival(r, "weapon_power"), ival(r, "weapon_evasion")
        if wp is not None and wev is not None and aid not in seen_w:
            seen_w.add(aid); weap.append((aid, wp, wev))
    elif kind == "shield":
        pe_, me = ival(r, "shield_physical_evasion"), ival(r, "shield_magical_evasion")
        if pe_ is not None and me is not None and aid not in seen_s:
            seen_s.add(aid); shld.append((aid, pe_, me))
    elif kind == "accessory":
        pe_, me = ival(r, "accessory_physical_evasion"), ival(r, "accessory_magical_evasion")
        if pe_ is not None and me is not None and aid not in seen_a:
            seen_a.add(aid); accs.append((aid, pe_, me))
    elif kind == "armor":
        hp, mp = ival(r, "armor_hp_bonus"), ival(r, "armor_mp_bonus")
        if hp is not None and mp is not None and hp < 256 and mp < 256 and aid not in seen_r:
            seen_r.add(aid); armr.append((aid, hp, mp))

print(f"\nconstraints: weapon={len(weap)} shield={len(shld)} accessory={len(accs)} armor={len(armr)}")
print("shield sample:", shld[:6])
print("weapon sample:", weap[:6])

find_table(weap, (4, 5), "WEAPON table (row+4=WP, row+5=WEv)")
find_table(shld, (0, 1), "SHIELD table (row+0=PhysEv, row+1=MagEv)")
find_table(accs, (0, 1), "ACCESSORY table (row+0=PhysEv, row+1=MagEv)")
find_table(armr, (0, 1), "ARMOR table (row+0=HP, row+1=MP)")
