#!/usr/bin/env python3
"""Canonical incidence counter for job-balance T2 benchmark bundles."""
from __future__ import annotations

import argparse
import json
from collections import defaultdict
from pathlib import Path
from typing import Any

SCALAR_SLOTS = ("secondary", "reaction", "support", "movement")
ARRAY_SLOTS = ("equipment_unlocks",)
INCIDENCE_KEYS = {
    "secondary": "incidence_by_secondary",
    "reaction": "incidence_by_reaction",
    "support": "incidence_by_support",
    "movement": "incidence_by_movement",
    "equipment_unlocks": "incidence_by_equipment_unlock",
}


def load_bundle(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def sorted_dict(mapping: dict[str, Any]) -> dict[str, Any]:
    return {key: mapping[key] for key in sorted(mapping)}


def collect_missing_coverage(bundle: dict[str, Any], records: list[dict[str, Any]]) -> dict[str, list[str]]:
    required = bundle["required_coverage"]
    covered: dict[str, set[str]] = defaultdict(set)

    for record in records:
        covered["primary_roles"].add(record["primary_role"])
        covered["armor_profiles"].add(record["coverage"]["armor_profile"])
        for mode in record["coverage"]["damage_modes"]:
            if mode in required["physical_damage_modes"]:
                covered["physical_damage_modes"].add(mode)
            if mode in required["nonphysical_pressure"]:
                covered["nonphysical_pressure"].add(mode)
        for tag in record["coverage"]["sensitivity_tags"]:
            covered["sensitivity_tags"].add(tag)
        if record["phase"] in required["mandatory_phases"]:
            covered["mandatory_phases"].add(record["phase"])

    missing: dict[str, list[str]] = {}
    for key in (
        "primary_roles",
        "armor_profiles",
        "physical_damage_modes",
        "nonphysical_pressure",
        "sensitivity_tags",
        "mandatory_phases",
    ):
        gap = sorted(set(required[key]) - covered[key])
        if gap:
            missing[key] = gap
    return sorted_dict(missing)


def validate_and_split_records(
    bundle: dict[str, Any], records: list[dict[str, Any]]
) -> tuple[list[dict[str, Any]], dict[str, list[str]], list[str]]:
    statuses = bundle["record_statuses"]
    phase_scope = set(bundle["counting_contract"]["incidence_scope"]["phases"])
    unknown = bundle["counting_contract"]["slot_tokens"]["unknown"]
    ignored: dict[str, list[str]] = defaultdict(list)
    counted: list[dict[str, Any]] = []
    errors: list[str] = []

    for record in records:
        record_id = record["id"]
        status = record["status"]
        if status not in statuses:
            errors.append(f"{record_id}: unknown status {status}")
            continue

        status_counted = bool(statuses[status]["counted"])
        include = bool(record["counting"]["include"])
        if include != status_counted:
            errors.append(
                f"{record_id}: counting.include={include} diverges from status counted={status_counted}"
            )

        if not status_counted:
            ignored[record["counting"]["reason"]].append(record_id)
            continue

        for slot in SCALAR_SLOTS:
            if record["slots"][slot] == unknown:
                errors.append(f"{record_id}: counted record has TBD in {slot}")
        for slot in ARRAY_SLOTS:
            if unknown in record["slots"][slot]:
                errors.append(f"{record_id}: counted record has TBD in {slot}")

        if record["phase"] in phase_scope:
            counted.append(record)

    return counted, {reason: sorted(ids) for reason, ids in sorted(ignored.items())}, sorted(errors)


def incidence_for_slot(
    records: list[dict[str, Any]], slot: str, bundle: dict[str, Any]
) -> dict[str, dict[str, int | float | str]]:
    none = bundle["counting_contract"]["slot_tokens"]["none"]
    denominator = len(records)
    counts: dict[str, int] = defaultdict(int)

    for record in records:
        raw_value = record["slots"][slot]
        values = raw_value if isinstance(raw_value, list) else [raw_value]
        for value in sorted(set(values)):
            if value == none:
                continue
            counts[value] += 1

    rows: dict[str, dict[str, int | float | str]] = {}
    for value in sorted(counts):
        numerator = counts[value]
        fraction = numerator / denominator
        rows[value] = {
            "numerator": numerator,
            "denominator": denominator,
            "fraction": fraction,
            "display_percentage": round(fraction * 100, bundle["counting_contract"]["canonical_output"]["display_percentage_decimals"]),
        }
    return rows


def threshold_hits(
    incidence_rows: dict[str, Any], bundle: dict[str, Any]
) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    warnings: list[dict[str, Any]] = []
    failures: list[dict[str, Any]] = []

    threshold_by_output = {
        "incidence_by_secondary": bundle["thresholds"]["secondary"],
        "incidence_by_reaction": bundle["thresholds"]["reaction_support_movement"],
        "incidence_by_support": bundle["thresholds"]["reaction_support_movement"],
        "incidence_by_movement": bundle["thresholds"]["reaction_support_movement"],
        "incidence_by_equipment_unlock": bundle["thresholds"]["equipment_unlock"],
    }

    for output_key, rows in incidence_rows.items():
        if isinstance(rows, str):
            continue
        thresholds = threshold_by_output[output_key]
        for value, row in rows.items():
            item = {
                "metric": output_key,
                "value": value,
                "numerator": row["numerator"],
                "denominator": row["denominator"],
                "fraction": row["fraction"],
            }
            if row["fraction"] > thresholds["fail_exclusive_gt"]:
                failures.append(item)
            elif row["fraction"] > thresholds["warning_exclusive_gt"]:
                warnings.append(item)

    sort_key = lambda item: (item["metric"], item["value"])
    return sorted(warnings, key=sort_key), sorted(failures, key=sort_key)


def canonical_output(bundle: dict[str, Any]) -> dict[str, Any]:
    records = bundle["benchmark_slots"]
    counted_records, ignored, validation_errors = validate_and_split_records(bundle, records)
    no_data = bundle["counting_contract"]["empty_denominator"]["canonical_token"]

    output: dict[str, Any] = {
        "total_counted_builds": len(counted_records),
    }

    incidence_outputs: dict[str, Any] = {}
    for slot, output_key in INCIDENCE_KEYS.items():
        if not counted_records:
            incidence_outputs[output_key] = no_data
        else:
            incidence_outputs[output_key] = incidence_for_slot(counted_records, slot, bundle)
        output[output_key] = incidence_outputs[output_key]

    output["missing_required_coverage"] = collect_missing_coverage(bundle, records)
    warnings, failures = threshold_hits(incidence_outputs, bundle)
    output["threshold_warnings"] = warnings
    output["threshold_failures"] = failures
    output["validation_errors"] = validation_errors
    output["ignored_records_by_reason"] = sorted_dict(ignored)
    return output


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("bundle", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    output = canonical_output(load_bundle(args.bundle))
    text = json.dumps(output, indent=2, sort_keys=False)
    if args.output:
        args.output.write_text(text + "\n", encoding="utf-8")
    else:
        print(text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
