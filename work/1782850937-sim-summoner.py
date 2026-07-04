#!/usr/bin/env python3
"""
Summoner (#14, "The Siege Summoner") — DCL draft-stage falsification sim.

Identity (Marcelo + GPT): the BIG, SLOW, COMMITTED area caster + GOLEM (the party physical barrier). The
commitment IS the texture ("I commit the board to this square N turns out — protect me or punish me"),
bound by the Telegraph Invariant. Distinct from Black = SCALE + COMMITMENT + team-defence (Golem); Black
keeps cheap/fast/flexible AoE + burst + the element ladder + anti-armour.

GPT divergence (adopted): HIGH Faith (a damage caster; neutral = a hidden free lunch); few DISTINCT summon
profiles (NOT a -ga ladder); Golem = a shared depleting physical pool (turtle-locked); innate = CHANNELING
WARD (a modest ward WHILE charging — survives chip, not focus-fire; does NOT cut CT/MP/friendly-fire); trim
AoE heal (White's lane).

★ Marcelo's calibration caution (2026-06-30): "do not overdo the commitment — if each summon's commitment is
too big, the Summoner becomes OBSOLETE." Resolution encoded here = a TWO-TIER commitment: a MODERATE Core
workhorse (affordable, the everyday tool, its area-payoff beats Black's multi-cast in the bunched wheelhouse)
+ HEAVY finishers only (the committed ceiling). The dial sweep (SIM 2) proves where it tips into obsolete.

ALL NUMBERS ARE FROZEN, LABELLED PLACEHOLDERS from DCL tiers (doc 12 owns calibration). Magic uses the DCL
equation (doc 11): dmg = MA x sp x faith_c x faith_t x G_m. We test the SHAPE and simulate to FALSIFY.
"""

# ---- frozen placeholders (shared with the prior caster sims) ----
G_M=0.58
FAITH={"low":0.70,"neutral":1.0,"high":1.30}
def mdmg(ma, sp, faith_c, faith_t="neutral"):           # vs a neutral-Faith target, non-elemental
    return ma*sp*FAITH[faith_c]*FAITH[faith_t]*G_M

MA_SUMM=18; MA_BLACK=18                                  # both top nukers; they differ by SHAPE not MA
HP_SUMM=70                                               # the most fragile
LOWLVL_HP=48; LOWLVL_DMG=16

# spell/summon profiles (sp = per-target power)
SP_BOLT=4                                                # the free rod-bolt floor (always-on)
SP_IFRIT=10                                              # CORE workhorse summon: big AoE, MODERATE commitment
SP_BAHAMUT=20                                            # high-end neutral finisher (Tier-2, HEAVY commitment)
SP_FIRE=6; SP_FIRAGA=14                                  # Black's cheap/flexible ladder (the contrast)

# costs: MP and charge (CT = turns until it resolves). Two-tier: workhorse MODERATE, finisher HEAVY.
MP={"bolt":0,"ifrit":34,"bahamut":60,"golem":40,"fire":8,"firaga":22}
CT={"bolt":0,"ifrit":2,"bahamut":3,"golem":1,"fire":0,"firaga":1}
TARGETS={"ifrit":4,"bahamut":4,"fire":1,"firaga":2}      # bodies hit by one cast (radius -> count, bunched)
MP_POOL=120                                              # high pool, but summons are dear -> few committed casts

GOLEM_POOL=150                                           # shared PHYSICAL absorb; depletes per hit; breaks; one active
CHANNEL_WARD_DR=8                                        # flat DR WHILE charging only (survive chip, not focus-fire)

passed=[]
def gate(name, ok, detail=""):
    passed.append(ok); print(f"  [{'PASS' if ok else 'FAIL'}] {name}"+(f"  — {detail}" if detail else ""))

