#!/usr/bin/env python3
"""
Claude independent reviewer tool for T5xT8 (timed untargetability composition).

Implemented from docs/job-balance/39-timed-untargetability-composition-schema.md and
the pinned bundle work/t5x-t8-timed-untargetability-scenarios-v0.json ONLY. Deliberately
does NOT read tools/check_timed_untargetability.py so the dual-independent gate holds.

Three models: targeting_decision, delayed_resolution, challenge_decision.
Diffs vs the bundle `expected` block AND GPT's output. 0 mismatches required.
"""

import argparse
import json
import sys


def effect_active(eff, events, tick):
    if eff.get("break_on_damage") and any(e.get("type") == "damage" and e["tick"] <= tick for e in events):
        return False
    if eff.get("break_on_action") and any(e.get("type") == "action" and e["tick"] <= tick for e in events):
        return False
    return eff.get("start_tick", 0) <= tick < eff.get("expire_tick", 0)


def active_effects(unit, tick):
    events = unit.get("events", [])
    return [eff for eff in unit.get("untargetable_effects", []) if effect_active(eff, events, tick)]


def candidate_eligible(cand, unit, tick):
    if unit is None:
        return False, False
    untargetable = len(active_effects(unit, tick)) > 0
    base_ok = (unit.get("can_target", True)
               and not unit.get("ai_ignored", False)
               and cand.get("can_reach", True)
               and cand.get("line_of_effect", True))
    eligible = base_ok and not untargetable
    return eligible, untargetable


def collect(scn, tick):
    """Shared eligibility pass for targeting/challenge models."""
    units = {u["unit_id"]: u for u in scn["units"]}
    eligible = []
    filtered_ids = set()
    for cand in scn["candidates"]:
        unit = units.get(cand["target_id"])
        elig, untargetable = candidate_eligible(cand, unit, tick)
        if untargetable:
            filtered_ids.add(cand["target_id"])
        if elig:
            eligible.append(cand)
    return eligible, sorted(filtered_ids)


def targeting(scn):
    tick = scn["decision_tick"]
    eligible, filtered = collect(scn, tick)
    if not eligible:
        sel_cid = sel_tid = "none"
        score = None
    else:
        best = None
        for c in eligible:
            if best is None or c["tactical_score"] > best["tactical_score"]:
                best = c
        sel_cid = best["candidate_id"]
        sel_tid = best["target_id"]
        score = best["tactical_score"]
    return {
        "scenario_id": scn["scenario_id"],
        "selected_candidate_id": sel_cid,
        "selected_target_id": sel_tid,
        "selected_total_score": score,
        "eligible_count": len(eligible),
        "untargetable_filtered_count": len(filtered),
        "untargetable_filtered_target_ids": filtered,
        "no_eligible_targets": not eligible,
    }


def challenge(scn):
    tick = scn["decision_tick"]
    ch = scn["challenge"]
    mode = ch["mode"]
    challenger = ch.get("challenger_target_id")
    soft_bonus = ch.get("soft_bonus", 0)
    eligible, filtered = collect(scn, tick)

    hard_applied = False
    reason = "score"
    applied_count = 0
    sel = None
    sel_bonus = 0

    if not eligible:
        sel_cid = sel_tid = "none"
        score = None
    else:
        if mode == "hard" and any(c["target_id"] == challenger for c in eligible):
            # force the (eligible) challenger target
            sel = next(c for c in eligible if c["target_id"] == challenger)
            hard_applied = True
            reason = "hard_challenge"
            score = sel["tactical_score"]
        else:
            # ordinary scoring; soft adds bonus to eligible challenger candidate
            best = None
            best_total = None
            for c in eligible:
                total = c["tactical_score"]
                if mode == "soft" and c["target_id"] == challenger:
                    total += soft_bonus
                if best is None or total > best_total:
                    best, best_total = c, total
            sel = best
            score = best_total
            if mode == "soft" and sel["target_id"] == challenger:
                sel_bonus = soft_bonus
            # applied_count = eligible challenger candidates that received the bonus
            if mode == "soft":
                applied_count = sum(1 for c in eligible if c["target_id"] == challenger)
        sel_cid = sel["candidate_id"]
        sel_tid = sel["target_id"]

    return {
        "scenario_id": scn["scenario_id"],
        "selected_candidate_id": sel_cid,
        "selected_target_id": sel_tid,
        "selected_total_score": score,
        "selected_challenge_bonus": sel_bonus,
        "eligible_count": len(eligible),
        "untargetable_filtered_count": len(filtered),
        "untargetable_filtered_target_ids": filtered,
        "challenge_bonus_applied_count": applied_count,
        "hard_challenge_applied": hard_applied,
        "selection_reason": reason,
        "no_eligible_targets": not eligible,
    }


