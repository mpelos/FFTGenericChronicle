#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T3xT5 healing-timing composition counter.

Separate impl from GPT's writer tool for the doc-07 dual gate. Implements the
formula_contract in work/t3x-t5-healing-timing-scenarios-v0.json (doc 16). Three
race models: active_heal_race, reaction_after_damage, revive_race. Reuses the
pinned T3 reaction carve-out (uses_resolved=effective_triggers; per-trigger
expected) and the T5 same-tick-unsafe policy. Recomputed independently; expected
values are NOT trusted.
"""
import json
import sys


def active_heal_race(s):
    h, t, tg = s["healing"], s["target"], s["threat"]
    delay, threat_tick = h["resolution_delay_ticks"], tg["threat_tick"]
    heal, dmg = h["effective_heal"], tg["incoming_damage"]
    hp0, cap = t["hp_before"], t["max_hp"]

    before = delay < threat_tick
    same_tick = delay == threat_tick

    if before:
        # heal resolves first, then damage
        hp_after_heal = min(cap, hp0 + heal)
        hp_after_threat = hp_after_heal - dmg
        heal_resolved = True
        survives = hp_after_threat > 0
        final_hp = max(0, hp_after_threat)
        timed = heal
    else:
        # threat first (incl. same-tick); delayed heal only if nonlethal
        hp_after_threat = hp0 - dmg
        if hp_after_threat > 0:
            heal_resolved = True
            final_hp = min(cap, hp_after_threat + heal)
            survives = True
            timed = heal
        else:
            heal_resolved = False
            final_hp = 0
            survives = False
            timed = 0
    return {
        "heal_before_threat": before,
        "same_tick_unsafe": same_tick,
        "heal_resolved": heal_resolved,
        "hp_after_threat": hp_after_threat,
        "final_hp": final_hp,
        "survives": survives,
        "timed_expected_heal": timed,
    }


def reaction_after_damage(s):
    r, t, tg = s["reaction"], s["target"], s["threat"]
    hp_after_damage = t["hp_before"] - tg["incoming_damage"]
    can_resolve = hp_after_damage > 0
    eff_triggers = min(r["incoming_triggers"], r["per_round_cap"])
    if can_resolve:
        timed = r["effective_heal"] * r["trigger_chance"] * eff_triggers
        resource = r["trigger_chance"] * eff_triggers
        final_hp = hp_after_damage + timed
        survives = True
    else:
        timed = 0
        resource = 0
        final_hp = 0
        survives = False
    return {
        "hp_after_damage": hp_after_damage,
        "reaction_can_resolve": can_resolve,
        "effective_triggers": eff_triggers,
        "timed_expected_heal": round(timed, 6),
        "expected_resource_consumed": round(resource, 6),
        "expected_final_hp": round(final_hp, 6),
        "survives": survives,
    }


def revive_race(s):
    rv, dc = s["revive"], s["death_clock"]
    delay, clock = rv["resolution_delay_ticks"], dc["death_clock_ticks"]
    before = delay < clock
    same_tick = delay == clock
    resolved = before  # same-tick unsafe in T3xT5.0
    return {
        "revive_before_death_clock": before,
        "same_tick_unsafe": same_tick,
        "revive_resolved": resolved,
        "final_hp": rv["revive_hp"] if resolved else 0,
        "timed_expected_heal": rv["expected_heal"] if resolved else 0,
    }


def delivery_comparison(s):
    # run each option through the active-heal race vs the same threat/target
    results = []
    for o in s["options"]:
        sub = {"healing": {"effective_heal": o["effective_heal"],
                           "expected_heal": o["expected_heal"],
                           "resolution_delay_ticks": o["resolution_delay_ticks"]},
               "target": s["target"], "threat": s["threat"]}
        r = active_heal_race(sub)
        results.append((o, r))

    # fastest: min delay, earliest on tie
    fastest = min(results, key=lambda x: x[0]["resolution_delay_ticks"])[0]["option_id"]
    # highest heal: max effective_heal, earliest on tie
    best_heal = max(range(len(results)), key=lambda i: results[i][0]["effective_heal"])
    highest_heal = results[best_heal][0]["option_id"]
    # reliable = survives
    reliable = [(o, r) for o, r in results if r["survives"]]
    reliable_count = len(reliable)
    # best: among survivors, highest final_hp (strict > keeps earliest on tie); if none, earliest overall
    if reliable:
        best = None
        for o, r in reliable:
            if best is None or r["final_hp"] > best[1]["final_hp"]:
                best = (o, r)
    else:
        best = results[0]
    return {
        "fastest_option_id": fastest,
        "highest_heal_option_id": highest_heal,
        "best_option_id": best[0]["option_id"],
        "reliable_option_count": reliable_count,
        "best_final_hp": best[1]["final_hp"],
        "best_timed_expected_heal": best[1]["timed_expected_heal"],
    }


MODELS = {"active_heal_race": active_heal_race,
          "reaction_after_damage": reaction_after_damage,
          "revive_race": revive_race,
          "delivery_comparison": delivery_comparison}


def run(bundle):
    rows, mism = [], []
    for s in bundle["scenarios"]:
        got = MODELS[s["model"]](s)
        exp = s["expected"]
        diffs = {}
        for k, g in got.items():
            e = exp[k]
            if isinstance(g, float) or isinstance(e, float):
                if round(float(g), 6) != round(float(e), 6):
                    diffs[k] = (g, e)
            elif g != e:
                diffs[k] = (g, e)
        # also flag any expected key the counter didn't produce
        extra = [k for k in exp if k not in got]
        if extra:
            diffs["__unchecked_expected_keys__"] = extra
        rows.append(s["scenario_id"])
        if diffs:
            mism.append({"id": s["scenario_id"], "diffs": diffs})
    return rows, mism


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "work/t3x-t5-healing-timing-scenarios-v0.json"
    with open(path) as f:
        bundle = json.load(f)
    rows, mism = run(bundle)
    print(json.dumps({"result": "0-mismatch" if not mism else f"{len(mism)} MISMATCH",
                      "rows": len(rows), "mismatches": mism}, indent=2))
