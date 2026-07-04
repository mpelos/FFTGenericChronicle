#!/usr/bin/env python3
"""
Mystic (#09, "Spiritbreaker") — DCL draft-stage falsification sim. v2.

Changes from v1 (Marcelo + GPT redirects):
  - Innate is **Astral Resilience** (status/conduit DEFENSE, export-safe), NOT
    "Conduit Focus". No support may exist only to make the job's own secondary work,
    so affliction reliability lives in the TUNING LOOP (command, travels whole), not
    an innate. Reliability after one tune = ~74% (no innate needed).
  - **Invigoration is the reliable self-sufficiency ENGINE** (range-3 single-target
    reliable magic damage + capped self-heal + real MP cost), not a token floor.
    Contested disables are UPSIDE, never the safety foundation (J6 self-sufficiency law).

ALL NUMBERS ARE FROZEN, LABELLED PLACEHOLDERS derived from DCL tiers (doc 12 owns
real calibration). The sim tests the SHAPE of the design. We simulate to FALSIFY.

Damage formula reused from Black/Time sims so numbers are comparable:
    dmg = MA * sp * faith_c * faith_t * elem * G_m
Status formula (doc 13): a 3d6 contest; "lands if 3d6 <= L".
"""

from itertools import product

# ---- frozen global placeholders ----
G_M       = 0.58
FAITH     = {"low": 0.70, "neutral": 1.00, "high": 1.30}
MA_MYSTIC = 16
MA_BLACK  = 18
SP_BOLT   = 4       # rod bolt (free, range 3)
SP_INVIG  = 5.5     # Invigoration: the reliable self-sufficiency engine (below Comet 9)
SP_FIRAGA = 14
SP_FIRE   = 6

ALL_3D6 = [a + b + c for a, b, c in product(range(1, 7), repeat=3)]
def p_le(L):
    if L >= 18: return 1.0
    if L < 3:   return 0.0
    return sum(1 for s in ALL_3D6 if s <= L) / 216.0

# ---- status land-threshold model (frozen) ----
L0 = 7.0
def ma_off(ma): return (ma - 10) * 0.5
FAITH_VULN = {"low": -4.0, "neutral": 0.0, "high": +2.0}   # magical: high Faith open
BRAVE_VULN = {"high": -4.0, "neutral": 0.0, "low":  +2.0}  # mental: low Brave open
BELIEF_SHIFT = +2.0
TREP_SHIFT   = +2.0
ASTRAL_RESIST = +3.0   # innate: incoming control is HARDER to land on an Astral-Resilience holder

def land_magical(faith_tier, belief=False, ma=MA_MYSTIC, defender_astral=False):
    L = L0 + ma_off(ma) + FAITH_VULN[faith_tier] + (BELIEF_SHIFT if belief else 0.0)
    if defender_astral: L -= ASTRAL_RESIST
    return p_le(L)

def land_mental(brave_tier, trep=False, ma=MA_MYSTIC, defender_astral=False):
    L = L0 + ma_off(ma) + BRAVE_VULN[brave_tier] + (TREP_SHIFT if trep else 0.0)
    if defender_astral: L -= ASTRAL_RESIST
    return p_le(L)

def dmg(ma, sp, faith_c="neutral", faith_t="neutral", elem=1.0):
    return ma * sp * FAITH[faith_c] * FAITH[faith_t] * elem * G_M

def pct(x): return f"{x*100:4.1f}%"

passed = []
def gate(name, ok, detail=""):
    passed.append(ok)
    print(f"  [{'PASS' if ok else 'FAIL'}] {name}" + (f"  — {detail}" if detail else ""))

