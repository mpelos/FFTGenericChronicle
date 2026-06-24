# Runtime Register And Action Context Book

Status: living canonical model.

Last major update: 2026-06-24, after discovering the battle participant/actor array at the native
pre-clamp frame (executing-action-pointer probe, Cross Slash AoE).

This document is the organized "book" for what we have learned about FFT Ivalice Chronicles'
battle runtime registers, unit structs, action state, and damage application path. Raw logs and
per-test notes remain useful evidence, but this file should be the first place to update when a
new memory/register fact changes our mental model.

## 0. How To Read This Book

The goal is not just to list offsets. The goal is to explain how the battle engine appears to work
around action selection, delayed actions, HP application, and the hooks our code mod can use.

Confidence labels:

- **Proven live**: reproduced by controlled live tests and logs.
- **Strong**: repeatedly observed and used successfully, but not exhaustively tested.
- **Working hypothesis**: useful model, still needs edge-case tests.
- **Refuted**: we tested it and should not build on it.

Core vocabulary:

- **Unit struct**: the per-battle unit object. The pointer changes per game launch, but the offsets
  inside the struct have been stable.
- **Stable unit hook**: the known non-virtualized hook around `battle_base_ptr`, where `rcx` is a
  unit pointer and the game reads `unit+0x30` HP.
- **Native pre-clamp hook**: the later native hook that sees staged debit/credit immediately before
  vanilla HP application and KO handling.
- **Staged debit**: the raw damage value stored near `unit+0x1C4` before the engine subtracts HP.
- **Pending action**: a delayed/charged action stored on the caster while waiting to resolve.
- **Active source**: the unit currently executing an immediate/basic action, usually marked by
  `unit+0x1BA == 1`.

## 1. Big Picture Runtime Model

The current proven architecture is:

```text
observe action/context memory
-> build custom formula context
-> rewrite native staged debit pre-clamp
-> let vanilla engine apply HP, clamp, UI number, and KO/death
```

This is a major upgrade over the older HP-write path.

Earlier we proved that directly writing HP to zero and/or setting KO status bytes creates
zombie-like state and does not trigger proper engine death. The current path avoids that by
modifying the number the engine is already about to consume. The engine remains responsible for
final HP application and KO.

Current split of responsibility:

- Code mod owns the combat number.
- Engine owns HP clamp, death/KO lifecycle, and final effect application.
- HP-write target is the authoritative final impacted unit.
- CT is fallback/diagnostic, not the primary action-context source.

## 2. Hook Map

### 2.1 Stable Unit Hook

Status: **proven live** for unit observation; **not semantically ideal** as a full action hook.

Known anchor:

```text
Name: battle_base_ptr / stable unit touchpoint
Signature context: 0F B7 41 30 66 89 42 0C
Approx module offset: module+0x226D98
Important instruction behavior: reads unit+0x30 HP
Observed register: rcx = touched unit pointer
```

What it is good for:

- Registering battle unit pointers.
- Reading stable unit fields.
- Observing CT, HP, MP, pending/action state changes.
- Capturing broad register/stack context around unit touches.
- Finding active-source markers for immediate/basic actions.

What it is not guaranteed to be:

- The true damage formula routine.
- The true action dispatcher.
- The exact CPU frame that applies HP.
- A reliable current-caster pointer for delayed actions.

Important register lessons:

- In simple single-target basic attacks, `rcx/rdi` at the source action boundary were the source,
  and `r8` sometimes pointed to the target.
- In Ninja dual wield, `r8` often pointed to Beowulf while Agrias was the real target.
- Therefore `r8` is diagnostic only. Do not treat it as target truth.

### 2.2 Native Pre-Clamp Hook

Status: **proven live** and currently the most important formula-write hook.

Known anchor:

```text
Approx RVA: 0x30A66F
Expected bytes: 0F BF 45 06
Observed purpose: reads staged debit before vanilla HP application
```

