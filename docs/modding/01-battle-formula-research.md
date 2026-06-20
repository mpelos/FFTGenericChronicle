# Battle Formula Research - FFT: The Ivalice Chronicles

This note records where battle mechanics live in the installed PC/Steam Enhanced build and
how this mod can alter them. It is intentionally technical and conservative: items marked
"verified locally" were checked against files on this machine; classic FFT/WotL formula
details come from FFHacktics and AeroStar's Battle Mechanics Guide and still need in-game
validation in Ivalice Chronicles before we rely on exact edge cases.

## Executive Summary

The battle system is split into four practical layers.

1. Hardcoded formula routines in `FFT_enhanced.exe`.
   These routines implement what a Formula id actually computes: weapon damage, Faith-scaled
   magic, Holy Sword, Punch Art, Throw, Jump, and similar behavior. Changing the math inside a
   formula, adding a new formula id, changing rounding rules, or changing global constants is a
   code-mod/ASM tier task.

2. Per-ability action data in Nex/NXD.
   The key editable surface for skill formulas is:

   ```text
   nxd/overrideabilityactiondata.nxd
   ```

   It can override `Formula`, `X`, `Y`, `Element`, `Range`, `EffectArea`, `Vertical`,
   `InflictStatus`, `CT`, and `MPCost` per ability id.

3. Hardcoded table patches through the mod loader's TableData XML.
   Jobs, skill metadata, command sets, weapon data, status data, item data, shops, and related
   hardcoded tables are exposed as XML templates in:

   ```text
   C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData
   ```

4. Encounter data in FFTPack/ENTD.
   Unit composition, enemy jobs, levels, equipment, skills, and positions live in
   `fftpack/battle_entd*_ent.bin`. This was already proven in the sibling New Game++ project.

The best starting strategy for Generic Chronicle is data-first: repoint abilities to existing
formula ids and retune `X/Y/CT/MP/range/status`, rebuild jobs and skillsets through XML, and
only use code/ASM when an intended mechanic cannot be expressed through existing formulas.

## Verified Local Paths

Game install:

```text
D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles
```

Enhanced executable:

```text
D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\FFT_enhanced.exe
```

Data packs:

```text
D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\data\enhanced
```

Mod loader:

```text
C:\Reloaded-II\Mods\fftivc.utility.modloader
```

Generic Chronicle mod root:

```text
D:\Projects\FFTGenericChronicle\mod\fftivc.generic.chronicle
```

FF16Tools CLI available via sibling project:

```text
D:\Projects\FFTModNewGame++\tools\FF16Tools.CLI-1.13.2-win-x64\win-x64\FF16Tools.CLI.exe
```

## Layer 1 - Hardcoded Formula Routines

Every action that produces battle numbers ultimately goes through a hardcoded routine selected
by a 1-byte Formula id. The formulas are the classic FFT/WotL style catalog: weapon damage,
Faith-scaled magic, HP drain, break/steal success, Holy Sword, Punch Art, Throw, Jump, and so
on.

Verified locally:

- `OverrideAbilityActionData.layout` says the PC/Steam handler for ability action overrides is
  around:

  ```text
  FFT_enhanced.exe+eea6e50
  ```

- `ItemWeaponData.xml` exposes a weapon `Formula` byte and `Power`.
- `AbilityActionData.xml` is not an XML data table. It only tells modders to edit Nex table
  `OverrideAbilityActionData`.

What this means:

- We can change which existing formula an ability uses.
- We can change the parameters fed to that formula (`X`, `Y`, element, status, CT, MP, etc.).
- We cannot create a brand-new formula or rewrite the exact computation through data alone.

Examples of formula-level behavior that likely needs code/ASM if changed globally:

- exact `PA * WP`, `MA * Y`, `PA * (WP + Y)`, or Faith-scaling routines;
- rounding/truncation order;
- Zodiac multipliers;
- Protect/Shell ratios;
- the weapon-type-to-XA mapping;
- adding new Formula ids beyond the hardcoded dispatch table.

## Layer 2 - Per-Ability Formula Overrides

Primary table:

```text
data\enhanced\0004.pac -> nxd/overrideabilityactiondata.nxd
```

Local copy already converted:

```text
D:\Projects\FFTGenericChronicle\work\override_ability.sqlite
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

`Key` is the ability id. `Flags12` and `Flags34` are short arrays that patch the four
ability-action flag fields. Scalar columns use this convention:

```text
-1  = inherit the base hardcoded value
>=0 = cast to byte and override that field
```

Verified local stock data:

```text
Rows in OverrideAbilityActionData: 368
Formula overrides: 0
X overrides: 0
Y overrides: 0
Range/area/vertical/element/status overrides: 0
CT overrides: 28
MPCost overrides: 4
```

This is important: Square/Nenkai's current surface already supports formula/power overrides,
but the vanilla remaster only uses it for CT and MP retuning. For our mod, setting `Formula`,
`X`, and `Y` should be the main way to change skill damage without touching the exe.

### Where is the vanilla baseline (per-ability Formula/X/Y)?

Caveat that shapes the whole design workflow: **the base Formula/X/Y/Element/Range/etc. for
each ability is NOT in any data file.** Verified by searching every `data\enhanced\*_files.txt`
manifest: the only ability-related Nex tables are `ability.<lang>.nxd` (UI text + JP, no math)
and `overrideabilityactiondata.nxd` (the sparse override, all math fields `= -1` in stock).
The actual base action table is **hardcoded inside `FFT_enhanced.exe`**.

Consequences:

- `OverrideAbilityActionData` is a *patch layer*. To change one ability we ADD a row override;
  we cannot read the current value from data because stock rows are `-1` (inherit).
- To know what an ability does *today* (its base Formula/X/Y), we must use one of:
  1. **FFHacktics WotL ability data** as the reference baseline (Ivalice Chronicles = WotL
     ruleset; reliable for formula id / X / Y / element / CT, but some values were rebalanced —
     see JP note below, so treat as a starting point and validate in-game), or
  2. **extract the hardcoded base table from `FFT_enhanced.exe`** (code/RE task, most accurate).
- Practical plan: build the design baseline from FFHacktics, override only what we change, and
  spot-check a few abilities in-game to confirm the remaster matches.

Example edit workflow:

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

## Ability Ids And Names

The localized ability table is in the language pack, not in base `0004.pac`.

Verified local path:

```text
data\enhanced\0004.en.pac -> nxd/ability.en.nxd
```

Converted local DB:

```text
D:\Projects\FFTGenericChronicle\work\ability_en.sqlite
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

