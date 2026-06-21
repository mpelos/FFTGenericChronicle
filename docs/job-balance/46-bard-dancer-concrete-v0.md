# Bard And Dancer Concrete Provisional V0

Status: Accepted for concrete-provisional action values
Date: 2026-06-21
Depends on:
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/job-balance/26-bard-dancer-v1-proposal.md`
- `docs/job-balance/40-sustained-area-throughput-composition-schema.md`
- `docs/job-balance/41-area-hp-over-time-composition-schema.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `work/gpt-bard-dancer-concrete-v0.json`

## Purpose

This is the first concrete-provisional value pass for Bard and Dancer.

It sets action-skill values for review and records a machine-diffable parity block for shared
reaction/support/movement. It does not finalize Bard/Dancer RSM numeric values because the populated
T2.1 build-incidence pass is still pending.

Bard and Dancer remain the only generic gender-restricted pair. Their action commands differ, but
their reaction, support, and movement records must remain exactly equal.

Claude review verdict: accepted on 2026-06-21 with no required changes.

## Vanilla Reference Pass

This pass consulted the vanilla atlas before assigning values:

- `Bardsong` and `Dance` command families;
- `global`, `healing`, `damage`, `mp`, `stat_up`, `stat_down`, `brave_up`, `timing`, `random`,
  `status_add`, `instant_ko`, and `ct_action` effect tags;
- `Performing`, `Haste`, `Slow`, `Poison`, `Regen`, `Blind`, `Silence`, `Immobilize`, `Oil`, and
  `KO` status/effect interactions.

Trust boundary:

- local ability ID, name, JP, random flags, CT overrides, and MP overrides are extracted facts;
- command ownership, effect summaries, tags, and status categories are consultation
  classifications;
- base Formula/X/Y/range/area/status bytes are not present in the local CSV and remain proof-first.

Name drift note: the local IVC records use `Seraph Song`, `Life's Anthem`, `Rousing Melody`,
`Battle Chant`, `Magickal Refrain`, `Nameless Song`, `Finale`, `Witch Hunt`, `Mincing Minuet`,
`Slow Dance`, `Polka`, `Heathen Frolic`, `Forbidden Dance`, and `Last Waltz`. External legacy guides
may use Angel/Life/Last Song or Wiznaibus/Disillusion/Last Dance vocabulary. This pass binds the
local slots and rejects both ordinary mapwide instant KO and free action-economy capstones.

## Shared Performance Model

Performance actions use a sustained tick model:

```text
ticks = 4, 12, 20, 28
interrupt_tick cancels ticks at or after that tick
```

Default normalization:

```text
default target cap = 4
strong effect target cap = 3
random song/dance target cap = 1 per tick
```

`Performing` is the cost for global value:

- the performer is committed while the performance is active;
- the performer cannot take other actions while performing;
- evasion is suppressed or reduced;
- interruption cancels future ticks.

HP performance formulas use both the target and the performer:

```text
Bard HP tick  = min(floor(round(percent * target_max_hp, 6)), cap, floor(round(K * BardMA * 0.60, 6)))
Dancer HP tick = min(floor(round(percent * target_max_hp, 6)), cap, floor(round(K * DancerPA * 0.60, 6)))
```

That keeps global HP effects from scaling freely against high-HP targets without involving the
performer's stats.

## Shared Reaction, Support, And Movement

RSM values are deferred, but the parity set is locked as a shared block for the next T2.1 pass:

| Slot | Skill | Role | Concrete value status |
| --- | --- | --- | --- |
| Reaction | `Earplugs` | Narrow anti-performance/speech defense. | Deferred |
| Reaction | `Encore` | Performance resilience candidate. | Deferred, high risk |
| Support | `Performance Mastery` | Song/dance specialization only. | Deferred |
| Support | `Stagecraft` | Narrow performer setup or self-risk reduction. | Deferred |
| Movement | `Performance Step` | Narrow performer positioning. | Deferred |
| Movement | `Fly` | Promotion candidate only if performers cannot function without it. | Deferred, high risk |

The data pass must compare Bard and Dancer RSM records field-by-field. Action sets may differ; RSM
records may not.

## Bard Values

Bard remains the support performer. Songs are slow global value, not direct White Mage, Time Mage,
or Orator replacement.

