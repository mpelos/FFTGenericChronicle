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
- CT is an observed engine field only; it is not accepted as a DCL action-context source.

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
| `+0x0A` | Innate ability 1 id | word | **Proven** ability id |
| `+0x0C` | Innate ability 2 id | word | **Proven** ability id |
| `+0x0E` | Innate ability 3 id | word | **Proven** ability id |
| `+0x10` | Innate ability 4 id | word | **Proven** ability id |
| `+0x14` | Reaction ability id | word | **Proven** equipped Reaction slot |
| `+0x16` | Support ability id | word | **Proven** equipped Support slot |
| `+0x18` | Movement ability id | word | **Proven** equipped Movement slot |
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
the *input-control* path — the cleaner primary, vs. the *output-control* hooks (pre-clamp `0x30A5D7`
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

Still unmapped from the struct: secondary action-skill state and height. The status bitfield is
**partly mapped and DATA-controllable** (see §2.3): `0x20`=KO and
`0x10`=Undead are both live-confirmed (the earlier offline `0x10`="control-flip" guess was WRONG);
remaining bits to map empirically. Stats drift with level, so only map level-matched dumps.

The ability-id block is **Proven**. Five independent unit snapshots resolve `+0x0A..+0x10` as four
innate abilities and `+0x14/+0x16/+0x18` as the equipped Reaction, Support, and Movement slots.
Their words match the ability catalog exactly; Ninja's `+0x0A = 477` also matches
`JobData.InnateAbilityId1 = 477` (Dual Wield). `+0x98` is a derived/effect bitfield, not the canonical
Support slot.

**✅ LIVE-PROVEN (LT1/LT2 mega-probe, 2026-07-02 — evidence `work/lt1-mega-probe-plan.md`):**

- **Status arrays**: source `+0x57..0x5B`, **immunity `+0x5C..0x60`**, effective `+0x61..0x65`,
  master `+0x1EF..0x1F3`, classic PSX bit layout verbatim (Blind landed as `eff[1] |= 0x20`
  Darkness; Charging = `[0] 0x08` through a Fire charge). Equipment-granted immunities appear in
  `+0x5C..` (e.g. Darkness+Sleep, DM+DA). **Writing an immunity bit is a proven input-control
  lever**: `imm[1] |= 0x20` on a clean unit → the status forecast dropped to 0% and the cast missed.
- **Position/facing**: `+0x4F` X, `+0x50` Y, `+0x51` facing (bit 7 = map level, bridge case
  untested); every move/facing change tracked coherently with the tile table (§2.5).
- **Turn owner**: `+0x1B8 == 1` — exactly-one invariant across player AND AI turns; `+0x1BA` = the
  in-flight action owner (set at confirm, persists to resolution); `+0x2E` also flips at turn grant.
- **Pending record**: `+0x1A1` type / `+0x1A2` ability id (Fire=`0x10`, Blind=`0xEA`, enemy=`0x118`
  — classic id space), `+0x18D` timer, `+0x1AC/+0x1B0` epicenter.
- **Per-job JP**: word arrays `+0xF0` (spendable) and `+0x11E` (total), **index = `jobId - 0x4A`**
  (Agrias +48 @ 6 = `0x50-0x4A`; Beowulf +53 @ 8 = `0x52-0x4A`; ally spillover +9/10 at the same
  index). Job level = total-JP vs Nex `GeneralJob.RequiredJobExp` thresholds. `+0x28` increments
  with actions (EXP candidate).
- **Reaction-eval id global**: `word[0x14186AFF0]` holds the reaction ability id under evaluation
  (445/451 observed at Mana-Shield/parry moments) — NOT the current action id (refuted for that
  role); `dword[0x14186AFF4]` = acting unit index (proven across player+enemy actions).
- **Staged reaction redirection**: Cloud's Mana Shield turned the staged HP debit into
  `+0x1C8 stagedMpDebit = 157` with result flag `+0x1E5 = 32` — reaction outcomes ride the same
  staged surface.

Still Hypothesis: job-level nibbles `+0xE4..0xEE`, elemental block `+0x52..0x56`,
equipment stat bonus `+0x3B/3C/3D` (snapshot-proven 15/15 but not
re-checked live).

### 2.5 Battle tile table — **Proven (LT1 2026-07-02)**

Tile table at VA `0x140D8DCB0`: 8 bytes/tile, 256 tiles/level, 2 levels (`+0x800`); index =
`(level<<8) + y*width + x`; map dims bytes at `0x140C6AD6A/6B`. Record: `+0` terrain type (`&0x3F`),
`+2` height, `+3` slope-height (`&0x1F`) | water-depth (`>>5`), `+4` per-corner slope weights,
`+5` **dynamic mark byte** (live: `0x20` = move-range highlight, `0x40`/`0xC0` = cursor/target —
range/AoE membership is a direct read), `+6` flags (bit0 unselectable, bit1 unwalkable). Corner
height = `2*b2 + ((b4>>2*corner)&3)*(b3&0x1F)` (recovered from `0x3050A5`). Full RE:
`work/dcl-tilemap-candidates.md`.

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
| `+0x57..+0x5B` | innate / equipment status-source array | **Proven** layout | five bytes, classic 40-bit status layout; OR-source for the effective array |
| `+0x5C..+0x60` | status-immunity array | **Proven** | five bytes, same bit positions as the status arrays; setting a bit makes the native forecast/roll reject that status |
| `+0x61..+0x65` | effective status array (mirror) | **Proven** layout | five bytes, classic 40-bit status layout; byte 0 is recomputed at `0x30D42A` from master byte 0 and source byte 0 |
| `+0x18D` | pending timer / charge phase | **Strong** | |
| `+0x1A0` | action-boundary byte | **Hypothesis** | |
| `+0x1A1` | action-boundary byte | **Hypothesis** | |
| `+0x1A2` | action id / last action id, u16 | **Strong** | `0` for basic attacks |
| `+0x1A8` | item/inventory side-effect id, u16 | **Strong** | consumed by `0x30CEA0`, whose valid range is the 261-entry item catalog and whose non-VM branch increments inventory quantity for that item id; Kiyomori stages item id `0x2B` here |
| `+0x1B8` | active marker-ish (`b8`) | **Hypothesis** | |
| `+0x1BA` | active source marker (`ba`) | **Strong** when exactly `1` | |
| `+0x1BB` | hit/phase marker (`bb`) | **Proven** | `0x02` on damage-apply (hit), `0x01` on an evade; target-side result state, not an attacker hand or Dual Wield strike index |
| `+0x1BE` | staged-result-present | **Proven** | `0x01` = damage result staged, `0x00` = evade / no-damage |
| `+0x1C0` | EVADE-TYPE (animation lever) | **Proven** | see enum below |
| `+0x1C4` | forecast HP-debit / staged HP-debit / target cache | **Proven**, context-dependent | damage preview number + ghost depletion; also "staged DAMAGE (word)" at apply |
| `+0x1C6` | forecast HP-credit / staged HP-credit / target cache | **Proven**, context-dependent | healing preview number + ghost refill; apply: `newHP = clamp(HP + heal - dmg)` |
| `+0x1C8` | staged MP-debit (word) | **Proven** (live 2026-07-04 LT9b: zeroing it pre-apply cancelled the Mana-Shield MP drain 5/5, incl. crit-boosted values) | MP analogue of `+0x1C4` |
| `+0x1CA` | staged MP-credit (word) | **Strong** | apply: `newMP = clamp(MP + 0x1CA - 0x1C8)` |
| `+0x1D0` | staged side-effect gates | **Strong** | bit `0x08` gates the `+0x1A8` item/inventory consumer; it is not a status-apply mask |
| `+0x1D8` | forecast / charge / target metadata | **Strong** | charge/forecast value (word) |
| `+0x1DB..+0x1DF` | staged status-add packet | **Proven** for ordinary HP-result delivery | five bytes in the classic 40-bit layout; filtered against effective state and immunity before native commit |
| `+0x1E0..+0x1E4` | staged status-remove packet | **Strong** | five bytes in the same layout; removal preserves innate/equipment source bits |
| `+0x1E5` | forecast / target metadata; resultKind bits | **Strong** | see resultKind bits below |
| `+0x1EF..+0x1F3` | durable status-master array | **Proven** layout | five bytes, classic 40-bit status layout; effective mirrors are `+0x61..+0x65` |
| `+0x1F5` | death / lifecycle byte | **Hypothesis** | |

Known bit / value meanings:

```text
unit+0x1EF bit 0x20  = KO/dead state (durable master)
unit+0x61  bit 0x20  = KO/dead state (effective mirror)
unit+0x1EF bit 0x10  = Undead (live-confirmed 2026-06-27; offline "control-flip" guess was WRONG)
unit+0x1EF value 0x08 = pending/charging action-ish (per-turn-cleared on +0x61 via the 0xF2 mask)
unit+0x61  value 0x08 = pending/charging mirror/state
```

**Status is DATA-controllable** (**Proven**: writing the Undead bit to master/effective made the unit
Undead). For status bit index `s` in the classic 40-bit layout, `byte = s / 8` and `mask` selects the
bit within that byte:

- **Force** a status → OR `mask` onto `+0x1EF+byte` (durable master) and `+0x61+byte` (effective mirror,
  so native behavior and presentation see it immediately).
- **Cure** an inflicted status → clear `mask` from master and effective. Clear the matching
  `+0x57+byte` source bit only when intentionally removing an innate/equipment-granted status.
- **Immunity / native suppression** → set the matching bit at `+0x5C+byte`; the engine produces 0%
  and rejects the native status without changing the durable arrays.

Byte 0 has an additional native turn-boundary recompute at `0x30D42A`:
`effective[0] = (master[0] & 0xF2) | source[0]`. The full five-byte layout is the authority; the old
single-byte `StatusOverride1EF/61/57` knobs are diagnostic-only.

The native per-action status transaction uses the staged packets rather than writing the durable
array directly. The ordinary result apply path validates the packet and calls the current-build
commit at `0x30C878`. Its remove consumer clears matching durable bits, while its add consumer ORs
durable bits, invokes native per-status side effects, and rebuilds effective state. Thirteen
real-code formula handlers converge on the common protected packet finalizer at `0x306988`.
`tools/analyze_dcl_status_transactions.py` guards the packet, validator, commit, and add/remove
consumer anchors for the installed executable.

The ordinary HP-result path reaches the DCL pre-clamp hook before that validator and commit. A
managed damage-plus-status rule therefore edits only its owned bit in the paired staged packet,
leaves every unrelated bit intact, and derives result bit `+0x1E5 & 0x08` from whether either packet
remains nonempty. A resisted or immune rule clears its owned bit from both halves. Numeric HP/MP
channels, the paired packet, and the result byte form one rollback-protected staged transaction;
the later native commit remains the sole durable/effective writer. This carrier applies only when
the action independently reaches the ordinary HP-result path. Status-only and special-formula
actions require a producer at their own calculation/finalization boundary.

**Proven for an ordinary add carrier:** a basic Attack with no native status rider preserves its
natural `14` HP debit while the managed pre-clamp transaction stages Blind as add byte `1`, mask
`0x20`, leaves the matching remove bit clear, and changes result flags from HP debit `0x80` to the
HP-plus-status composite `0x88`. Native delivery then presents Blind on the target, and the target's
ordinary Auto-Potion Reaction remains a separate `30` HP-credit transaction. The DCL does not write
the durable or effective arrays directly in this path.

Per-action RESULT / OUTCOME fields (**Proven**, the hit/miss/block/parry control surface; same
`0x200` unit struct, written by the engine at action resolution and read by the result/animation
selector at `module+0x205210`; the roll is Denuvo-virtualized but its OUTCOME lands in these bytes,
so writing them drives the result and the native animation):

```text
+0x1BB  hit/phase marker (byte)      0x02 on a damage-apply (hit), 0x01 on an evade; not active-hand identity
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

The high nibble is a bitwise numeric bundle (**Proven/Strong**), not an exclusive enum:
`0x80` is HP debit, `0x40` HP credit, `0x20` MP debit, and `0x10` MP credit. Native current-build
writers emit `0x50` for combined HP+MP credit and `0x90` for HP debit plus MP credit. The real-code
selector at `0x205286..0x2053EF` prioritizes `0x80` HP damage, then `0x20` MP debit, then low-bit
effects, then the shared `0x40/0x10` credit route. A combined HP+MP debit therefore uses `0xA0` and
selects HP damage as its primary presentation; exact `0xA0` popup/reaction composition is a live
presentation gate because no literal native `0xA0` writer is known.

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
- **Hypothesis** for self-hit or self-AoE: when the caster is also a final HP target, there may be no
  non-target actor to select. In that case, a target-linked actor with a positive action id is the
  self-caster candidate, logged as `resolved-self` by the probe.
- `+0x142` sits right before the self unit pointer at `+0x148`, making it the primary "this actor's
  current action id" candidate.
- No target list inside the caster actor — scanning the full `0x548` found only the self pointer at
  `+0x148`. Targets are taken from each pre-clamp HP event instead.
- The caster actor is constant across an AoE batch; the target actor varies. Stack slot index is
  not fixed, so the discriminator is by content (`+0x148 != current target`), not by slot.
- Native pre-clamp registers only carry the target (`rcx/rdi/r8`); the caster never appears in a
  register, only as an actor-struct pointer on the stack.

This is the strongest candidate for a real engine "current executing action context" and is
reachable straight from memory at damage time; it replaces CT for action classes covered by live
validation. An observe-only memory-only resolver
(`[PRECLAMP-ACTOR-CTX]`) is validated head-to-head in one battle/save for:

- basic attack: `actionId=0`, weapon identity from equipment;
- immediate named ability: `actionId=159` Divine Ruination;
- charged single-target action: `actionId=257` Braver;
- charged AoE action: `actionId=258` Cross Slash, same caster/action across multiple target HP
  events.

Credit/tick events with `oldDebit=0` return `no-caster-actor`, naturally separating real damage
actions from passive ticks.

Selector-frame source context (**Strong**, validated for normal-hit rows and a
Shirahadori/Blade-Grasp no-HP row in one battle/save): the result/animation selector at
`module+0x205210` carries the target/result actor through `actor`/`rbx`/`r8`/stack self refs and
the source actor through `rdx`, `r15`, and a stack source ref such as `+0xA0` or `+0x90`. The source
actor uses the same `actor+0x142` action id candidate (`0` for a basic attack). This is especially
important for no-HP outcomes: a Blade-Grasp-style row has `+0x1BE=0`, `+0x1C4=0`, `+0x1E5=0` and no
positive HP pre-clamp event, but the selector frame still exposes the non-target source actor/action.

Cross-confirmation: the result/animation selector at `module+0x205210` reads its record as
`[r8+0x148]` where `r8` is the actor object — i.e. the same `actor+0x148 -> unit` array.

Still open before promoting actor-context to primary `DamageEvent.Attacker`/`Action` everywhere
(gated on `oldDebit>0`, with pending tracker and selector context as complementary ownership
surfaces): overlapping/simultaneous pending casters, named-action no-HP outcomes, Hamedo/
First-Strike cancellation of the incoming action, self-hit/self-AoE with `resolved-self`, and
actor-array RVA/layout stability across a different battle/save. CT can appear in historical probe
logs, but it is not an accepted ownership surface.

## 4. Hook Map

### 4.1 Stable Unit Hook — **Proven** for unit observation; not a full action hook

```text
Name: battle_base_ptr / stable unit touchpoint
Signature context: 0F B7 41 30 66 89 42 0C
Approx module offset: module+0x226D20
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

#### Calc-entry order payload — **Proven**

`computeActionResult` at RVA `0x3099AC` copies the 20-byte order record and loads the word at
`orderRecord+8`. For action type `1` (weapon Attack), the native branch validates that word as
protected lookup kind `6`, resolves the corresponding weapon-data row at `0x2B8E00`, reads the
row's formula byte, and dispatches that formula at `0x309F4F`. The word is therefore the exact
weapon item id selected for this calculation.

Comparing the payload with the source unit's right and left weapon words at `+0x20` and `+0x24`
identifies the active side when the equipped ids differ. Equal ids leave the side ambiguous but
preserve the exact weapon identity, family, and skill route. The DCL calc-entry ring captures this
payload synchronously; downstream action and hit-decision caches retain it so a mixed-weapon
second strike cannot reuse the first weapon's decision.

### 4.2 Native Pre-Clamp Hook — **Proven**, the primary formula-write hook

```text
Approx RVA: 0x30A5D7
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

The same apply routine (`0x30A484`) also writes MP: `newMP = clamp(MP + word[+0x1CA] - word[+0x1C8],
0, MaxMP)`, stored `mov [rdi+0x34], ax` at `0x30A634`. So this one hook can rewrite **MP** cost/damage/
restore as well as HP — the MP control mechanism is identical to HP (force the staged MP words).

What it gives: pre-apply target HP; raw staged damage (`oldDebit`) before clamp; raw staged credit
(`oldCredit`) for heal/credit-like cases; a place to rewrite damage before vanilla applies HP and
KO; a native-frame register/stack capture point for the actor array (§3).

Managed same-frame bridge (**Proven**, fixed-debit and actor-formula proof): the mid-function ASM
hook can call a Reloaded reverse-wrapper C# callback synchronously, pass the target unit pointer
(`rdi`), staged state pointer (`rbp`), hook-save stack pointer, and pre-clamp buffer, then write the
callback's returned debit to `[rbp+6]` before vanilla consumes it. The callback can resolve the
caster from the native actor context by scanning saved registers/original stack roots for an actor
struct whose `+0x148` points to a registered unit, then compute a formula from caster and target
stats. Guarded proofs resolved caster `0x1E`, target `0x1F`, and wrote formula debit `56` in the
native HP-apply frame for both a basic attack (`oldDebit=151`, `actionId=0`) and an instant named
ability (`oldDebit=205`, `actionId=159`). The same bridge also resolved delayed Braver at HP-apply
time (`caster=0x32`, `target=0x1F`, `oldDebit=153`, `actionId=257`) and wrote formula debit `89`.
Broader action-family coverage remains separate validation work.

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

#### Staged target-CT delta — **Strong**

The state-apply tail owns a bounded target-CT delta after the pre-clamp commit:

```text
result + 0x15 = unit + 0x1D3 = staged CT delta byte
unit   + 0x41 = live CT
bounds supplied to the native delta helper = 0..255
```

The tail loads `byte[result+0x15]`, addresses `unit+0x41`, supplies the bounds `0..255`, and calls
the same native bounded-delta helper used for the neighboring staged stat channels. The pre-clamp
at `0x30A5D7` precedes this CT application, so managed code can stage a CT delta in the same
execution transaction as HP/MP and status effects without directly writing live CT after the
action resolves.

Staged one-byte stat and CT deltas use sign-magnitude encoding rather than a signed byte:

```text
increase by n = 0x80 | n
decrease by n = n
```

The native Song/Dance handlers provide symmetric witnesses: `0x81` is +1 Speed while `0x01` is
-1 Speed; `0xFF` is +127 CT while `0x7F` is -127 CT. Consequently CT +8 is encoded as `0x88`.

Formula `0x22`, used by the native status-only Iaido actions Kiyomori and Masamune, enters the
shared status-result builder and reaches the ordinary state-`0x15` apply path. The apply routine
does not gate the pre-clamp on a positive HP/MP debit or credit. This makes the existing pre-clamp
the **Strong** commit candidate for status-only support actions as well as damage and healing. The
native Protect/Shell or Regen/Haste rider remains present long enough to preserve the result carrier;
managed code replaces every inherited bit in the paired packet before native validation and commit.
One live vertical slice must still prove that an all-zero HP/MP formula-`0x22` execution fires the
managed callback exactly once. Staged CT changes use this same boundary; staging `0x88` for CT +8
still requires its own native-cap live gate.

Formula `0x38` dispatches directly to the same status finalizer at `0x306988`; its 22 catalog
actions all declare a 100% status result and have no real-code chance/prerequisite wrapper before
packet production. A retained native payload can therefore carry status-only execution into the
same pre-clamp replacement window. Generic ownership accepts 20 actions: one-bit contests,
deterministic cancel sets, independent per-bit outcomes, cached random-one groups, and shared
all-or-nothing groups. Nightmare and Toot select one group member before the shared resistance
contest; Poisonous Frog applies its Frog/Poison members from one contest. The two Dead-bearing
actions remain under the lifecycle owner. `tools/analyze_dcl_formula38_carriers.py` guards the
dispatch and exact partition.

### Native repeated-result carrier

**Strong:** action byte 4 bit `0x08` is the native `RandomFire` carrier. Its protected consumer
dispatches selector `0x2826B0`, which clears target-map bit `0x80` and sets it for exactly one
eligible tile. Result producer `0x281CE8` invokes that selector inside the repeat, sweeps the chosen
tile through the ordinary calculation at `0x3099AC`, increments the repeat index at RVA `0x7B0763`,
compares it with the repeat count at RVA `0x7B0762`, and writes the continuation flag to result
offset `+0x18`.

Formulas `0x1E/0x1F` initialize 1..10 repeats from the 100-point weight table
`5,5,10,10,20,20,10,10,5,5` at RVA `0x9069D0`. Formula `0x5E` initializes `X+1` repeats. Each native
repeat therefore selects one target and performs one ordinary calculation. A spell-level per-target
decision such as DCL Magic Evade remains valid when a target is selected again; a status packet is
produced afresh for each repeated result. Live integration confirms selector/pre-clamp/apply ordering
but does not define the repetition mechanism.

Formula `0x6A` Barrage uses the same count/index and continuation carrier without `RandomFire`.
Its initializer stores fixed count `4`; the dedicated random-tile selector is skipped, so the
original one-target selection remains active. Each repeated calculation enters the equipped-weapon
formula and ordinary normal-attack postprocessor. **Strong:** Barrage is a target-stable four-result
weapon transaction; live validation covers only downstream apply, presentation, hand identity, and
reaction cadence.

The 82 status actions in conditional formula families use a different carrier. Formula `0x0A`,
`0x0B`, and ten special families can skip their native packet finalizer when a prerequisite or
chance gate fails. The outer target sweep nevertheless returns from `computeActionResult` at
`0x281F12` for every evaluated target. **Proven:** confirmed player execution reaches that return in
battle state `0x2A`, while player forecast calls return through `.trace` at `0xEF53F14` in state
`0x19`. The runtime accepts only the outer-return/state-`0x2A` pair, evaluates the complete
per-ability DCL packet once, and stores that decision for reuse at pre-clamp. A successful packet
sets the native result carrier; a resisted, immune, or ineligible action clears every owned bit and
does not need to enter apply. The loaded catalog fixes the complete bit set, add/remove operation,
and `independent`, `all-or-nothing`, or `random-one` bundle mode. Formula `0x25`'s nested synthetic
Attack row never replaces the outer action identity in this cache.

Nameless Song `91` and Forbidden Dance `98` use the same execution producer. Their formula
handlers `0x1C/0x1D` retain exact action identity, tail-call the common status finalizer, and expose
complete catalog packets of five beneficial or eight harmful statuses in `Random` mode. The runtime
therefore owns each packet as one `random-one` contest. **Strong:** the native performance
scheduler retains the action at `unit+0x1A2`, emits ordinary result-apply events, and owns cadence
and cleanup. The managed producer preserves the handlers' caster-Sleep eligibility gate and never
resurrects a sleeping performer's tick. Live proof of tick cardinality and stop/cleanup ordering is
still required before enabling either performance rule set.

Formula `0x20` calls the shared status-result builder and then unconditionally enters its non-Faith
`MA * Y` damage tail. Ame-no-Murakumo and Muramasa are its two status-bearing catalog actions.
Clearing Muramasa's native `InflictStatus` therefore preserves the independent HP result; managed
code replaces its Confusion/Death Sentence bundle with one `random-one` contest. The exact handler,
catalog pair, data allowlist, and grouped mapping are guarded by
`tools/analyze_dcl_formula20_muramasa.py`.

Formula `0x5B` reads the target monster graphic-set byte at `target+0x18E`, subtracts 15, and accepts
only the resulting values 0 and 1. An ineligible target returns with an inactive result. The eligible
path stages an HP credit at target `result+0x06`, twice that value as an HP debit at source
`result+0x08`, and calls the common status finalizer at `0x306940`. Its status-cancel payload is
therefore separable from the species-gated paired numeric transaction.

Formula `0x52` compares the current target and caster unit pointers. A non-self result stages
`caster.maxHp - caster.hp` as HP debit and calls the status finalizer, which owns the native Oil
rider. The self result stages `caster.hp` as HP debit and skips that finalizer. The victim-only rider
can therefore be suppressed in data and restored by an exact pointer-identity condition without
changing either native damage branch.

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

### 5.1 CT: Observed Engine Field Only — **Proven** as CT; **Refuted** as mod ownership

`unit+0x41` is CT. It rises with Speed and drops/resets when a unit acts in the normal FFT turn
model. Early immediate-action resolution used `attacker ~= unit whose CT recently dropped/reset`,
which scored 5/6 by absolute-lowest CT and 6/6 with a largest-recent-drop tiebreak across 6
controlled attacks.

Why CT is not enough: Wait does not reset CT like an action; delayed/charged actions resolve
several turns later; counters / Hamedo / First-Strike-like reactions and interrupts may not create
a clean CT drop; multiple charged actions can be pending at once; in delayed Cross Slash AoE,
CT-only logic resolved no attacker for final HP events.

Rule: do not use CT for DCL action ownership. The mod's accepted ownership surfaces are native-frame
register/stack/actor context, pending context, selector context, and final pre-clamp HP/MP targets.
CT may remain in historical logs and sanity reports, but it must not be required by formulas or
runtime decisions.

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

Counter/First-Strike damage that reaches HP apply (**Strong**): the reaction attack's own HP events
route through the normal pre-clamp and selector hooks. Actor-context identifies the reaction attacker
as source and the original attacker as target.

First-Strike / Hamedo-like cancelled incoming actions expose a separate interrupted-action surface
(**Strong for a basic incoming attack**): before the reaction damage applies, the original target's
target cache can hold the interrupted incoming debit at `+0x1C4` with damage result metadata. At
the correlated `targetcache` hook-register frame, `hookPtr` and multiple register/stack unit refs
point to the original incoming source while `targetPtr` remains the defender. For a basic incoming
attack this gives source + target before HP apply is cancelled. Open reaction risks: named incoming
action id (`actionId > 0`) at this frame, Hamedo variants, criticals and random damage, multi-target
instant abilities, and identifying weapon/action family, not just source and target.

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

#### Staged stat, CT, Brave, and Faith deltas — **Strong**

The ordinary result record exposes six one-byte sign-magnitude delta channels. Bit `0x80` means
increase and bits `0x7F` carry magnitude; a clear high bit means decrease. State apply sends each
channel through the native bounded-delta helper:

| Result field | Live field | Native bounds |
| --- | --- | --- |
| `result+0x14` | raw Speed `unit+0x3A` | `1..50` |
| `result+0x15` | CT `unit+0x41` | `0..255` |
| `result+0x16` | raw PA `unit+0x38` | `1..99` |
| `result+0x17` | raw MA `unit+0x39` | `1..99` |
| `result+0x18` | Brave `unit+0x2B` | `0..100`, or minimum `10` for the native protected flag |
| `result+0x19` | Faith `unit+0x2D` | `0..100` |

Native witnesses include `0x81 = +1`, `0x01 = -1`, `0xFF = +127`, and `0x7F = -127`.
These carriers cover the direct stat/trait buff, debuff, and Talk families. The mechanism is not a
duration model: when the DCL action is temporary, it stages the authored status/token through the
DCL status-duration system; it uses these native numeric carriers only when an immediate persistent
delta is intended. Campaign safety and the per-ability temporary/permanent choice are design policy,
not missing result fields.

#### Persistent Song/Dance performances — **Strong**

Bardsong `86..92` uses formula `0x1C`; Dance `93..99` uses formula `0x1D`. Both handlers switch on
the exact action id. Their numeric outputs use the ordinary staged result channels:

| Effect family | Song channel | Dance channel |
| --- | --- | --- |
| HP | credit at `result+0x08` | debit at `result+0x06` |
| MP | credit at `result+0x0C` | debit at `result+0x0A` |
| raw Speed | `result+0x14 = 0x81` (+1) | `result+0x14 = 0x01` (-1) |
| CT | `result+0x15 = 0xFF` (+127) | `result+0x15 = 0x7F` (-127) |
| raw PA | `result+0x16 = 0x81` (+1) | `result+0x16 = 0x01` (-1) |
| raw MA | `result+0x17 = 0x81` (+1) | `result+0x17 = 0x01` (-1) |
| random status bundle | shared status-result finalizer | shared status-result finalizer |

The persistent state tests `unit+0x61 & 0x09` and retains the exact action id at `unit+0x1A2`.
The battle scheduler scans all 21 unit slots for the same Performing/Charging mask. Event class
`0x300` calls the ordinary state-apply routine; after validation, event class `0x200` clears bits
`0x01` and `0x08` from both `unit+0x61` and `unit+0x1EF` with mask `0xF6` and updates the pending
timer at `unit+0x18D`.

The DCL preserves this native cadence, action identity, and cleanup. Authored performance effects
replace the staged output of each tick through the ordinary calculation/pre-clamp surfaces; they do
not require a separate managed periodic timer. Runtime callback cardinality and concrete stop causes
remain a live validation gate.

#### Native Steal transactions — **Strong**

Formula `0x26` uses the exact action id to select an equipped item and its slot mask. The shared
selector at RVA `0x3065C0` covers both Steal `110..114` and Plunder `361..365`:

| Action pair | Equipment source | Staged slot mask |
| --- | --- | ---: |
| Helm `110/361` | `target+0x1A` | `0x80` |
| Armor `111/362` | `target+0x1C` | `0x40` |
| Shield `112/363` | `target+0x22` or `target+0x26` | `0x08` or `0x02` |
| Weapon `113/364` | `target+0x20` or `target+0x24` | `0x10` or `0x04` |
| Accessory `114/365` | `target+0x1E` | `0x20` |

The selector writes the chosen item id to `result+0x04` and the slot mask to `result+0x1B`.
Formula `0x26` writes `result+0x12 = 0x0010` on success. The native VM owns the subsequent permanent
equipment removal and inventory transfer; the DCL preserves that transaction and changes only the
authored eligibility/chance at the post-calculation, pre-commit result boundary.

Formula `0x27` stages equal and opposite values at `result+0x0E` for the two participants. The
state-apply path passes that signed channel to the native campaign-value bridge at `0x30CF34`.
Formula `0x28` stages paired EXP deltas at `result+0x2A`; its field-specific encoding uses a clear
high bit for addition and `0x80 | magnitude` for subtraction. State apply bounds live EXP at
`unit+0x28` to `0..255` and invokes the ordinary level/progression refresh.

The target sweep calls `computeActionResult` at `0x281F0D`; `0x281F12` is the existing hookable
post-calculation boundary before application. A live vertical slice still validates that authored
success/failure rewrites preserve exactly one native transfer and never duplicate inventory.

The same ordering defines the DCL AI-scoring boundary. The sweep calculates every selected target,
then copies the affected-target list and summary fields into its output record. `0x281F12` exposes
the normalized staged bundle after each target calculation and before the sweep output is complete.
The later pre-clamp at `0x30A5D7` is execution apply only and cannot change an already-consumed AI
score. Because the sweep is VM-owned and its protected consumer is not statically visible, whether
AI utility consumes the rewritten `0x281F12` bundle or an earlier protected aggregate remains a
live target-choice gate.

#### Native equipment-break transaction — **Strong**

Formulas `0x25` (Rend) and `0x2E` (Crush) use the same exact-id selector as equipment
Steal/Plunder. Valid selection writes the equipped item id to `result+0x04` and its slot mask to
`result+0x1B`:

| Slot | Equipment source | Mask |
| --- | --- | ---: |
| Helm | `target+0x1A` | `0x80` |
| Armor | `target+0x1C` | `0x40` |
| Shield | `target+0x22` or `target+0x26` | `0x08` or `0x02` |
| Weapon | `target+0x20` or `target+0x24` | `0x10` or `0x04` |
| Accessory | `target+0x1E` | `0x20` |

Successful destruction is staged as `result+0x12 = 0x0004`. Formula `0x2E` continues through its
ordinary physical damage chain after staging the bit, so side effect and numeric damage can be
controlled separately at the post-calculation boundary. Formula `0x25` is re-entrant when the
requested slot is absent: it saves the outer order, temporarily writes Basic Attack
`(type=1, action=0)`, calls `computeActionResult`, and restores the Rend order.

The DCL Knight replaces permanent item loss with reversible Guard Break and Bind Weapon. The
data-first route repoints the reused action record to an ordinary authored physical/status formula,
which also removes the nested Rend calculation. If presentation requires retaining a native break
formula, clearing result bit `0x0004` before the protected side-effect applicator suppresses the
equipment transaction while leaving separately authored numeric damage available.

#### Paired target/caster recoil results — **Strong**

Formula `0x42` exposes both sides of its recoil transaction before native state application:

```text
targetResult = qword[0x14186AF70]
casterResult = qword[0x14186AF60]

targetResult+0x06 = PA * Y
casterResult+0x06 = trunc((PA * Y) / X)
targetResult+0x27 = casterResult+0x27 = 0x80
casterResult+0x00 = 1
```

The target and caster records therefore enter the same ordinary HP clamp/KO lifecycle as two staged
debits. DCL recoil authors both records at the calculation/post-calculation boundary rather than
performing a late direct self-HP write. The live integration gate is cardinality—one target apply
and one caster apply—not discovery of the record identities or fields.

#### Paired target/source drain results — **Strong**

Drain actions use the same two-record transaction surface. The selected target record is
`qword[0x14186AF70] = target+0x1BE`. The source side is the scratch record at RVA `0x186AFC0`,
addressed through `qword[0x14186AF60]`. The scratch record is cleared before the selected-target
calculation sweep.

The native result shapes are:

| Transaction | Target record | Source record |
| --- | --- | --- |
| HP drain, ordinary target | `+0x06=amount`, `+0x27=0x80` | `+0x08=amount`, `+0x27=0x40`, `+0x00=1` |
| HP drain, Undead target | `+0x08=amount`, `+0x06=0`, `+0x27=0x40` | `+0x06=amount`, `+0x27=0x80`, `+0x00=1` |
| MP drain | `+0x0A=amount`, `+0x06=0`, `+0x27=0x20` | `+0x0C=amount`, `+0x27=0x10`, `+0x00=1` |

The shared result normalizer caps each HP/MP debit or credit word to `999`. State application then
computes `current + credit - debit` independently for each participant and clamps the result to
that participant's `0..MaxHP` or `0..MaxMP`. The paired source amount is therefore the normalized
staged amount; it is not first reduced to the target's remaining resource.

Formulas `0x0F`, `0x10`, `0x2F`, `0x30`, `0x47`, `0x4D`, `0x65`, and `0x66` all enter these paired
HP/MP transactions. Their twelve stock ability records have `AoE=0`, so no stock drain requires a
multi-target accumulation policy. DCL drains can author both records at the post-calculation
boundary while retaining native resource clamps, HP/KO lifecycle, and presentation. Amount,
avoidance, element, Undead reversal, and source-cap behavior remain per-ability policy.

#### Native Golem team pool — **Strong**

Formula `0x14` stages `result+0x12 = 0x0001` on successful activation. State apply selects one of
four unsigned 16-bit pools packed at RVA `0x186B020` from the activating unit's team/faction bits and
writes `unit+0x32` MaxHP into it. The stock Golem action is self-targeted, so its normal activation
uses the caster's MaxHP and team.

Native interception uses the same result word as a transaction marker: a negative
`result+0x12` selects the target team's pool from `unit+0x1EE >> 4 & 3`. State apply computes:

```text
pool[team] = max(pool[team] - signed(result+0x06), 0)
skip ordinary HP debit for this result
```

The whole triggering hit is absorbed even when its debit exceeds the remaining pool; the pool
becomes zero and no residual from that hit reaches HP. The packed four-pool qword participates in
the native battle-state import/export block at RVAs `0x30D855` and `0x30DA63`. DCL reuses this state
and lifecycle; an authored barrier magnitude belongs at the mapped pool initialization rather than
in a second managed store. The exact incoming action families that receive the negative marker and
recast cardinality remain live integration gates.

#### Native Malboro transformation — **Strong**

Formula `0x58` stages a dedicated transformation transaction, not a status bundle. Its only stock
record, ability 329 Malboro Spores, has `InflictStatus=0`. On a successful hit the formula requires:

- character id `unit+0x00` in `0x80..0x82`;
- `unit+0x05 & 0x04 == 0`;
- `unit+0x1B4 == 0`;
- job id `unit+0x03` outside `0x82..0x84`.

Eligible targets receive `result+0x12 = 0x0002`; ineligible targets receive the native failure
result. State apply recognizes bit `0x0002`, removes the unit's existing statuses/effects, clears
the related state ranges at `+0x57..+0x75` and `+0x1EF..+0x1F3`, and enters the protected native
job/appearance transformation continuation. DCL preserves this transaction. Only its visible final
job presentation and battle/campaign persistence require live integration validation.

### 5.4 Native weapon line-of-fire authority — **Strong**

The weapon-target evaluator at RVA `0x280170` selects the equipped hand and reads
`ITEM_WEAPON_DATA.AttackFlags` at weapon-data byte `+1`. It dispatches three distinct trajectory
policies: `Arc=0x10` to resolver `0x3129DC`, `Direct=0x20` to resolver `0x312ECC`, and
`Lunging=0x40` to resolver `0x2804D8`. The stock item table assigns Direct to every gun and
crossbow, Arc to every bow, and Lunging to every pole.

Arc and Direct receive the acting unit index, a pointer to the staged target coordinates at
`unit/order+0x1AC/+0x1AE/+0x1B0`, and the candidate unit index occupying those coordinates. Their
return value is the unit actually reached or intercepted, with a negative value for failure. The
call site accepts the intended candidate only when both indices are nonnegative and exactly equal.
This equality is the native authoritative line-of-fire and target-inclusion verdict for that weapon
trajectory; coordinate distance alone is not equivalent.

The Arc resolver is real code and performs trajectory/collision resolution. Direct enters protected
VM code at `0x312ECC`, so its internal obstacle algorithm remains opaque offline. The Lunging helper
is not generic projectile LoS: it rejects non-cardinal paths and recursively checks intermediate
terrain. Calling these resolvers asynchronously is unsafe because they participate in mutable native
targeting state; reuse belongs synchronously on the game thread or through a native basic-weapon
order that already traverses this evaluator.

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

### 6.3 Native Revive Preservation — **Proven**

The state-apply routine gates actions against a KO target through `unit+0x61 & 0x20` and accepts the
native revive state flags carried in the staged record at `unit+0x1DB` / `unit+0x1DC`. HP is then
applied from the ordinary staged debit/credit words before the native status/lifecycle tail runs.
Reraise and formula-driven Raise use this tail. A compute-point rewrite of Raise's native staged HP
credit from `46` to `111` reaches confirmed execution with `cached=1`; native apply clamps the credit
to the target's `91` MaxHP, changes HP from `0` to `91`, and then clears effective Dead at
`unit+0x61` bit `0x20`.

The DCL preserves that ownership boundary: a revive-specific formula may replace only the staged HP
credit at `unit+0x1C6`. It does not clear `Dead` in the durable/effective status arrays and does not
write a death-state byte. The native staged flags and lifecycle tail remain responsible for returning
the unit to the turn manager coherently. Authored revive credit therefore has the effective result
`min(authored credit, target MaxHP)`.

### 6.4 Native Bequeath Bacon / Crystal Preservation — **Strong**

Formula `0x57` dispatches to a dedicated handler that reads target level at `unit+0x29`, rejects a
target already at level `99`, and stages native level-up flag `0x80` at `result+0x12`. The state-apply
routine consumes that flag as level +1, caps it at `99`, and writes the bounded value back to
`unit+0x29`.

Bequeath Bacon's action data assigns Crystal to the caster. Its formula handler redirects the native
actor/result globals before entering a protected native finalizer, and the later state-apply path
processes status metadata through the 40-status loop. The finalizer is virtualized, so the complete
caster transition is **Strong**, not Proven. The state tail separately gates `unit+0x61 & 0x60`,
confirming that Crystal and KO are lifecycle states rather than ordinary temporary statuses.

The DCL preserves formula `0x57`, its bounded level mutation, and its caster Crystal/campaign
lifecycle unchanged. It does not replace the formula and never writes or clears Crystal directly. A
live test is required only as a regression gate if this formula, its status data, or the lifecycle tail
is modified.

### 6.5 Reaction-Chance Ownership — **Strong**

The current reaction dispatcher begins at RVA `0x30B4EC`. It reads the reaction-set bitfield at
`unit+0x94..0x97` and stages native Reaction ids `422..453`.

The reaction stager preserves `dword[0x14186AFF4]` as the incoming actor index. Its Counter branch
reads that index to select the original attacker's unit and writes the source index/coordinates into
the reaction order record. The source unit's `+0x1A1/+0x1A2` order fields and the per-target
calc-entry cache provide the incoming action type/id. This source/action identity is **Strong**;
cadence consumption must occur at an execution-only commit signal, not during chance evaluation.

The current `computeActionResult` entry at RVA `0x3099AC` has three direct callers across executable
sections (**Proven**). RVA `0x281F0D` is the ordinary affected-target sweep call. RVA `0x307ED0` is a nested
call from formula handler `0x25`, used by Rend Helm, Rend Armor, Rend Shield, and Rend Weapon. RVA
`0xEF53F0F` is the player-forecast call in executable `.trace` code; it returns at `0xEF53F14` in
battle state `0x19`, while confirmed execution returns at `0x281F12` in battle state `0x2A`. That
handler saves the outer order type/id, temporarily writes Attack `(type=1, id=0)`, re-enters the
calculation for the same target, and restores the outer order record. A latest-per-target cache that
records every entry can therefore replace the outer Rend identity with the synthetic inner Attack
identity. The UI forecast pointer does not distinguish this at entry: its builder publishes
`target+0x1BE` only after the forecast calculation returns. Preserve both caller provenance and the
outer identity until live Rend coverage establishes whether the fallback requires a distinct inner
decision.

The action-queue entry has one direct caller, `0x2121F7 -> 0x206344`, and iterates three actor
construction passes:

| Pass | Selection | Actor | Exact action-id source | First post-store RVA | Classification |
| ---: | --- | --- | --- | ---: | --- |
| `0` | VM-owned selector `0x282BDC` | new actor in `rbx` | selector output | `0x2066AE` | **Strong**, family unclassified |
| `1` | VM-owned classifier `0x28198C` | reused actor in `rdi` | `actor+0x17A` | `0x206743` | **Proven** generic action path |
| `2` | real-code selector `0x282E38` | new actor in `rbx` | selected `unit+0x1CE` | `0x206421` | **Proven** native Reaction commit |

Every path switches battle state to `0x29` and mirrors its exact action id to `actor+0x18C` and
`actor+0x142`. The listed RVAs are the first instructions after those stores, but they are not all
Reaction commits. Pass 1 fires for ordinary queued abilities, including Claw `280`, and therefore
cannot own Reaction cadence without an exact Reaction-id guard. Pass 2 walks the 21-unit battle
array, stages an order at `unit+0x1A0`, and consumes/clears the exact Reaction id at `unit+0x1CE`.
It has explicit real-code delivery branches for Bonecrusher `434`, Magick Counter `435`, Counter
Tackle `436`, Nature's Wrath `437`, reserved Reflect `440`, Auto-Potion `441`, and Counter `442`.
RVA `0x206421` is the **Proven** accepted native Reaction commit boundary. Counter `442` and
Auto-Potion `441` commit there with the reactor and incoming source. The actor target list at this
early boundary is not authoritative: it can be empty or retain the preceding target until the native
delivery branch materializes the final source target. Each visible Reaction produces one pass-2
commit; ordinary queue traffic remains on pass 1.

State `0x2C` at RVA `0x212C2E` is a **Proven per-execution effect boundary**, not a one-row-per-
Reaction cadence boundary. Native Counter preserves the presentation id `actor+0x18C = 442`, but
its executable id is Basic Attack `actor+0x142 = 0`; a Dual Wield Counter produces two state-`0x2C`
rows for the same accepted commit and final source target. The pass-2 commit owns Reaction
cardinality. State `0x2C` owns delivered native transaction/strike cardinality and requires an
idempotence token before any once-per-Reaction persistent mutation.

The pass-2 helper at RVA `0x283280` writes a typed order at reactor `unit+0x1A0` and derives target
coordinates from the unit selected by global RVA `0x186AFF4` (**Strong**). Counter `442` and
Bonecrusher `434` call it with order type `1`, payload `0`, and source-target validation; this is the
native basic-attack counter shape. Counter Tackle `436` uses type `0x0B`, payload `0x93`; Nature's
Wrath `437` uses type `0x11` with a tile-derived payload; Magick Counter `435` uses type `0x0B` with
the reactor's staged `unit+0x1E6` payload. The different helper inputs are why a carrier id is part of
the delivery contract rather than a cosmetic label.

Magick Counter's `unit+0x1E6` payload is the exact incoming ability id. `computeActionResult` copies
`orderRecord+2` into word RVA `0x7B0778`; the reaction dispatcher copies that word to
`unit+0x1E6`, and pass 2 emits it as the type-`0x0B` order payload. Native Magick Counter is
therefore a spell-copy delivery, not a generic magical basic attack.

Counter Tackle `436` is selected from `unit+0x95 & 0x02`. Its explicit pass-2 branch emits a
source-validated typed order `(type=0x0B, payload=147)`, where ability `147` is Rush. Rush retains
formula `0x37`, whose native contract is `random(1..Y) * PA` plus knockback. The active neuter data
inherits formula `0x37` and changes only X/Y, so it preserves the displacement carrier while DCL
damage ownership can replace the magnitude. Counter Tackle needs trigger/cadence filtering, not a
new shove effect.

Nature's Wrath computes `level*256 + y*mapWidth + x` from the **reactor's own tile**, reads terrain
type `tile[0] & 0x3F`, and maps it through the 64-byte table at RVA `0x67F750`. Every table entry is
an ordinary Geomancy payload `126..137`. The reaction therefore inherits the payload's normal
ability-data status rider; the selector does not create a special rider-free Geomancy action.

Vigilance `426` is selected by `unit+0x94 & 0x08`. The dispatcher stages the exact id at
`result+0x10` and the incoming action id at `result+0x28` inside a stack-copy simulation, then
restores the live battle globals and returns only whether a reaction was found (**Strong**). No
evade input, percentage, Dodge value, or status byte is modified in this real-code window; the
native effect is downstream/VM-owned. The matching vanilla PSX/PSP effect slot is Caution/Vigilance,
whose PSX effect applies Defending (**Strong continuity**, not IVC-live proof). Native Defending is
not equivalent to a transient DCL Dodge modifier for the current attack.

Blank reaction id `443` has generic-selector flag byte `0x06`, so the pass-2 generic route accepts it,
but the dispatcher does not test its corresponding `unit+0x96 & 0x04` reaction-set bit. It is a valid
carrier only when a custom producer stages `unit+0x1CE = 443`. The generic route targets the reactor's
own tile. RVA `0x2063BD` is the accepted-only boundary after the selector returns and before the actor
constructor at `0x2063CA`. `eax` is the reactor index, `word[rbp-0x2E]` is the exact Reaction id, and
the carrier-specific 20-byte order is complete at reactor `unit+0x1A0`. RVA `0x186AFF4` still owns the
incoming source index. Source retargeting at this boundary writes order target index `+0x0B` and the
source x/layer/y coordinates at `+0x0C/+0x0E/+0x10`; the later actor target list is not authoritative
at commit (**Proven live**).

The selected unit-table index in `eax`/order `+0x00` is a different namespace from the later actor's
`reactorIdx`/`actorIdx`. A live Counter materialized with selected/caster index `3` and then
committed/delivered through actor index `1`. Correlation across this boundary uses Reaction id,
incoming source, temporal order, and the selected unit/order pointer; it never assumes that the two
index values are equal.

The runtime binds a fail-closed accepted-order controller to this boundary. It matches one exact
Reaction id, optionally checks the native order type/payload, and can replace executable
`+0x01/+0x02` and/or copy the complete incoming-source target tuple. Live writes require exact
native-order guards and a bounded write count; invalid indices, mismatched orders, and exhausted
caps preserve the native order (**Implemented offline, live-gated**).

`OverrideAbilityActionData` contains keys `0..367`, so reaction id `443` cannot receive an ordinary
formula/status rider through that NXD surface. The dispatcher never evaluates the reaction-set bit
corresponding to `443`; it is therefore a useful blank-carrier probe for code-mod trigger production.
The generic synthetic-Reaction owner checks an exact equipped carrier at the successful incoming-result
commit instead of waiting for a native callback that cannot occur. It admits only a valid non-self
source, a surviving defender, and committed hit/action identity; it evaluates the configured taxonomy
rule and reserves a per-defender pass-2 producer request. Accepted-order rewriting owns any action or
target transformation; the producer contains no ability-specific effect (**Strong/offline-tested,
live-gated**).

Auto-Potion `441` has a fixed three-word eligible-item table at RVA `0x7154B8`: Potion `240`,
Hi-Potion `241`, and X-Potion `242` (**Proven-data/Strong-code**). The selector scans in that order
and chooses the first item with a nonzero inventory byte. It stores the selected item at
`unit+0x1A8`, writes order type `6`, and keeps Reaction id `441` at `unit+0x1A2`. No Elixir,
Phoenix Down, Remedy, or other item id belongs to this selector. The `unit+0x1EE & 0x30` special
path selects Potion `240` without scanning inventory.

The real-code item executor has a **Strong** inventory-consumption candidate at RVAs
`0x281692..0x2816B4`: it indexes the inventory byte by the selected item id and contains a guarded
one-unit decrement/write. The Auto-Potion order reaches VM-entry helper `0x281434`, so the exact
VM-to-decrement edge remains a live gate. Item selection and item consumption are distinct from
post-damage/survivor timing, once-per-cycle cadence, and any Item Lore multiplier policy.

Mana Shield `445` has a **Strong** native redirection contract. The dispatcher reads the reaction
unit through `qword[0x14186AF68]` and its target-local result record through
`qword[0x14186AF70] = unit+0x1BE`. When reaction bit `unit+0x96 & 0x01` is set, staged HP debit
`unit+0x1C4` is nonzero, and current MP `unit+0x34` is merely nonzero, RVAs
`0x30B9A9..0x30B9E9` add the complete HP debit to staged MP debit `unit+0x1C8` (capped at `999`),
select MP result flag `0x20`, clear the HP debit, and stage exact reaction id `445` at
`unit+0x1CE`. The later apply path clamps MP to `0..MaxMP` and never restores excess debit to HP.
Consequently one remaining MP point can natively prevent an entire HP hit. A proportional or
minimum-cost DCL Mana Shield must replace this policy rather than treating the native redirect as
the final balance rule.

Earplugs `448` has a **Strong** real-code trigger at RVAs `0x30B774..0x30B7E9`. It compares the
current formula id at byte RVA `0x7B0788` with `0x2A`, tests reaction bit
`unit+0x97 & 0x20`, and stages exact id `448` at `unit+0x1CE` with delivery kind `0x0B`.
Formula `0x2A` is the Speechcraft/morale family `116..125`. Bardsong `86..92` uses formula
`0x1C` and Dance `93..99` uses `0x1D`; those families do not traverse this real-code branch, though
a separate VM-internal Earplugs check remains possible.

Regenerate `428` has a **Strong** real-code staging trigger. Reaction bit
`unit+0x94 & 0x02` plus HP-damage result bit `unit+0x1E5 & 0x80` stages exact id `428` at
`unit+0x1CE` and copies staged HP debit `unit+0x1C4` to reaction payload `unit+0x1E6`. The branch
has no explicit current-HP/survivor test; lethal rejection and the actual Regen status delivery are
owned by surrounding or VM-internal processing.

Critical: Recover HP `431` has a **Strong** post-hit real-code contract. Reaction bit
`unit+0x95 & 0x40` selects the exact id through the native Brave gate. Its effect computes
`unit+0x32 MaxHP - unit+0x30 currentHP`, writes the entire missing amount as HP credit, and marks
result flag `0x40`. The native effect is therefore a full heal, not a minor recovery. A bounded
Squire Grit heal may retain carrier `431`, but code must replace the credited amount rather than
relying on the native effect unchanged.

The pass-2 source-exclusion index is global RVA `0x186AFF4`; the Counter result path writes it from
the current action context. Staging a carrier id at `unit+0x1CE` is a trigger-producer candidate,
not an established control: the delivery helpers also consume current action globals and unit
fields. A bounded observe-only capture across all three commit sites must establish runtime fire
count, pass ownership, and ordering before a boundary owns cadence mutation or reaction synthesis.

The queue store and battle dispatcher share state global RVA `0xC6B1CC` (**Strong**). The state
`0x29` handler advances explicitly to `0x2A`; the dispatcher then exposes adjacent `0x2B`, `0x2C`,
and `0x2D` handlers, with an explicit `0x2C -> 0x2D` transition. The transitions into `0x2B` and
`0x2C` are VM-owned. State `0x2A` resolves the current actor and reads presentation id `actor+0x18C`.
The executable action is already carrier-specific in the accepted selector order at `unit+0x1A0`:
Counter stores type `1`/payload `0`, Magick Counter stores type `0x0B`/the incoming ability, Counter
Tackle stores type `0x0B`/Rush `147`, and Auto-Potion stores type `6` plus its selected item.

RVA `0x212C2E` is the first instruction after state `0x2C` resolves the executed actor into `rax`.
It is downstream of the two state-`0x2B` VM execution workers and upstream of the explicit
`0x2C -> 0x2D` transition and real-code cleanup. The actor's presentation id `+0x18C`, executable id
`+0x142`, and final target count/list `+0x1A9/+0x1AA` are live there. A Counter commit retains
presentation id `442`, resolves executable Basic Attack `0`, and a Ninja Dual Wield response emits
two state-`0x2C` rows for one accepted commit (**Proven live**). This boundary owns delivered
transaction/strike auditing; once-per-Reaction cadence deduplicates against the pass-2 commit.

Reaction chance has two native delivery paths. Four real-code gates at RVAs `0x30BDEE`, `0x30BE44`,
`0x30BE9A`, and `0x30BEDA` call the native roll with defender Brave `unit+0x2B`. The enclosing gate
retains the exact Reaction ability id in `r11d` for the first/fourth sites and `ebx` for the
second/third sites. A hook can therefore replace the chance per exact Reaction id without replacing
the native effect or RNG.

VM-internal avoidance reactions such as Shirahadori do not traverse those four gates. The proven
input lever is still defender Brave `unit+0x2B`. Reaction staging writes the exact evaluated id to
`orderRecord+2` and mirrors it into `word[0x14186AFF0]`; native reaction formula dispatch reads that
global. Because `computeActionResult` receives the same order-record pointer, the DCL can classify the
actual VM evaluation id whether the reaction is equipped, innate, or derived, substitute an authored
chance only during that calculation, and restore the real Brave at its guarded sole epilogue.

The current executable has one linearly decoded return at RVA `0x309FB0`; the restore hook begins at
RVA `0x309FA1`. This VM-scoped delivery is **Strong** and requires a live vertical slice.

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

5. **Do not use CT for mod ownership.**
   Use native-frame register/stack/actor context, pending context, selector context, and pre-clamp
   target context instead.

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
- `[ACTOR-PROBE]`: per-unit `0x40..0x52` window snapshot at a damage event (legacy CT comparison).
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
