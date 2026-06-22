#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T6 armor-response counter.

Separate impl from GPT's writer tool for the doc-07 dual gate. Implements the
formula_contract in work/t6-armor-response-scenarios-v0.json (doc 13).

Composition (per axis agreement):
  base = armor_response[class][type]
  eff_pen = clamp(family_pen + penetration_bonus, 0, 1)        # one shared channel
  penetrated = base + eff_pen*(ceiling-base) if base < ceiling else base   # never lowers
  dynamic_delta = sum(response_delta)                          # additive, after penetration
  unclamped = penetrated + dynamic_delta
  final = clamp(unclamped, 0.25, 2.5)
  dmg_per_hit = floor(base_dmg * final); total = dmg_per_hit * hit_count
"""
import json
import math
import sys


def clamp(x, lo, hi):
    return max(lo, min(hi, x))


def calc(s, bundle):
    fc = bundle["formula_contract"]
    ceiling = fc["penetration_ceiling"]
    lo, hi = fc["combined_multiplier_clamp"]
    a, t = s["attack"], s["target"]
    base = bundle["armor_response"][t["armor_class"]][a["damage_type"]]
    fam_pen = bundle["family_penetration"][a["weapon_family"]]
    pen_bonus = sum(e.get("penetration_bonus", 0.0) for e in s["dynamic_effects"])
    eff_pen = clamp(fam_pen + pen_bonus, 0.0, 1.0)
    penetrated = base + eff_pen * (ceiling - base) if base < ceiling else base
    delta = sum(e.get("response_delta", 0.0) for e in s["dynamic_effects"])
    unclamped = penetrated + delta
    final = clamp(unclamped, lo, hi)
    dph = math.floor(round(a["base_damage_per_hit"] * final, 6))  # float-safe floor
    return {
        "base_response": round(base, 6),
        "effective_penetration": round(eff_pen, 6),
        "penetrated_response": round(penetrated, 6),
        "dynamic_delta": round(delta, 6),
        "unclamped_response": round(unclamped, 6),
        "final_response": round(final, 6),
        "damage_per_hit": dph,
        "total_damage": dph * a["hit_count"],
    }


def run(bundle):
    rows, mism = [], []
    for s in bundle["scenarios"]:
        got = calc(s, bundle)
        exp = s["expected"]
        diffs = {k: (got.get(k), exp[k]) for k in exp
                 if round(float(got.get(k)), 6) != round(float(exp[k]), 6)}
        rows.append({"id": s["scenario_id"], **got, "match": not diffs})
        if diffs:
            mism.append({"id": s["scenario_id"], "diffs": diffs})
    return rows, mism


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "work/t6-armor-response-scenarios-v0.json"
    with open(path) as f:
        bundle = json.load(f)
    rows, mism = run(bundle)
    print(json.dumps({"result": "0-mismatch" if not mism else f"{len(mism)} MISMATCH",
                      "rows": len(rows), "mismatches": mism}, indent=2))
