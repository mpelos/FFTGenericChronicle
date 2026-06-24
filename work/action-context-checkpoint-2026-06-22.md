# Action Context Checkpoint - 2026-06-22

This checkpoint exists so a fresh agent can continue the battle-mechanics code-mod investigation
without relying on conversation history.

Repository: `D:\Projects\FFTGenericChronicle`

Current branch when this was written: `main`

Current HEAD when this was written: `ed34dad`

Important user constraint:

- Do not edit Reloaded-II AppConfig JSON to enable/disable mods.
- Tell the user which mods to enable manually.
- For the current live probe, the user should enable only:
  - `fftivc.utility.modloader`
  - `fftivc.generic.chronicle.codemod`

## Main Objective

Build a flexible battle-mechanics code mod for Final Fantasy Tactics - The Ivalice Chronicles.

The long-term target is to support custom combat formulas that can use attacker, target, action,
equipment, armor/DR, and later richer battle context. The immediate RE goal is to find a reliable
primary source for action context:

- caster / actor;
- action identity;
- delayed / charged action timing;
- final target(s) at resolution;
- ideally a real engine pointer to the currently executing action.

CT-based attacker resolution has been proven useful for immediate actions, but it must be treated as
a fallback, not the final architecture.

## Canonical Reading Order

If starting fresh, read these files in order:

1. `work/action-context-checkpoint-2026-06-22.md`
   - This file. It summarizes the current state, open questions, and next steps.
2. `docs/modding/12-runtime-register-action-context-book.md`
   - The organized living model of hooks, registers, unit offsets, action lifecycle, damage/KO, and
     current design rules. Read this before diving into raw live-test narratives.
3. `docs/modding/11-charged-action-context-investigation.md`
   - Detailed live-test record for Braver and Cross Slash delayed-action context.
4. `docs/modding/07-live-findings.md`
   - Canonical live evidence up through earlier CT/death tests.
5. `docs/modding/05-battle-data-map.md`
   - Known unit struct offsets.
6. `docs/modding/06-code-mod-battle-runtime-architecture.md`
   - Runtime architecture and "engine owns death, code mod owns the number".
7. `docs/modding/04-re-strategy.md`
   - Reverse-engineering strategy and PSX/classic FFT function-map direction.

## Current Deployed Probe State

The latest code mod DLL has been built and deployed to:

`C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.dll`

The deployed runtime settings are:

`C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`

They were copied from:

`work/battle-runtime-settings.executing-action-pointer-probe.json`

The previous game log was archived to:

`D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt.bak-20260624-083056`

Offline checks passed after the latest probe changes:

```powershell
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj
dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj
python tools\report_runtime_profiles.py
python tools\test_runtime_profiles.py
python tools\test_runtime_tooling.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.executing-action-pointer-probe.json
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
```

The profile is observe-only:

- no HP rewrite;
- no MP rewrite;
- `MinHpFloor=0`, `CauseDeathOnZeroHp=false`;
- CT diagnostics enabled;
- HP event raw-diff probe enabled;
- hook register event probes enabled;
- hook register snapshots enabled for pending-resolve openings;
- native pre-clamp pointer scans enabled in `LogOnly` mode;
- actor probe enabled;
- pending-action candidate logging enabled.

Important log lines from the latest code:

```text
[HP-EVENT-PROBE kind=... event=N ptr=0x... prevHp=... currentHp=... delta=... appliedHpLoss=... rawForecastDamage=... lethal=... hpClamp=... action=...] diff=...
[PENDING-ACTION-CANDIDATES kind=damage event=N target=0x.../id=0x.. now=...] ...
[PENDING-ACTION-TRACK resolve-open ...]
[HOOK-REGS-EVENT kind=pendingresolve ...]
[PRECLAMP-REWRITE ... flags=0x1 ...]
[PRECLAMP-PTRSCAN event=N targetPtr=0x... id=0x.. now=...] ...
```

It prints, for registered units at each HP/MP event:

- `s61` = `unit+0x61`
- `t18D` = `unit+0x18D`
- `act` = `u16(unit+0x1A2)`
- `dmg1C4` = `u16(unit+0x1C4)`
- `chg1D8` = `unit+0x1D8`
- `f1E5` = `unit+0x1E5`
- `f1EF` = `unit+0x1EF`
- `b8` = `unit+0x1B8`
- `bb` = `unit+0x1BB`

The immediate test goal is to compare the stable-hook `pendingresolve` register snapshot with the
native pre-clamp register/stack pointer scan during a delayed AoE action. We are looking for a
shared current-action/controller object that can identify the resolving caster/action even when
several delayed actions might be pending.

## Known Live Unit Pointers

These pointers are session-specific. They were stable during the last live session but may change
after a fresh game launch.

- Ramza: `0x141855CE0`
- Ninja: `0x141855EE0`
- Agrias: `0x1418560E0`
- Cloud: `0x1418562E0`
- Beowulf: `0x1418564E0`

Known character ids:

- Ramza: `0x01`
- Ninja: `0x80`
- Agrias: `0x1E`
- Cloud: `0x32`
- Beowulf: `0x1F`

If the game is relaunched, rediscover or trust the codemod `[UNIT]` lines before using external
snapshot scripts with hard-coded pointers.

## Facts Already Proven

### 1. CT Is Useful, But Not Reliable Enough As Primary

CT reset at `unit+0x41` identifies actors for many immediate actions.

However, CT is not enough for the full combat redesign:

- Wait does not reset CT to zero.
- Delayed / charged actions resolve later, after other units have acted.
- Reactions and counters may not reset the acting unit's CT in a clean way.
- Multiple pending charged actions can exist at once.
- In the Cross Slash AoE delayed test, both final HP events resolved as `CTX resolved=none` under
  CT-only logic.

Conclusion: CT should remain a fallback and diagnostic signal, not the primary source of action
context.

### 2. Caster Pending State Lives In The Caster Unit Struct

For Cloud delayed Limit actions, the caster struct changed only after confirmation, not during
forecast.

Braver:

- action id: `257` / `0x0101`
- pending caster fields:
  - `Cloud+0x61 = 8`
  - `Cloud+0x18D = 2`
  - `Cloud+0x1A2 = 257`
  - `Cloud+0x1EF = 8`

Cross Slash:

- action id: `258` / `0x0102`
- pending caster fields:
  - `Cloud+0x61 = 8`
  - `Cloud+0x18D = 3`, later `1`
  - `Cloud+0x1A2 = 258`
  - `Cloud+0x1EF = 8`

After resolution:

- `+0x61` clears to `0`
- `+0x18D` returns to `255`
- `+0x1EF` clears to `0`
- `+0x1A2` remains the last action id

Conclusion:

- `+0x1A2` is action id / last action id.
- `+0x61`, `+0x18D`, and `+0x1EF` distinguish live pending state from historical last action.
- `+0x18D` behaves like charge/timer/phase.

### 3. Forecast Damage Lives On The Preview Target, But Is Not Runtime Target Identity

Observed field:

- `target+0x1C4` = forecast damage for the preview target.

Examples:

- Braver on Agrias: `Agrias+0x1C4 = 76`
- Braver on Beowulf: `Beowulf+0x1C4 = 153`
- Cross Slash on Ramza: `Ramza+0x1C4 = 187`
- Cross Slash centered on Agrias: `Agrias+0x1C4 = 115`

Related fields:

- `target+0x1D8`
- `target+0x1E5`

These seem associated with forecast/preview or target-side pending metadata.

Important correction from the user:

- Forecast damage should only be used if we need a forecast/preview/selection hook.
- It should not be the core runtime target model.
- For charged AoE, the selected target can be a character or tile epicenter.
- The final affected units are resolved later from skill area/range and current unit positions.
- Therefore it is expected that forecast does not list every final AoE target.

Conclusion:

- Forecast fields are useful evidence and possibly useful for UI preview parity.
- Final formula application should use the HP-write target at resolution time.

### 4. HP Write Target Is Authoritative For Final Impacted Units

At resolution, each impacted unit produces a real HP event.

Cross Slash AoE centered on Agrias:

- Agrias final HP event:
  - `322 -> 207 = 115`
- Ninja final HP event:
  - `276 -> 3 = 273`

Ninja never had visible forecast damage in the scanned unit-local forecast fields, but still took
real damage.

Conclusion:

- For formula execution, the HP event target is the authoritative target.
- The code mod does not need a complete precomputed AoE target list to apply a custom formula to
  every impacted unit.

### 5. Current Hook Is Stable But Not Semantically Ideal

The current runtime hook is the stable `battle_base_ptr` unit touchpoint:

- module offset: `module+0x226D98`
- signature context: `0F B7 41 30 66 89 42 0C`
- meaning: reads/writes unit HP-ish fields, touches unit structs often.

This hook is good for observing unit state and HP changes.

But it is not necessarily the engine's "damage routine" or "current action routine". The registers
at this hook may point to:

- the currently touched unit;
- current UI/turn state;
- nearby battle context;
- unrelated queue or stack roots.

In delayed action resolution, registers did not directly identify Cloud as caster.

Conclusion:

- The hook is sufficient for observation and formula rewriting.
- It may not be sufficient to discover the true action context without additional memory tracking or
  a better hook.

## Main Architectural Conclusion So Far

The most viable runtime model is:

```text
pending caster/action state + HP-write target = formula context
```

Specifically:

1. Detect pending charged actions from caster unit struct fields.
2. Track them while pending.
3. At each HP event, use the HP-write unit as the final target.
4. Match the HP event to the currently executing or pending action.
5. Use CT only as fallback for immediate/no-pending cases.

This is more robust than CT-only and does not depend on forecast target lists.

## Critical Open Problem

If multiple charged actions are pending at the same time, simply asking "which unit is pending?" is
not enough.

Example problem:

- 5 casters have pending actions.
- A target loses HP.
- Several pending actions may have similar timers or even the same action id.
- We must know which pending action is currently resolving.

Therefore, the final primary resolver needs one of these:

### Option A - Current Executing Action Pointer

Find the engine structure that represents the action currently resolving.

This is the cleanest solution if it exists and is readable. It should ideally contain:

- caster pointer or id;
- action id;
- target unit or target tile/epicenter;
- current effect / damage context.

This would solve:

- simultaneous pending charges;
- AoE;
- reactions/counters;
- non-CT actions;
- delayed spells and skills.

### Option B - Full Pending Action Table With Epicenter

Maintain our own pending table:

- caster;
- action id;
- timer / phase;
- selected unit or tile epicenter if found;
- maybe turn/order sequence;
- maybe effect family.

Then at HP-write time, match the HP target to a pending action by:

- which action reached execution phase;
- tile/epicenter and range/area;
- action id;
- event timing batch.

This is viable but more work because the code mod must understand targeting and AoE geometry.

### Option C - Hybrid

Use pending caster/action fields as a strong signal and continue searching for a current executing
action pointer. If the pointer is found, it becomes primary. If not, use the pending table plus CT
fallback.

This is the most practical path right now.

## What The Next Probe Must Discover

Update from 2026-06-23 first pass:

- The first pending-candidate live pass showed `Cloud s61=0` at both Cross Slash AoE HP-write
  events, so `caster+0x61` is already cleared before the current hook observes the damage.
- That pass also exposed a logger bug: the pending-candidate logger reused the normal `DUMP=0x180`
  snapshot, so fields beyond `+0x17F` printed as `-1`.
- The logger has now been fixed to read `0x200` bytes for pending-candidate logging only. The fixed
  DLL was deployed at `2026-06-23 08:22` local time, and the old log was archived as
  `work/live-captures/battleprobe_log.pending-action-probe-first-pass.20260623-082300.txt`.

The deployed DLL now logs pending-action candidates at each HP event with enough bytes to include
`+0x18D`, `+0x1A2`, `+0x1C4`, `+0x1D8`, `+0x1E5`, and `+0x1EF`. The next live Cross Slash AoE
test should answer:

Update from the pre-retest instrumentation review:

- Because live tests are slow, the probe was strengthened again before the next run.
- New setting: `LogActionStateChanges=true`.
- New log line: `[ACTION-STATE ...]`.
- It logs every change in the action/forecast signature for each registered unit:
  - `+0x61`
  - `+0x18D`
  - `+0x1A2`
  - `+0x1C4`
  - `+0x1D8`
  - `+0x1E5`
  - `+0x1EF`
  - `+0x1B8`
  - `+0x1BA`
  - `+0x1BB`
- This should show not only what exists at the HP-write event, but also when Cloud transitions from
  "pending Cross Slash" to "cleared/last action only".
- The strengthened DLL and `work/battle-runtime-settings.action-context-probe.json` were deployed at
  `2026-06-23 08:27` local time.

1. At the exact HP event for Agrias, does Cloud still appear with:
   - `s61=8`
   - `t18D=1` or another non-255 value
   - `act=258`
   - `f1EF=8`
2. At the exact HP event for Ninja, does Cloud still appear with those fields?
3. Does the engine clear Cloud's pending fields:
   - before the first HP write;
   - between AoE HP writes;
   - after all AoE HP writes?
4. Do any other pending-action-like units appear at the same time?
5. Do any registers or stack slots at HP event point to a structure that contains Cloud/action/target
   information?

Possible outcomes:

- If Cloud is visible as pending for all AoE HP writes, we can build a short-term resolver using
  live pending-state scan at HP event.
- If Cloud clears before HP writes, the code mod must track pending actions before resolution. This
  is already true for `+0x61`; the retest determines whether any higher-offset action/timer fields
  survive.
- If Cloud clears between AoE HP writes, we need to preserve the active resolving action for the
  entire damage batch.
- If multiple pending casters are visible, we need timer/epicenter/current-executing-action logic.

## Register / Memory RE Plan

The register plan is not "look at random registers". The goal is to identify whether the engine
exposes a real current action context near the hook.

### Stage 1 - HP Event Pending Visibility

Use the newly deployed `[PENDING-ACTION-CANDIDATES]` line.

Why:

- It directly answers whether the caster's pending state remains readable during HP writes.
- It does not require a new hook.
- It tells us whether a pending-state scan can be a short-term primary.

### Stage 2 - Compare Register Roots Across States

For these controlled states:

- baseline before selecting a skill;
- forecast;
- confirmed before Wait;
- pending on intervening turns;
- HP resolution;
- post-resolution.

Compare register roots and stack slots:

- `rbx`
- `rdx`
- `rsi`
- `r8`
- `r9`
- stack slots logged by `[HOOK-REGS-EVENT]`

Why:

- Stable roots that appear only in confirmed/pending/resolution may point to a battle context,
  pending queue, or executing action object.
- Roots that follow UI focus should be discarded as preview/current-focus state.

### Stage 3 - Search For Compact Records Near Roots

For readable roots, scan nearby memory for combinations of:

- Cloud pointer or char id `0x32`;
- action id `258`;
- timer/phase `3`, `1`, `255`;
- target pointer or target char id;
- damage values, only as optional evidence;
- tile/position values if known later.

Why:

- The real pending or executing action object should contain several of these values close together.
- A single value alone is too noisy.

### Stage 4 - Find Target/Epicenter Storage If Needed

If a current executing action pointer is not found, find where confirmed charged actions store:

- selected unit target; or
- tile epicenter.

Why:

- This is necessary to disambiguate multiple pending actions with the same or similar timers.
- It is not required for single-action live formula proof, but it is required for robust game-wide
  mechanics.

### Stage 5 - Better Hook Search

If the current unit-touch hook does not expose action context, search for a hook closer to:

- action execution;
- damage calculation;
- effect application;
- pending action dequeue / resolve.

Why:

- A better hook may have the current executing action pointer in a register.
- It may avoid many heuristics and make the mod reliable.

Use `docs/modding/04-re-strategy.md` and the classic FFT function map as guideposts.

## Next Live Experiment: Executing Action Pointer Probe

Status: current deployed experiment.

Current DLL/profile is already deployed:

```text
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.dll
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json
```

The deployed profile is:

```text
work\battle-runtime-settings.executing-action-pointer-probe.json
```

The user should launch through Reloaded-II with only:

- `fftivc.utility.modloader`
- `fftivc.generic.chronicle.codemod`

Keep the Generic Chronicle data mod disabled. Do not edit Reloaded-II AppConfig JSON.

### Objective

Find a reliable primary source for the currently resolving action, especially for delayed/charged
actions. The concrete question is:

```text
When Cross Slash resolves and native pre-clamp damage is staged for Agrias/Ninja, is there any
engine object reachable from registers/stack that identifies Cloud + action id + target/epicenter?
```

Why this matters:

- CT is only a fallback and fails conceptually for charged actions, Wait, reactions, and overlapping
  pending actions.
- The native pre-clamp hook already gives authoritative target + staged damage.
- The missing robust context is source/action identity at resolution time.
- If we find a current executing action/controller object, it can become the primary resolver.
- If we do not find one, the next practical path is strengthening our internal pending/resolving
  action tracker and searching for an earlier engine hook.

### Instrumentation To Inspect

This profile is observe-only. It should not rewrite HP/MP or change damage.

High-value log lines:

```text
[PENDING-ACTION-TRACK resolve-open ...]
[HOOK-REGS-EVENT kind=pendingresolve ...]
[HOOK-PTRSCAN-EVENT kind=pendingresolve ...]
[PRECLAMP-REWRITE ... flags=0x1 ...]    # LogOnly; not mutating staged damage
[PRECLAMP-PTRSCAN event=N targetPtr=0x... id=0x.. now=...]
[PENDING-ACTION-MATCH ...]
[DAMAGE ...]
```

What to compare:

1. Does the pending tracker open a resolving batch for Cloud/Cross Slash before HP events?
2. Do `pendingresolve` register roots point to a stable readable object?
3. Do native pre-clamp register/stack roots point to the same object or to an object containing
   known unit pointers?
4. Does any candidate object contain Cloud plus either Agrias/Ninja, action id `258`, or a nearby
   action/target/epicenter signature?
5. Are the two AoE victims attached to the same resolving batch?

### User Live Protocol

Test action:

```text
Cloud Cross Slash centered on Agrias, hitting Agrias + Ninja.
```

Report these milestones:

```text
baseline executing-pointer Cloud ativo
forecast Cross Slash AoE pronto, dano Agrias X
confirmado Cross Slash AoE, Cloud ainda ativo
pendente Cross Slash, ativo Beowulf
pendente Cross Slash, ativo Agrias
pendente Cross Slash, ativo Ninja
pos Cross Slash AoE, Agrias HP -X, Ninja HP -Y, ativo Z
```

After post-resolution, close the game so the log is stable.

### Expected Outcomes

Strong pass:

- `[PENDING-ACTION-TRACK resolve-open]` identifies Cloud/Cross Slash.
- `[HOOK-REGS-EVENT kind=pendingresolve]` or `[PRECLAMP-PTRSCAN]` exposes a readable root that
  consistently contains Cloud plus target/action evidence.
- Agrias and Ninja damage events map to the same resolving action batch.

Useful partial pass:

- No engine object is obvious, but the pending/resolving tracker correctly groups Agrias and Ninja
  under Cloud/Cross Slash. This supports using our pending table as the implementation path while
  RE continues.

Fail / pivot:

- No stable pendingresolve/pre-clamp root appears, and the resolving tracker cannot group the AoE
  events. Then search for an earlier hook around pending-action dequeue / action execution instead
  of relying on the stable unit hook.

### Post-Test Analysis

Copy or inspect:

```text
D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt
```

Run:

```powershell
python tools\analyze_battleprobe_log.py
```

Manual review is still required because `[PRECLAMP-PTRSCAN]` is new and may expose candidate roots
that no analyzer summarizes yet.

## Implementation Notes From Latest Changes

Files changed in the latest probe pass:

- `codemod/fftivc.generic.chronicle.codemod/Mod.cs`
  - Added `LogPendingActionCandidatesIfEnabled(...)`.
  - Called it on HP and MP events after hook-register event logging.
  - Added settings:
    - `LogPendingActionCandidatesOnEvent`
    - `LogAllPendingActionCandidates`
    - `PendingActionCandidateMaxUnits`
  - Added these settings to `Describe()`.