Observed register/state model:

```text
rdi = authoritative target unit pointer
rcx/r8 often also target in successful captures, but do not rely on them over rdi/event target
rbp = target + 0x1BE
[rbp+6] = target + 0x1C4 = staged debit / raw damage
[rbp+8] = staged credit / heal-like value
```

What this hook gives us:

- Pre-apply target HP.
- Raw staged damage (`oldDebit`) before clamp.
- Raw staged credit (`oldCredit`) for heal/credit-like cases.
- A place to rewrite damage before vanilla applies HP and KO.
- A native-frame register/stack capture point for hunting a current executing action object.

What was proven:

- Charged Cross Slash AoE rewrite worked:
  - Agrias `115 -> 77`
  - Ninja `273 -> 68`
- Lethal Braver rewrite worked:
  - forced staged debit `9999`
  - UI showed `999`
  - Beowulf died via vanilla KO.
- Immediate Ninja dual wield rewrite worked:
  - both hits `180 -> 87`
  - Agrias HP `322 -> 235 -> 148`

Noise to remember:

- The hook can also see credit/heal-like staged events where `oldDebit=0` and `oldCredit>0`.
- Damage code must explicitly require a positive staged debit unless the profile is intentionally
  handling healing/credit events.

Critical implementation rule:

```text
target = native pre-clamp unit pointer / HP event pointer
```

This is stronger than any forecast target, `r8`, CT guess, or UI focus state.

### 2.3 Refuted Or Noisy Memory Leads

Status: **important negative evidence**.

`0x03C36740..0x03C36920` looked promising during early forecast scans because it contained a unit
pointer and forecast-like values. Later Braver tests refuted it as a pending-action source:

```text
Preview Braver -> Agrias: slot followed Agrias
Confirmed/pending while Beowulf had control: slot followed Beowulf
Post-resolution: slot followed current focus again
```

Conclusion:

```text
0x03C367xx is UI/current-focus state, not the durable scheduled action object.
```

Other noisy leads:

- global numeric scans for values like `Cloud`, `Braver`, `153`, and timer `2` produced many
  persistent static/UI/cache candidates;
- raw pointer scans can find compact records containing unit pointers, but no proven current
  executing action object has been isolated yet;
- single values are too noisy. A real candidate must correlate actor, action id, timing, and target
  or epicenter across baseline, forecast, confirmed, pending, resolution, and post states.

### 2.4 Battle Participant / Actor Array

Status: **proven live** (one session) as a structure; **stability across launches still unverified**.

Discovered by the `executing-action-pointer-probe` during Cloud Cross Slash AoE. At the native
pre-clamp frame, the pointer scan found a contiguous array of per-participant "actor" structs.

```text
actor module+0xD31FE8 -> unit 0x141855CE0 (Ramza)
actor module+0xD32530 -> unit 0x141855EE0 (Ninja)
actor module+0xD32A78 -> unit 0x1418560E0 (Agrias)
actor module+0xD32FC0 -> unit 0x1418562E0 (Cloud / caster)
```

Layout:

- contiguous array, stride `0x548`;
- `actor+0x148` = pointer to the unit struct (this is how the scan identifies an actor: a root whose
  `+0x148` dereferences to a registered unit);
- `actor+0x0` = pointer to `(this - 0x548)` = previous array element (back-link).

Why it matters for action context:

- During each AoE HP-apply event, the caster's actor struct is present on the pre-clamp stack next to
  the current target's actor struct:

```text
Ninja hit  (oldDebit=273): stack+0x20 -> Cloud actor (caster), stack+0x50 -> Ninja actor (target)
Agrias hit (oldDebit=115): stack+0x60 -> Cloud actor (caster), stack+0x50 -> Agrias actor (target)
```

- The caster actor is constant across the AoE batch; the target actor varies.
- Stack slot index is not fixed, so the discriminator is by content, not by slot:

```text
caster = stack actor whose +0x148 unit != current pre-clamp target
```

- Native pre-clamp registers still only carry the target (`rcx/rdi/r8`); the caster never appeared in
  a register, only as an actor-struct pointer on the stack.

This is the strongest candidate so far for a real engine "current executing action context", and it
is reachable straight from memory at damage time. It could retire both CT and the pending-clear
heuristic if the resolving action id also lives inside the actor struct.

Open questions (being tested by the actor-struct dump probe):

- Does the resolving action id (`0x0102` = 258 for Cross Slash) live inside the actor struct?
- Is there a target pointer / target list / current-target index in the caster actor struct?
- Is the `module+0xD32xxx` actor RVA stable across game launches, or is it session-allocated like the
  unit pool?

Probe support: `PreClampActorStructDumpEnabled` / `...DumpBytes` / `...UnitOffset` (default `0x148`) /
`...DumpMaxLogs` emit `[PRECLAMP-ACTOR-DUMP root=0x... unit=0x.../id=0x.. bytes=N] <hex>` for any
scanned root that links to a registered unit at the actor->unit offset. Profile:
`work/battle-runtime-settings.executing-action-actor-dump-probe.json`.

Evidence:

- `work/live-captures/battleprobe_log.executing-action-pointer-probe-resolved-cross-slash-agrias-ninja.snapshot.txt`

## 3. Unit Struct Map

Offsets below are relative to the unit pointer.

### 3.1 Core Combat Stats

Status: **proven live**.

| Offset | Meaning | Width | Notes |
| --- | --- | ---: | --- |
| `+0x00` | Character id | byte | Examples: Ramza `0x01`, Cloud `0x32`, Ninja `0x80` |
| `+0x04` | Team/group id | byte | Used in sanity validation |
| `+0x05` | Friend/foe flags | byte | Bit `0x10` observed as foe-ish |
| `+0x28` | EXP | byte | Stable stat block |
| `+0x29` | Level | byte | Stable stat block |
| `+0x2A` | Max Brave | byte | Stable stat block |
| `+0x2B` | Brave | byte | Stable stat block |
| `+0x2C` | Max Faith | byte | Stable stat block |
| `+0x2D` | Faith | byte | Stable stat block |
| `+0x30` | HP | word | Current HP |
| `+0x32` | Max HP | word | Max HP |
| `+0x34` | MP | word | Current MP |
| `+0x36` | Max MP | word | Max MP |
| `+0x3E` | PA | byte | Physical Attack |
| `+0x3F` | MA | byte | Magical Attack |
| `+0x40` | Speed | byte | Speed |
| `+0x41` | CT | byte | Charge Time |
| `+0x42` | Move | byte | Movement |
| `+0x43` | Jump | byte | Jump |

Sanity validation now rejects impossible candidates such as:

- team greater than `16`;
- CT greater than `100`;
- PA/MA/Speed greater than `127`;
- Move/Jump greater than `32`;
- Brave/Faith greater than `100`.

This validation exists because a stride scan once accepted a false-positive "ghost struct" whose
fields looked superficially readable but were impossible for a real battle unit.

### 3.2 Status, Action, Forecast, And Damage Fields

Status: mixed, but the listed meanings are the best current model.

| Offset | Meaning | Confidence | Notes |
| --- | --- | --- | --- |
| `+0x61` | status / pending flag byte | **proven live** for KO bit; **strong** for pending bit |
| `+0x18D` | pending timer / charge phase | **strong** |
| `+0x1A0` | action-boundary byte | **working hypothesis** |
| `+0x1A1` | action-boundary byte | **working hypothesis** |
| `+0x1A2` | action id / last action id, u16 | **strong** |
| `+0x1B8` | active marker-ish (`b8`) | **working hypothesis** |
| `+0x1BA` | active source marker (`ba`) | **strong** when exactly `1` |
| `+0x1BB` | phase marker (`bb`) | **working hypothesis** |
| `+0x1C4` | forecast damage / staged debit / target cache | **proven live**, context-dependent |
| `+0x1D8` | forecast/charge/target metadata | **strong** |
| `+0x1E5` | forecast/target metadata | **strong** |
| `+0x1EF` | status/pending/KO mirror | **strong** |
| `+0x1F5` | death/lifecycle byte | **working hypothesis** |

