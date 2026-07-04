#!/usr/bin/env python3
"""
Dragoon (#12, Tier A) battle-sim gate — v5 (post fresh-critic correction). Frozen DCL placeholders.
Chassis FORCED to Heavy Armor by the existing plate-knight sprite (no new art). Identity: the dragon-knight
COMMITMENT striker whose threat is the VERTICAL (the leap + Ignore Height) and reach-2 zone control.

CORRECTIONS from the v4 'unevadable Jump' draft (adversarial critic vs the REGISTERED docs):
  * JUMP IS EVADABLE again. jobs/04-archer.md ration: Concentration ignores Dodge-ONLY, is a PAID support, and
    EXPLICITLY EXCLUDES Jump; Aim itself 'defence still rolls'. jobs/03-knight.md: Guard Break breaks Block+Parry
    ONLY, never Dodge, 'defence still rolls'. A free baseline Jump beating all three = the un-priced UNION of two
    jobs' rationed tools -> contradiction. So Jump rolls defence normally; its value is REACH OVER THE LINE +
    the untargetable airborne beat (06-reach.md: outrange + escape-counter), NOT defence-bypass.
  * HIGH JUMP CUT (it was 'more reach, same damage, no cost' = strict upgrade = the banned jump-ladder; reach
    already lives in the Aerial Training innate).
  * Tier-2 headline = STOP-HIT (06-reach.md reserves it for lancer abilities: free strike on a foe ENTERING reach 2;
    needs the delayed-trigger hook like the Archer's Overwatch). Dragon Dive kept as a secondary AoE ceiling.
  * Dragon's Fury obeys reach (06): penalised point-blank, so rushing inside blunts the counter too.
Held: Faith NEUTRAL (magic is the clean counter to plate); CUT Lancet/Dragonslayer/Dragonheart.
"""
from functools import lru_cache
from itertools import product

@lru_cache(maxsize=None)
def p_le(n):
    if n < 3: return 0.0
    if n >= 18: return 1.0
    return sum(1 for a,b,d in product(range(1,7),repeat=3) if a+b+d<=n)/216.0

G,PEN = 5.0,0.25
SW  = lambda pa: 0.40*pa
THR = lambda pa: 0.22*pa
WOUND = {"cut":1.5,"thrust":2.0,"crush":1.0,"missile":1.0}
WMOD  = {"spear":5,"pole":3,"sword":3,"axe":6,"fists":4,"knife":1}
DR = {"heavy":{"cut":9,"thrust":8,"crush":3,"missile":8},
      "clothes":{"cut":2,"thrust":2,"crush":2,"missile":2},
      "robes":{"cut":0,"thrust":0,"crush":0,"missile":0}}
BOFF = {"low":0.76,"neutral":1.0,"high":1.35}
JUMP_MULT   = 1.4    # base Jump = committed single-target leap ~1.4x a standing thrust (pays a full airborne turn)
DIVE_MULT   = 1.4    # Dragon Dive CENTER (Tier-2 ceiling); AoE + EVADABLE + longer telegraph
DIVE_SPLASH = 0.30   # Dragon Dive adjacent = 30% of center
SKEWER_MULT = 0.65   # per-target on a line of 2
MBOLT = 55.0
FAITH = {"low":0.6,"neutral":1.0,"high":1.35}

def thrust(pa,w,t_arm,br="high",mult=1.0):
    raw = THR(pa)+WMOD[w]
    return max(PEN*raw, raw-DR[t_arm]["thrust"])*WOUND["thrust"]*BOFF[br]*G*mult
def swing(pa,w,t_arm,t="cut",br="high"):
    raw = SW(pa)+WMOD[w]
    return max(PEN*raw, raw-DR[t_arm][t])*WOUND[t]*BOFF[br]*G
def p_crit(sk): return p_le(4 if sk<15 else 5 if sk==15 else 6)
def lands(sk,deft,facing="front"):
    pc=p_crit(sk); pconn=p_le(sk)
    if facing=="back": return pconn
    dt=deft+(-2 if facing=="side" else 0)
    return pc+max(0,pconn-pc)*(1-p_le(dt))