| Skill | Value | JP | Gate binding | Notes |
| --- | --- | ---: | --- | --- |
| `Seraph Song` | 5% max HP per tick, K 3.0 MA cap, per-tick cap 22, max 4 allies, 4 ticks | 100 | T3/T5/T11/T3xT5xT11 | Gentle global recovery; living allies only. |
| `Life's Anthem` | 7% max HP per tick, K 4.0 MA cap, per-tick cap 32, max 3 allies, 4 ticks | 200 | T3/T5/T11/T3xT5xT11 | Stronger recovery; no mapwide Arise loop. |
| `Rousing Melody` | +6 CT per completed tick, max 3 allies, 4 ticks | 250 | T5/T10/T11xT5 | No same-tick action grant, no Quick recursion. |
| `Battle Chant` | +2 Brave/morale per tick, +8 battle-scoped cap, max 4 allies | 200 | T2/T5/T11xT5 | No permanent Brave economy. |
| `Magickal Refrain` | +0.015 outgoing magic layer per tick, cap 1.06, max 4 allies | 250 | F4/F5/T11xT5 | Does not stack with Belief; use stronger layer. |
| `Nameless Song` | One random ally per tick; table: Protect, Shell, Float, Regen | 350 | T4/T8/T11xT5 | Excludes Reraise, Haste, Invisible, immunity, instant action. |
| `Finale` | Requires 2 completed song ticks, ends song, 12% max HP pulse, K 8.0 MA cap, pulse cap 60, max 4 allies, clears one light debuff | 600 | T3/T4/T5/T8/T11 | No revive, instant KO, or action grant. |

`Finale` can clear one of:

```text
Poison, Blind, Silence, Slow, Immobilize, Oil
```

It is deliberately a setup capstone, not a cold mapwide win button.

## Dancer Values

Dancer remains the pressure performer. Dances are slow global attrition/debuff pressure, not direct
Black Mage, Mystic, Time Mage, or Knight replacement.

| Skill | Value | JP | Gate binding | Notes |
| --- | --- | ---: | --- | --- |
| `Witch Hunt` | MP damage per tick = min(current MP, 12, floor(round(1.90 * DancerMA * 0.60, 6))), max 4 enemies, 4 ticks | 100 | T9/T11xT5 | Battle-scoped caster/resource pressure. |
| `Mincing Minuet` | 4% max HP per tick, K 3.0 PA cap, per-tick cap 20, max 4 enemies, 4 ticks | 200 | T3/T5/T11/T3xT5xT11/F5 | Low global damage over time; can KO. |
| `Slow Dance` | -6 CT per completed tick, max 3 enemies, 4 ticks | 250 | T5/T10/T11xT5 | No same-tick hard action denial, not Stop. |
| `Polka` | -0.03 outgoing physical layer per tick, floor 0.88, max 4 enemies | 200 | T7/T11xT5 | Battle-scoped physical output pressure. |
| `Heathen Frolic` | -0.03 outgoing magic layer per tick, floor 0.88, max 4 enemies | 250 | F4/T9/T11xT5 | Mystic remains targeted Faith/status controller. |
| `Forbidden Dance` | One random enemy per tick, 35% base / 21% default effective chance; table: Blind, Silence, Slow, Immobilize, Poison | 350 | T4/T8/T11xT5 | Excludes hard-control lottery results. |
| `Last Waltz` | Requires 2 completed dance ticks, ends dance, 12% max HP nonlethal pulse, K 8.0 PA cap, pulse cap 60, max 4 enemies | 600 | T3/T4/T5/T8/T11 | No instant KO, Stop, action denial, or CT-set hard lock. |

`Forbidden Dance` excludes:

```text
Stop, Sleep, Stone, Charm, Death, Toad, Vampire, Traitor
```

`Last Waltz` cannot KO. If a future version makes it a real finisher, it needs explicit boss/immune
exclusions and weakened-target preconditions before acceptance.

## Simulation Highlights

Stress reference inputs:

| Reference | Value |
| --- | ---: |
| Bard MA | 14 |
| Dancer PA / MA | 13 / 11 |
| Top physical stress reference | 415 |
| Bahamut 3-target reference | 405 |
| White Mage `Curaja` stress single target | 250 |
| Black Mage `Flare` stress single target | 324 |

Bard HP rows:

| Target max HP | Seraph tick | Seraph full cap 4 | Life tick | Life full cap 3 | Finale cap 4 |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 180 | 9 | 144 | 12 | 144 | 84 |
| 390 | 19 | 304 | 27 | 324 | 184 |
| 624 | 22 | 352 | 32 | 384 | 240 |

Dancer HP rows:

| Target max HP | Minuet tick | Minuet full cap 4 | Last Waltz cap 4 |
| ---: | ---: | ---: | ---: |
| 180 | 7 | 112 | 84 |
| 390 | 15 | 240 | 184 |
| 624 | 20 | 320 | 240 |

Interruption at tick 12 on HP 390 stress targets:

| Effect | Full value | Interrupted value |
| --- | ---: | ---: |
| `Seraph Song`, cap 4 | 304 | 76 |
| `Life's Anthem`, cap 3 | 324 | 81 |
| `Mincing Minuet`, cap 4 | 240 | 60 |

