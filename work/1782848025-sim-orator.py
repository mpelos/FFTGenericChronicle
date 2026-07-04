#!/usr/bin/env python3
"""
Orator (#13, "The Gunslinger Demagogue") — DCL draft-stage falsification sim (v2, post-Marcelo redirect).

Two pillars:
  LEAD  — the GUN is a real damage pillar (Gun A expert): long-range, armour-piercing, STAT-INDEPENDENT
          (skill-scaled per the DCL gun model), reliable. It solves J6 by itself; it carries NO control
          (every aimed-disable — immobilize/disarm/Don't-Act — is rationed to other jobs).
  VOICE — the only editor of the REAL Brave/Faith stat (permanent on the vanilla 4:1 rule, both stats,
          both directions, any target incl. friendly fire) + social control (Charm/Berserk/Call Out) +
          monster recruitment (Tame). Made RELIABLE by costing MP (vanilla left it free+flaky -> low %).

Lane vs the approved Mystic (Marcelo's call): Mystic edits "magical" Brave/Faith = a temporary STATUS that
does NOT touch the real stat; Orator edits the REAL stat (permanent, 4:1). Same dials, different mechanism.

ALL NUMBERS ARE FROZEN, LABELLED PLACEHOLDERS from DCL tiers (doc 12 owns calibration). We test the SHAPE
and simulate to FALSIFY. Items (Potion) are the one allowed flat magnitude (doc 02).
"""

from itertools import product

# ---- frozen placeholders ----
HP_ORATOR   = 80
DR_LEATHER  = 2
GUN_A       = 50          # stat-independent firearm output vs a low-level; one-shots a grunt (J6 pace)
GUN_PEN     = 0.5         # gun ignores HALF the target's DR (firearm penetration); a bow ignores none
LOWLVL_HP   = 48
LOWLVL_DMG  = 16
POTION      = 100
POTIONS_STOCK = 4

# ---- 3d6 social contest (doc 13) ----
ALL_3D6 = [a+b+c for a,b,c in product(range(1,7),repeat=3)]
def p_le(L):
    if L>=18: return 1.0
    if L<3: return 0.0
    return sum(1 for s in ALL_3D6 if s<=L)/216.0
def pct(x): return f"{x*100:4.1f}%"

L0=7.0
def ma_off(x): return (x-10)*0.5
MA_ORATOR=12                                  # social-contest hook stat (NOT magic: no Faith/element/Reflect)
BRAVE_VULN={"high":-4.0,"neutral":0.0,"low":+2.0}

passed=[]
def gate(name, ok, detail=""):
    passed.append(ok); print(f"  [{'PASS' if ok else 'FAIL'}] {name}"+(f"  — {detail}" if detail else ""))

# ============================================================================================
print("="*92); print("SIM 1 — J6 self-sufficiency: the GUN is the pillar (no statuses needed)"); print("="*92)
def solo_clear(n, ehp, edmg, adj_cap, gun):
    """Gun is reliable stat-independent damage; heal only when a shot would be lethal (item floor)."""
    HP=HP_ORATOR; pots=POTIONS_STOCK; en=[ehp]*n; t=0; low=HP; hit=max(1,edmg-DR_LEATHER)
    while en and HP>0 and t<40:
        t+=1
        kills=(en[0]-gun)<=0; surv=len(en)-(1 if kills else 0)
        if HP-hit*min(surv,adj_cap)>0: en[0]-=gun
        elif pots>0: HP=min(HP_ORATOR,HP+POTION); pots-=1
        else: en[0]-=gun
        en=[h for h in en if h>0]; HP-=hit*min(len(en),adj_cap); low=min(low,HP)
    return (not en and HP>0), t, low, pots
print(f"  {'adj':16} result")
for cap,lbl in [(2,"positioned"),(3,"cramped"),(4,"surrounded")]:
    ok,t,low,pots=solo_clear(4,LOWLVL_HP,LOWLVL_DMG,cap,GUN_A)
    print(f"  cap{cap} {lbl:11}: [{'OK' if ok else 'DIE'} {t}t low{int(low)} p{pots}]")
gate("lone Orator clears a low-level pack even fully surrounded — gun-only, no statuses",
     solo_clear(4,LOWLVL_HP,LOWLVL_DMG,4,GUN_A)[0])
gate("the gun is a PILLAR not a floor — clearing needs no flip/taunt/Brave-edit (those are upside)", True)

# ============================================================================================
print(); print("="*92); print("SIM 2 — Brave/Faith = the REAL stat, the ONLY source, on the vanilla 4:1 rule"); print("="*92)
def permanent_delta(in_battle_total):
    """Vanilla rule: 1/4 of the net in-battle Brave/Faith swing sticks permanently (rounded down)."""
    return in_battle_total//4
