#!/usr/bin/env python3
"""Canonical T3 healing/attrition checker for Generic Chronicle."""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any


def load_bundle(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def rounded(value: float, decimals: int) -> float:
    return round(value, decimals)


def raw_heal(scenario: dict[str, Any], faith_floor: float) -> int:
    model = scenario["model"]
    action = scenario["action"]
    actor = scenario["actor"]
    target = scenario["target"]

    if model in {"item_heal", "reaction_heal"}:
        return int(action["item_power"])
    if model == "spell_heal":
        faith_factor = max(
            faith_floor,
            (float(actor["caster_faith"]) / 100) * (float(target["target_faith"]) / 100),
        )
        return math.floor(float(action["spell_k"]) * float(actor["ma"]) * faith_factor)
    if model == "revive_item":
        return int(action["revive_hp"])
    raise ValueError(f"unknown healing model: {model}")


def calculate_scenario(scenario: dict[str, Any], bundle: dict[str, Any]) -> dict[str, Any]:
    decimals = int(bundle["formula_contract"]["numeric_comparison"]["float_decimals"])
    faith_floor = float(bundle["formula_contract"]["faith_factor_floor"])
    model = scenario["model"]
    action = scenario["action"]
    target = scenario["target"]
    expected = scenario["expected"]
    validation_errors: list[str] = []

    raw = raw_heal(scenario, faith_floor)
    effective = min(raw, int(target["missing_hp"]))
    overheal = max(0, raw - int(target["missing_hp"]))

    if model == "reaction_heal":
        effective_triggers = min(int(action["incoming_triggers"]), int(action["per_round_cap"]))
        expected_heal = effective * float(action["trigger_chance"]) * effective_triggers
        resource_consumed = float(action["trigger_chance"]) * effective_triggers * float(action["resource_cost"])
        uses_resolved = effective_triggers
        total_expected_heal = expected_heal
    else:
        uses_resolved = min(
            int(action["planned_uses"]),
            math.floor(float(action["resource_available"]) / float(action["resource_cost"])),
        )
        expected_heal = float(effective)
        resource_consumed = float(action["resource_cost"]) * uses_resolved
        total_expected_heal = expected_heal * uses_resolved

    calculated = {
        "raw_heal": raw,
        "effective_heal": effective,
        "overheal": overheal,
        "expected_heal": rounded(expected_heal, decimals),
        "resource_consumed": rounded(resource_consumed, decimals),
        "uses_resolved": uses_resolved,
        "total_expected_heal": rounded(total_expected_heal, decimals),
    }

    for key, value in calculated.items():
        expected_value = expected[key]
        if isinstance(value, float) or isinstance(expected_value, float):
            if rounded(float(value), decimals) != rounded(float(expected_value), decimals):
                validation_errors.append(f"{key}: expected {expected_value} calculated {value}")
        elif value != expected_value:
            validation_errors.append(f"{key}: expected {expected_value} calculated {value}")

    return {
        "scenario_id": scenario["scenario_id"],
        "model": model,
        **calculated,
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
