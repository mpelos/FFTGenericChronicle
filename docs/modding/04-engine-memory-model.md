# Engine Memory Model — Live Runtime

Single source of truth for the live battle-runtime engine model of FFT: The Ivalice Chronicles:
the per-unit struct, the battle actor array, action/forecast/damage state, the hooks the code mod
uses, and the damage → clamp → KO path.

Cross-references (do not duplicate those documents here):

- Code-mod reconciler pipeline, formula DSL, and runtime settings → `06-code-mod-runtime-dsl.md`.
- Reverse-engineering anchors (.text/.xcode AOBs, evade enum, Denuvo notes) → `05-reverse-engineering.md`.
- Editable data surfaces (NXD/XML/ENTD tables, item catalog, enums) → `03-battle-data-map.md`.

Confidence markers are terse inline labels: **Proven**, **Strong**, **Hypothesis**, **Refuted**.

## 1. Big-Picture Runtime Model

The proven combat-application architecture is:

```text
observe action/context memory
-> build custom formula context
-> rewrite native staged debit pre-clamp
-> let vanilla engine apply HP, clamp, UI number, and KO/death
```

Split of responsibility:

- Code mod owns the combat number.
- Engine owns HP clamp, death/KO lifecycle, and final effect application.
- HP-write/pre-clamp target is the authoritative final impacted unit.
- CT is fallback/diagnostic, not the primary action-context source.

Core vocabulary:

- **Unit struct**: the per-battle unit object. The pointer changes per game launch, but the offsets
  inside the struct are stable.
- **Battle actor array**: a separate per-participant array (stride `0x548`) that links to each unit
  struct and carries the resolving action id.
- **Stable unit hook**: the known non-virtualized hook around `battle_base_ptr`, where `rcx` is a
  unit pointer and the game reads `unit+0x30` HP.
- **Native pre-clamp hook**: the later native hook that sees staged debit/credit immediately before
  vanilla HP application and KO handling.
- **Staged debit**: the raw damage value stored near `unit+0x1C4` before the engine subtracts HP.
- **Pending action**: a delayed/charged action stored on the caster while waiting to resolve.
- **Active source**: the unit currently executing an immediate/basic action, usually marked by
  `unit+0x1BA == 1`.

## 2. Battle-Unit Struct Map

Offsets are relative to the unit pointer. The struct spans at least `0x200` bytes.

### 2.1 Core Combat Stats — **Proven**

| Offset | Meaning | Width | Notes |
| --- | --- | ---: | --- |
| `+0x00` | Character id | byte | Examples: Ramza `0x01`, Cloud `0x32`, Ninja `0x80`, Agrias `0x1E`, Beowulf `0x1F` |
| `+0x03` | Job id | byte | Stable per unit; e.g. Black Mage 80, Summoner 82, Ninja 89, Samurai 88, Ramza special-Squire 160 |
| `+0x04` | Team/group id | byte | Used in sanity validation |
| `+0x05` | Friend/foe flags | byte | Bit `0x10` observed as foe-ish |
| `+0x06` | Gender flags | byte | bit7 `0x80` Male, bit6 `0x40` Female, bit5 `0x20` Monster (classic FFT) |
| `+0x09` | Zodiac | hi-nibble | Classic order Aries 0 .. Pisces 11 |
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
| `+0x38` | Raw PA | byte | Base, pre-equipment |
| `+0x39` | Raw MA | byte | Base, pre-equipment |
| `+0x3A` | Raw Speed | byte | Base, pre-equipment |
| `+0x3E` | PA | byte | Physical Attack (effective) |
| `+0x3F` | MA | byte | Magical Attack (effective) |
| `+0x40` | Speed | byte | Speed (effective) |
| `+0x41` | CT | byte | Charge Time |
| `+0x42` | Move | byte | Movement |
| `+0x43` | Jump | byte | Jump |
| `+0x44` | Weapon attack R | byte | Effective, equipment-derived |
| `+0x45` | Weapon attack L | byte | Effective, equipment-derived |
| `+0x46` | Weapon parry R % | byte | Equipment-derived |
| `+0x47` | Weapon parry L % | byte | Equipment-derived |
| `+0x49` | Accessory/cloak physical evade % | byte | **Strong (inferred)** — drives evade-type `0x01` (cloak) |
| `+0x4A` | Shield physical parry % | byte | Equipment-derived |
| `+0x4B` | Physical evasion % | byte | = the job's `CharacterEvasion` |
| `+0x4E` | Shield magick parry % | byte | Equipment-derived |

