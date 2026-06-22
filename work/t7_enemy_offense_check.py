#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T7 enemy-offense/disarm counter.

Separate impl from GPT's writer tool for the doc-07 dual gate. Implements the
formula_contract in work/t7-enemy-offense-scenarios-v0.json (doc 14), reusing the
v0.2.1 family routines on the attacker side and recomputing them from job stats
(not trusting the bundle's expected base_raw_output).

Checks the substantive numeric/boolean fields. output_state is a descriptive
label (free text), reported but flagged separately — labels carry no numeric
authority and should not be a gate field unless deterministically derived.
"""
import json
import math
import sys


def wp_eff(wp, scalar):
    return wp * scalar  # FLOAT, floor only at routine end (v0.2.1 convention)


def routine_output(routine, wp, scalar, st, brave):
    we = wp_eff(wp, scalar)
    pa, ma, spd = st["pa"], st["ma"], st["spd"]
    if routine == "pa_wp":
        return pa * we
    if routine == "br_pa_wp":
        return math.floor(pa * brave / 100) * we
    if routine == "spd_pa_wp":
        return math.floor((pa + spd) / 2) * we
    if routine == "ma_wp":
        return ma * we
    if routine == "wp_wp":
        return we * we
    if routine == "br_pa_pa":
        return math.floor(pa * brave / 100) * pa
    if routine == "rdm_pa_wp":
        return math.floor((pa + 1) / 2) * we
    if routine == "pampa_wp":
        return math.floor((pa + ma) / 2) * we
    raise ValueError(f"unknown routine {routine}")


def stats(bundle, s):
    job = s["attacker"]["job"]
    st = dict(bundle["jobs"][job]["bands"][s["phase"]])
    for k, v in s["attacker"].get("stat_overrides", {}).items():
        st[k] = v
    return st


def calc(bundle, s):
    fams = bundle["families"]
    scalar = bundle["phase_wp_scalar"][s["phase"]]
    brave = bundle["defaults"]["brave"]
    st = stats(bundle, s)

    base_family = s["attack"]["weapon_family"]
    bf = fams[base_family]
    base_raw = math.floor(routine_output(bf["routine"], bf["wp"], scalar, st, brave))

    # permanent replacement (blocked effects ignored, counted)
    resulting_family = base_family
    blocked = 0
    perm_change = False
    for e in s["offense_effects"]:
        if e["kind"] in ("permanent_weapon_break", "permanent_steal", "permanent_disarm"):
            if e.get("blocked_by_safeguard"):
                blocked += 1
                continue
            resulting_family = e.get("fallback_family", "none")
            perm_change = True

    # resulting raw output from resulting family
    if resulting_family == "none":
        rfam = {"routine": "none", "damage_type": "none"}
        resulting_raw = 0
    else:
        rfam = fams[resulting_family]
        resulting_raw = math.floor(routine_output(rfam["routine"], rfam["wp"], scalar, st, brave))

    # temporary pressure after replacement
    jam = any(e["kind"] == "temporary_jam" for e in s["offense_effects"])
    prod = 1.0
    for e in s["offense_effects"]:
        if e["kind"] == "temporary_output_down":
            prod *= e["output_multiplier"]
    if jam or resulting_family == "none":
        eff_mult = 0.0
    else:
        eff_mult = max(0.0, min(1.0, prod))

    oph = math.floor(round(resulting_raw * eff_mult, 6))  # float-safe floor
    return {
        "base_raw_output_per_hit": base_raw,
        "resulting_family": resulting_family,
        "resulting_damage_type": rfam["damage_type"],
        "resulting_routine": rfam["routine"],
        "resulting_raw_output_per_hit": resulting_raw,
        "effective_output_multiplier": round(eff_mult, 6),
        "output_per_hit": oph,
        "total_output": oph * s["attack"]["hit_count"],
        "can_attack": oph > 0,
        "permanent_family_change": perm_change,
        "blocked_effects": blocked,
    }


def run(bundle):
    SUBSTANTIVE = ["base_raw_output_per_hit", "resulting_family", "resulting_damage_type",
                   "resulting_routine", "resulting_raw_output_per_hit",
                   "effective_output_multiplier", "output_per_hit", "total_output",
                   "can_attack", "permanent_family_change", "blocked_effects"]
    rows, mism = [], []
    for s in bundle["scenarios"]:
        got = calc(bundle, s)
        exp = s["expected"]
        diffs = {}
        for k in SUBSTANTIVE:
            g, e = got[k], exp[k]
            if isinstance(g, float) or isinstance(e, float):
                if round(float(g), 6) != round(float(e), 6):
                    diffs[k] = (g, e)
            elif g != e:
                diffs[k] = (g, e)
        rows.append({"id": s["scenario_id"], "match": not diffs})
        if diffs:
            mism.append({"id": s["scenario_id"], "diffs": diffs})
    return rows, mism


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "work/t7-enemy-offense-scenarios-v0.json"
    with open(path) as f:
        bundle = json.load(f)
    rows, mism = run(bundle)
    print(json.dumps({"result": "0-mismatch" if not mism else f"{len(mism)} MISMATCH",
                      "rows": len(rows), "mismatches": mism}, indent=2))
