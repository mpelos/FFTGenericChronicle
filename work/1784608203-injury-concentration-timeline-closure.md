# Injury concentration timeline closure

## Question

Do physical, direct-magic, and Area Injury executors commit their already-calculated concentration
result to the exact pending charged action, once per Strike and before the terminal Reaction?

## Result

Yes. Confirmed execution validates each target-owned Charging snapshot against the single timeline
attached to the battle before consuming RNG. The three Injury families deliver their
`DclInjuryConsequenceResult` to that timeline inside the guarded outer commit and publish the exact
`DclCanonicalChargedCancellation` beside the Strike result.

A failed concentration check cancels the original pending `ActionInstanceId` with
`ConcentrationFailure`. KO, Stun, and Knocked Down cancel directly with their specific reason and do
not consume a concentration roll. A preserved check retains the same action and due CT. Once an
earlier Strike cancels Charging, later Strikes in the same action create no further incident.

Injury and authored or critical displacement are already combined by
`ResolveStrikeIncident`, so one Strike cannot produce separate injury and displacement checks.

## Sentinels

- Direct magic: positive Injury, failed 3d6 concentration, exact pending action removed, one
  concentration ledger site, cancellation committed before the empty terminal Reaction window.
- Physical: first landed Strike fails concentration and removes the exact pending action; its status
  Rider and remaining Strike continue without another concentration site.
- Area: the first Strike KOs a Charging target, cancels the exact pending action with reason `Ko`
  without a concentration draw, then target-local KO short-circuiting skips later Strikes.

## Validation

- Build configuration: `InjuryConcentrationAllFamilies2`.
- Build result: zero warnings and zero errors.
- Runtime result: `formula runtime smoke tests passed`.

No live test is required for this canonical lifecycle. Native Charging-status and timeline callbacks
remain live-gated.
