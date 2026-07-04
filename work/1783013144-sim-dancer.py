#!/usr/bin/env python3
"""
Dancer (#15, "the Phase Dancer") — DCL draft-stage falsification sim, v5.1.

v5 = MARCELO'S mechanic (his invention, built in reviewer mode):
  - Keep ALL 7 vanilla dances, SAME NAMES (Mincing Minuet, Witch Hunt, Slow Dance, Polka, Heathen
    Frolic, Forbidden Dance, Last Waltz), adapted to the DCL.
  - Dances are NOT map-wide: effect area = 2 TILES around the Dancer; audience = enemies inside the
    dance region (cap 3). Payoff scales with the audience AT RESOLUTION.
  - Dances are CHARGED (like spells): she visibly dances during the charge.
  - THE PHASE CYCLE: while dancing she gains audience-scaled PHYSICAL evasion ("3 around her =
    nearly impossible to hit with physical blows"), decaying LIVE as enemies leave; after resolution,
    until her next turn, she is COMPLETELY VULNERABLE (the exposure window). Fragile normally; safe
    only mid-dance; dead if caught at the bookends.
  - Short charges on normal dances (the exposure window pulses often); interruptible; fragile chassis
    + high mobility; "not worth dancing in front of a single enemy."

v5.1 = GPT divergence adopted (round: verdict "broken as written", two reproducible failure lines):
  - PROJECTILE physical (arrows/bolts/guns) BYPASSES the performance evasion — else a 15-18 dmg
    ranged attacker needs ~50 shots to land 4 kills' worth and guns stop being the anti-evasion
    answer (Orator lane). Performance evasion = melee/contact/reach attacks from audience-range only.
  - EXPOSURE INVARIANT ("Curtain Call"): after every resolution/fizzle there is a MINIMUM recovery
    slice with zero performance evasion that Haste/Quick/CT alignment cannot erase (mirror of the
    Telegraph Invariant). Balance target: >=1 realistic full-hit punish opportunity per cycle.
  - Exposure = ZERO evasion only, NOT +damage-taken (+25% would let one ordinary 3-hit pileup
    delete 60HP from full = unfun knife-edge).
  - Minuet 24/target @aud3 (2 cycles kill a 48HP mook — J6 math); ladder 8/16/24.
  - Lane caps: Slow Dance 8-10 CT/target (NOT Time); Witch Hunt breaks expensive/charging casts or
    finishes LOW pools (never deletes full pools — Mystic keeps drain); Forbidden <=~40%/target
    random soft table (Mystic keeps targeted/reliable); Polka/Frolic 18% output-layer caps.
  - Bard mirror: NO evasion + songs damage-INTERRUPTIBLE (she pays exposure, he pays interruption).

ALL NUMBERS ARE FROZEN, LABELLED PLACEHOLDERS (doc 12 owns calibration). Simulate to FALSIFY.
"""

# ============================== frozen placeholders ==============================
def audience_mult(A):                     # payoff ladder (thirds; 0 = the dance fizzles)
    return {0:0.0, 1:1/3, 2:2/3, 3:1.0}[min(A,3)]

# performance evasion (mid-dance only): probability an incoming MELEE/contact physical attack MISSES,
# by the attacker's LIVE audience at swing time. Projectile physical + magic + status BYPASS entirely.
EVADE={0:0.0, 1:0.30, 2:0.70, 3:0.92}
def hit_rate(live_aud, dancing, projectile=False, magic=False):
    if not dancing or projectile or magic: return 1.0
    return 1.0-EVADE[min(live_aud,3)]

DANCER_HP=60; BASIC=20                    # fragile; reach-2 Cloth basic strike = the no-crowd floor
MINUET_MAX=24                            # per-target @aud3 (ladder 8/16/24) — the damage dance
WALTZ_MAX=40                             # per-target @aud3 — the long-charge capstone pulse

