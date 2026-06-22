#!/usr/bin/env python3
"""Reviewer-independent recompute for doc-58 (physical/foundation R/S/M concrete v0).

Built from work/sim-inputs-v0.2.1.json (pipeline + rsm_constants/stress_engines)
+ doc-58 disclosed rows. Does NOT read GPT's tool source. Honors effwp_rounding.
Verifies the ARITHMETIC of every formula-affecting row. Slot-achievability is a
DESIGN check done separately in the verdict (not a math check).
"""
import json, math, sys
B = json.load(open("work/sim-inputs-v0.2.1.json"))
CALC=B["calc"]; SCAL=CALC["phase_wp_scalar"]; PEN_CEIL=CALC["penetration_ceiling"]
CHIP=CALC["chip_floor"]; CLAMP=CALC["combined_multiplier_clamp"]; ROUND=CALC["effwp_rounding"]
ARM=B["armor_response"]; FAM=B["families"]; JOBS=B["jobs"]; SE=B["stress_engines"]
ARMORS=["plate","mail","leather","cloth"]; BRAVE=CALC["default_brave"]
def effwp(wp,ph):
    v=wp*SCAL[ph]; return v if ROUND=="none" else (math.floor(v) if ROUND=="floor" else round(v))
def pen(b,p): return b+p*(PEN_CEIL-b) if b<PEN_CEIL else b
def clamp(m): lo,hi=CLAMP; return max(lo,min(hi,m))
def dmg(pr,dt,p,a): return max(CHIP, math.floor(round(pr*clamp(pen(ARM[a][dt],p)),9)))
def js(job,ph,k): return JOBS[job]["bands"][ph][k]
fails=[]
def chk(label,got,exp):
    ok=got==exp
    if not ok: fails.append((label,got,exp))
    print(f"  {'OK ' if ok else 'XX '}{label:42s} {got} exp={exp}")
def prow(label,pr,dt,p,exp): chk(label,[dmg(pr,dt,p,a) for a in ARMORS],exp)

# routines
def pa_wp(pa,e): return pa*e
def spd_pa_wp(pa,spd,e): return math.floor((pa+spd)/2)*e
def rdm(pa,e): return ((pa+1)/2)*e
def br_pa_pa(pa,br): return math.floor(pa*br/100)*pa
AB=SE["attack_boost"]; DH=SE["two_hands"]
print(f"== effwp_rounding={ROUND!r}; attack_boost={AB}; two_hands={DH} ==\n")

print("SQUIRE Basic Training x1.10 (fixed action value, floored)")
for n,base,exp in [("throw_stone",12,13),("dash",18,19),("first_aid",20,22)]:
    chk(n, math.floor(base*1.10), exp)

print("\nCHEMIST Item Lore (hp x1.30, ether x1.20)")
for n,base,mult,exp in [("potion",30,1.3,39),("hi_potion",70,1.3,91),("x_potion",150,1.3,195),("ether",20,1.2,24),("hi_ether",50,1.2,60)]:
    chk(n, math.floor(base*mult), exp)

print("\nKNIGHT Equip Armor response (raw incoming 120, no pen)")
inc=120
for dt,exp in [("swing",[120,78]),("thrust",[120,78]),("crush",[120,138]),("missile",[120,96])]:
    cloth=math.floor(inc*ARM["cloth"][dt]); plate=math.floor(inc*ARM["plate"][dt])
    chk(f"{dt} [cloth,plate]",[cloth,plate],exp)

print("\nDEFENSIVE STACK strongest-single (incoming 120)")
ch={"armor_discipline":math.floor(120*0.9),"aegis":math.floor(120*0.85),"protect":math.floor(120*CALC['protect_multiplier']),"parry":math.floor(120*0.6)}
print(f"  channels: {ch}")
chk("accepted = strongest single (min)", min(ch.values()), 72)
chk("incorrect product != accepted", math.floor(120*0.9*0.85*CALC['protect_multiplier']*0.6), 36)

print("\nARCHER Bow Mastery late (longbow, pressure x1.10)")
pa=js("Archer","late","pa"); spd=js("Archer","late","spd"); lb=FAM["longbow"]
base=spd_pa_wp(pa,spd,effwp(lb["wp"],"late"))
prow("baseline",base,"missile",lb["penetration"],[114,148,131,137])
prow("bow_mastery",base*1.10,"missile",lb["penetration"],[125,163,144,150])
print("  Concentration expected-dmg = baseline*0.75 (non-floored EV)")
ev=[round(dmg(base,"missile",lb["penetration"],a)*0.75,2) for a in ARMORS]
chk("concentration EV",ev,[85.5,111,98.25,102.75])

