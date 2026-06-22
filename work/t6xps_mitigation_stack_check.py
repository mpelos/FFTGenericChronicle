#!/usr/bin/env python3
"""
Claude independent reviewer tool for T6xPS (mitigation-stack composition).

Implemented from docs/job-balance/33-mitigation-stack-composition-schema.md and the
pinned bundle work/t6xps-mitigation-stack-scenarios-v0.json ONLY. Deliberately does
NOT read tools/check_mitigation_stack.py so the dual-independent gate is meaningful.

Recomputes every field from raw inputs, then compares against:
  (a) the bundle's per-scenario `expected` block, and
  (b) GPT's canonical output (work/gpt-t6xps-mitigation-stack-v0.json) if present.
0 mismatches required.
"""

import argparse
import json
import math
import sys

DECIMALS = 6


def r6(x):
    return round(float(x), DECIMALS)


def clamp(x, lo, hi):
    return lo if x < lo else hi if x > hi else x


def compute(scn, contract, armor_response, family_penetration, mitigation_layers):
    ceiling = contract["penetration_ceiling"]
    lo, hi = contract["combined_multiplier_clamp"]
    chip_floor = contract["chip_floor"]

    attack = scn["attack"]
    fam = attack["weapon_family"]
    dtype = attack["damage_type"]
    base_pressure = attack["base_pressure_per_hit"]
    hit_count = attack["hit_count"]
    # penetration_bonus is optional; default 0. Support a couple of natural locations.
    pen_bonus = attack.get("penetration_bonus", scn.get("penetration_bonus", 0)) or 0

    armor_class = scn["target"]["armor_class"]

    base_response = armor_response[armor_class][dtype]

    eff_pen = clamp(family_penetration.get(fam, 0.0) + pen_bonus, 0.0, 1.0)
    if base_response < ceiling:
        penetrated = base_response + eff_pen * (ceiling - base_response)
    else:
        penetrated = base_response

    mit = 1.0
    for layer in scn.get("mitigation", []):
        mit *= mitigation_layers[layer]

    elem = (scn.get("element") or {}).get("multiplier", 1.0)
    zod = (scn.get("zodiac") or {}).get("multiplier", 1.0)

    combined = penetrated * mit * elem * zod
    bounded = clamp(combined, lo, hi)

    damage_per_hit = math.floor(round(base_pressure * bounded, DECIMALS))
    if base_pressure > 0:
        visible_per_hit = max(chip_floor, damage_per_hit)
    else:
        visible_per_hit = 0

    return {
        "scenario_id": scn["scenario_id"],
        "base_response": r6(base_response),
        "effective_penetration": r6(eff_pen),
        "penetrated_response": r6(penetrated),
        "mitigation_multiplier": r6(mit),
        "element_multiplier": r6(elem),
        "zodiac_multiplier": r6(zod),
        "combined_response": r6(combined),
        "bounded_response": r6(bounded),
        "damage_per_hit": damage_per_hit,
        "visible_damage_per_hit": visible_per_hit,
        "total_damage": damage_per_hit * hit_count,
        "visible_total_damage": visible_per_hit * hit_count,
    }


FLOAT_FIELDS = {
    "base_response", "effective_penetration", "penetrated_response",
    "mitigation_multiplier", "element_multiplier", "zodiac_multiplier",
    "combined_response", "bounded_response",
}
INT_FIELDS = {
    "damage_per_hit", "visible_damage_per_hit", "total_damage", "visible_total_damage",
}


def field_eq(name, a, b):
    if a is None or b is None:
        return False
    if name in FLOAT_FIELDS:
        return abs(float(a) - float(b)) < 0.5 * 10 ** (-DECIMALS)
    return int(a) == int(b)


def diff_rows(mine, other, label):
    mismatches = []
    by_id = {r["scenario_id"]: r for r in other}
    for row in mine:
        sid = row["scenario_id"]
        if sid not in by_id:
            mismatches.append(f"{label}: scenario {sid} missing")
            continue
        ref = by_id[sid]
        for k, v in row.items():
            if k == "scenario_id":
                continue
            if k not in ref:
                continue  # ref may not carry every field
            if not field_eq(k, v, ref[k]):
                mismatches.append(f"{label}: {sid}.{k} mine={v} ref={ref[k]}")
    return mismatches


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("bundle")
    ap.add_argument("--gpt-output", default="work/gpt-t6xps-mitigation-stack-v0.json")
    ap.add_argument("--output", default="work/claude-t6xps-mitigation-stack-v0.json")
    args = ap.parse_args()

    with open(args.bundle) as fh:
        bundle = json.load(fh)

    contract = bundle["formula_contract"]
    armor_response = bundle["armor_response"]
    family_penetration = bundle["family_penetration"]
    mitigation_layers = bundle["mitigation_layers"]
    scenarios = bundle["scenarios"]

    computed = [compute(s, contract, armor_response, family_penetration, mitigation_layers)
                for s in scenarios]

    with open(args.output, "w") as fh:
        json.dump({"scenario_count": len(computed), "rows": computed}, fh, indent=2)

    all_mismatches = []

    # (a) compare against bundle expected
    expected_rows = [dict(s["expected"], scenario_id=s["scenario_id"])
                     for s in scenarios if "expected" in s]
    all_mismatches += diff_rows(computed, expected_rows, "vs-bundle-expected")

    # (b) compare against GPT output
    try:
        with open(args.gpt_output) as fh:
            gpt = json.load(fh)
        gpt_rows = gpt.get("rows", gpt.get("scenarios", gpt if isinstance(gpt, list) else []))
        if isinstance(gpt_rows, dict):
            gpt_rows = gpt_rows.get("rows", [])
        # GPT nests computed values under calculated_fields; flatten for comparison.
        flat = []
        for r in gpt_rows:
            cf = r.get("calculated_fields", r)
            flat.append(dict(cf, scenario_id=r["scenario_id"]))
        if not any(k in flat[0] for k in FLOAT_FIELDS):
            all_mismatches.append("vs-gpt-output: could not locate calculated fields in GPT rows")
        all_mismatches += diff_rows(computed, flat, "vs-gpt-output")
    except FileNotFoundError:
        print("NOTE: GPT output not found; compared against bundle expected only.")

    print(f"scenario_count={len(computed)}")
    print(f"mismatch_count={len(all_mismatches)}")
    for m in all_mismatches:
        print("  " + m)
    return 1 if all_mismatches else 0


if __name__ == "__main__":
    sys.exit(main())
