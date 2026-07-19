# DCL battle-generation lifecycle closure

## Gap

The runtime had bounded caches and several pointer-reuse guards, but no single battle-generation
contract. The unit registry removed invalid pointers, while action, hit, compute-point, and status-plan
caches were index-keyed and could survive until TTL. Status durations and synthetic-Reaction state
could also outlive a same-pointer character replacement if the table slot never passed through an
invalid observation. Settings hot reload cleared only the pending tracker and compute-point cache.

That left a short but real cross-battle contamination window when a new battle reused the same unit
table slots and action tuple.

## Implemented mechanism

- `DclBattleLifecycle` tracks `(unit pointer, character id)` identities and a monotonic generation.
- The first identity starts a generation.
- Re-observing the same pair is stable.
- A different character at the same pointer is a hard boundary and drops old-generation identities.
- Forgetting the last current-generation unit ends the battle.
- Battle start, hard reuse, battle end, and settings hot reload call one reset path.
- The reset clears pending/action/hit/compute/status caches, status durations, Guard pools, MP trickle
  edges, Reaction cadence, synthetic reservations and the native producer mailbox, preview values,
  hit stamps, and legacy pre-clamp plan activations.
- Per-unit removal now also clears Reaction cadence, in addition to the existing status/Guard/MP and
  synthetic state.
- Native unit HP/MP/status/equipment data is not part of the reset.

## Offline falsifiers

The smoke suite proves generation start, stable identity, multiple-unit membership, same-pointer
identity reuse, stale-generation forget behavior, last-unit battle end, and a subsequent generation.
It also proves immediate `Clear()` behavior for action-context, hit-decision, compute-point, and
prepared-status caches, plus synthetic-Reaction reservation reuse after reset. The existing Guard,
MP, and Reaction cadence tests continue to prove their per-character pointer-reuse behavior.

The remaining evidence is live integration: leave one battle, enter another using the same process,
and verify the reset log precedes any new DCL decision while no previous preview/result/status/
Reaction state is consumed.
