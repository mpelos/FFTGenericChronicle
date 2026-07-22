# Injury settled movement WIP

## Objective

Route critical knockback and future authored Damage displacement through the same final native map
carrier used by standalone and Area ForcedMovement, with no per-tile work and no intermediate
Reaction.

## Implemented in this checkpoint

- `DclCanonicalInjuryMovementBranchSet` freezes one native verdict per positive requested-distance
  branch and validates target, origin, direction, distance, and path ownership.
- Physical, direct-magic, and Area Injury execution accept the branch set, select only the distance
  produced by the resolved Strike, suppress movement on KO, and fail closed if a selected distance
  has no frozen verdict.
- Aim cancellation uses positive settled movement instead of requested displacement.
- Physical, direct, and Area native projections carry the selected final movement beside the Injury
  carrier.
- Native physical/direct/Area composers validate branch origins against synchronized target tiles.
- A dedicated physical sentinel uses one eligible critical crushing Strike plus three misses without
  adding RNG or a Reaction window.
- Physical consequence settlement now uses the synchronized weapon damage type, matching damage,
  forecast, critical knockback selection, and the final consequence resolver. The previous use of
  the generic physical action profile could classify one Strike as different damage types across
  those stages.
- Physical forecast/AI carry exact moved-tile expectation and fall probability across the full
  per-Strike state distribution.
- Direct-magic forecast/AI enumerate normal versus critical delivery and every magnitude result,
  then resolve survival, requested distance, the frozen map branch, moved tiles, and fall.
- Area forecast/AI carry moved tiles and fall in the target-local HP/KO state across Strikes.
- Injury concentration planning receives actual settled displacement. A blocked movement no longer
  creates a movement incident merely because its requested distance was positive.
- The smoke-test executable accepts `--test-dcl-injury-movement`, which runs the canonical runtime
  through the Injury-movement sentinels and stops after the Area vertical.
- Execution/projection sentinels cover physical, direct magic, and a verified single-result Area
  carrier. Each publishes the selected movement in the native auxiliary carrier and retains only
  the one outer Reaction window.
- Focused branch sentinels cover KO suppression, a blocked zero-tile verdict, missing-distance
  rejection, exact fall probability, Aim preservation on zero movement, and zero-RNG concentration
  when both Injury and actual displacement are zero.
- Native publication rejects more than one positive movement carrier for the same target before
  apply while the conditional multi-Strike origin timeline is absent.

## Validation state

- `InjuryMovementSettlement18` builds with zero warnings and zero errors.
- `dotnet .../InjuryMovementSettlement18/...smoketests.dll --test-dcl-injury-movement`
  completed with code zero and printed `DCL Injury movement smoke tests passed.`
- The full canonical runtime mode still exceeded its 15-minute command limit after passing the
  earlier physical failure point. That full-mode result remains inconclusive and is not represented
  as a pass.
- A multi-Strike apply audit found an unresolved carrier dependency: the current per-Strike branch
  sets are all validated against the synchronized pre-action target tile. If the first native
  auxiliary carrier moves the target, a later movement carrier cannot validate that same origin.
  Forecast can enumerate total moved tiles, but native apply needs a conditional tile timeline (or
  another explicitly specified action-level aggregation rule) before multi-Strike Injury movement
  is closed.

## Required before closure

1. Specify and implement the conditional target-tile timeline for multi-Strike Injury movement;
   later map verdicts must originate from the actually selected earlier destination without making
   movement alter later target-local combat gates.
2. Add focused Injury-branch sentinels for KO suppression, blocked-zero movement, missing-distance
   rejection, fall probability, Aim preservation on zero movement, and concentration cardinality.
3. Validate the multi-Strike timeline through native auxiliary apply/readback rather than projection
   alone.
4. Only then promote the durable facts into `docs/modding` and the coverage report.

## Promoted durable subset

The supported one-positive-movement-per-target contract and the fail-closed multi-movement boundary
are reflected in `docs/modding/06-code-mod-runtime-dsl.md` and summarized in
`docs/modding/08-dcl-information-requirements.md`. The unresolved conditional multi-Strike origin
timeline remains only a WIP dependency and is not described as implemented.

## Closure continuation

The conditional origin timeline, contiguous native publication, and per-Strike apply/readback are
completed in `work/1784626435-injury-movement-timeline-closure.md`. The earlier required-before-
closure list remains above as investigation history rather than current status.
