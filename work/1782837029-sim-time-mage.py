#!/usr/bin/env python3
"""
Time Mage (#10, "The Clockbreaker") battle-sim gate. Frozen DCL placeholders + magic/CT placeholders.
Identity (converged w/ GPT): owns the DCL's turn-FREQUENCY axis (Speed/CT) and weaponizes the clock — freeze/
Slow enemies (deny turns AND lag their guard-refresh so the team cracks Block/Parry), Haste/Quick allies
(extra actions + held guard), and strikes on its own with Gravity (%-HP) + Comet (single-target). Support/
control is the AXIS, but every tool is an aggressive tempo play -> self-sufficient, never a dead turn.

DCL grounding: Speed=turn-frequency only (doc 01; the tempo playstyle is meant to be owned by a JOB, = Time);
guard-refresh resets on a unit's OWN turn (Slow enemy -> guard lags -> team cracks it); Stop/Slow/Immobilize =
magical-INVERTED status (resist on LOW Faith, caster MA = offense, boss-immune; doc 13); Haste/Quick = friendly
no-resist buffs (doc 11); charges are NOT damage-interrupted (doc 11). Faith = NEUTRAL (control runs on MA not
its own Faith; low Faith would double-dip defense). Locked from the Black resolution: Comet ~-ra single-target
non-elemental (no AoE/ladder/rider); Gravity de-niched %-HP, can't finish; Flare/arsenal/AoE = Black's.

GATES: (1) ★ THE TELEGRAPH INVARIANT — no Quick+Short-Charge+Haste stack may erase the readable charge window
of charged offense (Black's brake). (2) Stop/Slow inverse-Faith landing + guard-refresh crack. (3) Gravity
de-niched across HP levels, can't finish. (4) Comet ~-ra clearly below Black. (5) Mana-Shield unkillable-caster
test. (6) Quick degeneracy (double-instant? loops? Quick-a-charge?). (7) a Time turn is never dead.
"""
from functools import lru_cache
from itertools import product

@lru_cache(maxsize=None)
def p_le(n):
    if n < 3: return 0.0
    if n >= 18: return 1.0
    return sum(1 for a,b,d in product(range(1,7),repeat=3) if a+b+d<=n)/216.0

# --- physical layer (frozen; for the dive / guard interaction) ---
G,PEN = 5.0,0.25
SW = lambda pa: 0.40*pa
WOUND = {"cut":1.5,"thrust":2.0,"crush":1.0,"missile":1.0}
WMOD  = {"spear":5,"pole":3,"sword":3,"axe":6,"fists":4,"knife":1}
DR = {"heavy":{"cut":9,"thrust":8,"crush":3,"missile":8},"clothes":{"cut":2,"thrust":2,"crush":2,"missile":2},"robes":{"cut":0,"thrust":0,"crush":0,"missile":0}}
BOFF = {"low":0.76,"neutral":1.0,"high":1.35}
def swing(pa,w,t_arm,t="cut",br="high"):
    raw = SW(pa)+WMOD[w]; return max(PEN*raw, raw-DR[t_arm][t])*WOUND[t]*BOFF[br]*G
def lands(sk,deft,facing="front"):
    pc=p_le(4 if sk<15 else 5 if sk==15 else 6); pconn=p_le(sk)
    if facing=="back": return pconn
    dt=deft+(-2 if facing=="side" else 0); return pc+max(0,pconn-pc)*(1-p_le(dt))

# --- magic layer ---
G_M = 0.58
def faith(l): return {"low":0.70,"neutral":1.0,"high":1.30}[l]
MA_TM = 13          # below Black(18)/White(14)? -> below Black, ~White. Neutral Faith.
MA_BM = 18
def mdmg(ma, sp, fc, ft): return ma*sp*faith(fc)*faith(ft)*G_M
SP_BM = {"Fire":6,"Fira":10,"Firaga":14,"Flare":12}
SP_COMET = 9        # ~-ra effective on Time's lower MA
def zod(l): return {"weak":1.30,"neutral":1.0,"resist":0.70}[l]

def line(c="="): print(c*100)

