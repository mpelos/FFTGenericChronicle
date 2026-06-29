#!/usr/bin/env python3
"""
Chemist battle-simulation gate (retroactive). Frozen DCL placeholders.
Identity: Faith-PROOF item/alchemy toolbox. Field Lab (now innate-free + exportable Support, satchel
cost = no shield). J1 = the corner where magic is blanked (Silence/anti-magic/low-Faith) + armoured;
no-strictly-better vs Black; wrong-pick = dive / raw DPS race / volume-heal race.
"""
from functools import lru_cache
from itertools import product

@lru_cache(maxsize=None)
def p_le(n):
    if n<3: return 0.0
    if n>=18: return 1.0
    return sum(1 for a,b,d in product(range(1,7),repeat=3) if a+b+d<=n)/216.0

# placeholders (frozen): magic & alchemy magnitudes on the FFT HP scale
M_BLACK_BASE = 45.0     # Black spell at Faith 1.0 (ignores physical DR; needs MP, castable)
FAITH_FLOOR  = 0.60     # magic floor at low Faith
ALCHEMY      = 30.0     # Chemist thrown flask: FLAT, Faith-proof, DR-bypass, finite (below Black ceiling)

def black(faith, silenced=False, antimagic=False):
    if silenced or antimagic: return 0.0
    return M_BLACK_BASE * max(FAITH_FLOOR, faith)

print("="*80)
print("SIM 1 — J1 PROOF: the Faith-proof corner (Black vs Chemist alchemy)")
print("="*80)
print(f"  {'situation':38} {'Black':>8} {'Chemist alchemy':>16} {'winner':>10}")
cases=[
 ("soft target, high Faith (1.0)",      black(1.0)),
 ("soft target, LOW Faith (0.60 floor)",black(0.60)),
 ("under Silence / anti-magic",         black(0.0, silenced=True)),
 ("vs HEAVY armour (both bypass DR)",   black(1.0)),
]
for label,b in cases:
    win = "Black" if b>ALCHEMY else ("Chemist" if ALCHEMY>b else "tie")
    print(f"  {label:38} {b:>8.0f} {ALCHEMY:>16.0f} {win:>10}")
print("  -> J1 holds: Chemist OWNS the corner (low-Faith / Silence / anti-magic). Black wins raw on the")
print("     faithful. No-strictly-better: alchemy (30) sits between Black's ceiling (45) and floor (27).")

print()
print("="*80)
print("SIM 2 — NO-STRICTLY-BETTER vs Black (the dominance check)")
print("="*80)
print(f"  Black ceiling (Faith 1.0): {black(1.0):.0f}  >  alchemy {ALCHEMY:.0f}  -> Black out-nukes soft/faithful targets.")
print(f"  alchemy is FINITE (reagent stock), no Faith/MA scaling, no crit, thrown-LoS limited.")
print(f"  => neither dominates: Black = ceiling on soft targets; Chemist = reliability + the blanked corner.")

print()
print("="*80)
print("SIM 3 — SURVIVAL + Auto-Potion (guardrailed)")
print("="*80)
# Chemist: HP95, clothes DR2, Dodge10 (low Brave backline), NO shield.
G,PEN=5.0,0.25
SW=lambda pa:0.40*pa
def melee(pa,wmod,dr,wound,br=1.0):
    raw=SW(pa)+wmod; return max(PEN*raw,raw-dr)*wound*br*G
kn=melee(13,3,2,1.5,1.35)  # Knight sword vs clothes, high Brave
def crit(sk): return p_le(4)
def land(sk,deft):
    pc=crit(sk); pconn=p_le(sk); return pc+max(0,pconn-pc)*(1-p_le(deft))
eff=kn*land(13,10)
hp=95; auto=15  # Auto-Potion heals ~Potion (15), once/cycle, survive-only
print(f"  Knight hits Chemist {eff:.1f} eff dmg/turn -> raw TTK {hp/eff:.1f}; with Auto-Potion (~{auto}/cycle) the")
print(f"  effective intake drops to ~{eff-auto:.1f}/turn -> TTK ~{hp/max(1,eff-auto):.1f}. Guardrails: survive-only,")
print(f"  once/cycle, Potion-line only -> sustains chip, does NOT make it unkillable (focus/burst still drops it).")
print(f"  No shield + light + Move3 => dive pressure folds it (wrong-pick).")

print()
print("="*80)
print("SIM 4 — PORTABILITY: Field Lab now exportable (Knight + Items + Field Lab) — strictly better?")
print("="*80)
print(f"  alchemy magnitude is identical wherever thrown ({ALCHEMY:.0f}, flat). BUT an off-job alchemist pays:")
print(f"   - secondary slot on Items + support slot on Field Lab (2 slots, no other supports),")
print(f"   - the satchel cost: NO shield/off-hand,")
print(f"   - its own chassis (a Knight gains a Faith-proof ranged option but it's modest/finite).")
print(f"  The Chemist gets Field Lab FREE (slots open) + Faith-proof backline chassis + the full cure spine")
print(f"  + Throw Items export. => a costly, welcome SPLASH, not a strictly-better home. Portability OK.")

print()
print("="*80)
print("SIM 5 — ENEMY-USE")
print("="*80)
print("  Enemy Chemist: Auto-Potions chip, revives with Phoenix Down, throws Faith-proof flasks (Silence")
print("  won't stop it). Counterplay: BURST through Auto-Potion (it's once/cycle, Potion-line), focus the")
print("  revive target down again, or DIVE the shieldless light body. Legible, not oppressive.")