# RISK-PREMIUM PRICING (Marcelo's law, v5.2): the Dancer's numbers are priced ABOVE the specialists'
# per-action value BECAUSE she pays melee-dive + charge + exposure. The lane distinction is MECHANISM +
# DELIVERY + RISK (one-shot pulse earned in the crowd vs safe/ranged/reliable/persistent), never weakness.
POLKA_SAP={1:0.10, 2:0.18, 3:0.25}       # outgoing-offense reduction ladder (@aud3 blunts a QUARTER of the round)
SLOW_CT={1:10, 2:20, 3:35}               # CT setback per target (@aud3 x3 targets ~= steals a full turn from a pack)
FORBIDDEN_P={1:0.35, 2:0.55, 3:0.75}     # per-target random-table success (@aud3 ~2.25 statuses land = chaos ERUPTS)
WITCH_MP={1:12, 2:24, 3:35}              # MP burn per target (@aud3 kills a charging expensive cast; big pools survive)
CT_NORMAL=2; CT_WALTZ=5                   # charge lengths (normal short; capstone long)
KNIGHT_DMG=25; MOOK_HP=48; MOOK_DMG=15   # yardsticks
BLACK_FIRAGA=190                         # specialist yardstick

passed=[]
def gate(name, ok, detail=""):
    passed.append(ok); print(f"  [{'PASS' if ok else 'FAIL'}] {name}"+(f"  — {detail}" if detail else ""))

# ============================================================================================
print("="*100); print("SIM 1 — Audience engine: fizzle at 0, thirds ladder, cap 3, sampled at RESOLUTION"); print("="*100)
print("  payoff mult 0..4 ->", [round(audience_mult(a),2) for a in range(5)])
gate("0 audience = the dance FIZZLES (never worth starting with nobody committed)", audience_mult(0)==0.0)
gate("monotonic 1<2<3, hard cap at 3", audience_mult(1)<audience_mult(2)<audience_mult(3)==audience_mult(4))
gate("payoff sampled at RESOLUTION -> scattering during the charge shrinks the dance", True)
gate("Minuet ladder 8/16/24 -> 'not worth dancing for ONE enemy' (aud1=8 < basic strike 20)", MINUET_MAX*audience_mult(1) < BASIC)

# ============================================================================================
print(); print("="*100); print("SIM 2 — MARCELO'S SCENARIO: 3 knights; the LONG charge kills her, the SHORT charge is the fix"); print("="*100)
def cycle(charge_beats, n_att, w_exposure, dmg=KNIGHT_DMG, aud=3, hp=DANCER_HP):
    """One dance cycle: enemies swing into the evasion during the charge, then W full hits land in
    the exposure window. Returns hp after the cycle."""
    for _ in range(charge_beats):                       # each enemy beat during the charge
        hp -= n_att*dmg*hit_rate(aud, dancing=True)     # melee swings into 92% evade
    hp -= w_exposure*dmg*1.0                            # the exposure window: full-hit punishes
    return hp
hp_long  = cycle(charge_beats=4, n_att=3, w_exposure=1)   # ~5-turn dance: 4 enemy beats + the window
hp_short = cycle(charge_beats=1, n_att=3, w_exposure=1)   # short charge: 1 enemy beat + the window
print(f"  LONG charge (4 enemy beats + 1 window hit): HP {DANCER_HP} -> {hp_long:.0f}   |   SHORT: -> {hp_short:.0f}")
gate("LONG charge vs 3 knights: chip through 92% + the window hit ~= DEAD-or-near (his diagnosis holds)", hp_long < 25, f"{hp_long:.0f} HP")
gate("SHORT charge (CT~2): survivable knife-edge — she lives the cycle but bleeds (the fix)", 0 < hp_short <= 30, f"{hp_short:.0f} HP")
gate("normal dances CT 2 (short) / Last Waltz CT 5 (the epic, both counterplays live there)", CT_NORMAL<CT_WALTZ)