These evade bytes are **live inputs to the avoidance roll** — ✅ **PROVEN 2026-06-27**: the Denuvo
VM reads them from the unit's live struct at roll time, so **writing them before the roll controls
hit / miss / block / parry**. Denuvo virtualizes *code*, not *data* — the struct is normal writable
memory. Set on the **defender**: `+0x4B` high ⇒ class evade ("Miss", type `0x04`); `+0x46/+0x47` high ⇒
weapon parry (`0x02`); `+0x4A/+0x4E` high ⇒ shield block (`0x03`); `+0x49` high ⇒ cloak (`0x01`,
inferred); the five **physical** bytes `+0x46/+0x47/+0x4A/+0x4B/+0x4E` all `= 0` ⇒ guaranteed hit
(neutralizes avoidance in memory, no data edit). Values 0–100. The mod's `EvadeOverride*` knob exposes
nine bytes (`+0x46/47/48/49/4A/4B/4C/4D/4E`; `+0x48/4C/4D` are inferred magic-evade partners). This is
the *input-control* path — the cleaner primary, vs. the *output-control* hooks (pre-clamp `0x30A66F`
debit §4.2 + selector `0x205210`). Full proof: `work/input-control-evade-PROVEN.md`; mechanism + dead
ends in `05-reverse-engineering.md` §4.

Raw → effective relationship (**Proven**): for PA/MA/Speed, `raw (+0x38/39/3A) + sum(equipment
stat bonuses) == effective (+0x3E/3F/40)`.

Job stat-scaling block (**Proven**): the unit's `JobData` scaling row is copied into the struct.

```text
+0x8A HPGrowth   +0x8B HPMult    +0x8C MPGrowth   +0x8D MPMult
+0x8E SpeedGrowth +0x8F SpeedMult +0x90 PAGrowth   +0x91 PAMult
+0x92 MAGrowth   +0x93 MAMult
```

Other mapped fields:

- `+0x14C` unit display-name (ASCII string) — non-combat field.

Sanity validation rejects impossible candidates (a stride scan once accepted a false-positive
"ghost struct" with superficially readable but impossible fields):

- team greater than `16`;
- CT greater than `100`;
- PA/MA/Speed greater than `127`;
- Move/Jump greater than `32`;
- Brave/Faith greater than `100`.

Still unmapped from the struct: R/S/M / secondary ability ids (somewhere in `0x52..0x8F`), elemental
affinity (likely derived), and geometry (position/facing/height). `0x70..0x8F` look like object
pointers. The status bitfield is **partly mapped and DATA-controllable** (see §2.3): `0x20`=KO and
`0x10`=Undead are both live-confirmed (the earlier offline `0x10`="control-flip" guess was WRONG);
remaining bits to map empirically. Stats drift with level, so only map level-matched dumps.

Width limits: stats are bytes; HP/MP are 16-bit words; damage is a 16-bit word; engine math is
integer (the remaster applies some multipliers as AVX floats, then truncates to int).

### 2.2 Equipment Block — **Proven**

Equipped item ids are 16-bit little-endian words in a contiguous block just before the stat block.
Triple-confirmed offline against equip-screen ground truth across 8 units, then live read-back
validated at the damage frame (both attacker and target read correctly from live memory by the
`[PRECLAMP-EQUIP]` probe).

| Offset | Slot | Width |
| --- | --- | ---: |
| `+0x1A` | Head | word |
| `+0x1C` | Body | word |
| `+0x1E` | Accessory | word |
| `+0x20` | Right hand — weapon | word |
| `+0x22` | Right hand — shield | word |
| `+0x24` | Left hand — weapon | word |
| `+0x26` | Left hand — shield | word |

The word is the `item_id` (join `work/item_catalog.csv` for name, family, WP, element, evasion,
HP/MP bonus, equip bonus). Sentinels: empty hand on an equip-capable unit = `0x00FF` (255);
monster / no-equipment unit = `0x0000` in all slots.

Reading rules for formulas:

```text
primary weapon = word @ +0x20 (fall back to +0x24 if +0x20 is empty/255)
dual wield     = non-empty weapon at both +0x20 and +0x24   (Ninja Iga+Koga; Excalibur+Defender)
shield         = whichever of +0x22 / +0x26 is non-empty     (left-hand +0x26 in all samples)
two-handed     = +0x20 set, other three hand-words empty      (Cloud Materia Blade Plus)
```

Equipment is **not** in the `0x548` actor struct; the unit struct is the source of truth (no
roster/ENTD join needed). This also gives basic-attack weapon identity: a basic attack carries
action id `0`, but the weapon is `attacker_unit+0x20`.

Equipment flows into the formula context and branches it (**Proven**, offline `settingssimulate`
running the identical live pipeline `ReadUInt16 -> EquipmentSlotProbe -> AddSlotVariables ->
formula engine`): both sides read simultaneously; the 16-bit word width is load-bearing
(item ids can exceed 255, e.g. `id=256` Materia Blade Plus); formulas branch on weapon family from
the catalog join; target armor → DR, and the no-attacker case still applies target-side DR
(graceful degrade).