- `work/battle-runtime-settings.action-context-probe.json`
  - Enabled `LogPendingActionCandidatesOnEvent`.
- `docs/modding/11-charged-action-context-investigation.md`
  - Updated with Round 4 and Round 5 findings.
  - Corrected the AoE interpretation: forecast target is not final target identity.

The latest probe is still observe-only. It should not change HP/MP or gameplay behavior.

## Important Artifacts From Today's Tests

Single-target Cross Slash on Ramza:

- `work/live_unit_struct_snapshot.cross-slash-ramza-forecast.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-confirmed.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-pending-beowulf.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-pending-agrias.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-pending-ninja.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-post.extended.*`

AoE Cross Slash centered on Agrias:

- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-forecast.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-confirmed.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-pending-beowulf.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-pending-agrias.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-pending-ninja.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-post.extended.*`
- `work/live-captures/battleprobe_log.cross-slash-aoe-agrias-ninja-post.snapshot.txt`

Useful log excerpt from AoE resolution:

```text
[DAMAGE ptr=0x1418560E0 id=0x1E] 322 -> 207 = 115
[CTX ptr=0x1418560E0 id=0x1E] resolved=none ...
[DAMAGE ptr=0x141855EE0 id=0x80] 276 -> 3 = 273
[CTX ptr=0x141855EE0 id=0x80] resolved=none ...
```

This is the concrete proof that CT-only attribution failed for delayed AoE.

## Current Design Stance

Do not build the final redesign on CT-only resolution.

Use this priority order:

1. Real current executing action pointer, if found.
2. Pending caster/action tracker plus HP-write target.
3. Pending tracker plus target/epicenter if multiple pending actions need disambiguation.
4. CT reset / low-CT / counter inversion fallbacks for immediate and edge cases.

The next live test is designed to decide how much of item 2 is possible with only the current unit
struct fields at HP event time.

## 2026-06-23 Update: High-Offset Action-State Probe Result

Latest live test:

- Cloud used Cross Slash AoE centered on Agrias.
- Forecast on Agrias: `115`.
- Delayed resolution after Beowulf, Agrias, and Ninja turns.
- Final result:
  - Agrias HP `322 -> 207`, damage `115`.
  - Ninja HP `276 -> 3`, damage `273`.
  - Active unit after resolution: Ramza.

The strengthened probe answered the key HP-write question.

Cloud is visible as pending before resolution:

```text
Cloud s61=8 t18D=1 act=258 f1EF=8
```

But the engine clears Cloud's pending flags before the HP-write events:

```text
Cloud s61=8 t18D=255 act=258 f1EF=8
Cloud s61=0 t18D=255 act=258 f1EF=0
Agrias DAMAGE 322 -> 207 = 115
Ninja DAMAGE 276 -> 3 = 273
```

At HP-write time, Cloud only keeps `act=258`. Treat that as "last action id", not as enough proof
of current caster by itself.

Useful target-side finding:

- At HP write, each actual AoE victim exposes its own damage cache:
  - Agrias: `dmg1C4=115`, `chg1D8=2`, `f1E5=128`, `bb=2`.
  - Ninja: `dmg1C4=273`, `chg1D8=2`, `f1E5=136`, `bb=2`.

Conclusion:

- The current HP hook is reliable for final target and observed final damage.
- The current HP hook is too late to discover delayed-action caster by scanning currently pending
  units.
- CT-only attribution is not acceptable as the primary path for delayed/charged actions.

Next implementation direction:

1. Add an internal pending-action tracker.
2. Record `(caster, action id, pending flags/timer, optional target/forecast metadata)` when a unit
   enters pending state.
3. Detect the pending-to-cleared transition as the beginning of a resolving batch.
4. Attach subsequent HP writes in that short batch window to the resolving action.
5. Validate each target event with the target-local damage cache (`+0x1C4`) when available.
6. Keep searching for a real current-executing-action pointer/hook in parallel.

Why this matters:

The cleanup transition gives us a stronger signal than raw CT. It happens immediately before the
damage batch and is owned by the caster, while the HP-write event still gives the true affected
targets. This is likely enough to implement a robust delayed-action resolver prototype, even before
we find the engine's native executing-action pointer.

## Immediate Next Step And Objective

Immediate next step:

Implement a prototype `PendingActionTracker` inside the codemod runtime, without changing gameplay
yet. This should be observe-first: log its decisions, but do not rewrite HP based on them until the
logs prove the attribution is correct.

Objective:

Resolve delayed/charged action context without relying on CT. For every HP damage event caused by a
charged action, the runtime should be able to say:

- caster: which unit started the charged action;
- action id: which action is resolving, e.g. Cross Slash `258`;
- target: the unit whose HP is being changed by the HP-write event;
- batch: whether several HP writes belong to the same AoE/multi-target action;
- confidence/evidence: which pending/cleanup/damage-cache signals supported the match.

Prototype success criteria:

1. During the pending phase, the tracker records Cloud as owner of Cross Slash when the unit struct
   shows pending fields like `s61=8`, `act=258`, `f1EF=8`, and non-idle `t18D`.
2. When Cloud transitions from pending to cleared (`s61=0`, `f1EF=0`, `t18D=255`) while retaining
   `act=258`, the tracker opens a short "resolving batch" window for Cloud/Cross Slash.
3. The following HP writes for Agrias and Ninja are attributed to that same Cloud/Cross Slash batch.
4. Target-side `dmg1C4` represents raw formula/preview damage for each target when available:
   `115` for Agrias and `273` for Ninja in the latest nonlethal test. For lethal hits, the applied
   HP loss may be clamped to the target's previous HP, so `dmg1C4` can be greater than the observed
   HP delta and still be correct evidence.
5. CT is not used as the primary attribution signal for this delayed action. CT can remain as a
   fallback for immediate actions or unresolved edge cases.

Reason for doing this before more design work:

Custom battle formulas need reliable formula context. The final formula engine needs `caster`,
`action`, and `target` at the moment damage is applied. The current HP hook already gives target and
final damage, but not delayed-action caster. The tracker is the next offline implementation needed
to bridge that gap before testing formula rewrites on charged skills.

## 2026-06-23 Update: PendingActionTracker Offline Prototype

The observe-first `PendingActionTracker` prototype has now been implemented in the codemod runtime.

It is still diagnostic only:

- no HP/MP rewrites;
- no change to the existing CT/context resolver;
- no formula behavior changes;
- only new tracker/cache/match log lines.

Main new log families:

```text
[PENDING-ACTION-TRACK enter|update|resolve-open|resolve-close|abandon ...]
[PENDING-ACTION-TARGET enter|reenter|update|clear|drop ...]
[PENDING-ACTION-MATCH ...]
```

What the next live capture should prove:

1. Cloud enters the tracker as a pending action owner when `s61` and `f1EF` carry bit `0x08` and
   `act=258`.
2. The tracker opens a resolving batch when Cloud transitions to cleared pending flags while still
   retaining `act=258`.
3. Agrias and Ninja HP damage events match that same batch.
4. Current or recent target cache evidence validates damage values when available:
   - current cache: `dmg1C4` still present at HP write;
   - recent cache: `dmg1C4` was seen earlier and has cleared before HP write.
5. If a match fails, the logs include enough state to tell whether the tracker missed:
   - pending entry;
   - clear/resolve-open;
   - active batch window;
   - target-cache validation.

Hardening added before the next live test:

- Pending flags are treated as bit `0x08`, not exact byte equality, because `+0x61` is also a
  status byte.
- Before every positive HP damage match, the runtime refreshes all registered units with a `0x200`
  action probe snapshot. This reduces dependence on unit polling order and protects the first AoE HP
  event.
- The live action-context profile now uses a `5000ms` resolve window and `16` max batch events. This
  is deliberately wide for observe-only capture.
- The tracker retains recent target-side `dmg1C4` evidence after the live field clears, so single
  target delayed actions like Braver can still be validated if the target cache vanished before
  resolution.
- `[HOOK-REGS-EVENT ...]` now includes `hookAgeMs`, so register evidence can be weighted by how old
  the latest hook snapshot was when the polling layer noticed the HP/MP/CT event.

Primary expected live sequence for Cross Slash AoE:

```text
[PENDING-ACTION-TRACK enter caster=Cloud act=258 ...]
[PENDING-ACTION-TRACK update caster=Cloud act=258 ...]
[PENDING-ACTION-TRACK resolve-open batch=N caster=Cloud act=258 ...]
[PENDING-ACTION-MATCH ... target=Agrias ... resolved=Cloud ... batch=N ... confidence=damage-cache|recent-damage-cache]
[PENDING-ACTION-MATCH ... target=Ninja ... resolved=Cloud ... batch=N ... confidence=damage-cache|recent-damage-cache|recent-resolve]
```

If this sequence appears cleanly, the next implementation step is to make pending-action batch
resolution available to `BattleContextResolver` as a primary delayed-action source, keeping CT as
fallback. If it does not appear, inspect the `trackedPending`, `trackedResolving`, `activeBatches`,
`currentCache`, `recentCache`, and `hookAgeMs` fields before choosing the next hook/search path.

## 2026-06-23 Live Result: PendingActionTracker Cross Slash Proof

The pending-action tracker live probe succeeded on the migrated save / current PC.

Controlled action:

- Cloud used Cross Slash on Agrias.
- Preview damage shown by user: `187`.
- Resolution hit:
  - Ninja: `288 -> 15 = 273`.
  - Agrias: `470 -> 283 = 187`.
  - Next active unit: Ramza.

Key artifacts:

- `work/live-captures/battleprobe_log.pending-tracker-live-baseline-cloud-active.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-tracker-live-preview-cross-slash-agrias-187.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-tracker-live-confirmed-before-cloud-wait.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-tracker-live-resolved-cross-slash-agrias-ninja.snapshot.txt`
- `work/battleprobe_analysis.md`

Important log sequence:

```text
[PENDING-ACTION-TARGET enter target=Agrias dmg1C4=187/chg1D8=2/f1E5=128]
[PENDING-ACTION-TRACK enter caster=Cloud act=258 s61=8/t18D=2/f1EF=8]
[PENDING-ACTION-TRACK update caster=Cloud act=258 t18D=2 -> 1]
[PENDING-ACTION-TARGET enter target=Ninja dmg1C4=273/chg1D8=2/f1E5=128/bb=2]
[PENDING-ACTION-TARGET enter target=Agrias dmg1C4=187/chg1D8=2/f1E5=128/bb=2]
[PENDING-ACTION-TRACK resolve-open batch=1 caster=Cloud act=258 clear=s61=0/t18D=255/f1EF=0]
[DAMAGE Ninja] 288 -> 15 = 273
[PENDING-ACTION-MATCH event=1 target=Ninja resolved=Cloud source=pending-clear batch=1 act=258 confidence=damage-cache]
[DAMAGE Agrias] 470 -> 283 = 187
[PENDING-ACTION-MATCH event=2 target=Agrias resolved=Cloud source=pending-clear batch=1 act=258 confidence=damage-cache]
```

Crucial comparison:

- Pending-action tracker resolved both HP events to Cloud/Cross Slash, batch `1`, confidence
  `damage-cache`.
- Existing CT-low fallback resolved both HP events incorrectly to an enemy unit
  `0x1418544E0/id=0x82`.
- `HOOK-REGS-EVENT` had `hookAgeMs=2915`, so the register snapshot was stale and misleading for
  this delayed-resolution attribution.

Conclusion:

The internal pending-action table is now proven stronger than CT for delayed charged AoE context.
Next implementation step: expose pending-action batch matches as primary attacker/action context for
damage events, and keep CT as fallback only when no pending-action batch match exists.

## 2026-06-23 Implementation: Pending Action as Primary HP Context

Implemented the next step after the Cross Slash proof:

- `PendingActionTracker.MatchHpEvent` now returns structured `PendingActionMatch` data in addition
  to log lines.
- Positive HP events now prefer a pending-action match for `DamageEvent.Attacker`,
  `DamageEvent.AttackerSource`, and `DamageEvent.Action`.
- CT / low-CT / counter resolution still runs, but when pending context exists it is logged as
  fallback diagnostic context instead of becoming the primary attacker.
- Pending matches create an `ActionSignal` with `source=pending-clear`, `signal/id/actionId`,
  batch metadata, observed HP loss, target-cache damage, and confidence booleans.
- As a final pre-live hardening pass, pending matches only become primary HP context when the
  current or recent target damage cache matches the observed HP loss. `recent-resolve` matches
  remain diagnostic and are logged as `pendingRejected=.../reason=no-damage-cache-match`.
- Formula context now exposes `attacker.sourcePending`, `a.sourcePending`,
  `action.sourcePending`, plus common pending action variables such as `action.batchEvent`,
  `action.targetCacheDamage`, and `action.damageCacheMatch`.
- `RuntimeSettingsValidator` knows these variables, so settings can use them without false
  unknown-variable failures.

Validation:

```text
dotnet build codemod/fftivc.generic.chronicle.codemod/fftivc.generic.chronicle.codemod.csproj
dotnet build codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj
dotnet run --project codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj
python tools/test_runtime_profiles.py
dotnet run --project codemod/fftivc.generic.chronicle.codemod.settingsvalidate/fftivc.generic.chronicle.codemod.settingsvalidate.csproj
dotnet run --project codemod/fftivc.generic.chronicle.codemod.settingsvalidate/fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work/battle-runtime-settings.action-context-probe.json
```

All passed. The action-context probe profile still emits the expected hook-register warnings because
it intentionally enables short-capture RE probes.

Deployment:

- Installed to `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod` with
  `work\battle-runtime-settings.action-context-probe.json`.
- Previous deployed settings backup:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json.bak-20260623-112149`.
- Final double-check deploy backup:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json.bak-20260623-124521`.
- Final deployed DLL timestamp/hash:
  `2026-06-23 12:45:18`, sha256 prefix `DA4F7B18CFC3C5C1`.
- Pre-deploy game log backup:
  `work\live-captures\battleprobe_log.pre-pending-primary-deploy.20260623-112243.txt`.
- The live `battleprobe_log.txt` was cleared after backup so the next capture starts from a clean
  file.
- Next live run must restart/launch FFT through Reloaded so the newly deployed DLL is loaded.

## 2026-06-23 Live Result: Pending Action as Primary Context

The post-implementation live test succeeded. The newly deployed build promoted pending-action
matches to primary HP context while preserving CT-low as fallback diagnostics.

Controlled action:

- User baseline: Cloud active.
- Preview: Cross Slash on Agrias, `187`.
- Resolution:
  - Ninja: `288 -> 15 = 273`.
  - Agrias: `470 -> 283 = 187`.
  - Next active unit: Ramza.

Artifacts:

- `work/live-captures/battleprobe_log.pending-primary-live-baseline.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-primary-live-preview-cross-slash-agrias-187.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-primary-live-confirmed-before-cloud-wait.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-primary-live-resolved-cross-slash-agrias-ninja.snapshot.txt`
- `work/battleprobe_analysis.md`

Key log facts:

```text
[PENDING-ACTION-TARGET enter target=Agrias dmg1C4=187/chg1D8=2/f1E5=128]
[PENDING-ACTION-TRACK enter caster=Cloud act=258 s61=8/t18D=2/f1EF=8]
[PENDING-ACTION-TRACK resolve-open batch=1 caster=Cloud act=258]
[DAMAGE Ninja] 288 -> 15 = 273
[PENDING-ACTION-MATCH event=1 target=Ninja resolved=Cloud source=pending-clear batch=1 act=258 confidence=damage-cache observed=273]
[CTX Ninja] resolved=Cloud source=pending-clear pending=batch=1/act=258/event=1/16/confidence=damage-cache fallback=enemy-0x82 fallbackSource=ct-low
[DAMAGE Agrias] 470 -> 283 = 187
[PENDING-ACTION-MATCH event=2 target=Agrias resolved=Cloud source=pending-clear batch=1 act=258 confidence=damage-cache observed=187]
[CTX Agrias] resolved=Cloud source=pending-clear pending=batch=1/act=258/event=2/16/confidence=damage-cache fallback=enemy-0x82 fallbackSource=ct-low
```

Analyzer summary:

- HP damage events: `2`.
- Context resolved: `2/2`.
- Both contexts resolved to `0x1418562E0/id=0x32` with `source=pending-clear`.
- Both pending matches had `confidence=damage-cache`, so the final no-cache guard allowed them as
  primary context.
- CT-low fallback still chose `0x1418544E0/id=0x82` with `seen=2916ms`, proving again that CT-low
  is unsafe for delayed charged AoE attribution.
- No HP/MP rewrites occurred because the action-context profile is observe-only.

Conclusion:

The primary delayed-action context path is live-proven for Cross Slash AoE: `DamageEvent.Attacker`
and `DamageEvent.Action` can now carry Cloud/Cross Slash (`act=258`) when the HP event resolves.
Next implementation/test step should move from attribution to formula behavior: deploy a dry-run
profile with `RewriteObservedDamage=true`, `DryRunRewrites=true`, `LogResolvedRuntimeContext=true`,
and a harmless formula that references `attacker.sourcePending` and `action.id`.

## 2026-06-23 Setup: Pending Context Formula Dry Run

Prepared and deployed the next live-safe formula proof.

New files:

- `work/battle-runtime-settings.pending-context-dry-run.json`
- `work/runtime-simulation.pending-context-dry-run.json`

Profile intent:

- `DryRunRewrites=true`, so no HP writes occur.
- `RewriteObservedDamage=true` and `LogResolvedRuntimeContext=true`, so the runtime emits formula
  traces and dry-run decisions.
- Formula:

```text
if(event.isDamage && attacker.sourcePending && action.sourcePending && action.id == 258,
   vanillaDamage + 10,
   vanillaDamage)
```

Expected Cross Slash evidence:

- Ninja vanilla `273` should dry-run as `finalDamage=283`.
- Agrias vanilla `187` should dry-run as `finalDamage=197`.
- Both `[RUNTIME ...]` traces should include `attacker=...:pending-clear`,
  `action=pending-action-258:source=pending-clear:signal=258`,
  `trace.pending=1`, `trace.actionid=258`, and `trace.damagecachematch=1`.
- `[REWRITE-DRY-RUN ...]` should appear, but no `[REWRITE ...]` / `[REWRITE-VERIFY ...]` should
  appear.

Offline validation:

```text
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.pending-context-dry-run.json
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -- work\battle-runtime-settings.pending-context-dry-run.json work\runtime-simulation.pending-context-dry-run.json
```

Both passed. The simulation proves pending Cross Slash produces `finalDamage=197` for Agrias, while
the same event through `ct-low` stays at vanilla `187`.

Deployment:

- Installed to `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod`.
- Deployed settings backup:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json.bak-20260623-130207`.
- Pre-dry-run log backup:
  `work\live-captures\battleprobe_log.pre-pending-context-dry-run.20260623-130220.txt`.
- The live `battleprobe_log.txt` was cleared for the next run.

## 2026-06-23 Live Result: Pending Context Formula Dry Run

The formula dry-run proof succeeded.

Controlled action:

- User baseline: Cloud active.
- Preview: Cross Slash on Agrias, `187`.
- Resolution:
  - Ninja: `288 -> 15 = 273`.
  - Agrias: `470 -> 283 = 187`.
  - Next active unit: Ramza.

Artifacts:

- `work/live-captures/battleprobe_log.pending-context-dry-run-baseline.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-context-dry-run-preview-cross-slash-agrias-187.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-context-dry-run-confirmed-before-cloud-wait.snapshot.txt`
- `work/live-captures/battleprobe_log.pending-context-dry-run-resolved-cross-slash-agrias-ninja.snapshot.txt`
- `work/battleprobe_analysis.md`

Key result:

```text
[RUNTIME Ninja] attacker=Cloud:pending-clear action=pending-action-258:source=pending-clear:signal=258
vars=trace.pending=1,trace.actionpending=1,trace.actionid=258,trace.batchevent=1,trace.damagecachematch=1,trace.observedhploss=273
final=283:FinalDamageFormula
[REWRITE-DRY-RUN Ninja] vanillaDamage=273 finalDamage=283 HP 15->5

[RUNTIME Agrias] attacker=Cloud:pending-clear action=pending-action-258:source=pending-clear:signal=258
vars=trace.pending=1,trace.actionpending=1,trace.actionid=258,trace.batchevent=2,trace.damagecachematch=1,trace.observedhploss=187
final=197:FinalDamageFormula
[REWRITE-DRY-RUN Agrias] vanillaDamage=187 finalDamage=197 HP 283->273
```

Analyzer summary:

- Runtime contexts parsed: `2`.
- Attacker sources: `pending-clear=2`.
- Action: `pending-action-258`, signal `258`, source `pending-clear`.
- Rewrite events: `2`.
- Rewrite status: `dry-run=2`.
- Concrete HP rewrites: `0`.
- HP after dry-run remained at the engine-observed values (`Ninja=15`, `Agrias=283`) in subsequent
  action-state samples, proving no managed memory write occurred.

Conclusion:

The pending-action context is now live-proven through the full runtime formula path. JSON formulas can
read `attacker.sourcePending`, `action.sourcePending`, `action.id`, `action.batchEvent`, and
`action.damageCacheMatch` to make damage decisions for delayed charged actions. The next risk to test
is no longer basic context plumbing; it is real rewrite behavior with a conservative non-lethal
formula, or broader attribution coverage across other delayed actions.

## 2026-06-23 Research Roadmap Recalibration

The investigation compass has been consolidated in:

- `work/combat-redesign-research-roadmap.md`

Key recalibration:

- CT fallback is now treated as diagnostic-only. The research objective is to retire CT as a combat
  context source by replacing it with memory/action-context resolution.
- Braver and Cross Slash are recorded as proven delayed-action baselines.
- KO/lethal custom damage is a blocking requirement for the combat redesign, not a later polish
  item. `MinHpFloor=1` remains only a temporary safety mode.
- Repeating Cross Slash dry-runs has low value unless it tests a new gate. The highest-value next
  research tracks are KO/pre-damage discovery, CT retirement, action-family breadth, and equipment
  context.

## 2026-06-23 Setup: KO / Pre-Damage Observe-Only Probe

Prepared the next high-yield live test for the combat redesign roadmap.

New profile:

- `work/battle-runtime-settings.ko-pre-damage-probe.json`

Profile intent:

- Observe-only: no HP/MP rewrites, no `MinHpFloor`, no death-state writes.
- Capture two known nonlethal delayed AoE HP events from Cloud Cross Slash.
- Then capture one real vanilla lethal KO, ideally Ramza killing the Ninja left at low HP.
- Emit richer evidence than prior death captures:
  - `[HP-EVENT-PROBE ...]` with applied HP loss, raw forecast damage, lethal/clamp
    classification, and raw pre/post diff;
  - `[HP-EVENT-PRE-RAW ...]` / `[HP-EVENT-POST-RAW ...]` for short controlled event count;
  - `[DEATH-DUMP]`, `[DEATH-DIFF]`, and `[DEATH-FOLLOW]`;
  - hook register event snapshots, stack slots, and pointer scans;
  - actor probe across the full `0x00..0x1FF` unit snapshot;
  - pending-action tracker logs for the Cross Slash baseline.

Runtime instrumentation update:

- The standard unit snapshot copied by the hook was widened from `0x180` to `0x200`, matching the
  action/pending fields used in the delayed-action investigation.
- Added opt-in HP event raw-diff logging:
  - `LogHpEventProbe`
  - `HpEventProbeMaxLogs`
  - `HpEventProbeDiffMax`
  - `HpEventProbeDumpRaw`

Validation:

```text
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj
dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj
python tools\report_runtime_profiles.py
python tools\test_runtime_profiles.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.ko-pre-damage-probe.json
```

All passed. The profile validation emits expected RE-capture warnings for hook register probes,
pointer scans, and raw HP event dumps.

Deployment:

- Installed to `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod`.
- Deployed settings backup:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json.bak-20260623-135207`.
- Pre-probe log backup:
  `work\live-captures\battleprobe_log.pre-ko-pre-damage-probe.20260623-135217.txt`.
- The live `battleprobe_log.txt` at the local Steam install was cleared after backup.

## 2026-06-23 Live Result: KO / Pre-Damage Observe-Only Probe

The KO probe succeeded and captured both the known Cross Slash nonlethal baseline and a real
vanilla KO.

Artifacts:

- `work/live-captures/battleprobe_log.ko-pre-damage-baseline-cloud-active.snapshot.txt`
- `work/live-captures/battleprobe_log.ko-pre-damage-preview-cross-slash-agrias-187.snapshot.txt`
- `work/live-captures/battleprobe_log.ko-pre-damage-confirmed-before-cloud-wait.snapshot.txt`
- `work/live-captures/battleprobe_log.ko-pre-damage-resolved-cross-slash-agrias-ninja.snapshot.txt`
- `work/live-captures/battleprobe_log.ko-pre-damage-preview-ramza-rush-ninja-50.snapshot.txt`
- `work/live-captures/battleprobe_log.ko-pre-damage-resolved-ramza-rush-ninja-ko.snapshot.txt`
- `work/live-captures/battleprobe_log.ko-pre-damage-resolved-ramza-rush-ninja-ko-followup.snapshot.txt`
- `work/battleprobe_analysis.ko-pre-damage.md`

User-observed sequence:

- Baseline: Cloud active.
- Preview: Cloud Cross Slash on Agrias, `187`.
- Resolution: Agrias `-187`, Ninja `-273`, next active Ramza.
- Preview KO: Ramza Rush on Ninja, preview `50`.
- Resolution KO: Ninja died. Preview/raw formula damage was `50`; applied HP loss was clamped by
  the target's remaining HP, so the observed HP event was `15 -> 0 = 15`. Next active remained
  Ramza while waiting for Wait confirmation.

Cross Slash baseline remained good:

- Ninja: `[DAMAGE ptr=0x141855EE0 id=0x80] 288 -> 15 = 273`.
- Agrias: `[DAMAGE ptr=0x1418560E0 id=0x1E] 470 -> 283 = 187`.
- Both events matched Cloud/Cross Slash through `source=pending-clear`, `action.id=258`,
  `confidence=damage-cache`.
- CT fallback was stale/wrong during this same sequence and remains diagnostic-only.

KO evidence:

```text
[DAMAGE ptr=0x141855EE0 id=0x80] 15 -> 0 = 15 sampleAgeMs=19
[HP-EVENT-PROBE kind=damage event=3 ptr=0x141855EE0 id=0x80 prevHp=15 currentHp=0 delta=15 lethal=1 overkill=0 maxHp=288 team=0 foe=0 ct=101 action=s61=32/t18D=255/act=0/f1EF=32/dmg1C4=50/chg1D8=130/f1E5=128/b8=0/ba=0/bb=1] diff=+0x30:0F->00 +0x61:00->20 +0x63:21->20 +0x18C:00->01 +0x1BB:00->01 +0x1DB:00->20 +0x1EF:00->20 +0x1F1:01->00 +0x1F5:FF->10
[DEATH-DIFF ptr=0x141855EE0 id=0x80] alive->dead +0x30:0F->00 +0x61:00->20 +0x63:21->20 +0x18C:00->01 +0x1BB:00->01 +0x1DB:00->20 +0x1EF:00->20 +0x1F1:01->00 +0x1F5:FF->10
```

Note: the captured log predates the clarified probe fields. In a new build, the same event should
also show approximately `appliedHpLoss=15`, `rawForecastDamage=50`, `hpClamp=1`, and
`rawForecastOverkill=35`.

Important interpretation:

- Real KO is a coordinated state transition, not only `HP=0` and not only `+0x61`.
- The observed death-state diff touched at least `+0x30`, `+0x61`, `+0x63`, `+0x18C`, `+0x1BB`,
  `+0x1DB`, `+0x1EF`, `+0x1F1`, and `+0x1F5`.
- No delayed `[DEATH-FOLLOW]` changes appeared in the follow-up window; the observed unit-local
  death transition happened on the first HP-zero frame.
- This strengthens the earlier conclusion that post-damage HP reconciliation is not enough for
  custom lethal damage. The preferred route is still a pre-damage hook or a call/replication of the
  engine's KO routine.

Action-context caveat discovered by the KO event:

- The preview/action cache for Ramza Rush on Ninja was `dmg1C4=50`, representing raw formula damage
  before HP clamping. The HP event delta was `15` because the target had `15` HP remaining and
  vanilla clamped the applied loss to current HP.
- The previous pending/damage-cache matcher required exact `observedAppliedLoss == cachedRawDamage`,
  so it rejected lethal events where the target cache was still correct evidence.
- For lethal events, the resolver should treat `cachedDamage >= prevHp && currentHp == 0 &&
  observed == prevHp` as a strong target-cache match.
- The source/caster for immediate Rush was visible as a candidate clue (`Ramza` with `act=147`), but
  the current resolver did not promote it to primary context. Do not treat this as solved yet.

Next implementation direction:

1. Add lethal-aware target-cache matching so vanilla clamps do not cause false negatives.
2. Continue CT retirement by finding a current-action memory source for immediate actions such as
   Ramza Rush, rather than relying on CT.
3. Search for the pre-commit damage value or engine KO routine boundary before attempting custom
   lethal writes.

## 2026-06-23 Implementation: Lethal-Aware Target Cache Matching

Implemented the first offline fix from the KO result.

Runtime change:

- `[HP-EVENT-PROBE]` now logs applied HP loss/gain separately from raw target-side forecast damage,
  plus `hpClamp` and `rawForecastOverkill`, so future captures do not confuse formula damage with
  HP-capped applied loss.
- `PendingActionTracker` now treats a lethal clamped HP event as valid target-cache evidence when:
  - current HP is `0`;
  - observed HP loss is positive;
  - cached raw forecast/formula damage is at least the observed applied loss.
- Exact matches and lethal-clamp matches are tracked separately.
- Positive matches can now report confidence:
  - `damage-cache`
  - `recent-damage-cache`
  - `damage-cache-lethal-clamp`
  - `recent-damage-cache-lethal-clamp`
  - `recent-resolve`

New formula/log variables exposed through `action.*` and `act.*`:

- `exactDamageCacheMatch`
- `currentExactDamageCacheMatch`
- `recentExactDamageCacheMatch`
- `lethalClampDamageCacheMatch`
- `currentLethalClampDamageCacheMatch`
- `recentLethalClampDamageCacheMatch`
- `confidenceLethalClampDamageCache`

Important limitation:

- This does not solve immediate Rush attribution by itself. In the captured Rush KO event there was
  no pending resolving batch, so the next unknown is still the source/caster context for immediate
  actions. The new logic prevents the probe from discarding good target-cache evidence when a
  source context is otherwise available.

Validation:

```text
dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj
python tools\test_runtime_profiles.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.ko-pre-damage-probe.json
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.pending-context-dry-run.json
```

All passed. Two earlier parallel command attempts hit transient `.NET Host` file locks while building
the same project concurrently; rerunning the commands sequentially passed.

Deployment status:

- Not yet deployed after this local fix.
- Before the next live capture, close FFT and run the normal build/deploy flow with
  `work\battle-runtime-settings.ko-pre-damage-probe.json` or the next probe profile.

Next implementation direction:

1. Add or tune logging around immediate-action candidates so the next Rush/basic-action capture can
   promote a caster/action source without CT.
2. Search around the HP application boundary for a pre-commit damage value or engine KO routine.
3. Only after one of those paths is understood, attempt a custom lethal application test.

## 2026-06-23 Setup: Immediate Action / KO Boundary Probe

Implemented and deployed the next observe-only live probe.

New profile:

- `work/battle-runtime-settings.immediate-action-ko-boundary-probe.json`

Profile intent:

- Repeat the known high-yield sequence:
  1. Cloud active baseline.
  2. Cloud Cross Slash on Agrias, preview `187`.
  3. Resolve Cross Slash: Agrias `-187`, Ninja `-273`, next active Ramza.
  4. Ramza Rush on the low-HP Ninja, preview expected raw formula damage around `50`.
  5. Resolve Rush KO.
- Preserve all KO/pre-damage evidence from the previous profile.
- Add explicit raw-vs-applied HP event fields:
  - `appliedHpLoss`
  - `appliedHpGain`
  - `rawForecastDamage`
  - `hpClamp`
  - `rawForecastOverkill`
- Add ranked `[IMMEDIATE-ACTION-CANDIDATES ...]` event logs with:
  - candidate role: `target`, `source-like`, or `context`;
  - score;
  - `seenAgeMs`;
  - `ctDropAgeMs`;
  - `stateAgeMs`;
  - action state fields such as `act`, `s61`, `t18D`, `f1EF`, `dmg1C4`, `chg1D8`, `f1E5`, `bb`;
  - exact-applied and lethal-clamp cache flags.

What this test is meant to discover:

- Whether immediate Rush/basic actions expose a usable memory source for `caster=Ramza` and
  `action=Rush` without treating CT as the final answer.
- Whether the target-side raw formula damage and HP-capped applied loss are cleanly separated in the
  log.
- Whether hook/register evidence is fresh enough for immediate actions, unlike the stale delayed
  Cross Slash register evidence.
- Which candidate fields are most promising for the next resolver prototype.

Validation:

```text
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj
dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.immediate-action-ko-boundary-probe.json
python tools\report_runtime_profiles.py
python tools\test_runtime_profiles.py
```

All passed. The profile validator emits expected short-RE warnings for hook registers, pointer
scans, and HP event raw dumps.

Deployment:

- FFT was not running at deploy time.
- Installed DLL:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.dll`.
- Deployed DLL timestamp/hash:
  `2026-06-23 15:44:59`, sha256
  `2EC19577EE16D687C4A340353E2E2C30662A085428C69F46491FDC1CF97284BF`.
- Installed runtime settings:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`.
- Deployed settings backup:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json.bak-20260623-154502`.
- Pre-probe live log backup:
  `work\live-captures\battleprobe_log.pre-immediate-action-ko-boundary-probe.20260623-154450.txt`.
- Active live log path on this PC:
  `C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt`.
- The active live log was cleared before deploy.

Historical live instructions used for that 2026-06-23 probe:

1. Launch FFT through Reloaded-II with only:
   - `fftivc.utility.modloader`
   - `fftivc.generic.chronicle.codemod`
2. Load the same battle/save and stop when Cloud is active. Report:
   - `baseline: Cloud ativo`
3. Select Cross Slash on Agrias and report preview damage:
   - expected: `preview: Cross Slash na Agrias, dano 187`
4. Confirm the action, stop before Cloud's Wait, and report:
   - `confirmado: antes do Wait do Cloud`
5. Let Cross Slash resolve and report:
   - expected: `resolveu Cross Slash: Agrias -187, Ninja -273, próximo ativo Ramza`
6. With Ramza active, use Rush on the low-HP Ninja and report preview:
   - expected raw preview around `50`
7. Resolve Rush and report:
   - whether Ninja died;
   - applied damage shown by the game;
   - next active unit.

After each user report, capture a snapshot of the active log before asking for the next action.

## 2026-06-23 Live Result: Immediate Action / KO Boundary Probe

The deployed immediate-action / KO-boundary profile was run successfully. It captured the known
Cross Slash delayed AoE baseline, a lethal immediate Rush, and an unexpected but useful Reraise
revive.

Captured artifacts:

- `work/live-captures/battleprobe_log.immediate-ko-boundary-baseline-cloud-active.snapshot.txt`
- `work/live-captures/battleprobe_log.immediate-ko-boundary-preview-cross-slash-agrias-187.snapshot.txt`
- `work/live-captures/battleprobe_log.immediate-ko-boundary-confirmed-before-cloud-wait.snapshot.txt`
- `work/live-captures/battleprobe_log.immediate-ko-boundary-resolved-cross-slash-agrias-ninja.snapshot.txt`
- `work/live-captures/battleprobe_log.immediate-ko-boundary-preview-ramza-rush-ninja-50.snapshot.txt`
- `work/live-captures/battleprobe_log.immediate-ko-boundary-resolved-ramza-rush-ninja-ko-before-wait.snapshot.txt`
- `work/live-captures/battleprobe_log.immediate-ko-boundary-after-ramza-wait-ninja-reraise.snapshot.txt`

User-observed sequence:

- Baseline: Cloud active.
- Preview: Cloud Cross Slash on Agrias, `187`.
- Confirmed: stopped before Cloud's Wait.
- Resolution: Agrias `-187`, Ninja `-273`, next active Ramza.
- Preview KO: Ramza Rush on Ninja, preview `50`.
- Resolution KO: Ninja died, number shown by the game was `33`, Ramza still active before Wait.
- After Ramza Wait: next active Ninja, because Ninja had Reraise and revived.

Cross Slash delayed AoE remained clean:

- Cloud entered the pending tracker as caster `act=258`.
- The resolving batch opened for Cloud/Cross Slash and closed with `events=2`.
- Ninja:
  - HP event: `288 -> 15 = 273`;
  - `appliedHpLoss=273`;
  - `rawForecastDamage=273`;
  - `hpClamp=0`.
- Agrias:
  - HP event: `470 -> 283 = 187`;
  - `appliedHpLoss=187`;
  - `rawForecastDamage=187`;
  - `hpClamp=0`.

Immediate Rush / KO evidence:

- Preview target cache on Ninja entered as `dmg1C4=50/chg1D8=130/f1E5=128/bb=2`.
- At execution, Ramza appeared with `act=147` and `ba=1`.
- Just before the HP-zero frame, the Ninja target cache changed from `dmg1C4=50` to `dmg1C4=33`.
- The game showed `33`, matching the final execution cache rather than the first preview cache.
- The HP event was:

```text
[DAMAGE ptr=0x141855EE0 id=0x80] 15 -> 0 = 15 sampleAgeMs=12
[HP-EVENT-PROBE kind=damage event=3 ptr=0x141855EE0 id=0x80 prevHp=15 currentHp=0 delta=15 appliedHpLoss=15 appliedHpGain=0 rawForecastDamage=33 lethal=1 hpClamp=1 overkill=0 rawForecastOverkill=18 maxHp=288 team=0 foe=0 ct=101 action=s61=32/t18D=255/act=0/f1EF=32/dmg1C4=33/chg1D8=130/f1E5=128/b8=0/ba=0/bb=1]
```

Important correction to the earlier KO interpretation:

- The first preview cache can be stale by execution time.
- For Rush, the useful execution-time raw damage was `33`, not the earlier preview `50`.
- The HP event still applied only `15` because the target had `15` HP.
- Therefore the current event model should distinguish:
  - preview/raw-at-selection cache;
  - execution-time target cache;
  - applied HP loss after vanilla current-HP clamp.

Immediate-action candidate result:

- The candidate logger did expose the true actor as:
  - `Ramza`, pointer `0x141855CE0`, id `0x03`;
  - `role=source-like`;
  - `act=147`;
  - `stateAgeMs=1170`;
  - `ba=1`.
- However, Cloud also appeared as a stale `source-like` candidate with `act=258`, `ba=1`, and the
  same score. This means the current candidate score is useful as evidence but not yet a reliable
  resolver.
- The next resolver/probe should demote stale previous-action holders and add a tie-break that can
  distinguish the current immediate actor from old delayed-action owners.
- The target-side cache did correctly mark the KO as `lethalClamp=1` on the Ninja candidate.

Reraise / revive evidence:

After Ramza's Wait, Ninja revived and became the next active unit.

