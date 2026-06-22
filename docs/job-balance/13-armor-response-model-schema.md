# Armor Response And Guard-Break Model Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/t6-armor-response-scenarios-v0.json`

## Purpose

This document starts T6, the dynamic armor-response and guard-break validation track.

T6 exists to validate skills that can temporarily alter:

- armor response;
- guard break;
- defense-down or armor exposure;
- damage-type vulnerability;
- temporary penetration bonuses;
- damage-response clamps.

The immediate consumers are:

- Knight `Rend Armor`, `Shield Break`, and `Crushing Blow`;
- Archer `Piercing Shot`;
- future Monk anti-plate or guard-breaking techniques;
- later Geomancer, Mystic, Samurai, Dragoon, or Vanguard response effects if they alter
  physical mitigation.

## Source Notes

T6.0 extends the accepted Generic Chronicle formula v0.2.1 response model. It does not reinvent the
armor table.

Static baseline:

```text
work/sim-inputs-v0.2.1.json
armor_response[armor_class][damage_type]
```

The static v0.2.1 table is:

| Armor class | Swing | Thrust | Crush | Missile |
| --- | ---: | ---: | ---: | ---: |
| `plate` | 0.65 | 0.65 | 1.15 | 0.80 |
| `mail` | 0.75 | 1.10 | 0.95 | 1.10 |
| `leather` | 0.95 | 0.95 | 1.00 | 0.95 |
| `cloth` | 1.00 | 1.00 | 1.00 | 1.00 |

Existing v0.2.1 penetration is family/delivery-fixed:

```text
eff_resp = base + penetration * (penetration_ceiling - base) if base < penetration_ceiling
eff_resp = base otherwise
penetration_ceiling = 1.10
```

The existing combined multiplier clamp is:

```text
combined_multiplier_clamp = [0.25, 2.50]
```

The embedded `armor_response`, `family_penetration`, `penetration_ceiling`, and clamp values in the
T6 bundle are copies of v0.2.1. If the formula policy changes, these copies must be regenerated and
re-verified through Gate F5 rather than silently drifting.

## Vanilla Reference Consultation

T6 should consult:

- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

The vanilla reference is a creative palette, not a compatibility cage. T6 may reuse, repurpose, or
recombine existing FFT effect ideas if doing so creates a better Generic Chronicle combat model.
Concrete numbers still need validation, but design inspiration is allowed to be ambitious.

Relevant effect families include:

- `equipment_break`: `Rend Helm` ID 138, `Rend Armor` ID 139, `Rend Shield` ID 140,
  `Rend Weapon` ID 141, `Crush Armor` ID 160, `Crush Helm` ID 161, `Crush Weapon` ID 162,
  `Crush Accessory` ID 163, `Safeguard` ID 475;
- `defense`: `Protect` ID 9, `Protectja` ID 10, `Shell` ID 11, `Shellja` ID 12, `Wall` ID 13,
  `Golem` ID 65, `Aegis` ID 212, `Defense Boost` ID 466, `Magick Defense Boost` ID 468,
  `Parry` ID 447, `Shirahadori` ID 451, `Evasive Stance` ID 479;
- `damage_boost`: `Attack Boost` ID 465, `Magick Boost` ID 467, `Brawler` ID 472,
  `Doublehand` ID 476, `Dual Wield` ID 477;
- status mitigation: `Protect`, `Shell`, `Defending`;
- formula-sensitive support pieces that could make setup skills mandatory.

The reference is a consultation artifact. Its effect tags are not byte-accurate formula data.

T6 uses these references as vocabulary:

| T6 concept | Vanilla inspiration | Reference tags | Design interpretation |
| --- | --- | --- | --- |
| Armor exposure | `Rend Armor` ID 139, `Crush Armor` ID 160 | `equipment_break`, `damage` | Temporary response delta instead of permanent item deletion. |
| Guard break | `Rend Shield` ID 140, `Shield Break`-style Knight fantasy, `Parry` ID 447 | `equipment_break`, `defense`, `reaction` | Reduce defensive response/evasion layers without making all defenses irrelevant. |
| Penetrating shot | `Piercing Shot` proposed for Archer plus bow/crossbow missile identity | `damage`, `accuracy`, `equipment_break` adjacent | Temporary penetration bonus through the existing penetration channel. |
| Defensive posture | `Protect` ID 9, `Defending` status, `Defense Boost` ID 466 | `defense`, `status_add`, `support` | Negative response delta or separate T3/T4/T5 mitigation, depending on final skill. |
| Mandatory-piece risk | `Attack Boost` ID 465, `Doublehand` ID 476, `Dual Wield` ID 477, `Safeguard` ID 475 | `damage_boost`, `equipment_break`, `support` | T2 must catch any armor-breaker or defense support that becomes too universal. |

## Pinned Bundle

