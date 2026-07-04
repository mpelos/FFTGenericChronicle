#!/usr/bin/env python3
"""Peek regions around the ItemData tables for the weapon/shield/armor/accessory sub-tables.
Try matching: Dagger add_id=1 WP=3 WEv=5; Mythril Knife add_id=2 WP=4 WEv=5; and shields."""
from pathlib import Path
import pefile, csv

EXE = next(p for p in [
    Path(r"D:/SteamLibrary/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
    Path(r"C:/Program Files (x86)/Steam/steamapps/common/FINAL FANTASY TACTICS - The Ivalice Chronicles/FFT_enhanced.exe"),
] if p.exists())
pe = pefile.PE(str(EXE), fast_load=True)
data = EXE.read_bytes()
def off(rva): return pe.get_offset_from_rva(rva)

CSV = Path(r"D:/Projects/FFTGenericChronicle/work/item_catalog.csv")
rows = list(csv.DictReader(CSV.open(encoding="utf-8-sig")))
def ival(r, k):
    v = r.get(k, "").strip()
    return int(v) if v else None

print("weapons (aid, WP, WEv):", [(ival(r,'additional_data_id'), ival(r,'weapon_power'), ival(r,'weapon_evasion'))
      for r in rows if r['secondary_kind']=='weapon'][:8])
print("shields (aid, PEv, MEv):", [(ival(r,'additional_data_id'), ival(r,'shield_physical_evasion'), ival(r,'shield_magical_evasion'))
      for r in rows if r['secondary_kind']=='shield'][:8])
print("accessories:", [(ival(r,'additional_data_id'), ival(r,'accessory_physical_evasion'), ival(r,'accessory_magical_evasion'))
      for r in rows if r['secondary_kind']=='accessory'][:8])
print("armor:", [(ival(r,'additional_data_id'), ival(r,'armor_hp_bonus'), ival(r,'armor_mp_bonus'))
      for r in rows if r['secondary_kind']=='armor'][:8])
print()

def dump(rva, n, label, width=16):
    b = data[off(rva): off(rva)+n]
    print(f"--- {label} rva=0x{rva:X} ---")
    for i in range(0, n, width):
        print(f"  0x{rva+i:X}: {' '.join(f'{x:02X}' for x in b[i:i+width])}")
    print()

dump(0x80F690, 0x200, "gap after ItemData LOW (0x80F690)")
dump(0x810120, 0x300, "after weapon-type table (0x810120)")
dump(0x80E900, 0x190, "just before ItemData LOW")
