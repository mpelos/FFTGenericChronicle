# DCL Fear Fire run without Reloaded instrumentation

## Observation

The Fire action was executed in the Enhanced game session opened through Steam protocol
`steam://rungameid/1004640`. The active Reloaded log directory received no new FFT log for this
session. Its newest file remained `2026-07-18 16.02.24 ~ FFT_enhanced.txt`, last written at
`13:04:38` local time, and contains only the earlier basic Attack (`abilityId=0`) against target slot
`16`.

No `abilityId=16`, new `DCL-FEAR-CONFIRM`, or AoE expanded-target row exists for the Fire action.

## Classification

This run is inconclusive for private-builder AoE authority. It may supply a visual recollection of
the forecast, but it cannot be correlated with an injected runtime row and is not promoted to
`docs/modding` or counted as live proof.

The repetition must use Reloaded-II's configured launch path, confirm a fresh runtime-harness header
and Fear hook-install row, and run the turn-owner-authority DLL before Fire is selected.

