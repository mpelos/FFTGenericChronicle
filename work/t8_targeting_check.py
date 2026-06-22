#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T8 targeting/challenge counter.

Separate impl from GPT's writer tool for the doc-07 dual gate. Implements the
formula_contract in work/t8-targeting-challenge-scenarios-v0.json (doc 15):
per-enemy decision; eligibility; control override; hard/soft challenge; additive
soft bonus; earliest-in-order tie-break.
"""
import json
import sys


def base_score(c):
    return c["tactical_score"] + c["lethal_bonus"] + c["self_preservation_bonus"] + c["objective_bonus"]


def eligible(c):
    return c["can_target"] and c["can_reach"] and c["line_of_effect"] and not c["ai_ignored"]


def select_max(scored):
    # scored: list of (index, candidate, total). highest total, earliest index wins ties.
    best = None
    for idx, c, total in scored:
        if best is None or total > best[2]:  # strict > keeps earliest on tie
            best = (idx, c, total)
    return best


def calc(s):
    actor = s["actor"]
    ch = s["challenge"]
    cands = s["candidates"]
    elig = [(i, c) for i, c in enumerate(cands) if eligible(c)]
    eligible_count = len(elig)

    # control override takes precedence over challenge
    if actor["control_state"] != "normal":
        cid = actor["control_policy_candidate_id"]
        c = next(c for c in cands if c["candidate_id"] == cid)
        return {
            "selected_candidate_id": c["candidate_id"],
            "selected_target_id": c["target_id"],
            "selected_total_score": base_score(c),
            "selected_challenge_bonus": 0,
            "eligible_count": eligible_count,
            "challenge_bonus_applied_count": 0,
            "hard_challenge_applied": False,
            "control_override_applied": True,
            "selection_reason": "control_override",
        }

    mode = ch["mode"]

    # hard challenge: restrict to eligible challenger candidates if any exist
    if mode == "hard" and not actor["forced_target_immune"]:
        challengers = [(i, c) for i, c in elig if c["target_id"] == ch["challenger_target_id"]]
        if challengers:
            scored = [(i, c, base_score(c)) for i, c in challengers]
            idx, c, total = select_max(scored)
            return {
                "selected_candidate_id": c["candidate_id"],
                "selected_target_id": c["target_id"],
                "selected_total_score": total,
                "selected_challenge_bonus": 0,
                "eligible_count": eligible_count,
                "challenge_bonus_applied_count": 0,
                "hard_challenge_applied": True,
                "control_override_applied": False,
                "selection_reason": "hard_challenge",
            }
        # else fall through to normal scoring

    # normal / soft scoring over eligible candidates
    soft = mode == "soft"
    bonus_val = ch.get("soft_bonus", 0)
    applied = 0
    scored = []
    for i, c in elig:
        gets = soft and c["target_id"] == ch["challenger_target_id"] and bonus_val != 0
        if gets:
            applied += 1
        total = base_score(c) + (bonus_val if gets else 0)
        scored.append((i, c, total, gets))
    idx, c, total = select_max([(i, c, t) for i, c, t, g in scored])
    sel_gets = next(g for i, cc, t, g in scored if i == idx)
    return {
        "selected_candidate_id": c["candidate_id"],
        "selected_target_id": c["target_id"],
        "selected_total_score": total,
        "selected_challenge_bonus": bonus_val if sel_gets else 0,
        "eligible_count": eligible_count,
        "challenge_bonus_applied_count": applied,
        "hard_challenge_applied": False,
        "control_override_applied": False,
        "selection_reason": "score",
    }


def run(bundle):
    FIELDS = ["selected_candidate_id", "selected_target_id", "selected_total_score",
              "selected_challenge_bonus", "eligible_count", "challenge_bonus_applied_count",
              "hard_challenge_applied", "control_override_applied", "selection_reason"]
    rows, mism = [], []
    for s in bundle["scenarios"]:
        got = calc(s)
        exp = s["expected"]
        diffs = {k: (got[k], exp[k]) for k in FIELDS if got[k] != exp[k]}
        rows.append(s["scenario_id"])
        if diffs:
            mism.append({"id": s["scenario_id"], "diffs": diffs})
    return rows, mism


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "work/t8-targeting-challenge-scenarios-v0.json"
    with open(path) as f:
        bundle = json.load(f)
    rows, mism = run(bundle)
    print(json.dumps({"result": "0-mismatch" if not mism else f"{len(mism)} MISMATCH",
                      "rows": len(rows), "mismatches": mism}, indent=2))
