#!/usr/bin/env python3
"""
Geomancer (v2, MELEE) battle-simulation gate. Frozen DCL placeholders.
Identity: melee Axe-A bruiser whose tactical options come from the ground underfoot. NORMAL ATTACK is the
melee bread-and-butter (strong, Axe A, also Sword/Knife). COMMAND "Geomancy" = ONE short-range (2-3 tiles)
terrain-keyed tactical tool (element + reliable status), DELIBERATELY BELOW a normal hit in damage -> never
a "better normal attack" you'd spam. Chassis: Clothes & Suits, Move 4 + Ignore Terrain, strong PA, low
Faith (= magic-resist, its durability, NOT DR), shield/Doublehand fork. Geomancy = PA-scaled, DR-subject,
elemental (roster contrast: Black = DR-ignore/Faith/burst). Status reliable BY POSITION (stand on the
terrain), high base rate, still resistible/immune. No sustain / no hard-control / no guard-shred.
"""
from functools import lru_cache
from itertools import product

@lru_cache(maxsize=None)
def p_le(n):
    if n<3: return 0.0
    if n>=18: return 1.0
    return sum(1 for a,b,d in product(range(1,7),repeat=3) if a+b+d<=n)/216.0

G,PEN=5.0,0.25
SW=lambda pa:0.40*pa
WOUND={"cut":1.5,"thrust":2.0,"crush":1.0,"missile":1.0}
WMOD={"axe":6,"sword":3,"knife":1}
DR={"heavy":{"cut":9,"thrust":8,"crush":3,"missile":8},"clothes":{"cut":2,"thrust":2,"crush":2,"missile":2},"robes":{"cut":0,"thrust":0,"crush":0,"missile":0}}
BOFF={"low":0.76,"neutral":1.0,"high":1.35}
GEODR={"heavy":6,"clothes":2,"robes":0}     # DR-subject elemental DR per armor (placeholder)
ELEM={"weak":1.5,"neutral":1.0,"resist":0.5,"immune":0.0}
GEO_BASE=3                                    # Geomancy spell-like base (placeholder) — kept LOW on purpose

def melee(pa,w,t,arm,br="neutral"):
    raw=SW(pa)+WMOD[w]; return max(PEN*raw,raw-DR[arm][t])*WOUND[t]*BOFF[br]*G
def geomancy(pa,arm,elem="neutral",br="neutral"):
    raw=SW(pa)+GEO_BASE; return max(PEN*raw,raw-GEODR[arm])*ELEM[elem]*BOFF[br]*G
def p_crit(sk): return p_le(4 if sk<15 else 5 if sk==15 else 6)
def lands(sk,deft,facing="front"):
    pc=p_crit(sk); pconn=p_le(sk)
    if facing=="back": return pconn
    dt=deft+(-2 if facing=="side" else 0)
    return pc+max(0,pconn-pc)*(1-p_le(dt))

GEO=dict(pa=13,hp=130,axesk=14)   # Axe A skill 14, strong PA melee body

print("="*86)
print("SIM 1 — ★ DESIGN LAW: Geomancy must stay BELOW a normal melee hit (never a better-normal-attack)")
print("="*86)
ax=melee(13,"axe","cut","clothes","high")
print(f"  Normal attack (Axe A, PA13, high-Brave) vs clothes:   {ax:5.1f}/hit   <- the melee bread-and-butter")
print(f"  Geomancy vs clothes, by element:")
for e in ("neutral","weak"):
    g=geomancy(13,"clothes",e,"high")
    print(f"    {e:8}: {g:5.1f}  ({g/ax:.0%} of a normal hit)")
print(f"  -> Even ON A WEAKNESS, Geomancy ({geomancy(13,'clothes','weak','high'):.0f}) < a normal swing ({ax:.0f}).")
print(f"     When you CAN reach in melee, hitting is always the higher-damage play -> Geomancy is NEVER spam.")
print(f"     Its value is REACH / ELEMENT / STATUS, not raw damage. Law satisfied structurally.")

print()
print("="*86)
print("SIM 2 — J1: melee bruiser first; Geomancy as the TACTIC (reach / weakness / status)")
print("="*86)
print(f"  REACH: adjacent -> normal attack ({ax:.0f}). Can't reach (2-3 tiles)? Geomancy still hits ({geomancy(13,'clothes','neutral','high'):.0f}) ")
print(f"         -> no dead turn when melee whiffs the gap (the vanilla 'useless if it can't close' fix).")
print(f"  WEAKNESS: vs an ice-weak target, Snowstorm keys ice -> {geomancy(13,'clothes','weak','high'):.0f} (vs {geomancy(13,'clothes','neutral','high'):.0f} neutral).")
print(f"  vs HEAVY (DR-subject bites): Geomancy {geomancy(13,'heavy','neutral','high'):.0f} (DR eats it) — bring the normal axe")
print(f"         (axe cut vs heavy = {melee(13,'axe','cut','heavy','high'):.0f}, crush vs heavy = {melee(13,'axe','crush','heavy','high'):.0f}) or a DR-ignoring Black.")