```text
[HP-EVENT-PROBE kind=healing event=4 ptr=0x141855EE0 id=0x80 prevHp=0 currentHp=28 delta=28 appliedHpLoss=0 appliedHpGain=28 rawForecastDamage=0 lethal=0 hpClamp=0 ... ct=1 action=s61=0/t18D=255/act=0/f1EF=0/dmg1C4=0/chg1D8=0/f1E5=72/b8=1/ba=0/bb=2]
```

The revive diff reversed many of the KO-state fields through the engine:

- `+0x30:00->1C`;
- `+0x41:65->01`;
- `+0x61:20->00`;
- `+0x63:20->21`;
- `+0x1B8:00->01`;
- `+0x1BB:01->02`;
- `+0x1C4:21->00`;
- `+0x1C6:00->1C`;
- `+0x1D8:82->00`;
- `+0x1DB:20->00`;
- `+0x1DD:00->01`;
- `+0x1E0:00->20`;
- `+0x1E5:80->48`;
- `+0x1EF:20->00`;
- `+0x1F1:00->01`;
- `+0x1F5:10->FF`.

Implications:

- The probe is robust enough to separate execution raw damage from HP-capped applied loss.
- Lethal-clamp detection works on the live KO event.
- Immediate-action source resolution is not solved yet, but the needed signal is likely present:
  Ramza's `act=147` appears close to the HP event.
- Candidate scoring needs stale-action suppression before CT can be fully retired for immediate
  actions.
- Reraise confirms that death/revive state transitions are engine-owned multi-field transitions.
  This strengthens the case against manual post-damage HP/flag patching for custom lethal damage.

Next offline direction:

1. Build an offline parser/report over this capture to compare candidate freshness, action fields,
   and target cache state at the Rush HP event.
2. Tune immediate-action candidate scoring so Ramza/Rush wins over stale Cloud/Cross Slash without
   using CT as causality.
3. Search for a pre-commit damage value or KO routine boundary using the `50 -> 33 -> applied 15`
   transition as the guide.
4. Treat Reraise as separate revive-state evidence, not as a normal heal source attribution test.

## 2026-06-23 Implementation: Immediate Candidate Freshness Scoring

Implemented the first offline fix from the immediate Rush KO capture.

Runtime/logging change:

- Added action-age tracking separate from generic action-state age:
  - `actionIdAgeMs`: how long the current `act` value has been present for that unit;
  - `activeActionAgeMs`: how long the current active action signature `act>0 && ba!=0` has been
    present.
- `[IMMEDIATE-ACTION-CANDIDATES ...]` now logs:
  - `actionIdAgeMs`;
  - `activeActionAgeMs`;
  - `freshAct`;
  - `freshActive`;
  - `staleAct`;
  - `staleActive`.
- Candidate scoring now rewards fresh action ids / fresh active-action signatures and penalizes old
  stale action holders.
- Added a testable `ImmediateActionCandidateScoring` helper instead of keeping the scoring as
  ad-hoc inline logging code.

Why this fixes the Rush/Cloud tie:

- In the live Rush KO, Ramza's `act=147` appeared close to the HP event.
- Cloud still retained stale `act=258` from the earlier Cross Slash.
- The old score saw both as `source-like`.
- The new score distinguishes "fresh current action" from "old retained action id", so the same
  scenario ranks Ramza/Rush above stale Cloud/Cross Slash without using CT as causality.

Validation:

```text
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj
dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj
python tools\test_runtime_profiles.py
python tools\report_runtime_profiles.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.immediate-action-ko-boundary-probe.json
```

All passed. The profile validator still emits the expected five short-RE warnings for this noisy
probe profile.

Deployment status:

- Historical note: this scoring-only change was not deployed at this point in the investigation.
  It was superseded by the later action-boundary deployment and live validation below.

Historical next direction at that point:

1. Live-validate the new candidate ranking if it remains a blocker.
2. In parallel, start the pre-commit damage / KO routine boundary search using the captured
   `50 -> 33 -> applied 15` Rush transition.
3. Do not promote immediate candidates to primary action context until the fresh-action ranking has
   at least one live validation or stronger offline evidence from another immediate action capture.

## 2026-06-23 Offline Analysis: Rush KO Boundary Timeline

Added a dedicated offline analyzer:

- `tools/analyze_immediate_action_boundary.py`
- smoke test: `tools/test_immediate_action_boundary.py`
- generated report: `work/battleprobe_analysis.immediate-ko-boundary.md`

The analyzer parses the full immediate KO/Reraise snapshot and reconstructs the timing around the
Rush event from `[ACTION-STATE]`, `[PENDING-ACTION-TARGET]`, `[HP-EVENT-PROBE]`, and
`[IMMEDIATE-ACTION-CANDIDATES]` lines.

Key derived boundary intervals from the live capture:

| Marker | Line | Delta from previous | Delta from preview |
| --- | ---: | ---: | ---: |
| preview cache `50` | 208 | `0 ms` | `0 ms` |
| Ramza `act=147` appears | 212 | `108798 ms` | `108798 ms` |
| execution cache `33` | 218 | `1108 ms` | `109906 ms` |
| HP zero / KO flags | 220 | `62 ms` | `109968 ms` |
| Ramza post-hit `bb=1` | 234 | `1671 ms` | `111639 ms` |
| Reraise HP restore | 241 | `142114 ms` | `253752 ms` |

Important re-rank result for event 3, the Rush KO:

- Old score tied Ramza/Rush and stale Cloud/Cross Slash at `1300`.
- Offline action-age scoring ranks:
  - Ramza `0x141855CE0/id=0x03`, `act=147`, `freshAct/freshActive`, new score `2150`;
  - Cloud `0x1418562E0/id=0x32`, `act=258`, `staleAct/staleActive`, new score `-250`;
  - Ninja target lethal cache remains visible as `lethalClamp`.

This was strong offline evidence that the new freshness scoring fixed the specific Ramza-vs-Cloud
tie. The later action-boundary live validation confirmed the same result for the covered Rush case.

The useful KO boundary is now narrower:

- `dmg1C4=50` is selection/preview-time evidence.
- `dmg1C4=33` is execution-time raw damage and appears about `62 ms` before HP zero/KO flags in the
  current polling capture.
- HP zero, KO status fields, and death diff happen together at line `220`/`222`/`223`/`225`.

Next technical focus:

1. Search for the pre-commit damage value / engine KO routine around the execution-cache `33` to
   HP-zero transition.
2. If a live validation is needed, deploy the current build and confirm that the next
   `[IMMEDIATE-ACTION-CANDIDATES]` line logs `freshAct/freshActive` for the acting unit and
   `staleAct/staleActive` for previous action holders.
3. Keep Reraise as revive-state evidence, not as a regular healing attribution test.

## 2026-06-23 Implementation: Action Boundary Probe

Prepared the next surgical live probe for the narrowed `dmg1C4=33 -> HP zero / KO flags` boundary.

Runtime/logging change:

- Added `[ACTION-BOUNDARY ...]` lines for changes in a focused set of offsets:
  - HP: `+0x30/+0x31`;
  - status/pending/death: `+0x61`, `+0x63`, `+0x18C`, `+0x1DB`, `+0x1EF`, `+0x1F1`, `+0x1F5`;
  - action/target cache: `+0x18D`, `+0x1A0..+0x1A3`, `+0x1B8..+0x1BB`,
    `+0x1C4..+0x1C7`, `+0x1D8`, `+0x1DD`, `+0x1E0`, `+0x1E5`.
- Each line includes:
  - `event`;
  - `now`;
  - `touch`;
  - `hookAgeMs`;
  - `reason`, such as `forecast-damage-change`, `hp-zero`, `phase-change`,
    `status-pending-change`, `death-state-change`;
  - compact `prev=` and `curr=` field summaries;
  - short byte diff.
- Added optional hook-register snapshots on action-boundary events through
  `HookRegisterProbeOnActionBoundary`.
- `tools/analyze_immediate_action_boundary.py` now parses `[ACTION-BOUNDARY]` lines.
- `tools/test_immediate_action_boundary.py` validates the analyzer on a synthetic
  Ramza-fresh / Cloud-stale / KO-boundary sample.

Profile change:

- `work/battle-runtime-settings.immediate-action-ko-boundary-probe.json` now enables:
  - `LogActionBoundaryProbe=true`;
  - `ActionBoundaryProbeMaxLogs=96`;
  - `ActionBoundaryProbeDiffMax=32`;
  - `HookRegisterProbeOnActionBoundary=true`.

Validation:

```text
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj
dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj
python tools\test_immediate_action_boundary.py
python tools\report_runtime_profiles.py
python tools\test_runtime_profiles.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.immediate-action-ko-boundary-probe.json
```

All passed. The immediate-action/KO-boundary profile now emits six expected short-RE warnings:

- hook register probe;
- event-correlated hook registers;
- pointer scans;
- HP event probe;
- raw HP dumps;
- action boundary probe.

Deployment status:

- Deployed to Reloaded-II on `2026-06-23 16:29`.
- Target:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod`.
- Runtime settings installed:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`.
- Existing deployed settings were backed up as:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json.bak-20260623-162921`.
- Deployed DLL:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.dll`;
  SHA256 `2C923CD8B243D4297E70F036257A0EE80EE80C94F92418DBA2A4A1989BF49CA5`.
- Previous live log was archived as:
  `work\live-captures\battleprobe_log.pre-action-boundary-probe.20260623-162934.txt`.
- Active live log was cleared:
  `C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt`.

Next live validation goal, if used:

- Repeat only the high-yield Cloud Cross Slash -> Ramza Rush KO path.
- Confirm these lines around Rush:
  - target cache selection: `dmg1C4=50`;
  - action boundary: `forecast-damage-change` to `dmg1C4=33`;
  - action boundary: `hp-zero,status-pending-change,death-state-change`;
  - immediate candidates: Ramza `freshAct/freshActive`, stale Cloud `staleAct/staleActive`.

## 2026-06-23 Live Result: Action Boundary KO Validation

The deployed `[ACTION-BOUNDARY]` probe was live-validated with the same high-yield path:

1. Cloud active baseline.
2. Cross Slash preview on Agrias: `187`.
3. Confirmed before Cloud Wait.
4. Cross Slash resolved: Agrias `-187`, Ninja `-273`, next active Ramza.
5. Ramza Rush preview on low-HP Ninja: `50`.
6. Rush resolved lethal: Ninja died, Ramza still active before Wait.
7. After Ramza Wait: next active Ninja because Reraise revived him.

Captured snapshots:

- `work\live-captures\battleprobe_log.action-boundary-baseline-cloud-active.snapshot.txt`
- `work\live-captures\battleprobe_log.action-boundary-preview-cross-slash-agrias-187.snapshot.txt`
- `work\live-captures\battleprobe_log.action-boundary-confirmed-before-cloud-wait.snapshot.txt`
- `work\live-captures\battleprobe_log.action-boundary-resolved-cross-slash-agrias-ninja.snapshot.txt`
- `work\live-captures\battleprobe_log.action-boundary-preview-ramza-rush-ninja-50.snapshot.txt`
- `work\live-captures\battleprobe_log.action-boundary-resolved-ramza-rush-ninja-ko-before-wait.snapshot.txt`
- `work\live-captures\battleprobe_log.action-boundary-after-ramza-wait-ninja-reraise.snapshot.txt`

Generated analysis:

- `work\battleprobe_analysis.action-boundary-ko.md`

Important correction:

- The game-visible/raw execution value for Rush is `33`.
- The applied HP loss is `15` because the Ninja had only `15` HP remaining.
- The preview value was `50`; the engine changed the target cache to `33` before HP reached zero.

Critical Rush KO boundary:

```text
preview cache:     dmg1C4=50/chg1D8=130/f1E5=128/bb=2
Ramza action:      act=147/ba=1
execution cache:   dmg1C4=33/chg1D8=130/f1E5=128/bb=0
HP zero / KO:      hp 15 -> 0, s61 0 -> 32, f1EF 0 -> 32, bb 0 -> 1
HP event:          rawForecastDamage=33, appliedHpLoss=15, hpClamp=1, rawForecastOverkill=18
```

The action-boundary probe captured the exact short boundary:

- `event=47`: Ninja target cache entered preview value `50`.
- `event=52`: Ninja target cache changed `50 -> 33`.
- `event=53`: HP zero and KO/death fields landed together:
  `+0x30:0F->00 +0x61:00->20 +0x63:21->20 +0x18C:00->01 +0x1BB:00->01 +0x1DB:00->20 +0x1EF:00->20 +0x1F1:01->00 +0x1F5:FF->10`.

Immediate-action scoring validation:

- Ramza/Rush won the candidate ranking live:
  - pointer `0x141855CE0`, id `0x03`;
  - `act=147`;
  - `actionIdAgeMs=1179`;
  - `activeActionAgeMs=1179`;
  - `freshAct=1`;
  - `freshActive=1`;
  - score `2150`.
- Stale Cloud/Cross Slash was demoted:
  - pointer `0x1418562E0`, id `0x32`;
  - `act=258`;
  - `actionIdAgeMs=434376`;
  - `activeActionAgeMs=434376`;
  - `staleAct=1`;
  - `staleActive=1`;
  - score `-250`.

This validates the freshness scoring concept for the covered immediate Rush case and removes the
specific Ramza-vs-stale-Cloud tie as a blocker for this path. It does not yet prove all immediate
actions or all action families.

Reraise evidence:

- After Ramza Wait, Ninja revived and became active:
  `0 -> 28`, `f1E5=72`, `b8=1`, `bb=2`.
- The revive boundary reversed the KO state through an engine-owned multi-field transition:
  `+0x30:00->1C +0x61:20->00 +0x63:20->21 +0x1B8:00->01 +0x1BB:01->02 +0x1C4:21->00 +0x1D8:82->00 +0x1DB:20->00 +0x1EF:20->00 +0x1F1:00->01 +0x1F5:10->FF`.
- Continue treating Reraise/revive as state-machine evidence, not ordinary healing attribution.

Analyzer fix:

- `tools\analyze_immediate_action_boundary.py` now finds Rush KO semantically by looking for
  lethal damage with `rawForecastDamage=33`, `appliedHpLoss=15`, and `hpClamp=1`, instead of
  assuming a fixed event id. In this capture Rush KO is event `5`, not event `3`.

Updated next direction:

1. Start offline/static search around the `dmg1C4=33 -> HP zero / KO flags` boundary.
2. Look for a pre-commit damage value or engine-owned KO routine that can be reused for custom
   lethal damage.
3. Keep CT fallback as diagnostic-only. For the covered Rush case, memory freshness evidence is
   already better than CT.
4. At that point, do not attempt custom lethal damage until the pre-commit/KO path had a concrete
   hook or routine candidate. This condition is now satisfied enough for the controlled
   `ko-preclamp-force-agrias` proof at `0x30A66F`.

## 2026-06-23 Offline Result: KO Boundary Static Targets

After the action-boundary live validation, the static scan was rerun against the actual local
install:

- executable:
  `C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe`;
- generated report:
  `work\static_code_pattern_scan.local.md`;
- result: the local build matches known anchors:
  - `battle_base_ptr` at `0x226D98`;
  - `damage_mult_2` at `0x30A685`;
  - `jp_multiplier` at `0x283754`;
  - `xp_multiplier` at `0x283767`;
  - `min_spd_jmp_mov` at `0x36027F`;
  - the older direct `damage_multiplier` AOB is still absent from the static file.

New analyzer:

- `tools\analyze_ko_boundary_static_targets.py`;
- generated:
  `work\ko_boundary_static_target_analysis.md`.

The analyzer semantically selects the Rush KO event by:

- `rawForecastDamage=33`;
- `appliedHpLoss=15`;
- `lethal=1`;
- `hpClamp=1`.

It then extracts hook-register snapshots, stack addresses, module RVAs, static byte contexts, and
probable unit-field memory accesses near the live stack caller window.

Important live-to-static correlation:

- The hook snapshot attached to the execution cache, KO boundary, and HP event is identical.
- `rcx`, `rdi`, and `hookPtr` identify Ramza.
- `targetPtr` identifies the Ninja.
- Therefore the current hook snapshot is action-context evidence, not the exact HP write
  instruction.
- The useful live stack addresses are:
  - `0x1402F2EC1` / RVA `0x2F2EC1`;
  - `0x1402F37A2` / RVA `0x2F37A2`;
  - `0x1402F3884` / RVA `0x2F3884`.
- These sit below `damage_mult_2` and are best treated as action-resolution caller landmarks.

Static targets discovered near `damage_mult_2`:

- `0x30A6D3`: probable `rdi+0x1F5` write, `C6 87 F5 01 00 00 FF`;
- `0x30A908`: probable `rdi+0x61` write, `89 5F 61`;
- `0x30A912`: probable `rdi+0x1EF` write, `89 9F EF 01 00 00`;
- `0x30AAFC`: probable `rax+0x1BB` write, `C6 80 BB 01 00 00 02`;
- `0x30D42A`: probable `rdi+0x1EF` read, `8A 8F EF 01 00 00`;
- `0x30D433`: probable `rdi+0x1EF` mask/write, `88 8F EF 01 00 00`;
- `0x30D43C`: probable `rdi+0x61` write, `88 4F 61`;
- `0x2D7AC0` / `0x2D7AEC`: probable `rbx+0x1C4` target-cache writes.

Interpretation:

- The exact HP write/KO commit is still not proven.
- However, the static search now has concrete candidate RVAs around KO/death-state fields instead
  of a broad "search damage code" task.
- The `0x30A6D3 / 0x30A912 / 0x30D43C` cluster is the current highest-value static lead for
  understanding engine-owned KO state mutation.

Probe improvement deployed:

- `codemod\fftivc.generic.chronicle.codemod\Mod.cs` now stores the main module base/size and
  labels module addresses in hook-register snapshots as `module+0xRVA`.
- The probe now includes landmarks for:
  - the live KO stack RVAs;
  - `damage_mult_2`;
  - target-cache `+0x1C4` candidates;
  - KO/death-state field candidates near `damage_mult_2`.
- Deployed to:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod`;
- DLL timestamp after deploy:
  `2026-06-23 17:16:44`;
- active runtime settings were preserved.

Validation:

```text
python tools\analyze_ko_boundary_static_targets.py --exe "C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe" --output work\ko_boundary_static_target_analysis.md
python -m py_compile tools\analyze_ko_boundary_static_targets.py tools\scan_static_code_patterns.py
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj --no-restore
python tools\test_immediate_action_boundary.py
python tools\test_static_code_patterns.py
dotnet publish codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj -c Release -o "C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod" --no-restore
```

Historical next direction from this static pass:

1. Do not run another generic Cross Slash/Rush repetition just to see the same boundary again.
2. Use the next live run only to validate one of the static leads or to collect deeper stack/context
   around the `0x30A6D3`, `0x30A912`, `0x30D43C`, and `0x2D7AC0/0x2D7AEC` candidates. This led to
   the landmark and HP-apply probes that narrowed the current target to pre-clamp staged damage.
3. The best next engineering task is to decide whether these RVAs can be safely hooked or used as
   return-address classifiers to narrow the real pre-commit/KO routine.

## 2026-06-23 Implementation: KO Landmark Probe

Prepared the historical live probe for the autosave-before-Rush test.

New runtime feature:

- `LandmarkProbeEnabled`;
- `LandmarkProbeMaxLogs`;
- `LandmarkProbeStackSlots`;
- `LandmarkProbes[]`.

Behavior:

- Installs read-only asm hooks at configured module RVAs.
- Each hit writes register state into a native ring buffer.
- The polling thread emits `[LANDMARK-HIT ...]` only when the configured base register reads as a
  valid battle unit, avoiding log budget waste on unrelated non-unit hits.
- The hook hot path does not call managed code and does not write game state.
- Each configured RVA has `ExpectedBytes`; if the local executable bytes do not match, the hook is
  skipped and logged as `[LANDMARK-SKIP ...]`.

New profile:

- `work\battle-runtime-settings.ko-landmark-probe.json`;
- deployed active settings:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`.

Hooked landmarks:

