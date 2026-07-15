#!/usr/bin/env python3
"""Build a structurally complete DCL sidecar for every IVC item record.

The source catalog describes native data.  This sidecar adds the DCL-owned fields that the native
tables do not carry (damage type, Weight, armor profile, weapon-family identity, and authoring
gates).  Blank numeric DCL fields are intentional and reported as incomplete calibration; this tool
must never turn a relative design tier into a made-up final value.
"""
from __future__ import annotations

import argparse
import csv
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CATALOG = ROOT / "work" / "item_catalog.csv"


@dataclass(frozen=True)
class WeaponPolicy:
    role: str
    damage_type: str
    wmod_tier: str
    reach_policy: str
    handedness: str
    parry_tier: str
    skill_primary: str
    overcap_route: str = "none"
    armor_divisor_tier: str = "none"
    bolt_role: str = "none"
    special_rule: str = "none"


W = WeaponPolicy
WEAPON_POLICIES: dict[str, WeaponPolicy] = {
    "Knife": W("assassin_finisher", "thrust", "low", "1", "1H", "low", "PA", special_rule="modest_speed_grant"),
    "NinjaBlade": W("light_dual_wield_blade", "cut", "low-medium", "1", "1H", "medium", "PA"),
    "Sword": W("defensive_1h_blade", "cut", "medium", "1", "1H", "high", "PA"),
    "Katana": W("draw_out_2h_blade", "cut", "high", "1", "2H", "high", "PA", special_rule="draw_out_repeatable_mp_cost"),
    "KnightSword": W("brute_2h_blade", "cut", "very-high", "1", "2H", "medium", "PA"),
    "Axe": W("anti_plate_brute", "crush", "very-high", "1", "1H", "low", "PA"),
    "Flail": W("anti_guard_crusher", "crush", "high", "1", "1H", "very-low", "PA", special_rule="defender_parry_minus4_block_minus2"),
    "Bag": W("job_utility_platform", "crush", "low", "1", "1H", "low", "PA", special_rule="job_buff_or_debuff_pending"),
    "Polearm": W("offensive_reach", "thrust", "medium", "2", "2H", "medium", "PA", special_rule="reach_escape_counter_pointblank"),
    "Pole": W("defensive_reach", "crush", "low-medium", "2", "2H", "very-high", "PA", special_rule="reach_escape_counter_pointblank"),
    "Rod": W("offensive_magic_implement", "magic", "n/a", "3", "1H", "none", "MA", bolt_role="offensive_elemental_bolt", special_rule="offensive_magic_modifier"),
    "Staff": W("support_magic_implement", "magic", "n/a", "3", "1H", "none", "MA", bolt_role="support_elemental_or_heal_bolt", special_rule="support_heal_modifier"),
    "Bow": W("arc_strength_ranged", "missile", "medium", "native-projectile", "2H", "none", "PA", armor_divisor_tier="low", special_rule="arc_trajectory"),
    "Crossbow": W("marksman_direct_ranged", "missile", "medium", "native-projectile", "2H", "none", "weapon-skill", overcap_route="raw-damage", armor_divisor_tier="medium", special_rule="straight_line"),
    "Gun": W("armor_defeater_ranged", "missile", "medium-low", "native-projectile", "2H", "none", "weapon-skill", overcap_route="penetration", armor_divisor_tier="high", special_rule="straight_line"),
    "Instrument": W("bard_utility_platform", "crush", "very-low", "1", "2H", "low", "PA", special_rule="song_identity_job_authored"),
    "Cloth": W("dancer_utility_platform", "crush", "very-low", "2", "1H", "low", "PA", special_rule="dance_identity_job_authored"),
    "Book": W("orator_utility_platform", "crush", "very-low", "1", "1H", "low", "PA", special_rule="talk_identity_job_authored"),
}

