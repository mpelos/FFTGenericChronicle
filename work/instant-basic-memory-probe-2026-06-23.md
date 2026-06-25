# Instant Basic Attack Memory Probe - 2026-06-23

## Goal

Map what memory/register context exists around a simple instant/basic attack before trying another
formula rewrite. The concrete live action was:

- Agrias basic attack -> Beowulf.
- Data mod disabled.
- Observe-only/log-only profile:
  `work\battle-runtime-settings.instant-basic-memory-probe.json`.
- No critical.

User observation:

```text
preview: 151
UI: 151
Beowulf HP loss: 151
critical: no
```

Log artifact:

- `work\live-captures\battleprobe_log.instant-basic-memory-probe-agrias-beowulf.snapshot.txt`

## Key Unit Pointers

From this capture:

```text
Agrias  = 0x1418560E0 / id=0x1E
Beowulf = 0x1418564E0 / id=0x1F
```

## Timeline

### 1. Agrias enters active/basic-action state

The stable `battle_base_ptr` hook touched Agrias:

```text
[HOOK-REGS count=4 ptr=0x1418560E0 id=0x1E]
  rcx=0x1418560E0
  rdi=0x1418560E0
  r8 =0x1418564E0
```

At this point the runtime did not yet know `0x1418564E0` was Beowulf, so the text classifier printed
it as `module+0x18564E0`. After later registration, exact pointer comparison proves this was Beowulf.

Action-state transition:

```text
[ACTION-STATE Agrias] act=0 dmg1C4=0 b8=1 ba=0 bb=0
[ACTION-BOUNDARY Agrias] +0x1A0:00->12 +0x1A1:00->01 +0x1BA:00->01
[HOOK-REGS-EVENT kind=actionboundary] rcx=Agrias rdi=Agrias r8=Beowulf
```

Important interpretation:

- For this basic attack, `+0x1A2 actionId` stays `0`.
- `+0x1BA ba=1` marks Agrias as executing/active.
- `+0x1A0/+0x1A1` changed at the same moment; their exact meaning is still unknown.
- The stable hook carries **source in rcx/rdi** and **target in r8** before HP apply.

### 2. Pre-clamp HP apply fires on Beowulf

The native pre-clamp hook at `0x30A66F` fired:

```text
[PRECLAMP-REWRITE event=1 ptr=0x1418564E0 state=0x14185669E id=0x1F
 hp=314/314 oldDebit=151 oldCredit=0 flags=0x1
 pre=hp=314 ... dmg1C4=151 ... bb=1
 live=hp=163 ... dmg1C4=151 ... bb=2]
 regs:
   rcx=Beowulf
   rdi=Beowulf
   r8=Beowulf
   rbp=0x14185669E
```

Important interpretation:

- This hook is early enough to see Beowulf's pre-apply HP (`314`) and staged debit (`151`).
- `live=hp=163` appears because the polling/log thread reads the unit after the hook already
  returned and vanilla wrote HP.
- `rbp = target + 0x1BE`; `[rbp+6]` is exactly `target+0x1C4` (`dmg1C4=151`).
- The `stateDump` is not a separate action object. It is a tail window inside Beowulf's unit struct.
- At pre-clamp time the registers/stack captured here show the **target**, but do not show Agrias.

### 3. Later managed target-cache observation is too late

After vanilla apply, the managed poll sees:

```text
[PENDING-ACTION-TARGET enter target=Beowulf ... dmg1C4=151 ... bb=2]
[PRECLAMP-IMMEDIATE-CANDIDATES ... selected=Agrias ...]
[PRECLAMP-FORMULA-CANDIDATE ... attacker=Agrias ... queuedPlan=0]
```

This proves the earlier managed candidate path was too late for basic attacks. The source can be
resolved after the fact, but by then the native pre-clamp hook has already run.

## Current Conclusions

1. The basic attack path likely does **not** create the same pending-action batch as delayed/charged
   actions.
2. The target-cache landmarks `0x2D7AC0` / `0x2D7AEC` did not emit hits in this capture, despite
   installing successfully. Basic attacks may populate `unit+0x1C4` through a different path.
3. The native pre-clamp hook at `0x30A66F` is still the right result-application point:
   - it sees pre-apply HP;
   - it sees staged debit;
   - changing this debit is already live-proven to change UI/HP/KO.
