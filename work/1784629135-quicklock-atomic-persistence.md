# QuickLock atomic persistence

## Gap

Quick applied its CT grant and controller lock before the state registry validated the persistent
QuickLock materialization. A malformed duration or other registry-invalid materialization could
throw after those first two mutations, leaving a turn-eligible clock and controller without the
matching state.

## Closure

- The state registry exposes a no-mutation validation path for ordinary stacking policies.
- The canonical battle validation path combines observed UnitKey checks, prepared-payload checks,
  and generic registry application checks.
- Quick constructs and validates the complete QuickLock application before declaration RNG or any
  CT/controller mutation.
- Confirmed Quick routes persistence through the canonical battle owner.
- The commit callback retains defensive rollback: if persistence fails after controller/CT commit,
  it removes the controller lock and applies the exact inverse CT delta before rethrowing.
- Granted-turn completion prepares an exact QuickLock controller/state-instance reservation before
  the battle expires target/source-turn states. Missing or duplicate persistence fails while the
  active turn and controller remain intact. Successful battle completion then removes that exact
  state instance and controller lock.

## Sentinel

A confirmed Quick request supplies an invalid positive duration to an `ExplicitCommand` QuickLock.
The request fails before execution and preserves all five observable owners:

- target CT remains at its original value;
- the QuickLock controller remains unlocked;
- the state registry remains empty;
- the underlying random source consumes zero dice;
- the battle execution ledger remains empty.

The valid confirmed Quick path continues to consume one shared casting site, grant exact CT,
materialize one QuickLock, and publish the outer action.

A second negative sentinel grants a turn with a controller lock but deliberately omits its
persistent state. Turn completion rejects the mismatch before mutation and preserves both the
active-turn boundary and controller for diagnosis/retry. The valid timeline path still removes the
single exact lock only after completing the granted turn.

## Verification

- isolated `QuickLockTurnAtomicity2` build: zero warnings and zero errors;
- `--test-dcl-injury-movement`: passed the full canonical runtime path, direct and physical
  execution, Area single/multi-Strike execution, Injury movement, prepared-state, and QuickLock
  sentinels;
- complete smoke executable: `formula runtime smoke tests passed`.
