# LT10 autonomous live-test plan

## Purpose

LT10 closes three DCL outcome-authority barriers using the already-built profile
`work/battle-runtime-settings.lt10-counterpath-probe.json`:

- LT10-A: identify whether `0x30C798` is the counter/reaction result path or a shared commit path;
- LT10-B: observe the staged status encoding before enabling any status or Move writes;
- LT10-C: determine whether the pre-clamp miss-presentation writes survive until draw time.

## Offline gate

- The installed executable static scan passes.
- The code mod builds in Release with zero warnings and zero errors.
- C# smoke tests pass, including the LT10 validator cases.
- The shipped LT10 profile validates with its write modes disabled.
- The full `codemod/run-offline-checks.ps1 -SkipGitDiffCheck` gate passes.
- The first gate run exposed a stale generated formula-context report. The generator already reads
  `FormulaRuntimeContextBuilder.cs`; regenerating the report adds the newly cataloged
  `boostJp`, `weaponOptionsAbilityId`, and `weaponRange` variables and restores the passing gate.

## Fixed startup route

1. Launch through Reloaded-II.
2. Choose Enhanced.
3. Press `Enter` during the opening movie.
4. Choose **Load > Manual Saves > 05** (first manual save).
5. Do not save over the file.

## LT10-A hypothesis and evidence

Hypothesis: function entry `0x30C798` is the distinct staging path used by counters that bypass
calc-entry `0x309A44`.

Evidence to capture:

- installation must not log `[DCL-CTRPATH-SKIP]`;
- provoke at least one normal attack and at least one Counter reaction;
- correlate each `[DCL-CTRPATH]` with target index, `e8/e9`, HP, and nearby `[DCL]` lines;
- a counter should still produce the known `reason=no-calc-entry` pre-clamp observable;
- if `[DCL-CTRPATH]` fires for ordinary attacks, classify `0x30C798` as shared rather than
  counter-specific instead of forcing the original hypothesis.

## LT10-B hypothesis and evidence

Hypothesis: the status staged for the current hit is represented by the combination of ailment
`+0x1A8`, apply mask `+0x1D0`, kind `+0x1C0`, and result flag `+0x1E5` visible in the pre-clamp
window.

First pass is strictly observe-only:

- keep `DclStatusSuppressEnabled=false`;
- keep `DclStatusForceId=-1` and raw force value disabled;
- keep StatusPoke and MovePoke targets disabled;
- compare `[DCL-STATUS]` from a plain hit with a hit that visibly applies a status;
- also record the staged bytes for a DCL-forced miss to see whether the native VM still staged a
  proc behind the force-connect path.

Only after the encoding is differentiated may later iterations enable suppress, force, durable
status add/remove, or the Move poke one at a time.

## LT10-C hypothesis and evidence

Hypothesis A: the pre-clamp write survives and a forced miss renders a Miss/evade glyph with no
damage number. Hypothesis B: the VM rewrites the presentation fields later and the screen still
shows `0`/a number.

Evidence to capture:

- visible result of multiple DCL-forced misses;
- corresponding `[DCL]` lines including `pres=d8:OLD->NEW kind:OLD->NEW`;
- HP and MP remain unchanged on the miss;
- note glyph/animation correctness, not just the absence of damage;
- if B occurs, do not iterate blindly: use the recorded fields to move the write to the planned
  later finalizer slice.

## Stop conditions

- Stop immediately on an AOB guard skip, crash, save prompt, unexpected persistent-state write,
  or mismatch between visible HP/MP and the log.
- Capture the log before closing the game.
- Close through **Options > Exit Game > Yes**, then verify `FFT_enhanced.exe` stopped.

## Result handling

Create a new timestamped LT10 result file containing the exact battle actions, screen observations,
log excerpts, classification changes, and next hypotheses. Promote only live-proven facts into the
owning `docs/modding/` documents.
