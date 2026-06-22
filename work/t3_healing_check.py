#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T3 healing/attrition counter.

Separate impl from GPT's writer tool for the doc-07 dual gate. Implements the
contract in work/t3-healing-attrition-scenarios-v0.json (doc 10), action-normalized.

Also reports, per row, which optional paths are EXERCISED (success_chance<1,
per_round_cap binding) so we can see whether the gate actually tests them.
"""
import json
import math
import sys

FLOOR = None  # faith_factor_floor from bundle


def faith_factor(cf, tf, floor):
    return max(floor, (cf / 100) * (tf / 100))


def calc(s, floor):
    a, t, m = s["action"], s["target"], s["model"]
    missing = t["missing_hp"]
    cost = a.get("resource_cost", 0)

    if m == "item_heal" or m == "revive_item":
        raw = a["revive_hp"] if m == "revive_item" else a["item_power"]
    elif m == "spell_heal":
        raw = math.floor(a["spell_k"] * s["actor"]["ma"]
                         * faith_factor(s["actor"]["caster_faith"],
                                        t["target_faith"], floor))
    elif m == "reaction_heal":
        raw = a["item_power"]
    else:
        return {"validation_errors": [f"unknown model {m}"]}

    eff = min(raw, missing)
    over = max(0, raw - missing)

    if m == "reaction_heal":
        eff_triggers = min(a.get("incoming_triggers", 1), a.get("per_round_cap", 1))
        tc = a["trigger_chance"]
        expected = eff * tc * eff_triggers       # across-triggers EV
        consumed = tc * eff_triggers * cost
        uses = eff_triggers                      # uses_resolved = effective_triggers (reaction carve-out)
        total = expected                         # total = expected_heal; generic per-use*uses does NOT apply to reactions
    else:
        sc = a.get("success_chance", 1.0)
        expected = eff * sc
        avail = a.get("resource_available", 0)
        planned = a.get("planned_uses", 1)
        uses = min(planned, math.floor(avail / cost)) if cost else planned
        consumed = cost * uses
        total = expected * uses

    return {
        "scenario_id": s["scenario_id"], "model": m,
        "raw_heal": raw, "effective_heal": eff, "overheal": over,
        "expected_heal": round(expected, 6),
        "resource_consumed": round(consumed, 6),
        "uses_resolved": uses,
        "total_expected_heal": round(total, 6),
    }


def run(bundle):
    floor = bundle["formula_contract"]["faith_factor_floor"]
    rows, mism = [], []
    for s in bundle["scenarios"]:
        got = calc(s, floor)
        exp = s["expected"]
        diffs = {k: (got.get(k), exp[k]) for k in exp if got.get(k) != exp[k]}
        rows.append({**got, "match": not diffs})
        if diffs:
            mism.append({"id": s["scenario_id"], "diffs": diffs})
    return rows, mism


def untested_paths(bundle):
    """Flag gate-integrity gaps: multiplier paths never exercised, identical rows."""
    notes = []
    sc_lt1 = [s["scenario_id"] for s in bundle["scenarios"]
              if s["action"].get("success_chance", 1.0) != 1.0]
    if not sc_lt1:
        notes.append("success_chance<1 NEVER exercised (all 1.0) -> '*success_chance' path untested")
    cap_binds = [s["scenario_id"] for s in bundle["scenarios"]
                 if s["model"] == "reaction_heal"
                 and s["action"].get("incoming_triggers", 1) > s["action"].get("per_round_cap", 1)
                 and s["action"].get("per_round_cap", 1) > 1]
    multi_trigger = [s["scenario_id"] for s in bundle["scenarios"]
                     if s["model"] == "reaction_heal"
                     and min(s["action"].get("incoming_triggers", 1),
                             s["action"].get("per_round_cap", 1)) > 1]
    if not multi_trigger:
        notes.append("no row with effective_triggers>1 -> per_round_cap multi-trigger math untested "
                     "(rows 8/9 are output-identical; cap distinction not gate-tested)")
    return notes


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "work/t3-healing-attrition-scenarios-v0.json"
    with open(path) as f:
        bundle = json.load(f)
    rows, mism = run(bundle)
    print(json.dumps({
        "result": "0-mismatch" if not mism else f"{len(mism)} MISMATCH",
        "rows": len(rows), "mismatches": mism,
        "gate_integrity_notes": untested_paths(bundle),
    }, indent=2))
