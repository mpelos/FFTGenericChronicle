#!/usr/bin/env python3
"""
Dragoon (#12, Tier A) battle-sim gate — v4 (ADOPTED design, post third-agent merge). Frozen DCL placeholders.
Chassis FORCED to Heavy Armor by the existing plate-knight sprite (no new art). Identity: the dragon-knight
COMMITMENT striker — grounded it's a slow plate body; its threat is VERTICAL (the leap + Ignore Height) and
reach-2 zone control. NOT anti-plate (thrust is anti-SOFT: highest wound, eaten by Heavy DR).

Merged from a third agent's plan (kept the two ideas that beat our converged draft):
  * JUMP beats ACTIVE DEFENSE — unevadable (no Dodge/Parry/Block), still respects DR; counter = RELOCATE off the
    marked tile. Gives Jump a real niche (the answer to high-Dodge / high-Block), one clean positional counterplay.
  * AoE moved OUT of base Jump into a Tier-2 capstone DRAGON DIVE; base Jump is pure SINGLE-TARGET precision
    (kills the 'safe artillery' worry by removal). Dragon Dive is AoE but EVADABLE + longer telegraph (real choice).
Held against the third agent: Faith NEUTRAL (magic stays the counter, not 'irrelevant'); NO Lunge (normal-attack
clone + needs distance-scaled melee dmg the engine lacks); CUT Dragonheart (Reraise = universal mine-bait -> healer
lanes), keep Dragon's Fury. Innate ADOPTED: Aerial Training (leap reach/height; exported as Support).
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
DIVE_MULT   = 1.4    # Dragon Dive CENTER (Tier-2); same crash, but AoE + EVADABLE + longer telegraph
DIVE_SPLASH = 0.30   # Dragon Dive adjacent = 30% of center (the cluster-punish consolation)
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
print("SIM 1 - * DESIGN LAW: base Jump is NOT 'a better normal you'd spam' (it is unevadable + over-the-line)")
print("="*96)
norm = thrust(PA,"spear","clothes","high")
skw1 = thrust(PA,"spear","clothes","high",mult=SKEWER_MULT)
jc   = thrust(PA,"spear","clothes","high",mult=JUMP_MULT)
print(f"  Normal spear thrust vs Clothes:  {norm:5.1f}/hit  (reach-2 bread-and-butter, rolls Dodge, immediate)")
print(f"  Skewer per-target vs Clothes:    {skw1:5.1f}      ({skw1/norm:.0%} of normal; line-2 = {2*skw1:.0f} split, each rolls Dodge)")
print(f"  Jump CENTER vs Clothes:          {jc:5.1f}      ({jc/norm:.0%} of normal) - costs a full AIRBORNE turn + telegraph")
print(f"  -> 2 normals (2 turns) = {2*norm:.0f} > one Jump ({jc:.0f}), and are immediate w/ no telegraph: Jump is never melee-spam.")
print(f"     Jump's value is being UNEVADABLE (beats Dodge/Parry/Block) + reaching OVER the line onto a boxed-in target;")
print(f"     paid by tempo (a turn aloft), a telegraph, and a clean positional counter (relocate). Distinct role, not 'normal+'.")

print()
print("="*96)
print("SIM 2 - JUMP (Core): UNEVADABLE single-target, respects DR. Deletes boxed-in SOFT; chip into Heavy")
print("="*96)
for name,A in [("Robes caster",ROBES_CASTER),("Clothes backliner",CLOTHES_BACK),
               ("Clothes mid-HP front",CLOTHES_FRONT),("Heavy front (knight)",HEAVY_FRONT)]:
    d=thrust(PA,"spear",A["arm"],"high",mult=JUMP_MULT)
    shot = "ONE-SHOT" if d>=A["hp"] else f"{d/A['hp']:.0%} HP ({A['hp']-d:.0f} left)"
    print(f"  Jump center vs {name:22}: {d:5.1f}  HP {A['hp']:3} -> {shot:18} (lands unless target RELOCATES; Dodge/Block don't save it)")
print(f"  Anti-SOFT by design (thrust wound 2.0); DR still applies so Heavy only takes a chip. Unevadable => no Dodge-RNG")
print(f"  whiff: the ONLY out is leaving the tile. So it reliably cracks high-Dodge skirmishers & high-Block walls (its niche),")
print(f"  but a target that still has its turn just steps away. JUMP_MULT sets the one-shot breakpoint vs ~100-HP soft (numbers phase).")

print()
print("="*96)
print("SIM 3 - DRAGON DIVE (Tier-2 capstone): AoE crash, EVADABLE + telegraphed = cluster-punish, NOT safe artillery")
print("="*96)
for name,A in [("Robes caster",ROBES_CASTER),("Clothes backliner",CLOTHES_BACK),("Clothes mid-HP front",CLOTHES_FRONT)]:
    center=thrust(PA,"spear",A["arm"],"high",mult=DIVE_MULT); sp=center*DIVE_SPLASH
    pc=lands(DRAGOON["sk"],A["deft"])
    print(f"  Dive vs {name:22}: center {center:5.1f} | adj {sp:4.1f} ({sp/A['hp']:.0%} HP)  - each target rolls Dodge/relocates (p~{pc:.0%} on center)")
print(f"  Trades base Jump's UNEVADABLE property for AREA: every target in the zone can Dodge OR relocate, and the charge is")
print(f"  longer (Tier-2 acquisition + telegraph). So Dive != a strict upgrade of Jump (precision-unevadable vs area-evadable),")
print(f"  and a cluster that scatters eats only the {DIVE_SPLASH:.0%} adjacent chip. Punishes BAD formation; never free artillery.")

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
print(f"  magic as the deliberate two-sided counter to the plate - low Faith would over-armor it and erase the counter.")

print()
print("="*96)
print("SIM 5 - SKEWER under the Samurai lane; HOLE A/B fixed (Aerial Training reach + fixed CT + relocate counter)")
print("="*96)
print(f"  Skewer: line-2 only, {skw1:.0f} each ({SKEWER_MULT:.0%}), each rolls Dodge, no riders - below the Samurai's WIDE Faith-free")
print(f"          area burst (3+ tiles, full). Distinct: a spear formation-punish, not area artillery.")
print(f"  HOLE A (vanilla +1..+7 ladder): GONE. Reach/height live in the INNATE Aerial Training (one quality, not a ladder),")
print(f"          exported as a Support - an off-job needs Jump-secondary + Aerial Training for the full leap; the Dragoon is")
print(f"          the best HOST (plate + high PA + free primary slot + free innate), never a 'better jump' bought piecemeal.")
print(f"  HOLE B (can't-time-the-landing): FIXED legible CT (you plan the land) + UNEVADABLE => the read is positional, not luck.")
print(f"          Boxed-in target lands; a target with a turn relocates. Readable both ways.")

print()
print("="*96)
print("SIM 6 - PORTABILITY + LANE CHECK (three Heavy martials, distinct play)")
print("="*96)
print(f"  Moat = plate + high PA + free primary Jump slot + free Aerial Training innate. Off-job leaper: Jump-secondary + Aerial")
print(f"  Training (Spear A via Polearm Training) - but no plate (dies grounded) and lower PA (weak crash). Welcome splash, never better.")
print(f"  Shared Heavy Armor, distinct jobs: Knight = shield/Block guard-break WALL (holds a line) - Samurai = Faith-free AoE")
print(f"  BURSTER - Dragoon = no-shield VERTICAL commitment striker (unevadable leap over the line, reach-2 zone, single-ish).")
print(f"  vs the agile lane: Thief slips THROUGH (Dodge, facing, theft) - the Dragoon goes OVER (height, plate, unevadable crash).")
print(f"  Reaction Dragon's Fury (reach-2 counter-thrust, gated by spear+reach+Brave) reinforces the choke; Dragonheart CUT")
print(f"  (Reraise = universal mine-bait, belongs to healer lanes). Movement Ignore Height (vanilla-faithful vertical traversal).")
