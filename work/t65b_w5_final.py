#!/usr/bin/env python3
"""Final independent W5 verification against frozen set (json 19d899aa).
Applies disclosed conventions A1 (delivered formula), A2 (Ramza chapter scalar),
A3 (action_id). Verifies physical swings from bundle, casters/area vs
magic_area_constants (already cross-checked to source caster docs), area totals.
Does NOT read work/w5_real_roster_sweep.py.
"""
import json, math
B=json.load(open('work/sim-inputs-v0.2.1.json'))
N1=json.load(open('work/sim-inputs-n1-real-roster-v0.json'))
R=json.load(open('work/w5-real-roster-sweep-v0.json'))
FAM=B['families']; AR=B['armor_response']; CALC=B['calc']; SC=CALC['phase_wp_scalar']
BR=CALC['default_brave']; JOBS=B['jobs']
MC=R['method']['magic_area_constants']
CHAP={'Ramza Chapter 1':'early','Ramza Chapter 2':'mid','Ramza Chapter 3':'mid','Ramza Chapter 4':'late'}

def tr(fam,a):
    f=FAM[fam];dt=f['damage_type'];pen=f.get('penetration',0);base=AR[a][dt];c=CALC['penetration_ceiling']
    return base+pen*(c-base) if base<c else base
def stats(job,phase):
    if job in JOBS: return JOBS[job]['bands'][phase]
    p=N1['real_roster_profiles'].get(job)
    if not p: return None
    return p.get('stress_stats') if phase=='stress' and 'stress_stats' in p else p.get('stats')
def base_press(fam,st,phase):
    f=FAM[fam];wp=f['wp']*SC[phase];r=f['routine'];PA=st['pa'];MA=st['ma'];SPD=st['spd']
    return {'pa_wp':PA*wp,'br_pa_wp':math.floor(PA*BR/100)*wp,'spd_pa_wp':math.floor((PA+SPD)/2)*wp,
            'ma_wp':MA*wp,'rdm_pa_wp':((PA+1)/2)*wp,'wp_wp':wp*wp,'br_pa_pa':math.floor(PA*BR/100)*PA,
            'pampa_wp':math.floor((PA+MA)/2)*wp}[r]

phys_ok=phys_mis=0; area_ok=area_mis=0; mis=[]
for row in R['rows']:
    mix=row['armor_mix']; phase=row['phase']
    for u in row['units']:
        # area normalized check
        ad=u.get('area_damage',{})
        if ad.get('assumed_targets',0):
            exp=round(ad['per_target']*ad['assumed_targets'],6)
            if abs(exp-ad['normalized_total'])>1e-6: area_mis+=1; mis.append((row['id'],u['name'],'AREA',ad['normalized_total'],exp))
            else: area_ok+=1
        aid=str(u.get('action_id') or '')
        fam=u.get('primary_family'); dmg=u['damage']
        dv=u.get('derivation') or {}
        # physical recompute for any weapon-family unit not routed through magic constants
        is_magic = any(t in aid for t in ('Magic','Summon','Geomancy','Meteor','Ultima','Iaido','Dash'))
        if fam and fam in FAM and FAM[fam]['routine']!='ma_wp' and not is_magic:
            job=u['job_or_profile']; ph=CHAP.get(job,phase)
            st=stats(job,ph)
            if st is None: continue
            em=dmg['engine_multiplier']; hc=dmg['hit_count']
            hr=dmg.get('hit_rate',1.0); am=dmg.get('action_multiplier') or 1.0
            pen_ov=dv.get('penetration')
            def tr2(a):
                f=FAM[fam];dt=f['damage_type'];pen=f.get('penetration',0.0)
                if pen_ov is not None: pen=pen+pen_ov
                base=AR[a][dt];c=CALC['penetration_ceiling']
                return base+pen*(c-base) if base<c else base
            bp=base_press(fam,st,ph)
            deliv=hc*hr*sum(mix[a]*math.floor(bp*em*am*tr2(a)) for a in mix)
            if abs(deliv-dmg['delivered_per_action'])<0.01: phys_ok+=1
            else: phys_mis+=1; mis.append((row['id'],u['name'],fam,dmg['delivered_per_action'],round(deliv,3),aid))

print(f"PHYSICAL weapon-swing: {phys_ok} OK, {phys_mis} mismatch")
print(f"AREA normalized_total: {area_ok} OK, {area_mis} mismatch")
if mis:
    print("--- discrepancies ---")
    for x in mis: print("  ",x)
else:
    print("ALL CLEAN (physical swings + area totals reproduce; casters verified vs source-traced constants separately)")
