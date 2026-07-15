# LT11 — selector-authored miss and selective reaction gate

## Purpose

Validate the two downstream controls required by the LT10 result:

1. a cached DCL miss reaches selector `0x205210` as a native no-damage miss (`+0x1BE=0`, kind `0x06`), so the game renders **Miss** instead of numeric `0`;
2. the same cached miss changes the four native Brave-gate reaction roll chances to zero, so Counter does not execute;
3. a cached DCL hit preserves the natural selector result and natural reaction chance.

The LT10 late presentation write is disabled because its values were overwritten downstream. The
LT10 `0x30C798` counter-path candidate is disabled because it did not fire for the observed Counter.
The status probe is disabled to keep this test focused.

## Offline gate

- `codemod/run-offline-checks.ps1 -SkipGitDiffCheck` passes.
- All four reaction call-site AOB guards must match before any selective reaction hook activates.
- Runtime settings validation reports zero errors.
- Install log contains selector `dclOutcome=ON` and all four `[DCL-REACTION-HOOK]` lines.
- Any selector/reaction hook skip or failure aborts the live test.

## Live setup

- Launch through Reloaded-II with only Utility Mod Loader and Generic Chronicle codemod enabled.
- Enhanced version; press Enter to skip the intro.
- Load → Manual Saves → first entry, Save 05.
- Enter Mandalia Plain and reproduce Rion attacking Chocobo Janus, whose Counter forecast is visible.

## Evidence required

### Miss case

- `[DCL-HIT] ... outcome=miss` for Rion → Janus.
- `[DCL-SELECTOR] ... decision=miss`, with result code changed to `0x00` and kind to `0x06`.
- On screen: **Miss**, with no numeric `0` and no damage.
- If the Counter Brave gate is evaluated, `[DCL-REACTION-GATE] ... chance=N->0 decision=miss`.
- Counter does not execute.

### Hit case

- `[DCL-HIT] ... outcome=hit` for Rion → Janus.
- No miss override for that execution.
- DCL damage applies normally.
- Counter remains able to execute according to its natural Brave roll; its own attack continues
  through the ordinary calc/DCL path.

Repeat attacks as needed until at least one miss and one hit are observed. Close the game with
Alt+F4 without saving after the evidence is captured.

## Interpretation gates

- **Pass:** clean Miss presentation and no Counter on a cached miss; natural behavior retained on a hit.
- **Selector fail:** numeric `0`, damage, missing selector log, or selector hook failure.
- **Reaction fail:** Counter executes on a cached miss, or the gate cannot correlate the defender
  before the cache TTL expires.
- **Crash/hang:** fail closed; archive the log and return to offline ABI/stack review before retrying.