Known bit meanings:

```text
unit+0x61 bit 0x20 = KO/dead state
unit+0x1EF bit 0x20 = KO/dead mirror/state
unit+0x61 value 0x08 = pending/charging action-ish
unit+0x1EF value 0x08 = pending/charging mirror/state
```

Important nuance:

`+0x1C4` is not one single concept. It is the same location used by different phases:

- during forecast, it can hold preview damage for the selected/primary target;
- around resolution, it can hold staged damage for the actual HP-write target;
- for immediate/basic actions, polling it from managed code can be too late to queue a pre-clamp
  rewrite;
- for AoE, secondary targets may not show forecast damage there, but do get staged damage at final
  HP application.

## 4. CT: Useful Fallback, Not The Final Resolver

Status: **proven live** as CT; **refuted** as robust primary context.

`unit+0x41` is CT. It rises with Speed and drops/resets when a unit acts in the normal FFT turn
model.

CT helped resolve actors in early immediate-action tests:

```text
attacker ~= unit whose CT recently dropped/reset
```

Why this is not enough:

- Wait does not reset CT the same way as an action.
- Delayed/charged actions resolve several turns later.
- Counters, Hamedo/First Strike-like reactions, and interrupts may not create a clean CT drop.
- Multiple charged actions can be pending at once.
- In delayed Cross Slash AoE, CT-only logic resolved no attacker for final HP events.

Current rule:

```text
Use CT as fallback and diagnostic only.
Do not make CT the primary source for the combat redesign.
```

Existing fallback behavior:

- CT reset/drop can still be useful when no better action context exists.
- A counter-inversion fallback exists conceptually/runtime-side: if a unit immediately damages the
  unit that just attacked it, the resolver can invert the previous resolved HP-damage pair and mark
  the source as `counter-inversion`.
- This is a fallback heuristic, not a final reaction system. Hamedo/First Strike-like flows still
  need live evidence.

## 5. Immediate / Basic Action Model

Status: **strong**, with live formula rewrite success.

### 5.1 What Happens

Basic Attack often has no explicit action id:

```text
unit+0x1A2 = 0
```

The source instead becomes visible through active-source state:

```text
source unit has unit+0x1BA == 1
```

For basic attacks and dual wield, the native pre-clamp target pointer gives the final target.

Best current model:

```text
source = current active source-like unit (exact ba == 1)
target = native pre-clamp target pointer
action = implicit/basic when action id is 0
```

### 5.2 Why Target-Cache Polling Failed

We tried:

```text
wait until target+0x1C4 has damage
-> resolve source
-> queue plan
```

For immediate/basic damage this was too late. The native pre-clamp hook had already applied the hit
by the time managed polling saw the target cache.

The working solution is eager:

```text
source active state observed
-> discover nearby plausible unit structs by stride
-> prequeue formula plans for possible targets
-> native pre-clamp hook selects the real target by pointer
-> require positive staged debit
```

This looks broad, but the final write is gated by:

- exact native target pointer;
- positive staged damage;
- formula plan matching;
- sane unit validation;
- exact active-source marker `ba == 1`.

### 5.3 Dual Wield

Status: **proven live**.

Dual wield is two separate native pre-clamp events, not one aggregate event.

Observed vanilla:

```text
Ninja -> Agrias
hit 1: oldDebit=180, HP 322 -> 142
hit 2: oldDebit=180, HP 142 -> 0, KO
```

Observed custom rewrite:

```text
Formula: max(1, attacker.pa * 10 - target.faith)
Ninja PA = 15
Agrias Faith = 63
Result per hit = 87

hit 1: oldDebit=180 -> forcedDebit=87, HP 322 -> 235
hit 2: oldDebit=180 -> forcedDebit=87, HP 235 -> 148
```

Implementation implication:

```text
Immediate/basic plans need maxWrites >= 2 for dual wield.
```

Open immediate-action risks:

- counters;
- Hamedo/First Strike-like reaction attacks;
- misses/evades/blocks;
- criticals and random damage;
- multi-target instant abilities;
- identifying weapon/action family, not just source and target.

### 5.4 Mana Shield And Non-HP Channels

Status: **proven live as an engine behavior; not solved as formula context**.

In an early CT actor test, Ramza attacked a Mana Shield unit and produced no HP event because the
engine redirected the damage to MP.

Implications:

- HP-only damage logs can miss real attacks when the engine routes them to MP.
- MP loss/gain is a separate event channel and needs separate rewrite/attribution handling.
- Do not conclude "no action happened" just because no HP event fired.

## 6. Charged / Delayed Action Model

Status: **strong** for Cloud Limit tests; still needs broader spell/skill coverage.

Delayed actions have at least three representations:

1. **Forecast / preview state** before confirmation.
2. **Confirmed pending state** stored on the caster.
3. **Resolution state** when final HP events happen.

These are not the same object/state.

### 6.1 Caster Pending Fields

Cloud Limit examples:

```text
Braver action id = 257 / 0x0101
Cross Slash action id = 258 / 0x0102
```

While pending:

```text
caster+0x61  = 8
caster+0x18D = timer/phase (Braver 2, Cross Slash 3 then 1)
caster+0x1A2 = action id
caster+0x1EF = 8
```

After resolution:

```text
caster+0x61  = 0
caster+0x18D = 255
caster+0x1A2 = same action id, now historical last action
caster+0x1EF = 0
```

Critical rule:

```text
action id alone is not pending state.
It remains after resolution.
```

Pending state needs the flags/timer.

### 6.2 Forecast Target Fields

Forecast damage appears on the selected/primary target:

```text
Braver -> Agrias:  Agrias+0x1C4 = 76
Braver -> Beowulf: Beowulf+0x1C4 = 153
Cross Slash -> Ramza: Ramza+0x1C4 = 187
Cross Slash centered on Agrias: Agrias+0x1C4 = 115
```

Related fields:

```text
target+0x1D8
target+0x1E5
```

Important limitations:

- Forecast damage can clear before final resolution.
- If the pending target itself gets a turn, its local forecast fields may clear.
- For AoE, forecast may only mark the selected unit/tile/epicenter, not every final victim.
- Therefore forecast target fields are not final target identity.

### 6.2.1 Preview Damage, UI Damage, And Probability

Status: **partly proven, partly unknown**.

The game computes enough during preview to show:

- selected action name;
- selected target or tile/epicenter;
- displayed damage;
- displayed hit chance / modifier.

But preview is not the same thing as final resolution:

- preview target data can be a selected unit or tile/epicenter;
- final AoE victims are resolved later from range/area/positions;
- preview damage can differ from final staged damage if the target set or state changes;
- final UI damage follows the staged debit consumed by the native HP-apply path, including our
  pre-clamp rewrites.

Probability / RNG caveat:

```text
We know the UI calculates and displays a hit chance during preview.
We do not yet know whether the final hit/miss roll is consumed at confirmation or at resolution.
```

Do not build formula identity or RNG assumptions on preview probability until a dedicated hook/log
test proves when the final roll happens.

### 6.3 AoE Resolution

Status: **proven live** for Cross Slash AoE.

Cross Slash centered on Agrias:

```text
Forecast showed Agrias damage 115.
Ninja had no forecast damage field.
At resolution:
  Agrias took 115.
  Ninja took 273.
```

