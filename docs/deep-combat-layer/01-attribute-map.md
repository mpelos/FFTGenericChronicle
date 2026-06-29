# Attribute Map — Use or Replace

Status: Draft
Date: 2026-06-25
Depends on: 00-overview.
Review: Pending.

## The constraint

> **HARD CONSTRAINT (Marcelo, 2026-06-25):** every existing FFT character-menu attribute must be
> either *used* or *replaced*. Nothing in the character menu may be useless. The player must never
> see a stat that does nothing.

This is the spine of the DCL. The model was not designed first and checked against the attributes
afterward — the requirement that every attribute earn its place actively shaped the design. The
audit below is the proof that the constraint is satisfied: every line has a home.

## The audit

| FFT attribute | Role in the DCL | Where detailed |
|---------------|-----------------|----------------|
| **PA** (Physical Attack) | **ST (Strength) — base only.** PA is the character's strength input to the GURPS thrust/swing damage table. **Base PA excludes the weapon's PA bonus** (the weapon contributes separately as a flat damage modifier; counting both would double-count). | `02-damage-model.md` |
| **HP** | Hit points, **decoupled from ST**; its own FFT pool (existing curves survive). **Double-homed: Base HP = the GURPS HT analogue** — innate physical resilience that resists *physical* statuses (poison, disease, stun, knockdown). Uses **base HP only** (excludes armor/equipment HP bonuses) → resilience is intrinsic, not bought with gear (the parallel to ST = base PA). | `02`, `12` (death), status |
| **Speed** | **Turn frequency (CT).** Drives how often a unit acts. A *residual* emergent defense remains because guard resets on your own turn (a fast unit refreshes its depleted parry/block sooner) — open follow-up. **No longer feeds Dodge** (validation B1, 2026-06-26 — see note below). | `04-hit-and-defense.md` |
| **Move / Jump** | Positioning resources — unchanged in spirit, but far more valuable because facing, flanking, and reach now decide fights. | `05`, `06` |
| **C-Ev** (Class Evade) | **Primary source of Dodge** (the innate, non-depleting defense floor), together with equipment. After validation B1, **Dodge = C-Ev + equipment** (Speed removed) → evasion is a real build axis (evasive class + light armor, paid in DR) and the **survival mechanism for light clothes-&-suits builds**. | `04` |
| **S-Ev** (Shield Evade) | Becomes **Block** — a strong but **depleting** active defense granted by shields. | `04` |
| **W-Ev** (Weapon Evade) | Becomes **Parry** — a strong but **depleting** active defense granted by the weapon (≈ skill/2 + 3). | `04` |
| **WP** (Weapon Power) | The weapon's **flat additive damage modifier** (scales fast to large endgame numbers). Pairs with the weapon's damage *type* and *reach*. | `02`, `03` |
| **Weapon range** | **Reach.** A weapon's range becomes its GURPS-style reach: outrange, escape-counter, point-blank weakness. | `06-reach.md` |
| **Job Level** | **Weapon-skill growth.** Skill in a weapon family rises with job level (per-job, per-level tables) — *not* via JP. | `10-weapon-skill.md` |
| **JP** | Stays the **ability-purchase** currency (learning abilities), as in vanilla. It does **not** drive skill growth. | `10`, `11` |
| **MA** (Magic Attack) | Magic output, on the **magic axis** (FFT-native). | `11-magic.md` |
| **MP** | Spell resource, FFT-native. | `11` |
| **Faith** | **Magic/spiritual temperament** — permanent two-sided trait; scales magic output *and* magic vulnerability, floor 0.60. | `08-trait-faith.md` |
| **Magic Evade** | **Per-target magic resist** — every target (incl. each unit caught in an AoE) rolls to evade an offensive spell. Built from **equipment + anti-magic jobs**, no universal floor; capped below 100%. | `11` |
| **Brave** | **Physical-aggression temperament** + the **GURPS Will analogue** — permanent two-sided trait: physical offense / courage reactions / **mental resilience** (Will: resists fear, taunt, charm, confuse, berserk) vs active-defense penalty. Does **not** touch magic. | `07-trait-brave.md` |
| **Zodiac sign** | **Sign compatibility** — FFT's attacker × target matchup, **kept** but **surfaced** and on a **much subtler band** (a relational tilt to damage + hit, not a personal slider; not an elemental affinity). | `09-trait-zodiac.md` |

## The three permanent traits — two sliders and a matchup web

Brave, Faith, and Zodiac form a deliberate trio of **permanent, transparent, two-sided** axes — but
they are **not** all the same *kind* of axis:

- **Brave = physical temperament** (the body / aggression) — a personal **slider** (`07`).
- **Faith = magic temperament** (the spirit / the arcane) — a personal **slider** (`08`).
- **Zodiac = sign compatibility** (the elemental/astral matchup) — a **relational** axis (attacker ×
  target), not a personal slider (`09`).

Brave and Faith each have a clean upside and downside with **no universally-correct value**. Zodiac
keeps the same "two-sided, none-dominant" spirit by a different route: by the grid's symmetry every
sign has the identical distribution of good/bad matchups, so **no sign is stronger than another** —
each just wins and loses different pairings. The same "should I lean into this?" question recurs
across three dimensions; for Zodiac the question is *relational* ("who am I good against?"). All
three are **surfaced to the player** — including Zodiac, whose vanilla version was hidden (`09`).

## Note on Speed (amended — validation B1, 2026-06-26)

The original design double-homed Speed (**Dodge floor + turn frequency**). The validation found this
made Speed **strictly dominant** — a fast unit got more turns *and* a higher Dodge *and* faster
guard-refresh, with no hard tradeoff (a P9 violation; `validation/report.md` B1, sim-confirmed with
Speed isolated from Brave). **Fix: Speed is decoupled from Dodge.**

- **Speed now drives turn frequency (CT) only.** `Dodge = C-Ev (class) + equipment`; Speed is out of
  the Dodge formula. Evasion becomes a real build axis (evasive class + light armor, paid for in
  DR/protection) — the **survival mechanism for light clothes-&-suits builds**, not a free rider on Speed.
  A fast unit still acts more often, but is no longer harder to hit for free.
- The **guard-refresh tie is KEPT** (depleting Parry/Block fully reset on the unit's own turn — a fast
  unit refreshes sooner, so focus-fire cracks *slow* tanks; the intended "tempo = hold the line"
  identity; **not** a partial refresh). It is balanced not by a refresh-rule change but at the
  **job-design level**: the agile jobs (Thief/Ninja/Archer) are fast *because* their per-hit offense
  is set correspondingly lower — each job's Speed is calibrated against its offensive profile (`12`).
  A fast + high-offense + high-mitigation package is therefore never a buildable job. See
  `04-hit-and-defense.md`.

Speed remains deliberately **frequency, not offense.** It multiplies *how often* a unit acts; it never
feeds weapon damage. A *finesse* path (light-weapon damage scaling off Speed, as in D&D/GURPS) was
considered and **rejected**: those systems are safe because their agility stat grants no extra turns,
whereas our Speed *does* (CT) — so Speed→damage would compound to ~Speed² and would turn PA from a
*complement* (magnitude) into a dead *substitute*. So **PA = magnitude, Speed = frequency**, both
wanted; the breadth of an "agile" playstyle comes from **job skills** (tempo / mobility synergies),
not from an attribute-scaling axis. Weapon `+Speed` grants (e.g. the knife) are fine as **stat
sticks**, kept modest — they are distinct from finesse.
