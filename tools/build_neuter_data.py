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
    python tools/build_neuter_data.py --placeholder-mode sentinel-coarse-v1
    python tools/build_neuter_data.py --build-nxd
Outputs/prepares (repo data-mod source, deploy with deploy.ps1 after the NXD step):
    mod/fftivc.generic.chronicle/FFTIVC/tables/enhanced/ItemWeaponData.xml
    mod/fftivc.generic.chronicle/FFTIVC/tables/enhanced/AbilityChargeAimData.xml
    work/override_ability.neuter.sqlite
Then run the printed FF16Tools command to rebuild:
    mod/fftivc.generic.chronicle/FFTIVC/data/enhanced/nxd/overrideabilityactiondata.nxd

The default `uniform` mode is the live-proven neuter. `sentinel-coarse-v1` is an opt-in
calibration mode: it emits distinct low/mid/high placeholder magnitudes so the runtime can
decode `vanillaDamage` bands into action variables. That mode is intentionally coarse and must
be live-calibrated before use in a serious balance pass.
"""
from __future__ import annotations
import argparse
import shutil
import sqlite3
import subprocess
import sys
import xml.etree.ElementTree as ET
from contextlib import closing
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
MODLOADER = Path(r"C:/Reloaded-II/Mods/fftivc.utility.modloader/TableData")
ITEM_TEMPLATE = MODLOADER / "ItemData.xml"
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

PLACEHOLDER_MODES = ("uniform", "sentinel-coarse-v1")
SENTINEL_BAND_VALUES = {
    "low": 1,
    "mid": 4,
    "high": 7,
}

# Native instant-KO riders that can be replaced by DclInstantKoRule after their ordinary damage
# route is authored. Crystal/Bequeath Bacon is deliberately excluded: crystalization is a different
# corpse/campaign lifecycle and cannot be represented by lethal HP debit.
DCL_INSTANT_KO_ABILITY_IDS = (30, 137, 157, 183, 210, 262, 331, 344, 352)
# Every member retains an ordinary single-result HP carrier after InflictStatus is cleared.
# Dedicated KO, RandomFire, status-only, self/caster, and custom-formula records remain excluded.
DCL_STATUS_RIDER_ABILITY_IDS = (
    80, 82, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136,
    155, 156, 158, 159, 200, 202, 203, 204, 209, 211, 219, 284, 357,
)
# Support actions whose numeric result remains an independent apply carrier after only
# InflictStatus is cleared. Kept separate from the ordinary damage-rider allowlist because their
# eligibility and paired numeric semantics require a dedicated static proof.
DCL_SUPPORT_STATUS_RIDER_ABILITY_IDS = (252,)
# Formula 0x52 stages a victim result with Oil but a separate caster self-KO result without Oil.
# Its managed rule is therefore statically required to use dcl.isSelf == 0.
DCL_CONDITIONAL_STATUS_RIDER_ABILITY_IDS = (277,)
SENTINEL_WEAPON_CATEGORY_BANDS = {
    # GURPS-oriented first pass: blades/blunt hand weapons are swing/cut placeholders.
    "Knife": "low",
    "NinjaBlade": "low",
    "Sword": "low",
    "KnightSword": "low",
    "FellSword": "low",
    "Katana": "low",
    "Axe": "low",
    "Flail": "low",
    "Rod": "low",
    "Staff": "low",
    "Bag": "low",
    "Cloth": "low",
    # Thrust/reach/ranged weapons need a different placeholder family.
    "Pole": "mid",
    "Polearm": "mid",
    "Crossbow": "mid",
    "Bow": "mid",
    "Gun": "mid",
    "Book": "mid",
    "Instrument": "mid",
}
SENTINEL_AIM_CHARGE_BAND = "mid"


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
    parser.add_argument(
        "--placeholder-mode",
        choices=PLACEHOLDER_MODES,
        default="uniform",
        help=(
            "uniform keeps the proven Power/X/Y=1 neuter. sentinel-coarse-v1 emits distinct "
            "low/mid/high placeholder magnitudes for action-identity live calibration."
        ),
    )
    parser.add_argument(
        "--dcl-instant-ko-neuter",
        type=int,
        nargs="+",
        choices=DCL_INSTANT_KO_ABILITY_IDS,
        default=[],
        help=(
            "Ability ids whose native Dead rider is replaced by a harmless formula-0x08 placeholder "
            "and no InflictStatus. Enable each id only together with an authored DclInstantKoRule."
        ),
    )
    parser.add_argument(
        "--dcl-status-rider-neuter",
        type=int,
        nargs="+",
        choices=DCL_STATUS_RIDER_ABILITY_IDS,
        default=[],
        help=(
            "Ordinary single-result damage ability ids whose native status rider is removed with "
            "InflictStatus=0. Enable each id only with exact authored DclStatusRules."
        ),
    )
    parser.add_argument(
        "--dcl-support-status-rider-neuter",
        type=int,
        nargs="+",
        choices=DCL_SUPPORT_STATUS_RIDER_ABILITY_IDS,
        default=[],
        help=(
            "Statically approved support ability ids whose status rider is removed with "
            "InflictStatus=0 while their numeric result remains the apply carrier."
        ),
    )
    parser.add_argument(
        "--dcl-conditional-status-rider-neuter",
        type=int,
        nargs="+",
        choices=DCL_CONDITIONAL_STATUS_RIDER_ABILITY_IDS,
        default=[],
        help=(
            "Statically approved split-result ability ids whose native rider is removed while an "
            "exact managed target condition prevents it from leaking onto the caster result."
        ),
    )
    return parser.parse_args()


def load_weapon_categories_by_weapon_data_id() -> dict[int, str]:
    if not ITEM_TEMPLATE.exists():
        sys.exit(f"ERROR: item template not found: {ITEM_TEMPLATE}")
    tree = ET.parse(ITEM_TEMPLATE)
    root = tree.getroot()
    entries = root.find("Entries")
    if entries is None:
        sys.exit("ERROR: <Entries> not found in item template")

    categories: dict[int, str] = {}
    for item in entries.findall("Item"):
        flags = item.findtext("TypeFlags", "")
        if "Weapon" not in {part.strip() for part in flags.split(",")}:
            continue
        add_id_text = item.findtext("AdditionalDataId")
        category = item.findtext("ItemCategory", "None")
        if add_id_text is None:
            continue
        add_id = int(add_id_text)
        if add_id not in categories or categories[add_id] == "None":
            categories[add_id] = category
    return categories


def weapon_sentinel_band(weapon_data_id: int, categories_by_weapon_data_id: dict[int, str]) -> str:
    category = categories_by_weapon_data_id.get(weapon_data_id, "None")
    return SENTINEL_WEAPON_CATEGORY_BANDS.get(category, "low")


def weapon_placeholder_power(
    weapon_data_id: int,
    vanilla_power: int,
    categories_by_weapon_data_id: dict[int, str],
    placeholder_mode: str,
) -> int | None:
    if vanilla_power <= 0:
        return None
    if placeholder_mode == "uniform":
        return NEUTER_POWER if vanilla_power > NEUTER_POWER else None
    band = weapon_sentinel_band(weapon_data_id, categories_by_weapon_data_id)
    return SENTINEL_BAND_VALUES[band]


def charge_aim_placeholder_power(placeholder_mode: str) -> int:
    if placeholder_mode == "uniform":
        return NEUTER_CHARGE_AIM_POWER
    return SENTINEL_BAND_VALUES[SENTINEL_AIM_CHARGE_BAND]


def build_weapon_neuter(placeholder_mode: str = "uniform") -> tuple[str, int]:
    if not TEMPLATE.exists():
        sys.exit(f"ERROR: weapon template not found: {TEMPLATE}")
    tree = ET.parse(TEMPLATE)
    root = tree.getroot()
    entries = root.find("Entries")
    if entries is None:
        sys.exit("ERROR: <Entries> not found in weapon template")

    categories_by_weapon_data_id = load_weapon_categories_by_weapon_data_id() if placeholder_mode != "uniform" else {}
    edited: list[tuple[int, int, str]] = []
    for w in entries.findall("ItemWeapon"):
        wid_el = w.find("Id")
        pow_el = w.find("Power")
        if wid_el is None or pow_el is None:
            continue
        wid = int(wid_el.text)
        power = int(pow_el.text)
        target_power = weapon_placeholder_power(wid, power, categories_by_weapon_data_id, placeholder_mode)
        if target_power is None or power == target_power:
            continue
        edited.append((wid, target_power, weapon_sentinel_band(wid, categories_by_weapon_data_id)))

    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "",
        "<!--",
        "  GENERATED by tools/build_neuter_data.py - do not hand-edit.",
        "  Data-layer NEUTER PLACEHOLDER (docs/modding/06 section 1, docs/modding/07 Test 1b).",
        f"  Placeholder mode: {placeholder_mode}.",
        "  Uniform mode forces every weapon Power to 1 so vanilla physical attacks are tiny and non-lethal;",
        "  sentinel-coarse-v1 uses low/mid placeholder bands for action-identity calibration;",
        "  the code-mod reconciler owns the real damage result. Only <Id> + <Power> are shipped so",
        "  the loader merges per-property and nothing else is disturbed.",
        f"  Weapons emitted: {len(edited)}. Ability/spell/monster placeholders",
        "  are handled by sibling OverrideAbilityActionData NXD and AbilityChargeAimData XML neuters.",
        "-->",
        "",
        "<ItemWeaponTable>",
        "  <Version>1</Version>",
        "  <Entries>",
    ]
    for wid, target_power, _band in edited:
        lines.append("    <ItemWeapon>")
        lines.append(f"      <Id>{wid}</Id>")
        lines.append(f"      <Power>{target_power}</Power>")
        lines.append("    </ItemWeapon>")
    lines.append("  </Entries>")
    lines.append("</ItemWeaponTable>")
    lines.append("")
    return "\n".join(lines), len(edited)


def build_charge_aim_neuter(placeholder_mode: str = "uniform") -> tuple[str, int]:
    if not CHARGE_AIM_TEMPLATE.exists():
        sys.exit(f"ERROR: charge/aim template not found: {CHARGE_AIM_TEMPLATE}")
    tree = ET.parse(CHARGE_AIM_TEMPLATE)
    root = tree.getroot()
    entries = root.find("Entries")
    if entries is None:
        sys.exit("ERROR: <Entries> not found in charge/aim template")

    target_power = charge_aim_placeholder_power(placeholder_mode)
    edited = []
    for ability in entries.findall("AbilityChargeAim"):
        id_el = ability.find("Id")
        power_el = ability.find("Power")
        if id_el is None or power_el is None:
            continue
        ability_id = int(id_el.text)
        power = int(power_el.text)
        if power <= 0 or power == target_power:
            continue
        edited.append(ability_id)

    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "",
        "<!--",
        "  GENERATED by tools/build_neuter_data.py - do not hand-edit.",
        "  Data-layer NEUTER PLACEHOLDER for high-id Aim/Charge actions.",
        f"  Placeholder mode: {placeholder_mode}.",
        "  OverrideAbilityActionData only has rows through ability 367; Aim +2..+20 live in this",
        "  hardcoded secondary table instead. Force only Power so charge/aim attacks stay as",
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
        lines.append(f"      <Power>{target_power}</Power>")
        lines.append("    </AbilityChargeAim>")
    lines.append("  </Entries>")
    lines.append("</AbilityChargeAimTable>")
    lines.append("")
    return "\n".join(lines), len(edited)


def classify_damaging_ability_bands() -> dict[int, str]:
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
    ids: dict[int, str] = {}
    for a in entries.findall("Ability"):
        id_el = a.find("Id")
        fl_el = a.find("AIBehaviorFlags")
        if id_el is None or fl_el is None:
            continue
        flags = {f.strip() for f in (fl_el.text or "").split(",")}
        if "HP" in flags and "TargetEnemies" in flags and "TargetAllies" not in flags:
            ids[int(id_el.text)] = ability_sentinel_band(flags)
    return ids


def classify_damaging_abilities() -> list[int]:
    return sorted(classify_damaging_ability_bands())


def ability_sentinel_band(flags: set[str]) -> str:
    if "MagicalAttack" in flags or "AffectedByFaith" in flags or "Reflectable" in flags:
        return "high"
    if "PhysicalAttack" in flags or "Melee3Directions" in flags or "Ranged3Directions" in flags or "NonSpearAttack" in flags:
        return "mid"
    return "low"


def ability_placeholder_xy(ability_id: int, ability_bands: dict[int, str], placeholder_mode: str) -> int:
    if placeholder_mode == "uniform":
        return NEUTER_XY
    return SENTINEL_BAND_VALUES[ability_bands.get(ability_id, "low")]


def build_ability_neuter(
    placeholder_mode: str = "uniform",
    output_path: Path = NEUTER_OVERRIDE_SQLITE,
) -> tuple[int, int, list[int]]:
    """Copy the base override sqlite and force X=1,Y=1 on every damaging ability row that exists
    in the (sparse, 368-row) table. Returns (neutered, skipped_out_of_range, skipped_ids)."""
    if not BASE_OVERRIDE_SQLITE.exists():
        sys.exit(
            f"ERROR: base override sqlite not found: {BASE_OVERRIDE_SQLITE}\n"
            "Regenerate it with FF16Tools nxd-to-sqlite from the game's overrideabilityactiondata.nxd."
        )
    ability_bands = classify_damaging_ability_bands()
    damaging = set(ability_bands)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(BASE_OVERRIDE_SQLITE, output_path)
    con = sqlite3.connect(output_path)
    cur = con.cursor()
    present = {r[0] for r in cur.execute("SELECT Key FROM OverrideAbilityActionData")}
    to_neuter = sorted(damaging & present)
    skipped = sorted(damaging - present)  # ids beyond the 368-row table
    cur.executemany(
        "UPDATE OverrideAbilityActionData SET X=?, Y=? WHERE Key=?",
        [(xy := ability_placeholder_xy(k, ability_bands, placeholder_mode), xy, k) for k in to_neuter],
    )
    con.commit()
    con.close()
    return len(to_neuter), len(skipped), skipped


def apply_dcl_instant_ko_neuter(
    path: Path = NEUTER_OVERRIDE_SQLITE,
    ability_ids: tuple[int, ...] | list[int] = DCL_INSTANT_KO_ABILITY_IDS,
) -> int:
    """Remove native Dead delivery so the runtime 3d6 contest is the sole KO authority.

    Formula 0x08 guarantees an ordinary HP staging path into the proven pre-clamp hook. X/Y=1 keeps
    the native placeholder harmless; InflictStatus=0 removes the inherited Dead rider. The runtime
    then either zeroes/preserves authored ordinary damage on failure or supplies a lethal debit on
    success, letting native HP apply own the complete death lifecycle.
    """
    selected = tuple(dict.fromkeys(int(ability_id) for ability_id in ability_ids))
    unsupported = sorted(set(selected) - set(DCL_INSTANT_KO_ABILITY_IDS))
    if unsupported:
        raise ValueError(f"unsupported instant-KO ability ids: {unsupported}")
    with closing(sqlite3.connect(path)) as con:
        present = {int(row[0]) for row in con.execute("SELECT Key FROM OverrideAbilityActionData")}
        missing = sorted(set(selected) - present)
        if missing:
            raise ValueError(f"instant-KO ability ids missing from override table: {missing}")
        con.executemany(
            "UPDATE OverrideAbilityActionData SET Formula=8, X=1, Y=1, InflictStatus=0 WHERE Key=?",
            [(ability_id,) for ability_id in selected],
        )
        con.commit()
    return len(selected)


def apply_dcl_status_rider_neuter(
    path: Path = NEUTER_OVERRIDE_SQLITE,
    ability_ids: tuple[int, ...] | list[int] = DCL_STATUS_RIDER_ABILITY_IDS,
) -> int:
    """Remove a native status rider while preserving an ordinary HP result as the apply carrier.

    The allowlist is deliberately narrower than every action with status metadata. Status-only
    formulas can lose their only result when InflictStatus becomes zero; instant KO, RandomFire,
    self/caster, and custom carriers have separate ownership and are rejected here.
    """
    selected = tuple(dict.fromkeys(int(ability_id) for ability_id in ability_ids))
    unsupported = sorted(set(selected) - set(DCL_STATUS_RIDER_ABILITY_IDS))
    if unsupported:
        raise ValueError(f"unsupported ordinary damage status-rider ability ids: {unsupported}")
    with closing(sqlite3.connect(path)) as con:
        present = {int(row[0]) for row in con.execute("SELECT Key FROM OverrideAbilityActionData")}
        missing = sorted(set(selected) - present)
        if missing:
            raise ValueError(f"status-rider ability ids missing from override table: {missing}")
        con.executemany(
            "UPDATE OverrideAbilityActionData SET InflictStatus=0 WHERE Key=?",
            [(ability_id,) for ability_id in selected],
        )
        con.commit()
    return len(selected)


def apply_dcl_support_status_rider_neuter(
    path: Path = NEUTER_OVERRIDE_SQLITE,
    ability_ids: tuple[int, ...] | list[int] = DCL_SUPPORT_STATUS_RIDER_ABILITY_IDS,
) -> int:
    """Remove an allowlisted support rider while preserving its proven numeric result carrier."""
    selected = tuple(dict.fromkeys(int(ability_id) for ability_id in ability_ids))
    unsupported = sorted(set(selected) - set(DCL_SUPPORT_STATUS_RIDER_ABILITY_IDS))
    if unsupported:
        raise ValueError(f"unsupported support status-rider ability ids: {unsupported}")
    with closing(sqlite3.connect(path)) as con:
        present = {int(row[0]) for row in con.execute("SELECT Key FROM OverrideAbilityActionData")}
        missing = sorted(set(selected) - present)
        if missing:
            raise ValueError(f"support status-rider ability ids missing from override table: {missing}")
        con.executemany(
            "UPDATE OverrideAbilityActionData SET InflictStatus=0 WHERE Key=?",
            [(ability_id,) for ability_id in selected],
        )
        con.commit()
    return len(selected)


def apply_dcl_conditional_status_rider_neuter(
    path: Path = NEUTER_OVERRIDE_SQLITE,
    ability_ids: tuple[int, ...] | list[int] = DCL_CONDITIONAL_STATUS_RIDER_ABILITY_IDS,
) -> int:
    """Remove an allowlisted rider whose native formula has distinct victim/caster result branches."""
    selected = tuple(dict.fromkeys(int(ability_id) for ability_id in ability_ids))
    unsupported = sorted(set(selected) - set(DCL_CONDITIONAL_STATUS_RIDER_ABILITY_IDS))
    if unsupported:
        raise ValueError(f"unsupported conditional status-rider ability ids: {unsupported}")
    with closing(sqlite3.connect(path)) as con:
        present = {int(row[0]) for row in con.execute("SELECT Key FROM OverrideAbilityActionData")}
        missing = sorted(set(selected) - present)
        if missing:
            raise ValueError(f"conditional status-rider ability ids missing from override table: {missing}")
        con.executemany(
            "UPDATE OverrideAbilityActionData SET InflictStatus=0 WHERE Key=?",
            [(ability_id,) for ability_id in selected],
        )
        con.commit()
    return len(selected)


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
    print(f"[neuter] placeholder mode: {args.placeholder_mode}")
    xml, count = build_weapon_neuter(args.placeholder_mode)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(xml, encoding="utf-8")
    print(f"[neuter] wrote {OUT}")
    print(f"[neuter] weapons emitted: {count}")

    charge_xml, charge_count = build_charge_aim_neuter(args.placeholder_mode)
    CHARGE_AIM_OUT.parent.mkdir(parents=True, exist_ok=True)
    CHARGE_AIM_OUT.write_text(charge_xml, encoding="utf-8")
    print(f"[neuter] wrote {CHARGE_AIM_OUT}")
    print(f"[neuter] Aim/Charge abilities emitted: {charge_count}")

    neutered, skipped_n, skipped_ids = build_ability_neuter(args.placeholder_mode)
    instant_ko_neutered = apply_dcl_instant_ko_neuter(ability_ids=args.dcl_instant_ko_neuter) if args.dcl_instant_ko_neuter else 0
    status_riders_neutered = apply_dcl_status_rider_neuter(
        ability_ids=args.dcl_status_rider_neuter
    ) if args.dcl_status_rider_neuter else 0
    support_status_riders_neutered = apply_dcl_support_status_rider_neuter(
        ability_ids=args.dcl_support_status_rider_neuter
    ) if args.dcl_support_status_rider_neuter else 0
    conditional_status_riders_neutered = apply_dcl_conditional_status_rider_neuter(
        ability_ids=args.dcl_conditional_status_rider_neuter
    ) if args.dcl_conditional_status_rider_neuter else 0
    ABILITY_NXD_OUT.parent.mkdir(parents=True, exist_ok=True)
    print(f"[neuter] ability sqlite written: {NEUTER_OVERRIDE_SQLITE}")
    print(f"[neuter] damaging abilities emitted: {neutered} (skipped {skipped_n} out of table range: {skipped_ids})")
    if instant_ko_neutered:
        print(f"[neuter] DCL instant-KO native riders suppressed: {instant_ko_neutered}")
    if status_riders_neutered:
        print(f"[neuter] DCL ordinary damage status riders suppressed: {status_riders_neutered}")
    if support_status_riders_neutered:
        print(f"[neuter] DCL support status riders suppressed: {support_status_riders_neutered}")
    if conditional_status_riders_neutered:
        print(f"[neuter] DCL conditional status riders suppressed: {conditional_status_riders_neutered}")
    if args.build_nxd:
        build_ability_nxd(args.ff16tools)
    else:
        print("[neuter] NEXT: build the NXD with FF16Tools, e.g.:")
        print("  python tools/build_neuter_data.py --build-nxd")


if __name__ == "__main__":
    main()
