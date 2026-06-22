#!/usr/bin/env python3
"""Reviewer-independent recompute for doc-56 (Samurai/Ninja concrete v0).

Built ONLY from work/sim-inputs-v0.2.1.json (the pinned shared bundle) and the
doc-56 markdown row claims. Does NOT read GPT's tool source. Honors the bundle's
pinned calc.effwp_rounding. First live test of effwp_rounding="none" on the
fractional-mid cells (katana 13.5, ninja_blade 9.75).
"""
import json, math, sys

B = json.load(open("work/sim-inputs-v0.2.1.json"))
CALC = B["calc"]
SCAL = CALC["phase_wp_scalar"]
PEN_CEIL = CALC["penetration_ceiling"]
CHIP = CALC["chip_floor"]
CLAMP = CALC["combined_multiplier_clamp"]
ROUND = CALC["effwp_rounding"]
ARM = B["armor_response"]
FAM = B["families"]
JOBS = B["jobs"]
ARMORS = ["plate", "mail", "leather", "cloth"]

def effwp(wp, phase):
    v = wp * SCAL[phase]
    if ROUND == "none":
        return v
    if ROUND == "floor":
        return math.floor(v)
    raise SystemExit("unknown effwp_rounding " + ROUND)

def penetrate(base, pen):
    if base < PEN_CEIL:
        return base + pen * (PEN_CEIL - base)
    return base

def tresp(dtype, armor, pen):
    return penetrate(ARM[armor][dtype], pen)

def clamp(m):
    lo, hi = CLAMP
    return max(lo, min(hi, m))

def dmg(pressure, dtype, pen, armor):
    m = clamp(tresp(dtype, armor, pen))
    return max(CHIP, math.floor(round(pressure * m, 9)))

def jstat(job, phase, k):
    return JOBS[job]["bands"][phase][k]

# ---- routines ----
def br_pa_wp(pa, brave, ewp):
    return math.floor(pa * brave / 100) * ewp

def spd_pa_wp(pa, spd, ewp):
    return math.floor((pa + spd) / 2) * ewp

def rdm_pa_wp(pa, ewp):  # expected
    return ((pa + 1) / 2) * ewp

BRAVE = CALC["default_brave"]
fails = []
def check(label, got, exp):
    ok = (got == exp)
    if not ok:
        fails.append((label, got, exp))
    print(f"  {'OK ' if ok else 'XX '}{label:42s} got={got:4d} exp={exp}")

def row(label, pressure, dtype, pen, exp):
    got = [dmg(pressure, dtype, pen, a) for a in ARMORS]
    ok = got == exp
    if not ok:
        fails.append((label, got, exp))
    print(f"  {'OK ' if ok else 'XX '}{label:34s} {got}  exp={exp}")

print(f"== bundle effwp_rounding = {ROUND!r} ==\n")

# ===================== SAMURAI =====================
print("SAMURAI katana (br_pa_wp, wp18 swing pen0, Br70)")
kwp = FAM["katana"]["wp"]
for phase, exp in [("mid",[61,70,89,94]),("late",[105,121,153,162]),("stress",[117,135,171,180])]:
    pa = jstat("Samurai", phase, "pa")
    p = br_pa_wp(pa, BRAVE, effwp(kwp, phase))
    row(f"katana {phase} (eff={effwp(kwp,phase)})", p, "swing", 0.0, exp)

# Doublehand x1.80 late
pa = jstat("Samurai","late","pa")
p = br_pa_wp(pa, BRAVE, effwp(kwp,"late")) * 1.80
row("Doublehand x1.80 late", p, "swing", 0.0, [189,218,277,291])

print("\nSAMURAI Iaido late (katana-spirit * mult, swing pen0)")
iaido_late = [
    ("Ashura x0.60",0.60,[63,72,92,97]),
    ("Kotetsu x0.75",0.75,[78,91,115,121]),
    ("Bizen x0.90",0.90,[94,109,138,145]),
    ("Ame x0.85",0.85,[89,103,130,137]),
    ("Muramasa x0.50",0.50,[52,60,76,81]),
    ("Kiku x0.90",0.90,[94,109,138,145]),
    ("Chirijiraden x1.10",1.10,[115,133,169,178]),
]
pa = jstat("Samurai","late","pa")
base_late = br_pa_wp(pa, BRAVE, effwp(kwp,"late"))
for name, mult, exp in iaido_late:
    row(name, base_late*mult, "swing", 0.0, exp)