- `0x2D7AC0`: `target-cache-write-1c4`, base `rbx`, expected `40 88 BB C4 01 00 00`;
- `0x2D7AEC`: `target-cache-init-1c4`, base `rbx`, expected `66 C7 83 C4 01 00 00 40 50`;
- `0x30A6D3`: `ko-write-1f5`, base `rdi`, expected `C6 87 F5 01 00 00 FF`;
- `0x30A908`: `ko-write-61`, base `rdi`, expected `89 5F 61`;
- `0x30A912`: `ko-write-1ef`, base `rdi`, expected `89 9F EF 01 00 00`;
- `0x30AAFC`: `death-state-write-1bb`, base `rax`, expected `C6 80 BB 01 00 00 02`;
- `0x30D42A`: `ko-read-1ef`, base `rdi`, expected `8A 8F EF 01 00 00`;
- `0x30D433`: `ko-mask-write-1ef`, base `rdi`, expected `88 8F EF 01 00 00`;
- `0x30D43C`: `ko-write-61-late`, base `rdi`, expected `88 4F 61`.

Important correction:

- The lightweight static decoder originally reported some overlapping starts:
  `0x30A911` and `0x30D432`.
- Direct byte inspection corrected the hookable starts to `0x30A912` and `0x30D433`.
- `0x30D43C` was confirmed as the correct start for `88 4F 61`.

Deployment:

- DLL deployed to:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.dll`;
- SHA256:
  `8CEB0F281E72A4B3AFC8F3EB68B4A3AF41F2A3FC4A6325AE37F56A6A4C4C3342`;
- active game log cleared:
  `C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt`.

Validation:

```text
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj --no-restore
python tools\test_runtime_profiles.py
python tools\test_static_code_patterns.py
python tools\test_immediate_action_boundary.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.ko-landmark-probe.json
```

Expected live test:

1. Load the autosave that starts before Ramza confirms Rush on the low-HP Ninja.
2. Wait 1-2 seconds and report `baseline autosave`.
3. Preview Rush on the Ninja and report the preview damage.
4. Confirm Rush.
5. Report KO/Reraise outcome.

Decision target:

- If `target-cache-*` hits on the Ninja before HP zero, they are candidates for the execution damage
  cache/pre-commit value.
- If `ko-write-*` hits on the Ninja at the KO frame, the static KO-field cluster is probably inside
  or immediately adjacent to engine-owned KO state mutation.

Next test plan:

Question:

- Do the byte-verified static RVAs actually execute on the low-HP Ninja during the Rush KO?
- If yes, which landmark happens before the `dmg1C4=33 -> HP zero / KO flags` boundary, and which
  happens as part of the engine-owned KO state mutation?

Why this is the right next test:

- Cross Slash attribution, Rush immediate-action freshness, raw-vs-applied lethal clamp, and the
  vanilla KO field diff are already proven.
- Repeating the full Cross Slash setup would mostly reproduce known evidence.
- The unsolved redesign blocker is still custom lethal damage: we need either a pre-commit damage
  interception point or a safe engine-owned KO path.
- The static pass produced concrete candidates; the live test now needs to validate those
  candidates, not rediscover the same HP event.

Setup:

- Use the autosave if it loads before Ramza confirms Rush.
- Required state:
  - Cross Slash has already left the Ninja at low HP;
  - Ninja is still alive;
  - Ramza can still preview/confirm Rush;
  - Rush has not already been confirmed.
- The active deployed profile is `ko-landmark-probe`, observe-only, with no HP/MP rewrites.

User steps:

1. Open the game through Reloaded.
2. Load the autosave.
3. Wait 1-2 seconds and report `baseline autosave`.
4. Preview Ramza `Rush` on the Ninja and report the preview damage.
5. Confirm Rush.
6. Report whether the Ninja died, the shown damage if visible, and whether Reraise activated.
7. Close the game and report that it is closed so the final log can be captured.

Artifacts to capture after the test:

- Active game log:
  `C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt`;
- copy to `work\live-captures\...ko-landmark...snapshot.txt`;
- analyze for:
  - `[LANDMARK-HOOK ...]` install success/skips;
  - `[LANDMARK-HIT ... target-cache-* ...]`;
  - `[LANDMARK-HIT ... ko-write-* ...]`;
  - `[ACTION-BOUNDARY]`;
  - `[HP-EVENT-PROBE]`;
  - `[IMMEDIATE-ACTION-CANDIDATES]`.

Pass/fail interpretation:

- Strong pass: one or more landmark hits show the Ninja as the base unit and bracket the KO
  boundary. This gives us a concrete hook family to inspect next.
- Partial pass: target-cache landmarks hit the Ninja but KO-field landmarks do not. Focus next on
  pre-commit/cache mutation and add deeper stack capture there.
- Partial pass: KO-field landmarks hit the Ninja but target-cache landmarks do not. Focus next on
  engine-owned KO routine/state mutation, not pre-commit damage.
- Negative result: no relevant landmark hits on the Ninja. Treat these static sites as adjacent or
  unrelated for this action; next work should use deeper stack return-address classifiers from
  `0x2F2EC1`, `0x2F37A2`, and `0x2F3884`.
- Crash on load/action: disable or bisect landmark probes by profile, because the hooks are too
  invasive at one of the candidate RVAs.

## 2026-06-23 Live Baseline: KO Landmark RVA Correction

First `baseline autosave` attempt with `ko-landmark-probe` did not proceed to Rush.

Captured snapshots:

- `work\live-captures\battleprobe_log.ko-landmark-baseline-autosave.snapshot.txt`;
- `work\live-captures\battleprobe_log.ko-landmark-baseline-autosave.bad-rva-skips.snapshot.txt`.

What happened:

- The profile loaded and the two target-cache hooks installed:
  - `0x2D7AC0`;
  - `0x2D7AEC`.
- The KO-field hooks were skipped by `ExpectedBytes` validation because several JSON `Rva` values
  had been entered with incorrect decimal conversions:
  - `0x30A6D3`;
  - `0x30A908`;
  - `0x30A912`;
  - `0x30AAFC`;
  - `0x30D42A`;
  - `0x30D433`;
  - `0x30D43C`.

Fix applied:

- Corrected decimal RVAs in `work\battle-runtime-settings.ko-landmark-probe.json`.
- Reinstalled the corrected settings to:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`.
- Cleared the active game log.

Important instruction:

- The first baseline is setup-validation evidence only.
- Do not use it to judge the static KO candidates.
- The game must be restarted through Reloaded so the corrected landmark hooks install at startup,
  then the autosave baseline should be captured again before previewing/confirming Rush.

## 2026-06-23 Live Test: Corrected KO Landmark Rush/Reraise

Validated run after restarting through Reloaded with corrected decimal RVAs.

Captured snapshots:

- `work\live-captures\battleprobe_log.ko-landmark-baseline-autosave.corrected-rvas.snapshot.txt`;
- `work\live-captures\battleprobe_log.ko-landmark-preview-ramza-rush-ninja-50.snapshot.txt`;
- `work\live-captures\battleprobe_log.ko-landmark-resolved-ramza-rush-ninja-ko-before-wait.snapshot.txt`;
- `work\live-captures\battleprobe_log.ko-landmark-after-ramza-wait-ninja-reraise.snapshot.txt`.

User-observed sequence:

1. Baseline autosave: Ramza active before Rush.
2. Preview: `Ramza Rush -> Ninja`, predicted damage `50`.
3. Resolution before Ramza Wait: Ninja died, shown number `33`, Ramza still active.
4. After Ramza Wait: next active Ninja, because Reraise revived him.

Probe validation:

- Corrected baseline: `9` landmark hooks installed, `0` skips, `0` hits before action.
- Preview: still `9` hooks installed, `0` skips, `0` landmark hits.
- KO resolution before Wait: `1` landmark hit.
- After Wait/Reraise: `3` total landmark hits.

KO hit before Wait:

- `[LANDMARK-HIT event=1 id=3 name=ko_write_1f5 rva=0x30A6D3]`;
- instruction writes byte `FF` to target `+0x1F5`;
- base unit was the Ninja at `0x141855EE0`;
- captured fields at the hit:
  - `hp=0`;
  - `ct=101`;
  - `dmg1C4=33`;
  - `chg1D8=130`;
  - `f1E5=128`;
  - `f1EF=32`;
  - `b8=0`, `ba=0`, `bb=1`;
  - raw `+0x30=0000`, `+0x1C4=2100`, `+0x1D8=8200`, `+0x1EF=2000`,
    `+0x1F5=1000`.

Interpretation:

- Preview does not execute these KO landmarks.
- The first KO landmark observed for the lethal Rush is `ko_write_1f5`.
- At that point HP is already zero and the displayed/applied damage slot has `33`.
- This makes `0x30A6D3` a strong engine-owned KO-state mutation landmark, but probably not the
  earliest pre-commit damage arithmetic site.
- `+0x1C4` continues to be the concrete shown/applied damage field for this immediate action.

Reraise/revive hits after Wait:

- `[LANDMARK-HIT event=2 id=3 name=ko_write_1f5 rva=0x30A6D3]`;
- `[LANDMARK-HIT event=3 id=6 name=death_state_write_1bb rva=0x30AAFC]`;
- both captured at the same timestamp with the Ninja revived:
  - `hp=28`;
  - `ct=1`;
  - `dmg1C4=0`;
  - `f1E5=72`;
  - `f1EF=0`;
  - `b8=1`, `ba=0`, `bb=2`;
  - raw `+0x30=1C00`, `+0x1BB=0211`, `+0x1F1=0100`, `+0x1F5=FF00`.

Interpretation:

- Reraise is a separate revive/state-machine transition, not ordinary healing evidence.
- `death_state_write_1bb` was not observed during the immediate pre-Wait KO snapshot, but did
  execute during the Reraise transition.
- `ko_write_1f5` fires both on KO and on revive/Reraise, so it is a lifecycle landmark, not a
  death-only marker.
- `+0x1BB`/`bb` remains a useful phase/death-state byte, but its semantics must be interpreted with
  the broader lifecycle (`1` during KO, `2` after Reraise) instead of as a simple dead/alive boolean.

Follow-up completed:

- This question led to the `ko-hp-apply-probe` and then to the pre-clamp staged-damage proof below.
- The current active experiment is no longer another passive KO landmark read. It is the one-shot
  `ko-preclamp-force-agrias` test, which changes staged damage before vanilla HP clamp and checks
  whether engine-owned KO lifecycle remains coherent.

## 2026-06-23 Offline Analysis: KO HP-Apply Lifecycle

New generated report:

- `work\ko_lifecycle_disassembly_analysis.md`;
- generator: `tools\analyze_ko_lifecycle_disasm.py`.

Python dependencies used locally:

- `capstone`;
- `pefile`.

Disassembly read:

- The live stack RVAs `0x2F3799`, `0x2F37A2`, `0x2F3884`, and `0x2F2EC1` are scheduler/dispatch
  frames.
- Static direct call/jump search found no direct call to `0x30A51C`; the scheduler uses an indirect
  `call qword ptr [rbx]`, consistent with the live stack.
- The state-apply routine around `0x30A51C` computes and applies HP/MP-like unit state.
- In the relevant HP path:
  - `0x30A673`: reads current HP from `rdi+0x30`;
  - `0x30A685..0x30A698`: computes signed HP delta, floors at zero, caps at max HP;
  - `0x30A6B6`: compares old HP with clamped HP;
  - `0x30A6C3`: writes clamped HP to `rdi+0x30`;
  - `0x30A6D3`: writes `FF` to `rdi+0x1F5`.

Important conclusion:

- `0x30A6D3` is after the HP write, so it is not the earliest damage/KO decision point.
- The live question for the now-completed `ko-hp-apply-probe` was whether KO/status state is already
  armed before the HP write.
- Two earlier instructions are now high-value:
  - `0x30A595`: tests `rdx+0x61` bit `0x20`;
  - `0x30A5C0`: writes `r13b` to `rdx+0x1BB`.
- If those fire before `0x30A6C3` with `+0x61=0x20` / `+0x1BB=1`, then vanilla has already
  entered a KO lifecycle before HP is committed.
- If not, an HP-write proof at `0x30A6C3` may be sufficient to make downstream engine code process
  custom lethal HP as normal KO.

Historical profile that was subsequently run:

- `work\battle-runtime-settings.ko-hp-apply-probe.json`;
- active deployed settings:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`;
- deployed settings SHA256:
  `B40A7DBB6E4DEF96B13C3340C3DCB1203E9E2460EC6F806C86F6745C0508A35B`.

Profile probes:

- `0x30A595`: `pre-death-status-test-61`, base `rdx`, expected `44 84 62 61`;
- `0x30A5C0`: `death-state-write-1bb-early`, base `rdx`, expected `44 88 AA BB 01 00 00`;
- `0x30A673`: `hp-read-current-30`, base `rdi`, expected `0F B7 57 30`;
- `0x30A68C`: `hp-raw-sum-test`, base `rdi`, expected `85 C0`;
- `0x30A6B6`: `hp-change-compare-old-new`, base `rdi`, expected `41 3B D7`;
- `0x30A6C3`: `hp-write-clamped-30`, base `rdi`, expected `66 44 89 7F 30`;
- `0x30A6D3`: `ko-write-1f5`, base `rdi`, expected `C6 87 F5 01 00 00 FF`;
- `0x30AAFC`: `reraise-death-state-write-1bb`, base `rax`, expected
  `C6 80 BB 01 00 00 02`.

Validation:

```text
python tools\analyze_ko_lifecycle_disasm.py
python -m json.tool work\battle-runtime-settings.ko-hp-apply-probe.json
python tools\report_runtime_profiles.py
python tools\test_runtime_profiles.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.ko-hp-apply-probe.json
```

Additional byte validation against the installed `FFT_enhanced.exe` passed for all eight profile
landmarks.

Live test that was subsequently run:

1. Open the game through Reloaded after the new profile is active.
2. Load the same autosave before Ramza Rush on the low-HP Ninja.
3. Wait 1-2 seconds and report `baseline hp-apply autosave`.
4. Preview Ramza `Rush` on the Ninja and report preview damage.
5. Confirm Rush.
6. Before Ramza Wait, report whether the Ninja died and the shown number.
7. Finish Ramza Wait and report whether Reraise makes Ninja active.

Decision target:

- The key ordering is:
  - `pre-death-status-test-61`;
  - `death-state-write-1bb-early`;
  - `hp-read-current-30`;
  - `hp-raw-sum-test`;
  - `hp-write-clamped-30`;
  - `ko-write-1f5`.
- If the first two already show KO state before `hp-write-clamped-30`, the custom lethal proof
  should not rely on HP write alone.
- If KO state appears only after or because of `hp-write-clamped-30`, the next proof can target a
  controlled custom HP-write hook at or before `0x30A6C3`.

## 2026-06-23 Live Test: KO HP-Apply Rush/Reraise Readout

Validated run with `work\battle-runtime-settings.ko-hp-apply-probe.json`.

Captured snapshots:

- `work\live-captures\battleprobe_log.ko-hp-apply-baseline-autosave.snapshot.txt`;
- `work\live-captures\battleprobe_log.ko-hp-apply-baseline-after-correct-load.mixed-with-wrong-load.snapshot.txt`;
- `work\live-captures\battleprobe_log.ko-hp-apply-preview-ramza-rush-ninja-50.snapshot.txt`;
- `work\live-captures\battleprobe_log.ko-hp-apply-resolved-ramza-rush-ninja-ko-before-wait.snapshot.txt`;
- `work\live-captures\battleprobe_log.ko-hp-apply-after-ramza-wait-ninja-reraise.snapshot.txt`.

Important capture note:

- The first autosave baseline includes an initial wrong-load/post-Reraise state, then the correct
  pre-Reraise autosave. The useful live evidence is the isolated preview, resolution, and post-Wait
  snapshots.

User-observed sequence:

1. Correct baseline: Ramza active, Rush available, pre-Reraise.
2. Preview: `Ramza Rush -> Ninja`, predicted damage `50`.
3. Resolution before Ramza Wait: Ninja died, shown number `16`, Ramza still active.
4. After Ramza Wait: next active Ninja, Reraise revived him, visible HP `28`.

Probe validation:

- Runtime installed all `8/8` configured landmark hooks.
- Preview produced `0` landmark hits, confirming these landmarks are resolution/apply-only.
- Resolution before Wait produced `9` landmark hits, with `0` lost hits.
- Post-Wait/Reraise snapshot contains `17` total landmark hits, with the Reraise landmark at event
  `17`.

KO resolution facts on the Ninja target (`ptr=0x141855EE0`, `id=0x80`):

- Events `1..7` are the lethal Rush state apply on the Ninja.
- Important timing note: landmark registers are captured immediately by the assembly hook, but
  `baseRead`, `fields=`, and `raw=` are read later by the poller while formatting
  `[LANDMARK-HIT]`. Treat those field snapshots as near-event state, not guaranteed
  pre-instruction memory.
- By the time the poller formatted `event=1`, the target snapshot showed KO-like state:
  - `hp=0`;
  - `ct=101`;
  - `s61=32`;
  - `dmg1C4=16`;
  - `chg1D8=130`;
  - `f1E5=128`;
  - `f1EF=32`;
  - `b8=0`, `ba=0`, `bb=1`;
  - raw `+0x61=2000`, `+0x1BB=0111`, `+0x1C4=1000`, `+0x1D8=8200`,
    `+0x1EF=2000`, `+0x1F1=0000`, `+0x1F5=1000`.
- `event=1` (`0x30A595`) exactly captured `r12=0x20`, `r13=0x1`, and target base in `rdx`.
- `event=2` (`0x30A5C0`) writes `r13b` to `+0x1BB`, but the captured state already had
  `+0x1BB=01` by the time the poller read the unit snapshot.
- `event=4` (`0x30A68C`) captured the actual HP arithmetic:
  - `rdx=0xF`: old HP `15`;
  - `rax=0xFFFFFFFF`: raw signed result `-1`;
  - `r15=0x120`: max HP `288` before clamp completion.
- `event=5` (`0x30A6B6`) captured:
  - `rdx=0xF`: old HP `15`;
  - `r15=0`: clamped new HP `0`.
- `event=6` (`0x30A6C3`) writes clamped HP `0` to `+0x30`.
- `event=7` (`0x30A6D3`) is the post-HP-write lifecycle mark at `+0x1F5`.

Ramza-side events:

- Events `8..9` are on Ramza (`ptr=0x141855CE0`, `id=0x03`), not the KO target.
- They show the same `0x30A595`/`0x30A5C0` pair can run for actor/action lifecycle state, so live
  analysis must key these events by base unit pointer and not by instruction alone.

Reraise facts after Ramza Wait:

- Events `10..17` are the revive/Reraise lifecycle on the Ninja after Wait.
- The revived state matches the user's visible HP:
  - `hp=28`;
  - `ct=1`;
  - `s61=0`;
  - `dmg1C4=0`;
  - `f1E5=72`;
  - `f1EF=0`;
  - `b8=1`, `ba=0`, `bb=2`;
  - raw `+0x30=1C00`, `+0x1BB=0211`, `+0x1F1=0100`, `+0x1F5=FF00`.
- `event=17` hit `reraise_death_state_write_1bb` at `0x30AAFC` with base `rax` pointing to the
  Ninja and `+0x1BB=02`.

Interpretation:

- The HP apply routine is downstream of vanilla damage staging, but it is the first proven
  engine-owned clamp/write site for lethal HP application.
- HP write alone at `0x30A6C3` is probably too late for a robust custom-lethal design because it
  bypasses part of the state-apply lifecycle.
- The arithmetic inside this routine is valuable because it proves how the vanilla side buffer turns
  old HP `15` plus delta `-16` into clamped HP `0`.
- The ideal long-term static target remains the producer of the staged damage debit/credit before HP
  math: whatever writes the real `unit+0x1C4` / `unit+0x1C6` values consumed by this routine.
- For the next live test, we are deliberately bypassing that producer search with a one-shot
  pre-clamp proof. This is the most useful experiment now because it directly tests whether the
  final custom formula system can feed the engine at the staged-damage boundary and let vanilla
  perform HP zero, KO lifecycle, Reraise, and turn flow.

Next experiment implemented below:

- `ko-preclamp-force-agrias`: force Agrias's staged Cross Slash debit from `187` to `9999` at
  `0x30A66F`, before vanilla reads `unit+0x1C4`.

## 2026-06-23 Offline Follow-up: HP Apply Dataflow Recalibration

The regenerated `work\ko_lifecycle_disassembly_analysis.md` now reflects the post-live static
findings.

New static facts:

- `0x30A51C` is the state-apply routine entry. It takes a unit index, derives the live battle-unit
  pointer, and stores current global pointers around `0x186AF68` / `0x186AF70`.
- The state buffer consumed by the HP math is not an external side buffer. It is the tail of the same
  unit struct: `rbp = unit + 0x1BE`.
- Therefore the HP math consumes:
  - `unit+0x1C4` as signed HP debit;
  - `unit+0x1C6` as signed HP credit;
  - `unit+0x30` as old HP;
  - `unit+0x32` as max HP.
- The formula at the consumption point is:
  `newRawHp = oldHp + s16[unit+0x1C6] - s16[unit+0x1C4]`, then floor at `0`, cap at max HP, write
  clamped HP to `unit+0x30`.
- For the live Rush KO, the exact registers prove old HP `15`, staged debit `16`, staged credit `0`,
  raw result `-1`, and clamped result `0`.
- For Reraise, the later state is consistent with staged debit `0`, staged credit `28`, old HP `0`,
  and restored HP `28`.
- `0x30A908` / `0x30A912` are cleanup/clear writes in the post-HP status tail, not the KO-arm
  producer.
- `0x2D7AC0` / `0x2D7AEC` are likely writes in a separate `0x248`-stride target/cache table, not the
  direct live unit-tail producer consumed by `0x30A51C`.

Recalibrated next objective:

Find or bypass the producer of the staged debit/credit fields. The best next proof is probably not a
late `+0x30` HP rewrite. It is a controlled pre-clamp intervention that changes `unit+0x1C4` before
`0x30A68C`, then observes whether vanilla downstream logic performs HP zero, KO flags, displayed
number, Reraise, and turn flow correctly.

## 2026-06-23 Implementation: Pre-Clamp Staged-Damage Proof

Prepared and deployed the next live proof profile:

- source profile: `work\battle-runtime-settings.ko-preclamp-force-agrias.json`;
- installed profile:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`;
- previous installed settings backup:
  `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json.bak-20260623-193204`.

