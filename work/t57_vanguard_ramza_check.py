#!/usr/bin/env python3
"""Reviewer-independent recompute for doc-57 (Vanguard/Ramza concrete v0).

Built from work/sim-inputs-v0.2.1.json (formula pipeline) + the doc-57 DISCLOSED
provisional stat rows (Vanguard/Ramza are NOT yet bundle job entries). Does NOT
read GPT's tool source. Honors pinned calc.effwp_rounding.

INDEPENDENCE NOTE: for this pair the per-job STAT inputs are doc-provisional
(shared disclosed block), not bundle-rooted like the prior 5 pairs. The pipeline
recompute is fully independent; the stat block is a shared input. Flagged in verdict.
"""
import json, math, sys

B = json.load(open("work/sim-inputs-v0.2.1.json"))
CALC = B["calc"]; SCAL = CALC["phase_wp_scalar"]
PEN_CEIL = CALC["penetration_ceiling"]; CHIP = CALC["chip_floor"]; CLAMP = CALC["combined_multiplier_clamp"]
ROUND = CALC["effwp_rounding"]; ARM = B["armor_response"]; FAM = B["families"]
MAGIC = B["magic"]; FFLOOR = MAGIC["faith_factor_floor"]
ARMORS = ["plate","mail","leather","cloth"]
BRAVE = CALC["default_brave"]; FAITH = CALC["default_faith"]

def effwp(wp, phase):
    v = wp*SCAL[phase]
    return v if ROUND=="none" else (math.floor(v) if ROUND=="floor" else round(v))
def pen(base,p): return base + p*(PEN_CEIL-base) if base<PEN_CEIL else base
def tresp(dt,a,p): return pen(ARM[a][dt],p)
def clamp(m):
    lo,hi=CLAMP; return max(lo,min(hi,m))
def dmg(pressure,dt,p,a): return max(CHIP, math.floor(round(pressure*clamp(tresp(dt,a,p)),9)))

fails=[]
def row(label,pressure,dt,p,exp):
    got=[dmg(pressure,dt,p,a) for a in ARMORS]
    ok=got==exp
    if not ok: fails.append((label,got,exp))
    print(f"  {'OK ' if ok else 'XX '}{label:36s} {got} exp={exp}")
def scalar_check(label,got,exp):
    ok=got==exp
    if not ok: fails.append((label,got,exp))
    print(f"  {'OK ' if ok else 'XX '}{label:36s} got={got} exp={exp}")

# routines
def pa_wp(pa,ewp): return pa*ewp
def rdm_pa_wp(pa,ewp): return ((pa+1)/2)*ewp
def br_pa_pa(pa,brave): return math.floor(pa*brave/100)*pa
def hybrid(pa,ma,ewp): return math.floor((pa+ma)/2)*ewp

print(f"== effwp_rounding={ROUND!r} ==\n")

# ===== VANGUARD (provisional: plate, late PA12 MA6 SPD7) =====
PA=12; ph="late"
sword=FAM["sword"]; spear=FAM["spear"]; axe=FAM["axe"]; fists=FAM["fists"]
e_sw=effwp(sword["wp"],ph); e_sp=effwp(spear["wp"],ph); e_ax=effwp(axe["wp"],ph)
p_sword=pa_wp(PA,e_sw)            # 192
p_spear=pa_wp(PA,e_sp)            # 180
p_axe=rdm_pa_wp(PA,e_ax)         # 130 expected
p_fists=br_pa_pa(PA,BRAVE)        # 96 (fists wp0 -> br_pa_pa uses PA not wp)

print("VANGUARD baseline late")
row("sword",      p_sword,"swing",sword["penetration"],[124,144,182,192])
row("spear",      p_spear,"thrust",spear["penetration"],[125,198,173,181])
row("axe expected",p_axe, "crush",axe["penetration"],[149,123,130,130])
row("fists",      p_fists,"crush",fists["penetration"],[110,93,97,97])

