# Code-Mod Runtime: Config & Formula DSL

> Scope: **04-engine-memory-model.md** owns engine/memory facts (unit-struct offsets, the stable
> hook anchor, death/clamp/KO engine model, native-frame action context, equipment offsets).
> **This file (06)** owns the code-mod's runtime-settings configuration language and the C#
> formula DSL: operators, functions, variables, and every rule schema.
>
> Related: `05-reverse-engineering.md` (anchors / Denuvo), `03-battle-data-map.md` (editable
> surfaces / enums).

The custom battle math lives in a Reloaded-II C# runtime. Rather than replacing the
Denuvo-virtualized vanilla damage routine, the code mod **owns the final battle result** and
drives all custom mechanics through a hot-reloadable JSON settings file
(`battle-runtime-settings.json`) plus an embedded C# expression engine.

## Strict profile composition

Mechanism profiles are combined with `tools/compose_runtime_settings.py` and a JSON manifest.
Composition is structural and fail-closed:

- object properties are merged recursively;
- named rule/variable/slot arrays are merged by their `Name` property while preserving source
  order;
- identical shared values are accepted;
- divergent scalars, unnamed arrays, or named records are conflicts and stop generation;
- every conflict requires an explicit top-level entry in the manifest's `resolutions` object;
- intentional non-conflict changes belong in the manifest's `patch` object;
- `--check-only` fails when the generated profile is missing or stale.

The generated settings file is therefore reproducible from its source profiles and cannot inherit
silent last-writer-wins behavior. A composed mechanism scaffold is not a final balance profile:
design-open rules and metadata remain excluded until their owning DCL specifications are ratified.

## Runtime pipeline

The code mod owns the outcome through five timeless stages:

1. **Data-layer neuter.** The data mod forces damaging actions to a safe, nonlethal vanilla
   placeholder result, so vanilla cannot kill, crystallize, or trigger side effects before the
   runtime corrects HP.
2. **Observe.** A read-only sampler over stable, non-virtualized unit touchpoints detects HP/MP
   deltas (the unit-struct offsets and hook anchor are defined in 04).
3. **Resolve context.** Attacker, target, action family, and equipment are resolved as far as
   available context allows.
4. **Compute.** A pure, deterministic C# formula (the DSL below) produces a signed final result.
5. **Reconcile.** The runtime applies the computed result through the engine's own HP/KO
   lifecycle — rewriting the staged debit pre-clamp where context resolves in time, or correcting
   HP/MP post-apply as a fallback. The exact lethal-delivery path and the engine-owned
   death/KO/clamp model are owned by `04` §6 (`MinHpFloor=1` is the post-damage fallback).

Because the code mod owns the final HP through the engine's own apply step, the damage preview and
the resolved hit can diverge. Forecast/preview text can remain vanilla or data-layer placeholder
unless a preview-specific hook rewrites it. When the runtime rewrites the native pre-clamp staged
debit, the floating damage number and final HP follow the rewritten staged value; when the runtime
falls back to late HP correction, the floating number can remain vanilla/placeholder while final HP
is corrected afterward. This preview/resolution split is durable behavior of the architecture.

## Data layer: safe vanilla placeholder

The data mod sets damaging abilities to a controlled vanilla result:

- low nonlethal damage for damage actions;
- low nonlethal healing for healing actions;
- predictable action flags / range / area / element / status;
- vanilla formulas kept close enough that AI/preview are not absurd.

Rationale: if vanilla can kill, crystallize, trigger reaction chains, or apply the wrong side
effect before the runtime corrects HP, the post-damage engine fights state that already changed.
A placeholder result gives a clean event signal and leaves the real outcome to the runtime.

The exe reads `OverrideAbilityActionData` `Formula`/`X`/`Y` for damage magnitude (not only
CT/MP), so the data lever for the placeholder exists end to end. Weapon-power attacks are
neutered through `ItemWeaponData` `Power=1`; offensive abilities are neutered by forcing `X=1,
Y=1` on damaging rows; secondary-power actions (Aim/Charge) are neutered through their own
`Power=1`. Heals (target-allies) are left untouched.

Death is engine-owned: a direct byte write (HP=0, or HP=0 plus a KO flag bit) does not cause a real
KO and creates a zombie-like state, so the code mod never writes 0. How lethal damage is delivered
through vanilla's own HP/KO lifecycle — the pre-clamp staged-debit rewrite (primary) and the
`MinHpFloor=1` post-damage write (fallback) — is documented in `04` §6.

## Runtime unit registry

The stable battle-base hook acts as a read-only hot-path sampler and unit-pointer discovery
source (anchor and `BattleUnit` field offsets are documented in 04). A managed polling thread
maintains, per unit pointer: last full snapshot, last HP/MP, team/faction, and a first-seen hex
dump for unknown-field mapping. Once a pointer has been seen by the hook, the poller reads that
unit directly from process memory each tick, so HP/MP baselines stay roughly one poll interval
old.

Units are keyed by **unit pointer, not character id** (duplicate `id` enemies share a char id but
are distinct units).

Runtime knobs:

- `UnitPollIntervalMs` defaults to `25`. Lower it for tighter timing; keep it positive.
- `MaxTrackedBattleUnits` defaults to `64`. New unit pointers beyond the cap are skipped with a
  `[UNIT-SKIP]` log instead of growing the registry without bound.

This registry is the foundation for target detection, DR lookup, stat reads, and HP/MP correction.

## Event detector

The base detector compares current vs. previous HP for the same registered unit pointer and
raises a damage event on a decrease. It is then hardened to:

- separate damage from poison/regen/status ticks by timing and active-action context;
- support healing (`currentHP > previousHP`);
- support MP deltas;
- group several deltas in a short window for multi-hit actions;
- ignore deltas caused by the runtime's own reconciliation writes.

## Context resolver

Context is a set of dimensions, not a single piece:

- **target unit / target stats**: the HP/MP-delta unit pointer and its struct fields.
- **attacker unit / action id**: the primary architecture is the native pre-clamp actor context
  described in `04-engine-memory-model.md`: the pre-clamp stack exposes actor structs, each actor's
  `+0x148` links to its unit, and the caster actor's `+0x142` carries the resolving action id. The
  current code has both an observe-only resolver for this path (`PreClampResolveActorContext`,
  logging `[PRECLAMP-ACTOR-CTX]`) and a validation-grade managed pre-clamp callback proof that uses
  the same actor context to compute caster/target-stat damage in the native HP-apply frame for
  basic, instant named, and delayed named damage. DCL
  attacker/action ownership must come from native-frame context (registers, stack roots, actor
  structs), pending context, or selector context. CT-derived source fields are legacy/debug trace
  fields only and are not accepted by the mod as damage authorship.
  Some historical proof profiles may still log or expose CT-derived provenance fields such as
  `a.ct`, `a.sourceCt`, and `a.sourceCounter`; shipping DCL formulas must not depend on them.
- **action family**: bridged through `ActionSignalRules`, which decode controlled vanilla deltas
  into `action.*` variables (`action.swing`, `action.thrust`, `action.cut`, `action.spell`, etc.).
- **equipment ids**: read from fixed offsets or discovered through catalog scan slots, joined
  against the static item catalog.

`a.sourceRecent` (recent-unit heuristic) remains a legacy/debug field and is not an accepted DCL
attacker model. `InferAttackerFromRecentUnits` (off by default) gates whether the heuristic is passed
into formulas for old proof profiles; shipping DCL profiles should leave it off.

### MemoryTableProbes

`MemoryTableProbes` are an opt-in, disabled-by-default RIP-relative memory-table probe framework
for hunting a persistent roster/unit table from normal memory. Patterns come from JSON; the
runtime checks pages with `VirtualQuery` before reading and can log named row fields.

Shape:

```json
{
  "MemoryTableProbes": [
    {
      "Name": "RosterUnitTableCandidate",
      "Enabled": false,
      "Pattern": "PUT INDEPENDENTLY DISCOVERED AOB HERE",
      "RipRelativeOffset": 3,
      "InstructionLength": 7,
      "TargetAddend": 0,
      "DereferenceCount": 0,
      "Count": 55,
      "Stride": 600,
      "LogRows": true,
      "LogEmptyRows": false,
      "MaxRowsToLog": 16,
      "MinPresenceScore": 1,
      "Fields": [
        { "Name": "UnitIndex", "Offset": 1, "Width": "Byte", "EmptyValue": 255 },
        { "Name": "Job", "Offset": 2, "Width": "Byte" },
        { "Name": "Index2", "Offset": 44, "Width": "Byte" }
      ]
    }
  ]
}
```

Supported field widths:

```text
Byte / UInt8
SByte / Int8
Word / UInt16
Short / Int16
DWord / UInt32
Int / Int32
QWord / UInt64
Long / Int64
```

Each enabled probe registers a startup scan; the table resolves as
`table = instruction + InstructionLength + disp32 + TargetAddend`, with optional
`DereferenceCount`. Rows log as `[MEMTABLE-ROW ...]` only when `LogRows=true` and their field
presence score meets `MinPresenceScore`. This is evidence collection only; formula variables are
not populated from these rows.

## Formula engine

When a damage/healing/MP event has enough context, the runtime computes its own result through a
pure, deterministic C# expression engine. The engine is fed snapshots and static table data and
returns a value; the runtime layer handles all memory reads/writes.

A global formula example:

```json
{
  "RewriteObservedDamage": true,
  "FinalDamageFormula": "clamp(vanillaDamage + t.pa - equipmentDr, 0, t.hp)",
  "FormulaVariables": {
    "minChip": 1
  },
  "FormulaDerivedVariables": [
    { "Name": "baseDamage", "Formula": "vanillaDamage + t.pa" },
    { "Name": "penetrating", "Formula": "max(0, baseDamage - equipmentDr)" }
  ],
  "FormulaTables": {
    "thrustDice": [0, 1, 1, 1, 1, 1, 1, 1],
    "thrustAdds": [0, -3, -2, -1, 0, 1, 2, 3],
    "swingDice":  [0, 1, 1, 1, 1, 1, 1, 2],
    "swingAdds":  [0, -2, -1, 0, 1, 2, 3, -1]
  },
  "FormulaMatrices": {
    "typeResponsePermille": [
      [650, 750, 950, 1000],
      [650, 1100, 950, 1000],
      [1150, 950, 1000, 1000],
      [800, 1100, 950, 1000]
    ]
  },
  "FormulaMaps": {
    "armorDrByItem": {
      "172": 3,
      "0xAE": 6
    }
  },
  "EquipmentSlots": [
    { "Name": "Body", "Offset": 112, "Width": "Byte" }
  ],
  "EquipmentDrRules": [
    { "Name": "Leather Armor DR 3", "Slot": "Body", "ItemId": 172, "DamageReduction": 3 }
  ],
  "ApplyEquipmentDr": false
}
```

### Supported expression features

```text
Operators:   + - * / %   > >= < <= == !=   && || !   parentheses
             && and || short-circuit: skipped branches are parsed but not evaluated
Functions:   min(...) max(...) clamp(value,min,max) abs(x) sign(x)
             if(condition, trueValue, falseValue)    # lazy: only the selected branch evaluates
             targetByte(offset) targetSByte(offset)
             targetWord(offset) targetShort(offset) targetDWord(offset)
             attackerByte(offset) attackerSByte(offset)
             attackerWord(offset) attackerShort(offset) attackerDWord(offset)
                                                     # offsets can be decimal or 0xNN
             hasBit(value,bitIndex)
             hasAnyBits(value,mask) hasAllBits(value,mask) noBits(value,mask)
             bitAnd(a,b,...) bitOr(a,b,...) bitXor(a,b,...)
             shl(value,bits) shr(value,bits)
             bitExtract(value,startBit,width)
             signedBitExtract(value,startBit,width)
             table(name,index)                       # fail if missing/out of range
             tableClamp(name,index)                  # clamp index to table bounds
             tableOr(name,index,fallback)            # fallback if missing/out of range
             matrix(name,row,column)                 # 2D lookup; fail if missing/out of range
             matrixClamp(name,row,column)            # clamp row/column to matrix bounds
             matrixOr(name,row,column,fallback)      # fallback if missing/out of range
             map(name,key)                           # sparse lookup; fail if missing key
             mapOr(name,key,fallback)                # fallback if missing map/key
             diceMin(dice,sides,adds)
             diceMax(dice,sides,adds)
             diceAvg(dice,sides,adds)                # deterministic integer average, floor
             diceAvgRound(dice,sides,adds)           # deterministic integer average, rounded
             diceAvgCeil(dice,sides,adds)            # deterministic integer average, ceiling
             diceRoll(dice,sides,adds)               # event-seeded deterministic roll
             diceRollAt(index,dice,sides,adds)        # event-seeded roll independent of call order
             rand(min,max)                           # inclusive event-seeded integer
             randAt(index,min,max)                    # inclusive event-seeded integer by stream
             floorDiv(value,divisor)
             ceilDiv(value,divisor)
             roundDiv(value,divisor)
             mulDiv(value,numerator,denominator)      # floor(value * numerator / denominator)
             mulDivCeil(value,numerator,denominator)
             mulDivRound(value,numerator,denominator)
Variables:   vanillaDamage vanillaDamageAbs vanillaHealing
             observedHpDelta observedHpLoss observedHpGain
             previousHp currentHp equipmentDr
             vanillaMpChange vanillaMpDelta vanillaMpChangeAbs
             vanillaMpLoss vanillaMpGain
             observedMpDelta observedMpLoss observedMpGain
             previousMp currentMp
             damageResponsePermille responsePermille typeResponsePermille
             combinedResponsePermille boundedResponsePermille
             damageResponse.* / response.*:
               rawPermille permille ruleCount clamped
             typeResponse.* combinedResponse.* boundedResponse.*:
               rawPermille or permille aliases for the same response layer
             event.index event.seed
             event.isDamage event.isHealing event.isHpLoss event.isHpGain
             event.isMpLoss event.isMpGain event.isMpChange
             target.* or t.*:
               charId level hp maxHp mp maxMp team isFoe isAlly pa ma speed move jump brave faith
               job jobId jobIndex jobJp jobTotalJp jobLevel
               innateAbilityId1 innateAbilityId2 innateAbilityId3 innateAbilityId4
               reactionAbilityId supportAbilityId movementAbilityId
               zodiac genderFlags isMale isFemale isMonster maxBrave maxFaith
               rawPa rawMa rawSpeed                       # base stats before equipment
               weaponAtk weaponAtkL weaponParry weaponParryL shieldPhysParry shieldMagParry physEva
               hpGrowth hpMult mpGrowth mpMult spdGrowth spdMult paGrowth paMult maGrowth maMult
             attacker.* or a.*:
               present inferred sourceRecent
               charId level hp maxHp mp maxMp team isFoe isAlly pa ma speed move jump brave faith
               job jobId jobIndex jobJp jobTotalJp jobLevel
               innateAbilityId1 innateAbilityId2 innateAbilityId3 innateAbilityId4
               reactionAbilityId supportAbilityId movementAbilityId
               zodiac genderFlags isMale isFemale isMonster maxBrave maxFaith
               rawPa rawMa rawSpeed weaponAtk weaponAtkL weaponParry weaponParryL
               shieldPhysParry shieldMagParry physEva
               hpGrowth hpMult mpGrowth mpMult spdGrowth spdMult paGrowth paMult maGrowth maMult
             action.* or act.*:
               present sourceVanillaDamage signal vanillaDamage vanillaDamageAbs vanillaHealing
               vanillaMpChange vanillaMpLoss vanillaMpGain
               isDamage isHealing isMpLoss isMpGain sourceMpChange
               plus every configured ActionSignalRules.Variables and VariableFormulas key,
               defaulting to 0
             slot.<slotName> and slot_<slotName> for target item ids (backwards-compatible)
             targetSlot.<slotName> / tslot.<slotName> for target item ids
             attackerSlot.<slotName> / aslot.<slotName> for attacker item ids
             <slotExpr>.offset scanMatches ambiguous widthByte widthWord
             <slotExpr>.<itemField> when item_catalog.csv is loaded
             FormulaVariables entries, plus const.<name> aliases
             FormulaMaps entries, usable through map/mapOr with decimal or 0x hex JSON keys
             FormulaPreActionVariables entries, evaluated before ActionSignalRules and then
               available to DR/response/final formulas
             FormulaPreResponseVariables entries, evaluated before equipment DR, damage response,
               and final damage
             FormulaDerivedVariables entries, evaluated in order before final damage
Rule gates:  DamageRules.ConditionFormula, MpRules.ConditionFormula, EquipmentDrRules.ConditionFormula, and
             DamageResponseRules.ConditionFormula use this same expression context. A nonzero
             result means "match"; zero means "skip this rule".
Rewrite gate: RewriteConditionFormula also uses this context after derived variables are ready.
              A nonzero result means "rewrite HP"; zero means "leave vanilla HP alone".
MP rewrite gate: MpRewriteConditionFormula uses the MP event context after derived variables are
                 ready. A nonzero result means "rewrite MP"; zero preserves vanilla MP.
```

The unit prefixes also expose position/facing, raw status bytes, named effective/source/immunity/
master status bits, and elemental affinities. This includes predicates such as
`t.status.ko`, `a.status.invisible`, and `t.element.absorb.fire`. The exhaustive source-derived
catalog is generated by `tools/report_runtime_formula_context.py` into
`work/runtime_formula_context.md`; that report is the inventory to consult instead of maintaining a
second hand-written variable list here.

Raw reads are little-endian. `Byte`/`Word`/`DWord` read unsigned values, while `SByte` and
`Short` sign-extend memory fields that are stored as signed values. Attacker raw reads
intentionally fail the rewrite when attacker context is absent. `if(...)`, `&&`, and `||` are
lazy, so guards like `if(a.present, attackerByte(0x44), 0)`,
`a.present && attackerByte(0x44)`, and `!a.present || attackerByte(0x44)` can safely wrap raw
attacker reads until action context is mapped.

Bit helpers are intended for mapped status/element/equipment flag fields. `hasBit` accepts bit
indexes `0..62`. `bitExtract`/`signedBitExtract` accept widths `1..62` as long as the requested
range stays within bit `62`. Mask and bitfield helpers reject negative values so a bad formula
fails closed instead of interpreting signed integers as flag sets. Example once a status
byte/word offset is confirmed:

```json
{
  "FormulaPreActionVariables": [
    { "Name": "status.poison", "Formula": "hasBit(targetByte(0x90), 5)" },
    { "Name": "status.oil", "Formula": "hasAnyBits(targetWord(0x90), 0x0200)" },
    { "Name": "status.stance", "Formula": "bitExtract(targetDWord(0x98), 8, 4)" }
  ],
  "RewriteConditionFormula": "status.poison",
  "FinalDamageFormula": "vanillaDamage + status.oil * 10 + status.stance"
}
```

### Attacker context and legacy diagnostic settings

The primary RE model for attacker/action context is the pre-clamp actor array (`actor+0x148 -> unit`,
caster `actor+0x142 -> action id`), owned by `04-engine-memory-model.md`. The stable diagnostic
surface is exposed as an observe-only probe:

```json
{
  "PreClampResolveActorContext": true,
  "PreClampActorActionIdOffset": 322,
  "PreClampActorContextMaxLogs": 64
}
```

It emits `[PRECLAMP-ACTOR-CTX]` for head-to-head validation against pending and selector context.
The same context is also used by the validation-grade managed pre-clamp callback proof, which has
applied caster/target-stat damage in the native HP-apply frame for basic attack, instant named
ability, and delayed named ability rows. Shipping DSL integration must route formula evaluation
through this native-frame context rather than the legacy CT/recent-unit paths.
The CT and recent-damage knobs below are legacy diagnostics for comparing old resolver traces and
must not be required by shipping DCL profiles:

```json
{
  "ResolveAttackerByCt": false,
  "CtDropWindowMs": 4000,
  "ResolveCounterFromRecentDamage": false,
  "CounterEventWindowMs": 1500
}
```

Set these to `true` only in throwaway RE/comparison profiles. They are not part of the accepted DCL
runtime ownership path.

```json
{
  "LogAttackerCandidates": true,
  "InferAttackerFromRecentUnits": false,
  "RecentAttackerWindowMs": 1500,
  "PreferOpposingTeamAttacker": true,
  "MaxAttackerCandidatesToLog": 4
}
```

Formula examples:

```text
if(a.present, max(1, a.pa * 10 - t.faith), vanillaDamage)
if(a.sourceCounter, a.pa * 3, if(a.present, a.pa * 4, vanillaDamage))

# weapon attack scaled by base PA and the job's PA multiplier
max(1, mulDiv(a.weaponAtk + a.rawPa, a.paMult, 100) - equipmentDr)
# zodiac compatibility-style swing: same sign bonus, opposite sign penalty
mulDiv(a.pa * 5, if(a.zodiac == t.zodiac, 125, if((a.zodiac + 6) % 12 == t.zodiac, 75, 100)), 100)
# monster units hit harder; faith scales magic vs the target's faith
if(a.isMonster, a.pa * 2, mulDiv(a.ma * 5, t.faith, 100))
```

### Dice and ratio helpers

`diceRoll` and `rand` use `event.seed` plus a per-formula call counter, so the same event
evaluates to the same rolled result while separate events can vary. Prefer
`diceRollAt(index,dice,sides,adds)` or `randAt(index,min,max)` for important named rolls (base
weapon roll, crit roll, status rider roll); indexed streams stay stable if the formula is
refactored or another random helper is inserted earlier. Use `diceAvgRound` for stable
average-damage results, `diceAvg` when floor-average behavior is intentional, and `diceRoll` when
variance is desired.

### Derived, pre-action, and pre-response variables

- `FormulaPreActionVariables`: evaluated after unit slots are read but before `ActionSignalRules`
  choose action metadata. Use for reusable action-selection tags (`pre.weaponBlade`, `pre.unarmed`).
- `FormulaPreResponseVariables`: evaluated after slots/action context exist but before equipment
  DR, response rules, and final damage. Use to derive reusable tags (`armor.plate`, `armor.mail`)
  once instead of duplicating item filters in every response rule.
- `FormulaDerivedVariables`: an ordered list evaluated after target/attacker/action/slot variables
  and `equipmentDr` are known, before final damage. If any derived formula fails, the runtime
  skips the HP rewrite rather than applying a partial result.

Derived-variable example:

```json
{
  "RewriteObservedDamage": true,
  "FormulaDerivedVariables": [
    { "Name": "grossDamage", "Formula": "diceRollAt(1, 2, 6, 0)" },
    { "Name": "penetrating", "Formula": "max(0, grossDamage - equipmentDr)" },
    { "Name": "wound.num", "Formula": "if(action.cut, 3, if(action.impale, 2, 1))" },
    { "Name": "wound.den", "Formula": "if(action.cut, 2, 1)" },
    { "Name": "finalDamage", "Formula": "mulDiv(penetrating, wound.num, wound.den)" }
  ],
  "FinalDamageFormula": "finalDamage"
}
```

### Tables, maps, and matrices

- `FormulaTables`: dense integer lookup tables, callable via `table` / `tableClamp` / `tableOr`.
  Intended path for GURPS-like swing/thrust tables without hardcoding them in C#.
- `FormulaMaps`: sparse integer lookup maps keyed by ids (per-item DR, per-weapon damage class,
  special item tags). JSON keys can be decimal or `0x` hex. `map(name,key)` is strict and skips
  the HP rewrite if the map or key is absent; `mapOr` is the safe live-mapping form.
- `FormulaMatrices`: 2D lookup, callable via `matrix` / `matrixClamp` / `matrixOr`. Use when a
  rule is naturally two-dimensional (e.g. damage-type index x armor-class index).

Table names can be bare identifiers or quoted strings:

```text
tableClamp(swing, a.pa)
tableClamp("gurps.swing", a.pa)
```

Matrix and map examples:

```json
{
  "FormulaMatrices": {
    "response": [
      [650, 750, 950, 1000],
      [650, 1100, 950, 1000],
      [1150, 950, 1000, 1000],
      [800, 1100, 950, 1000]
    ]
  },
  "FormulaDerivedVariables": [
    { "Name": "responsePermille", "Formula": "matrixClamp(response, damageTypeIndex, armorClassIndex)" }
  ]
}
```

```json
{
  "FormulaMaps": {
    "armorDrByItem": { "172": 1, "174": 6 },
    "weaponModeByItem": { "19": 1, "0x2A": 2 }
  },
  "FormulaDerivedVariables": [
    { "Name": "armor.dr", "Formula": "mapOr(armorDrByItem, slot.body.itemId, 0)" },
    { "Name": "weapon.mode", "Formula": "mapOr(weaponModeByItem, aslot.weapon.itemId, 0)" }
  ]
}
```

### Item metadata variables

When `item_catalog.csv` is loaded, slot expressions expose joined static item metadata
(`slot.<name>.<itemField>`). The mod loads the catalog at startup from `ItemCatalogPath`.

Implemented item metadata variables:

```text
id requiredLevel additionalDataId equipBonusId
weaponPower weaponFormula weaponEvasion weaponRange weaponOptionsAbilityId
armorHpBonus armorMpBonus
shieldPhysicalEvasion shieldMagicalEvasion
accessoryPhysicalEvasion accessoryMagicalEvasion
bonusPa bonusMa bonusSpeed bonusMove bonusJump boostJp
isWeapon isArmor isShield isAccessory
category_<normalizedItemCategory>
type_<normalizedTypeFlag>
```

Every list column of `item_catalog.csv` is additionally exploded into per-name 0/1 variables so
formulas never parse strings (canonical sets extracted from the CSV, names normalized to
lowercase identifiers):

```text
atkflag_<flag>       # weapon_attack_flags: arc direct forcedtwohands lunging striking
                     #                      throwable twohands twoswords
element_<element>    # weapon_elements:     dark earth fire holy ice lightning water wind
innate_<status>      # bonus_innate_status    (28 statuses: berserk blind charm confuse disable
immune_<status>      # bonus_immune_status     doom faith float haste immobilize invisible ko
start_<status>       # bonus_starting_status   poison protect reflect regen reraise shell silence
                     #                         sleep slow stone stop toad traitor undead vampire)
absorb_<element>     # bonus_absorb_elements   (same 8-element set)
nullify_<element>    # bonus_nullify_elements
halve_<element>      # bonus_halve_elements
weak_<element>       # bonus_weak_elements
strong_<element>     # bonus_strong_elements
```

### Ability metadata variables (`AbilityCatalog`)

The mod also loads `work/wotl_ability_action_baseline.csv` (512 ability rows, ids 0–511, 1:1
PSX/WotL/IVC) from `AbilityCatalogPath` (default `wotl_ability_action_baseline.csv`, deployed
next to the DLL) into an `AbilityCatalog` mirroring the `ItemCatalog` pattern. The ability id
for a resolved action comes from the calc-entry order record (`05` — hook `0x3099AC`,
record bytes `[2..3]`). The catalog is loaded, hot-reloaded, exposed on `BattleFormulaEngine`, and
injected as `ability.*` into damage, preview, hit/defense, status, instant-KO, and reaction formula
contexts.

The same calc-entry record exposes its action-dependent payload at bytes `[8..9]`. For action type
`1`, native formula selection proves that this payload is the exact weapon item id used by the
calculation. Formula contexts expose:

```text
action.payload action.payloadId action.payloadKnown
action.weaponItemId action.weaponKnown
action.weaponMatchesRight action.weaponMatchesLeft
action.weaponSideKnown action.weaponSide    # 1 right, 2 left, 0 unknown/ambiguous
action.weapon.<itemField>                   # the full ItemCatalog variable surface
```

The side flags compare the payload with unit weapon slots `+0x20/+0x24`. If both slots contain the
same id, both match flags are true and `action.weaponSideKnown` is false; `action.weapon.*` remains
exact, so weapon-family and weapon-skill formulas are unaffected. The action cache and hit-decision
cache include the payload in their identity key.

`AbilityCatalogEntry.AddVariables(context, "ability")` exposes (prefix-dot style):

```text
ability.id ability.abilityId ability.formula ability.x ability.y
ability.range ability.aoe ability.vertical ability.ct ability.mp_cost ability.jp_cost
ability.inflict_status                     # raw inflict-status byte (hex column)
ability.<flag>                             # 0/1 for each FFTPatcher ability flag, snake_case:
    force_self_target blank7 weapon_range vertical_fixed vertical_tolerance weapon_strike
    auto target_self hit_enemies hit_allies top_down_target follow_target random_fire
    linear_attack three_directions hit_caster reflectable arithmetickable silenceable
    mimicable normal_attack persevere show_quote animate_on_miss counter_flood counter_magic
    direct shirahadori requires_sword requires_materia_blade evadeable used_by_enemies
ability.element_<element>                  # dark earth fire holy ice lightning water wind
ability.inflict_<status>                   # 30 statuses: berserk bloodsuck charm confusion
                                           # crystal darkness dead deathsentence dontact dontmove
                                           # faith float frog haste innocent invite oil petrify
                                           # poison protect reflect regen reraise shell silence
                                           # sleep slow stop transparent undead
ability.inflict_mode_all / _random / _separate / _cancel   # 0/1 (AllOrNothing -> all)
```

`DclAbilityMetadataPath` optionally points to a separate authored CSV overlay. The vanilla baseline
remains unchanged. The overlay requires these columns:

```text
ability_id,approved,action_kind,damage_type,avoidance_policy,status_category,side_effect_policy,power,strike_count
```

Only rows with `approved=1` (also `true` or `yes`) are merged. Every approved row must use a known
enum value for all five classifications; duplicate ids, unknown values, invalid numeric fields, or
missing schema columns disable the entire overlay while leaving the 512-row vanilla catalog
available. `power` is non-negative. `strike_count` is optional for compatibility and defaults to
`0`; it is limited to `0..99`. The `managed_multistrike` and
`managed_multistrike_status_rider` side-effect policies require `strike_count` in `2..99`, and every
other side-effect policy requires `strike_count=0`. `native_multistrike` and
`native_multistrike_status_rider` explicitly preserve engine-owned repeated result events and never
arm managed aggregation. The `_status_rider` variants retain managed status ownership without
changing who owns repetition. The generated
authoring template always includes the field. An unapproved row has no runtime effect. The default
path is empty, so the overlay is opt-in.

Approved rows add the following formula variables. Every variable also exists with value `0` when
the overlay is disabled or the current ability has no approved row:

```text
ability.dcl.approved
ability.dcl.power
ability.dcl.strike_count
ability.dcl.action_kind_<value>
ability.dcl.damage_type_<value>
ability.dcl.avoidance_<value>
ability.dcl.status_category_<value>
ability.dcl.side_effect_<value>
```

The enum vocabulary covers the DCL physical/magic/hybrid/healing/status/resource action families;
physical weapon types and elemental/spiritual/untyped magic; physical contest, per-target or
per-strike Magic Evade, status-contest chains, auto-hit, and native avoidance; mental, inverted
mental, physical, magical, lifecycle, and mixed status categories; and the managed/native
transaction families used by status riders, multi-strike, equipment, inventory, multi-unit resource
transfer, traits, and preserved native specials.

`ability.dcl.strike_count` is the authored DCL count, not a claim about the native formula's commit
cardinality. A managed multistrike uses it for forecast and execution; `0` means no authored count
and never arms the managed route. A native multistrike derives its count and index from the engine's
carrier instead of this field.

String-ish columns (names, formula_text, targeting notes) are deliberately not formula
variables; the entry keeps `Name` (IVC name, WotL fallback) for logging only.

Example once `Body` is a confirmed equipment offset:

```text
slot.body                 # item id
slot.body.armorHpBonus    # armor secondary HP bonus from item_catalog.csv
slot.body.category_armor  # 1 if ItemCategory is Armor, otherwise 0
aslot.weapon              # attacker weapon item id, when attacker context is supplied
aslot.weapon.weaponPower  # attacker weapon Power from item_catalog.csv
aslot.weapon.present      # 1 if attacker slot was read, 0 if attacker context is absent
```

### Resolved-context tracing

`LogResolvedRuntimeContext=true` emits `[RUNTIME ...]` lines for each rewritten event showing the
resolved attacker, action signal, target/attacker slots, equipment DR, percent/type response, and
final formula result. `FormulaTraceVariables` are diagnostic-only formula probes appended to the
trace as `vars=...`; a failing trace probe logs `ERR(...)` and does not cancel the HP rewrite.

```json
{
  "LogResolvedRuntimeContext": true,
  "FormulaTraceVariables": [
    { "Name": "gross", "Formula": "basePressure" },
    { "Name": "penetrating", "Formula": "max(0, basePressure - equipmentDr)" },
    { "Name": "result.final", "Formula": "result.finalDamage" }
  ]
}
```

## ActionSignalRules

`ActionSignalRules` are the sentinel-channel bridge: a controlled vanilla damage or MP delta
classifies an observed event into `action.*` variables before a true ability-id hook exists.
Rules match in order. Every variable declared in `Variables` or `VariableFormulas` is installed
with a default value of `0`, so `action.swing`, `action.thrust`, `action.power`, etc. are safe
even when no signal matched. These formulas run after configured `EquipmentSlots` /
`AttackerEquipmentSlots` are read, so they can use `slot.*`, `targetSlot.*`, `aslot.*`,
`attackerSlot.*` item metadata plus any `FormulaPreActionVariables` tags.

Supported match fields:

```text
HP events:  Faction, EventKind, Team, CharId, MinLevel, MaxLevel,
            VanillaDamage, MinVanillaDamage, MaxVanillaDamage, ConditionFormula
MP events:  VanillaMpChange, MinVanillaMpChange, MaxVanillaMpChange
```

Damage-filtered rules do not match MP events, and MP-filtered rules do not match HP events.
`EventKind` accepts `Any`, `HP`/`HpChange`, `Damage`/`HpLoss`, `Healing`/`HpGain`, `MP`/`MpChange`,
`Loss`/`MpLoss`, and `Gain`/`MpGain`. Effect fields are `Signal`, `Variables` (fixed integer
constants), and `VariableFormulas` (expression-backed values).

Constant-driven example:

```json
{
  "RewriteObservedDamage": true,
  "ActionSignalRules": [
    { "Name": "Sword swing placeholder", "VanillaDamage": 7, "Signal": 41, "Variables": { "swing": 1, "cut": 1 } },
    { "Name": "Spear thrust placeholder", "VanillaDamage": 8, "Signal": 42, "Variables": { "thrust": 1, "impale": 1 } }
  ],
  "FinalDamageFormula": "if(action.swing, tableClamp(swing, a.pa), tableClamp(thrust, a.pa)) - equipmentDr"
}
```

Formula-coded band example (derives action metadata from the observed delta):

```json
{
  "RewriteObservedDamage": true,
  "ActionSignalRules": [
    {
      "Name": "Heavy swing placeholder band",
      "ConditionFormula": "event.isDamage && vanillaDamageAbs >= 10 && vanillaDamageAbs <= 19",
      "Signal": 100,
      "Variables": { "swing": 1, "cut": 1 },
      "VariableFormulas": { "power": "vanillaDamageAbs - 10", "woundNum": "3", "woundDen": "2" }
    }
  ],
  "FinalDamageFormula": "mulDiv(max(0, tableClamp(swing, a.pa) + action.power - equipmentDr), action.woundNum, action.woundDen)"
}
```

Weapon-derived signal example (classifies from attacker weapon metadata):

```json
{
  "ActionSignalRules": [
    {
      "Name": "Sword swing from attacker weapon",
      "ConditionFormula": "a.present && aslot.weapon.category_sword",
      "Variables": { "swing": 1, "cut": 1 },
      "VariableFormulas": { "weaponPower": "aslot.weapon.weaponPower", "targetArmorBonus": "slot.body.armorHpBonus" }
    }
  ]
}
```

Healing sentinels use the same mechanism with signed HP events:

```json
{
  "RewriteObservedHealing": true,
  "ActionSignalRules": [
    {
      "ConditionFormula": "event.isHealing && vanillaHealing >= 10",
      "Variables": { "heal": 1 },
      "VariableFormulas": { "healPower": "vanillaHealing + t.ma" }
    }
  ],
  "FinalDamageFormula": "-action.healPower"
}
```

## DamageRules

`DamageRules` select per-event HP behavior. If one or more rules match, the first matching rule
takes precedence over the global `FlatDamageReduction` / `ProofFinalDamage` fallback.

Supported `DamageRules` match fields:

```text
Faction: Any | Ally | Foe
EventKind: Any | HP/HpChange | Damage/HpLoss | Healing/HpGain
Team
CharId
MinLevel
MaxLevel
ActionSignal
RequiredActionVariable
ConditionFormula
```

Supported `DamageRules` effect fields:

```text
FinalDamageFormula
FinalDamage
FlatDamageReduction
ScaleNumerator / ScaleDenominator
MinFinalDamage
MaxFinalDamage
```

`FinalDamageFormula` and `FinalDamage` are signed. Positive values are damage; negative values
are healing. Damage events clamp the result to `0..9999`; healing events clamp it to `-9999..0`.
`FlatDamageReduction` and equipment DR are only applied to damage events. Use `EventKind` so a
rule does not cross HP event types (a `Damage` rule will not match a healing event).

Example:

```json
{
  "RewriteObservedDamage": true,
  "DamageRules": [
    { "Name": "Foes have DR 10", "EventKind": "Damage", "Faction": "Foe", "FlatDamageReduction": 10, "MinFinalDamage": 0 },
    { "Name": "Swing table", "RequiredActionVariable": "swing", "FinalDamageFormula": "max(0, tableClamp(swing, a.pa) - equipmentDr)" }
  ]
}
```

The coarse event-level gate `RewriteConditionFormula` runs after derived variables are ready. Use
it when a settings file should only own specific classified events:

```json
{
  "RewriteObservedDamage": true,
  "RewriteConditionFormula": "event.isDamage && action.present && a.present",
  "FinalDamageFormula": "finalDamage"
}
```

If the gate evaluates to zero, the result is an intentional no-rewrite with reason
`RewriteConditionFormula=0`. If the gate formula itself fails, the runtime skips the rewrite and
reports the formula error.

## MpRules

`MpRules` select per-event MP behavior. If one or more match, the first matching rule takes
precedence over the global `FinalMpChangeFormula`; if none match,
`FinalMpChangeFormula`/`ProofFinalMpLoss`/`ProofFinalMpGain` are used.

Supported `MpRules` match fields:

```text
Faction: Any | Ally | Foe
EventKind: Any | Loss/MpLoss | Gain/MpGain
Team, CharId, MinLevel, MaxLevel
ActionSignal
RequiredActionVariable
ConditionFormula
```

Supported `MpRules` effect fields:

```text
FinalMpChangeFormula
FinalMpChange
ScaleNumerator / ScaleDenominator
MinFinalMpChange / MaxFinalMpChange
```

Example:

```json
{
  "RewriteObservedMpLoss": true,
  "RewriteObservedMpGain": true,
  "ActionSignalRules": [
    { "Name": "MP cost sentinel", "EventKind": "MpLoss", "VanillaMpChange": -8, "Variables": { "spell": 1 } }
  ],
  "MpRules": [
    { "Name": "Spell MP loss rule", "EventKind": "Loss", "RequiredActionVariable": "spell", "FinalMpChangeFormula": "-min(previousMp, vanillaMpLoss + t.ma)" },
    { "Name": "Small gain cap", "EventKind": "Gain", "MaxFinalMpChange": 5 }
  ],
  "FinalMpChangeFormula": "vanillaMpChange"
}
```

## EquipmentDrRules

`EquipmentDrRules` subtract flat damage reduction by matched equipment before HP is rewritten.
If `ApplyEquipmentDr=true`, the runtime subtracts `equipmentDr` after the formula result; if the
formula already subtracts `equipmentDr`, leave `ApplyEquipmentDr=false` to avoid double DR.

Supported `EquipmentDrRules` match fields:

```text
Slot
ItemId
MinItemId / MaxItemId
ItemCategory
TypeFlag
SecondaryKind
NameContains
MinArmorHpBonus / MaxArmorHpBonus
MinWeaponPower / MaxWeaponPower
ActionSignal
RequiredActionVariable
ConditionFormula
```

Supported DR effect fields:

```text
DamageReduction
DamageReductionFormula
```

`ConditionFormula` and `DamageReductionFormula` use the same expression language as
`FinalDamageFormula`. In addition to target/action variables, DR rules get `item.*` for the
currently matched item and `slotItemId`. If a DR condition/formula fails to parse or references an
unknown variable, the runtime skips the HP rewrite and logs `[REWRITE-SKIP]` rather than applying
a questionable result.

Catalog-backed and action-typed example:

```json
{
  "RewriteObservedDamage": true,
  "ItemCatalogPath": "item_catalog.csv",
  "ApplyEquipmentDr": true,
  "EquipmentSlots": [
    { "Name": "Body", "Offset": 112, "Width": "Byte" },
    { "Name": "Shield", "Offset": 110, "Width": "Byte" }
  ],
  "EquipmentDrRules": [
    { "Name": "Body armor DR from HP bonus", "Slot": "Body", "ItemCategory": "Armor", "DamageReductionFormula": "max(1, item.armorHpBonus / 20)" },
    { "Name": "Body armor cut DR", "Slot": "Body", "ItemCategory": "Armor", "RequiredActionVariable": "cut", "DamageReduction": 5 },
    { "Name": "Any shield DR", "Slot": "Shield", "SecondaryKind": "shield", "DamageReduction": 2 }
  ]
}
```

`Offset` is decimal in JSON (`112` is `0x70`); these are shapes, not confirmed offsets (equipment
offsets are mapped in 04). The DR table is the mod's own data, keyed by item id / item category /
equip bonus id, so armor reduces incoming damage without abusing MaxHP. The subtractive pipeline:

```text
base dice damage + weapon bonus -> gross damage
gross damage - equipmentDr      -> penetrating damage
penetrating damage * wound mod  -> final damage
```

## DamageResponseRules (percent/type model)

`DamageResponseRules` implement the coarse percent/type armor model: each matching rule returns a
nonnegative permille multiplier.

```text
1000 = neutral x1.00
 650 = resist x0.65
1150 = vulnerable x1.15
```

The runtime multiplies every matching rule into `combinedResponse.permille`, clamps it into
`boundedResponse.permille` (via `MinDamageResponsePermille` / `MaxDamageResponsePermille`), then
exposes both to formulas. If `ApplyDamageResponseRules=true`, the bounded response is applied
after the formula result and optional equipment DR. `DamageResponseChipFloor` can preserve a
visible minimum for positive damage that would floor to zero.

Response rules can be global (no `Slot`/item filters, fire once when action/condition matches) or
slot/item-backed. Rule formulas get the same target/action/slot/item variables as the main
formula, plus `item.*` for the currently matched item. Use `FormulaPreResponseVariables` to derive
shared classification tags (`armor.plate`, `armor.mail`, `armor.light`) once and reuse them.

Supported `DamageResponseRules` match fields:

```text
Slot
ItemId
MinItemId / MaxItemId
ItemCategory
TypeFlag
SecondaryKind
NameContains
MinArmorHpBonus / MaxArmorHpBonus
MinWeaponPower / MaxWeaponPower
ActionSignal
RequiredActionVariable
ConditionFormula
```

Supported response effect fields:

```text
MultiplierPermille
MultiplierNumerator / MultiplierDenominator
MultiplierFormula
```

Use `MultiplierPermille` for exact policy constants like `650` or `1150`. Use `MultiplierFormula`
when the response must depend on item metadata, action variables, status probes, or
`FormulaPreResponseVariables`.

Armor-response shape:

```json
{
  "RewriteObservedDamage": true,
  "FinalDamageFormula": "basePressure",
  "FormulaVariables": { "basePressure": 100 },
  "ApplyDamageResponseRules": true,
  "MinDamageResponsePermille": 250,
  "MaxDamageResponsePermille": 2500,
  "DamageResponseChipFloor": 1,
  "ActionSignalRules": [
    { "VanillaDamage": 7, "Variables": { "swing": 1 } },
    { "VanillaDamage": 8, "Variables": { "thrust": 1 } },
    { "VanillaDamage": 9, "Variables": { "crush": 1 } },
    { "VanillaDamage": 10, "Variables": { "missile": 1 } }
  ],
  "EquipmentSlots": [ { "Name": "Body", "Offset": 112, "Width": "Byte" } ],
  "FormulaPreResponseVariables": [
    { "Name": "armor.plate", "Formula": "slot.body.category_armor && slot.body.armorHpBonus >= 60" },
    { "Name": "armor.mail", "Formula": "slot.body.category_armor && slot.body.armorHpBonus >= 40 && slot.body.armorHpBonus < 60" },
    { "Name": "armor.light", "Formula": "slot.body.category_clothing || (slot.body.category_armor && slot.body.armorHpBonus < 40)" },
    { "Name": "armor.cloth", "Formula": "slot.body.category_robe" }
  ],
  "DamageResponseRules": [
    { "Name": "plate swing", "ConditionFormula": "armor.plate && action.swing", "MultiplierPermille": 650 },
    { "Name": "plate crush", "ConditionFormula": "armor.plate && action.crush", "MultiplierPermille": 1150 },
    { "Name": "mail thrust", "ConditionFormula": "armor.mail && action.thrust", "MultiplierPermille": 1100 },
    { "Name": "light swing", "ConditionFormula": "armor.light && action.swing", "MultiplierPermille": 950 }
  ]
}
```

Class boundaries above use catalog categories plus armor HP bonus as an offline approximation;
final class boundaries are a design decision.

## Equipment slots: fixed and catalog-scanned

Fixed offsets are preferred once confirmed (mapped in 04). Before that, slot probes can search the
raw snapshot for catalog-backed item ids. A slot with no fixed `Offset` searches a range for
exactly one item id matching its filters; ambiguous scans expose `slot.<name>.scanMatches` and do
not mark the slot present unless `AllowAmbiguousSearchMatch=true`.

```json
{
  "EquipmentSlots": [
    { "Name": "Body", "SearchStart": 68, "SearchEnd": 383, "SearchWidth": "Byte", "SecondaryKind": "armor", "TypeFlag": "Armor", "AllowAmbiguousSearchMatch": false }
  ],
  "AttackerEquipmentSlots": [
    { "Name": "Weapon", "SearchStart": 68, "SearchEnd": 383, "SearchWidth": "Byte", "SecondaryKind": "weapon", "TypeFlag": "Weapon", "AllowAmbiguousSearchMatch": false }
  ]
}
```

Supported scan filters mirror the item/rule filters:

```text
ItemId
MinItemId / MaxItemId
ItemCategory
TypeFlag
SecondaryKind
NameContains
MinArmorHpBonus / MaxArmorHpBonus
MinWeaponPower / MaxWeaponPower
AllowItemIdZero
AllowAmbiguousSearchMatch
```

If exactly one catalog match is found, the slot is present and formulas can read item metadata. If
several match and ambiguity is not allowed, the slot is not present; formulas can still read
`slot.body.scanMatches`, `slot.body.ambiguous`, and `slot.body.offset` to help mapping.
`AttackerEquipmentSlots` only become live-useful once action context supplies an attacker snapshot
(`a.present`); when attacker context is absent, configured attacker slots resolve to safe
zero/default variables instead of crashing the formula.

## Reconciler: signed HP/MP write semantics

After vanilla applies its placeholder, the reconciler computes the desired value and writes it.

HP:

```text
vanillaHP = target.HP
wantedHP = previousHP - finalDamage
write target.HP = Clamp(wantedHP, 0, target.MaxHP)
```

`finalDamage` is signed: positive values reduce HP, negative values restore HP. Damage rewrites
are gated by `RewriteObservedDamage` and clamp to nonnegative final damage; healing rewrites are
separately gated by `RewriteObservedHealing` and clamp to nonpositive final damage.

MP:

```text
vanillaMP = target.MP
wantedMP = previousMP + finalMpChange
write target.MP = Clamp(wantedMP, 0, target.MaxMP)
```

`finalMpChange` is signed: negative values spend/drain MP, positive values restore MP. MP loss is
gated by `RewriteObservedMpLoss`; MP gain by `RewriteObservedMpGain`. If `FinalMpChangeFormula` is
empty, `ProofFinalMpLoss` / `ProofFinalMpGain` provide fixed signed changes.

The write is done from managed code outside the asm hook and is guarded with:

- the destination `HP`/`MP` word is checked as writable through `VirtualQuery` before applying;
- the write uses `WriteProcessMemory` against the current process, so failures log as
  `[REWRITE-FAILED]` / `[MP-REWRITE-FAILED]` instead of relying on a raw pointer write;
- tracking HP/MP is advanced to the desired post-rewrite value after a successful write;
- `DryRunRewrites=true` evaluates formulas and logs `[REWRITE-DRY-RUN]` / `[MP-REWRITE-DRY-RUN]`
  but does not call `WriteProcessMemory`, does not arm the echo guard, and keeps HP/MP tracking on
  the observed live value;
- `ValueRewriteEchoGuard` suppresses an exact delayed echo of the runtime's own HP/MP write within
  `SuppressOwnRewriteEchoWindowMs` (default `1000`);
- clamps for `0..MaxHP` and `0..MaxMP`;
- per-event id/seed for deterministic formula evaluation.

Lethal delivery (pre-clamp staged-debit rewrite as primary, `MinHpFloor=1` post-damage write as
fallback) and the engine-owned KO/death/clamp lifecycle are owned by `04` §6.

### Hot reload

The code mod watches `battle-runtime-settings.json` and the configured `ItemCatalogPath`,
`AbilityCatalogPath`, and optional `DclAbilityMetadataPath` during the polling loop; changes are
picked up about once per second, so
formula constants, tables, DR rules, action signals, and catalog data can be iterated without
rebuilding the DLL. If a settings
edit is not valid JSON, the runtime logs `[SETTINGS-RELOAD-FAILED]` and keeps the last valid
settings. `UnitPollIntervalMs` and `MaxTrackedBattleUnits` are also runtime settings.

### Proof/fixed-value settings

For early validation, fixed-value paths bypass formulas:

```json
{ "RewriteObservedDamage": true, "ProofFinalDamage": 1, "FlatDamageReduction": 0, "AffectAllies": true, "AffectFoes": true }
```

```json
{ "RewriteObservedHealing": true, "ProofFinalHealing": 4, "AffectAllies": true, "AffectFoes": true }
```

If `FlatDamageReduction` is greater than zero it takes precedence
(`finalDamage = max(0, vanillaDamage - FlatDamageReduction)`); otherwise the proof uses
`finalDamage = ProofFinalDamage`. A matching `DamageRules` entry overrides both.

## Outcome control hooks (hit↔miss authority) — proven, validation-grade

Beyond the reconciler (which adjusts HP/MP *after* vanilla applies), two native hooks let the mod
author the hit-vs-evade OUTCOME itself — proven live (2026-06-26) to turn a guaranteed 100%-hit into a
0-damage "Miss" (`05-reverse-engineering.md` §4, Control recipe; proof
`work/battleprobe_log.hit-to-miss-v2-PASS.*.txt`). The render and the HP debit are independent paths,
so a downgraded hit needs BOTH hooks.

- **Pre-clamp staged-debit hook** (`0x30A5D7`) — forces/zeros the staged HP debit before vanilla
  applies it. Settings: `PreClampDamageRewriteEnabled`; `PreClampDamageRewriteForcedDebit` /
  `…ForcedCredit` (`0` = zero the damage, `-1` = leave); `PreClampDamageRewriteTargetCharId`
  (`-1` = any target); `PreClampDamageRewriteMinHp` / `…MaxHp` (HP-range guard, default `1`/`9999`);
  `PreClampDamageRewriteMaxWrites`; `PreClampDamageRewriteLogOnly`. `PreClampManagedCallbackEnabled`
  is an opt-in ABI bridge that calls a C# reverse-wrapper callback from this same native frame and
  lets the callback return the debit written to `[rbp+6]`; fixed-debit plus basic, instant named,
  and delayed named caster/target-stat formula proofs are proven. The current callback formula path is
  validation-grade and must be broadened into the full DSL/action-family integration before it is
  treated as the shipping interface.
- **Result/animation selector hook** (`0x205210`) — sets the evade-type render. Settings:
  `ResultSelectorControlEnabled`; `ResultSelectorControlForceEvadeType` (enum per 05 §4: `0x04`
  class-evade / `0x03` shield / `0x02` weapon / `0x01` cloak / `0x06` miss / `0x00` hit);
  `ResultSelectorControlForceResultCode` (`+0x1BE`: `0` = evade path); `ResultSelectorControlMatchEvadeType`
  (only act on a record currently showing this type; `0` = a natural hit); `ResultSelectorControlTargetCharId`;
  `ResultSelectorControlMaxWrites`; `ResultSelectorControlLogOnly`. The probe side
  (`ResultSelectorProbe*`, RVA `2118160`) logs every selector pass.

The shipping-oriented DCL miss path is keyed by the formula decision rather than the fixed proof
knobs above:

- `DclMissOutputControlEnabled` enables staged-debit ownership for cached DCL misses.
- `DclMissSelectorOutcomeEnabled` delivers a cached miss at selector `0x205210` by writing the
  configured `DclMissKindValue` (normally `0x06`) to `+0x1C0` and clearing `+0x1BE`.
- `DclMissSuppressReactionsEnabled` hooks all four Counter-class Brave-roll call sites
  (`0x30BDEE`, `0x30BE44`, `0x30BE9A`, `0x30BEDA`). A cached miss forces chance zero; hits and cache
  misses preserve the native chance.
- The selector and reaction features require `DclMissOutputControlEnabled`. The selective reaction
  gate conflicts with the global `ReactionChanceControlEnabled` owner of the same sites.
- All four reaction-site AOB guards are validated before any hook is installed. A mismatch disables
  the complete selective gate rather than leaving reaction-dependent partial coverage.

**Proven:** a basic attack with a cached miss zeroed a staged 532 debit, produced selector kind
`0x06` with `+0x1BE=0`, preserved the target's HP, and changed the defender's natural Counter chance
from 73 to 0. The action then completed without a Counter.

**Validation-grade caveat.** The current guards are a *global* `MaxWrites` budget plus
`TargetCharId` / `MatchEvadeType` filters — fine for single-shot proofs but scenario-fragile (an
earlier qualifying pass can spend the budget; this is exactly why the first hit→miss attempt failed
and the second, on a fresh first-action target, passed). The shipping interface will harden to
**per-action arming** on both hooks plus selector `+0x1BE!=0` gating (skip resting/teardown passes),
and the force decision will come from the DCL formula output rather than fixed JSON immediates. Until
then, treat these as proof knobs, not a stable API.

