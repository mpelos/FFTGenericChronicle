# Charged Action Context Investigation

Status: active RE path after the forecast scans on 2026-06-22.

## Objective

Find a robust primary source for action context in the live battle runtime:

- caster / actor;
- target or target list;
- action identity;
- timing state for delayed / charged actions.

CT-based resolution works as a fallback, but it should not be the primary source for a full combat
redesign. Delayed actions, Wait, counters, reactions, and queued execution make CT inherently
heuristic.

## Mental Model

A charged action likely has at least three different representations:

1. **Preview / forecast state.** The UI can show actor, target, action name, hit chance, modifier,
   and predicted damage before confirmation. This state can be canceled and should not be assumed to
   be the real pending action object.
2. **Scheduled / pending action state.** After confirmation, the engine must remember enough to
   resolve the action later: caster, action id, target tile/unit(s), charge timing, and possibly the
   forecast values. This is the object we want.
3. **Resolution state.** When the action executes, the engine may rebuild or copy a smaller context
   into the damage routine. This may be different from both preview and pending state.

Therefore, a good candidate should survive from confirmed/pending until just before resolution, and
should change target when only the target changes. A pure preview/UI candidate may look excellent
before confirmation and then turn into "current focused unit" after a Wait.

## Evidence From Forecast Tests

Controlled action: Cloud uses Braver with two different targets.

### Beowulf target

- Preview: Braver -> Beowulf, UI `153` damage, `100%`, `125%`, Beowulf HP `314`, MP `180`.
- Confirmed while still on Cloud.
- Pending while Beowulf had control.
- Post-resolution: Beowulf HP dropped by `153`.

### Agrias target

- Preview: Braver -> Agrias, UI `76` damage, `100%`, `100%`, Agrias HP `322`, MP `232`.
- Confirmed while still on Cloud.
- Pending while Beowulf had control.
- Post-resolution: Agrias HP dropped by `76`.

### Eliminated Candidate: `0x03C367xx`

The block around `0x03C36740..0x03C36920` looked promising because it contained the preview target
pointer and forecast-like values.

It is now classified as UI/current-focus state, not the pending action:

| State | Slot at `0x03C368D0` |
| --- | --- |
| Preview Agrias | Agrias |
| Confirmed Agrias, still Cloud turn | Agrias |
| Pending Agrias, Beowulf turn | Beowulf |
| Post Braver Agrias | Agrias / current focus |

This means the slot follows the focused/active UI unit, not the scheduled Braver target.

## Live Round 2: Struct-Carried Pending Action Evidence

Controlled action:

- Cloud selects Braver (`0x0101`) targeting Agrias.
- Forecast UI: `76` damage.
- Cloud confirms Braver, then Waits.
- Beowulf gets a turn while Braver is still pending.
- Beowulf Waits.
- Braver resolves on Agrias for `76` damage.

Important captures:

- `work/live_unit_struct_snapshot.round2.baseline-no-target.*`
- `work/live_unit_struct_snapshot.round2.forecast-agrias.*`
- `work/live_unit_struct_snapshot.round2.confirmed-agrias.*`
- `work/live_unit_struct_snapshot.round2.pending-beowulf.extended.*`
- `work/live_unit_struct_snapshot.round2.post-braver-agrias.extended.*`
- `work/live-captures/battleprobe_log.round2.post-braver-agrias.snapshot.txt`

### Target Forecast / Pending Damage Fields

Agrias' unit struct changed during forecast, before confirmation:

| State | Agrias HP | `Agrias+0x1C4` | `Agrias+0x1D8` | `Agrias+0x1E5` |
| --- | ---: | ---: | ---: | ---: |
| Baseline | `322` | `0` | `0` | `0` |
| Forecast Braver | `322` | `76` | `2` | `128` |
| Confirmed Braver | `322` | `76` | `2` | `128` |
| Pending on Beowulf | `322` | `76` | `2` | `128` |
| Post-resolution | `246` | `0` | `0` | `0` |

Interpretation:

- `target+0x1C4` is a strong candidate for forecast / pending damage.
- `target+0x1D8` is a strong candidate for charge / timing metadata for the pending action.
- `target+0x1E5` is action/target-state metadata; meaning still unknown.
- The fields survive confirmation and the caster's Wait, then clear exactly when damage resolves.

This is stronger than the earlier raw pointer scans: the target itself carries the pending damage
state we need to reconcile delayed actions.

### Caster Pending Action Fields

Cloud's unit struct did not change during forecast, but changed after confirmation:

| State | `Cloud+0x61` | `Cloud+0x18D` | `Cloud+0x1A2` as u16 | `Cloud+0x1EF` |
| --- | ---: | ---: | ---: | ---: |
| Baseline | `0` | `255` | `0` | `0` |
| Forecast Braver | `0` | `255` | `0` | `0` |
| Confirmed Braver | `8` | `2` | `257` | `8` |
| Pending on Beowulf | `8` | `2` | `257` | `8` |
| Post-resolution | `0` | `255` | `257` | `0` |

Interpretation:

- `caster+0x1A2` is a strong candidate for last/queued action id. For Braver it is `0x0101`.
- `caster+0x61` and `caster+0x1EF` behave like pending-action/charging flags.
- `caster+0x18D` behaves like pending charge/timer state: `2` while Braver is queued and `255`
  after it resolves.
- `caster+0x1A2` remains `257` after resolution, so the action id alone is not sufficient to
  prove an action is pending. It must be combined with the pending flags/timer.

### Runtime Implication

CT-based attacker resolution failed for this delayed hit, as expected:

- The damage event on Agrias logged `resolved=none`.
- Hook registers at damage time were misleading: active/current units included Beowulf, not Cloud.
- The reliable context was instead already visible earlier in the unit structs:
  - caster pending action: Cloud had `+0x61=8`, `+0x18D=2`, `+0x1A2=0x0101`;
  - target pending damage: Agrias had `+0x1C4=76`, `+0x1D8=2`.

This suggests the code mod should maintain a pending-action tracker from struct fields, not try to
infer delayed action context only at the HP write.

## Live Round 3: Same Action, Different Target

Controlled action:

- Cloud selects Braver (`0x0101`) targeting Beowulf.
- Forecast UI: `153` damage.
- Cloud confirms Braver, then Waits.
- Beowulf gets a turn while Braver is still pending.
- Beowulf Waits.
- Braver resolves on Beowulf for `153` damage.

Important captures:

- `work/live_unit_struct_snapshot.beowulf-baseline-no-target.extended.*`
- `work/live_unit_struct_snapshot.beowulf-forecast.extended.*`
- `work/live_unit_struct_snapshot.beowulf-confirmed.extended.*`
- `work/live_unit_struct_snapshot.beowulf-pending.extended.*`
- `work/live_unit_struct_snapshot.beowulf-post.extended.*`
- `work/live-captures/battleprobe_log.beowulf-post.snapshot.txt`

### Target-Local Forecast Fields Are Confirmed, But Not Stable Through Target Turns

The Beowulf forecast confirmed the target-local mapping:

| State | Beowulf HP | `Beowulf+0x1C4` | `Beowulf+0x1D8` | `Beowulf+0x1E5` |
| --- | ---: | ---: | ---: | ---: |
| Baseline | `314` | `0` | `0` | `0` |
| Forecast Braver | `314` | `153` | `2` | `128` |
| Confirmed Braver | `314` | `153` | `2` | `128` |
| Pending on Beowulf | `314` | `0` | `0` | `0` |
| Post-resolution | `161` | `0` | `0` | `0` |

Interpretation:

- `target+0x1C4` is definitely target-local forecast damage: changing only the target changed the
  field from `Agrias+0x1C4=76` to `Beowulf+0x1C4=153`.
- It is not guaranteed to survive until resolution. When the pending target itself becomes the
  active unit, the target-local forecast fields can clear before the delayed action resolves.
- Therefore `target+0x1C4` is useful for forecast/confirmation capture, but cannot be the only
  delayed-action matcher at HP-write time.

### Caster Pending Fields Remained Stable

Cloud's pending-action fields matched the Agrias test:

| State | `Cloud+0x61` | `Cloud+0x18D` | `Cloud+0x1A2` as u16 | `Cloud+0x1EF` |
| --- | ---: | ---: | ---: | ---: |
| Baseline | `0` | `255` | `0` | `0` |
| Forecast Braver | `0` | `255` | `0` | `0` |
| Confirmed Braver | `8` | `2` | `257` | `8` |
| Pending on Beowulf | `8` | `2` | `257` | `8` |
| Post-resolution | `0` | `255` | `257` | `0` |

This is now the strongest signal for delayed action ownership:

- `caster+0x61 == 0x08`
- `caster+0x18D == 2` for this Braver setup
- `caster+0x1A2 == 0x0101`
- `caster+0x1EF == 0x08`

The action id remains after resolution, so pending state must be keyed by the flags/timer.

### Global Numeric Scans Were Not Useful

`tools/scan_live_named_value_records.py` found many records containing `Cloud`, `Beowulf`, `Braver`,
`153`, and `2`, but the same 44 candidates persisted after resolution. Treat these as static
ability/UI/cache noise until proven otherwise.

Raw pointer scans also remained noisy:

- pending Beowulf candidates: `0x15F0798E8`, `0x15E806778`, `0x15EBFFEF8`, `0x15BB74C28`,
  `0x15EC00588`;
- post Beowulf candidates mostly persisted or shifted with UI/current-turn state.

These are lower priority than the unit-struct fields.

## Round 4 - Cross Slash Longer Charge

Goal: verify whether the caster-side pending fields remain stable for a longer delayed action, and
whether target-local forecast fields survive until final resolution.

Setup:

- Cloud used Cross Slash on Ramza.
- UI forecast damage: `187`.
- Cross Slash action id observed: `258` (`0x0102`).
- The action remained pending through Beowulf, Agrias, and Ninja turns, then resolved on Ramza.

Artifacts:

- `work/live_unit_struct_snapshot.cross-slash-ramza-forecast.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-confirmed.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-pending-beowulf.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-pending-agrias.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-pending-ninja.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-ramza-post.extended.*`
- `work/live-captures/battleprobe_log.cross-slash-ramza-pending-beowulf.snapshot.txt`
- `work/live-captures/battleprobe_log.cross-slash-ramza-pending-agrias.snapshot.txt`
- `work/live-captures/battleprobe_log.cross-slash-ramza-pending-ninja.snapshot.txt`
- `work/live-captures/battleprobe_log.cross-slash-ramza-post.snapshot.txt`

### Target Forecast Fields Cleared Before Resolution

Ramza target-local fields:

| State | Ramza HP | `Ramza+0x1C4` | `Ramza+0x1D8` | `Ramza+0x1E5` |
| --- | ---: | ---: | ---: | ---: |
| Forecast Cross Slash | `567` | `187` | `2` | `128` |
| Confirmed Cross Slash | `567` | `187` | `2` | `128` |
| Pending on Beowulf | `567` | `187` | `2` | `128` |
| Pending on Agrias | `567` | `0` | `0` | `0` |
| Pending on Ninja | `567` | `0` | `0` | `0` |
| Post-resolution | `380` | `0` | `0` | `0` |

Interpretation:

- `target+0x1C4` is a forecast/preview damage cache, not a durable pending-action record.
- The value can survive one or more intervening turns, but it can also clear before damage resolves.
- The code mod must capture and retain target candidates when they are visible; it cannot rely on
  these fields being present at HP-write time.

### Caster Pending Fields Survived Until Resolution

Cloud caster-side fields:

| State | Cloud CT | `Cloud+0x61` | `Cloud+0x18D` | `Cloud+0x1A2` as u16 | `Cloud+0x1EF` |
| --- | ---: | ---: | ---: | ---: | ---: |
| Confirmed Cross Slash | `28` | `8` | `3` | `258` | `8` |
| Pending on Beowulf | `28` | `8` | `3` | `258` | `8` |
| Pending on Agrias | `46` | `8` | `1` | `258` | `8` |
| Pending on Ninja | `46` | `8` | `1` | `258` | `8` |
| Post-resolution | `64` | `0` | `255` | `258` | `0` |

Interpretation:

- `caster+0x1A2` is the action id/last action id. It remains `258` after resolution.
- `caster+0x61 == 8`, `caster+0x1EF == 8`, and `caster+0x18D != 255` identify the active pending
  state.
- `caster+0x18D` behaves like a pending countdown/phase. It started at `3` for Cross Slash and later
  reached `1` before execution.
- The caster-side pending state is currently the best primary signal for delayed action ownership.

### Runtime Implication

For charged/delayed actions, the runtime should maintain its own pending table:

1. Detect forecast/confirmation while `target+0x1C4` is nonzero and record candidate target(s).
2. Detect caster pending state with `caster+0x61/+0x18D/+0x1A2/+0x1EF`.
3. Join the caster pending state to the recently observed forecast target candidates.
4. When HP changes later, resolve attacker/action from the pending table first.
5. Use CT-reset as fallback only when no pending action matches.

## Round 5 - Cross Slash AoE

Goal: determine how delayed AoE actions expose primary and secondary targets.

Setup:

- Cloud used Cross Slash centered on Agrias.
- UI forecast damage on Agrias: `115`.
- The user reported the final AoE hit as Agrias `-115` and Ninja `-273`.
- The action remained pending through Beowulf, Agrias, and Ninja turns, then resolved.

Artifacts:

- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-forecast.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-confirmed.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-pending-beowulf.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-pending-agrias.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-pending-ninja.extended.*`
- `work/live_unit_struct_snapshot.cross-slash-aoe-agrias-ninja-post.extended.*`
- `work/live-captures/battleprobe_log.cross-slash-aoe-agrias-ninja-pending-beowulf.snapshot.txt`
- `work/live-captures/battleprobe_log.cross-slash-aoe-agrias-ninja-pending-agrias.snapshot.txt`
- `work/live-captures/battleprobe_log.cross-slash-aoe-agrias-ninja-post.snapshot.txt`

### Forecast Only Marked the Primary Target

Target-local fields:

| State | Agrias HP | `Agrias+0x1C4` | Ninja HP | `Ninja+0x1C4` |
| --- | ---: | ---: | ---: | ---: |
| Forecast | `322` | `115` | `276` | `0` |
| Confirmed | `322` | `115` | `276` | `0` |
| Pending on Beowulf | `322` | `115` | `276` | `0` |
| Pending on Agrias | `322` | `0` | `276` | `0` |
| Pending on Ninja | `322` | `0` | `276` | `0` |
| Post-resolution | `207` | `0` | `3` | `0` |

Interpretation:

- `target+0x1C4` captured only the preview/forecast target for this AoE setup.
- The secondary target (Ninja) never receiving visible forecast damage is expected: charged AoE
  actions can target a character or tile as an epicenter, and affected units are resolved later from
  the action's range/area plus current unit positions.
- For formula execution, forecast damage should not be treated as target identity. The authoritative
  target is the unit whose HP changes at resolution time.
- Forecast fields are only needed if the code mod needs a forecast/selection hook, UI preview parity,
  or an early hint to connect a confirmed action to a target/epicenter before resolution.

### Damage Events Were Separate HP Writes

At resolution, the log recorded two HP events in the same hook window:

- `[DAMAGE ptr=0x1418560E0 id=0x1E] 322 -> 207 = 115` for Agrias.
- `[DAMAGE ptr=0x141855EE0 id=0x80] 276 -> 3 = 273` for Ninja.

Both events resolved `CTX ... resolved=none` with CT-only logic, which is expected for a delayed
action after several intervening turns.

Open question:

- Are `caster+0x61/+0x18D/+0x1A2/+0x1EF` still visible at the exact HP-write event before the
  engine clears them? The current log does not print those fields for all known units at damage
  event time. The next probe pass should log pending-action candidates on every HP event.
- If we later need full pre-resolution targeting, where does the game store the selected character or
  tile epicenter for a charged action? This is separate from formula execution, because HP-write
  events already provide the final impacted units.

## Round 6 - Pending-Candidate Probe At HP Write

Goal: determine whether the caster-side pending action fields are still visible at the exact HP-write
events for a delayed AoE action.

Setup:

- Same Cross Slash AoE setup as Round 5.
- New code-mod probe enabled `LogPendingActionCandidatesOnEvent`.
- Cloud confirmed Cross Slash centered on Agrias.
- The action stayed pending through Beowulf, Agrias, and Ninja turns.
- Final user-observed damage: Agrias `-115`, Ninja `-273`.

Artifacts:

- `work/live_unit_struct_snapshot.pending-action-probe-baseline.extended.*`
- `work/live_unit_struct_snapshot.pending-action-probe-cross-slash-aoe-forecast.extended.*`
- `work/live_unit_struct_snapshot.pending-action-probe-cross-slash-aoe-confirmed.extended.*`
- `work/live_unit_struct_snapshot.pending-action-probe-cross-slash-aoe-pending-beowulf.extended.*`
- `work/live_unit_struct_snapshot.pending-action-probe-cross-slash-aoe-pending-agrias.extended.*`
- `work/live_unit_struct_snapshot.pending-action-probe-cross-slash-aoe-pending-ninja.extended.*`
- `work/live_unit_struct_snapshot.pending-action-probe-cross-slash-aoe-post.extended.*`
- `work/live-captures/battleprobe_log.pending-action-probe-cross-slash-aoe-post.snapshot.txt`

### Pre-Resolution State Was Still Clean

Immediately before the final Wait on Ninja, Cloud still had the expected pending state:

| Unit | HP | CT | `+0x61` | `+0x18D` | `+0x1A2` | `+0x1EF` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Cloud | `428` | `46` | `8` | `1` | `258` | `8` |

### HP Write Event Already Saw `+0x61` Cleared

At final resolution, the log recorded the two HP writes in this order:

- Ninja: `276 -> 3 = 273`
- Agrias: `322 -> 207 = 115`

The new pending-candidate line showed `Cloud s61=0` for both events. This means the known
caster-side pending flag at `+0x61` is cleared before the HP-write hook observes damage.

However, this first probe pass had a logging bug: `LogPendingActionCandidatesIfEnabled` reused the
normal `DUMP=0x180` unit snapshot, so fields beyond `+0x17F` were printed as `-1`:

- `+0x18D`
- `+0x1A2`
- `+0x1C4`
- `+0x1D8`
- `+0x1E5`
- `+0x1EF`

The bug was fixed after the test by making the pending-candidate logger read `0x200` bytes per unit
without changing the main hook buffer. Retest is needed to learn whether any of the higher offsets
survive into the HP-write event.

Interpretation so far:

- A pure "scan all currently pending units at HP write" resolver is probably too late if it depends
  on `+0x61`.
- The runtime likely needs to record pending actions before resolution, or find a better current
  executing action pointer.
- The exact HP-write event may still expose useful high-offset fields after the logger fix, but this
  remains unproven.

## Updated Working Hypotheses

1. **Forecast damage is stored on the target unit.** At least for Braver, `target+0x1C4` receives
   the forecast damage. Cross Slash confirmed the same pattern with `Ramza+0x1C4=187` and
   `Agrias+0x1C4=115`. It may persist through pending, but can clear before resolution. For AoE,
   secondary targets may never appear in this field.
2. **Confirmed/pending action identity is stored on the caster unit.** Braver set
   `caster+0x1A2=0x0101`; Cross Slash set `caster+0x1A2=0x0102`. This survives while pending and
   remains as last action after resolution.
3. **Pending state requires flags/timers, not just action id.** `caster+0x61`, `caster+0x18D`, and
   `caster+0x1EF` distinguish a live pending action from historical last action.
4. **The runtime should track pending actions continuously.** When a caster enters a pending state,
   record `(caster, action id, charge/timer)`. Forecast target/epicenter evidence can be retained
   as optional metadata, but final formula application should use the HP-write target at resolution.
5. **Wait remains a negative control.** Beowulf's Wait changes current-turn/CT state and may clear
   its own target-local forecast fields, but it does not erase Cloud's caster pending-action state.
   This separates action ownership from current active unit.

## Logging Profile

Use:

```powershell
work\battle-runtime-settings.action-context-probe.json
```

Properties:

- observe-only: no HP/MP rewrites;
- logs hook register snapshots for CT drops, HP events, and MP events;
- logs stack slots near the stable hook;
- scans readable non-unit register roots for known unit pointers;
- logs actor-probe unit windows so CT changes remain visible;
- keeps CT attacker diagnostics enabled only as fallback evidence.

Deploy with `codemod\prepare-live-mapping.ps1` **without** `-EnableModInAppConfig`. The user should
enable only these Reloaded-II mods:

- `fftivc.utility.modloader`
- `fftivc.generic.chronicle.codemod`

## External Scanner

Use `tools\scan_live_action_context_records.py` while the game is sitting in a controlled state:

```powershell
python tools\scan_live_action_context_records.py `
  --unit Cloud=0x1418562E0 `
  --unit Beowulf=0x1418564E0 `
  --unit Agrias=0x1418560E0 `
  --unit Ramza=0x141855CE0 `
  --unit Ninja=0x141855EE0 `
  --actor Cloud `
  --target Agrias `
  --near-bytes 0x600 `
  --max-span 0x900 `
  --output work\live_action_context_records.pending-agrias.md `
  --json-output work\live_action_context_records.pending-agrias.json
```