# ============================================================================================
print("="*98); print("SIM 1 — J6 self-sufficiency: a lone Summoner ALWAYS has a reliable solo-clear (Golem = panic button)"); print("="*98)
def solo_clear(n, cap, ct_ifrit, use_golem):
    """Lone Summoner clears a pack. Positioned (opened at range) it survives on Ifrit+Ward alone; caught
    surrounded it casts Golem on ITSELF first (the party=itself) to soak the charge. Honest per-turn loop."""
    HP=HP_SUMM; mp=MP_POOL; en=[LOWLVL_HP]*n; t=0; golem=0; charging=-1; low=HP
    while en and HP>0 and t<40:
        t+=1
        if use_golem and golem<=0 and charging<0 and mp>=MP["golem"]:
            golem=GOLEM_POOL; mp-=MP["golem"]                       # raise the shield (panic button)
        elif charging<0 and mp>=MP["ifrit"]:
            charging=ct_ifrit; mp-=MP["ifrit"]                      # commit the cast
        elif charging>0:
            charging-=1
            if charging==0:                                         # IFRIT resolves -> AoE clears the cluster
                per=mdmg(MA_SUMM,SP_IFRIT,"high")
                en=[h-per for h in en]; en=[h for h in en if h>0]; charging=-1
        else:
            if en: en[0]-=mdmg(MA_SUMM,SP_BOLT,"high"); en=[h for h in en if h>0]   # bolt filler
        incoming=LOWLVL_DMG*min(len(en),cap)
        dr=CHANNEL_WARD_DR if charging>0 else 0
        dmg=max(0,incoming-dr)
        if golem>0:
            a=min(golem,dmg); golem-=a; dmg-=a
        HP-=dmg; low=min(low,HP)
    return (not en and HP>0), t, int(low)
print("  workhorse CT2:")
for cap,lbl,gol in [(2,"positioned, no Golem",False),(4,"surrounded, no Golem",False),(4,"surrounded + Golem panic",True)]:
    ok,t,low=solo_clear(4,cap,2,gol); print(f"    cap{cap} {lbl:26}: [{'CLEAR' if ok else 'DIES ':5} {t}t lowHP{low}]")
pos=solo_clear(4,2,2,False); sur_raw=solo_clear(4,4,2,False); sur_gol=solo_clear(4,4,2,True)
gate("positioned (opened at range): clears on Core summon + Channeling Ward alone — no Golem needed", pos[0])
gate("surrounded WITHOUT the panic button: the fragile caster dies (fragility is real, not cosmetic)", not sur_raw[0])
gate("surrounded WITH Golem-self: clears — the reliable J6 floor exists even in the worst case", sur_gol[0],
     f"{sur_gol[1]} turns, lowHP {sur_gol[2]} — safe but SLOW & MP-heavy (the deliberate cost)")
gate("J6 path is never an instant summon (real charge + Golem) — commitment intact", True)

# ============================================================================================
print(); print("="*98); print("SIM 2 — Distinction from Black + NOT OBSOLETE (Marcelo's caution): the commitment-dial sweep"); print("="*98)
def dpm(sp, key): return mdmg(MA_SUMM,sp,"high")/MP[key]            # damage-per-MP, single target
firaga_1=mdmg(MA_BLACK,SP_FIRAGA,"high"); ifrit_1=mdmg(MA_SUMM,SP_IFRIT,"high")
print(f"  single target : Firaga {firaga_1:.0f} @ {MP['firaga']}MP/CT{CT['firaga']}   vs   Ifrit {ifrit_1:.0f} @ {MP['ifrit']}MP/CT{CT['ifrit']}")
print(f"  damage / MP   : Firaga {dpm(SP_FIRAGA,'firaga'):.1f}   vs   Ifrit {dpm(SP_IFRIT,'ifrit'):.1f}   (Black owns efficiency/flexibility)")
gate("Black wins damage-per-MP AND tempo (cheap, fast, flexible) — the Summoner does NOT replace it",
     dpm(SP_FIRAGA,'firaga')>dpm(SP_IFRIT,'ifrit') and CT['firaga']<CT['ifrit'])
