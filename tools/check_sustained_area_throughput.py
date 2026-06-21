#!/usr/bin/env python3
"""Canonical GPT checker for T11xT5 sustained area throughput."""
from __future__ import annotations

import argparse
import json
import sys
from copy import deepcopy


DECIMALS = 6


def r6(value):
    return round(float(value), DECIMALS)


def tick_times(timing):
    start = int(timing.get("start_tick", 0))
    first = start + int(timing.get("first_tick_delay", 0))
    interval = int(timing["tick_interval"])
    end = start + int(timing["duration_ticks"])
    interrupt_tick = timing.get("interrupt_tick")
    ticks = []
    tick = first
    while tick <= end:
        if interrupt_tick is not None and tick >= int(interrupt_tick):
            break
        ticks.append(tick)
        tick += interval
    return ticks


def unit_active(unit, tick):
    start = int(unit.get("active_start_tick", -10**9))
    expire = unit.get("active_expire_tick")
    if tick < start:
        return False
    if expire is not None and tick >= int(expire):
        return False
    return bool(unit.get("targetable", True))


def in_area(unit, origin, area):
    shape = area["shape"]
    if shape == "mapwide":
        return True
    vertical_tolerance = int(area.get("vertical_tolerance", 0))
    center = area.get("center", origin)
    if abs(int(unit.get("z", 0)) - int(center.get("z", 0))) > vertical_tolerance:
        return False
    dx = abs(int(unit["x"]) - int(center["x"]))
    dy = abs(int(unit["y"]) - int(center["y"]))
    if shape == "single":
        return dx == 0 and dy == 0
    if shape == "diamond":
        return dx + dy <= int(area.get("radius", 0))
    if shape == "square":
        return max(dx, dy) <= int(area.get("radius", 0))
    raise ValueError(f"unsupported shape: {shape}")


def group_allows(unit, effect):
    group = effect.get("target_group", "enemies")
    team = unit["team"]
    if group == "enemies":
        return team == "enemy"
    if group == "allies":
        return team == "ally"
    if group == "all_units":
        if effect.get("ally_safe", False) and team == "ally":
            return False
        return team in {"enemy", "ally"}
    raise ValueError(f"unsupported target_group: {group}")


def affected_units(scenario, tick):
    effect = scenario["effect"]
    origin = scenario.get("origin", {"x": 0, "y": 0, "z": 0})
    area = effect["area"]
    units = []
    for unit in scenario.get("units", []):
        if not unit_active(unit, tick):
            continue
        if not group_allows(unit, effect):
            continue
        if in_area(unit, origin, area):
            units.append(unit)
    return units


def unit_contribution(unit, effect):
    scoring = effect.get("scoring", {})
    enemy_weight = float(scoring.get("enemy_weight", 1.0))
    ally_weight = float(scoring.get("ally_weight", 1.0))
    priority = float(unit.get("priority", 1.0))
    if unit["team"] == "enemy":
        return priority * enemy_weight
    if unit["team"] == "ally":
        return priority * ally_weight
    return 0.0


def score_tick(units, effect):
    per_target_value = float(effect["per_target_value"])
    raw_score_units = sum(unit_contribution(unit, effect) for unit in units)
    raw_count = len(units)
    cap = effect.get("target_count_cap")
    effective_count = raw_count
    cap_scale = 1.0
    if cap is not None and raw_count > 0:
        effective_count = min(raw_count, int(cap))
        cap_scale = effective_count / raw_count
    return r6(raw_score_units * cap_scale * per_target_value), effective_count


def compute_row(scenario):
    ticks = tick_times(scenario["timing"])
    scores = []
    raw_counts = []
    effective_counts = []
    ids_by_tick = []
    for tick in ticks:
        units = affected_units(scenario, tick)
        score, effective_count = score_tick(units, scenario["effect"])
        scores.append(score)
        raw_counts.append(len(units))
        effective_counts.append(effective_count)
        ids_by_tick.append([unit["unit_id"] for unit in units])
    total = r6(sum(scores))
    interrupted = scenario["timing"].get("interrupt_tick") is not None
    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario.get("model", "sustained_area_throughput"),
        "tick_times": ticks,
        "tick_count": len(ticks),
        "raw_target_counts_by_tick": raw_counts,
        "effective_target_counts_by_tick": effective_counts,
        "affected_ids_by_tick": ids_by_tick,
        "per_tick_scores": scores,
        "total_score": total,
        "interrupted": interrupted,
        "no_effect": total == 0.0,
    }


def compare(row, expected):
    mismatches = []
    for key, expected_value in expected.items():
        got_value = row.get(key)
        if got_value != expected_value:
            mismatches.append({"field": key, "got": got_value, "expected": expected_value})
    return mismatches


def run(bundle):
    rows = []
    mismatches = []
    for scenario in bundle["scenarios"]:
        row = compute_row(scenario)
        expected = scenario.get("expected")
        if expected:
            diff = compare(row, expected)
            if diff:
                mismatches.append({"scenario_id": scenario["scenario_id"], "mismatches": diff})
        rows.append({**row, "validation_errors": []})
    return rows, mismatches


def write_expected(bundle_path, bundle):
    updated = deepcopy(bundle)
    for scenario in updated["scenarios"]:
        row = compute_row(scenario)
        scenario["expected"] = {
            key: value
            for key, value in row.items()
            if key not in {"scenario_id", "model"}
        }
    with open(bundle_path, "w", encoding="utf-8") as handle:
        json.dump(updated, handle, indent=2)
        handle.write("\n")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("bundle")
    parser.add_argument("--output")
    parser.add_argument("--write-expected", action="store_true")
    args = parser.parse_args()

    with open(args.bundle, encoding="utf-8") as handle:
        bundle = json.load(handle)

    if args.write_expected:
        write_expected(args.bundle, bundle)
        with open(args.bundle, encoding="utf-8") as handle:
            bundle = json.load(handle)

    rows, mismatches = run(bundle)
    result = {
        "schema_version": bundle["schema_version"],
        "scenario_count": len(rows),
        "mismatch_count": len(mismatches),
        "mismatches": mismatches,
        "rows": rows,
    }

    if args.output:
        with open(args.output, "w", encoding="utf-8") as handle:
            json.dump(result, handle, indent=2)
            handle.write("\n")
    else:
        print(json.dumps(result, indent=2))

    return 1 if mismatches else 0


if __name__ == "__main__":
    sys.exit(main())
