# Mitigation Stack Composition Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/13-armor-response-model-schema.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/32-campaign-artifacts-provisional-v0.md`
- `docs/formula-balance/10-mitigation-and-scaling-policy-v0.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/t6xps-mitigation-stack-scenarios-v0.json`

## Purpose

This document starts T6xPS, the mitigation-stack validation gate.

T6 validated armor response and guard-break mechanics. T6xPS validates the ordinary percent-layer
stack that sits on top of armor response:

```text
type_response with penetration
* Protect/Shell-style mitigation
* element response
* Zodiac response
```

The immediate consumers are:

- White Mage `Protect`, `Shell`, and `Wall`;
- Summoner `Golem` and `Carbuncle` if represented as percent protection or routing;
- Samurai `Kiyomori`-style protection draws;
- Mystic `Mana Shield` or magic-defense posture if percent-based;
- Vanguard `Aegis Stance`, `Intercede`, `Intervention`, and `Armor Discipline`;
- campaign stress row `GCV-SYN-C-MITIGATION-STACK` from
  `docs/job-balance/32-campaign-artifacts-provisional-v0.md`.

## Pinned Bundle

Pinned input bundle:

```text
work/t6xps-mitigation-stack-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t6xps-mitigation-stack-v0.json
```

Canonical GPT checker:

```text
tools/check_mitigation_stack.py
```

## Formula Contract

T6xPS consumes the T6 armor-response pipeline through the penetrated response stage.

### Step 1 - Base Armor Response

```text
base_response = armor_response[armor_class][damage_type]
```

### Step 2 - Penetration

```text
effective_penetration = clamp(family_penetration + penetration_bonus, 0.0, 1.0)
penetrated_response = base + effective_penetration * (penetration_ceiling - base)
  if base < penetration_ceiling
penetrated_response = base otherwise
```

This is copied from T6. Penetration improves resisted responses toward the ceiling. It does not
lower responses already above the ceiling.

### Step 3 - Mitigation Layers

Each active Protect/Shell/Wall-like ordinary percent layer contributes a multiplier.

```text
mitigation_multiplier = product(active mitigation layer multipliers)
```

Protect and Shell are separate layers. A Wall-like state is represented by both layers being active,
not by a new fused multiplier.

The v0 bundle uses:

```text
protect = 0.667
shell = 0.667
```

These are validation constants copied from the current formula policy. They do not finalize skill
duration, JP cost, target area, or availability.

### Step 4 - Element And Zodiac Layers

```text
element_multiplier = element response multiplier
zodiac_multiplier = zodiac response multiplier
```

Hard nullify, absorb, immunity, or reflection are not ordinary percent layers. They must be reported
as separate tactical cases in later gates.

### Step 5 - Single Final Clamp

All ordinary percent layers multiply before clamping:

```text
combined_response =
  penetrated_response
  * mitigation_multiplier
  * element_multiplier
  * zodiac_multiplier

bounded_response = clamp(combined_response, 0.25, 2.50)
```

The clamp happens once, at the end. Intermediate clamping is rejected.

The bundle includes an order-sensitivity row that would produce a different value if a checker
clamped midway through the chain.

### Step 6 - Damage Projection And Chip Floor

```text
damage_per_hit = floor(round(base_pressure_per_hit * bounded_response, 6))
visible_damage_per_hit =
  max(chip_floor, damage_per_hit) if base_pressure_per_hit > 0
  0 otherwise
total_damage = damage_per_hit * hit_count
visible_total_damage = visible_damage_per_hit * hit_count
```

The `round(..., 6)` step is required for deterministic cross-implementation behavior.

The chip floor is a display/playability projection. It prevents positive-pressure rows from
reporting visible `0` damage after the low clamp. It does not apply to zero base pressure.

## Scenario Set

The first bundle includes rows for:

- neutral baseline armor response;
- Protect only;
- Shell only;
- Wall-style both-applied mitigation;
- low-clamp floor pressure from armor, Protect, element resistance, and bad Zodiac;
- single-final-clamp order sensitivity;
- high-clamp ceiling pressure from armor weakness, elemental weakness, and good Zodiac;
- penetration before mitigation;
- penetration bonus addition below the clamp;
- penetration bonus addition with upper penetration clamp at `1.0`;
- multi-hit chip floor;
- Shell plus element resistance on cloth;
- penetration not lowering plate's crush vulnerability;
- zero base pressure not receiving chip damage.

These rows validate mitigation-stack machinery. They do not set final values for any job skill.

## Expected Counter Output

GPT and Claude T6xPS counters should produce:

- one row per scenario;
- `scenario_id`;
- calculated response fields;
- damage projection fields;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T6xPS output can be used to accept or reject mitigation values.

## What T6xPS Does Not Decide

Still open:

- exact Protect, Shell, Wall, Golem, Carbuncle, Aegis, Intercede, or Kiyomori values;
- duration, CT, refresh, dispel, and target area;
- spell routing and Reflect behavior;
- AI targeting changes from protection effects;
- action-economy effects from Intervention or Quick-like reactions;
- whether a future effect is modeled as ordinary percent mitigation, targetability, reflection,
  damage redirection, or a separate special case.

## Claude Review Request

Claude should review whether:

- Protect and Shell should be represented as separate ordinary percent layers for this gate;
- the single-final-clamp contract is sufficiently enforced by the row set;
- the low-floor, high-ceiling, both-applied, penetration-before-mitigation, and chip-floor rows are
  sufficient for T6xPS V0;
- any additional row is required before running the dual-independent checker.

Claude review verdict: accepted.

Claude independently reran `work/t6xps_mitigation_stack_check.py` against the updated 14-row bundle
and `work/gpt-t6xps-mitigation-stack-v0.json`. The reviewer result was `scenario_count=14` and
`mismatch_count=0` against both the bundle expected values and GPT's calculated output.

The two post-review rows close the original dead-branch gap:

- `t6xps_penetration_bonus_subcap_longbow_plate` proves `family_penetration + penetration_bonus`
  below the upper clamp.
- `t6xps_penetration_bonus_clamp_gun_plate_protect` proves the upper clamp at `1.0`.

T6xPS gate #1 is cleared.
