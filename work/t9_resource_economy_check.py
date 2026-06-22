#!/usr/bin/env python3
"""
Claude independent reviewer tool for T9 (resource/MP economy).

Implemented from docs/job-balance/35-resource-economy-model-schema.md and the pinned
bundle work/t9-resource-economy-scenarios-v0.json ONLY. Deliberately does NOT read
tools/check_resource_economy.py so the dual-independent gate is meaningful.

Recomputes every output field from raw inputs, then compares against:
  (a) the bundle's per-scenario `expected` block, and
  (b) GPT's canonical output (work/gpt-t9-resource-economy-v0.json) if present.
0 mismatches required.
"""

import argparse
import json
import math
import sys

DECIMALS = 6


def r6(x):
    return round(float(x), DECIMALS)


def prod(xs):
    p = 1.0
    for x in xs:
        p *= x
    return p


def effective_cost(base_cost, scenario_mults, event_mults, min_cost):
    raw = base_cost * prod(scenario_mults) * prod(event_mults)
    return max(min_cost, math.ceil(round(raw, DECIMALS)))


def compute(scn):
    max_r = scn["resource"]["max_resource"]
    cur = min(max(scn["resource"]["starting_resource"], 0), max_r)
    scenario_mults = scn.get("cost_multipliers", [])
    scn_min_cost = scn.get("min_cost", 0)

    paid = 0
    recovered = 0
    lost = 0
    overcap = 0
    succ = 0
    failed = 0
    outcomes = []
    minimum = cur  # includes initial clamped value, sampled after each event

    def apply_recovery(amount):
        nonlocal cur, recovered, overcap
        before = cur
        new = min(max_r, cur + amount)
        added = new - before
        overcap += max(0, before + amount - max_r)
        cur = new
        recovered += added
        return added

    for ev in scn.get("events", []):
        kind = ev["kind"]
        if kind == "cast":
            cost = effective_cost(ev.get("base_cost", 0), scenario_mults,
                                  ev.get("cost_multipliers", []), scn_min_cost)
            if cur >= cost:
                cur -= cost
                paid += cost
                succ += 1
                outcomes.append(f"cast_success:{cost}")
                if "refund_on_success" in ev:
                    added = apply_recovery(ev["refund_on_success"])
                    outcomes.append(f"refund:{added}")
            else:
                failed += 1
                outcomes.append(f"cast_fail:{cost}")
        elif kind == "recover":
            amt = ev.get("flat_amount", 0) + math.floor(max_r * ev.get("percent_of_max", 0))
            added = apply_recovery(amt)
            outcomes.append(f"recover:{added}")
        elif kind == "drain":
            amt = min(ev["drain_amount"], ev["target_resource"])
            added = apply_recovery(amt)
            outcomes.append(f"drain:{added}")
        elif kind == "resource_damage":
            rl = min(cur, ev["amount"])
            cur -= rl
            lost += rl
            outcomes.append(f"resource_damage:{rl}")
        else:
            raise ValueError(f"unknown event kind {kind}")
        minimum = min(minimum, cur)

    ratio = r6(recovered / paid) if paid > 0 else 0.0

    ref_cost = ref_remaining = ref_can = None
    if "reference_cast" in scn:
        ref = scn["reference_cast"]
        ref_min = ref.get("min_cost", scn_min_cost)
        ref_cost = effective_cost(ref.get("base_cost", 0), scenario_mults,
                                  ref.get("cost_multipliers", []), ref_min)
        ref_remaining = math.floor(cur / ref_cost) if ref_cost > 0 else 0
        ref_can = ref_remaining > 0

    return {
        "scenario_id": scn["scenario_id"],
        "ending_resource": cur,
        "minimum_resource": minimum,
        "total_cast_cost_paid": paid,
        "total_recovered": recovered,
        "total_resource_lost": lost,
        "overcap_lost": overcap,
        "successful_casts": succ,
        "failed_casts": failed,
        "event_outcomes": outcomes,
        "recovery_to_spend_ratio": ratio,
        "reference_effective_cost": ref_cost,
        "remaining_reference_casts": ref_remaining,
        "can_cast_reference": ref_can,
    }


FLOAT_FIELDS = {"recovery_to_spend_ratio"}
LIST_FIELDS = {"event_outcomes"}
NULLABLE_INT = {"reference_effective_cost", "remaining_reference_casts"}
BOOL_OR_NULL = {"can_cast_reference"}
INT_FIELDS = {"ending_resource", "minimum_resource", "total_cast_cost_paid",
              "total_recovered", "total_resource_lost", "overcap_lost",
              "successful_casts", "failed_casts"}


def field_eq(name, a, b):
    if name in FLOAT_FIELDS:
        return abs(float(a) - float(b)) < 0.5 * 10 ** (-DECIMALS)
    if name in LIST_FIELDS:
        return list(a) == list(b)
    if name in BOOL_OR_NULL:
        return a == b
    if name in NULLABLE_INT:
        if a is None or b is None:
            return a is None and b is None
        return int(a) == int(b)
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
    ap.add_argument("--gpt-output", default="work/gpt-t9-resource-economy-v0.json")
    ap.add_argument("--output", default="work/claude-t9-resource-economy-v0.json")
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