### 2.3 Status, Action, Forecast, and Damage Fields

| Offset | Meaning | Confidence | Notes |
| --- | --- | --- | --- |
| `+0x57` | innate / equipment status source | **Strong** | OR-source for the effective byte; clear here to cure equip-sourced status |
| `+0x61` | effective status byte (mirror) | **Proven** KO bit; **Strong** | recomputed in real code at `0x30D42A` as `(+0x1EF & 0xF2) \| +0x57` |
| `+0x18D` | pending timer / charge phase | **Strong** | |
| `+0x1A0` | action-boundary byte | **Hypothesis** | |
| `+0x1A1` | action-boundary byte | **Hypothesis** | |
| `+0x1A2` | action id / last action id, u16 | **Strong** | `0` for basic attacks |
| `+0x1B8` | active marker-ish (`b8`) | **Hypothesis** | |
| `+0x1BA` | active source marker (`ba`) | **Strong** when exactly `1` | |
| `+0x1BB` | hit/phase marker (`bb`) | **Proven** | `0x02` on damage-apply (hit), `0x01` on an evade |
| `+0x1BE` | staged-result-present | **Proven** | `0x01` = damage result staged, `0x00` = evade / no-damage |
| `+0x1C0` | EVADE-TYPE (animation lever) | **Proven** | see enum below |
| `+0x1C4` | forecast HP-debit / staged HP-debit / target cache | **Proven**, context-dependent | damage preview number + ghost depletion; also "staged DAMAGE (word)" at apply |
| `+0x1C6` | forecast HP-credit / staged HP-credit / target cache | **Proven**, context-dependent | healing preview number + ghost refill; apply: `newHP = clamp(HP + heal - dmg)` |
| `+0x1C8` | staged MP-debit (word) | **Strong** (static-proven) | MP analogue of `+0x1C4` |
| `+0x1CA` | staged MP-credit (word) | **Strong** | apply: `newMP = clamp(MP + 0x1CA - 0x1C8)` |
| `+0x1D8` | forecast / charge / target metadata | **Strong** | charge/forecast value (word) |
| `+0x1E5` | forecast / target metadata; resultKind bits | **Strong** | see resultKind bits below |
| `+0x1EF` | status master / KO mirror (durable) | **Strong** | the durable status master; `+0x61` is recomputed from it |
| `+0x1F5` | death / lifecycle byte | **Hypothesis** | |

Known bit / value meanings:

```text
unit+0x1EF bit 0x20  = KO/dead state (durable master)
unit+0x61  bit 0x20  = KO/dead state (effective mirror)
unit+0x1EF bit 0x10  = Undead (live-confirmed 2026-06-27; offline "control-flip" guess was WRONG)
unit+0x1EF value 0x08 = pending/charging action-ish (per-turn-cleared on +0x61 via the 0xF2 mask)
unit+0x61  value 0x08 = pending/charging mirror/state
```

**Status is DATA-controllable** (✅ live-confirmed: `+0x1EF/+0x61 |= 0x10` made Ramza Undead). The
effective byte `+0x61` is recomputed in real code at `0x30D42A` as `(+0x1EF & 0xF2) | +0x57`, so:
- **Force** a status → OR the bit onto `+0x1EF` (durable master) AND `+0x61` (mirror, so it shows this
  frame); the `0x08`-class bits are masked out each turn (`& 0xF2`), so re-write them every poll.
- **Cure** → clear the bit on `+0x1EF`, `+0x61`, and `+0x57` (the innate/equipment source).
Remaining bit→ailment meanings are mapped empirically (offline guesses proved partly wrong). Mod knob:
`StatusOverride1EF/61/57` (see `06-code-mod-runtime-dsl.md`).

Per-action RESULT / OUTCOME fields (**Proven**, the hit/miss/block/parry control surface; same
`0x200` unit struct, written by the engine at action resolution and read by the result/animation
selector at `module+0x205210`; the roll is Denuvo-virtualized but its OUTCOME lands in these bytes,
so writing them drives the result and the native animation):

```text
+0x1BB  hit/phase marker (byte)      0x02 on a damage-apply (hit), 0x01 on an evade
+0x1BE  staged-result-present (byte) 0x01 = damage result staged, 0x00 = evade / no-damage
+0x1C0  EVADE-TYPE (byte)  ** the animation lever ** (also passed in cl to the selector):
        0x00 hit | 0x01 cloak/accessory evade | 0x02 weapon parry | 0x03 shield parry/block
        0x04 class evade ("Miss") | 0x06 plain miss (failed accuracy roll, e.g. Steal)
        0x0B Blade Grasp (reaction; live-observed)
        (0x05 / 0x07–0x0A are unobserved gaps, likely unused)
+0x1C4  forecast/staged HP-DMG (word)   +0x1C6  forecast/staged HP-HEAL (word)
        apply: newHP = clamp(HP + heal - dmg)
+0x1C8  staged MP-DEBIT (word) +0x1CA  staged MP-CREDIT (word)  apply: newMP = clamp(MP + 0x1CA - 0x1C8)
+0x1D8  charge/forecast value (word)
+0x1E5  resultKind bits: 0x80 damage | 0x40 heal | 0x10 heal/MP | 0x08 status | 0x01 stat-change | 0x20 special
```

