# Battle Data Map — Every Editable Variable And Its Limits

A complete reference map of the battle system's accessible variables and the hard limits around
them, organized by domain (abilities, jobs, items, status, commands, encounters). This file is the
owner of the engine's **enum vocabularies**, the **per-domain field catalog**, the **per-action
result/outcome surface**, and the **hard-limit / "needs code" list**.

Background not repeated here:
- The four data layers and how they stack — see `00-overview.md`.
- The concrete edit workflow (unpack/edit/deploy each surface) and the JP-decode formula — see
  `01-data-editing-surfaces.md`.
- The Formula-id catalog (which routine each id `0x00`–`0x64` computes) — see
  `02-formula-id-catalog.md`.
- The live runtime battle-unit struct offsets (HP `+0x30`, PA `+0x3E`, CT `+0x41`, death bit
  `+0x61`, equipment `+0x1A`..`+0x26`, raw stats `+0x38`..`+0x3A`, growth block `+0x8A`..`+0x93`,
  the battle-actor array, and the damage-hook anchors) — owned by `04-engine-memory-model.md`.

Each domain below names the data surface, the editable fields, and the caps that bound them.

---

## A. Per-action RESULT / OUTCOME fields (the hit/miss/block/parry surface)

The per-action result/outcome control surface — the hit/phase marker (`+0x1BB`), the staged-result
flag (`+0x1BE`), the **evade-type enum** (`+0x1C0`: `0x00` hit / `0x01` cloak / `0x02` weapon-parry /
`0x03` shield / `0x04` class-evade / `0x06` miss / `0x0B` Blade Grasp), the staged HP/MP words
(`+0x1C4`/`+0x1C6` HP, `+0x1C8`/`+0x1CA` MP), and the `resultKind` bits (`+0x1E5`) — lives in the
live unit struct and is owned by `04-engine-memory-model.md` §2.3. It is a writable lever that drives
hit-vs-evade and the on-screen animation: the accuracy roll is virtualized, but its outcome lands in
these bytes (output-control). It is now ALSO controllable from the **input** side — writing the
defender's evade bytes before the roll is the ✅ proven primary path; this output surface is the
fallback (see `04` §2.1). See `04` §2.3 for the byte table and enum values, and
`05-reverse-engineering.md` for the hook anchors and the force-hit / force-evade control recipe.

---

## B. Per-ability ACTION variables (the formula surface)

`OverrideAbilityActionData` (Nex, `0004.pac`). Sparse override: `-1` = inherit the exe base,
`>=0` = override (cast to byte). 368 rows; base Formula/X/Y are exe-hardcoded.

```text
Flags12[]      patches AbilityFlags1, AbilityFlags2     (physical/magical, evadable, ...)
Flags34[]      patches AbilityFlags3, AbilityFlags4
Range          targeting range
EffectArea     area of effect radius
Vertical       vertical tolerance
Element        element (see enum)
Formula        which hardcoded routine (id 0x00-0x64; see 02-formula-id-catalog.md)
X, Y           the routine's two parameters (byte each)
InflictStatus  status applied (see enum)
CT             charge time
MPCost         MP cost
```

Ability metadata (`AbilityData.xml`, 512 ids): `JPCost`, `ChanceToLearn`, `Flags`, `AbilityType`,
`AIBehaviorFlags`. Text/JP (`ability.en.nxd`, 512 rows): `Name`, `Description`, `IconId`,
`JpCost1/2` (JP = JpCost1 + 256*JpCost2), `IsRandomDamage`, `IsRandomStatus`.

Limits: ability id <= 511; AbilityType id <= 453; X and Y are single bytes (0-255); only two
numeric parameters per ability; the formula *routines* themselves are exe code (Tier 2 to change).

---

## C. Per-job variables (the unit-build surface)

`JobData.xml` (174 jobs). Everything that defines a class:

```text
Stat curves:  HPGrowth HPMultiplier  MPGrowth MPMultiplier  SpeedGrowth SpeedMultiplier
              PAGrowth PAMultiplier   MAGrowth MAMultiplier
              (DisplayedStat = RawStat * Multiplier / 1638400; Growth drives level-up)
Mobility:     Move  Jump  CharacterEvasion
Command:      JobCommandId   InnateAbilityId1-4
Status:       InnateStatus  ImmuneStatus  StartingStatus
Elements:     AbsorbElements  NullifyElements  HalveElements  WeakElements
Equipment:    EquippableItems (34 type vocab)
Monster:      MonsterPortrait  MonsterPalette  MonsterGraphic
```

