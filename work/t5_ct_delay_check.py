#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T5 CT/delay/interrupt counter.

Separate impl from GPT's writer tool for the doc-07 dual gate. Implements the
formula_contract in work/t5-ct-delay-scenarios-v0.json (doc 11) LITERALLY, so any
gap between the written formula and the pinned expected values shows up as a
mismatch (that is the gate's job).
"""
import json
import math
import sys

POST_TURN = {"move_and_act": -100, "move_only": -80, "act_only": -80, "wait": -60}


def ticks_to_turn(ct, speed):
    return 0 if ct >= 100 else math.ceil((100 - ct) / speed)


def calc(s):
    m = s["model"]
    if m == "turn_readiness":
        u = s["unit"]
        return {"ticks_to_turn": ticks_to_turn(u["current_ct"], u["speed"])}
    if m == "post_turn_ct":
        u = s["unit"]
        return {"new_ct": u["current_ct"] + POST_TURN[u["turn_choice"]]}
    if m == "ctr_from_spell_speed":
        ctr = math.ceil(100 / s["action"]["spell_speed"])
        return {"ctr": ctr, "ticks_to_resolution": ctr}
    if m == "delayed_target_safety":
        ttr = s["action"]["ticks_to_resolution"]
        t = s["target"]
        tttt = ticks_to_turn(t["current_ct"], t["speed"])
        safe = ttr < tttt
        whiff = max(0, ttr - tttt)
        # PROPOSED pin: predictability follows target_safe (honors same_tick_policy);
        # whiff_window is the lateness magnitude, informational only.
        pred = "safe" if safe else "unsafe"
        return {"target_ticks_to_turn": tttt, "target_safe": safe,
                "can_resolve_on_target": safe, "whiff_window_ticks": whiff,
                "predictability": pred}
    if m == "interrupt_window":
        a = s["action"]
        return {"interrupts_before_resolution": a["interrupt_tick"] < a["ticks_to_resolution"]}
    if m == "speed_delta":
        u = s["unit"]
        sa = u["speed_before"] + u["speed_delta"]
        return {"ticks_to_turn_before": ticks_to_turn(u["current_ct"], u["speed_before"]),
                "speed_after": sa,
                "ticks_to_turn_after": ticks_to_turn(u["current_ct"], sa)}
    if m == "jump_like":
        return {"jump_ticks": math.ceil(50 / s["unit"]["speed"])}
    return {"validation_errors": [f"unknown model {m}"]}


def run(bundle):
    rows, mism = [], []
    for s in bundle["scenarios"]:
        got = calc(s)
        exp = s["expected"]
        diffs = {k: (got.get(k), exp[k]) for k in exp if got.get(k) != exp[k]}
        rows.append({"scenario_id": s["scenario_id"], "model": s["model"], **got,
                     "match": not diffs})
        if diffs:
            mism.append({"id": s["scenario_id"], "diffs": diffs})
    return rows, mism


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "work/t5-ct-delay-scenarios-v0.json"
    with open(path) as f:
        bundle = json.load(f)
    rows, mism = run(bundle)
    print(json.dumps({"result": "0-mismatch" if not mism else f"{len(mism)} MISMATCH",
                      "rows": len(rows), "mismatches": mism}, indent=2))