print()
print("="*86)
print("SIM 3 — STATUS is TACTICAL & reliable-by-position (Oil combo / Blind), not a lottery")
print("="*86)
# Oil: next fire hit amplified. Blind: target accuracy ~halved.
fire_no_oil=geomancy(13,"clothes","weak","high")            # a fire Magma Surge on a fire-weak target
fire_after_oil=fire_no_oil*1.5                              # Oil amplifies the follow-up fire
print(f"  OIL combo: Magma Surge applies Oil (after dmg) -> the NEXT fire hit (own/ally Black) x1.5:")
print(f"    follow-up fire {fire_no_oil:.0f} -> {fire_after_oil:.0f}. A planned 2-step, not a 20% proc.")
blind_atk_before=melee(13,"sword","cut","clothes","high")*lands(14,8)
blind_atk_after =melee(13,"sword","cut","clothes","high")*lands(14,8)*0.5   # Blind ~halves accuracy
print(f"  BLIND (Sandstorm): an enemy physical attacker's output {blind_atk_before:.0f} -> ~{blind_atk_after:.0f} (accuracy ~halved).")
print(f"  Reliability = you must STAND on the terrain to access that status (positional cost) + high base")
print(f"  rate, still resisted/immune. 'Reliable' = strategically dependable, NOT guaranteed/immunity-bypass.")

print()
print("="*86)
print("SIM 4 — SURVIVAL & the off-hand fork (Shield Block vs Doublehand)")
print("="*86)
HP=GEO['hp']
dh=melee(13,"axe","cut","clothes","high")            # Doublehand-ish ceiling (treat as the +wmod build)
print(f"  Doublehand: more damage, exposed. Shield: finite Block (active defence), less damage. A real fork.")
mag=38.0*0.6   # incoming magic shrugged by LOW Faith
dive=melee(14,"axe","cut","clothes","high")*lands(14,9)   # a bruiser dives it
print(f"  Magic shrugged by LOW Faith: ~{mag:.0f}/turn -> TTK {HP/mag:.1f} (magic-resistant, its durability).")
print(f"  Dived by a bruiser: ~{dive:.0f}/turn -> TTK {HP/dive:.1f} (Clothes, no Heavy DR; Shield Block helps but drains).")
print(f"  -> durable vs MAGIC, foldable to focused PHYSICAL dive: honest two-sided (it IS a frontliner, so it")
print(f"     fights back with normal attacks + Nature's Wrath rather than trying to avoid melee).")

print()
print("="*86)
print("SIM 5 — PORTABILITY: Axe A export + Landreader; welcome splash, not strictly-better")
print("="*86)
print(f"  Geomancy-secondary WITHOUT Landreader: can't key the tile -> no element/status (a weak non-elemental")
print(f"  short poke). WITH Landreader (a slot): a real terrain elementalist on its own body. Axe Mastery")
print(f"  (Axe A) exports like the Archer's Bow A — a high-PA host swings Axe A hard, BUT pays the support")
print(f"  slot + lacks Landreader/Ignore-Terrain/Nature's Wrath unless it spends MORE slots. The native gets")
print(f"  ALL of it free on the magic-resist mobile chassis. Welcome splash; flag Axe-A magnitude for the")
print(f"  grade-budget reconciliation (15).")

print()
print("="*86)
print("SIM 6 — LANE CHECK & ENEMY-USE")
print("="*86)
print(f"  Distinct: Knight (Heavy wall/guard-break) · Monk (unarmed dive/sustain) · Samurai (Faith-free area")
print(f"  burst) · Dragoon (reach/Jump) · Black (DR-ignore Faith burst). Geo = melee + terrain tactics, no")
print(f"  sustain/hard-control/guard-shred, DR-subject elemental.")
print(f"  Enemy Geomancer: durable frontliner that Blinds your archers, Oils your fire-weak, Poisons your")
print(f"  wall, shrugs your magic. Counter: kite it (short reach), focus-fire the Clothes body, bring")
print(f"  element-neutral/resistant units, deny it varied terrain (barren/cramped maps shrink its tactics).")
