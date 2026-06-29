# KO Corpse Undead State Composition Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/10-healing-attrition-model-schema.md`
- `docs/job-balance/11-ct-delay-model-schema.md`
- `docs/job-balance/15-targeting-challenge-model-schema.md`
- `docs/job-balance/16-healing-timing-composition-schema.md`
- `work/t3x-t5x-t8-ko-corpse-undead-scenarios-v0.json`

## Purpose

This document starts T3xT5xT8, the KO/corpse/undead state composition gate.

Necromancer's optional corpse/raise sub-kit needs more than healing math, timing, or targeting in
isolation. It needs a shared state model for:

- when KO bodies are legal targets;
- how death-clock timing gates corpse actions;
- whether a corpse is consumed, preserved, or converted;
- who controls the resulting object or undead state;
- whether the result can act, be targeted, be healed, be revived, or expire;
- how immunity policies block corpse or undead manipulation.

T3xT5xT8 V0 is not a full encounter simulator and does not approve acting raised bodies. The V1
Necromancer posture remains non-acting corpse objects by default.

## Pinned Bundle

Pinned input bundle:

```text
work/t3x-t5x-t8-ko-corpse-undead-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t3x-t5x-t8-ko-corpse-undead-v0.json
```

Canonical GPT checker:

```text
tools/check_ko_corpse_undead.py
```

## Formula Contract

T3xT5xT8 V0 processes one target state and a fixed sequence of corpse or undead events.

### Step 1 - Target Eligibility

An event can target the current body/state if:

```text
target not already consumed
target state is in required_states
target is targetable
target is reachable
line_of_effect is true
target immunity tags do not intersect blocked_by_tags
```

This is the T8 side of the composition. It keeps corpse and undead effects from bypassing targeting
or immunity policy.

### Step 2 - Death Clock Timing

If the target has a `death_clock_ticks` value:

```text
resolves_before_death_clock = resolution_delay_ticks < death_clock_ticks
```

Same-tick resolution is unsafe, inherited from T3xT5. If an event does not resolve before the death
clock, it fails with `death_clock_expired`.

Targets with `death_clock_ticks = null` are persistent objects or states for this V0 snapshot and
do not run the death-clock check.

### Step 3 - State Conversion

On success, an event may:

```text
consume target
preserve target
create a new object/state with control_owner, targetability, expiry, and can_act flag
```

If the target is consumed, later events in the same scenario fail with `target_consumed`.

### Step 4 - Acting Body Policy

V1 policy:

```text
allow_acting_bodies = false
```

If an event requests `created_can_act = true` while `allow_acting_bodies = false`, the created object
is still created but its `can_act` field is forced to `false`, and the row records
`acting_body_suppressed`.

A diagnostic `allow_acting_bodies = true` row exists only to prove the switch and output field. It
is not approval for acting raised bodies in final Necromancer data.

For shipped Necromancer/skill data, `allow_acting_bodies` must be hard-set to `false`. The `true`
branch is a test-harness contrast row only, not a designer-facing tuning knob.

## Scenario Set

The first bundle includes rows for:

- KO corpse puppet before death clock, consuming the body;
- same-tick death clock failure;
- after-death-clock failure;
- preserving a corpse as a zone anchor;
- preserving a corpse and then consuming it in a later successful event;
- consumed corpse blocking a second action;
- boss/unique immunity;
- wrong-state target failure;
- wrong-state failure before targeting checks;
- targetable failure before reach/line checks;
- unreachable failure before line checks;
- immunity failure before death-clock checks;
- undead-marked command success;
- natural undead command success;
- successful state action without creating an object;
- untargetable, unreachable, and line-blocked failures;
- requested acting body suppressed by V1 policy;
- no-death-clock persistent corpse object;
- diagnostic acting-body allowed branch.

These rows validate state machinery. They do not set final Necromancer values or approve acting
raised bodies.

## Expected Counter Output

GPT and Claude T3xT5xT8 counters should produce:

- one row per scenario;
- successful and failed action counts;
- final target state and consumed flag;
- created object summaries;
- same-tick, death-clock miss, and acting-body suppression counts;
- event outcome list and failure reason list;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T3xT5xT8 output can be used by concrete corpse, undead, or raise
  proposals.

## What T3xT5xT8 V0 Does Not Decide

Still open:

- exact Necromancer skill list, JP costs, CT, MP, range, hit rates, or durations;
- whether the optional corpse sub-kit survives final design;
- acting raised bodies in shipped data;
- T10 action grants from raised bodies;
- T11 area zones around corpse anchors;
- full revive inversion, healing inversion, or undead HP damage math.

## Claude Review Request

Claude should review whether:

- same-tick unsafe should apply to corpse/death-clock actions;
- consumed corpse should block later corpse actions in the same row;
- V1 acting-body suppression is represented clearly;
- the diagnostic acting-body row should remain as harness coverage or be removed;
- the scenario set exercises all core branches before accepting T3xT5xT8 V0.

Claude review verdict: accepted.

Claude independently reran `work/t3xt5xt8_ko_corpse_undead_check.py` against the expanded 21-row
bundle and `work/gpt-t3x-t5x-t8-ko-corpse-undead-v0.json`. The reviewer result was
`scenario_count=21` and `mismatch_count=0` against both the bundle expected values and GPT's
calculated output.

The accepted row set includes `t3x5x8_multi_success_preserve_then_consume`, which proves
multi-success accumulation, ordered multi-create output, and preserve-before-consume sequencing.

The diagnostic `allow_acting_bodies=true` row remains test-harness coverage only. Shipped
Necromancer/skill data must hard-set `allow_acting_bodies=false`.

T3xT5xT8 gate #5 is cleared.
