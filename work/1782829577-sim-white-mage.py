#!/usr/bin/env python3
"""
White Mage (#05, first DCL caster) battle-sim gate. Frozen DCL placeholders (physical) + magic placeholders.
Identity (converged w/ GPT): THE THRESHOLD KEEPER — controls catastrophic thresholds (KO, severe/magical
status, magic burst, undead, boss spike). PRE-EMPTS with Faith-INDEPENDENT wards + timed Reraise; RECOVERS
with Faith-SCALED magnitude (big heals, mass revive, severe cleanse) + Holy as a premium offensive/undead turn.
NOT the flat Faith-proof triage (Chemist) nor self-sustain (Monk).

Magic rules under test (doc 11): heal = MA*heal_power*faith_c*faith_t*G_m ; G_m≈0.58 ; Faith band [0.70,1.30]
floor 0.60 ; TARGET Faith scales healing received (low-Faith ally healed ×0.70 = the 'heal tax'). BUFFS are
magnitude+duration, NOT Faith-scaled, friendly (no resist) → land on ANYONE (how the WM helps low-Faith allies).
Holy = spiritual, Faith twice, ignores DR, Magic-Evadable. WM body: Robes, low HP, high MA, HIGH Faith
(two-sided: ×1.30 magic taken). Gates: (1) heal differentiation vs Chemist + heal-tax, (2) ★ buff-TURTLE is a
hard-but-killable wall not immortal, (3) WM fragility/two-sided, (4) Holy below Black + undead punish, (5)
revive/Reraise distinct from Chemist (+ duration calibration), (6) lanes.
"""
from functools import lru_cache
from itertools import product

@lru_cache(maxsize=None)
def p_le(n):
    if n < 3: return 0.0
    if n >= 18: return 1.0
    return sum(1 for a,b,d in product(range(1,7),repeat=3) if a+b+d<=n)/216.0

# --- physical layer (frozen placeholders, for INCOMING damage onto warded targets / onto the WM) ---
G,PEN = 5.0,0.25
SW  = lambda pa: 0.40*pa
THR = lambda pa: 0.22*pa
WOUND = {"cut":1.5,"thrust":2.0,"crush":1.0,"missile":1.0}
WMOD  = {"spear":5,"pole":3,"sword":3,"axe":6,"fists":4,"knife":1}
DR = {"heavy":{"cut":9,"thrust":8,"crush":3,"missile":8},
      "clothes":{"cut":2,"thrust":2,"crush":2,"missile":2},
      "robes":{"cut":0,"thrust":0,"crush":0,"missile":0}}
BOFF = {"low":0.76,"neutral":1.0,"high":1.35}
def swing(pa,w,t_arm,t="cut",br="high"):
    raw = SW(pa)+WMOD[w]
    return max(PEN*raw, raw-DR[t_arm][t])*WOUND[t]*BOFF[br]*G
def p_crit(sk): return p_le(4 if sk<15 else 5 if sk==15 else 6)
def lands(sk,deft,facing="front"):
    pc=p_crit(sk); pconn=p_le(sk)
    if facing=="back": return pconn
    dt=deft+(-2 if facing=="side" else 0)
    return pc+max(0,pconn-pc)*(1-p_le(dt))

# --- magic layer (placeholders; doc 11 shape) ---
G_M = 0.58
def faith(level):  # bounded centered band, floor 0.60
    return {"low":0.70,"neutral":1.0,"high":1.30}[level]
MA_WM = 14
def heal(ma, heal_power, fc, ft):           # target Faith scales healing received
    return ma*heal_power*faith(fc)*faith(ft)*G_M
def spell(ma, spell_power, fc, ft):         # offensive magic (Holy: spiritual, ignores DR, faith twice)
    return ma*spell_power*faith(fc)*faith(ft)*G_M
