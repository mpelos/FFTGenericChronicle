#!/usr/bin/env python3
"""
Dump the installed FFT Ivalice Chronicles item tables into one joined CSV.

The output is meant for battle-runtime reverse engineering: when the live unit probe sees small
integer candidates in the unknown equipment region, this catalog lets us quickly test whether
those values line up with real item ids or secondary item table ids.

Run from the project root:
    python tools/dump_item_catalog.py
"""
from __future__ import annotations

import argparse
import csv
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_TABLE_DIR = Path(r"C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData")
DEFAULT_OUT = ROOT / "work" / "item_catalog.csv"

COMMON_FIELDS = [
    "item_id",
    "name",
    "type_flags",
    "item_category",
    "required_level",
    "additional_data_id",
    "equip_bonus_id",
    "price",
    "shop_availability",
    "secondary_kind",
]

SECONDARY_FIELDS = [
    "weapon_range",
    "weapon_attack_flags",
    "weapon_formula",
    "weapon_power",
    "weapon_evasion",
    "weapon_elements",
    "weapon_options_ability_id",
    "armor_hp_bonus",
    "armor_mp_bonus",
    "shield_physical_evasion",
    "shield_magical_evasion",
    "accessory_physical_evasion",
    "accessory_magical_evasion",
]

BONUS_FIELDS = [
    "bonus_pa",
    "bonus_ma",
    "bonus_speed",
    "bonus_move",
    "bonus_jump",
    "bonus_innate_status",
    "bonus_immune_status",
    "bonus_starting_status",
    "bonus_absorb_elements",
    "bonus_nullify_elements",
    "bonus_halve_elements",
    "bonus_weak_elements",
    "bonus_strong_elements",
    "bonus_boost_jp",
]

FIELDNAMES = COMMON_FIELDS + SECONDARY_FIELDS + BONUS_FIELDS


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--table-dir", type=Path, default=DEFAULT_TABLE_DIR)
    parser.add_argument("-o", "--output", type=Path, default=DEFAULT_OUT)
    return parser.parse_args()


def parse_table(path: Path, row_tag: str) -> dict[int, dict[str, str]]:
    if not path.exists():
        raise SystemExit(f"missing table: {path}")

    parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
    root = ET.parse(path, parser=parser).getroot()
    entries = root.find("Entries")
    if entries is None:
        raise SystemExit(f"missing <Entries> in {path}")

    rows: dict[int, dict[str, str]] = {}
    for node in entries.findall(row_tag):
        row = row_from_node(node)
        try:
            row_id = int(row["Id"])
        except KeyError as exc:
            raise SystemExit(f"missing <Id> in {path} row {ET.tostring(node, encoding='unicode')[:120]}") from exc
        rows[row_id] = row

    return rows


def row_from_node(node: ET.Element) -> dict[str, str]:
    row: dict[str, str] = {}
    last_tag = ""
    for child in list(node):
        if child.tag is ET.Comment:
            if last_tag == "Id" and "Name" not in row:
                row["Name"] = english_name(child.text or "")
            continue

        if not isinstance(child.tag, str):
            continue

        row[child.tag] = (child.text or "").strip()
        last_tag = child.tag

    return row


def english_name(comment: str) -> str:
    # Comments are shaped like " Dagger / japanese / french / german ".
    return comment.strip().split("/", maxsplit=1)[0].strip()


def int_text(row: dict[str, str], key: str) -> str:
    value = row.get(key, "")
    return str(int(value)) if value else ""


def split_flags(text: str) -> set[str]:
    return {part.strip() for part in text.split(",") if part.strip()}


def secondary_kind(type_flags: str) -> str:
    flags = split_flags(type_flags)
    if "Weapon" in flags:
        return "weapon"
    if "Shield" in flags:
        return "shield"
    if "Armor" in flags or "Headgear" in flags:
        return "armor"
    if "Accessory" in flags:
        return "accessory"
    return ""