## Input-control of avoidance (hit / miss / block / parry) — ✅ proven, the cleaner primary

Beyond authoring the outcome at the hooks above, the mod can plant the **inputs** the engine reads and
let the native roll produce the outcome — **proven live 2026-06-27** (`05-reverse-engineering.md` §4,
Input-control; `work/input-control-evade-PROVEN.md`). Denuvo virtualizes code, not data, so the unit
struct the VM reads is normal writable memory: write the **defender's** evade bytes before the roll and
the VM honors them (Ramza forced to a 0%-hit preview and an evade, 0 damage, engine-rendered). This
needs no data-gutting and no result-forging — the engine does everything from our planted values.

- **Persistent evade write** (the unit poller). Settings: `EvadeOverrideEnabled`;
  `EvadeOverrideTargetCharId` (`-1` = all units); `EvadeOverride46/47/48/49/4A/4B/4C/4D/4E` (nine evade
  bytes, `0`–`100`, `-1` = leave; `48/4C/4D` are inferred magic-evade partners); `EvadeOverrideMaxLogs`;
  `EvadeOverrideSweepSlots` (when broadcasting, also sweep ±N×`0x200` of the tracked span so **untracked**
  units — e.g. the actual defender — are boosted too; `0` = tracked only).
- Byte → outcome (on the defender): `+0x4B`→class evade (`0x04` "Miss"); `+0x46`/`+0x47`→weapon parry
  (`0x02`); `+0x4A`/`+0x4E`→shield block (`0x03`); `+0x49`→cloak (`0x01`, inferred); the five **physical**
  bytes `+0x46/+0x47/+0x4A/+0x4B/+0x4E` **all = `0`** → guaranteed hit (neutralizes avoidance in memory).
  Evade applies front/side only.

**Status.** The current knob writes a *static* value to every (or one) unit — a proof lever, not the
shipping API. The shipping form will write **per-action** from the DCL formula: identify the defender
via the pending-action tracker, compute the (attacker, defender) hit result, and write that defender's
evade bytes just before the roll. Damage value continues to come from the pre-clamp.

## Input-control of status, reactions, and MP — ✅ proven (status + reactions live)

The same "plant the data the VM reads" mechanism extends past avoidance:

- **Status** (`StatusOverride*`). ✅ live-confirmed 2026-06-27 (`+0x1EF/+0x61 |= 0x10` made Ramza
  Undead). The effective byte `+0x61` is recomputed in real code (`0x30D42A`) as `(+0x1EF & 0xF2) | +0x57`.
  Settings: `StatusOverrideEnabled`; `StatusOverrideTargetCharId`; `StatusOverride1EF` (durable master,
  OR-mask), `StatusOverride61` (effective mirror, OR-mask), `StatusOverride57` (innate/equipment source);
  `StatusOverrideMaxLogs`; `StatusOverrideSweepSlots`. **Force** = OR onto `+0x1EF` AND `+0x61` (re-write
  the `0x08`-class bits each poll — they're masked off per turn); **cure** = clear `+0x1EF`/`+0x61`/`+0x57`.
  Remaining bit→ailment meanings are still being mapped empirically.
- **Reactions** (`BraveOverride*`). ✅ proven (Blade Grasp/Hamedo/Counter trigger on a `roll(100, Brave)`;
  cluster `0x30BExx`). Write the defender's Brave `+0x2B` before the roll to suppress. Settings:
  `BraveOverrideEnabled`; `BraveOverrideTargetCharId`; `BraveOverride2A` (MaxBrave) / `2B` (Brave — the
  reaction-roll input) / `2C` (MaxFaith) / `2D` (Faith); `BraveOverrideMaxLogs`; `BraveOverrideSweepSlots`.
  ⚠️ **Never write Brave < 10** — the engine flips the unit to chicken/panic at `0x30A925`.
- **MP** — same mechanism as HP via the combined pre-clamp hook (`0x30A5D7`, §"Outcome control hooks"):
  the staged MP words are `+0x1C8` (debit) / `+0x1CA` (credit), applied as `newMP = clamp(MP + 0x1CA -
  0x1C8)`. Force them exactly like the HP debit/credit.

  Native Mana Shield `445` moves the entire staged HP debit into the MP-debit word whenever current
  MP is greater than zero, then clears HP debit. It does not compare available MP with incoming
  damage; the apply clamp therefore lets one MP prevent the whole hit. The combined DCL context
  exposes `dcl.oldMpDebit`, `target.mp`, `target.maxMp`, `DclDamageFormula`, and
  `DclMpDebitFormula`, which is sufficient to compute an authored bounded HP/MP split. A replacement
  that rejects the redirect or changes both channels enables `DclResultFlagsControlEnabled` so the
  same transaction also normalizes result flag `+0x1E5`.

  `DclResultFlagsControlEnabled` rebuilds the numeric high nibble from the final staged channels:
  HP debit `0x80`, HP credit `0x40`, MP debit `0x20`, and MP credit `0x10`.
  `DclResultFlagsPreserveMask` defaults to `0x0F` and may preserve only low nonnumeric effect bits;
  numeric bits are always derived so stale native HP/MP presentation cannot survive a reroute. The
  flag is written last in the staged-channel transaction. Any write failure restores every earlier
  channel before the callback fails open. Native `0x50` and `0x90` results prove combined flags;
  partial HP+MP debit uses `0xA0`, whose exact popup/reaction composition remains live-gated.

### DCL per-battle MP budget and own-turn trickle

Native current MP (`unit+0x34`) and MaxMP (`unit+0x36`) are the DCL's per-battle resource pool. Pool
size and spell costs remain data-authored; the code mod does not maintain a shadow MP balance.

`DclMpTrickleEnabled` adds the separate small-regeneration mechanism. On the **Strong** own-turn
marker `unit+0x1B8` false-to-true edge, it evaluates `DclMpTrickleFormula` in the normal unit,
equipment, tables, maps, and `DclDerivedVariables` context, clamps the requested credit to
`0..(MaxMP-currentMP)`, and writes the **Proven** current-MP field. First observation only initializes
edge state, so enabling or attaching while a turn is already active does not grant MP. A pointer that
changes character id resets its edge state. `DryRunRewrites` logs without writing, and
`DclMpTrickleMaxLogs` bounds diagnostics.

This feature is independent of action-result `DclPipelineEnabled`: it is poll/turn driven rather than
an HP/MP staged-outcome formula. An empty formula while enabled is a settings-validation error.

## Action-context and roll probes (LT3, 2026-07-02) — the DCL context spine

Four probe/control surfaces added for the LT3 campaign (`work/lt3-calc-rng-results.md`):

- **Calc-entry probe** (`CalcEntryProbeEnabled`, `CalcEntryProbeRva` = `0x3099AC`). ✅ PROVEN live.
  Ring-buffer hook on `computeActionResult` — the single real-code per-(action, target) calc entry.
  Each fire logs: caster slot, action type, **ability id** (`0x01`/0 basic attack, `0x0B`/16 Fire,
  `0x45`/234 Blind…), target index, caster team, turn owner. Fires at preview-open, continuously
  during a charge, at execution, and for **AI actions including candidate-target sweeps** — the
  universal action-context surface AND the AI discriminator (team `unit+0x04` + turn-owner global
  `dword[0x1407B0708]`).
- **Roll-RNG probe** (`RollRngProbeEnabled`, `RollRngProbeRva` = `0x278EE0`). ✅ ran live. The RNG
  head is a Denuvo trampoline; the probe rings (caller return-address, range, chance) per call.
  Result: Fire/Blind accuracy+status rolls are **VM-internal callers** (Blind captured at
  `chance=71` = the displayed %); the only real-code combat roll is the reaction Brave-gate
  (`0x30BDF3`, `chance=61` = Brave).
- **Magic-accuracy / status-chance hooks** (`MagicAccuracyControl*` @ `0x304E2E`,
  `StatusChanceControl*` @ `0x306633`, ForcedChance −1/0..100). Installed and byte-validated but
  ❌ **0 fires for Fire/Blind** — those real-code handlers serve other formula ids; do NOT rely on
  them for the standard spells. Kept for coverage of the handlers that do route through them.
- **Staged-bundle output lever** (`StagedBundleProbe*` @ `0x281F12`, the sweep post-call). **Proven**
  for staged damage. It observes the target bundle at the compute point and can force `+0x1C0` kind,
  `+0x1C4` damage, and `+0x1E5` result flags. Forcing `+0x1C4=111` on a Fire target makes it take
  exactly 111, so the compute-point write reaches the applied result. `+0x1A8` and `+0x1D0` are
  excluded from writes: the former is the order's action-dependent payload (a weapon/item id on
  the relevant paths) and bit `0x08` of the latter gates the native
  item-return/inventory consumer. Their legacy force settings are validator- and runtime-blocked.
  The target sweep builds its output only after this per-target boundary. Protected enemy utility
  consumes the rewritten bundle for target ranking, so this is the permanent AI-visible numeric
  writer boundary.

`DclComputePointNumericEnabled` evaluates the four HP/MP formulas at `0x281F12` for AI state `0x05`
and confirmed execution state `0x2A`. `DclComputePointNumericPlan` preserves unowned native channels,
clamps authored channels, composes final result flags, and makes the write set explicit. AI rows are
transient. Confirmed execution stores the exact natural and final bundle in `DclComputePointCache`;
pre-clamp consumes that cache and never evaluates the formulas against rewritten inputs. Authored
misses publish zero in all four numeric channels but retain the native connected-result byte until
the separate miss selector/output handshake completes.

**Proven:** charged actions retain their exact caster/action/target identity through the final
outer-sweep calculation. Death publishes its probability-weighted lethal debit during AI state
`0x05`; when the charge resolves in state `0x2A`, the transaction samples the 3d6 contest once,
records the final bundle with `cached=1`, and pre-clamp consumes that same result with
`computePoint=1`.

**Proven revive behavior:** the same writer can replace Raise's staged HP credit while leaving the
native KO-target flags untouched. An authored `111` replaces the native `46` at confirmed
state `0x2A`; native apply clamps the result to the target's `91` MaxHP, heals `0 -> 91`, and then
clears effective Dead. Revive formulas should author only the HP credit and treat their applied
value as `min(authored credit, target MaxHP)`; they must not clear Dead directly.

## DCL pre-clamp pipeline (`DclPipelineEnabled`) — ✅ proven live 2026-07-04 (LT6)

The first end-to-end DCL delivery: a **one-switch pipeline** that computes a config-authored
damage formula from the full (attacker, target, equipment, ability) context and rewrites the
staged debit **same-hit**, inside the pre-clamp hook. It wires together the two proven anchors:

1. **Calc-entry probe** (`0x3099AC`, auto-installed by the switch) — the asm stub rings
   `(order-record ptr, target idx, packed casterIdx/type/abilityId, payload)` per (action, target). The
   slot is fully written **before** the ring count is published (x86 TSO), so a consumer never
   reads a torn slot. Both the poller and the DCL callback drain the ring (shared gate, separate
   cursors) into `DclActionContextCache` — latest `(casterIdx, actionType, abilityId, payload)` per target
   index, timestamped at drain (bounded by the poll cadence; `DclActionContextMaxAgeMs` guards on
   top, default 5000 ms).
   Formula `0x25` is a known re-entrant exception: its Rend handler temporarily changes the same
   order record to Attack `(type=1,id=0)` and calls calc-entry from RVA `0x307ED0`. The current
   latest-per-target cache cannot distinguish that synthetic inner calculation from the outer Rend
   action. The player-forecast path calls calc-entry from executable `.trace` RVA `0xEF53F0F` and is
   distinguished from confirmed execution by both caller provenance and battle state; nested Rend
   still requires its own live coverage before the cache suppresses or separately owns it. See
   `04-engine-memory-model.md` §6.5.
2. **Pre-clamp managed callback** (`0x30A5D7`, LT4-proven ABI) — on each staged damage apply, the
   C# callback: resolves the target's unit-table index (base+`0x1853CE0`, stride `0x200`), looks
   up the cached action context, snapshots **attacker and target** unit structs, builds a
   `FormulaContext` via `FormulaRuntimeContextBuilder` (unit vars incl. raw growths/zodiac/gender,
   `EquipmentSlots`/`AttackerEquipmentSlots` item vars from `ItemCatalog`, ability vars from
   `AbilityCatalog`, settings vars/tables/matrices/maps, `dcl.oldDebit`/`dcl.oldCredit`,
   `action.type`/`action.abilityId`/`action.payload`/`action.weapon.*`), evaluates **`DclDamageFormula`**, and returns the clamped
   result as the new debit. Any failure (no context, read error, formula error) falls through to
   vanilla and logs `[DCL-MISS]`/`[DCL-ERR]`; successes log `[DCL] caster/target/ability/result`.
   Logs are queued and flushed by the poller — the hook thread never touches the log file.

Settings: `DclPipelineEnabled` (implies the calc-entry probe + pre-clamp hook + managed callback),
`DclDamageFormula` (validated at deploy time against a synthetic full context — unknown variables
are deploy errors), `DclActionContextMaxAgeMs`, `DclDecisionMaxLogs`. The validator **rejects**
profiles that combine the pipeline with `PreClampFormulaPlanEnabled` or forced debit/credit values
(those write after the callback in the same stub and would silently overwrite the DCL result) and
warns when `PreClampDamageRewriteLogOnly` would disable the callback.

Formula variables available to `DclDamageFormula`: `a.*`/`attacker.*` and `t.*`/`target.*` unit
vars, `aslot.*`/`tslot.*` (+ `attackerSlot.*`/`targetSlot.*`/`slot.*`) item vars, `ability.*`
vars, `action.type`/`action.abilityId`, `dcl.oldDebit`/`dcl.oldCredit`, and every
`FormulaVariables`/`FormulaTables`/`FormulaMatrices`/`FormulaMaps` entry. **`DclDerivedVariables`**
(same schema as `FormulaDerivedVariables`) are evaluated in order in this context before the
damage formula, enabling multi-step models; an unknown input anywhere in the chain is a deploy
error (validator) and a `[DCL-ERR]` fall-through-to-vanilla at runtime.

Test profiles:
- `work/battle-runtime-settings.lt6-dcl-preclamp.json` — plumbing slice (LT5-A4 force-hit stack +
  a minimal predictable formula). **✅ PASSED live 2026-07-04:** 6 attack scenarios (5 attackers,
  incl. dual wield), every swing 100% preview + connected, every UI HP drop equaled its `[DCL]`
  `debit` and differed from vanilla `oldDebit` (e.g. Ramza→Agrias 80 vs vanilla 576; Ninja→Ramza
  45+45 vs 270+270). Zero `[DCL-ERR]`. Notes: (a) the forecast panel still shows the **vanilla**
  number (`oldDebit`) — preview paint is a separate, already-proven lever, not yet wired to the DCL
  formula; (b) one `[DCL-MISS] reason=no-calc-entry` fired for a hit **on the Ninja attacker**
  (target `0x80`, unit idx 17) — almost certainly a counter/reaction hit, meaning **reaction
  attacks do not pass through calc-entry `0x3099AC`** (or their context ages differently); they
  safely fall through to vanilla damage. Reactions are a known open front.
- `work/battle-runtime-settings.lt7-dcl-damage-model.json` — the provisional GURPS-shaped weapon
  damage model (thr/sw base off raw PA, weapon Power, subtractive armor DR by damage type, wound
  multipliers, Brave trait scaling) ported from the reconciler-era `dcl-damage-slice` profile to
  same-hit delivery via `DclDerivedVariables`. Basic attacks only (`action.type == 1`); spells
  keep vanilla damage. Numbers remain provisional pending calibration with Marcelo.
  **✅ PASSED live 2026-07-04:** 6 weapon hits across 4 damage types (knife 34, rod 19/18,
  bow 31, knight sword 117, katana 81 — all == `[DCL]` debit, all ≠ vanilla oldDebit) and armor DR
  tracked the target (same Agrias rod: 19 vs lightly-armored Thief, 18 vs armored Ramza); Fire hit
  for exactly vanilla 122 (`debit == oldDebit`, `actionType=0x0B` falls through as authored).
  Three live observations: (a) the pre-clamp refires repeatedly during a charged spell's
  evaluation/forecast loop (dozens of `122 → 122` rewrites — idempotent, benign); (b) charged
  spells log `[DCL-MISMATCH]` (frame caster ptr ≠ cached caster) yet the **cache is the correct
  side** — ability resolved as Fire id 16 with the right caster; the frame-side pointer is not
  trustworthy for charged actions; (c) another `[DCL-MISS] no-calc-entry` on a counterattack hit,
  confirming LT6: reactions bypass calc-entry and stay vanilla.

## DCL hit control (`DclHitControlEnabled`) — ✅ delivery proven live 2026-07-04 (LT8), miss lever frontal-arc only

**LT8 live results (2026-07-04):** authored 50% hit on basic attacks delivered end-to-end — 13 of 14
swings matched their `[DCL-HIT]` decision 1:1 on screen (forced misses displayed as class-evade
"Miss"/Evaded; hits dealt the LT7 model damage; Fire and monster specials untouched; dual-wield
swing 2 correctly reused the cached decision, `cached=1`). The one mismatch exposed an **engine
truth, not a delivery bug**: the class-evade byte `+0x4B` is only consulted for attacks in the
target's **frontal arc** (classic FFT evasion rules, already in the LT5 ledger) — a side/rear
attack ignored the stamped 100 and landed (with a crit; crits multiply the staged debit ×1.2
before our rewrite, so DCL currently nullifies crit damage). Additional LT8 findings: a Mana
Shield hit stages `oldDebit=0` (HP damage redirected to MP) and the model formula overwrote it
with phantom HP damage — profiles must guard `dcl.oldDebit > 0`; monster basic attacks use their
own action types (`0xB0` Choco Beak / `0xB3` Tackle / `0xB9` Claw et al.), so `action.type == 1`
does not cover them; the preview % flickers 0/100 with the live cached decision at redraw time.
**Consequence:** input-control cannot force a miss from side/rear (accessory evade is
item-table-derived and monsters wear none). The definitive miss is output-control — see the
result-commit section below.