Pinned input bundle:

```text
work/t6-armor-response-scenarios-v0.json
```

The bundle defines response formulas, scenario rows, and expected values for the first
dual-independent T6 run.

## Formula Contract

T6.0 is per-application and per-hit. It does not multiply by duration, action frequency, CT timing,
or party follow-up. Duration is context only for later T6xT5 composition.

### Step 1 - Static Base

```text
base_response = armor_response[armor_class][damage_type]
```

### Step 2 - Penetration Composition

Existing family penetration and temporary penetration bonuses use the same channel:

```text
effective_penetration = clamp(family_penetration + penetration_bonus, 0.0, 1.0)
```

Then the v0.2.1 softening formula applies exactly once:

```text
penetrated_response = base_response + effective_penetration * (penetration_ceiling - base_response)
  if base_response < penetration_ceiling
penetrated_response = base_response
  otherwise
```

This avoids double-counting penetration. A skill may increase `penetration_bonus`, or it may apply a
response delta, but it should not describe the same benefit as both.

### Step 3 - Dynamic Response Deltas

Temporary guard-break, armor exposure, defense-down, and type-vulnerability effects are additive
perturbations to the penetrated response:

```text
dynamic_delta = sum(response_delta values)
unclamped_response = penetrated_response + dynamic_delta
```

This is intentionally additive rather than multiplicative. The v0.2 armor table is already a
multiplier table; T6 perturbations are response-point shifts on that table.

### Step 4 - Clamp

The final response multiplier respects the existing v0.2.1 combined multiplier clamp:

```text
final_response = clamp(unclamped_response, 0.25, 2.50)
```

### Step 5 - Per-Hit Damage Projection

For T6.0 scenario rows that include damage projection:

```text
damage_per_hit = floor(round(base_damage_per_hit * final_response, 6))
total_damage = damage_per_hit * hit_count
```

The `round(..., 6)` step is required for deterministic cross-implementation behavior. Products such
as `100 * 1.15` can land at `114.99999999999999` in IEEE double arithmetic, and a naive floor would
incorrectly produce `114` instead of the mathematically intended `115`.

This projection is a narrow validation aid. It does not replace the full weapon-family damage
harness.

## Scenario Set

The first bundle includes rows for:

- unchanged static armor response;
- existing family penetration;
- additive guard break;
- armor exposure with existing penetration;
- temporary penetration bonus stacking through the penetration channel;
- penetration not lowering responses already above the ceiling;
- type vulnerability;
- high clamp at `2.50`;
- low clamp at `0.25`;
- per-hit multi-hit projection;
- penetration cap at `1.0`;
- multiple response deltas summing additively.

These rows validate response machinery. They do not set final values for Knight, Archer, Monk, or
any other job skill.

## Expected Counter Output

GPT and Claude T6 counters should produce:

- one row per scenario;
- `scenario_id`;
- `model`;
- calculated response fields;
- damage projection fields when present;
- validation errors, if any.

The dual-independent gate from document 07 applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T6 output can be used to accept or reject skill values.

## What T6.0 Does Not Decide

Still open for later T6 versions:

- exact Knight `Rend Armor`, `Shield Break`, or `Crushing Blow` values;
- exact Archer `Piercing Shot` values;
- exact Monk anti-plate values;
- duration, tick timing, dispel timing, and refresh behavior;
- whether `Protect`, `Shell`, `Defense Boost`, or `Defending` are represented as T6 response
  deltas or handled only through T3/T4/T5 tracks;
- AI targeting and party follow-up behavior after an exposed target appears;
- mandatory-piece incidence for armor-break setup skills.

## Design Guardrails

- Setup skills must not become mandatory for every physical party. T2 must flag any accepted build
  population where one armor-breaker appears too often.
- Guard break should create tactical openings, not permanent equipment deletion by default.
- T6 modifiers should be narrow enough that plate still creates demand for crush, mail still
  creates demand for thrust/missile, and cloth does not become secretly tanky through generic
  response stacking.
- Response deltas must be visible enough for the player to understand why a setup mattered.

## Claude Review Request

Claude should review whether:

- the additive response-delta composition is the right T6.0 choice;
- temporary penetration bonus stacking avoids double-counting existing family penetration;
- clamp rows are sufficient;
- per-application/per-hit normalization is clear enough;
- the reference-doc consultation is properly wired;
- any additional row is required before implementing or accepting counters.

Claude review verdict: Accepted (claude-opus-4-8, 2026-06-21).

Acceptance notes:

- Claude verified the pinned bundle has no duplicate object keys.
- Claude's independent counter produced 12 rows and 0 mismatches after the float-safe floor pin.
- T6.0 validates response machinery only. Concrete `Rend Armor`, `Crushing Blow`, `Piercing Shot`,
  or similar skill values still require a later T6.1 data pass.
