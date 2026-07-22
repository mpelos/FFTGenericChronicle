# Injury movement conditional timeline closure

## Scope closed

Critical knockback and authored Injury displacement use the settled ForcedMovement carrier across
physical, direct-magic, and Area Damage. Physical and Area multi-Strike actions can select more than
one positive movement for the same target without replaying map logic or exposing intermediate
tiles as gameplay events.

## Implemented contract

- Each reachable positive distance remains a frozen native verdict.
- A multi-Strike branch forest indexes those verdict sets by every reachable origin tile for that
  Strike.
- Physical execution tracks one projected movement tile per target. Area execution and exact
  evaluation carry the tile in their target-local state.
- The projected movement tile selects only the later map verdict. It does not alter deferred
  attack, defense, target, Injury, Rider, or Reaction gates.
- Forecast and AI enumerate all reachable origin branches without execution RNG.
- Native publication accepts multiple movement carriers for one target only when every later origin
  equals the preceding selected destination.
- Native auxiliary apply validates origin and destination readback for every selected movement in
  Strike order.
- KO and blocked zero movement leave the tile unchanged, preserve Aim, and create no displacement
  concentration incident.

## Validation

- `InjuryMovementTimeline4` builds with zero warnings and zero errors.
- `dotnet .../InjuryMovementTimeline4/...smoketests.dll --test-dcl-injury-movement`
  completes with code zero and prints `DCL Injury movement smoke tests passed.`
- The physical sentinel selects and applies `X -> X+1 -> X+2` across two critical Strikes while its
  exact forecast carries every reachable origin through all four Barrage Strikes.
- The Area NativeRepeat sentinel selects, publishes, and applies
  `X -> X+1 -> X+2 -> X+3` across three critical Strikes.
- A discontinuous synthetic plan remains rejected before native apply.

## Remaining live boundary

The production adapter still has to capture the complete frozen branch forest from native map
state and bind the auxiliary writer/readback to the live apply callbacks. No live test is required
to establish the offline timeline semantics.
