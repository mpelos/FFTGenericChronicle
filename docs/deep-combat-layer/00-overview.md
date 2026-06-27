# Deep Combat Layer — Overview

Status: Draft (high-level skeleton complete; calibration and a few systems open)
Date: 2026-06-25
Review: Pending — design-in-progress, not yet ratified.

## What this is

The **Deep Combat Layer (DCL)** is a clean-sheet combat model for FFT Generic Chronicle, strongly
inspired by GURPS 4e and adapted, as far as the FFT engine and feel allow, into a tactical-RPG
shape. It was explored on 2026-06-25 in a long interrogation session seeded by a GURPS discussion.

The DCL is a **separate track** — "another perspective" on combat — **not** an evolution of the
validated v0.2 formula work in `docs/formula-balance/`. It does not inherit that track's
decisions and is not bound by them. Where the two disagree (most visibly: the v0.2 policy is
explicitly **C-bounded / multiplicative with no subtractive DR**, while the DCL is built on
**subtractive DR**), the disagreement is intentional. These are two different answers to the same
question, kept side by side.

Nothing here is committed to the mod. This folder is the detailed record of the DCL design so far,
so that every decision taken in the session is written down and recoverable.

## Why GURPS, and what we kept

We lean on GURPS for one overriding reason: its combat formulas are **rich and already
well-balanced** — the product of decades of refinement and play. Rather than reinvent balance from
scratch, the DCL adapts a system that has largely already solved these problems, and bends it to
FFT's feel. Concretely, GURPS gives combat three things FFT's vanilla math does not:

1. **Deterministic, legible damage.** Damage is computed, not rolled (see `02-damage-model.md`).
   What the preview shows is what happens. Randomness is confined to *whether* you hit and
   *whether* the defender turns the blow aside — never to *how much* a confirmed hit does.
2. **A real armor/weapon matchup.** Damage type vs armor type matters: a cutting edge, a thrusting
   point, a crushing mass, and a missile each interact differently with what the target wears
   (see `03-damage-types-and-armor.md`). This is the engine that makes "bring the right tool"
   a real decision instead of flavor.
3. **Active defenses and positioning as the core loop.** Two independent rolls (hit, then defend),
   facing, reach, and guard depletion turn a fight into a spatial puzzle rather than a damage-race
   (see `04-hit-and-defense.md`, `05-facing-and-positioning.md`, `06-reach.md`).

What we deliberately did **not** import from GURPS: body-type / injury-tolerance distinctions
(cut by Marcelo), and the literal GURPS number scale (we re-range and bridge to FFT's HP and PA
magnitudes rather than using raw thrust/swing values).

## Guiding principles

- **Balance through contextual differentiation — the core philosophy.** Every option, and every
  weapon type especially, earns its place by being *best in some context and worse in others*; **no
  weapon, trait, or build is strictly better than another.** An advantage on one axis is always paid
  for on another — a bigger hit costs defense, reach costs point-blank safety, penetration costs raw
  damage, scaling-with-a-stat costs a low floor. The deliverable is a roster of genuinely different,
  *situational* choices, never a power ranking. This is the lens every weapon and equipment decision
  is checked against. (It is *why* we adapt GURPS at all — see above: rich, pre-balanced formulas.)
- **Every existing FFT attribute is used or replaced.** This is a hard constraint: nothing in the
  character menu may be dead weight. The full audit lives in `01-attribute-map.md`.
- **Deterministic damage, random contest.** Preview must equal result.
- **Two-sided traits — two sliders and a matchup web.** Brave and Faith are each a permanent
  personal "régua" (slider) with a real upside *and* a real downside — no universally-best setting.
  **Zodiac is the third axis but a *relational* one** (attacker × target sign compatibility, `09`),
  not a personal slider; by the grid's symmetry no sign is stronger than another, so it is still
  two-sided — a web of matchups rather than a régua. See `07`–`09`.
- **Legibility over hidden math.** Vanilla's Zodiac compatibility multiplier is **kept but surfaced
  and softened** (shown in the preview, on a much subtler band — `09`); opaque evade stacking is
  replaced by transparent, readable systems.
- **No new equipment.** Per the project-wide rule, the DCL re-uses existing items; it never adds
  new ones. Weapons gain meaning through *type*, *reach*, and *modifier*, not through new SKUs.

## Document map

| File | Topic |
|------|-------|
| `00-overview.md` | This document. Scope, philosophy, map. |
| `01-attribute-map.md` | The use-or-replace constraint and the full attribute audit. |
| `02-damage-model.md` | The deterministic damage pipeline (model "d"). |
| `03-damage-types-and-armor.md` | swing/thrust/crush/missile, wound multipliers, the plate problem. |
| `04-hit-and-defense.md` | Two 3d6 rolls, crit/fumble, active defenses, depletion, reset-on-turn. |
| `05-facing-and-positioning.md` | Front/side/back and the counterplay triangle. |
| `06-reach.md` | Reach identity: outrange, escape-counter, point-blank weakness, stop-hit. |
| `07-trait-brave.md` | Brave — physical-aggression temperament (validated). |
| `08-trait-faith.md` | Faith — magic/spiritual temperament. |
| `09-trait-zodiac.md` | Zodiac — attacker × target sign compatibility (subtle damage + hit modifier). |
| `10-weapon-skill.md` | Weapon skill per family, grown by job level. |
| `11-magic.md` | Magic on its own FFT-native axis. |
| `12-open-questions.md` | Living register of resolved/open items (calibration, rosters, presentation, feasibility). |
| `13-statuses-and-reactions.md` | Status infliction (3d6 contest), categories, the new statuses (stun, knockdown, fear, taunt), and the Brave reaction taxonomy. |
| `14-equipment.md` | Weapons and gear in DCL terms: the dial template, the off-hand model, and the weapon-family tables (blades, crush, reach, magic, ranged). In progress. |
| `15-job-authoring.md` | How to author a job on the DCL: the five laws (J1–J5), the fantasy→axes pipeline, the validation rubric. |

## Status of the skeleton

The high-level skeleton is **complete**: every FFT attribute is homed, and every major system has a
decided shape. As of 2026-06-25 the wounds/death model (heroic FFT), the status approach
(skill-driven, Brave-resisted), counters (vanilla reactions), multi-hit (guard-shredder), fumble
(just a miss), and charged-action interruption (no damage-interrupt) are all resolved. What remains
is **detail, not direction**: numeric calibration, the per-job status / reaction / skill rosters, the
weapon-family numbers, player-facing presentation, and an implementation-feasibility pass. Open items
are tracked in `12-open-questions.md`.
