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
for a resolved action comes from the calc-entry order record (`05` — hook `0x309A44`,
record bytes `[2..3]`). The catalog is loaded, hot-reloaded, and exposed on
`BattleFormulaEngine`; injection into formula contexts is the next construction step (the DCL
decision pipeline).

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

The code mod watches `battle-runtime-settings.json` and the configured `ItemCatalogPath` and
`AbilityCatalogPath` during the polling loop; changes are picked up about once per second, so
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

- **Pre-clamp staged-debit hook** (`0x30A66F`) — forces/zeros the staged HP debit before vanilla
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

## Action-context and roll probes (LT3, 2026-07-02) — the DCL context spine

Four probe/control surfaces added for the LT3 campaign (`work/lt3-calc-rng-results.md`):

- **Calc-entry probe** (`CalcEntryProbeEnabled`, `CalcEntryProbeRva` = `0x309A44`). ✅ PROVEN live.
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
  (`0x30BE8B`, `chance=61` = Brave).
- **Magic-accuracy / status-chance hooks** (`MagicAccuracyControl*` @ `0x304E2E`,
  `StatusChanceControl*` @ `0x306633`, ForcedChance −1/0..100). Installed and byte-validated but
  ❌ **0 fires for Fire/Blind** — those real-code handlers serve other formula ids; do NOT rely on
  them for the standard spells. Kept for coverage of the handlers that do route through them.
- **Staged-bundle output lever** (`StagedBundleProbe*` @ `0x281F8A`, the sweep post-call). ✅ PROVEN
  LT4 (2026-07-02). Reads/writes the target's staged effect bundle at the compute point, before
  apply: `Force*` (gated on `StagedBundleForceTargetCharId`) overwrite `+0x1C0` kind, `+0x1C4` dmg,
  `+0x1A8` ailment, `+0x1D0` apply-mask, `+0x1E5` result-flag. Forcing `+0x1C4=111` on a Fire target
  made it take exactly 111 (natural 78/138) → **the compute-point write leaks to the applied
  result**, a second damage-output lever alongside the pre-clamp, per-(action,target) and shared by
  physical/magic/AI (same sweep). The status apply-mask/ailment are NOT staged here for a hit-Blind
  (that path is elsewhere); status control stays on the proven input levers.

## DCL pre-clamp pipeline (`DclPipelineEnabled`) — ✅ proven live 2026-07-04 (LT6)

The first end-to-end DCL delivery: a **one-switch pipeline** that computes a config-authored
damage formula from the full (attacker, target, equipment, ability) context and rewrites the
staged debit **same-hit**, inside the pre-clamp hook. It wires together the two proven anchors:

1. **Calc-entry probe** (`0x309A44`, auto-installed by the switch) — the asm stub rings
   `(order-record ptr, target idx, packed casterIdx/type/abilityId)` per (action, target). The
   slot is fully written **before** the ring count is published (x86 TSO), so a consumer never
   reads a torn slot. Both the poller and the DCL callback drain the ring (shared gate, separate
   cursors) into `DclActionContextCache` — latest `(casterIdx, actionType, abilityId)` per target
   index, timestamped at drain (bounded by the poll cadence; `DclActionContextMaxAgeMs` guards on
   top, default 5000 ms).
2. **Pre-clamp managed callback** (`0x30A66F`, LT4-proven ABI) — on each staged damage apply, the
   C# callback: resolves the target's unit-table index (base+`0x1853CE0`, stride `0x200`), looks
   up the cached action context, snapshots **attacker and target** unit structs, builds a
   `FormulaContext` via `FormulaRuntimeContextBuilder` (unit vars incl. raw growths/zodiac/gender,
   `EquipmentSlots`/`AttackerEquipmentSlots` item vars from `ItemCatalog`, ability vars from
   `AbilityCatalog`, settings vars/tables/matrices/maps, `dcl.oldDebit`/`dcl.oldCredit`,
   `action.type`/`action.abilityId`), evaluates **`DclDamageFormula`**, and returns the clamped
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
  attacks do not pass through calc-entry `0x309A44`** (or their context ages differently); they
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
**managed decision callback at calc-entry `0x309A44`** — the same reverse-wrapper pattern as the
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
looks up a **decision cache** keyed `(casterIdx, targetIdx, abilityId, actionType)` with TTL
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
left untouched to avoid racing the imminent VM read. A successful pre-clamp DCL damage rewrite
invalidates the matching decision cache entry, so the next real swing, including dual-wield swing 2,
rolls a fresh hit decision.

**Known V1 limitation:** only a HIT is consumption-invalidated (a forced MISS stages no damage, so
no pre-clamp rewrite fires). A repeat of the same `(caster, target, ability, type)` within the TTL
window after a MISS reuses that MISS (logged with `cached=1`) instead of rolling independently —
per-swing UI-vs-log consistency is unaffected. Closing this needs a proven miss-consumption signal
(new RE); deferred until after LT8 validates delivery — **built 2026-07-04**: the `0x205B38`
result-kind commit hook is that signal, active under `DclMissOutputControlEnabled` (see the miss
output-control subsection below). Invalidation may also clear a newer same-key
decision recorded between roll and damage apply — the cost is one extra re-roll, never a wrong
outcome.

Settings: `DclHitControlEnabled` (default false), `DclHitChanceFormula` (validated at deploy time
against the synthetic DCL hit context, after the derived chain — unknown variables and zero-debit
errors are deploy errors), `DclHitDecisionTtlMs` (2500), `DclHitForcedRoll` (-1 = real RNG,
0..99 = deterministic roll for live tests), `DclHitMaxLogs` (400, a separate budget from
`DclDecisionMaxLogs`),
`DclMissClassEvadeValue` (100). The RNG is seeded once at install and the seed logged
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

Known scope limits (as-built, before LT8):
- **Reactions/counters are untouched** — they bypass calc-entry `0x309A44` entirely (observed
  LT6/LT7: counterattack hits log `[DCL-MISS] no-calc-entry` on the damage path). Under the
  baseline stack they behave as force-hit vanilla.
- **Spells are not gated in LT8** — the test formula forces `pct=100` for `action.type != 1`;
  magic avoidance authoring is future work on the same mechanism.
- **The preview % display stays vanilla** — the forecast panel is a separate, already-proven
  display lever (`0x227FFE`), not yet wired to `DclHitChanceFormula`.

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
   presentation needs its own RE slice (LT5-C territory). Consequence B: the kind-side consumption
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
   (Strong/static-proven, `04-engine-memory-model.md` §2.3; apply at `0x30A51C` computes
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
   harmless; flipping the kind (and the Miss presentation generally) needs its own RE slice. ExecuteFirst shim (pre-clamp-style mid-function ABI: the function
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
the plain invalidate-on-rewrite (LT8 semantics unchanged). The fire order of `0x30A66F` vs
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