print("\nMONK Brawler (fists; x1.25 brawler, x1.375 brawler+martial)")
for ph,exp_b,exp_br,exp_bm in [("mid",[80,68,71,71],[100,85,88,88],[110,93,97,97]),("stress",[172,145,152,152],[215,182,190,190],[237,200,209,209])]:
    pa=js("Monk",ph,"pa"); fp=br_pa_pa(pa,BRAVE)
    prow(f"{ph} baseline",fp,"crush",FAM["fists"]["penetration"],exp_b)
    prow(f"{ph} brawler x1.25",fp*1.25,"crush",FAM["fists"]["penetration"],exp_br)
    prow(f"{ph} brawler+martial x1.375",fp*1.375,"crush",FAM["fists"]["penetration"],exp_bm)

print("\nSAMURAI Doublehand")
pa=js("Samurai","late","pa"); kp=br_pa_wp=math.floor(pa*BRAVE/100)*effwp(FAM["katana"]["wp"],"late")
prow("katana late baseline",kp,"swing",0.0,[105,121,153,162])
prow("katana late doublehand",kp*DH,"swing",0.0,[189,218,277,291])
pa=js("Samurai","stress","pa"); ks=math.floor(pa*BRAVE/100)*effwp(FAM["knight_sword"]["wp"],"stress")
prow("knight_sword stress baseline",ks,"swing",0.0,[130,150,190,200])
prow("knight_sword stress doublehand",ks*DH,"swing",0.0,[234,270,342,360])

print("\nNINJA Dual Wield (single, dual=2x, dual+attack_boost=2x floor(AB*armor))")
def dual_ab(host,ph,fam,exp_s,exp_d,exp_dab):
    pa=js(host,ph,"pa"); spd=js(host,ph,"spd"); f=FAM[fam]
    base=spd_pa_wp(pa,spd,effwp(f["wp"],ph)); dt=f["damage_type"]; p=f["penetration"]
    single=[dmg(base,dt,p,a) for a in ARMORS]; dual=[2*x for x in single]
    abp=base*AB; dab=[2*dmg(abp,dt,p,a) for a in ARMORS]
    chk(f"{fam} {ph} single",single,exp_s); chk(f"{fam} {ph} dual",dual,exp_d); chk(f"{fam} {ph} dual+AB",dab,exp_dab)
dual_ab("Ninja","late","ninja_blade",[92,107,135,143],[184,214,270,286],[246,286,362,380])
dual_ab("Ninja","stress","ninja_blade",[109,126,160,169],[218,252,320,338],[292,338,428,450])
dual_ab("Ninja","stress","knife",[108,171,150,157],[216,342,300,314],[288,456,400,420])

print("\nNINJA Throw Mastery late (throw pressure x1.10, missile pen0.20)")
def throwp(ph,tv): pa=js("Ninja",ph,"pa"); spd=js("Ninja",ph,"spd"); return math.floor((pa+spd)/2)*(tv*SCAL[ph])
for n,tv,base_exp,tm_exp in [("ninja_blades",12,[113,145,129,134],[124,159,142,148]),("knight_swords",13,[122,157,140,145],[135,173,154,160])]:
    prow(f"{n} baseline",throwp("late",tv),"missile",0.20,base_exp)
    prow(f"{n} throw_mastery",throwp("late",tv)*1.10,"missile",0.20,tm_exp)

print("\nVANGUARD Intercede(action 0.75/0.25) vs Intervention(reaction 0.80/0.20)")
chk("intercede ally",math.floor(120*0.75),90); chk("intercede chip",math.floor(120*0.25),30)
chk("intervention ally",math.floor(120*0.80),96); chk("intervention chip",math.floor(120*0.20),24)

print("\nVANGUARD Training x1.10 (doc57 provisional Vanguard PA12)")
pa=12; axe=rdm(pa,effwp(FAM["axe"]["wp"],"late")); sw=pa_wp(pa,effwp(FAM["sword"]["wp"],"late")); sp=pa_wp(pa,effwp(FAM["spear"]["wp"],"late"))
prow("breach_axe baseline",axe*0.75,"crush",0.0,[112,92,97,97])
prow("breach_axe VT",axe*0.75*1.10,"crush",0.0,[123,101,107,107])
prow("decisive_sword VT",sw*1.20*1.10,"swing",0.0,[164,190,240,253])
prow("decisive_spear VT",sp*1.20*1.10,"thrust",FAM["spear"]["penetration"],[165,261,229,239])

print("\nLIFEFONT min(floor(hp*0.08),40)")
for hp,exp in [(150,12),(280,22),(430,34),(520,40)]:
    chk(f"hp_{hp}", min(math.floor(hp*0.08),40), exp)

print("\n"+"="*50)
if fails:
    print(f"FAIL: {len(fails)} mismatches")
    for l,g,e in fails: print(f"  {l}: got {g} exp {e}")
    sys.exit(1)
print("ALL ARITHMETIC ROWS MATCH")
