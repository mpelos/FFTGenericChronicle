# Zodiac — Sign Compatibility (attacker × target)

Status: Draft (model decided 2026-06-28; magnitudes "Subtle" band chosen; matrix locked; tuning open)
Date: 2026-06-28 (supersedes the 2026-06-25 "elemental temperament" draft)
Depends on: 01-attribute-map, 02-damage-model, 04-hit-and-defense, 11-magic.
Review: Pending.

## Decision: keep FFT's compatibility mechanic, much subtler (reverses the earlier redesign)

This **replaces** the prior draft of this document (Zodiac as an *elemental temperament* — a personal
innate elemental affinity/weakness, 12 signs → 4 elements). That redesign is **dropped.**

The new decision (Marcelo, 2026-06-28): **Zodiac works like vanilla FFT / FFTIC** — a
**compatibility modifier between the attacker's and the target's sign** that nudges **damage** and
**hit chance**. The grid is FFT's. What changes from vanilla is **magnitude and legibility**:

- **Magnitude:** vanilla's swings are **brutal** (±25%/±50% damage, multiplicative hit → up to a 3×
  damage spread and ~5.5× combined output on the same attack). The DCL makes the effect **substantial
  but much subtler** — a *tilt*, not a *swing* (the "Subtle" band below).
- **Legibility:** vanilla's compatibility is **hidden**. The DCL **keeps the mechanic but surfaces
  it** (the matchup tier and its effect are shown in the preview, like everything else in the DCL).
  This satisfies the `00` legibility principle without removing the mechanic — we reverted the
  *behavior*, not the *principle*.

This is a `01` **"use"** of the Zodiac attribute (the most direct one — vanilla itself uses it),
satisfying the use-or-replace constraint.

## The compatibility grid (FFT's, confirmed)

The grid is not arbitrary — it is a clean **distance rule** on the 12-sign wheel in calendar order
(indices 0–11): Aries 0, Taurus 1, Gemini 2, Cancer 3, Leo 4, Virgo 5, Libra 6, Scorpio 7,
Sagittarius 8, Capricorn 9, Aquarius 10, Pisces 11. Compatibility depends on the circular distance
`d` between the **attacker's** and the **target's** sign:

| `d` (circular) | Astrological relation | Tier |
|---|---|---|
| 0 (same sign) | conjunction | **Good** |
| 1, 2 | — | Neutral |
| 3 (and 9) | square | **Bad** |
| 4 (and 8) | trine | **Good** |
| 5 | — | Neutral |
| 6 (opposite) | opposition | **Best** (everyday) / **Worst** (designed content) |

Per sign this yields exactly **1 best · 2 good · 2 bad · 6 neutral** (+ self = Good) — the same
structure FFTIC presents ("2 positive, 2 negative, 1 best"). The relationship is **symmetric** (it
is a property of the unordered pair).

### Best vs Worst without gender (the one IC-forced choice)

In classic FFT the opposite sign is **Best if opposite gender, Worst if same gender**. **IC Enhanced
removed the gender gate**, and the DCL has **no gender concept at all** (`01`). So the opposite pair
must resolve to one value. Decision:

> **Opposite = Best (the everyday band). Worst is reserved as designed content** — monsters (which in
> vanilla already convert Best/Worst → Bad) and specific scripted foes/items.