BODY_POLICIES = {
    "Armor": ("body", "heavy", "9/8/3"),
    "Clothing": ("body", "light", "2/2/2"),
    "Robe": ("body", "robe", "0/0/0"),
}
HEAD_CATEGORIES = {"Helmet", "Hat", "HairAdornment"}
ACCESSORY_CATEGORIES = {"Armguard", "Armlet", "Cloak", "Perfume", "Ring", "Shoes"}
EXTERNAL_WEAPON_CATEGORIES = {"Throwing", "Bomb"}


FIELDS = [
    "item_id", "name", "category", "secondary_kind", "route", "slot", "family_role",
    "damage_type", "damage_type_confidence", "wmod_tier", "current_weapon_power", "dcl_wmod",
    "reach_policy", "current_range", "dcl_range", "handedness", "parry_tier",
    "current_weapon_evasion", "dcl_parry", "skill_primary", "overcap_route",
    "armor_divisor_tier", "dcl_armor_divisor", "armor_class", "dr_profile_cut_thrust_crush",
    "dcl_dr_cut", "dcl_dr_thrust", "dcl_dr_crush", "weight_required", "dcl_weight",
    "bolt_role", "current_elements", "dcl_element", "block_source", "dcl_block",
    "special_rule", "readiness", "authoring_gates",
]


def empty_row(source: dict[str, str]) -> dict[str, str]:
    row = {field: "" for field in FIELDS}
    row.update({
        "item_id": source["item_id"],
        "name": source["name"],
        "category": source["item_category"],
        "secondary_kind": source["secondary_kind"],
        "current_weapon_power": source["weapon_power"],
        "current_range": source["weapon_range"],
        "current_weapon_evasion": source["weapon_evasion"],
        "current_elements": source["weapon_elements"],
        "damage_type_confidence": "n/a",
        "weight_required": "false",
        "bolt_role": "none",
        "block_source": "none",
        "special_rule": "none",
    })
    return row


