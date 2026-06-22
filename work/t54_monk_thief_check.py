#!/usr/bin/env python3
"""Independent reviewer recompute for doc 54 (Monk/Thief concrete).
Built from work/sim-inputs-v0.2.1.json schema ONLY, not GPT's tool.
"""
import json, math

B = json.load(open("/home/mpelos/Projects/FFTGenericChronicle/work/sim-inputs-v0.2.1.json"))
P = json.load(open("/home/mpelos/Projects/FFTGenericChronicle/work/gpt-monk-thief-concrete-v0.json"))
fam, ar, calc, jobs = B["families"], B["armor_response"], B["calc"], B["jobs"]
BR=calc["default_brave"]; CEIL=calc["penetration_ceiling"]; CLAMP=calc["combined_multiplier_clamp"]
CHIP=calc["chip_floor"]; WPS=calc["phase_wp_scalar"]

def effwp(f,ph): return fam[f]["wp"]*WPS[ph]
def routine_pressure(job,f,ph):
    j=jobs[job]["bands"][ph]; PA,SPD=j["pa"],j["spd"]; w=effwp(f,ph); r=fam[f]["routine"]
    if r=="pa_wp": return PA*w
    if r=="spd_pa_wp": return math.floor((PA+SPD)/2)*w
    if r=="br_pa_wp": return math.floor(PA*BR/100)*w
    if r=="br_pa_pa": return math.floor(PA*BR/100)*PA
    if r=="wp_wp": return w*w
    raise ValueError(r)
def tresp(f,armor,xp=0.0):
    dt=fam[f]["damage_type"]; base=ar[armor][dt]; pen=fam[f]["penetration"]+xp
    return base+pen*(CEIL-base) if base<CEIL else base
def damage(job,f,armor,mult=1.0,ph="mid",xp=0.0):
    pr=routine_pressure(job,f,ph)*mult; c=max(CLAMP[0],min(CLAMP[1],tresp(f,armor,xp)))
    return max(CHIP,math.floor(round(pr*c,9)))

AR=["plate","mail","leather","cloth"]; fails=[]; checked=0
def crow(label,job,f,d,mult=1.0,ph="mid",xp=0.0):
    global checked
    for a in AR:
        g=damage(job,f,a,mult,ph,xp); checked+=1
        if g!=d[a]: fails.append(f"{label}[{a}]: got {g} exp {d[a]}")

S=P["simulation_rows"]
mb=S["mid_basic_attack_damage"]
crow("monk fists","Monk","fists",mb["monk_fists"])
crow("thief knife","Thief","knife",mb["thief_knife"])
crow("thief fists fallback","Thief","fists",mb["thief_fists_fallback"])
ma=S["monk_mid_action_damage"]
crow("normal fists","Monk","fists",ma["normal fists"])
crow("Pummel x0.95","Monk","fists",ma["Pummel fists x0.95"],mult=0.95)
crow("Cyclone x0.75","Monk","fists",ma["Cyclone fists x0.75"],mult=0.75)
crow("Aurablast x0.80","Monk","fists",ma["Aurablast fists x0.80"],mult=0.80)
crow("Shockwave x0.90","Monk","fists",ma["Shockwave fists x0.90"],mult=0.90)
crow("Doom Fist x0.50","Monk","fists",ma["Doom Fist fists x0.50"],mult=0.50)
ta=S["thief_mid_action_damage"]
crow("normal knife","Thief","knife",ta["normal knife"])
crow("Steal Gil x0.70","Thief","knife",ta["Steal Gil knife x0.70"],mult=0.70)
crow("equipment steal x0.35","Thief","knife",ta["equipment steal knife x0.35"],mult=0.35)
crow("Steal Heart x0.25","Thief","knife",ta["Steal Heart knife x0.25"],mult=0.25)

# Steal Armor T6 per-armor
rt=S["steal_armor_t6_response_rows"]; dba=rt["response_delta_by_armor"]; cap=rt["response_cap"]; bd=rt["base_damage_per_hit"]
for r in rt["rows"]:
    armor,dt=r["armor"],r["type"]; base=ar[armor][dt]; checked+=1
    if abs(base-r["base_response"])>1e-9: fails.append(f"T6 {armor}/{dt} base: {base} exp {r['base_response']}")
    after=round(min(cap,base+dba[armor]),6); checked+=1
    if abs(after-r["after_steal"])>1e-9: fails.append(f"T6 {armor}/{dt} after: {after} exp {r['after_steal']}")
    proj=math.floor(round(bd*after,6)); checked+=1
    if proj!=r["projected_damage"]: fails.append(f"T6 {armor}/{dt} proj: {proj} exp {r['projected_damage']}")

# Steal Shield T4 facing — round() (hit rates round), shield mult fully removed (0.0)
sb=S["steal_shield_t4_rows"]; bh=sb["base_hit"]; cls=sb["class_evade"]; shd=sb["shield_evade"]
acc=sb["accessory_evade"]; mafter=sb["shield_evasion_multiplier_after_steal"]
def hit(c,s,a2,w=0): return bh*(1-c/100)*(1-s/100)*(1-a2/100)*(1-w/100)
def smodel(fac):
    if fac=="front": return hit(cls,shd,acc), hit(cls,shd*mafter,acc)
    if fac=="side":  return hit(cls,shd,0),   hit(cls,shd*mafter,0)
    return hit(cls,0,0), hit(cls,0,0)
for r in sb["rows"]:
    b,af=smodel(r["facing"]); b=round(b); af=round(af); checked+=2
    if b!=r["before"]: fails.append(f"T4 {r['facing']} before: {b} exp {r['before']}")
    if af!=r["after"]: fails.append(f"T4 {r['facing']} after: {af} exp {r['after']}")

# Steal Weapon output
sw=S["steal_weapon_output_rows"]; wm=sw["weapon_output_multiplier"]
for r in sw["rows"]:
    g=math.floor(round(r["incoming_weapon_damage"]*wm,6)); checked+=1
    if g!=r["after_steal_weapon"]: fails.append(f"StealWeapon {r['incoming_weapon_damage']}: {g} exp {r['after_steal_weapon']}")

# Monk sustain (heal-before-damage)
ms=S["monk_sustain_rows"]
heal_map={"no heal":0,"Potion":30,"Chakra":40,"Hi-Potion":70}
for r in ms["rows"]:
    h=heal_map[r["option"]]; fin=max(0,r["hp_before"]+h-r["incoming_damage"]); surv=fin>0; checked+=2
    if fin!=r["final_hp"]: fails.append(f"sustain {r['option']} final: {fin} exp {r['final_hp']}")
    if surv!=r["survives"]: fails.append(f"sustain {r['option']} survives: {surv} exp {r['survives']}")

print(f"checked cells: {checked}")
print(f"mismatches: {len(fails)}")
for x in fails: print("  MISMATCH:",x)
print("RESULT:", "PASS 0-mismatch" if not fails else "FAIL")