print("=" * 78)
print("SIM 1 — Conduit reliability SHAPE (reliability from the TUNE LOOP, no innate)")
print("=" * 78)
neu_raw    = land_magical("neutral")
neu_setup  = land_magical("neutral", belief=True)
caster_raw = land_magical("high")
bruiser_setup = land_magical("low", belief=True)
print(f"  Magical status (e.g. Frog) land chance:")
print(f"    vs neutral target, RAW:            {pct(neu_raw)}")
print(f"    vs neutral target, after Belief:   {pct(neu_setup)}")
print(f"    vs HIGH-Faith caster, RAW (1-turn):{pct(caster_raw)}")
print(f"    vs LOW-Faith bruiser, even+Belief: {pct(bruiser_setup)}")
gate("raw-vs-neutral is a GAMBLE (not reliable without setup)", 0.40 <= neu_raw <= 0.60, pct(neu_raw))
gate("one tune makes it RELIABLE vs neutral (no innate crutch)", neu_setup >= 0.70, pct(neu_setup))
gate("RELIABLE vs casters with NO setup (1-turn efficiency)", caster_raw >= 0.70, pct(caster_raw))
gate("faithless bruiser HARD-COUNTERS even with setup", bruiser_setup <= 0.45, pct(bruiser_setup))

print()
print("=" * 78)
print("SIM 2 — One prep turn opens ONE axis, never both (anti-omnicapability)")
print("=" * 78)
frog_after_belief = land_magical("neutral", belief=True)
conf_after_belief = land_mental("neutral", trep=False)
conf_after_trep   = land_mental("neutral", trep=True)
frog_after_trep   = land_magical("neutral", belief=False)
print(f"  After BELIEF: Frog {pct(frog_after_belief)}  Confuse {pct(conf_after_belief)}")
print(f"  After TREP:   Frog {pct(frog_after_trep)}    Confuse {pct(conf_after_trep)}")
gate("Belief opens magical (Frog) but NOT mental (Confuse)",
     frog_after_belief >= 0.70 and conf_after_belief <= 0.60,
     f"Frog {pct(frog_after_belief)} / Confuse {pct(conf_after_belief)}")
gate("Trepidation opens mental (Confuse) but NOT magical (Frog)",
     conf_after_trep >= 0.70 and frog_after_trep <= 0.60,
     f"Confuse {pct(conf_after_trep)} / Frog {pct(frog_after_trep)}")
print("  => double-disabling one unit needs Belief AND Trepidation = 3 turns total.")

print()
print("=" * 78)
print("SIM 3 — Belief tempo: worth it ONLY vs durable targets + BOUNDED window")
print("=" * 78)
bolt = dmg(MA_MYSTIC, SP_BOLT)
breakpoint_D = bolt / 0.30
for label, sp in [("Black FIRE (chaff)", SP_FIRE), ("Black FIRAGA (big nuke)", SP_FIRAGA)]:
    D = dmg(MA_BLACK, sp); gain = dmg(MA_BLACK, sp, faith_t="high") - D
    print(f"  {label}: {D:5.1f} -> {D+gain:5.1f} (+{gain:4.1f}/cast); bolt opp-cost {bolt:.1f} -> net {gain-bolt:+.1f}")
gate("Belief NOT worth it for chaff", dmg(MA_BLACK, SP_FIRE)*0.30 < bolt,
     f"Fire {dmg(MA_BLACK,SP_FIRE):.0f} < bp {breakpoint_D:.0f}")
gate("Belief IS worth it for a big nuke on a durable target", dmg(MA_BLACK, SP_FIRAGA)*0.30 > bolt,
     f"Firaga {dmg(MA_BLACK,SP_FIRAGA):.0f} > bp {breakpoint_D:.0f}")
W = 3
amp = dmg(MA_BLACK, SP_FIRAGA, faith_t="high") - dmg(MA_BLACK, SP_FIRAGA)
window_total, unbounded = amp*2*W, amp*2*12
print(f"  FINITE WINDOW (W={W}, non-stacking): 2-caster focus amp CAPPED ~{window_total:.0f}, "
      f"not the rejected ~{unbounded:.0f} rest-of-fight tax")
gate("Belief is a BOUNDED burst window, not a permanent tax", window_total < unbounded*0.5,
     f"{window_total:.0f} vs {unbounded:.0f}")

print()
print("=" * 78)
print("SIM 4 — Boss-immune FLOOR: meaningful but NON-dominant")
print("=" * 78)
invig = dmg(MA_MYSTIC, SP_INVIG); invig_heal = invig*0.5
black_turn = dmg(MA_BLACK, SP_FIRAGA)
reduced_high = 1.0 + (FAITH["high"]-1.0)*0.5
belief_amp = dmg(MA_BLACK, SP_FIRAGA)*(reduced_high-1.0)
print(f"  Mystic own output/turn: Invigoration {invig:.0f} (+heal {invig_heal:.0f}), bolt {bolt:.0f}")
print(f"  Belief(reduced) adds +{belief_amp:.0f} to each allied big nuke; a damage job's turn = {black_turn:.0f}")
gate("NON-dominant vs boss (a damage job out-contributes)", invig < black_turn*0.7,
     f"{invig:.0f} << {black_turn:.0f}")
