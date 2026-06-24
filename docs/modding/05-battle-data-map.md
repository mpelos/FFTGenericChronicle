# Battle Data Map - Every Variable We Can Touch, And Its Limits

Complete map of the battle system's accessible variables, *before* any design decisions, so we
know the full toolbox. Generated/validated from the installed game + mod loader on this machine.
Raw field dump: `work/battle_data_inventory.md` (regenerate with `tools/map_battle_data.py`).
Baseline values: `work/baseline_jobs.csv`, `work/baseline_abilities.csv` (`tools/dump_baseline.py`).
Joined item catalog for equipment mapping: `work/item_catalog.csv` (`tools/dump_item_catalog.py`).

## Access tiers (how to reach each variable)

```text
A. Runtime battle-unit struct (live, in memory)   -> Tier 2 code mod (Reloaded-II hook). Read/write any stat mid-battle.
B. Nex/NXD tables (.nxd in data\enhanced\nxd)      -> FF16Tools nxd<->sqlite; loader merges cells. Per-ability action math, UI/text.
C. TableData XML (hardcoded tables)                -> copy template to FFTIVC\tables\enhanced\; edit only changed props. Jobs, items, status, weapons.
D. ENTD encounter binaries (fftpack\*.bin)         -> entd_tool.py. Per-battle unit roster/jobs/levels/gear.
```

A = read it live / override the result. B+C = change the data the engine reads. Most of the
overhaul is B+C; A is for math the formula catalog can't express (see `03`/`04`).

---

## A. Live battle-unit variables (runtime struct)

These are the per-unit values in memory during battle - everything a Tier-2 damage hook can read
or rewrite. **CONFIRMED LIVE on this install** (probe iter 3 read every field below with sane,
internally-consistent values). Base pointer = the unit; same layout the classic `Battle_Stats`
describes.

```text
+0x00 char id (byte)          +0x30 HP    (word)   +0x32 MaxHP (word)
+0x04 team/group id (byte)    +0x34 MP    (word)   +0x36 MaxMP (word)
+0x05 friend/foe (bit 0x10)   +0x3E PA    (byte)   +0x3F MA    (byte)
+0x28 EXP (byte)              +0x40 Speed (byte)   +0x41 CT (byte, charge time)
+0x29 Level (byte)            +0x42 Move  (byte)   +0x43 Jump  (byte)
+0x2A MaxBrave (byte)         +0x2B Brave (byte)
+0x2C MaxFaith (byte)         +0x2D Faith (byte)
```

All of the above verified live across 4 distinct units (probe iter 4 full-struct hex dump). The
0x28-0x43 block is the solid combat-stat region. Not yet anchored (need units with known
equipment/status to map): ~0x14-0x1F (likely raw/derived stats, 16-bit words), 0x38-0x3B &
0x44-0x47 (base/derived stats?), and 0x70-0x8F (looks like object pointers). Equipment ids,
status bitfields, and R/S/M ability ids weren't locatable from full-HP/no-status samples - map
them later from controlled units or at the formula hook. The current harness now captures through
0x17F so the next controlled battle can test whether those fields live later in the unit object.

**CONFIRMED LIVE (2026-06-21, Tests 2a/2b/2c):** `+0x61` is a **status byte**, and **bit `0x20`
is set on death** (KO/dead). In 5/5 vanilla deaths (humans and monsters) the alive->dead diff was
exactly `+0x30->00` (HP) plus `+0x61:00->20`, with no other consistent change and no delayed
follow-up change. This is the first mapped bit of the status bitfield region.

Important limit: writing HP=0 and/or setting `+0x61 |= 0x20` is **not a death trigger**. Live
Tests 2b/2c proved byte writes create zombie-like state; the engine owns real KO/death. The code
mod should read this bit for KO/status checks (`hasBit(targetByte(0x61), 5)`), but lethal custom
damage should use the accepted engine-owned path (`MinHpFloor=1`) until a true pre-damage or engine
death trigger is found. `DeathStateWrites` remains only as legacy/refuted probe infrastructure.

