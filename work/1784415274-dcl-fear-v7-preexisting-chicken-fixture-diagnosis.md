# DCL Fear v7 preexisting-Chicken fixture diagnosis

## Scope

This journal records why the v7 live attempt archived as
`1784415251-dcl-fear-v7-preexisting-chicken-fixture-deviated.log` cannot validate the
Fear forced-turn coordinator.

## Observations

- The restored autosave already carried Arthur's visible Chicken state before the new game
  process started.
- The first resumed ally was Leona rather than Josephine. The deployed CT fixture changed the
  snapshot and turn aliases but did not change the attack aliases that also participate in resumed
  turn ownership.
- The loaded battle also carried queued actions, including Odin, so the first apparent Arthur
  activations were charge resolutions rather than a clean controllable turn.
- The log contains no `DCL-FEAR-CHICKEN` and no `DCL-FEAR-FLEE` row.
- A same-process Fervor attempt produced a resisted status roll (`9` against resistance `9`). A
  later calculation staged Chicken with roll `15` and `packetAdd=0x04`.
- The staged `dcl-fear` duration later expired on Arthur (`master=0x04->0x00`,
  `effective=0x04->0x00`) before the forced-turn hook produced any dispatch row.

## Interpretation

This run is a fixture deviation, not a v7 coordinator result. It mixes a status that existed at
process start, a later DCL status application, stale queued actions, and inconsistent CT aliases.
The absence of dispatch rows cannot distinguish coordinator behavior from the invalid starting
state and status lifetime.

## Next valid protocol

Restore the canonical two-unit snapshot
`1784390906-fft-autoenhanced-snapshot.png` unchanged. Its live records contain no Chicken bit, and
the autonomous-control runbook already verifies that Continue resumes at Josephine's actionable
turn with Arthur as the intended target. Apply Fervor and observe Arthur's next turn within the same
game process. Require `DCL-STATUS`, `DCL-FEAR-CHICKEN`, and `DCL-FEAR-FLEE` evidence before judging
the coordinator.
