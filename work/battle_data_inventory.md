# TableData XML surfaces (hardcoded tables, edited via FFTIVC/tables)

## AbilityActionData.xml

_(no Entries - see header / pointer table)_

## AbilityChargeAimData.xml  (AbilityChargeAim x 8)

Fields: Id, Ticks, Power


## AbilityData.xml  (Ability x 512)

Fields: Id, JPCost, ChanceToLearn, Flags, AbilityType, AIBehaviorFlags

- `Flags` vocab (3): DisplayAbilityName, DontLearnWithJP, LearnOnHit
- `AbilityType` vocab (9): Aim, Item, Jumping, Math, Movement, Normal, Reaction, Support, Throwing
- `AIBehaviorFlags` vocab (27): AddStatus, AffectedByFaith, CancelStatus, CheckCT_Target, EvadeWithMotion, Evadeable, HP, LinearAttack, MP, MagicalAttack, Melee3Directions, NonSpearAttack, OnlyHitsAlliesOrSelf, OnlyHitsEnemies, PhysicalAttack, RandomHits, Ranged3Directions, Reflectable, Silence, Stats, StopAtObstacle, TargetAllies, TargetEnemies, TargetMap, UndeadReverse, Unequip, UsableByAI

## AbilityEffectNumberFilterData.xml  (AbilityEffectNumberFilter x 454)

Fields: Id, EffectId


## AbilityJumpData.xml  (AbilityJump x 12)

Fields: Id, Range, Vertical


## AbilityMathData.xml  (AbilityMath x 8)

Fields: Id, AbilityType

- `AbilityType` vocab (8): CT, Experience, Height, Level, MultipleOf3, MultipleOf4, MultipleOf5, Prime

## AbilityThrowData.xml  (AbilityThrow x 12)

Fields: Id, ItemType

- `ItemType` vocab (12): Axe, Bomb, Book, Flail, Katana, Knife, KnightSword, NinjaBlade, Pole, Polearm, Sword, Throwing

## AbilityTypeData.xml  (AbilityType x 454)

Fields: Id, ChargeEffectType, AnimationId, BattleTextId


## CommandTypeData.xml  (CommandType x 256)

Fields: Id, Menu


## ItemAccessoryData.xml  (ItemAccessory x 32)

Fields: Id, PhysicalEvasion, MagicalEvasion


## ItemArmorData.xml  (ItemArmor x 64)

Fields: Id, HPBonus, MPBonus


## ItemCategoryToDataTypeData.xml  (ItemCategoryToDataType x 14)

Fields: Id, DataTypeId


## ItemConsumableData.xml  (ItemConsumable x 14)

Fields: Id, Formula, Z, StatusEffectId


## ItemData.xml  (Item x 261)

Fields: Id, Palette, SpriteID, RequiredLevel, TypeFlags, AdditionalDataId, ItemCategory, Unused_0x06, EquipBonusId, Price, ShopAvailability, Unused_0x0B

- `TypeFlags` vocab (6): Accessory, Armor, Headgear, Rare, Shield, Weapon

## ItemDataTypeToItemIdRangeData.xml  (ItemDataTypeToItemIdRange x 14)

Fields: Id, ItemIdRangeId


## ItemEquipBonusData.xml  (ItemEquipBonus x 85)

Fields: Id, PABonus, MABonus, SpeedBonus, MoveBonus, JumpBonus, InnateStatus, ImmuneStatus, StartingStatus, AbsorbElements, NullifyElements, HalveElements, WeakElements, StrongElements, BoostJP

- `InnateStatus` vocab (9): Faith, Float, Haste, Protect, Reflect, Regen, Reraise, Shell, Undead
- `ImmuneStatus` vocab (18): Berserk, Blind, Charm, Confuse, Disable, Doom, Immobilize, KO, Poison, Silence, Sleep, Slow, Stone, Stop, Toad, Traitor, Undead, Vampire
- `StartingStatus` vocab (3): Invisible, Reraise, Stone
- `AbsorbElements` vocab (4): Earth, Fire, Holy, Ice
- `NullifyElements` vocab (1): Lightning
- `HalveElements` vocab (4): Dark, Fire, Ice, Lightning
- `WeakElements` vocab (2): Lightning, Water
- `StrongElements` vocab (8): Dark, Earth, Fire, Holy, Ice, Lightning, Water, Wind

## ItemIdRangeToCategoryData.xml  (ItemIdRangeToCategory x 14)

Fields: Id, StartItemId


## ItemOptionsData.xml  (ItemOptions x 128)

Fields: Id, OptionType, Effects

- `OptionType` vocab (4): AllOrNothing, Cancel, Random, Separate

## ItemShieldData.xml  (ItemShield x 16)

Fields: Id, PhysicalEvasion, MagicalEvasion


## ItemShopsData.xml  (ItemShops x 256)

Fields: Id, Shops


## ItemWeaponData.xml  (ItemWeapon x 128)

