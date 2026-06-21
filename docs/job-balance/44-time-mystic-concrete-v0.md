# Time Mage And Mystic Concrete Provisional V0

Status: Accepted for concrete-provisional action values
Date: 2026-06-21
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/20-time-mystic-v1-proposal.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/34-area-terrain-model-schema.md`
- `docs/job-balance/35-resource-economy-model-schema.md`
- `docs/job-balance/36-action-economy-model-schema.md`
- `docs/job-balance/37-ko-corpse-undead-state-composition-schema.md`
- `docs/job-balance/38-spell-routing-reflect-composition-schema.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/gpt-time-mystic-concrete-v0.json`

## Purpose

This is the first concrete-provisional action value pass for Time Mage and Mystic.

It sets action-skill values for review. It does not finalize reaction, support, or movement values.
`Critical: Quick`, `Swiftspell`, `Teleport`, `Halve MP`, `Mana Shield`, `Manafont`, and `Magick
Defense Boost` remain deferred until T2.1 populated build incidence and the later RSM/support pass.

The controller lane is intentionally conservative. Time Mage should change timing windows without
becoming mandatory upkeep, and Mystic should change spiritual/status state without making hard
control the safest answer to every encounter.

## Shared Formula Contracts

Hostile status rows use the current default Faith floor as a status reliability proxy:

```text
effective_status = floor(round(base_hit * 0.60, 6))
```

Ally-only buffs use `100%` on valid targets in V0. Their price is action cost, CT, MP, area limit,
and opportunity cost rather than ally-buff miss chance.

`Gravity`, `Graviga`, and similar proportional HP effects do not scale with MA:

```text
percent_amount = floor(round(percent_of_current_hp * current_hp, 6))
per_target_amount = min(percent_amount, per_target_cap)
nonlethal_amount = min(per_target_amount, current_hp - 1)
```

`Meteor` uses the same Faith-linked magic routine as other damage spells:

```text
amount = floor(round(K * MA * max(0.60, casterFaith * targetFaith / 10000)) * ordinary_layers)
```

`Quick` uses the accepted T10 action-grant policy:

```text
party_grant_cap = 1
per_target_grant_cap = 1
block_granted_action_grants = true
```

Faith-window effects are battle-scoped ordinary layers in V0:

```text
Belief = 1.15 Faith-linked amount layer
Disbelief = 0.80 Faith-linked amount layer
```

These layers are deliberately small because they can affect every caster.

## Time Mage Values

Time Mage is a tempo controller. The V0 numeric lane is:

- `Haste` and `Slow` change a short turn window, not the whole fight;
- `Quick` is one action spent for one action granted, with recursion blocked;
- `Gravity` and `Graviga` pressure high HP but do not kill;
- `Meteor` is a slower, unsafe, off-role area capstone set just below `Bahamut` on maximum total.

| Skill | Effect | Value | MP | CT | JP | Gate binding |
| --- | --- | --- | ---: | ---: | ---: | --- |
| `Haste` | speed buff | x1.50 Speed, 24 ticks | 8 | 2 | 100 | T5/T9 |
| `Hasteja` | area speed buff | x1.50 Speed, 24 ticks, max 3 allies | 26 | 5 | 600 | T5/T11/T9 |
| `Slow` | speed debuff | x0.67 Speed, 24 ticks, 80 base hit | 8 | 2 | 80 | T4/T5/T9 |
| `Slowja` | area speed debuff | x0.67 Speed, 24 ticks, 65 base hit, max 3 enemies | 26 | 5 | 600 | T4/T5/T11/T9 |
| `Stop` | CT freeze | 12 ticks, 45 base hit | 20 | 3 | 350 | T4/T5/T9 |
| `Immobilize` | movement lock | 24 ticks, 85 base hit | 8 | 2 | 100 | T4/T5/T9 |
| `Float` | terrain utility | 36 ticks, ally utility | 10 | 1 | 200 | T5/T11/T9 |
| `Reflect` | spell routing | 24 ticks, 100 ally / 60 enemy base hit | 16 | 2 | 300 | T8xSR/T5/T9 |
| `Quick` | action grant | one grant, no recursion | 42 | 4 | 900 | T10/T5/T9 |
| `Gravity` | current-HP pressure | 25% current HP, cap 120, nonlethal | 20 | 3 | 250 | T3/T5/T9 |
| `Graviga` | area current-HP pressure | 20% current HP, cap 90, max 3, nonlethal | 36 | 5 | 550 | T3/T5/T11/T9 |
| `Meteor` | unsafe area damage | K 14, max 3 targets, expected 1.8 | 58 | 10 | 900 | F4/T5/T11/T9 |

### Time Tempo Checks

Tempo rows use a 24-tick window from current CT 40.

| Speed | Base turns | Haste turns | Haste delta | Slow turns | Slow delta |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 6 | 1 | 2 | +1 | 1 | 0 |
| 8 | 2 | 3 | +1 | 1 | -1 |
| 9 | 2 | 3 | +1 | 1 | -1 |

This makes `Haste` visible without doubling the whole fight. At representative Speed 8, `Haste`
adds one turn in the window and `Slow` removes one turn in the window.

Area tempo rows at Speed 8:

| Skill | Max targets | Effective hit | Max turn delta | Expected turn delta | Read |
| --- | ---: | ---: | ---: | ---: | --- |
| `Hasteja` | 3 allies | 100% | +3 | +3.00 | Very strong, paid by CT 5, MP 26, and setup action. |
| `Slowja` | 3 enemies | 39% | -3 | -1.17 | Powerful if it lands, but not reliable hard-lock. |

`Hasteja` is a major F5/T2.1 watch item. It is intentionally exciting, but if build incidence shows
it becoming mandatory, the default first cut is to reduce the target cap from 3 allies to 2 allies.
If the job still over-indexes after that, shorten the window before touching the single-target
`Haste` lane. The trigger for this cut is T2.1 showing `Hasteja` or Time Magicks appearing as a
late-party default rather than as a committed tempo-controller plan.

### Quick Checks

| Scenario | Success | Failure reason | Source actions spent | Successful grants | Net action delta |
| --- | --- | --- | ---: | ---: | ---: |
| normal one-for-one | yes | - | 1 | 1 | 0 |
| invalid target | no | target cannot receive | 1 | 0 | -1 |
| recursion attempt | no | recursion blocked | 1 | 0 | -1 |
| second grant in capped window | no | party cap | 1 | 0 | -1 |

The accepted V0 rule is that `Quick` creates timing value, not free action economy. It can still be
very strong because moving an action earlier can save a unit, land a combo, or finish a threat.
That strength belongs to T10 and T2.1, not to raw damage formulas.

### Gravity And Meteor Checks

| Skill | Target HP | Raw percent | Per-target after cap | Max targets | Max total |
| --- | ---: | ---: | ---: | ---: | ---: |
| `Gravity` | 180 | 45 | 45 | 1 | 45 |
| `Gravity` | 390 | 97 | 97 | 1 | 97 |
| `Gravity` | 624 | 156 | 120 | 1 | 120 |
| `Graviga` | 180 | 36 | 36 | 3 | 108 |
| `Graviga` | 390 | 78 | 78 | 3 | 234 |
| `Graviga` | 624 | 124 | 90 | 3 | 270 |

Gravity spells are high-HP pressure and setup, not finishers.

Stress `Meteor` uses Time Mage MA 16:

| Skill | K | Per target | Expected targets | Expected total | Max targets | Max total | Max / 415 | Max / Bahamut |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `Meteor` | 14 | 134 | 1.8 | 241.2 | 3 | 402 | 0.969 | 0.993 |

`Meteor` is deliberately close to `Bahamut` on maximum total, but worse in practical reliability:
CT 10, MP 58, unsafe all-units area, and lower expected target count. It is a binding F5 watch item
alongside `Bahamut`. They must be rechecked together because both ride the same ceiling line:
`Meteor` is 402/415 and `Bahamut` is 405/415. If the T1 weapon dump lowers the top-physical
reference or real MA exceeds the anchors, both capstones can cross the provisional ceiling in the
same F5 sweep.

### Time Mage MP Checks

| Scenario | Starting MP | Successful casts | Failed casts | Ending MP | Read |
| --- | ---: | ---: | ---: | ---: | --- |
| mid control mix | 90 | 4 | 1 | 38 | Haste, Slow, Reflect, and Gravity fit; Quick fails afterward. |
| late premium pressure | 120 | 2 | 1 | 52 | Quick plus Hasteja fit; Meteor does not also fit. |

The MP rows make the expensive tempo actions mutually exclusive inside one tactical sequence.

## Mystic Values

Mystic is a spiritual/status controller. The V0 numeric lane is:

- soft statuses sit around 45-51% effective hit at default Faith;
- hard control is low accuracy and short duration;
- Faith changes are battle-scoped amount layers, not permanent Faith grief;
- drains are useful but too small to replace Black Mage, White Mage, or Summoner;
- MP drain can produce short-term resource gain but depletes the target resource.

| Skill | Effect | Value | MP | CT | JP | Gate binding |
| --- | --- | --- | ---: | ---: | ---: | --- |
| `Chant` | Mystic setup | next Mystic Art +10 base status hit, no stacking | 0 | 0 | 0 | T4/T5 |
| `Umbra` | Blind | 75 base hit, 24 ticks | 8 | 2 | 100 | T4/T5/T9 |
| `Empowerment` | MP drain | K 5 MP damage, 50% drain to caster | 8 | 2 | 200 | T9 |
| `Invigoration` | HP drain | K 10 damage, 50% drain to caster | 14 | 3 | 350 | T3/T5/F4/T9 |
| `Belief` | Faith window | 1.15 Faith amount layer, 24 ticks | 12 | 2 | 400 | F4/F5/T4/T5/T9 |
| `Disbelief` | Faith window | 0.80 Faith amount layer, 24 ticks | 12 | 2 | 400 | F4/F5/T4/T5/T9 |
| `Corruption` | undead mark | 65 base hit, 24 ticks | 14 | 3 | 300 | T3/T4/T5/T8/T3xT5xT8 |
| `Quiescence` | Silence | 75 base hit, 24 ticks | 10 | 2 | 170 | T4/T5/T9 |
| `Fervor` | Berserk | 60 base hit, 18 ticks | 14 | 3 | 400 | T4/T5/T8/T9 |
| `Trepidation` | Brave window | -15 Brave, 24 ticks, 80 base hit | 8 | 2 | 200 | T4/T5/F5 |
| `Delirium` | Confuse | 55 base hit, 12 ticks | 18 | 3 | 400 | T4/T5/T8/T9 |
| `Harmony` | spiritual cleanup | clears Mystic spiritual/control set | 16 | 2 | 800 | T4/T5/T9 |
| `Hesitation` | Disable | 60 base hit, 12 ticks | 12 | 2 | 100 | T4/T5/T9 |
| `Repose` | Sleep | 60 base hit, 12 ticks | 18 | 3 | 350 | T4/T5/T9 |
| `Induration` | Stone | 35 base hit, 8 ticks | 30 | 5 | 600 | T4/T5/T9 |

### Mystic Status Checks

Default hostile rows multiply base hit by the 0.60 Faith floor.

| Skill | Status | Base hit | Effective hit | Effective after `Chant` |
| --- | --- | ---: | ---: | ---: |
| `Umbra` | Blind | 75 | 45 | 51 |
| `Quiescence` | Silence | 75 | 45 | 51 |
| `Trepidation` | Brave window | 80 | 48 | 54 |
| `Corruption` | Undead-mark | 65 | 39 | 45 |
| `Fervor` | Berserk | 60 | 36 | 42 |
| `Hesitation` | Disable | 60 | 36 | 42 |
| `Repose` | Sleep | 60 | 36 | 42 |
| `Delirium` | Confuse | 55 | 33 | 39 |
| `Induration` | Stone | 35 | 21 | 27 |

`Chant` is allowed because it spends an action to improve one Mystic Art. It should not stack and
should not improve non-Mystic statuses.

### Mystic Drain And Faith Checks

Stress Mystic uses MA 14.

| Skill | Stress value | Recovery | Read |
| --- | ---: | ---: | --- |
| `Empowerment` | 42 MP damage | 21 MP recovered | Useful anti-caster pressure, target-resource limited. |
| `Invigoration` | 84 HP damage | 42 HP recovered | 0.556 of Black Mage tier I; sustain, not burst. |

Faith-window projections:

| Reference | Base | `Belief` 1.15 | `Belief` / 415 | `Disbelief` 0.80 |
| --- | ---: | ---: | ---: | ---: |
| Black Mage stress `Flare` | 324 | 372 | 0.896 | 259 |
| Black Mage stress tier IV | 238 | 273 | 0.658 | 190 |
| White Mage stress `Holy` | 250 | 287 | 0.692 | 200 |
| `Meteor` stress per target | 134 | 154 | 0.371 | 107 |

`Belief` is useful but intentionally small. Belief plus weak-element Black Magic remains an F5 watch
item because element weakness is already a high-payoff condition.

The larger watch item is the cross-job compound already named in doc 43:

```text
Mystic Belief x Geomancer Magma Surge/Oil x Summoner Ifrit or Salamander area
```

Doc 43 already flags the constructible `Oil` into fire-summon cluster as not-final. `Belief` adds a
third job and turns the weak-element multiplier stack into a 2.30x Faith-linked area vector. F5 must
test the full three-job stack as one compound case, not Belief-versus-weakness in isolation.

| Base summon row | Neutral per target | Combined layer | Per target | 3-target total |
| --- | ---: | ---: | ---: | ---: |
| `Ifrit` | 81 | 2.30 | 186 | 558 |
| `Salamander` | 99 | 2.30 | 227 | 681 |

These totals are not accepted as final balance values. They are named so the real-roster F5 pass
tests the actual cross-job dominance vector.

### Mystic MP Checks

| Scenario | Starting MP | Successful casts | Failed casts | Ending MP | Read |
| --- | ---: | ---: | ---: | ---: | --- |
| mixed control | 80 | 5 | 0 | 27 | Control plus one drain is affordable but spends most MP. |
| repeated `Empowerment` into 80 target MP | 60 | 3 | 0 | 52 | Target MP is depleted after two useful drains; the third cast becomes a net cost. |

The repeated `Empowerment` row is a T9 watch item. It is acceptable only if the target-resource cap
and action cost remain visible; it must not become an infinite MP engine.

## Lane Separation

Accepted lane target if this pass survives review:

- Black Mage remains the main fast magical damage caster.
- Summoner remains the reliable delayed clustered area caster.
- Time Mage owns tempo, action windows, Reflect routing, Gravity setup, and the slow unsafe Meteor.
- Mystic owns Faith windows, anti-caster status, spiritual control, and bounded drain.
- Hard control exists, but low hit chance and short duration keep it from replacing damage.
- Faith manipulation is tactical and battle-scoped, not permanent character grief.
- Mystic viability must be checked on its non-hard-control value: Faith windows, bounded drains,
  anti-caster pressure, cleanup, and soft debuffs. Hard control is intentionally low enough that it
  should not be the job's main reason to exist.

## Deferred Items

Still deferred:

- final RSM values for Time Mage and Mystic, pending T2.1 populated build incidence;
- `Critical: Quick`, `Swiftspell`, `Teleport`, `Halve MP`, `Mana Shield`, `Manafont`, and `Magick
  Defense Boost`;
- full Haste/Slow duration semantics after richer T5 turn-queue rows;
- final Quick acceptance after T10 plus real build-incidence checks;
- `Meteor` real-roster F5 against `Bahamut` and top physical;
- `Hasteja` T2.1/F5 incidence because +3 ally turns in a window is strong;
- `Belief` plus weak-element magic during real-roster F5, especially the full cross-job
  `Belief` x `Magma Surge`/`Oil` x `Ifrit`/`Salamander` area stack from doc 43;
- Mystic viability if hard-control rows prove too low to carry the job by themselves;
- final status immunity, boss policy, and exact durations after T4/T8 rows;
- final acceptance until T1 Windows weapon dump and formula-balance v1.

## Claude Review Request

Claude should review whether:

- Haste/Slow 24-tick windows create useful tempo without mandatory upkeep;
- `Hasteja` at +3 max turn delta is acceptable with CT 5 and MP 26, or must be cut now;
- `Quick` T10 rows correctly keep normal Quick at net action delta 0 and block recursion;
- `Gravity`/`Graviga` percent rows are bounded and nonlethal;
- `Meteor` at 402 max total is acceptable given CT 10, MP 58, unsafe area, and expected 1.8 targets;
- Mystic status rates and short hard-control durations are useful but not oppressive;
- `Belief`/`Disbelief` as 1.15/0.80 battle-scoped Faith amount layers are safe enough for V0;
- `Empowerment` MP drain is target-resource limited enough to avoid an infinite loop;
- this action-only concrete pass is acceptable while RSM values wait for T2.1.

Claude review verdict: accepted as concrete-provisional by claude-opus-4-8 on 2026-06-21.

Review notes:

- Time Mage and Mystic action values were independently recomputed from the accepted gate models:
  T5 tempo, T10 `Quick`, percent-HP guard, T9 MP, F4 Faith routine, and status x0.60;
- `Quick` keeps normal use at net action delta 0 and blocks recursive action grants;
- `Hasteja` is accepted only with the named T2.1/F5 watch: default first cut is 3 allies to 2
  allies, then shortening the 24-tick window if still mandatory;
- `Meteor` and `Bahamut` are a joint F5 ceiling watch at 402 versus 405;
- the three-job `Belief` x `Magma Surge`/`Oil` x fire summon compound is recorded with concrete
  worst-case totals: `Ifrit` 558 and `Salamander` 681 across three targets;
- the compound rows are not accepted as final balance values; they are the named real-roster F5
  dominance investigation;
- final acceptance remains gated by T1 weapon dump, formula-v1, real-roster F5, and T2.1;
- RSM values remain deferred to T2.1 populated incidence.
