# Data Editing Surfaces

This file describes the editable layers of the FFT: The Ivalice Chronicles battle system and how to edit each one by data. The four-layer model is summarized in `00-overview.md`; this file documents the edit mechanics for each surface.

## What Data-Only Can Change vs. What Needs Code

Every action that produces battle numbers passes through a hardcoded routine in `FFT_enhanced.exe` selected by a 1-byte Formula id. The routines implement the classic FFT/WotL catalog: weapon damage, Faith-scaled magic, HP drain, break/steal success, Holy Sword, Punch Art, Throw, Jump, and similar behaviors. Data editing repoints abilities to existing formula ids and retunes their parameters; it does not rewrite the math inside a formula.

Data-only changes (no code):

- ability formula id;
- ability `X` and `Y` constants;
- ability element/status/range/area/vertical;
- ability CT and MP;
- ability metadata, type, learn chance, AI flags;
- job stat growth/multipliers and equipment permissions;
- command/skillset composition;
- weapon formula, power, range, element, evasion, proc/options;
- status duration/interaction flags;
- item values and shops;
- encounter unit composition and fixed/scaling levels.

Changes that require code/ASM:

- New formula ids.
- Rewriting formula internals.
- New scaling variables not already used by some formula.
- New rules for global modifiers: Zodiac, Faith, Protect, Shell, Brave, Attack Boost, etc.
- Hardcoded formula-slot behavior: Break target slot, Steal target slot, Talk Brave/Faith interpretation, item Z-values, Knockback tied to a specific routine, and other slot-specific logic.
- Increasing hardcoded table sizes beyond exposed limits.

When a code mod is required, a Reloaded-II C# code mod using `fftivc.utility.modloader` interfaces or Faith Framework is preferred over raw exe patching.

Formula-level behavior that needs code/ASM if changed globally includes the exact `PA * WP`, `MA * Y`, `PA * (WP + Y)`, or Faith-scaling routines; rounding/truncation order; Zodiac multipliers; Protect/Shell ratios; the weapon-type-to-XA mapping; and adding new Formula ids beyond the hardcoded dispatch table. The Formula-ID catalog and the Tier-1/Tier-2 variable constraint live in `02-formula-id-catalog.md`.

## Per-Ability Formula Overrides (NXD OverrideAbilityActionData)

The primary editable surface for skill formulas is the Nex/NXD table:

```text
data\enhanced\0004.pac -> nxd/overrideabilityactiondata.nxd
```

Layout file:

```text
C:\Reloaded-II\Mods\fftivc.utility.modloader\Nex\Layouts\ffto\OverrideAbilityActionData.layout
```

Columns:

```text
Key
unused
Flags12
Flags34
Range
EffectArea
Vertical
Element
Formula
X
Y
InflictStatus
CT
MPCost
```

`Key` is the ability id. `Flags12` and `Flags34` are short arrays that patch the four ability-action flag fields. Scalar columns use this convention:

```text
-1  = inherit the base hardcoded value
>=0 = cast to byte and override that field
```

`OverrideAbilityActionData` can override `Formula`, `X`, `Y`, `Element`, `Range`, `EffectArea`, `Vertical`, `InflictStatus`, `CT`, and `MPCost` per ability id.

### Override Table Is a Patch Layer

The base Formula/X/Y/Element/Range/etc. for each ability is not in any data file. The only ability-related Nex tables are `ability.<lang>.nxd` (UI text + JP, no math) and `overrideabilityactiondata.nxd` (the sparse override). The actual base action table is hardcoded inside `FFT_enhanced.exe`. The PC/Steam handler for ability action overrides is around `FFT_enhanced.exe+eea6e50` (per `OverrideAbilityActionData.layout`).

In stock data, every math field is `-1` (inherit): there are 368 rows, with 0 Formula/X/Y/Range/area/vertical/element/status overrides, 28 CT overrides, and 4 MPCost overrides. The remaster ships this surface to retune CT and MP only, even though it already supports formula/power overrides. Setting `Formula`, `X`, and `Y` is the main way to change skill damage without touching the exe.

Consequences of the override being a patch layer:

- To change one ability, ADD a row override. The current value cannot be read from data because stock rows are `-1` (inherit).
- To know what an ability does today (its base Formula/X/Y), use one of:
  1. FFHacktics WotL ability data as the reference baseline (Ivalice Chronicles = WotL ruleset; reliable for formula id / X / Y / element / CT, but some values were rebalanced — see the JP note below — so treat as a starting point and validate in-game), or
  2. extract the hardcoded base table from `FFT_enhanced.exe` (code/RE task, most accurate).

### Edit Workflow

```powershell
$ff = "D:\Projects\FFTModNewGame++\tools\FF16Tools.CLI-1.13.2-win-x64\win-x64\FF16Tools.CLI.exe"

& $ff unpack `
  -i "D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\data\enhanced\0004.pac" `
  -f "nxd/overrideabilityactiondata.nxd" `
  -o work\nxd_extract `
  -g fft

& $ff nxd-to-sqlite -i work\nxd_extract\nxd -o work\override_ability.sqlite -g fft

# Edit rows in SQLite:
# Key = ability id
# Formula/X/Y/etc = desired values

& $ff sqlite-to-nxd -i work\override_ability.sqlite -o work\nxd_out -g fft
```

Final mod path:

```text
mod\fftivc.generic.chronicle\FFTIVC\data\enhanced\nxd\overrideabilityactiondata.nxd
```

## Ability Ids, Names, and JP Decode

The localized ability table is in the language pack, not in base `0004.pac`:

```text
data\enhanced\0004.en.pac -> nxd/ability.en.nxd
```

Useful columns:

```text
Key
Name
Description
IconId
JpCost1
JpCost2
IsRandomDamage
IsRandomStatus
```

The JP cost in `ability.en.nxd` is split across `JpCost1` and `JpCost2`, while `AbilityData.xml` has a `JPCost` field that its own comment says is unused. For skill identity and UI text, use `ability.en.nxd`. For ability metadata and AI flags, use `AbilityData.xml`. For action formula/math, use `OverrideAbilityActionData`.

JP is decoded as a 16-bit little-endian value:

```text
JP = JpCost1 + 256 * JpCost2     (16-bit little-endian)

Cure   50 + 256*0 = 50    (known 50)   OK
Cura  180 + 256*0 = 180   (known 180)  OK
Curaga 194 + 256*1 = 450  (known 450)  OK
Curaja  32 + 256*3 = 800  (known 800)  OK
Holy    88 + 256*2 = 600  (known 600)  OK
```

`ability.en` has 512 rows. Some single-spell JP values differ from PSX/WotL (e.g. local Fira = 200, classic = 120): Ivalice Chronicles rebalanced some costs, so the local table is authoritative over old guides for actual values.

## TableData XML

The loader exposes hardcoded tables (jobs, skill metadata, command sets, weapon data, status data, item data, shops, and related tables) as XML templates under:

```text
C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData
```

Edits are made by copying only the edited entries/properties into one of:

```text
FFTIVC\tables\enhanced\<TableName>.xml
FFTIVC\tables\classic\<TableName>.xml
FFTIVC\tables\combined\<TableName>.xml
```

For a battle-system overhaul, target `enhanced` first.

Important loader rule: leave only the elements/properties that the mod actually edits. The loader tracks edited properties, so keeping the template minimal lowers merge conflicts.

```text
Only leave elements/properties that this mod actually edits.
```

High-value tables:

| Table | Main use |
| --- | --- |
| `AbilityData.xml` | Ability metadata, learn chance, ability type, AI behavior flags. Not the damage formula. |
| `AbilityTypeData.xml` | Animation, charge effect, battle text id. |
| `AbilityChargeAimData.xml` | Charge/Aim secondary tuning. |
| `AbilityJumpData.xml` | Jump secondary tuning. |
| `AbilityThrowData.xml` | Throw secondary tuning. |
| `AbilityMathData.xml` | Arithmeticks targeting behavior. |
| `JobData.xml` | Job command id, innates, equipment, stat growth/multipliers, movement, status, elements. |
| `JobCommandData.xml` | Which abilities are in a command/skillset. |
| `MonsterJobCommandData.xml` | Monster skill lists. |
| `JobNeedLevelData.xml` | Job unlock requirements. |
| `ItemWeaponData.xml` | Weapon formula, power, range, evasion, element, proc/option ability. |
| `ItemEquipBonusData.xml` | Gear PA/MA/Speed/HP/MP bonuses. |
| `ItemArmorData.xml`, `ItemShieldData.xml`, `ItemAccessoryData.xml` | Gear defensive stats. |
| `ItemConsumableData.xml` | Potion/item values. |
| `ItemShopsData.xml` | Shop progression. |
| `StatusEffectData.xml` | Status counters, stacking/cancel/check flags. |
| `CommandTypeData.xml` | Command category behavior. |
| `SpawnData.xml`, `SpawnVarianceData.xml` | Generic/random spawn rules. |

The comprehensive field catalog, enum vocabularies, and hard limits for these tables live in `03-battle-data-map.md`.

### Weapon Formula Surface

Weapon damage is controlled by `ItemWeaponData.xml`. Relevant fields:

```xml
<Formula>1</Formula>
<Power>3</Power>
<Range>1</Range>
<AttackFlags>...</AttackFlags>
<Evasion>5</Evasion>
<Elements>None</Elements>
<OptionsAbilityId>0</OptionsAbilityId>
```

The XML header notes:

- table size is 128 weapon rows;
- if using Formula 02, `OptionsAbilityId` should be an ability id;
- otherwise `OptionsAbilityId` should be an item options id or 0.

Weapon attacks can be heavily redesigned as data, but the weapon formula routines themselves are still hardcoded.

### Job And Stat Surface

Job rebuilds primarily go through `JobData.xml`. Key fields:

```text
JobCommandId
InnateAbilityId1-4
EquippableItems
HPGrowth / HPMultiplier
MPGrowth / MPMultiplier
SpeedGrowth / SpeedMultiplier
PAGrowth / PAMultiplier
MAGrowth / MAMultiplier
Move
Jump
CharacterEvasion
InnateStatus
ImmuneStatus
StartingStatus
Absorb/Nullify/Halve/Weak/Strengthen elements
```

Classic FFT stat formulas use raw stats plus class multipliers/growth constants. Ivalice Chronicles exposes those job constants in `JobData.xml`; exact level-up math should be validated in-game when changing growths.

## Encounters (ENTD)

Unit composition, enemy jobs, levels, equipment, skills, and positions live in the FFTPack/ENTD files:

```text
data\enhanced\0002.pac -> fftpack/battle_entd1_ent.bin
data\enhanced\0002.pac -> fftpack/battle_entd2_ent.bin
data\enhanced\0002.pac -> fftpack/battle_entd3_ent.bin
data\enhanced\0002.pac -> fftpack/battle_entd4_ent.bin
```

FFTPack files load from:

```text
FFTIVC\data\enhanced\fftpack\<file>
```

This layer matters because any global job/skill/formula redesign needs matching story-battle and random-battle redesign. Live runtime struct offsets for units are documented in `04-engine-memory-model.md`.

## Sources

- Local loader layout: `C:\Reloaded-II\Mods\fftivc.utility.modloader\Nex\Layouts\ffto\OverrideAbilityActionData.layout`
- Local loader templates: `C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData\*.xml`
- Nenkai FFT mod loader: https://github.com/Nenkai/fftivc.utility.modloader
- Nenkai FFT creating mods: https://nenkai.github.io/ffxvi-modding/modding/creating_mods_fft/
- Nenkai Nex/NXD editing: https://nenkai.github.io/ffxvi-modding/tutorials/nex/nxd_editing/
- Nenkai FFT mod loader API: https://nenkai.github.io/ffxvi-modding/modding/mod_loader_api_fft/
- Nenkai FFT Nex layouts: https://github.com/Nenkai/fftivc-nex-layouts
- FFHacktics formulas: https://ffhacktics.com/wiki/Formulas
- FFHacktics formula table: https://ffhacktics.com/wiki/Formula_Table
- FFHacktics formula hacking: https://ffhacktics.com/wiki/Formula_Hacking
- AeroStar FFT Battle Mechanics Guide: https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
