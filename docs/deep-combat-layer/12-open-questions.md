# Open Questions

Status: Living register
Date: 2026-06-25
Depends on: all prior documents.
Review: Pending.

This is the register of everything **not yet decided** in the DCL. The high-level skeleton is
complete; what remains is detail and calibration — except for one genuinely open *design* piece (the
wounds/death model), flagged first.

## 1. Wounds & death model — RESOLVED (2026-06-25): heroic FFT model

**Decision (Marcelo):** **persistent** HP-based degradation is rejected — the model is heroic. A
unit fights at **full effectiveness until 0 HP** (no death-spiral; **no persistent critical-state /
≤1/3-HP penalty**); at 0 HP, vanilla **death → countdown → treasure/chest**. Rationale: FFT is heroic
(heroes perform at 100% until they fall, like D&D); and a persistent HP-debuff unfairly **punishes
melee more than ranged**, since melee takes the hits.

**Automatic major-wound trigger — SET ASIDE (legibility).** Marcelo (2026-06-25): an automatic
"single hit > ½ HP → hidden resist → reel" rule is **too hidden/confusing** for FFT (a unit suddenly
gets stunned with no clear cause), which clashes with the DCL's legibility principle. So there is
**no automatic damage-triggered reeling.** It may be revisited only if clearly *telegraphed* (e.g.
a weapon property "may knock down", shown up front) — never as a hidden universal rule.

Instead, **stun / knockdown / fear are ability/skill-driven statuses** (item 8) — inflicted by
explicit job skills and weapon properties, with clear cause and effect, and **resisted by Brave**.
The free-form options explored below are kept as a historical record.

*(Historical — explored and rejected:)* **Never decided.** How does a unit go down, and what happens on the way?

- **FFT-native:** a unit drops to 0 HP, becomes a KO'd body with a **crystal/treasure countdown**;
  no death-spiral on the way down — full effectiveness until 0, then out.
- **GURPS-native:** a **death spiral** — performance degrades as HP drops (shock penalties, HT
  checks to stay conscious/alive, major-wound stun). Death is a series of checks, not a single
  threshold.
