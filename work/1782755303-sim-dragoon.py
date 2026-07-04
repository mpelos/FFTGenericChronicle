#!/usr/bin/env python3
"""
Dragoon (#12, Tier A) battle-simulation gate. Frozen DCL placeholders.
Identity: durable VERTICAL BACKLINE-CRASHER + spear-reach chokepoint controller (NOT anti-plate — that was
the inherited break: thrust eats Heavy DR and has the HIGHEST wound mult, so the spear is anti-SOFT). Leaps
OVER the front line (untargetable, SHORT airtime) to crash a high-wound thrust onto a boxed-in squishy, and
holds a corridor at reach 2. Chassis: Heavy Armor (plate, -1 Move), high PA, NEUTRAL Faith (magic is the
counter to the plate anvil), high Brave two-sided (offense+courage reaction / weak active defense), moderate
HP, reach-2 spear. Core: Jump (short-air untargetable crash, rolls Dodge) / Lancet (reach-2 HP/MP drain,
sub-normal, sustain+anti-caster) / Skewer (line-2 formation punish, reduced, no riders). Design laws checked:
no skill is "a better normal attack you'd spam"; no PHYSICAL DR-ignore; two-sided cost real.
"""
from functools import lru_cache
from itertools import product

@lru_cache(maxsize=None)
def p_le(n):
    if n < 3: return 0.0
    if n >= 18: return 1.0
    return sum(1 for a,b,d in product(range(1,7),repeat=3) if a+b+d<=n)/216.0

G,PEN = 5.0,0.25
SW  = lambda pa: 0.40*pa     # swing component (cut weapons)
THR = lambda pa: 0.22*pa     # thrust component (spear/pole) — low per-PA, but WOUND 2.0 is highest
WOUND = {"cut":1.5,"thrust":2.0,"crush":1.0,"missile":1.0}
WMOD  = {"spear":5,"pole":3,"sword":3,"axe":6,"fists":4,"knife":1}
DR = {"heavy":{"cut":9,"thrust":8,"crush":3,"missile":8},
      "clothes":{"cut":2,"thrust":2,"crush":2,"missile":2},
      "robes":{"cut":0,"thrust":0,"crush":0,"missile":0}}
BOFF = {"low":0.76,"neutral":1.0,"high":1.35}
# Dragoon ability placeholders (fixed BEFORE reading outcomes):
JUMP_MULT   = 1.4    # the committed leap crashes ~1.4x a standing thrust (pays a turn of airtime)
LANCET_MULT = 0.6    # Lancet raw is 60% of a normal thrust — deliberately sub-normal
HEAL_FRAC   = 0.5    # Lancet heals 50% of damage dealt (partial, not full)
SKEWER_MULT = 0.65   # per-target on a line of 2 (reduced each)
# magic (neutral Faith app=1.0; ignores DR) — placeholder black-bolt incoming, scaled by Faith
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

# archetypes (HP / armour / defence-skill)
ROBES_CASTER = dict(hp=95,  arm="robes",   deft=7)    # black/white/time
CLOTHES_BACK = dict(hp=100, arm="clothes", deft=11)   # archer/thief (evasive-ish)
CLOTHES_FRONT= dict(hp=130, arm="clothes", deft=8)    # monk/geomancer mid-HP frontliner
HEAVY_FRONT  = dict(hp=150, arm="heavy",   deft=6)    # knight
DRAGOON      = dict(hp=130, arm="heavy",   pa=14, deft=6, sk=14)  # high PA, plate, LOW active defence (high Brave)
PA = DRAGOON["pa"]

print("="*90)
print("SIM 1 — ★ DESIGN LAW: no Core skill is 'a better normal attack you'd spam' (+ no phys DR-ignore)")
print("="*90)
norm = thrust(PA,"spear","clothes","high")
lan  = thrust(PA,"spear","clothes","high",mult=LANCET_MULT)
skw1 = thrust(PA,"spear","clothes","high",mult=SKEWER_MULT)
jmp  = thrust(PA,"spear","clothes","high",mult=JUMP_MULT)
print(f"  Normal spear thrust vs Clothes:        {norm:5.1f}/hit  <- the reach-2 bread-and-butter")
print(f"  Lancet (drain) vs Clothes:             {lan:5.1f}      ({lan/norm:.0%} of normal) -> never a normal-attack clone")
print(f"  Skewer per-target vs Clothes:          {skw1:5.1f}      ({skw1/norm:.0%} of normal each; line of 2 = {2*skw1:.0f} split, each rolls Dodge)")
print(f"  Jump vs Clothes:                       {jmp:5.1f}      ({jmp/norm:.0%} of normal) — costs a full airborne turn")
print(f"  -> In melee range, 2 normal attacks (2 turns) = {2*norm:.0f} > one Jump ({jmp:.0f}). Jump is NEVER melee-spam;")
print(f"     its value is REACH (over the front line) + the untargetable beat, not raw per-turn damage. Law OK.")
print(f"  -> All damage is THRUST (DR-subject, no DR-ignore). Anti-Heavy is NOT the Dragoon's lane.")

print()
print("="*90)
print("SIM 2 — JUMP target profile: deletes the boxed-in SOFT backline; NOT mid-HP fronts; poor into Heavy")
print("="*90)
for name,A in [("Robes caster",ROBES_CASTER),("Clothes backliner",CLOTHES_BACK),
               ("Clothes mid-HP front",CLOTHES_FRONT),("Heavy front (knight)",HEAVY_FRONT)]:
    d=thrust(PA,"spear",A["arm"],"high",mult=JUMP_MULT)
    shot = "ONE-SHOT" if d>=A["hp"] else f"{d/A['hp']:.0%} of HP ({A['hp']-d:.0f} left)"
    print(f"  Jump vs {name:22}: {d:5.1f}  HP {A['hp']:3} -> {shot}")
