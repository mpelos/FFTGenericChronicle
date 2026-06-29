# Healing Timing Composition Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/10-healing-attrition-model-schema.md`
- `docs/job-balance/11-ct-delay-model-schema.md`
- `work/t3-healing-attrition-scenarios-v0.json`
- `work/t5-ct-delay-scenarios-v0.json`
- `work/t3x-t5-healing-timing-scenarios-v0.json`

## Purpose

This document starts the T3xT5 composition: healing, revive, and automatic recovery under timing
pressure.

T3 validated action-normalized recovery amounts and resources. T5 validated CT, delay, and same-tick
timing. T3xT5 asks the next required question:

```text
Does the recovery resolve in time to matter?
```

The immediate consumers are:

- Squire `First Aid`;
- Chemist `Potion`, `Phoenix Down`, `Auto-Potion`, `Item Lore`, and item reliability;
- White Mage delayed healing comparison;
- future Monk, Time Mage, Necromancer, Bard, Dancer, and Ramza sustain or revive designs.

## Scope

T3xT5.0 is still not a full encounter simulator.

It models a single healing race:

- active healing before, after, or on the same tick as incoming danger;
- fast-but-weak versus slow-but-strong delivery comparison;
- automatic reaction healing after damage;
- revive timing before death-clock expiration;
- T3 effective-heal and expected-heal values under T5 timing rules.

It does not model:

- movement/range pathfinding;
- enemy target selection;
- multiple actors in a full CT queue;
- spell interruption by damage;
- AoE healing;
- status prevention versus cure;
- random Phoenix Down HP;
- inventory economy beyond the resource expected values inherited from T3.

## Pinned Bundle

Pinned input bundle:

```text
work/t3x-t5-healing-timing-scenarios-v0.json
```

The bundle defines timing race rows and expected values for the first dual-independent T3xT5 run.

## Formula Contract

T3xT5.0 inherits:

- T3 effective healing:
  `effective_heal = min(raw_heal, missing_hp)`;
- T3 reaction expected value:
  `effective_heal * trigger_chance * min(incoming_triggers, per_round_cap)`;
- T5 same-tick policy:
  a same-tick race is unsafe for the delayed action.

### Active Healing Race

For active item or spell healing:

```text
heal_before_threat = resolution_delay_ticks < threat_tick
same_tick_unsafe = resolution_delay_ticks == threat_tick
```

If healing resolves before the threat:

```text
hp_after_heal = min(max_hp, hp_before + effective_heal)
hp_after_threat = hp_after_heal - incoming_damage
heal_resolved = true
```

If the threat lands before or on the same tick as healing:

```text
hp_after_threat = hp_before - incoming_damage
heal_resolved = hp_after_threat > 0
```

Delayed healing can still matter after a nonlethal hit. It cannot prevent a KO if the target is
already down before resolution.

### Delivery Comparison

When comparing multiple delivery options in the same timing window:

```text
evaluate each option using the active healing race
reliable options are options where survives = true
best option sorts by survives, then final_hp, then scenario order
```

This is the first T3xT5 reliability check that can say when a weaker immediate item is better than
a stronger delayed spell, or when the delayed spell is safe enough to win.

### Reaction Healing After Damage

Reaction healing is post-damage and survivor-only:

```text
hp_after_damage = hp_before - incoming_damage
reaction_can_resolve = hp_after_damage > 0
timed_expected_heal = effective_heal * trigger_chance * effective_triggers
  if reaction_can_resolve
timed_expected_heal = 0 otherwise
```

This means Auto-Potion-like effects can recover from a nonlethal hit, but they do not prevent a
lethal hit in T3xT5.0.

### Revive Race

For revive timing:

```text
revive_before_death_clock = resolution_delay_ticks < death_clock_ticks
```

Same-tick revive is unsafe in T3xT5.0. A later gameflow-accurate pass can replace this if exact
phase order proves otherwise.

## Scenario Set

The first bundle includes rows for:

- immediate item healing preventing a KO;
- delayed spell healing losing to a lethal incoming hit;
- delayed spell healing resolving before danger;
- immediate weak item beating stronger delayed spell when danger arrives first;
- stronger delayed spell beating immediate weak item when there is enough time;
- max-HP cap binding after delayed healing;
- same-tick healing being unsafe;
- delayed healing still resolving after a nonlethal hit;
- Auto-Potion-like reaction healing after a nonlethal hit;
- Auto-Potion-like reaction failing to prevent lethal damage;
- Auto-Potion-like reaction with multiple effective triggers capped by `per_round_cap`;
- delivery comparison where multiple options survive and the higher final HP wins;
- delivery comparison tie-break where multiple reliable options land at equal final HP;
- Phoenix Down-like revive before death-clock expiration;
- same-tick revive being too late;
- overheal-capped item healing before a threat.

These rows validate composition machinery. They do not set final values for any healing, revive, or
reaction skill.

## Expected Counter Output

GPT and Claude T3xT5 counters should produce:

- one row per scenario;
- `scenario_id`;
- `model`;
- timing fields relevant to the model;
- `timed_expected_heal`;
- final HP or expected final HP;
- validation errors, if any.

The dual-independent gate from document 07 applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T3xT5 output can be used to accept or reject healing values.

## What T3xT5.0 Does Not Decide

Still open for later composition versions:

- exact Chemist item values;
- exact White Mage spell CTR and spell power values;
- exact Auto-Potion trigger chance, item tier, and per-round cap;
- exact Phoenix Down revive HP;
- cast interruption and Silence/Disable prevention;
- full death-clock/crystal/chest phase ordering;
- whether Time Mage can improve healing timing through CT manipulation.

## Claude Review Request

Claude should review whether:

- the survivor-only reaction rule is the right T3xT5.0 assumption;
- same-tick unsafe should apply to healing and revive races in this first pass;
- the active-heal race handles delayed-but-nonlethal cases clearly;
- the scenario set is enough to use this composition before concrete healing values;
- any additional row is required before accepting T3xT5.0.

Claude review verdict: Accepted after adding delivery comparison, max-HP cap, multi-trigger
reaction, and reliable-option tie-break rows requested during review (claude-opus-4-8, 2026-06-21).
