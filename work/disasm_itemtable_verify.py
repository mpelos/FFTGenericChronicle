#!/usr/bin/env python3
"""Verify the found item sub-tables:
  weapon    rva 0x80F690 stride 8 (+4 WP, +5 WEv)
  shield    rva 0x80FA90 stride 2 (+0 phys, +1 magic)
  armor     rva 0x80FAB0 stride 2 (+0 HP, +1 MP)
  accessory rva 0x80FB30 stride 2 (+0 phys, +1 magic)
1) dump shield table with CSV names; 2) cross-check high-id weapons;
3) search whole file for duplicate copies (loader source blobs)."""
from pathlib import Path
import pefile, csv

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
pe = pefile.PE(str(EXE), fast_load=True)
data = EXE.read_bytes()
def off(rva): return pe.get_offset_from_rva(rva)
def rva_of_off(fo):
    for s in pe.sections:
        if s.PointerToRawData <= fo < s.PointerToRawData + s.SizeOfRawData:
            return fo - s.PointerToRawData + s.VirtualAddress, s.Name.rstrip(b"\x00").decode()
    return None, "?"

CSV = Path(r"D:/Projects/FFTGenericChronicle/work/item_catalog.csv")
rows = list(csv.DictReader(CSV.open(encoding="utf-8-sig")))
def ival(r, k):
    v = r.get(k, "").strip()
    return int(v) if v else 0

W, S, R, A = 0x80F690, 0x80FA90, 0x80FAB0, 0x80FB30

print("### SHIELD table dump (rva 0x80FA90, stride 2) ###")
shields = [r for r in rows if r["secondary_kind"] == "shield"]
for r in shields:
    aid = ival(r, "additional_data_id")
    b = data[off(S + aid*2): off(S + aid*2) + 2]
    csvv = (ival(r, "shield_physical_evasion"), ival(r, "shield_magical_evasion"))
    mark = "OK" if (b[0], b[1]) == csvv else "MISMATCH"
    star = "  <== 50% phys" if b[0] == 0x32 else ""
    print(f"  aid {aid:>2} @0x{S+aid*2:X}: phys={b[0]:>3} magic={b[1]:>3}  csv={csvv} {mark}  {r['name']}{star}")

print("\n### HIGH-ID weapon cross-check (ids 256..260) ###")
for iid in range(256, 261):
    if iid >= len(rows): break
    r = rows[iid]
    aid = ival(r, "additional_data_id")
    b = data[off(W + aid*8): off(W + aid*8) + 8]
    print(f"  id {iid} {r['name']}: aid={aid} row={' '.join(f'{x:02X}' for x in b)} "
          f"csv WP={ival(r,'weapon_power')} WEv={ival(r,'weapon_evasion')} "
          f"range={ival(r,'weapon_range')} formula={r['weapon_formula']}")

print("\n### weapon table row layout probe (first 12 rows + CSV) ###")
weaps = {ival(r, "additional_data_id"): r for r in rows if r["secondary_kind"] == "weapon"}
for aid in range(12):
    b = data[off(W + aid*8): off(W + aid*8) + 8]
    r = weaps.get(aid)
    ex = (f"WP={ival(r,'weapon_power')} WEv={ival(r,'weapon_evasion')} rng={ival(r,'weapon_range')} "
          f"form={r['weapon_formula']} elem={r['weapon_elements']} opt={ival(r,'weapon_options_ability_id')} [{r['name']}]" if r else "")
    print(f"  aid {aid:>2}: {' '.join(f'{x:02X}' for x in b)}   {ex}")

print("\n### duplicate-copy search (whole file) ###")
for name, rva, ln in (("weapon", W, 0x400), ("shield", S, 0x20), ("armor", R, 0x80), ("accessory", A, 0x40)):
    needle = data[off(rva): off(rva)+ln]
    hits = []
    p = 0
    while True:
        p = data.find(needle, p)
        if p < 0: break
        hits.append(p); p += 1
    locs = []
    for h in hits:
        rv, sec = rva_of_off(h)
        locs.append(f"file_off 0x{h:X} (rva 0x{rv:X} {sec})" if rv else f"file_off 0x{h:X} (no section)")
    print(f"  {name} ({ln} bytes): {len(hits)} occurrence(s)")
    for l in locs: print(f"      {l}")

print("\n### accessory table dump ###")
accs = [r for r in rows if r["secondary_kind"] == "accessory"]
for r in accs[:34]:
    aid = ival(r, "additional_data_id")
    b = data[off(A + aid*2): off(A + aid*2) + 2]
    csvv = (ival(r, "accessory_physical_evasion"), ival(r, "accessory_magical_evasion"))
    mark = "OK" if (b[0], b[1]) == csvv else "MISMATCH"
    print(f"  aid {aid:>2} @0x{A+aid*2:X}: phys={b[0]:>3} magic={b[1]:>3}  csv={csvv} {mark}  {r['name']}")
