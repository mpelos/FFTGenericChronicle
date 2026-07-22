# Area ForcedMovement canonical vertical

## Scope

This checkpoint validates the revised DCL pure Area `ForcedMovement` Carrier without introducing
jobs, Fear, stop-hit behavior, or per-tile movement callbacks.

## Hypothesis

A one-Strike, magnitude-free Area action with one `ForcedMovement` Carrier can reuse the canonical
Area family when every target supplies one immutable final native movement verdict. Delivery,
immunity, resistance, forecast, AI, execution, native projection, payment, and Reaction ownership
must remain correlated under one outer ActionInstance.

## Offline result

- Capability loading accepts only the exact pure movement shape and exact `SingleResult` or
  `StatusPacket` carrier/rewrite pair.
- The native target list remains the immutable geometric batch. Each canonical target retains one
  final movement verdict with matching target, origin, requested distance, and direction.
- Carrier immunity is evaluated before the Area Quick Contest and consumes no resistance RNG.
- A delivered target publishes no HP/MP/status magnitude; its final movement verdict appears only
  in auxiliary native projection.
- A resisted or immune target publishes an empty carrier and no movement auxiliary.
- Player forecast and AI use the same target delivery probability and expose expected moved tiles.
- Execution consumes one shared caster roll plus only the reachable per-target resistance rolls.
- All targets settle under one payment and one terminal Reaction window. No intermediate tile is
  represented in the ledger or native projection.

The smoke sentinel uses two targets: one delivered two tiles and one movement-immune target. It
asserts six total random draws, two target ledger rows, auxiliary movement only for the delivered
target, empty numeric channels for both, and one Reaction result.

## Regression found and corrected

The initial pure-status Area branch had attached the delivered-numeric magnitude validation to an
`else if` whose owner was the status-only branch. Introducing the second magnitude-free Carrier
made that control-flow ambiguity visible. The validation now independently rejects a landed
numeric strike without magnitude while permitting either pure status or pure movement to remain
magnitude-free.

## Validation

```text
dotnet build codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c AreaMovementSentinel --no-restore --nologo
Build succeeded. 0 Warning(s). 0 Error(s).

dotnet run --project codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c AreaMovementSentinel --no-build
formula runtime smoke tests passed
```

## Remaining lifecycle gap

`DclCanonicalForcedMovementResult` correctly derives `CancelAim` and
`CreatesConcentrationIncident` only from positive final displacement. The standalone and Area
movement execution paths do not yet commit those signals into the Aim registry or the charged
action timeline. This gap must be closed before movement-caused Aim cancellation or concentration
can be claimed as an executed lifecycle consequence. The native map callback, critical knockback,
and Damage-plus-ForcedMovement composition also remain separate later gates.
