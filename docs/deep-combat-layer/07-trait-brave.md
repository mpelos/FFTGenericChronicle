# Trait: Brave — Physical-Aggression Temperament

Status: Draft (approach validated via simulation 2026-06-25; fine tuning open)
Date: 2026-06-25
Depends on: 04-hit-and-defense, 02-damage-model.
Review: Pending.

## Role

Brave is the **physical** member of the permanent-trait trio (Brave = body, Faith = spirit,
Zodiac = element; see `01-attribute-map.md`). It is a **permanent two-sided régua (slider)** like
Faith: every value buys something and pays something, and **there is no universally-best Brave.**

The litmus Marcelo set for the design: *the slider must be clean on both ends, and it must answer —
"why would a mage not want high Brave?"* The validated design answers that cleanly (see below).

## Approach: aggression dial (option A — active-defense penalty)

Of the two candidate models, Marcelo chose **(A): high Brave penalizes active defense** (over (B):
high Brave changes damage magnitude only). High Brave makes you hit harder and more recklessly but
**easier to hit back.**

### HIGH Brave

- **+ Physical offense** — more physical damage output.
- **+ Courage reactions fire** — *aggressive* reaction abilities (Counter, ripostes) trigger more
  (FFT-native: their chance is Brave%). High Brave does **not** boost defensive/caution reactions —
  see "Reactions and Brave" below.
- **+ Composure** — resist **mental/morale statuses** (stun, fear, taunt) and **interruption** from
  interrupt skills: the brave don't flinch. (The automatic major-wound trigger was set
  aside for legibility — see `12`, items 1 & 8; composure keys off the skill-driven status system.)
- **− Active defense** — penalty to Dodge / Parry / Block; you get hit more often.

### LOW Brave

- **− Physical offense** — less physical damage.
- **− Composure** — easily **stunned / feared / interrupted** by skill-driven statuses.
- **+ Caution reactions fire** — *defensive/careful* reaction abilities (guard, evade-boost,
  Blade-Grasp-like) trigger more at low Brave. Low Brave does **not** enable courage reactions.
- **+ Active defense** — bonus to Dodge / Parry / Block; harder to hit.

## Reactions and Brave (job-defined)

Brave does **not** make *all* reactions fire. The naive FFT rule — every reaction's chance is the
unit's Brave% — would leave a low-Brave unit with a **dead reaction slot**, which is too high a cost
and breaks the two-sided design (a low-Brave build would just lose a whole ability type). Instead,
reactions come in three flavours, and **the job defines** which it grants:

- **Courage reactions** (Counter, ripostes) — fire more at **high** Brave (chance ∝ Brave%).
- **Caution reactions** (defensive/careful: guard-up, evade-boost, Blade-Grasp-like) — fire more at
  **low** Brave (chance ∝ inverse Brave).
- **Neutral reactions** (utility) — fire regardless of Brave (flat, or keyed to another stat).

So the Reaction slot is **always live**; Brave only shifts *which* reactions you excel with. This
makes low Brave a real, positive build identity — the careful fighter who turtles and triggers
defensive reactions — not merely "less aggressive." Two-sided, like Faith. (Per-job reaction rosters
and exact trigger curves are detail — see `12`, item 8. Inverse/flat triggers likely need a code
hook — feasibility, `12` item 7.)

## Why magic stays out of Brave (the mage litmus)

**Brave touches physical only. Magic output stays entirely on Faith** (`08`). This is the single
decision that makes the slider clean:

> A mage gains *no* magic offense from high Brave — it only wrecks the mage's already-fragile active
> defense. So a mage leans **low** Brave: its power comes from Faith and positioning, and low Brave
> buys it the defensive cushion it desperately needs. A mage that goes high Brave is paying its
> survivability for an offense bonus it cannot use. The slider answers Marcelo's question by
> construction.

Had Brave also scaled magic, high Brave would be universally good for everyone — the exact failure
the first design iteration hit (every build wanted one end). Decoupling magic onto Faith is what
created a real two-sided choice.

## Simulation validation (2026-06-25)

The régua was validated across jobs/builds. Results:

| Build | Wants | Why |
|-------|-------|-----|
| **Tank** | **Low** Brave | Survival job; the active-defense bonus + it doesn't lean on counters. |
| **Melee DPS** | **High** Brave | Glass-cannon; high Brave ~doubles physical damage, accepts fragility. |
| **Duelist / Archer** | **Real choice** | Genuinely build-dependent; no dominant answer. |
| **Mage** | **Low** Brave | (see litmus above). |

Confirmed: **no universal-best Brave**; every build pays a real trade. High Brave roughly **doubles**
physical damage but **craters survivability** — a clean glass-cannon ↔ bunker axis.

### Provisional knobs (mid-game, calibration open)

- Offense multiplier across the Brave range: **~0.76× to ~1.56×**.
- Active-defense shift across the range: **~ +3 (low) to −2 (high)**.
- Interrupt-resist (composure): `p_le( round(Brave/10) + 5 )` on the 3d6 table.
- Sim constants that produced the clean régua: `k_off ≈ 0.012`, `def_div = 12`, physical-only.

## The caveat: Brave is an "under-fire" dial

Brave's downside (−active defense) only bites *when you are attacked*. A perfectly-protected
backliner who never gets hit pays no price for high Brave and could min-max it. This is the known
soft spot. It is **mitigated by the facing/flank game** (`05`): the DCL is built so that "never gets
hit" is hard to guarantee — flanking, focus-fire, and reach exist precisely to reach protected
units. The open tuning question is whether to shrink the offense swing (`k_off`) for tighter
Brave↔Faith symmetry so the min-max payoff is smaller (`12-open-questions.md`).

## Open items

`k_off` (offense-swing size for Brave↔Faith symmetry), the exact curves, the composure threshold
definition (what counts as a "major wound" / "charged action" to interrupt), and final per-band
numbers are deferred — `12-open-questions.md`.
