# Spell Routing And Reflect Composition Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/15-targeting-challenge-model-schema.md`
- `docs/job-balance/20-time-mystic-v1-proposal.md`
- `docs/job-balance/21-summoner-geomancer-v1-proposal.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/32-campaign-artifacts-provisional-v0.md`
- `work/t8xsr-spell-routing-reflect-scenarios-v0.json`

## Purpose

This document starts T8xSR, the spell routing and Reflect composition gate.

T8 validated targeting-policy machinery. T8xSR validates the next routing question:

```text
When a spell hits Reflect or a routing effect, who becomes the final target?
```

The immediate consumers are:

- Time Mage `Reflect`;
- Summoner `Carbuncle`;
- future spell redirection, decoy, bounce, or routing effects.

T8xSR V0 is per spell-routing event. It does not decide spell damage, healing, status rate, CT, MP,
area targeting, or Reflect duration.

## Pinned Bundle

Pinned input bundle:

```text
work/t8xsr-spell-routing-reflect-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t8xsr-spell-routing-reflect-v0.json
```

Canonical GPT checker:

```text
tools/check_spell_routing_reflect.py
```

## Formula Contract

### Step 1 - Reflect Trigger

A spell reflects only if:

```text
spell.reflectable = true
and original_target.has_reflect = true
```

If either condition is false, the spell keeps the original target.

### Step 2 - Reflection Candidate Eligibility

When reflection triggers, candidates are evaluated in scenario order.

A candidate is eligible if:

```text
candidate unit exists
candidate.can_target = true
candidate.line_of_effect = true
candidate.spell_immune = false
candidate.ai_ignored = false
```

This inherits the T8 principle that routing cannot ignore targetability.

### Step 3 - Candidate Selection

Eligible candidates are scored by `routing_score`.

```text
select highest routing_score
tie_breaker = earliest eligible candidate in scenario order
```

If no candidate is eligible:

```text
fizzled = true
final_target_id = none
```

### Step 4 - Backfire Flags

T8xSR reports routing risk explicitly:

```text
hostile_backfire = spell.intent = hostile and final target team = caster team
beneficial_backfire = spell.intent = beneficial and final target team != caster team
```

These flags are warning signals, not final balance values.

### Step 5 - One Reflection Only

T8xSR V0 suppresses reflection loops:

```text
if selected reflected target also has_reflect:
  secondary_reflect_suppressed = true
```

The spell still lands on that selected target. It does not bounce again in V0.

## Scenario Set

The first bundle includes rows for:

- hostile spell bouncing back to caster team;
- beneficial spell backfiring to an enemy;
- non-reflectable spell ignoring Reflect;
- reflectable spell on a non-Reflect target;
- no legal reflected target fizzles;
- spell-immune candidate skipped;
- line-blocked candidate skipped;
- routing score selecting the highest-value legal target;
- deterministic tie-break;
- secondary Reflect loop suppression;
- untargetable and AI-ignored candidate filtering.
- reflected hostile spell that does not backfire because it routes to a non-caster-team target;
- reflected beneficial spell that does not backfire because it routes to caster-team ally;
- nonexistent candidate filtering.

These rows validate routing machinery. They do not set final Reflect, Carbuncle, or spell values.

## Expected Counter Output

GPT and Claude T8xSR counters should produce:

- one row per scenario;
- `scenario_id`;
- reflect trigger fields;
- final target and final team;
- fizzle and backfire flags;
- eligible candidate count;
- selected routing score;
- secondary-reflect suppression flag;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T8xSR output can be used by concrete Reflect, Carbuncle, or spell-routing
  proposals.

## What T8xSR V0 Does Not Decide

Still open:

- exact Reflect duration, CT, MP, JP, target area, or hit rate;
- whether area spells reflect per target, per spell, or not at all;
- damage, healing, Faith, Shell, element, or status calculations after routing;
- full AI movement or spell selection;
- multiple-bounce Reflect loops.

## Claude Review Request

Claude should review whether:

- one-reflection-only is the right V0 anti-loop policy;
- the candidate eligibility set is sufficient;
- backfire flags capture the player-legible routing risk;
- no-legal-target should fizzle in V0;
- the scenario set exercises all core branches before accepting T8xSR V0.

Claude review verdict: Accepted on 2026-06-21.

Claude reran the independent checker against the expanded 15-row bundle and GPT output:

- `scenario_count=15`
- `fields_compared=270`
- `mismatch_count=0`

The accepted bundle covers trigger behavior, candidate existence, targetability filters,
highest-score selection, deterministic tie-breaks, no-candidate fizzle, secondary Reflect
suppression, hostile backfire by final team, and beneficial backfire by final team.