**CONFIRMED LIVE (2026-06-21, actor-probe attacker resolution):** `+0x41` is **CT (charge time)**.
It rises each tick proportional to `+0x40` Speed and **resets to a low value when the unit acts**
(classic FFT CT model: charge to 100, act, subtract). Across 6 controlled attacks the attacker was
always the registered unit (≠ target) whose CT was lowest / had just dropped at the damage event
(5/6 by absolute-lowest CT; the one tie resolved by largest recent CT drop -> 6/6). This is how the
code mod now resolves the **attacker** for attacker-dependent formulas, faction-agnostic, without
hooking the (Denuvo-virtualized) action dispatcher. `+0x40` Speed was simultaneously re-confirmed as
the stable per-unit speed value (distinct, unchanging across the snapshots). Full evidence table in
`07-live-findings.md` (LIVE TEST 4) and `work/handoff-to-gpt-2026-06-21.md`.

Current harness support for this mapping: `Mod.cs` copies `0x00..0x17F`, logs `[DUMP]` plus
`[CANDIDATES]` for non-zero bytes/plausible 16-bit ids in `0x44..0x17F`, and logs `[DIFF]` when
those unknown bytes change. It tracks CT drops for attacker resolution (`ct-reset`), exposes
`target.ct`/`attacker.ct` plus `a.sourceCt`/`a.sourceCounter` to formulas, and can optionally log
short `[HOOK-REGS]` snapshots at the stable hook to hunt for a currently-acting unit or action
context pointer. Use controlled units with deliberately different gear/status to identify stable
equipment slots and volatile action/status fields. Run `python tools\analyze_battleprobe_log.py`
after a test battle to generate `work\battleprobe_analysis.md` with diff offset frequencies,
per-unit candidate summaries, runtime attacker sources, hook-register summaries, and candidate
item-id hits from `work\item_catalog.csv`.

Also reachable at the hook sites (from AOBs in `04`): the computed **damage** value (in `edx`
before the `[rax+0x06]` store), the player/enemy tag, and the damage/JP/XP multiplier sites.
Not yet mapped from the struct but known to exist in the classic layout: equipped item ids,
R/S/M ability ids, active status bitfields, raw stats (RHP/RMP/...), and per-stat growth
constants - locate these by extending the struct walk. See `04-re-strategy.md`.

Limits: stats are bytes (HP/MP are 16-bit words); damage stored as a 16-bit word; engine math is
integer (the remaster applies some multipliers as AVX floats, then truncates to int).

Note (separate struct): besides the unit struct above, the engine keeps a per-participant **battle
actor array** (contiguous, stride `0x548`), where `actor+0x148` -> the unit struct and the resolving
action id is stored at `actor+0x142`. This is the source for runtime caster/action identity at damage
time. Raw model and offsets: `docs/modding/12-runtime-register-action-context-book.md` section 2.4.

---

## B. Per-ability ACTION variables (the formula surface)

`OverrideAbilityActionData` (Nex, `0004.pac`). Sparse override: `-1` = inherit exe base, `>=0` =
override (cast to byte). 368 rows; base Formula/X/Y are exe-hardcoded (baseline from FFHacktics).

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

Ability metadata (`AbilityData.xml`, 512 ids): `JPCost`, `ChanceToLearn`, `Flags`,
`AbilityType`, `AIBehaviorFlags`. Text/JP (`ability.en.nxd`, 512 rows): `Name`, `Description`,
`IconId`, `JpCost1/2` (JP = JpCost1 + 256*JpCost2), `IsRandomDamage`, `IsRandomStatus`.

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

Job tree / unlock: `GeneralJob` (Nex) - `RequiredJobExp[8]`, `RequiredJobIds[]`,
`RequiredJobLevels[]`, `RequiredJobPositions[]`. `JobNeedLevelData.xml` - per-job level gates by
class name (Squire..OnionKnight). Text: `job.en.nxd` (Name/Description).

