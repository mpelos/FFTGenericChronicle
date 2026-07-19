# DCL Fear v7 carrier and forced-route live result

## Scope

Validate the job-free Fear delivery and forced-movement transaction with Josephine casting Fervor
on Arthur. The fixture gives Arthur 999 maximum HP and does not assign a new job ability.

## Exact artifacts

- Live log: `work/1784426539-dcl-fear-v7-deterministic-carrier-forced-flee-live.log`
- Live-log SHA-256: `58D2B7AFF48B5977CE7104A5719290A253020085EC1858376FAF8E1F36C9EF6B`
- Runtime/data manifest: `work/1784423186-dcl-unified-sentinel-v7-runtime-data-pair.json`
- Installed action-data SHA-256: `077ACA440092B212B362CCADEAB715E01100B253F961DCB069FF5C09AA89F175`
- Installed runtime-settings SHA-256: `C09817E082FB0F53B77AE371978A72E4B81B42711AEACC19914DA69562BE185E`
- Invalid zero-HP autosave preserved as
  `work/1784426243-dcl-fear-invalid-zero-hp-live-autosave.png` before restoring the 999-HP fixture.

## Live observations

1. Formula `0x38` supplied the native carrier result at outer-sweep entry:
   Fervor reached the producer with `flags=0x08`.
2. The integrated transaction exposed a duplicate DCL status contest. The post-calculation producer
   rolled `3` against resistance `9`, classified the result as resisted, and cleared
   `flags=0x08->0x00`. The later pre-clamp path rebuilt the plan, rolled `14`, and staged Chicken
   with `packetAdd=0x04` and `flags=0x08`.
3. Arthur's durable status byte contained `+0x63=0x04`, he changed visually into Chicken, and he
   remained alive at `747/999 HP`.
4. The Chicken dispatcher selected the DCL route. The coordinator recorded
   `before=6,0,2`, `selected=9,0,0`, byte-exact restoration to `6,0,2`, `routeLength=3`,
   `cursorBefore=0`, and `battleStateAfter=0x10`.
5. Arthur moved without receiving the ordinary action menu. Control later advanced to Josephine.
   The current handled epilogue therefore stages and executes the flee route but does not preserve
   the required post-move voluntary action opportunity.
6. `tools/analyze_dcl_fear_flee_live.py` passes the bounded route-staging invariants:
   `events=1`, `errors=0`. That analyzer does not assert single-roll status ownership or the
   post-move action opportunity.

## Classification

- **Proven live:** the formula-`0x38` data carrier materializes the native status result required by
  post-calculation reskinning.
- **Proven live:** the DCL coordinator chooses a distinct destination, restores the source tuple,
  resolves a nonempty route, enters state `0x10`, and produces visible forced movement.
- **Refuted live:** the integrated status path owns one resistance roll. The observed producer and
  pre-clamp paths rolled independently.
- **Refuted live for the current bridge:** route completion returns the unit to the intended
  voluntary action opportunity. Arthur's turn ended after forced movement.

## Offline correction after the run

`DclStatusPlanCache` is now one-shot. The post-calculation producer records every owned plan,
including fully resisted/fail-closed plans, and pre-clamp consumes that exact plan with `TryTake`.
A producer-logged resisted plan cannot be rolled or logged again at pre-clamp. The runtime smoke
tests cover successful one-shot consumption, resisted producer-logged consumption, duplicate-take
rejection, and battle-reset clearing.

Validation after the correction:

- C# formula/runtime smoke tests: PASS.
- Unified DCL sentinel action-data builder tests: PASS.
- Runtime/data-pair validator smoke tests: PASS.

## Next gate

Deploy the one-shot status-plan correction and repeat Fervor without a forced roll. One action must
emit one resistance roll. A resisted roll must leave Arthur human; a successful roll must apply
Chicken with the same recorded roll. The post-route continuation remains a separate mechanism gap:
the route completion path must return the same unit to a legal non-hostile action opportunity rather
than the forced-control handled epilogue ending its turn.
