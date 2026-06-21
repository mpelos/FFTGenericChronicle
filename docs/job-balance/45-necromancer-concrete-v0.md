# Necromancer Concrete Provisional V0

Status: Accepted for concrete-provisional action values
Date: 2026-06-21
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/27-necromancer-v1-proposal.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/35-resource-economy-model-schema.md`
- `docs/job-balance/37-ko-corpse-undead-state-composition-schema.md`
- `docs/job-balance/41-area-hp-over-time-composition-schema.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/gpt-necromancer-concrete-v0.json`

## Purpose

This is the first concrete-provisional action value pass for Necromancer, the replacement for
Calculator/Arithmetician.

This pass does not finalize reaction, support, or movement values. `Soulbind`, `Death's Door`,
`Dark Lore`, `Deathcraft`, `Grave Step`, and `Shadow Step` remain deferred until T2.1 populated
build incidence and the later RSM/support pass.

Necromancer is intentionally late and state-dependent. It should reward poison, wounds, Doom,
undead state, KO bodies, and caster MP pressure. It should not recreate Calculator through abstract
global rules.

## Provisional Stat Anchor

`work/sim-inputs-v0.2.1.json` does not yet contain a Necromancer job record. V0 therefore uses
Mystic's bands as the conservative provisional anchor:

| Phase | HP | PA | MA | Speed |
| --- | ---: | ---: | ---: | ---: |
| early anchor | 112 | 2 | 6 | 6 |
| mid anchor | 210 | 4 | 10 | 7 |
| late anchor | 322 | 5 | 12 | 8 |
| stress anchor | 390 | 6 | 14 | 9 |

This is not final job data. It only gives us a reproducible stress point until the real
Necromancer job row exists.

Equipment posture remains the V1 proposal:

```text
book, pole, fists
```

That posture is a Mystic coexistence watch, not an accepted final equipment solution.

## Hard No-Calculator Rule

V0 concrete Necromancer contains:

- no arithmeticks selectors;
- no level/height/CT/prime/multiple targeting;
- no mapwide abstract spell routing;
- no spell that bypasses range, line, CT, MP, Faith, hit chance, immunity, target state, and
  positioning all at once.

The JSON records `arithmeticks_selectors_present = false`.

## Formula Contracts

Dark Faith-linked drains use the current Faith floor:

```text
amount = floor(round(K * MA * 0.60))
drain_recovery = floor(0.50 * amount)
```

Hostile status rows use the same V0 status proxy as Time/Mystic:

```text
effective_status = floor(round(base_hit * 0.60, 6))
```

Percent max-HP attrition uses the shared float guard:

```text
percent_amount = floor(round(percent_of_max_hp * max_hp, 6))
per_tick_amount = min(percent_amount, per_tick_cap)
```

Corpse actions inherit the accepted T3xT5xT8 policy:

```text
allow_acting_bodies = false
same-tick death-clock resolution is unsafe
Corpse Puppet creates a non-acting targetable object only
```

`Death Mark` fallback damage is explicit:

```text
death_mark_fallback = min(floor(round(0.20 * max_hp, 6)), 120)
```

The fallback is nonlethal. The lethal branch only applies if the mark persists to expiry and the
target is at or below 50% max HP.

## Action Values

| Skill | Effect | Value | MP | CT | JP | Gate binding |
| --- | --- | --- | ---: | ---: | ---: | --- |
| `Rot` | attrition status | 8% max HP per tick, cap 45, 4 ticks, 70 base hit | 12 | 2 | 150 | T3/T5/T9 |
| `Death Mark` | delayed conditional lethal | 36-tick mark, 45 base hit; lethal only at or below 50% max HP, otherwise nonlethal 20% max HP fallback cap 120 | 22 | 3 | 450 | T4/T5/T8/T3 |
| `Drain` | HP drain | K 12, 50% heal to caster | 16 | 3 | 250 | T3/T3xT5/F4/T9 |
| `Syphon` | MP drain | K 6 MP damage, 50% MP recovery | 10 | 2 | 250 | T9 |
| `Undead Mark` | undead-state window | 24 ticks, 55 base hit | 18 | 3 | 450 | T3/T3xT5/T4/T5/T8 |
| `Corpse Puppet` | KO-body object | consumes KO body; creates non-acting targetable object for 24 ticks | 12 | 2 | 500 | T3xT5xT8 |
| `Command Undead` | restricted control | undead/undead-marked only, 12 ticks, 40 base hit | 22 | 4 | 650 | T4/T5/T8/T3xT5xT8 |
| `Gravebind` | corpse-anchor area attrition | 6% max HP per tick, cap 35, 3 ticks, max 3 targets | 24 | 4 | 600 | T3xT5xT11/T11/T9 |
| `Dark Harvest` | conditional finisher damage | requires mark/rot/undead state or HP <= 30%; 30% max HP damage, cap 140, 70 base hit | 34 | 4 | 900 | T4/T5/T8/T3 |