print(f"  Read: cleanly deletes Robes/low-HP Clothes (the isolated caster/archer you leap to); does NOT")
print(f"  reliably one-shot the mid-HP Clothes frontliner; chip-only into Heavy. Matches the intended profile.")

print()
print("="*90)
print("SIM 3 — TWO-SIDED: tanky vs PHYSICAL (plate), soft vs MAGIC (neutral Faith); grounded exposure")
print("="*90)
# incoming physical: a knight sword swing onto the plate Dragoon
knight_hit = swing(14,"sword","heavy","cut","high")*lands(13,DRAGOON["deft"])
mag = MBOLT*FAITH["neutral"]   # magic ignores plate DR; neutral Faith = full
print(f"  Incoming knight sword vs Dragoon plate:  ~{knight_hit:4.1f}/turn -> TTK {DRAGOON['hp']/max(1,knight_hit):4.1f} (plate DR + crush-light sword = tanky)")
print(f"  Incoming black bolt vs Dragoon (neutral Faith, ignores DR): ~{mag:4.1f}/turn -> TTK {DRAGOON['hp']/mag:4.1f} (MAGIC is the counter)")
print(f"  -> tanky vs physical / soft vs magic = clean two-sided (contrast: Geomancer is Clothes+low-Faith =")
print(f"     magic-RESIST but physical-fragile; Dragoon is the inverse). High Brave = weak Dodge ({DRAGOON['deft']}):")
print(f"     when GROUNDED between Jumps it is easy to hit; plate covers physical, magic/crush punish it.")

print()
print("="*90)
print("SIM 4 — LANCET sustain: clutch, NOT un-attritionable (esp. Heavy-vs-Heavy)")
print("="*90)
lan_c = thrust(PA,"spear","clothes","high",mult=LANCET_MULT); heal_c=lan_c*HEAL_FRAC
lan_h = thrust(PA,"spear","heavy","high",mult=LANCET_MULT);   heal_h=lan_h*HEAL_FRAC
print(f"  Lancet vs Clothes:  dmg {lan_c:4.1f} -> heal {heal_c:4.1f}/turn  (vs a normal thrust {norm:.0f} you'd rather throw)")
print(f"  Lancet vs Heavy:    dmg {lan_h:4.1f} -> heal {heal_h:4.1f}/turn  (thrust eaten by plate -> tiny heal: NO Heavy-duel lock)")
print(f"  Heavy-vs-Heavy duel: enemy knight does ~{knight_hit:.0f}/turn to the Dragoon; Lancet heals only {heal_h:.0f}")
print(f"  -> net {-(knight_hit-heal_h):+.0f}/turn: the Dragoon STILL loses an attrition duel into plate. Sustain is")
print(f"     clutch (top up after a dive vs soft targets), never a heal engine. Guardrail holds; no hard cap needed.")

print()
print("="*90)
print("SIM 5 — SKEWER stays UNDER the Samurai area lane (narrow line-2, reduced, riderless)")
print("="*90)
print(f"  Skewer = line of 2 only, {skw1:.0f} each ({SKEWER_MULT:.0%} of normal), each rolls Dodge, no status/no DR-bypass.")
print(f"  Total {2*skw1:.0f} across exactly 2 in-line tiles vs Samurai's WIDE Faith-free area burst (3+ tiles, full).")
print(f"  -> chokepoint/formation punish, spear-shaped; clearly below the Samurai's area-damage role. (Samurai")
print(f"     numbers are a later job; the structural gap — line-2-reduced vs wide-full — is the guarantee.)")

print()
print("="*90)
print("SIM 6 — JUMP reliability: the short-airtime fix vs the vanilla 'walk away' feel-bad")
print("="*90)
# A jump still rolls Dodge on landing; a target that gets a turn can relocate off the tile.
pj = lands(DRAGOON["sk"], ROBES_CASTER["deft"])
print(f"  Boxed-in caster (walled by its own front line, can't relocate): Jump connects p~{pj:.0%} (rolls Dodge), lands the delete.")
print(f"  Mobile/unscreened target that gets a turn: can step off the tile -> Jump WHIFFS (counterplay intact).")
print(f"  Short airtime = LESS time for the target to get a turn before landing AND a briefer untargetable window")
print(f"  (a one-beat dodge, not a parking spot). Durability = PLATE, not disappearing. Two-sided, vanilla feel-bad fixed.")

print()
print("="*90)
print("SIM 7 — PORTABILITY + LANE CHECK")
print("="*90)
print(f"  Native moat = high PA + PLATE + free innate (High Jump) on a reach-2 spear body. An off-job leaper pays")
print(f"  Jump-secondary + High Jump support (full leap; without it = weak vanilla-style long-air leap) + usually")
print(f"  Polearm Training (Spear A) — 2-3 slots, and STILL lacks plate/PA. Welcome splash, never strictly-better.")
print(f"  Lane: Thief slips THROUGH (fragile, facing/theft, Dodge-survives) · Ninja = fast multi-hit assassin ·")
print(f"  Dragoon reaches the backline by HEIGHT + delayed commitment (untargetable air, single heavy thrust,")
print(f"  plate-durable, no facing/theft/stealth/multi-hit). Distinct approach, distinct durability.")