def merge_secondary(row: dict[str, str], out: dict[str, str], tables: dict[str, dict[int, dict[str, str]]]) -> None:
    kind = out["secondary_kind"]
    add_id = int(row.get("AdditionalDataId", "0") or "0")
    secondary = tables.get(kind, {}).get(add_id, {})

    if kind == "weapon":
        out.update(
            {
                "weapon_range": int_text(secondary, "Range"),
                "weapon_attack_flags": secondary.get("AttackFlags", ""),
                "weapon_formula": int_text(secondary, "Formula"),
                "weapon_power": int_text(secondary, "Power"),
                "weapon_evasion": int_text(secondary, "Evasion"),
                "weapon_elements": secondary.get("Elements", ""),
                "weapon_options_ability_id": int_text(secondary, "OptionsAbilityId"),
            }
        )
    elif kind == "armor":
        out.update(
            {
                "armor_hp_bonus": int_text(secondary, "HPBonus"),
                "armor_mp_bonus": int_text(secondary, "MPBonus"),
            }
        )
    elif kind == "shield":
        out.update(
            {
                "shield_physical_evasion": int_text(secondary, "PhysicalEvasion"),
                "shield_magical_evasion": int_text(secondary, "MagicalEvasion"),
            }
        )
    elif kind == "accessory":
        out.update(
            {
                "accessory_physical_evasion": int_text(secondary, "PhysicalEvasion"),
                "accessory_magical_evasion": int_text(secondary, "MagicalEvasion"),
            }
        )


def merge_bonus(row: dict[str, str], out: dict[str, str], bonuses: dict[int, dict[str, str]]) -> None:
    bonus_id = int(row.get("EquipBonusId", "0") or "0")
    bonus = bonuses.get(bonus_id, {})
    out.update(
        {
            "bonus_pa": int_text(bonus, "PABonus"),
            "bonus_ma": int_text(bonus, "MABonus"),
            "bonus_speed": int_text(bonus, "SpeedBonus"),
            "bonus_move": int_text(bonus, "MoveBonus"),
            "bonus_jump": int_text(bonus, "JumpBonus"),
            "bonus_innate_status": bonus.get("InnateStatus", ""),
            "bonus_immune_status": bonus.get("ImmuneStatus", ""),
            "bonus_starting_status": bonus.get("StartingStatus", ""),
            "bonus_absorb_elements": bonus.get("AbsorbElements", ""),
            "bonus_nullify_elements": bonus.get("NullifyElements", ""),
            "bonus_halve_elements": bonus.get("HalveElements", ""),
            "bonus_weak_elements": bonus.get("WeakElements", ""),
            "bonus_strong_elements": bonus.get("StrongElements", ""),
            "bonus_boost_jp": bonus.get("BoostJP", ""),
        }
    )


def build_catalog(table_dir: Path) -> list[dict[str, str]]:
    items = parse_table(table_dir / "ItemData.xml", "Item")
    tables = {
        "weapon": parse_table(table_dir / "ItemWeaponData.xml", "ItemWeapon"),
        "armor": parse_table(table_dir / "ItemArmorData.xml", "ItemArmor"),
        "shield": parse_table(table_dir / "ItemShieldData.xml", "ItemShield"),
        "accessory": parse_table(table_dir / "ItemAccessoryData.xml", "ItemAccessory"),
    }
    bonuses = parse_table(table_dir / "ItemEquipBonusData.xml", "ItemEquipBonus")

    rows: list[dict[str, str]] = []
    for item_id in sorted(items):
        item = items[item_id]
        out = {name: "" for name in FIELDNAMES}
        out.update(
            {
                "item_id": str(item_id),
                "name": item.get("Name", ""),
                "type_flags": item.get("TypeFlags", ""),
                "item_category": item.get("ItemCategory", ""),
                "required_level": int_text(item, "RequiredLevel"),
                "additional_data_id": int_text(item, "AdditionalDataId"),
                "equip_bonus_id": int_text(item, "EquipBonusId"),
                "price": int_text(item, "Price"),
                "shop_availability": item.get("ShopAvailability", ""),
                "secondary_kind": secondary_kind(item.get("TypeFlags", "")),
            }
        )
        merge_secondary(item, out, tables)
        merge_bonus(item, out, bonuses)
        rows.append(out)

    return rows


def write_csv(rows: Iterable[dict[str, str]], output: Path) -> None:
    output.parent.mkdir(exist_ok=True)
    with output.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, FIELDNAMES)
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    args = parse_args()
    rows = build_catalog(args.table_dir)
    write_csv(rows, args.output)
    counts: dict[str, int] = {}
    for row in rows:
        counts[row["secondary_kind"] or "other"] = counts.get(row["secondary_kind"] or "other", 0) + 1
    summary = ", ".join(f"{kind}={count}" for kind, count in sorted(counts.items()))
    print(f"wrote {args.output} ({len(rows)} items; {summary})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
