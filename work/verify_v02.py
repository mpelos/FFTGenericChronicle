#!/usr/bin/env python3
"""Independent verification of v0.2: diff every family row AND recompute the 5 metrics."""
import json, os, math
HERE=os.path.dirname(os.path.abspath(__file__))
B=json.load(open(os.path.join(HERE,"sim-inputs-v0.2.json")))
G=json.load(open(os.path.join(HERE,"gpt-sim-v0.2-results.json")))
C=B["calc"]; CEIL=C["penetration_ceiling"]; LO,HI=C["combined_multiplier_clamp"]; FLOOR=C["chip_floor"]
PWS=C["phase_wp_scalar"]; SE=B["stress_engines"]
SUP={"two_hands":SE["two_hands"],"attack_boost":SE["attack_boost"],
     "two_swords":1.0,"high_brave":1.0,"accuracy_evasive":1.0,"none":1.0}

def wp_eff(wp,band): return wp*PWS[band]   # float; floor only at the end
def pressure(routine,wp,band,st):
    w=wp_eff(wp,band); pa,ma,spd,br=st["pa"],st["ma"],st["spd"],st["brave"]
    return {"pa_wp":pa*w,"br_pa_wp":(pa*br//100)*w,"spd_pa_wp":((pa+spd)//2)*w,
            "ma_wp":ma*w,"rdm_pa_wp":((pa+1)/2)*w,"wp_wp":w*w,
            "br_pa_pa":(pa*br//100)*pa,"pampa_wp":((pa+ma)//2)*w}[routine]
def resp(ac,dt,pen):
    base=B["armor_response"][ac][dt]; return base+pen*(CEIL-base) if base<CEIL else base

# ---- 1. row diff ----
mism=[]; checked=0; recomputed=[]
for row in G["rows"]:
    fam=row.get("family")
    if fam not in B["families"]: continue
    f=B["families"][fam]; st=row["attacker_effective_stats"]; ac=row["target_armor_class"]
    band=row["attacker_level_or_band"]; sup=row.get("support_context","none")
    p=pressure(f["routine"],f["wp"],band,st)*SUP.get(sup,1.0)
    m=min(HI,max(LO,resp(ac,f["damage_type"],f["penetration"])))
    per_hit=max(FLOOR,math.floor(p*m)); mine=per_hit*row.get("hit_count",1)
    checked+=1
    if mine!=row["damage_on_hit"]:
        mism.append((row["scenario_id"],fam,sup,ac,band,mine,row["damage_on_hit"]))
    recomputed.append({"fam":fam,"dt":f["damage_type"],"ac":ac,"band":band,"sup":sup,
                       "dmg":mine,"hp":row["target_effective_stats"]["hp"]})
print(f"ROW DIFF: checked {checked} family rows; mismatches {len(mism)}")
for s in mism[:20]: print("  ",s)

# ---- 2. metrics (independent) ----
late_stress=[r for r in recomputed if r["band"] in ("late","stress")]
# scale band
worst=max(recomputed,key=lambda r:r["dmg"]/r["hp"] if r["hp"] else 0)
print(f"\nSCALE_BAND: max dmg/HP = {worst['dmg']/worst['hp']:.4f} ({worst['fam']} {worst['band']} {worst['ac']})  -> {'PASS' if worst['dmg']/worst['hp']<=1.25 else 'FAIL'}")
# plate matchup
def avg(rows,dt):
    v=[r['dmg'] for r in recomputed if r['ac']=='plate' and r['dt']==dt]; return sum(v)/len(v) if v else 0
pc,ps,pt=avg(recomputed,'crush'),avg(recomputed,'swing'),avg(recomputed,'thrust')
mt=[r['dmg'] for r in recomputed if r['ac']=='mail' and r['dt']=='thrust']; ms=[r['dmg'] for r in recomputed if r['ac']=='mail' and r['dt']=='swing']
mt,ms=sum(mt)/len(mt),sum(ms)/len(ms)
print(f"PLATE_MATCHUP: plate crush {pc:.1f} > swing {ps:.1f} & thrust {pt:.1f} ; mail thrust {mt:.1f} > swing {ms:.1f} -> {'PASS' if pc>ps and pc>pt and mt>ms else 'FAIL'}")
# dominance: best family per (ac,band,sup) group in late/stress
from collections import defaultdict
groups=defaultdict(list)
for r in late_stress: groups[(r['ac'],r['band'],r['sup'])].append(r)
best=defaultdict(int)
for g,rs in groups.items(): best[max(rs,key=lambda r:r['dmg'])['fam']]+=1
total=len(groups); topfam,topn=max(best.items(),key=lambda kv:kv[1])
print(f"NO_DOMINANCE: {total} late/stress groups; top={topfam} {topn} ({topn/total:.2%}) -> {'PASS' if topn/total<=0.50 else 'FAIL'}")
print("   counts:",dict(sorted(best.items(),key=lambda kv:-kv[1])))
# magic coexistence
phys_max=max(r['dmg'] for r in late_stress)
ma_bm=round(12*1.50); mag=round(26*ma_bm*max(B['magic']['faith_factor_floor'],0.49))
print(f"MAGIC_COEXIST: magic {mag} / phys_max {phys_max} = {mag/phys_max:.4f} -> {'PASS' if mag/phys_max>=0.65 else 'FAIL'}")
# viability quick (combat best, role-exempt)
EX={"instrument","knife","ninja_blade"}
fam_best=defaultdict(int)
for r in recomputed: fam_best[r['fam']]=max(fam_best[r['fam']],r['dmg'])
cbest=max(v for k,v in fam_best.items() if k not in EX)
weak=[k for k,v in fam_best.items() if k not in EX and v/cbest<0.65]
print(f"VIABILITY(@.65, exempt {EX}): weak={weak if weak else 'NONE -> PASS'}")
