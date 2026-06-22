#!/usr/bin/env python3
"""
Claude independent reviewer tool for T11 (area/terrain model).

Implemented from docs/job-balance/34-area-terrain-model-schema.md and the pinned
bundle work/t11-area-terrain-scenarios-v0.json ONLY. Deliberately does NOT read
tools/check_area_terrain.py so the dual-independent gate is meaningful.

Recomputes every output field from raw inputs, then compares against:
  (a) the bundle's per-scenario `expected` block, and
  (b) GPT's canonical output (work/gpt-t11-area-terrain-v0.json) if present.
0 mismatches required.
"""

import argparse
import json
import sys

DECIMALS = 6


def r6(x):
    return round(float(x), DECIMALS)


def vert_ok(unit, ref_z, vtol):
    return abs(int(unit["z"]) - int(ref_z)) <= int(vtol)


def in_shape(unit, panel, origin, ab):
    """Geometry membership only (group/targetability handled by caller)."""
    shape = ab["shape"]
    vtol = ab.get("vertical_tolerance", 0)
    ux, uy, uz = unit["x"], unit["y"], unit["z"]
    if shape == "mapwide":
        return True  # all units in snapshot; targetability filtered by caller
    if shape == "single":
        return ux == panel["x"] and uy == panel["y"] and vert_ok(unit, panel["z"], vtol)
    if shape == "diamond":
        r = ab["radius"]
        return (abs(ux - panel["x"]) + abs(uy - panel["y"])) <= r and vert_ok(unit, panel["z"], vtol)
    if shape == "square":
        r = ab["radius"]
        return max(abs(ux - panel["x"]), abs(uy - panel["y"])) <= r and vert_ok(unit, panel["z"], vtol)
    if shape == "line":
        length = ab["length"]
        dx = panel["x"] - origin["x"]
        dy = panel["y"] - origin["y"]
        # axis-aligned only: exactly one of dx,dy nonzero; else no-hit candidate.
        if (dx != 0) == (dy != 0):
            return False  # non-axis-aligned (or zero) line -> no hits
        if dx != 0:
            sx = 1 if dx > 0 else -1
            if uy != origin["y"]:
                return False
            off = ux - origin["x"]
            if off == 0 or (1 if off > 0 else -1) != sx:
                return False
            return abs(off) <= length and vert_ok(unit, origin["z"], vtol)
        else:
            sy = 1 if dy > 0 else -1
            if ux != origin["x"]:
                return False
            off = uy - origin["y"]
            if off == 0 or (1 if off > 0 else -1) != sy:
                return False
            return abs(off) <= length and vert_ok(unit, origin["z"], vtol)
    raise ValueError(f"unknown shape {shape}")


def terrain_satisfied(panel, origin, treq):
    if not treq:
        return True
    allowed = set(treq["allowed"])
    if treq["scope"] == "origin_panel":
        return origin.get("terrain") in allowed
    # target_panel
    return panel.get("terrain") in allowed


def candidate_valid(panel, origin, treq):
    if not panel.get("can_target_panel", True):
        return False
    if not panel.get("line_of_effect", True):
        return False
    return terrain_satisfied(panel, origin, treq)


def affected_for_panel(panel, origin, ab, scoring, units):
    """Return (affected_unit_list_in_input_order, enemy_hits, ally_hits, score)."""
    group = ab["target_group"]
    ally_safe = ab.get("ally_safe", False)
    ew = scoring["enemy_weight"]
    aw = scoring["ally_weight"]
    affected = []
    enemy_hits = 0
    ally_hits = 0
    score = 0.0
    for u in units:
        if not u.get("targetable", True):
            continue
        team = u["team"]
        # target group filter
        if group == "enemies" and team != "enemy":
            continue
        if group == "allies" and team != "ally":
            continue
        if group == "all_units" and ally_safe and team == "ally":
            continue
        if not in_shape(u, panel, origin, ab):
            continue
        priority = u.get("priority", 1.0)
        if team == "enemy":
            enemy_hits += 1
            score += priority * ew
        else:
            ally_hits += 1
            score += priority * aw
        affected.append(u["unit_id"])
    return affected, enemy_hits, ally_hits, score