This is the core performance tradeoff: a protected performer gets meaningful sustained value; a
punished performer loses most of it.

Tempo rows use Speed 8 and duration 28:

| Initial CT | Base turns / rem | Rousing turns / rem | Slow Dance turns / rem |
| ---: | --- | --- | --- |
| 40 | 2 / 64 | 2 / 88 | 2 / 40 |
| 70 | 2 / 94 | 3 / 18 | 2 / 70 |

Rousing and Slow Dance can swing an already-near turn, but they are not Quick, Hasteja, Stop, or
global action denial.

Layer rows:

| Row | Result |
| --- | ---: |
| `Flare` 324 with max `Magickal Refrain` 1.06 | 343 |
| Top physical 415 with max `Polka` 0.88 | 365 |
| `Flare` 324 with max `Heathen Frolic` 0.88 | 285 |
| `Forbidden Dance` expected status applications over four ticks | 0.84 |

Capstone and throughput ratios:

| Row | Total | Ratio |
| --- | ---: | ---: |
| `Finale`, HP 624, cap 4 / top physical 415 | 240 | 0.578313 |
| `Last Waltz`, HP 624, cap 4 / top physical 415 | 240 | 0.578313 |
| `Mincing Minuet`, HP 624, cap 4 / top physical 415 | 320 | 0.771084 |
| `Seraph Song`, HP 624, cap 4 / Bahamut 405 | 352 | 0.869136 |
| `Life's Anthem`, HP 624, cap 3 / Bahamut 405 | 384 | 0.948148 |

Random variance rows:

| Skill | Min | Expected | Max | Notes |
| --- | ---: | ---: | ---: | --- |
| `Nameless Song` applications, no eligible targets to one target per tick | 0 | 4 | 4 | With a uniform 4-result table, each result expects 1 application over 4 ticks; any one result can appear 0-4 times. |
| `Forbidden Dance` successful statuses | 0 | 0.84 | 4 | 4 attempts at 21% effective chance; probability of at least one success is 0.610499. |
| `Forbidden Dance` per specific status in uniform 5-result table | 0 | 0.168 | 4 | No hard-control results are in the table. |

## Lane Separation

Accepted provisional lane separation:

- Bard wins when protected global recovery, morale, or mild magic support is better than a direct
  healer/caster action.
- White Mage still owns immediate focused delayed healing, revive, and mitigation spells.
- Orator remains the targeted Brave/Faith/social controller; Bard morale is slow and global.
- Time Mage remains the targeted tempo specialist; Bard/Dancer CT pulses are smaller and
  performance-gated.
- Dancer wins when slow global pressure is better than direct burst or targeted status.
- Black Mage and Summoner still own direct damage and delayed area damage.
- Mystic remains the targeted status/Faith controller.

## Watch Items

- `Life's Anthem` at HP 624 cap 3 totals 384 over a full uninterrupted performance, close to
  Bahamut 405. It is acceptable only if interruption and target caps remain real.
- `Seraph Song` at HP 624 cap 4 totals 352 and must not replace White Mage/Chemist in F5.
- `Mincing Minuet` at HP 624 cap 4 totals 320 over time and must not replace Black Mage/Summoner.
- `Magickal Refrain` must not stack with `Belief` in this first concrete pass, preventing the known
  Belief x Oil x fire-weak compound from growing.
- `Finale` and `Last Waltz` are nonlethal/non-action-economy capstones in V0. Any later finisher
  rewrite requires T8 boss/immune exclusions.

## Deferred Items

Still deferred:

- exact RSM numeric values and final inclusion/cutting of `Encore` and `Fly`, pending T2.1;
- final mapwide geometry and target membership, pending final T11 implementation;
- exact `Performing` implementation details;
- full five-metric real-roster F5 sweep after formula-v1 and T1 data;
- final JP/campaign pacing after the broader progression pass;
- final roster-level acceptance until T1/formula-v1, F5, and T2.1.

## Claude Review Verdict

Claude accepted this pass as concrete-provisional. Independent recomputation reported zero numeric
mismatches across:

- Bard and Dancer HP-over-time rows;
- band scaling on HP 390;
- interruption at tick 12;
- tempo rows at Speed 8;
- layer rows;
- capstone and throughput ratios;
- random variance rows.

Claude also accepted that:

- the reference pass covers all relevant vanilla skill/effect/status families;
- the performance model makes `Performing` a real cost;
- HP songs/dances are correctly target-count-normalized and interruption-sensitive;
- `Rousing Melody` and `Slow Dance` stay out of Quick/Hasteja/Stop territory;
- `Nameless Song` and `Forbidden Dance` random tables are bounded enough;
- `Finale` and `Last Waltz` are safely rewritten away from ordinary instant KO and free action
  economy;
- the shared RSM parity block is machine-diffable enough for the later data pass.
