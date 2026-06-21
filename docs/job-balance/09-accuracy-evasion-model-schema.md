# Accuracy And Evasion Model Schema V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `work/t4-accuracy-evasion-scenarios-v0.json`

## Purpose

This document starts T4, the accuracy/evasion/positioning validation track.

T4 exists to validate skills that can alter:

- hit chance;
- physical evasion;
- magical evasion;
- shield, accessory, class, and weapon evasion;
- facing;
- line-of-fire eligibility;
- height-aware ranged identity.

The immediate consumers are Knight and Archer provisional designs:

- Knight: `Parry`, `Shield Break`, guard pressure, shield/evasion stacks.
- Archer: `Concentration`, `Arrow Guard`, `High-Ground Shot`, `Aimed Shot` accuracy pieces.
- Chemist: `Smoke Bomb`, if it alters accuracy/evasion.

## Source Notes

The T4.0 formulas are anchored in the Final Fantasy Tactics Battle Mechanics Guide by AeroStar:

- physical evasion uses class, shield, accessory, and weapon evasion depending on facing;
- weapon evasion only applies when the target has Weapon Guard/Parry-style behavior;
- magical evasion uses magical shield and accessory evasion only;
- zodiac compatibility does not affect evasion;
- Charge/Aim attacks use normal weapon evasion and fail if the target panel is vacated before
  resolution;
- Jump is listed as not evadable, which makes it a useful non-evadable control row later.

Reference URL:

```text
https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
```

T4 does not copy FFT as a hard design rule. It uses FFT's model as the baseline so the mod can
change accuracy/evasion deliberately instead of accidentally.

## Pinned Bundle

Pinned input bundle:

```text
work/t4-accuracy-evasion-scenarios-v0.json
```

The bundle defines the formulas, scenarios, and expected values for the first dual-independent T4
run.

## Formula Contract

T4.0 uses integer truncation after the full multiplication.

Before each evasion factor is applied:

```text
effective_evade = min(100, max(0, evade * evasion_multiplier))
```

Each factor is therefore always:

```text
(100 - effective_evade)
```

After truncation, final displayed hit is also clamped:

```text
expected_hit = min(100, max(0, truncated_hit))
```

For physical attacks:

```text
front_hit = trunc(base_hit * (100 - P.CEv) * (100 - P.SEv) * (100 - P.AEv) * (100 - WEv) / 100^4)
side_hit  = trunc(base_hit * (100 - P.SEv) * (100 - P.AEv) * (100 - WEv) / 100^3)
rear_hit  = trunc(base_hit * (100 - P.AEv) / 100)
```

For magical attacks:

```text
magic_hit = trunc(base_hit * (100 - M.SEv) * (100 - M.AEv) / 100^2)
```

For T4.0:

- values are percentages from `0` to `100`;
- facing is an input label: `front`, `side`, `rear`, or `any`;
- line-of-fire is a target eligibility flag, not yet a geometry solver;
- height is recorded as scenario context and only affects output if a later accepted skill adds a
  height rule;
- `evasion_multiplier` can model Defending/Abandon-style doubled evasion in stress rows;
- T4.0 does not model random rolls, only expected displayed hit percentage.

## Scenario Set

The first bundle intentionally includes small closed-form rows:

- physical front/side/rear with the same evasion profile;
- magical shield/accessory evasion;
- lower base-hit physical rows;
- weapon evasion enabled versus disabled;
- doubled-evasion stress;
- an explicit over-100 effective evasion clamp row;
- non-evadable action row;
- line-of-fire blocked row;
- height context row with no vanilla hit change.

These rows are not final balance scenarios. They are arithmetic and contract rows to prove GPT and
Claude implement the same T4 baseline before using it for job skills.

## Expected Counter Output

GPT and Claude T4 counters should produce:

- one row per scenario;
- `scenario_id`;
- `model`: `physical`, `magical`, `non_evadable`, or `targeting`;
- `expected_hit`;
- `can_target`;
- formula factors used;
- validation errors, if any.

The dual-independent gate from document 07 applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T4 output can be used to accept or reject skill values.

## What T4.0 Does Not Decide

Still open for later T4 versions:

- exact Concentration redesign;
- exact Arrow Guard/Parry behavior;
- whether Shield Break changes evasion directly or applies a guard state;
- bow/crossbow line-of-fire geometry;
- height bonus shape for Archer;
- interaction with status effects such as Blind, Darkness, Defending, or custom smoke;
- exact rules for Blind/Darkness, if the mod wants them in T4.1 rather than a later status track.

## Claude Review Request

Claude should review whether:

- the formula contract is specific enough for independent implementation;
- the scenario set covers the right first rows;
- source interpretation is correct;
- line-of-fire and height are scoped narrowly enough for T4.0;
- any additional row is required before implementing counters.

Claude review verdict: Accepted (claude-opus-4-8, 2026-06-20).
