#!/usr/bin/env python3
"""Independent recompute of ALL v0.1 rows (base + stress) vs GPT's results."""
import json, os, math
HERE = os.path.dirname(os.path.abspath(__file__))
B = json.load(open(os.path.join(HERE, "sim-inputs-v0.1.json")))
G = json.load(open(os.path.join(HERE, "gpt-sim-v0.1-results.json")))
CALC=B["calc"]; CEIL=CALC["penetration_ceiling"]; LO,HI=CALC["combined_multiplier_clamp"]; FLOOR=CALC["chip_floor"]
SUPPORT_MULT = {"two_hands": 2.0, "attack_boost": 4/3, "two_swords": 1.0,
                "high_brave": 1.0, "accuracy_evasive": 1.0, "none": 1.0}

def pressure(routine, wp, st):
    pa,ma,spd,br = st["pa"],st["ma"],st["spd"],st["brave"]
    return {"pa_wp":pa*wp,"br_pa_wp":(pa*br//100)*wp,"spd_pa_wp":((pa+spd)//2)*wp,
            "ma_wp":ma*wp,"rdm_pa_wp":((pa+1)/2)*wp,"wp_wp":wp*wp,
            "br_pa_pa":(pa*br//100)*pa,"pampa_wp":((pa+ma)//2)*wp}[routine]
def resp(ac,dt,pen):
    base=B["armor_response"][ac][dt]
    return base+pen*(CEIL-base) if base<CEIL else base

mism=[]; checked=0
for row in G["rows"]:
    fam=row.get("family")
    if fam not in B["families"]: continue
    f=B["families"][fam]; st=row["attacker_effective_stats"]; ac=row["target_armor_class"]
    sm=SUPPORT_MULT.get(row.get("support_context","none"),1.0)
    p=pressure(f["routine"], f["wp"], st)*sm
    m=min(HI,max(LO,resp(ac,f["damage_type"],f["penetration"])))
    per_hit=max(FLOOR, math.floor(p*m))
    mine=per_hit*row.get("hit_count",1)
    theirs=row["damage_on_hit"]
    checked+=1
    if mine!=theirs:
        mism.append((row["scenario_id"],fam,row.get("support_context"),ac,mine,theirs))
print(f"checked {checked} family rows (base+stress); mismatches: {len(mism)}")
for s in mism[:30]: print("  ",s)
if not mism: print("ALL v0.1 ROWS AGREE -> gate holds on the stress matrix too.")