4. For instant/basic actions, the missing bridge is an **early immediate-action context** captured
   from the stable hook:

   ```text
   source = rcx/rdi touched unit (Agrias)
   target = r8 if r8 is a valid battle unit pointer (Beowulf)
   action = implicit/basic when +0x1A2 == 0 and +0x1BA == 1
   ```

5. At pre-clamp time, only the target is obvious. So source/target/action must be remembered before
   pre-clamp, not discovered there.

## Implication For The Code Mod

The likely architecture for instant/basic actions is:

```text
stable unit hook observes source+target registers
-> create short-lived immediate action context/plan
-> compute formula from source/target/action if enough context exists
-> pre-clamp hook matches target and staged debit
-> rewrite debit before vanilla HP apply
```

For a GURPS-like redesign, this is promising because the custom formula usually does not need the
vanilla damage number. It needs source stats, target stats/DR, weapon/action class, and maybe
engine hit/crit/evasion result. If the engine has already decided that HP damage is occurring, the
pre-clamp hook is a natural place to replace the amount.

## Open Questions

1. Is `r8` reliably the target for:
   - other basic attackers/targets;
   - dual wield multi-hit;
   - friendly-fire;
   - movement+attack vs attack-only?
2. Does `r8` point to a tile/target object rather than a unit for AoE or non-unit target actions?
3. What do `+0x1A0/+0x1A1` mean during a basic action?
4. Does the same source+target register pattern appear for reaction attacks such as Counter/Hamedo?
5. For miss/evade/block/critical, when do we know the final result, and is there a staged debit at
   `+0x1C4` only when HP damage will actually happen?
6. Can the pre-clamp plan table allow wildcard `expectedDebit=-1` so a context captured before
   vanilla damage is computed can still match the target at apply time?

## Recommended Next Tests

Keep the current observe-only profile, but improve register classification so possible unit pointers
are recognized even before they have been registered by the hook.

Then run small isolated tests:

1. Agrias basic attack -> Beowulf again, to confirm reproducibility of `r8=target`.
2. Ramza basic attack -> Beowulf or Agrias, preferably with move+attack once and attack-only once.
3. Ninja dual wield -> Agrias, to see whether one early source+target context covers two pre-clamp
   events.
4. A simple counter/reaction setup, later, after the basic/dual-wield path is stable.

## Live Probe B: Ramza Basic Attack -> Beowulf, Lethal 912

User report:

```text
Ramza attacked Beowulf
shown damage: 912
Beowulf died
```

Log artifact:

- `work\live-captures\battleprobe_log.instant-basic-memory-probe-ramza-beowulf-lethal.snapshot.txt`

Important units in this capture:

- Ramza/source, per user action: `0x141855CE0`, char id `0x01`.
- Beowulf/target: `0x1418564E0`, char id `0x1F`.

### 1. Source+target register pattern repeats

At the stable hook before HP application:

```text
[HOOK-REGS ptr=0x141855CE0 id=0x01]
rcx = 0x141855CE0  source unit
rdi = 0x141855CE0  source unit
r8  = 0x1418564E0  target unit
```

The action-boundary event for Ramza also shows:

```text
prev: b8=1 ba=0 bb=0
curr: b8=0 ba=1 bb=0
diff: +0x1A0:00->10 +0x1A1:00->01 +0x1B8:01->00 +0x1B9:01->03 +0x1BA:00->01
```

This supports the same interpretation as the Agrias capture: instant/basic actions expose the
source through `rcx/rdi` and the current target through `r8` before native HP application.

### 2. Lethal staged debit and native KO path

The target acquired a staged debit before the native pre-clamp hook:

```text
[PENDING-ACTION-TARGET enter target=0x1418564E0/id=0x1F
 dmg1C4=912 chg1D8=130 f1E5=136 bb=0]
```

At native pre-clamp:

```text
[PRECLAMP-REWRITE event=2 ptr=0x1418564E0 id=0x1F
 hp=314/314 oldDebit=912 oldCredit=0
 pre=hp=314 ... s61=0 ... dmg1C4=912 ... f1EF=0 ... bb=1
 live=hp=0 ... s61=32 ... dmg1C4=912 ... f1EF=32 ... bb=1]
```

And the managed HP probe confirmed:

```text
[HP-EVENT-PROBE kind=damage ptr=0x1418564E0 id=0x1F
 prevHp=314 currentHp=0 appliedHpLoss=314 rawForecastDamage=912
 lethal=1 hpClamp=1 rawForecastOverkill=598]
```

