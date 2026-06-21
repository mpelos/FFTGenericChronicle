#!/usr/bin/env python3
"""Canonical T6 armor-response checker for Generic Chronicle."""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any


def load_bundle(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def rounded(value: float, decimals: int) -> float:
    return round(value, decimals)


def calculate_scenario(scenario: dict[str, Any], bundle: dict[str, Any]) -> dict[str, Any]:
    contract = bundle["formula_contract"]
    decimals = int(contract["numeric_comparison"]["float_decimals"])
    low, high = (float(value) for value in contract["combined_multiplier_clamp"])
    penetration_ceiling = float(contract["penetration_ceiling"])

    attack = scenario["attack"]
    target = scenario["target"]
    effects = scenario.get("dynamic_effects", [])
    expected = scenario["expected"]
    validation_errors: list[str] = []

    armor_class = target["armor_class"]
    damage_type = attack["damage_type"]
    weapon_family = attack["weapon_family"]

    base_response = float(bundle["armor_response"][armor_class][damage_type])
    family_penetration = float(bundle["family_penetration"][weapon_family])
    penetration_bonus = sum(float(effect.get("penetration_bonus", 0.0)) for effect in effects)
    effective_penetration = clamp(family_penetration + penetration_bonus, 0.0, 1.0)

    if base_response < penetration_ceiling:
        penetrated_response = base_response + effective_penetration * (
            penetration_ceiling - base_response
        )
    else:
        penetrated_response = base_response

    dynamic_delta = sum(float(effect.get("response_delta", 0.0)) for effect in effects)
    unclamped_response = penetrated_response + dynamic_delta
    final_response = clamp(unclamped_response, low, high)
    damage_per_hit = math.floor(
        rounded(float(attack["base_damage_per_hit"]) * final_response, decimals)
    )
    total_damage = damage_per_hit * int(attack["hit_count"])

    calculated = {
        "base_response": rounded(base_response, decimals),
        "effective_penetration": rounded(effective_penetration, decimals),
        "penetrated_response": rounded(penetrated_response, decimals),
        "dynamic_delta": rounded(dynamic_delta, decimals),
        "unclamped_response": rounded(unclamped_response, decimals),
        "final_response": rounded(final_response, decimals),
        "damage_per_hit": damage_per_hit,
        "total_damage": total_damage,
    }

    for key, expected_value in expected.items():
        value = calculated.get(key)
        if isinstance(value, float) or isinstance(expected_value, float):
            if rounded(float(value), decimals) != rounded(float(expected_value), decimals):
                validation_errors.append(f"{key}: expected {expected_value} calculated {value}")
        elif value != expected_value:
            validation_errors.append(f"{key}: expected {expected_value} calculated {value}")

    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario["model"],
        "calculated_fields": calculated,
        "validation_errors": validation_errors,
    }


def canonical_output(bundle: dict[str, Any]) -> dict[str, Any]:
    rows = [calculate_scenario(scenario, bundle) for scenario in bundle["scenarios"]]
    rows.sort(key=lambda row: row["scenario_id"])
    mismatches = [
        {
            "scenario_id": row["scenario_id"],
            "validation_errors": row["validation_errors"],
        }
        for row in rows
        if row["validation_errors"]
    ]
    return {
        "scenario_count": len(rows),
        "mismatch_count": len(mismatches),
        "mismatches": mismatches,
        "rows": rows,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("bundle", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    output = canonical_output(load_bundle(args.bundle))
    text = json.dumps(output, indent=2)
    if args.output:
        args.output.write_text(text + "\n", encoding="utf-8")
    else:
        print(text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
