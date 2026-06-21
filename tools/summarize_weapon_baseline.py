#!/usr/bin/env python3
"""Summarize verified IVC weapon data by Generic Chronicle weapon family.

Inputs:
  - work/baseline_weapons.csv from tools/dump_weapons.py
  - work/item_catalog.csv from tools/dump_item_catalog.py
  - optional work/sim-inputs-v0.2.json for current design WP comparison

Outputs:
  - work/baseline_weapon_families.csv
  - work/baseline_weapon_summary.md
"""
from __future__ import annotations

import csv
import json
from pathlib import Path
from statistics import median

ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT / "work"
BASELINE_WEAPONS = WORK / "baseline_weapons.csv"
ITEM_CATALOG = WORK / "item_catalog.csv"
SIM_INPUTS = WORK / "sim-inputs-v0.2.json"
OUT_CSV = WORK / "baseline_weapon_families.csv"
OUT_MD = WORK / "baseline_weapon_summary.md"

CATEGORY_TO_FAMILY = {
    "Knife": "knife",
    "NinjaBlade": "ninja_blade",
    "Sword": "sword",
    "KnightSword": "knight_sword",
    "Katana": "katana",
    "Bow": "longbow",
    "Crossbow": "crossbow",
    "Gun": "gun",
    "Polearm": "spear",
    "Staff": "staff",
    "Rod": "rod",
    "Pole": "pole",
    "Axe": "axe",
    "Flail": "flail",
    "Instrument": "instrument",
    "Book": "book",
    "Cloth": "cloth_weapon",
    "Bag": "bag",
}


def read_csv(path: Path) -> list[dict[str, str]]:
    if not path.exists():
        raise SystemExit(f"missing input: {path}")
    with path.open(newline="", encoding="utf-8") as fh:
        return list(csv.DictReader(fh))


def read_int(row: dict[str, str], name: str, default: int = 0) -> int:
    try:
        return int(row.get(name, "") or default)
    except ValueError:
        return default


def split_csv_cell(text: str) -> set[str]:
    if not text or text == "None":
        return set()
    return {part.strip() for part in text.split(",") if part.strip()}


def sim_wp_by_family() -> dict[str, int]:
    if not SIM_INPUTS.exists():
        return {}
    data = json.loads(SIM_INPUTS.read_text(encoding="utf-8"))
    families = data.get("families", {})
    return {family: int(spec.get("wp", 0)) for family, spec in families.items()}


def summarize() -> int:
    weapons = read_csv(BASELINE_WEAPONS)
    catalog_rows = read_csv(ITEM_CATALOG)
    catalog = {read_int(row, "item_id", -1): row for row in catalog_rows}
    sim_wp = sim_wp_by_family()

    grouped: dict[str, list[tuple[dict[str, str], dict[str, str]]]] = {
        family: [] for family in CATEGORY_TO_FAMILY.values()
    }

    ignored: dict[str, int] = {}
    for weapon in weapons:
        item_id = read_int(weapon, "Id", -1)
        item = catalog.get(item_id, {})
        category = item.get("item_category", "")
        family = CATEGORY_TO_FAMILY.get(category)
        if family is None:
            ignored[category or "unknown"] = ignored.get(category or "unknown", 0) + 1
            continue
        grouped[family].append((weapon, item))

    rows: list[dict[str, str | int]] = []
    for family in sorted(grouped):
        entries = grouped[family]
        powers = [read_int(weapon, "Power") for weapon, _ in entries]
        formulas = sorted({weapon.get("Formula", "") for weapon, _ in entries if weapon.get("Formula", "")})
        ranges = sorted({weapon.get("Range", "") for weapon, _ in entries if weapon.get("Range", "")}, key=lambda x: int(x) if x.isdigit() else 999)
        flags = sorted(set().union(*(split_csv_cell(weapon.get("AttackFlags", "")) for weapon, _ in entries)))
        elements = sorted(set().union(*(split_csv_cell(weapon.get("Elements", "")) for weapon, _ in entries)))
        categories = sorted({item.get("item_category", "") for _, item in entries if item.get("item_category", "")})
        top_entries = sorted(
            entries,
            key=lambda pair: (read_int(pair[0], "Power"), read_int(pair[0], "Id")),
            reverse=True,
        )[:5]
        top_items = "; ".join(
            f"{item.get('name') or weapon.get('Name') or weapon.get('Id')}(WP{read_int(weapon, 'Power')},F{weapon.get('Formula', '')})"
            for weapon, item in top_entries
        )
        wp_max = max(powers) if powers else 0
        design_wp = sim_wp.get(family, 0)
        rows.append(
            {
                "family": family,
                "item_categories": "|".join(categories),
                "weapon_count": len(entries),
                "wp_min": min(powers) if powers else 0,
                "wp_median": int(median(powers)) if powers else 0,
                "wp_max": wp_max,
                "sim_v0_2_wp": design_wp,
                "sim_minus_verified_max": design_wp - wp_max if design_wp else "",
                "formula_ids": "|".join(formulas),
                "ranges": "|".join(ranges),
                "attack_flags": "|".join(flags),
                "elements": "|".join(elements),
                "top_items": top_items,
            }
        )

    WORK.mkdir(exist_ok=True)
    with OUT_CSV.open("w", newline="", encoding="utf-8") as fh:
        fieldnames = list(rows[0].keys()) if rows else []
        writer = csv.DictWriter(fh, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)

    lines = [
        "# Baseline Weapon Family Summary",
        "",
        "Source: `work/baseline_weapons.csv` joined with `work/item_catalog.csv`.",
        "",
        "This is verified local IVC table data. It does not prove vanilla formula behavior in",
        "battle, but it replaces the old missing-weapon-baseline state for weapon WP, range,",
        "flags, elements, and per-weapon formula ids.",
        "",
        "| Family | Count | WP min/med/max | Sim v0.2 WP | Formula IDs | Ranges | Top verified items |",
        "| --- | ---: | ---: | ---: | --- | --- | --- |",
    ]
    for row in rows:
        lines.append(
            f"| `{row['family']}` | {row['weapon_count']} | "
            f"{row['wp_min']}/{row['wp_median']}/{row['wp_max']} | "
            f"{row['sim_v0_2_wp']} | `{row['formula_ids']}` | `{row['ranges']}` | "
            f"{row['top_items']} |"
        )

    if ignored:
        ignored_text = ", ".join(f"{name or 'unknown'}={count}" for name, count in sorted(ignored.items()))
        lines.extend(["", f"Ignored non-design/extra categories: {ignored_text}."])

    OUT_MD.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {OUT_CSV} ({len(rows)} families)")
    print(f"wrote {OUT_MD}")
    return len(rows)


if __name__ == "__main__":
    summarize()
