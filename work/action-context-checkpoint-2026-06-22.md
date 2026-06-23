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
2. `docs/modding/11-charged-action-context-investigation.md`
   - Detailed live-test record for Braver and Cross Slash delayed-action context.
3. `docs/modding/07-live-findings.md`
   - Canonical live evidence up through earlier CT/death tests.
4. `docs/modding/05-battle-data-map.md`
   - Known unit struct offsets.
5. `docs/modding/06-code-mod-battle-runtime-architecture.md`
   - Runtime architecture and "engine owns death, code mod owns the number".
6. `docs/modding/04-re-strategy.md`
   - Reverse-engineering strategy and PSX/classic FFT function-map direction.

## Current Deployed Probe State

The latest code mod DLL has been built and deployed to:

`C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.dll`

The deployed runtime settings are:

`C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`

They were copied from:

`work/battle-runtime-settings.action-context-probe.json`

The old game log was archived to:

`work/live-captures/battleprobe_log.pre-pending-action-probe.20260622-230137.txt`

Offline checks passed after the latest probe changes:

```powershell
codemod\run-offline-checks.ps1
```

The profile is observe-only:

- no HP rewrite;
- no MP rewrite;
- CT diagnostics enabled;
- hook register event probes enabled;
- actor probe enabled;
- new pending-action candidate logging enabled.

New log line added by the latest code:

```text
[PENDING-ACTION-CANDIDATES kind=damage event=N target=0x.../id=0x.. now=...] ...
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

The next live test should answer whether a charged-action caster is still visible as pending at the
exact HP-write event.

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

## Next Live Test Protocol

Current DLL/profile is already deployed.

The user should launch through Reloaded-II with only:

- `fftivc.utility.modloader`
- `fftivc.generic.chronicle.codemod`

No Reloaded AppConfig JSON edits.

Test:

1. Start from the known Cloud Limit setup.
2. Use Cloud's Cross Slash centered on Agrias so it hits Agrias and Ninja if they remain in range.
3. At preview, user says:

```text
forecast Cross Slash AoE pronto
```

4. Confirm the action.
5. Before Cloud Waits, user says:

```text
confirmado Cross Slash AoE
```

6. Agent captures a snapshot if useful.
7. User Waits Cloud.
8. On each intervening active unit, user stops and reports:

```text
pendente Cross Slash AoE, ativo X
```

9. Agent captures snapshots as needed.
10. After resolution, user reports:

```text
pos Cross Slash AoE, Agrias HP -X, Ninja HP -Y, ativo Z
```

11. Agent copies the game log from:

`D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt`

and checks for:

- `[PENDING-ACTION-CANDIDATES kind=damage ...]`
- `[DAMAGE ...]`
- `[CTX ...]`
- `[HOOK-REGS-EVENT kind=damage ...]`

Primary expected evidence:

- During Agrias and Ninja HP events, either Cloud is still listed as pending action candidate, or he
  has already cleared.

This determines whether live pending-state scan is viable or whether our own pending table is
mandatory.

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
4. Target-side `dmg1C4` matches the observed HP loss for each target when available:
   `115` for Agrias and `273` for Ninja in the latest test.
5. CT is not used as the primary attribution signal for this delayed action. CT can remain as a
   fallback for immediate actions or unresolved edge cases.

Reason for doing this before more design work:

Custom battle formulas need reliable formula context. The final formula engine needs `caster`,
`action`, and `target` at the moment damage is applied. The current HP hook already gives target and
final damage, but not delayed-action caster. The tracker is the next offline implementation needed
to bridge that gap before testing formula rewrites on charged skills.
