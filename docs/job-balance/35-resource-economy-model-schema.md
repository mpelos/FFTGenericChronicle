# Resource And MP Economy Model Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `work/t9-resource-economy-scenarios-v0.json`

## Purpose

This document starts T9, the resource and MP economy validation gate.

T9 exists because caster and support balance cannot be accepted from per-action damage, healing, or
timing alone. A spell that is fair once can become campaign-warping if the party can cast it every
turn, and a spell that is strong can still be healthy if MP pressure keeps it selective.

The immediate consumers are:

- Chemist `Ether` and other MP recovery items;
- Monk `Chakra` if MP restoration remains in scope;
- White Mage, Black Mage, Time Mage, Mystic, Summoner, Necromancer, Bard/Dancer, and Ramza MP
  pressure;
- Mystic `Halve MP`, `Manafont`, `Mana Shield`, or similar resource pieces;
- Summoner `Summon Focus` or high-cost summon pacing;
- Necromancer drain or syphon-style resource loops.

T9 V0 is tactical resource economy inside an encounter. It is not campaign economy, gil economy,
shop stock, treasure, or item-price balance.

## Pinned Bundle

Pinned input bundle:

```text
work/t9-resource-economy-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t9-resource-economy-v0.json
```

Canonical GPT checker:

```text
tools/check_resource_economy.py
```

## Formula Contract

T9 V0 processes a fixed sequence of resource events for one resource pool, normally MP.

### Step 1 - Resource Bounds

```text
current_resource = min(max(starting_resource, 0), max_resource)
```

Resource never drops below `0` and never exceeds `max_resource`.

### Step 2 - Effective Cast Cost

For a `cast` event:

```text
multiplier_product = product(scenario cost_multipliers) * product(event cost_multipliers)
raw_cost = base_cost * multiplier_product
effective_cost = max(min_cost, ceil(round(raw_cost, 6)))
```

`min_cost` defaults to `0`, but spell rows should usually pin it to `1` so discounts cannot create
free spells unless a future design explicitly wants that.

All multipliers are applied before the single ceiling step. Intermediate rounding is rejected. The
`round(..., 6)` step is required before `ceil` so exact-integer products do not drift from floating
point representation.

### Step 3 - Cast Success And Refunds

```text
cast succeeds if current_resource >= effective_cost
```

On success:

```text
current_resource -= effective_cost
total_cast_cost_paid += effective_cost
successful_casts += 1
```

If the event has `refund_on_success`, that recovery is applied after the cost is paid.

On failure:

```text
current_resource unchanged
failed_casts += 1
refund_on_success does not apply
```

This matters for Manafont-like or syphon-like designs. Refunds should reward successful action
cycles, not allow underfunded spells to bootstrap themselves.

### Step 4 - Recovery

For `recover` events:

```text
recovery_amount = flat_amount + floor(max_resource * percent_of_max)
```

For `drain` events:

```text
recovery_amount = min(drain_amount, target_resource)
```

Recovery is capped at `max_resource`:

```text
overcap_lost += max(0, current_resource + recovery_amount - max_resource)
current_resource = min(max_resource, current_resource + recovery_amount)
```

### Step 5 - Resource Loss

For `resource_damage` events:

```text
resource_lost = min(current_resource, amount)
current_resource -= resource_lost
total_resource_lost += resource_lost
```

This branch covers MP pressure, MP damage, and simple Mana Shield resource drain. It does not decide
HP mitigation or damage prevention; those need a later composition if the final skill requires it.

### Step 6 - Availability Projection

A scenario may define `reference_cast`. T9 reports:

```text
reference_effective_cost
remaining_reference_casts = floor(current_resource / reference_effective_cost)
can_cast_reference = remaining_reference_casts > 0
```

This is a spell-availability projection, not a command to make all spells repeatedly castable.

## Scenario Set

The first bundle includes rows for:

- basic repeated spell budget;
- insufficient-resource cast failure;
- exact-zero resource after a cast;
- `Halve MP`-style ceiling behavior;
- stacked discounts with single final rounding;
- minimum cost preventing free spells;
- event-local cost multipliers;
- scenario and event cost multipliers compounding on the same cast;
- starting resource clamped above max and below zero;
- flat recovery with and without overcap loss;
- combined flat plus percent recovery;
- percent-of-max recovery with floor rounding;
- drain capped by target resource and by drain amount;
- MP/resource damage clamped at zero;
- refund after successful cast;
- refund recovery capped at max resource with overcap loss;
- failed cast receiving no refund;
- a neutral resource loop with recovery-to-spend ratio `1.0`;
- remaining reference-cast availability after discounts.

These rows validate resource machinery. They do not set final MP costs or recovery amounts for any
specific skill.

## Expected Counter Output

GPT and Claude T9 counters should produce:

- one row per scenario;
- `scenario_id`;
- final resource, minimum resource, paid cast cost, recovered resource, resource lost, and overcap;
- successful and failed cast counts;
- event outcome list;
- recovery-to-spend ratio;
- optional reference-cast availability fields;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T9 output can be used by concrete MP-cost, MP-recovery, discount, drain,
  or resource-loop proposals.

## What T9 V0 Does Not Decide

Still open:

- exact MP costs, Ether amounts, Chakra MP restoration, Halve MP, Manafont, Syphon, or Summon Focus
  values;
- item prices, shop stock, gil pressure, or campaign inventory economy;
- spell CT, hit rate, area, target count, Faith, Shell, or Reflect routing;
- whether a resource loop is healthy; V0 measures loops so later gates can reject or price them;
- HP prevention from Mana Shield or similar effects.

## Claude Review Request

Claude should review whether:

- cost multipliers should use one final `ceil` after all multipliers;
- failed casts should leave resource unchanged and skip refunds;
- recovery should use cap/overcap accounting as written;
- drain should be capped by target resource in V0;
- the scenario set exercises all core branches before accepting T9 V0.

Claude review verdict: accepted.

Claude independently reran `work/t9_resource_economy_check.py` against the expanded 24-row bundle
and `work/gpt-t9-resource-economy-v0.json`. The reviewer result was `scenario_count=24` and
`mismatch_count=0` against both the bundle expected values and GPT's calculated output.

The accepted row set includes the required post-review branches:

- `t9_scenario_and_event_multipliers_compound` proves scenario-level and event-level cost
  multipliers compound instead of replacing each other.
- `t9_drain_capped_by_drain_amount` proves the drain-amount side of the drain cap.

It also includes rows for `min_cost=0` free casts and refund overcap accounting.

T9 gate #3 is cleared.