Job tree / unlock: `GeneralJob` (Nex) — `RequiredJobExp[8]`, `RequiredJobIds[]`,
`RequiredJobLevels[]`, `RequiredJobPositions[]`. `JobNeedLevelData.xml` — per-job level gates by
class name (Squire..OnionKnight). Text: `job.en.nxd` (Name/Description).

Limits: job id <= 175. Growth/Multiplier are bytes (lower Growth = faster growth in classic math).
Equippable is a fixed 34-type vocabulary (no inventing new equip slots in data).

---

## D. Item / weapon / armor variables

```text
ItemData.xml        (261)  Palette SpriteID RequiredLevel TypeFlags AdditionalDataId
                           ItemCategory EquipBonusId Price ShopAvailability
ItemWeaponData.xml  (128)  Range AttackFlags Formula(1-7) Power(0-40) Evasion Elements OptionsAbilityId
ItemArmorData.xml   (64)   HPBonus MPBonus
ItemShieldData.xml  (16)   PhysicalEvasion MagicalEvasion
ItemAccessoryData.xml(32)  PhysicalEvasion MagicalEvasion
ItemEquipBonusData.xml(85) PABonus MABonus SpeedBonus MoveBonus JumpBonus
                           InnateStatus ImmuneStatus StartingStatus
                           Absorb/Nullify/Halve/Weak/StrongElements  BoostJP
ItemConsumableData.xml(14) Formula Z StatusEffectId
ItemOptionsData.xml (128)  OptionType(AllOrNothing/Cancel/Random/Separate) Effects
ItemShopsData.xml   (256)  Shops (availability)
```

Note `EquipBonus` has `StrongElements` and `BoostJP` — levers jobs don't expose. Weapon `Formula`
is its own small set (1-7), separate from the ability formula catalog.

Limits: global item id <= 260; secondary weapon data id <= 127; secondary armor data id <= 63;
shield <= 15; accessory <= 31; weapon Power <= 40.

Runtime keying note (for code-mod DR rules): the global `item_id` is the runtime equipment key. The
secondary ids (`AdditionalDataId`) are **not** globally unique across the weapon/armor/shield/
accessory tables, so a raw runtime candidate matching `item_id` is much stronger evidence than one
matching only a secondary id. Code-mod settings can match DR rules by `ItemId`, `ItemCategory`,
`TypeFlag`, `SecondaryKind`, and simple item-stat thresholds rather than hardcoding each item.

---

## E. Status effects

`StatusEffectData.xml` (40 statuses): `Order` (UI sort), `Counter` (duration ticks), `CheckFlags`,
`CancelFlags`, `NoStackFlags`. Text/icon: `uistatuseffect.en.nxd`.

The full status vocabulary (exact tokens from the `ImmuneStatus` enum used in JobData/EquipBonus,
38 named + 2 system slots = the 40-slot table):

```text
KO Critical Undead Crystal Chest
Poison Oil Float Reraise Invisible Regen Protect Shell Haste Slow Stop
Charging Jump Charm Toad Doom Vampire Faith Atheist Sleep Disable Immobilize
Confuse Blind Silence Berserk Reflect Defending Performing Stone Traitor Chicken
(+ Unused1, and one more system slot to fill 40)
```

Limits: status id <= 39 (fixed 40-slot table). `Counter` sets duration; `CheckFlags`/`CancelFlags`
control interactions (e.g. FreezeCT, PoisonRegen, mutually-cancelling sets).

**Proven static:** the modloader template serializes byte-for-byte to the unique 640-byte
`StatusEffectData` table embedded in Enhanced. `StatusEffectData.Id` is a zero-based table index,
while `StatusEffectType` is one-based; the owning relation is
`table_index = StatusEffectType - 1`. Row 24 is therefore Poison and row 39 is Doom. Comments that
label a template row with the same numeric `StatusEffectType` value are displaced by one and are not
row authority. The exact nonzero native counters are Poison 36, Regen 36, Protect 32, Shell 32,
Haste 32, Slow 24, Stop 20, Empty_32 24, Faith 32, Atheist 32, Charmed 32, Sleep 60,
Immobilize 24, Disable 24, Reflect 32, and Doom 3.

