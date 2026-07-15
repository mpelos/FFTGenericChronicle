# Fast Continue battle-fixture validation

## Purpose

Remove the long and failure-prone manual-save navigation from repeated live tests.

## Fixture construction

- Loaded **Manual Saves > 05** once.
- Started a random Mandalia Plain encounter.
- Deployed only Josephine, the Black Mage used by LT37.
- Waited through the initial enemy turns until Josephine's actionable command menu was visible.
- Closed FFT Enhanced with `Alt+F4` and verified that the game process stopped.
- Snapshotted the resulting autosave as
  `work/1784092904-fft-autoenhanced-snapshot.png`.
- Snapshot SHA-256:
  `1CB4ACEB69388185F4EC9E4BB3A47D052F0CC31ED929713A962C34EEE6951AF8`.

## Validation

With Reloaded-II kept open, the atomic fast-load sequence was executed:

1. Launch Application.
2. Enhanced Start Game at `(360, 470)`.
3. Wait 4.2 seconds and press `Enter`.
4. Wait 1.6 seconds and click Continue at `(640, 578)`.
5. Wait 22 seconds before inspection.

The game returned directly to Josephine's actionable turn with the same enemies and battlefield
state. Total time from the Enhanced/Classic selector to the validated battle state was 28.12
seconds. No Load Game screen, save tab, save row, formation screen, or preliminary enemy turn had
to be controlled during the repeated run.

## Operational conclusion

Manual Save 05 is a fixture seed, not the repeated-test entry point. Repeated A/B tests restore the
named autosave snapshot while the game is stopped and enter through Continue using one uninterrupted
input burst.
