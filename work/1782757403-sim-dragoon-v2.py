#!/usr/bin/env python3
"""
Dragoon (#12, Tier A) battle-simulation gate — v2, FFT-FAITHFUL rework. Frozen DCL placeholders.
Marcelo course-correction: FFT-faithfulness is the lens; fix the REAL vanilla holes; no invented mechanics.
  - Lancet CUT (HP+MP both-drain isn't FFT, and drain is off-theme for a Lancer).
  - HOLE A (the Jump +1..+7 range/height LADDER = fake choice, only the top matters) -> range/height is now an
    INNATE FIXED job property (no purchasable tiers); exported as Support "Jump Training".
  - HOLE B (Speed-derived, unpredictable, tempo-dead landing) -> Jump gets a FIXED SHORT CT (legible landing
    tick, not Speed-derived); timing is plannable + the escape window shrinks, but counterplay stays (a target
    that gets a turn can move/be healed; landing still rolls Dodge). NOT auto-hit.
Identity unchanged: durable VERTICAL backline-crasher + spear-reach chokepoint controller (anti-soft, not
anti-plate). Chassis: Heavy Armor (plate, -1 Move), high PA, NEUTRAL Faith (magic counters the anvil), high
Brave two-sided, moderate HP, reach-2 spear. Core: Jump / Skewer / strong normal. R: Dragon's Fury.
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
THR = lambda pa: 0.22*pa     # thrust: low per-PA, but WOUND 2.0 is highest -> anti-SOFT
WOUND = {"cut":1.5,"thrust":2.0,"crush":1.0,"missile":1.0}
WMOD  = {"spear":5,"pole":3,"sword":3,"axe":6,"fists":4,"knife":1}
DR = {"heavy":{"cut":9,"thrust":8,"crush":3,"missile":8},
      "clothes":{"cut":2,"thrust":2,"crush":2,"missile":2},
      "robes":{"cut":0,"thrust":0,"crush":0,"missile":0}}
BOFF = {"low":0.76,"neutral":1.0,"high":1.35}
JUMP_MULT   = 1.4    # the committed leap crashes ~1.4x a standing thrust (pays a full airborne turn)
SKEWER_MULT = 0.65   # per-target on a line of 2 (reduced each)
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
DRAGOON      = dict(hp=130, arm="heavy",   pa=14, deft=6, sk=14)
PA = DRAGOON["pa"]

print("="*90)
print("SIM 1 — ★ DESIGN LAW: no Core skill is 'a better normal attack you'd spam' (Lancet cut; all FFT-real)")
print("="*90)
norm = thrust(PA,"spear","clothes","high")
skw1 = thrust(PA,"spear","clothes","high",mult=SKEWER_MULT)
jmp  = thrust(PA,"spear","clothes","high",mult=JUMP_MULT)
print(f"  Normal spear thrust vs Clothes:   {norm:5.1f}/hit  <- the reach-2 bread-and-butter")
print(f"  Skewer per-target vs Clothes:     {skw1:5.1f}      ({skw1/norm:.0%} of normal each; line-2 = {2*skw1:.0f} split, each rolls Dodge)")
print(f"  Jump vs Clothes:                  {jmp:5.1f}      ({jmp/norm:.0%} of normal) — but costs a full airborne turn")
print(f"  -> In melee range, 2 normal attacks (2 turns) = {2*norm:.0f} > one Jump ({jmp:.0f}): Jump is NEVER melee-spam.")
print(f"     Its value is REACH over the front line + the untargetable beat. Skewer's is HITTING TWO. Law OK.")

print()
print("="*90)
print("SIM 2 — JUMP target profile: deletes the boxed-in SOFT backline; NOT mid-HP fronts; poor into Heavy")
print("="*90)
for name,A in [("Robes caster",ROBES_CASTER),("Clothes backliner",CLOTHES_BACK),
               ("Clothes mid-HP front",CLOTHES_FRONT),("Heavy front (knight)",HEAVY_FRONT)]:
    d=thrust(PA,"spear",A["arm"],"high",mult=JUMP_MULT)
    shot = "ONE-SHOT" if d>=A["hp"] else f"{d/A['hp']:.0%} of HP ({A['hp']-d:.0f} left)"
    pc = lands(DRAGOON["sk"], A["deft"])
    print(f"  Jump vs {name:22}: {d:5.1f}  HP {A['hp']:3} -> {shot:18}  (connects p~{pc:.0%}; evasive bodies dodge more)")
print(f"  Read: deletes the isolated Robes/low-HP Clothes you leap to (accepted identity — you spent an exposed")
print(f"  turn to reach it); does NOT one-shot the mid-HP Clothes front; chip into Heavy. CALIBRATION: JUMP_MULT")
print(f"  sets the exact one-shot breakpoint vs ~100-HP soft bodies (numbers phase), not a structural break.")

print()
print("="*90)
print("SIM 3 — TWO-SIDED: tanky vs PHYSICAL (plate), soft vs MAGIC (neutral Faith); grounded exposure")
print("="*90)
knight_hit = swing(14,"sword","heavy","cut","high")*lands(13,DRAGOON["deft"])
mag = MBOLT*FAITH["neutral"]
print(f"  Incoming knight sword vs Dragoon plate:  ~{knight_hit:4.1f}/turn -> TTK {DRAGOON['hp']/max(1,knight_hit):4.1f} (plate DR = tanky vs physical)")
print(f"  Incoming black bolt (neutral Faith, ignores DR): ~{mag:4.1f}/turn -> TTK {DRAGOON['hp']/mag:4.1f} (MAGIC is the counter)")
print(f"  -> tanky vs physical / soft vs magic = clean two-sided, the exact inverse of the Geomancer (Clothes +")
print(f"     low-Faith = magic-resist / physical-fragile). High Brave = weak Dodge ({DRAGOON['deft']}): grounded between")
print(f"     Jumps it is easy to hit — plate covers physical, magic & crush punish it. Real counterplay.")

print()
print("="*90)
print("SIM 4 — SKEWER stays UNDER the Samurai area lane (narrow line-2, reduced, riderless)")
print("="*90)
print(f"  Skewer = line of 2 only, {skw1:.0f} each ({SKEWER_MULT:.0%} of normal), each rolls Dodge, no status/no DR-bypass.")
print(f"  Total {2*skw1:.0f} across exactly 2 in-line tiles vs the Samurai's WIDE Faith-free area burst (3+ tiles, full).")
print(f"  -> chokepoint/formation punish, spear-shaped (FFT line-AoE is real); clearly below the Samurai area role.")

print()
print("="*90)
print("SIM 5 — JUMP fixed (HOLE A+B): legible timing + innate range, counterplay intact (the vanilla fix)")
print("="*90)
pj = lands(DRAGOON["sk"], ROBES_CASTER["deft"])
print(f"  HOLE A fix: range/height is INNATE & FIXED (generous) — no +1..+7 ladder, no fake 'buy the biggest'.")
print(f"             Exported as Support 'Jump Training': an off-job Jump-secondary hops short; the support grants")
print(f"             the Dragoon's real reach/height. (FFT-faithful: range was always the vanilla dimension.)")
print(f"  HOLE B fix: FIXED SHORT CT (legible landing tick, NOT Speed-derived) -> you can PLAN the land + the")
print(f"             escape window shrinks. Boxed-in caster (walled by its own line, can't relocate): connects p~{pj:.0%}.")
print(f"             Mobile/unscreened target that still gets a turn -> can step off / be healed -> Jump whiffs.")
print(f"  -> legible + worth the commitment, WITHOUT becoming auto-hit (that would be oppressive). Counterplay kept.")

print()
print("="*90)
print("SIM 6 — PORTABILITY + LANE CHECK")
print("="*90)
print(f"  Native moat = high PA + PLATE + the free innate (generous Jump range) on a reach-2 spear body. An off-job")
print(f"  leaper pays Jump-secondary + Jump Training (real reach) + usually Polearm Training (Spear A) — 2-3 slots,")
print(f"  and STILL lacks plate/PA (weak crash, dies grounded). Welcome splash, never strictly-better.")
print(f"  Lane: Thief slips THROUGH (fragile, facing/theft, Dodge-survives) · Ninja = fast multi-hit assassin ·")
print(f"  Dragoon reaches the backline by HEIGHT + delayed legible commitment (untargetable air, single heavy")
print(f"  thrust, plate-durable; no facing/theft/stealth/multi-hit). Distinct approach, distinct durability.")