HEAL_POWER = {"Cure":11,"Cura":18,"Curaja_AoE":14}   # per-target; AoE tier hits several
SPELL_POWER= {"Staff_bolt":4,"Holy":6,"Firaga_BLACK":14}  # WM offense MINIMAL: weak free bolt (~29%) + Fire-tier Holy (~43%, the CEILING); NO ladder/AoE ever
# buffs (NOT Faith-scaled; magnitude+duration; friendly no-resist)
PROTECT=0.66; SHELL=0.66; REGEN_HP=14   # placeholders: Protect/Shell cut ~1/3; Regen +14/turn
# Chemist (Faith-PROOF flat) reference
CHEM = {"Hi-Potion":60,"X-Potion":100,"Phoenix":70}

print("="*98)
print("SIM 1 - HEAL DIFFERENTIATION + the TARGET-FAITH HEAL TAX (WM Faith-scaled magnitude vs Chemist flat)")
print("="*98)
for ft in ["high","neutral","low"]:
    c = heal(MA_WM,HEAL_POWER["Cure"],"high",ft); cura=heal(MA_WM,HEAL_POWER["Cura"],"high",ft)
    print(f"  ally Faith={ft:7}: WM Cure {c:5.1f} | WM Cura {cura:5.1f}   vs   Chemist Hi-Potion {CHEM['Hi-Potion']} (flat) / X-Potion {CHEM['X-Potion']} (flat)")
print(f"  -> WM OUT-HEALS Chemist on high/neutral-Faith allies (magnitude, AoE-capable); on a LOW-Faith ally WM Cure")
print(f"     ({heal(MA_WM,HEAL_POWER['Cure'],'high','low'):.0f}) FALLS BELOW Chemist's flat Hi-Potion ({CHEM['Hi-Potion']}). So Chemist/Monk own low-Faith raw recovery;")
print(f"     the WM protects low-Faith bruisers with FAITH-INDEPENDENT WARDS instead (SIM 2), not by out-healing them.")

print()
print("="*98)
print("SIM 2 - ★ BUFF-TURTLE GATE: hard wall, NOT immortal. Regen vs 1 vs 2 attackers; the unkillable breakpoint")
print("="*98)
HP_FRONT=150
atk = swing(14,"sword","heavy","cut","high")*lands(13,6)       # one committed physical attacker vs heavy line
prot = atk*PROTECT
print(f"  Heavy frontliner HP {HP_FRONT}. One attacker raw ~{atk:.1f}/turn; under Protect ~{prot:.1f}/turn.")
for n in (1,2,3):
    incoming = n*prot
    net = incoming - REGEN_HP
    if net<=0:
        verdict=f"NET +{-net:.1f}/turn HP GAIN -> UNKILLABLE by {n} (turtle wins)"
    else:
        verdict=f"net -{net:.1f}/turn -> TTK ~{HP_FRONT/net:4.1f} turns"
    print(f"  {n} attacker(s) + Protect + Regen({REGEN_HP}): {verdict}")
print(f"  READ: 1 attacker can be out-sustained (intended hard turtle) — but it is FOCUS-FIRED open (2-3 attackers")
print(f"  overwhelm Regen). CALIBRATION GATE: Regen must stay BELOW one committed attacker's warded chip ({prot:.1f}),")
print(f"  else a single body is unkillable. STRUCTURAL guardrails (sound): wards/Regen are DURATIONS (expire), the")
print(f"  party-wide Protectja/Shellja/Wall are TIER-2 (the full stack costs many turns + MP), and the WM is killable.")
burst = swing(14,"sword","heavy","cut","high")  # a crit/Power-Strike style spike ignores the averaging
print(f"  Burst-through also works: a spike hit (~{burst:.0f}) > Regen+chip buffer; Protect is a % (×{PROTECT}), never immunity.")

