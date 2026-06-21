# Targeting And Challenge Model Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `work/t8-targeting-challenge-scenarios-v0.json`

## Purpose

This document starts T8, the targeting and challenge validation track.

T8 exists to validate whether mark, challenge, taunt, or forced-target behavior is healthy for
Generic Chronicle combat. It assumes the data mod can represent the selected behavior. The question
for this slice is not technical proof; it is whether the targeting policy creates good FFT combat
instead of hard-locking encounters, erasing movement, or becoming a mandatory defender tax.

The immediate consumers are:

- Knight `Challenge`;
- future defender, controller, Orator, Time Mage, Necromancer, Special Knight, or Ramza effects
  that alter enemy target selection;
- status interactions that affect AI attention or control.

## Source Notes

T8.0 is intentionally a focused targeting-policy model, not a full AI simulator.

It models a single enemy decision with a list of eligible action candidates. Each candidate already
encodes tactical context such as reach, line of effect, lethal opportunity, self-preservation, and
objective value. T8 then tests how challenge pressure changes target selection.

This lets us validate the policy shape before concrete job values exist.

## Vanilla Reference Consultation

T8 should consult:

- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

Relevant vanilla/status vocabulary includes:

- `Invisible`, which changes AI targeting and should make a target ignored until broken;
- `Berserk`, `Charm`, `Confuse`, `Traitor`, and `Vampire`, which can change control, allegiance, or
  target behavior;
- `Sleep`, `Disable`, `Stop`, and `Toad`, which deny or constrain action selection;
- Orator-style control vocabulary such as `Entice` ID 116 and `Tame` ID 470;
- Knight `Challenge` from the Generic Chronicle proposal, which has no vanilla equivalent but fits
  the durable armed-control fantasy.

The vanilla reference is a creative palette, not a compatibility cage. T8 may decide that a new
Generic Chronicle targeting policy is healthier than any direct vanilla status behavior.

## Pinned Bundle

Pinned input bundle:

```text
work/t8-targeting-challenge-scenarios-v0.json
```

The bundle defines targeting policy rows and expected values for the first dual-independent T8 run.

## Formula Contract

T8.0 is per-decision. It does not multiply by duration, CT frequency, hit chance, damage, or full
encounter simulation.

### Step 1 - Candidate Eligibility

A candidate is eligible if:

```text
can_target and can_reach and line_of_effect and not ai_ignored
```

This keeps challenge from targeting invisible, unreachable, untargetable, or line-blocked units.

### Step 2 - Control Override

If the acting unit is under a control state that owns its target behavior, challenge is ignored for
this decision:

```text
control_state != normal -> select control_policy_candidate_id
```

T8.0 uses this for statuses such as `Confuse`, `Charm`, `Berserk`, `Traitor`, or `Vampire`.
Control override is not gated by normal AI eligibility; the control policy owns the target
selection.

### Step 3 - Hard Challenge

Hard challenge is a forced-target policy:

```text
if mode = hard and actor is not forced_target_immune and challenger has an eligible candidate:
  select the best eligible candidate targeting the challenger
```

Hard challenge is allowed as a modeled branch because it may be useful for narrow non-boss cases,
but it is high-risk. Boss-like or forced-target-immune actors ignore hard challenge and fall back to
normal scoring.

### Step 4 - Soft Challenge

Soft challenge is the default healthy direction for Knight `Challenge` unless later evidence proves
hard challenge is better:

```text
candidate_score = tactical_score
  + lethal_bonus
  + self_preservation_bonus
  + objective_bonus
  + challenge_bonus if mode = soft and candidate targets challenger
```

The bonus is additive. It can redirect close decisions, but it must be beatable by lethal
opportunities, self-preservation, mission objectives, invalid targeting, or status/control
overrides.

### Step 5 - Deterministic Selection

If no override applies:

```text
select eligible candidate with highest candidate_score
tie_breaker = earliest candidate in scenario order
```

T8.0 deliberately keeps the tie-breaker simple so GPT and Claude can compare exact rows before any
future full AI model exists.

## Scenario Set

The first bundle includes rows for:

- normal scoring with no challenge;
- deterministic tie-break when two eligible candidates have equal score;
- soft challenge redirecting a close target choice;
- soft challenge applying to multiple eligible challenger-targeting candidates;
- soft challenge losing to a lethal opportunity;
- soft challenge losing to self-preservation;
- soft challenge failing when the challenger is unreachable;
- hard challenge selecting the challenger when valid;
- hard challenge selecting the best of multiple challenger-targeting candidates;
- hard challenge ignored by forced-target immunity;
- hard challenge falling back when the challenger is ineligible;
- control override ignoring challenge;
- invisible/AI-ignored challenger being ineligible.

These rows validate targeting-policy machinery. They do not set final values for Knight, Orator,
Time Mage, Necromancer, Special Knight, Ramza, or any status effect.

## Expected Counter Output

GPT and Claude T8 counters should produce:

- one row per scenario;
- `scenario_id`;
- `model`;
- selected candidate and target;
- selected score and challenge bonus fields;
- override flags;
- validation errors, if any.

The dual-independent gate from document 07 applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T8 output can be used to accept or reject challenge-like values.

## What T8.0 Does Not Decide

Still open for later T8 versions:

- exact Knight `Challenge` value, range, duration, hit rate, CT cost, and refresh behavior;
- whether `Challenge` is a hard force, soft mark, zone effect, CT pressure, or counter-pressure;
- boss, monster, undead, unique-unit, and status-immune policies;
- how full enemy AI evaluates healing, spellcasting, movement, charging, item use, and objectives;
- implementation proof for data-mod targeting changes;
- T2 incidence thresholds for defender or anti-targeting tools.

## Design Guardrails

- Challenge should make enemies respect the Knight's position, not remove enemy agency.
- Soft challenge should redirect close choices, not override obvious lethal or survival decisions.
- Hard challenge should be narrow, counterable, or boss-limited if it survives later design.
- Targeting manipulation must not become a mandatory support or defender tax. T2 must flag any
  accepted build population where one challenge tool or one anti-challenge counter appears too
  often.
- Status control and AI targeting must be explicit. Do not hide broad control inside a small
  numeric mark.

## Claude Review Request

Claude should review whether:

- soft challenge as the default healthy direction is appropriate;
- hard challenge should remain in T8.0 as a tested high-risk branch;
- the eligibility and control override rules are sufficient;
- the scenario set proves additive soft-challenge scoring and hard-challenge fallback;
- any additional row is required before accepting T8.0.

Claude review verdict: Accepted after adding tie-break, hard-challenge ineligible fallback, multiple
challenger-candidate, and multi-bonus rows requested during review (claude-opus-4-8, 2026-06-21).