def classify(source: dict[str, str]) -> tuple[dict[str, str], list[str]]:
    row = empty_row(source)
    errors: list[str] = []
    item_id = int(source["item_id"])
    category = source["item_category"]
    kind = source["secondary_kind"]

    if item_id == 0:
        row.update({
            "route": "unarmed-sentinel", "slot": "hands", "family_role": "unarmed_job_derived",
            "damage_type": "crush", "damage_type_confidence": "designed", "wmod_tier": "job-derived",
            "reach_policy": "1", "dcl_range": "1", "handedness": "unarmed", "parry_tier": "job-derived",
            "skill_primary": "PA+martial-arts", "special_rule": "common_fist_penalty_or_martial_arts_wmod",
            "readiness": "mechanism-ready-authoring-required",
            "authoring_gates": "unarmed wmod curve;untrained fist penalty;Martial Arts parry eligibility",
        })
        return row, errors

    if category in EXTERNAL_WEAPON_CATEGORIES:
        row.update({
            "route": "thrown-payload-external", "slot": "inventory", "family_role": "throw_command_payload",
            "damage_type": "external", "damage_type_confidence": "requires-formula-policy",
            "reach_policy": "throw-command", "handedness": "not-equipped", "skill_primary": "throw-command",
            "readiness": "reverse-engineering",
            "authoring_gates": "throw payload formula;damage type;range/trajectory;inventory consumption;DCL hit/defense routing",
        })
        return row, errors

    if category in WEAPON_POLICIES:
        policy = WEAPON_POLICIES[category]
        row.update({
            "route": "equipped-weapon", "slot": "hands", "family_role": policy.role,
            "damage_type": policy.damage_type, "damage_type_confidence": "designed",
            "wmod_tier": policy.wmod_tier, "reach_policy": policy.reach_policy,
            "dcl_range": policy.reach_policy if policy.reach_policy != "native-projectile" else source["weapon_range"],
            "handedness": policy.handedness, "parry_tier": policy.parry_tier,
            "skill_primary": policy.skill_primary, "overcap_route": policy.overcap_route,
            "armor_divisor_tier": policy.armor_divisor_tier, "bolt_role": policy.bolt_role,
            "special_rule": policy.special_rule, "weight_required": "true",
            "readiness": "structure-ready-numeric-authoring-required",
        })
        gates = ["Weight", "wmod"]
        if policy.parry_tier != "none":
            gates.append("parry calibration")
        if policy.armor_divisor_tier != "none":
            gates.append("armor divisor calibration")
        if policy.bolt_role != "none":
            gates.extend(["explicit SKU element", "magic modifier calibration", "range data authoring"])
        if policy.special_rule != "none":
            gates.append(policy.special_rule)
        row["authoring_gates"] = ";".join(gates)
        return row, errors

    if category in BODY_POLICIES:
        slot, armor_class, profile = BODY_POLICIES[category]
        row.update({
            "route": "body-armor", "slot": slot, "family_role": f"{armor_class}_body_chassis",
            "armor_class": armor_class, "dr_profile_cut_thrust_crush": profile,
            "weight_required": "true", "readiness": "structure-ready-numeric-authoring-required",
            "authoring_gates": "Weight;per-SKU DR;modest HP policy;native HP retune",
        })
        return row, errors

    if category in HEAD_CATEGORIES:
        head_class = {"Helmet": "martial-head", "Hat": "caster-head", "HairAdornment": "special-head"}[category]
        row.update({
            "route": "headgear", "slot": "head", "family_role": head_class,
            "armor_class": head_class, "weight_required": "true", "readiness": "identity-authoring-required",
            "authoring_gates": "head-slot HP/MP/DR identity decision;Weight;per-SKU stats",
        })
        return row, errors

    if category == "Shield":
        row.update({
            "route": "shield", "slot": "off-hand", "family_role": "finite_active_wall",
            "handedness": "off-hand-with-1H", "weight_required": "true", "block_source": "shield",
            "readiness": "structure-ready-numeric-authoring-required",
            "authoring_gates": "Weight;Block calibration;1H/off-hand eligibility;physical and missile coverage",
        })
        return row, errors

    if category in ACCESSORY_CATEGORIES:
        row.update({
            "route": "accessory", "slot": "accessory", "family_role": "specialization-trinket",
            "armor_class": "accessory", "weight_required": "true", "readiness": "identity-authoring-required",
            "authoring_gates": "accessory identity policy;Weight;per-SKU resists/movement/specials",
        })
        return row, errors

    if category == "Item":
        row.update({
            "route": "consumable-external", "slot": "inventory", "family_role": "item_command_payload",
            "readiness": "formula-map-required",
            "authoring_gates": "item command DCL formula;inventory consumption;targeting and delivery",
        })
        return row, errors

    if category == "None" and not kind:
        row.update({
            "route": "reserved", "slot": "none", "family_role": "reserved-record",
            "readiness": "reserved", "authoring_gates": "none",
        })
        return row, errors

    errors.append(f"item {item_id}:{source['name']} has no DCL category route ({category}/{kind})")
    row.update({"route": "unclassified", "readiness": "reverse-engineering", "authoring_gates": "classification"})
    return row, errors


def load_manifest(path: Path) -> tuple[list[dict[str, str]], list[str]]:
    with path.open(newline="", encoding="utf-8-sig") as handle:
        source = list(csv.DictReader(handle))
    rows: list[dict[str, str]] = []
    errors: list[str] = []
    for item in source:
        row, row_errors = classify(item)
        rows.append(row)
        errors.extend(row_errors)
    ids = [row["item_id"] for row in rows]
    if len(rows) != 261:
        errors.append(f"expected 261 item rows, found {len(rows)}")
    if len(ids) != len(set(ids)):
        errors.append("item ids are not unique")
    if any(row["route"] == "unclassified" for row in rows):
        errors.append("manifest contains unclassified rows")
    return rows, errors


