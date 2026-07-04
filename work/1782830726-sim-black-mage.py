#!/usr/bin/env python3
"""
Black Mage (#06, the offensive caster) battle-sim gate. Frozen DCL physical placeholders + magic placeholders.
Identity (converged w/ GPT): THE ARSENAL — the widest *attack* spellbook; mastery = reading the board
(element x tier x shape x placement x timing), NOT planting the biggest shell (that's the future Summoner's
heavy barrage). Two legible throughlines: best ANTI-ARMOR answer (magic ignores DR) + best flexible BURST.
Pure offense: no support/sustain beyond the free bolt floor.

DCL facts that reshape Black vs vanilla (doc 11): charge NOT interrupted by damage (only KO / a Brave-resisted
interrupt skill) -> the CT window is now a POSITIONAL telegraph, not a survival lottery; multiplicative
spell-centric tiers stay meaningful at every MA; magic ignores physical DR; AoE uncosted (Option A, the open
M2 risk); Faith two-sided (high-Faith glass cannon); Flare non-elemental; Death/Toad/Poison = 3d6 status.
Brakes under test: (1) -ga must be WRONG into one target, right at k>=2; (2) Rod Attunement raises FLOOR not
burst; (3) anti-armor parity; (4) glass-cannon TTK + Magic-Evade; (5) FRIENDLY FIRE = the load-bearing brake
on 'press -ga' (flagged as a doc-11 dependency). Lanes: element/Zodiac read + Flare; burst-vs-sustain budget;
identity wall vs White.
"""
from functools import lru_cache
from itertools import product

@lru_cache(maxsize=None)
def p_le(n):
    if n < 3: return 0.0
    if n >= 18: return 1.0
    return sum(1 for a,b,d in product(range(1,7),repeat=3) if a+b+d<=n)/216.0

# --- physical layer (frozen placeholders; for anti-armor comparison + incoming dive damage) ---
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

# --- magic layer (placeholders; doc 11 spine: dmg = base(MA) x spell_power x faith_c x faith_t x zodiac x shell x G_m) ---
G_M = 0.58
def faith(level): return {"low":0.70,"neutral":1.0,"high":1.30}[level]
def zod(level):   return {"weak":1.30,"neutral":1.0,"resist":0.70}[level]
SHELL = 0.50
MA_BM = 18   # highest MA in the roster (vanilla 150 vs White 110 ~ 18 vs 14)
def mdmg(ma, sp, fc, ft, z="neutral", shell=1.0):
    return ma*sp*faith(fc)*faith(ft)*zod(z)*shell*G_M
# spell ladder (multiplicative, spell-centric). Fire(basic)=6 == White's Holy ceiling; ratio Fire:Firaga=0.43
SP   = {"bolt":4,"Fire":6,"Fira":10,"Firaga":14,"Flare":12}
MP   = {"bolt":0,"Fire":8,"Fira":16,"Firaga":28,"Flare":30,"Death":30}
CT   = {"bolt":0,"Fire":2,"Fira":3,"Firaga":5,"Flare":5,"Death":6}
MP_BUDGET = 72
# Rod Attunement (innate+export): matching element ONLY -> stronger bolt + cheaper BASIC tier. NOT mid/ga/Flare/Death.
ATTUNE_BOLT = 1.25     # matching-element bolt floor x1.25
ATTUNE_BASIC_MP = 0.6  # matching-element basic tier MP x0.6 (sustain, never burst)

def line(c="="): print(c*100)

line(); print("SIM 1 - TIER PROFILES: -ga must be WRONG into one target, RIGHT at a cluster (k=1 vs k>=2)"); line()
print(f"  {'spell':8} {'dmg/target':>10} {'MP':>4} {'CT':>3} {'dmg/MP(k1)':>11} {'dmg/MP(k2)':>11} {'dmg/CT(k1)':>11} {'dmg/CT(k2)':>11}")
for s in ("Fire","Fira","Firaga"):
    d = mdmg(MA_BM, SP[s], "neutral","neutral")
    print(f"  {s:8} {d:10.1f} {MP[s]:4} {CT[s]:3} {d/MP[s]:11.2f} {2*d/MP[s]:11.2f} {d/CT[s]:11.2f} {2*d/CT[s]:11.2f}")
print("  READ: at k=1, Fire dominates dmg/MP and dmg/CT (cheap+fast); Firaga is the WORST single-target choice.")
print("  At k=2 Firaga's per-MP/per-CT overtakes -> -ga is a CLUSTER tool, never the single-target button. Crossover confirmed.")

