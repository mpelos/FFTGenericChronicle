#!/usr/bin/env python3
"""Canonical T8 targeting/challenge checker for Generic Chronicle."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


def load_bundle(path: Path) -> dict[str, Any]:
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def is_eligible(candidate: dict[str, Any]) -> bool:
    return (
        bool(candidate.get("can_target"))
        and bool(candidate.get("can_reach"))
        and bool(candidate.get("line_of_effect"))
        and not bool(candidate.get("ai_ignored"))
    )


def base_score(candidate: dict[str, Any]) -> int:
    return (
        int(candidate.get("tactical_score", 0))
        + int(candidate.get("lethal_bonus", 0))
        + int(candidate.get("self_preservation_bonus", 0))
        + int(candidate.get("objective_bonus", 0))
    )


def score_candidate(
    candidate: dict[str, Any],
    challenge: dict[str, Any],
    eligible: bool,
    ignore_challenge: bool = False,
) -> dict[str, int]:
    bonus = 0
    if (
        eligible
        and not ignore_challenge
        and challenge.get("mode") == "soft"
        and candidate["target_id"] == challenge.get("challenger_target_id")
    ):
        bonus = int(challenge.get("soft_bonus", 0))
    return {
        "base_score": base_score(candidate),
        "challenge_bonus": bonus,
        "total_score": base_score(candidate) + bonus,
    }


def candidate_by_id(candidates: list[dict[str, Any]], candidate_id: str) -> dict[str, Any]:
    for candidate in candidates:
        if candidate["candidate_id"] == candidate_id:
            return candidate
    raise KeyError(f"missing candidate_id: {candidate_id}")


def choose_highest(scored: list[dict[str, Any]]) -> dict[str, Any]:
    if not scored:
        raise ValueError("no eligible candidates")
    return max(enumerate(scored), key=lambda item: (item[1]["score"]["total_score"], -item[0]))[1]


def score_eligible_candidates(
    candidates: list[dict[str, Any]],
    challenge: dict[str, Any],
    ignore_challenge: bool = False,
) -> list[dict[str, Any]]:
    scored: list[dict[str, Any]] = []
    for candidate in candidates:
        eligible = is_eligible(candidate)
        if not eligible:
            continue
        scored.append(
            {
                "candidate": candidate,
                "score": score_candidate(candidate, challenge, eligible, ignore_challenge),
            }
        )
    return scored


def calculate_scenario(scenario: dict[str, Any]) -> dict[str, Any]:
    actor = scenario["actor"]
    challenge = scenario["challenge"]
    candidates = scenario["candidates"]
    expected = scenario["expected"]
    validation_errors: list[str] = []

    control_state = actor.get("control_state", "normal")
    scored = score_eligible_candidates(
        candidates,
        challenge,
        ignore_challenge=control_state != "normal",
    )
    eligible_count = len(scored)
    challenge_bonus_applied_count = sum(
        1 for row in scored if int(row["score"]["challenge_bonus"]) > 0
    )
    hard_challenge_applied = False
    control_override_applied = False
    selection_reason = "score"

    if control_state != "normal":
        control_override_applied = True
        selection_reason = "control_override"
        selected_candidate = candidate_by_id(candidates, actor["control_policy_candidate_id"])
        selected_score = score_candidate(
            selected_candidate,
            challenge,
            is_eligible(selected_candidate),
            ignore_challenge=True,
        )
    elif challenge.get("mode") == "hard" and not bool(actor.get("forced_target_immune")):
        challenger_id = challenge.get("challenger_target_id")
        challenger_rows = [
            row for row in scored if row["candidate"]["target_id"] == challenger_id
        ]
        if challenger_rows:
            selected = choose_highest(challenger_rows)
            selected_candidate = selected["candidate"]
            selected_score = selected["score"]
            hard_challenge_applied = True
            selection_reason = "hard_challenge"
        else:
            selected = choose_highest(scored)
            selected_candidate = selected["candidate"]
            selected_score = selected["score"]
    else:
        selected = choose_highest(scored)
        selected_candidate = selected["candidate"]
        selected_score = selected["score"]

    calculated = {
        "selected_candidate_id": selected_candidate["candidate_id"],
        "selected_target_id": selected_candidate["target_id"],
        "selected_total_score": selected_score["total_score"],
        "selected_challenge_bonus": selected_score["challenge_bonus"],
        "eligible_count": eligible_count,
        "challenge_bonus_applied_count": challenge_bonus_applied_count,
        "hard_challenge_applied": hard_challenge_applied,
        "control_override_applied": control_override_applied,
        "selection_reason": selection_reason,
    }

    for key, expected_value in expected.items():
        value = calculated.get(key)
        if value != expected_value:
            validation_errors.append(f"{key}: expected {expected_value} calculated {value}")

    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario["model"],
        "calculated_fields": calculated,
        "validation_errors": validation_errors,
    }


def canonical_output(bundle: dict[str, Any]) -> dict[str, Any]:
    rows = [calculate_scenario(scenario) for scenario in bundle["scenarios"]]
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
