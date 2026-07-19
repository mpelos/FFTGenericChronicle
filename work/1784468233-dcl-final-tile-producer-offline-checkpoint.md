# DCL final-tile producer offline checkpoint

## Scope decision

- Mid-route stop-hit and every other between-tile gameplay effect are excluded.
- Position-triggered abilities may begin only after ordinary movement fully completes on the final
  committed tile.
- Fear is outside the active DCL scope and supplies no implementation or test requirement.

## Boundary result

Static control-flow and byte guards bind the post-route boundary to the exact five-byte call at RVA
`0x1FE93B` (`E8 90 E9 FF FF`). Movement updater `0x1FE59C` has already selected the terminal path.
The called finalizer reaches gameplay position commit caller `0xD43CF29`; after return, actor and
battle-unit coordinates agree while battle state is still `0x11`. The later dispatcher helper
`0x203ED4` advances the ordinary path to state `0x12`.

`tools/analyze_dcl_final_tile.py --check-only` reports PASS against the installed Enhanced
executable.

## Implemented offline

- `DclFinalTileNativeAsm` installs only as `ExecuteAfter` over the five-byte finalizer call.
- The native shim performs no calls and writes only a private 64-slot unmanaged ring.
- It synchronously copies actor/unit identity, route length/cursor, the full 128-byte route record,
  actor current/target tile, committed unit tile, actor state, and battle state.
- It preserves flags and every scratch register it uses and publishes the sequence last.
- The managed drain accepts only nonempty terminal routes with idle actor state, converged
  actor/target/unit coordinates, and battle state `0x11`.
- Battle generation, actor/unit identity, route signature, terminal cursor, and final tile provide
  idempotent event identity.
- Zero-length settlement, nonterminal route, coordinate mismatch, duplicate, and non-movement state
  fail closed.
- No reach, job, ability, target, effect, damage, Reaction, or presentation policy is embedded.

The runtime no longer installs or polls the Fear and Approach compatibility hooks. Settings
validation rejects either retired control when armed. Historical unified v3+ profiles remain in
`work/` as evidence but are removed from the active offline validation list; unified v2 remains the
latest valid pre-Fear sentinel.

## Offline validation

- C# Release build: PASS, zero warnings/errors.
- C# smoke tests: PASS.
- Final-tile static analyzer: PASS.
- Runtime settings validation: PASS for active profiles; explicit rejection tests cover wrong
  final-tile RVA, Approach enabled, and Fear enabled.
- Complete .NET offline gate: PASS.
- Coverage generator: PASS, 39 mechanisms.
- Timeless-doc checker: PASS.

## Minimal live gate

Enable only the observe-only final-tile producer and capture:

1. one completed player movement;
2. one completed AI movement;
3. Move preview followed by cancel;
4. one non-movement action;
5. any zero-length settlement emitted by the native updater.

Acceptance requires exactly one `[DCL-FINAL-TILE] accepted=1` row for each completed player/AI move,
with terminal cursor, actor state zero, equal actor/target/unit tiles, and battle state `0x11`.
Preview, cancel, non-movement action, zero-length settlement, and duplicates must produce zero
accepted rows. Movement must remain visually continuous and unchanged.

No gameplay effect belongs in this first live gate. An authored final-tile predicate/effect consumer
is a separate layer after event cardinality and timing are proven.
