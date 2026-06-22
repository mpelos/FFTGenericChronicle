#!/usr/bin/env python3
"""
Claude independent reviewer tool for T3xT5xT8 (KO/corpse/undead state composition).

Implemented from docs/job-balance/37-ko-corpse-undead-state-composition-schema.md and
the pinned bundle work/t3x-t5x-t8-ko-corpse-undead-scenarios-v0.json ONLY. Deliberately
does NOT read tools/check_ko_corpse_undead.py so the dual-independent gate is meaningful.

Recomputes every output field from raw inputs, then compares against:
  (a) the bundle's per-scenario `expected` block, and
  (b) GPT's canonical output if present.
0 mismatches required.
"""

import argparse
import json
import sys


def b(x):
    return "true" if x else "false"


def compute(scn):
    pol = scn["policy"]
    allow_acting = pol.get("allow_acting_bodies", False)
    tgt = scn["target"]

    state = tgt["state"]
    consumed = False
    death_clock = tgt.get("death_clock_ticks")

    succ = 0
    failed = 0
    consumed_count = 0
    created = []
    same_tick = 0
    clock_miss = 0
    suppressed = 0
    outcomes = []
    reasons = []

    for ev in scn["events"]:
        eid = ev["event_id"]
        reason = None

        if consumed:
            reason = "target_consumed"
        elif state not in ev["required_states"]:
            reason = "wrong_state"
        elif not tgt.get("targetable", False):
            reason = "target_ineligible"
        elif not tgt.get("can_reach", False):
            reason = "target_unreachable"
        elif not tgt.get("line_of_effect", False):
            reason = "line_blocked"
        elif set(tgt.get("immunity_tags", [])) & set(ev.get("blocked_by_tags", [])):
            reason = "immune"
        elif death_clock is not None and ev["resolution_delay_ticks"] >= death_clock:
            reason = "death_clock_expired"
            clock_miss += 1
            if ev["resolution_delay_ticks"] == death_clock:
                same_tick += 1

        if reason is None:
            succ += 1
            outcomes.append(f"{eid}:success")
            if ev.get("consume_target"):
                consumed = True
                consumed_count += 1
                state = "consumed"
            cr = ev.get("creates")
            if cr:
                can_act = cr.get("can_act", False)
                if can_act and not allow_acting:
                    can_act = False
                    suppressed += 1
                created.append(
                    f"{eid}:{cr['state']}:{cr['control_owner']}:"
                    f"can_act={b(can_act)}:targetable={b(cr.get('targetable', False))}:"
                    f"expiry={cr.get('expiry_ticks')}"
                )
        else:
            failed += 1
            outcomes.append(f"{eid}:fail:{reason}")
            reasons.append(f"{eid}:{reason}")

    return {
        "scenario_id": scn["scenario_id"],
        "successful_actions": succ,
        "failed_actions": failed,
        "final_target_state": "consumed" if consumed else state,
        "target_consumed": consumed,
        "consumed_count": consumed_count,
        "created_objects": created,
        "same_tick_unsafe_count": same_tick,
        "death_clock_miss_count": clock_miss,
        "acting_body_suppressed_count": suppressed,
        "event_outcomes": outcomes,
        "failure_reasons": reasons,
    }


LIST_FIELDS = {"created_objects", "event_outcomes", "failure_reasons"}
BOOL_FIELDS = {"target_consumed"}
STR_FIELDS = {"final_target_state"}
INT_FIELDS = {"successful_actions", "failed_actions", "consumed_count",
              "same_tick_unsafe_count", "death_clock_miss_count",
              "acting_body_suppressed_count"}


def field_eq(name, a, c):
    if name in LIST_FIELDS:
        return list(a) == list(c)
    if name in BOOL_FIELDS:
        return bool(a) == bool(c)
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
    ap.add_argument("--gpt-output", default="work/gpt-t3x-t5x-t8-ko-corpse-undead-v0.json")
    ap.add_argument("--output", default="work/claude-t3x-t5x-t8-ko-corpse-undead-v0.json")
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
