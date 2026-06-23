# Scenario Set V0

Status: Accepted
Scenario set version: `scenario-set-v0`
Date: 2026-06-20
Depends on:
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/02-variable-palette.md`
- `docs/formula-balance/03-family-taxonomy-and-viability.md`
- `docs/formula-balance/04-proof-and-baseline-plan.md`
- `docs/formula-balance/05-formula-proposal-protocol.md`
- `docs/formula-balance/06-damage-spec-v0.md`
- `docs/formula-balance/07-player-feedback-signals.md`
- `work/baseline_jobs.csv`
- `work/baseline_abilities.csv`
Review: Approved by Claude on 2026-06-20 after correcting job anchors to the generic player
job block.

## Purpose

This document defines the shared scenario set for provisional damage simulations.

`05-formula-proposal-protocol.md` requires every formula proposal to use a versioned scenario
set. This file is that first shared version. It gives GPT and Claude the same early, mid, and
late combat situations before any family-specific formula proposal is written.

This scenario set does not accept any formula and does not override the Windows proof gate in
`04-proof-and-baseline-plan.md`. Until `work/baseline_weapons.csv` exists, any formula tested
against this scenario set can only be:

```text
Conceptually viable, pending verified-baseline re-sim
```

## Source Labels

Use these labels in scenario rows and simulation outputs:

- `verified-local`: derived from committed local CSVs or docs in this repo.
- `missing-weapon-baseline`: blocked on `work/baseline_weapons.csv`.
- `WotL-fallback`: classic/WotL assumption used until IVC proof replaces it.
- `assumed`: scenario choice made for coverage, not a verified IVC encounter.
- `to-verify-in-04`: should be updated after the Windows proof session.

## Committed Local Anchors

The job profiles below are anchored in `work/baseline_jobs.csv`.

All anchors use the generic player job block `74-92` for stat consistency. Lower ids such as
Knight `61` are stronger non-generic variants and must not be used as scenario anchors unless a
future proposal explicitly tests non-generic enemies.

| Job | Job id | Useful local facts | Role in scenarios |
| --- | --- | --- | --- |
| Squire | 74 | baseline generic profile, sword/knife/axe/flail access | early average physical attacker and target |
| Chemist | 75 | low PA/MA, gun/knife access | light target, gun baseline user |
| Knight | 76 | high HP/PA, shield/armor/sword/knight sword access | durable target and strong PA attacker |
| Archer | 77 | bow/crossbow access, moderate PA | ranged baseline attacker |
| Monk | 78 | high HP/PA, Brave-sensitive unarmed route | martial stress case |
| White Mage | 79 | high MP/MA support profile | magic-relevant target and healer baseline |
| Black Mage | 80 | high MA, low HP, rod access | offensive magic baseline |
| Time Mage | 81 | high MA support/status profile | status/CT baseline |
| Summoner | 82 | high MP/MA, low Speed, staff/rod access | delayed AoE magic baseline |
| Thief | 83 | high Speed, knife access, high class evasion | light/evasive target and fast attacker |
| Mystic | 85 | MA profile, pole/book/staff/rod access | magic/status hybrid target |
| Orator | 84 | gun/knife access, lower combat stats | low-stat ranged/support test |
| Geomancer | 86 | balanced PA/MA, axe/sword/shield access | hybrid physical baseline |
| Dragoon | 87 | high PA/HP, polearm access | reach/jump physical baseline |
| Samurai | 88 | high PA, katana access, lower HP | Brave/MA-adjacent physical baseline |
| Ninja | 89 | high Speed/PA, knife/ninja blade/flail access | fast/dual-wield stress attacker |
| Arithmetician | 90 | low Speed/PA/MA, book/pole access | fragile magic-relevant target, not a formula baseline |
| Dancer | 92 | cloth/bag/knife access, low HP | job-lock niche and fragile target |
| Bard | 91 | instrument/bag access, very low PA | job-lock niche and fragile target |

Ability names and override columns are anchored in `work/baseline_abilities.csv`, but most base
Formula/X/Y values are hardcoded in `FFT_enhanced.exe` and remain WotL fallback until extracted
or proven.

## Phase Definitions

These are coverage phases, not exact encounter recreations.

| Phase | Scenario level band | Purpose | Label |
| --- | --- | --- | --- |
| Early | 4-10 | Prove that a formula does not break Chapter 1-style damage scale | assumed |
| Mid | 18-30 | Test formulas after core jobs and mid-tier equipment begin interacting | assumed |
| Late | 40-60 | Test endgame identity and dominance risks | assumed |
| Stress | 60+ or optimized build | Expose support, Brave/Faith, dual-wield, and reliability collapse risks | assumed |

Exact displayed stats for each phase should be filled by the simulation harness from the
accepted stat model or by a documented WotL fallback. Do not treat the phase bands as verified
IVC encounter levels.

## Target Profiles

Every family-level proposal should test at least the target profiles relevant to its damage type.

| Target id | Profile | Suggested local anchor | What it tests |
| --- | --- | --- | --- |
| `T-LIGHT` | low durability, normal evasion | Chemist, Black Mage, Bard, Dancer | overkill, floor damage, weak target pacing |
| `T-DURABLE` | high HP/physical profile | Knight, Monk, Dragoon | whether damage remains useful into hard targets |
| `T-MAGIC` | high Faith or magic-relevant profile | White Mage, Black Mage, Time Mage, Mystic | Faith/magic coexistence and Shell/status assumptions |
| `T-EVASIVE` | high class evasion or reliability pressure | Thief, Ninja, Samurai | accuracy, Direct flags, expected value after evasion |
| `T-RESIST` | element/status resistance context | job/equipment TBD after baseline | element/status edge cases |

`T-RESIST` is intentionally incomplete until equipment and status baselines are expanded.

## Attacker Profiles

Formula proposals should pick attacker profiles by family role, not by whichever profile makes
the formula look best.

| Attacker id | Profile | Suggested local anchor | Used for |
| --- | --- | --- | --- |
| `A-AVG-PHYS` | average physical user | Squire, Geomancer | baseline weapon usability |
| `A-STR-PHYS` | strong PA user | Knight, Monk, Dragoon, Samurai | high-stat scaling and late-game pressure |
| `A-FAST` | Speed-heavy user | Thief, Ninja, Archer | knife/ninja/bow/speed formulas |
| `A-RANGE` | ranged physical user | Archer, Chemist, Orator | bow/crossbow/gun reliability and positioning |
| `A-MAG` | strong MA user | Black Mage, Summoner, Time Mage, White Mage | spell and magic-weapon coexistence |
| `A-HYBRID` | mixed PA/MA user | Geomancer, Samurai, Mystic | hybrid weapon and ability routines |
| `A-JOBLOCK` | narrow family user | Bard, Dancer, Samurai, Ninja | job-lock niche payoff |

## Core Scenario Matrix

Each row is a reusable scenario shell. A formula proposal may add family-specific scenarios, but
it should not remove the relevant shared rows without documenting why.

| Scenario id | Phase | Attacker | Targets | Equipment contexts | Primary question |
| --- | --- | --- | --- | --- | --- |
| `S-EARLY-AVG-MELEE` | Early | `A-AVG-PHYS` | `T-LIGHT`, `T-DURABLE` | low WP and next available WP | Does the family work before late-game supports? |
| `S-EARLY-FAST` | Early | `A-FAST` | `T-LIGHT`, `T-EVASIVE` | early knife/bow-style context | Does Speed scaling help without spiking early damage? |
| `S-EARLY-MAGIC` | Early | `A-MAG` | `T-LIGHT`, `T-MAGIC` | low-tier spell/rod/staff context | Does magic remain useful with CT and Faith assumptions? |
| `S-MID-PA` | Mid | `A-STR-PHYS` | `T-LIGHT`, `T-DURABLE` | mid WP and shield/no-shield | Does PA scaling stay in the FFT damage band? |
| `S-MID-RANGE` | Mid | `A-RANGE` | `T-LIGHT`, `T-EVASIVE` | bow/crossbow/gun-style contexts | Does range plus reliability create dominance? |
| `S-MID-HYBRID` | Mid | `A-HYBRID` | `T-DURABLE`, `T-MAGIC` | mixed PA/MA equipment | Does hybrid scaling create a distinct role? |
| `S-MID-VOLATILE` | Mid | `A-STR-PHYS`, `A-AVG-PHYS` | `T-LIGHT`, `T-DURABLE` | random-family low/high WP contexts | Is volatility interesting rather than trash? |
| `S-LATE-PREMIUM` | Late | `A-STR-PHYS` | `T-DURABLE`, `T-EVASIVE` | high WP, premium support candidate | Does the family compete without becoming universal? |
| `S-LATE-FAST` | Late | `A-FAST` | `T-LIGHT`, `T-DURABLE`, `T-EVASIVE` | late knife/ninja/bow contexts | Does Speed scaling survive endgame? |
| `S-LATE-MAGIC` | Late | `A-MAG` | `T-DURABLE`, `T-MAGIC` | late spell/rod/staff context | Does magic coexist with instant physical routes? |
| `S-LATE-JOBLOCK` | Late | `A-JOBLOCK` | `T-LIGHT`, `T-DURABLE` | job-native equipment only and borrowed skillset context | Is the job-locked family worth using? |
| `S-STRESS-SUPPORTS` | Stress | family-best plausible attacker | `T-DURABLE`, `T-EVASIVE` | no support, Attack Boost, Two Hands, Two Swords, Martial Arts where legal | Does a support engine collapse balance? |
| `S-STRESS-BRAVE-FAITH` | Stress | relevant physical or magical user | `T-MAGIC`, `T-DURABLE` | Brave 30/70/85/97 and Faith 30/70/85/97 cases | Does Brave/Faith optimization break the formula or reaction/status ecology? |
| `S-STRESS-ACCURACY` | Stress | `A-RANGE`, `A-FAST` | `T-EVASIVE` | evadable, Direct/unevadable if legal | Does expected damage remain honest after hit rate? |

`S-STRESS-BRAVE-FAITH` must separate these lenses:

- Brave-scaling weapon families use continuous Brave values, especially `30`, `70`, `85`, and `97`.
- Morale, challenge, and reaction-susceptibility rows may use Brave bands if the accepted mechanic
  is banded.
- Magic damage, magical healing, and faith-facing status rows use Faith values `30`, `70`, `85`,
  and `97` with the accepted Faith floor unless a later policy supersedes it.
- `Faith`-status/Oil-style setup spikes may be included as combo stress rows, but only as
  multi-action setup checks rather than normal one-action benchmarks.

## Required Equipment Contexts

Every formula proposal must test at least two equipment contexts for the family:

1. `context-floor`: the weakest or earliest plausible version of the family in the tested phase.
2. `context-main`: the expected normal version of the family in that phase.

Late-game proposals should add:

3. `context-ceiling`: the strongest plausible legal version before rare/unique abuse.
4. `context-stress`: the strongest plausible legal version with dominant supports or stat
   manipulation.

Until `work/baseline_weapons.csv` exists, equipment contexts must be labeled:

```text
Equipment context: estimated / WotL-fallback / missing-weapon-baseline
```

## Required Output Columns

Simulation output using this scenario set should include:

```text
scenario_set_version
scenario_id
phase
attacker_profile
attacker_job
attacker_level_or_band
attacker_effective_stats
attacker_equipment_context
target_profile
target_job
target_level_or_band
target_effective_stats
target_status_or_element_context
formula_or_routine
damage_spec_version
damage_on_hit
hit_rate_assumption
expected_damage_after_hit_rate
min
max
mode_or_expected_value
baseline_comparison
player_signal_check
design_verdict
technical_verdict
open_proof_needs
```

## Proposal Coverage Rules

For a narrow family formula proposal, minimum coverage is:

- one early or mid scenario where the family should first be useful;
- one late scenario where the family is expected to remain viable;
- one durable target;
- one light target;
- one stress row for the support or stat engine most likely to break the family;
- at least two equipment contexts.

For a global formula or shared-routine proposal, minimum coverage is:

- every phase: early, mid, late;
- average and strong attacker profiles;
- light, durable, magic-relevant, and evasive targets when applicable;
- all affected families in at least one scenario each;
- stress rows for Two Hands, Two Swords, Attack Boost, Martial Arts, Brave/Faith, and accuracy
  whenever those engines can legally interact with the formula.

## Player-Signal Check Integration

Each scenario verdict should include the `07-player-feedback-signals.md` check:

```text
Failure mode addressed:
Dominance engine tested:
Magic/physical coexistence impact:
Accuracy/reliability impact:
Legibility impact:
FFT-feel risk:
```

This prevents simulations from declaring victory only because damage numbers are close.

## What Remains Blocked

This scenario set is useful for shared provisional simulations, but it does not unblock final
formula acceptance.

Still blocked until the Windows `04` session:

- verified weapon WP/formula/range/evasion/equipment contexts;
- proof that `ItemWeaponData.Formula` reroutes weapon computation in game;
- proof that `OverrideAbilityActionData.Formula/X/Y` changes damage in game;
- verified rounding, modifier order, and enough `damage-spec-v1` facts to retire the most
  important `damage-spec-v0` assumptions.

After `work/baseline_weapons.csv` exists, update this document or supersede it with
`scenario-set-v1`.
