# Timed Untargetability Composition Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/11-ct-delay-model-schema.md`
- `docs/job-balance/15-targeting-challenge-model-schema.md`
- `docs/job-balance/25-ninja-v1-proposal.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `work/t5x-t8-timed-untargetability-scenarios-v0.json`

## Purpose

This document starts T5xT8, the timed untargetability composition gate.

T5 validates timing windows. T8 validates target eligibility and challenge-style target selection.
T5xT8 validates what happens when a unit becomes temporarily untargetable during those windows.

The immediate consumers are:

- Dragoon `Jump` and any airborne timing;
- `Vanish`, `Invisible`, or future temporary untargetability effects;
- delayed attacks or spells that resolve after the target's state changes;
- Knight `Challenge` or future target-routing tools when the intended target cannot legally be
  selected.

T5xT8 V0 is per targeting or resolution event. It does not decide final duration, JP, CT, MP, hit
rate, status accuracy, damage, AI movement, or exact implementation hooks.

## Vanilla Reference Consultation

The expanded vanilla atlas should be checked before any job uses this gate:

- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

Relevant vocabulary includes:

- `Jump`, which creates an airborne untargetable timing state;
- `Invisible`, which changes targetability and AI attention until broken;
- `Vanish`, reaction ID 425, which is tagged as `reaction`, `status_add`, and `defense`;
- delayed CT actions such as `Aim`, Limit skills, spells, and summons;
- T8 targetability terms such as `can_target`, `line_of_effect`, and `ai_ignored`.

The goal is not to copy vanilla status duration. The goal is to keep targetability state explicit so
future job skills cannot accidentally create permanent safety, unavoidable whiffs, or broken
challenge locks.

## Pinned Bundle

Pinned input bundle:

```text
work/t5x-t8-timed-untargetability-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t5x-t8-timed-untargetability-v0.json
```

Canonical GPT checker:

```text
tools/check_timed_untargetability.py
```

## Formula Contract

### Step 1 - Active Untargetability Window

An untargetable effect is active at an evaluation tick if:

```text
effect.start_tick <= evaluation_tick < effect.expire_tick
```

Same-tick expiry is therefore expired before targeting or resolution at that tick.

```text
evaluation_tick = expire_tick -> not active
```

This mirrors the T5 habit of making timing boundaries explicit instead of relying on hidden phase
order.

### Step 2 - Break Events

An effect can opt into break events:

```text
break_on_damage = true
break_on_action = true
```

If a matching event has:

```text
event.tick <= evaluation_tick
```

then the effect is already broken for that evaluation.

Jump-like airborne effects normally do not use break-on-damage or break-on-action. Vanish or
Invisible-like effects may use one or both, depending on final job design.

### Step 3 - Candidate Eligibility

A targeting candidate is eligible if:

```text
target unit exists
target.can_target = true
target.ai_ignored = false
candidate.can_reach = true
candidate.line_of_effect = true
target has no active untargetable effect at decision_tick
```

Untargetability is an eligibility filter, not a score penalty.

### Step 4 - Selection

For ordinary targeting:

```text
select eligible candidate with highest tactical_score
tie_breaker = earliest eligible candidate in scenario order
```

If no target is eligible:

```text
selected_candidate_id = none
selected_target_id = none
no_eligible_targets = true
```

### Step 5 - Challenge Composition

Soft challenge applies its bonus only to eligible candidates:

```text
if mode = soft and candidate is eligible and target_id = challenger_target_id:
  add soft_bonus
```

Hard challenge can force only an eligible challenger target. If the challenger target is
untargetable, hard challenge falls back to ordinary scoring.

### Step 6 - Delayed Resolution

A delayed action resolves on its locked target only if that target is targetable at
`resolution_tick`:

```text
resolves_on_target = target exists
  and target.can_target = true
  and target.ai_ignored = false
  and line_of_effect_at_resolution = true
  and can_reach_at_resolution = true
  and target has no active untargetable effect at resolution_tick
```

If the target is untargetable at resolution, the action fizzles or whiffs for this V0 model:

```text
fizzle_reason = target_untargetable
```

## Scenario Set

The first bundle includes rows for:

- ordinary visible targeting baseline;
- active Vanish filtering a high-score target;
- Vanish expired before the decision tick;
- Vanish expiring on the decision tick;
- damage breaking Vanish before the decision tick;
- action breaking Invisible before the decision tick;
- Jump airborne filtering a target;
- Jump landing on the decision tick;
- all candidates untargetable producing no legal selection;
- delayed action whiffing against active Vanish;
- delayed action resolving after Vanish expires;
- soft challenge not applying bonus to an untargetable challenger;
- hard challenge falling back when the challenger is untargetable;
- untargetability that starts after the decision tick not affecting the current decision.
- soft challenge applying its bonus to an eligible challenger and flipping target selection;
- hard challenge forcing an eligible challenger despite lower tactical score;
- deterministic tie-break between equal eligible targeting candidates;
- soft challenge applying its bonus but still losing to a stronger tactical target.

These rows validate timing and targetability machinery. They do not set final Dragoon, Ninja,
Knight, Time Mage, status, or reaction values.

## Expected Counter Output

GPT and Claude T5xT8 counters should produce:

- one row per scenario;
- `scenario_id`;
- model-specific selected target or resolution fields;
- active/filtered untargetability fields;
- challenge application fields where relevant;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T5xT8 output can be used by concrete Jump, Vanish, Invisible,
  challenge, or delayed-action proposals.

## What T5xT8 V0 Does Not Decide

Still open:

- exact duration for Jump, Vanish, Invisible, or any future untargetable effect;
- exact CT, MP, JP, hit rate, or reaction trigger rate;
- whether every delayed action should whiff, retarget, or convert when the target becomes invalid;
- whether area spells check untargetability per target or per area center;
- full AI pathing, movement, or target acquisition;
- implementation proof for each data-mod targetability hook.

## Claude Review Request

Claude should review whether:

- same-tick expiry should count as expired for V0;
- untargetability should be an eligibility filter rather than a score penalty;
- break-on-damage and break-on-action events are sufficient for V0;
- delayed action whiff is the right conservative V0 behavior;
- the scenario set covers the core branches before accepting T5xT8 V0.

Claude review verdict: Accepted on 2026-06-21.

Claude reran the independent checker against the expanded 18-row bundle and GPT output:

- `scenario_count=18`
- `fields_compared=280`
- `mismatch_count=0`

The accepted bundle covers timed targetability filtering, same-tick expiry, damage/action breaks,
Jump airborne and landing timing, no-target fallback, delayed-action whiff versus post-expiry
resolution, soft challenge applied and blocked branches, hard challenge forced and fallback
branches, and deterministic tie-breaks.