Code changes:

- Added `PreClampDamageRewriteEnabled` runtime support in the code mod.
- The hook installs at `0x30A66F`, before vanilla executes `movsx eax, word ptr [rbp+6]`.
- At that point:
  - `rdi` is the unit pointer;
  - `rbp = unit + 0x1BE`;
  - `[rbp+6]` is `unit+0x1C4`, the staged HP debit;
  - `[rbp+8]` is `unit+0x1C6`, the staged HP credit.
- The hook is one-shot and guarded by:
  - target char id `0x1E` (Agrias);
  - old staged debit exactly `187`;
  - old staged credit exactly `0`;
  - current HP at least `188`;
  - expected bytes `0F BF 45 06`.
- On match, it writes:
  - `unit+0x1C4 = 9999`;
  - `unit+0x1C6 = 0`;
  before vanilla HP clamp reads the debit.
- It emits `[PRECLAMP-REWRITE ...]` with exact hook-time `hp`, old debit/credit, forced
  debit/credit, unit pointer, and state pointer.

Why this test matters:

- It tests the practical custom-lethal architecture we actually want: custom damage feeds the same
  staged damage input that vanilla already consumes.
- If Agrias dies from a normally nonlethal Cross Slash, and the downstream log shows vanilla HP
  clamp, KO lifecycle, displayed damage behavior, and turn flow are coherent, then we can stop
  treating late `+0x30` HP writes as the primary design path.
- If she only reaches HP zero without full KO lifecycle, then we still need the earlier KO-state
  producer or a minimal lifecycle write set.

Why it makes sense now relative to the final combat redesign:

- The final system needs `custom formula -> engine-safe application`, not just `custom formula ->
  visible HP number`.
- We already proved pending-action context can identify Cloud/Cross Slash/target batches, and we
  already proved the HP apply routine consumes staged debit/credit from `unit+0x1C4` / `unit+0x1C6`.
- Searching for the original producer is still useful, but it can be slow. This proof answers the
  more important architectural question first: if a custom formula supplies a lethal staged debit at
  the consumption point, will the engine complete KO correctly?
- A pass would move lethal custom formulas from "needs unknown KO trigger" to "can use pre-clamp
  staged damage injection"; a fail would tell us to prioritize the KO-state producer/minimal
  lifecycle write set before building more formula families.

Historical live test instructions below were superseded by the recalibrated `115` proof in the next
section. Keep them only as context for why the proof was built.

Next live test instructions:

1. Launch the game through Reloaded-II with `Generic Chronicle (Battle Probe)` enabled.
2. Load the Cloud-active baseline, not the Ramza/Ninja KO autosave.
3. Confirm Cloud is active.
4. Select `Cross Slash` on Agrias and verify the preview damage is `187`.
5. Before confirming, tell the log checkpoint as: `baseline preclamp Cloud ativo`.
6. Then tell the preview as: `preview preclamp: Cross Slash na Agrias, dano 187`.
7. Confirm the action.
8. Report exactly:
   - whether Agrias died;
   - what damage number was shown on Agrias;
   - whether Ninja was also hit normally;
   - who is the next active unit after Cloud's Wait.

Expected log evidence:

- `[PRECLAMP-REWRITE ... id=0x1E ... oldDebit=187 oldCredit=0 forcedDebit=9999 forcedCredit=0]`;
- at `0x30A68C`, exact registers should show old HP in `rdx` and raw signed HP result below zero;
- at `0x30A6B6`, `r15=0`;
- at `0x30A6C3`, vanilla writes clamped HP zero;
- HP event/action logs should show whether the displayed/applied damage uses the forced staged debit,
  the clamped HP loss, or another display-only value.

## 2026-06-23 Live Test: Pre-Clamp Staged-Damage Proof Passed

The first prepared profile expected Cross Slash on Agrias to preview `187`, but the actual loaded
Cloud-active baseline previewed `115`. That first preview was not confirmed. The profile was
recalibrated to:

- `PreClampDamageRewriteExpectedDebit=115`;
- `PreClampDamageRewriteMinHp=116`;
- `PreClampDamageRewriteForcedDebit=9999`;
- target char id `0x1E` (Agrias).

Captured artifacts:

- `work\live-captures\battleprobe_log.preclamp-baseline-cloud-not-active.invalid.snapshot.txt`
  - invalid first baseline, captured before Cloud was actually active;
- `work\live-captures\battleprobe_log.preclamp-baseline-cloud-active.snapshot.txt`
  - first Cloud-active baseline before recalibration;
- `work\live-captures\battleprobe_log.preclamp-preview-cross-slash-agrias-115.snapshot.txt`
  - preview proving the current baseline was `115`, not `187`;
- `work\live-captures\battleprobe_log.preclamp115-baseline-cloud-active.snapshot.txt`;
- `work\live-captures\battleprobe_log.preclamp115-preview-cross-slash-agrias-115.snapshot.txt`;
- `work\live-captures\battleprobe_log.preclamp115-resolved-cross-slash-agrias-ko.snapshot.txt`;
- `work\battleprobe_analysis.preclamp115-agrias-ko.md`.

User-observed result:

- Cloud used Cross Slash on Agrias.
- Agrias died.
- UI damage shown on Agrias: `999`.
- Ninja was also hit normally for `273`.
- After Cloud's Wait and resolution, active unit was Ramza.

Key proof lines:

```text
[PRECLAMP-REWRITE-HOOK] ... targetId=0x1E expectedDebit=115 expectedCredit=0 forcedDebit=9999 forcedCredit=0
[PRECLAMP-REWRITE event=1 ptr=0x1418560E0 ... id=0x1E hp=322/322 oldDebit=115 oldCredit=0 forcedDebit=9999 forcedCredit=0 ... live=hp=0 ... dmg1C4=9999 ...]
[LANDMARK-HIT event=19 ... hp_raw_sum_test ... rax=0xFFFFDA33 ... rdx=0x142 ... r15=0x142]
[LANDMARK-HIT event=20 ... hp_change_compare_old_new ... rdx=0x142 ... r15=0x0]
[LANDMARK-HIT event=21 ... hp_write_clamped_30 ... r15=0x0]
[DEATH-DIFF ptr=0x1418560E0 id=0x1E] alive->dead +0x30:42->00 +0x31:01->00 +0x61:00->20 +0x18C:00->01 +0x1BB:00->01 +0x1C4:73->0F +0x1C5:00->27 +0x1DB:00->20 +0x1EF:00->20 +0x1F5:FF->13
[HP-EVENT-PROBE kind=damage event=1 ... prevHp=322 currentHp=0 ... rawForecastDamage=9999 lethal=1 hpClamp=1 rawForecastOverkill=9677 ...]
[PENDING-ACTION-MATCH kind=damage event=1 ... resolved=0x1418562E0/id=0x32 source=pending-clear batch=1 act=258 ... confidence=damage-cache-lethal-clamp ...]
```

Interpretation:

- The pre-clamp staged-damage injection path works for a controlled lethal proof.
- Writing `unit+0x1C4=9999` before vanilla reads the staged HP debit caused the vanilla HP-apply
  routine to compute raw HP `322 - 9999 = -9677`, clamp HP to `0`, and run coherent KO/lifecycle
  state changes.
- This is qualitatively different from the refuted direct `unit+0x30=0` path. The engine, not the
  codemod, performed the final HP clamp and KO lifecycle.
- The UI displayed `999`, not `9999`. Treat this as likely display clamp / presentation behavior.
  The memory proof still shows staged debit `9999`, HP clamp to zero, and real KO.
- The same AoE batch left Ninja's normal result intact: Ninja took `273` and did not get the forced
  Agrias-only rewrite.
- Pending action context also held during the custom lethal case:
  - Agrias event resolved to Cloud/Cross Slash via `source=pending-clear`, `act=258`,
    `confidence=damage-cache-lethal-clamp`;
  - Ninja event resolved to the same batch with exact `damage-cache` confidence.

Decision unlocked:

- The final custom formula architecture can target staged damage injection before vanilla HP apply,
  instead of relying on late HP rewrites for lethal damage.
- Next implementation work should generalize this proof:
  1. compute custom damage from the resolved formula context;
  2. write custom staged debit/credit at the pre-clamp hook for matching targets/events;
  3. let vanilla HP apply handle clamp, KO, Reraise, and turn-flow lifecycle;
  4. keep late HP rewrite as a fallback/legacy path for nonlethal or unresolved cases only until
     the staged path covers the needed action families.

Open follow-up:

- Determine whether the UI damage display should intentionally use the full staged debit, the
  clamped HP loss, or a custom display path. This proof only established that the engine-safe lethal
  application path works.

## 2026-06-23 Offline Update: Formula Candidate Probe For Pre-Clamp Plans

After the staged-damage proof passed, the next technical question became: can the managed runtime
build a custom formula result early enough to feed the pre-clamp hook, rather than only observing the
HP event after vanilla has already applied it?

Implemented low-risk pieces:

- `BattleFormulaEngine.EvaluateForStagedApply(DamageEvent)`:
  - runs the same formula/context/action-signal pipeline as `Evaluate`;
  - bypasses only the late-rewrite enable gate (`RewriteObservedDamage` / `RewriteObservedHealing`);
  - keeps faction gates, action-signal rules, DR, response rules, rewrite condition, and final damage
    formula behavior intact;
  - lets profiles keep late HP writes disabled while still calculating the damage that a staged
    pre-clamp plan would write.
- `PendingActionTracker.MatchTargetCache(...)`:
  - matches the active resolving pending-action batch against a target's live `+0x1C4` damage cache;
  - does **not** consume/increment the batch event count;
  - therefore can be used as a planning probe before the real HP event consumes the batch.
- `LogPreClampFormulaCandidates` runtime setting:
  - when enabled, the runtime watches target-cache damage (`unit+0x1C4`) and pending-action batches;
  - if a cache-backed pending match exists, it builds a synthetic damage event:
    `previousHp=target.hp`, `vanillaDamage=target.dmg1C4`, `attacker=pending caster`;
  - it evaluates the configured formula in staged mode and logs:
    `[PRECLAMP-FORMULA-CANDIDATE ... oldDebit=... forcedDebit=... attacker=... action=...]`;
  - it is observe-only. It does not install the pre-clamp rewrite hook and does not write HP.
- New live-observe profile:
  - `work\battle-runtime-settings.preclamp-formula-candidates.json`;
  - formula: `max(1, a.pa * 10 - t.faith)`;
  - condition: `event.isDamage && a.present`;
  - late HP rewrites remain disabled.