A damage hit shows `+1BB=02 +1BE=01 +1C4=dmg +1E5=0x80`. A healing result shows `+1C4=0`,
`+1C6=heal`, and `+1E5=0x40`; the forecast HP bar refills from `+0x1C6` and clamps at MaxHP. An
EVADE shows `+1BB=01 +1BE=00 +1C4=0`, and the evade variants differ ONLY in `+0x1C0` (e.g. `0x01`
cloak vs `0x03` shield). `+0x1E5` is orthogonal to hit-vs-evade: it carries the action's effect-kind,
so it is `0x00` for a basic-attack evade but stays nonzero when an evaded ability still carries an
effect (an evaded equipment-break keeps `+1E5=0x01`). The detailed RE recipe and anchors for driving
this surface live in `05-reverse-engineering.md`.

This is the **output-control** surface (write the result after the roll). There is now a cleaner,
✅ **proven primary**: **input-control** — write the defender's evade bytes (`+0x46/+0x47/+0x4A/+0x4B/
+0x4E`, see §2.1) *before* the roll and the VM produces these `+0x1BE/+0x1C0/+0x1C4` outcome bytes
naturally, with the engine rendering the forecast %, animation, and damage. Prefer input-control for
hit/miss/block/parry; reserve this output surface for cases input-control can't reach.

Important nuance on `+0x1C4`: it is not one single concept. The same location is reused by
different phases:

- during forecast, it can hold preview damage for the selected/primary target;
- around resolution, it holds staged damage for the actual HP-write target;
- for immediate/basic actions, polling it from managed code can be too late to queue a pre-clamp
  rewrite;
- for AoE, secondary targets may not show forecast damage there, but do get staged damage at final
  HP application.

Important nuance on `+0x1C6`: it is the HP-credit twin of `+0x1C4`. During forecast, it holds preview
healing for the selected/primary target and drives both the green healing number and the HP-bar ghost
refill. Around resolution, it holds staged healing for the actual HP-write target. The forecast object
alias is `target_unit + 0x1BE`, so `obj+0x8 == unit+0x1C6`.

`+0x1C6` is not exclusive to explicit healing spells/items. Passive or side-effect healing, including
Regen-style HP credit, also uses the same staged-credit surface. Formula control therefore must gate
healing rewrites by action context/effect identity; a generic "any HP-credit event" rule is too broad.

### 2.4 Preview Hit-% UI Buffer — **Proven** (DCL Layer 1, visual)

The displayed attack-forecast hit-% does **not** live in the unit struct — it sits in a UI buffer at
the static address `0x1407832C0` (RVA `0x7832C0`), plus three transient heap mirrors. Located via a
differential memory scan (`work/mem_scan.py`). The value the renderer draws is `0x7832C0`.

Data flow (real code, not VM): a global pointer at `0x142FF3CF8` holds a **forecast object**; that
object's field `+0x2C` is the computed hit-%; real code copies it to the display buffer at `0x228004`
(`mov word [0x7832C0], ax`, source loaded at `0x227FFA movzx eax,[rbp+0x2C]`). ⚠️ **The forecast
object's `+0x2C` (hit-%) is unrelated to the unit struct's `+0x2C` = Faith (§2.1) — different objects.**

Control: hook `0x227FFE` (a non-RIP instruction between the load and the store) and set `AX` before the
store → the engine writes our value at copy time, on the same redraw the renderer reads. ✅ proven live
2026-06-27 (forced 7 shown for every target while the engine's true value was 3). **Purely visual** —
the real hit roll is computed independently in the VM and is unaffected. Full RE: `05` §10; mod knob
`PreviewHitPct*` in `06`.

## 3. Battle Actor Array — **Proven** (one battle/save; cross-battle stability unverified)

The engine keeps a per-participant "actor" array separate from the unit struct. Discovered at the
native pre-clamp frame via the executing-action-pointer probe.

Layout:

- contiguous array, stride `0x548`;
- `actor+0x148` = pointer to the unit struct (a root identifies as an actor when its `+0x148`
  dereferences to a registered unit);
- `actor+0x0` = pointer to `(this - 0x548)` = previous array element (back-link).

Action context at the native pre-clamp damage frame, memory-only, no CT and no pending-clear
heuristic:

```text
target   = pre-clamp unit pointer (per HP event)
caster   = stack actor struct whose +0x148 != current target   (works for charged AND immediate)
actionId = caster_actor + 0x142                                  (also 0x17A / 0x18C / 0x1BC)
```

- The resolving action id lives in the caster actor struct: `258` (Cross Slash), `257` (Braver),
  `0` (basic attack — weapon identity then comes from equipment, §2.2). It is `0` in every
  target/other actor.
- `+0x142` sits right before the self unit pointer at `+0x148`, making it the primary "this actor's
  current action id" candidate.
- No target list inside the caster actor — scanning the full `0x548` found only the self pointer at
  `+0x148`. Targets are taken from each pre-clamp HP event instead.
- The caster actor is constant across an AoE batch; the target actor varies. Stack slot index is
  not fixed, so the discriminator is by content (`+0x148 != current target`), not by slot.
- Native pre-clamp registers only carry the target (`rcx/rdi/r8`); the caster never appears in a
  register, only as an actor-struct pointer on the stack.

This is the strongest candidate for a real engine "current executing action context" and is
reachable straight from memory at damage time; it can retire both CT and the pending-clear
heuristic. An observe-only memory-only resolver (`[PRECLAMP-ACTOR-CTX]`) is validated head-to-head
for Cross Slash AoE (resolved both hits to `caster=Cloud actionId=258` purely from the actor array,
agreeing 100% with the pending tracker, with no CT; returns `no-caster-actor` for credit/tick
events with `oldDebit=0`, naturally separating real actions from passive ticks).

Cross-confirmation: the result/animation selector at `module+0x205210` reads its record as
`[r8+0x148]` where `r8` is the actor object — i.e. the same `actor+0x148 -> unit` array.

Still open before promoting actor-context to primary `DamageEvent.Attacker`/`Action` (gated on
`oldDebit>0`, keeping pending tracker + CT as fallback): overlapping/simultaneous pending casters,
counters/reactions, an explicit immediate-basic head-to-head, and actor-array RVA/layout stability
across a DIFFERENT battle/save (captures so far were the same battle/save; the actor bases were
identical across two launches of the same save, but cross-battle stability is unverified).

## 4. Hook Map

### 4.1 Stable Unit Hook — **Proven** for unit observation; not a full action hook

```text
Name: battle_base_ptr / stable unit touchpoint
Signature context: 0F B7 41 30 66 89 42 0C
Approx module offset: module+0x226D98
Important instruction behavior: reads unit+0x30 HP
Observed register: rcx = touched unit pointer
```

Good for: registering battle unit pointers; reading stable unit fields; observing CT/HP/MP/
pending/action state changes; capturing broad register/stack context around unit touches; finding
active-source markers for immediate/basic actions.

Not guaranteed to be: the true damage formula routine; the true action dispatcher; the exact CPU
frame that applies HP; a reliable current-caster pointer for delayed actions.

Register lessons:

- In simple single-target basic attacks, `rcx/rdi` at the source action boundary were the source,
  and `r8` sometimes pointed to the target.
- In Ninja dual wield, `r8` often pointed to Beowulf while Agrias was the real target. Therefore
  `r8` is diagnostic only — do not treat it as target truth.

### 4.2 Native Pre-Clamp Hook — **Proven**, the primary formula-write hook

```text
Approx RVA: 0x30A66F
Expected bytes: 0F BF 45 06
Observed purpose: COMBINED HP+MP apply — reads staged debit/credit before vanilla applies HP and MP
```

Register/state model:

```text
rdi = authoritative target unit pointer
rcx/r8 often also target in successful captures, but do not rely on them over rdi/event target
rbp = target + 0x1BE
[rbp+6]  = target + 0x1C4 = staged HP-debit / raw damage
[rbp+8]  = target + 0x1C6 = staged HP-credit / heal-like value
[rbp+10] = target + 0x1C8 = staged MP-debit ; [rbp+12] = +0x1CA = staged MP-credit
```

The same apply routine (`0x30A51C`) also writes MP: `newMP = clamp(MP + word[+0x1CA] - word[+0x1C8],
0, MaxMP)`, stored `mov [rdi+0x34], ax` at `0x30A6CC`. So this one hook can rewrite **MP** cost/damage/
restore as well as HP — the MP control mechanism is identical to HP (force the staged MP words).

What it gives: pre-apply target HP; raw staged damage (`oldDebit`) before clamp; raw staged credit
(`oldCredit`) for heal/credit-like cases; a place to rewrite damage before vanilla applies HP and
KO; a native-frame register/stack capture point for the actor array (§3).

**Proven live (2026-06-26):** forcing `word[rbp+6]=0` here zeros a guaranteed hit's damage — a
100%-to-hit attack left the target at full HP (567/567). Paired with the selector evade-type write
(`05-reverse-engineering.md` §4, Control recipe) this gives full hit→miss control: debit-zero on this
path + evade animation on the selector path are independent and both required. Proof log
`work/battleprobe_log.hit-to-miss-v2-PASS.*.txt`.

Critical implementation rule:

```text
target = native pre-clamp unit pointer / HP event pointer
```

This is stronger than any forecast target, `r8`, CT guess, or UI focus state.

Noise: the hook can also see credit/heal-like staged events where `oldDebit=0` and `oldCredit>0`.
Damage code must explicitly require a positive staged debit unless intentionally handling
healing/credit events.

### 4.3 Refuted / Noisy Memory Leads

- `0x03C36740..0x03C36920` is **UI/current-focus state, not the scheduled-action object**. It once
  looked promising (it contained a unit pointer and forecast-like values), but its slot follows the
  focused/active UI unit rather than the scheduled action target. **Refuted** as a pending-action
  source.
- Global numeric scans for values like `Cloud`, `Braver`, `153`, timer `2` produce many persistent
  static/UI/cache candidates that survive resolution; treat as noise.
- Raw pointer scans can find compact records containing unit pointers, but no proven current
  executing action object was isolated by scanning alone (the actor array in §3 came from the
  pre-clamp stack, not blind scanning). A real candidate must correlate actor, action id, timing,
  and target/epicenter across baseline, forecast, confirmed, pending, resolution, and post states.

## 5. Action-Context Models

### 5.1 CT: Useful Fallback, Not the Final Resolver — **Proven** as CT; **Refuted** as robust primary

`unit+0x41` is CT. It rises with Speed and drops/resets when a unit acts in the normal FFT turn
model. Early immediate-action resolution used `attacker ~= unit whose CT recently dropped/reset`,
which scored 5/6 by absolute-lowest CT and 6/6 with a largest-recent-drop tiebreak across 6
controlled attacks.

Why CT is not enough: Wait does not reset CT like an action; delayed/charged actions resolve
several turns later; counters / Hamedo / First-Strike-like reactions and interrupts may not create
a clean CT drop; multiple charged actions can be pending at once; in delayed Cross Slash AoE,
CT-only logic resolved no attacker for final HP events.

Rule: use CT as fallback and diagnostic only; do not make CT the primary source. A counter-inversion
fallback exists conceptually: if a unit immediately damages the unit that just attacked it, the
resolver can invert the previous resolved HP-damage pair and mark the source as `counter-inversion`
(`a.sourceCounter`). This is a heuristic, not a final reaction system.

### 5.2 Immediate / Basic Action Model — **Strong**, with live formula rewrite success

Basic attack often has no explicit action id (`unit+0x1A2 = 0`); the source becomes visible through
active-source state (`source unit has unit+0x1BA == 1`). Best model:

```text
source = current active source-like unit (exact ba == 1)
target = native pre-clamp target pointer
action = implicit/basic when action id is 0; weapon identity from attacker_unit+0x20
```

Why target-cache polling failed: waiting until `target+0x1C4` has damage then resolving the source
was too late for immediate/basic damage — the native pre-clamp hook had already applied the hit by
the time managed polling saw the target cache. The working solution is eager: observe source active
state → discover nearby plausible unit structs by stride → prequeue formula plans for possible
targets → let the native pre-clamp hook select the real target by pointer → require positive staged
debit. The broad prequeue is gated by: exact native target pointer; positive staged damage; formula
plan matching; sane unit validation; exact active-source marker `ba == 1`.

Dual wield (**Proven**): two separate native pre-clamp events, not one aggregate event. Each hit has
its own staged debit. Implementation implication: immediate/basic plans need `maxWrites >= 2`.

Open immediate-action risks: counters; Hamedo/First-Strike-like reactions; misses/evades/blocks;
criticals and random damage; multi-target instant abilities; identifying weapon/action family, not
just source and target.

Mana Shield and non-HP channels (**Proven** as engine behavior; not solved as formula context): an
attack on a Mana Shield unit produces no HP event because the engine redirects the damage to MP.
HP-only damage logs can miss real attacks routed to MP; MP loss/gain is a separate event channel
needing separate rewrite/attribution handling. Do not conclude "no action happened" just because no
HP event fired.

### 5.3 Charged / Delayed Action Model — **Strong** for Cloud Limit; broader coverage pending

Delayed actions have at least three distinct representations: forecast/preview (before
confirmation), confirmed pending (stored on the caster), and resolution (final HP events).

Caster pending fields (Cloud Limit examples; Braver action id `257`/`0x0101`, Cross Slash
`258`/`0x0102`):

```text
While pending:                 After resolution:
caster+0x61  = 8               caster+0x61  = 0
caster+0x18D = timer/phase     caster+0x18D = 255
caster+0x1A2 = action id       caster+0x1A2 = same action id (now historical last action)
caster+0x1EF = 8               caster+0x1EF = 0
```

`+0x18D` behaves like a pending countdown/phase: Braver `2`; Cross Slash started `3` and reached `1`
before execution. Critical rule: action id alone is not pending state — it remains after resolution.
Pending state requires the flags/timer (`+0x61`, `+0x18D`, `+0x1EF`).

Forecast target fields appear on the selected/primary target:

```text
Braver -> Agrias:   Agrias+0x1C4 = 76,  +0x1D8 = 2, +0x1E5 = 128
Braver -> Beowulf:  Beowulf+0x1C4 = 153
Cross Slash -> Ramza:   Ramza+0x1C4 = 187
Cross Slash centered on Agrias: Agrias+0x1C4 = 115
```

`target+0x1C4` is definitely target-local forecast damage (changing only the target moved the value
from `Agrias+0x1C4=76` to `Beowulf+0x1C4=153`). Limitations: it can clear before final resolution
(when the pending target itself takes a turn its local forecast fields can clear); for AoE it may
mark only the selected unit/tile/epicenter, not every final victim. Therefore forecast target fields
are not final target identity — capture and retain them when visible, but do not rely on them at
HP-write time. `+0x1D8` is charge/timing metadata; `+0x1E5` is action/target-state metadata.

Preview vs resolution (**partly proven**): the game computes enough during preview to show action
name, selected target or tile/epicenter, displayed damage, and displayed hit chance/modifier. But
preview is not final resolution: preview target data can be a unit or a tile/epicenter; final AoE
victims are resolved later from range/area/positions; preview damage can differ from final staged
damage; final UI damage follows the staged debit consumed by the native HP-apply path (including
pre-clamp rewrites). RNG caveat: the UI calculates and displays a hit chance during preview, but it
is unknown whether the final hit/miss roll is consumed at confirmation or at resolution — do not
build formula identity or RNG assumptions on preview probability until a dedicated test proves when
the final roll happens.

AoE resolution (**Proven** for Cross Slash AoE): a charged AoE selects a character or tile as an
epicenter; final affected units are resolved later from area/range and current positions. Example:
Cross Slash centered on Agrias showed forecast `115` on Agrias only (Ninja had no forecast field),
and at resolution Agrias took `115` while Ninja took `273`. Each final HP event is its own
authoritative target; the full final AoE target list is not needed ahead of time to replace damage
— only a correct resolving action/caster context attached to the HP-write batch.

Pending state is cleared before HP write (**Proven** for Cross Slash AoE): immediately before the
final Wait, Cloud still showed `s61=8 t18D=1 act=258 f1EF=8`; at the HP-write events Cloud already
showed `s61=0 t18D=255 act=258 f1EF=0`. The stable HP hook register context still pointed at the
target (Ninja), not Cloud. So scanning for "currently pending units" at HP-write time is too late.
The pending-tracker implementation model:

```text
track pending caster/action while it exists (s61=8, act!=0, f1EF=8, timer not idle)
-> when caster transitions pending -> cleared with same act, mark action as resolving briefly
-> attach near-term HP events to that resolving action batch
-> target = each HP-write target; dmg1C4 validates damage and cred1C6 validates healing
```

Hard open problem: with multiple simultaneous pending actions, which one is resolving for a given HP
event? The robust answer is the actor array (§3) or a richer pending table with timer, action id,
selected unit/tile/epicenter, and batch timing. Wait remains a negative control — it changes
current-turn/CT state and may clear a unit's own target-local forecast fields, but it does not erase
the caster's pending-action state, separating action ownership from current active unit.

For healing, the same pending tracker uses `+0x1C6` as a credit-cache match. A delayed explicit heal
can therefore resolve as `source=pending-clear` with `credit-cache`, giving formulas a real caster and
target without CT. Phase/result markers still matter: after a staged rewrite, `+0x1C6` may reflect the
authored credit during result phase, so formula-plan profiles should avoid queueing new plans from
post-apply target-cache echoes.

## 6. Damage → Clamp → KO Path — **Proven**

The native staged-debit path:

```text
target+0x1C4 receives raw staged damage
native pre-clamp hook reads oldDebit
code mod may rewrite oldDebit
engine applies HP loss
engine clamps HP at 0
engine displays/clamps UI number as needed
engine sets KO/status bytes if lethal
```

UI clamp: large staged damage is presentation-clamped — raw staged debit `9999` displays as `999`
while the real `9999` still drives the lethal HP apply.

KO bytes observed at death:

```text
unit+0x61:  00 -> 20
unit+0x1EF: 00 -> 20
other lifecycle bytes such as +0x1BB, +0x1DB, +0x1F5 may also change in KO cases
```

The death/status flag `+0x61` bit `0x20` is set on **every** death (5/5 vanilla deaths, humans and
monsters: alive→dead diff was exactly `+0x30->00` plus `+0x61:00->20`, no other consistent change,
no delayed follow-up).

### 6.1 Engine-Owned Death Constraint — **Refuted** that a memory write can cause death

A memory write of `HP=0` and/or setting `+0x61 |= 0x20` does **not** trigger real death:

- `HP=0` alone → zombie: the unit stands at 0 HP, its CT keeps ticking, it takes turns.
- `HP=0` + `+0x61 |= 0x20` → still zombie: Regen heals it back to life (Regen does not tick on dead
  units, so the engine still considers it ALIVE); setting the bit produces a buggy partial state
  (immune, attacks pass through) the engine never expects.

Conclusion: `+0x61` bit `0x20` is an **effect** of death, not a **trigger**. Real death is an
internal engine **routine** (almost certainly inside the Denuvo-virtualized damage path) that
updates state **outside** the unit struct (turn manager / active-unit list), keyed on the engine's
own damage reaching 0. The symptoms can be replicated but the routine cannot be invoked by a write.
Therefore DEATH must be owned by vanilla. The code mod should read this bit for KO/status checks
(`hasBit(targetByte(0x61), 5)`) but must not write it as a death mechanism.

### 6.2 Same-Hit Formula-Owned KO via Pre-Clamp — **Proven**

The pre-clamp staged-debit path delivers formula-owned lethal damage through the engine's own HP/KO
lifecycle in the same hit: managed formula evaluation produces the value, the native pre-clamp plan
rewrites the staged debit, and vanilla HP apply clamps to `0` and sets the KO/death lifecycle fields.

Historical late-HP-write path (superseded): the old architecture used `MinHpFloor=1`, causing a
two-hit kill — the rewrite left the target at 1 HP and a later vanilla chip delivered the real KO.
The pre-clamp path supersedes that for contexts resolvable in time (same-hit formula-owned KO
through vanilla HP apply). Late HP-write remains useful as fallback/debugging infrastructure but is
no longer the preferred damage architecture. (The reactive poller alone cannot prevent death: the
engine fires death the instant HP hits 0, before any ~25 ms poll, and death is a separate state from
the HP value — a faster poll narrows but never closes the race.)

## 7. Current Runtime Design Rules

These are the rules the code mod should follow unless a newer test updates this model.

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

## 8. Session Pointers and Identity

Unit pointers are session-specific: useful inside one live capture, never copied into long-lived
logic. After a fresh launch, rediscover pointers from `[UNIT]` lines or fresh snapshots, keyed on
char/team/stat sanity. Common char ids in the controlled setup: Ramza `0x01`, Ninja `0x80`, Agrias
`0x1E`, Cloud `0x32`, Beowulf `0x1F`.

## 9. Log-Line Glossary (reference)

- `[UNIT]`: registered unit pointer and identity.
- `[DUMP]` / `[CANDIDATES]` / `[DIFF]`: full struct hex on first hook-touch; non-zero / plausible
  16-bit id candidates; diffs when unknown bytes change.
- `[ACTION-STATE]`: changes in action/forecast/pending signature fields.
- `[ACTION-BOUNDARY]`: focused diff around action boundary bytes.
- `[HOOK-REGS]`: broad stable-hook register capture.
- `[HOOK-REGS-EVENT]`: recent hook snapshot correlated with HP/MP/CT/pending-resolve events.
- `[HOOK-PTRSCAN-EVENT]`: readable roots from a correlated stable-hook register snapshot.
- `[ACTOR-PROBE]`: per-unit `0x40..0x52` window snapshot at a damage event (CT resolution).
- `[PENDING-ACTION-CANDIDATES]`: registered unit pending/action state at event time.
- `[PENDING-ACTION-TRACK]`: runtime pending/resolving action lifecycle.
- `[PRECLAMP-PLAN-QUEUE]`: formula plan staged for native pre-clamp.
- `[PRECLAMP-REWRITE]`: actual native staged debit/credit rewrite.
- `[PRECLAMP-PTRSCAN]`: native pre-clamp register/stack root scan for unit-pointer context.
- `[PRECLAMP-ACTOR-CTX]`: memory-only caster/action resolver from the actor array.
- `[PRECLAMP-ACTOR-DUMP]`: actor-struct hex dump for any scanned root linking to a registered unit.
- `[PRECLAMP-EQUIP]`: live equipment-block read at the damage frame.
- `[PRECLAMP-IMMEDIATE-CANDIDATES]` / `[PRECLAMP-FORMULA-CANDIDATE]`: immediate-source scoring.
- `[DAMAGE]`: observed HP loss.
- `[HP-EVENT-PROBE]`: raw damage/clamp/lethal diagnostics.
- `[DEATH-DUMP]` / `[DEATH-DIFF]` / `[DEATH-FOLLOW]` / `[DEATH-WRITE]`: death-state capture/write
  instrumentation.