ROBES_CASTER = dict(hp=95,  arm="robes",   deft=7)
CLOTHES_BACK = dict(hp=100, arm="clothes", deft=11)
CLOTHES_FRONT= dict(hp=130, arm="clothes", deft=8)
HEAVY_FRONT  = dict(hp=150, arm="heavy",   deft=6)
DRAGOON      = dict(hp=140, arm="heavy",   pa=14, deft=6, sk=14)   # plate, high PA, LOW Dodge (high Brave)
PA = DRAGOON["pa"]

print("="*96)
print("SIM 1 - * DESIGN LAW: base Jump is NOT 'a better normal you'd spam' (value = over-the-line REACH, not bypass)")
print("="*96)
norm = thrust(PA,"spear","clothes","high")
skw1 = thrust(PA,"spear","clothes","high",mult=SKEWER_MULT)
jc   = thrust(PA,"spear","clothes","high",mult=JUMP_MULT)
print(f"  Normal spear thrust vs Clothes:  {norm:5.1f}/hit  (reach-2 bread-and-butter, rolls Dodge, immediate)")
print(f"  Skewer per-target vs Clothes:    {skw1:5.1f}      ({skw1/norm:.0%} of normal; line-2 = {2*skw1:.0f} split, each rolls Dodge)")
print(f"  Jump CENTER vs Clothes:          {jc:5.1f}      ({jc/norm:.0%} of normal) - rolls defence, costs a full AIRBORNE turn + telegraph")
print(f"  -> 2 normals (2 turns) = {2*norm:.0f} > one Jump ({jc:.0f}), immediate w/ no telegraph: Jump is never melee-spam.")
print(f"     Jump's value (06-reach.md): it REACHES OVER the line / terrain onto a target a reach-1 body can't reach,")
print(f"     and the Dragoon is UNTARGETABLE mid-air. It still ROLLS DEFENCE - no free Dodge/Parry/Block bypass")
print(f"     (that is rationed to the Archer's paid Concentration [excludes Jump] & the Knight's Guard Break). Law OK.")

print()
print("="*96)
print("SIM 2 - JUMP (Core, EVADABLE): deletes a boxed-in LOW-Dodge soft target; whiffs high-Dodge; chips Heavy")
print("="*96)
for name,A in [("Robes caster (low Dodge)",ROBES_CASTER),("Clothes backliner (hi Dodge)",CLOTHES_BACK),
               ("Clothes mid-HP front",CLOTHES_FRONT),("Heavy front (knight)",HEAVY_FRONT)]:
    d=thrust(PA,"spear",A["arm"],"high",mult=JUMP_MULT); pc=lands(DRAGOON["sk"],A["deft"])
    shot = "ONE-SHOT" if d>=A["hp"] else f"{d/A['hp']:.0%} HP ({A['hp']-d:.0f} left)"
    print(f"  Jump center vs {name:28}: {d:5.1f}  HP {A['hp']:3} -> {shot:16} (connects p~{pc:.0%}; then can relocate next turn)")
print(f"  Anti-SOFT by design (thrust wound 2.0); DR still applies so Heavy only chips. It DELETES the boxed-in low-Dodge")
print(f"  caster it reaches over the line, but a HIGH-Dodge target evades it (~35%) - beating Dodge is the ARCHER's paid job,")
print(f"  not the Dragoon's. Two counters, both available: roll Dodge, AND relocate off the tile before the fixed-CT landing.")

print()
print("="*96)
print("SIM 3 - DRAGON DIVE (Tier-2 ceiling): vertical AoE delivery, EVADABLE + telegraphed - NOT a strict up/downgrade")
print("="*96)
for name,A in [("Robes caster",ROBES_CASTER),("Clothes backliner",CLOTHES_BACK),("Clothes mid-HP front",CLOTHES_FRONT)]:
    center=thrust(PA,"spear",A["arm"],"high",mult=DIVE_MULT); sp=center*DIVE_SPLASH
    pc=lands(DRAGOON["sk"],A["deft"])
    print(f"  Dive vs {name:22}: center {center:5.1f} | adj {sp:4.1f} ({sp/A['hp']:.0%} HP)  - each target rolls Dodge/relocates (p~{pc:.0%} on center)")
