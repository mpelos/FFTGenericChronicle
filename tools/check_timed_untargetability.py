#!/usr/bin/env python3
"""Canonical GPT checker for T5xT8 timed untargetability composition."""
from __future__ import annotations

import argparse
import json
import sys
from copy import deepcopy


BREAK_EVENT_BY_FLAG = {
    "break_on_action": "action",
    "break_on_damage": "damage",
}


def units_by_id(scenario):
    return {unit["unit_id"]: unit for unit in scenario.get("units", [])}


def effect_broken(effect, events, tick):
    for flag, event_type in BREAK_EVENT_BY_FLAG.items():
        if not effect.get(flag, False):
            continue
        for event in events:
            if event.get("type") == event_type and event.get("tick", 0) <= tick:
                return True
    return False


def active_untargetable_effects(unit, tick):
    active = []
    events = unit.get("events", [])
    for effect in unit.get("untargetable_effects", []):
        start_tick = int(effect.get("start_tick", 0))
        expire_tick = effect.get("expire_tick")
        if tick < start_tick:
            continue
        if expire_tick is not None and tick >= int(expire_tick):
            continue
        if effect_broken(effect, events, tick):
            continue
        active.append(effect["kind"])
    return active


def candidate_eligible(candidate, units, tick):
    unit = units.get(candidate["target_id"])
    if unit is None:
        return False
    if not unit.get("can_target", True):
        return False
    if unit.get("ai_ignored", False):
        return False
    if not candidate.get("can_reach", True):
        return False
    if not candidate.get("line_of_effect", True):
        return False
    if active_untargetable_effects(unit, tick):
        return False
    return True


def candidate_score(candidate, challenge=None, eligible=True):
    base = int(candidate.get("tactical_score", 0))
    bonus = 0
    if (
        challenge
        and eligible
        and challenge.get("mode") == "soft"
        and candidate["target_id"] == challenge.get("challenger_target_id")
    ):
        bonus = int(challenge.get("soft_bonus", 0))
    return {
        "base_score": base,
        "challenge_bonus": bonus,
        "total_score": base + bonus,
    }


def choose_highest(scored):
    if not scored:
        return None
    best = None
    for row in scored:
        if best is None or row["score"]["total_score"] > best["score"]["total_score"]:
            best = row
    return best


def score_candidates(candidates, units, tick, challenge=None):
    scored = []
    filtered_untargetable = []
    for candidate in candidates:
        unit = units.get(candidate["target_id"])
        active = active_untargetable_effects(unit, tick) if unit else []
        eligible = candidate_eligible(candidate, units, tick)
        if active and not eligible:
            filtered_untargetable.append(candidate["target_id"])
        if not eligible:
            continue
        scored.append(
            {
                "candidate": candidate,
                "score": candidate_score(candidate, challenge, eligible=True),
            }
        )
    return scored, sorted(set(filtered_untargetable))


def targeting_decision(scenario, challenge=None):
    units = units_by_id(scenario)
    tick = int(scenario["decision_tick"])
    scored, filtered = score_candidates(scenario["candidates"], units, tick, challenge)
    selected = choose_highest(scored)
    if selected is None:
        selected_candidate_id = "none"
        selected_target_id = "none"
        selected_total_score = None
    else:
        selected_candidate_id = selected["candidate"]["candidate_id"]
        selected_target_id = selected["candidate"]["target_id"]
        selected_total_score = selected["score"]["total_score"]
    return {
        "selected_candidate_id": selected_candidate_id,
        "selected_target_id": selected_target_id,
        "selected_total_score": selected_total_score,
        "eligible_count": len(scored),
        "untargetable_filtered_count": len(filtered),
        "untargetable_filtered_target_ids": filtered,
        "no_eligible_targets": selected is None,
    }


def challenge_decision(scenario):
    units = units_by_id(scenario)
    tick = int(scenario["decision_tick"])
    challenge = scenario["challenge"]
    actor = scenario["actor"]
    scored, filtered = score_candidates(scenario["candidates"], units, tick, challenge)
    challenge_bonus_applied_count = sum(
        1 for row in scored if row["score"]["challenge_bonus"] > 0
    )
    selected = None
    hard_challenge_applied = False
    selection_reason = "score"

    if challenge.get("mode") == "hard" and not actor.get("forced_target_immune", False):
        challenger_rows = [
            row
            for row in scored
            if row["candidate"]["target_id"] == challenge.get("challenger_target_id")
        ]
        if challenger_rows:
            selected = choose_highest(challenger_rows)
            hard_challenge_applied = True
            selection_reason = "hard_challenge"

    if selected is None:
        selected = choose_highest(scored)

    if selected is None:
        selected_candidate_id = "none"
        selected_target_id = "none"
        selected_total_score = None
        selected_challenge_bonus = 0
    else:
        selected_candidate_id = selected["candidate"]["candidate_id"]
        selected_target_id = selected["candidate"]["target_id"]
        selected_total_score = selected["score"]["total_score"]
        selected_challenge_bonus = selected["score"]["challenge_bonus"]

    return {
        "selected_candidate_id": selected_candidate_id,
        "selected_target_id": selected_target_id,
        "selected_total_score": selected_total_score,
        "selected_challenge_bonus": selected_challenge_bonus,
        "eligible_count": len(scored),
        "untargetable_filtered_count": len(filtered),
        "untargetable_filtered_target_ids": filtered,
        "challenge_bonus_applied_count": challenge_bonus_applied_count,
        "hard_challenge_applied": hard_challenge_applied,
        "selection_reason": selection_reason,
        "no_eligible_targets": selected is None,
    }


def delayed_resolution(scenario):
    units = units_by_id(scenario)
    action = scenario["action"]
    tick = int(action["resolution_tick"])
    target_id = action["target_id"]
    target = units.get(target_id)
    active = active_untargetable_effects(target, tick) if target else []
    base_targetable = bool(target and target.get("can_target", True) and not target.get("ai_ignored", False))
    line_ok = bool(action.get("line_of_effect_at_resolution", True))
    reach_ok = bool(action.get("can_reach_at_resolution", True))
    resolves = bool(base_targetable and line_ok and reach_ok and not active)
    if resolves:
        reason = "resolved"
    elif target is None:
        reason = "target_missing"
    elif active:
        reason = "target_untargetable"
    elif not base_targetable:
        reason = "target_invalid"
    elif not line_ok:
        reason = "line_blocked"
    else:
        reason = "out_of_reach"
    return {
        "target_active_untargetable_effects": active,
        "target_untargetable_at_resolution": bool(active),
        "resolves_on_target": resolves,
        "fizzle_reason": reason,
    }


def compute_row(scenario):
    model = scenario["model"]
    if model == "targeting_decision":
        fields = targeting_decision(scenario)
    elif model == "challenge_decision":
        fields = challenge_decision(scenario)
    elif model == "delayed_resolution":
        fields = delayed_resolution(scenario)
    else:
        fields = {"validation_error": f"unknown model: {model}"}
    return {
        "scenario_id": scenario["scenario_id"],
        "model": model,
        **fields,
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
