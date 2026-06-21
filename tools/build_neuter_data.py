#!/usr/bin/env python3
"""Build the data-layer "neuter placeholder" for the Generic Chronicle battle runtime.

Why this exists (see docs/modding/06 section 1 and docs/modding/07 Test 1b):
The reactive HP reconciler in the code mod cannot PREVENT death - the engine fires the
death state the instant vanilla damage brings HP to 0, before our poll wakes. The fix is
to make vanilla damage harmless and predictable so the engine never kills / never shows a
wrong number, and our C# engine owns the real outcome. Test D proved the data lever works
(the exe reads OverrideAbilityActionData Formula/X/Y, and ItemWeaponData Power).

This script emits both halves of the neuter placeholder:
  1. ItemWeaponData XML: every weapon Power is forced to 1, so weapon-power attacks
     (PA*WP, WP*WP, (PA+Sp)/2*WP, ...) deal a tiny, non-lethal, non-zero delta.
  2. OverrideAbilityActionData sqlite/NXD source: damaging offensive abilities are detected
     from AbilityData AIBehaviorFlags (HP + TargetEnemies, not TargetAllies) and get X=Y=1.
  3. AbilityChargeAimData XML: Aim/Charge secondary Power is forced to 1 so high-id
     Aim actions outside OverrideAbilityActionData cannot scale back up through their
     hardcoded side table.

Coverage / gaps (documented honestly):
  - Covers: every attack that scales with weapon Power (most human physical attacks).
  - Covers: spells/skills/monster actions whose ability id is present in the 368-row
    OverrideAbilityActionData override table and whose magnitude reads X or Y.
  - Covers: Aim/Charge high-id actions whose magnitude reads AbilityChargeAimData Power.
  - Does NOT cover: rare formulas that ignore X/Y, WP, and charge/aim Power (for example
    %-damage/Gravity), or any action family that never produces a weapon-power,
    ability-parameter, or charge/aim placeholder delta.

Usage:
    python tools/build_neuter_data.py
    python tools/build_neuter_data.py --build-nxd
Outputs/prepares (repo data-mod source, deploy with deploy.ps1 after the NXD step):
    mod/fftivc.generic.chronicle/FFTIVC/tables/enhanced/ItemWeaponData.xml
    mod/fftivc.generic.chronicle/FFTIVC/tables/enhanced/AbilityChargeAimData.xml
    work/override_ability.neuter.sqlite
Then run the printed FF16Tools command to rebuild:
    mod/fftivc.generic.chronicle/FFTIVC/data/enhanced/nxd/overrideabilityactiondata.nxd
"""
from __future__ import annotations
import argparse
import shutil
import sqlite3
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
MODLOADER = Path(r"C:/Reloaded-II/Mods/fftivc.utility.modloader/TableData")
TEMPLATE = MODLOADER / "ItemWeaponData.xml"
ABILITY_TEMPLATE = MODLOADER / "AbilityData.xml"
CHARGE_AIM_TEMPLATE = MODLOADER / "AbilityChargeAimData.xml"
OUT = REPO / "mod/fftivc.generic.chronicle/FFTIVC/tables/enhanced/ItemWeaponData.xml"
CHARGE_AIM_OUT = REPO / "mod/fftivc.generic.chronicle/FFTIVC/tables/enhanced/AbilityChargeAimData.xml"

# Ability (spell/skill/monster) neuter via the OverrideAbilityActionData NXD.
BASE_OVERRIDE_SQLITE = REPO / "work/override_ability.sqlite"          # extracted base (sparse, -1=inherit)
NEUTER_OVERRIDE_SQLITE = REPO / "work/override_ability.neuter.sqlite"  # base + X=1,Y=1 on damaging rows
ABILITY_NXD_OUT = REPO / "mod/fftivc.generic.chronicle/FFTIVC/data/enhanced/nxd/overrideabilityactiondata.nxd"
DEFAULT_FF16TOOLS = Path(r"D:/Projects/FFTModNewGame++/tools/FF16Tools.CLI-1.13.2-win-x64/win-x64/FF16Tools.CLI.exe")

NEUTER_POWER = 1  # tiny but non-zero so the reconciler still observes a delta
NEUTER_XY = 1     # X/Y forced to 1 on damaging abilities -> damage collapses to ~one stat (non-lethal)
NEUTER_CHARGE_AIM_POWER = 1


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build Generic Chronicle data-layer neuter placeholder artifacts.")
    parser.add_argument(
        "--build-nxd",
        action="store_true",
        help="Also rebuild overrideabilityactiondata.nxd from work/override_ability.neuter.sqlite using FF16Tools.",
    )
    parser.add_argument(
        "--ff16tools",
        type=Path,
        default=DEFAULT_FF16TOOLS,
        help=f"Path to FF16Tools.CLI.exe. Default: {DEFAULT_FF16TOOLS}",
    )
    return parser.parse_args()