Fields: Id, Range, AttackFlags, Formula, Unused_0x03, Power, Evasion, Elements, OptionsAbilityId

- `AttackFlags` vocab (8): Arc, Direct, ForcedTwoHands, Lunging, Striking, Throwable, TwoHands, TwoSwords
- `Elements` vocab (6): Fire, Holy, Ice, Lightning, Water, Wind

## JobCommandData.xml  (JobCommand x 176)

Fields: Id, ExtendAbilityIdFlagBits, ExtendReactionSupportMovementIdFlagBits, AbilityId1, AbilityId2, AbilityId3, AbilityId4, AbilityId5, AbilityId6, AbilityId7, AbilityId8, AbilityId9, AbilityId10, AbilityId11, AbilityId12, AbilityId13, AbilityId14, AbilityId15, AbilityId16, ReactionSupportMovementId1, ReactionSupportMovementId2, ReactionSupportMovementId3, ReactionSupportMovementId4, ReactionSupportMovementId5, ReactionSupportMovementId6


## JobData.xml  (Job x 174)

Fields: Id, JobCommandId, InnateAbilityId1, InnateAbilityId2, InnateAbilityId3, InnateAbilityId4, EquippableItems, HPGrowth, HPMultiplier, MPGrowth, MPMultiplier, SpeedGrowth, SpeedMultiplier, PAGrowth, PAMultiplier, MAGrowth, MAMultiplier, Move, Jump, CharacterEvasion, InnateStatus, ImmuneStatus, StartingStatus, AbsorbElements, NullifyElements, HalveElements, WeakElements, MonsterPortrait, MonsterPalette, MonsterGraphic

- `EquippableItems` vocab (34): Armguard, Armlet, Armor, Axe, Bag, Book, Bow, Cloak, Cloth, Clothing, Crossbow, FellSword, Flail, Gun, HairAdornment, Hat, Helmet, Instrument, Katana, Knife, KnightSword, LipRouge, NinjaBlade, Perfume, Pole, Polearm, Ring, Robe, Rod, Shield, Shoes, Staff, Sword, Unarmed
- `InnateStatus` vocab (4): Atheist, Float, KO, Undead
- `ImmuneStatus` vocab (38): Atheist, Berserk, Blind, Charging, Charm, Chest, Chicken, Confuse, Critical, Crystal, Defending, Disable, Doom, Faith, Float, Haste, Immobilize, Invisible, Jump, KO, Oil, Performing, Poison, Protect, Reflect, Regen, Reraise, Shell, Silence, Sleep, Slow, Stone, Stop, Toad, Traitor, Undead, Unused1, Vampire
- `StartingStatus` vocab (1): KO
- `AbsorbElements` vocab (6): Dark, Earth, Fire, Holy, Ice, Water
- `NullifyElements` vocab (6): Dark, Earth, Fire, Holy, Ice, Wind
- `HalveElements` vocab (2): Ice, Wind
- `WeakElements` vocab (7): Earth, Fire, Holy, Ice, Lightning, Water, Wind

## JobNeedLevelData.xml  (JobNeedLevel x 22)

Fields: Id, Squire, Chemist, Knight, Archer, Monk, WhiteMage, BlackMage, TimeMage, Summoner, Thief, Orator, Mystic, Geomancer, Dragoon, Samurai, Ninja, Arithmetician, Bard, Dancer, Mime, DarkKnight, OnionKnight, Unknown1, Unknown2


## MapTrapFormationData.xml  (MapTrapFormation x 128)

Fields: Id, X1, Y1, TrapFlags1, RareItemId1, CommonItemId1, X2, Y2, TrapFlags2, RareItemId2, CommonItemId2, X3, Y3, TrapFlags3, RareItemId3, CommonItemId3, X4, Y4, TrapFlags4, RareItemId4, CommonItemId4


## MonsterJobCommandData.xml  (MonsterJobCommand x 48)

Fields: Id, AbilityId1, AbilityId2, AbilityId3, AbilityId4


## SpawnData.xml  (Spawn x 4)

Fields: Id, HP, MP, Speed, PA, MA, Helmet, Armor, Accessory, RightWeapon, RightShield, LeftWeapon, LeftShield


## SpawnVarianceData.xml  (SpawnVariance x 4)

Fields: Id, HP, MP, Speed, PA, MA


## StatusEffectData.xml  (StatusEffect x 40)

Fields: Id, Unused_0x00, Unused_0x01, Order, Counter, CheckFlags, CancelFlags, NoStackFlags

- `CheckFlags` vocab (15): CantReact, Check1, Check10, Check11, Check12, Check2, Check8, Check9, ConfusionTransparentCharmSleep, CrystalTreasure, DefendPerform, FreezeCT, IgnoreAttacks, KO, PoisonRegen


# Nex/NXD table schemas (battle-relevant; edited via .nxd override)

## OverrideAbilityActionData  (13 columns)
  table_name=OverrideAbilityActionData  set_table_type=SingleKeyed  use_base_row_id=true