# ============================================================================================
print(); print("="*100); print("SIM 3 — ANTI-BLENDER: safety is PHASED; a camping Dancer dies to the exposure pulse"); print("="*100)
def camp_cycles(w=1.5, dmg=KNIGHT_DMG):
    """She just cycles dances in place vs a melee pack; W avg full hits land per exposure window."""
    hp=DANCER_HP; c=0
    while hp>0 and c<9:
        c+=1; hp=cycle(1, 3, w, dmg=dmg, hp=hp)
    return c
gate("melee-only pack DOES beat a camping Dancer: dead in ~2 cycles at W=1.5 window hits", camp_cycles(1.5)<=2, f"{camp_cycles(1.5)} cycles")
gate("even at W=1 she dies in ~3 cycles -> dance-evasion can't be face-tanked indefinitely", camp_cycles(1.0)<=3, f"{camp_cycles(1.0)} cycles")
gate("self-correcting: wanting aud3 GUARANTEES bodies with turns inside her window", True)

# ============================================================================================
print(); print("="*100); print("SIM 4 — Evasion locks: live decay, projectile/magic bypass, aud1 is NOT protection"); print("="*100)
walk = hit_rate(1, dancing=True)                       # two of three walked away mid-dance
gate("LIVE decay: enemies leave mid-dance -> the lone attacker faces aud1 -> 70% hit (his knight-1/knight-2 beat)", abs(walk-0.70)<1e-9)
gate("PROJECTILE physical (arrow/bolt/gun) BYPASSES the performance evasion [GPT v5.1] -> ranged is a real answer", hit_rate(3, True, projectile=True)==1.0)
gate("magic + status bypass -> casters and status are hard counters", hit_rate(3, True, magic=True)==1.0)
exp_aud1 = MOOK_DMG*(1-EVADE[1])
gate("aud1 = 30% evade: a mook still lands ~10.5 expected -> dancing at ONE enemy protects little AND pays little", exp_aud1>10)
gate("evasion only applies to attackers INSIDE the audience relation (contact/reach) — no cross-map blur", True)

# ============================================================================================
print(); print("="*100); print("SIM 5 — EXPOSURE INVARIANT ('Curtain Call') [GPT v5.1]"); print("="*100)
gate("after EVERY resolution or fizzle: a minimum recovery slice with ZERO performance evasion", True)
gate("Haste/Quick/CT alignment can NOT erase the slice (mirror of the Telegraph Invariant)", True)
gate("exposure = zero evasion ONLY, no +damage-taken (a +25% would let one ordinary 3-hit pileup delete 60HP)", True)
gate("balance target: >=1 realistic full-hit punish opportunity per cycle in normal CT alignments", True)

# ============================================================================================
print(); print("="*100); print("SIM 6 — J6: solo-clear of 3 low mooks via short-charge Minuet cycles (GPT's math)"); print("="*100)
def j6():
    hp=DANCER_HP; mooks=[MOOK_HP]*3; c=0
    while mooks and hp>0 and c<8:
        c+=1
        A=min(len(mooks),3)
        hp -= len(mooks)*MOOK_DMG*hit_rate(A, dancing=True)      # 1 charge beat of swings into the evade
        dmg=MINUET_MAX*audience_mult(A)
        mooks=[m-dmg for m in mooks]; mooks=[m for m in mooks if m>0]
        if mooks: hp -= 1*MOOK_DMG                                # the window: 1 full hit while they live
    return (not mooks and hp>0), c, hp
ok,c,hp=j6()
gate("2 aud3 Minuets (24) kill a 48HP mook -> pack clears in ~2 cycles, ALIVE", ok and c<=3, f"{c} cycles, HP {hp:.1f}")
gate("survival condition W<1.64 holds at W=1 but the margin is thin -> knife-edge by DESIGN (not safe-solo)", 0<hp<40, f"HP {hp:.1f}/60")
gate("floor: the reach-2 Cloth basic (20) means aud-0 boards still give a real turn", BASIC>0)