Interpretation:

- `unit+0x1C4` held the raw/staged damage value `912`.
- The engine applied only `314` HP loss because Beowulf had `314/314` HP.
- The same native path set KO/death state:
  - `+0x61: 0x00 -> 0x20`;
  - `+0x1EF: 0x00 -> 0x20`.
- This is exactly the behavior the pre-clamp staged-debit architecture wants: custom formula writes
  the staged debit, then vanilla owns clamp, UI number, HP write, and KO.

### 3. Non-damage/credit noise exists

Before the lethal hit, there was another pre-clamp event:

```text
[PRECLAMP-REWRITE event=1 ptr=0x141855EE0 id=0x80
 hp=277/277 oldDebit=0 oldCredit=34]
```

This is not the Ramza -> Beowulf hit. It is a separate credit-like event on another unit. The
important implementation implication is that the immediate context bridge must only treat a
pre-clamp event as damage when the staged debit is positive and the target matches the pending
context/plan. Credits/heals/automatic ticks must be ignored or routed separately.

### 4. Immediate candidate scoring sees the right source after the fact

The managed candidate scorer, which runs too late for pre-clamp rewriting in the current probe,
ranked Ramza as the immediate source:

```text
[PRECLAMP-IMMEDIATE-CANDIDATES target=0x1418564E0/id=0x1F oldDebit=912
 selected=0x141855CE0/id=0x01/act=0/score=2300]

[PRECLAMP-FORMULA-CANDIDATE ... oldDebit=912 ... attacker=0x141855CE0/id=0x01
 source=immediate-action]
```

This is useful validation of the source heuristic, but not sufficient by itself. The source context
must be captured earlier from the stable hook if we want the native pre-clamp hook to rewrite the
same hit.

## Updated Confidence After Probe B

Higher confidence:

- `r8` is very likely the current unit target for instant/basic single-target actions.
- `rcx/rdi` at the stable hook identify the unit currently being processed; near the action
  boundary this is the source/attacker.
- The native pre-clamp staged debit path correctly handles lethal overkill and lets the engine set
  KO.

Still not proven:

- Whether one captured source+target context safely covers dual wield's two hits.
- Whether reaction attacks expose the same source/target pattern or invert timing.
- Whether `r8` remains a unit pointer for AoE, tile-targeted, or no-target actions.
- Whether miss/evade/block events produce no staged debit, zero staged debit, or a separate event.

Best next live probe:

```text
Ninja basic dual wield -> Agrias
```

Reason: it tests whether one immediate source/target context can feed multiple pre-clamp damage
events in the same action. This is the next fragile case before turning the observation into a
writer.

## Live Probe C: Ninja Dual Wield -> Agrias, 180 + 180, Lethal By Second Hit

User report:

```text
Ninja dual wield -> Agrias
shown damage: 180 / 180
Agrias lost all 322 HP
```

Log artifact:

- `work\live-captures\battleprobe_log.instant-basic-memory-probe-ninja-dual-agrias-lethal.snapshot.txt`

Important units in this capture:

- Ninja/source: `0x141855EE0`, char id `0x80`.
- Agrias/target: `0x1418560E0`, char id `0x1E`, HP `322/322`.
- Beowulf: `0x1418564E0`, char id `0x1F`.

### 1. Dual wield is two separate native pre-clamp damage events

First hit:

```text
[PRECLAMP-REWRITE event=1 ptr=0x1418560E0 id=0x1E
 hp=322/322 oldDebit=180 oldCredit=0
 pre=hp=322 ... dmg1C4=180 ... bb=1
 live=hp=142 ... dmg1C4=180 ... bb=2]

[HP-EVENT-PROBE kind=damage event=1
 prevHp=322 currentHp=142 appliedHpLoss=180 rawForecastDamage=180
 lethal=0 hpClamp=0]
```

Second hit:

```text
[PRECLAMP-REWRITE event=2 ptr=0x1418560E0 id=0x1E
 hp=142/322 oldDebit=180 oldCredit=0
 pre=hp=142 ... dmg1C4=180 ... bb=1
 live=hp=0 ... s61=32 ... f1EF=32 ... bb=1]

[HP-EVENT-PROBE kind=damage event=2
 prevHp=142 currentHp=0 appliedHpLoss=142 rawForecastDamage=180
 lethal=1 hpClamp=1 rawForecastOverkill=38]
```