def compute(scn):
    ab = scn["ability"]
    scoring = scn["scoring"]
    origin = scn["origin"]
    treq = scn.get("terrain_requirement")
    units = scn["units"]
    candidates = scn["candidates"]
    min_score = scn.get("minimum_score_to_select", 0)

    total = len(candidates)
    valid_count = 0
    best = None  # (score, idx, panel, affected, eh, ah)
    for idx, panel in enumerate(candidates):
        if not candidate_valid(panel, origin, treq):
            continue
        valid_count += 1
        affected, eh, ah, score = affected_for_panel(panel, origin, ab, scoring, units)
        if best is None or score > best[0]:
            best = (score, idx, panel, affected, eh, ah)

    ratio = r6(valid_count / total) if (treq and total > 0) else None

    none_out = {
        "selected_panel_id": "none",
        "selected_score": 0.0,
        "selected_enemy_hits": 0,
        "selected_ally_hits": 0,
        "selected_target_count": 0,
        "selected_affected_ids": [],
        "valid_candidate_count": valid_count,
        "total_candidate_count": total,
        "terrain_available_ratio": ratio,
    }

    if best is None:
        out = dict(none_out, no_selection_reason="no_valid_candidate")
    elif best[0] < min_score:
        out = dict(none_out, no_selection_reason="below_minimum_score")
    else:
        score, idx, panel, affected, eh, ah = best
        out = {
            "selected_panel_id": panel["panel_id"],
            "selected_score": r6(score),
            "selected_enemy_hits": eh,
            "selected_ally_hits": ah,
            "selected_target_count": len(affected),
            "selected_affected_ids": affected,
            "valid_candidate_count": valid_count,
            "total_candidate_count": total,
            "terrain_available_ratio": ratio,
            "no_selection_reason": "none",
        }
    out["scenario_id"] = scn["scenario_id"]
    return out


FLOAT_FIELDS = {"selected_score", "terrain_available_ratio"}
LIST_FIELDS = {"selected_affected_ids"}
STR_FIELDS = {"selected_panel_id", "no_selection_reason"}
INT_FIELDS = {"selected_enemy_hits", "selected_ally_hits", "selected_target_count",
              "valid_candidate_count", "total_candidate_count"}


def field_eq(name, a, b):
    if name in FLOAT_FIELDS:
        if a is None or b is None:
            return a is None and b is None
        return abs(float(a) - float(b)) < 0.5 * 10 ** (-DECIMALS)
    if name in LIST_FIELDS:
        return list(a) == list(b)
    if name in INT_FIELDS:
        return int(a) == int(b)
    return a == b


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
            if k == "scenario_id":
                continue
            if k not in ref:
                continue
            compared += 1
            if not field_eq(k, v, ref[k]):
                mismatches.append(f"{label}: {sid}.{k} mine={v!r} ref={ref[k]!r}")
    return mismatches, compared


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("bundle")
    ap.add_argument("--gpt-output", default="work/gpt-t11-area-terrain-v0.json")
    ap.add_argument("--output", default="work/claude-t11-area-terrain-v0.json")
    args = ap.parse_args()

    with open(args.bundle) as fh:
        bundle = json.load(fh)
    scenarios = bundle["scenarios"]
    computed = [compute(s) for s in scenarios]

    with open(args.output, "w") as fh:
        json.dump({"scenario_count": len(computed), "rows": computed}, fh, indent=2)

    all_mismatches = []
    total_compared = 0

    expected = [dict(s["expected"], scenario_id=s["scenario_id"])
                for s in scenarios if "expected" in s]
    m, c = diff_rows(computed, expected, "vs-bundle-expected")
    all_mismatches += m
    total_compared += c

    try:
        with open(args.gpt_output) as fh:
            gpt = json.load(fh)
        gpt_rows = gpt.get("rows", [])
        m, c = diff_rows(computed, gpt_rows, "vs-gpt-output")
        all_mismatches += m
        total_compared += c
        if c == 0:
            all_mismatches.append("vs-gpt-output: compared 0 fields (plumbing bug?)")
    except FileNotFoundError:
        print("NOTE: GPT output not found; compared against bundle expected only.")

    print(f"scenario_count={len(computed)}")
    print(f"fields_compared={total_compared}")
    print(f"mismatch_count={len(all_mismatches)}")
    for m in all_mismatches:
        print("  " + m)
    return 1 if all_mismatches else 0


if __name__ == "__main__":
    sys.exit(main())
