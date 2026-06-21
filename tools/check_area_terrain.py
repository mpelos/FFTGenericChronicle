#!/usr/bin/env python3
"""Canonical GPT checker for T11 area and terrain snapshots."""

import argparse
import json
import math
import sys
from copy import deepcopy

DECIMALS = 6


def r6(value):
    if value is None:
        return None
    return round(float(value), DECIMALS)


def coord_key(panel):
    return (panel.get("x"), panel.get("y"), panel.get("z", 0))


def terrain_lookup(scenario):
    lookup = {}
    for tile in scenario.get("tiles", []):
        lookup[coord_key(tile)] = tile.get("terrain")
    return lookup


def panel_terrain(panel, tiles):
    return panel.get("terrain", tiles.get(coord_key(panel), "unknown"))


def terrain_ok(candidate, scenario, tiles):
    req = scenario.get("terrain_requirement")
    if not req:
        return True

    allowed = set(req.get("allowed", []))
    scope = req.get("scope", "target_panel")
    if scope == "origin_panel":
        terrain = panel_terrain(scenario["origin"], tiles)
    else:
        terrain = panel_terrain(candidate, tiles)
    return terrain in allowed


def terrain_ratio(scenario, candidates, tiles):
    req = scenario.get("terrain_requirement")
    if not req:
        return None
    if not candidates:
        return 0.0
    valid = sum(1 for candidate in candidates if terrain_ok(candidate, scenario, tiles))
    return r6(valid / len(candidates))


def candidate_valid(candidate, scenario, tiles):
    if not candidate.get("can_target_panel", True):
        return False
    if not candidate.get("line_of_effect", True):
        return False
    return terrain_ok(candidate, scenario, tiles)


def vertical_ok(unit, candidate, tolerance):
    return abs(unit.get("z", 0) - candidate.get("z", 0)) <= tolerance


def in_line(unit, origin, candidate, length, vertical_tolerance):
    if abs(unit.get("z", 0) - origin.get("z", 0)) > vertical_tolerance:
        return False

    dx = candidate["x"] - origin["x"]
    dy = candidate["y"] - origin["y"]
    ux = unit["x"] - origin["x"]
    uy = unit["y"] - origin["y"]

    if dx == 0 and dy == 0:
        return False
    if dx != 0 and dy != 0:
        return False
    if dx == 0:
        if ux != 0:
            return False
        if uy == 0:
            return False
        if (uy > 0) != (dy > 0):
            return False
        return abs(uy) <= length
    if uy != 0:
        return False
    if ux == 0:
        return False
    if (ux > 0) != (dx > 0):
        return False
    return abs(ux) <= length


def in_area(unit, origin, candidate, ability):
    shape = ability["shape"]
    vertical_tolerance = ability.get("vertical_tolerance", 0)

    if shape == "mapwide":
        return True
    if shape == "line":
        length = ability.get("length", max(abs(candidate["x"] - origin["x"]), abs(candidate["y"] - origin["y"])))
        return in_line(unit, origin, candidate, length, vertical_tolerance)

    if not vertical_ok(unit, candidate, vertical_tolerance):
        return False

    dx = abs(unit["x"] - candidate["x"])
    dy = abs(unit["y"] - candidate["y"])

    if shape == "single":
        return dx == 0 and dy == 0
    if shape == "diamond":
        return dx + dy <= ability.get("radius", 0)
    if shape == "square":
        return max(dx, dy) <= ability.get("radius", 0)
    raise ValueError(f"unsupported shape: {shape}")


def group_allows(unit, ability):
    group = ability.get("target_group", "enemies")
    team = unit["team"]
    if group == "enemies":
        return team == "enemy"
    if group == "allies":
        return team == "ally"
    if group == "all_units":
        if ability.get("ally_safe", False) and team == "ally":
            return False
        return team in {"enemy", "ally"}
    raise ValueError(f"unsupported target_group: {group}")


def affected_units(candidate, scenario):
    ability = scenario["ability"]
    origin = scenario["origin"]
    units = []
    for unit in scenario.get("units", []):
        if ability.get("targetable_required", True) and not unit.get("targetable", True):
            continue
        if not group_allows(unit, ability):
            continue
        if in_area(unit, origin, candidate, ability):
            units.append(unit)
    return units


def score_units(units, scenario):
    scoring = scenario.get("scoring", {})
    enemy_weight = scoring.get("enemy_weight", 1.0)
    ally_weight = scoring.get("ally_weight", -1.0)
    total = 0.0
    for unit in units:
        priority = unit.get("priority", 1.0)
        if unit["team"] == "enemy":
            total += priority * enemy_weight
        elif unit["team"] == "ally":
            total += priority * ally_weight
    return r6(total)


def base_empty_row(scenario, total_candidates, valid_candidates, terrain_available_ratio, reason):
    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario.get("model", "area_terrain"),
        "selected_panel_id": "none",
        "selected_score": 0.0,
        "selected_enemy_hits": 0,
        "selected_ally_hits": 0,
        "selected_target_count": 0,
        "selected_affected_ids": [],
        "valid_candidate_count": valid_candidates,
        "total_candidate_count": total_candidates,
        "terrain_available_ratio": terrain_available_ratio,
        "no_selection_reason": reason,
    }


def compute_row(scenario):
    tiles = terrain_lookup(scenario)
    candidates = scenario.get("candidates", [])
    total_candidates = len(candidates)
    terr_ratio = terrain_ratio(scenario, candidates, tiles)
    valid = [candidate for candidate in candidates if candidate_valid(candidate, scenario, tiles)]

    if not valid:
        return base_empty_row(scenario, total_candidates, 0, terr_ratio, "no_valid_candidate")

    scored = []
    for index, candidate in enumerate(valid):
        affected = affected_units(candidate, scenario)
        score = score_units(affected, scenario)
        scored.append((index, candidate, affected, score))

    best = None
    for item in scored:
        if best is None or item[3] > best[3]:
            best = item

    _, candidate, affected, score = best
    minimum_score = scenario.get("minimum_score_to_select", 0)
    if score < minimum_score:
        return base_empty_row(scenario, total_candidates, len(valid), terr_ratio, "below_minimum_score")

    enemy_hits = sum(1 for unit in affected if unit["team"] == "enemy")
    ally_hits = sum(1 for unit in affected if unit["team"] == "ally")
    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario.get("model", "area_terrain"),
        "selected_panel_id": candidate["panel_id"],
        "selected_score": score,
        "selected_enemy_hits": enemy_hits,
        "selected_ally_hits": ally_hits,
        "selected_target_count": len(affected),
        "selected_affected_ids": [unit["unit_id"] for unit in affected],
        "valid_candidate_count": len(valid),
        "total_candidate_count": total_candidates,
        "terrain_available_ratio": terr_ratio,
        "no_selection_reason": "none",
    }


def equal_field(key, got, expected):
    if isinstance(got, float) or isinstance(expected, float):
        return math.isclose(float(got), float(expected), abs_tol=0.5 * 10 ** (-DECIMALS))
    return got == expected


def compare(row, expected):
    mismatches = []
    for key, expected_value in expected.items():
        got_value = row.get(key)
        if not equal_field(key, got_value, expected_value):
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
