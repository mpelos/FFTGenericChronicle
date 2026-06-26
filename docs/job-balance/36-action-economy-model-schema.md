# Turn-Grant And Action-Economy Model Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/11-ct-delay-model-schema.md`
- `work/t10-action-economy-scenarios-v0.json`

## Purpose

This document starts T10, the turn-grant and action-economy validation gate.

T5 already covers timing, CT delay, Haste/Slow speed states, overwatch timing windows, and delayed
resolution. T10 covers the effects T5 explicitly cannot accept:

- immediate action grants such as Time Mage `Quick`;
- `Critical: Quick`;
- action refunds;
- extra attacks from reactions or intervention-style protection;
- acting raised bodies if that design ever returns;
- any effect that can recursively produce more actions.

T10 V0 is not a full battle simulator. It is a deterministic ledger for action-grant attempts inside
one tactical window.

## Pinned Bundle

Pinned input bundle:

```text
work/t10-action-economy-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t10-action-economy-v0.json
```

Canonical GPT checker:

```text
tools/check_action_economy.py
```

## Formula Contract

T10 V0 processes a fixed sequence of action-grant attempts.

### Step 1 - Policy

Each scenario defines:

```text
party_grant_cap
per_target_grant_cap
block_granted_action_grants
```

`block_granted_action_grants` is the V0 anti-recursion rule. If an action produced by a grant tries
to grant another action, the attempt fails before caps or target eligibility can make a loop.

Concrete Quick-class values should default to `block_granted_action_grants = true`. A diagnostic
`false` row exists only to prove the policy switch and cap machinery; it is not approval for
recursive action grants in final job data.

For shipped skill/job data, `block_granted_action_grants` must be hard-set to `true`. The `false`
branch is a test-harness contrast row only, not a designer-facing tuning knob.

### Step 2 - Units

Each unit has:

```text
can_act
can_receive_grant
```

`can_act` covers the acting source's ability to execute the grant attempt. `can_receive_grant`
covers KO, Stop, Disable, untargetable timing, or any other state that should prevent receiving an
immediate action in this abstract model.

### Step 3 - Attempt Cost

Each event carries:

```text
source_action_spent
```

That value is counted when the attempt occurs, whether the attempt succeeds or fails. This lets T10
distinguish:

- normal `Quick`, which usually spends one action to grant one action;
- reactions such as `Critical: Quick`, which may spend zero natural actions;
- intervention or action-refund effects that create net action advantage if not bounded.

### Step 4 - Attempt Resolution

For each grant attempt:

```text
if trigger_context == granted_action and block_granted_action_grants:
  fail recursion_blocked
elif source cannot act:
  fail source_cannot_act
elif target cannot receive grant:
  fail target_cannot_receive
elif target grant count >= per_target_grant_cap:
  fail per_target_cap
elif party grant count >= party_grant_cap:
  fail party_cap
else:
  success
```

Successful grants increment both party grant count and target grant count.

### Step 5 - Net Action Delta

```text
net_action_delta = successful_grants - total_source_actions_spent
```

This is not a final power score. It is a mechanical warning signal:

- `0` can still be strong if timing matters;
- positive values are dangerous and require strict caps, triggers, or costs;
- repeated positive values are a loop risk.

## Scenario Set

The first bundle includes rows for:

- normal Quick-style one-for-one action grant;
- invalid target failure;
- source unable to act;
- exact per-target cap;
- per-target cap parameter greater than `1`;
- party grant cap;
- party cap applying across natural and reaction grants;
- granted-action recursion blocked;
- granted-action recursion blocked before other failure reasons;
- granted-action recursion blocked before source failure;
- diagnostic bounded granted-action chaining when the anti-recursion switch is false;
- Critical: Quick-style zero-action reaction grant;
- reaction chain capped by per-target limit;
- intervention-style extra attack grant;
- self action refund;
- failed Quick still spending the source action;
- party cap `0` blocking all grants.
- source failure taking precedence over target failure;
- source failure taking precedence over party cap;
- per-target cap taking precedence over party cap.
- per-target cap `0` blocking grants.

These rows validate action-grant machinery. They do not set final Quick, Critical: Quick,
Intervention, overwatch, corpse, or refund values.

## Expected Counter Output

GPT and Claude T10 counters should produce:

- one row per scenario;
- `scenario_id`;
- successful and failed grant counts;
- total source actions spent;
- net action delta;
- party grant count;
- per-target grant counts;
- event outcome list;
- failure reason list;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T10 output can be used by concrete Quick-class, reaction grant,
  Intervention, or action-refund proposals.

## What T10 V0 Does Not Decide

Still open:

- exact Quick CT, MP, range, hit rate, and job timing;
- whether `Critical: Quick` survives final design;
- exact `Intervention` implementation;
- overwatch trigger shape or enemy movement rules;
- Haste/Slow speed-state values, which remain T5;
- corpse, undead, or raised-body action design;
- full turn-order simulation.
- recursive action grants in final skill data; the policy-false row is diagnostic only.

## Claude Review Request

Claude should review whether:

- the V0 anti-recursion rule should hard-block granted-action grants;
- failed attempts should still count `source_action_spent`;
- caps should be evaluated after source/target eligibility as written;
- `net_action_delta` is the right warning signal for this gate;
- the scenario set exercises all core branches before accepting T10 V0.

Claude review verdict: accepted.

Claude independently reran `work/t10_action_economy_check.py` against the expanded 21-row bundle and
`work/gpt-t10-action-economy-v0.json`. The reviewer result was `scenario_count=21` and
`mismatch_count=0` against both the bundle expected values and GPT's calculated output.

The accepted row set proves the full resolution order:

```text
recursion > source > target > per_target > party
```

It also keeps `block_granted_action_grants=false` as a test-harness contrast row only. Shipped
skill/job data must hard-set `block_granted_action_grants=true`.

T10 gate #4 is cleared.
