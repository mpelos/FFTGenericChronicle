#!/usr/bin/env python3
"""Independent reviewer checker for T3xT5xT11 (area HP over time).

Built solely from docs/job-balance/41-area-hp-over-time-composition-schema.md
and the pinned bundle work/t3x-t5x-t11-area-hp-over-time-scenarios-v0.json.
Does NOT read tools/check_area_hp_over_time.py (preserves dual-independence).

Usage:
  t3xt5xt11_area_hp_over_time_check.py BUNDLE [--gpt GPT_OUTPUT]
"""
import argparse
import json
import sys

NEG_INF = float("-inf")
POS_INF = float("inf")


def tick_schedule(timing):
    """first_tick=start+delay, repeat every interval, while tick<=start+duration,
    cancel ticks >= interrupt_tick if interrupt_tick exists."""
    start = timing["start_tick"]
    delay = timing["first_tick_delay"]
    interval = timing["tick_interval"]
    duration = timing["duration_ticks"]
    interrupt = timing.get("interrupt_tick")
    last = start + duration
    ticks = []
    t = start + delay
    # guard against non-positive interval (would loop forever)
    if interval <= 0:
        raise ValueError("tick_interval must be > 0")
    while t <= last:
        if interrupt is not None and t >= interrupt:
            break
        ticks.append(t)
        t += interval
    return ticks


def is_active(unit, tick):
    start = unit.get("active_start_tick", NEG_INF)
    expire = unit.get("active_expire_tick", POS_INF)
    return start <= tick < expire


def in_area(unit, effect, origin):
    area = effect.get("area", {})
    shape = area.get("shape", "mapwide")
    if shape == "mapwide":
        return True
    center = area.get("center") or origin or {}
    ux, uy = unit.get("x", 0), unit.get("y", 0)
    uz = unit.get("z", 0)
    cx, cy = center.get("x", 0), center.get("y", 0)
    cz = center.get("z", 0)
    vtol = area.get("vertical_tolerance", 0)
    dx, dy = abs(ux - cx), abs(uy - cy)
    if shape == "single":
        return dx == 0 and dy == 0 and abs(uz - cz) <= vtol
    if shape == "diamond":
        return (dx + dy) <= area.get("radius", 0) and abs(uz - cz) <= vtol
    if shape == "square":
        return max(dx, dy) <= area.get("radius", 0) and abs(uz - cz) <= vtol
    raise ValueError("unknown shape %r" % shape)


def matches_group(unit, effect):
    group = effect["target_group"]
    team = unit["team"]
    if group == "allies":
        return team == "ally"
    if group == "enemies":
        return team == "enemy"
    if group == "all_units":
        if effect.get("ally_safe", False) and team == "ally":
            return False
        return True
    raise ValueError("unknown target_group %r" % group)


