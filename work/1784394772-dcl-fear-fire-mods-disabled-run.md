# DCL Fear Fire live run — mods disabled

## Observation

The player cast Fire during the Reloaded session logged at:

- `C:\Users\mmpel\AppData\Roaming\Reloaded-Mod-Loader-II\Logs\2026-07-18 17.01.08 ~ FFT_enhanced.txt`

The session started at log time `14:01:08` and ended at `14:06:49`.

## Evidence

- Reloaded reports only `fftivc.utility.modloader` plus its shared dependencies.
- There is no load block for `fftivc.generic.chronicle` or `fftivc.generic.chronicle.codemod`.
- There are no DCL pre-confirm, caster, ability, target-list, or Fear decision rows.
- `C:\Reloaded-II\Apps\fft_enhanced.exe\AppConfig.json` contains only `fftivc.utility.modloader` in `EnabledMods`.
- The same app config still lists both Generic Chronicle mods in `SortedMods`, so they are installed but disabled for this app profile.

## Conclusion

This run is inconclusive for the Fear Fire probe. The Fire execution itself cannot validate the corrected turn-owner caster authority or the AoE target expansion because the code mod was not present in the session.

## Required retry condition

Enable all three relevant app mods before launching through Reloaded:

1. `fftivc.utility.modloader`
2. `fftivc.generic.chronicle`
3. `fftivc.generic.chronicle.codemod`

Then repeat Josephine's Black Magicks > Fire with the intended adjacent target layout and preserve the resulting DCL log rows.
