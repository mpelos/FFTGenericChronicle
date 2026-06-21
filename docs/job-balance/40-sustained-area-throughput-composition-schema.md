# Sustained Area Throughput Composition Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/11-ct-delay-model-schema.md`
- `docs/job-balance/26-bard-dancer-v1-proposal.md`
- `docs/job-balance/34-area-terrain-model-schema.md`
- `docs/job-balance/39-timed-untargetability-composition-schema.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `work/t11x-t5-sustained-area-throughput-scenarios-v0.json`

## Purpose

This document starts T11xT5, the sustained area throughput gate.

T11 validated area membership and target counting. T5 validated tick timing and interruption
windows. T11xT5 validates the composition that Bard, Dancer, auras, zones, and other repeated area
effects need:

```text
per_tick_value * affected_target_count * tick_count
```

T11xT5 V0 is a throughput model. It does not decide final Bard/Dancer values, JP, CT, MP, status
rates, exact song/dance names, performer vulnerability, or whether a specific effect heals, damages,
buffs, or debuffs. It only validates that repeated area value is counted consistently over time.

## Vanilla Reference Consultation

The expanded vanilla atlas should be checked before any performance or sustained area proposal:

- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`

Relevant vanilla vocabulary includes:

- Bard `Seraph Song`, `Life's Anthem`, `Rousing Melody`, `Battle Chant`, `Magickal Refrain`,
  `Nameless Song`, and `Finale`;
- Dancer `Witch Hunt`, `Mincing Minuet`, `Slow Dance`, `Polka`, `Heathen Frolic`,
  `Forbidden Dance`, and `Last Waltz`;
- `Performing`, which should make sustained value interruptible and risky;
- `global`, `healing`, `damage`, `stat_up`, `stat_down`, `mp`, `timing`, and `random` effect tags.

The goal is not to preserve vanilla values. The goal is to prevent global or large-area repeated
effects from becoming either meaningless or mandatory because target count and duration were never
modeled together.

## Pinned Bundle

Pinned input bundle:

```text
work/t11x-t5-sustained-area-throughput-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t11x-t5-sustained-area-throughput-v0.json
```

Canonical GPT checker:

```text
tools/check_sustained_area_throughput.py
```

## Formula Contract

### Step 1 - Tick Schedule

Ticks begin at:

```text
start_tick + first_tick_delay
```

and repeat every:

```text
tick_interval
```

while:

```text
tick <= start_tick + duration_ticks
```

If `interrupt_tick` exists, ticks at or after that tick are canceled:

```text
tick >= interrupt_tick -> canceled
```

This means a same-tick interruption cancels the tick at that moment.

The output field `interrupted` means `interrupt_tick` was present on the scenario. It does not mean
the interrupt necessarily canceled at least one tick; a row with `interrupt_tick` after the final
scheduled tick still reports `interrupted = true`.

### Step 2 - Unit Activity Window

A unit contributes on a tick only if it is active and targetable at that tick:

```text
active_start_tick <= tick
tick < active_expire_tick, if active_expire_tick exists
targetable = true
```

This lets T11xT5 model enemies leaving or entering a sustained area without needing full AI
movement.

### Step 3 - Area Membership

T11xT5 V0 supports these T11 shape families:

```text
mapwide
single
diamond
square
```

Area membership is evaluated independently at each tick. The default mapwide shape hits every
targetable unit allowed by target group.

### Step 4 - Target Group And Ally Safety

Each effect chooses:

```text
enemies
allies
all_units
```

For `all_units`, `ally_safe=true` excludes allied units from hostile throughput. Unsafe `all_units`
effects may include ally collateral through negative ally weighting.

### Step 5 - Per-Tick Score

Each affected unit contributes:

```text
priority * enemy_weight if enemy
priority * ally_weight  if ally
```

Then:

```text
per_tick_score = per_target_value * sum(unit contributions)
```

Values are rounded to six decimals.

### Step 6 - Target Count Cap

If `target_count_cap` exists and raw target count is greater than zero, aggregate score is scaled
proportionally:

```text
cap_scale = min(raw_target_count, target_count_cap) / raw_target_count
per_tick_score = per_tick_score * cap_scale
```

This is not a final Bard/Dancer cap policy. It is a deterministic V0 proxy for testing target-count
normalization without deciding exact performance values.

### Step 7 - Total Score

The sustained throughput proxy is:

```text
total_score = sum(per_tick_scores)
```

The output also reports tick times, raw target counts, effective target counts, affected IDs per
tick, interruption state, and whether the effect produced no value.

## Scenario Set

The first bundle includes rows for:

- mapwide allied recovery baseline;
- mapwide enemy pressure baseline;
- unsafe `all_units` collateral reducing score;
- ally-safe `all_units` excluding collateral;
- interruption before the second tick;
- same-tick interruption canceling the first tick;
- a target leaving mid-performance;
- proportional target-count cap;
- ticks with no valid targets and no effect;
- first tick after duration producing no ticks;
- a diamond zone counting only units inside area and vertical tolerance.
- non-default priority weighting;
- a target entering mid-performance through `active_start_tick`;
- target-count cap combined with weighted unsafe `all_units`;
- square zone diagonal coverage;
- `targetable=false` unit exclusion;
- an interrupt after the final scheduled tick, pinning `interrupted` as interrupt presence.

These rows validate sustained area machinery. They do not set final Bard, Dancer, Summoner,
Geomancer, Time Mage, zone, aura, or performance values.

## Expected Counter Output

GPT and Claude T11xT5 counters should produce:

- one row per scenario;
- `scenario_id`;
- tick times and tick count;
- raw and effective target count by tick;
- affected IDs by tick;
- per-tick scores and total score;
- interruption and no-effect flags;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T11xT5 output can be used by concrete Bard/Dancer, aura, zone, or
  sustained area proposals.

## What T11xT5 V0 Does Not Decide

Still open:

- exact Bard/Dancer performance tick values, durations, JP, CT, MP, hit rates, or status rates;
- exact `Performing` vulnerability, interruption chance, or defensive penalties;
- whether final global effects use hard caps, diminishing returns, per-target caps, or encounter
  budgets;
- random song/dance result tables;
- HP healing or HP attrition interactions, which belong to T3xT5xT11;
- full AI movement into or out of zones.

## Claude Review Request

Claude should review whether:

- same-tick interruption canceling the tick is appropriate for V0;
- target activity windows are sufficient for V0 dynamic target count;
- proportional target-count cap is acceptable as a deterministic normalization proxy;
- unsafe `all_units` collateral and ally-safe exclusion are represented correctly;
- the scenario set covers the Bard/Dancer sustained throughput branches before accepting T11xT5 V0.

Claude review verdict: Accepted on 2026-06-21.

Claude reran the independent checker against the expanded 17-row bundle and GPT output:

- `scenario_count=17`
- `fields_compared=306`
- `mismatch_count=0`

The accepted bundle covers sustained tick schedules, same-tick and after-last interruptions, dynamic
target entry and exit, mapwide/diamond/square shapes, ally-safe and unsafe collateral, priority
weighting, targetability filtering, proportional target-count cap, and no-effect rows.