def simulate(scenario):
    timing = scenario["timing"]
    effect = scenario["effect"]
    origin = scenario.get("origin")
    affects_ko = effect.get("affects_ko", False)
    kind = effect["kind"]
    value = effect["per_target_value"]
    undead_invert = effect.get("undead_inverts_healing", False)

    # mutable hp state, declaration order preserved
    units = scenario["units"]
    hp = {u["unit_id"]: u["hp"] for u in units}
    max_hp = {u["unit_id"]: u["max_hp"] for u in units}

    ticks = tick_schedule(timing)

    affected_by_tick = []
    raw_heal_by_tick, eff_heal_by_tick, overheal_by_tick = [], [], []
    raw_dmg_by_tick, eff_dmg_by_tick, overkill_by_tick = [], [], []
    hp_by_tick = []
    ko_ids = []

    for tick in ticks:
        affected = []
        rh = eh = oh = rd = ed = ok = 0
        for u in units:
            uid = u["unit_id"]
            if not is_active(u, tick):
                continue
            if not u.get("targetable", True):
                continue
            if not in_area(u, effect, origin):
                continue
            if not matches_group(u, effect):
                continue
            if hp[uid] <= 0 and not affects_ko:
                continue
            affected.append(uid)

            # determine heal vs damage for this unit/tick
            this_kind = kind
            if kind == "healing" and undead_invert and u.get("undead", False):
                this_kind = "damage"

            if this_kind == "healing":
                raw = value
                eff = min(raw, max_hp[uid] - hp[uid])
                over = raw - eff
                hp[uid] = min(max_hp[uid], hp[uid] + eff)
                rh += raw
                eh += eff
                oh += over
            else:  # damage
                raw = value
                eff = min(raw, hp[uid])
                over = raw - eff
                hp[uid] = max(0, hp[uid] - eff)
                rd += raw
                ed += eff
                ok += over
                if hp[uid] <= 0 and uid not in ko_ids:
                    ko_ids.append(uid)

        affected_by_tick.append(affected)
        raw_heal_by_tick.append(rh)
        eff_heal_by_tick.append(eh)
        overheal_by_tick.append(oh)
        raw_dmg_by_tick.append(rd)
        eff_dmg_by_tick.append(ed)
        overkill_by_tick.append(ok)
        hp_by_tick.append({u["unit_id"]: hp[u["unit_id"]] for u in units})

    no_effect = all(len(a) == 0 for a in affected_by_tick)

    return {
        "scenario_id": scenario["scenario_id"],
        "model": scenario["model"],
        "tick_times": ticks,
        "tick_count": len(ticks),
        "affected_ids_by_tick": affected_by_tick,
        "raw_heal_by_tick": raw_heal_by_tick,
        "effective_heal_by_tick": eff_heal_by_tick,
        "overheal_by_tick": overheal_by_tick,
        "raw_damage_by_tick": raw_dmg_by_tick,
        "effective_damage_by_tick": eff_dmg_by_tick,
        "overkill_by_tick": overkill_by_tick,
        "hp_by_tick": hp_by_tick,
        "total_raw_heal": sum(raw_heal_by_tick),
        "total_effective_heal": sum(eff_heal_by_tick),
        "total_overheal": sum(overheal_by_tick),
        "total_raw_damage": sum(raw_dmg_by_tick),
        "total_effective_damage": sum(eff_dmg_by_tick),
        "total_overkill": sum(overkill_by_tick),
        "ko_ids": ko_ids,
        "no_effect": no_effect,
    }


COMPARE_FIELDS = [
    "tick_times", "tick_count", "affected_ids_by_tick",
    "raw_heal_by_tick", "effective_heal_by_tick", "overheal_by_tick",
    "raw_damage_by_tick", "effective_damage_by_tick", "overkill_by_tick",
    "hp_by_tick",
    "total_raw_heal", "total_effective_heal", "total_overheal",
    "total_raw_damage", "total_effective_damage", "total_overkill",
    "ko_ids", "no_effect",
]


def diff(a, b):
    out = {}
    for f in COMPARE_FIELDS:
        if a.get(f) != b.get(f):
            out[f] = {"mine": a.get(f), "other": b.get(f)}
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("bundle")
    ap.add_argument("--gpt")
    ap.add_argument("--against-expected", action="store_true",
                    help="also compare against bundle's own expected blocks")
    args = ap.parse_args()

    with open(args.bundle) as f:
        bundle = json.load(f)
    scenarios = bundle["scenarios"]

    mine = [simulate(s) for s in scenarios]
    mine_by_id = {r["scenario_id"]: r for r in mine}

    print("scenarios=%d" % len(scenarios))

    # 1) self-consistency vs bundle expected
    exp_mismatch = 0
    if args.against_expected:
        for s in scenarios:
            sid = s["scenario_id"]
            d = diff(mine_by_id[sid], s["expected"])
            if d:
                exp_mismatch += 1
                print("EXPECTED-MISMATCH %s: %s" % (sid, json.dumps(d)))
        print("expected_mismatch_count=%d" % exp_mismatch)

    # 2) vs GPT output
    gpt_mismatch = 0
    if args.gpt:
        with open(args.gpt) as f:
            gpt = json.load(f)
        gpt_by_id = {r["scenario_id"]: r for r in gpt["rows"]}
        if set(gpt_by_id) != set(mine_by_id):
            print("ID-SET-MISMATCH mine=%s gpt=%s" % (
                sorted(mine_by_id), sorted(gpt_by_id)))
        for sid in mine_by_id:
            if sid not in gpt_by_id:
                continue
            d = diff(mine_by_id[sid], gpt_by_id[sid])
            if d:
                gpt_mismatch += 1
                print("GPT-MISMATCH %s: %s" % (sid, json.dumps(d)))
        print("gpt_mismatch_count=%d" % gpt_mismatch)

    rc = (1 if exp_mismatch or gpt_mismatch else 0)
    sys.exit(rc)


if __name__ == "__main__":
    main()
