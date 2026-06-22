#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T6xT7 offense/armor composition counter.

Separate impl from GPT's writer tool for the doc-07 dual gate. Implements the
formula_contract in work/t6x-t7-offense-armor-scenarios-v0.json (doc 17): consume
T7 output_per_hit as pre-armor output, apply the EXACT T6 armor-response pipeline
to both the original weapon and the resulting (post-disarm) weapon, and report the
matchup-flip ratios. Float-safe floor(round(x,6)). Recomputed independently.
"""
import json
import math
import sys

CEILING = 1.1
CLAMP = (0.25, 2.5)


def side_response(bundle, armor_class, family, damage_type, dyn):
    if family == "none":
        return 0.0
    base = bundle["armor_response"][armor_class][damage_type]
    fam_pen = bundle["family_penetration"][family]
    # contract: sum ALL penetration_bonus / response_delta values, regardless of effect kind
    pen_bonus = sum(e.get("penetration_bonus", 0) for e in dyn)
    eff_pen = max(0.0, min(1.0, fam_pen + pen_bonus))
    penetrated = base + eff_pen * (CEILING - base) if base < CEILING else base
    delta = sum(e.get("response_delta", 0) for e in dyn)
    return max(CLAMP[0], min(CLAMP[1], penetrated + delta))


def dph(output, response):
    return math.floor(round(output * response, 6))


def ratio(num, den):
    return round(num / den, 6) if den != 0 else 0.0


def calc(bundle, s):
    ac = s["target"]["armor_class"]
    dyn = s["dynamic_effects"]
    o, r = s["original"], s["resulting"]

    o_resp = side_response(bundle, ac, o["family"], o["damage_type"], dyn)
    o_dph = dph(o["output_per_hit"], o_resp)
    o_total = o_dph * o["hit_count"]

    r_resp = side_response(bundle, ac, r["family"], r["damage_type"], dyn)
    r_dph = dph(r["output_per_hit"], r_resp) if r["family"] != "none" else 0
    r_total = r_dph * r["hit_count"]

    return {
        "original_final_response": round(o_resp, 6),
        "original_damage_per_hit": o_dph,
        "original_total_damage": o_total,
        "resulting_final_response": round(r_resp, 6),
        "resulting_damage_per_hit": r_dph,
        "resulting_total_damage": r_total,
        "raw_output_ratio": ratio(r["output_per_hit"], o["output_per_hit"]),
        "response_ratio": ratio(r_resp, o_resp),
        "damage_ratio": ratio(r_total, o_total),
        "total_damage_delta": r_total - o_total,
    }


def run(bundle):
    rows, mism = [], []
    for s in bundle["scenarios"]:
        got = calc(bundle, s)
        exp = s["expected"]
        diffs = {}
        for k, g in got.items():
            e = exp[k]
            if isinstance(g, float) or isinstance(e, float):
                if round(float(g), 6) != round(float(e), 6):
                    diffs[k] = (g, e)
            elif g != e:
                diffs[k] = (g, e)
        rows.append(s["scenario_id"])
        if diffs:
            mism.append({"id": s["scenario_id"], "diffs": diffs})
    return rows, mism


def copy_check(bundle):
    """Copy-drift of embedded T6 tables vs canonical v0.2.1 source."""
    try:
        ref = json.load(open("work/sim-inputs-v0.2.1.json"))
    except FileNotFoundError:
        return ["sim-inputs-v0.2.1.json not found"]
    errs = []
    for ac, resp in bundle["armor_response"].items():
        for dt, v in resp.items():
            rv = ref["armor_response"].get(ac, {}).get(dt)
            if rv != v:
                errs.append(f"armor_response[{ac}][{dt}] bundle={v} ref={rv}")
    for fam, pen in bundle["family_penetration"].items():
        rv = ref["families"].get(fam, {}).get("penetration")
        if rv != pen:
            errs.append(f"family_penetration[{fam}] bundle={pen} ref={rv}")
    if bundle["formula_contract"]["penetration_ceiling"] != ref["calc"]["penetration_ceiling"]:
        errs.append("penetration_ceiling drift")
    if bundle["formula_contract"]["combined_multiplier_clamp"] != ref["calc"]["combined_multiplier_clamp"]:
        errs.append("combined_multiplier_clamp drift")
    return errs


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "work/t6x-t7-offense-armor-scenarios-v0.json"
    with open(path) as f:
        bundle = json.load(f)
    rows, mism = run(bundle)
    errs = copy_check(bundle)
    print(json.dumps({"result": "0-mismatch" if not mism else f"{len(mism)} MISMATCH",
                      "rows": len(rows), "mismatches": mism,
                      "copy_drift_vs_v0.2.1": errs or "0 drift"}, indent=2))
