#!/usr/bin/env python3
"""Canonical GPT checker for T9 resource and MP economy sequences."""

import argparse
import json
import math
import sys
from copy import deepcopy

DECIMALS = 6


def r6(value):
    return round(float(value), DECIMALS)


def clamp(value, low, high):
    return max(low, min(high, value))


def multiplier_product(values):
    product = 1.0
    for value in values:
        product *= float(value)
    return product


def effective_cost(base_cost, min_cost, scenario_multipliers, event_multipliers):
    raw = float(base_cost) * multiplier_product(scenario_multipliers) * multiplier_product(event_multipliers)
    return max(int(min_cost), int(math.ceil(round(raw, DECIMALS))))


def apply_recovery(current, max_resource, amount):
    new_raw = current + amount
    overcap = max(0, new_raw - max_resource)
    return min(max_resource, new_raw), overcap


def recover_amount(event, max_resource):
    return int(event.get("flat_amount", 0)) + int(math.floor(max_resource * float(event.get("percent_of_max", 0.0))))


def compute_row(scenario):
    max_resource = int(scenario["resource"]["max_resource"])
    current = clamp(int(scenario["resource"]["starting_resource"]), 0, max_resource)
    min_seen = current
    scenario_multipliers = scenario.get("cost_multipliers", [])

    total_cast_cost_paid = 0
    total_recovered = 0
    total_resource_lost = 0
    overcap_lost = 0
    successful_casts = 0
    failed_casts = 0
    event_outcomes = []

    for event in scenario.get("events", []):
        kind = event["kind"]
        if kind == "cast":
            cost = effective_cost(
                event["base_cost"],
                event.get("min_cost", scenario.get("min_cost", 0)),
                scenario_multipliers,
                event.get("cost_multipliers", []),
            )
            if current >= cost:
                current -= cost
                total_cast_cost_paid += cost
                successful_casts += 1
                event_outcomes.append(f"cast_success:{cost}")
                if "refund_on_success" in event:
                    refund = int(event["refund_on_success"])
                    current, lost = apply_recovery(current, max_resource, refund)
                    total_recovered += refund - lost
                    overcap_lost += lost
                    event_outcomes.append(f"refund:{refund - lost}")
            else:
                failed_casts += 1
                event_outcomes.append(f"cast_fail:{cost}")
        elif kind == "recover":
            amount = recover_amount(event, max_resource)
            current, lost = apply_recovery(current, max_resource, amount)
            total_recovered += amount - lost
            overcap_lost += lost
            event_outcomes.append(f"recover:{amount - lost}")
        elif kind == "drain":
            amount = min(int(event["drain_amount"]), int(event.get("target_resource", event["drain_amount"])))
            current, lost = apply_recovery(current, max_resource, amount)
            total_recovered += amount - lost
            overcap_lost += lost
            event_outcomes.append(f"drain:{amount - lost}")
        elif kind == "resource_damage":
            amount = min(current, int(event["amount"]))
            current -= amount
            total_resource_lost += amount
            event_outcomes.append(f"resource_damage:{amount}")
        else:
            raise ValueError(f"unsupported event kind: {kind}")

        min_seen = min(min_seen, current)

    if total_cast_cost_paid:
        recovery_to_spend_ratio = r6(total_recovered / total_cast_cost_paid)
    else:
        recovery_to_spend_ratio = 0.0

    row = {
        "scenario_id": scenario["scenario_id"],
        "model": scenario.get("model", "resource_economy"),
        "ending_resource": current,
        "minimum_resource": min_seen,
        "total_cast_cost_paid": total_cast_cost_paid,
        "total_recovered": total_recovered,
        "total_resource_lost": total_resource_lost,
        "overcap_lost": overcap_lost,
        "successful_casts": successful_casts,
        "failed_casts": failed_casts,
        "event_outcomes": event_outcomes,
        "recovery_to_spend_ratio": recovery_to_spend_ratio,
    }

    reference = scenario.get("reference_cast")
    if reference:
        ref_cost = effective_cost(
            reference["base_cost"],
            reference.get("min_cost", scenario.get("min_cost", 0)),
            scenario_multipliers,
            reference.get("cost_multipliers", []),
        )
        row["reference_effective_cost"] = ref_cost
        row["remaining_reference_casts"] = current // ref_cost if ref_cost else 0
        row["can_cast_reference"] = row["remaining_reference_casts"] > 0
    else:
        row["reference_effective_cost"] = None
        row["remaining_reference_casts"] = None
        row["can_cast_reference"] = None

    return row


def equal_field(got, expected):
    if isinstance(got, float) or isinstance(expected, float):
        return math.isclose(float(got), float(expected), abs_tol=0.5 * 10 ** (-DECIMALS))
    return got == expected


def compare(row, expected):
    mismatches = []
    for key, expected_value in expected.items():
        got_value = row.get(key)
        if not equal_field(got_value, expected_value):
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