line(); print("SIM 2 - ROD ATTUNEMENT raises the FLOOR, not the burst (+ the committed-element two-sided cost)"); line()
bolt0 = mdmg(MA_BM, SP["bolt"], "neutral","neutral")
boltA = mdmg(MA_BM, SP["bolt"], "neutral","neutral")*ATTUNE_BOLT
fire_mp0, fire_mpA = MP["Fire"], round(MP["Fire"]*ATTUNE_BASIC_MP)
print(f"  Matching element : bolt {bolt0:.1f} -> {boltA:.1f} (x{ATTUNE_BOLT}); basic Fire MP {fire_mp0} -> {fire_mpA} (x{ATTUNE_BASIC_MP}).")
print(f"  UNCHANGED by Attunement: Fira {mdmg(MA_BM,SP['Fira'],'neutral','neutral'):.1f}@{MP['Fira']}MP, "
      f"Firaga {mdmg(MA_BM,SP['Firaga'],'neutral','neutral'):.1f}@{MP['Firaga']}MP, Flare {mdmg(MA_BM,SP['Flare'],'neutral','neutral'):.1f}@{MP['Flare']}MP.")
print(f"  -> Attunement smooths SUSTAIN in your specialty (more cheap casts + a beefier anti-armor bolt); the BURST")
print(f"     budget is untouched, so it does NOT feed 'press -ga'. Two-sided cost: vs a board that RESISTS your rod")
print(f"     element, your cheap plan craters: Fire(resist) {mdmg(MA_BM,SP['Fire'],'neutral','neutral','resist'):.1f} "
      f"vs Fire(neutral) {mdmg(MA_BM,SP['Fire'],'neutral','neutral'):.1f} -> you must pay off-element or Flare.")

line(); print("SIM 3 - ANTI-ARMOR PARITY: magic is flat across armor (ignores DR); a fighter's swing collapses vs plate"); line()
print(f"  {'target armor':12} {'Black Fire':>11} {'Black bolt(floor)':>18} {'Fighter swing(PA14 sword)':>26}")
for arm in ("heavy","clothes","robes"):
    bf = mdmg(MA_BM, SP["Fire"], "neutral","neutral")           # magic: armor-independent
    bb = mdmg(MA_BM, SP["bolt"], "neutral","neutral")
    fs = swing(14,"sword",arm,"cut","high")
    print(f"  {arm:12} {bf:11.1f} {bb:18.1f} {fs:26.1f}")
print("  READ: Black's damage is IDENTICAL vs plate and vs robes; the fighter's sword craters against heavy DR.")
print("  Even out of MP, the free bolt floor still chips plate -> Black is THE anti-armor answer (canon role holds).")

line(); print("SIM 4 - GLASS CANNON (two-sided): robes + low HP + HIGH Faith; folds to a dive, near-one-shot by magic"); line()
HP_BM = 75
dive = swing(13,"knife","robes","cut","high")*lands(14,7,"side")
enemy_firaga = mdmg(18, SP["Firaga"], "high","high")   # enemy nuker on the HIGH-Faith Black (x1.30 target)
print(f"  Black HP {HP_BM} (robes, no DR). Thief dive ~{dive:.1f}/hit -> TTK {HP_BM/max(1,dive):.1f}: folds to melee pressure.")
print(f"  Enemy Firaga on the high-Faith Black (target x1.30): ~{enemy_firaga:.0f} -> one-shot. High Faith = glass BOTH ways.")
print(f"  Magic-Evade caps ~50% (coin-flip, never a wall); charges aren't damage-interrupted, but a KO before resolve")
print(f"  still eats the cast -> the fragile body IS the timer. Disruptable on all 3 status axes (low Brave/low HP/high Faith).")

line(); print("SIM 5 - ★ FRIENDLY FIRE is the load-bearing 'press -ga' brake (flagged as a doc-11 dependency)"); line()
per = mdmg(MA_BM, SP["Firaga"], "neutral","neutral")   # Firaga per-target
print(f"  Firaga per-target ~{per:.0f}. Case: a melee scrum = 3 enemies + 1 of YOUR units inside the blast radius.")
print(f"   WITH friendly fire : enemy dmg {3*per:.0f}, but you also hit your own unit for {per:.0f} (~{per/HP_BM:.0%} of a robe HP,")
print(f"     lethal to a softened ally) -> you must PLACE the blast off your own line, or hit an enemy-only cluster. Real cost.")
print(f"   WITHOUT friendly fire: enemy dmg {3*per:.0f}, allies safe -> blanket the scrum every time = the 'press -ga' failure.")
print(f"  READ: friendly fire is the cleanest FFT-native brake; if DCL ends up WITHOUT it, the no-FF backup brakes kick in")
print(f"  (-ga bad dmg/MP at k=1 [SIM 1], modest radius, long CT [enemies leave], per-target Magic-Evade). Confirm in doc 11.")