- Deployment status at this checkpoint:
  - deployed to `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\`;
  - active runtime settings copied from
    `work\battle-runtime-settings.preclamp-formula-candidates.json`;
  - Reloaded-II mod enablement JSON was not edited.

Why this step matters:

- The proof showed that a native pre-clamp staged debit can cause real vanilla KO.
- The formula engine previously calculated only after an HP delta was observed.
- The new candidate probe tests whether the runtime can resolve `caster + action + target + oldDebit`
  during the target-cache window, before the HP write, which is the missing bridge between
  "custom formula exists" and "custom formula feeds vanilla HP apply."

Expected next live evidence:

```text
[PENDING-ACTION-MATCH kind=preclamp-cache ... consume=0 ... resolved=<Cloud> ... act=258 ...]
[PRECLAMP-FORMULA-CANDIDATE ... oldDebit=115 ... forcedDebit=<formula result> shouldStage=1 ... attacker=<Cloud> source=pending-clear ...]
[PRECLAMP-FORMULA-RUNTIME ... final=<same formula result>:FinalDamageFormula]
```

If those lines appear before the subsequent `[DAMAGE]` / `[HP-EVENT-PROBE]` for the same target,
then the managed runtime is seeing enough information early enough to enqueue a native rewrite plan.
If they appear only after the HP event or do not appear, the plan table will need an earlier producer
hook near the target-cache write instead of relying only on polling.

## 2026-06-23 Live Test: Pre-Clamp Formula Candidate Probe Passed

Profile used:

- `work\battle-runtime-settings.preclamp-formula-candidates.json`;
- observe-only, no HP rewrite and no pre-clamp native rewrite hook;
- formula: `max(1, a.pa * 10 - t.faith)`;
- user action: Cloud confirmed Cross Slash AoE on Agrias, waited through Beowulf/Agrias/Ninja, then
  Cross Slash resolved.

Captured artifacts:

- `work\live-captures\battleprobe_log.preclamp-candidates-baseline-cloud-active.snapshot.txt`;
- `work\live-captures\battleprobe_log.preclamp-candidates-preview-cross-slash-agrias-115.snapshot.txt`;
- `work\live-captures\battleprobe_log.preclamp-candidates-confirmed-cross-slash-cloud-active.snapshot.txt`;
- `work\live-captures\battleprobe_log.preclamp-candidates-pending-beowulf.snapshot.txt`;
- `work\live-captures\battleprobe_log.preclamp-candidates-pending-agrias.snapshot.txt`;
- `work\live-captures\battleprobe_log.preclamp-candidates-pending-ninja.snapshot.txt`;
- `work\live-captures\battleprobe_log.preclamp-candidates-resolved-cross-slash-aoe.snapshot.txt`;
- `work\battleprobe_analysis.preclamp-candidates-cross-slash-aoe.md`.

User-observed result:

- Agrias lost `115` HP;
- Ninja lost `273` HP;
- active unit after resolution: Ramza.

Important timing:

```text
[PENDING-ACTION-TRACK resolve-open batch=1 caster=0x1418562E0/id=0x32 act=258 ...]
[PRECLAMP-FORMULA-CANDIDATE ... ptr=0x141855EE0 id=0x80 hp=276/276 oldDebit=273 forcedDebit=68 ... attacker=0x1418562E0/id=0x32 source=pending-clear ...]
[PRECLAMP-FORMULA-RUNTIME ptr=0x141855EE0 id=0x80] ... final=68:FinalDamageFormula
[PRECLAMP-FORMULA-CANDIDATE ... ptr=0x1418560E0 id=0x1E hp=322/322 oldDebit=115 forcedDebit=77 ... attacker=0x1418562E0/id=0x32 source=pending-clear ...]
[PRECLAMP-FORMULA-RUNTIME ptr=0x1418560E0 id=0x1E] ... final=77:FinalDamageFormula
[DAMAGE ptr=0x141855EE0 id=0x80] 276 -> 3 = 273
[DAMAGE ptr=0x1418560E0 id=0x1E] 322 -> 207 = 115
```

Interpretation:

- The runtime can resolve delayed/AoE action context before the HP event:
  - caster: Cloud (`0x32`);
  - action id: `258` (Cross Slash);
  - source: `pending-clear`;
  - per-target old staged damage: Ninja `273`, Agrias `115`;
  - computed formula result: Ninja `68`, Agrias `77`.
- The first candidate for both targets appeared with pre-damage HP:
  - Ninja `hp=276/276`;
  - Agrias `hp=322/322`.
- Therefore a managed producer can enqueue a native pre-clamp rewrite plan in time for this delayed
  AoE case, as long as the native hook consumes the plan at the HP apply read.
- The preview cache does not stay live for the whole charge:
  - Agrias preview cache `+0x1C4=115` appeared before confirmation;
  - it cleared when Agrias became active;
  - it was written again when Cross Slash resolved.
  This means the real plan should be keyed to the resolving batch/target-cache rewrite near
  execution, not to the original preview cache alone.
- Duplicate candidate logs can appear after one target has already taken damage because the target
  cache remains live for a short period. The plan table must be one-shot per target/debit/action
  and should ignore already-consumed slots.

Decision unlocked:

- Implement the native pre-clamp plan table:
  1. managed runtime watches `PRECLAMP-FORMULA-CANDIDATE` conditions;
  2. when `shouldStage=1`, enqueue a plan keyed by target pointer + old debit/credit + batch/action;
  3. pre-clamp hook scans active plans, rewrites `[rbp+6]` / `[rbp+8]`, records a hit, and consumes
     the slot;
  4. vanilla HP apply handles clamp/KO/lifecycle.

Next proof after implementation:

- same Cross Slash AoE scenario, formula active:
  - expected vanilla old debits: Ninja `273`, Agrias `115`;
  - expected staged formula debits: Ninja `68`, Agrias `77`;
  - user should observe HP loss near `68` and `77` if the plan table applies before vanilla HP write.

## 2026-06-23 Offline Update: Native Pre-Clamp Plan Table Implemented

Implemented after the candidate probe passed:

- Added a native plan table to the pre-clamp rewrite buffer:
  - max supported slots: `32`;
  - each slot stores target pointer, expected HP/maxHP, expected staged debit/credit, forced
    debit/credit, action id, batch id, creation tick, write count, max writes, and flags;
  - managed code writes all fields while the slot is inactive, then marks `PLAN_ACTIVE=1` last;
  - the ASM hook scans active plan slots before the legacy static forced proof path.
- Pre-clamp hook behavior now has two paths:
  1. plan path:
     - match `rdi` target pointer;
     - match current `unit+0x30` / `unit+0x32` expected HP and max HP;
     - match `[rbp+6]` old debit and `[rbp+8]` old credit;
     - record `[PRECLAMP-REWRITE ... flags=0xE action=<id>]`;
     - write forced debit/credit;
     - deactivate the slot;
  2. legacy static proof path:
     - preserved for `ko-preclamp-force-agrias`;
     - now uses a separate static write counter instead of reusing the event ring sequence counter.
- `LogPreClampFormulaCandidateIfEnabled` now optionally queues a plan:
  - setting: `PreClampFormulaPlanEnabled`;
  - setting: `PreClampFormulaPlanRequirePhaseZero` defaults true;
  - this avoids queuing duplicate post-HP candidates where `bb=2`;
  - queue log: `[PRECLAMP-PLAN-QUEUE ...]`.
- Added runtime settings:
  - `PreClampFormulaPlanEnabled`;
  - `PreClampFormulaPlanSlots`;
  - `PreClampFormulaPlanWindowMs`;
  - `PreClampFormulaPlanMaxWrites`;
  - `PreClampFormulaPlanRequirePhaseZero`.
- Added live-apply profile:
  - `work\battle-runtime-settings.preclamp-plan-cross-slash-demo.json`;
  - formula: `max(1, a.pa * 10 - t.faith)`;
  - expected Cross Slash AoE result in the known baseline:
    - Ninja old `273` -> forced `68`;
    - Agrias old `115` -> forced `77`.

Validation:

- `dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj`
  passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj`
  passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -- work\battle-runtime-settings.preclamp-plan-cross-slash-demo.json`
  passed.
- `codemod\run-offline-checks.ps1` passed.

Deployment status:

- Deployed to `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\`.
- Active runtime settings copied from:
  `work\battle-runtime-settings.preclamp-plan-cross-slash-demo.json`.
- Reloaded-II mod enablement JSON was not edited.

Next live test:

- Enable only `fftivc.utility.modloader` and `fftivc.generic.chronicle.codemod`.
- Keep the data mod disabled.
- Repeat Cloud Cross Slash AoE on Agrias/Ninja.
- Expected user-visible HP losses if the plan table applies:
  - Agrias: `77` instead of `115`;
  - Ninja: `68` instead of `273`.
- Expected log evidence:
  - `[PRECLAMP-PLAN-QUEUE ... oldDebit=273 ... forcedDebit=68 ...]`;
  - `[PRECLAMP-REWRITE ... oldDebit=273 ... forcedDebit=68 ... flags=0xE action=258]`;
  - `[PRECLAMP-PLAN-QUEUE ... oldDebit=115 ... forcedDebit=77 ...]`;
  - `[PRECLAMP-REWRITE ... oldDebit=115 ... forcedDebit=77 ... flags=0xE action=258]`;
  - HP events should show the forced losses if the hook wins the timing race.

## 2026-06-23 Live Result: Native Pre-Clamp Plan Table Passed

Profile:

- `work\battle-runtime-settings.preclamp-plan-cross-slash-demo.json`
- Formula: `max(1, a.pa * 10 - t.faith)`
- Data mod disabled.
- Active mods: `fftivc.utility.modloader`, `fftivc.generic.chronicle.codemod`.

Scenario:

- Cloud confirmed delayed Cross Slash AoE on Agrias.
- Beowulf, Agrias, and Ninja waited while the action remained pending.
- Cross Slash then resolved against Ninja and Agrias.

User-visible result:

- Agrias UI damage: `77`; HP `322 -> 245`.
- Ninja UI damage: `68`; HP `276 -> 208`.
- Next active unit: Ramza.

Expected vanilla values in this baseline:

- Agrias: `115`.
- Ninja: `273`.

Log artifact:

- `work\live-captures\battleprobe_log.preclamp-plan-post-cross-slash-success.snapshot.txt`

Critical log evidence:

```text
[PRECLAMP-PLAN-QUEUE ... id=0x80 hp=276/276 oldDebit=273 forcedDebit=68 ... pending=batch=1/act=258]
[PRECLAMP-PLAN-QUEUE ... id=0x1E hp=322/322 oldDebit=115 forcedDebit=77 ... pending=batch=1/act=258]
[PRECLAMP-REWRITE ... id=0x80 ... oldDebit=273 ... forcedDebit=68 ... action=258 ... live=hp=208 ... dmg1C4=68 ...]
[PRECLAMP-REWRITE ... id=0x1E ... oldDebit=115 ... forcedDebit=77 ... action=258 ... live=hp=245 ... dmg1C4=77 ...]
[DAMAGE ptr=0x141855EE0 id=0x80] 276 -> 208 = 68
[DAMAGE ptr=0x1418560E0 id=0x1E] 322 -> 245 = 77
```

Conclusion:

- The pending-action tracker resolved the delayed action context correctly:
  Cloud `id=0x32`, action `258`, source `pending-clear`.
- The managed runtime computed formula-backed debits before the vanilla HP apply.
- The native plan table reached the HP-apply hook in time.
- The hook rewrote the engine's staged damage before UI/final HP application.
- The user-visible floating damage numbers and final HP changes both reflected the custom formula.

This is the first live proof that a delayed AoE action can be converted from vanilla damage to a
formula-owned result through the engine's own damage application path, without late HP writes and
without relying on CT as the primary attacker source for the final damage event.

Immediate implication:

- The architecture can move from "late observed HP rewrite with engine-owned death fallback" toward
  "pre-clamp staged result rewrite with engine-owned application/death."
- CT should remain a fallback/diagnostic signal, while pending-action/action-memory context should
  become the preferred source for delayed actions.

Next recommended tests:

- Immediate single-target attack through the same pre-clamp plan path.
- Lethal custom formula through the pre-clamp plan path, to verify same-hit KO is now possible.
- A charged single-target action and a different AoE action family, to validate action tracking
  beyond Cloud Limit/Cross Slash.
- Multiple simultaneous pending actions, to stress batch matching by action id, damage cache, and
  target metadata.

## 2026-06-23 Prepared Next Live Test: Formula Lethal Braver

Prepared and deployed:

- `work\battle-runtime-settings.preclamp-plan-lethal-braver-demo.json`

Deployment:

- `codemod\build-deploy.ps1 -RuntimeSettings work\battle-runtime-settings.preclamp-plan-lethal-braver-demo.json`
- Installed to `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`.
- Reloaded-II mod enablement JSON was not edited.

Purpose:

- Combine the two prior proofs:
  1. formula-backed native pre-clamp plan table can replace vanilla staged damage;
  2. lethal staged damage consumed by vanilla HP apply produces real same-hit KO.
- Use a delayed single-target action rather than Cross Slash AoE, so this also tests a second Cloud
  Limit action family and avoids multi-target ambiguity.

Profile behavior:

- `FinalDamageFormula="9999"`
- late observed HP rewrites disabled;
- pre-clamp plan table enabled;
- `CaptureStructOnDeath=true`;
- intended action: Cloud Braver on Beowulf.

Expected pass:

- On resolution, Beowulf should take lethal damage and die in the same hit.
- The UI may clamp the displayed damage, likely around `999`, but HP/KO state is the real proof.
- Expected logs:
  - `[PENDING-ACTION-TRACK ... act=<Braver action id> ...]`;
  - `[PRECLAMP-PLAN-QUEUE ... oldDebit=<vanilla Braver> forcedDebit=9999 ...]`;
  - `[PRECLAMP-REWRITE ... forcedDebit=9999 ...]`;
  - `[DEATH-DIFF ... id=0x1F ... +0x61:00->20 ...]` or equivalent KO lifecycle;
  - final HP event showing Beowulf HP reaches `0`.

## 2026-06-23 Live Result: Formula Lethal Braver Passed

Profile:

- `work\battle-runtime-settings.preclamp-plan-lethal-braver-demo.json`
- Formula: `9999`
- Data mod disabled.
- Active mods: `fftivc.utility.modloader`, `fftivc.generic.chronicle.codemod`.

Scenario:

- Cloud selected Braver on Beowulf.
- Preview showed vanilla damage `153`.
- User confirmed Braver, waited Cloud, then waited Beowulf.
- Braver resolved before Agrias became active.

User-visible result:

- UI damage: `999`.
- Beowulf died: yes.
- Beowulf HP: `0/314`.
- Next active unit: Agrias.

Log artifact:

- `work\live-captures\battleprobe_log.preclamp-plan-lethal-braver-success.snapshot.txt`

Critical log evidence:

```text
[PENDING-ACTION-TRACK ... caster=0x1418562E0/id=0x32 act=257 ...]
[PENDING-ACTION-TRACK resolve-open batch=1 caster=0x1418562E0/id=0x32 act=257 ...]
[PRECLAMP-PLAN-QUEUE ... id=0x1F hp=314/314 oldDebit=153 forcedDebit=9999 ... pending=batch=1/act=257]
[PRECLAMP-FORMULA-CANDIDATE ... id=0x1F hp=314/314 oldDebit=153 forcedDebit=9999 ... attacker=0x1418562E0/id=0x32 source=pending-clear ...]
[PRECLAMP-REWRITE ... id=0x1F ... oldDebit=153 ... forcedDebit=9999 ... action=257 ... live=hp=0 ... dmg1C4=9999 ...]
[DEATH-DIFF ptr=0x1418564E0 id=0x1F] alive->dead +0x30:3A->00 +0x31:01->00 +0x61:00->20 +0x18C:00->01 +0x1BB:00->01 +0x1C4:99->0F +0x1C5:00->27 +0x1DB:00->20 +0x1EF:00->20 +0x1F5:FF->13
[DAMAGE ptr=0x1418564E0 id=0x1F] 314 -> 0 = 314
[HP-EVENT-PROBE ... rawForecastDamage=9999 lethal=1 hpClamp=1 rawForecastOverkill=9685 ...]
```

Conclusion:

- Formula-backed pre-clamp staged damage can produce a real same-hit KO.
- The runtime resolved Cloud as the delayed caster and Braver as action `257` via `pending-clear`;
  CT was not needed as the primary attacker source.
- The managed formula result (`9999`) was queued before vanilla HP apply.
- The native pre-clamp hook replaced vanilla Braver staged debit `153` with `9999`.
- Vanilla HP apply clamped Beowulf to `0` and produced the KO lifecycle (`+0x61=0x20` and related
  death-state fields).
- UI display clamped/presented the overkill as `999`, matching the earlier forced Agrias proof.

This confirms the intended architecture for lethal custom damage:

`pending/action memory context -> custom formula -> pre-clamp staged debit -> vanilla HP clamp/KO`.

The old late HP rewrite path is no longer the desired primary damage architecture. It should remain
only as a fallback/debug path while the pre-clamp staged path is generalized.

Next recommended tests:

- Immediate weapon/basic attack through the same formula plan path.
- Non-Cloud charged action family, especially a spell or skill that does not spend MP.
- Healing / MP changes through an analogous staged result path.
- Multiple overlapping pending actions, to test batch disambiguation when more than one charged
  action is active.

## 2026-06-23 Prepared Next Live Test: Immediate Basic Attack Plan Path

Prepared profile:

- `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`

Purpose:

- Prove that immediate/basic actions can use the same pre-clamp staged-damage architecture that
  passed for delayed Cross Slash and lethal Braver.
- Do this with CT disabled as a resolver, so a pass means action-memory context, not CT, selected
  the attacker.

Runtime changes made for this test:

- `LogPreClampFormulaCandidateIfEnabled` still prefers `pending-clear` matches for delayed actions.
- If no pending match is present and `PreClampFormulaCandidateAllowImmediateAction=true`, it scans
  registered battle units for a fresh immediate-action source.
- The immediate source scorer reuses the `ImmediateActionCandidateScoring` model from the earlier
  Ramza Rush KO probe:
  - source-like unit, not the target;
  - positive action id at `+0x1A2`;
  - fresh action id and, by default, fresh active action marker `+0x1BA`;
  - high score and clear margin over the next eligible candidate.
- Accepted immediate sources create:
  - attacker source `immediate-action`;
  - `ActionSignal("immediate-action-<id>", "immediate-action", ...)`;
  - plan context `context=immediate-action/act=<id>` in `[PRECLAMP-PLAN-QUEUE]`.

New runtime settings:

- `PreClampFormulaCandidateAllowImmediateAction`
- `PreClampImmediateActionMinScore`
- `PreClampImmediateActionMinMargin`
- `PreClampImmediateActionMaxAgeMs`
- `PreClampImmediateActionRequireFreshActive`

New formula variables:

- `attacker.sourceImmediate`, `a.sourceImmediate`
- `action.sourceImmediate`, `act.sourceImmediate`
- immediate diagnostics:
  - `action.freshActionId`
  - `action.freshActiveAction`
  - `action.actionIdAgeMs`
  - `action.activeActionAgeMs`
  - `action.margin`
  - `action.runnerUpScore`

Profile behavior:

- late observed HP rewrites disabled;
- pre-clamp plan table enabled;
- `ResolveAttackerByCt=false`;
- `ResolveAttackerByLowCtFallback=false`;
- `InferAttackerFromRecentUnits=false`;
- `RewriteConditionFormula="event.isDamage && a.sourceImmediate && action.sourceImmediate && action.freshActiveAction"`;
- `FinalDamageFormula="max(1, a.pa * 10 - t.faith)"`.

Recommended live action:

1. Keep the data mod disabled.
2. Enable only `fftivc.utility.modloader` and `fftivc.generic.chronicle.codemod`.
3. Agrias basic-attacks Beowulf.
4. Report UI damage and Beowulf HP loss.
5. Close the game so the log can be captured.

Expected pass:

- Formula damage should be visible, likely around `45` from the recent Agrias/Beowulf stats, instead
  of the prior vanilla `10`.
- Logs should include:
  - `[PRECLAMP-IMMEDIATE-CANDIDATES ... selected=... id=0x1E ...]`;
  - `[PRECLAMP-FORMULA-CANDIDATE ... source=immediate-action ...]`;
  - `[PRECLAMP-PLAN-QUEUE ... context=immediate-action/act=...]`;
  - `[PRECLAMP-REWRITE ... forcedDebit=<formula result> ...]`;
  - final `[DAMAGE ...]` matching the forced debit.

Expected safe failure:

- Vanilla damage applies.
- Logs show why no immediate source was accepted: low score, no fresh `ba`, stale action id, or small
  margin between candidates.

Important operational note:

- At the time this checkpoint block was written, `FFT_enhanced.exe` and `Reloaded-II.exe` were still
  open, so the new DLL/profile had not yet been deployed. Close the game before copying the new
  build and runtime settings into the Reloaded-II mod folder.

## 2026-06-23 Immediate Basic Attack Test 7a: Safe Failure, No Rewrite

Profile:

- `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`
- First-attempt version:
  - required positive action id;
  - required fresh active action;
  - CT/recent fallback disabled.

Live action:

- Agrias basic-attacked Beowulf.
- Data mod disabled.

User observation:

```text
Preview/UI damage: 151
Beowulf HP: 163/314 after hit
No extra effect
Agrias attacked only; did not finish the turn
```

Important correction:

- This was not a rewrite. The preview already showed `151`, and Beowulf lost `151`.

Log artifact:

- `work\live-captures\battleprobe_log.preclamp-plan-immediate-basic-agrias-beowulf.snapshot.txt`

Key evidence:

```text
[PRECLAMP-IMMEDIATE-CANDIDATES target=... id=0x1F oldDebit=151 ... selected=none]
  Agrias id=0x1E ... act=0 ... b8=1/ba=0 ...
[PRECLAMP-FORMULA-CANDIDATE ... oldDebit=151 forcedDebit=151 shouldStage=0 queuedPlan=0 ... source=none ...]
[ACTION-STATE ptr=... id=0x1E ... act=0 ... b8=1 ba=1 ...]
[DAMAGE ptr=... id=0x1F] 314 -> 163 = 151
```

Conclusion:

- The immediate source detector failed safely: no selected source, no plan queued, vanilla damage
  passed through.
- The first target-cache opportunity happened too early:
  - target Beowulf had `dmg1C4=151`;
  - Agrias was only `b8=1/ba=0`;
  - `act=0`, so the first version rejected her.
- After that, Agrias became `ba=1` while still `act=0`, shortly before HP apply.

New hypothesis:

- For basic Attack, `+0x1A2` may stay zero because the action identity is implicit/basic.
- The stronger post-confirm execution signal is fresh `ba=1`.
- The target cache remains live long enough that a source-triggered rescan may queue the formula
  plan before the pre-clamp hook fires.

Implemented follow-up for Test 7b:

- `ImmediateActionCandidateScoreInput.AllowZeroActionIdActiveSource`.
- `RuntimeSettings.PreClampImmediateActionAllowZeroActionId`.
- `TrackActionProbeAges` now treats `ba=1` with `act=0` as a distinct active-action epoch so
  `activeActionAgeMs` becomes fresh.
- When a unit has fresh `ba=1`, the runtime rescans all registered units with `dmg1C4>0` and
  re-runs pre-clamp formula candidate evaluation.
- Profile updated:
  - `PreClampImmediateActionAllowZeroActionId=true`;
  - `PreClampImmediateActionMinScore=1600`.

Validation after implementation:

- `dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj -c Release`
  passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release`
  passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -c Release -- work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`
  passed with expected probe warnings.

Next live test:

- Redeploy after closing FFT/Reloaded.
- Repeat Agrias basic attack on Beowulf.
- Expected pass:
  - `[PRECLAMP-IMMEDIATE-CANDIDATES ... selected=... id=0x1E/act=0 ...]`;
  - `[PRECLAMP-FORMULA-CANDIDATE ... source=immediate-action ... shouldStage=1 queuedPlan=1 ...]`;
  - `[PRECLAMP-PLAN-QUEUE ... context=immediate-action/act=0 ...]`;
  - `[PRECLAMP-REWRITE ... forcedDebit=<formula result> ... action=0 ...]`;
  - HP loss matching formula result instead of preview `151`.

## 2026-06-23 Immediate Basic Attack Test 7b: Zero Action Id Seen, Freshness Gate Too Strict

Profile:

- `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`
- Second-attempt version:
  - allowed `act=0` when `ba=1`;
  - still required fresh active action;
  - CT/recent fallback disabled.

Live action:

- Agrias basic-attacked Beowulf.
- Data mod disabled.

User observation:

```text
Preview damage: 151
UI damage: 151
Beowulf HP loss: 151
No critical
Agrias only attacked, then the game was closed
```

Log artifact:

- `work\live-captures\battleprobe_log.preclamp-plan-immediate-basic-7b-agrias-beowulf.snapshot.txt`

Key evidence:

```text
[ACTION-STATE ... Agrias id=0x1E ... act=0 ... b8=1 ba=0 bb=0]
[ACTION-STATE ... Agrias id=0x1E ... act=0 ... b8=1 ba=1 bb=0]
[ACTION-STATE ... Agrias id=0x1E ... act=0 ... b8=1 ba=1 bb=1]
[PRECLAMP-IMMEDIATE-CANDIDATES ... oldDebit=151 ... selected=none]
  Beowulf target ... dmg1C4=151 ... bb=2
  Agrias active-source-like score=250 eligible=0 act=0 freshActive=0
    stateAge=28302 actionAge=42928 activeAge=29386 ... b8=1/ba=1/bb=1
