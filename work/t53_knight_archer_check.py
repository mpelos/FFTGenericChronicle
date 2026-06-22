#!/usr/bin/env python3
"""Independent reviewer recompute for doc 53 (Knight/Archer concrete) — CANONICAL rev.
Built from work/sim-inputs-v0.2.1.json schema ONLY, not GPT's tool.
Verifies every simulation_rows cell in work/gpt-knight-archer-concrete-v0.json (ad42a807...).
"""
import json, math

B = json.load(open("/home/mpelos/Projects/FFTGenericChronicle/work/sim-inputs-v0.2.1.json"))
P = json.load(open("/home/mpelos/Projects/FFTGenericChronicle/work/gpt-knight-archer-concrete-v0.json"))

fam, ar, calc, jobs = B["families"], B["armor_response"], B["calc"], B["jobs"]
BR = calc["default_brave"]; CEIL = calc["penetration_ceiling"]
CLAMP = calc["combined_multiplier_clamp"]; CHIP = calc["chip_floor"]; WPS = calc["phase_wp_scalar"]

def effwp(family, phase): return fam[family]["wp"] * WPS[phase]
def routine_pressure(job, family, phase):
    j = jobs[job]["bands"][phase]; PA, SPD = j["pa"], j["spd"]; w = effwp(family, phase)
    r = fam[family]["routine"]
    if r == "pa_wp":     return PA * w
    if r == "spd_pa_wp": return math.floor((PA+SPD)/2) * w
    if r == "br_pa_wp":  return math.floor(PA*BR/100) * w
    if r == "br_pa_pa":  return math.floor(PA*BR/100) * PA
    if r == "wp_wp":     return w * w
    raise ValueError(r)
def type_response(family, armor, extra_pen=0.0):
    dt = fam[family]["damage_type"]; base = ar[armor][dt]; pen = fam[family]["penetration"] + extra_pen
    return base + pen*(CEIL-base) if base < CEIL else base
def damage(job, family, armor, mult=1.0, phase="mid", extra_pen=0.0):
    pressure = routine_pressure(job, family, phase) * mult
    combined = max(CLAMP[0], min(CLAMP[1], type_response(family, armor, extra_pen)))
    return max(CHIP, math.floor(round(pressure * combined, 9)))

ARMORS = ["plate","mail","leather","cloth"]; fails = []; checked = 0
def check_row(label, job, family, d, mult=1.0, phase="mid", extra_pen=0.0):
    global checked
    for a in ARMORS:
        got = damage(job, family, a, mult, phase, extra_pen); checked += 1
        if got != d[a]: fails.append(f"{label} [{a}]: got {got} exp {d[a]}")

S = P["simulation_rows"]
e = S["early_unlock_warning_damage"]
check_row("early knight sword","Knight","sword",e["knight_sword_family_sword"],phase="early")
check_row("early archer longbow","Archer","longbow",e["archer_longbow"],phase="early")
check_row("early archer crossbow","Archer","crossbow",e["archer_crossbow"],phase="early")
m = S["mid_basic_attack_damage"]
check_row("mid knight sword","Knight","sword",m["knight_sword_family_sword"])
check_row("mid knight fists","Knight","fists",m["knight_fists"])
check_row("mid archer longbow","Archer","longbow",m["archer_longbow"])
check_row("mid archer crossbow","Archer","crossbow",m["archer_crossbow"])
check_row("mid monk fists ref","Monk","fists",m["monk_fists_reference"])
k = S["knight_mid_action_damage"]
check_row("Guarded Strike x0.85","Knight","sword",k["Guarded Strike sword x0.85"],mult=0.85)
check_row("Rend Weapon x0.50","Knight","sword",k["Rend Weapon sword x0.50"],mult=0.50)
check_row("Rend Armor x0.50","Knight","sword",k["Rend Armor sword x0.50"],mult=0.50)
check_row("Shield/stat Rend x0.35","Knight","sword",k["Shield/stat Rend sword x0.35"],mult=0.35)
check_row("Crushing Blow fists x0.85","Knight","fists",k["Crushing Blow fists x0.85"],mult=0.85)
a = S["archer_mid_action_damage"]
check_row("Quick Shot longbow x0.75","Archer","longbow",a["Quick Shot longbow x0.75"],mult=0.75)
check_row("Quick Shot crossbow x0.75","Archer","crossbow",a["Quick Shot crossbow x0.75"],mult=0.75)
check_row("Aimed Shot longbow x1.35","Archer","longbow",a["Aimed Shot longbow x1.35"],mult=1.35)
check_row("Aimed Shot crossbow x1.35","Archer","crossbow",a["Aimed Shot crossbow x1.35"],mult=1.35)
check_row("Pinning Shot longbow x0.70","Archer","longbow",a["Pinning Shot longbow x0.70"],mult=0.70)
check_row("Pinning Shot crossbow x0.70","Archer","crossbow",a["Pinning Shot crossbow x0.70"],mult=0.70)
check_row("Piercing Shot longbow","Archer","longbow",a["Piercing Shot longbow x1.10 pen+0.20"],mult=1.10,extra_pen=0.20)
check_row("Piercing Shot crossbow","Archer","crossbow",a["Piercing Shot crossbow x1.10 pen+0.20"],mult=1.10,extra_pen=0.20)
check_row("High-Ground Shot longbow x1.15","Archer","longbow",a["High-Ground Shot longbow x1.15"],mult=1.15)
check_row("High-Ground Shot crossbow x1.15","Archer","crossbow",a["High-Ground Shot crossbow x1.15"],mult=1.15)

