#!/usr/bin/env python3
"""Smoke test for the complete DCL item sidecar classification."""
from __future__ import annotations

import csv
from collections import Counter

import report_dcl_item_sidecar as report


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    rows, errors = report.load_manifest(report.DEFAULT_CATALOG)
    with report.DEFAULT_CATALOG.open(newline="", encoding="utf-8-sig") as handle:
        native_rows = list(csv.DictReader(handle))
    check(not errors, "classification errors:\n" + "\n".join(errors))
    check(len(rows) == 261, f"expected 261 items, got {len(rows)}")
    check(len({row["item_id"] for row in rows}) == 261, "item ids must be unique")
    check(not any(row["route"] == "unclassified" for row in rows), "unclassified item route")

    by_id = {int(row["item_id"]): row for row in rows}
    by_name = {row["name"]: row for row in rows if row["name"]}
    check(by_id[0]["route"] == "unarmed-sentinel" and by_id[0]["damage_type"] == "crushing",
          "item 0 must be the job-derived unarmed sentinel")
    check(all(not row["damage_mode"] and not row["damage_type"] for row in rows if row["route"] == "equipped-weapon"),
          "weapon families must not turn an old profile hint into unauthored damage mode/type")
    check(by_name["Dagger"]["skill_family"] == "Knife" and by_name["Dagger"]["handedness"] == "1H",
          "Knife policy drift")
    check(by_name["Leather Armor"]["armor_class"] == "heavy" and "dcl_dr" in by_name["Leather Armor"] and
          "dr_profile_cut_thrust_crush" not in by_name["Leather Armor"],
          "body armor must expose one per-item DR rather than a damage-type matrix")
    check(by_name["Flame Rod"]["bolt_role"] == "offensive_elemental_bolt" and by_name["Flame Rod"]["dcl_range"] == "1",
          "Rod melee Reach must remain 1; bolt range belongs to authored magical metadata")
    check(by_name["Crossbow"]["skill_family"] == "Crossbow" and by_name["Crossbow"]["overcap_route"] == "raw-damage",
          "Crossbow marksmanship policy drift")
    check(by_name["Romandan Pistol"]["overcap_route"] == "penetration", "Gun penetration policy drift")
    check(by_name["Venetian Shield"]["block_source"] == "shield" and by_name["Venetian Shield"]["weight_required"] == "true",
          "shield Block/Weight policy drift")
    check(by_name["Potion"]["route"] == "consumable-external", "consumables must not enter equipped Attack")
    check(by_name["Shuriken"]["route"] == "thrown-payload-external", "Throw payload must use external dispatch")

    equipped = [row for row in rows if row["route"] in {"equipped-weapon", "body-armor", "headgear", "shield", "accessory"}]
    check(equipped and all(row["weight_required"] == "true" for row in equipped),
          "every real equipment piece must require Weight")
    check(all(row["family_role"] for row in equipped), "every equipment row needs a structural role")
    check(all(row["authoring_gates"] for row in rows), "every row needs explicit completion gates")

    routes = Counter(row["route"] for row in rows)
    check(routes["equipped-weapon"] == 123, f"expected 123 ordinary equipped weapon SKUs, got {routes['equipped-weapon']}")
    check(routes["body-armor"] == 37, f"expected 37 body armor SKUs, got {routes['body-armor']}")
    check(routes["headgear"] == 29, f"expected 29 headgear SKUs, got {routes['headgear']}")
    check(routes["shield"] == 16 and routes["accessory"] == 33, "gear category count drift")
    check(routes["consumable-external"] == 14 and routes["thrown-payload-external"] == 6,
          "external item category count drift")
    check(routes["reserved"] == 2, "only two terminal records should remain reserved")

    native_by_id = {row["item_id"]: row for row in native_rows}
    ranged_expectations = {
        "Bow": ("Arc", "arc_trajectory", "Bow", "none"),
        "Crossbow": ("Direct", "straight_line", "Crossbow", "raw-damage"),
        "Gun": ("Direct", "straight_line", "Guns", "penetration"),
    }
    for category, (native_flag, special_rule, skill_family, overcap_route) in ranged_expectations.items():
        category_rows = [row for row in rows if row["category"] == category]
        check(category_rows, f"missing {category} rows")
        for row in category_rows:
            native = native_by_id[row["item_id"]]
            check(native_flag in native["weapon_attack_flags"].split(", "),
                  f"{row['name']} must retain native {native_flag} trajectory flag")
            check(row["route"] == "equipped-weapon" and row["reach_policy"] == "native-projectile",
                  f"{row['name']} must be an equipped native projectile")
            check(row["dcl_range"] == native["weapon_range"],
                  f"{row['name']} DCL range must mirror native weapon range until authored otherwise")
            check(row["special_rule"] == special_rule and row["skill_family"] == skill_family,
                  f"{row['name']} ranged DCL identity drift")
            check(row["overcap_route"] == overcap_route,
                  f"{row['name']} overcap route drift")

    thrown_payloads = [row for row in rows if row["category"] in {"Throwing", "Bomb"}]
    check(thrown_payloads and all(row["route"] == "thrown-payload-external" for row in thrown_payloads),
          "Throwing/Bomb SKUs must stay out of equipped native projectile routing")

    print("DCL item sidecar smoke test passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
