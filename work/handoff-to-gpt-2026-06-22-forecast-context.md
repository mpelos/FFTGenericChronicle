# Handoff to GPT - 2026-06-22 - Forecast/Action Context Investigation

Repo: `D:\Projects\FFTGenericChronicle`

Purpose of this handoff: continue the live RE investigation on another machine / another GPT
instance without losing the current reasoning. The immediate goal is not balance design. The goal is
to find a robust primary source for battle action context in FFT Ivalice Chronicles code modding:

- attacker / caster
- target(s)
- action identity
- ideally forecast or final damage metadata

CT resolution works as a fallback, but it is too fragile to be the primary long-term solution for a
full battle mechanics redesign.

## Fresh-Session Setup Checklist

If you are starting from a fresh GPT session, do this first:

1. Get the latest repo state:

```powershell
cd D:\Projects\FFTGenericChronicle
git pull --rebase origin main
git status --short --branch
```

2. Confirm you have the forecast/action-context tooling:

```powershell
Test-Path tools\scan_live_forecast_values.py
Test-Path tools\scan_live_unit_pointers.py
Test-Path tools\analyze_pointer_scan_triplet.py
```

3. Confirm the latest handoff/tooling commit is present:

```text
8923342 Add live action-context probe tooling and handoff
```

4. Ignore unrelated untracked sprite files if they exist. They were intentionally not part of this
   investigation:

```text
docs/modding/08-sprite-asset-pipeline.md
work/handoff-to-gpt-2026-06-21-part2.md
work/sprite-extract/
```

5. Paths in this handoff are from the original Windows machine. On another machine, verify the game
   and Reloaded paths before running live commands:

```text
D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt
C:\Reloaded-II\
```

Most scripts have `--log`, `--pid`, `--process-name`, or PowerShell parameters if paths differ.

## Read This First

Read in this order:

1. `work/handoff-to-gpt-2026-06-21.md`
   - Older global TL;DR after the FIX PASS work.
   - Covers engine-owned death, MinHpFloor, CT discovery, and open RE questions.

2. `docs/modding/07-live-findings.md`
   - Canonical live-test record through the CT/death tests.
   - Important: death by raw HP/status writes is not viable; engine owns death.

3. `docs/modding/10-ct-attacker-resolution-live-results.md`
   - CT attacker resolution live proof.
   - Important: CT works for the controlled immediate-attack case, but is now considered fallback,
     not the desired primary source.

4. `docs/modding/09-hook-register-context-probe.md`
   - Current RE path and tooling.
   - Describes hook register event logs, pointer scans, and the new external scanner flow.

5. This file.
   - Current state of the forecast/action-context investigation and next live protocol.

Useful code files:

- `codemod/fftivc.generic.chronicle.codemod/Mod.cs`
- `codemod/fftivc.generic.chronicle.codemod/RuntimeSettingsValidator.cs`
- `work/battle-runtime-settings.hook-register-probe.json`
- `tools/scan_live_unit_pointers.py`
- `tools/compare_live_unit_pointer_scans.py`
- `tools/analyze_pointer_scan_triplet.py`
- `tools/scan_live_forecast_values.py`
- `tools/analyze_battleprobe_log.py`
- `tools/watch_live_mapping.py`

Useful evidence files from the latest live run:

- `work/live_unit_pointer_scan.baseline.md`
- `work/live_unit_pointer_scan.baseline.json`
- `work/live_unit_pointer_scan.pending-braver.md`
- `work/live_unit_pointer_scan.pending-braver.json`
- `work/live_unit_pointer_scan.post-braver.md`
- `work/live_unit_pointer_scan.post-braver.json`
- `work/live_unit_pointer_scan_diff.baseline-vs-pending-braver.md`
- `work/live_unit_pointer_scan_diff.pending-vs-post-braver.md`
- `work/live_unit_pointer_scan_triplet.braver.md`

If time is short, read only this file plus `docs/modding/09-hook-register-context-probe.md`; then
skim `work/live_unit_pointer_scan_triplet.braver.md` to understand the main false positive.

## Baseline Architecture Already Proven

Do not re-litigate this unless new evidence directly contradicts it.