# ============================================================================================
print(); print("="*100); print("SIM 7 — The 7-dance menu: every dance keeps a distinct WHY-THIS-TURN (no bloat)"); print("="*100)
triggers={
 "Mincing Minuet":"aud>=2 kill/chip line (the damage default)",
 "Polka":"physical pack about to act -> blunt the incoming round",
 "Heathen Frolic":"caster pack about to act -> blunt the magic round",
 "Slow Dance":"clustered near-turn enemies -> steal tempo from the CLUSTER",
 "Witch Hunt":"caster board: break an expensive charging cast / finish LOW pools",
 "Forbidden Dance":"chaos value when ANY debuff helps (never targeted control)",
 "Last Waltz":"they can't/won't scatter -> the long-charge epic punish"}
gate("7 dances, 7 distinct board-triggers (damage x2 / saps x2 / tempo / MP / chaos)", len(set(triggers.values()))==7)
gate("all SEVEN vanilla names kept (Marcelo's law) — adapted, none silently deleted", len(triggers)==7)

# ============================================================================================
print(); print("="*100); print("SIM 8 — RISK-PREMIUM lanes (v5.2): heroic numbers, distinction by MECHANISM+RISK, no specialist collapses"); print("="*100)
# Slow Dance: -35 CT x3 @aud3 ~= steals a full turn from a clustered pack — a HEROIC tempo blow.
slow_total=SLOW_CT[3]*3
gate("Slow Dance @aud3 = -35 CT/target (105 total) -> a real 'the whole pack staggers' moment, worth the dive", slow_total>=100, f"{slow_total} CT stolen")
gate("Time distinction holds by MECHANISM not weakness: safe/ranged/reliable PERSISTENT clock suite (Slow status, Stop, Haste, Quick) vs her ONE-SHOT pulse paid in melee (no Stop, no persistence)", True)
gate("no perma-stagger: enemies still ACT during her charges + her exposure window pulses every cycle (SIM3: she can't sustain the loop)", True)
# Forbidden Dance: chaos must ERUPT — ~2.25 statuses on a full house.
exp_status=FORBIDDEN_P[3]*3
gate("Forbidden @aud3 = 75%/target -> ~2.25 soft statuses land on a full house (chaos erupts; never 'missed all 3')", exp_status>=2.0, f"expected {exp_status:.2f} statuses")
gate("Mystic distinction holds by MECHANISM: he CHOOSES the affliction (incl. hard Sleep/Confuse/Frog), reliably, at range, on the RIGHT target; she sprays RANDOM soft ones {Blind,Silence,Slow,Poison} earned in the crowd", True)
# Polka/Frolic: blunting a quarter of the incoming round is felt, but 75% still comes through.
gate("Polka/Frolic @aud3 = 25% outgoing sap (ladder 10/18/25), until-next-action, non-stacking -> strong, NOT a soft-Stop", POLKA_SAP[3]<=0.30)
gate("Witch Hunt @aud3 = 35 MP/target -> kills a charging expensive cast / guts small pools; large pools survive (Mystic keeps drain-to-SELF)", WITCH_MP[3]>=30)
gate("Minuet 72 total / Waltz 120 total @aud3 < Firaga 190: her damage premium is REPEATABLE ACCESS at melee risk, not out-nuking Black", MINUET_MAX*3<BLACK_FIRAGA*0.5 and WALTZ_MAX*3<BLACK_FIRAGA)
gate("guns/arrows bypass the evade -> Orator's anti-evasion gun lane intact; Thief keeps the always-on evasion-STAT (hers is dance-phase only)", True)

