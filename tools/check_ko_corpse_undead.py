#!/usr/bin/env python3
"""Canonical GPT checker for T3xT5xT8 KO/corpse/undead state composition."""

import argparse
import json
import sys
from copy import deepcopy


def intersects(left, right):
    return bool(set(left or []) & set(right or []))


def eligible(target, event):
    if target.get("consumed", False):
        return False, "target_consumed"
    if target["state"] not in event.get("required_states", []):
        return False, "wrong_state"
    if not target.get("targetable", True):
        return False, "target_ineligible"
    if not target.get("can_reach", True):
        return False, "target_unreachable"
    if not target.get("line_of_effect", True):
        return False, "line_blocked"
    if intersects(target.get("immunity_tags", []), event.get("blocked_by_tags", [])):
        return False, "immune"
    return True, "eligible"


def death_clock_ok(target, event):
    clock = target.get("death_clock_ticks")
    if clock is None:
        return True, False
    delay = event.get("resolution_delay_ticks", 0)
    return delay < clock, delay == clock


def created_summary(event_id, created):
    return (
        f"{event_id}:{created['state']}:{created['control_owner']}:"
        f"can_act={str(created['can_act']).lower()}:"
        f"targetable={str(created['targetable']).lower()}:"
        f"expiry={created['expiry_ticks']}"
    )


def compute_row(scenario):
    policy = scenario.get("policy", {})
    allow_acting = policy.get("allow_acting_bodies", False)
    target = deepcopy(scenario["target"])

    successful_actions = 0
    failed_actions = 0
    consumed_count = 0
    same_tick_unsafe_count = 0
    death_clock_miss_count = 0
    acting_body_suppressed_count = 0
    created_objects = []
    event_outcomes = []
    failure_reasons = []

    for event in scenario.get("events", []):
        event_id = event["event_id"]
        ok, reason = eligible(target, event)
        if ok:
            ok, same_tick = death_clock_ok(target, event)
            if same_tick:
                same_tick_unsafe_count += 1
            if not ok:
                reason = "death_clock_expired"
                death_clock_miss_count += 1

        if not ok:
            failed_actions += 1
            event_outcomes.append(f"{event_id}:fail:{reason}")
            failure_reasons.append(f"{event_id}:{reason}")
            continue

        successful_actions += 1
        event_outcomes.append(f"{event_id}:success")

        if event.get("consume_target", False):
            target["consumed"] = True
            target["state"] = "consumed"
            consumed_count += 1

        created = event.get("creates")
        if created:
            requested_can_act = bool(created.get("can_act", False))
            final_can_act = requested_can_act and allow_acting
            if requested_can_act and not allow_acting:
                acting_body_suppressed_count += 1
            created_objects.append(
                created_summary(
                    event_id,
                    {
                        "state": created["state"],
                        "control_owner": created.get("control_owner", "none"),
                        "can_act": final_can_act,
                        "targetable": bool(created.get("targetable", True)),
                        "expiry_ticks": created.get("expiry_ticks"),
                    },
                )
            )

    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario.get("model", "ko_corpse_undead_state"),
        "successful_actions": successful_actions,
        "failed_actions": failed_actions,
        "final_target_state": target["state"],
        "target_consumed": bool(target.get("consumed", False)),
        "consumed_count": consumed_count,
        "created_objects": created_objects,
        "same_tick_unsafe_count": same_tick_unsafe_count,
        "death_clock_miss_count": death_clock_miss_count,
        "acting_body_suppressed_count": acting_body_suppressed_count,
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
