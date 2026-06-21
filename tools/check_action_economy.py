#!/usr/bin/env python3
"""Canonical GPT checker for T10 action grants and action-economy caps."""

import argparse
import json
import sys
from copy import deepcopy


def unit_by_id(scenario):
    return {unit["unit_id"]: unit for unit in scenario.get("units", [])}


def resolve_event(event, scenario, units, target_counts, party_count):
    policy = scenario["policy"]
    source = units[event["source_unit_id"]]
    target = units[event["target_unit_id"]]

    if event.get("trigger_context") == "granted_action" and policy.get("block_granted_action_grants", True):
        return False, "recursion_blocked"
    if not source.get("can_act", True):
        return False, "source_cannot_act"
    if not target.get("can_receive_grant", True):
        return False, "target_cannot_receive"
    if target_counts.get(target["unit_id"], 0) >= policy["per_target_grant_cap"]:
        return False, "per_target_cap"
    if party_count >= policy["party_grant_cap"]:
        return False, "party_cap"
    return True, "success"


def compute_row(scenario):
    units = unit_by_id(scenario)
    target_counts = {}
    party_count = 0
    successful_grants = 0
    failed_grants = 0
    total_source_actions_spent = 0
    event_outcomes = []
    failure_reasons = []

    for event in scenario.get("events", []):
        spent = int(event.get("source_action_spent", 0))
        total_source_actions_spent += spent
        success, reason = resolve_event(event, scenario, units, target_counts, party_count)
        event_id = event["event_id"]
        if success:
            target_id = event["target_unit_id"]
            target_counts[target_id] = target_counts.get(target_id, 0) + 1
            party_count += 1
            successful_grants += 1
            event_outcomes.append(f"{event_id}:success")
        else:
            failed_grants += 1
            event_outcomes.append(f"{event_id}:fail:{reason}")
            failure_reasons.append(f"{event_id}:{reason}")

    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario.get("model", "action_economy"),
        "successful_grants": successful_grants,
        "failed_grants": failed_grants,
        "total_source_actions_spent": total_source_actions_spent,
        "net_action_delta": successful_grants - total_source_actions_spent,
        "party_grant_count": party_count,
        "target_grant_counts": dict(sorted(target_counts.items())),
        "event_outcomes": event_outcomes,
        "failure_reasons": failure_reasons,
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
