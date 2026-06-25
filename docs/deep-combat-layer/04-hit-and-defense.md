# Hit and Defense

Status: Draft (structure locked; calibration partially validated)
Date: 2026-06-25
Depends on: 02-damage-model, 01-attribute-map.
Review: Pending.

## Two independent rolls

A physical attack resolves as **two separate 3d6 rolls**:

1. **Hit roll.** The attacker rolls 3d6; the attack connects on `≤ weapon skill` (`10`).
2. **Defense roll.** If it connects, the defender rolls 3d6; the attack is turned aside on
   `≤ best available active defense`.

A blow only deals damage if it **hits and is not defended**. Damage itself is then deterministic
(`02`). This split is the heart of the loop: offense is "can I land it?", defense is "can I turn it
aside?", and they are resolved by different actors with different stats.

### 3d6 reference (probability of rolling ≤ N)

| Target N | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 |
|----------|---|---|----|----|----|----|----|----|----|
| P(≤N) | 26% | 38% | 50% | 63% | 74% | 84% | 91% | 95% | 98% |

The meaningful band is **9–16**, where both the curve and the crit/fumble edges bite. Skill and
defense values are tuned to sit here (`10`).

## Critical hits and fumbles

Taken from GURPS, scaled to skill:

- **Critical hit** — bypasses the defense roll entirely (the defender gets no chance to turn it
  aside): natural **3 or 4 always**; **5 if skill ≥ 15**; **6 if skill ≥ 16**. Higher skill widens
  the crit window — another reason the 9–16 band matters.
- **Fumble / critical failure**: natural **18 always**; **17 if skill ≤ 15**. Low skill fumbles more.
  A fumble is simply an **automatic miss** — no extra penalty. The heroic model avoids punishing
  swing; the rare crit-fail just means the blow whiffs.

## The three active defenses

| Defense | FFT source | Behaviour | Value |
|---------|-----------|-----------|-------|
| **Dodge** | C-Ev + Speed | **Always on, never depletes.** The innate floor every unit has. | ~8 baseline |
| **Parry** | W-Ev (weapon) | Strong, but **depletes** with use. | ≈ skill/2 + 3 |
| **Block** | S-Ev (shield) | Strong, but **depletes** with use. Only with a shield. | shield-derived |

### Automatic best-defense + depletion (Q11 = automatic)

Each incoming attack **automatically uses the best available defense** and **depletes that one**.
The player does not pick per-attack; a ladder emerges naturally:

- First attacks are met by the strong depleting defenses (Block, then Parry).
- As those deplete, the unit falls back down the ladder, eventually to the **Dodge floor** (which
  never depletes — there is always *some* defense).

This turns "a defensive unit" from a static wall into a **resource under pressure**: each attack
spends defense.

### Reset on the defender's own turn → Speed becomes defense

Depleted defenses **refresh on the defender's own turn.** This is the mechanism that makes **Speed
an emergent defensive stat**:

- A **slow** unit that gets **focus-fired** sees its Block/Parry depleted and *cannot refresh them*
  until its distant next turn — its guard collapses to the Dodge floor and it gets hit.
- A **fast** unit refreshes sooner, so it holds its full defensive ladder more often.

Speed thus pulls double duty (see `01-attribute-map.md`): it feeds the Dodge floor *and* governs how
quickly the depleting defenses come back. Tempo and survivability are linked.

## Calibration target (partially validated)

From simulation on 2026-06-25:

- **Baseline must favor the attacker.** A competent physical attacker (skill ≥ 12) striking a target
  with **no shield** (Dodge ≈ 8) front-on must land **> 50%** of the time. Achieved: **55–67%**.
  This prevents the whiff-fest that pure active defenses can cause.
- **Defensive investment is felt.** A target that has invested in **shield + parry** drops the
  attacker's front-on success to **~31%** — which is precisely what *triggers the depletion game*
  (you can't just out-roll a turtle front-on; you flank it, focus-fire it, or crush it — `05`).

So the system has two regimes by design: open targets die to direct attacks (>50%), turtled targets
force the positional/attrition game (~31% until their guard is broken).

## Multi-hit and dual-wield

A multi-strike attack (dual-wield "Two Swords", multi-hit weapons/abilities) resolves **each strike
independently**: every strike makes its **own hit roll** and, if it connects, the defender makes its
**own defense roll** against it — and **each strike depletes a defense** (the depletion ladder above).

This gives multi-hit a distinct identity: a **guard-shredder**. Two strikes spend two defenses, so a
dual-wielder is a one-unit **focus-fire engine** — the built-in answer to a turtled target
(synergises directly with the depletion game and with flanking, `05`). It is **not** merely "more
DPS".

Consequences:
- Each strike is its own contest, so each can **crit** (bypass defense) or **fumble** — more strikes
  = more crit chances *and* more fumble chances.
- Balanced by **lower power per strike** (smaller modifier and/or a per-hit skill penalty, à la GURPS
  Rapid Strike and FFT's dual-wield split): total damage vs an *open* target stays comparable to a
  single heavy blow, while the edge vs a *turtled* target is the guard-shredding.

Detail/open: whether a counter (reaction) fires once or per strike, and the exact per-strike
power/skill penalty, are calibration — see `12`.

## Magic defense

Targeted/bolt spells can be **dodged via Magic Evade** ("magic dodge"); this is the magic-axis
analogue of the Dodge floor. Magic does not use Parry/Block and does not pass through physical DR.
See `11-magic.md`.

## Open items

The exact Dodge/Parry/Block formulas and depletion amounts, the per-attack depletion cost, what a
fumble does, and full validation of the calibration regimes are in `12-open-questions.md`.
