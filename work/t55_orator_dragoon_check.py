#!/usr/bin/env python3
"""Independent reviewer recompute for doc 55 (Orator/Dragoon).
Built from work/sim-inputs-v0.2.1.json ONLY. Tests effWP floor vs unfloored to
expose any cross-doc convention drift (committed docs 52-54 used UNFLOORED effWP).
"""
import json, math
B=json.load(open("/home/mpelos/Projects/FFTGenericChronicle/work/sim-inputs-v0.2.1.json"))
P=json.load(open("/home/mpelos/Projects/FFTGenericChronicle/work/gpt-orator-dragoon-concrete-v0.json"))
fam,ar,calc,jobs,mag=B["families"],B["armor_response"],B["calc"],B["jobs"],B["magic"]
CEIL=calc["penetration_ceiling"]; CLAMP=calc["combined_multiplier_clamp"]; CHIP=calc["chip_floor"]
WPS=calc["phase_wp_scalar"]; FFLOOR=mag["faith_factor_floor"]
ROUND=calc.get("effwp_rounding","none")   # bundle-pinned convention
print(f"[bundle effwp_rounding = {ROUND!r}]")

def effwp(f,ph,floor_wp):
    v=fam[f]["wp"]*WPS[ph]
    if floor_wp or ROUND=="floor": return math.floor(v)
    if ROUND=="round": return round(v)
    return v
def rp(job,f,ph,brave,floor_wp):
    j=jobs[job]["bands"][ph]; PA,SPD=j["pa"],j["spd"]; w=effwp(f,ph,floor_wp); r=fam[f]["routine"]
    if r=="pa_wp": return PA*w
    if r=="spd_pa_wp": return math.floor((PA+SPD)/2)*w
    if r=="br_pa_wp": return math.floor(PA*brave/100)*w
    if r=="br_pa_pa": return math.floor(PA*brave/100)*PA
    if r=="wp_wp": return w*w
    raise ValueError(r)
def tresp(f,armor,xp=0.0):
    dt=fam[f]["damage_type"]; base=ar[armor][dt]; pen=fam[f]["penetration"]+xp
    return base+pen*(CEIL-base) if base<CEIL else base
def dmg(job,f,armor,mult=1.0,ph="mid",brave=70,floor_wp=False):
    pr=rp(job,f,ph,brave,floor_wp)*mult; c=max(CLAMP[0],min(CLAMP[1],tresp(f,armor)))
    return max(CHIP,math.floor(round(pr*c,9)))
AR=["plate","mail","leather","cloth"]
S=P["simulation_rows"]

def show(label,job,f,exp,mult=1.0,ph="mid",brave=70):
    unf=[dmg(job,f,a,mult,ph,brave,False) for a in AR]
    flo=[dmg(job,f,a,mult,ph,brave,True) for a in AR]
    e=[exp[a] for a in AR]
    uok="OK" if unf==e else "DIFF"; fok="OK" if flo==e else "DIFF"
    print(f"{label}: exp={e} unfloored={unf}[{uok}] floored={flo}[{fok}]")

print("=== GUN (wp_wp) ===")
g=S["orator_gun_baseline"]
show("orator_gun_mid","Orator","gun",g["orator_gun_mid"])
show("orator_gun_late","Orator","gun",g["orator_gun_late"],ph="late")
print("=== DRAGOON SPEAR (the suspect) ===")
d=S["dragoon_spear_damage"]
show("spear_mid","Dragoon","spear",d["mid_normal_spear"])
show("jump_mid x1.25","Dragoon","spear",d["mid_jump_x1_25"],mult=1.25)
show("spear_late","Dragoon","spear",d["late_normal_spear"],ph="late")
show("jump_late x1.25","Dragoon","spear",d["late_jump_x1_25"],mult=1.25,ph="late")
print("=== BRAVE rows (cloth) ===")
bm={"brave_62":62,"brave_70":70,"brave_78":78,"brave_80":80}
for row in S["brave_force_multiplier_rows_cloth"]:
    rt=row["route"]
    if rt=="Monk fists mid": job,f,ph="Monk","fists","mid"
    elif rt=="Monk fists late": job,f,ph="Monk","fists","late"
    elif rt=="Samurai katana late": job,f,ph="Samurai","katana","late"
    elif rt=="Knight sword late": job,f,ph="Knight","knight_sword","late"
    for bk,bv in bm.items():
        unf=dmg(job,f,"cloth",1.0,ph,bv,False); flo=dmg(job,f,"cloth",1.0,ph,bv,True)
        exp=row[bk]; tag="OK" if unf==exp else ("FLOOR-OK" if flo==exp else "DIFF")
        if unf!=exp: print(f"  {rt} {bk}({bv}): exp={exp} unf={unf} flo={flo} [{tag}]")
print("  (brave rows: only mismatches-vs-unfloored shown above; none => all OK unfloored)")
print("=== FAITH rows (Black Mage mid, K=20) ===")
MA=jobs["Black Mage"]["bands"]["mid"]["ma"]; K=mag["sample_spells"]["mid"]
for row in S["faith_force_multiplier_rows_black_mage_mid_k20"]:
    cf,tf=row["caster_faith"],row["target_faith"]
    ff=max(FFLOOR,(cf/100)*(tf/100)); val=math.floor(round(K*MA*ff,9))
    exp=row["damage"]; print(f"  CFa{cf}/TFa{tf}: exp={exp} got={val} ff={round(ff,4)} {'OK' if val==exp else 'DIFF'}")
print("=== JUMP CT = ceil(50/Speed) ===")
for row in S["jump_timing_rows"]:
    sp=row["speed"]; got=math.ceil(50/sp); exp=row["jump_ticks"]
    print(f"  Speed {sp}: exp={exp} got={got} {'OK' if got==exp else 'DIFF'}")