This is expected. A charged AoE action can select a character or tile as an epicenter. Final
affected units are resolved later from area/range and current positions.

Formula rule:

```text
For damage rewriting, every final HP event is its own authoritative target.
```

We do not need the full final AoE target list ahead of time just to replace damage. We only need a
correct resolving action/caster context attached to the HP-write batch.

### 6.4 Pending State Is Cleared Before HP Write

Status: **proven live** for Cross Slash AoE probe.

Immediately before the final Wait on Ninja, Cloud still had:

```text
s61=8, t18D=1, act=258, f1EF=8
```

At the HP-write events for Agrias and Ninja, Cloud already showed:

```text
s61=0, t18D=255, act=258, f1EF=0
```

Meaning:

- scanning for "currently pending units" at HP-write time is too late;
- the code mod must record pending/resolving actions before the HP writes;
- or we must find a better hook closer to action dequeue / resolution.

Best current delayed-action implementation model:

```text
track pending caster/action while it exists
-> when caster transitions pending -> cleared with same act, mark action as resolving briefly
-> attach near-term HP events to that resolving action batch
-> target = each HP-write target
```

Hard open problem:

```text
If 5 actions are pending at once, which one is resolving for this HP event?
```

The robust answer likely needs either:

- a real current executing action pointer from the engine; or
- a richer pending table with timer, action id, selected unit/tile/epicenter, and batch timing.

## 7. Damage, UI, Clamp, And KO

Status: **proven live**.

The native staged-debit path behaves like this:

```text
target+0x1C4 receives raw staged damage
native pre-clamp hook reads oldDebit
code mod may rewrite oldDebit
engine applies HP loss
engine clamps HP at 0
engine displays/clamps UI number as needed
engine sets KO/status bytes if lethal
```

Observed lethal vanilla:

```text
Ramza -> Beowulf
raw staged damage 912
Beowulf HP 314 -> 0
KO bits set by engine
```

Observed lethal custom:

```text
Braver forced debit 9999
UI showed 999
Beowulf died
HP 314 -> 0
KO bits set by engine
```

Observed KO bytes:

```text
unit+0x61: 00 -> 20
unit+0x1EF: 00 -> 20
other lifecycle bytes such as +0x1BB, +0x1DB, +0x1F5 may also change in KO cases
```

Refuted path:

```text
Do not directly write HP=0 and set KO bytes as the death mechanism.
```

That produced zombie-like behavior in earlier tests. The engine must own death.

Historical path:

- The old late HP-write architecture used `MinHpFloor=1` for custom lethal damage, causing a
  two-hit kill: our rewrite left the target at 1 HP, then a later vanilla chip delivered real KO.
- The pre-clamp staged-debit path supersedes that for contexts we can resolve in time: it supports
  same-hit formula-owned KO through vanilla HP apply.
- Late HP-write remains useful as fallback/debugging infrastructure, but it is not the preferred
  damage architecture anymore.

## 8. What We Know About Registers

### 8.1 Stable Unit Hook Registers

Status: **mixed**.

Useful facts:

- `rcx` equals the touched unit pointer at the stable unit hook.
- Around simple basic action boundaries, `rcx/rdi` often identify the source.
- `r8` can point to a plausible target in simple single-target attacks.

Refined facts:

- `r8` is not reliable target truth. Ninja dual wield refuted it.
- Stable hook registers did not reveal Cloud as caster at delayed AoE HP-write time.
- Some readable roots/stack slots may still point to useful controller objects, but no clean
  current-action object has been proven yet.

### 8.2 Native Pre-Clamp Registers

Status: **strong/proven for target and staged state**.

Useful facts:

- `rdi` is the target unit pointer in our successful damage captures.
- `rbp = target + 0x1BE`.
- `[rbp+6]` maps to `target+0x1C4`, the staged debit.
- Pre-clamp registers mostly expose the target, not necessarily the source/caster.

