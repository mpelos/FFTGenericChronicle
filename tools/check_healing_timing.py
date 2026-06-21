#!/usr/bin/env python3
"""Canonical T3xT5 healing timing checker for Generic Chronicle."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


def load_bundle(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def rounded(value: float, decimals: int) -> float:
    return round(value, decimals)


def calculate_active_heal_race(scenario: dict[str, Any], decimals: int) -> dict[str, Any]:
    healing = scenario["healing"]
    target = scenario["target"]
    threat = scenario["threat"]

    hp_before = int(target["hp_before"])
    max_hp = int(target["max_hp"])
    effective_heal = int(healing["effective_heal"])
    expected_heal = float(healing["expected_heal"])
    resolution_delay = int(healing["resolution_delay_ticks"])
    threat_tick = int(threat["threat_tick"])
    incoming_damage = int(threat["incoming_damage"])

    heal_before_threat = resolution_delay < threat_tick
    same_tick_unsafe = resolution_delay == threat_tick

    if heal_before_threat:
        hp_after_heal = min(max_hp, hp_before + effective_heal)
        hp_after_threat = hp_after_heal - incoming_damage
        heal_resolved = True
        final_hp = max(0, hp_after_threat)
    else:
        hp_after_threat = hp_before - incoming_damage
        heal_resolved = hp_after_threat > 0
        if heal_resolved:
            final_hp = min(max_hp, hp_after_threat + effective_heal)
        else:
            final_hp = max(0, hp_after_threat)

    survives = final_hp > 0
    timed_expected_heal = expected_heal if heal_resolved else 0.0

    return {
        "heal_before_threat": heal_before_threat,
        "same_tick_unsafe": same_tick_unsafe,
        "heal_resolved": heal_resolved,
        "hp_after_threat": hp_after_threat,
        "final_hp": final_hp,
        "survives": survives,
        "timed_expected_heal": rounded(timed_expected_heal, decimals),
    }


def calculate_reaction_after_damage(scenario: dict[str, Any], decimals: int) -> dict[str, Any]:
    reaction = scenario["reaction"]
    target = scenario["target"]
    threat = scenario["threat"]

    hp_after_damage = int(target["hp_before"]) - int(threat["incoming_damage"])
    effective_triggers = min(int(reaction["incoming_triggers"]), int(reaction["per_round_cap"]))
    reaction_can_resolve = hp_after_damage > 0 and effective_triggers > 0

    if reaction_can_resolve:
        timed_expected_heal = (
            float(reaction["effective_heal"]) * float(reaction["trigger_chance"]) * effective_triggers
        )
        expected_resource_consumed = (
            float(reaction["trigger_chance"]) * effective_triggers * float(reaction["resource_cost"])
        )
    else:
        timed_expected_heal = 0.0
        expected_resource_consumed = 0.0

    expected_final_hp = max(0, hp_after_damage)
    if reaction_can_resolve:
        expected_final_hp += timed_expected_heal

    return {
        "hp_after_damage": hp_after_damage,
        "reaction_can_resolve": reaction_can_resolve,
        "effective_triggers": effective_triggers,
        "timed_expected_heal": rounded(timed_expected_heal, decimals),
        "expected_resource_consumed": rounded(expected_resource_consumed, decimals),
        "expected_final_hp": rounded(expected_final_hp, decimals),
        "survives": expected_final_hp > 0,
    }


def calculate_delivery_comparison(scenario: dict[str, Any], decimals: int) -> dict[str, Any]:
    option_rows: list[dict[str, Any]] = []
    for index, option in enumerate(scenario["options"]):
        option_scenario = {
            "healing": option,
            "target": scenario["target"],
            "threat": scenario["threat"],
        }
        result = calculate_active_heal_race(option_scenario, decimals)
        option_rows.append(
            {
                "index": index,
                "option": option,
                "result": result,
            }
        )

    fastest = min(option_rows, key=lambda row: (int(row["option"]["resolution_delay_ticks"]), row["index"]))
    highest_heal = max(option_rows, key=lambda row: (int(row["option"]["effective_heal"]), -row["index"]))
    best = max(
        option_rows,
        key=lambda row: (
            bool(row["result"]["survives"]),
            int(row["result"]["final_hp"]),
            -row["index"],
        ),
    )

    return {
        "fastest_option_id": fastest["option"]["option_id"],
        "highest_heal_option_id": highest_heal["option"]["option_id"],
        "best_option_id": best["option"]["option_id"],
        "reliable_option_count": sum(1 for row in option_rows if bool(row["result"]["survives"])),
        "best_final_hp": best["result"]["final_hp"],
        "best_timed_expected_heal": best["result"]["timed_expected_heal"],
    }


def calculate_revive_race(scenario: dict[str, Any], decimals: int) -> dict[str, Any]:
    revive = scenario["revive"]
    death_clock = scenario["death_clock"]

    resolution_delay = int(revive["resolution_delay_ticks"])
    death_clock_ticks = int(death_clock["death_clock_ticks"])
    revive_before_death_clock = resolution_delay < death_clock_ticks
    same_tick_unsafe = resolution_delay == death_clock_ticks
    revive_resolved = revive_before_death_clock
    timed_expected_heal = float(revive["expected_heal"]) if revive_resolved else 0.0
    final_hp = int(revive["revive_hp"]) if revive_resolved else 0

    return {
        "revive_before_death_clock": revive_before_death_clock,
        "same_tick_unsafe": same_tick_unsafe,
        "revive_resolved": revive_resolved,
        "final_hp": final_hp,
        "timed_expected_heal": rounded(timed_expected_heal, decimals),
    }


def calculate_scenario(scenario: dict[str, Any], bundle: dict[str, Any]) -> dict[str, Any]:
    decimals = int(bundle["formula_contract"]["numeric_comparison"]["float_decimals"])
    model = scenario["model"]
    expected = scenario["expected"]
    validation_errors: list[str] = []

    if model == "active_heal_race":
        calculated = calculate_active_heal_race(scenario, decimals)
    elif model == "delivery_comparison":
        calculated = calculate_delivery_comparison(scenario, decimals)
    elif model == "reaction_after_damage":
        calculated = calculate_reaction_after_damage(scenario, decimals)
    elif model == "revive_race":
        calculated = calculate_revive_race(scenario, decimals)
    else:
        calculated = {}
        validation_errors.append(f"unknown model: {model}")

    for key, expected_value in expected.items():
        value = calculated.get(key)
        if isinstance(value, float) or isinstance(expected_value, float):
            if rounded(float(value), decimals) != rounded(float(expected_value), decimals):
                validation_errors.append(f"{key}: expected {expected_value} calculated {value}")
        elif value != expected_value:
            validation_errors.append(f"{key}: expected {expected_value} calculated {value}")

    return {
        "scenario_id": scenario["scenario_id"],
        "model": model,
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
