# Formula Balance Principles

Status: Accepted
Date: 2026-06-20
Depends on: `docs/formula-balance/00-envelope.md`
Review: Approved by Claude on 2026-06-20.

## Primary Goal

Generic Chronicle's first balance goal is to make every weapon family meaningfully useful in
endgame while preserving FFT's feel.

The player should want to build strong late-game units around different weapon families because
those families have different strengths, not because every weapon has been flattened into the
same damage profile.

## Working Diagnosis

The current design hypothesis is that late-game FFT weapon diversity collapses because too many
strong choices converge around sword access and sword-like damage. Likely contributors include:

- stable `PA * WP` style damage on common sword-like attacks;
- high WP on late-game sword paths, which scales cleanly with PA;
- Two-Hand style scaling that effectively amplifies WP and therefore rewards already-strong
  PA-based sword builds;
- sword damage that is often independent of Brave/Faith, avoiding variance or stat dependence
  that constrains other routes;
- strong job and skill access tied to sword users;
- strong late-game equipment support for sword paths;
- swordskill-style routines that scale cleanly and remain broadly useful;
- rival families such as spears, axes, bows, guns, knives, and staves having narrower, less
  reliable, more positional, or less supported formula identities.

This diagnosis is not yet a final numeric proof. It must be checked against IVC data, WotL
references where IVC values are missing, and actual endgame build tests.

Confidence: medium
Dependency: Mixed
Proof state: needs baseline dump and playtest

## Non-Goals

- Do not turn FFT into a tabletop RPG simulation.
- Do not copy GURPS or any other external system literally.
- Do not make the game harder as the purpose of this phase.
- Do not balance individual item progression in this phase.
- Do not finalize numeric formulas before the relevant technical path is proven.

## Core Principles

### 1. Preserve FFT Feel

The final result should feel like a better-balanced FFT, not a different combat engine wearing
FFT assets.

Expected implications:

- Damage ranges should remain emotionally familiar to FFT players.
- Combat pacing should remain recognizable.
- PA, MA, Speed, Brave, Faith, CT, equipment, job identity, range, height, status, and elements
  should remain part of the design language.
- Complexity may exist under the hood, but the player's read should stay clear.

Observable checks:

- No new resource economy is introduced just to support weapon formulas.
- CT/turn economy remains recognizably FFT unless a later approved doc explicitly changes it.
- Damage magnitudes stay in the same broad order of magnitude as comparable FFT actions unless
  a drift is intentional and documented.
- The formula vocabulary should still read through FFT-native concepts such as WP, PA, MA,
  Speed, Brave, Faith, range, height, element, status, and CT.

Confidence: high
Dependency: Mixed
Proof state: needs playtest

### 2. Weapon Families Need Distinct Reasons To Exist

The unit of balance for this phase is the weapon family, not the individual item.

Every weapon family should have at least one strong endgame reason to be chosen. The reason can
come from damage shape, consistency, scaling stat, range, risk, status interaction, positioning,
magic interaction, equipment synergy, or another FFT-native axis.

The reason should be legible in play. A player should not need to solve the formula to understand
why a family feels useful.

Confidence: high
Dependency: Mixed
Proof state: needs playtest

### 3. Swords Stay Good, But Stop Being Universal

The goal is not to punish swords. Swords can remain familiar, stable, and broadly useful. The
goal is to make the surrounding weapon ecosystem strong enough that swords stop being the
default best answer to most physical damage problems.

This should emerge from better formulas and family identities, not from an isolated anti-sword
nerf agenda.

Confidence: high
Dependency: Mixed
Proof state: needs playtest

### 4. External References Are Tools, Not Laws

GURPS is a useful reference because its damage model distinguishes ideas such as thrust, swing,
penetration, and damage type. Those ideas may help Generic Chronicle escape shallow `stat * WP`
balance.

But GURPS is not a rule source for this mod. A borrowed idea is valid only if it improves weapon
balance while preserving FFT feel.

This is especially important for penetration and armor concepts. The current envelope does not
prove point-by-point armor DR as a native FFT data lever, so penetration remains a hypothesis
until mapped to Tier 1 or proven through Tier 2.

Confidence: high
Dependency: Research-only
Proof state: needs design review

### 5. Tier-1 First, Tier-2 When Worth Proving

The baseline design target is Tier 1 data-only implementation. Tier 2 code hooks are allowed
when they materially improve the final goal, but documents must name the dependency clearly.

Design should degrade gracefully:

- The ideal identity describes what the family should become.
- The Tier-1 fallback describes what can be approximated with current data.
- The Tier-2 dependency describes what requires custom code.

Confidence: medium
Dependency: Mixed
Proof state: needs proof patch and hook research

### 6. Metrics Come Early, Thresholds Stay Provisional

We need evaluation criteria before numeric formulas, but early thresholds are calibration aids,
not hard rules.

Initial metrics:

- Family viability: how many weapon families can anchor a credible endgame build.
- Dominance risk: whether one family is the best general answer too often.
- Damage scale: whether normal, strong, and exceptional attacks remain in FFT-like ranges.
- Role clarity: whether a player can explain why a family is useful after playing it.
- Magic coexistence: whether physical weapons and magic both remain desirable.
- Build diversity: whether strong parties naturally include different equipment profiles.
- Technical feasibility: whether the family identity is Tier-1, Tier-2, mixed, or still
  speculative.
- Baseline drift: how far proposed formulas move representative endgame damage away from IVC or
  WotL reference ranges.

Provisional targets for later calibration:

- No weapon family should be the universal best answer across most late-game physical roles.
- Multiple families should be able to anchor strong endgame units.
- Any large damage-scale drift from vanilla FFT should be intentional and documented.

Confidence: medium
Dependency: Mixed
Proof state: needs playtest

## First Accepted Direction To Seek From Review

The next review should confirm or reject this operating principle:

> Tier 1 is the mandatory baseline; Tier 2 is an extension path under proof; every family identity
> should be written with ideal identity plus Tier-1 fallback when possible.

If approved, the next document should combine weapon-family taxonomy with formula feasibility,
because family identity and available FFT formula routines are tightly coupled.

Before that taxonomy becomes final, the project should capture a weapon-data baseline from
`ItemWeaponData.xml` with at least weapon family, formula id, power, range, element, evasion, and
attack flags.