The scanner intentionally ignores forecast numbers and ranks compact records containing actor +
target pointers. It is meant to answer a narrower question than the forecast scanner: "where are the
caster and target stored together right now?"

## Next Live Protocol

Use Braver again because it is a safe delayed action with no MP cost and clear UI text.

1. Fresh launch through Reloaded-II with the action-context probe profile.
2. Let the battle sit until the log has `[UNIT]` lines for the party.
3. Baseline before selecting Braver.
4. Preview Braver on target A, capture forecast scanner and action-context scanner.
5. Confirm Braver, do not Wait yet. Capture scanners and later inspect `[HOOK-REGS-EVENT kind=ctdrop]`.
6. Wait with Cloud. Stop at Beowulf before choosing anything. Capture scanners.
7. Wait with Beowulf. After Braver resolves, stop at next controllable turn. Capture post.
8. Repeat with target B.

Expected useful result:

- a candidate that contains Cloud + target A while A is pending;
- the same shape contains Cloud + target B when B is pending;
- the candidate is absent or changed after resolution;
- hook-register pointer roots around Cloud's CT drop point to the same region or a parent queue.

If no raw pointer candidate survives, pivot to non-pointer storage:

- scan for unit char ids around the same regions;
- scan for map coordinates / target tile;
- use hook-register roots from the CT-drop event as anchors;
- look for action id / skill id changes rather than damage values.

