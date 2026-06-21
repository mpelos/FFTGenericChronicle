# Area HP Over Time Composition Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/10-healing-attrition-model-schema.md`
- `docs/job-balance/11-ct-delay-model-schema.md`
- `docs/job-balance/16-healing-timing-composition-schema.md`
- `docs/job-balance/34-area-terrain-model-schema.md`
- `docs/job-balance/40-sustained-area-throughput-composition-schema.md`
- `work/t3x-t5x-t11-area-hp-over-time-scenarios-v0.json`

## Purpose

This document starts T3xT5xT11, the area HP over time gate.

T3 validated effective healing, overheal, attrition, and revive plumbing. T5 validated timing
windows. T11 validated area membership and target count. T11xT5 validated sustained area
throughput without HP state.

T3xT5xT11 validates the final missing composition:

```text
area HP effect * tick schedule * changing HP state
```

The immediate consumers are:

- Bard healing songs;
- Dancer HP attrition dances;
- Regen/Poison-like repeated HP effects;
- area healing, area attrition, undead inversion, and HP-state saturation;
- future caster, performer, aura, zone, or Necromancer effects that change HP over time.

T3xT5xT11 V0 does not decide final values, JP, CT, MP, hit rates, status rates, spell formulas,
Faith, exact performance duration, or full AI movement.

## Pinned Bundle

Pinned input bundle:

```text
work/t3x-t5x-t11-area-hp-over-time-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t3x-t5x-t11-area-hp-over-time-v0.json
```

Canonical GPT checker:

```text
tools/check_area_hp_over_time.py
```

## Formula Contract

### Step 1 - Tick Schedule

T3xT5xT11 uses the same tick schedule as T11xT5:

```text
first_tick = start_tick + first_tick_delay
repeat every tick_interval
tick occurs while tick <= start_tick + duration_ticks
tick >= interrupt_tick is canceled, if interrupt_tick exists
```

### Step 2 - Unit Eligibility

A unit is eligible on a tick if:

```text
unit is active at that tick
unit.targetable = true
unit is inside the area shape
unit matches the target group
unit.hp > 0 unless effect.affects_ko = true
```

The default V0 policy is that units at `hp <= 0` stop receiving future HP-over-time ticks. Revive or
corpse effects must opt into a later explicit `affects_ko` rule.

### Step 3 - Area Membership

T3xT5xT11 V0 supports the same area families used by T11xT5:

```text
mapwide
single
diamond
square
```

Area membership is recalculated independently every tick.

### Step 4 - Healing

For each affected living target:

```text
raw_heal = per_target_value
effective_heal = min(raw_heal, max_hp - current_hp)
overheal = raw_heal - effective_heal
current_hp = min(max_hp, current_hp + effective_heal)
```

### Step 5 - Damage / Attrition

For each affected living target:

```text
raw_damage = per_target_value
effective_damage = min(raw_damage, current_hp)
overkill = raw_damage - effective_damage
current_hp = max(0, current_hp - effective_damage)
```

If a unit reaches `hp = 0`, it is reported in `ko_ids` and excluded from later ticks unless a future
scenario explicitly opts into `affects_ko`.

### Step 6 - Undead Healing Inversion

If:

```text
effect.kind = healing
effect.undead_inverts_healing = true
target.undead = true
```

then that target receives damage instead of healing for that tick.

This is a V0 hook for Necromancer and undead interactions. It does not decide the final undead job
rules.

### Step 7 - No-Effect Flag

`no_effect` reports whether no unit was affected on any tick:

```text
no_effect = true if affected_ids_by_tick is empty for every tick
```

It does not mean the effective HP delta was zero. A heal that targets only a full-HP unit still
affected that unit and therefore reports `no_effect = false`, with the value appearing as overheal.

## Scenario Set

The first bundle includes rows for:

- one-tick area healing with overheal cap;
- repeated area healing that saturates HP over time;
- area attrition across multiple enemies;
- KO excluding a unit from future ticks;
- healing inversion on undead;
- targets entering and leaving the area over time;
- unsafe `all_units` collateral damage;
- ally-safe `all_units` excluding collateral;
- diamond area membership plus targetable filtering;
- no valid targets producing no effect.
- full-HP overheal that affects a unit but has zero effective healing;
- interruption canceling later HP ticks;
- square area membership;
- undead healing inversion causing overkill and KO;
- vertical tolerance excluding an elevated unit.

These rows validate area HP-over-time machinery. They do not set final Bard, Dancer, White Mage,
Black Mage, Summoner, Geomancer, Necromancer, aura, zone, or performance values.

## Expected Counter Output

GPT and Claude T3xT5xT11 counters should produce:

- one row per scenario;
- `scenario_id`;
- tick times and tick count;
- affected IDs by tick;
- raw/effective heal by tick;
- overheal by tick;
- raw/effective damage by tick;
- overkill by tick;
- HP snapshots after each tick;
- total raw/effective heal, overheal, raw/effective damage, overkill;
- KO IDs and no-effect flag;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T3xT5xT11 output can be used by concrete area healing, area attrition,
  Bard/Dancer, undead, aura, or zone proposals.

## What T3xT5xT11 V0 Does Not Decide

Still open:

- exact HP values for songs, dances, spells, auras, zones, Poison, Regen, or undead skills;
- Faith, Shell, Protect, element, resistance, absorb, or status accuracy;
- revive/corpse behavior for KO targets;
- full AI movement into or out of area effects;
- item inventory/MP economy for repeated effects;
- final Bard/Dancer performance duration, interruption chance, or `Performing` penalties.

## Claude Review Request

Claude should review whether:

- KO targets should be excluded from future HP-over-time ticks by default in V0;
- undead healing inversion belongs in this gate;
- overheal, overkill, and HP snapshots are sufficient output fields;
- target entry/exit plus area membership cover the core T3xT5xT11 branches;
- any required row is missing before accepting the final infra sprint gate.

Claude review verdict: accepted.

Final validation:

- GPT checker: `scenario_count=15`, `mismatch_count=0`.
- Claude independent checker: `scenarios=15`, `expected_mismatch_count=0`, `gpt_mismatch_count=0`.
- Claude accepted T3xT5xT11 as the final infra-sprint gate on 2026-06-21.
