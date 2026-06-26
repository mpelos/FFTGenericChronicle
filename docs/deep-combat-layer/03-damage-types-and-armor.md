# Damage Types and Armor

Status: Draft (structure locked; sharpness tuned to ~medium; constants open)
Date: 2026-06-25
Depends on: 02-damage-model.
Review: Pending.

## The four damage types

Every physical weapon deals one of four damage types. Each has a **wound multiplier** applied after
subtractive DR (`02-damage-model.md`):

| Type | GURPS analogue | Wound mult | Identity |
|------|----------------|-----------|----------|
| **Swing / cutting** | cutting | **×1.5** | High multiplier on flesh; slashing weapons (most swords, axes). |
| **Thrust / impaling** | impaling | **×2** | Highest multiplier; points (spears, rapiers, some thrust attacks). |
| **Crush** | crushing | **×1** | No bonus multiplier — but the answer to armor (see below). Maces, hammers, staves, fists. |
| **Missile / piercing** | piercing | **×1** | Ranged; pairs with armor-divisor penetration (bows, guns). |

The multiplier is on the **post-DR** number, so it rewards *getting through* the armor: a cutting
weapon that barely penetrates still gets ×1.5 on the sliver that lands, but if DR eats everything
the multiplier multiplies the penetration floor only.

> **The multipliers are NOT a power ranking — they ride different bases (validation B2).** Thrust's
> ×2 is not a strict upgrade over cutting's ×1.5. Per `02-damage-model.md`, `base(PA)` is **type-split**:
> thrust uses the GURPS **thrust** value `thr(PA)` (lower) and swing/cut & crush use the **swing** value
> `sw(PA)` (higher), with `thr < sw`. So the big ×2 lands on a *smaller* number and the ×1.5 on a
> *larger* one — on raw flesh they come out close (ST≈11: cut `sw≈4.5 ×1.5 ≈ 6.8` vs thrust
> `thr≈2.5 ×2 ≈ 5.0`). Thrust's ×2 only pulls ahead **after armor**, where doubling what got *through*
> DR matters — so thrust is the anti-armor/penetration specialist and cut is the raw-damage generalist
> (strong vs light targets). Reading the ×2 alone as "best" is the exact mistake that broke B2.

## The crush ↔ cutting tradeoff

Crush has the worst wound multiplier (×1) but is the answer to armor. To make that a *fair* trade
rather than a strict downgrade, **crush weapons carry larger raw modifiers** — roughly **~1.5× the
`wmod`** of an equivalent cutting/impaling weapon. So:

- **Cutting / impaling:** trade raw for multiplier (smaller `wmod`, bigger ×).
- **Crush:** trades multiplier for raw (bigger `wmod`, ×1) — and pairs that raw with low crush-DR
  on heavy armor, so the raw lands.

This keeps the four types genuinely different shapes rather than a power ranking.

## Armor classes and the response matrix

Armor provides **type-specific DR**. Different armor classes resist the four types differently. The
load-bearing case is heavy plate:

### The full-plate rule

Heavy plate has **high DR vs cutting and (to a degree) thrust, but low DR vs crush.** This is the
historical truth (you don't cut plate; you dent the person inside it) and it is the mechanical
engine that makes crush a real archetype. Against a fully-plated target:

- A **cutting** weapon scrapes — high cutting-DR eats most of it; the wound multiplier multiplies
  little. (Early sim: literally zero before the penetration floor was added.)
- A **crush** weapon connects — low crush-DR + crush's larger raw modifier means the blow lands,
  even though crush's ×1 multiplier is unimpressive. Crush is *the* tool vs plate.

## The answers to plate

Plate is meant to be a genuine wall — but a wall with **three** documented answers, so it's a puzzle
and not a hard counter:

1. **Crush (primary).** The full-plate rule above. Bring a hammer.
2. **Penetration / armor divisor (ranged).** The v0.2 `penetration` column is repurposed into a real
   **DR divisor**: a missile/gun with armor divisor halves (or better) the target's DR before
   subtraction. This rescues missile-vs-plate, giving ranged its own way through. Without it,
   piercing's ×1 + high plate-DR would make archers useless against knights.
3. **Brute force.** Enough raw `base(PA) + wmod` overwhelms even good DR. A strong enough attacker
   with the "wrong" type still does *something* meaningful — the matchup is a tax, not a lock.

## Matchup sharpness: MEDIUM

How much the right/wrong tool matters was an explicit dial. Marcelo chose **medium** (option b):

- **Right tool:** roughly **+35%** damage over neutral.
- **Wrong tool:** target is roughly **~2× tankier** (you do about half).

Medium is deliberately not "rock-paper-scissors hard" (where the wrong weapon is useless and you're
forced to swap) and not "barely matters" (where type is flavor). At medium, the right tool is a
clear, rewarding advantage and the wrong tool is a real handicap you can still fight through with
brute force or by changing the engagement (flank, focus-fire — see `05`).

## What was cut

**Body-type / injury-tolerance** (GURPS' unliving/homogenous/diffuse, etc.) was explicitly removed
by Marcelo. There is no creature-body-type axis multiplying or dividing wounds. The four damage
types and the armor matrix carry the entire "what hurts what" load.

## Open constants

Exact per-class DR values per type, the crush raw-modifier multiplier (~1.5×), the armor-divisor
values, and the precise sharpness tuning (validating the ~+35% / ~2× targets) are deferred
calibration — `12-open-questions.md`.