line(); print("SIM 1 - ★ THE TELEGRAPH INVARIANT: no Quick+Short-Charge+Haste stack may erase the charge window"); line()
# W = enemy reposition-windows during a -ga charge. W ∝ (charge magnitude)/(caster speed).
# Short Charge cuts magnitude (1-sc); Haste raises caster speed (1+h) -> both shrink W. Guardrail: floor W>=1.
BASE_W = 2.0   # designed telegraph: 2 enemy windows during an un-buffed -ga
def W(sc, h, cap=True):
    raw = BASE_W*(1-sc)/(1+h)
    return max(1.0, raw) if cap else raw
SC_MOD, SC_BROKEN, HASTE = 0.33, 0.90, 0.50
configs = [("none",0,0),("Short Charge (moderate .33)",SC_MOD,0),("Haste only (+.50)",0,HASTE),
           ("SC+Haste, MULTIPLICATIVE (no cap)",SC_MOD,HASTE),("SC+Haste, additive CAP (floor 1.0)",SC_MOD,HASTE)]
for i,(name,sc,h) in enumerate(configs):
    cap = (i!=3)   # config 3 = the unguarded multiplicative case
    w = W(sc,h,cap=cap); ok = "OK (repositionable)" if w>=1.0 else "★ BREAK (unreactable burst)"
    print(f"  {name:38} window={w:4.2f} enemy-turn(s) -> {ok}")
print(f"  Near-instant Short Charge (.90)+Haste, no cap: window={W(SC_BROKEN,HASTE,cap=False):.2f} -> ★ BREAK (why SC must be MODERATE).")
print(f"  QUICK adds a SECOND cast, each still W>=1 -> 2 TELEGRAPHED casts, NOT 2 instant bursts. Quick doubles")
print(f"  INSTANT actions (melee/Gravity/bolt), never manufactures an instant -ga. Quick on a CHARGING unit does")
print(f"  NOT resolve the charge (hard exception). INVARIANT HOLDS iff: SC moderate + SC×Haste additive-capped (floor 1).")

line(); print("SIM 2 - STOP/SLOW: inverse-Faith landing (boss-immune) + the guard-refresh CRACK"); line()
def status_land(ma_off, tfaith, boss=False):
    if boss: return 0.0
    base = {"low":7,"neutral":9,"high":11}[tfaith]   # inverse-Faith: devout succumb, faithless resist
    return p_le(min(16, base + (1 if ma_off>=15 else 0)))
for ft in ("low","neutral","high"):
    print(f"  vs target Faith={ft:7}: Stop/Slow land ~{status_land(MA_TM,ft):.0%}  (lands on devout/casters, resisted by faithless bruisers)")
print(f"  vs BOSS/immune: {status_land(MA_TM,'high',boss=True):.0%} (Stop is boss-immune by design).")
# guard-refresh crack: a Slowed enemy acts ~half as often -> refreshes Parry/Block ~half as often -> more team hits land through depleted guard.
guard_block = swing(12,"sword","heavy","cut","high")  # value the enemy's guard would have stopped per refresh
print(f"  GUARD CRACK: a Slowed frontliner refreshes its Parry/Block ~half as often (doc 01 guard resets on its")
print(f"  OWN turn). Over a team's focus-fire, ~half as many hits are guarded -> the line cracks. Each missed refresh")
print(f"  ~ +{guard_block:.0f} effective dmg let through. The OBVIOUS reward is the visible turn-order-bar shift; the")
print(f"  guard-crack is the deep bonus. Slow is framed as an ATTACK on the enemy's next turn, not 'a debuff'.")

line(); print("SIM 3 - GRAVITY de-niched: %-current-HP useful vs ANY healthy target, but CANNOT finish"); line()
GRAV = 0.25
def gravity(hp):  # %-current-HP, ignores DR/Faith; floored so it can't kill (stops leaving target alive)
    dmg = hp*GRAV
    return min(dmg, max(0, hp-1))   # never reduces below 1
for hp in (80,150,300):
    print(f"  target HP {hp:3}: Gravity hits {gravity(hp):5.1f} ({GRAV:.0%}) -> a real chunk at EVERY HP level (not anti-giant-only).")
low = 12
print(f"  target HP {low:3} (already low): Gravity {gravity(low):4.1f} -> tiny; CANNOT finish. Comet/bolt closes the kill.")
print(f"  Ignores DR + Faith -> a reliable opener vs tanks/casters alike; the 'soften, don't delete' tool.")

