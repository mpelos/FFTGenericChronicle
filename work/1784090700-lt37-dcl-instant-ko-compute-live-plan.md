# LT37 — Instant KO through the AI-facing compute-point writer

## Purpose

This A/B slice validates the remaining composition boundary, not the 3d6 arithmetic already covered
offline: Death must be scored as expected lethal debit during AI evaluation, rolled exactly once for
confirmed execution, cached at the post-calculation boundary, and delivered through native HP/KO
apply without a second contest.

## Hard safety gate

Do not deploy either profile until all of the following are true:

- the generated action-data override changes ability `30` only as requested and replaces its native
  Dead rider with the harmless formula placeholder;
- the generated NXD and the matching runtime profile are recorded by SHA-256;
- the selected fixture exposes Death on a controllable unit;
- `FFT_enhanced.exe` is stopped before any autosave restore or deployed-file replacement;
- the current executable passes the complete runtime-anchor audit.

The runtime rule must never run against native, non-neutralized Death. If the fixture lacks Death,
author a deterministic test loadout offline; do not substitute a different multi-status ability.

## Profiles

- A, forced resistance: `work/1784090700-battle-runtime-settings.lt37a-dcl-instant-ko-compute-resist.json`
- B, forced KO: `work/1784090700-battle-runtime-settings.lt37b-dcl-instant-ko-compute-ko.json`

The files differ only in their note and `DclStatusForcedRoll` (`3` versus `18`). Both keep the LT36
compute-point writer, result-flag ownership, calc provenance, and staged-bundle probes enabled.

## Fast game route

Establish the Death fixture once from **Load > Manual Saves > 05**, snapshot its Enhanced autosave,
then repeat both halves through the runbook fast path:

1. restore the exact fixture while the game is stopped;
2. launch through Reloaded-II and click **Enhanced > Start Game**;
3. wait about 4.2 seconds, press `Enter`, wait about 1.6 seconds, then click **Continue** directly;
4. wait about 22 seconds before inspecting the battle.

## A — forced resistance

Cast Death on a living, nonimmune target. Required evidence:

- any AI-state evaluations for Death log `[DCL-KO-COMPUTE] phase=ai-expected` with the exact
  resistance-derived `successPermille` and expected debit;
- confirmed execution logs exactly one
  `[DCL-KO-COMPUTE] phase=execution ... roll=3 outcome=resisted debit=0`;
- its `[DCL-COMPUTE-POINT]` execution row has `cached=1` and final HP debit `0`;
- pre-clamp consumes the compute-point result (`computePoint=1` in the normal DCL delivery log);
- no legacy `[DCL-KO]` row and no second KO roll occur for the same action;
- the target remains alive, takes no damage, retains coherent CT/turn ownership, and can act.

## B — forced KO

Restore the identical fixture, deploy only the B settings file, and target a living, nonimmune unit
whose resistance is below `18`. Required evidence:

- execution logs exactly one
  `[DCL-KO-COMPUTE] phase=execution ... roll=18 outcome=engine-owned-ko`;
- the final debit equals current HP plus any same-result HP credit;
- the compute-point row is cached once and the pre-clamp row reuses it without a legacy KO row;
- the native result is coherent: HP `0`, prone/dead presentation, no normal CT turn, vanilla corpse
  targeting/immunity behavior, and no standing zero-HP or partial-Dead state.

## Evidence and cleanup

Retain bounded runtime logs, before/after screenshots, deployed DLL/settings/NXD hashes, target HP,
Faith, immunity, staged credit/debit, resistance, roll, and post-action turn state in new
timestamp-prefixed `work/` artifacts. Restore the stable installed DLL, settings, data NXD, and
autosave after the test, verify their hashes, and close the game.