`Death Mark` is not a cold instant kill. If the target is above the 50% HP threshold when the
countdown expires, it deals fallback nonlethal damage instead of killing.

`Dark Harvest` is not a random hard KO. It requires prior setup or low HP, it has boss/immunity
exclusions, and its damage is capped.

## Status And Control Checks

| Skill | Base hit | Effective hit | Duration/countdown | Read |
| --- | ---: | ---: | ---: | --- |
| `Rot` | 70 | 42 | 4 ticks | Main attrition setup, not guaranteed. |
| `Death Mark` | 45 | 27 | 36 ticks | Slow, cleanseable, conditional lethal pressure. |
| `Undead Mark` | 55 | 33 | 24 ticks | Dangerous inversion window, not reliable enough to spam. |
| `Command Undead` | 40 | 24 | 12 ticks | Restricted control; undead/marked only. |
| `Dark Harvest` | 70 | 42 | instant | Still needs setup eligibility. |

The hard-control surface is intentionally narrow. `Command Undead` is the closest thing to control,
but it is state-gated and low accuracy.

## Drain And Resource Checks

Stress anchor uses MA 14.

| Skill | Stress value | Recovery | Comparison |
| --- | ---: | ---: | --- |
| `Drain` | 101 HP damage | 50 HP recovered | 0.669 of Black Mage tier I. |
| `Syphon` | 50 MP damage | 25 MP recovered | 1.190 of Mystic `Empowerment`, but late and target-resource limited. |

MP sequence checks:

| Scenario | Starting MP | Successful casts | Failed casts | Ending MP | Read |
| --- | ---: | ---: | ---: | ---: | --- |
| late dark mix | 100 | 5 | 1 | 8 | Necromancer can layer state, but cannot fit every tool. |
| repeated `Syphon` into 90 target MP | 70 | 3 | 0 | 60 | Target MP is depleted after two useful drains; third cast is a net cost. |

`Syphon` is allowed to be stronger than Mystic `Empowerment` because Necromancer is late and narrower.
It remains a T9 watch item because repeated drains can refill the caster until the target MP is gone.

The drain pair is the thinnest Mystic coexistence point. T2.1 must specifically inspect
Necromancer/Mystic and Mystic/Necromancer builds that stack `Drain`/`Syphon` with
`Invigoration`/`Empowerment`. The intended moat is kit composition: Necromancer brings corpse,
`Death Mark`, `Dark Harvest`, and `Gravebind`, while Mystic brings Faith windows, broader spiritual
control, and `Harmony`.

## Attrition And Finisher Checks

| Skill | Target max HP | Per tick | Ticks | Per-target total | Max targets | Max total |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `Rot` | 180 | 14 | 4 | 56 | 1 | 56 |
| `Rot` | 390 | 31 | 4 | 124 | 1 | 124 |
| `Rot` | 624 | 45 | 4 | 180 | 1 | 180 |
| `Gravebind` | 180 | 10 | 3 | 30 | 3 | 90 |
| `Gravebind` | 390 | 23 | 3 | 69 | 3 | 207 |
| `Gravebind` | 624 | 35 | 3 | 105 | 3 | 315 |

These are full-duration values. They require the status or zone to persist and do not represent
instant burst.

`Gravebind` joins the area-over-time F5 watch family as its own attrition vector. It is not part of
the doc 43/44 weak-element compound: it is non-elemental percent max-HP attrition, so `Belief` and
`Oil` do not amplify it.

`Death Mark` expiry rows:

| Target max HP | HP at expiry | Threshold | Outcome | HP delta |
| ---: | ---: | ---: | --- | ---: |
| 180 | 70 | 90 | lethal if mark persists | 70 |
| 180 | 100 | 90 | fallback nonlethal damage | 36 |
| 390 | 150 | 195 | lethal if mark persists | 150 |
| 624 | 300 | 312 | lethal if mark persists | 300 |
| 624 | 400 | 312 | fallback nonlethal damage | 120 |

`Dark Harvest` rows:

| Target max HP | Current HP | 30% threshold | Eligible by HP only | Damage after cap | KOs if lands |
| ---: | ---: | ---: | --- | ---: | --- |
| 180 | 54 | 54 | yes | 54 | yes |
| 390 | 117 | 117 | yes | 117 | yes |
| 624 | 187 | 187 | yes | 140 | no |
| 624 | 300 | 187 | no | 140 | no |

`Dark Harvest` is a finisher against already-low targets. It is not enough by itself to delete a
durable stress target.

## Corpse And Undead Checks

