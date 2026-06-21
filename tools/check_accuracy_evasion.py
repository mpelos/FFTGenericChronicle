#!/usr/bin/env python3
"""Canonical T4 accuracy/evasion checker for Generic Chronicle."""
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


def effective_evade(value: float, multiplier: float) -> float:
    return clamp(value * multiplier, 0, 100)


def final_hit(value: float) -> int:
    return int(clamp(math.trunc(value), 0, 100))


def factor(percent_evade: float) -> float:
    return 100 - percent_evade


def calculate_scenario(scenario: dict[str, Any]) -> dict[str, Any]:
    attack = scenario["attack"]
    target = scenario["target"]
    model = scenario["model"]
    expected = scenario["expected"]
    validation_errors: list[str] = []
    factors: dict[str, Any] = {"base_hit": attack["base_hit"]}

    targeting_blocked = (
        attack.get("line_of_fire") == "blocked"
        or bool(attack.get("panel_vacated_before_resolution", False))
    )
    if targeting_blocked:
        can_target = False
        hit = 0
        factors["targeting_blocked"] = True
    else:
        can_target = True
        factors["targeting_blocked"] = False
        if model == "targeting":
            hit = final_hit(attack["base_hit"])
        elif model == "non_evadable" or not attack.get("can_be_evaded", True):
            hit = final_hit(attack["base_hit"])
            factors["evade_terms"] = []
        elif model == "magical":
            multiplier = float(target.get("evasion_multiplier", 1))
            shield = effective_evade(target["m_shield_evade"], multiplier)
            accessory = effective_evade(target["m_accessory_evade"], multiplier)
            terms = [factor(shield), factor(accessory)]
            factors["effective_evades"] = {
                "m_shield_evade": shield,
                "m_accessory_evade": accessory,
            }
            factors["evade_terms"] = terms
            hit = final_hit(attack["base_hit"] * terms[0] * terms[1] / 10000)
        elif model == "physical":
            multiplier = float(target.get("evasion_multiplier", 1))
            class_evade = effective_evade(target["p_class_evade"], multiplier)
            shield = effective_evade(target["p_shield_evade"], multiplier)
            accessory = effective_evade(target["p_accessory_evade"], multiplier)
            weapon = (
                effective_evade(target["weapon_evade"], multiplier)
                if target.get("weapon_evade_enabled", False)
                else 0
            )
            facing = attack["facing"]
            effective = {
                "p_class_evade": class_evade,
                "p_shield_evade": shield,
                "p_accessory_evade": accessory,
                "weapon_evade": weapon,
            }
            if facing == "front":
                terms = [factor(class_evade), factor(shield), factor(accessory), factor(weapon)]
                divisor = 100000000
            elif facing == "side":
                terms = [factor(shield), factor(accessory), factor(weapon)]
                divisor = 1000000
            elif facing == "rear":
                terms = [factor(accessory)]
                divisor = 100
            else:
                validation_errors.append(f"unknown physical facing: {facing}")
                terms = []
                divisor = 1
            product = attack["base_hit"]
            for term in terms:
                product *= term
            factors["effective_evades"] = effective
            factors["evade_terms"] = terms
            factors["divisor"] = divisor
            hit = final_hit(product / divisor)
        else:
            validation_errors.append(f"unknown model: {model}")
            hit = 0

    if can_target != expected["can_target"] or hit != expected["hit"]:
        validation_errors.append(
            f"expected can_target={expected['can_target']} hit={expected['hit']} "
            f"but calculated can_target={can_target} hit={hit}"
        )

    return {
        "scenario_id": scenario["scenario_id"],
        "model": model,
        "can_target": can_target,
        "expected_hit": expected["hit"],
        "calculated_hit": hit,
        "factors": factors,
        "validation_errors": validation_errors,
    }


def canonical_output(bundle: dict[str, Any]) -> dict[str, Any]:
    rows = [calculate_scenario(scenario) for scenario in bundle["scenarios"]]
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
