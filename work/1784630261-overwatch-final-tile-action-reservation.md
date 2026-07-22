# Overwatch final-tile Action reservation

## Audit result

The prepared-state runtime rejected mid-route movement but its production-shaped Overwatch input
contained only booleans. It could not prove which unit moved, which final tile was committed, which
prepared owner/weapon fired, or which target belonged to the effect Action. It also had no battle
event replay guard or ActionInstance reservation.

The unit-removal audit exposed a separate unresolved edge: the DCL specifies that hard removal
clears all states, but does not state how a still-charging unit-target spell settles when its tracked
target is removed from the battle entirely. No target-loss cancellation/payment policy was invented
during this pass.

## Implemented contract

- One accepted `DclFinalTileSnapshot` supplies the current battle generation, mover table/character
  identity, completed native route, and identical actor/target/unit final tile.
- The battle resolves the mover through its observed `UnitKey`; a mismatched event fails before
  prepared-state mutation.
- The caller supplies the complete candidate set. Duplicate state ids are rejected and candidates
  resolve in ascending `StateInstanceId` regardless of input order.
- Every candidate binds the exact prepared owner, current owner tile, payload weapon slot, source
  capability, target eligibility, range, vertical, trajectory, and authored trigger verdict.
- The battle accepts each movement sequence only once. Duplicate or stale event delivery fails
  before consuming a surviving state.
- Firing candidates reserve ActionInstance ids in stable candidate order, consume one persistent
  use, and return an immediate zero-MP unit-target declaration whose tracked target is the mover.
- The persistent payload owns both `EffectAbilityId` and `EffectActionId`. Application, checkpoint
  restore, final-tile validation, and trigger commit require that native ability to remain bound to
  the exact Action/profile revision; an Action name alone is never treated as an executable carrier.
- The timeline checkpoint schema advances to revision 4 and revalidates every restored prepared
  payload against the loaded runtime before the timeline resumes.
- Nonmatching trigger conditions preserve the prepared state and reserve no ActionInstance.
- Events before settlement remain on the old no-op path and never enter the final-tile batch.

## Sentinels

The canonical runtime smoke path proves:

1. a final-tile event whose character identity disagrees with the observed mover is rejected without
   changing either prepared state;
2. a native effect ability bound to another Action is rejected before state application;
3. reversed candidate input is normalized to stable state-instance order;
4. one firing state is consumed and receives the exact ability plus owner/mover/tile zero-cost
   declaration;
5. one nonmatching state survives with its remaining use unchanged and no reservation;
6. replaying the same native movement sequence is rejected without consuming the survivor.

## Verification

- isolated `OverwatchEffectBinding` build: zero warnings and zero errors;
- `--test-dcl-injury-movement`: passed direct, physical, Area single/multi-Strike, Injury movement,
  prepared-state, QuickLock, final-tile reservation, and effect-binding sentinels;
- complete smoke executable: `formula runtime smoke tests passed`.

## Remaining native boundary

The finalizer hook remains observe-only until its current-build live proof is accepted. After that
proof, the adapter must construct the complete candidate batch from synchronized unit/equipment/map
state and dispatch each reserved Action sequentially through its classified confirmed-execution
family. Effect presentation, protection redirect/result, and prepared-state visible links remain
separate bindings.