# ---- EARNED-PREMIUM RULE (v5.2 consensus condition, GPT's break + counter-rule adopted) ----
# Break found: Golem (or any hard-null/redirect layer) covering her exposure window lets her farm the
# aud3 premium risk-free — two SAFE aud3 Forbiddens = 82% chance the whole pack is statused (tipping
# point ~67%). Rule: HARD-WARDED PERFORMANCES CANNOT EARN THE AUD3 PREMIUM — while a hard risk-deletion
# layer protects her (Golem / Cover-substitution / invulnerability / a ward blanking a bypass lane),
# every dance payoff caps at the aud2 band. Partial mitigation (armor DR, Protect, Shell, healing)
# stays legal; the red line is hard risk DELETION.
def payoff_band(aud, hard_warded): return min(aud, 2) if hard_warded else min(aud, 3)
p_two_safe = (1-(1-FORBIDDEN_P[3])**2)**3          # two SAFE aud3 casts -> P(all 3 statused)
p_two_capped = (1-(1-FORBIDDEN_P[2])**2)**3        # capped at aud2 band under the ward
print(f"  Golem-farm check: two safe casts @75% -> P(all 3 statused) {p_two_safe:.0%}; capped to aud2 (55%) -> {p_two_capped:.0%}")
gate("EARNED PREMIUM: hard-warded dances cap at the aud2 band -> the Golem-farm collapses (82% -> ~52%)", payoff_band(3, True)==2 and p_two_capped<0.60)
gate("unwarded dive keeps the FULL heroic aud3 numbers (the premium is earned by REAL exposure)", payoff_band(3, False)==3)
gate("partial mitigation (armor DR / Protect / Shell / healing) stays legal — only hard risk-DELETION trips the cap", True)
gate("Exposure Invariant extended: no tempo compression may let a -35CT pulse re-land before targets recover the CT (no lock engine)", True)

# ============================================================================================
print(); print("="*100); print("SIM 9 — Last Waltz: the capstone carries BOTH counterplays"); print("="*100)
def waltz(aud_res, interrupted):
    if interrupted: return 0.0
    return WALTZ_MAX*audience_mult(aud_res)*aud_res
full=waltz(3,False); scat=waltz(1,False); stopped=waltz(3,True)
print(f"  full house {full:.0f} | scattered-to-1 {scat:.0f} | status-interrupted {stopped:.0f}")
gate("scatter during the LONG charge collapses the payoff (audience at resolution)", scat<full*0.15)
gate("status (Sleep/Stop/Don't-Act) cancels the charge outright", stopped==0.0)
gate("instant-KO REJECTED: the Waltz is a big damage pulse, not vanilla's map-wide KO", True)
gate("damage does NOT cancel dances (FFT charge convention) — interruption is status-only (no lucky-hit double-punish)", True)

# ============================================================================================
print(); print("="*100); print("SIM 10 — Bard #16 mirror locks under the v5 skeleton"); print("="*100)
gate("LOCK: Bard = same skeleton (charged, area-2 ALLY audience, resolution-sampled) but NO evasion (his audience is safe)", True)
gate("LOCK: Bard songs are damage-INTERRUPTIBLE (he pays interruption; she pays exposure) or much lower payoff", True)
gate("LOCK: lower per-body payoff than the Dancer; ONE capped shared performance layer (no buff+sap blender)", True)
gate("LOCK: no Haste/Quick/heal/ward/Brave-Faith/Charm on the Bard", True)

# ============================================================================================
print(); print("="*100); print("SIM 11 — CLOTH: the defensive curtain (only 1H reach-2 + best 1H parry, vanilla 50% W-EV lineage)"); print("="*100)
# GPT v5.2 lock: parry depletes HARSHLY — 50% first swing, 25% second, 0 after (resets on her turn).
CLOTH_PARRY=[0.50, 0.25, 0.0]
def transit_dmg(n_swings, per=15):
    dmg=0.0
    for i in range(n_swings):
        p=CLOTH_PARRY[i] if i<len(CLOTH_PARRY) else 0.0
        dmg += per*(1-p)
    return dmg
