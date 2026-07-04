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

## Principle: every magnitude is formula-derived (only items are flat)

All ability **damage and healing** is **computed from a formula that scales with the actor's stats** —
physical via the pipeline below (`base(PA)`/skill × `wmod` × … × `G`), magic via the Faith-scaled axis
(`11`), healing via MA/Faith or a fraction of formula damage. **No ability deals or heals a flat
constant.** The only magnitudes allowed to be **fixed numbers are consumable items** (Potion = +100 HP,
per the item economy) — hand-placed content, not stat-scaling abilities. A flat-number ability is a bug,
not a balance knob: an ability's damage is tuned through its **spell-power / weapon-mod / multiplier**,
never a raw constant, so it grows with the unit and reads off one transparent equation. (Out of scope
here: **costs** — MP / CT / JP — and the **3d6 status-contest** numbers (`13`), which are their own
systems, not damage.)

## The pipeline (model "d")

For a confirmed, undefended hit:

```
injury = max( pen_floor ,  max(0, [ base(IN) + wmod ] − DR_type) )  × wound_mult × trait_mult × G
```

Term by term:

| Term | Meaning |
|------|---------|
| `base(IN)` | The GURPS strength→damage value from the attacker's **damage input** `IN`, via a re-ranged table. `IN` = **base PA** (ST) for most weapons; **= weapon `skill` for crossbow & gun** (marksmanship weapons — validation A5; `10`/`14`). **Type-split (validation B2): `thr` for thrust weapons (lower), `sw` for swing/cut & crush (higher)** — see below. **Base PA only**: the weapon's PA bonus is *not* included here. |
| `wmod` | The **weapon's flat additive modifier** (from WP). Scales fast to large endgame numbers — GURPS *structure*, FFT *pace*, not literal +1/+2 steps. |
| `DR_type` | The target's **subtractive Damage Resistance** against this damage *type*. Different armor resists different types differently (`03`). |
| `pen_floor` | **Penetration floor.** A minimum fraction of the pre-mult raw damage always gets through (≈15–33%), so even a bad matchup chips. |
| `wound_mult` | The damage-type **wound multiplier** (swing ×1.5, thrust ×2, crush ×1, missile ×1) — see `03`. |
| `G` | A global **bridge constant** mapping GURPS-native magnitudes onto FFT's HP scale. |
| `trait_mult` | The **trait offense multiplier**: the **Brave** physical-offense multiplier (`07`, ~0.76–1.35) for ordinary physical weapons; **1.0 (trait-neutral)** for crossbow/gun skill weapons (`10`). Magic runs on its own Faith-scaled axis (`11`), not here. |

**The headline is honest about its modifiers (validation B7).** `trait_mult` above is the Brave
multiplier, shown in the equation rather than hidden. One more **documented add-on**: a master's
**over-cap weapon skill** (`10`) feeds in as extra **damage** (into the bracket) or **penetration**
(a DR reduction) — a real term, called out here so the "transparent" formula stays transparent.

### `base(PA)` is type-split: `thr` for thrust, `sw` for swing (validation B2, 2026-06-26)

`base(PA)` is **not** a single number shared by all weapons. It is the GURPS **thrust** value
`thr(PA)` for thrust-type weapons and the **swing** value `sw(PA)` for swing/cut and crush weapons,
where **`thr(PA) < sw(PA)`** (the GURPS table). This is load-bearing — it is what **balances the wound
multipliers** (`wound_mult`: cut ×1.5, thrust ×2, crush ×1).

The original draft left `base(PA)` as one shared value; the validation (B2) showed that a single base
multiplied by the type's `wound_mult` makes **thrust (×2) strictly dominate cut (×1.5)** on the same
number — and because the multiplier is outside the additive `wmod`, no weapon-power tuning can
compensate (the gap *grows* with PA). Restoring the GURPS split fixes it at the root:

- thrust's ×2 falls on the **smaller** `thr` base; cut's ×1.5 on the **larger** `sw` base.
- On **raw** damage, cut is competitive-to-higher — e.g. ST≈11: cut `sw≈4.5 ×1.5 ≈ 6.8` vs thrust
  `thr≈2.5 ×2 ≈ 5.0`.
- Thrust's edge appears only **after armor / on penetration** — the ×2 doubles what got *through* DR,
  and matters more vs high-HP. So thrust = the anti-armor/penetration specialist, cut = the raw-damage
  generalist (strong vs light targets): a **contextual** split, not a universal ranking (P1′). This
  also restores cut's winning contexts (validation B11). Crush is swung → `sw` base × 1 (paid back by
  its heavier `wmod`); missile keeps its own ×1 base and earns its niche through **range/kiting**, not
  raw damage (`03`, `06`).

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