`Corpse Puppet` rows:

| Scenario | Result | Failure reason | Created object |
| --- | --- | --- | --- |
| KO body, resolves before death clock | success | - | non-acting, targetable, expires after 24 ticks |
| same-tick death clock | fail | death clock expired / same tick unsafe | none |
| boss/unique immune corpse | fail | immunity | none |
| wrong target state | fail | wrong state | none |

The V0 corpse object cannot act, use skills, trigger reactions, inherit gear, or create an extra
turn. Any acting body would require a later T10 approval and is not accepted here.

`Undead Mark` healing inversion examples:

| Incoming heal | Normal target delta | Undead-marked target delta |
| ---: | ---: | ---: |
| 50 | +50 | -50 |
| 108 | +108 | -108 |
| 250 | +250 | -250 |

This is dangerous and must remain battle-scoped and cleanseable. Final Holy Water/item/support
cleanup policy is deferred.

`Undead Mark` healing inversion is a state-inversion hook, not a damage-amplification stack. It can
turn enemy healing into damage against the marked target, but it is not in the `Belief`/`Oil`/weak
element area-damage family and should be tracked separately in F5.

## Lane Separation

Accepted lane target if this pass survives review:

- Black Mage remains the better immediate magical damage caster.
- Mystic remains the broader spiritual/status controller.
- Time Mage remains the tempo/action-window controller.
- Summoner remains the better delayed area payoff caster.
- Necromancer wins only when dark state matters: attrition, drain, undead inversion, KO bodies,
  death countdown, or setup-gated finishing.
- Necromancer does not own global selectors, mapwide spell routing, or free abstract targeting.

## Deferred Items

Still deferred:

- final RSM values for Necromancer, pending T2.1 populated build incidence;
- real Necromancer stat/multiplier row and formula-v1 re-sim; current values use Mystic anchor;
- final `book`/`pole`/`fists` equipment coexistence against Mystic after real weapon data;
- drain-pair coexistence with Mystic, especially `Drain`/`Syphon` stacked with
  `Invigoration`/`Empowerment`;
- final `Rot` and `Gravebind` area/tick behavior after real-roster F5;
- `Gravebind` as a separate area-over-time percent-HP attrition vector outside the
  `Belief`/`Oil`/weak-element compound;
- final `Death Mark` immunity and cleanse policy after T4/T5/T8;
- final `Undead Mark` healing/revive inversion with item/support cleanup policy, tracked as
  state inversion rather than damage amplification;
- `Corpse Puppet` remains non-acting in V0; any acting body requires new T10 approval;
- `Dark Harvest` boss/immune policy and marked-target eligibility during F5;
- final acceptance until T1 Windows weapon dump and formula-balance v1.

## Claude Review Request

Claude should review whether:

- the Mystic-derived Necromancer stat anchor is acceptable for V0 while the real job row is missing;
- the no-Calculator rule is specific enough in concrete terms;
- `Rot`, `Death Mark`, `Drain`, `Syphon`, `Undead Mark`, `Corpse Puppet`, `Command Undead`,
  `Gravebind`, and `Dark Harvest` preserve the V1 dark-state identity;
- `Death Mark` is safe enough as conditional delayed lethal pressure;
- `Dark Harvest` is setup-gated enough and not a hidden instant KO;
- `Syphon` being stronger than Mystic `Empowerment` is acceptable for a late narrower job;
- `Corpse Puppet` correctly follows T3xT5xT8 and creates no acting body;
- `Undead Mark` healing inversion is legible enough for V0 with cleanup deferred;
- this action-only concrete pass is acceptable while RSM values wait for T2.1.

Claude review verdict: accepted as concrete-provisional by claude-opus-4-8 on 2026-06-21.

Review notes:

- all drain, recovery, `Death Mark` fallback, status, attrition, `Dark Harvest`, corpse, undead
  inversion, and MP sequence rows were independently recomputed with 0 regressions;
- the formula block now self-describes `drain_recovery = floor(0.50 * amount)` and
  `death_mark_fallback = min(floor(round(0.20 * max_hp, 6)), 120)`;
- the rounding convention is accepted as round-to-nearest-int damage and floor recovery;
- `Corpse Puppet` honors T3xT5xT8 with `allow_acting_bodies=false`;
- `Drain`/`Syphon` versus Mystic `Invigoration`/`Empowerment` is recorded as a T2.1 coexistence
  watch;
- `Gravebind` is recorded as a separate area-over-time percent-HP attrition vector outside the
  `Belief`/`Oil` weak-element compound;
- `Undead Mark` is recorded as state inversion, not damage amplification;
- final acceptance remains gated by T1 weapon dump, formula-v1, real-roster F5, and T2.1;
- RSM values remain deferred to T2.1 populated incidence.
