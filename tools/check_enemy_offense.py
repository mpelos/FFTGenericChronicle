#!/usr/bin/env python3
"""Canonical T7 enemy-offense checker for Generic Chronicle."""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
from typing import Any


PERMANENT_REPLACEMENT_EFFECTS = {
    "permanent_weapon_break",
    "permanent_disarm",
    "weapon_steal",
}


def load_bundle(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def rounded(value: float, decimals: int) -> float:
    return round(value, decimals)


def stats_for_attacker(scenario: dict[str, Any], bundle: dict[str, Any]) -> dict[str, int]:
    phase = scenario["phase"]
    attacker = scenario["attacker"]
    job = attacker["job"]
    stats = dict(bundle["jobs"][job]["bands"][phase])
    stats["brave"] = int(bundle["defaults"]["brave"])
    stats["faith"] = int(bundle["defaults"]["faith"])
    for key, value in attacker.get("stat_overrides", {}).items():
        stats[key] = int(value)
    return stats


def stat(stats: dict[str, int], name: str) -> int:
    if name == "speed":
        return int(stats.get("speed", stats.get("spd", 0)))
    return int(stats.get(name, 0))


def phase_wp(family: str, phase: str, bundle: dict[str, Any]) -> float:
    wp = float(bundle["families"][family].get("wp", 0))
    scalar = float(bundle["phase_wp_scalar"].get(phase, 1.0))
    return wp * scalar


def routine_value(family: str, stats: dict[str, int], phase: str, bundle: dict[str, Any]) -> float:
    family_data = bundle["families"][family]
    routine = family_data["routine"]
    wp = phase_wp(family, phase, bundle)
    pa = stat(stats, "pa")
    ma = stat(stats, "ma")
    speed = stat(stats, "speed")
    brave = stat(stats, "brave")

    if routine == "pa_wp":
        return pa * wp
    if routine in {"brave_pa_wp", "br_pa_wp"}:
        return math.floor(pa * brave / 100) * wp
    if routine in {"speed_pa_wp", "spd_pa_wp"}:
        return math.floor((pa + speed) / 2) * wp
    if routine == "ma_wp":
        return ma * wp
    if routine == "wp_wp":
        return wp * wp
    if routine in {"brave_pa_pa", "br_pa_pa"}:
        return math.floor(pa * brave / 100) * pa
    if routine in {"pa_ma_avg_wp", "pampa_wp"}:
        return math.floor((pa + ma) / 2) * wp

    raise ValueError(f"unsupported T7 routine: {routine}")


def family_fields(family: str | None, stats: dict[str, int], phase: str, bundle: dict[str, Any]) -> dict[str, Any]:
    if family is None or family == "none":
        return {
            "resulting_family": "none",
            "resulting_damage_type": "none",
            "resulting_routine": "none",
            "resulting_raw_output_per_hit": 0,
        }

    family_data = bundle["families"][family]
    raw = routine_value(family, stats, phase, bundle)
    if isinstance(raw, float) and raw.is_integer():
        raw = int(raw)
    return {
        "resulting_family": family,
        "resulting_damage_type": family_data["damage_type"],
        "resulting_routine": family_data["routine"],
        "resulting_raw_output_per_hit": raw,
    }


def output_state(
    normal_state: str,
    has_output_down: bool,
    permanent_family_change: bool,
    temporary_jammed: bool,
    resulting_family: str,
) -> tuple[str, bool]:
    if resulting_family == "none":
        return "permanent_no_family", False
    if temporary_jammed:
        return "temporary_jammed", False
    if permanent_family_change and has_output_down:
        return f"{normal_state}_temporary_output_down", True
    if permanent_family_change:
        return normal_state, True
    if has_output_down:
        return "temporary_output_down", True
    return "normal", True


def calculate_scenario(scenario: dict[str, Any], bundle: dict[str, Any]) -> dict[str, Any]:
    decimals = int(bundle["formula_contract"]["numeric_comparison"]["float_decimals"])
    stats = stats_for_attacker(scenario, bundle)
    phase = scenario["phase"]
    attack = scenario["attack"]
    effects = scenario.get("offense_effects", [])
    expected = scenario["expected"]
    validation_errors: list[str] = []

    base_family = attack["weapon_family"]
    base_raw = routine_value(base_family, stats, phase, bundle)
    if isinstance(base_raw, float) and base_raw.is_integer():
        base_raw = int(base_raw)

    resulting_family: str | None = base_family
    blocked_effects = 0
    permanent_state = "normal"

    for effect in effects:
        if effect.get("blocked_by_safeguard"):
            blocked_effects += 1
            continue
        if effect.get("kind") in PERMANENT_REPLACEMENT_EFFECTS:
            resulting_family = str(effect.get("fallback_family", "none"))
            if resulting_family == "none":
                permanent_state = "permanent_no_family"
            else:
                permanent_state = "permanent_break_fallback"

    resulting = family_fields(resulting_family, stats, phase, bundle)
    permanent_family_change = resulting["resulting_family"] != base_family

    multiplier = 1.0
    has_output_down = False
    temporary_jammed = False
    for effect in effects:
        if effect.get("blocked_by_safeguard"):
            continue
        if effect.get("kind") == "temporary_jam" or effect.get("output_to_zero"):
            temporary_jammed = True
        if "output_multiplier" in effect:
            multiplier *= float(effect["output_multiplier"])
            has_output_down = True

    state, can_attack = output_state(
        permanent_state,
        has_output_down,
        permanent_family_change,
        temporary_jammed,
        resulting["resulting_family"],
    )
    effective_multiplier = clamp(multiplier, 0.0, 1.0)
    if not can_attack:
        effective_multiplier = 0.0

    output_per_hit = math.floor(
        rounded(float(resulting["resulting_raw_output_per_hit"]) * effective_multiplier, decimals)
    )
    total_output = output_per_hit * int(attack.get("hit_count", 1))

    calculated = {
        "base_raw_output_per_hit": base_raw,
        **resulting,
        "effective_output_multiplier": rounded(effective_multiplier, decimals),
        "output_per_hit": output_per_hit,
        "total_output": total_output,
        "can_attack": can_attack,
        "permanent_family_change": permanent_family_change,
        "blocked_effects": blocked_effects,
        "output_state": state,
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