print("\nVANGUARD action rows")
row("Breach axe x0.75",   p_axe*0.75,"crush",axe["penetration"],[112,92,97,97])
row("Breach fists x0.75", p_fists*0.75,"crush",fists["penetration"],[82,70,73,73])
row("Sunder sword x0.45", p_sword*0.45,"swing",sword["penetration"],[56,64,82,86])
row("Sunder axe x0.45",   p_axe*0.45,"crush",axe["penetration"],[67,55,58,58])
row("Decisive sword x1.20",p_sword*1.20,"swing",sword["penetration"],[149,172,218,230])
row("Decisive spear x1.20",p_spear*1.20,"thrust",spear["penetration"],[150,237,208,218])
row("Decisive axe x1.20",  p_axe*1.20,"crush",axe["penetration"],[179,148,156,156])
row("Decisive sword x0.75",p_sword*0.75,"swing",sword["penetration"],[93,108,136,144])

print("\nVANGUARD exposure rows (final_resp=min(cap, base+delta), dmg on base 100)")
EXP={"plate":0.06,"mail":0.05,"leather":0.03,"cloth":0.0}; CAP=1.15
for a,dt,exp in [("plate","swing",71),("plate","crush",115),("mail","missile",115),("leather","thrust",98),("cloth","swing",100)]:
    fr=min(CAP, ARM[a][dt]+EXP[a]); got=math.floor(round(100*fr,9))
    scalar_check(f"exposure {a} {dt}",got,exp)

print("\nVANGUARD mitigation rows (incoming 120; strongest single channel)")
inc=120
scalar_check("Intercede ally x0.75", math.floor(inc*0.75), 90)
scalar_check("Intercede vanguard chip 0.25", math.floor(inc*0.25), 30)
scalar_check("Aegis ally x0.85", math.floor(inc*0.85), 102)
scalar_check("both -> min(90,102)", min(math.floor(inc*0.75),math.floor(inc*0.85)), 90)

# ===== RAMZA (provisional chapters; band->phase scalar) =====
# C1 band0/A->early, C2 B/C->mid, C3 C/D->mid, C4 E->late, stress->stress
print("\nRAMZA weapon/hybrid rows")
RZ={"c1":(4,4,"early"),"c2":(8,7,"mid"),"c3":(9,8,"mid"),"c4":(12,12,"late"),"c4s":(13,13,"stress")}
def rsword(pa,ph): return pa_wp(pa,effwp(sword["wp"],ph))
row("C1 sword (early)",   rsword(4,"early"),"swing",0.0,[20,24,30,32])
row("Squire early ref",   rsword(4,"early"),"swing",0.0,[20,24,30,32])
row("C2 sword (mid)",     rsword(8,"mid"),"swing",0.0,[62,72,91,96])
row("C3 sword mid",       rsword(9,"mid"),"swing",0.0,[70,81,102,108])
row("C3 Spellblade x0.85",hybrid(9,8,effwp(sword["wp"],"mid"))*0.85,"swing",0.0,[53,61,77,81])
row("C4 sword (late)",    rsword(12,"late"),"swing",0.0,[124,144,182,192])
row("C4 Arc Blade x1.00", hybrid(12,12,effwp(sword["wp"],"late"))*1.0,"swing",0.0,[124,144,182,192])

print("\nRAMZA magic rows (Faith 70/70 -> faith_floor 0.60)")
def umag(K,ma):
    fac=max(FFLOOR,(FAITH/100)*(FAITH/100))
    return math.floor(round(K*ma*fac,9))
scalar_check("Ultima K22 late (MA12)",  umag(22,12),158)
scalar_check("Ultima K22 stress (MA13)",umag(22,13),171)
scalar_check("BlackMage K26 late (MA15)",umag(26,15),234)
scalar_check("BlackMage K26 stress(MA18)",umag(26,18),280)

print("\n"+"="*50)
if fails:
    print(f"FAIL: {len(fails)} mismatches")
    for l,g,e in fails: print(f"  {l}: got {g} exp {e}")
    sys.exit(1)
print("ALL ROWS MATCH")