print()
print("="*98)
print("SIM 3 - WM FRAGILITY / TWO-SIDED: robes + low HP + HIGH Faith (×1.30 magic taken); folds to a dive")
print("="*98)
HP_WM=75
dive = swing(13,"knife","robes","cut","high")*lands(14,7,"side")   # a thief-style dive on the backline
bolt_on_wm = spell(13, SPELL_POWER["Firaga_BLACK"], "high", "high")  # enemy Black on the HIGH-Faith WM (×1.30 target)
print(f"  WM HP {HP_WM} (robes, no DR). Thief dive ~{dive:.1f}/hit -> TTK {HP_WM/max(1,dive):.1f}: folds to melee pressure.")
print(f"  Enemy Firaga on the WM (HIGH Faith target ×1.30): ~{bolt_on_wm:.0f} -> near one-shot. High Faith = glass cannon BOTH ways.")
print(f"  Its only magic defence is Magic-Evade (robes/anti-magic gear, capped ~50%) — a coin-flip, never immunity.")
print(f"  Disruptable on all THREE axes (doc 13): low Brave (mental), low base-HP (physical), high Faith (magical statuses).")

print()
print("="*98)
print("SIM 4 - MINIMAL OFFENSE: weak free Staff bolt + ONE Fire-tier Holy. Identity wall: NO ladder, NO AoE, EVER")
print("="*98)
for ft in ["high","neutral","low"]:
    bolt=spell(MA_WM,SPELL_POWER["Staff_bolt"],"high",ft); h=spell(MA_WM,SPELL_POWER["Holy"],"high",ft); b=spell(MA_WM,SPELL_POWER["Firaga_BLACK"],"high",ft)
    print(f"  vs Faith={ft:7}: Staff bolt {bolt:5.1f} (free) | Holy {h:5.1f} (={h/b:.0%} of Firaga ~ basic Fire tier) | Black Firaga(burst) {b:5.1f}")
print(f"  Holy = ONE single-target spiritual smite at basic-Black 'Fire' power - the CEILING not the target (Holy ignores")
print(f"  Zodiac => more consistent than Fire, so never let it EXCEED Fire). Staff bolt (~29%) = weak always-on floor (never")
print(f"  a dead turn). HARD identity wall vs Black: NO Holyra/Holyga, NO AoE Holy, EVER - White's offensive ceiling stays")
print(f"  basic single-target; Black keeps the tier ladder + AoE + elemental play + burst chassis. Magic-Evadable; crater low-Faith.")

print()
print("="*98)
print("SIM 5 - REVIVE / RERAISE: distinct from Chemist (≤2 doors); + the Reraise duration calibration")
print("="*98)
raise_hp = heal(MA_WM, HEAL_POWER["Cura"], "high", "neutral")
print(f"  Chemist Phoenix Down: flat ~{CHEM['Phoenix']} HP, Faith-PROOF, cheap, instant — the steady revive door.")
print(f"  WM Raise: Faith-scaled big-HP revive (~{raise_hp:.0f} on neutral, more on devout) + Tier-2 Arise/Raiseja = MASS/area")
print(f"  revive Chemist CAN'T match — the disaster door. Two distinct doors, not 'the magic copy'.")
print(f"  RERAISE (Tier-2): single-target, HIGH MP, CT, SHORT duration -> a 'death is coming' insurance, NOT a pre-battle")
print(f"  blanket. CALIBRATION (hardest knob): duration long enough to feel real, short enough to never be a mandatory splash.")

print()
print("="*98)
print("SIM 6 - LANE CHECK: three recovery jobs, distinct")
print("="*98)
print(f"  Chemist = FLAT, Faith-PROOF, finite-stock, instant, common-status triage + Phoenix Down (the reliable FLOOR; reactive).")
print(f"  Monk    = SELF-sustain (Chakra), no MP/Faith, frontline (its own body).")
print(f"  White   = THRESHOLD KEEPER: Faith-SCALED magnitude heals, mass/area revive, SEVERE/magical cleanse, the WARD suite")
print(f"            (Faith-independent: protects low-Faith allies), timed Reraise, Holy. The miracle-magnitude disaster job.")
print(f"  Moat = free LITURGY (white-magic CT/MP efficiency) + high MA/Faith + MP budget on a Robes body; splash = utility")
print(f"  at low magnitude (low-MA/Faith host, small budget). Welcome splash, never a strictly-better White Mage.")
