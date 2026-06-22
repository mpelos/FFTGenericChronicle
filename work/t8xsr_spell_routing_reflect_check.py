#!/usr/bin/env python3
"""
Claude independent reviewer tool for T8xSR (spell routing / Reflect).

Implemented from docs/job-balance/38-spell-routing-reflect-composition-schema.md and
the pinned bundle work/t8xsr-spell-routing-reflect-scenarios-v0.json ONLY. Deliberately
does NOT read tools/check_spell_routing_reflect.py so the dual-independent gate holds.

Recomputes every output field from raw inputs, then compares against:
  (a) the bundle's per-scenario `expected` block, and
  (b) GPT's canonical output if present.
0 mismatches required.
"""

import argparse
import json
import sys


def compute(scn):
    spell = scn["spell"]
    units = {u["unit_id"]: u for u in scn["units"]}
    caster_team = spell["caster_team"]
    intent = spell["intent"]

    orig = units.get(spell["original_target_id"], {})
    triggered = bool(spell.get("reflectable")) and bool(orig.get("has_reflect"))

    eligible = []
    final_id = None
    final_team = None
    score = None
    secondary = False

    if not triggered:
        final_id = spell["original_target_id"]
        final_team = orig.get("team")
    else:
        for cand in spell.get("reflection_candidates", []):
            u = units.get(cand["target_id"])
            if u is None:
                continue  # candidate unit must exist
            if not u.get("can_target", True):
                continue
            if not u.get("line_of_effect", True):
                continue
            if u.get("spell_immune", False):
                continue
            if u.get("ai_ignored", False):
                continue
            eligible.append((cand, u))
        if not eligible:
            final_id = "none"
            final_team = "none"
        else:
            # highest routing_score, earliest on tie (stable: iterate, replace on strict >)
            best = None
            for cand, u in eligible:
                if best is None or cand["routing_score"] > best[0]["routing_score"]:
                    best = (cand, u)
            final_id = best[0]["target_id"]
            final_team = best[1].get("team")
            score = best[0]["routing_score"]
            secondary = bool(best[1].get("has_reflect"))

    fizzled = triggered and not eligible

    hostile_backfire = (intent == "hostile" and not fizzled and final_team == caster_team)
    beneficial_backfire = (intent == "beneficial" and not fizzled
                           and final_team != caster_team and final_team is not None)

    return {
        "scenario_id": scn["scenario_id"],
        "reflect_triggered": triggered,
        "final_target_id": final_id,
        "final_team": final_team if final_team is not None else "none",
        "fizzled": fizzled,
        "eligible_reflection_count": len(eligible),
        "selected_routing_score": score,
        "hostile_backfire": hostile_backfire,
        "beneficial_backfire": beneficial_backfire,
        "secondary_reflect_suppressed": secondary,
    }


BOOL_FIELDS = {"reflect_triggered", "fizzled", "hostile_backfire",
               "beneficial_backfire", "secondary_reflect_suppressed"}
NULLABLE_INT = {"selected_routing_score"}
INT_FIELDS = {"eligible_reflection_count"}
STR_FIELDS = {"final_target_id", "final_team"}


def field_eq(name, a, c):
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
    ap.add_argument("--gpt-output", default="work/gpt-t8xsr-spell-routing-reflect-v0.json")
    ap.add_argument("--output", default="work/claude-t8xsr-spell-routing-reflect-v0.json")
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
