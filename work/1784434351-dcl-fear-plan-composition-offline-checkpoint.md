# DCL Fear plan-composition offline checkpoint

## Scope

Replace the refuted route-only Fear continuation with a native plan-time transaction that preserves
forced flight while allowing one legal non-hostile action. This checkpoint changes no job, job data,
or job specification.

## Prior falsifier

The live route coordinator selected and executed a valid flee route, but returned through the native
Chicken handled result. The only outer caller treats every nonzero forced-control result as a complete
plan and skips ordinary planning. Arthur therefore moved and ended his turn with standard Chicken
behavior.

## Static findings

1. The forced-control resolver's only executable caller is trace RVA `0x1098B8B5`. Return `-1` is
   failure, return `0` enters ordinary planning, and any other return skips directly to the handled
   zero-return epilogue.
2. Effective Immobilize is byte `unit+0x65`, mask `0x08`. Reachability builder RVA `0x31FA9D`
   branches on that exact bit; its set path at `0x31FB97` bypasses movement-range expansion and
   records only the active unit's current tile.
3. Ordinary action planner `0x38D658` invokes the same selector `0x38E11C`, chooses a winner through
   `0x321390`, and publishes the composed X/Y/layer record at RVA `0x1872EAC`.
4. The legal action must be composed before returning handled. Re-entering the planner after route
   animation is unnecessary if the ordinary planner is synchronously constrained to the selected
   flee tile.

## Implemented transaction

`DclFearNative` now:

1. snapshots the original coordinate tuple and effective Chicken/Immobilize bytes;
2. runs the proven Chicken selector-to-winning-planner prefix and captures the flee tile;
3. restores the selector's coordinate mutation;
4. lends the flee tile to the unit, preserving the low seven bits of `unit+0x51`;
5. temporarily clears effective Chicken and sets effective Immobilize;
6. calls ordinary planner `0x38D658` while managed Fear target authorization remains active;
7. requires the published composed-plan tile to equal the selected flee tile;
8. restores the original coordinate tuple and both effective-status bytes on success and every
   fallback path;
9. returns handled only for a valid composed plan; otherwise it falls back to untouched Chicken.

The unmanaged audit block records before/selected/planned/intermediate-restored/final-restored tiles,
planner result, both original effective bytes, and exact restoration success. Managed Fear ownership
recognizes the synchronous `PlanningLegalAction` state even while effective Chicken is temporarily
hidden, so opposing-target candidate invalidation remains active.

## Offline validation

- Formula/runtime C# smoke tests: PASS.
- `tools/analyze_dcl_fear_plan_composition.py`: PASS.
- Existing flee-route static anchors: PASS.
- Fear target and pre-confirm boundaries: PASS.
- Timeless-doc check and its self-tests: PASS.
- Static report: `work/1784434178-dcl-fear-plan-composition-analysis.md`.

## Remaining live gate

Use the corrected Josephine Fervor fixture. One cast must produce exactly one DCL status contest. On
success, the Fear log must report `PlanComposed`, equal selected/planned tiles, identical
before/final-restored tiles, and `statusRestored=1`. Arthur must visibly flee and then execute at most
one self/ally/item/defensive action or Wait. He must never target an opponent. A planner failure or
tile mismatch may fall back to native Chicken but does not satisfy complete Fear semantics.