Interpretation:

- Dual wield does not aggregate damage.
- Each hit reuses the same target staged damage field:
  - `Agrias+0x1C4 = 180`;
  - `Agrias+0x1D8 = 130`;
  - `Agrias+0x1E5 = 128`.
- The second hit is a normal lethal clamp: staged raw damage `180`, available HP `142`, final HP `0`,
  KO bits set by vanilla.

### 2. Source stays visible as the active unit across both hits

Ninja's action boundary:

```text
[ACTION-BOUNDARY ptr=0x141855EE0 id=0x80
 prev=... b8=1/ba=0/bb=0
 curr=... b8=1/ba=1/bb=0]
diff=+0x1A0:00->11 +0x1A1:00->01 +0x1BA:00->01
```

The immediate source candidate became:

```text
[PRECLAMP-IMMEDIATE-CANDIDATES target=Agrias oldDebit=180
 selected=0x141855EE0/id=0x80/act=0/score=2300]
```

And the second hit's HP-event candidate still identifies the Ninja:

```text
[IMMEDIATE-ACTION-CANDIDATES kind=damage event=2 target=Agrias
 0x141855EE0/id=0x80 role=active-source-like score=2200 currentActive=1 freshActive=1]
```

This supports a source model based on the current active/executing unit, not on CT.

### 3. `r8` is not a reliable target pointer

In this capture, while Agrias is the real target, the stable hook often shows:

```text
rcx/rdi = 0x141855EE0  Ninja/source
r8      = 0x1418564E0  Beowulf, not Agrias
```

So the earlier `r8 == target` hypothesis is unsafe. It was true in the Agrias -> Beowulf and
Ramza -> Beowulf single-target samples, but not in this dual-wield sample.

Updated interpretation:

- Use the native pre-clamp target pointer / HP event target as the authoritative target.
- Use the source-side action state (`ba=1`, source-like active state, recent source hook) as the
  authoritative source.
- Treat `r8` as a diagnostic clue only, not as a required source of truth.

### 4. Implementation implication

The immediate/basic writer should not depend on an early `source+target` register pair. A better
bridge is:

```text
stable hook observes and refreshes current active source
-> target unit receives staged damage at +0x1C4
-> managed side creates/refreshes a short-lived source-owned action context
-> native pre-clamp hook matches the actual target pointer and positive oldDebit
-> rewrite staged debit before vanilla HP apply
-> keep the source context alive for multi-hit until the source action phase ends
```

For dual wield, a one-shot plan is not enough if it is consumed by the first hit. The plan/context
needs one of:

- per-source-action multi-hit allowance;
- or a reusable short-lived plan that can rewrite multiple matching target damage events;
- or a managed requeue on target phase changes before the next native pre-clamp.

The safest near-term implementation is a configurable immediate-action plan with:

- target pointer match;
- positive `oldDebit`;
- short expiry;
- small max writes, initially `2`, for basic dual-wield coverage;
- source context retained while the source remains action-active.

## Implementation Pass After Probe C

Implemented in `codemod\fftivc.generic.chronicle.codemod\Mod.cs`:

- Native pre-clamp plan slots now actually respect `PLAN_MAX_WRITES`.
  - Previously the plan slot was deactivated after the first match even when `maxWrites > 1`.
  - Now a plan remains active until `writeCount >= maxWrites`.
- Native pre-clamp plan matching now supports `-1` wildcard values for:
  - expected HP;
  - expected MaxHP;
  - expected debit;
  - expected credit.
- Immediate-action plans can override generic pending-action plan behavior:
  - `PreClampImmediateActionPlanMaxWrites`;
  - `PreClampImmediateActionPlanRequireExpectedHp`.
- `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json` now sets:
  - `PreClampImmediateActionPlanMaxWrites = 2`;
  - `PreClampImmediateActionPlanRequireExpectedHp = false`.

Why this exact shape:

- Charged/delayed plans keep the stricter existing behavior by default.
- Immediate/basic plans can survive dual wield's two separate native HP applies.
- The authoritative target is still the native pre-clamp `rdi`/event target; `r8` is not used as
  target truth.

Verification:

```text
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
```

Result:

```text
Offline checks passed.
```

Deployed live profile:

- `work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json`
- Installed to:
  - `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.dll`
  - `C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`

Next live test:

```text
Ninja dual wield -> Agrias
```

Expected if immediate multi-hit pre-clamp plans work:

- Vanilla preview may still be `180 / 180`.
- Runtime formula is `max(1, a.pa * 10 - t.faith)`.
- From Probe C stats:
  - Ninja PA = `15`;
  - Agrias Faith = `63`;
  - expected custom hit = `150 - 63 = 87`.
- Expected live outcome:
  - UI/applied damage should be about `87 / 87`;
  - Agrias should lose about `174` total, not die from full `322`.

Evidence to confirm in log:

```text
[PRECLAMP-PLAN-QUEUE ... context=immediate-action ... maxWrites=2 expectedHp=any]
[PRECLAMP-REWRITE ... oldDebit=180 forcedDebit=87 ...]
[PRECLAMP-REWRITE ... oldDebit=180 forcedDebit=87 ...]
```

## 2026-06-23 Live Failure: Target Cache Plan Was Too Late

Live test result after the first implementation pass:

```text
Ninja dual wield -> Agrias
UI/applied: 180 / 180
Agrias HP loss: 322, KO
```

Log snapshot:

```text
work\live-captures\battleprobe_log.preclamp-plan-immediate-ninja-dual-agrias-failed-180x2.snapshot.txt
```

Important evidence:

- Active profile was correct:
  - `PreClampImmediateActionPlanMaxWrites=2`;
  - `PreClampImmediateActionPlanRequireExpectedHp=false`;
  - data mod disabled.
- There was no `[PRECLAMP-PLAN-QUEUE]` before the hits and no `[PRECLAMP-REWRITE]`.
- `[PRECLAMP-FORMULA-RUNTIME ... final=87]` appeared only after target HP was already `0/322`.

Conclusion:

```text
Polling for the target-side +0x1C4 cache is too late for immediate/basic attacks.
```

For immediate/basic actions, the native pre-clamp hook can apply the rewrite, but managed polling
cannot wait for the target cache to appear and then still populate a plan before the native damage
read. The plan must exist before the target's pre-clamp event.

## 2026-06-23 Implementation: Eager Immediate Target Plans

Implemented a second immediate/basic path:

```text
source active state observed (ba=1)
-> discover nearby unit structs by 0x200 stride
-> prequeue one formula-backed plan per possible target
-> native pre-clamp hook selects the real target by pointer
-> plan only matches positive staged debit
```

Code changes:

- Added nearby unit discovery for eager immediate plans:
  - `BattleUnitStride = 0x200`;
  - `PreClampImmediateActionNearbyUnitScanRadius`;
  - discovered units are also seeded into `_unitObservations` so same-frame action-probe reads work.
- Added `PreClampImmediateActionPlanEagerTargets`.
- Added native plan expected-debit sentinel:
  - `-1` = any;
  - `-2` = positive staged debit only.
- Eager plans evaluate:
  - source = active immediate source-like unit;
  - target = each discovered possible target;
  - synthetic vanilla damage = `1` for formula evaluation.
- Plan matching remains target-safe because the hook still requires the real native target pointer.

Updated live profile:

```text
work\battle-runtime-settings.preclamp-plan-immediate-basic-demo.json
```

Now includes:

```text
PreClampImmediateActionPlanEagerTargets=true
PreClampImmediateActionNearbyUnitScanRadius=8
PreClampImmediateActionPlanMaxWrites=2
PreClampImmediateActionPlanRequireExpectedHp=false
```

Verification:

```text
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
Offline checks passed.
```

Deployed to:

```text
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod
```

Next live test remains:

```text
Ninja dual wield -> Agrias
```

Expected if eager plans work:

```text
Preview/UI forecast may still show 180 / 180.
Applied damage should become about 87 / 87.
Agrias should lose about 174 total and survive.
```

Evidence to confirm:

```text
[PRECLAMP-EAGER-PLAN-CANDIDATE ... source=Ninja ... target=Agrias ... forcedDebit=87 ... queuedPlan=1]
[PRECLAMP-PLAN-QUEUE ... oldDebit=positive ... forcedDebit=87 ... maxWrites=2 ... expectedHp=any]
[PRECLAMP-REWRITE ... oldDebit=180 ... forcedDebit=87 ...]
[PRECLAMP-REWRITE ... oldDebit=180 ... forcedDebit=87 ...]
```

## 2026-06-24 Live Failure: Eager Scan Accepted a Ghost Struct

Live result:

```text
Ninja dual wield -> Agrias
First hit showed 999 and killed Agrias.
Second hit whiffed because the target was already dead.
```

Snapshot:

```text
work\live-captures\battleprobe_log.preclamp-eager-immediate-ninja-agrias-999.snapshot.txt
```

Cause:

- The eager nearby-unit stride scan found a false-positive struct at `0x141856AE0`.
- It passed the old `TryCreateUnitSnapshot` checks because level/HP/MP were superficially valid.
- Its real fields were nonsense for a battle unit:

```text
id=0x66 team=58 Lv1 HP620 MP762 PA115 MA3 Sp174 CT3 Mv195 Jp3 Br1 Fa1
action=s61=7/t18D=6/act=33265/dmg1C4=49281/chg1D8=221/f1E5=144/f1EF=144/b8=129/ba=144/bb=224
```

- Immediate source scoring treated `ba != 0` as active, so `ba=144` became "current active".
- That ghost source produced:

```text
FinalDamageFormula = ghost.PA * 10 - Agrias.Faith = 1150 - 63 = 1087
```

- Native pre-clamp correctly applied the first matching Agrias plan:

```text
[PRECLAMP-REWRITE ... oldDebit=180 forcedDebit=1087 ...]
```

So the hook/target path worked; the source validation was wrong.

Fix deployed:

- Action source marker now requires `ActiveMarker2 == 1`; non-zero garbage no longer counts.
- `TrackActionProbeAges`, eager immediate queueing, and immediate candidate identity checks all use
  the exact marker.
- `TryCreateUnitSnapshot` now rejects obviously impossible battle-unit stats:
  - team > 16;
  - CT > 100;
  - PA/MA/Speed > 127;
  - Move/Jump > 32;
  - Brave/Faith > 100.
- The immediate live profile now uses:

```text
PreClampImmediateActionNearbyUnitScanRadius=4
```

This radius still covers the five real units in the current test formation but excludes the ghost
slot seen in the failed capture. The stat sanity checks are the primary protection; the smaller
radius is an extra live-test guardrail.

Verification:

```text
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
Offline checks passed.
```

Redeployed to:

```text
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod
```

Next live test remains:

```text
Ninja dual wield -> Agrias
```

Expected now:

```text
No ghost source in the log.
Plans for Agrias should use source=Ninja/id=0x80 and forcedDebit=87.
Applied damage should be about 87 / 87; Agrias should survive.
```

## 2026-06-24 Live Success: Immediate Dual Wield Rewritten Pre-Clamp

Live result after the ghost-source fix:

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

Confirmed evidence:

```text
[PRECLAMP-EAGER-PLAN-CANDIDATE ... source=0x141855EE0/id=0x80 target=0x1418560E0/id=0x1E ... forcedDebit=87 ... queuedPlan=1]
[PRECLAMP-PLAN-QUEUE ... ptr=0x1418560E0 id=0x1E ... oldDebit=positive ... forcedDebit=87 ... maxWrites=2 ...]
[PRECLAMP-REWRITE event=1 ... oldDebit=180 ... forcedDebit=87 ... live=hp=235 ... dmg1C4=87 ...]
[DAMAGE ptr=0x1418560E0 id=0x1E] 322 -> 235 = 87
[PRECLAMP-REWRITE event=2 ... oldDebit=180 ... forcedDebit=87 ... live=hp=148 ... dmg1C4=87 ...]
[DAMAGE ptr=0x1418560E0 id=0x1E] 235 -> 148 = 87
```

What this proves:

- The immediate/basic path can rewrite staged native damage before vanilla HP apply.
- The vanilla UI preview can remain `180` while the actual applied damage becomes our custom
  formula result.
- Dual wield is two separate native pre-clamp events, and the current immediate plan shape can
  rewrite both hits.
- Source was resolved without CT:

```text
source = active unit with ba == 1 = Ninja/id=0x80
target = native pre-clamp ptr = Agrias/id=0x1E
```

Formula used:

```text
max(1, attacker.pa * 10 - target.faith)
= max(1, 15 * 10 - 63)
= 87
```

Remaining caveat observed in the success log:

- Eager plans continue to be refreshed repeatedly while the source remains action-active.
- This is safe in the controlled test because native target pointer + positive debit still gates
  the rewrite, but it is noisy and should be tightened before broad testing.
- Follow-up: stop/expire eager source contexts more aggressively once the source action phase ends
  or after the expected hit batch has completed.
