# Offense Armor Composition Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/13-armor-response-model-schema.md`
- `docs/job-balance/14-enemy-offense-disarm-model-schema.md`
- `work/t6-armor-response-scenarios-v0.json`
- `work/t7-enemy-offense-scenarios-v0.json`
- `work/t6x-t7-offense-armor-scenarios-v0.json`

## Purpose

This document starts the T6xT7 composition: enemy-offense family replacement feeding armor
response.

T6 validated dynamic armor response. T7 validated enemy-offense reduction, jam, break, and family
replacement. T6xT7 asks the next required question:

```text
What happens when the T7 resulting family and damage type hit a T6 armor profile?
```

The immediate consumers are:

- Knight `Rend Weapon`;
- Knight `Rend Armor`, `Shield Break`, and `Crushing Blow` when combined with weapon disruption;
- Thief steal/disarm concepts;
- Safeguard and anti-break counterplay;
- future enemy designs that use weapon break, jam, or replacement.

## Scope

T6xT7.0 is per-application and per-hit. It does not model accuracy, evasion, AI targeting, CT,
duration, or party follow-up.

It consumes T7 output as the pre-armor damage value:

```text
pre_armor_output_per_hit = t7_output_per_hit
```

Then it applies the exact T6 armor-response pipeline:

```text
base_response = armor_response[armor_class][damage_type]
effective_penetration = clamp(family_penetration + penetration_bonus, 0.0, 1.0)
penetrated_response = base + effective_penetration * (penetration_ceiling - base)
  if base < penetration_ceiling
penetrated_response = base otherwise
final_response = clamp(penetrated_response + sum(response_delta), 0.25, 2.50)
damage_per_hit = floor(round(pre_armor_output_per_hit * final_response, 6))
total_damage = damage_per_hit * hit_count
```

The `penetration_bonus` and `response_delta` sums are field-based, not kind-based. Any dynamic
effect carrying `penetration_bonus` contributes to penetration, and any dynamic effect carrying
`response_delta` contributes to the additive response shift.

If T7 produces no resulting family, T6xT7 returns zero damage and `none` response fields. There is
no armor matchup to evaluate when no offensive family remains.

Ratio fields return `0.0` if their denominator is `0`, because the comparison is undefined in that
degenerate case.

## Why This Composition Matters

Weapon break is not only "less raw output." It can also change the damage-type matchup.

The key example is sword to fists against plate:

- sword keeps high raw output but uses `swing`, which plate resists;
- fists lose raw output but use `crush`, which plate is weak to.

T6xT7 must measure both effects together. Otherwise a future Knight or Thief design might assume
that disarm always reduces output by a simple percentage, missing the armor-response flip.

## Pinned Bundle

Pinned input bundle:

```text
work/t6x-t7-offense-armor-scenarios-v0.json
```

The bundle defines original-vs-resulting comparison rows and expected values for the first
dual-independent T6xT7 run.

## Scenario Set

The first bundle includes rows for:

- sword breaking to fists against plate;
- sword breaking to fists against cloth;
- spear breaking to fists against mail;
- sword breaking to fists against leather;
- no-fallback weapon break producing zero offense;
- temporary output-down without family/type change;
- break to fists plus T6 guard-break response delta;
- temporary output-down plus T6 penetration bonus;
- penetration bonus clamping at `1.0`;
- multiple dynamic response deltas summing together;
- final response clamping at `2.50`;
- multi-hit output after T7 reduction;
- float-safe floor through armor response.

These rows validate composition machinery. They do not set final values for Knight, Thief,
Safeguard, or any weapon-break skill.

## Expected Counter Output

GPT and Claude T6xT7 counters should produce:

- one row per scenario;
- `scenario_id`;
- original family/type response and damage;
- resulting family/type response and damage;
- raw output ratio;
- response ratio;
- damage ratio;
- validation errors, if any.

The dual-independent gate from document 07 applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T6xT7 output can be used to accept or reject weapon-break values.

## What T6xT7.0 Does Not Decide

Still open for later composition versions:

- exact Knight `Rend Weapon` values;
- exact Thief steal/disarm values;
- whether enemies can re-equip;
- duration and refresh timing;
- shield/evasion/accuracy effects;
- AI target selection after weapon break;
- party follow-up value after a target's armor matchup changes.

## Claude Review Request

Claude should review whether:

- consuming T7 `output_per_hit` before T6 armor response is the right composition boundary;
- no-family output should remain zero with no armor-response fields;
- the sword-to-fists plate/cloth rows demonstrate the armor-matchup flip clearly enough;
- the T6 response pipeline is copied without drift;
- any additional row is required before accepting T6xT7.0.

Claude review verdict: Accepted after adding penetration clamp, multiple response-delta, mail,
leather, and final-response clamp rows requested during review (claude-opus-4-8, 2026-06-21).