Current probe direction:

```text
pending tracker sees "resolving action opened"
-> log latest stable-hook registers as kind=pendingresolve
native pre-clamp sees each staged HP debit
-> scan pre-clamp registers/stack roots for known unit pointers
-> compare both views for a shared current-action/controller object
```

The dedicated profile for this is:

```text
work/battle-runtime-settings.executing-action-pointer-probe.json
```

It is observe-only. The pre-clamp hook runs in `LogOnly` mode and emits `[PRECLAMP-PTRSCAN]`
without changing staged damage.

### 8.3 Session Pointers And Identity

Status: **operational rule**.

Unit pointers are session-specific. They are useful inside one live capture, but they should not be
copied into long-lived logic.

Common IDs observed in our controlled setup:

```text
Ramza   char id 0x01
Ninja   char id 0x80
Agrias  char id 0x1E
Cloud   char id 0x32
Beowulf char id 0x1F
```

Example pointers from one session:

```text
Ramza   0x141855CE0
Ninja   0x141855EE0
Agrias  0x1418560E0
Cloud   0x1418562E0
Beowulf 0x1418564E0
```

After a fresh game launch, rediscover pointers from `[UNIT]` lines or fresh snapshots.

### 8.4 Formula Context Surface Connected To These Findings

Status: **implemented surface; some inputs still need live confirmation**.

The runtime can expose formula variables from:

- target unit stats;
- attacker unit stats when action context resolves;
- action context diagnostics;
- action signal rules;
- configured equipment slot scans;
- item catalog metadata;
- constants, lookup tables, maps, matrices, and helper functions.

Important caveats:

- attacker variables are only as good as the resolved action source;
- action variables from sentinel bands are implemented, but true action identity from engine memory
  is still an open problem;
- equipment/slot metadata exists in the formula layer, but exact live equipment offsets are not yet
  confirmed enough for final armor DR;
- DR can be modeled in formula code once equipment identity is reliable.

## 9. Current Runtime Design Rules

These are the rules the code mod should follow unless a newer test updates this book.

1. **Use native pre-clamp for damage amount rewrites.**
   It is the proven place where custom damage can preserve vanilla HP/KO.

2. **Use HP-write/pre-clamp target as final target truth.**
   Forecast targets, UI focus, and `r8` are not authoritative.

3. **Use exact `ba == 1` for immediate active source.**
   Nonzero is unsafe because ghost structs can contain garbage values.

4. **Validate unit structs aggressively.**
   Stride scans can find false positives.

5. **Do not use CT as primary.**
   Keep it as fallback and diagnostic.

6. **Track delayed actions before HP writes.**
   Pending flags clear before final damage events.

7. **Treat forecast as optional metadata.**
   Forecast values are useful for UI parity and early hints, not final damage target identity.

8. **Keep action identity separate from action state.**
   `+0x1A2` can be last action after resolution; flags/timer determine live pending state.

9. **Expect multiple HP events per action.**
   Dual wield and AoE both produce separate native pre-clamp events.

10. **Filter debit vs credit.**
    Pre-clamp can see credit/heal-like events. Damage rewrites should require positive staged debit
    unless explicitly handling healing.

11. **Never trust session pointers across launches.**
    Use pointers for one capture; use offsets and char/team/stat sanity for reusable logic.

12. **Separate preview truth from resolution truth.**
    Preview tells us what the UI is considering. Resolution HP/pre-clamp events tell us what the
    engine is actually applying.

## 10. Evidence Index

Canonical docs:

- `docs/modding/05-battle-data-map.md`
- `docs/modding/07-live-findings.md`
- `docs/modding/09-hook-register-context-probe.md`
- `docs/modding/11-charged-action-context-investigation.md`
- `work/action-context-checkpoint-2026-06-22.md`
- `work/instant-basic-memory-probe-2026-06-23.md`

Important live captures:

- `work/live-captures/battleprobe_log.instant-basic-memory-probe-agrias-beowulf.snapshot.txt`
- `work/live-captures/battleprobe_log.instant-basic-memory-probe-ramza-beowulf-lethal.snapshot.txt`
- `work/live-captures/battleprobe_log.instant-basic-memory-probe-ninja-dual-agrias-lethal.snapshot.txt`
- `work/live-captures/battleprobe_log.preclamp-plan-immediate-ninja-dual-agrias-failed-180x2.snapshot.txt`
- `work/live-captures/battleprobe_log.preclamp-eager-immediate-ninja-agrias-999.snapshot.txt`
- `work/live-captures/battleprobe_log.preclamp-eager-immediate-ninja-agrias-success-87x2.snapshot.txt`
- `work/live-captures/battleprobe_log.action-state-probe-cross-slash-aoe-post.snapshot.txt`

Useful log line families:

- `[UNIT]`: registered unit pointer and identity.
- `[ACTION-STATE]`: changes in action/forecast/pending signature fields.
- `[ACTION-BOUNDARY]`: focused diff around action boundary bytes.
- `[HOOK-REGS]`: broad stable-hook register capture.
- `[HOOK-REGS-EVENT]`: recent hook snapshot correlated with HP/MP/CT/pending-resolve events.
- `[HOOK-PTRSCAN-EVENT]`: readable roots from a correlated stable-hook register snapshot.
- `[PENDING-ACTION-CANDIDATES]`: registered unit pending/action state at event time.
- `[PENDING-ACTION-TRACK]`: runtime pending/resolving action lifecycle.
- `[PRECLAMP-PLAN-QUEUE]`: formula plan staged for native pre-clamp.
- `[PRECLAMP-REWRITE]`: actual native staged debit/credit rewrite.
- `[PRECLAMP-PTRSCAN]`: native pre-clamp register/stack root scan for unit-pointer context.
- `[DAMAGE]`: observed HP loss.
- `[HP-EVENT-PROBE]`: raw damage/clamp/lethal diagnostics.

## 11. Open Problems

Highest priority:

1. **Current executing action pointer** (strong lead found — see 2.4)
   The battle participant/actor array is the leading candidate: at the native pre-clamp frame the
   caster's actor struct is on the stack alongside the target's, and `caster = stack actor whose
   +0x148 != current target`. Remaining: confirm the resolving action id lives inside the actor
   struct (actor-struct dump probe) and verify the actor RVA/layout is stable across launches.

2. **Multiple simultaneous pending actions**
   A resolving-window heuristic may work for one pending action, but overlapping charges need a
   stronger discriminator.

3. **Action identity for immediate/basic attacks**
   Basic Attack has `act=0`; we still need weapon/action family, element, formula id, and reaction
   context for a full redesign.

4. **Counters / Hamedo / reactions**
   These may invert source/target timing and may not use normal CT/action markers.

5. **Miss, evade, block, critical, random damage**
   We need to know whether failed hits produce no staged debit, zero debit, or separate state.

6. **Equipment and DR**
   The runtime still needs robust equipment/armor identity if armor DR is formula-owned.

7. **Forecast hook**
   Only needed if we want UI preview to show custom damage before confirmation.

8. **MP / Mana Shield channel**
   HP events do not cover all damaging actions. MP redirection and MP damage need their own
   pre-clamp or rewrite path.

9. **Preview probability / final RNG timing**
   We need to know when the final roll is consumed before we can safely model hit/miss preview or
   guarantee replay-safe random behavior.

## 12. How To Add New Knowledge

When a new live test teaches us something, update this file with:

1. The exact behavior in plain language.
2. The offset/register/hook involved.
3. Confidence level.
4. At least one evidence artifact path.
5. Whether it changes a runtime design rule.

Raw logs can stay in `work/live-captures`, and detailed one-off narratives can stay in `work/`.
This book should contain the distilled model we are willing to build on.