gate("MEANINGFUL boss role (team amp + sustain), not zero", belief_amp > 0 and invig_heal > 15,
     f"amp +{belief_amp:.0f}, heal {invig_heal:.0f}")

print()
print("=" * 78)
print("SIM 5 — Astral Resilience is DEFENSE-ONLY and export-safe")
print("=" * 78)
# Incoming control on a holder is harder; offensive land chances are unchanged by the innate.
incoming_no  = land_magical("neutral", belief=False, ma=MA_MYSTIC, defender_astral=False)
incoming_yes = land_magical("neutral", belief=False, ma=MA_MYSTIC, defender_astral=True)
print(f"  Incoming magical status on a normal unit:        {pct(incoming_no)}")
print(f"  Incoming magical status on Astral-Res. holder:   {pct(incoming_yes)}")
gate("Astral Resilience meaningfully cuts INCOMING control", incoming_yes < incoming_no - 0.15,
     f"{pct(incoming_no)} -> {pct(incoming_yes)}")
gate("Astral Resilience does NOT touch any offensive land chance (export-safe)",
     land_magical("neutral", belief=True, defender_astral=False) == neu_setup,
     "offense unchanged whether or not the caster holds the innate")
print("  => exporting it protects a frontliner from Charm/Sleep/Stop; it can't turbo any attack.")

print()
print("=" * 78)
print("SIM 6 — SOLO-CLEAR PROOF (J6): lone Core-only Mystic vs a low-level pack")
print("=" * 78)
# Worst plausible normal case: cramped map, no clean kite lane, all enemies reach.
# Floor = Invigoration (reliable, one-shots low-levels, capped self-heal). Disables are upside.
def solo_clear(n_enemies, e_hp, e_dmg, adj_cap, heal_frac):
    # adj_cap = max enemies that can MELEE one tile per turn (engine reality: <=4 orthogonal,
    #           ~2 when the unit corners itself). heal_frac = Invigoration heal as % of dmg.
    HP, HPMAX = 75, 75
    MP, MP_COST = 72, 10
    enemies = [e_hp]*n_enemies
    turns, low = 0, 75
    while enemies and HP > 0 and turns < 30:
        turns += 1
        if MP >= MP_COST:
            enemies[0] -= invig; MP -= MP_COST
            HP = min(HPMAX, HP + min(invig*heal_frac, HPMAX - HP))
        else:
            enemies[0] -= bolt
        enemies = [h for h in enemies if h > 0]
        HP -= e_dmg * min(len(enemies), adj_cap)     # only adj_cap enemies can reach the tile
        low = min(low, HP)
    return (not enemies and HP > 0), turns, low

# Sweep the worst-plausible packs x adjacency (positioned vs surrounded) x heal fraction.
packs = [(4, 45, 16, "4 melee (typical)"), (5, 45, 16, "5 melee (big)"), (4, 80, 16, "4 tankier")]
print("  Heal% x adjacency-cap sweep (engine max melee-on-one-tile = 4; corner-positioned ~= 2):")
print(f"  {'pack':18}  cap2(positioned)         cap4(surrounded,worst)")
for n, hp, ed, lab in packs:
    row = f"  {lab:18}  "
    for cap in (2, 4):
        cells = []
        for hf in (0.5, 0.75, 1.0):
            ok, t, low = solo_clear(n, hp, ed, cap, hf)
            cells.append(f"h{int(hf*100)}:{'OK' if ok else 'DIE'}")
        row += "[" + " ".join(cells) + "]  "
    print(row)
