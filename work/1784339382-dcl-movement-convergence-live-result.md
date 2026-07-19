# DCL movement convergence live result

## Result

The active ordinary-walking path is the native movement updater. Its idle-only convergence boundary
at `0x1FE793` provides the complete existing engine route needed by an Approach trigger.

The dual read-only capture installed both native `0x1FE793` and trace-equivalent `0xD575143`
boundaries. It recorded five complete native routes across four AI actors and Ramza:

| Actor class | Route length | Dispatch cursors | Terminal confirmation |
| --- | ---: | --- | --- |
| AI | 6 | `1..6` | current = target = final tile |
| AI | 3 | `1..3` | current = target = final tile |
| AI | 4 | `1..4` | current = target = final tile |
| AI | 4 | `1..4` | current = target = final tile |
| Player | 6 | `1..6` | current = target = final tile |

All 23 dispatched edges are cardinal. For every route, the next event's current tile equals the
previous event's target tile. Each route is followed by a zero-length event that retains the final
cursor and has both current and target coordinates equal to the last dispatched target.

The trace-equivalent boundary emitted only zero-length idle observations in the dual capture; it did
not own any of these ordinary player or AI routes. The preceding trace-only capture emitted no hits,
so it cannot stand alone as the ordinary-walking gate.

## Probe semantics

The Landmark hook copies registers synchronously but reads actor memory when the managed poll drains
the ring. At `0x1FE793` the game is idle and has not consumed the next byte yet; by the time actor
memory is formatted, the immediate dispatch has advanced the cursor and staged the next edge. The
correct logged invariant is therefore `cursor=1..length`, not an initial `cursor=0` row.

An implementation must copy the current tile, route byte/cursor, actor pointer, and linked unit
identity synchronously at the native hook. A managed poll of live actor memory is evidence tooling,
not a safe implementation boundary.

## Evidence

- Raw dual log: `1784338832-dcl-movement-convergence-dual-live.log`
- Strict complete-route analysis: `1784338832-dcl-movement-convergence-dual-live-analysis.md`
- Independent route-shape analysis: `1784338832-dcl-movement-route-dual-live-analysis.md`
- Trace-only zero-hit checkpoint: `1784338263-dcl-movement-convergence-zero-hit-checkpoint.md`
- Pre-live restoration manifest: `1784338458-dcl-movement-convergence-dual-pre-live-backup-manifest.md`

The installed DLL, PDB, and runtime settings were restored to the exact hashes recorded in the
manifest after capture.

## Next mechanism step

Build a fail-closed one-shot Approach producer at `0x1FE793`. It must detect an outside-to-inside
reach transition from the current tile and the next existing route edge, bind exact mover/reactor
identity, suppress duplicate delivery for the same edge, and hand a basic-weapon order to the
already proven synthetic-Reaction transaction. Job selection and balance policy remain out of scope.