Limits: job id <= 175. Growth/Multiplier are bytes (lower Growth = faster growth in classic
math). Equippable is a fixed 34-type vocabulary (no inventing new equip slots in data).

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

Note `EquipBonus` has `StrongElements` and `BoostJP` - levers jobs don't expose. Weapon `Formula`
is its own small set (1-7) separate from the ability formula catalog. Limits: global item id <=
260; secondary weapon data id <= 127; secondary armor data id <= 63; shield <= 15; accessory <=
31; weapon Power <= 40.

`tools/dump_item_catalog.py` joins the installed modloader tables into `work/item_catalog.csv`.
The useful DR/memory-mapping columns are:

```text
item_id, name, type_flags, item_category, additional_data_id, equip_bonus_id, secondary_kind
weapon_formula, weapon_power, weapon_evasion
armor_hp_bonus, armor_mp_bonus
shield_physical_evasion, shield_magical_evasion
accessory_physical_evasion, accessory_magical_evasion
bonus_pa, bonus_ma, bonus_speed, bonus_move, bonus_jump, bonus_*status, bonus_*elements
```

For DR design, treat `item_id` as the likely runtime equipment key until proven otherwise. The
secondary ids (`AdditionalDataId`) are not globally unique across weapon/armor/shield/accessory
tables, so a raw candidate matching `item_id` is much stronger evidence than a raw candidate
matching only a secondary id.

The code mod build copies `work/item_catalog.csv` to the installed Reloaded-II code-mod folder as
`item_catalog.csv` when the file exists. Runtime settings can then match DR rules by `ItemId`,
`ItemCategory`, `TypeFlag`, `SecondaryKind`, and simple item stat thresholds without hardcoding
every armor one by one.

---

## E. Status effects

`StatusEffectData.xml` (40 statuses): `Order` (UI sort), `Counter` (duration ticks),
`CheckFlags`, `CancelFlags`, `NoStackFlags`. Text/icon: `uistatuseffect.en.nxd`.

The full status vocabulary (exact tokens from the `ImmuneStatus` enum used in JobData/EquipBonus,
38 confirmed + 2 system slots = the 40-slot table):

```text
KO Critical Undead Crystal Chest
Poison Oil Float Reraise Invisible Regen Protect Shell Haste Slow Stop
Charging Jump Charm Toad Doom Vampire Faith Atheist Sleep Disable Immobilize
Confuse Blind Silence Berserk Reflect Defending Performing Stone Traitor Chicken
(+ Unused1, and one more system slot to fill 40)
```

Limits: status id <= 39 (fixed 40-slot table). `Counter` sets duration; `CheckFlags`/`CancelFlags`
control interactions (e.g. FreezeCT, PoisonRegen, mutually-cancelling sets).

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

ENTD (`fftpack/battle_entd1-4_ent.bin`): per-encounter unit roster - sprite, flags, name id,
**level** (1-99 fixed, 100+ = party-relative), bravery, faith, job, job level, secondary
skillset, R/S/M abilities, equipment, position. See `entd_tool.py` and the New Game++ project.

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

---

## How to access each (commands)

```text
NXD read/edit:   FF16Tools.CLI unpack -i 0004.pac -f nxd/<table>.nxd -o work\... -g fft
                 FF16Tools.CLI nxd-to-sqlite -i work\...\nxd -o x.sqlite -g fft   (edit)   sqlite-to-nxd ...
                 deploy to:  mod\fftivc.generic.chronicle\FFTIVC\data\enhanced\nxd\<table>.nxd
TableData XML:   copy C:\...\modloader\TableData\<Name>.xml -> mod\...\FFTIVC\tables\enhanced\<Name>.xml
                 keep ONLY edited <Id> entries/properties (loader merges per-property)
ENTD:            python tools\entd_tool.py dump-entry/patch-levels ...
Runtime hook:    Reloaded-II C# mod, AOB-scan + CreateHook (see 04-re-strategy.md)
```
