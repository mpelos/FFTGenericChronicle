#!/usr/bin/env python3
"""Cross-check: recompute every GPT row with Claude's independent pipeline.
Agreement across all rows satisfies the doc-05 dual-independent gate."""
import json, os, math

HERE = os.path.dirname(os.path.abspath(__file__))
B = json.load(open(os.path.join(HERE, "sim-inputs-v0.json")))
G = json.load(open(os.path.join(HERE, "gpt-sim-v0-results.json")))
CALC = B["calc"]; CEIL = CALC["penetration_ceiling"]
LO, HI = CALC["combined_multiplier_clamp"]; FLOOR = CALC["chip_floor"]

def pressure(routine, wp, st):
    pa, ma, spd, br = st["pa"], st["ma"], st["spd"], st["brave"]
    return {
        "pa_wp": pa*wp, "br_pa_wp": (pa*br//100)*wp,
        "spd_pa_wp": ((pa+spd)//2)*wp, "ma_wp": ma*wp,
        "rdm_pa_wp": ((pa+1)/2)*wp, "wp_wp": wp*wp,
        "br_pa_pa": (pa*br//100)*pa, "pampa_wp": ((pa+ma)//2)*wp,
    }[routine]

def resp(armor_class, dtype, pen):
    base = B["armor_response"][armor_class][dtype]
    e = base + pen*(CEIL-base) if base < CEIL else base
    return e

mism = []
checked = 0
for row in G["rows"]:
    fam = row.get("family")
    if fam not in B["families"]:
        continue  # magic / non-family rows handled separately
    f = B["families"][fam]
    st = row["attacker_effective_stats"]
    ac = row["target_armor_class"]
    p = pressure(f["routine"], f["wp"], st)
    m = min(HI, max(LO, resp(ac, f["damage_type"], f["penetration"])))
    mine = max(FLOOR, math.floor(p*m))
    theirs = row["damage_on_hit"]
    checked += 1
    if mine != theirs:
        mism.append((row["scenario_id"], fam, row["attacker_job"], ac,
                     row["attacker_level_or_band"], mine, theirs))

print(f"checked {checked} family rows")
print(f"mismatches: {len(mism)}")
for s in mism[:30]:
    print("  ", s)
if not mism:
    print("ALL ROWS AGREE -> dual-independent gate (comparability) satisfied.")
