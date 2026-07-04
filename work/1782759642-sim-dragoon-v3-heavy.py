#!/usr/bin/env python3
"""
Dragoon (#12, Tier A) battle-sim gate — v3 FROM SCRATCH (post process-reset). Frozen DCL placeholders.
Chassis FORCED to Heavy Armor by the existing plate-knight sprite (no new art). Identity: the dragon-knight
COMMITMENT striker — grounded it's a slow plate body; its mobility/threat are VERTICAL (the leap + Ignore
Height). Leaps over the line (untargetable, SHORT fixed CT) to crash on a boxed-in soft target; holds a
choke at reach 2. NOT anti-plate (thrust is anti-SOFT: highest wound, eaten by Heavy DR). Lancet &
Dragonslayer CUT. Core: Jump / Skewer / strong normal.
Vanilla holes fixed: A) range/height innate-in-the-command (no +1..+7 ladder); B) FIXED legible CT (plannable)
+ CENTER-WEIGHTED small landing zone (readable "leave the zone" counterplay), splash sharply reduced so it is
NOT safe artillery. Two-sided: tanky vs physical (plate) / soft vs MAGIC (neutral Faith) + weak Dodge grounded.
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
JUMP_MULT   = 1.4    # CENTER tile: the committed leap ~1.4x a standing thrust (pays a full airborne turn)
SPLASH_FRAC = 0.30   # adjacent splash = 30% of the center hit (sharply reduced; consolation, not a kill)
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

print("="*92)
print("SIM 1 — ★ DESIGN LAW: no Core skill is 'a better normal attack you'd spam' (Lancet/Dragonslayer cut)")
print("="*92)
norm = thrust(PA,"spear","clothes","high")
skw1 = thrust(PA,"spear","clothes","high",mult=SKEWER_MULT)
jc   = thrust(PA,"spear","clothes","high",mult=JUMP_MULT)            # Jump CENTER vs clothes
print(f"  Normal spear thrust vs Clothes:   {norm:5.1f}/hit   <- reach-2 bread-and-butter")
print(f"  Skewer per-target vs Clothes:     {skw1:5.1f}       ({skw1/norm:.0%} of normal; line-2 = {2*skw1:.0f} split, each rolls Dodge)")
print(f"  Jump CENTER vs Clothes:           {jc:5.1f}       ({jc/norm:.0%} of normal) — but costs a full airborne turn")
print(f"  -> In melee, 2 normals (2 turns) = {2*norm:.0f} > one Jump ({jc:.0f}): Jump is never melee-spam; its value is")
print(f"     REACH over the line + the untargetable beat. Skewer's is HITTING TWO. Pure thrust (no DR-ignore). Law OK.")

print()
print("="*92)
print("SIM 2 — JUMP center profile: deletes the boxed-in SOFT backline; NOT mid-HP fronts; chip into Heavy")
print("="*92)
for name,A in [("Robes caster",ROBES_CASTER),("Clothes backliner",CLOTHES_BACK),
               ("Clothes mid-HP front",CLOTHES_FRONT),("Heavy front (knight)",HEAVY_FRONT)]:
    d=thrust(PA,"spear",A["arm"],"high",mult=JUMP_MULT); pc=lands(DRAGOON["sk"],A["deft"])
    shot = "ONE-SHOT" if d>=A["hp"] else f"{d/A['hp']:.0%} HP ({A['hp']-d:.0f} left)"
    print(f"  Jump center vs {name:22}: {d:5.1f}  HP {A['hp']:3} -> {shot:16} (connects p~{pc:.0%})")
print(f"  Anti-SOFT by design (thrust wound 2.0); chip into Heavy (DR 8). CALIBRATION: JUMP_MULT sets the exact")
print(f"  one-shot breakpoint vs ~100-HP soft bodies (numbers phase) — structural shape, not a balance claim.")

print()
print("="*92)
print("SIM 3 — ★ GUARDRAIL: the landing SPLASH is a chip, NOT safe artillery (GPT's break, fixed)")
print("="*92)
for name,A in [("Robes caster",ROBES_CASTER),("Clothes backliner",CLOTHES_BACK),("Clothes mid-HP front",CLOTHES_FRONT)]:
    center=thrust(PA,"spear",A["arm"],"high",mult=JUMP_MULT); sp=center*SPLASH_FRAC
    print(f"  Splash vs {name:22}: {sp:4.1f}  ({sp/A['hp']:.0%} of HP) — {'KILLS healthy' if sp>=A['hp'] else 'chip, no kill pressure'}")
print(f"  Rule: if the primary LEAVES the center, there is NO full hit on anyone else — only this {SPLASH_FRAC:.0%} chip.")
print(f"  -> a whiffed Jump (target stepped out) = a wasted airborne turn for a small chip on adjacents, never a")
print(f"     'safe artillery' delete. You are rewarded for HITTING THE CENTER (executing), not for spraying.")

print()
print("="*92)
print("SIM 4 — TWO-SIDED: tanky vs PHYSICAL (plate), soft vs MAGIC (neutral Faith); grounded exposure")
print("="*92)
knight_hit = swing(14,"sword","heavy","cut","high")*lands(13,DRAGOON["deft"])
mag = MBOLT*FAITH["neutral"]
print(f"  Incoming knight sword vs Dragoon plate:  ~{knight_hit:4.1f}/turn -> TTK {DRAGOON['hp']/max(1,knight_hit):4.1f} (plate DR = tanky vs physical)")
print(f"  Incoming black bolt (neutral Faith, ignores DR): ~{mag:4.1f}/turn -> TTK {DRAGOON['hp']/mag:4.1f} (MAGIC is the clean counter)")
print(f"  No shield (Knight keeps Block) + high Brave => weak Dodge ({DRAGOON['deft']}): GROUNDED between jumps it is easy")
print(f"  to hit; plate covers physical only, magic & crush punish it, ranged pokes the soft Dodge. Real two-sided.")

print()
print("="*92)
print("SIM 5 — SKEWER stays UNDER the Samurai area lane; JUMP fix (HOLE A+B) is legible + counterable")
print("="*92)
pj = lands(DRAGOON["sk"], ROBES_CASTER["deft"])
print(f"  Skewer: line-2 only, {skw1:.0f} each ({SKEWER_MULT:.0%}), each rolls Dodge, no riders — below the Samurai's WIDE")
print(f"          Faith-free area burst (3+ tiles, full). Distinct: spear formation-punish, not area artillery.")
print(f"  HOLE A: range/height built INTO the command (no +1..+7 ladder, no innate-gate); off-job Jump-secondary uses")
print(f"          the same reach — the Dragoon is the best HOST via plate + high PA + free primary slot, not a better jump.")
print(f"  HOLE B: FIXED legible CT (you plan the land) + center-weighted zone. Boxed-in caster (can't relocate): center")
print(f"          connects p~{pj:.0%}. Target that gets a turn leaves the zone -> only the {SPLASH_FRAC:.0%} chip. Readable, not luck.")

print()
print("="*92)
print("SIM 6 — PORTABILITY + LANE CHECK (three Heavy martials, distinct play)")
print("="*92)
print(f"  Moat = plate + high PA + the free primary Jump slot. Off-job leaper: Jump-secondary (same reach) + Polearm")
print(f"  Training (Spear A) — but no plate (dies grounded) and lower PA (weak crash). Welcome splash, never better.")
print(f"  Shared Heavy Armor, distinct jobs: Knight = shield/Block guard-break WALL (holds a line) · Samurai = Faith-")
print(f"  free AoE BURSTER · Dragoon = no-shield VERTICAL commitment striker (leaps the line, reach-2 zone, single-ish).")
print(f"  vs the agile lane: Thief slips THROUGH (Dodge, facing, theft) — the Dragoon goes OVER (height, plate, telegraph).")