The JP cost in `ability.en.nxd` is split across `JpCost1` and `JpCost2`, while
`AbilityData.xml` has a `JPCost` field that its own comment says is unused. For skill identity
and UI text, use `ability.en.nxd`. For ability metadata and AI flags, use `AbilityData.xml`.
For action formula/math, use `OverrideAbilityActionData`.

Sample rows from local extraction:

```text
1  Cure      JpCost1=50  JpCost2=0
2  Cura      JpCost1=180 JpCost2=0
3  Curaga    JpCost1=194 JpCost2=1
4  Curaja    JpCost1=32  JpCost2=3
15 Holy      JpCost1=88  JpCost2=2
```

JP decode is now solved (verified locally against known WotL costs):

```text
JP = JpCost1 + 256 * JpCost2     (16-bit little-endian)

Cure   50 + 256*0 = 50    (known 50)   OK
Cura  180 + 256*0 = 180   (known 180)  OK
Curaga 194 + 256*1 = 450  (known 450)  OK
Curaja  32 + 256*3 = 800  (known 800)  OK
Holy    88 + 256*2 = 600  (known 600)  OK
```

`Ability-en` has 512 rows. Note some single-spell JP values differ from PSX/WotL (e.g. local
Fira = 200, classic = 120), i.e. Ivalice Chronicles rebalanced some costs; trust the local
table, not old guides, for actual values.

## Layer 3 - TableData XML

The loader docs say to copy only edited entries/properties into:

```text
FFTIVC\tables\enhanced\<TableName>.xml
FFTIVC\tables\classic\<TableName>.xml
FFTIVC\tables\combined\<TableName>.xml
```

For this mod, target `enhanced` first.

High-value tables for a battle-system overhaul:

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

Important loader rule:

```text
Only leave elements/properties that this mod actually edits.
```

That keeps merge conflicts lower because the loader tracks edited properties.

## Weapon Formula Surface

Weapon damage is controlled by:

```text
C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData\ItemWeaponData.xml
```

Relevant fields:

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

So weapon attacks can be heavily redesigned as data, but the weapon formula routines
themselves are still hardcoded.

## Job And Stat Surface

Job rebuilds primarily go through:

```text
JobData.xml
```

Key fields:

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

Classic FFT stat formulas use raw stats plus class multipliers/growth constants. Ivalice
Chronicles exposes those job constants in `JobData.xml`; exact level-up math should still be
validated in-game once we start changing growths.

## Layer 4 - Encounters

Encounter files live in:

```text
data\enhanced\0002.pac -> fftpack/battle_entd1_ent.bin
data\enhanced\0002.pac -> fftpack/battle_entd2_ent.bin
data\enhanced\0002.pac -> fftpack/battle_entd3_ent.bin
data\enhanced\0002.pac -> fftpack/battle_entd4_ent.bin
```

The sibling New Game++ project already proved that FFTPack files are loaded correctly from:

```text
FFTIVC\data\enhanced\fftpack\<file>
```

This layer matters because any global job/skill/formula redesign will probably need matching
story-battle and random-battle redesign.

## What We Can Change Without Code

High confidence:

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

## What Probably Requires Code/ASM

- New formula ids.
- Rewriting formula internals.
- New scaling variables not already used by some formula.
- New rules for global modifiers: Zodiac, Faith, Protect, Shell, Brave, Attack Boost, etc.
- Hardcoded formula-slot behavior: Break target slot, Steal target slot, Talk Brave/Faith
  interpretation, item Z-values, Knockback tied to a specific routine, and other slot-specific
  logic.
- Increasing hardcoded table sizes beyond exposed limits.

If we reach this tier, prefer a Reloaded-II C# code mod using `fftivc.utility.modloader`
interfaces or Faith Framework before raw exe patching.

## Practical Next Steps

1. DONE - `tools/dump_baseline.py` joins `ability_en.sqlite` + `override_ability.sqlite` and
   parses `JobData.xml`, producing the design baseline:
   - `work/baseline_jobs.csv` (174 jobs: stats, growth/multipliers, move/jump, evasion,
     elements, equippable, innates)
   - `work/baseline_abilities.csv` (512 abilities: name, decoded JP, the 29 vanilla CT/MP
     overrides, random-damage/status flags)
2. DONE - JP decode solved: `JP = JpCost1 + 256*JpCost2` (see Layer 2 above).
3. Fill in vanilla baseline Formula/X/Y per ability from FFHacktics (not in local data) into the
   ability design table, so we redesign from real starting values.
4. Create a small proof patch: change one harmless ability's `Formula`, `X`, and `Y`, deploy it,
   and test in a save.
5. Create minimal XML patches for one job and one weapon to prove TableData editing.
6. Resolve the Tier-2 (custom-formula / exe-hook) question - see `03-` once the code-mod
   research lands - then decide how much of Generic Chronicle needs a code-mod layer.

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
