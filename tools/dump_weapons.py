#!/usr/bin/env python3
"""
Dump the weapon baseline for Generic Chronicle formula-balance work.

Outputs:
  - work/baseline_weapons.csv : every weapon (ItemWeapon x128) with its data-editable fields,
                                including the per-weapon Formula id (the R1 lever).

Why this matters: the formula-balance docs (docs/formula-balance/02-variable-palette.md and
03-family-taxonomy-and-viability.md) need the REAL ItemWeaponData values - especially the
per-weapon `Formula` field - to (a) confirm R1 (is the weapon base routine reassignable in
data?) and (b) ground each family's WP/range/element/flags in verified numbers instead of the
WotL/FFHacktics working catalog.

Schema (from work/battle_data_inventory.md, ItemWeaponData.xml, ItemWeapon x128):
  Id, Range, AttackFlags, Formula, Unused_0x03, Power, Evasion, Elements, OptionsAbilityId
  AttackFlags vocab: Arc, Direct, ForcedTwoHands, Lunging, Striking, Throwable, TwoHands, TwoSwords
  Elements vocab:    Fire, Holy, Ice, Lightning, Water, Wind

NOTE: this reads the mod loader's TableData XML on the WINDOWS game machine. It does NOT run on
the Linux checkout (the game / Reloaded-II / TableData live on Windows). Run it from the project
root on the machine that has the game:
    python tools/dump_weapons.py
Override WEAPONDATA_XML below if your install path differs.
"""
from __future__ import annotations

import csv
import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT / "work"

# Input (override if your setup differs) - same TableData dir dump_baseline.py uses.
WEAPONDATA_XML = Path(r"C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData\ItemWeaponData.xml")

# Known fields in declaration order (see schema above). Unused_0x03 kept for completeness.
WEAPON_FIELDS = [
    "Range",
    "AttackFlags",
    "Formula",        # <-- the R1 lever: per-weapon base routine selector
    "Unused_0x03",
    "Power",          # WP
    "Evasion",
    "Elements",
    "OptionsAbilityId",
]

# <Id>5</Id> <!-- Coral Sword / ... -->  -> capture id and English name (first comment segment)
ID_NAME_RE = re.compile(r"<Id>(\d+)</Id>\s*(?:<!--\s*([^/|]+?)\s*[/|])?")


def dump_weapons() -> int:
    if not WEAPONDATA_XML.exists():
        raise SystemExit(
            f"ItemWeaponData.xml not found at:\n  {WEAPONDATA_XML}\n"
            "Edit WEAPONDATA_XML in this script to point at your mod loader's TableData dir, "
            "and run on the Windows machine that has the game."
        )

    raw = WEAPONDATA_XML.read_text(encoding="utf-8")
    id_to_name = {int(m.group(1)): (m.group(2) or "").strip() for m in ID_NAME_RE.finditer(raw)}

    tree = ET.parse(WEAPONDATA_XML)
    root = tree.getroot()

    # Auto-discover any extra fields present beyond the known set, so nothing is silently dropped.
    discovered: list[str] = []
    for w in root.iter("ItemWeapon"):
        for child in w:
            tag = child.tag
            if tag not in ("Id",) and tag not in WEAPON_FIELDS and tag not in discovered:
                discovered.append(tag)
    fields = WEAPON_FIELDS + discovered
    if discovered:
        print(f"note: extra fields not in known schema, appended: {discovered}")

    WORK.mkdir(exist_ok=True)
    out = WORK / "baseline_weapons.csv"
    n = 0
    with out.open("w", newline="", encoding="utf-8") as fh:
        wr = csv.writer(fh)
        wr.writerow(["Id", "Name"] + fields)
        for w in root.iter("ItemWeapon"):
            wid = w.findtext("Id")
            row = [wid, id_to_name.get(int(wid), "") if wid and wid.isdigit() else ""]
            for f in fields:
                row.append(w.findtext(f, ""))
            wr.writerow(row)
            n += 1
    print(f"wrote {out}  ({n} weapons)")

    # Quick R1 signal: how many distinct Formula ids appear across weapons.
    formulas = sorted({(w.findtext("Formula") or "").strip() for w in root.iter("ItemWeapon")} - {""})
    print(f"distinct weapon Formula ids present ({len(formulas)}): {formulas}")
    return n


if __name__ == "__main__":
    dump_weapons()
