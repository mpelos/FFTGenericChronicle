#!/usr/bin/env python3
"""
Archer battle-simulation gate (retroactive) + the Bow-A-export portability abuse check.
Frozen DCL placeholders. Identity: zone-control marksman. Aim (charge -> +eff skill + ignore
range/height/cover), Concentration (ignore Dodge), crossbow anti-armor, Pinning, Countershot.
Innate Marksmanship (free; ignore range+height on basic shots; exported). Bow A + Crossbow B export.
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
WMOD={"bow":3,"crossbow":3,"knife":1,"fists":4}
DR={"heavy":{"missile":8},"clothes":{"missile":2},"robes":{"missile":0}}
BOFF={"low":0.76,"neutral":1.0,"high":1.35}
def bowdmg(pa,arm,br="neutral",wp="bow"):
    raw=SW(pa)+WMOD[wp]; return max(PEN*raw,raw-DR[arm]["missile"])*1.0*BOFF[br]*G
def p_crit(sk): return p_le(4 if sk<15 else 5 if sk==15 else 6)
def lands(sk,deft):
    pc=p_crit(sk); pconn=p_le(sk); return pc+max(0,pconn-pc)*(1-p_le(deft))

# grade->skill: A14 B13 C12 D10
ARCHER=dict(pa=11,bowsk=14,xbowsk=13,hp=100,br="neutral")

print("="*82)
print("SIM 1 — J1: zone control (Aim accuracy, Concentration vs evasive, crossbow vs armour)")
print("="*82)
ev_dodge=11  # evasive target
print(f"  vs EVASIVE target (Dodge {ev_dodge}):")
print(f"    plain bow:        land {lands(ARCHER['bowsk'],ev_dodge):.0%}")
print(f"    + Concentration (ignore Dodge): land {lands(ARCHER['bowsk'],0):.0%}  -> the evasive answer")
print(f"  vs HEAVY armour (crossbow, skill-primary anti-armor via divisor):")
print(f"    bow vs heavy:      {bowdmg(11,'heavy'):.0f}/hit (walled by missile DR8)")
print(f"    crossbow vs heavy: ~{bowdmg(11,'heavy','neutral','crossbow')*1.8:.0f}/hit (innate divisor halves DR — the anti-armor lane)")
print("  -> J1 holds: Aim/Concentration handle evasive + range/height/cover; crossbow handles armour;")
print("     Move4 + Vantage kite and hold high ground. A real fielded zone-controller.")

print()
print("="*82)
print("SIM 2 — Aim vs Rapid Shot tempo  &  Marksmanship-vs-Aim (is Aim still worth the CT?)")
print("="*82)
tgt=10
aim_dmg=bowdmg(11,"clothes")*lands(ARCHER['bowsk'],tgt)          # patient: full, accurate
rapid_each=bowdmg(11,"clothes")*0.7*lands(ARCHER['bowsk']-2,tgt) # 2 weak shots at -2 to hit
print(f"  Aim (patient): {aim_dmg:.1f} eff/turn (accurate, can ignore cover) | Rapid Shot (fast): {2*rapid_each:.1f} eff/turn (2 weak)")
print(f"  Marksmanship (free) already ignores range+height -> so is Aim still worth a CT charge?")
print(f"    YES: Aim adds EFFECTIVE SKILL (raises crit/connect) AND solves COVER (Marksmanship does not).")
print(f"    vs a covered/high-Dodge target Aim still earns its charge; Marksmanship only zeroes range/height.")

print()
print("="*82)
print("SIM 3 — SURVIVAL / wrong-pick: a kiter, folds in a cramped scrum")
print("="*82)
melee_on_archer=  (SW(13)+3-2)*1.5*1.35*G * lands(13,10)   # Knight reaches it
print(f"  If reached, a Knight does {melee_on_archer:.0f} eff/turn -> TTK {ARCHER['hp']/melee_on_archer:.1f} (low HP, no parry on 2H ranged).")
print(f"  At range (Move4 + Vantage + high ground) it dictates spacing and rarely gets reached. Wrong-pick =")
print(f"  cramped melee maps where the gap collapses. Honest two-sided.")

print()
print("="*82)
print("SIM 4 — ★ PORTABILITY ABUSE: Bow A exported (host + Aim + Bow Training) vs the Archer")
print("="*82)
print("  Bow damage scales with PA -> a higher-PA host out-damages the Archer PER SHOT. The question:")
print("  does that make the host a STRICTLY BETTER marksman, or just a raw-damage sidegrade?")
print(f"  {'unit (high-Brave bow A)':28} {'bow dmg/hit':>12} {'package':>40}")
rows=[
 ("Archer PA11 (native)",      bowdmg(11,'clothes','high'), "Aim+ArcVolley+Rapid+Pinning+Countershot+Concentration+free Marksmanship+Move4 kiter"),
 ("Monk PA14 +Aim+BowTraining", bowdmg(14,'clothes','high'), "single-target bow nuke only; 2 slots; NO Concentration/Volley/Pin/Countershot"),
 ("Ninja PA12 +Aim+BowTraining",bowdmg(12,'clothes','high'), "fast bow nuke; 2 slots; same missing package"),
 ("Knight PA13 +Aim+BowTraining",bowdmg(13,'clothes','high'),"durable bow; 2 slots; Move3, no kite, missing package"),
]
for n,d,pkg in rows:
    print(f"  {n:28} {d:>12.0f} {pkg:>40}")
mk=bowdmg(14,'clothes','high'); ar=bowdmg(11,'clothes','high')
print(f"  -> Monk out-damages the Archer's raw shot by ~{100*(mk-ar)/ar:.0f}% — BUT only single-target, no")
print(f"     Concentration (loses vs evasive), no Arc Volley/Pinning/Countershot, 2 slots spent, and the")
print(f"     Archer keeps free Marksmanship + Concentration + the whole Aim suite + the kiter chassis.")
print(f"     VERDICT: a raw-damage SIDEGRADE host, not a strictly-better marksman. Watch if the % balloons")
print(f"     at endgame PA spread (flag for the grade-budget reconciliation).")

print()
print("="*82)
print("SIM 5 — ENEMY-USE & cover guardrail")
print("="*82)
print("  Enemy Archer kites, pins (Immobilize), and returns fire (Countershot). Counterplay: close under")
print("  COVER (Marksmanship does NOT ignore cover — only Aim does, at a charge), Stalwart vs the pin,")
print("  flank/Vantage-deny, or magic. Cover staying relevant is the key guardrail (held).")