[PRECLAMP-FORMULA-CANDIDATE ... oldDebit=151 forcedDebit=151 shouldStage=0 queuedPlan=0 ... source=none ...]
```

Conclusion:

- The zero-action-id hypothesis is correct enough to see Agrias as `active-source-like`.
- The failure was the freshness gate, not target/source visibility:
  - at HP apply, Agrias still had the current execution marker `ba=1`;
  - but `activeAge=29386ms`, so `freshActiveAction=0`;
  - because the profile required freshness, Agrias was rejected and vanilla damage passed.
- For live/basic actions, "fresh" is the wrong hard gate. The player can spend many seconds in
  confirm/animation/menu time while the unit remains the current active source.

Implemented follow-up for Test 7c:

- `ImmediateActionCandidateScore` now exposes `CurrentActiveAction` separately from
  `FreshActiveAction`.
- `ba=1`/current active state gets its own score bonus.
- Zero-action-id basic sources are not penalized just because the active marker is old; stale age is
  still logged diagnostically.
- `BuildImmediateActionSignal` now exposes `action.currentActiveAction`.
- Profile updated:
  - `PreClampImmediateActionRequireFreshActive=false`;
  - `RewriteConditionFormula="event.isDamage && a.sourceImmediate && action.sourceImmediate && action.currentActiveAction"`.

Validation after 7c implementation:

- `dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj -c Release`
  passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release`
  passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -c Release -- work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`
  passed with expected probe warnings.

Current operational state:

- 7c deploy was attempted but blocked because `Reloaded-II.exe` was still running.
- Need close Reloaded-II, redeploy, then repeat Agrias basic attack on Beowulf.

Expected Test 7c pass:

- `[PRECLAMP-IMMEDIATE-CANDIDATES ... selected=... id=0x1E/act=0/currentActive=1 ...]`;
- `[PRECLAMP-FORMULA-CANDIDATE ... source=immediate-action ... shouldStage=1 queuedPlan=1 ...]`;
- `[PRECLAMP-PLAN-QUEUE ... context=immediate-action/act=0 ...]`;
- `[PRECLAMP-REWRITE ... forcedDebit=<formula result> ... action=0 ...]`;
- Beowulf HP loss should be formula-owned instead of the vanilla preview/UI `151`.

## 2026-06-23 Instant Basic Memory Probe: Source+Target Found Before HP Apply

Detailed analysis:

- `work\instant-basic-memory-probe-2026-06-23.md`

Profile:

- `work\battle-runtime-settings.instant-basic-memory-probe.json`
- Observe-only/log-only.
- Added pre-clamp register, stack, `rbp` state-tail, and `rdi` target snapshot capture.

Live action:

```text
Agrias basic attack -> Beowulf
preview: 151
UI: 151
Beowulf HP loss: 151
critical: no
```

Log artifact:

- `work\live-captures\battleprobe_log.instant-basic-memory-probe-agrias-beowulf.snapshot.txt`

Major discovery:

- At the stable `battle_base_ptr` hook during Agrias's action boundary:

```text
rcx = Agrias
rdi = Agrias
r8  = Beowulf
```

- Later, at native pre-clamp `0x30A66F`:

```text
rcx/rdi/r8 = Beowulf
oldDebit = 151
pre HP = 314/314
live HP after hook = 163/314
rbp = Beowulf + 0x1BE
[rbp+6] = Beowulf + 0x1C4 = staged debit
```

Conclusion:

- For instant/basic attacks, the managed target-cache observation is too late, but source+target are
  visible earlier in stable hook registers.
- The target-cache landmarks `0x2D7AC0` / `0x2D7AEC` did not hit for this basic attack, so basic may
  populate `unit+0x1C4` through a different path.
- The likely instant-action architecture is:

```text
stable hook captures source+target (rcx/rdi + r8)
-> short-lived immediate action context/plan
-> pre-clamp hook matches target and staged debit
-> rewrite debit before vanilla HP apply
```

Next useful validation:

- Improve register classification for not-yet-registered unit pointers.
- Repeat a few observe-only instant tests:
  - Agrias -> Beowulf repeat;
  - Ramza -> Beowulf/Agrias;
  - Ninja dual wield -> Agrias.

## 2026-06-23 Instant Basic Memory Probe: Ramza -> Beowulf Lethal 912

Detailed analysis:

- `work\instant-basic-memory-probe-2026-06-23.md`

Log artifact:

- `work\live-captures\battleprobe_log.instant-basic-memory-probe-ramza-beowulf-lethal.snapshot.txt`

Live action:

```text
Ramza basic attack -> Beowulf
shown damage: 912
Beowulf died
```

Key result:

- The same source/target register pattern found in the Agrias basic attack repeated here:

```text
source/attacker: 0x141855CE0, id=0x01, Ramza per user action
target:          0x1418564E0, id=0x1F, Beowulf

stable hook:
  rcx = source
  rdi = source
  r8  = target
```

The Ramza action-boundary transition was:

```text
prev: b8=1 ba=0 bb=0
curr: b8=0 ba=1 bb=0
diff: +0x1A0:00->10 +0x1A1:00->01 +0x1B8:01->00 +0x1B9:01->03 +0x1BA:00->01
```

The target staged damage before native HP application was:

```text
Beowulf +0x1C4 = 912
Beowulf +0x1D8 = 130
Beowulf +0x1E5 = 136
```

At native pre-clamp:

```text
target hp: 314/314
oldDebit: 912
oldCredit: 0
after vanilla: hp=0, +0x61=0x20, +0x1EF=0x20
```

Interpretation:

- This is a clean vanilla lethal proof for the staged-debit path.
- The engine consumed raw staged debit `912`, clamped HP from `314` to `0`, and set KO status bits
  itself.
- Therefore our desired final path remains valid:

```text
early action context capture -> custom formula -> native pre-clamp staged debit -> vanilla HP/KO
```

Important noise:

- A separate credit-like pre-clamp event happened before the lethal hit:

```text
ptr=0x141855EE0 id=0x80 oldDebit=0 oldCredit=34
```

This is not the attack. The writer must distinguish positive staged debit damage events from
credits/heals/automatic ticks.

Current confidence:

- Stronger confidence that `r8` is a useful current-target pointer for instant/basic actions.
- Stronger confidence that source can be captured at the stable hook from `rcx/rdi` during the
  source unit's action boundary.
- Not yet enough confidence to enable writes for all instant/basic attacks.

Next highest-value test:

```text
Ninja basic dual wield -> Agrias
```

Why:

- It checks whether one captured immediate source+target context can cover two HP pre-clamp events.
- It is the natural bridge between single-hit basics and the real combat edge cases that will matter
  for the redesign.

## 2026-06-23 Instant Basic Memory Probe: Ninja Dual Wield -> Agrias

Detailed analysis:

- `work\instant-basic-memory-probe-2026-06-23.md`

Log artifact:

- `work\live-captures\battleprobe_log.instant-basic-memory-probe-ninja-dual-agrias-lethal.snapshot.txt`

Live action:

```text
Ninja dual wield -> Agrias
shown damage: 180 / 180
Agrias HP lost: 322/322, died on second hit
```

Key result:

- Dual wield is two separate native pre-clamp damage events against the same target.
- First hit:

```text
Agrias HP 322 -> 142
oldDebit=180
rawForecastDamage=180
lethal=0
```

- Second hit:

```text
Agrias HP 142 -> 0
oldDebit=180
rawForecastDamage=180
lethal=1
rawForecastOverkill=38
+0x61=0x20
+0x1EF=0x20
```

Important correction:

- `r8` is not a reliable target pointer.
- In the dual-wield capture, `r8` often pointed to Beowulf (`0x1418564E0/id=0x1F`) even though the
  real target was Agrias (`0x1418560E0/id=0x1E`).
- Therefore the earlier `r8 == target` hypothesis should be downgraded to "diagnostic clue only".

Better model:

```text
target = native pre-clamp ptr / HP-event ptr
source = current active source-like unit, usually ba=1 around the action
```

The Ninja/source action state stayed useful across both hits:

```text
Ninja ptr=0x141855EE0 id=0x80
prev: b8=1 ba=0 bb=0
curr: b8=1 ba=1 bb=0
diff: +0x1A0:00->11 +0x1A1:00->01 +0x1BA:00->01
```

The candidate scorer selected Ninja as source before the first pre-clamp and again after the second
damage event:

```text
selected=0x141855EE0/id=0x80/act=0/score=2300
```

Implementation implication:

- Immediate/basic action support should not depend on capturing `source+target` from registers in
  one event.
- It should maintain a short-lived current-source context while the source remains action-active,
  then use the native pre-clamp target pointer as the authoritative target.
- Multi-hit support requires the plan/context to survive the first hit:
  - either small `maxWrites=2` for immediate basic actions;
  - or a reusable source-owned action context that can create/recreate pre-clamp rewrite plans per
    matching target event.

New next step:

```text
Implement an immediate-action source context bridge that uses active-source state plus pre-clamp
target, not r8 target, then run observe/write tests with single-hit and dual-wield.
```

## 2026-06-23 Implementation: Immediate Multi-Hit Pre-Clamp Plans

Detailed analysis:

- `work\instant-basic-memory-probe-2026-06-23.md`

Implemented after the Ninja dual-wield probe:

- Native pre-clamp plan table now truly honors `PLAN_MAX_WRITES`.
  - Old behavior effectively deactivated a plan after one match regardless of max writes.
  - New behavior keeps the slot active until `writeCount >= maxWrites`.
- Native pre-clamp plan matching now supports `-1` wildcard values for expected HP/MaxHP/debit/credit.
- Immediate-action plans have separate settings:
  - `PreClampImmediateActionPlanMaxWrites`;
  - `PreClampImmediateActionPlanRequireExpectedHp`.
- The immediate-basic live profile now sets:
  - `PreClampImmediateActionPlanMaxWrites=2`;
  - `PreClampImmediateActionPlanRequireExpectedHp=false`.

Rationale:

- Charged/delayed actions keep strict one-plan-per-target behavior by default.
- Immediate/basic actions need a source-owned plan that can survive dual wield's two separate native
  pre-clamp events.
- Because `r8` was proven unreliable in Probe C, target truth is the native pre-clamp target pointer,
  not `r8`.
- Source truth is the source-like active unit (`ba=1`) selected by immediate candidate scoring.

Verified:

```text
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
Offline checks passed.
```

Deployed:

```text
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.dll
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json
```

Active live profile:

- `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`

Next test instructions:

```text
Enable only:
- fftivc.utility.modloader
- fftivc.generic.chronicle.codemod

Keep the Generic Chronicle data mod disabled.

Run:
Ninja dual wield -> Agrias
```

Expected:

- Preview may still show vanilla `180 / 180`.
- Formula path should replace each hit with:

```text
max(1, Ninja.PA * 10 - Agrias.Faith)
= max(1, 15 * 10 - 63)
= 87
```

- Expected live result: about `87 / 87`, Agrias loses about `174` total, and should not die from
  full `322`.

Log evidence to look for:

```text
[PRECLAMP-PLAN-QUEUE ... context=immediate-action ... maxWrites=2 expectedHp=any]
[PRECLAMP-REWRITE ... oldDebit=180 forcedDebit=87 ...]
[PRECLAMP-REWRITE ... oldDebit=180 forcedDebit=87 ...]
```

## 2026-06-23 Follow-Up: First Immediate Plan Failed, Eager Plan Deployed

The first immediate multi-hit plan was live-refuted:

```text
Ninja dual wield -> Agrias
UI/applied: 180 / 180
Agrias HP loss: 322, KO
```

Snapshot:

```text
work\live-captures\battleprobe_log.preclamp-plan-immediate-ninja-dual-agrias-failed-180x2.snapshot.txt
```

Meaning:

- The native pre-clamp hook is still correct.
- The managed plan was not queued early enough.
- Waiting until target-side `+0x1C4` is visible is too late for immediate/basic damage.

Current deployed fix:

```text
active source-like unit (ba=1)
-> eager formula evaluation for nearby possible targets
-> one native plan per possible target
-> native hook chooses the actual target by pointer
-> expected debit requires positive staged damage
```

New settings in the immediate live profile:

```text
PreClampImmediateActionPlanEagerTargets=true
PreClampImmediateActionNearbyUnitScanRadius=8
```

The retest at that point was:

```text
Ninja dual wield -> Agrias
```

Expected:

```text
Preview may show 180 / 180.
Applied damage should be about 87 / 87.
Agrias should lose about 174 total and survive.
```

Expected log evidence:

```text
[PRECLAMP-EAGER-PLAN-CANDIDATE ... source=Ninja ... target=Agrias ... forcedDebit=87 ... queuedPlan=1]
[PRECLAMP-PLAN-QUEUE ... oldDebit=positive ... forcedDebit=87 ... maxWrites=2 ... expectedHp=any]
[PRECLAMP-REWRITE ... oldDebit=180 ... forcedDebit=87 ...]
[PRECLAMP-REWRITE ... oldDebit=180 ... forcedDebit=87 ...]
```

## 2026-06-24 Correction: Eager Source Must Reject Ghost Structs

The first eager live test failed:

```text
Ninja dual wield -> Agrias
First hit: 999, Agrias died.
Second hit: whiffed.
```

Root cause:

- Nearby stride scan accepted a ghost struct at `0x141856AE0`.
- It had impossible live-unit fields (`team=58`, `Sp=174`, `Move=195`, `ba=144`) but old snapshot
  validation only checked level/HP/MP.
- Immediate source scoring accepted `ba != 0`, so `ba=144` became an active source.
- The formula then used ghost `PA=115`, producing `1087` damage against Agrias.

Fix deployed:

- Source-active marker is now exact: `ActiveMarker2 == 1`.
- Eager queueing, action-age tracking, and immediate source identity checks use that exact marker.
- Unit snapshot validation rejects impossible team/CT/stats/mobility/Brave/Faith values.
- Current immediate test profile uses `PreClampImmediateActionNearbyUnitScanRadius=4`.

Gate:

```text
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
Offline checks passed.
```

## 2026-06-24 Success: Immediate Dual Wield Works

Live result:

```text
Ninja dual wield -> Agrias
Preview/UI forecast: 180
Applied damage: 87 / 87
Agrias HP: 322 -> 235 -> 148
```

Snapshot:

```text
work\live-captures\battleprobe_log.preclamp-eager-immediate-ninja-agrias-success-87x2.snapshot.txt
```

Confirmed:

- Source was Ninja via active marker:

```text
source=0x141855EE0/id=0x80, ba=1
```

- Target was Agrias via native pre-clamp target pointer:

```text
target=0x1418560E0/id=0x1E
```

- Both native pre-clamp events were rewritten:

```text
oldDebit=180 -> forcedDebit=87
oldDebit=180 -> forcedDebit=87
```

Meaning:

```text
Immediate/basic damage can be code-modded pre-clamp with native HP apply and native KO semantics.
Dual wield is covered by maxWrites=2.
```

Current caveat:

```text
Eager immediate plans are still noisy/repeated while ba=1 stays active.
Next cleanup should tighten source-context lifetime after the hit batch completes.
```

## 2026-06-24 Live Result: Executing Action Pointer Probe (Cross Slash AoE)

The observe-only `executing-action-pointer-probe` was run on the current PC.

Controlled action:

- Cloud Cross Slash centered on Agrias (hits Agrias + Ninja).
- Forecast on Agrias: `115`.
- Delayed resolution after Beowulf, Agrias, Ninja turns.
- Result:
  - Ninja `277 -> 4 = 273`.
  - Agrias `322 -> 207 = 115`.
  - Next active unit: Ramza.

Artifacts:

- `work/live-captures/battleprobe_log.executing-action-pointer-probe-resolved-cross-slash-agrias-ninja.snapshot.txt`
- `work/live-captures/battleprobe_log.executing-action-pointer-probe-full.20260624-194730.txt`
- `work/live-captures/battleprobe_log.pre-executing-action-pointer-probe.20260624-init-only.txt`

### Verdict: partial pass + strong structural lead

1. Internal pending tracker reconfirmed (already known): both AoE hits grouped into one batch
   and resolved to Cloud/Cross Slash.
   - Ninja and Agrias both: `resolved=0x1418562E0/id=0x32 source=pending-clear batch=1 act=258 confidence=damage-cache`.
   - `fallback=none`, `ctCandidates=none` -> CT resolved nothing again.
2. The stable-hook `kind=pendingresolve` scan was useless for attribution: `hookAgeMs=38745`
   (snapshot ~39s stale) and `HOOK-PTRSCAN-EVENT kind=pendingresolve ... nohits`. The value is at
   the native pre-clamp frame, not the stable hook at resolve-open.
3. NEW: the native pre-clamp pointer scan revealed a battle participant/actor array.

### The battle participant/actor array (new structure)

Each scanned root that is an actor struct links to its unit at `+0x148`. Observed map (this session):

```text
actor module+0xD31FE8 -> unit 0x141855CE0 (Ramza)
actor module+0xD32530 -> unit 0x141855EE0 (Ninja)
actor module+0xD32A78 -> unit 0x1418560E0 (Agrias)
actor module+0xD32FC0 -> unit 0x1418562E0 (Cloud)   <- caster
```

- Contiguous array, stride `0x548`. Predicted Beowulf actor: `module+0xD33508`.
- Actor `+0x148` = pointer to the unit struct.
- Actor `+0x0` = pointer to `(this - 0x548)` = previous array element (back-link), consistent with a
  contiguous array.

### Caster is reachable at damage time

During BOTH AoE HP-apply events (same native tick `now=37793402198`), the caster's actor struct
(Cloud `module+0xD32FC0`) is present on the pre-clamp stack next to the current target's actor struct:

```text
Ninja hit  (oldDebit=273): stack+0x20 -> Cloud actor (caster), stack+0x50 -> Ninja actor (target)
Agrias hit (oldDebit=115): stack+0x60 -> Cloud actor (caster), stack+0x50 -> Agrias actor (target)
```

- The slot index of the caster differs per hit, so it cannot be hardcoded.
- BUT the caster actor is constant across the AoE batch while the target actor varies.
- Practical discriminator: `caster = stack actor whose +0x148 unit != current pre-clamp target`.
- The native pre-clamp registers (`rcx/rdi/r8`) still only carry the target; in the Agrias hit
  `r11`/`stack+0x10` carried Ninja's state struct (`unit+0x1BE`), i.e. the two AoE victims are
  cross-linked, but the caster never appeared in a register.

This is the candidate "current executing action context" the probe was hunting: a real engine
structure that links the resolving caster to each target HP-apply, available straight from memory at
damage time. It is the strongest lead so far for retiring both CT and the pending-clear heuristic.

### Open question: does the action id live in the actor struct?

We see the caster actor pointer, but have not yet dumped the actor struct bytes to find the resolving
action id (Cross Slash `0x0102` / `258`), target list, current-target index, or charge fields. If
the action id is inside the actor struct, caster+action+target can be read directly from engine
memory at damage time with no pending tracker.

### Next probe deployed: actor-struct dump

Implemented and deployed an extension to the pre-clamp pointer scan:

- New runtime settings:
  - `PreClampActorStructDumpEnabled`
  - `PreClampActorStructDumpBytes`
  - `PreClampActorStructUnitOffset` (default `0x148`)
  - `PreClampActorStructDumpMaxLogs`
- New log line: `[PRECLAMP-ACTOR-DUMP root=0x... unitOff=+0x148 unit=0x.../id=0x.. bytes=N] <hex>`.
- When a scanned root links to a registered unit at `PreClampActorStructUnitOffset`, the runtime
  dumps that actor struct's raw bytes (from the buffer the scan already read; no extra process read).

Profile: `work/battle-runtime-settings.executing-action-actor-dump-probe.json`

- Dumps `1352` (`0x548`) bytes per actor struct (the full stride), `unitOffset=0x148`, max `32` dumps.
- Still observe-only: `PreClampDamageRewriteLogOnly=true`, no HP/MP writes.

Offline gate: `dotnet build`, smoke tests, `report_runtime_profiles.py`, `test_runtime_profiles.py`,
`test_runtime_tooling.py`, settings validation, and `run-offline-checks.ps1` all passed. Deployed via
`build-deploy.ps1 -RuntimeSettings ...` with the game closed. The live log was archived and cleared.

Next live test (same controlled action, Cloud Cross Slash AoE on Agrias). After resolution, inspect:

```text
[PRECLAMP-ACTOR-DUMP ...]   # for Cloud (caster) and each target
```

Goals:

1. Find `02 01` (`0x0102` = 258) or `258` inside the caster actor struct, and map its offset.
2. Look for a target pointer / target list / current-target index in the caster actor struct.
3. Compare caster vs target actor dumps to find caster-only action fields.
4. Separately, relaunch and re-capture to test whether the `module+0xD32xxx` actor RVA and the
   `+0x148` / `0x548` layout are stable across game launches.