- The stable unit struct is around size `0x200B`.
- Important offsets:
  - `+0x00` char id
  - `+0x04` team
  - `+0x29` level
  - `+0x2B` Brave
  - `+0x2D` Faith
  - `+0x30` HP
  - `+0x32` MaxHP
  - `+0x34` MP
  - `+0x3E` PA
  - `+0x3F` MA
  - `+0x40` Speed
  - `+0x41` CT
  - `+0x61` status, bit `0x20` = KO visual/effect flag
- The runtime can observe HP/MP changes and rewrite final HP/MP.
- Same-hit clean death is not solved.
- Current working architecture is:
  - neutralize or reduce vanilla result
  - observe hit
  - compute custom formula in C#
  - write HP result with `MinHpFloor` if needed
  - let engine own death
- CT reset can identify attackers in many immediate-action cases:
  - attacker = unit whose CT recently dropped
  - target = unit whose HP changed
- CT is now considered fallback because charged actions, Wait, counters, Hamedo-like reactions, and
  timing windows make it inherently heuristic.

## What Was Implemented In This Pass

### Code mod probe additions

`Mod.cs` now has event-correlated hook register probes:

- `[HOOK-REGS]`: broad stable hook snapshots.
- `[HOOK-REGS-EVENT kind=damage|healing|mploss|mpgain|ctdrop ...]`: latest hook snapshot attached
  to observed HP/MP/CT events.
- `[HOOK-PTRSCAN-EVENT ...]`: reads readable non-unit register roots and scans their first bytes
  for exact known unit pointers.

New settings in `work/battle-runtime-settings.hook-register-probe.json`:

- `HookRegisterProbeOnHpEvent`
- `HookRegisterProbeOnMpEvent`
- `HookRegisterProbeOnCtDrop`
- `HookRegisterProbeEventMaxLogs`
- `HookRegisterProbeStackSlots`
- `HookRegisterProbePointerScanBytes`
- `HookRegisterProbePointerMaxLogs`
- `HookRegisterProbePointerMaxPointersPerRoot`

`RuntimeSettingsValidator.cs` validates/warns for these noisy read-only settings.

Important live-deploy detail:

- `prepare-live-mapping.ps1` has an `-EnableModInAppConfig` switch, but the user explicitly does
  not want the agent to use it.
- Tell the user which mods to enable in Reloaded-II instead.
- Running `prepare-live-mapping.ps1` without `-EnableModInAppConfig` is acceptable: it builds/deploys
  and archives logs, but does not enable mods.

### External scanner tooling

New / updated tools:

- `tools/scan_live_unit_pointers.py`
  - Opens `FFT_enhanced.exe` read-only.
  - Resolves unit pointers from latest `[UNIT]` lines in `battleprobe_log.txt`.
  - Scans committed readable memory for exact 64-bit unit pointer values.
  - Emits nearby groups of unit pointers.
  - Can now optionally include raw hits in JSON with `--json-include-hits`.

- `tools/compare_live_unit_pointer_scans.py`
  - Pairwise diff for two pointer scan JSON files.

- `tools/analyze_pointer_scan_triplet.py`
  - Compares baseline / pending / post scans.
  - Important because pairwise diffs are noisy: turn-order arrays rotate and look like "new"
    pointer groups.
  - Reports:
    - exact pending-only groups
    - pending groups that survived post
    - address-stable name changes

- `tools/scan_live_forecast_values.py`
  - New tool for the next phase.
  - Searches live memory for clusters containing unit pointers plus forecast values such as damage,
    hit percent, modifier percent, HP/MP, ratios, and text (`Braver`).
  - This is meant for the action preview screen, before confirmation.

Scanner dependency to remember:

- `--unit-id Name=0xID` resolves pointers from the latest `[UNIT ptr=...] id=...` lines in
  `battleprobe_log.txt`.
- If a scanner warns that it cannot resolve a unit id, the runtime log is stale or missing unit
  observations. Make sure the game was launched through Reloaded-II with the code mod enabled and
  the observe-only profile deployed, then let the battle sit long enough for `[UNIT]` lines to log.
- If necessary, pass explicit pointers with `--unit Name=0xPTR`, copied from the latest `[UNIT]`
  lines in the log.

## Latest Live Test: Cloud Braver -> Beowulf

User performed:

1. Baseline before Cloud confirmed Braver.
2. Cloud used Limit Braver on Beowulf.
3. Game stopped at Beowulf's menu before confirming Wait, while Braver was pending.
4. Scan ran.
5. User accidentally confirmed Wait immediately after scan finished.
6. Post-resolution scan was captured.

Unit ids / pointers observed in this run:

- Cloud: id `0x32`, ptr `0x1418562E0`
- Beowulf: id `0x1F`, ptr `0x1418564E0`
- Agrias: id `0x1E`, ptr `0x1418560E0`
- Ramza: id `0x01`, ptr `0x141855CE0`
- Ninja: id `0x80`, ptr `0x141855EE0`

External pointer scan counts:

- Baseline: `105` groups
- Pending Braver: `139` groups
- Post Braver: `142` groups

Important result:

- `0x15E417BC8` looked promising at first because pending had `Beowulf, Cloud` close together.
- But the same `Beowulf, Cloud` shape survived after Braver resolved.
- The triplet report shows it as an address-stable name change:
  - baseline: `Ramza, Cloud`
  - pending: `Beowulf, Cloud`
  - post: `Beowulf, Cloud`
- Conclusion: `0x15E417BC8` is very likely turn-order / current-turn / cache state, not the Braver
  pending action object.

Also important:

- No exact pending-only pointer group containing both `Cloud` and `Beowulf` was found.
- Pending-only groups mostly contained combinations like `Cloud + Ninja`, `Cloud + Ramza`, etc.,
  inside broad repeated structures around `0x15E800000`. These look more like rotating arrays /
  cache snapshots than a clean pending-action object.

Hook event evidence:

- Braver damage event at HP write time saw Beowulf as target.
- Registers near the stable hook at damage time mostly pointed at Beowulf/target or unrelated
  readable roots, not Cloud/caster.
- CT drop still identifies Cloud around scheduling, but this is exactly the fallback we want to
  avoid depending on.

## Key Reasoning Shift

The user pointed out a crucial fact from the action preview screen:

- At Braver selection/target preview, UI already displays:
  - action: Braver
  - actor: Cloud
  - target: Beowulf
  - damage: `153`
  - chance: `100%`
  - modifier/extra displayed as `125%`

Therefore, the preview window is likely richer than the pending-action window.

The game must have at least three contexts:

1. Preview / forecast context
   - current actor
   - selected action
   - hovered/selected target
   - displayed damage
   - displayed hit chance / modifiers
   - target panel/unit information

2. Confirmed pending action
   - caster
   - action id
   - target unit or target tile
   - scheduled resolution / charge timing
   - may or may not store forecast damage

3. Damage resolution context
   - final target
   - final damage
   - final hit/avoid result
   - may or may not still have caster in nearby registers at the stable HP hook

Do not assume that confirmed pending actions store damage. The preview certainly computes a display
forecast, but after confirmation the engine may store only intent and recalculate later.

Also do not assume the random roll has happened at preview. It probably has not, because consuming
RNG before the player confirms/cancels would be odd. But that is still an assumption. Treat preview
chance as forecast, not proof of final RNG timing.

## Current Objective

Find a primary action context source better than CT.

The ideal target is one of:

1. A preview/action builder object containing actor + target + action + forecast result.
2. A confirmed pending-action object containing actor + target/tile + action + schedule.
3. A pre-damage execution context that exists shortly before HP write and contains actor + target +
   action, even if damage is recomputed there.

The immediate next investigation should search the preview screen first.

Why preview first:

- It definitely has actor/action/target/damage/chance information somewhere.
- It is player-paused and easy to hold stable.
- It may expose the action builder or forecast object before data collapses into a pending queue.

## Next Live Test Protocol

Important user preference:

- Do not edit Reloaded-II AppConfig JSON to enable mods.
- Tell the user which mods to enable instead.

Mods for this test:

- `fftivc.utility.modloader`
- `fftivc.generic.chronicle.codemod`
- Data mod can stay off.

Before live test on a new machine, deploy the observe-only profile:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\prepare-live-mapping.ps1 -RuntimeSettings work\battle-runtime-settings.hook-register-probe.json
```

This should not enable mods in AppConfig. It deploys settings/DLL and archives the old log.

### Step A - forecast baseline

Ask user to load before Cloud uses Braver. When Cloud's menu is open before selecting Braver, user
says:

```text
forecast baseline pronto
```

Run a forecast scan. If using the Beowulf target test and expected preview values are unchanged:

```powershell
python tools\scan_live_forecast_values.py `
  --unit-id Cloud=0x32 `
  --unit-id Beowulf=0x1F `
  --unit-id Agrias=0x1E `
  --unit-id Ramza=0x01 `
  --unit-id Ninja=0x80 `
  --value damage=153 `
  --value hit=100 `
  --value mod=125 `
  --value cloudHp=428 `
  --value cloudMp=89 `
  --value targetHp=314 `
  --value targetMp=180 `
  --ratio modRatio=1.25 `
  --text Braver `
  --near-bytes 0x300 `
  --max-groups 220 `
  --output work\live_forecast_scan.baseline-beowulf.md `
  --json-output work\live_forecast_scan.baseline-beowulf.json