def build_weapon_neuter() -> tuple[str, int]:
    if not TEMPLATE.exists():
        sys.exit(f"ERROR: weapon template not found: {TEMPLATE}")
    tree = ET.parse(TEMPLATE)
    root = tree.getroot()
    entries = root.find("Entries")
    if entries is None:
        sys.exit("ERROR: <Entries> not found in weapon template")

    edited = []
    for w in entries.findall("ItemWeapon"):
        wid_el = w.find("Id")
        pow_el = w.find("Power")
        if wid_el is None or pow_el is None:
            continue
        wid = int(wid_el.text)
        power = int(pow_el.text)
        if power <= NEUTER_POWER:
            continue  # already tiny / "nothing equipped" (Power 0) - leave as inherited
        edited.append(wid)

    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "",
        "<!--",
        "  GENERATED by tools/build_neuter_data.py - do not hand-edit.",
        "  Data-layer NEUTER PLACEHOLDER (docs/modding/06 section 1, docs/modding/07 Test 1b).",
        "  Every weapon Power is forced to 1 so vanilla physical attacks are tiny and non-lethal;",
        "  the code-mod reconciler owns the real damage result. Only <Id> + <Power> are shipped so",
        "  the loader merges per-property and nothing else is disturbed.",
        f"  Weapons neutered: {len(edited)} (Power>1 in vanilla). Ability/spell/monster placeholders",
        "  are handled by sibling OverrideAbilityActionData NXD and AbilityChargeAimData XML neuters.",
        "-->",
        "",
        "<ItemWeaponTable>",
        "  <Version>1</Version>",
        "  <Entries>",
    ]
    for wid in edited:
        lines.append("    <ItemWeapon>")
        lines.append(f"      <Id>{wid}</Id>")
        lines.append(f"      <Power>{NEUTER_POWER}</Power>")
        lines.append("    </ItemWeapon>")
    lines.append("  </Entries>")
    lines.append("</ItemWeaponTable>")
    lines.append("")
    return "\n".join(lines), len(edited)


def build_charge_aim_neuter() -> tuple[str, int]:
    if not CHARGE_AIM_TEMPLATE.exists():
        sys.exit(f"ERROR: charge/aim template not found: {CHARGE_AIM_TEMPLATE}")
    tree = ET.parse(CHARGE_AIM_TEMPLATE)
    root = tree.getroot()
    entries = root.find("Entries")
    if entries is None:
        sys.exit("ERROR: <Entries> not found in charge/aim template")

    edited = []
    for ability in entries.findall("AbilityChargeAim"):
        id_el = ability.find("Id")
        power_el = ability.find("Power")
        if id_el is None or power_el is None:
            continue
        ability_id = int(id_el.text)
        power = int(power_el.text)
        if power <= NEUTER_CHARGE_AIM_POWER:
            continue
        edited.append(ability_id)

    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "",
        "<!--",
        "  GENERATED by tools/build_neuter_data.py - do not hand-edit.",
        "  Data-layer NEUTER PLACEHOLDER for high-id Aim/Charge actions.",
        "  OverrideAbilityActionData only has rows through ability 367; Aim +2..+20 live in this",
        "  hardcoded secondary table instead. Force only Power=1 so charge/aim attacks stay as",
        "  placeholder deltas while keeping CT/Ticks inherited from vanilla.",
        f"  Aim/Charge abilities neutered: {len(edited)} (Power>1 in vanilla).",
        "-->",
        "",
        "<AbilityChargeAimTable>",
        "  <Version>1</Version>",
        "  <Entries>",
    ]
    for ability_id in edited:
        lines.append("    <AbilityChargeAim>")
        lines.append(f"      <Id>{ability_id}</Id>")
        lines.append(f"      <Power>{NEUTER_CHARGE_AIM_POWER}</Power>")
        lines.append("    </AbilityChargeAim>")
    lines.append("  </Entries>")
    lines.append("</AbilityChargeAimTable>")
    lines.append("")
    return "\n".join(lines), len(edited)


def classify_damaging_abilities() -> list[int]:
    """Offensive HP-damage abilities: AIBehaviorFlags has HP + TargetEnemies, not TargetAllies.

    This is the set that can kill a unit with vanilla damage (Fire/Bolt/Ice lines, monster
    skills, etc.) - exactly what the weapon neuter does NOT cover. Heals (Cure = TargetAllies)
    are correctly excluded.
    """
    if not ABILITY_TEMPLATE.exists():
        sys.exit(f"ERROR: ability template not found: {ABILITY_TEMPLATE}")
    tree = ET.parse(ABILITY_TEMPLATE)
    root = tree.getroot()
    entries = root.find("Entries")
    ids = []
    for a in entries.findall("Ability"):
        id_el = a.find("Id")
        fl_el = a.find("AIBehaviorFlags")
        if id_el is None or fl_el is None:
            continue
        flags = {f.strip() for f in (fl_el.text or "").split(",")}
        if "HP" in flags and "TargetEnemies" in flags and "TargetAllies" not in flags:
            ids.append(int(id_el.text))
    return ids