# T6 Rend Armor per-armor delta
rt = S["rend_armor_t6_response_rows"]
dba = rt["response_delta_by_armor"]; cap = rt["response_cap"]; basedmg = rt["base_damage_per_hit"]
for r in rt["rows"]:
    armor, dt = r["armor"], r["type"]; base_resp = ar[armor][dt]; checked += 1
    if abs(base_resp - r["base_response"]) > 1e-9:
        fails.append(f"T6 {armor}/{dt} base: got {base_resp} exp {r['base_response']}")
    after = round(min(cap, base_resp + dba[armor]), 6); checked += 1
    if abs(after - r["after_rend"]) > 1e-9:
        fails.append(f"T6 {armor}/{dt} after: got {after} exp {r['after_rend']}")
    proj = math.floor(round(basedmg * after, 6)); checked += 1
    if proj != r["projected_damage"]:
        fails.append(f"T6 {armor}/{dt} proj: got {proj} exp {r['projected_damage']}")

# T4 Shield Break — facing model (doc-09 authoritative; reviewer reverse-engineered)
sb = S["shield_break_t4_rows"]
bh=sb["base_hit"]; cls=sb["class_evade"]; shd=sb["shield_evade"]; acc=sb["accessory_evade"]
wpn=sb["weapon_evade"]; ma=sb["shield_evasion_multiplier_after_break"]
def hit(c,s,ac,w): return bh*(1-c/100)*(1-s/100)*(1-ac/100)*(1-w/100)
def shield_model(f):
    if f=="front": return hit(cls,shd,acc,wpn), hit(cls,shd*ma,acc,wpn)
    if f=="side":  return hit(cls,shd,0,0),     hit(cls,shd*ma,0,0)
    return hit(cls,0,0,0), hit(cls,0,0,0)
for r in sb["rows"]:
    b,af = shield_model(r["facing"]); b=math.floor(round(b,6)); af=math.floor(round(af,6)); checked += 2
    if b != r["before"]: fails.append(f"T4 {r['facing']} before: model {b} exp {r['before']}")
    if af != r["after"]: fails.append(f"T4 {r['facing']} after: model {af} exp {r['after']}")

# Rend Weapon enemy-offense
rw = S["rend_weapon_enemy_offense_rows"]; wm = rw["weapon_output_multiplier"]
for r in rw["rows"]:
    got = math.floor(round(r["incoming_weapon_damage"]*wm,6)); checked += 1
    if got != r["after_rend_weapon"]: fails.append(f"RendWeapon {r['incoming_weapon_damage']}: got {got} exp {r['after_rend_weapon']}")

# Knight stat-pressure rows
sp = S["knight_stat_pressure_rows"]
for r in sp["rows"]:
    eff, before, after = r["effect"], r["before"], r["after"]; checked += 1
    if "Rend Weapon plus Rend Power" in eff:
        # R1 demonstration: non-compound — stronger single reduction = min multiplier (0.75), NOT product
        got = math.floor(round(before*min(0.75,0.85),6))
    elif "Rend Power" in eff or "Rend Magick" in eff:
        got = math.floor(round(before*0.85,6))
    elif "Rend Speed" in eff:
        got = before - 1
    elif "Rend MP" in eff:
        got = before - min(30, math.floor(before*0.25))
    else:
        got = None
    if got != after: fails.append(f"StatPressure '{eff}': got {got} exp {after}")

# Pinning Shot tempo
ps = S["pinning_shot_tempo_rows"]; ctd = ps["ct_damage"]
for r in ps["rows"]:
    got = max(0, r["target_ct_before"]-ctd); checked += 1
    if got != r["target_ct_after"]: fails.append(f"Pinning {r['target_ct_before']}: got {got} exp {r['target_ct_after']}")

print(f"checked cells: {checked}")
print(f"mismatches: {len(fails)}")
for x in fails: print("  MISMATCH:", x)
print("RESULT:", "PASS 0-mismatch" if not fails else "FAIL")
