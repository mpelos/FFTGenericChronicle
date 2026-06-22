#!/usr/bin/env python3
"""
Claude independent reviewer tool for T10 (turn-grant / action-economy).

Implemented from docs/job-balance/36-action-economy-model-schema.md and the pinned
bundle work/t10-action-economy-scenarios-v0.json ONLY. Deliberately does NOT read
tools/check_action_economy.py so the dual-independent gate is meaningful.

Recomputes every output field from raw inputs, then compares against:
  (a) the bundle's per-scenario `expected` block, and
  (b) GPT's canonical output (work/gpt-t10-action-economy-v0.json) if present.
0 mismatches required.
"""

import argparse
import json
import sys


def compute(scn):
    pol = scn["policy"]
    party_cap = pol["party_grant_cap"]
    per_target_cap = pol["per_target_grant_cap"]
    block_granted = pol["block_granted_action_grants"]

    units = {u["unit_id"]: u for u in scn["units"]}

    party_count = 0
    target_counts = {}
    succ = 0
    failed = 0
    spent = 0
    outcomes = []
    reasons = []

    for ev in scn["events"]:
        eid = ev["event_id"]
        src = units.get(ev["source_unit_id"], {})
        tgt_id = ev["target_unit_id"]
        tgt = units.get(tgt_id, {})
        # source_action_spent counted whenever the attempt occurs (success or fail).
        spent += ev.get("source_action_spent", 0)

        reason = None
        if ev.get("trigger_context") == "granted_action" and block_granted:
            reason = "recursion_blocked"
        elif not src.get("can_act", False):
            reason = "source_cannot_act"
        elif not tgt.get("can_receive_grant", False):
            reason = "target_cannot_receive"
        elif target_counts.get(tgt_id, 0) >= per_target_cap:
            reason = "per_target_cap"
        elif party_count >= party_cap:
            reason = "party_cap"

        if reason is None:
            succ += 1
            party_count += 1
            target_counts[tgt_id] = target_counts.get(tgt_id, 0) + 1
            outcomes.append(f"{eid}:success")
        else:
            failed += 1
            outcomes.append(f"{eid}:fail:{reason}")
            reasons.append(f"{eid}:{reason}")

    return {
        "scenario_id": scn["scenario_id"],
        "successful_grants": succ,
        "failed_grants": failed,
        "total_source_actions_spent": spent,
        "net_action_delta": succ - spent,
        "party_grant_count": party_count,
        "target_grant_counts": target_counts,
        "event_outcomes": outcomes,
        "failure_reasons": reasons,
    }


LIST_FIELDS = {"event_outcomes", "failure_reasons"}
DICT_FIELDS = {"target_grant_counts"}
INT_FIELDS = {"successful_grants", "failed_grants", "total_source_actions_spent",
              "net_action_delta", "party_grant_count"}


def field_eq(name, a, b):
    if name in LIST_FIELDS:
        return list(a) == list(b)
    if name in DICT_FIELDS:
        return {k: int(v) for k, v in a.items()} == {k: int(v) for k, v in b.items()}
    if name in INT_FIELDS:
        return int(a) == int(b)
    return a == b


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
    ap.add_argument("--gpt-output", default="work/gpt-t10-action-economy-v0.json")
    ap.add_argument("--output", default="work/claude-t10-action-economy-v0.json")
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
