# Squire Good-Job Rediscussion V0

Status: GPT/Claude consensus for Marcelo validation
Date: 2026-06-23
Scope: Squire only

Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/52-squire-chemist-concrete-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/job-balance/61-jp-boost-removal-decision-v0.md`

## Purpose

This document revisits Squire under the updated good-job premises:

- learned skills should feel useful and readable;
- strong combos are healthy when they require real setup, opportunity cost, or party coordination;
- persistent combat math changes should be visible in battle;
- direct fixed recovery should scale modestly instead of dying after the early game;
- reactions should not all become Brave optimization problems;
- no Gil values are changed;
- no new equipment is added.

This pass supersedes the Squire rows in docs 52 and 58 where they conflict. It does not change
Chemist, item economy, JP costs, prerequisites, or final validation results.

## Squire Identity

Squire is the scrappy starter grunt: a low-complexity frontline utility job that teaches positioning,
weapon use, tempo, and clutch survival. Its fantasy is guts, not hidden stat efficiency.

Squire should remain useful as an early chassis and as a utility secondary, but it should not become a
late damage engine or a mandatory support package for every physical unit.

## Consensus Package

| Skill | Slot | Base value | With `Basic Training` | Guardrail |
| --- | --- | --- | --- | --- |
| `Throw Stone` | Action | fixed 12 crush chip | fixed 18 crush chip | Utility, finisher, positioning pressure; intentionally non-scaling. |
| `Dash` | Action | fixed 18 crush body-check | fixed 27 crush body-check | Adjacent only; remains positional pressure, not a weapon replacement. |
| `First Aid` | Action | scaling adjacent heal | scaling adjacent heal, still capped | No revive; never exceeds the current Potion-tier safety cap. |
| `Focus` | Action | visible Focus; next physical action x1.40, +10 hit, +10 crit | visible Focus; next physical action x1.50, +15 hit, +15 crit | Single-use; non-stacking; expires at end of next turn. |
| `Rally` | Action | range 2 ally gains +20 CT | range 2 ally gains +25 CT | Ally-only; no self-target; once per target per round; not Haste. |
| `All-Out Strike` | Action | normal attack output x1.35; user gains Exposed | normal attack output x1.50; user gains Exposed | Replaces `Weapon Drill`; no per-family modal riders. |
| `Grit` | Reaction | HP <= 1/3 direct-damage desperation guard | unchanged | Off Brave; once per unit round; visible `Grit Guard`; channel-bound. |
| `Basic Training` | Support | explicit Squire-action upgrade table | - | Not a blind multiplier; Squire/Fundaments actions only. |
| `Move +1` | Movement | Move +1 | - | Early mobility floor; intentionally outclassed later. |

## Skill Notes

### First Aid

`First Aid` should become a modest scaling patch heal instead of a flat 20 HP action.

Provisional formula target:

```text
raw_heal = 20 + floor(Level / 3) + (2 * PA)
effective_heal = min(raw_heal, current_potion_tier_heal - 10, missing_hp)
```

Rules:

- adjacent only;
- no revive;
- no item stock;
- no interaction can lift it above the Potion-tier cap;
- `Basic Training` does not bypass the cap.

The intent is that `First Aid` remains useful for chip recovery later, while Chemist item healing
keeps the reliable sustain lane.

### Focus

`Focus` becomes a visible setup status instead of a tiny invisible nudge.

It applies a one-use Focus state to the user. The next physical action gets the listed damage, hit,
and crit bonus. The state is consumed by the next eligible physical action, does not stack, and
expires at the end of the user's next turn.

This intentionally supports combos such as Focus into Jump, Charge-style actions, or All-Out Strike.
Those combos are healthy if the two-turn setup, positioning, and exposure cost remain real.

### Rally

`Rally` is a discrete tempo gift, not a Haste substitute.

It immediately grants CT to one ally within range 2. It cannot target the user and cannot affect the
same target more than once per round. The value is deliberately single-target and legible instead of
a small AoE nudge.

### All-Out Strike

`All-Out Strike` replaces `Weapon Drill`.

It is a normal equipped-weapon attack output multiplied by the listed value. Because it inherits the
equipped weapon's family and armor response, it preserves the new weapon-family formula ecosystem
without reintroducing modal per-family riders.

After using the action, the user gains visible Exposed until the start of their next turn.

```text
Exposed = incoming damage x1.25
```

Rules:

- Exposed applies to all incoming damage;
- Exposed is visible;
- Exposed is non-stacking;
- Exposed ends at the start of the user's next turn;
- no accuracy penalty is attached to the strike;
- no new equipment or Gil hooks are involved.

The skill is meant to feel strong. The risk is positional and tactical: a protected or repositioned
Squire can exploit it safely, while a reckless Squire becomes punishable.

### Grit

`Grit` moves Squire defense away from Brave optimization and into desperation identity.

When the unit is targeted by eligible direct damage while at HP <= 1/3 max HP, `Grit` applies visible
`Grit Guard` for the current hit and until the start of the unit's next turn.

```text
Grit Guard = eligible incoming direct damage x0.70
```

Rules:

- once per unit round;
- no Brave trigger;
- eligible direct damage only;
- channel-bound with other major mitigation effects;
- visible status feedback required.

`Exposed` and `Grit Guard` are separate channels and combine normally. A Squire who uses All-Out
Strike while low on HP can be both reckless and stubborn:

```text
incoming direct damage while both apply = base_damage x1.25 x0.70
```

That interaction is acceptable because it is readable, risky, and identity-rich.

### Basic Training

`Basic Training` is no longer a generic `x1.10` or blind `x1.50` output multiplier.

It is an explicit Squire-action upgrade table:

| Squire action | Trained result |
| --- | --- |
| `Throw Stone` | fixed 18 crush chip |
| `Dash` | fixed 27 crush body-check |
| `First Aid` | same scaling formula, still capped below Potion-tier healing |
| `Focus` | x1.50, +15 hit, +15 crit |
| `Rally` | +25 CT |
| `All-Out Strike` | normal attack output x1.50 |

It does not affect:

- ordinary attacks;
- weapon formulas generally;
- spells;
- items;
- `Ultima`;
- reaction, support, or movement abilities;
- non-Squire command sets.

This makes the support slot matter for players who intentionally carry Squire/Fundaments, while
avoiding multiplicative surprises on broader builds.

## Validation Hooks

These are later validation hooks, not blockers to this provisional rediscussion artifact:

- `M-SECONDARY-COUNT`: count whether `Basic Training` plus Squire secondary becomes mandatory too
  early or across too many physical builds.
- `I-ATTRITION`: test `First Aid` scaling and its Potion-tier cap against real campaign sustain.
- Action economy: test `Rally` chains, especially multiple Squires trying to pull one unit forward.
- Combo convergence: test Focus into Jump, Charge-style actions, and All-Out Strike.
- Burst ceiling: test `Basic Training` Focus into trained All-Out Strike with crit assumptions.
- Basic-hit dominance: confirm All-Out Strike's Exposed risk keeps it from being a free replacement
  for every ordinary attack.

## Reviewer Notes

Claude approved this Squire direction before Chemist discussion resumes. The only substantive
revision during review was making `All-Out Strike` bolder:

- Claude proposed the conservative x1.20 base / x1.35 trained values;
- GPT argued that this would feel too weak after the Exposed downside and pushed x1.35 base /
  x1.50 trained;
- Claude conceded to the bolder values;
- final consensus is x1.35 base / x1.50 trained.

Chemist remains untouched until this Squire artifact is reviewed.