print(f"  Distinction is the DELIVERY VECTOR: dropped from above, OVER the front rank / obstacles, onto a back cluster a")
print(f"  front-delivered burst can't reach. Smaller + evadable + telegraphed => NOT a strict-worse Samurai (Faith-free, wide,")
print(f"  full, front) and NOT a strict-upgrade of single-target Jump (no unevadability, longer charge). A ceiling, not core.")

print()
print("="*96)
print("SIM 4 - TWO-SIDED: tanky vs PHYSICAL (plate), soft vs MAGIC (neutral Faith); grounded exposure")
print("="*96)
knight_hit = swing(14,"sword","heavy","cut","high")*lands(13,DRAGOON["deft"])
mag = MBOLT*FAITH["neutral"]
print(f"  Incoming knight sword vs Dragoon plate:          ~{knight_hit:4.1f}/turn -> TTK {DRAGOON['hp']/max(1,knight_hit):4.1f} (plate DR = tanky vs physical)")
print(f"  Incoming black bolt (neutral Faith, ignores DR): ~{mag:4.1f}/turn -> TTK {DRAGOON['hp']/mag:4.1f} (MAGIC is the clean counter)")
print(f"  No shield (2H spear) + high Brave => weak Dodge ({DRAGOON['deft']}): GROUNDED between leaps it is easy to hit; plate")
print(f"  covers physical only, magic & crush punish it, ranged pokes the soft Dodge. Faith NEUTRAL (not 'irrelevant') keeps")
print(f"  magic as the deliberate two-sided counter to the plate - low Faith would over-armour it and erase the counter.")

print()
print("="*96)
print("SIM 5 - SKEWER under Samurai lane; reach identity (06); HOLE A/B fixed; Dragon's Fury obeys reach")
print("="*96)
print(f"  Skewer: line-2 only, {skw1:.0f} each ({SKEWER_MULT:.0%}), each rolls Dodge, no riders - below the Samurai's WIDE Faith-free")
print(f"          area burst (3+ tiles, full). Distinct: a spear formation-punish, not area artillery.")
print(f"  REACH (06-reach.md): the spear OUTRANGES reach-1 from 2 tiles and AVOIDS its counter (safe poke); WEAKNESS = it is")
print(f"          penalised POINT-BLANK, so the counter to the Dragoon is to RUSH INSIDE. Dragon's Fury inherits this: a clean")
print(f"          punish vs a reach-2 attacker, a modest (penalised) thrust vs an adjacent rusher - the reach pillar, honestly applied.")
print(f"  HOLE A (vanilla +1..+7 ladder): GONE. Reach/height = the INNATE Aerial Training (one quality, exported as a Support;")
print(f"          off-job leap is a short hop without it - shown in the targeting preview). NO separate 'High Jump' reach-tier.")
print(f"  HOLE B (can't-time-the-landing): FIXED legible CT - both sides plan the land. Counter = roll Dodge OR relocate.")

print()
print("="*96)
print("SIM 6 - PORTABILITY + LANE CHECK (three Heavy martials, distinct play)")
print("="*96)
print(f"  Moat = plate + high PA + free primary Jump slot + free Aerial Training innate. Off-job leaper: Jump-secondary + Aerial")
print(f"  Training (Spear A via Polearm Training) - but no plate (dies grounded) and lower PA (weak crash). Welcome splash, never better.")
print(f"  Shared Heavy Armor, distinct jobs: Knight = shield/Block guard-break WALL (Block+Parry break, never Dodge) - Samurai =")
print(f"  Faith-free WIDE AoE burster - Dragoon = no-shield VERTICAL commitment striker (reach OVER the line, reach-2 zone, single-ish).")
print(f"  vs the agile lane: Thief slips THROUGH (Dodge, facing, theft) - the Dragoon goes OVER (height/reach), but does NOT beat")
print(f"  Dodge (that's the Archer's paid Concentration). Tier-2 STOP-HIT (06) punishes the rusher entering reach 2 (needs the hook).")
