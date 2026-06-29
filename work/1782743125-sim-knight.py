#!/usr/bin/env python3
"""
Knight battle-simulation gate (retroactive). Frozen DCL placeholders.
Identity: Battle-Master soft-tank. Guard Break (shred Block+Parry, never Dodge) = flagship opener.
Weapon Master now innate-free + exportable Support (master maneuvers off-job at a 2-slot cost; basic
maneuvers without). Heavy Armor stays job-gated (NOT exportable, B10). Knight Sword A stays exclusive.
J1 = vs high-guard comps (turtles/shields/parry) + bodyblock a glass backline; wrong = evasive/burst/kite.
"""
from functools import lru_cache
from itertools import product

@lru_cache(maxsize=None)
def p_le(n):
    if n<3: return 0.0
    if n>=18: return 1.0
    return sum(1 for a,b,d in product(range(1,7),repeat=3) if a+b+d<=n)/216.0

G,PEN=5.0,0.25
SW=lambda pa:0.40*pa; THR=lambda pa:0.22*pa
WOUND={"cut":1.5,"thrust":2.0,"crush":1.0,"missile":1.0}
WMOD={"knife":1,"sword":3,"ksword":7,"flail":5,"fists":4,"ninjablade":2}
DR={"heavy":{"cut":9,"thrust":8,"crush":3,"missile":8},"clothes":{"cut":2,"thrust":2,"crush":2,"missile":2},"robes":{"cut":0,"thrust":0,"crush":0,"missile":0}}
BOFF={"low":0.76,"neutral":1.0,"high":1.35}
def dmg(pa,w,t,arm,br="neutral"):
    raw=(SW(pa) if t in("cut","crush") else THR(pa))+WMOD[w]
    return max(PEN*raw,raw-DR[arm][t])*WOUND[t]*BOFF[br]*G
def p_crit(sk): return p_le(4 if sk<15 else 5 if sk==15 else 6)
def lands(sk,deft,facing="front"):
    pc=p_crit(sk); pconn=p_le(sk)
    if facing=="back": return pconn
    dt=deft+(-2 if facing=="side" else 0)
    return pc+max(0,pconn-pc)*(1-p_le(dt))

print("="*80)
print("SIM 1 — GUARD BREAK (flagship): open a high-guard target for the team")
print("="*80)
# High-guard target: clothes + shield, strong block/parry. Guard Break shreds Block+Parry -> Dodge floor.
guard_before=12   # best active defense = Block 12 (fresh)
dodge_floor=7
ally=dmg(13,"sword","cut","clothes","neutral")   # a sword ally's hit (clothes target so DR isn't the confound)
print(f"  vs a high-guard target (Block12 -> Dodge7 after Guard Break), a Sword ally:")
print(f"    BEFORE: {ally*lands(13,guard_before):5.1f} eff dmg/turn (walled by the guard)")
print(f"    AFTER : {ally*lands(13,dodge_floor):5.1f} eff dmg/turn  ({lands(13,dodge_floor)/lands(13,guard_before):.1f}x)")
print(f"  -> Guard Break converts the WHOLE team's chip vs turtles/shields/parry-duelists. Never touches")
print(f"     Dodge (evasive targets stay an Archer/flank problem) -> no overlap, honest negative space.")
print(f"  vs a HEAVY turtle, pair Guard Break with crush/thrust (DR still walls cut): Flail crush vs heavy")
print(f"    = {dmg(13,'flail','crush','heavy'):.0f}/hit (plate is soft to crush, DR3) — the Knight's own anti-armor tool.")

print()
print("="*80)
print("SIM 2 — SOFT-TANK survival: tanky vs the right tool, cracked by the counters")
print("="*80)
HP=160
for atk,lbl in [
    (dmg(13,"sword","cut","heavy","high")*lands(13,12), "Sword (cut) front vs fresh Block"),
    (dmg(14,"fists","crush","heavy","high")*lands(13,12), "Monk crush front (plate soft to crush)"),
    (dmg(13,"sword","cut","heavy","high")*lands(13,7,"side"), "Sword from the FLANK (block bypassed-ish)"),
    (38.0, "Black spell (ignores physical DR)"),
]:
    print(f"  {lbl:42} {atk:5.1f} eff/turn -> TTK {HP/max(1,atk):4.1f}")
print("  -> NOT an unkillable anvil: cut-front is walled (very tanky), but crush, flank, magic, and")
print("     focus-fire (drains Block) all crack it. Soft-tank as intended.")

print()
print("="*80)
print("SIM 3 — PORTABILITY: Weapon Master now exportable (Ninja + Arts of War + Weapon Master)")
print("="*80)
print("  Off-job master maneuvers cost TWO slots (Arts of War secondary + Weapon Master support).")
print("  A Ninja Guard-Breaker is strong (fast opener) BUT: pays 2 slots, and Heavy Armor is NOT")
print("  exportable (B10) -> it stays clothes-fragile, no shield-wall, no Hold Ground/Bulwark line-hold.")
print("  The Knight gets Weapon Master FREE on the durable Heavy-Armor chassis (the moat). Maneuvers are")
print("  control/openers, not raw damage -> a welcome splash, not a strictly-better home. Portability OK.")

print()
print("="*80)
print("SIM 4 — BRAVE FORK (two-sided build choice)")
print("="*80)
sword=dmg(13,"sword","cut","clothes")
print(f"  LOW-Brave wall:  off x0.76 ({sword*0.76:.0f}/hit) but active-def +3 (harder to hit), taunt-RESIST -> the anvil.")
print(f"  HIGH-Brave line-breaker: off x1.35 ({sword*1.35:.0f}/hit, Doublehand) but active-def -2, taunt-VULNERABLE.")
print(f"  -> a real two-sided choice (B9): same job, opposite builds, neither universally best.")

print()
print("="*80)
print("SIM 5 — ENEMY-USE")
print("="*80)
print("  Enemy Knight Guard-Breaks your turtle, Taunts your aggressive units out of position, and holds a")
print("  chokepoint (Hold Ground/Bulwark) you can't shove it off. Counterplay: evasive units (Guard Break")
print("  can't touch Dodge), crush/magic vs its Heavy Armor, flank/focus to drain its Block, kite it (Move3).")
