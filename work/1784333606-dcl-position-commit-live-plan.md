# DCL position-commit caller-correlation live plan

## Purpose

Separate accepted gameplay movement from setup, cursor preview, state restoration, scripted
relocation, and presentation synchronization at the shared position writer. The test is an
engine-mechanism investigation only. It implements no job, ability assignment, balance value, or
combat effect.

## Offline prerequisites

- `tools/analyze_dcl_position_commit.py` returns PASS for the executable on disk.
- Native thunk `0x27192C` and body `0xE7D721B` are byte-verified.
- First canonical write `0xE7D735A` exposes unit index in `rdi`, destination X in `r10b`, Y in
  `r9b`, level high bit in `r14b`, and the low layer/facing nibble in `r8b`.
- At that write, the original thunk-caller return address is at hook-time stack offset `+0x58`.
- Runtime profile: `work/1784333606-battle-runtime-settings.dcl-position-commit-observe.json`.
- Code-mod build and smoke tests pass.

## Safety contract

- The hook is `ExecuteFirst` and captures registers plus one stack qword into its private ring.
- It changes no coordinate, battle field, action, HP, MP, status, data table, or save.
- The captured stack qword is copied synchronously by the hook; managed polling does not infer a
  return address from a stale stack pointer.
- Expected-byte mismatch, hook-install failure, crash, or any unexpected write ends the run.
- Do not save. Close the game with `Alt+F4` after the bounded observation.

## Manual-save route

Use the runbook route requested for repeatable tests: launch via Reloaded-II, choose **Enhanced**,
press **Enter** immediately to skip the intro, choose **Load**, enter **Manual Saves**, and load the
first entry, save `05`.

## Controlled sequence

1. Let the loaded battle settle without input for several seconds. This captures setup/restoration
   calls.
2. On a controllable unit, open **Move**, move the cursor over several candidate tiles, then cancel
   without confirming. This is the preview-only negative control.
3. Open **Move** again, choose a destination at least three traversable tiles away, confirm, and let
   the complete walking animation finish. This is the accepted-movement positive control.
4. If the turn remains active, change only facing or use **Wait**. This separates facing-only state
   from coordinate change.
5. Close without saving and archive the fresh `battleprobe_log.txt` into timestamped `work/`.

## Required evidence

- One `[LANDMARK-HOOK dcl_position_commit]` line with RVA `0xE7D735A`,
  `battleUnitIndex=rdi`, and `captureStack=+0x58`.
- No `[LANDMARK-SKIP]`, `[LANDMARK-FAILED]`, crash, or lost-ring event during the controlled window.
- Every captured return address resolves to a direct-call return RVA from the static 24-caller set.
- Preview-only input is distinguishable from the confirmed walk by caller and/or coordinate-change
  sequence.
- The confirmed walk establishes whether the writer fires per traversed tile or only for the final
  tile.

## Decision

A caller is eligible as the DCL approach-event producer only if it occurs for accepted movement,
identifies the moving unit and destination, and is absent from preview-only activity or can be
guarded by a proven commit state. A final-tile-only caller can detect outside-to-inside reach entry
between old/new positions; a per-step caller can additionally model the exact first crossed reach
boundary. No Stop-hit delivery is implemented until this producer distinction is proven.
