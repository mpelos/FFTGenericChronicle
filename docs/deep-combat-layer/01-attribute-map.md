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
| **Speed** | **Double duty.** (1) Feeds **Dodge**, the always-on non-depleting active-defense floor. (2) Drives **CT/turn frequency** — which becomes *emergent defense* because guard resets on your own turn (a fast unit refreshes its depleted parry/block sooner). | `04-hit-and-defense.md` |
| **Move / Jump** | Positioning resources — unchanged in spirit, but far more valuable because facing, flanking, and reach now decide fights. | `05`, `06` |
| **C-Ev** (Class Evade) | Folds into **Dodge** (the innate, non-depleting defense floor). | `04` |
| **S-Ev** (Shield Evade) | Becomes **Block** — a strong but **depleting** active defense granted by shields. | `04` |
| **W-Ev** (Weapon Evade) | Becomes **Parry** — a strong but **depleting** active defense granted by the weapon (≈ skill/2 + 3). | `04` |
| **WP** (Weapon Power) | The weapon's **flat additive damage modifier** (scales fast to large endgame numbers). Pairs with the weapon's damage *type* and *reach*. | `02`, `03` |
| **Weapon range** | **Reach.** A weapon's range becomes its GURPS-style reach: outrange, escape-counter, point-blank weakness. | `06-reach.md` |
| **Job Level** | **Weapon-skill growth.** Skill in a weapon family rises with job level (per-job, per-level tables) — *not* via JP. | `10-weapon-skill.md` |
| **JP** | Stays the **ability-purchase** currency (learning abilities), as in vanilla. It does **not** drive skill growth. | `10`, `11` |
| **MA** (Magic Attack) | Magic output, on the **magic axis** (FFT-native). | `11-magic.md` |
| **MP** | Spell resource, FFT-native. | `11` |
| **Faith** | **Magic/spiritual temperament** — permanent two-sided trait; scales magic output *and* magic vulnerability, floor 0.60. | `08-trait-faith.md` |
| **Magic Evade** | **Magic dodge** — lets targeted/bolt spells be evaded. | `04`, `11` |
| **Brave** | **Physical-aggression temperament** + the **GURPS Will analogue** — permanent two-sided trait: physical offense / courage reactions / **mental resilience** (Will: resists fear, taunt, charm, confuse, berserk) vs active-defense penalty. Does **not** touch magic. | `07-trait-brave.md` |
| **Zodiac sign** | **Elemental temperament** — repurposed from the hidden compatibility multiplier into a transparent innate elemental affinity/weakness. | `09-trait-zodiac.md` |

## The three permanent traits

Brave, Faith, and Zodiac form a deliberate trio of **permanent, transparent, two-sided personal
axes**:

- **Brave = physical temperament** (the body / aggression).
- **Faith = magic temperament** (the spirit / the arcane).
- **Zodiac = elemental temperament** (the innate elemental leaning).

Each has a clean upside and a clean downside; none has a universally-correct value. This symmetry
is a design goal, not a coincidence — it is what lets the same "should I push this slider?" question
recur across three different combat dimensions. The hidden vanilla Zodiac multiplier is gone; all
three are surfaced to the player.

## Note on double-homed Speed

Speed is the one attribute carrying two distinct loads (Dodge floor + turn frequency). This is
intentional and load-bearing: it is what ties *tempo* to *defense*. A slow, heavily-armored unit
that gets focus-fired will see its depleting defenses (Parry/Block) collapse before its next turn
refreshes them, while its Dodge floor remains — so Speed indirectly governs how long a unit can
hold a line under pressure. See `04-hit-and-defense.md` for the depletion/reset mechanic that makes
this matter.

Speed is deliberately **frequency + defense, not offense.** It multiplies *how often* a unit acts
(and how well it evades); it never feeds weapon damage. A *finesse* path (light-weapon damage scaling
off Speed, as in D&D/GURPS) was considered and **rejected**: those systems are safe because their
agility stat grants no extra turns, whereas our Speed *does* (CT) — so Speed→damage would compound to
~Speed² and would turn PA from a *complement* (magnitude) into a dead *substitute*. So **PA =
magnitude, Speed = frequency**, both wanted; the breadth of an "agile" playstyle comes from **job
skills** (tempo / mobility synergies), not from an attribute-scaling axis. Weapon `+Speed` grants
(e.g. the knife) are fine as **stat sticks**, kept modest — they are distinct from finesse.