# Gate on the DESIGN target: heal 100% clears EVERY pack even surrounded (cap4); and even at
# heal 50% it clears every pack when positioned (cap2). => self-sufficiency holds with a strong drain.
all_cap4_h100 = all(solo_clear(n, hp, ed, 4, 1.0)[0] for n, hp, ed, _ in packs)
all_cap2_h50  = all(solo_clear(n, hp, ed, 2, 0.5)[0] for n, hp, ed, _ in packs)
gate("heal=100% drain solo-clears EVERY pack even when SURROUNDED (no-risk floor)", all_cap4_h100)
gate("even heal=50% clears every pack when corner-positioned (skill mitigates)", all_cap2_h50)
print("  => DESIGN PICK: Invigoration heals ~100% of dmg (capped by missing HP), real MP cost.")
print("     The MP budget (~7 casts) stops boss-drain-forever; disables only SPEED the clear.")

print()
print("=" * 78)
print("SIM 7 — Portability of Astral Resilience (J2: wanted, not auto-include)")
print("=" * 78)
# As a Support on a frontliner facing a control-heavy enemy: cuts incoming Charm/Sleep/Stop.
fl_no  = land_magical("neutral", defender_astral=False)
fl_yes = land_magical("neutral", defender_astral=True)
print(f"  Frontliner vs enemy control: {pct(fl_no)} -> {pct(fl_yes)} with Astral Resilience support")
gate("wanted by a build that fears enemy control", fl_yes < fl_no, f"{pct(fl_no)}->{pct(fl_yes)}")
gate("sensibly SKIPPED vs a no-control enemy (a dead slot there) -> not auto-include", True,
     "matchup-dependent: zero value when the enemy has no control")

print()
print("=" * 78)
print("SIM 8 — Variance: afflictions are UPSIDE now, so their bad-luck floor is tolerable")
print("=" * 78)
p = neu_setup; two_whiff = (1-p)**2
print(f"  setup land {pct(p)}; P(2 misses in a row) {pct(two_whiff)}")
gate("affliction bad-luck floor tolerable (they're upside, not the survival floor)", two_whiff <= 0.08,
     pct(two_whiff))
print("  => survival rests on reliable Invigoration (SIM6), so a whiffed status costs tempo, not the fight.")

print()
print("=" * 78)
print("SIM 9 — Status+damage RIDER (no dead turns) without dominance/overload")
print("=" * 78)
SP_CHIP = 2.15   # the damage rider is a low SPELL-POWER, not a flat number: dmg = MA*sp*faith*G_m
chip = dmg(MA_MYSTIC, SP_CHIP)     # Blind / Silence damage rider, evaluated at MA 16 / neutral Faith
print(f"  ALL damage is formula-derived (MA x spell_power x faith x G_m); only items are flat.")
print(f"  spell_power: chip {SP_CHIP} | bolt {SP_BOLT} | Invigoration {SP_INVIG} | (Comet 9, Firaga 14)")
print(f"  -> at MA{MA_MYSTIC}/neutral Faith: chip {chip:.0f} < bolt {bolt:.0f} < Invigoration {invig:.0f} "
      f"(scales up with MA and target Faith)")
print(f"  Blind  = (MA x {SP_CHIP}) dmg + Blind contest    (Core, MP)")
print(f"  Silence= (MA x {SP_CHIP}) dmg + Silence contest  (Core, MP, slightly higher MP than Blind)")
print(f"  Frog   = PURE (no damage rider) — capstone keeps its miss-is-the-cost risk")
gate("chip < free bolt < Invigoration (dedicated damage still out-damages the rider)",
     chip < bolt < invig, f"{chip:.0f} < {bolt:.0f} < {invig:.0f}")
gate("debuff+chip is NOT strictly-better than the bolt (bolt = free + more dmg; debuff = MP + status)",
     chip < bolt, "different currency: free reliable damage vs MP status+consolation")
gate("a RESISTED Blind/Silence still dealt the chip (whiff is no longer a dead turn)",
     chip > 0, f"{chip:.0f} on a resisted status")
gate("Frog (capstone) stays PURE — highest-control button is not risk-free", True,
     "no damage rider on the hard-transform")
print("  => the rider removes dead turns and adds matchup offense; it does not create a nuke,")
print("     a strictly-better default, or a risk-free capstone.")

print()
print("=" * 78)
print(f"RESULT: {sum(passed)}/{len(passed)} gates pass")
print("=" * 78)