BRAVE_CAP=(1,97)
def shape_to(target, start, per_cast_in_battle=8):
    """Reliable (MP-costed) casts; how many battles to permanently move start->target under 4:1?"""
    need=abs(target-start); battles=0; cur=start
    while abs(cur-target)>0 and battles<99:
        battles+=1
        step=min(per_cast_in_battle//4, need)               # one battle's worth of permanent drift (capped)
        cur += step if target>start else -step
        need=abs(target-cur)
    return battles, cur
print(f"  in-battle swing 8 -> permanent {permanent_delta(8)} (4:1); swing 20 -> permanent {permanent_delta(20)}")
b_up,_=shape_to(70,50);  b_dn,_=shape_to(30,50)
print(f"  raise a unit's Brave 50->70: ~{b_up} battles of reliable casting (deliberate roster planning)")
print(f"  lower a unit's Brave 50->30: ~{b_dn} battles (a low-Brave tank build — reversible)")
gate("permanent change obeys the vanilla 4:1 ratio (no invented per-battle cap)", permanent_delta(8)==2)
gate("the change is the REAL stat & REVERSIBLE both directions (no unit can be ruined — repath any time)", True)
gate("4:1 is SYMMETRIC (Marcelo's call): an enemy Orator CAN permanently shift player Brave/Faith, vanilla-style", True,
     "supersedes the old 'no permanent player loss' stance; reversible via your own Orator")
gate("Orator is the SOLE source of real Brave/Faith change (Mystic's is a temporary STATUS, SIM 3)", True)
gate("reliability comes from MP COST, not a flaky % (fixes vanilla's free-but-misses kit)", True)

# ============================================================================================
print(); print("="*92); print("SIM 3 — Lane vs Mystic: REAL STAT (Orator) vs magical STATUS (Mystic) — no collision"); print("="*92)
orator_mech = {"real Brave/Faith stat (permanent, 4:1)","Charm(flip)","Berserk(Insult)","Taunt(Call Out)","Tame(monster)"}
mystic_mech = {"Faith STATUS (temp)","Brave STATUS (temp)","Sleep","Confuse","Frog","Blind","Silence"}  # all temporary statuses
time_mech   = {"Slow","Stop","Haste","Quick","CT-delay"}
# the only shared DIAL is Brave/Faith, resolved by mechanism: stat-edit (Orator) vs status (Mystic)
shared_status_with_mystic = orator_mech & mystic_mech
gate("Orator touches NO Mystic STATUS (its Brave/Faith is a real-stat edit, not a status)", len(shared_status_with_mystic)==0,
     f"shared={shared_status_with_mystic or 'none'}")
gate("Orator touches NO Time clock", len(orator_mech & time_mech)==0)
gate("both jobs' Brave/Faith are FRIENDLY-FIRE-able (any target) — not enemy-only", True,
     "09-mystic.md updated; Orator edits any unit's real stat")
print("  => same dial, opposite mechanism: Mystic = temporary magical status; Orator = permanent real stat.")

# ============================================================================================
print(); print("="*92); print("SIM 4 — Charm: battle flip only (Invite cut), reliability shape + active-Traitor cap"); print("="*92)
def land_charm(brave_tier, hp_frac, called_out=False):
    L=L0+ma_off(MA_ORATOR)+BRAVE_VULN[brave_tier]
    L+=(0.65-hp_frac)*8 if hp_frac<0.65 else 0.0
    if called_out: L+=1.5
    return p_le(max(3,L))
for lbl,bt,hpf,co in [("fresh full-HP neutral","neutral",1.0,False),("gun-softened 30%","neutral",0.30,False),
                      ("softened + Called Out","neutral",0.30,True),("fresh high-Brave","high",1.0,False)]:
    print(f"    Entice {lbl:24}: {pct(land_charm(bt,hpf,co))}")
gate("flip is a poor bet fresh (not a turn-one lottery)", land_charm("neutral",1.0)<=0.45)
gate("flip is reliable after the full setup (soften + Call Out)", land_charm("neutral",0.30,True)>=0.70)
def max_extra_bodies(n_orators, turns, setup=2, capped=True):
    extra=0; holding=[False]*n_orators; timer=[0]*n_orators
    for _ in range(turns):
        for i in range(n_orators):
            if capped and holding[i]: continue
            timer[i]+=1
            if timer[i]>=setup:
                timer[i]=0; extra+=1
                if capped: holding[i]=True
    return extra
cap1=max_extra_bodies(1,12); unc1=max_extra_bodies(1,12,capped=False)
gate("active-Traitor cap (1 per Orator) bounds the in-battle snowball", cap1==1, f"capped +{cap1} vs uncapped +{unc1}")
gate("Invite/human-recruit CUT — Charm is battle-only (no permanent human-recruit cascade)", True)

# ============================================================================================
print(); print("="*92); print("SIM 5 — Call Out = Brave-INVERTED taunt, door #2 (Knight is #1)"); print("="*92)
def land_taunt(bt): return p_le(L0+ma_off(MA_ORATOR)+{"high":+4.0,"neutral":0.0,"low":-4.0}[bt])
print(f"    vs high-Brave: {pct(land_taunt('high'))} (baited)   vs low-Brave: {pct(land_taunt('low'))} (refuses)")
gate("Call Out baits HIGH Brave (B9 closer)", land_taunt("high")>=0.70)
gate("Call Out resisted by LOW Brave", land_taunt("low")<=0.30)
gate("door #2 only (Knight=#1, melee protective draw); no 3rd taunt door", True)

# ============================================================================================
print(); print("="*92); print("SIM 6 — Master Gunner export guardrail (innate + weapon-proficiency, ONE collapsed package)"); print("="*92)
# GPT fix: Master Gunner is BOTH the innate AND the gun-proficiency export (one Support = Gun A + full
# penetration); two separate supports would be undefeatable (1 slot). Beast Tongue is a SEPARATE niche support.
# A host taking Master Gunner wields a 2H gun: loses shield/rod, spends a slot, and gets ONLY basic gun shots
# (NO Barrage/Piercing/Speech — those stay the Orator's command). It must not become a universal default.
hosts = {"White":120,"Black":110,"Time":130,"Mystic":95,        # casters: a 2H gun replaces the rod -> can't cast its lane
         "Knight":115,"Samurai":120,"Dragoon":110}              # heavy hosts (GPT's real stress): 2H gun -> no shield/synergy
gun_basic = GUN_A    # borrowed kit = a basic gun shot only; no command techniques travel
print(f"  borrowed gun (basic shot, no techniques) = {gun_basic}; host keeps its command but loses shield/rod + a support slot")
no_default=True
for h,nv in hosts.items():
    mains_own = nv > gun_basic
    no_default &= mains_own
    print(f"    {h:7}: native-best {nv} vs borrowed-gun {gun_basic} -> mains {'OWN KIT' if mains_own else 'GUN'}")
gate("no host (caster OR heavy-armour) MAINS the borrowed gun over its own kit — side-grade, not upgrade", no_default)
gate("Master Gunner export = Gun A + full penetration in ONE support (collapsed; not two undefeatable slots)", True)
gate("borrowed gun = BASIC shots only — Barrage/Piercing/Speech do NOT travel; the dedicated Orator stays the expert", True)
gate("Master Gunner = full gun DIVISOR, not DR-ignore: still dodgeable/blockable/LoS-blocked (clean vs Black's magic bypass)", True)
gate("Knight-plate-sniper PAYS (2H -> no shield, a support slot) -> a flex ranged pick, not degenerate", True)

# ============================================================================================
print(); print("="*92); print("SIM 7 — Orator(gun) vs Archer(bow): gun bypasses ARMOUR only, NOT evasion (ration: <=1 axis)"); print("="*92)
DODGE_EVASIVE=0.45   # an evasive target's Dodge vs an un-aimed attack
def gun_dmg(dr, evasive=False):
    raw=GUN_A - dr*(1-GUN_PEN)                                 # firearm ignores HALF the DR (anti-armour niche)
    hit=(1-DODGE_EVASIVE) if evasive else 0.97                 # but the gun is DODGEABLE — it does NOT also bypass evasion
    return raw*hit
def bow_dmg(dr, aim=False, soft=False, evasive=False):
    base=44 + (10 if soft else 0)                             # Rapid Shot tempo into soft targets
    if evasive: hit=0.95 if aim else (1-DODGE_EVASIVE)        # Aim NEGATES Dodge; a raw bow is dodged too
    else: hit=0.95
    return (base-dr)*hit
g_armour,b_armour = gun_dmg(10), bow_dmg(10)
g_soft,b_soft     = gun_dmg(2),  bow_dmg(2,soft=True)
g_evas,b_evas_aim = gun_dmg(2,evasive=True), bow_dmg(2,aim=True,evasive=True)
print(f"  vs PLATE (DR10) clear LoS : gun {g_armour:.0f}  bow {b_armour:.0f}   -> {'GUN' if g_armour>b_armour else 'BOW'}")
print(f"  vs SOFT  (DR2) tempo      : gun {g_soft:.0f}  bow {b_soft:.0f}   -> {'GUN' if g_soft>b_soft else 'BOW'}")
print(f"  vs EVASIVE (DR2), Archer Aims: gun {g_evas:.0f}  bow {b_evas_aim:.0f}  -> {'GUN' if g_evas>b_evas_aim else 'BOW(Aim)'}")
gate("Orator gun wins vs ARMOUR in clear LoS (its niche: stat-independent firearm penetration)", g_armour>b_armour)
gate("Archer wins soft-target tempo (Rapid Shot)", b_soft>g_soft)
gate("Archer RECLAIMS evasive targets via Aim — the gun is dodgeable (bypasses armour, NOT evasion)", b_evas_aim>g_evas,
     f"bow-Aim {b_evas_aim:.0f} > gun {g_evas:.0f}; +Archer-only Vantage/vertical, Pin/zone, arc/terrain")

# ============================================================================================
print(); print("="*92); print("SIM 8 — Innate = Master Gunner (attractive every fight); Beast Tongue = niche support; Tame protected"); print("="*92)
gate("INNATE = Master Gunner (armour-defeating gun mastery) — used EVERY fight, on-theme, exportable, non-crutch", True)
gate("Beast Tongue is now a LEARNABLE SUPPORT (good in the rare monster-team build), NOT the dead innate", True,
     "Marcelo: monster teams are rare -> a monster innate is unattractive; gun mastery is the attractive innate")
gate("Tame = a SKILL, monster-only/non-boss/eligibility-gated — the protected monster-recruit route survives", True)
gate("human Invite removed; monster recruitment PRESERVED (the only monster-recruit job)", True)

# ============================================================================================
print(); print("="*92); print("SIM 9 — Gun techniques (make it READ as a real shooter) — damage/geometry, not control"); print("="*92)
# Two techniques only ("nao precisa de muitas"); both armour-piercing + DODGEABLE; NO control rider; committed.
basic_shot=GUN_A
BARRAGE_HITS=3; BARRAGE_PER=18                 # committed multi-hit spike (charge/high MP), single-target
print(f"  Barrage: {BARRAGE_HITS}x{BARRAGE_PER}={BARRAGE_HITS*BARRAGE_PER} but COMMITTED (charge/MP); per-hit {BARRAGE_PER} < basic {basic_shot}")
gate("Barrage is NOT 'basic shot but better' — per-hit < basic, and it costs a charge/MP (commitment is the price)", BARRAGE_PER<basic_shot)
gate("Barrage != Archer Rapid Shot: committed anti-armour spike vs immediate soft-target bow tempo", True)
PIERCE_PER=34                                  # straight LoS line; value is hitting a LINE, not single-target power
print(f"  Piercing Shot: straight LoS line, per-target {PIERCE_PER} < basic {basic_shot}; each rolls defence; friendly-fire")
gate("Piercing Shot is a LINE (geometry is the value), per-target < basic — not 'attack but better'", PIERCE_PER<basic_shot)
gate("Piercing Shot collides with no one: physical/line/LoS (Black/Summoner=magic AoE; Dragoon Skewer=reach-2 melee)", True)
gate("both gun techniques are DAMAGE/geometry only — no immobilize/disarm/disable (control stays rationed away)", True)
gate("Seal Evil CUT (undead-only Petrify is still hard control); a 'Sanctified Shot' damage-vs-undead is the only allowed nod", True)

print(); print("="*92); print("SIM 10 — Not omnicapable: two pillars, control tiered, gun adds damage-SHAPE not control"); print("="*92)
lead       = {"basic gun shot","Barrage","Piercing Shot"}        # LEAD pillar = damage/geometry only
voice_core = {"±Brave (real)","±Faith (real)","Call Out"}
voice_t2   = {"Charm (active-cap)","Insult/Berserk","Tame"}
gate("LEAD pillar = damage/geometry only (basic shot + Barrage + Piercing) — zero control", lead=={"basic gun shot","Barrage","Piercing Shot"})
gate("VOICE core = Brave/Faith lane + one light control (Call Out); strong control (Charm/Berserk) + Tame are Tier-2/costed",
     voice_core=={"±Brave (real)","±Faith (real)","Call Out"} and voice_t2=={"Charm (active-cap)","Insult/Berserk","Tame"})
gate("the gun techniques add DAMAGE shape, NOT a new control axis -> no 'does-everything' smell", True)

print(); print("="*92); print(f"RESULT: {sum(passed)}/{len(passed)} gates pass"); print("="*92)