print("\nSAMURAI Iaido mid fractional-WP proof (eff=13.5)")
pa = jstat("Samurai","mid","pa")
base_mid = br_pa_wp(pa, BRAVE, effwp(kwp,"mid"))
for name, mult, exp in [
    ("katana mid",1.0,[61,70,89,94]),
    ("Ashura x0.60 mid",0.60,[36,42,53,56]),
    ("Kotetsu x0.75 mid",0.75,[46,53,67,70]),
    ("Bizen x0.90 mid",0.90,[55,63,80,85]),
    ("Chirijiraden x1.10 mid",1.10,[67,77,98,103]),
]:
    row(name, base_mid*mult, "swing", 0.0, exp)

# ===================== NINJA =====================
print("\nNINJA melee (spd_pa_wp; dual = 2x floored single)")
def ninja_single(fam, phase):
    pa = jstat("Ninja", phase, "pa"); spd = jstat("Ninja", phase, "spd")
    f = FAM[fam]
    return spd_pa_wp(pa, spd, effwp(f["wp"], phase)), f["damage_type"], f["penetration"]

melee = [
    ("ninja_blade","mid",[57,65,83,87],[114,130,166,174]),
    ("knife","mid",[56,89,78,81],[112,178,156,162]),
    ("ninja_blade","late",[92,107,135,143],[184,214,270,286]),
    ("knife","late",[91,145,127,133],[182,290,254,266]),
]
for fam, phase, exp_s, exp_d in melee:
    p, dt, pen = ninja_single(fam, phase)
    single = [dmg(p, dt, pen, a) for a in ARMORS]
    dual = [2*x for x in single]
    oks = single == exp_s; okd = dual == exp_d
    if not oks: fails.append((f"{fam} {phase} single", single, exp_s))
    if not okd: fails.append((f"{fam} {phase} dual", dual, exp_d))
    print(f"  {'OK ' if oks else 'XX '}{fam} {phase} single {single} exp={exp_s}")
    print(f"  {'OK ' if okd else 'XX '}{fam} {phase} dual   {dual} exp={exp_d}")

print("\nNINJA rejected-flail warning rows (rdm_pa_wp expected, wp24 crush pen0)")
fwp = FAM["flail"]["wp"]
for phase, exp_s, exp_d in [("mid",[113,94,99,99],[226,188,198,198]),("late",[179,148,156,156],[358,296,312,312])]:
    pa = jstat("Ninja", phase, "pa")
    p = rdm_pa_wp(pa, effwp(fwp, phase))
    single = [dmg(p, "crush", 0.0, a) for a in ARMORS]
    dual = [2*x for x in single]
    oks = single==exp_s; okd = dual==exp_d
    if not oks: fails.append((f"flail {phase} single", single, exp_s))
    if not okd: fails.append((f"flail {phase} dual", dual, exp_d))
    print(f"  {'OK ' if oks else 'XX '}flail {phase} single {single} exp={exp_s}")
    print(f"  {'OK ' if okd else 'XX '}flail {phase} dual   {dual} exp={exp_d}")

# ===================== THROW =====================
print("\nNINJA Throw (floor((PA+SPD)/2)*(tv*scalar), missile pen0.20, no brave/armor-route)")
def throw_pressure(phase, tv):
    pa = jstat("Ninja", phase, "pa"); spd = jstat("Ninja", phase, "spd")
    return math.floor((pa + spd) / 2) * (tv * SCAL[phase])

throws = [  # name, tv, mid_exp, late_exp
    ("Shuriken",7,[40,51,46,48],[66,84,75,78]),
    ("Daggers",8,[46,59,52,55],[75,96,86,89]),
    ("Swords",9,[52,66,59,61],[85,108,97,100]),
    ("Flails",10,[58,74,66,68],[94,121,107,112]),
    ("Katana",10,[58,74,66,68],[94,121,107,112]),
    ("Ninja Blades",12,[69,89,79,82],[113,145,129,134]),
    ("Axes",11,[63,81,72,75],[104,133,118,123]),
    ("Polearms",10,[58,74,66,68],[94,121,107,112]),
    ("Poles",9,[52,66,59,61],[85,108,97,100]),
    ("Knights Swords",13,[75,96,85,89],[122,157,140,145]),
    ("Books",9,[52,66,59,61],[85,108,97,100]),
    ("Bombs",10,[58,74,66,68],[94,121,107,112]),
]
for name, tv, mid_exp, late_exp in throws:
    for phase, exp in [("mid",mid_exp),("late",late_exp)]:
        p = throw_pressure(phase, tv)
        row(f"Throw {name} {phase}", p, "missile", 0.20, exp)

print("\n" + ("="*50))
if fails:
    print(f"FAIL: {len(fails)} mismatched rows")
    for lbl, got, exp in fails:
        print(f"  {lbl}: got {got} exp {exp}")
    sys.exit(1)
print("ALL ROWS MATCH")
