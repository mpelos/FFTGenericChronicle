# Statuses and Reactions

Status: Draft (design complete; roster mapping & numbers open)
Date: 2026-06-25
Depends on: 01-attribute-map, 04-hit-and-defense, 07-trait-brave, 11-magic.
Review: Pending.

## Scope

The DCL keeps FFT's existing status roster as its base and adds a small set of new statuses, a
single resistance mechanic (a 3d6 contest), and a Brave-driven reaction taxonomy. This document
specifies how statuses land and resist, the new statuses, the repurposed ones, and how reactions
relate to Brave. Per-status durations, the full roster mapping, and exact numbers are calibration
(see the end of this document and `12`).

## Infliction and resistance — a 3d6 contest

When a status-inflicting skill connects, the target rolls **3d6 against a resistance number**; on a
pass it resists. This is the same 3d6 language as hit/defense (`04`) — one consistent, legible system.

- The resistance number comes from the **stat of the status's category** (below).
- **Equipment immunity** still auto-resists (FFT-native), as do the usual immunities.
- Implementation: this replaces FFT's native %-based infliction, so it **needs a code hook**
  (feasibility, `12` item 7). The mapping from a stat (Brave, base HP) to a 3d6-scale resistance
  number is calibration.

## The GURPS resilience mapping

GURPS splits resilience into two attributes — **HT** (physical) and **Will** (mental). The DCL maps
them onto existing FFT attributes, completing a clean four-attribute parallel:

| GURPS | DCL | Governs / resists |
|-------|-----|-------------------|
| **ST** | **PA** (base) | melee damage input (`02`) |
| **HT** | **HP** (base) | physical resilience — poison, disease, stun, knockdown |
| **Will** | **Brave** | mental resilience — fear, taunt, charm, confuse, berserk |
| (arcane) | **Faith** | magical statuses |

Two deliberate points:

- **HT = base HP, not total HP.** Physical-status resistance uses **base HP only** (excludes armor /
  equipment HP bonuses) — resilience is innate constitution, not bought with gear. This is the exact
  parallel to **ST = base PA** (`02`). Consequence: **armor gives DR (and maybe an HP pool) but does
  NOT grant status resistance.** A robust unit shrugs off poison naked; a frail mage in heavy armor
  does not.
- **Brave = Will.** The "composure" half of Brave (`07`) is the *will* function — resisting mental
  statuses — not physical toughness.

A mage (low Brave + low base HP) is therefore disruptable on **both** axes — coherent glass-cannon
counterplay, and a real cost of the low-Brave/low-HP build.

## Status categories

| Category | Resists with | Statuses |
|----------|--------------|----------|
| **Mental / will** | **Brave** | Fear, Taunt (new) · Charm, Confuse, Berserk (FFT, **moved here**) |
| **Physical / body** | **base HP** | Stun, Knockdown (new) · Poison, Disease |
| **Magical** | Faith / MA | Sleep, Petrify, Frog, Stop, Slow, Don't Act/Move, Death Sentence … (FFT logic kept) |

Moving FFT's mind-control statuses (Charm, Confuse, Berserk) onto the **Brave** axis is intentional:
control of the mind is resisted by willpower/courage, not faith. It enriches Brave's composure half
(high Brave = mentally tough) and makes the loss-of-control statuses target low-Brave units naturally.

## New statuses

### Stun — physical (base-HP resisted)

A brief daze. **You lose your action** (no attack / ability) on your next turn but **can still move**
(reposition or retreat). Duration ~1 turn (clears after that turn). No defense penalty (kept clean).

**Implementation:** a reskin of FFT **"Don't Act"** — critical (kneeling) animation, keeps the **DA**
status balloon. Cheap (reuses a native status; the balloon reading "DA" is the only cosmetic cost).

### Knockdown — physical (base-HP resisted)

You are downed. While waiting for your turn you lie prone; **on your turn you stand up but cannot
move** that turn — though you **can still act** (attack). Duration ~1 turn. The complement of stun:
**stun costs the action, knockdown costs the move.**

**Implementation:** a reskin of FFT **"Don't Move"** — dead (lying) animation, keeps the **DM**
status balloon. Cheap, same as stun.

