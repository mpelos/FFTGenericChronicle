# Damage Model

Status: Draft (structure locked; constants open)
Date: 2026-06-25
Depends on: 00-overview, 01-attribute-map.
Review: Pending.

## Principle: deterministic damage

Damage is **computed, never rolled**. There are no damage dice. The combat preview must equal the
result. All randomness in the DCL lives in two places only: the **hit roll** and the **active-defense
roll** (`04-hit-and-defense.md`). Once a hit is confirmed and not defended, its damage is exact.

This is the GURPS feel inverted for tactics: GURPS rolls damage dice, but for a tactical game where
the player plans around a preview number, a swingy confirmed hit is frustrating. We keep GURPS'
*structure* (a strength→damage table, damage types, subtractive armor) and drop its damage RNG.

## The pipeline (model "d")

For a confirmed, undefended hit:

```
injury = max( pen_floor ,  max(0, [ base(PA) + wmod ] − DR_type) )  × wound_mult × G
```

Term by term:

| Term | Meaning |
|------|---------|
| `base(PA)` | The GURPS-style thrust/swing value derived from the attacker's **base PA** (ST), via a re-ranged table — see below. **Base PA only**: the weapon's PA bonus is *not* included here. |
| `wmod` | The **weapon's flat additive modifier** (from WP). Scales fast to large endgame numbers — GURPS *structure*, FFT *pace*, not literal +1/+2 steps. |
| `DR_type` | The target's **subtractive Damage Resistance** against this damage *type*. Different armor resists different types differently (`03`). |
| `pen_floor` | **Penetration floor.** A minimum fraction of the pre-mult raw damage always gets through (≈15–33%), so even a bad matchup chips. |
| `wound_mult` | The damage-type **wound multiplier** (swing ×1.5, thrust ×2, crush ×1, missile ×1) — see `03`. |
| `G` | A global **bridge constant** mapping GURPS-native magnitudes onto FFT's HP scale. |

### Why base PA, not weapon PA

Weapons raise PA in vanilla. If both `base(PA)` *and* `wmod` counted the weapon, the weapon would
be added twice. So **ST = base PA** (the character's own strength), and the weapon contributes
exactly once, as `wmod` (plus its type, reach, and parry bonus). This was an explicit decision.

### PA → ST re-range

Raw FFT PA values, dropped into the literal GURPS thrust/swing table, would sit near the bottom of
that table (where thrust/swing are tiny), making subtractive DR swallow everything. So PA is
**re-ranged** into a usable ST band before the table lookup — roughly `ST ≈ PA + 4` (exact offset
is a calibration knob, `12`). This keeps the table responsive at FFT's PA magnitudes.

### Subtractive DR + the penetration floor

DR is **subtractive** (GURPS-style): it is removed from raw damage *before* the wound multiplier.
This is the sharpest departure from the v0.2 `formula-balance` track, which is multiplicative and
C-bounded with no subtractive DR. Subtractive DR is what makes armor *type* matter and what makes
the "right tool" matchup real.

The danger of pure subtractive DR is the **chip-zero wall**: a weak weapon vs heavy armor does
literally zero (this actually happened in early simulation — sword vs plate = 0, a ZeroDivisionError
in the diagnostics that was itself the finding). Three mechanisms tame it:

1. **Penetration floor (`pen_floor`):** a minimum slice of raw damage always lands (≈15–33%). This
   *helps early game* (when DR can otherwise dominate) and *costs nothing late* (when raw damage
   already exceeds the floor). It guarantees no attack is ever fully nullified.
2. **PA→ST re-range** (above): keeps base damage off the floor of the table.
3. **DR scales with the weapon-mod inflation:** as `wmod` inflates into big endgame numbers, DR
   scales alongside, so armor stays relevant late instead of becoming a rounding error.

### G — the bridge constant

GURPS-native injury numbers are small (single/low-double digits). FFT HP runs to hundreds. `G`
multiplies the GURPS-scale injury up to the FFT HP scale so the model produces FFT-feeling damage.
`G` is a pure calibration constant (`12-open-questions.md`).

## What lives elsewhere

- The **damage types**, their **wound multipliers**, the **crush↔cutting raw/mult tradeoff**, the
  **armor classes**, the **full-plate rule**, and the **answers to plate** (crush, armor-divisor
  penetration, brute force) are all in `03-damage-types-and-armor.md`.
- **Whether** the hit lands and **whether** it is defended is in `04-hit-and-defense.md`.
- **Magic damage does not use this pipeline** — it runs on its own FFT-native axis and does not pass
  through physical DR or wound multipliers (`11-magic.md`).

## Open constants

`G`, the PA→ST offset, the exact `pen_floor` fraction, the DR-scaling curve, and the per-weapon
`wmod` values are all deferred calibration (Marcelo: "deixa a calibragem fina para depois"). They
are tracked in `12-open-questions.md`.