```

If the UI values differ, update `damage`, `hit`, `mod`, HP/MP values accordingly.

If this command prints warnings about missing unit ids, do not continue the test yet. Fix pointer
resolution first by checking the fresh log:

```powershell
Select-String -Path "D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt" -Pattern "\[UNIT ptr=.*id=0x32|\[UNIT ptr=.*id=0x1F" | Select-Object -Last 10
```

Then either wait for the runtime to log units or rerun the scan with explicit `--unit Cloud=0x...`
and `--unit Beowulf=0x...`.

### Step B - preview

User selects:

```text
Limit -> Braver -> Beowulf
```

But does not confirm. User stops exactly on the preview screen with target highlighted and says:

```text
preview Beowulf 153 100 125
```

Run the same command but output:

```text
work\live_forecast_scan.preview-beowulf.*
```

The important condition: do not press Confirm yet. This is the strongest window.

This is the highest-value capture. If the user accidentally confirms before the scan, do not treat
the pending scan as a replacement; ask for another preview attempt.

### Step C - pending after confirmation

Tell user "pode confirmar".

User confirms Braver. When the game reaches Beowulf's menu, user must not confirm Wait. User says:

```text
pendente Beowulf
```

Run the same forecast scan again, output:

```text
work\live_forecast_scan.pending-beowulf.*
```

Also optionally run the pointer-only scan:

```powershell
python tools\scan_live_unit_pointers.py `
  --unit-id Cloud=0x32 `
  --unit-id Beowulf=0x1F `
  --unit-id Agrias=0x1E `
  --unit-id Ramza=0x01 `
  --unit-id Ninja=0x80 `
  --near-bytes 0x400 `
  --max-groups 180 `
  --json-include-hits `
  --output work\live_unit_pointer_scan.pending-beowulf-2.md `
  --json-output work\live_unit_pointer_scan.pending-beowulf-2.json