def write_csv(path: Path, rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(rows)


def render_markdown(rows: list[dict[str, str]], source: Path, csv_path: Path | None) -> str:
    routes = Counter(row["route"] for row in rows)
    readiness = Counter(row["readiness"] for row in rows)
    families: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        if row["route"] == "equipped-weapon":
            families[row["category"]].append(row)
    weighted = [row for row in rows if row["weight_required"] == "true"]
    numeric_missing = Counter()
    for row in weighted:
        if not row["dcl_weight"]:
            numeric_missing["Weight"] += 1
    for row in rows:
        if row["route"] == "equipped-weapon" and not row["dcl_wmod"] and row["wmod_tier"] != "n/a":
            numeric_missing["weapon wmod"] += 1
        if row["parry_tier"] not in {"", "none", "job-derived"} and not row["dcl_parry"]:
            numeric_missing["weapon parry"] += 1
        if row["armor_divisor_tier"] not in {"", "none"} and not row["dcl_armor_divisor"]:
            numeric_missing["armor divisor"] += 1
        if row["route"] == "body-armor" and not row["dcl_dr_cut"]:
            numeric_missing["per-SKU body DR"] += 1
        if row["route"] == "shield" and not row["dcl_block"]:
            numeric_missing["shield Block"] += 1

    lines = [
        "# DCL item sidecar manifest",
        "",
        f"Source: `{source.as_posix()}` (`{len(rows)}` records).",
    ]
    if csv_path:
        lines.append(f"Row manifest: `{csv_path.as_posix()}`.")
    lines.extend([
        "",
        "This is a complete structural classification, not final balance. Relative design tiers are",
        "preserved as tiers; every missing numeric DCL field remains an explicit authoring gate.",
        "",
        "## Routes",
        "",
        "| Route | Items |",
        "| --- | ---: |",
    ])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(routes.items()))
    lines.extend(["", "## Readiness", "", "| Readiness | Items |", "| --- | ---: |"])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(readiness.items()))
    lines.extend(["", "## Equipped weapon families", "", "| Family | SKUs | Type | Reach | Hands | Parry | Scale | Role |", "| --- | ---: | --- | --- | --- | --- | --- | --- |"])
    for family, group in sorted(families.items()):
        row = group[0]
        lines.append(f"| {family} | {len(group)} | {row['damage_type']} | {row['reach_policy']} | {row['handedness']} | {row['parry_tier']} | {row['skill_primary']} | {row['family_role']} |")
    lines.extend([
        "",
        "## Explicit incomplete numeric authoring",
        "",
        f"Equipment records requiring Weight: **{len(weighted)}**.",
        "",
        "| Numeric field | Unauthored rows |",
        "| --- | ---: |",
    ])
    lines.extend(f"| {name} | {count} |" for name, count in sorted(numeric_missing.items()))
    lines.extend([
        "",
        "These blanks are validation gates. They must be filled by explicit DCL calibration and native-data",
        "authoring; the report intentionally does not copy vanilla WP/evasion/HP values into new semantics.",
        "",
        "## Mechanism and design gates exposed by the catalog",
        "",
        "- Helmet/Hat/Hair Adornment still need a head-slot HP/MP/DR identity and per-SKU policy.",
        "- Accessories still need per-family/per-SKU roles, including resist, movement, and special-property policy.",
        "- Throwing weapons, bombs, and consumables dispatch outside equipped-weapon Attack and require explicit formula routing.",
        "- Rod/Staff bolts need explicit element and range authoring for every SKU; Staff heal-on-attack variants need an ability-level route.",
        "- Unarmed uses the item-0 sentinel at runtime but its wmod/parry are job-derived, never item-derived.",
        "",
    ])
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    parser.add_argument("--csv", type=Path)
    parser.add_argument("--markdown", type=Path)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    rows, errors = load_manifest(args.catalog)
    if errors:
        raise SystemExit("\n".join(errors))
    if args.csv:
        write_csv(args.csv, rows)
    markdown = render_markdown(rows, args.catalog, args.csv)
    if args.markdown:
        args.markdown.parent.mkdir(parents=True, exist_ok=True)
        args.markdown.write_text(markdown, encoding="utf-8")
    else:
        print(markdown)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