The hit% layer of the DCL, implementing the "own RNG + binary forcing" design (handoff
2026-07-03 §6.3c / definitive ledger §4): the VM's accuracy % is not writable (globals are
recomputed at compute time — refuted live), so the mod does not fight the VM roll. Instead a
**managed decision callback at calc-entry `0x3099AC`** — the same reverse-wrapper pattern as the
pre-clamp callback, appended into the **same single asm hook** as the probe ring-write (this
codebase composes same-site logic by appending blocks into one `CreateAsmHook`; internal order is
therefore deterministic: probe rings first, decision callback runs second, and the static evade
stamp is suppressed — see validator) — computes the authored hit chance, rolls the mod's own RNG,
and forces the binary outcome **before the VM avoidance roll inside that very call** by writing
the target's evade input bytes, the inputs the VM is proven to honor:

- **HIT** → target `+0x46..+0x4E` all 0 (Concentrate-equivalent: no evade source can win).
- **MISS** → all 0 **except class evade `+0x4B` = `DclMissClassEvadeValue`** (default 100).
  Class evade is job-derived and read live from the struct by the VM (proven 2026-06-27); 100
  makes the class-evade source win, rendering the guaranteed class-evade "Miss" (proven LT5-B).

Per fire the callback: reads the order record (`rcx`: dword[0] packs casterIdx/type/abilityId)
and target index (`dl`); guards both indices (< 64, else fail-open + `[DCL-HIT-MISS] reason=...`);
looks up a **decision cache** keyed `(casterIdx, targetIdx, abilityId, actionType, actionPayload)` with TTL
`DclHitDecisionTtlMs` (default 2500 ms) so preview/charge/AI refires of the same action reuse ONE
rolled outcome; on a fresh decision it snapshots attacker+target, builds the same full
`FormulaContext` as the damage callback (unit + equipment + ability + settings vars), applies the
**shared `DclDerivedVariables` chain**, evaluates **`DclHitChanceFormula`**, clamps to 0..100,
and rolls `Next(100)` (hit iff roll < pct). This is **pre-roll**: `dcl.oldDebit`/`dcl.oldCredit`
are 0 in the hit context — hit formulas must not depend on them.

Failure/error handling is fail-open but active: once the target index is known-valid, every
failure/error path zeros target `+0x46..+0x4E` before returning, restoring the HIT baseline. The
guards that fire before a valid target exists (`null-order-record`, `target-index-oob`) still write
nothing. Stamp writes are ordered to fail toward HIT if interrupted: HIT writes `+0x4B = 0` first,
then the other evade bytes; MISS writes the other evade bytes first, then writes `+0x4B =
DclMissClassEvadeValue` last. Exceptions are swallowed and counted; logs are queued and flushed by
the poller (no file I/O on the hook thread).

The poller sweeps tracked stamped targets once per poll tick. If a target's decision has expired or
vanished, the sweep re-zeros target `+0x46..+0x4E` and untracks it; targets with live decisions are
left untouched to avoid racing the imminent VM read.

Armed output control uses a two-consumer retirement handshake. The pre-clamp callback marks staged
result consumption; the result selector `0x205210` marks outcome delivery. Either may run first, and
duplicate marks are idempotent. The entry retires only after both sides fire, so neither hook can
lose the decision needed to keep damage and presentation coherent. The older `0x205B38` kind hook
marks the same outcome-delivery side when it is reachable, but it is not required for retirement.
TTL remains the fail-safe when an action family omits one consumer.

Settings: `DclHitControlEnabled` (default false), `DclHitChanceFormula` (validated at deploy time
against the synthetic DCL hit context, after the derived chain — unknown variables and zero-debit
errors are deploy errors), `DclHitDecisionTtlMs` (2500), `DclHitForcedRoll` (-1 = real RNG,
0..99 = deterministic roll for live tests), `DclHitMaxLogs` (400, a separate budget from
`DclDecisionMaxLogs`), `DclMissClassEvadeValue` (100), and `DclPreviewHitPctEnabled` (false;
mirrors the authored chance into the forecast copy hook). The RNG is seeded once at install and logged
(`[DCL-HIT-INSTALL] seed=...`).

Log formats:

```
[DCL-HIT] caster=0x%X target=0x%X ability=%d type=0x%X pct=%d roll=%d outcome=hit|miss cached=%d
[DCL-HIT-MISS] reason=<target-index-oob|caster-index-oob|null-order-record|empty-formula|target-read-failed|caster-read-failed> ...
[DCL-HIT-ERR] caster=... target=... ability=... type=... error=<derived-chain or formula error>
```

Validator rules (deploy gates): hit control **requires** `DclPipelineEnabled` (catalogs + context
machinery) and the LT5-A4 force-hit baseline — `ItemTableEvadeZeroEnabled` on AND
`EvadeCopierOverrideEnabled` on with **all** `EvadeCopierOverride46..4E = 0` — so residual
equipment evade (which the VM derives from the item tables, not the unit bytes) can never steal a
HIT decision. It **rejects** `CalcEntryEvadeStampEnabled` (two writers of the same target bytes
at the same site; the decision callback subsumes the stamp), an empty `DclHitChanceFormula`,
`DclHitForcedRoll` outside -1..99, and `DclMissClassEvadeValue` outside 0..255; it **warns** that
the callback runs managed code on the calc-entry hot path (preview/charge/AI evaluation fire it,
not just execution).

`DclPreviewHitPctEnabled` requires `DclHitControlEnabled`. It cannot be combined with a static
`PreviewHitPctForcedValue`, because both modes own the same copy-time display value.

Known scope limits (as-built, before LT8):
- **Reactions/counters are untouched** — they bypass calc-entry `0x3099AC` entirely (observed
  LT6/LT7: counterattack hits log `[DCL-MISS] no-calc-entry` on the damage path). Under the
  baseline stack they behave as force-hit vanilla.
- **Magic avoidance** uses the dedicated Magic Evade model below; status-only spells and healing
  remain outside that model.
- **Forecast integration is optional** — `DclPreviewHitPctEnabled` wires the separate display lever
  at `0x227FFE` to `DclHitChanceFormula`; when disabled, the preview percentage remains vanilla.

### DCL physical contest and managed physical multistrikes

`DclPhysicalContestEnabled` replaces the generic percentage decision for actions selected by
`DclPhysicalContestConditionFormula`. `DclAttackSkillFormula` supplies the attack target for a 3d6
roll. `DclDodgeFormula`, `DclParryFormula`, and `DclBlockFormula` supply defense targets;
`DclDefenseAllowedFormula` and `DclDefenseModifierFormula` implement facing and other policy. Dodge
wins ties over Parry, and Parry wins ties over Block. Critical attacks bypass defense; fumbles and
failed attack rolls never request a defense. A normal successful attack requests the chosen defense,
and a finite Parry or Block charge is consumed whether that defense succeeds or fails.

`DclParryUsesFormula` and `DclBlockUsesFormula` establish the finite Guard pools. Capacity formulas
cannot reference `guard.*` because they create that state. The subsequent defense formulas can read:

```text
guard.parryRemaining
guard.parryMax
guard.blockRemaining
guard.blockMax
```

Pools refresh on the defender's own-turn active edge. A selected charge is not removed while the
decision is speculative: the cache records the intended Parry/Block attempts, and the successful
pre-clamp apply commits them exactly once. Repeated callbacks are idempotent. If the live pool no
longer contains all expected charges, the commit spends only charges that still exist and records the
shortfall in `[DCL-GUARD]`.

An approved ability enters the managed physical multistrike route only when its metadata has a
managed-multistrike side-effect policy, `strike_count >= 2`, and the physical-contest condition
selects the action. The runtime performs one independent attack/defense/critical/fumble contest per
authored strike. Every strike sees the local Guard state left by the preceding strikes, while the
live pool remains untouched until the aggregate result commits. `DclDerivedVariables` and all
physical contest formulas are re-evaluated for each strike, with these additional inputs:

```text
dcl.strike.count      # authored number of strikes
dcl.strike.index      # zero-based current strike during contest evaluation
dcl.strike.number     # one-based current strike during contest evaluation
```

The aggregate decision is cached under the normal action/target key. Execution formulas receive:

```text
dcl.strike.count
dcl.strike.hitCount
dcl.strike.normalHitCount
dcl.strike.criticalCount
dcl.strike.attackMissCount
dcl.strike.fumbleCount
dcl.strike.evadedCount
dcl.strike.defendedCount
dcl.strike.parryAttempts
dcl.strike.blockAttempts
dcl.strike.anyHit
```

`dcl.strike.index` and `dcl.strike.number` are `0` in aggregate damage and preview contexts because
no individual strike is selected there. Preview receives the authored `count` and zero outcome
counts; execution receives the cached rolled counts before derived variables and damage formulas are
evaluated. This lets a damage formula price normal and critical landed strikes separately without
reusing the native aggregate debit.

The displayed aggregate hit chance is the exact probability that at least one strike lands. It is
computed over the 3d6 distribution and a state table of remaining Parry/Block charges, so defended
branches that deplete finite Guard change the probability of later strikes. It does not use the
particular sequence that execution happened to roll.

The managed physical route authors one staged result for the outer action and does not synthesize a
result or reaction per strike. **Strong:** the native Pummel carrier therefore sums damage into one
apply and presents at most one native target-reaction opportunity; integrated live validation owns
the final cardinality proof. `[DCL-STRIKE]` records every contest, `[DCL-HIT]`
records the aggregate counts and exact chance, and `[DCL-GUARD]` records the atomic charge commit.
Per-strike magic targeting, Magic Evade, status riders, and multiple native result presentations use
separate carriers and are not implied by physical `managed_multistrike` metadata.
Formula `0x6A` Barrage is marked `native_multistrike`: its fixed four native results must never enter
this aggregate route, which would otherwise multiply four managed contests into each native repeat.

### DCL Magic Evade (`DclMagicEvadeEnabled`)

Magic Evade is an explicit hit-decision model for offensive magic. It runs after the physical-model
applicability check and before the generic `DclHitChanceFormula` fallback.

- `DclMagicEvadeConditionFormula` selects the action family. `ability.formula == 8` selects the
  catalog's native magic-damage family and excludes formula `0x0C` healing and formula `0x0A`
  status-only actions.
- `DclMagicEvadeFormula` returns the target's authored Magic Evade percentage. Equipment-derived
  formulas use item-catalog variables such as `shieldMagicalEvasion` and
  `accessoryMagicalEvasion`; job bonuses can enter through `target.job*` or derived variables.
- `DclMagicEvadeCapPct` clamps the evade result before rolling. The default is 50, which guarantees
  at least a 50% hit chance and prevents immunity.
- The model computes `hitPct = 100 - cappedMagicEvade` and uses the same 0..99 RNG and forced-roll
  control as the generic percent model.
- Calc-entry is per target, so each final AoE victim receives an independent decision. Healing is
  not evaded; status-only magic uses the DCL status contest instead.

The pure managed-magic multistrike resolver supports both metadata policies:

- `magic_evade_per_target`: one Magic Evade decision admits or rejects every authored strike against
  that target, matching the DCL's spell-level per-target rule. This is the authoring and classifier
  default, including for `RandomFire`;
- `magic_evade_per_strike`: every authored strike evaluates and rolls Magic Evade independently,
  with aggregate landed/evaded counts and the exact any-hit probability. This is an explicit
  exception for a future projectile whose design calls for independent avoidance, not an inference
  from native repetition alone.

**Strong:** native `RandomFire` owns repetition and selects exactly one target before one ordinary
calculation per repeat. The runtime reads the native repeat count/index pair and retains the cached
spell-level Magic Evade decision until the final repeat. Because the cache remains keyed by target,
selecting a different target creates its own decision and selecting the same target again reuses the
first decision. A status-bearing RandomFire action uses `native_multistrike_status_rider`; after a
successful landing its post-calc producer creates one fresh packet plan for each repeated result.
Live integration still verifies selector, pre-clamp, status apply, and presentation ordering.

`[DCL-HIT]` identifies this branch with `model=magic-evade magicEvade=N`. An authored miss uses the
normal output-control path, which cancels staged HP/MP debit and credit and selects the clean miss
outcome. The validator requires hit control, selector/output miss delivery, a nonempty applicability
formula, a nonempty evade formula, and a cap within 0..100. Forecast parity additionally uses
`DclPreviewHitPctEnabled`.

The unit formula surface exposes `evade48`, `evade4C`, `evade4D`, and `magEvaRawMax` as diagnostic
reads. Production Magic Evade formulas should not treat those bytes as durable authored stats while
hit control is active, because the force-hit baseline deliberately zeroes native evade inputs.

Test profile: `work/battle-runtime-settings.lt8-dcl-hitcontrol.json` — the LT7 damage-model
profile + hit control (`if(action.type == 1, 50, 100)`), stamp off, real RNG. PASS = every basic
attack's on-screen outcome (damage vs class-evade "Miss") matches its `[DCL-HIT]` line
one-for-one; connecting swings still deal the LT7 model damage (`[DCL]` debit); spells forced-hit
with vanilla damage.

### DCL miss output-control (`DclMissOutputControlEnabled`) — ✅ damage-side proven live 2026-07-04 (LT9); presentation + MP open

