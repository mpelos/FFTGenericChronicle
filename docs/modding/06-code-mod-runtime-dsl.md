# Code-Mod Runtime: Config & Formula DSL

> Scope: **04-engine-memory-model.md** owns engine/memory facts (unit-struct offsets, the stable
> hook anchor, death/clamp/KO engine model, CT attacker resolution, equipment offsets).
> **This file (06)** owns the code-mod's runtime-settings configuration language and the C#
> formula DSL: operators, functions, variables, and every rule schema.
>
> Related: `05-reverse-engineering.md` (anchors / Denuvo), `03-battle-data-map.md` (editable
> surfaces / enums).

The custom battle math lives in a Reloaded-II C# runtime. Rather than replacing the
Denuvo-virtualized vanilla damage routine, the code mod **owns the final battle result** and
drives all custom mechanics through a hot-reloadable JSON settings file
(`battle-runtime-settings.json`) plus an embedded C# expression engine.

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
  current code has an observe-only resolver for this path (`PreClampResolveActorContext`, logging
  `[PRECLAMP-ACTOR-CTX]`) and uses CT reset / counter-inversion only as fallback and diagnostic
  provenance. Formula context exposes `a.present`, `a.ct`, `a.sourceCt`, `a.sourceCounter`, with
  target equivalents; `sourceCt` means "this attacker came from the CT fallback", not "CT is the
  primary model".
- **action family**: bridged through `ActionSignalRules`, which decode controlled vanilla deltas
  into `action.*` variables (`action.swing`, `action.thrust`, `action.cut`, `action.spell`, etc.).
- **equipment ids**: read from fixed offsets or discovered through catalog scan slots, joined
  against the static item catalog.

`a.sourceRecent` (recent-unit heuristic) remains a legacy/debug fallback, not the primary
attacker model. `InferAttackerFromRecentUnits` (off by default) gates whether the heuristic is
passed into formulas; when off, the runtime only logs `[CTX]` candidates.

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
               job zodiac genderFlags isMale isFemale isMonster maxBrave maxFaith
               rawPa rawMa rawSpeed                       # base stats before equipment
               weaponAtk weaponAtkL weaponParry weaponParryL shieldPhysParry shieldMagParry physEva
               hpGrowth hpMult mpGrowth mpMult spdGrowth spdMult paGrowth paMult maGrowth maMult
             attacker.* or a.*:
               present inferred sourceRecent
               charId level hp maxHp mp maxMp team isFoe isAlly pa ma speed move jump brave faith
               job zodiac genderFlags isMale isFemale isMonster maxBrave maxFaith
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

### Attacker context and fallback provenance settings

The primary RE model for attacker/action context is the pre-clamp actor array (`actor+0x148 -> unit`,
caster `actor+0x142 -> action id`), owned by `04-engine-memory-model.md`. In the current runtime
surface, that path is still exposed as an observe-only probe:

```json
{
  "PreClampResolveActorContext": true,
  "PreClampActorActionIdOffset": 322,
  "PreClampActorContextMaxLogs": 64
}
```

It emits `[PRECLAMP-ACTOR-CTX]` for head-to-head validation against the pending tracker and fallback
resolvers. CT is not the design-primary attacker source anymore; keep it enabled only as a fallback
and diagnostic signal while the actor-context resolver is promoted into formula context. Relevant
fallback settings:

```json
{
  "ResolveAttackerByCt": true,
  "CtDropWindowMs": 4000,
  "ResolveCounterFromRecentDamage": true,
  "CounterEventWindowMs": 1500
}
```

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
weaponPower weaponFormula weaponEvasion
armorHpBonus armorMpBonus
shieldPhysicalEvasion shieldMagicalEvasion
accessoryPhysicalEvasion accessoryMagicalEvasion
bonusPa bonusMa bonusSpeed bonusMove bonusJump
isWeapon isArmor isShield isAccessory
category_<normalizedItemCategory>
type_<normalizedTypeFlag>
```

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

The code mod watches `battle-runtime-settings.json` and the configured `ItemCatalogPath` during
the polling loop; changes are picked up about once per second, so formula constants, tables, DR
rules, action signals, and catalog data can be iterated without rebuilding the DLL. If a settings
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

- **Pre-clamp staged-debit hook** (`0x30A66F`) — forces/zeros the staged HP debit before vanilla
  applies it. Settings: `PreClampDamageRewriteEnabled`; `PreClampDamageRewriteForcedDebit` /
  `…ForcedCredit` (`0` = zero the damage, `-1` = leave); `PreClampDamageRewriteTargetCharId`
  (`-1` = any target); `PreClampDamageRewriteMinHp` / `…MaxHp` (HP-range guard, default `1`/`9999`);
  `PreClampDamageRewriteMaxWrites`; `PreClampDamageRewriteLogOnly`.
- **Result/animation selector hook** (`0x205210`) — sets the evade-type render. Settings:
  `ResultSelectorControlEnabled`; `ResultSelectorControlForceEvadeType` (enum per 05 §4: `0x04`
  class-evade / `0x03` shield / `0x02` weapon / `0x01` cloak / `0x06` miss / `0x00` hit);
  `ResultSelectorControlForceResultCode` (`+0x1BE`: `0` = evade path); `ResultSelectorControlMatchEvadeType`
  (only act on a record currently showing this type; `0` = a natural hit); `ResultSelectorControlTargetCharId`;
  `ResultSelectorControlMaxWrites`; `ResultSelectorControlLogOnly`. The probe side
  (`ResultSelectorProbe*`, RVA `2118160`) logs every selector pass.

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
  ⚠️ **Never write Brave < 10** — the engine flips the unit to chicken/panic at `0x30A9BD`.
- **MP** — same mechanism as HP via the combined pre-clamp hook (`0x30A66F`, §"Outcome control hooks"):
  the staged MP words are `+0x1C8` (debit) / `+0x1CA` (credit), applied as `newMP = clamp(MP + 0x1CA -
  0x1C8)`. Force them exactly like the HP debit/credit.

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
  `PreviewHitPctExpectedBytes` (default `41 BA 02 00 00 00`, validated before activation). The hook
  writes a 16-byte record (`[0]` fire count, `[4]` last natural %, `[8]` forced (`-1`=logOnly),
  `[12]` site RVA) whose address is printed at install as `[PREVIEW-HITPCT-HOOK] … buf=0x…`, so the
  result is verifiable from outside the process (`work/read_hitpct_hook.py`).
- **Proven live:** forcing `7` made an Agrias→Ramza preview (true odds 3%) render `7%` on every
  target; memory cross-check showed `fireCount=1`, `lastNatural=3`, `0x7832C0=7`, source `+0x2C=3`.
- **Purely visual.** The actual hit roll is computed independently in the VM at execution time;
  this only paints the forecast. The shipping form will feed `PreviewHitPctForcedValue` from the DCL
  formula's (attacker, defender) hit result so the preview matches the real authored odds.

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
authors the preview number+bar, and `PreClampDamageRewrite` at the **correct** RVA `0x30A66F`
(decimal `3188335` — *not* `3188847`=`0x30A86F`, which silently SKIPs) authors the result; feed both
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
