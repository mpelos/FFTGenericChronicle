#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T4 accuracy/evasion calculator.

Separate implementation from GPT's writer-side counter so the doc-07 dual gate
(0 row mismatch) is meaningful. Implements the formula contract from
work/t4-accuracy-evasion-scenarios-v0.json (doc 09).

Directional model (verified vs FFT Battle Mechanics Guide):
  front = Class, Shield, Accessory, Weapon ; side = Shield, Accessory, Weapon
  (class is front-only) ; rear = Accessory only ; magic = Shield, Accessory (facing-
  independent). trunc after full multiplication. Each evade is a sequential
  independent breakpoint -> displayed hit = base * prod(100 - eff_ev)/100^n.

Clamp (required patch): eff_ev = min(100, max(0, ev * evasion_multiplier)); each
term (100 - eff_ev) stays in [0,100]; final hit clamped to [0,100]. Non-breaking
on the v0 rows; guards against doubled-evasion sign flips.
"""
import json
import sys


def eff(ev, mult):
    return min(100, max(0, ev * mult))


def calc(s):
    a, t, m = s["attack"], s["target"], s["model"]
    base = a["base_hit"]
    mult = t.get("evasion_multiplier", 1)
    factors = {}

    if a.get("line_of_fire") == "blocked" or a.get("panel_vacated_before_resolution"):
        return {"can_target": False, "hit": 0, "factors": {"reason": "untargetable"}}
    if a.get("can_be_evaded") is False:
        return {"can_target": True, "hit": min(100, max(0, base)),
                "factors": {"non_evadable": True}}

    if m == "magical":
        se, ae = eff(t["m_shield_evade"], mult), eff(t["m_accessory_evade"], mult)
        factors = {"m_shield": se, "m_accessory": ae}
        val = base * (100 - se) * (100 - ae) / (100 ** 2)
    else:
        wev_raw = t["weapon_evade"] if t.get("weapon_evade_enabled") else 0
        ce = eff(t["p_class_evade"], mult)
        se = eff(t["p_shield_evade"], mult)
        ae = eff(t["p_accessory_evade"], mult)
        we = eff(wev_raw, mult)
        fac = a["facing"]
        if fac == "front":
            factors = {"class": ce, "shield": se, "accessory": ae, "weapon": we}
            val = base * (100 - ce) * (100 - se) * (100 - ae) * (100 - we) / (100 ** 4)
        elif fac == "side":
            factors = {"shield": se, "accessory": ae, "weapon": we}
            val = base * (100 - se) * (100 - ae) * (100 - we) / (100 ** 3)
        elif fac == "rear":
            factors = {"accessory": ae}
            val = base * (100 - ae) / 100
        else:
            return {"can_target": True, "hit": None,
                    "factors": {"error": f"bad facing {fac}"}}
    hit = min(100, max(0, int(val)))  # truncate, then clamp
    return {"can_target": True, "hit": hit, "factors": factors}


def run(bundle):
    rows, mism = [], []
    for s in bundle["scenarios"]:
        got = calc(s)
        exp = s["expected"]
        ok = got["can_target"] == exp["can_target"] and got["hit"] == exp["hit"]
        rows.append({"scenario_id": s["scenario_id"], "model": s["model"],
                     "can_target": got["can_target"], "calculated_hit": got["hit"],
                     "expected_hit": exp["hit"], "factors": got["factors"],
                     "match": ok})
        if not ok:
            mism.append((s["scenario_id"], got, exp))
    return rows, mism


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "work/t4-accuracy-evasion-scenarios-v0.json"
    with open(path) as f:
        bundle = json.load(f)
    rows, mism = run(bundle)
    print(json.dumps({"rows": sorted(rows, key=lambda r: r["scenario_id"]),
                      "mismatches": mism,
                      "result": "0-mismatch" if not mism else f"{len(mism)} MISMATCH"},
                     indent=2, sort_keys=True))