## Round 7: Action-State Probe With High Offsets Fixed

Test:

- Cloud used Cross Slash centered on Agrias.
- Forecast showed Agrias damage `115`.
- The delayed action resolved after Beowulf, Agrias, and Ninja turns.
- Resolution hit:
  - Agrias: `322 -> 207 = 115`
  - Ninja: `276 -> 3 = 273`
- The next active unit after resolution was Ramza.

Artifacts:

- `work/live_unit_struct_snapshot.action-state-probe-cross-slash-aoe-baseline.extended.*`
- `work/live_unit_struct_snapshot.action-state-probe-cross-slash-aoe-forecast.extended.*`
- `work/live_unit_struct_snapshot.action-state-probe-cross-slash-aoe-confirmed.extended.*`
- `work/live_unit_struct_snapshot.action-state-probe-cross-slash-aoe-pending-beowulf.extended.*`
- `work/live_unit_struct_snapshot.action-state-probe-cross-slash-aoe-pending-agrias.extended.*`
- `work/live_unit_struct_snapshot.action-state-probe-cross-slash-aoe-pending-ninja.extended.*`
- `work/live_unit_struct_snapshot.action-state-probe-cross-slash-aoe-post.extended.*`
- `work/live_unit_struct_snapshot.action-state-probe-cross-slash-aoe-post-latest.extended.*`
- `work/live-captures/battleprobe_log.action-state-probe-cross-slash-aoe-post.snapshot.txt`
- `work/live-captures/battleprobe_log.action-state-probe-cross-slash-aoe-post.latest.txt`

Key log sequence:

```text
[ACTION-STATE Cloud] ... s61=8 t18D=1 act=258 f1EF=8
[ACTION-STATE Cloud] ... s61=8 t18D=255 act=258 f1EF=8
[ACTION-STATE Cloud] ... s61=0 t18D=255 act=258 f1EF=0
[ACTION-STATE Agrias] ... hp=207 dmg1C4=115 chg1D8=2 f1E5=128 bb=2
[DAMAGE Agrias] 322 -> 207 = 115
[PENDING-ACTION-CANDIDATES damage] ... Cloud s61=0 t18D=255 act=258 f1EF=0 ...
[ACTION-STATE Ninja] ... hp=3 dmg1C4=273 chg1D8=2 f1E5=136 bb=2
[DAMAGE Ninja] 276 -> 3 = 273
[PENDING-ACTION-CANDIDATES damage] ... Cloud s61=0 t18D=255 act=258 f1EF=0 ...
```

Findings:

1. Cloud stays in a recognizable pending-action state for most of the delay:
   `s61=8`, `act=258`, `f1EF=8`, and countdown-like `t18D`.
2. Immediately before HP damage events, the engine clears the live pending flags:
   `s61` becomes `0`, `f1EF` becomes `0`, and `t18D` is already `255`.
3. At HP-write time, Cloud still has `act=258`, but this is only "last action id"; it is not
   enough to prove that Cloud is the currently resolving caster by itself.
4. The target-side damage cache reappears for every actual AoE victim at resolution:
   - Agrias has `dmg1C4=115`.
   - Ninja has `dmg1C4=273`.
5. The stable HP hook register context still does not expose Cloud directly. For both damage
   events, `hookPtr`/`rcx` pointed at Ninja, not at Cloud.
6. CT fallback correctly failed here: no recent CT drop identified Cloud as attacker for the
   delayed AoE damage.

Interpretation:

- A pure "scan live pending units at HP write" resolver is too late for charged actions.
- The current HP hook remains valuable for final target and final observed damage.
- Delayed-action caster attribution now needs one of these:
  - an internal runtime pending/resolving action table captured before HP write;
  - a better pre-damage/current-executing-action hook;
  - both, with the table as an implementation path and the real hook as the long-term primary.

The most promising implementation path from this evidence is:

1. When a unit enters pending state (`s61=8`, `act!=0`, `f1EF=8`, timer not idle), record a
   pending action owned by that unit.
2. When that same unit transitions from pending to cleared while keeping the same `act`, mark that
   action as "resolving" for a short batch window.
3. Attach HP events inside that window to the resolving action, using the HP-write target as the
   real affected unit and `dmg1C4` as a validation signal.
4. For overlapping charged actions, disambiguate by action timing, charge countdown, affected target
   cache, and eventually target/epicenter metadata if we can find it.

This does not remove the need to search for a real executing-action pointer, but it gives us a
concrete code-mod path that is stronger than CT-only attribution.

## 2026-06-24 Update: executing-action context found (battle actor array)

The search for a real executing-action context (referenced above as still needed) has a concrete
answer for damage-time attribution. See `docs/modding/12-runtime-register-action-context-book.md`
section 2.4 and `work/action-context-checkpoint-2026-06-22.md` for full detail.

At the native pre-clamp frame the engine exposes a per-participant actor array (stride `0x548`,
`actor+0x148` -> unit). The resolving caster's actor struct sits on the pre-clamp stack next to the
current target's actor, and the resolving action id is stored inside the caster actor at `+0x142`
(also `0x17A/0x18C/0x1BC`).

For the charged actions investigated here this was validated live:

- Cross Slash: caster actor (Cloud) `+0x142 = 258` (`0x0102`).
- Braver: caster actor (Cloud) `+0x142 = 257` (`0x0101`).
- All non-caster actors had `0`, confirming it is the caster's current action id.

This means delayed/charged caster+action can be read from memory at damage time without CT and
without the pending-clear heuristic. The pending-action tracker remains valid and is still the proven
path until a memory-only resolver is implemented and validated, especially for overlapping pending
actions (multiple casters charged simultaneously), which this single-caster test did not cover.
