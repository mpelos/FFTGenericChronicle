# Formula Proposal Protocol

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/formula-balance/00-envelope.md`
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/02-variable-palette.md`
- `docs/formula-balance/03-family-taxonomy-and-viability.md`
- `docs/formula-balance/04-proof-and-baseline-plan.md`
Review: Approved by Claude on 2026-06-20 after adding independent recomputation as a review
requirement.

## Purpose

This protocol defines how concrete formula proposals must be written, simulated, reviewed, and
accepted.

No formula is accepted on concept alone. Every formula needs a shared calculation spec, shared
scenario set, documented simulation output, and Claude review.

## Status Labels For Formula Proposals

Formula proposals must use one of these statuses:

- `Exploratory`: idea only; not simulated.
- `Conceptually viable, pending verified-baseline re-sim`: simulated against estimated or WotL
  fallback baselines, but not against verified IVC baseline data.
- `Rejected`: failed simulation, violated design goals, or depends on unacceptable cost/risk.
- `Accepted`: simulated against verified IVC baseline, reviewed by Claude, and approved.

Until the Windows proof plan runs and baseline data is captured, formula proposals cannot be
more than `Conceptually viable, pending verified-baseline re-sim`.

## S1 - Shared Calculation Spec

All simulations must use the same calculation spec for a given proposal version. Independent
review is only useful if GPT and Claude are reviewing the same arithmetic.

The spec must declare:

- formula being tested;
- exact operation order;
- truncation/flooring assumptions;
- variance/randomness model;
- hit-rate assumptions;
- support modifiers included or excluded;
- elemental modifiers included or excluded;
- Protect/Shell and defensive modifiers included or excluded;
- Zodiac included or excluded;
- which pieces are verified IVC facts, WotL fallback, or assumptions.

Pre-proof specs must be labeled as assumptions when IVC truncation, variance, or modifier order
has not been verified.

Template:

```text
Spec version:
Formula:
Operation order:
Rounding/truncation:
Variance:
Accuracy:
Supports:
Element:
Protect/Shell:
Zodiac:
Verified facts:
Fallback assumptions:
Open proof needs:
```

## S2 - Shared Scenario Set

Every formula proposal must use a shared scenario set so different formulas can be compared
against the same combat situations.

The scenario set must have its own version. If scenarios change, prior PASS verdicts must be
rechecked against the new scenario version before they can keep their status.

Minimum scenario coverage:

- early-game, mid-game, and late-game;
- at least one strong attacker and one average attacker;
- light target, durable target, and magic-relevant target when applicable;
- at least two equipment contexts for the weapon family;
- baseline comparison against IVC data, or WotL fallback when IVC data is not yet available.

Scenario inputs should be derived from committed baselines where possible:

- `work/baseline_jobs.csv`
- `work/baseline_abilities.csv`
- `work/baseline_weapons.csv` after the Windows session creates it

Template:

```text
Scenario set version:
Scenario id:
Phase: early / mid / late
Attacker archetype:
Attacker stats:
Attacker equipment:
Target archetype:
Target stats:
Target equipment/status:
Baseline action:
Test action:
Notes:
```

## S3 - Simulation Output And Verdict

Each proposal must output more than numbers. It needs a verdict against the accepted design
metrics.

Required output:

- input scenario table;
- formula and spec version used;
- expected damage or effect result;
- min/max/mode or expected value for random formulas;
- hit-rate assumptions if evasion or accuracy matters;
- comparison to baseline;
- PASS/FAIL against design criteria.

Design criteria:

- no universal weapon-family dominance;
- FFT-like damage scale unless drift is intentional and documented;
- distinct weapon-family identity;
- physical/magic coexistence;
- clear player-readable behavior;
- acceptable implementation dependency and proof cost.

Template:

```text
Proposal:
Spec version:
Scenario results:
Baseline comparison:
Variance / hit-rate notes:
Design verdict:
Technical verdict:
Overall status:
```

## S4 - Independent Review Model

GPT writes the proposal and documents the primary simulation. Claude reviews as a blocking
reviewer, but review is not only a read-through of GPT's output.

The review must check:

- arithmetic correctness;
- scenario coverage;
- whether assumptions are labeled correctly;
- whether the formula meets the design goal;
- whether the formula creates new dominance risks;
- whether the implementation dependency is honest.

For any non-trivial formula, Claude must recompute a subset of the scenarios through a different
path from the primary harness run. The recomputation can be manual, spreadsheet-based, or a
separate calculation path, but it must be independent enough to catch a wrong spec interpretation
or harness bug.

The shared harness exists for comparability: both reviewers should use the same spec and same
scenario inputs. It is not the source of truth by itself. A formula earns confidence when two
independent derivations agree on the relevant outputs.

If Claude's recomputation diverges from GPT's simulation, the proposal is blocked until
reconciled. The reconciliation must classify the cause as one of:

- spec ambiguity or spec error;
- arithmetic or implementation error;
- design interpretation disagreement.

The fix should happen in the right layer. Do not patch numbers to hide a spec problem, and do not
change the design verdict when the arithmetic is simply wrong.

## Harness Policy

A shared simulation harness is allowed and encouraged, but it does not replace review.

If the project adds `tools/sim_damage.py`, it should:

- load a versioned calculation spec;
- load a shared scenario set;
- compute scenario outputs consistently;
- emit a reviewable matrix;
- keep assumptions visible rather than hiding them in code.

The harness should make comparison easier, not turn formula approval into an unreviewed script
output.

## What Remains Blocked

Even with this protocol accepted:

- no concrete formula proposal can be accepted unless it follows this protocol;
- no numeric formula can be treated as consensus;
- no family-specific formula document should claim final viability.

Until the Windows proof plan runs:

- formula proposals can only be provisional;
- baseline comparisons must be labeled as estimated or WotL fallback;
- any IVC-specific damage conclusion remains pending verified-baseline re-simulation.