```

### Step D - post resolution

Tell user "pode dar Wait".

After Braver resolves and the game is controllable again, user says:

```text
pos Braver
```

Run the forecast scan one more time:

```text
work\live_forecast_scan.post-beowulf.*
```

Then compare:

- baseline vs preview
- preview vs pending
- pending vs post

There is not yet a dedicated triplet analyzer for forecast scans, but the JSON has groups and can be
inspected directly. Add one if useful, following `tools/analyze_pointer_scan_triplet.py`.

### Step E - target-swap preview, if Beowulf preview has candidates

If the Beowulf preview scan finds promising clusters, repeat only the baseline/preview pieces with
Cloud Braver targeting Agrias instead of Beowulf:

```text
forecast baseline agrias pronto
preview Agrias <damage> <hit> <mod>
```

Why: this distinguishes real target context from "next unit in timeline" / current-turn cache. A
true forecast/action object should swap Beowulf references/values for Agrias references/values when
the target changes while Cloud and Braver stay constant.

## Expected Outcomes

Strong success:

- Preview scan finds a small cluster containing:
  - `unit:Cloud:ptr64`
  - `unit:Beowulf:ptr64`
  - `value:damage:...` for `153`
  - maybe `text:Braver`
  - maybe chance/mod values
- Cluster is absent in baseline.
- This would likely be the forecast/action builder object or a UI forecast model.

Very useful:

- Preview has actor + target + action/damage, but pending loses damage.
- This means forecast data is UI/builder-time only, while pending action stores intent.
- Next step would be to hook/capture at confirm time and store context in our own runtime tracker.

Also useful:

- Pending has actor + target + action but no damage.
- This is still enough for the code mod if we can identify action identity and target reliably.
- Damage can be computed by our own formula at resolution.

Negative but informative:

- No pointer clusters in preview, but values/text cluster exists.
- Then preview UI may use unit ids, actor indices, target tile coordinates, or copied UI models rather
  than raw unit pointers.
- Next step: scan for small ids (`0x32`, `0x1F`), target coordinates, action ids, and memory diffs.

Harder path:

- Preview data is only in UI objects with no clean bridge to runtime action data.
- Then continue with pending-action queue search and/or pre-damage execution hook.

## Important Hypotheses

Hypothesis A: preview forecast object exists and is easiest to find.

- Most likely near UI/battle controller memory.
- It may contain raw pointers, copied ids, or both.
- It should change when target changes from Beowulf to Agrias.

Hypothesis B: pending action object stores intent, not forecast damage.

- If true, `153` will appear in preview but not pending.
- This is fine. We only need caster/action/target; our code mod can compute damage.

Hypothesis C: final damage may be recomputed at resolution.

- If target stats/status/position change between preview and resolution, final may differ.
- The mod architecture should not depend on stored vanilla forecast damage except as a signal.

Hypothesis D: the known `0x15E417BC8` region is not the action object.

- It appears to be turn-order/current-turn/cache state.
- Do not chase it as the primary pending Braver context unless new target-change tests contradict
  this.

## How To Interpret Forecast Scan Output

Start with `work\live_forecast_scan.preview-*.md`:

- Check `Pattern Hits`.
  - If `unit:Cloud:ptr64` or target unit hits are zero, the scan did not resolve pointers or the UI
    forecast object does not store raw unit pointers.
  - If `value:damage:*` hits are huge, prefer clusters that also contain unit or text hits.
- Check `Candidate Clusters`.
  - Best candidate: a small span containing a Cloud unit hit, target unit hit, and damage/hit/mod or
    `Braver` text.
  - Medium candidate: unit + damage only, or target + damage + text.
  - Weak candidate: only common numeric values like `100`/`125`; these are very noisy.
- Compare baseline vs preview manually first.
  - Anything already present in baseline is probably permanent UI/cache state.
  - Anything that appears only in preview is worth deeper probing.
- Compare preview vs pending.
  - If `153` disappears but Cloud/target/action remains, pending likely stores intent without
    forecast damage.
  - If the same cluster remains pending, the game may store forecast damage in the pending action.
- Compare target swap.
  - A real target object should change when target changes from Beowulf to Agrias.
  - A timeline/current-turn cache may keep Beowulf even when Agrias is the preview target.

If the scan finds only numbers but no unit pointers, extend the search in the next pass:

- actor/target char ids as bytes/words: Cloud `0x32`, Beowulf `0x1F`, Agrias `0x1E`
- target panel HP/MP values
- action name text: `Braver`
- possible action id, if discovered later
- tile coordinates / facing / height, if the UI reveals them

If there are too many false positives, reduce the search:

- remove `--include-byte-values` unless specifically hunting ids
- increase `--min-distinct-kinds` to `3`
- focus on `damage`, `Braver`, and unit pointers first
- use a smaller `--near-bytes`, e.g. `0x100`

## Current Warnings

- Keep CT fallback, but do not build the whole redesign on CT.
- Do not rely on `Wait` causing CT reset; Wait can leave CT non-zero.
- Counters/reactions and charged skills can break naive CT resolution.
- The stable HP hook is too late for caster context in delayed actions.
- Pointer-only search may miss objects that store actor index, target index, target tile, or copied
  forecast structs instead of raw unit pointers.

## Failure Modes And Recovery

- Game not launched through Reloaded-II:
  - no fresh runtime log or no code mod module; ask the user to launch through Reloaded-II with the
    code mod enabled.
- Scanner cannot resolve unit ids:
  - inspect latest `[UNIT]` lines; pass explicit `--unit Name=0xPTR` if needed.
- User confirms preview too early:
  - pending scan is still useful, but repeat preview; preview is the main target.
- The UI damage value changes because equipment/data mod/profile differs:
  - use the user's visible numbers in `--value damage=... --value hit=... --value mod=...`.
- The data mod is off:
  - that is acceptable for this observe-only investigation. The code mod is the important one.
- The profile is too noisy:
  - keep the capture short, then close the game. The probe profile is intentionally not for normal
    play.

## Verification Commands

Before committing or handing back after edits:

```powershell
python -m py_compile tools\scan_live_unit_pointers.py tools\compare_live_unit_pointer_scans.py tools\analyze_pointer_scan_triplet.py tools\scan_live_forecast_values.py
python tools\test_runtime_tooling.py
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj -c Release
```

Full gate, if time permits:

```powershell
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
```