def delayed(scn):
    units = {u["unit_id"]: u for u in scn["units"]}
    act = scn["action"]
    tick = act["resolution_tick"]
    unit = units.get(act["target_id"])
    acts = active_effects(unit, tick) if unit else []
    untargetable = len(acts) > 0
    resolves = (unit is not None
                and unit.get("can_target", True)
                and not unit.get("ai_ignored", False)
                and act.get("line_of_effect_at_resolution", True)
                and act.get("can_reach_at_resolution", True)
                and not untargetable)
    return {
        "scenario_id": scn["scenario_id"],
        "target_active_untargetable_effects": [e["kind"] for e in acts],
        "target_untargetable_at_resolution": untargetable,
        "resolves_on_target": resolves,
        "fizzle_reason": "resolved" if resolves else ("target_untargetable" if untargetable else "target_invalid"),
    }


DISPATCH = {"targeting_decision": targeting, "challenge_decision": challenge,
            "delayed_resolution": delayed}


def compute(scn):
    return DISPATCH[scn["model"]](scn)


LIST_FIELDS = {"untargetable_filtered_target_ids", "target_active_untargetable_effects"}
BOOL_FIELDS = {"no_eligible_targets", "hard_challenge_applied",
               "target_untargetable_at_resolution", "resolves_on_target"}
NULLABLE_INT = {"selected_total_score"}
INT_FIELDS = {"eligible_count", "untargetable_filtered_count",
              "selected_challenge_bonus", "challenge_bonus_applied_count"}


def field_eq(name, a, c):
    if name in LIST_FIELDS:
        return list(a) == list(c)
    if name in BOOL_FIELDS:
        return bool(a) == bool(c)
    if name in NULLABLE_INT:
        if a is None or c is None:
            return a is None and c is None
        return int(a) == int(c)
    if name in INT_FIELDS:
        return int(a) == int(c)
    return a == c


def diff_rows(mine, other, label):
    mismatches = []
    by_id = {r["scenario_id"]: r for r in other}
    compared = 0
    for row in mine:
        sid = row["scenario_id"]
        if sid not in by_id:
            mismatches.append(f"{label}: scenario {sid} missing in ref")
            continue
        ref = by_id[sid]
        for k, v in row.items():
            if k == "scenario_id" or k not in ref:
                continue
            compared += 1
            if not field_eq(k, v, ref[k]):
                mismatches.append(f"{label}: {sid}.{k} mine={v!r} ref={ref[k]!r}")
    return mismatches, compared


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("bundle")
    ap.add_argument("--gpt-output", default="work/gpt-t5x-t8-timed-untargetability-v0.json")
    ap.add_argument("--output", default="work/claude-t5x-t8-timed-untargetability-v0.json")
    args = ap.parse_args()

    with open(args.bundle) as fh:
        bundle = json.load(fh)
    scenarios = bundle["scenarios"]
    computed = [compute(s) for s in scenarios]

    with open(args.output, "w") as fh:
        json.dump({"scenario_count": len(computed), "rows": computed}, fh, indent=2)

    all_m = []
    total_compared = 0
    expected = [dict(s["expected"], scenario_id=s["scenario_id"])
                for s in scenarios if "expected" in s]
    m, c = diff_rows(computed, expected, "vs-bundle-expected")
    all_m += m
    total_compared += c
    try:
        with open(args.gpt_output) as fh:
            gpt = json.load(fh)
        m, c = diff_rows(computed, gpt.get("rows", []), "vs-gpt-output")
        all_m += m
        total_compared += c
        if c == 0:
            all_m.append("vs-gpt-output: compared 0 fields (plumbing bug?)")
    except FileNotFoundError:
        print("NOTE: GPT output not found; compared against bundle expected only.")

    print(f"scenario_count={len(computed)}")
    print(f"fields_compared={total_compared}")
    print(f"mismatch_count={len(all_m)}")
    for x in all_m:
        print("  " + x)
    return 1 if all_m else 0


if __name__ == "__main__":
    sys.exit(main())
