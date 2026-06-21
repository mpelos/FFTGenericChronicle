#!/usr/bin/env python3
"""Canonical T6xT7 offense/armor composition checker for Generic Chronicle."""
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


def response_for_attack(
    attack: dict[str, Any],
    target: dict[str, Any],
    effects: list[dict[str, Any]],
    bundle: dict[str, Any],
) -> dict[str, float | int]:
    contract = bundle["formula_contract"]
    decimals = int(contract["numeric_comparison"]["float_decimals"])

    family = attack["family"]
    output_per_hit = int(attack["output_per_hit"])
    hit_count = int(attack["hit_count"])

    if family == "none" or output_per_hit <= 0:
        return {
            "final_response": 0.0,
            "damage_per_hit": 0,
            "total_damage": 0,
        }

    armor_class = target["armor_class"]
    damage_type = attack["damage_type"]
    base_response = float(bundle["armor_response"][armor_class][damage_type])
    family_penetration = float(bundle["family_penetration"].get(family, 0.0))
    penetration_bonus = sum(float(effect.get("penetration_bonus", 0.0)) for effect in effects)
    effective_penetration = clamp(family_penetration + penetration_bonus, 0.0, 1.0)
    penetration_ceiling = float(contract["penetration_ceiling"])

    if base_response < penetration_ceiling:
        penetrated_response = base_response + effective_penetration * (
            penetration_ceiling - base_response
        )
    else:
        penetrated_response = base_response

    dynamic_delta = sum(float(effect.get("response_delta", 0.0)) for effect in effects)
    low, high = (float(value) for value in contract["combined_multiplier_clamp"])
    final_response = clamp(penetrated_response + dynamic_delta, low, high)
    damage_per_hit = math.floor(rounded(output_per_hit * final_response, decimals))

    return {
        "final_response": rounded(final_response, decimals),
        "damage_per_hit": damage_per_hit,
        "total_damage": damage_per_hit * hit_count,
    }


def safe_ratio(numerator: float, denominator: float, decimals: int) -> float:
    if denominator == 0:
        return 0.0
    return rounded(numerator / denominator, decimals)


def calculate_scenario(scenario: dict[str, Any], bundle: dict[str, Any]) -> dict[str, Any]:
    decimals = int(bundle["formula_contract"]["numeric_comparison"]["float_decimals"])
    target = scenario["target"]
    effects = scenario.get("dynamic_effects", [])
    expected = scenario["expected"]
    validation_errors: list[str] = []

    original = response_for_attack(scenario["original"], target, effects, bundle)
    resulting = response_for_attack(scenario["resulting"], target, effects, bundle)

    original_output = float(scenario["original"]["output_per_hit"])
    resulting_output = float(scenario["resulting"]["output_per_hit"])
    original_total = float(original["total_damage"])
    resulting_total = float(resulting["total_damage"])

    calculated = {
        "original_final_response": original["final_response"],
        "original_damage_per_hit": original["damage_per_hit"],
        "original_total_damage": original["total_damage"],
        "resulting_final_response": resulting["final_response"],
        "resulting_damage_per_hit": resulting["damage_per_hit"],
        "resulting_total_damage": resulting["total_damage"],
        "raw_output_ratio": safe_ratio(resulting_output, original_output, decimals),
        "response_ratio": safe_ratio(
            float(resulting["final_response"]),
            float(original["final_response"]),
            decimals,
        ),
        "damage_ratio": safe_ratio(resulting_total, original_total, decimals),
        "total_damage_delta": int(resulting["total_damage"]) - int(original["total_damage"]),
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