def build_ability_neuter() -> tuple[int, int, list[int]]:
    """Copy the base override sqlite and force X=1,Y=1 on every damaging ability row that exists
    in the (sparse, 368-row) table. Returns (neutered, skipped_out_of_range, skipped_ids)."""
    if not BASE_OVERRIDE_SQLITE.exists():
        sys.exit(
            f"ERROR: base override sqlite not found: {BASE_OVERRIDE_SQLITE}\n"
            "Regenerate it with FF16Tools nxd-to-sqlite from the game's overrideabilityactiondata.nxd."
        )
    damaging = set(classify_damaging_abilities())
    shutil.copyfile(BASE_OVERRIDE_SQLITE, NEUTER_OVERRIDE_SQLITE)
    con = sqlite3.connect(NEUTER_OVERRIDE_SQLITE)
    cur = con.cursor()
    present = {r[0] for r in cur.execute("SELECT Key FROM OverrideAbilityActionData")}
    to_neuter = sorted(damaging & present)
    skipped = sorted(damaging - present)  # ids beyond the 368-row table
    cur.executemany(
        "UPDATE OverrideAbilityActionData SET X=?, Y=? WHERE Key=?",
        [(NEUTER_XY, NEUTER_XY, k) for k in to_neuter],
    )
    con.commit()
    con.close()
    return len(to_neuter), len(skipped), skipped


def ff16tools_command(ff16tools: Path) -> list[str]:
    return [
        str(ff16tools),
        "sqlite-to-nxd",
        "-i",
        str(NEUTER_OVERRIDE_SQLITE),
        "-o",
        str(ABILITY_NXD_OUT.parent),
        "-g",
        "fft",
        "-t",
        "OverrideAbilityActionData",
    ]


def build_ability_nxd(ff16tools: Path) -> None:
    if not ff16tools.exists():
        sys.exit(f"ERROR: FF16Tools CLI not found: {ff16tools}")
    if not NEUTER_OVERRIDE_SQLITE.exists():
        sys.exit(f"ERROR: neuter override sqlite not found: {NEUTER_OVERRIDE_SQLITE}")

    ABILITY_NXD_OUT.parent.mkdir(parents=True, exist_ok=True)
    cmd = ff16tools_command(ff16tools)
    print("[neuter] building ability NXD:", flush=True)
    print("  " + " ".join(f'"{part}"' if " " in part else part for part in cmd), flush=True)
    subprocess.run(cmd, check=True)
    if not ABILITY_NXD_OUT.exists() or ABILITY_NXD_OUT.stat().st_size == 0:
        sys.exit(f"ERROR: FF16Tools did not produce a non-empty NXD: {ABILITY_NXD_OUT}")
    print(f"[neuter] ability NXD written: {ABILITY_NXD_OUT}")


def main() -> None:
    args = parse_args()
    xml, count = build_weapon_neuter()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(xml, encoding="utf-8")
    print(f"[neuter] wrote {OUT}")
    print(f"[neuter] weapons neutered to Power={NEUTER_POWER}: {count}")

    charge_xml, charge_count = build_charge_aim_neuter()
    CHARGE_AIM_OUT.parent.mkdir(parents=True, exist_ok=True)
    CHARGE_AIM_OUT.write_text(charge_xml, encoding="utf-8")
    print(f"[neuter] wrote {CHARGE_AIM_OUT}")
    print(f"[neuter] Aim/Charge abilities neutered to Power={NEUTER_CHARGE_AIM_POWER}: {charge_count}")

    neutered, skipped_n, skipped_ids = build_ability_neuter()
    ABILITY_NXD_OUT.parent.mkdir(parents=True, exist_ok=True)
    print(f"[neuter] ability sqlite written: {NEUTER_OVERRIDE_SQLITE}")
    print(f"[neuter] damaging abilities set X=Y={NEUTER_XY}: {neutered} (skipped {skipped_n} out of table range: {skipped_ids})")
    if args.build_nxd:
        build_ability_nxd(args.ff16tools)
    else:
        print("[neuter] NEXT: build the NXD with FF16Tools, e.g.:")
        print("  python tools/build_neuter_data.py --build-nxd")


if __name__ == "__main__":
    main()