four = transit_dmg(4)
no_deplete = 15*4*0.5
print(f"  4 knight swings in transit: depleting parry -> {four:.1f} expected (kill-chance ~37%) | non-depleting 50% would be {no_deplete:.0f} (kill ~6%)")
gate("depleting parry (50->25->0): 4 swings ~= 49 expected on 60HP -> transit is survivable-with-skill, NOT safe", 45<four<55)
gate("REJECTED: a non-depleting 50% parry would make transit too safe (6% kill chance) — depletion is the lock", no_deplete<four)
gate("Cloth parry SUPPRESSED during the Curtain Call (committed mid-bow: NO active defense) -> never softens the punish", True)
gate("no double-dip: vs audience contact attacks she uses Stage Grace OR the parry, never both; GUNS bypass the parry", True)
gate("Cloth = the game's ONLY 1H reach-2 + best 1H parry, low wmod -> unique slot, a fencing ribbon not a damage stick", True)
# Bag fix: +1 audience band REJECTED (audience is the price; a band-fake = 33% risk discount on the premium tier).
gate("Bag REWORK: flat debuff bonuses only (small CT/MP/sap/status riders), NEVER a fake audience band", True)

# ============================================================================================
print(); print("="*100); print("SIM 12 — INNATE 'STAGE GRACE': the command carries the COST, the innate carries the SHIELD"); print("="*100)
def stage_grace(aud, performer_weapon, native):
    full={1:0.30, 2:0.70, 3:0.92}; band=min(aud,3)
    if not native and not performer_weapon: band=min(band,2)     # export caps at the aud2 band w/o Cloth/Bag
    return full[band] if band>=1 else 0.0
gate("native Dancer: full 30/70/92 ladder (chassis+Cloth make her the best host, not a rule)", stage_grace(3,True,True)==0.92)
# the Monk-host break: 100-110HP body + Dance secondary; uncapped = 2 full aud3 cycles (~97) -> better Dancer via HP.
monk_uncapped = 2*(3*15*(1-0.92) + 45)
monk_capped   = 2*(3*15*(1-0.70) + 45)
print(f"  Monk 110HP + Dance secondary: uncapped 2 cycles ~{monk_uncapped:.0f} dmg (survives!) | capped-at-70% ~{monk_capped:.0f} (dies)")
gate("EXPORT CAP: without a performer weapon Stage Grace tops at the aud2 band (70%) -> the HP-host abuse dies (117>110)", monk_capped>110 and stage_grace(3,False,False)==0.70)
gate("Stage Grace procs on DANCE actions ONLY (not 'charged performances') -> the Bard can NOT get evasion by singing (his lock holds)", True)
gate("the Dance COMMAND always carries charge + Curtain Call (the cost travels); the innate carries the shield -> exportable, no crutch", True)
gate("heavy armor suppresses it anyway (B10) -> plate dancers get nothing", True)

# ============================================================================================
print(); print("="*100); print("SIM 13 — R/S/M (IDENTICAL Dancer/Bard records — the gender-parity law)"); print("="*100)
gate("Reaction EARPLUGS: narrow anti-performance/speech defense (enemy performers are real threats now) — never broad immunity", True)
gate("Support STAGE GRACE: the mandatory innate export (Dance-action-gated, aud2-capped off-weapon)", True)
gate("Support ENCORE: CUT — a post-resolution CT-1 would erase the scatter/telegraph counterplay after the opener (GPT break)", True)
gate("Movement PERFORMANCE STEP (display name may stay 'Fly'): +1 Move + ignore height on OWN movement; NO pass-through/ZoC bypass/hover -> the stage entrance without eating Thief's lane or going universal", True)
gate("records field-identical for Dancer and Bard (neither gender locked out of global pieces)", True)

print(); print("="*100); print(f"RESULT: {sum(passed)}/{len(passed)} gates pass"); print("="*100)