### Fear — mental (Brave resisted) · the mirror of Berserk

The unit is too frightened to fight: it **auto-flees from the enemy** (forced movement away) and
**cannot use any action that targets an enemy** (no offensive actions). It **can** still use
self/ally/item and defensive actions, and it **moves** (the flight is the movement).

- **Reactions stay normal — including offensive ones.** A cornered, feared unit still counters by
  instinct. This follows the general rule below.
- It is the clean **mirror of Berserk**: Berserk = forced aggression (auto-attack); Fear = forced
  cowardice (auto-flee + no offense).
- The goal is to make the player *feel* the unit is afraid — not to fully neutralise it.
- Loss-of-control here is acceptable (even charming) **because the status is uncommon**; it remains
  short, curable, and Brave-resistable. Rarity is the guardrail.

**Implementation:** the flee uses FFT's existing flee AI (pathing jank around cliffs/corners is
accepted); the "no action targeting an enemy" filter needs a hook.

### Taunt — mental (Brave resisted) · directed aggression

The tank's tool: pull an enemy's aggression onto yourself, protecting the backline. Distinct from
Berserk (chaotic, nearest target) — Taunt is **directed** aggression (target = the taunter).

- **Ideal (the target we want):** a directed compulsion — the taunted unit is forced to attack the
  **taunter** specifically (approaching if needed). This is where we'd like to get to.
- **Fallback (shippable):** a **1-turn Berserk** (reuse the native Berserk status). It degrades to
  "auto-attack nearest", but **approximates the ideal when the taunter is the nearest enemy** to the
  taunted unit — so positioning recovers part of the effect.
- Reactions stay normal (general rule below).

**Implementation:** the ideal requires understanding/modifying the game's targeting AI (out of scope
for now); the fallback is a cheap native-Berserk reskin limited to one turn.

### Interrupt — a skill effect (Brave resisted)

Cancels a target's **charged action** (a queued spell/ability). This is **ability-driven only** —
damage never interrupts a charge (`11`, `04`). Brave (composure) resists it. It is the dedicated
anti-caster tool, paid for explicitly rather than handed out by any hit.

## Reactions are instinctive (general rule)

**Mental statuses affect voluntary actions, not reflexes.** A unit under fear / taunt / charm /
confuse still triggers its **reactions** (Counter, etc.) by instinct. This keeps the mental statuses
about *what the unit chooses to do*, not about switching it off, and gives the cornered/feared unit a
last instinctive bite.

## Reaction categories (job-defined)

Reactions are not uniformly gated by Brave% (that would leave low-Brave units with a dead reaction
slot). There are three flavours, and the **job defines** which it grants (full treatment in `07`):

- **Courage** (Counter, ripostes) — fire more at **high** Brave (chance ∝ Brave%).
- **Caution** (guard, evade-boost, Blade-Grasp-like) — fire more at **low** Brave (∝ inverse Brave).
- **Neutral** (utility) — fire regardless of Brave.

So the Reaction slot is always live; Brave only shifts *which* reactions a build excels with — the
two-sided design applied to reactions.

## Implementation notes (feasibility)

- **Cheap (reskins of native statuses):** Stun (Don't Act), Knockdown (Don't Move), Taunt fallback
  (1-turn Berserk). Cosmetic cost only (balloons read DA/DM; flee pathing jank).
- **Needs a code hook:** the 3d6 resistance contest (replaces FFT's % infliction), the "no action
  targeting an enemy" filter (Fear), inverse/flat reaction triggers (caution/neutral), and the
  directed-taunt AI (the Taunt *ideal*).
- Tracked under `12` item 7 (feasibility pass).

## Open / calibration

- Map every FFT status into a category and confirm the moved ones (Charm/Confuse/Berserk → Brave).
- Per-status **durations**.
- The **Brave → 3d6 resist number** and **base HP → 3d6 resist number** curves.
- **Frequency / availability** of the control statuses (Fear, Taunt, Charm, etc.) — few jobs, real
  cost — so they stay characterful and never oppressive.
- Per-job reaction rosters and trigger curves (with `07`, `10`).