This mirrors a principle the DCL already uses elsewhere (`11`: "big swings are rare designed
properties, not the everyday band"), and because Best occurs against only **1 sign in 12**, the
top-of-band effect is naturally rare.

## The magnitudes — the "Subtle" band (chosen 2026-06-28)

Zodiac is the **smallest lever in the stack** — below Faith's ±30% (`08`) and below Brave's range
(`07`). It is a *tilt*, not a *swing*. Subtle per hit, **substantial over a battle**: a +10% damage
lean compounds across many strikes, so a unit consistently in good matchups out-damages one in bad
matchups by ~20% over an engagement — it decides attrition without deciding any single hit.

| Compatibility | **Damage** | **Hit (3d6 target)** |
|---|---|---|
| **Best** (opposite) | **+20%** (×1.20) | **+1** |
| **Good** | **+10%** (×1.10) | — (0) |
| Neutral | 0 | — (0) |
| **Bad** | **−10%** (×0.90) | — (0) |
| **Worst** (designed content) | **−20%** (×0.80) | **−1** |

Two deliberate shape choices:

- **Damage in 10% steps** (Good/Bad ±10, Best/Worst ±20) — half of vanilla. Same-attack spread drops
  from **3× → 1.5×**.
- **Accuracy only at the opposite matchup.** On 3d6 the *minimum* step is ±1 (≈ ±7–10pp) — already
  the granularity floor. Spreading ±1 across every tier would make *every* common matchup move
  accuracy ~10pp, which is **not** subtle. Confining the hit nudge to the rare **Best/Worst**
  (opposite sign, 1 in 12) keeps accuracy "present and substantial where it matters" while everyday
  matchups are a clean damage-only tilt.

### What this feels like (worked example)

Attacker **skill 12** (lands 74% vs a neutral target), base hit **100 damage**, expected output per
swing (hit% × damage; defense roll omitted for clarity — the *ratios* hold with it):

```
                   hit    dmg    expected
Worst  (content)   63%  ×  80  =   50   ▓▓▓▓▓
Bad                74%  ×  90  =   67   ▓▓▓▓▓▓▓
Neutral            74%  × 100  =   74   ▓▓▓▓▓▓▓▓
Good               74%  × 110  =   81   ▓▓▓▓▓▓▓▓▓
Best   (opposite)  84%  × 120  =  101   ▓▓▓▓▓▓▓▓▓▓▓
```

- **Common matchup (Good vs Bad): ~1.2×** output — felt, never decisive.
- **Rare matchup (Best vs Worst): ~2.0×** output — a real "it's the opposite sign!" moment, far from
  vanilla's ~5.5×.

### Calibration knobs (the band can move; the grid cannot)

| Band | Good/Bad dmg | Best/Worst dmg | Good/Bad hit | Best/Worst hit | Common | Rare |
|---|---|---|---|---|---|---|
| Vanilla (rejected) | ±25% | ±50% | multiplicative | multiplicative | ~2.2× | ~5.5× |
| Medium | ±10% | ±20% | +1/−1 | +2/−2 | ~1.6× | ~2.7× |
| **Subtle (chosen)** | ±10% | ±20% | 0 | +1/−1 | **~1.2×** | **~2.0×** |
| Very subtle | ±5% | ±10% | 0 | +1/−1 | ~1.1× | ~1.6× |

If playtest finds the chosen band too strong, drop toward "Very subtle"; if accuracy should bite in
common matchups too, move to "Medium". The **grid (distance rule, 5 tiers) is fixed**; only the band
is calibration.

## How it enters the formulas

A single multiplicative term, `zodiac_mult`, derived from the attacker×target sign, plus an additive
hit modifier `zodiac_hit`. Damage stays deterministic (preview = result); the hit modifier is part of
the random landing contest — both consistent with `00`'s "deterministic damage, random contest".

**Physical** (`02`), alongside `trait_mult` (Brave), applied post-DR:
```
injury = max(pen_floor, max(0, base(IN)+wmod − DR_type)) × wound_mult × trait_mult × zodiac_mult × G
```

**Magic / healing** (`11`) — `zodiac_mult` is its **own** term (it is *not* an elemental affinity; see
next section):
```
dmg  = base(MA) × spell_power × faith_mult × element_mult × zodiac_mult × G_m
heal = base(MA) × heal_power  × faith_mult ×               zodiac_mult × G_m
```
All terms multiplicative and commutative — no stacking order. The "Subtle" band (Best ×1.20) is small
enough to sit comfortably under the magic stack's reserve soft-cap (`11`).

**Hit** — `zodiac_hit` (+1 / 0 / −1) modifies **only the attacker's 3d6 hit-roll target** (`04`). It
does **not** touch the defender's defense roll (no double-dip) and does **not** change crit/fumble
thresholds. Where another landing roll exists it applies the same way: magic vs **Magic Evade**, and —
optionally, consistent with vanilla's "percentage-based actions" — the **status** 3d6 contest (`13`).
Scope as agreed is **damage + hit**; status is the natural, consistent extension if wanted.

## What this changes elsewhere in the DCL (ripples)

1. **Legibility principle (`00`):** unchanged in spirit — we keep compatibility but **surface** it.
   The `00` wording ("Vanilla's hidden Zodiac compatibility multiplier … replaced") is amended to
   "…surfaced and softened" (`00` updated).
2. **The trait trio → "two sliders + a matchup web" (`00`/`01`):** Brave and Faith remain personal
   two-sided *sliders*. **Zodiac is no longer a personal slider** — it is a **relational matchup**
   between two units. The DCL's anti-dominance property **survives**: by the grid's symmetry every
   sign has the identical distribution (1 best / 2 good / 2 bad / rest neutral), so **no sign is
   stronger than another** — each just wins and loses different matchups. Two-sided as a *web*, not a
   *régua*.
3. **Elemental affinity moves out of Zodiac.** The dropped redesign made signs grant elemental
   resist/weakness; reverting removes that. Innate elemental resist/weak/halve/absorb now lives where
   it does in vanilla FFT — on **equipment, status (e.g. Oil), job-innate properties, and designed
   content** — and is carried by `element_mult` in the magic spine (`11`), independent of the sign.
   Holy/Dark remain Faith-axis and outside any elemental interaction (`08`). Lightning is simply a
   normal element with no special sign tie.

## The full compatibility matrix (everyday band)

Attacker (row) × target (column). `B` = Best (×1.20, +1 hit) · `G` = Good (×1.10) · `·` = Neutral ·
`b` = Bad (×0.90). The opposite-sign cell is `B` in the everyday band; **Worst** (×0.80, −1 hit)
overrides that cell only for designed content (monsters/scripted foes). Symmetric.

| atk\tgt | Ari | Tau | Gem | Cnc | Leo | Vir | Lib | Sco | Sag | Cap | Aqu | Pis |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Ari** | G | · | · | b | G | · | B | · | G | b | · | · |
| **Tau** | · | G | · | · | b | G | · | B | · | G | b | · |
| **Gem** | · | · | G | · | · | b | G | · | B | · | G | b |
| **Cnc** | b | · | · | G | · | · | b | G | · | B | · | G |
| **Leo** | G | b | · | · | G | · | · | b | G | · | B | · |
| **Vir** | · | G | b | · | · | G | · | · | b | G | · | B |
| **Lib** | B | · | G | b | · | · | G | · | · | b | G | · |
| **Sco** | · | B | · | G | b | · | · | G | · | · | b | G |
| **Sag** | G | · | B | · | G | b | · | · | G | · | · | b |
| **Cap** | b | G | · | B | · | G | b | · | · | G | · | · |
| **Aqu** | · | b | G | · | B | · | G | b | · | · | G | · |
| **Pis** | · | · | b | G | · | B | · | G | b | · | · | G |

Per sign — **Best / Good / Bad** (opposite / trines / squares):

| Sign | Best | Good | Bad |
|------|------|------|-----|
| Aries | Libra | Leo, Sagittarius | Cancer, Capricorn |
| Taurus | Scorpio | Virgo, Capricorn | Leo, Aquarius |
| Gemini | Sagittarius | Libra, Aquarius | Virgo, Pisces |
| Cancer | Capricorn | Scorpio, Pisces | Aries, Libra |
| Leo | Aquarius | Aries, Sagittarius | Taurus, Scorpio |
| Virgo | Pisces | Taurus, Capricorn | Gemini, Sagittarius |
| Libra | Aries | Gemini, Aquarius | Cancer, Capricorn |
| Scorpio | Taurus | Cancer, Pisces | Leo, Aquarius |
| Sagittarius | Gemini | Aries, Leo | Virgo, Pisces |
| Capricorn | Cancer | Taurus, Virgo | Aries, Libra |
| Aquarius | Leo | Gemini, Libra | Taurus, Scorpio |
| Pisces | Virgo | Cancer, Scorpio | Gemini, Sagittarius |

## Open items

- **Final band** (chosen "Subtle"; may move to "Very subtle"/"Medium" in playtest — table above).
- **Status application:** whether `zodiac_hit` also modifies the `13` status contest (vanilla did via
  "percentage-based actions"). Currently scoped to damage + hit.
- **Presentation:** exactly how the matchup tier is surfaced in the preview/UI (`12` item 6).
- **Serpentarius** (the 13th/secret sign, if present in IC) is neutral to all — confirm handling.
- Magnitudes interact with the global magic/physical calibration (`G`, `G_m`, the reserve soft-cap) —
  `12-open-questions.md`.