**LT9 live results (2026-07-04):** the core goal PASSED — forced misses delivered from **any angle
and against monsters** (rear ×2, side vs a Chocobo, rear with a crit roll: all `outcome=forced-miss
debit=0`, 0 HP lost on screen), closing the LT8 frontal-arc hole. The facing log fields worked and
calibrated the facing enum: **`+0x51`: 0 = −y, 1 = −x, 2 = +y, 3 = +x** (every player-reported
front/side/rear case matches the geometry; Strong pending one more battle's confirmation). Three
findings:
1. **The kind hook at `0x205B38` NEVER fired** (zero `[DCL-KIND]` lines): the store executes only
   when the engine has a real evade outcome to commit — the `+0x15C` bit-4 gate skips it for plain
   hits, so with the VM force-connecting there is nothing for it to intercept. The Q1 "sole
   writer" claim needs refinement: sole writer of *evade* kinds, not a per-execution commit for
   all outcomes. Consequence A: a forced miss renders as a **"0" damage popup with the hit (even
   crit) animation**, not a "Miss" — cosmetically wrong, functionally a miss; the Miss
   presentation is authored by its own slice (see "DCL miss presentation
   (`DclMissPresentationEnabled`, LT10-C slice)" below). Consequence B: the kind-side consumption
   signal is dead in this mode — decisions retire via TTL only (V1 semantics; dual-wield swings
   still share one roll).
2. **CT sanity guard was too strict**: a live unit at CT 108 (legal in IVC) failed the unit
   reader's `ct > 100` check → context build failed → fail-open let FULL VANILLA damage through
   (killed a test target). Fixed same day: the guard accepts the byte's full range.
   **✅ Fix proven live (LT9b, same day):** zero `invalid CT` errors across a full battle
   including a 912-damage attacker.
3. **Mana Shield leaks on a forced miss**: the HP debit is zeroed but the vanilla MP redirect
   (full 201 MP) still applied — the MP channel is not yet covered by the forced-miss path.
   **✅ Fix proven live (LT9b, same day):** 5/5 forced misses vs a Mana-Shield target logged
   `mpDebit=N->0` (including a crit-boosted 268 and a 912) with 0 MP lost on screen; hits still
   drain vanilla MP — which also **live-proves the staged MP-debit word `unit+0x1C8`** (promote in
   `04-engine-memory-model.md`). LT9b also confirmed AI attackers pass through the authored hit%.

The fix for LT8's frontal-arc finding, on the project's output-control-first rule: stop asking
the VM to miss and rewrite what it committed instead. When `DclMissOutputControlEnabled` is on,
the three delivery stages coordinate:

1. **Calc-entry (decision callback):** BOTH outcomes stamp the target's evade bytes `+0x46..+0x4E`
   all 0 — the VM always connects, from any angle, against anything. The decision itself is
   rolled/cached/logged exactly as under LT8; only the stamp changes. With the setting off (or
   **either** managed hook dead, below) the LT8 class-evade MISS stamp is untouched — the
   input-control path remains the fallback.
2. **Pre-clamp (damage callback):** before evaluating `DclDamageFormula`, the callback consults
   the decision cache for the resolved `(caster, target, ability, type)`; a live MISS decision
   short-circuits to a forced staged debit of **0** (the formula never runs) and logs the `[DCL]`
   line with `outcome=forced-miss`. Hits proceed exactly as before.

   **MP coverage (LT9 fix):** the callback's return value only rewrites the staged HP-debit
   (`word[rbp+6] == unit+0x1C4`); it does not touch the staged **MP**-debit at `word[unit+0x1C8]`
   (Strong/static-proven, `04-engine-memory-model.md` §2.3; apply at `0x30A484` computes
   `newMP = clamp(MP + [+0x1CA] − [+0x1C8])`). LT9 exposed the gap: against a Mana-Shield target the
   HP debit was already 0 (damage redirected to MP), so zeroing HP alone left the full vanilla MP
   redirect and the target still lost 201 MP on a "missed" swing. The forced-miss branch now also
   zeroes `word[targetPtr+0x1C8]` in place (`targetPtr` is the unit base — the same pointer whose
   `+0x30` HP the guards read), under the identical `DclMissOutputControlEnabled && both-hooks-active`
   gate, so nothing new is exposed when a miss is not being authored. It reads the staged MP-debit
   first and, when it was nonzero, appends `mpDebit=N->0` to the `[DCL] outcome=forced-miss` line;
   a zero (or read/write failure) is silent and leaves MP untouched. Like `+0x1C0` at the kind hook,
   `+0x1C8` is static-proven but not yet live-proven — the LT9 re-run against Mana Shield is its proof.
3. **Result-kind commit hook (the third managed hook):** static RE
   (`work/1783184308-dcl-miss-consumption-and-counter-path.md` §Q1) located the sole real-code
   writer of the per-target outcome-kind byte `record+0x1C0` at RVA **`0x205B38`** (fn `0x2055FC`):
   `mov [rdi+0x1C0], r12b`, gated by `test [rdi+0x15C],4 ; je`, mirroring the byte to `+0x360`
   while arming the 60-frame result animation. **LT9 REFUTED the "fires once per executed target"
   reading**: the bit-4 gate skips the store on plain hits, so the site only commits **real evade
   outcomes** — and since output-control force-connects the VM, the hook never fires in exactly
   the mode that needs it (zero `[DCL-KIND]` lines in the LT9 log). The hook stays installed and
   harmless; flipping the kind (and the Miss presentation generally) is handled by the LT10-C
   presentation slice below (`DclMissPresentationEnabled`). ExecuteFirst shim (pre-clamp-style mid-function ABI: the function
   body's rsp is 16-aligned because `0x2055FC` makes calls, 64 bytes of GPR+flags saves keep it,
   `sub 0x80` covers shadow + xmm0-5): rdi = result record, r12b = the VM-committed kind
   (`0x00` hit / `0x04` class / `0x06` miss / `0x0B` Blade Grasp). The managed callback maps the
   record to the target (`+0x1BC`), resolves the cached action context and decision, and on a live
   MISS returns `DclMissKindValue` (default `0x06`) for the shim to place in `r12b` **before** the
   original store — the engine itself commits and mirrors the forced kind. Otherwise it returns -1
   and the natural kind stands.

**Consumption-signal design (INOPERATIVE under force-connect — see the LT9 refutation above; kept
for a future presentation slice that makes the commit fire):** the intended signal was the commit
firing once per executed target. **Both** outcomes use the same
**two-consumer handshake** (`DclHitDecisionCache.MarkConsumed`): the kind hook and the pre-clamp
side each mark their side, and whichever fires second retires the entry. Hits are symmetric with
misses because the pre-clamp HIT path invalidates the decision immediately (its own
rewrite-success), so if it fired before `0x205B38` the kind callback would find nothing and log
`decision=none` — breaking the LT9 per-swing 1:1 verification. Under armed output-control the
pre-clamp HIT path therefore marks-consumed instead of invalidating, leaving the decision live for
the kind callback to read (`decision=hit`) regardless of order; with output-control off it keeps
the plain invalidate-on-rewrite (LT8 semantics unchanged). The fire order of `0x30A5D7` vs
`0x205B38` within one action is **not proven**, and retiring on first touch would hand the other
consumer nothing (full formula damage on a "missed" swing, a kept hit-kind on a zeroed one, or a
`decision=none` log). If one side never fires, TTL expiry is the backstop — exactly the pre-LT9
behavior, never worse. The poller's stale-stamp sweep is unchanged.

**Fail-safe:** the site is **Strong (static), unproven live** — LT9 is its proof. The install is
double-AOB-guarded: the store bytes (`44 88 A7 C0 01 00 00`) at `DclMissKindRva` **and** the
unique 16-byte gate window `F6 87 5C 01 00 00 04 74 07 44 88 A7 C0 01 00 00` at rva-9. Any
mismatch (or a wrapper failure) logs `[DCL-KIND-SKIP]` and leaves `_dclMissKindHookActive` false.
Output-control delivery is armed only when **both** managed hooks are live — the kind hook renders
"miss" but only the pre-clamp hook zeroes the staged debit, so a kind-only install would ship a
rendered miss dealing full vanilla damage. All three stages gate on `_preClampDamageRewriteHookActive
&& _dclMissKindHookActive`; if either is missing the mod falls back to the proven LT8 input-control
behavior. Pre-clamp installs first, so when the kind hook comes up half-armed it logs
`[DCL-KIND-DISABLED]` naming the reason (the earlier `[PRECLAMP-REWRITE-SKIP]`/`-FAILED` line).

Settings: `DclMissOutputControlEnabled` (false), `DclMissKindRva` (0x205B38),
`DclMissKindExpectedBytes` (the store bytes; the gate window is checked as a built-in constant),
`DclMissKindValue` (6), `DclMissKindLogOnly` (false — true observes the commit and logs but never
writes r12b; the consumption handshake still runs). Validator: ERROR without
`DclHitControlEnabled` (the decision layer this rides on), ERROR on `DclMissKindValue` outside
0..255, WARN describing the third managed hook and its unproven-live status.

Log formats (bounded by `DclHitMaxLogs`):

```
[DCL-KIND] target=%d naturalKind=0x%X forcedKind=0x%X|kept decision=hit|miss|none
[DCL-KIND-ERR] fails=%d                                (poller-surfaced; emitted only when the count changes)
[DCL] ... result=0 debit=0 oldDebit=%d outcome=forced-miss
[DCL-HIT] ... ax=%d ay=%d af=%d tx=%d ty=%d tf=%d      (new position/facing tail)
```

The `[DCL-HIT]` line now carries raw attacker/target position and facing bytes (`+0x4F` X,
`+0x50` Y, `+0x51` facing — Strong, `work/dcl-unit-state-candidates.md` §2; -1 on read failure).
The facing enum's compass mapping is unconfirmed, so LT9 should correlate the raw values against
the attack angles rather than interpret them.

Known open questions (LT9 observables):
- **Kind flip vs damage popup:** the commit site writes the kind, but the render classifier also
  reads `+0x1BE` (result flag) and `+0x1C4` (staged dmg) — whether a flipped `0x06` alone renders
  the full "Miss" presentation, a 0-damage popup, or something hybrid is exactly what LT9 records.
- **Reactions on a forced miss:** the VM believes it hit — counters, knockback and status procs
  firing on a rendered miss are expected observables to record, not automatic failures.
- **The `+0x15C` bit-4 gate:** preview-vs-execute discrimination via that bit is Hypothesis; if it
  is wrong the hook could fire on non-commits (watch `[DCL-KIND]` fire counts vs swings).
- **Multiple pre-clamp fires per commit** (multi-hit staging) would let the second fire race the
  handshake retirement; never observed in LT6-LT8 (one fire per swing per target), and the `[DCL]`
  log would expose it.

Test profile: `work/battle-runtime-settings.lt9-dcl-missoutput.json` — LT8 + output-control, and
the damage formula grows the Mana-Shield guard from LT8's findings:
`if(action.type == 1 && dcl.oldDebit > 0, dcl.weaponModel, dcl.oldDebit)`. PASS criteria are in
the profile's `_note` (misses render as misses from any angle including side/rear and against
monsters; hits unchanged from LT8; `[DCL-KIND]` shows every flip).

### DCL counter-path probe (`DclCounterPathProbeEnabled`) — observe-only, built 2026-07-04, awaiting LT10

Counters/reactions bypass the calc-entry hook `0x3099AC` (proven LT6-LT9: a counter's damage fires
the pre-clamp with `reason=no-calc-entry` — no `computeActionResult` entry exists for it). Static RE
(`work/1783184308-dcl-miss-consumption-and-counter-path.md` §Q2) located the **Strong-static**
candidate for where a counter's result is staged instead: fn **`0x30C798`**, whose sole caller is
`0x20460A` inside the action-resolution driver `0x20435C`. It reads the target index at record
`+0x1BC`, applies staged HP via `0x30A484`, and emits result bytes `+0x1E8`/`+0x1E9`. Whether it is
**counter-specific** (vs. a shared post-roll commit used by ordinary actions too) is a **Hypothesis**
this probe settles live.

`DclCounterPathProbeEnabled` installs a read-only `ExecuteFirst` hook at `0x30C798`. This is a
**function entry** (standard prologue `mov [rsp+8],rbx; push rdi; sub rsp,0x20` — same shape as
calc-entry `0x3099AC`), so the shim runs, then the stolen prologue, then the body unchanged. **At
entry `rcx` carries the result record** — verified on the exe on disk (pefile+capstone, the RE
report's method): the prologue is `48 89 5C 24 08 57 48 83 EC 20 80 79 01 FF` = `cmp byte[rcx+1],0xFF`,
immediately followed by `movzx eax,byte[rcx+0x1BC]` (the target-idx read off `rcx`). The native shim
captures only `rcx` into a ring buffer; the managed drain reads the record fields and the target HP
(unit table, idx guard `<64`) under try/catch and logs — **no writes to game memory**. `ExpectedBytes`
doubles as the AOB guard: any mismatch logs `[DCL-CTRPATH-SKIP]` and disables the probe.

Settings: `DclCounterPathProbeEnabled` (false), `DclCounterPathProbeRva` (`0x30C798`),
`DclCounterPathProbeExpectedBytes` (the verified function-entry prologue), `DclCounterPathProbeMaxLogs`
(200). Validator: WARN describing the Strong-static observe-only site; ERROR if `MaxLogs < 0`, or if
`Rva <= 0` / `ExpectedBytes` empty while enabled.

Log format (bounded by `DclCounterPathProbeMaxLogs`):

```
[DCL-CTRPATH] rcx=0x%X targetIdx=%d e8=%d e9=%d hp=%d
```

Test profile: `work/battle-runtime-settings.lt10-counterpath-probe.json` (LT9 stack + the probe on).
**PASS** = provoke counters (attack units with the Counter reaction) and confirm `[DCL-CTRPATH]` fires
for the counter's target with sane values, correlating with the existing `[DCL-MISS]`/`[DCL]`
`reason=no-calc-entry` lines from that counter. Also observe whether it fires for **normal** actions —
if it does, `0x30C798` is a **shared** post-roll commit, not counter-specific (that refutes the
hypothesis, and is a valuable result either way). `[DCL-CTRPATH-SKIP]` at install = AOB guard tripped;
report immediately.

### DCL status authority and the retired staged-auxiliary path

The authoritative status surfaces are the five-byte source, immunity, effective, and durable-master
arrays documented in `04-engine-memory-model.md` §2.3. Status add/remove is data-controllable through
the durable/effective arrays; native status rejection is data-controllable through the immunity array.

The legacy `DclStatus*` staged probe name is misleading. Its `+0x1A8` word is an item/inventory
side-effect id and `+0x1D0` bit `0x08` gates the native item consumer. The Kiyomori action stages
item id `0x2B` at `+0x1A8`; consumer `0x30CEA0` accepts only item ids `1..0x104`, queries the owned
item count through `0x279064`, and increments the inventory quantity for that item when below 99.
These fields are not status authority.

**1. Legacy staged-auxiliary observation.** `DclStatusStageProbeEnabled` remains log-only for profile
compatibility and emits `[DCL-STAGED-AUX]` with the item auxiliary word, side-effect mask, outcome
kind, and result flags. It requires `DclPipelineEnabled`. `DclStatusSuppressEnabled`,
`DclStatusForceId`, and `DclStatusForceValue` are retired. The validator rejects them and the runtime
emits `[DCL-STAGED-AUX-WRITE-BLOCKED]` without constructing a write plan. The staged-bundle hook also
hard-blocks `StagedBundleForceAilment` and `StagedBundleForceApplyMask` even when no standalone
settings validation was run.

**2. Direct status poke — ADD/REMOVE outside actions.** A one-shot guarded write to the durable
status master region of a chosen unit, mirroring the Brave/status override pokes:
`StatusPokeTargetCharId` (−1 off), `StatusPokeMode` (`add` = OR / `remove` = AND-NOT),
`StatusPokeOffset` (default `+0x1EF` master; bounded to the unit struct `0x0..0x1FF`),
`StatusPokeMask`/`StatusPokeValue` (either supplies the bit mask), `StatusPokeMaxWrites` (default 1).
Sanity-checks the charId is registered, then logs
`[STATUS-POKE] unit=0x%X id=.. mode=.. off=0x%X mask=.. old=0x%X new=0x%X`. For status byte index
`n`, force via `(+0x1EF+n)|=bit` and `(+0x61+n)|=bit`; cure an inflicted status by clearing both.
Only clear the matching source byte `+0x57+n` when intentionally removing an innate/equipment effect.
The one-shot is consumed on the **first matching target regardless of outcome** — a no-op match (bit already in the
desired state, logged `no-op(already-set)`) or a write failure (logged `write-failed`) both count, so
the poke never silently re-fires later when vanilla flips the byte back. Direct status writes are
**Proven**; DCL-owned durations use the proven turn-owner signal and clear the authored master/effective
bit after the configured number of target turns.

Generic `DclStatusRule` authoring treats status byte 0 as lifecycle-sensitive. The only permitted
byte-0 add/remove mask is `0x10` (`Undead`), whose durable/effective write behavior is **Proven**.
`Crystal`, `KO`, `Charging`, `Jumping`, `Defending`, and `Performing` remain outside the generic
status-rule path; each requires its engine-owned lifecycle mechanism or a dedicated integration.

Every rule also declares `NativeRiderPolicy`. `absent` means the action has no native status rider;
`suppressed-by-data` means the matching action-data build removes it. `retained-as-carrier` keeps a
statically approved status-only rider so the native formula still produces an applicable result,
then replaces every inherited bit in the paired packet before native validation. Empty or unknown
values fail validation, so managed status authority cannot silently compose with an inherited
native rider.

The first retained carrier group is formula-`0x22` Kiyomori (`81`, Protect/Shell) and Masamune
(`84`, Regen/Haste). Their state-`0x15` apply path reaches pre-clamp without requiring a positive
HP/MP channel. Complete ownership is byte 3 masks `0x20|0x10` for Kiyomori and `0x40|0x08` for
Masamune.

The second group is the safe formula-`0x38` subset:
`149,181,182,187,188,189,190,191,192,193,194,195,287,313,326,327,328,346,350,356`. Formula `0x38`
dispatches directly to the common finalizer and every catalog member declares a 100% status result.
The accepted subset contains one-bit actions, deterministic cancel sets, `Separate` bundles,
`random-one` groups, and `all-or-nothing` groups. Nightmare `194` and Toot `313` cache one random
member and share one resistance contest across the group. Poisonous Frog `346` shares one contest
between Frog and Poison, while equipment immunity remains per bit. Suffocate `183` and Finishing
Touch `262` remain lifecycle-owned because they include Dead.

Every retained rule must use `ActionType=-1`, and the complete native bit set for that ability must
be owned. Missing ownership or any other ability id fails validation. `ConditionFormula` is forbidden
because it would create an execution where an inherited bit has no managed decision. A
match/resistance evaluation error clears the owned inherited bit and logs `outcome=fail-closed`; it
never falls through to the native contest. The corresponding action data must retain its native
rider; neutralizing it would destroy the carrier this policy relies on.

`ContestMode` defaults to `independent`. Shared carriers use one nonempty `ContestGroup` and either
`random-one` or `all-or-nothing`. Every group member has the same exact ability, action scope, mode,
and `ResistanceFormula`; the formula cannot reference `status.*`. Random-one members not selected
clear their inherited packet bit and log `outcome=not-selected`. The carrier allowlist fixes the
required mode per ability, so changing a correlated native bundle into independent rolls fails
validation.

Formula `0x0A`, `0x0B`, and ten special conditional families are not members of the retained
allowlist. Their handlers can skip packet finalization, so they use `NativeRiderPolicy` value
`replaced-post-calc`. The runtime hooks the proven outer-sweep completion at `0x281F12`, requires
the cached calc row to have outer provenance and battle state `0x2A`, and evaluates the exact
action/target packet there. Forecast calls return through `.trace` at `0xEF53F14` in state `0x19`
and cannot consume this execution decision. The producer stores the resulting plan; pre-clamp
reuses it instead of rolling 3d6 again.

Nameless Song `91` and Forbidden Dance `98` use the same producer rather than a custom periodic
timer. Formula `0x1C/0x1D` retains exact performance action identity and supplies complete
catalog-derived `Random` packets, so each tick is one `random-one` contest. The producer preserves
the native caster-Sleep eligibility check before any managed packet is staged. The native scheduler
continues to own tick cadence and Performing cleanup; those two event shapes remain a live gate.

The ability catalog is mandatory for this policy. Validation derives every native status bit and
requires complete ownership, `ActionType=-1`, the native add/remove operation, and the matching
bundle mode: multi-bit `AllOrNothing` uses one `all-or-nothing` group, multi-bit `Random` uses one
`random-one` group, and other actions use `independent`. A match or formula error clears the owned
native bit fail-closed. Native action data remains intact so forecast and AI retain a read-only
probability carrier; displaying the authored DCL probability instead of that native value remains a
separate presentation/scoring integration. `tools/analyze_dcl_status_conditional_producer.py`
guards the 82 conditional actions, two performance actions, two RandomFire status actions, native
anchors, execution gate, cached plan, performance Sleep guard, and Rend preservation. The
RandomFire formulas require the catalog's native flag and exact seven-bit `random-one` ownership.

Bequeath Bacon preserves its engine-owned special path. Formula `0x57` performs the bounded target
level gain and routes the caster's Crystal transition through the native lifecycle finalizer
(04 §6.4). DCL rules never replace that formula or write/clear the Crystal bit.

**3. Data-first native-rider suppression.** The action override can set `InflictStatus=0` when an
independent ordinary HP result still carries the action into the successful DCL apply window.
`tools/build_neuter_data.py --dcl-status-rider-neuter <ids>` exposes a fail-closed 26-action
allowlist for this shape and changes only `InflictStatus`. Every selected id must have exact
`DclStatusRules`; the option does not authorize policy by itself. Dedicated instant-KO, RandomFire,
status-only, self/caster, and custom carriers are rejected from this ordinary allowlist because
clearing their rider can erase the only result or violate another mechanism's cardinality/lifecycle
owner. Statically proven support and split-result exceptions use their own narrower options.

Native status application is a paired 40-bit transaction: add masks at `unit+0x1DB..+0x1DF`, remove
masks at `unit+0x1E0..+0x1E4`, and the low result/presentation bit at `unit+0x1E5`. The current-build
commit at `0x30C878` preserves native immunity, source-bit, animation, and per-status side effects.
Status-only integration therefore targets this native carrier; it does not convert actions into
fake zero-damage results. The conditional producer can create the carrier at confirmed execution;
forecast/AI parity remains a presentation/scoring layer rather than an execution-output gap.

**Proven for the ordinary HP-result carrier:** an authored Blind rule on basic Attack stages
`packetAdd=0x20`, `packetRemove=0x00` in status byte `1`, preserves the natural `14` HP debit, and
changes the native result flags from `0x80` to `0x88`. The native delivery displays Blind on the
target and still dispatches its independent Auto-Potion Reaction. This proves that packet ownership,
numeric delivery, native status commit, and downstream Reaction selection compose without a fake
zero-damage result or a duplicate apply.

For the 26 allowed ordinary damage-plus-rider actions, the pre-clamp runtime is the managed packet
producer. It clears each rule-owned bit from both native packet halves, then stages the successful
operation in exactly one half; immunity or a successful 3d6 resistance leaves the bit absent from
both. All unrelated packet bits are preserved. Packet bytes and `+0x1E5 & 0x08` commit inside the
same rollback-protected transaction as authored HP/MP debit and credit. The game's later validator
and `0x30C878` committer own durable/effective state, source preservation, animations, and native
per-status consequences. Duplicate rules for one ability/action/status bit fail validation because
one packet bit has one managed owner.

Muramasa `82` is the grouped member of this data-suppressed set. Formula `0x20` retains its `MA * Y`
HP result after `InflictStatus` is cleared. Its Confusion byte 1 mask `0x10` and Death Sentence byte
4 mask `0x01` rules use `ActionType=-1`, one `random-one` `ContestGroup`, and the same
`ResistanceFormula`. Missing either removed bit, changing the mode, or using a grouped
data-suppressed ability outside the static allowlist fails validation.

Crushing Blow `219` and Unholy Sacrifice `357` are also ordinary data-suppressed riders. Formula
`0x67` is a direct alias of the already accepted formula-`0x2D` damage/status handler, so Stop is
independent of its HP carrier. Formula `0x69` derives its power term and unconditionally enters the
formula-`0x4E` non-Faith damage pipeline, so clearing Slow likewise preserves HP delivery. Their
dispatch, byte anchors, exact catalog riders, and data allowlist membership are guarded by
`tools/analyze_dcl_dark_knight_formulas.py`.

Blood Drain `200/284` uses the mapped formula-`0x47` paired HP transaction. Removing only its native
Blood Suck rider leaves the target HP debit and source HP credit intact; an independent
`suppressed-by-data` rule owns Blood Suck at byte 1 mask `0x04`. The exact single-target catalog
pair, drain helpers, result caps, and allowlist membership are guarded by
`tools/analyze_dcl_drain_transactions.py`.

Dragon's Gift `252` uses a separate support-rider allowlist and
`--dcl-support-status-rider-neuter 252`. Formula `0x5B` first accepts only monster graphic-set ids
15 and 16, stages the target HP credit and twice that value as a paired source HP debit, and then
unconditionally enters the common status finalizer on the eligible path. Clearing only
`InflictStatus` preserves that Dragon/Hydra gate and numeric transaction. Eleven
`suppressed-by-data` remove rules own the complete Cancel packet: Darkness, Confusion, Silence,
Oil, Berserk, Frog, Poison, Stop, Sleep, Don't Move, and Don't Act. The handler bytes, exact catalog
set, and exclusive support allowlist are guarded by `tools/analyze_dcl_dragon_gift.py`.

Self-Destruct `277` uses the split-result option
`--dcl-conditional-status-rider-neuter 277`. Formula `0x52` stages
`caster.maxHp - caster.hp` as each non-self victim's HP debit and calls the status finalizer only for
that branch. Its caster branch instead stages `caster.hp` as a lethal self-debit and skips the rider.
The formula context exposes `dcl.isSelf` from exact unit-pointer equality. Validation accepts only
one byte 2 mask `0x80` Oil add with `ConditionFormula="dcl.isSelf == 0"`; missing or changing that
condition fails closed so Oil cannot leak onto the caster result. The handler, catalog identity,
data allowlist, and branch contract are guarded by `tools/analyze_dcl_self_destruct.py`.

**4. Instant KO through engine-owned lethal HP.** `DclInstantKoControlEnabled` evaluates exact
per-ability `DclInstantKoRules` with the same 3d6 resistance language as status rules. Equipment KO
immunity (`immunity byte 0`, mask `0x20`) auto-resists. A successful rule replaces the staged HP
debit with `current HP + staged HP credit`; native HP apply clamps to zero and owns the complete KO
lifecycle. A resisted or immune rule never writes or clears the `Dead` bit and can either preserve
the ability's authored ordinary damage or zero it through `ZeroDamageOnFailure`.

When the compute-point writer is enabled, instant KO is part of the same idempotent numeric
transaction. AI scoring receives the expected debit from the exact 3d6 success probability, not a
sampled future roll. Confirmed execution rolls once and caches either lethal debit or the authored
failure debit; pre-clamp reuses that result without a second contest. The legacy execution-only path
remains as a fallback for profiles that do not enable the compute-point writer.

**Proven:** with native Death removed from the action data, a forced execution roll of `3` against
resistance `14` produces a resisted zero-debit transaction and leaves the target alive. A forced
roll of `18` against the same target produces a lethal debit equal to current HP, and native HP
apply performs the `430 -> 0` transition. Neither path invokes the legacy instant-KO delivery.

Each instant-KO rule requires `NativeKoSuppressedByData=true`, and validation rejects the rule until
that acknowledgement is present. The matching action data must remove the native KO rider first so
a resisted DCL roll cannot still kill. `tools/build_neuter_data.py --dcl-instant-ko-neuter <ids>`
supplies the harmless formula-`0x08`, `X=Y=1`, no-status staging route for selected catalog abilities
that add `Dead`; each id is enabled only when its ordinary DCL damage route and instant-KO rule are
authored. `--dcl-instant-ko-neuter 30` isolates Death for a vertical-slice test. `Crystal` is
deliberately excluded because corpse crystallization is not equivalent to lethal HP delivery and its
only catalog route, Bequeath Bacon, remains native.

`tools/validate_dcl_runtime_data_pair.py` enforces the cross-artifact half of this contract. It checks
that the runtime ability set exactly matches the declared data set, verifies the selected SQLite rows
and NXD hashes, and rejects forced rolls or probe switches in an integrated profile. Runtime-settings
validation alone cannot prove the contents of the paired binary action table.

**5. Brave reaction taxonomy through native effects/RNG.** `DclReactionTaxonomyEnabled` routes exact
`DclReactionRules` into `courage`, `caution`, or `neutral` chance policies. `ConditionFormula` can
suppress a native evaluation before its chance is applied. Default Courage chance is
live Brave, default Caution chance is `100 - Brave`, and Neutral requires `FlatChance` or an explicit
`ChanceFormula`. Custom formulas receive `reaction.abilityId`, `reaction.brave`,
`reaction.inverseBrave`, `reaction.flatChance`, the three `reaction.is*` mode flags, and the incoming
identity fields `reaction.sourceValid`, `reaction.incomingActionValid`, `reaction.sourceIdx`,
`reaction.targetIdx`, `reaction.sourceCharId`, `reaction.incomingActionType`,
`reaction.incomingAbilityId`, `reaction.sourceTurnEpoch`, `reaction.targetTurnEpoch`, and
`reaction.isSelfSource`. When the source is valid, ordinary
`attacker.*` / `a.*`, `action.*`, `ability.*`, and attacker equipment variables describe that
incoming source/action. All chance results clamp to `0..100`.

When a DCL hit decision is still live, the same context exposes `reaction.incomingHitKnown`,
`reaction.incomingHit`, `reaction.incomingMiss`, `reaction.incomingPhysicalOutcome`,
`reaction.incomingDefenseKind`, `reaction.incomingAttackMiss`, `reaction.incomingFumble`,
`reaction.incomingDefended`, and `reaction.incomingCritical`. A condition that depends on these
outcomes checks `incomingHitKnown` first; native actions outside the DCL decision cache report the
outcome as unknown rather than inventing one.

The four real-code Brave gates receive the exact Reaction id from the enclosing native function and
replace only the chance argument to the native roll. `DclMissSuppressReactionsEnabled` composes after
the taxonomy: a cached DCL miss still forces the resulting chance to zero. The global
`ReactionChanceControlEnabled` conflicts with this owner and is rejected.

`ConditionFormula` is a suppress-only filter: false produces chance zero on an evaluation the native
engine already opened. It does not synthesize a missing trigger. Riposte-on-miss, Countershot for an
action family the native record ignores, or any other new reaction window needs a separate trigger
producer. Conditions also never consume cadence state because the chance path may run during
forecast or AI evaluation.

Reaction conditions can combine unit coordinates, named statuses, ability targeting metadata, and
configured equipment/weapon range. They do not yet receive the native weapon line-of-fire verdict
or an execution-provenance enum that distinguishes reactions, damage-over-time, and field ticks.
The engine's Arc/Direct weapon resolvers authorize a candidate when their reached/intercepted unit
index equals the intended target (`04-engine-memory-model.md` §5.4), but that equality is not wired
into `ConditionFormula`. A geometric distance check or `!a.status.invisible` must not be documented
as true line of sight.

Nature's Wrath already selects ordinary Geomancy action `126..137` from the reactor's own tile via
the native terrain table. Because active Geomancy uses the same payload ids, the final reaction's
`no rider` rule cannot be implemented by globally deleting those riders. The status-output rule must
receive execution provenance that identifies the Nature's Wrath carrier while retaining the selected
payload id for damage and element lookup.

The hookable Earplugs branch covers formula `0x2A` (Speechcraft actions `116..125`). Bardsong
`86..92` and Dance `93..99` use formula `0x1C/0x1D` and are outside that branch. A final narrow
Earplugs policy therefore needs an explicit action-id membership map and either proof of a separate
VM path or DCL suppression for the protected performance families; it must not become a broad status
immunity.

Regenerator `428` already has a real-code HP-damage trigger: reaction bit `unit+0x94 & 0x02` and
result bit `unit+0x1E5 & 0x80` stage the exact reaction plus the HP-debit payload. The branch itself
does not check survivor HP. Its final near-native path therefore needs only the authored Caution
chance if a lethal-hit live gate confirms that the surrounding worker suppresses invalid delivery.

Critical: Recover HP carrier `431` natively credits all missing HP after its Brave-gated post-hit
selection. If Grit's final minor clutch payoff is HP recovery, preserve the carrier for presentation
and trigger timing but replace that credit with the authored bounded amount under reaction
provenance. The payoff, low-HP threshold, amount, and cadence remain design inputs; the native full
heal is not an acceptable default for a minor Grit effect.

The incoming source index comes from `dword[0x14186AFF4]`, which the native Counter staging branch
also uses to identify the original attacker. Incoming action type/id prefer the fresh calc-entry
cache for the same target/source and fall back to the source unit's order record at `+0x1A1/+0x1A2`.
Because the cache is keyed by the exact calc-entry target index, a matching cache entry also proves
that this target participated in that action evaluation; it does not by itself distinguish forecast,
AI scoring, and execution.
This context is **Strong** offline; filters that consume persistent cadence remain gated on an
execution-only signal rather than consuming state during forecast/AI evaluation. The action queue
has three post-store actor boundaries: pass 0 at `0x2066AE`, pass 1 at `0x206743`, and pass 2 at
`0x206421`. Every boundary follows the action-id mirrors at `actor+0x18C` and `actor+0x142`;
passes 0/2 use the actor in `rbx`, while pass 1 reuses the actor in `rdi`. Pass 1 is **Proven** to
carry ordinary actions such as Claw `280`, so it is not a Reaction-specific cadence boundary. Pass
2 at `0x206421` is the **Proven** accepted native Reaction commit boundary. Counter `442` and
Auto-Potion `441` each produce exactly one pass-2 commit with agreeing actor ids and the expected
reactor/source ownership. Counter materializes its source target before its native effect; the
self-directed Auto-Potion shape legitimately has no explicit target. The target list at the commit
snapshot is not authoritative; native delivery can fill or replace it afterward. Persistent
cadence is owned once at the accepted pass-2 commit, while final target/effect work follows the
delivered native action.

`DclReactionCommitProbeEnabled` installs bounded observe-only hooks at all three actor boundaries. The
configured `DclReactionCommitProbeRva` and `DclReactionCommitProbeExpectedBytes` own the pass-2 AOB
guard; pass 0/1 have current-build exact-byte guards in the runtime. Any failed guard prevents the
probe set from installing. `DclReactionCommitProbeMaxLogs` caps formatted events. A row is tagged
`[DCL-REACTION-COMMIT]` only when the mirrored actor ids agree and the id is a native Reaction in
`422..453`; other fires are `[DCL-REACTION-COMMIT-NOISE]`. Both tags record the actor pointer,
reactor/source unit indices, id copies, staged record pointer, and target count/first eight target
indices at `actor+0x1A9/+0x1AA`. The probe does not write game memory or consume cadence.

`DclReactionActionReplacementEnabled` is an observe-only diagnostic at the pass-2 commit hook. It
can report a proposed executable id while preserving `actor+0x18C`, but live writes at this site are
retired: native carrier delivery overwrites `actor+0x142` after commit (Counter `442` becomes Basic
Attack `0`). The accepted post-selector boundary at `0x2063BD` exposes the complete executable order
before actor construction; replacement changes the order type and payload there, not `actor+0x142`.

Counter `442` is the preferred basic retaliation carrier. Its pass-2 branch calls the native typed
order writer with type `1`, payload/action `0`, and target coordinates derived from source-index
global RVA `0x186AFF4`. A future trigger producer can therefore propose `unit+0x1CE=442` for the
reactor immediately before the pass-2 selector, but it must first prove that the source global,
reactor ownership, and cleanup lifetime are valid for the added trigger window. The commit control
does not perform this staging.

Counter Tackle `436` does not need action replacement. Its native pass-2 order is already
`type=0x0B`, payload `147` (Rush), validated and targeted against the incoming source. Formula
`0x37` owns Rush damage and knockback, and the active data leaves that formula inherited. The final
Squire policy should suppress non-melee/non-landed evaluations through the exact-id reaction rule and
let the native Rush order deliver displacement; only multi-hit cadence and one live knockback control
remain policy/integration gates.

Native Magick Counter `435` is not the final Rod Counter effect: it copies incoming
`orderRecord+2` through global RVA `0x7B0778` and reactor `unit+0x1E6`, then emits a type-`0x0B`
order with that spell id. Rod Counter needs a type-`1`, payload-`0` basic-action order so the equipped
Rod selects the DCL bolt formula. Replacing only `actor+0x142` after commit does not prove that the
VM ignores the already-authored typed order; the Counter `442` carrier is structurally safer.

`DclReactionPreSelectorProbeEnabled` installs an observe-only hook at the configured pass-2
pre-selector RVA (current build `0x2063A9`). Before native consumption it snapshots the source and
evaluated-Reaction globals, incoming actor/record identity, and all 21 `unit+0x1CE` candidate words
with active markers. `[DCL-REACTION-PRESELECT]` is evidence for producer design only; the probe has
no carrier id setting and cannot write a candidate.

`DclAutoPotionConsumeProbeEnabled` installs an observe-only hook at the shared item-inventory
decrement (current build RVA `0x2816B2`). It records the native selected item id, old inventory
count, decrement, order context, `+0x1A2` action id, `+0x1A8` selected item, source index, and battle
state before the native subtraction. The logger classifies Auto-Potion only when action id `441`
and matching item id `240..242` agree; all other item events remain negative controls. The hook is
expected-byte guarded, bounded by `DclAutoPotionConsumeProbeMaxLogs`, and has no write setting.

`DclWeaponLineOfFireProbeEnabled` installs two observe-only post-return hooks at the Arc and Direct
weapon resolver call sites (current build RVAs `0x28030B` and `0x2803A3`). Each event records the
acting unit, staged coordinates, intended candidate index, resolver result index, action identity,
weapons, source index, and battle state. `[DCL-WEAPON-LOF] included=True` means the native gate's
nonnegative `candidateIdx == resultIdx` contract succeeds. The hooks never invoke a resolver or
change targeting state; both are expected-byte guarded and share a bounded ring/log cap.

`DclReactionCadenceState` tracks each unit slot/character's own-turn rising-edge epoch and provides
idempotent consumption primitives for one reaction per own cycle or one per attacker-action token.
The attacker token combines source slot/character, source turn epoch, incoming action type, and
ability id. The primitive is offline-tested; runtime trigger policies consume it only at a native
execution/effect-commit boundary, never in a forecast chance callback.

Vigilance cannot use its native effect unchanged. Its real-code branch only stages id `426` and the
incoming action id; the downstream native Caution/Vigilance effect is Defending. The final Thief rule
must instead add its authored bonus to `DclDodgeFormula` before the physical defense roll, suppress
the native Defending delivery, and reserve/commit the once-per-own-cycle token without consuming it
during forecast or AI scoring. The calc-provenance classifier is the timing authority for that split.

The synthetic-Reaction transaction supplies a configurable carrier whose native dispatcher has no
trigger. Blank carrier `443` is the current structural probe: the pass-2 generic selector accepts it
if `unit+0x1CE` is staged externally, but the dispatcher never evaluates its equipped-reaction bit.
The successful pre-clamp result path checks an exact `unit+0x14 == configured carrier` owner after
every fail-open return, requires a valid non-self source/action and a surviving defender, treats the
committed result as the landed-hit authority, evaluates the carrier's configured courage, caution,
or neutral taxonomy rule once per attacker-action token, and arms a private per-defender mailbox.
Replayed result callbacks reuse the accepted/rejected reservation; they never reroll, restage, or
consume cadence.

The pre-selector consumes each request once, requires an active surviving unit and empty
`unit+0x1CE`, and either audits `would-stage` or writes the configured carrier under
`DclSyntheticReactionMaxWrites`. The separately configured accepted-order controller at `0x2063BD`
may replace the action head, copy the incoming source target tuple, or do both before actor
construction. The synthetic transaction itself never writes a status, stat, or job-specific effect.
Only an exact producer-owned pass-2 commit that agrees on actor, carrier, and source consumes
`TryConsumeAttackerAction(carrier, token)`, so repeated callbacks and duplicate commit rows cannot
commit twice. Native execution of the accepted order owns effect delivery; state `0x2C` audits each
delivered strike/transaction (**Strong/offline-tested, live-gated**).

`DclSyntheticReactionEnabled` is disabled by default and `DclSyntheticReactionLogOnly` defaults
true. The profile selects `DclSyntheticReactionCarrierId`, requires exactly one taxonomy rule for
that carrier, and composes the guarded pre-selector, pass-2 commit, materialization, and accepted-order
rewrite boundaries. `DclSyntheticReactionTrigger` currently accepts only
`successful-hit-survivor`; deterministic tests may set `DclSyntheticReactionForcedRoll = 0..99`.
Live staging is bounded to `DclSyntheticReactionMaxWrites = 1..32`.

`DclReactionProducerEnabled` is a disabled-by-default vertical-test control hosted by the AOB-guarded
pass-2 pre-selector hook. It considers only the configured battle-unit index, requires that unit to
be active and `unit+0x1CE` to be empty, and then either logs `would-stage` or writes one configured
Reaction id. Live mode is bounded by `DclReactionProducerMaxWrites` and requires the reaction-commit
probe so the accepted queue pass is captured. It does not decide DCL eligibility, source targeting,
effect policy, or cadence; those remain separate transaction inputs.

`DclReactionRetargetEnabled` is an observe-only diagnostic at the pass-2 commit. It reports the
incoming source as the candidate target for an exact carrier. Live writes at this site are retired:
Counter proves that `actor+0x1A9/+0x1AA` can be empty or stale at commit and is overwritten by the
native carrier branch. Retargeting binds instead to the complete accepted order at `0x2063BD`.

`DclReactionMaterializationProbeEnabled` observes RVA `0x2063BD`, after pass 2 accepts and fully
materializes a carrier-specific `unit+0x1A0` order but before the actor constructor at `0x2063CA`.
Each bounded row snapshots the exact Reaction id, reactor/source indices, and all 20 order bytes,
including executable type/payload, item payload, target unit index, and x/layer/y coordinates. It is
observe-only and guarded by `DclReactionMaterializationProbeExpectedBytes`.

The row's `reactorIdx` and `casterIdx` are selected unit-table indices. They are not interchangeable
with the subsequent actor's `reactorIdx`/`actorIdx`: a live Counter used selected/caster index `3`
and actor index `1`. The live analyzer accepts `--actor-reactor` for this explicit namespace bridge
and otherwise defaults it to the selected index for fixtures where they happen to agree.

`DclReactionOrderRewriteEnabled` adds a guarded controller to this same boundary. It is disabled and
log-only by default. One exact `DclReactionOrderRewriteCarrierId` may replace order `+0x01/+0x02`
through `DclReactionOrderRewriteActionType/AbilityId`, retarget the complete unit delivery to the
incoming source, or do both. Source retarget writes mode `5`, source index `+0x0B`, x from source
`unit+0x4F` to order `+0x0C`, layer bit `unit+0x51.bit7` to `+0x0E`, and y from `unit+0x50` to
`+0x10`. Selected/source indices outside `0..20`, original-order mismatch, and exhausted write caps
leave the order unchanged. Live mode requires exact expected native type and payload guards; every
attempt records the original order head, final 20-byte order, disposition, and cumulative write
count in the materialization ring.

`DclReactionEffectProbeEnabled` observes RVA `0x212C2E`, the state-`0x2C` boundary after VM execution
and current-actor resolution but before state-`0x2D` cleanup. Each bounded row captures actor pointer
and index, presentation id `+0x18C`, executable id `+0x142`, source index, battle state, and target
list. It performs no effect or cadence write. The boundary fires once per delivered native execution
transaction: Counter retains presentation id `442`, exposes executable Basic Attack `0`, and emits
two rows for a Ninja's Dual Wield response. It can own per-strike effect auditing, but once-per-
Reaction cadence must deduplicate against the earlier pass-2 commit.

Set `VmInternalAvoidance=true` only for a VM-owned avoidance reaction that does not traverse the four
gates. The calc-entry callback reads the exact Reaction id from `orderRecord+2`; this identifies the
actual evaluation for equipped, innate, and derived reactions. It writes the authored chance
temporarily to defender Brave `unit+0x2B` and pushes a thread-local restoration frame. A guarded tail
at the sole `computeActionResult` epilogue restores the original byte. The tail is installed before
the entry writer, and an expected-byte mismatch disables the VM-scoped writer.
`BraveOverrideEnabled` conflicts with this scoped ownership and is rejected.

Real-code exact-id routing is **Strong** from current-build disassembly. VM-scoped exact-id avoidance
routing is also **Strong** and remains behind a live vertical-slice gate.

**3. Move-write poke (piggyback).** Move `+0x42` is **Proven as a field** (04 §2.1) and the inventory
(§1.19 "Weight → Move") calls for a cheap live poke to prove it as a **write** for the Weight→Move
design rule. `MovePokeTargetCharId` (−1 off) + `MovePokeValue` (0..32) + `MovePokeMaxWrites`
one-shot-write `+0x42` and log `[MOVE-POKE] unit=.. off=0x42 old=.. new=..`.

**Confidence boundary.** The four five-byte status arrays and direct add/remove are Proven. The native
immunity array is also a Proven input-control surface. Formula-driven per-action status rules,
3d6 resistance, authored duration ownership, and forecast parity are separate integration work; none
of them depends on the retired `+0x1A8/+0x1D0` path.

### DCL miss presentation (`DclMissPresentationEnabled`, LT10-C slice) — built 2026-07-04, awaiting live A/B test

LT9 shipped a functional forced miss that renders as a **"0" damage popup with the hit animation**,
not a "Miss" glyph (the kind hook `0x205B38` never fires under force-connect — see the miss
output-control section). This slice makes the forced-miss branch also author the **presentation**, so
the swing draws a Miss/evade glyph and no number.

**Routing (Strong/static, 2026-07-04; full decode in
`work/1783203631-dcl-miss-presentation-re.md`).** The draw fn `0x2667E0` reads `record+0x1D8`, a 32-bit
"what-to-draw" bitfield, in **mutually-exclusive stages**:

- **bit 2 (mask `0x4`) = damage-number route** — snapshots `word[+0x1C4]` (the staged debit) into the
  damage popup. This is the bit LT9's "0" popup takes.
- **bits `0x10..0x18` = evade/miss-glyph route** — entered **only if the low 16 bits are all clear**
  (`test r8w,r8w / je`). Within it, **bit `0x17` (mask `0x20000`)** reads `byte[+0x1C0]` for the glyph
  kind: `01` → accessory-evade glyph, `02`/`03` → parry/block glyph, else the generic **miss glyph
  `0x22`**. Bit `0x16` is a narrower class-Miss branch (which glyph kind `0x06` yields there is a
  **Hypothesis**).

Neither bit 2 nor the evade bits has a real-code setter (they are VM-set), but `+0x1D8` is **plain
record memory** — a direct read-modify-write works, and the draw loop `btr`-self-clears the bits after
drawing. A **mirror `byte[+0x360]`** of the kind is compared at `0x26D38A`; a stale mirror can mis-gate
a redraw/SFX, so it is kept in sync.

**What the branch does (`record` = the same target unit base the pre-clamp callback already writes:
staged HP debit `+0x1C4`, MP `+0x1C8`).** When `DclMissPresentationEnabled` is on **and** the same
both-hooks output-control gate holds, the forced-miss branch, after zeroing the HP/MP debit:

1. reads old `dword[+0x1D8]` and old `byte[+0x1C0]` (for the log);
2. writes `byte[+0x1C0] = DclMissPresentationKind` (default `6`) and, when `DclMissPresentationMirrorWrite`
   is on (default `true`), the mirror `byte[+0x360]` — this replicates the engine finalizer at
   `0x205B4B` (`mov [rdi+0x360], r12b` on the same record base, alignment-verified), it does not invent
   the write; the toggle lets the RE report's live Test 3 flip the mirror on/off between battles without
   a rebuild;
3. RMWs `dword[+0x1D8]`: **clears the entire draw-bit range 0..24** (`& ~0x01FFFFFF` — the glyph
   route is only entered when the low 16 bits are ALL clear, and the stage-C special popups at bits
   `0x19..0x1D` would also compete; clearing just bit 2 is not enough) and **sets the glyph bit**
   `DclMissPresentationGlyphBit` (default `0x17`) as `| (1 << bit)`, per the RE report's
   recommended write-set.

Every write is try/catch-guarded and ordered kind-first, bitfield-last, so a partial failure leaves at
worst the current LT9 "0" popup and never disturbs the debit-zero already applied. The `+0x1D8` glyph
route is armed **only if the kind byte actually wrote** — on a kind-write failure the bitfield is left
untouched (no glyph route pointing at a stale kind) and the log clause reads `pres=skipped(kind-write-failed)`.

**#1 live unknown — the A/B experiment this slice exists to run.** Whether the VM populates
`+0x1D8`/`+0x1C0` **before** the apply-staging where our pre-clamp hook (`0x30A5D7`) fires — **case A**:
our write survives to the draw fn → "Miss" renders — or **after** — **case B**: the VM clobbers our
write → the number still shows and the hook must move later. The forced-miss `[DCL]` line is extended
with `pres=d8:0xOLD->0xNEW kind:0xOLD->0xNEW` (old→new values) as the A/B evidence: if the on-screen
result still shows a number, the logged post-write values were clobbered later (case B), and the d8
values say where. **Case-B contingency (pre-planned, Strong/static):** move the presentation writes to
a late hook at the action-finalizer tail `0x2059AC` (state `0x2D`), which runs after the VM has
populated the record and before the draw fn consumes it — see the RE report for the site decode. The
glyph-bit (`0x16` vs `0x17`) and kind (`4` vs `6`) mapping are **Hypothesis** — both iterate via
settings between battles without a rebuild.

Settings: `DclMissPresentationEnabled` (false), `DclMissPresentationKind` (`6`, glyph kind byte →
`+0x1C0`/`+0x360`), `DclMissPresentationGlyphBit` (`0x17`, range `0x10..0x18`),
`DclMissPresentationMirrorWrite` (`true`, gates the `+0x360` mirror write for live Test 3). Validator: ERROR if
enabled without `DclMissOutputControlEnabled`; ERROR on kind outside 0..255 or glyph bit outside
`0x10..0x18`; WARN stating the ordering (case A/B) and the glyph-bit/kind mapping are unproven and
iterable via settings.

Test profile: `work/battle-runtime-settings.lt10-counterpath-probe.json` enables
`DclMissPresentationEnabled` (kind 6, glyph bit `0x17`) on the LT9+LT10 stack. PASS (case A) = a forced
miss renders a Miss/evade glyph with no damage number; case B = a "0"/number still shows — record it,
read the `pres=` d8 values, and iterate glyph bit `0x16` vs `0x17` / kind `4` vs `6` via settings edits.

## Preview display control (forecast hit-% AND damage) — ✅ proven live 2026-06-27 / 2026-06-28

The sections above author the **outcome**; this one authors the **forecast numbers** the panel shows
(DCL Layer 1 — display custom hit-% and damage without changing the roll). RE in `05-reverse-engineering.md`
§10: the displayed attack hit-% is copied by real code from a live forecast object (`object+0x2C`,
the computed %) into the static display buffer `0x1407832C0` that the renderer reads; the UI is
retained-mode, so an external write is racy (the engine recomputes on every redraw). The mod wins
deterministically by hooking `0x227FFE` (a clean non-RIP instruction between the load and the store)
`ExecuteFirst` and setting `AX` to the forced value **before** the engine's own store runs — the game
then writes our value at copy time, on the same redraw the renderer draws, with no race.

- **Forecast hit-% paint** (asm hook). Settings: `PreviewHitPctControlEnabled`;
  `PreviewHitPctForcedValue` (`0`–`65535` to force; `-1` = observe only); `PreviewHitPctLogOnly`
  (record the natural % without overwriting); `PreviewHitPctRva` (default `0x227FFE`);
  `PreviewHitPctExpectedBytes` (default `41 BA 02 00 00 00`, validated before activation).
  `DclPreviewHitPctEnabled` activates the formula-driven path: calc-entry mirrors the percentage from
  the same cached DCL decision used for execution into one native int per target, and the asm hook
  maps `rbp = target_unit + 0x1BE` to that slot and substitutes it at copy time. It makes no managed
  call on the UI path. The buffer stores `[0]` fire count, `[4]` last natural %, `[8]` displayed
  override, `[12]` site RVA, then 64 per-target DCL percentages; its address is printed at install.
- **Proven live:** forcing `7` made an Agrias→Ramza preview (true odds 3%) render `7%` on every
  target; memory cross-check showed `fireCount=1`, `lastNatural=3`, `0x7832C0=7`, source `+0x2C=3`.
- **Proven live, formula-driven:** a basic attack rendered the DCL-authored 50% while retaining its
  native 532-damage/KO and Counter forecast. Execution reused the cached decision, rolled 90,
  missed, left the target at 363 HP, and reduced the Counter chance from 69 to 0.
- **Purely visual.** The hook only paints the forecast; `DclHitControlEnabled` owns the roll and
  execution outcome. The shared cached decision keeps the displayed chance and executed roll inputs
  identical.

### Forecast HP preview (number + HP-bar)

The forecast HP amount and the target HP-bar ghost both read staged HP fields from the forecast object
(`obj == target_unit + 0x1BE`):

- damage/debit: `obj+0x6 == unit+0x1C4`, the staged HP-damage;
- healing/credit: `obj+0x8 == unit+0x1C6`, the staged HP-heal.

These are the same fields the pre-clamp (`PreClampDamageRewrite`) rewrites to author the real result
(RE in §11 of `05`). Control the matching field and preview *and* result agree.
Three levers, escalating from robust-but-laggy to clean-but-narrow:

- **`PreviewForecastPoke*` — the universal lever (use this).** A poll-write of the configured forecast
  amount field. Settings: `PreviewForecastPokeEnabled`; `PreviewForecastPokeValue` (staged amount to
  show; `-1` = off); `PreviewForecastDamageFieldOffset` (`0x6` for damage/debit, `0x8` for
  healing/credit). Each poll it derefs the forecast global, validates the pointer lands in the unit
  table, and writes the value (structural RVAs `PreviewForecastGlobalRva` `0x2FF3CF8`,
  `PreviewForecastUnitTableRva` `0x1853CE0`, `…UnitStride` `0x200`, `…ObjOffset` `0x1BE` are
  overridable). Works for **any** action. Because the panel is retained-mode (drawn once per open,
  §11), the value shows on the **(re)open** of the preview, not while it is held. Logs the first few
  writes as `[FORECAST-POKE] obj=… unitIdx=… wrote unit+0x1C4=…` or `unit+0x1C6=…`.
- **`PreviewForecastSource*` — compute-time finalizer hooks (first-open clean).** Force the
  `obj+0x6` store as the engine computes it, so the number+bar are right on the *first* open. Settings:
  `PreviewForecastSourceControlEnabled`; `PreviewForecastSourceForcedValue` (`-1` = observe);
  `PreviewForecastSourceLogOnly`. Hooks a fixed site list — `0x30637E` (magic, confirmed), `0x308D8F`
  (physical candidate), `0x307DC4`, `0x309664` — with per-site fire counters in a buffer
  (`[PREVIEW-SOURCE-SUMMARY] buf=0x…`). Per-formula coverage, so pair it with the poke as the catch-all.
- **`PreviewDamage*` — display-number paint (cosmetic, NUMBER only).** Paints display buffer
  `0x1407832BE` via the forecast-number dispatch (all numeric branches). Settings:
  `PreviewDamageControlEnabled`; `PreviewDamageForcedValue` (`-1` = observe); `PreviewDamageLogOnly`.
  ⚠️ **Paints the number only — the HP-bar reads `obj+0x6`, not this buffer**, so used alone it
  reproduces the "shows 500 but the bar says 184" mismatch. Layer it on the poke as a number guarantee.

**Coherent recipe (proven):** `PreviewForecastPoke` (+ optional `PreviewForecastSource`/`PreviewDamage`)
authors the preview number+bar, and `PreClampDamageRewrite` at the **correct** RVA `0x30A5D7`
(decimal `3188183` — *not* `3188695`=`0x30A7D7`, which silently SKIPs) authors the result; feed both
the same value and preview == result. For damage, poke `obj+0x6` and force `word[+0x1C4]`; for healing,
poke `obj+0x8` and force `word[+0x1C6]`. Healing preview uses `+0x1C4=0`, `+0x1C6=heal`, and
`+0x1E5=0x40`; the ghost refill clamps at MaxHP.

Healing formulas need stricter context gating than damage proofs. Regen-style passive HP credit and
explicit action healing both arrive as `event.isHealing` over `+0x1C6`; shipping profiles should gate
on fresh actor/action context or action identity, not only `event.isHealing`. `PreviewForecastPoke` is
a proof lever until it is armed per selected action/target.

Delayed explicit heals should prefer pending-action context with `action.sourcePending` and
`action.creditCacheMatch`; the tracker matches the staged `+0x1C6` credit the same way damage uses the
`+0x1C4` debit. Formula-backed healing profiles should also require the target cache to still be in
phase zero before queueing a pre-clamp plan, so the authored result-phase credit is not interpreted as
a fresh formula candidate.

## What this architecture provides

- arbitrary math in C#;
- config-defined target-side and attacker-aware integer formulas;
- action-family variables through `ActionSignalRules` (until true ability/action ids are mapped);
- GURPS-like swing/thrust damage tables;
- armor DR as true damage reduction;
- C-bounded percent/type armor responses for `swing` / `thrust` / `crush` / `missile` matchups;
- per-weapon/per-skill damage types;
- custom global damage scaling and ally/enemy rules;
- proven hit↔miss / parry / block control by BOTH input-control (write the defender's live evade bytes
  before the VM roll — Denuvo virtualizes code, not data) and output-control (two native hooks);
- proven status control (`StatusOverride*`, live-confirmed Undead), reaction control (`BraveOverride*`,
  Brave%-gate), MP control (combined pre-clamp `+0x1C8`/`+0x1CA`), and **full forecast display control —
  hit-% (`PreviewHitPct*`) plus HP amount number + HP-bar ghost (`PreviewForecastPoke*`) for damage,
  magic, and healing actions, coherent with the applied result** — each a working lever, with per-action
  formula-driven arming as the remaining engineering step;
- a foundation for AI scoring on the custom formula and richer preview/animation patches.

Follow-up layers not owned by the config/DSL: AI scoring based on the custom formula, full
KO/crystal/reaction-status lifecycle, per-action arming of the control levers (the preview poke and
pre-clamp currently take a static value; the shipping form feeds them the DCL formula's
(attacker, target) result), perfect ability-id capture, magic always-hit (the separate Faith
avoidance roll, §9/§11 of `05`), and multi-hit/reaction chains.

## Sources

- `04-engine-memory-model.md` (engine/memory facts), `05-reverse-engineering.md`,
  `03-battle-data-map.md`
- `codemod/fftivc.generic.chronicle.codemod/Mod.cs`
- Nenkai FFT mod loader: https://github.com/Nenkai/fftivc.utility.modloader
- Reloaded-II hook docs: https://reloaded-project.github.io/Reloaded-II/CheatSheet/CallingHookingGameFunctions/
- Reloaded.Hooks asm hook docs: https://reloaded-project.github.io/Reloaded.Hooks/AssemblyHooks/
