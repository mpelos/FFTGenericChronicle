# Squire

Status: Decision (consolidated). Engine: **Deep Combat Layer** (canonical).
Supersedes the scattered Squire layers — `04`, `05`, `52`, `58` (Squire rows), and `68` — which are
folded into this single decision doc.

> **On the numbers.** The durable decision is each skill's *identity, intent, and guardrail*. The
> concrete values in parentheses are **v0.2-era and provisional** — they were tuned against the old
> formula model and will be re-derived when the kit is rebased onto the Deep Combat Layer (see *DCL
> rebase notes* at the end). Read the prose as the decision; read the numbers as a starting point.

## Identity / compass

Squire is the **scrappy starter grunt**: a low-complexity frontline utility job that teaches
positioning, weapon use, tempo, and clutch survival. Its fantasy is **guts, not hidden stat
efficiency**.

It should stay useful as an early chassis and as a utility secondary, but it must **not** become a
late-game damage engine or a mandatory support package for every physical unit.

## Role & build basics

| Field | Value |
|-------|-------|
| Primary role | `melee-physical` |
| Secondary tags | `starter`, `utility` |
| Growth profile | physical |
| Armor class | `leather` |
| Weapon families | sword, knife, axe, flail, fists (swing / thrust / crush) |
| Role reason | The baseline flexible physical job; teaches weapon identity without decaying into pure JP filler. |

**Good at:** early-game flexibility, positioning utility, clutch survival, a cheap utility secondary.
**Bad at / countered by:** raw late-game damage, specialist roles, anything that rewards a deep kit —
Squire is deliberately broad and shallow.

## Action skills

| Skill | Intent | Guardrail |
|-------|--------|-----------|
| **Throw Stone** | Scaling crush chip — finisher / positioning pressure. | Intentionally low-ceiling; never a ranged-damage plan. *(v0.2: raw = 8 + ⌊Level/5⌋ + ⌊PA/2⌋.)* |
| **Dash** | Adjacent crush body-check; positional pressure (may carry shove / facing). | Adjacent only; stays below ordinary weapon damage. *(v0.2: raw = 12 + ⌊Level/4⌋ + PA.)* |
| **First Aid** | Modest scaling adjacent patch-heal. | Capped below the current Potion tier; no revive; no stock. *(v0.2: raw = 20 + ⌊Level/3⌋ + 2·PA, capped at potion-tier − 10 and missing HP.)* |
| **Focus** | Visible one-use setup: the next physical action hits harder and more reliably. | Single-use, non-stacking, expires end of next turn. Combos with Jump / Charge / All-Out Strike are intended. *(v0.2: ×1.40 dmg, +10 hit, +10 crit.)* |
| **Rally** | A discrete tempo gift: hand CT to one nearby ally. | Range 2, ally-only (no self), once per target per round; **not** Haste. *(v0.2: +20 CT.)* |
| **All-Out Strike** | A bold committed strike: a stronger weapon attack that leaves the user **Exposed** (more vulnerable) until their next turn. | Replaces vanilla `Weapon Drill`. Inherits the equipped weapon's family/matchup (no per-family riders). The risk is positional, not an accuracy penalty. *(v0.2: output ×1.35; `Exposed` = incoming ×1.25.)* |

`Exposed` is a **shared status vocabulary** with Knight (same meaning and magnitude): Squire
self-applies it as a risk; Knight applies it to enemies as a setup window.

## Reaction / Support / Movement

| Slot | Skill | Intent | Guardrail |
|------|-------|--------|-----------|
| Reaction | **Grit** | Desperation guard: at low HP, eligible incoming direct damage is reduced for that hit. | HP ≤ 1/3; once per unit round; **off Brave**; visible `Grit Guard`; channel-bound with other major mitigation. *(v0.2: ×0.70.)* |
| Support | **Basic Training** | An explicit Squire-action upgrade table — **not** a blind output multiplier. Makes the support slot matter for committed Squire/Fundament builds. | Only improves Squire/Fundament actions; never ordinary attacks, weapons, spells, items, Ultima, or other command sets. *(v0.2 upgrades: Throw Stone ×1.50, Dash ×1.40, Focus ×1.50/+15/+15, Rally +25 CT, All-Out Strike ×1.50; First Aid same formula, still capped.)* |
| Movement | **Move +1** | Early mobility floor. | Intentionally outclassed by later movement options. |

`Grit Guard` and `Exposed` are **separate channels** and combine normally — a low-HP Squire who uses
All-Out Strike is both reckless and stubborn (v0.2: ×1.25 × ×0.70). That interaction is acceptable
because it is readable, risky, and identity-rich.

## Open items / validation hooks

- `M-SECONDARY-COUNT`: does `Basic Training` + Squire secondary become mandatory across too many
  physical builds?
- `I-ATTRITION`: `First Aid` scaling vs its Potion-tier cap against real campaign sustain.
- Action economy: `Rally` chains (multiple Squires pulling one unit forward).
- Combo convergence: Focus → Jump / Charge / All-Out Strike.
- Burst ceiling: trained Focus → trained All-Out Strike with crits.
- Basic-hit dominance: confirm All-Out Strike's `Exposed` risk keeps it from replacing every attack.

## DCL rebase notes

How the v0.2 mechanics above re-express on the Deep Combat Layer (to be done in the rebase pass):

- **All-Out Strike / Brave fit.** "More output, more vulnerable" *is* the DCL Brave axis (high
  aggression = +offense, −active defense). All-Out Strike becomes a one-action spike that spends
  active defense, and `Exposed` re-expresses as a temporary active-defense / guard penalty rather
  than a flat ×1.25 incoming multiplier (the DCL has no output-multiplier mitigation).
- **Grit.** A low-HP desperation mitigation; under the DCL express it as a temporary DR or
  active-defense bonus, not a flat ×0.70.
- **Damage/heal scaling** (Throw Stone, Dash, First Aid) re-derives on the DCL damage scale; the
  crush type and "stays below a real weapon/heal" intent carry over directly.
- **Rally / Focus** are mostly engine-neutral: CT exists in the DCL (Speed = turn frequency), and
  Focus's hit/crit bonus maps onto the 3d6 skill roll and crit window.
