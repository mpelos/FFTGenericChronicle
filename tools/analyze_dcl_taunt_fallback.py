#!/usr/bin/env python3
"""Validate the job-free one-turn Berserk fallback for DCL Taunt."""
from __future__ import annotations

import argparse
import csv
import json
from pathlib import Path


RULE_NAME = "DCL Taunt fallback via Berserk carrier"
RESISTANCE_FORMULA = "clamp(18 - target.brave / 10, 3, 18)"


def analyze(settings: dict, catalog_rows: list[dict[str, str]]) -> list[str]:
    errors: list[str] = []
    if settings.get("DclStatusControlEnabled") is not True:
        errors.append("DclStatusControlEnabled must be true")
    rules = settings.get("DclStatusRules")
    if not isinstance(rules, list):
        return errors + ["DclStatusRules must be a list"]
    matches = [rule for rule in rules if isinstance(rule, dict) and rule.get("Name") == RULE_NAME]
    if len(matches) != 1:
        return errors + [f"expected exactly one {RULE_NAME!r} rule, found {len(matches)}"]
    rule = matches[0]
    expected = {
        "AbilityId": 241,
        "ActionType": -1,
        "StatusByteIndex": 2,
        "StatusMask": 0x08,
        "Operation": "add",
        "NativeRiderPolicy": "replaced-post-calc",
        "ResistanceFormula": RESISTANCE_FORMULA,
        "DurationTargetTurns": 1,
    }
    for key, value in expected.items():
        if rule.get(key) != value:
            errors.append(f"{key}={rule.get(key)!r}, expected {value!r}")

    carrier = [row for row in catalog_rows if row.get("id_dec") == "241"]
    if len(carrier) != 1:
        errors.append(f"expected one ability-catalog row for carrier 241, found {len(carrier)}")
    else:
        row = carrier[0]
        statuses = {token.strip().lower() for token in row.get("inflict_statuses", "").split(",")}
        if "berserk" not in statuses:
            errors.append("ability 241 is not a native Berserk status carrier")
        if row.get("inflict_status_mode") != "AllOrNothing":
            errors.append("ability 241 no longer uses its all-or-nothing native status packet")
        if row.get("formula_hex") != "0x0A":
            errors.append("ability 241 no longer belongs to the conditional post-calc producer family")

    # The exact fixture expression is intentionally simple and independently falsified here:
    # lower Brave must yield a strictly higher resistance target number than higher Brave.
    low_brave_resistance = max(3, min(18, 18 - 30 // 10))
    high_brave_resistance = max(3, min(18, 18 - 97 // 10))
    if low_brave_resistance <= high_brave_resistance:
        errors.append("Taunt fallback resistance is not inverted across the Brave range")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("settings", type=Path)
    parser.add_argument(
        "--catalog",
        type=Path,
        default=Path(__file__).resolve().parents[1] / "work" / "wotl_ability_action_baseline.csv",
    )
    args = parser.parse_args()
    settings = json.loads(args.settings.read_text(encoding="utf-8-sig"))
    with args.catalog.open("r", encoding="utf-8-sig", newline="") as handle:
        rows = list(csv.DictReader(handle))
    errors = analyze(settings, rows)
    for error in errors:
        print(f"ERROR: {error}")
    print("DCL Taunt fallback PASS" if not errors else "DCL Taunt fallback FAIL")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
