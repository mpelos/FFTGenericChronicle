#!/usr/bin/env python3
"""
Monk battle-simulation gate (retroactive) + the FULL-Martial-Arts-export portability abuse check.
Frozen DCL placeholders. Identity: self-sufficient melee diver. Highest HP, NO DR, high Brave
(taunt-vuln, -active-def), no shield/Block, reach-1 CRUSH fists, range-capped sustain (Chakra/Revive).
RETROFIT under test: Martial Arts exports FULL via a Support (unarmed weapon + scaling) — BUT unarmed
damage scales with the holder's MONK-job-level (the moat: low off-job unless they grind Monk), and the
support forces the unarmed/no-shield combat mode (GPT guardrail). Pummel exports tightly.
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
WMOD={"knife":1,"sword":3,"ksword":7,"flail":5,"ninjablade":2,"bow":3}
DR={"heavy":{"cut":9,"thrust":8,"crush":3,"missile":8},"clothes":{"cut":2,"thrust":2,"crush":2,"missile":2},"robes":{"cut":0,"thrust":0,"crush":0,"missile":0}}
BOFF={"low":0.76,"neutral":1.0,"high":1.35}

# --- Martial Arts: unarmed wmod scales with the holder's MONK job-level (0..8). The MOAT. ---
# Native Monk mains the job -> full level 8 -> fist wmod ~4 (the frozen placeholder). Off-job that
# splashed the support but barely leveled Monk gets a proportionally weak fist.
def ma_wmod(monk_jl):           # 8->4, 4->2, 2->1, 0->0 (untrained: support removes the penalty floor)
    return round(4*monk_jl/8)
def fistdmg(pa,arm,monk_jl,br="neutral"):
    raw=SW(pa)+ma_wmod(monk_jl)
    return max(PEN*raw,raw-DR[arm]["crush"])*WOUND["crush"]*BOFF[br]*G   # crush
def wpndmg(pa,w,t,arm,br="neutral"):
    raw=SW(pa)+WMOD[w]
    return max(PEN*raw,raw-DR[arm][t])*WOUND[t]*BOFF[br]*G

def p_crit(sk): return p_le(4 if sk<15 else 5 if sk==15 else 6)
def lands(sk,deft,facing="front"):
    pc=p_crit(sk); pconn=p_le(sk)
    if facing=="back": return pconn
    dt=deft+(-2 if facing=="side" else 0)
    return pc+max(0,pconn-pc)*(1-p_le(dt))

MONK=dict(pa=14,hp=200,jl=8,sk=13)   # highest HP, high PA, full Monk level

print("="*84)
print("SIM 1 — J1: self-sufficient diver (crush anti-armour, in-scrum sustain, long fight)")
print("="*84)
print(f"  Monk fist (PA14, full Monk lvl) vs HEAVY (plate soft to crush, DR3):")
print(f"    {fistdmg(14,'heavy',8,'high'):.0f}/hit  vs a Sword into the same plate: {wpndmg(13,'sword','cut','heavy','high'):.0f}/hit (walled by DR9)")
print(f"  -> crush is the melee anti-armour lane (distinct from Knight shred / crossbow / alchemy).")
print(f"  Chakra (self+adjacent, MP-free) sustains the dive; Revive (adjacent) is clutch. No healer needed.")
print(f"  -> J1 holds: dives, outlasts a physical brawl, cracks armour, needs no backline.")

print()
print("="*84)
print("SIM 2 — anti-omnipotence levers actually BITE (no DR / Brave / reach-1 / range-capped)")
print("="*84)
HP=MONK['hp']
for atk,lbl in [
    (38.0, "Black spell (ignores physical DR) — no DR means it lands FULL"),
    (wpndmg(11,"bow","missile","clothes","high")*lands(14,8), "Archer kite (reach-1 Monk can't answer at range)"),
    (wpndmg(14,"fists" if False else "sword","cut","clothes","high")*lands(13,8), "Mirror bruiser melee (poor Dodge, no Block)"),
]:
    print(f"  {lbl:56} {atk:5.1f} eff/turn -> TTK {HP/max(1,atk):4.1f}")
print(f"  Highest HP ({HP}) is the ONLY buffer: tanky vs sustained melee chip, but magic-burst/kite/taunt")
print(f"  all bypass it. High Brave = taunt-vulnerable + -active-def. Honest negative space, not omnipotent.")

print()
print("="*84)
print("SIM 3 — ★ PORTABILITY: FULL Martial Arts export — does the Monk-job-level scaling moat hold?")
print("="*84)
print("  Rule under test: Martial Arts exports the unarmed weapon + its scaling, BUT scaling reads the")
print("  HOLDER's Monk-job-level. Off-job hosts that didn't grind Monk punch with a WEAK fist. Plus the")
print("  no-shield clause (GPT): the support forces unarmed mode -> the host loses its own off-hand/shield.")
print(f"  {'unit':40} {'fist/hit':>9} {'notes':>30}")
rows=[
 ("Monk PA14 full-lvl (native, FREE slot)",        fistdmg(14,'clothes',8,'high'), "+highest HP +Chakra/Revive native"),
 ("Knight PA13 +MA +MA-Support, Monk lvl2",         fistdmg(13,'clothes',2,'high'), "2 slots; NO shield/wall; weak fist"),
 ("Knight PA13 +MA +MA-Support, GRINDED Monk lvl8", fistdmg(13,'clothes',8,'high'), "2 slots+grind; STILL no shield/Hold Ground; loses the wall"),
 ("Ninja PA12 +MA +MA-Support, Monk lvl8",          fistdmg(12,'clothes',8,'high'), "fast fists; lower HP, no Chakra unless 3rd slot"),
]
for n,d,note in rows:
    print(f"  {n:40} {d:>9.0f} {note:>30}")
nat=fistdmg(14,'clothes',8,'high'); kn=fistdmg(13,'clothes',8,'high')
print(f"  -> Even a fully-grinded Knight fist ({kn:.0f}) trails the native Monk ({nat:.0f}, higher PA) AND must give")
print(f"     up its shield/Hold-Ground wall to use it (no-shield clause) + spend 2 slots + grind Monk to 8.")
print(f"     A low-investment splash (lvl2) punches for {fistdmg(13,'clothes',2,'high'):.0f} — a noodle. Moat holds: welcome")
print(f"     splash (Chakra/Revive donor is the real prize), never a strictly-better Monk.")

print()
print("="*84)
print("SIM 4 — high-PA host fist vs its OWN native weapon (does MA obsolete the host's kit?)")
print("="*84)
kn_fist=fistdmg(13,'clothes',8,'high'); kn_sword=wpndmg(13,'sword','cut','clothes','high')
print(f"  Knight PA13 grinded-Monk fist: {kn_fist:.0f}/hit (crush) vs its own Sword: {kn_sword:.0f}/hit (cut x1.5)")
print(f"  -> The host's own swung weapon (cut x1.5) out-damages borrowed fists into soft targets; fists only")
print(f"     win vs PLATE (crush). So MA-support is a situational anti-armour option, not a free upgrade.")

print()
print("="*84)
print("SIM 5 — PUMMEL (exported) vs high-guard target: cracks guard, but obsoletes Knight Guard Break?")
print("="*84)
# Pummel = multi-hit; each hit rolls vs guard but chips it. Knight Guard Break = full Block+Parry strip in 1 action.
guard=12
pummel_hits=3
print(f"  Pummel ({pummel_hits} fist hits) vs Block{guard}: each hit lands {lands(13,guard):.0%}; it CHIPS guard via volume")
print(f"  but does not STRIP it — vs a fresh wall most hits are turned. Knight Guard Break strips Block+Parry to")
print(f"  the Dodge floor in ONE action for the whole team. Different tools: Pummel = personal anti-armour burst,")
print(f"  Guard Break = team opener. No obsolescence (Pummel can't open a target for allies the way GB does).")

print()
print("="*84)
print("SIM 6 — ENEMY-USE")
print("="*84)
print("  Enemy Monk dives your backline, self-heals (Chakra), and crushes your plate. Counterplay: KITE it")
print("  (reach-1, no ranged answer), MAGIC-burst it (no DR), TAUNT it (high Brave), or charm/confuse/fear")
print("  (high Brave + can't ignore the flags). An enemy armored-martial splash loses its shield to punch ->")
print("  answer it like any bruiser. All the player's levers work.")
