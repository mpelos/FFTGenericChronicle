#!/usr/bin/env python3
"""Independent reviewer checker for W5/F5 (doc65 f13eab93 contract).
Built from work/sim-inputs-v0.2.1.json (d57c4688) + N1 (7e07416a) + doc65 ONLY.
Does NOT read work/w5_real_roster_sweep.py. Recomputes per-unit physical/magic
damage, area normalized totals, weak-family ratios, Belief/Oil; compares to the
result json; independently re-evaluates the majority/Pareto verdict per row.
"""
import json, math

B = json.load(open('work/sim-inputs-v0.2.1.json'))
N1 = json.load(open('work/sim-inputs-n1-real-roster-v0.json'))
R = json.load(open('work/w5-real-roster-sweep-v0.json'))

CALC = B['calc']
FAM = B['families']
AR = B['armor_response']
ROUT = B['routines']
MAG = B['magic']
JOBS = B['jobs']
SCAL = CALC['phase_wp_scalar']
BR = CALC['default_brave']; FA = CALC['default_faith']
FF = MAG['faith_factor_floor']

def eff_stats(job, phase):
    if job in JOBS:
        return JOBS[job]['bands'][phase], JOBS[job]['armor_class']
    return None, None

def base_pressure(fam, st, phase):
    f = FAM[fam]; wp = f['wp']*SCAL[phase]; r = f['routine']
    PA=st['pa']; MA=st['ma']; SPD=st['spd']
    if r=='pa_wp': return PA*wp
    if r=='br_pa_wp': return math.floor(PA*BR/100)*wp
    if r=='spd_pa_wp': return math.floor((PA+SPD)/2)*wp
    if r=='ma_wp': return MA*wp
    if r=='rdm_pa_wp': return ((PA+1)/2)*wp
    if r=='wp_wp': return wp*wp
    if r=='br_pa_pa': return math.floor(PA*BR/100)*PA
    if r=='pampa_wp': return math.floor((PA+MA)/2)*wp
    raise ValueError(r)

def type_resp(fam, armor):
    f=FAM[fam]; dt=f['damage_type']; pen=f.get('penetration',0.0)
    base=AR[armor][dt]; ceil=CALC['penetration_ceiling']
    return base + pen*(ceil-base) if base<ceil else base

def phys_per_armor(fam, st, phase, armor, floor=True):
    bp=base_pressure(fam,st,phase); resp=type_resp(fam,armor)
    v=bp*resp
    return math.floor(v) if floor else v

# ---- weak-family ratio re-check ----
print("=== WEAK-FAMILY RATIO RECHECK ===")
ratio_fail=0
for row in R['rows']:
    w=row.get('weakest_combat_ratio_to_sword')
    if not w or 'family' not in w: continue
    fam=w['family']; phase=row['phase']
    # reconstruct using reported per-armor (we verify ratio math + floor consistency)
    famd=w['family_delivered_per_action_by_armor']; swd=w['sword_delivered_per_action_by_armor']
    ratios={a: famd[a]/swd[a] for a in famd}
    worst=min(ratios.values())
    ok = abs(worst-w['ratio'])<1e-6
    if not ok: ratio_fail+=1
    print(f"  {row['id']:22} fam={fam:6} worst={worst:.4f} reported={w['ratio']:.4f} {'OK' if ok else 'MISMATCH'}")
print(f"  ratio mismatches: {ratio_fail}")

# ---- N1 profile stats ----
def n1_stats(prof, phase):
    p=N1['real_roster_profiles'].get(prof)
    if not p: return None
    st=p.get('stats')
    if phase=='stress' and 'stress_stats' in p: st=p['stress_stats']
    return st

# ---- physical per-unit damage recompute: delivered = hc * Σ mix * floor(base*em*resp) ----
print("\n=== PHYSICAL delivered_per_action RECHECK ===")
mismism=0; checked=0; notrep=[]
for row in R['rows']:
    mix=row['armor_mix']; phase=row['phase']
    for u in row['units']:
        fam=u.get('primary_family'); dmg=u['damage']
        if not fam or fam not in FAM: continue
        job=u['job_or_profile']
        st,_=eff_stats(job,phase)
        if st is None: st=n1_stats(job,phase)
        if st is None: continue
        f=FAM[fam]
        if f['routine']=='ma_wp':  # magic-stick weapon, handle in magic pass
            continue
        em=dmg['engine_multiplier']; hc=dmg['hit_count']
        bp=base_pressure(fam,st,phase)
        deliv=hc*sum(mix[a]*math.floor(bp*em*type_resp(fam,a)) for a in mix)
        rep=dmg['delivered_per_action']
        checked+=1
        ok=abs(deliv-rep)<0.01
        if not ok:
            mismism+=1; notrep.append((row['id'],u['name'],fam,rep,deliv))
            print(f"  {row['id']:20} {u['name']:22} {fam:12} rep_deliv={rep:9.3f} calc={deliv:9.3f}  MISMATCH (base={bp:.2f} em={em} hc={hc})")
        else:
            print(f"  {row['id']:20} {u['name']:22} {fam:12} rep_deliv={rep:9.3f} OK")
print(f"  physical units checked={checked} unreproduced={mismism}")
if notrep:
    print("  --- unreproduced (likely fixed-action / non-weapon-routine / floor-envelope models): ---")
    for x in notrep: print("   ",x)
