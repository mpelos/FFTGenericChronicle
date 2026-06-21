#!/usr/bin/env python3
"""Canonical T5 CT/delay/interrupt checker for Generic Chronicle."""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any


def load_bundle(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def ticks_to_turn(speed: int, current_ct: int) -> int:
    if current_ct >= 100:
        return 0
    return math.ceil((100 - current_ct) / speed)


def calculate_scenario(scenario: dict[str, Any], bundle: dict[str, Any]) -> dict[str, Any]:
    model = scenario["model"]
    expected = scenario["expected"]
    validation_errors: list[str] = []
    calculated: dict[str, Any]

    if model == "turn_readiness":
        unit = scenario["unit"]
        calculated = {
            "ticks_to_turn": ticks_to_turn(int(unit["speed"]), int(unit["current_ct"])),
        }
    elif model == "post_turn_ct":
        unit = scenario["unit"]
        deltas = bundle["formula_contract"]["post_turn_ct"]
        calculated = {
            "new_ct": int(unit["current_ct"]) + int(deltas[unit["turn_choice"]]),
        }
    elif model == "ctr_from_spell_speed":
        action = scenario["action"]
        ctr = math.ceil(100 / int(action["spell_speed"]))
        calculated = {
            "ctr": ctr,
            "ticks_to_resolution": ctr,
        }
    elif model == "delayed_target_safety":
        action = scenario["action"]
        target = scenario["target"]
        target_turn = ticks_to_turn(int(target["speed"]), int(target["current_ct"]))
        ticks_to_resolution = int(action["ticks_to_resolution"])
        target_safe = ticks_to_resolution < target_turn
        calculated = {
            "target_ticks_to_turn": target_turn,
            "target_safe": target_safe,
            "can_resolve_on_target": target_safe,
            "whiff_window_ticks": max(0, ticks_to_resolution - target_turn),
            "predictability": "safe" if target_safe else "unsafe",
        }
    elif model == "interrupt_window":
        action = scenario["action"]
        calculated = {
            "interrupts_before_resolution": int(action["interrupt_tick"])
            < int(action["ticks_to_resolution"]),
        }
    elif model == "speed_delta":
        unit = scenario["unit"]
        speed_before = int(unit["speed_before"])
        speed_after = speed_before + int(unit["speed_delta"])
        current_ct = int(unit["current_ct"])
        calculated = {
            "ticks_to_turn_before": ticks_to_turn(speed_before, current_ct),
            "speed_after": speed_after,
            "ticks_to_turn_after": ticks_to_turn(speed_after, current_ct),
        }
    elif model == "jump_like":
        unit = scenario["unit"]
        calculated = {
            "jump_ticks": math.ceil(50 / int(unit["speed"])),
        }
    else:
        calculated = {}
        validation_errors.append(f"unknown model: {model}")

    for key, expected_value in expected.items():
        value = calculated.get(key)
        if value != expected_value:
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
