#!/usr/bin/env python3
"""
Claude independent reviewer tool for T11xT5 (sustained area throughput).

Implemented from docs/job-balance/40-sustained-area-throughput-composition-schema.md
and the pinned bundle work/t11x-t5-sustained-area-throughput-scenarios-v0.json ONLY.
Deliberately does NOT read tools/check_sustained_area_throughput.py.

Diffs vs the bundle `expected` block AND GPT's output. 0 mismatches required.
"""

import argparse
import json
import sys

DECIMALS = 6


def r6(x):
    return round(float(x), DECIMALS)


def tick_schedule(timing):
    start = timing["start_tick"]
    end = start + timing["duration_ticks"]
    t = start + timing["first_tick_delay"]
    interval = timing["tick_interval"]
    interrupt = timing.get("interrupt_tick")
    ticks = []
    while t <= end:
        if interrupt is not None and t >= interrupt:
            break
        ticks.append(t)
        t += interval
    return ticks


def unit_active(unit, tick):
    if not unit.get("targetable", True):
        return False
    if "active_start_tick" in unit and tick < unit["active_start_tick"]:
        return False
    if "active_expire_tick" in unit and not (tick < unit["active_expire_tick"]):
        return False
    return True


def in_area(unit, area, origin):
    shape = area["shape"]
    if shape == "mapwide":
        return True
    center = area.get("center", origin or {})
    dx = unit.get("x", 0) - center.get("x", 0)
    dy = unit.get("y", 0) - center.get("y", 0)
    dz = unit.get("z", 0) - center.get("z", 0)
    vtol = area.get("vertical_tolerance", 0)
    if abs(dz) > vtol:
        return False
    if shape == "single":
        return dx == 0 and dy == 0
    if shape == "diamond":
        return abs(dx) + abs(dy) <= area["radius"]
    if shape == "square":
        return max(abs(dx), abs(dy)) <= area["radius"]
    raise ValueError(f"unknown shape {shape}")


def compute(scn):
    timing = scn["timing"]
    eff = scn["effect"]
    origin = scn.get("origin")
    area = eff["area"]
    group = eff["target_group"]
    ally_safe = eff.get("ally_safe", False)
    scoring = eff.get("scoring", {})
    ew = scoring.get("enemy_weight", 1)
    aw = scoring.get("ally_weight", 1)
    per_target = eff["per_target_value"]
    cap = eff.get("target_count_cap")
    units = scn["units"]

    ticks = tick_schedule(timing)
    raw_by, eff_by, ids_by, scores_by = [], [], [], []

    for tk in ticks:
        affected = []
        weighted = 0.0
        for u in units:
            team = u["team"]
            if group == "enemies" and team != "enemy":
                continue
            if group == "allies" and team != "ally":
                continue
            if group == "all_units" and ally_safe and team == "ally":
                continue
            if not unit_active(u, tk):
                continue
            if not in_area(u, area, origin):
                continue
            affected.append(u["unit_id"])
            priority = u.get("priority", 1)
            weighted += priority * (ew if team == "enemy" else aw)
        raw = len(affected)
        score = per_target * weighted
        if cap is not None and raw > 0:
            score *= min(raw, cap) / raw
        eff_count = min(raw, cap) if cap is not None else raw
        raw_by.append(raw)
        eff_by.append(eff_count)
        ids_by.append(affected)
        scores_by.append(r6(score))

    total = r6(sum(scores_by))
    return {
        "scenario_id": scn["scenario_id"],
        "tick_times": ticks,
        "tick_count": len(ticks),
        "raw_target_counts_by_tick": raw_by,
        "effective_target_counts_by_tick": eff_by,
        "affected_ids_by_tick": ids_by,
        "per_tick_scores": scores_by,
        "total_score": total,
        "interrupted": timing.get("interrupt_tick") is not None,
        "no_effect": total == 0.0,
    }


FLOAT_LIST = {"per_tick_scores"}
INT_LIST = {"tick_times", "raw_target_counts_by_tick", "effective_target_counts_by_tick"}
NESTED_LIST = {"affected_ids_by_tick"}
FLOAT_FIELDS = {"total_score"}
BOOL_FIELDS = {"interrupted", "no_effect"}
INT_FIELDS = {"tick_count"}


def lists_eq_float(a, b):
    if len(a) != len(b):
        return False
    return all(abs(float(x) - float(y)) < 0.5 * 10 ** (-DECIMALS) for x, y in zip(a, b))


def field_eq(name, a, c):
    if name in FLOAT_LIST:
        return lists_eq_float(a, c)
    if name in INT_LIST:
        return [int(x) for x in a] == [int(x) for x in c]
    if name in NESTED_LIST:
        return [list(x) for x in a] == [list(x) for x in c]
    if name in FLOAT_FIELDS:
        return abs(float(a) - float(c)) < 0.5 * 10 ** (-DECIMALS)
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
    ap.add_argument("--gpt-output", default="work/gpt-t11x-t5-sustained-area-throughput-v0.json")
    ap.add_argument("--output", default="work/claude-t11x-t5-sustained-area-throughput-v0.json")
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