line(); print("SIM 6 - ELEMENT READ: exploit weakness vs resisted vs Flare (the resist-proof single-target button)"); line()
for z in ("weak","neutral","resist"):
    fg = mdmg(MA_BM, SP["Firaga"], "neutral","neutral", z)
    fl = mdmg(MA_BM, SP["Flare"], "neutral","neutral")   # non-elemental: ignores Zodiac
    win = "Firaga" if fg>=fl else "FLARE"
    print(f"  vs Zodiac={z:7}: Firaga {fg:6.1f} | Flare {fl:6.1f} (non-elemental, resist-proof)  -> right tool: {win}")
print("  READ: on an exploitable board the tiered element wins; on a resistant/absorb board Flare is the costly answer.")
print("  Flare is single-target + high MP/CT -> 'this one must die', never the everyday button. The element wheel must be REAL")
print("  in encounters (a content dependency) or the read collapses to tier math.")

line(); print("SIM 7 - BUDGET STYLE: burst-then-floor vs cheap-sustain; the bolt floor is never a dead turn"); line()
fire = mdmg(MA_BM, SP["Fire"], "neutral","neutral"); firaga = mdmg(MA_BM, SP["Firaga"], "neutral","neutral")
n_burst = MP_BUDGET//MP["Firaga"]; n_sustain = MP_BUDGET//MP["Fire"]
print(f"  MP budget {MP_BUDGET}. BURST: {n_burst}x Firaga ({n_burst*firaga:.0f} front-loaded) then fall to the free bolt ({bolt0:.1f}/turn).")
print(f"  SUSTAIN: {n_sustain}x Fire ({n_sustain*fire:.0f} spread thin) keeping a burst in reserve. Both keep casting MAGIC every turn;")
print(f"  the only zero-output turn is a CHARGE turn. Budget shapes STYLE, never an on/off (heroic mage, doc 11).")

line(); print("SIM 8 - IDENTITY WALL vs White Mage: Black is the burst/AoE/ladder; White's offense is a single basic-Fire smite"); line()
holy = mdmg(14, 6, "high","neutral")  # White: MA 14, Holy=Fire-tier(6), the CEILING, single-target, no ladder/AoE
print(f"  White Holy (MA14, Fire-tier, single-target, NO ladder/AoE, the CEILING): {holy:.1f}")
print(f"  Black Fire {fire:.1f} / Fira {mdmg(MA_BM,SP['Fira'],'neutral','neutral'):.1f} / Firaga {firaga:.1f} (+AoE) / Flare {mdmg(MA_BM,SP['Flare'],'neutral','neutral'):.1f}")
print(f"  -> Black's basic ~= White's ceiling, then Black KEEPS CLIMBING (ladder + AoE + element exploit + burst).")
print(f"  The wall holds numerically: White tops out where Black starts. No overlap.")

line(); print("SIM 9 - DEATH (Tier-2 cruelty): 3d6 status on INVERSE Faith; swingy, boss-immune (illustrative placeholder)"); line()
# magical status (doc 13): caster MA = offense, resist on INVERSE Faith (low-Faith resists, high-Faith vulnerable). Placeholder model.
def death_land(ma_off, target_faith_level, boss=False):
    if boss: return 0.0
    base = {"low":7,"neutral":9,"high":11}[target_faith_level]  # inverse-Faith: devout target = easier to land
    return p_le(min(16, base + (1 if ma_off>=18 else 0)))
for ft in ("low","neutral","high"):
    print(f"  vs target Faith={ft:7}: Death land ~{death_land(MA_BM,ft):.0%}  (inverse-Faith: the devout are vulnerable, the atheist resists)")
print(f"  vs BOSS/immune: {death_land(MA_BM,'high',boss=True):.0%} -> Death is a poor boss answer by design.")
print("  READ: single-target, expensive (30 MP / 6 CT), swingy, immunity-respecting -> a late cruelty button, not a rotation.")