Columns: unused:int, Flags12:short[], Flags34:short[], Range:short, EffectArea:short, Vertical:short, Element:short, Formula:short, X:short, Y:short, InflictStatus:short, CT:short, MPCost:short

## Ability  (23 columns)
  table_name=Ability  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, IconId:int, Name:string, Description:string, Unknown10:string, Unknown14:short, Unknown16:byte, Unknown17:byte, Unknown18:byte, Unknown19:byte, Unknown1A:short, Comment:string, UiId:int, BattleVoiceIds:int[], UiId2:int, Unknown30:int, Unknown34:int, AbilityReactionVoiceTypeId:int, Unknown3C:int, JpCost1:byte, JpCost2:byte, IsRandomDamage:byte, IsRandomStatus:byte

## AbilityReactionVoiceType  (12 columns)
  table_name=AbilityReactionVoiceType  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Unknown4:int, Unknown8:int, UnknownC:int, Unknown10:int, Unknown14:int, Unknown18:int, Unknown1C:int, Unknown20:int, Unknown24:int, Unknown28:int, Unknown2C:int

## Job  (16 columns)
  table_name=Job  set_table_type=SingleKeyed  use_base_row_id=true
Columns: Unknown1:int, Unknown2:string, Name:string, Unknown4:string, Description:string, Unknown6:string, jobtype+Id:int, Unknown8:int, jobcommand+Id:uint, Unknown10:int, Unknown11:int, TexturePartsIndex:uint, uijobabilityhelp+Id:uint, egg+Id:uint, Unknown15:int, HideJobTree:int

## GeneralJob  (42 columns)
  table_name=GeneralJob  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Comment:string, RequiredJobExp:int[], RequiredJobIds:int[], RequiredJobLevels:short[], RequiredJobPositions:int[], Unknown28:int, Unknown2C:int, Unknown30:int, Unknown34:int, Unknown38:int, Unknown3C:int, Unknown40:int, Unknown44:int, Unknown48:int, Unknown4C:int, Unknown50:int, Unknown54:int, Unknown58:int, Unknown5C:int, Unknown60:int, Unknown64:int, Unknown68:byte, Unknown69:byte, Unknown6A:byte, Unknown6B:byte, Unknown6C:byte, Unknown6D:byte, Unknown6E:byte, Unknown6F:byte, Unknown70:byte, Unknown71:byte, Unknown72:byte, Unknown73:byte, Unknown74:byte, Unknown75:byte, Unknown76:byte, Unknown77:byte, Unknown78:byte, Unknown79:byte, Unknown7A:byte, Unknown7B:byte

## JobType  (4 columns)
  table_name=JobType  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Comment:string, MaleVisualIconId:int, FemaleVisualIconId:int

## JobCommand  (9 columns)
  table_name=JobCommand  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, IconId:int, Name:string, Description:string, Description2:string, Unknown14:int, Comment:string, Unknown1C:int, UiDialogId:int

## Item  (16 columns)
  table_name=Item  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Name:string, NameSingular:string, NamePlural:string, Description:string, Name2:string, Unknown18:byte, Unknown19:byte, Unknown1A:byte, Unknown1B:byte, UiStatusEffectId:int, Comment:string, UiItemCategoryId:int, SortOrder:int, Unknown2C:byte, IsRandomDamage:byte

## Battle  (1 columns)
  table_name=Battle  set_table_type=SingleKeyed  use_base_row_id=false
Columns: DLCFlags:int

## BattleObjective  (4 columns)
  table_name=BattleObjective  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Name:string, Unknown8:int, IsProtectionMission:byte

## CharaZodiacStoneCLUT  (3 columns)
  table_name=CharaZodiacStoneCLUT  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Comment:string, CLUTData:byte[]

## CharShapeLUTParam  (6 columns)
  table_name=CharShapeLUTParam  set_table_type=SingleKeyed  use_base_row_id=false
Columns: DLCFlags:int, Comment:string, Unknown8:float, UnknownC:float, Unknown10:short, Unknown12:short

## CharTacticalViewParam  (5 columns)
  table_name=CharTacticalViewParam  set_table_type=DoubleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Comment:string, Unknown8:float[], Unknown10:float[], Unknown18:float[]

## ContinuousBattleTimeline  (2 columns)
  table_name=ContinuousBattleTimeline  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Unknown4:string

## MapVariationRandomBattle  (2 columns)
  table_name=MapVariationRandomBattle  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Comment:string

## UIStatusEffect  (10 columns)
  table_name=UIStatusEffect  set_table_type=SingleKeyed  use_base_row_id=false
Columns: DLCFlags:int, Comment:string, Type:int, Name:string, Caption:string, Unknown14:int, Unknown18:int, Unknown1C:int, Unknown20:short, Unknown22:byte

## UIUnitStatusNumParam  (4 columns)
  table_name=UIUnitStatusNumParam  set_table_type=SingleKeyed  use_base_row_id=true
Columns: DLCFlags:int, Unknown4:int, Unknown8:int, UnknownC:int