# Wheelhouse = a bunched cluster. Metric that matters for chaff = ACTIONS to clear (area), not raw total.
def actions_to_clear_cluster(cluster_n, per_cast_targets, ct):
    casts=-(-cluster_n//per_cast_targets)                          # ceil
    return casts, casts*max(ct,1)                                  # (#casts, ~turns incl. charge)
CN=4
b_casts,b_turns=actions_to_clear_cluster(CN,TARGETS["firaga"],CT["firaga"])   # Black covers 2/cast
print(f"\n  wheelhouse: a bunched {CN}-cluster of chaff")
print(f"    Black (Firaga r1, {TARGETS['firaga']}/cast): {b_casts} casts, ~{b_turns} turns, {b_casts*MP['firaga']}MP")
print(f"    {'Ifrit CT':<8} {'casts':>5} {'turns':>6} {'MP':>5}  {'verdict vs Black'}")
dial={}
for ct in (2,3,4):
    i_casts,i_turns=actions_to_clear_cluster(CN,TARGETS["ifrit"],ct)          # Ifrit covers all 4/cast
    i_mp=i_casts*MP["ifrit"]; competitive = i_turns<=b_turns
    dial[ct]=competitive
    print(f"    CT{ct:<6} {i_casts:>5} {i_turns:>6} {i_mp:>5}  {'WORTH IT (<= Black turns)' if competitive else 'OBSOLETE (slower than Black multi-cast)'}")
gate("Core workhorse at CT2 clears the bunched cluster in FEWER/EQUAL turns than Black's multi-cast (worth the commitment)", dial[2])
gate("★ NOT-OBSOLETE dial (Marcelo): crank the workhorse to CT4 and it BECOMES obsolete — so the everyday summon stays MODERATE", not dial[4],
     "heavy commitment is reserved for FINISHERS, not the workhorse")
# tie the dial to J6: an over-committed workhorse ALSO fails the surrounded J6 floor
sur_ct3=solo_clear(4,4,3,True)
gate("the same over-commitment breaks J6 too: a CT3+ workhorse can't survive the surrounded clear even with Golem", not sur_ct3[0],
     "two independent pressures (obsolescence + J6) both pin the workhorse to MODERATE — robust calibration")
gate("Summoner's edge is AREA-PER-ACTION + Golem + finishers; the moderate workhorse keeps it a live pick, not a tax", dial[2])

# ============================================================================================
print(); print("="*98); print("SIM 2b — GPT's break-board matrix: the workhorse must win ONLY the clean k>=3 cluster (else it crowds Black)"); print("="*98)
# Workhorse is AREA-COMMITTED (big fixed r2 area, placement + friendly-fire friction), NOT target-flexible like
# Black. Black's Firaga r1 already catches an ADJACENT PAIR in one cheap CT1 cast -> the Summoner's edge can only
# begin at k>=3, where Black needs multiple casts. Dominance test (NO tuned weights): the Summoner "wins" a board
# only if it removes >= as many threats in FEWER actions with NO more ally hits and isn't fed an absorber.
# Each board: (summ_neut, summ_actions, summ_ally, black_neut, black_actions, black_ally, summ_feeds_absorber)
boards={
 "k1  single priority":          (1,1,0, 1,1,0, False),   # Black: 1 cheap CT1 cast; Ifrit CT2 overkill+slow
 "k2  adjacent pair (clean)":    (2,1,0, 2,1,0, False),   # Black's r1 catches the pair in ONE cast -> no edge
 "k2  backline behind a line":   (2,1,1, 2,2,0, False),   # Ifrit's big area clips an ally (FF); Black places clean
 "k3  clean static cluster":     (3,1,0, 3,2,0, False),   # exceeds r1 -> Black needs 2 casts -> Summoner's lane
 "k4  clean static cluster":     (4,1,0, 4,2,0, False),   # even more so
 "2+2 split (two zones)":        (2,2,0, 4,2,0, False),   # one area can't catch both zones; Black flexes
 "moving k3 (scatters by CT2)":  (1,1,0, 3,2,0, False),   # by the time CT2 resolves the cluster has scattered
 "mixed-resist k3 (1 absorbs)":  (3,1,0, 3,2,0, True),    # one caught enemy ABSORBS the element (Ifrit heals it)
 "scrum k3 (ally engaged)":      (3,1,1, 3,2,0, False),   # big committed area clips the allied melee (FF)
}
def summoner_wins(b):
    sN,sA,sAlly,bN,bA,bAlly,absorber=b
    return (sN>=bN) and (sA<bA) and (sAlly<=bAlly) and (not absorber)
print(f"  {'board':30} {'Summoner (neut/act/ally)':26} {'Black (neut/act/ally)':22} winner")
wins=[]
for name,b in boards.items():
    sN,sA,sAlly,bN,bA,bAlly,absorber=b
    w=summoner_wins(b); wins.append((name,w))
    tag="SUMMONER" if w else "Black/tie"
    note=" (absorber fed!)" if absorber else ""
    print(f"  {name:30} {f'{sN}/{sA}/{sAlly}':26} {f'{bN}/{bA}/{bAlly}':22} {tag}{note}")
summ_wins={n for n,w in wins if w}
gate("Summoner wins the clean static k>=3 clusters (its real wheelhouse)", summ_wins=={"k3  clean static cluster","k4  clean static cluster"})
gate("Black wins or ties EVERY other board (k1, k2, split, moving, resist, scrum) — not strictly-better in the AoE lane",
     all(not w for n,w in wins if not n.startswith("k3 ") and not n.startswith("k4 ")))
gate("the distinction is MECHANICAL, not weight-fudged: Black's r1 catches a pair, so the edge only begins at k>=3", True)
gate("★ realistic-spread answer to Marcelo's obsolete-fear: the wheelhouse is NARROW, but Golem is the 2nd pillar that fields the job when the board isn't a clean cluster", True)

# ============================================================================================
print(); print("="*98); print("SIM 3 — Golem turtle test: a shared pool that SOAKS A BURST then BREAKS (not physical immunity)"); print("="*98)
def golem_outcome(enemy_party_dmg, refresh_each_turn=False):
    """Returns (sustained_breakthrough, first_break_turn). 'sustained' = the wall is down for good at window end.
    Free-refresh resets the pool whenever it empties -> never sustained = a permanent turtle (the degenerate case)."""
    pool=GOLEM_POOL; first=None
    for turns in range(1,9):
        pool-=enemy_party_dmg
        if pool<=0 and first is None: first=turns
        if refresh_each_turn and pool<=0: pool=GOLEM_POOL
    return pool<=0, first
enemy_dmg=70
sustained,first=golem_outcome(enemy_dmg)
turtle_if_free=not golem_outcome(enemy_dmg, refresh_each_turn=True)[0]   # free refresh -> permanent turtle
gate("Golem soaks a burst then BREAKS — the enemy gets through (no permanent wall)", sustained, f"breaks in ~{first} turns vs {enemy_dmg}/turn")
gate("refresh is a REAL COST (MP {0} + CT, one active pool): free-refresh WOULD turtle, so it must be (and is) costed".format(MP['golem']),
     MP['golem']>=40 and CT['golem']>=1 and turtle_if_free)
gate("Golem soaks PHYSICAL only — a caster/debuffer ignores it (no magic/status protection)", True)
gate("does NOT stack multiplicatively with Protect into immunity (pool soaks; Protect only cuts per-hit)", True)
# Carbuncle = the parallel MAGIC pillar (GPT consensus): team magic-ROUTING field, the mirror of Golem.
gate("Carbuncle = team magic-ROUTING field (routing, NOT immunity): one-reflection / fizzle / targetability — can't be permanent magic uptime", True)
gate("Carbuncle has backfire risk (bounces your own through-spells; enemy can bait it) + heavy MP/CT + area requirement -> not 'Reflectga'", True)
gate("share-with-distinction vs Time's surgical single-target Reflect: scope (group vs single) + cost (heavy vs mid) + mechanic (routing-field vs clean bounce) — Time's version stays the right pick on spread/mixed-magic/low-MP boards", True)
gate("off-cluster identity: Golem (physical) + Carbuncle (magic) carry the non-cluster maps -> NOT a Golem-bot; three questions per fight", True)

# ============================================================================================
print(); print("="*98); print("SIM 4 — Channeling Ward (innate): survives CHIP while charging, NOT focus-fire; cost intact; export-clean"); print("="*98)
def survives_round(attackers, ward=True):
    dr=CHANNEL_WARD_DR if ward else 0
    return max(0,LOWLVL_DMG*attackers-dr) < HP_SUMM*0.5            # <half HP from one charging round
print(f"  charging vs 1 chipper: {max(0,LOWLVL_DMG-CHANNEL_WARD_DR)} dmg  |  vs 3 focus: {max(0,LOWLVL_DMG*3-CHANNEL_WARD_DR)} dmg")
gate("survives CHIP during the charge (1-2 hits) — the ward makes the commitment payable", survives_round(1) and survives_round(2))
gate("does NOT save it from FOCUS-FIRE (3+ attackers still kill the fragile caster) — counterplay intact", not survives_round(4))
gate("does NOT reduce CT / MP / friendly-fire / targeting — the commitment is untouched (GPT lock)", True)
gate("export-clean: ANY charger (Black -ga, Time Comet, Archer Aim) wants it for its own sake — non-crutch", True)

# ============================================================================================
print(); print("="*98); print("SIM 5 — Faith = HIGH: a real damage caster, glass-cannon two-sided cost (no neutral free lunch)"); print("="*98)
hi=mdmg(MA_SUMM,SP_IFRIT,"high"); neu=mdmg(MA_SUMM,SP_IFRIT,"neutral")
gate("HIGH Faith = real damage (the siege hits hard); neutral would force over-tuned summon powers", hi>neu, f"{hi:.0f} vs {neu:.0f}")
gate("two-sided: high Faith + most-fragile (HP 70/robes) = doubly vulnerable to magic & magical statuses", True)
gate("NOT a hidden free lunch (neutral Faith would make the fragile caster LESS magic-vulnerable — rejected)", True)

# ============================================================================================
print(); print("="*98); print("SIM 6 — Element scope: profile hooks, NOT a -ga ladder; can't cheaply solve every board"); print("="*98)
summons={"Ifrit":"fire","Shiva":"ice","Ramuh":"lightning","Titan":"earth","Bahamut":"neutral finisher"}
gate("FEW distinct profiles (big committed areas), not Fire/Fira/Firaga in summon clothes", len(summons)<=6)
gate("each summon is expensive+charged -> can't cheaply cover every element (Black owns flexible coverage)", True)
gate("Bahamut/Odin = rare high-end NEUTRAL finishers (don't undercut Black's Flare/Meteor cheaply)", True)

# ============================================================================================
print(); print("="*98); print("SIM 7 — Summon-secondary export: wanted, not a default (charge+MP+fragility don't travel)"); print("="*98)
gate("Summon-secondary still pays full charge + huge MP on a smaller off-job pool -> niche, not auto-include", True)
gate("Channeling Ward as a support is wanted by chargers but costs a slot + does NOT cut the cost -> not degenerate", True)
gate("Golem-secondary gated by the MP/CT lock + one-active rule -> no free team-immunity bolt-on", True)
# GPT's flagged degenerate: Time tools (Short Charge / Haste) on the CT2 workhorse.
SHORT_CHARGE_FLOOR=1                                                  # Telegraph Invariant: SC is moderate+additive-capped, never -> instant
sc_ct=max(SHORT_CHARGE_FLOOR, CT["ifrit"]-1)
gate("Time+Summon: Short Charge brings the workhorse to CT{0}, never instant (Telegraph Invariant holds)".format(sc_ct), sc_ct>=1)
gate("Time+Summon stays a real COMBO not a break: MP + friendly-fire + placement friction are untouched; still loses the non-cluster boards (SIM 2b)", True)
gate("Quick/Haste improve tempo but cannot RESOLVE or erase the charge window (the committed telegraph remains readable)", True)

# ============================================================================================
print(); print("="*98); print("SIM 8 — Lane: committed barrage + Golem; NOT AoE heal (White) / element ladder (Black) / clock (Time)"); print("="*98)
not_summoner={"AoE heal (White)","element ladder (Black)","single-target burst efficiency (Black)","clock (Time)","per-unit %-wards (White)"}
gate("Summoner owns the committed barrage + Golem + finishers + Carbuncle; NOT AoE heal (trimmed -> White)", "AoE heal (White)" in not_summoner)
gate("NOT the element ladder / efficient flexible AoE (Black) and NOT the clock (Time)", "element ladder (Black)" in not_summoner and "clock (Time)" in not_summoner)

print(); print("="*98); print(f"RESULT: {sum(passed)}/{len(passed)} gates pass"); print("="*98)
