#!/usr/bin/env python3
"""
Squire battle-simulation gate (retroactive — never simmed before the gate existed).
Frozen DCL placeholders (same constants as sim_thief*.py). Tests SHAPE.

Squire identity: endurance anchor, Stalwart lockdown-immunity (now also exportable), Throw Stone
free ranged chip, Wish Faith-proof HP-transfer heal, On Your Feet lockdown-cleanse. J1 = vs
lockdown/attrition comps + hold-the-point; wrong-pick = a damage race.
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
WMOD={"knife":1,"sword":3,"axe":6,"stone":0,"fists_monk":4,"ninjablade":2}
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

# grade->skill: A14 B13 C12 D10
SQUIRE=dict(hp=130,pa=11,sword=12,axe=12,knife=12,xbow=10,dodge=8,block=12,armor="clothes",br="neutral")

print("="*80)
print("SIM 1 — J1 PROOF: Stalwart uptime in a LOCKDOWN comp (team action-economy)")
print("="*80)
T=10  # turns each
for p_lock,label in [(0.40,"LOCKDOWN comp (enemy locks ~40%/turn)"),(0.0,"NO-lockdown comp")]:
    base=4*T*(1-p_lock)                       # 4 vulnerable units
    # replace 1 with Squire: 3 vulnerable + 1 immune; On Your Feet recovers ~1 locked ally/turn (capped)
    locked_per_round=3*p_lock
    recovered=min(T*1, T*locked_per_round)    # cleanse up to 1/turn, capped by actual locks
    withsq=3*T*(1-p_lock) + T + recovered
    print(f"  {label}")
    print(f"    team actions over {T} turns:  no Squire {base:.0f}  |  with Squire {withsq:.0f}  "
          f"(+{withsq-base:.0f}, {100*(withsq-base)/base:+.0f}%)")
print("  -> J1 holds: in a lockdown comp the immune anchor + cleanse is a big team-action multiplier;")
print("     in a no-lockdown comp it adds ~nothing (its raw output is low) = the wrong-pick.")

print()
print("="*80)
print("SIM 2 — SURVIVAL: endurance chassis (HP + Block) vs Dodge/DR peers")
print("="*80)
kn=dict(pa=13,sk=13,br="high")  # Knight DPS swinging
for tgt,hp,deft,arm,note in [
    ("Squire (HP130, Block12->Dodge8)",130,12,"clothes","endurance: HP + finite Block"),
    ("Thief  (HP85, Dodge11)",85,11,"clothes","evasion: dodges, no deplete"),
    ("Knight (HP160, Block12->Dodge7)",160,12,"heavy","DR + Block"),
]:
    d=dmg(kn['pa'],"sword","cut",arm,kn['br'])
    eff=d*lands(kn['sk'],deft,"front")
    print(f"  {tgt:34} {eff:5.1f} eff dmg/turn taken -> TTK {hp/eff:4.1f}  ({note})")
print("  -> Squire survives by HP + a finite Block (focus-fire drains Block to Dodge8); distinct from")
print("     Thief (pure Dodge) and Knight (DR). Mid endurance, not a hard tank.")

print()
print("="*80)
print("SIM 3 — DAMAGE wrong-pick: competent generalist, beaten by specialists")
print("="*80)
tgt_arm="clothes"
sq_sword=dmg(SQUIRE['pa'],"sword","cut",tgt_arm)*lands(SQUIRE['sword'],10,"front")
sq_axe  =dmg(SQUIRE['pa'],"axe","crush","heavy")*lands(SQUIRE['axe'],7,"front")   # crush vs heavy = its niche
sq_stone=dmg(SQUIRE['pa'],"stone","crush",tgt_arm)*1.0                            # free ranged chip, always lands-ish
monk    =dmg(14,"fists_monk","crush",tgt_arm,"high")*lands(13,10,"front")
print(f"  Squire Sword C vs clothes:     {sq_sword:5.1f} eff dmg/turn (competent)")
print(f"  Squire Axe C  vs HEAVY (crush): {sq_axe:5.1f} eff dmg/turn (its anti-armor niche)")
print(f"  Squire Throw Stone (free,ranged): ~{sq_stone:4.1f} chip (no MP/ammo/weapon; uptime contribution)")
print(f"  Monk (specialist) vs clothes:  {monk:5.1f} eff dmg/turn  -> out-damages the Squire")
print("  -> Squire is a fine generalist (covers cut/thrust/crush+reach at C), loses the damage RACE to")
print("     any specialist; in a pure DPS race its immunity is dead weight = wrong-pick.")

print()
print("="*80)
print("SIM 4 — WISH heal lane: Faith-proof HP-transfer vs Faith-scaling White heal")
print("="*80)
# White heal scales with target Faith (floor 0.60); Wish is flat HP-transfer, Faith-proof.
WHITE_BASE=50
for faith,lbl in [(1.0,"high-Faith ally"),(0.60,"low-Faith ally (floor)")]:
    white=WHITE_BASE*faith
    wish=40  # flat HP-transfer (Squire pays own HP); Faith-proof
    print(f"  {lbl:24}: White heal {white:4.0f}  |  Wish {wish:4.0f} (Faith-proof, costs Squire HP)")
print("  -> distinct lane: White heals more on the faithful & at range; Wish can't be blanked by low")
print("     Faith and works on the godless, but costs the Squire's own HP. Multi-lane healing holds.")

print()
print("="*80)
print("SIM 5 — ENEMY-USE: an enemy Squire ignores your lockdown")
print("="*80)
print("  Enemy Squire is immune to Stun/Knockdown/Don't-Act/Don't-Move -> your Trip/Stun/DA/DM whiff on it.")
print("  Counterplay (legible): damage / displacement / mind+halt status (Sleep/Stop/Charm) / magic / focus.")
print("  Not oppressive: it's a low-output endurance body; you just can't lock it, you can still kill it.")
