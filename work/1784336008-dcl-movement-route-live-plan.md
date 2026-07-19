# DCL movement-route read-only live gate

## Purpose

Identify which statically equivalent per-tile arrival implementation ordinary player and AI
movement use, and verify that one hook event corresponds to each visible traversed tile.

This gate observes the engine route already attached to the movement actor. It does not implement a
job, reaction, stop-hit, coordinate change, route change, status change, HP/MP change, action write,
data edit, or save edit.

## Probe

Use `1784336007-battle-runtime-settings.dcl-movement-route-observe.json`. Its three raw-base
landmarks run immediately before the corresponding completed-step handler copies actor target
X/Y (`+0x8C/+0x8D`) to current X/Y (`+0x88/+0x89`). Each record also captures current/target layer,
route cursor `+0xA4`, route length `+0xA8`, route bytes, current staged route byte `+0x128`, actor
pointer, and the actor's linked-record pointer bytes at `+0x148`.

## Controlled observations

1. Load the usual Manual Save 05 fixture.
2. Move one player unit along a visibly multi-tile route with at least one turn or height/layer
   change. Record start, every visible tile, and destination.
3. End the turn and allow one AI unit to complete a visibly multi-tile route. Record the same.
4. Stop after those two movements; no attack or combat rewrite is needed.

## Pass conditions

- All three hooks install or unused variants remain silent; no expected-byte failure or crash.
- Exactly one active implementation emits the ordinary route, or the division between implementations
  is deterministic and explainable by movement state.
- For player and AI, each event's old current X/Y and target X/Y form the observed next-tile
  transition.
- Cursor is monotonic by one across arrivals and never exceeds length.
- Event count equals visible traversed-tile count for each movement.
- Final event destination agrees with the already proven canonical final-position commit.

Any duplicate, missing tile, cursor discontinuity, unexpected implementation split, or actor-read
failure falsifies the candidate boundary and returns the investigation to offline mapping.
