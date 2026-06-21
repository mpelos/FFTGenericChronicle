#!/usr/bin/env python3
"""Canonical GPT checker for T8xSR spell routing and Reflect composition."""

import argparse
import json
import sys
from copy import deepcopy


def units_by_id(scenario):
    return {unit["unit_id"]: unit for unit in scenario.get("units", [])}


def candidate_eligible(candidate, units):
    unit = units.get(candidate["target_id"])
    if unit is None:
        return False
    if not unit.get("can_target", True):
        return False
    if not unit.get("line_of_effect", True):
        return False
    if unit.get("spell_immune", False):
        return False
    if unit.get("ai_ignored", False):
        return False
    return True


def select_candidate(candidates, units):
    eligible = []
    for index, candidate in enumerate(candidates):
        if candidate_eligible(candidate, units):
            eligible.append((index, candidate))
    if not eligible:
        return None, 0

    best = None
    for index, candidate in eligible:
        score = candidate.get("routing_score", 0)
        if best is None or score > best[1].get("routing_score", 0):
            best = (index, candidate)
    return best[1], len(eligible)


def compute_row(scenario):
    units = units_by_id(scenario)
    spell = scenario["spell"]
    original = units[spell["original_target_id"]]

    reflect_triggered = bool(spell.get("reflectable", False) and original.get("has_reflect", False))
    selected_score = None
    eligible_count = 0
    fizzled = False
    secondary_reflect_suppressed = False

    if reflect_triggered:
        selected, eligible_count = select_candidate(spell.get("reflection_candidates", []), units)
        if selected is None:
            final_target_id = "none"
            final_team = "none"
            fizzled = True
        else:
            final_target_id = selected["target_id"]
            final_unit = units[final_target_id]
            final_team = final_unit["team"]
            selected_score = selected.get("routing_score", 0)
            secondary_reflect_suppressed = bool(final_unit.get("has_reflect", False))
    else:
        final_target_id = spell["original_target_id"]
        final_team = original["team"]

    caster_team = spell["caster_team"]
    intent = spell["intent"]
    hostile_backfire = bool(intent == "hostile" and final_team == caster_team)
    beneficial_backfire = bool(intent == "beneficial" and final_team != "none" and final_team != caster_team)

    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario.get("model", "spell_routing_reflect"),
        "reflect_triggered": reflect_triggered,
        "final_target_id": final_target_id,
        "final_team": final_team,
        "fizzled": fizzled,
        "eligible_reflection_count": eligible_count,
        "selected_routing_score": selected_score,
        "hostile_backfire": hostile_backfire,
        "beneficial_backfire": beneficial_backfire,
        "secondary_reflect_suppressed": secondary_reflect_suppressed,
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