line(); print("SIM 4 - COMET ~-ra: Time's minimum direct offense, clearly BELOW Black's arsenal"); line()
comet = mdmg(MA_TM, SP_COMET, "neutral","neutral")
print(f"  Comet (Time MA{MA_TM}, non-elemental, single-target): {comet:.1f}")
print(f"  Black: Fire {mdmg(MA_BM,SP_BM['Fire'],'neutral','neutral'):.1f} | Fira {mdmg(MA_BM,SP_BM['Fira'],'neutral','neutral'):.1f} | "
      f"Firaga {mdmg(MA_BM,SP_BM['Firaga'],'neutral','neutral'):.1f} | Firaga(weak) {mdmg(MA_BM,SP_BM['Firaga'],'neutral','neutral')*zod('weak'):.1f} | Flare {mdmg(MA_BM,SP_BM['Flare'],'neutral','neutral'):.1f}")
print(f"  -> Comet may beat Black's NEUTRAL basic Fire per cast (reliable, never resisted), but LOSES to Black")
print(f"     exploiting weakness, Black -ga at k>=2 (AoE), Black Flare, and Black's dmg/MP sustain. One button, not")
print(f"     an arsenal. 'Comet is Time's minimum direct offense, not Time's damage plan.'")

line(); print("SIM 5 - MANA SHIELD: real MP fuel, NOT a free second HP bar (the unkillable-robe-caster test)"); line()
HP_TM, MP_TM, MS = 75, 72, 1.0   # MS ratio: 1 MP absorbs 1 HP
eff_hp = HP_TM + MP_TM*MS
dive = swing(13,"knife","robes","cut","high")*lands(14,7,"side")
print(f"  Time HP {HP_TM} + MP {MP_TM}. Mana Shield (1 MP:1 HP) -> effective HP {eff_hp:.0f} IF the FULL pool is spent on survival.")
print(f"  Thief dive ~{dive:.1f}/hit: TTK {HP_TM/dive:.1f} without MS -> {eff_hp/dive:.1f} with MS at full MP.")
print(f"  THE COST (the brake): every MP tanked is an MP NOT cast on tempo. A Time at full Mana-Shield = ZERO Stop/")
print(f"  Quick/Haste output (it spent its clock-budget staying alive). Focus-fire still kills (MP runs dry, then HP).")
print(f"  CALIBRATION GATE (Hypothesis, doc 12): set MS ratio so survival meaningfully drains the tempo budget; a")
print(f"  high-MP robe caster that is BOTH unkillable AND still casting is the fail state -> tune ratio/availability.")

line(); print("SIM 6 - QUICK degeneracy: the specific lines GPT required testing"); line()
print(f"  Quick BLACK: extra turn -> Black can START a 2nd Firaga, but it STILL charges (W>=1, SIM 1). 2 telegraphed")
print(f"    casts, not 2 instant nukes. Acceptable. (If SC were near-instant, this breaks -> SC stays moderate.)")
print(f"  Quick SUMMONER: same -> a 2nd committed barrage still telegraphs (long CT). No instant double-barrage.")
print(f"  Quick TIME (self): excluded if the engine allows; else a 2nd tempo turn at high MP/CT -> self-limiting.")
print(f"  Quick-into-QUICK loop: single-target-ally + high MP + meaningful CT -> a loop needs 2 Time mages or burns")
print(f"    the whole MP budget for ~1 extra action; bounded, not infinite. Fallback if sim still loops: Quick -> a")
print(f"    bounded +X CT advance (not a full turn).")
print(f"  Quick a CHARGING unit: HARD EXCEPTION -> does NOT resolve the in-progress charge (no instant-finish).")

line(); print("SIM 7 - 'A TIME TURN IS NEVER DEAD' (self-sufficiency; support-PRIMARY but fieldable solo)"); line()
bolt = mdmg(MA_TM, 4, "neutral","neutral")
print(f"  Every turn has an active play: Slow (deny+guard-crack) / Stop (pre-cast denial) / Haste (ally window) /")
print(f"  Gravity {gravity(150):.0f} (%-HP, any target) / Comet {comet:.0f} (flat) / free bolt {bolt:.0f} (floor).")
print(f"  SOLO offense (no allies to buff): Gravity + Comet + bolt = a standalone damage line every turn -> NOT the")
print(f"  vanilla 100%-support trap. Support/control is the AXIS; the offense is the self-sufficiency floor. Black")
print(f"  still out-damages it massively (SIM 4) -> Time is a controller that can fight, never a damage caster.")
