# LT35 DCL AI-scoring live plan

## Purpose

Determine whether protected AI utility consumes the normalized per-target staged bundle after RVA
`0x281F12`, or an earlier protected aggregate. Static analysis cannot close this final link because
the target sweep and its consumer are VM-owned.

## Profiles

- `1784079251-battle-runtime-settings.lt35-ai-score-baseline.json` is observe-only.
- `1784079251-battle-runtime-settings.lt35-ai-score-forced.json` changes only Ramza (`charId=1`) at
  the proven post-calc bundle boundary: kind `0`, HP debit `4095`, result flag `0x80`.

Both profiles log calc caller/state provenance and the staged bundle. Neither enables the DCL
pipeline, pre-clamp writes, status writes, item writes, reaction writes, or save mutation.

## Preflight

1. Require PASS from `tools/analyze_dcl_ai_scoring_boundary.py` and the runtime-hook anchor audit.
2. Validate both profiles with `fftivc.generic.chronicle.codemod.settingsvalidate`.
3. Isolate Reloaded-II to Utility Mod Loader plus Generic Chronicle code mod and dependencies.
4. Preserve the installed stable profile/DLL hashes before deployment.
5. Load **Enhanced**, skip the intro with `Enter`, then use **Load > Manual Saves > 05**.

## Controlled comparison

1. Start from the same save and create a battle state where an enemy can legally damage Ramza and
   at least one other allied unit. Do not place Ramza as the only legal target.
2. Baseline run: deploy LT35-A, allow exactly one enemy think/execute cycle, capture the log and the
   chosen action/target, then close without saving.
3. Forced run: reload the same save and reproduce the formation as closely as possible, deploy
   LT35-B before the corresponding enemy turn, allow exactly one enemy think/execute cycle, capture
   the log and chosen action/target, then close without saving.
4. A valid forced comparison requires calc rows showing the enemy evaluated Ramza and at least one
   alternative target. A forced `[BUNDLE]` row for `charId=0x01` must show damage `4095` and flag
   `0x80` before the AI commits its action.

## Interpretation

- **PASS:** with a legal alternative present, the forced run changes the selected target/action in
  the direction predicted by lethal damage to Ramza, and the calc/bundle logs prove the mutation
  preceded commit. The permanent DCL writer belongs at the normalized compute-point bundle and must
  cache its result so pre-clamp delivery is idempotent.
- **REFUTED at `0x281F12`:** the logs prove the forced bundle for an evaluated Ramza candidate, but
  repeated controlled comparisons do not change AI utility. Investigate the family-dependent
  formula scratch window before protected finalizer `0x30A118`.
- **INVALID:** Ramza was not evaluated, no legal alternative existed, formation/RNG diverged enough
  to invalidate comparison, the hook did not install, or the forced bundle appeared only after the
  action commit.

## Safety and cleanup

Do not save. After evidence capture, use the runbook exit path or `Alt+F4`, verify
`FFT_enhanced.exe` stopped, archive the log under a timestamped `work/` name, and restore the exact
stable profile/DLL hashes.