Runtime side (not data): the live status bytes — master `+0x1EF`, effective mirror `+0x61`
(`= (+0x1EF & 0xF2) | +0x57`), innate `+0x57` — are DATA-controllable at runtime and owned by
`04-engine-memory-model.md` §2.3 (live-confirmed bits: `0x20`=KO, `0x10`=Undead).

---

## F. Commands, skillsets, spawns, traps

```text
JobCommandData.xml (176)  16x AbilityId + 6x ReactionSupportMovementId per command  (= a skillset)
                          + ExtendAbilityIdFlagBits / ExtendRSMIdFlagBits for extra slots
MonsterJobCommandData(48) 4x AbilityId (monster skill sets)
CommandTypeData.xml (256) Menu (command category behavior: Item/Throw/Jump/...)
SpawnData.xml (4)         generic spawn template: HP MP Speed PA MA + 7 equipment slots
SpawnVarianceData.xml(4)  randomized variance on HP/MP/Speed/PA/MA
MapTrapFormationData(128) 4x (X,Y,TrapFlags,RareItemId,CommonItemId) per map
```

ENTD (`fftpack/battle_entd1-4_ent.bin`): per-encounter unit roster — sprite, flags, name id,
**level** (1-99 fixed, 100+ = party-relative), bravery, faith, job, job level, secondary skillset,
R/S/M abilities, equipment, position.

---

## G. Enumerations (the full vocabularies)

```text
Elements (8):        Fire Ice Lightning Wind Earth Water Holy Dark
EquippableItems (34):Unarmed Knife NinjaBlade Sword KnightSword FellSword Katana Axe Flail
                     Rod Staff Pole Polearm Crossbow Bow Gun Book Instrument Bag
                     Cloth Shield  Helmet Hat HairAdornment  Armor Clothing Robe
                     Shoes Armguard Armlet Cloak Ring Perfume LipRouge
WeaponAttackFlags(8):Striking Lunging Direct Arc Throwable TwoHands ForcedTwoHands TwoSwords
AbilityType (9):     Normal Item Throwing Jumping Math Aim Reaction Support Movement
Ability.Flags (3):   DisplayAbilityName DontLearnWithJP LearnOnHit
AIBehaviorFlags(27): TargetEnemies TargetAllies TargetMap OnlyHitsEnemies OnlyHitsAlliesOrSelf
                     HP MP Stats Silence AddStatus CancelStatus Unequip UndeadReverse
                     PhysicalAttack MagicalAttack Melee3Directions Ranged3Directions LinearAttack
                     NonSpearAttack StopAtObstacle Evadeable EvadeWithMotion Reflectable
                     RandomHits CheckCT_Target AffectedByFaith UsableByAI
ThrowItemType (12):  Knife NinjaBlade Sword KnightSword Katana Axe Flail Pole Polearm Book Bomb Throwing
ItemTypeFlags (6):   Weapon Shield Armor Headgear Accessory Rare
ItemOptionType (4):  AllOrNothing Cancel Random Separate
StatusEffect CheckFlags (15): KO CantReact FreezeCT IgnoreAttacks PoisonRegen CrystalTreasure
                     DefendPerform ConfusionTransparentCharmSleep + Check1/2/8/9/10/11/12
Status (~40):        see section E
```

---

## H. Hard limits & what needs code (Tier 2)

```text
Table-size caps (hardcoded):  Ability 512 | AbilityType 454 | AbilityMath 422 | Job 176
                              Weapon 128 | Armor 64 | Shield 16 | Accessory 32 | Item 261
                              Status 40 | ItemOptions 128 | JobCommand 176 | CommandType 256
Numeric:  ability X/Y = 1 byte each; stats = bytes; HP/MP/damage = 16-bit; integer math only.
Fixed vocab: elements (8), equip types (34), status (40) - cannot add new ones in data.
Per-ability params: only Formula + X + Y + Element + Status (no third scalar) in data.

Needs a Tier-2 code hook (no data path):
  - new damage math / new scaling variable / >2 parameters per ability
  - changing what a Formula id computes, or adding formula ids
  - changing global modifiers (Zodiac, Faith ratio, Protect/Shell 2/3, crit, variance)
  - the weapon-type -> XA mapping; the 1638400 stat divisor
  - reading/writing live unit state mid-action beyond what a formula already uses
```

Engine math is integer: the remaster applies some multipliers as AVX floats, then truncates to int.

To reach each surface (unpack/edit/deploy steps for Nex/NXD, TableData XML, ENTD, and the runtime
hook), see `01-data-editing-surfaces.md`.