- **Hybrid options:** keep FFT's 0-HP-countdown for *death*, but borrow GURPS' **major-wound stun**
  for *flow* (this is already partly assumed — Brave's "composure" half resists exactly this, `07`).

This must be resolved before the model is whole; it touches HP, Brave (composure), interruption, and
the entire feel of taking damage. **Next-session candidate.**

## 2. What a fumble does — RESOLVED (2026-06-25)

A fumble is simply an **automatic miss** — no extra penalty. (Marcelo: keep it clean and
non-punishing, consistent with the heroic model; the rare crit-fail just whiffs.) Documented in `04`.

## 3. Interruption rules — RESOLVED (2026-06-25)

**Decision:** taking damage does **not** interrupt a charged action (vanilla). Interruption happens
only via a **specific interrupt skill** (or full incapacitation — KO / a stopping status), and
**Brave composure resists** it. It is thus part of the skill-driven status system (item 8), never an
automatic damage effect. See `11`.

*(Historical:)* What exactly can be interrupted? Magic charge times are the obvious case ("charged action"), but the
precise list of interruptible actions, and what counts as a "major wound" (>½ HP assumed) for the
composure check (`07`), are open. Now part of the **status & charge system** (item 8); under the heroic wound model (item 1) interruption is not an automatic HP-threshold effect.

## 4. Multi-hit / dual-wield × defense rolls — RESOLVED (2026-06-25)

Each strike resolves **independently**: own hit roll, own defense roll, and **each depletes a
defense**; each can crit or fumble. This makes multi-hit/dual-wield a **guard-shredder** (one-unit
focus-fire), balanced by **lower power per strike**. Documented in `04`. Remaining detail: per-strike
power/skill penalty (calibration) and whether a counter fires once or per strike.

## 5. Numeric calibration (the big bucket)

Marcelo explicitly deferred fine calibration ("deixa a calibragem fina para depois"). Open numbers:

- **Damage model (`02`):** `G` (GURPS→FFT bridge), PA→ST offset (~+4), `pen_floor` fraction
  (~15–33%), the DR-scaling-with-`wmod` curve.
- **Damage types (`03`):** per-armor-class DR per type, crush raw-modifier multiplier (~1.5×),
  armor-divisor values, validating the ~+35% right-tool / ~2× wrong-tool sharpness.
- **Hit/defense (`04`):** exact Dodge/Parry/Block formulas, depletion amount per attack, validation
  of the >50% open-target and ~31% turtled-target regimes.
- **Speed (`01`/`04`):** the **variance** of Speed across builds — how much faster a fast build is
  than a slow one. This is the knob that sets how strong the *tempo* axis is: too high and Speed is
  the only stat that matters (the vanilla-FFT trap), too low and it is flavorless. Recommended
  **moderate-low**. It also bounds weapon `+Speed` grants (e.g. the knife). Finesse (Speed→damage)
  is **not** on the table — `01`.
  - **Per-job Speed calibration (validation B1 — MANDATORY when designing jobs).** After B1, Speed
    buys turn-frequency + the kept guard-refresh edge (Dodge is decoupled — `01`/`04`). The agile jobs
    (Thief/Ninja/Archer) are fast **by design**, but each job's Speed MUST be calibrated against that
    job's **offensive profile / mechanics**: more turns × *correspondingly lower per-hit offense*. A
    fast + high-offense + high-mitigation package must never be a buildable job — fast jobs pay for
    tempo in damage-per-hit (or another axis). This per-job calibration — not a refresh-rule change —
    is the guardrail that keeps the kept full-refresh mechanic from making Speed dominant.
- **Facing (`05`):** the −2 side modifier, back-strike rules for large units.
- **Reach (`06`):** point-blank penalty, stop-hit ability numbers.
- **Brave (`07`):** `k_off` (offense-swing size for Brave↔Faith symmetry), curves, composure threshold.
  **High-end offense multiplier set to ~1.35** (down from 1.56) by validation B9 tuning
  (`sim_brave_offense`) — modest *because* its corrective (taunt) is deliberately rare; final value +
  Faith-symmetry match + the low-end floor (toward 0.60) remain calibration. `def_div=12`.
- **Faith (`08`):** Faith enters magic **twice** — caster output × target vulnerability — each on a
  **bounded band centered at 1.0** (mid-faith neutral), provisional **[0.70, 1.30]** (`sim_magic_faith`:
  cuts vanilla's ~11.6× buildable double-faith swing to ~3.5×; keeps a real atheist-tank identity
  without immunity; Faith is a bounded spreader, not the stack's explosion source). Exact band width +
  magic-vulnerability slope remain calibration.
- **Zodiac (`09`) — MODEL CHANGED 2026-06-28:** reverted from "elemental temperament" to **FFT-style
  sign compatibility** (attacker × target), applied to **damage + hit**, on a **much subtler band**
  than vanilla and **surfaced** in the preview. Grid = FFT's distance rule (1 best / 2 good / 2 bad /
  rest neutral; matrix locked in `09`). Band chosen = **"Subtle"**: damage Good ×1.10 / Bad ×0.90 /
  Best ×1.20 / Worst ×0.80; hit +1/−1 only on the opposite-sign (Best/Worst) matchup; opposite = Best
  everyday, **Worst reserved for designed content**. Enters as `zodiac_mult` (physical `02` + magic
  `11`) and `zodiac_hit` (attacker's 3d6 hit roll, `04`). Note: **elemental affinity moved off Zodiac**
  to equipment/status/job/content (`element_mult`, `11`). Still calibration: final band ("Subtle" vs
  "Very subtle"/"Medium"); whether `zodiac_hit` also touches the `13` status contest; Serpentarius
  handling; presentation (item 6).
- **Weapon skill (`10`):** the **`skill(grade, jobLevel, charLevel)` formula shape is RESOLVED**
  (validation A5, `sim_skill_scaling`): `skill = base[g] + rate[g]·(J·(jobLevel−1) +
  K·(jobLevel÷8)·(charLevel−1))` with provisional `J=2.5`, `K=0.25` (→ **one job level ≈ 10 character
  levels**, grade-independent), `rate = A1.00 B0.72 C0.50 D0.32 F0.20`, char term **gated by
  `jobLevel÷8`** so a maxed grade-F at 99 stays sub-cap (≈13) while a grade-A reaches ≈55 — low grade
  weak even at 99, passes all 10 design targets across a wide robust plateau. **Still calibration:** the
  `base`/`rate`/`J`/`K`/cap magnitudes (move with the `base()` table + `G`); per-job/level skill tables;
  job×family grade matrix; Sword Master value; over-cap skill→damage/penetration conversion rate.
  - **Crossbow & gun = skill-primary (RESOLVED, validation A5):** their damage input is **weapon skill**,
    not PA (`base(skill)` in `02`; crossbow→raw, gun→penetration) — trait-neutral, scales to 99 via
    skill so they never go obsolete (no flat damage, no new-equipment dependency). The exact
    skill→damage and skill→penetration rates are part of the formula tuning above.
- **Equipment (`14`):** all weapon families *plus the Weight model, armor, and shield slots* are now
  *designed* in `14-equipment.md` (blades / crush / reach / magic / ranged / performer / unarmed;
  **every piece carries Weight**; armor = DR-by-type + modest HP vs Weight→−Move/−Dodge; shield = Block
  top-rung + ranged coverage, DR-light; fell-sword rejected — not in TIC). Open: the per-family/per-class
  **numbers** (relative tiers → values, incl. the `MA_wmod` curve, the untrained-fist penalty
  `fist_pen`, per-armor-class DR/HP, **per-piece Weight values**, Block magnitude), and the **helmet /
  accessory** slots (not designed yet).
  - **Weight → Move/Dodge curve (`14`, RESOLVED-model 2026-06-26):** the model is locked (per-piece
    Weight, summed, run through a curve — **never** a flat per-item `−Move`, because Move is too coarse).
    Open **calibration:** the per-piece Weight values; the **Weight→Move breakpoints** (coarse, with a
    generous dead-zone — light −0, mail & normal plate −1, loaded plate −2/−3); the **Weight→Dodge
    slope** (fine, near-smooth — and **monotone: lighter always dodges more**, which is what keeps
    mail/plate non-dominated at the *same* Move tier — validation B10). Locks: **no PA/ST in the calc**
    (same Weight = same penalty, else strong units escape the tradeoff); **Weight coupled to DR** (a
    "tough-and-light" piece is a rationed premium only). The curve is a **Tier-2 computed hook**
    (item 7); per-piece Weight is data.
  - **Armor-class non-domination (`14`, RESOLVED, validation B10, `sim_armor_calibration`):** the
    "armor triangle" is reframed honestly as a **2-pole mitigation↔avoidance axis** (Plate↔Robe) with
    Mail/Leather as non-tank interior, and armor is **largely job-gated** (so P1′ bites only where one
    job can equip more than one class). The sim fixes provisional relative numbers (DR cut/thr/crush ≈
    Plate 9/8/3, Mail 5/5/5, Leather 2/2/2, Robe 0/0/0; Weight 26/16/8/3 → Move: leather/robe 0,
    mail/plate −1, loaded plate −2) where **no class is strictly dominated** and **each is the best pick
    in some context** — mail/plate stay non-dominated at the same Move tier via the monotone Dodge
    gradient + mail's flat DR covering plate's crush hole. Mail's clean niche = the anti-crush /
    anti-plate-hole tank; Leather = mobility chassis (positional defense). Post-B1
    (Speed off Dodge) **plate, not robe, holds the best worst-case** — the over-robustness artifact is
    gone. Open: only the absolute magnitudes (ride the global G / DR-scaling).
  - **Armor CT reserve knob (`14`, RESOLVED-with-reserve 2026-06-25):** armor costs **Move + Dodge,
    never CT** (GURPS-faithful; CT stays pure Speed-stat, `01`). A **small heavy-armor CT penalty** is
    held in reserve as the one knob to deploy *only if* the leather-melee proves too weak vs the
    plate-melee in playtest. **Main calibration risk of the armor model:** whether Move + Dodge +
    positioning is enough to make leather-melee competitive with plate-melee's reliable DR + HP.
  - **HP pool home (`14`, OPEN):** whether armor's modest HP stays on the body slot or migrates
    entirely to the head slot (orthogonal: body = DR + Weight, head = HP/MP pool) — decided with the
    **helmet** slot. Base-HP stays the gear-independent status-resist stat (`13`) either way.
  - **Shield DB reserve knob (`14`, RESOLVED-with-reserve 2026-06-25):** the shield is **Block-only,
    DR-light, no passive Defense Bonus** — a finite per-turn resource. A modest always-on **DB** (flat
    bonus to active defenses while facing) is held in reserve only if the shield plays too binary.
  - **Defense coverage rule (`04`, NEW ruling 2026-06-25):** **Dodge covers everything (floor), Parry
    is melee-only, Block (shield) covers melee *and* ranged.** This is what gives the shield its niche
    (the melee answer to ranged / the plate-tank's survival on the approach). `04` should absorb this
    coverage table; numbers (Block magnitude vs Parry) are calibration.
- **Magic (`11`):** the damage **shape is RESOLVED** (`sim_magic_shape`) — **multiplicative,
  spell-centric** `base(MA) × spell_power × faith × element × G_m`, the conceptual mirror of physical
  (**physical subtracts, magic multiplies**); magic ignores physical DR (anti-armor); base(MA) linear
  (no GURPS table). **#1 magic risk:** no structural damper → band is calibration-held, stacked
  multipliers compound (keep Faith the one big two-sided multiplier; elemental affinity/Shell bounded
  bands; Zodiac the subtle sign matchup `09`; soft-cap reserve). **Faith count is RESOLVED** (`sim_magic_faith`): **twice** (caster output × target
  vulnerability), each a **bounded band centered at 1.0**, provisional **[0.70, 1.30]** — the only shape
  consistent with the locked two-sided Faith (`08`/A2), and a bounded spreader (≤1.69×), not the
  explosion source. **Affinity/Shell/Zodiac stacking is RESOLVED** (`sim_magic_stack`): **all-multiplicative &
  commutative** (no stacking order), **modest bounded bands** (provisional elemental affinity weak ×1.30 /
  resist ×0.70, Shell ×0.50; Zodiac sign matchup subtler still — Best ×1.20 / Worst ×0.80, `09`), big
  elemental swings (×2/absorb) kept as **rare designed properties**, **soft-cap (~2.5×) in reserve**
  (dormant under the modest bands). The compounding corner stays ~2.2× and defense mirrors
  offense (a hard turtle ~0.24×, never immunity, with attacker outs). **The economy is RESOLVED**
  (`sim_magic_economy`): MP is a **per-battle budget** (small trickle) gating the **big spells**, over a
  free always-on floor that is the **caster weapon's basic Attack** — a **range-3 MA-scaled elemental
  bolt** (magic-gun Formula `0x04`, `14`), **element set by the equipped SKU, not the job**, committed
  for the battle (no in-combat swap → the elemental matchup stays a planned choice). Staves carry the bolt too (a healer
  is never useless); **strong healing stays MP-gated** (heal-on-attack is a floor-tier Staff SKU only).
  So the mage acts with magic every turn (heroic), the depleted floor is the anti-armor chipper (≥
  fighter vs plate, ~43% vs soft), and the budget (not just CT) gates bursts. This **revises `14`** (the
  caster basic Attack: was physical reach-1, now the magic bolt). **Healing is RESOLVED**
  (`sim_magic_heal`): same spine **minus** element/Shell — `heal = base(MA) × heal_power × faith_c ×
  faith_t × G_m` — with the **target's Faith scaling healing received** (one Faith rule: devout = more
  magic in/out + more healing; atheist = resists nukes **and** healed less = a wash, heal-inefficient not
  un-healable). Same band [0.70,1.30] (gentler [0.80,1.20] in reserve); undead invert healing (designed
  property). **Magic Evade is RESOLVED** (structure): offensive magic is **resistible per target** —
  every unit, **including each one caught in an AoE** (almost all FFT magic is AoE), rolls Magic Evade
  independently; a **binary** evade (fits "randomness only in landing"). **Source = equipment + jobs
  naturally strong vs magic, NO universal floor** (unlike physical Dodge) → magic reliably lands on
  un-invested units (its identity as the answer to evasion/armor); **off the Speed axis** (B1); **capped
  below 100%** (never full immunity). The weapon bolt (`14`) is subject to it; status uses its own resist
  (`13`); healing is not evaded. **AoE×facing is RESOLVED** (cut): facing does **not** affect area magic
  — **magic owns the *position* axis, physical owns the *facing* axis** (`11`/`05`); magic's spatial
  richness is **spell shape (burst/line/cross) × clustering**, and Magic Evade is facing-independent.
  Still open: the **Magic Evade % values** (calibration); magnitudes — **`G_m`: `sim_magic_economy` is
  evidence toward ~3** (balances mage~fighter/battle;
  the placeholder 8 one-shots everyone), plus spell tiers (incl. the weapon bolt's + heal tiers),
  Faith / elemental-affinity / Shell / Zodiac band widths, reserve-cap value, MP pool/trickle, base(MA) curve.

## 6. Player-facing readability / presentation

The DCL adds real depth (damage types, reach, facing, depletion, three traits). How is this surfaced
so it's *legible* and not overwhelming? Preview must show the deterministic result; the matchup
(right/wrong tool), the defense ladder state, and facing bonuses all need clear UI. Legibility is a
stated principle (`00`) but the presentation design is not done. Bounded by engine/UI moddability.

## 7. Implementation tier / feasibility

Untouched here: which of this is data-only (Tier 1), which needs code hooks (Tier 2), and which is
infeasible on the current modding surface. The DCL is currently a *design*, deliberately unconstrained
by implementation cost during exploration. A feasibility pass against the `formula-balance` envelope
(`docs/formula-balance/00-envelope.md`) is a future step before any of this could be built.

**Known Tier-2 (code-hook) candidates flagged so far:** the **Weight → Move/Dodge curve** (`14` — a
computed penalty from summed Weight; per-piece Weight itself is data); the reaction inverse/flat
triggers (`07`); the status-infliction 3d6 contest and any reskinned-status behaviours (`13`).

## 8. Statuses & conditions (stun, knockdown, fear, taunt, …) — DESIGNED (see `13-statuses-and-reactions.md`)

The status system is now designed in **`13-statuses-and-reactions.md`** (3d6 resist contest;
mental→Brave / physical→base-HP / magical→Faith categories; stun & knockdown as Don't-Act / Don't-Move
reskins; fear = mirror of Berserk; taunt = directed-compulsion ideal with a 1-turn-Berserk fallback;
reactions are instinctive; courage/caution/neutral reaction taxonomy). What remains is
**detail/calibration** (roster mapping, durations, the stat→3d6 resist curves, control-status
frequency). The original framing is kept below for history.

A deliberate status system, applied by **explicit job skills and weapon properties** (legible cause
and effect) — *not* by HP thresholds or a hidden major-wound trigger (item 1). Statuses identified
so far:

- **Stun** (**physical** — resisted by **base-HP**, not Brave; `13`, validation A1) — lose your next
  action; can still move.
- **Knockdown** (physical, NEW — Marcelo, 2026-06-25) — the unit is downed; on its **next turn it
  cannot move (it only stands up), but it CAN still act/attack.** Loses positioning, keeps offense.
  Clean counterpart to stun: **stun costs the action, knockdown costs the move.**
- **Fear** (mental) — intimidation-driven; the home for the "fear" idea explored and rejected as an
  automatic *wound* mechanic. Resisted by **Brave**.
- **Taunt / provoke** (**mental, inverted** — resisted by **low** Brave; high Brave is *vulnerable*;
  `13`, validation B9) — skill-driven; pulls target aggression onto the taunter.

**Brave's "composure" half** (`07`) = the resist axis for the **will-override mental** statuses (fear,
charm, confuse, berserk) + charged-action interruption (high Brave shrugs off; low Brave succumbs).
**Stun/knockdown are physical** (base-HP, not Brave); **taunt is inverted** (low Brave resists, high
Brave is vulnerable) — see `13`. The Brave régua was **amended by validation B9** to add the taunt
vulnerability (the gear that closes the backliner min-max — `07`).

To grill (detail): exact list, durations, the resist mechanic (3d6 vs a Brave-derived number?),
whether physical **knockdown** resists via Brave or a physical stat, interaction with charge-time
interruption, and how statuses are cured/removed.

---

When an item here is resolved, move its decision into the relevant numbered document and strike it
from this register.
