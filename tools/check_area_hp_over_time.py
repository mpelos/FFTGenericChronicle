#!/usr/bin/env python3
"""Canonical GPT checker for T3xT5xT11 area HP over time."""
from __future__ import annotations

import argparse
import json
import sys
from copy import deepcopy


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
    if tick < int(unit.get("active_start_tick", -10**9)):
        return False
    expire = unit.get("active_expire_tick")
    if expire is not None and tick >= int(expire):
        return False
    return bool(unit.get("targetable", True))


def in_area(unit, origin, area):
    shape = area["shape"]
    if shape == "mapwide":
        return True
    center = area.get("center", origin)
    if abs(int(unit.get("z", 0)) - int(center.get("z", 0))) > int(area.get("vertical_tolerance", 0)):
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
    group = effect.get("target_group", "allies")
    team = unit["team"]
    if group == "allies":
        return team == "ally"
    if group == "enemies":
        return team == "enemy"
    if group == "all_units":
        if effect.get("ally_safe", False) and team == "ally":
            return False
        return team in {"ally", "enemy"}
    raise ValueError(f"unsupported target_group: {group}")


def eligible_units(scenario, states, tick):
    effect = scenario["effect"]
    origin = scenario.get("origin", {"x": 0, "y": 0, "z": 0})
    area = effect["area"]
    units = []
    for unit in scenario.get("units", []):
        state = states[unit["unit_id"]]
        if state["hp"] <= 0 and not effect.get("affects_ko", False):
            continue
        if not unit_active(unit, tick):
            continue
        if not group_allows(unit, effect):
            continue
        if in_area(unit, origin, area):
            units.append(unit)
    return units


def apply_tick(units, states, effect):
    value = int(effect["per_target_value"])
    totals = {
        "raw_heal": 0,
        "effective_heal": 0,
        "overheal": 0,
        "raw_damage": 0,
        "effective_damage": 0,
        "overkill": 0,
        "ko_ids": [],
    }
    for unit in units:
        state = states[unit["unit_id"]]
        max_hp = int(state["max_hp"])
        hp = int(state["hp"])
        kind = effect["kind"]
        if kind == "healing" and unit.get("undead", False) and effect.get("undead_inverts_healing", False):
            kind = "damage"

        if kind == "healing":
            raw = value
            missing = max(0, max_hp - hp)
            effective = min(raw, missing)
            state["hp"] = min(max_hp, hp + effective)
            totals["raw_heal"] += raw
            totals["effective_heal"] += effective
            totals["overheal"] += raw - effective
        elif kind == "damage":
            raw = value
            effective = min(raw, max(0, hp))
            state["hp"] = max(0, hp - effective)
            totals["raw_damage"] += raw
            totals["effective_damage"] += effective
            totals["overkill"] += raw - effective
            if hp > 0 and state["hp"] <= 0:
                totals["ko_ids"].append(unit["unit_id"])
        else:
            raise ValueError(f"unsupported effect kind: {kind}")
    return totals


def compute_row(scenario):
    states = {
        unit["unit_id"]: {
            "hp": int(unit["hp"]),
            "max_hp": int(unit["max_hp"]),
        }
        for unit in scenario.get("units", [])
    }
    ticks = tick_times(scenario["timing"])
    affected_ids_by_tick = []
    hp_by_tick = []
    tick_rows = []
    for tick in ticks:
        units = eligible_units(scenario, states, tick)
        affected_ids_by_tick.append([unit["unit_id"] for unit in units])
        tick_result = apply_tick(units, states, scenario["effect"])
        tick_rows.append(tick_result)
        hp_by_tick.append({unit_id: state["hp"] for unit_id, state in states.items()})

    total_raw_heal = sum(row["raw_heal"] for row in tick_rows)
    total_effective_heal = sum(row["effective_heal"] for row in tick_rows)
    total_overheal = sum(row["overheal"] for row in tick_rows)
    total_raw_damage = sum(row["raw_damage"] for row in tick_rows)
    total_effective_damage = sum(row["effective_damage"] for row in tick_rows)
    total_overkill = sum(row["overkill"] for row in tick_rows)
    ko_ids = sorted({unit_id for row in tick_rows for unit_id in row["ko_ids"]})
    any_affected = any(ids for ids in affected_ids_by_tick)
    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario.get("model", "area_hp_over_time"),
        "tick_times": ticks,
        "tick_count": len(ticks),
        "affected_ids_by_tick": affected_ids_by_tick,
        "raw_heal_by_tick": [row["raw_heal"] for row in tick_rows],
        "effective_heal_by_tick": [row["effective_heal"] for row in tick_rows],
        "overheal_by_tick": [row["overheal"] for row in tick_rows],
        "raw_damage_by_tick": [row["raw_damage"] for row in tick_rows],
        "effective_damage_by_tick": [row["effective_damage"] for row in tick_rows],
        "overkill_by_tick": [row["overkill"] for row in tick_rows],
        "hp_by_tick": hp_by_tick,
        "total_raw_heal": total_raw_heal,
        "total_effective_heal": total_effective_heal,
        "total_overheal": total_overheal,
        "total_raw_damage": total_raw_damage,
        "total_effective_damage": total_effective_damage,
        "total_overkill": total_overkill,
        "ko_ids": ko_ids,
        "no_effect": not any_affected,
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
