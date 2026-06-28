# Generic Chronicle - Battle Modding Knowledge Base (Overview)

Entry point to the manual of truth for modifying the battle system of **FINAL FANTASY TACTICS -
The Ivalice Chronicles** (Steam, Enhanced v1.5.0). This file is the synthesis and index; the
specifics live in `01`-`07`. Scope is *what is technically possible* and *where the battle math
lives* - not gameplay design.

The dated investigation logs (probe runs, build checkpoints, live-evidence narration) live in the
repo's `work/` folder; this manual states durable facts only.

## Feasibility: custom formulas using attacker + target attributes + equipment

There are two levels, and the answer differs:

**Level 1 - Data-only (any attacker/target/equipment variable, from the hardcoded catalog).**
Achievable with pure data, no code, no reverse-engineering. Each ability is re-pointed to one of
the ~100 hardcoded formula routines, and tuned via two byte parameters (X, Y) + element + status.
Those routines already read attacker PA/MA/Brave/Faith, target stats, weapon Power, and
equipment-derived stats. Re-point each ability's Formula/X/Y/Element through the
`OverrideAbilityActionData` NXD table and retune everything else through TableData XML. The limit:
you cannot invent new math - you choose from the fixed catalog and feed only two byte parameters
(no new variable combinations, no >2 params, no custom scaling curve).

**Level 2 - Arbitrary new math (any combination of attributes/equipment, any number of terms, any
scaling).** Requires a code mod. The direct formula routine is Denuvo-virtualized and cannot be
located by static AOB scan — **but Denuvo virtualizes *code*, not *data*:** the unit struct and UI
buffers the VM reads/writes are ordinary, externally read/write-able memory. So the code mod controls
combat by hooking stable `.text` touchpoints and reading/writing that memory, never by hooking the
virtualized math. Proven control (2026-06-27):

- **Damage / MP magnitude** — rewrite the engine's staged debit at the pre-clamp hook (`0x30A66F`)
  just before HP/MP application; even lethal damage lands in the SAME hit through vanilla's own HP
  clamp and KO/death (a memory write cannot force death — the engine still owns it). This supersedes
  the older late post-damage reconciler (now a fallback).
- **Hit / miss / block / parry** — write the defender's live evade bytes before the roll
  (input-control, the proven primary), or repaint the result selector `0x205210` (output-control).
- **Status** (live-confirmed Undead via `+0x1EF/+0x61`), **reactions** (Brave-gate via `+0x2B`), and
  the **full forecast display** — hit-% (hook `0x227FFE`) plus HP amount number/bar ghost
  (poll-write of `obj+0x6`==`unit+0x1C4` for damage, `obj+0x8`==`unit+0x1C6` for healing) — are
  likewise controllable and coherent with the applied result for physical, magic, and healing actions.

Attacker/action context is resolved at the damage frame, with CT as a fallback signal. Live struct
offsets, the hook map, and the runtime DSL/control levers are documented in `04`, `05`, and `06`.

## The four editing layers

The mod has four distinct editing surfaces:

- **Layer A - Runtime code mod (Reloaded-II, `FFT_enhanced.exe`).** Hooks stable `.text`
  touchpoints to read the live battle-unit struct (HP/MP/PA/MA/Speed/Brave/Faith/Level/etc.) and
  rewrite computed damage/HP/MP. It also authors combat OUTCOMES (hit/miss/block/parry), status,
  reactions, and the full forecast display (hit-%, HP amount number + HP-bar ghost) via additional stable
  hooks and live-struct writes. This
  is how arbitrary Level-2 math is done. The hardcoded formula
  routines (the math each "Formula id" computes, classic WotL catalog ids 0x00-0x64) also live in
  the exe; changing the math directly is a code mod, but abilities are normally just re-pointed to
  existing formulas instead. Struct offsets: see `04`.
- **Layer B - Nex/NXD tables.** Per-ability action data
  (Formula/X/Y/Element/Range/AoE/Vertical/Status/CT/MP) in `OverrideAbilityActionData`, plus
  UI/text tables (ability/job/item `.nxd`). No exe needed.
- **Layer C - TableData XML (mod loader).** Jobs, weapons, armor, status, skillsets, items, shops,
  spawns. No exe needed.
- **Layer D - ENTD encounter data (`fftpack/*.bin`).** Per-battle roster: jobs, levels, gear,
  skills, placement.

## Key facts

- **~90% of a battle overhaul is data-only** (no exe): re-point every ability's formula, retune
  X/Y/Element/Status/CT/MP via `OverrideAbilityActionData`, rebuild every job via `JobData.xml`,
  rework weapons/armor/status/skillsets via TableData XML, rebuild encounters via ENTD.
- **Arbitrary math is not possible in data.** Data picks from the fixed catalog of ~100 hardcoded
  routines and feeds two byte parameters (X, Y) + element/status. Free math requires the Layer-A
  code mod, via the same-hit pre-clamp staged-debit hook in `06` (not a direct damage-routine hook).
- **Denuvo virtualizes code, not data — combat is PROVEN controllable (2026-06-27).** The damage
  routine can't be hooked, but the memory it reads/writes can: damage/MP via the pre-clamp hook
  (`0x30A66F`, same-hit), hit/miss/block/parry via the defender's live evade bytes (input-control) or
  the result selector (`0x205210`, output-control), status via `+0x1EF/+0x61`, reactions via Brave
  `+0x2B`, and the full forecast display — hit-% (hook `0x227FFE`) and HP amount number + HP-bar
  ghost (`obj+0x6`==`unit+0x1C4` for damage, `obj+0x8`==`unit+0x1C6` for healing).
  The committed build target is the Deep
  Combat Layer (DCL): "output-control first, input only where output can't." Details in `04`/`05`/`06`.
- **No modding API exposes a battle-formula callback.** Loader managers only patch data tables;
  Faith Framework is a live Nex editor. The direct formula routine is Denuvo-virtualized; the code
  mod instead hooks stable `.text` touchpoints to read battle units, detect HP/MP deltas, resolve
  attacker/action context at the damage frame (CT as fallback), compute formulas in C#, and
  reconcile the final number.
- **Classic FFT/WotL mods are a knowledge map, not portable code** (PSX/PSP MIPS vs x64
  reimplementation). They supply the exact math, struct layout, dispatch architecture, and a
  validation oracle, turning the RE from blind to guided.
- **Base per-ability Formula/X/Y is not in any data file** - it is exe-hardcoded; the override
  table is sparse (`-1` = inherit). Use FFHacktics WotL data as the design baseline.
- **JP decode:** `JP = JpCost1 + 256*JpCost2`. Decode and edit surfaces are detailed in `01`.

## Document index

| File | Contents |
|------|----------|
| `00-overview.md` | (this file) synthesis, four editing layers, feasibility, index, paths, sources |
| `01-data-editing-surfaces.md` | the editing surfaces; verified local paths; edit workflows; JP decode; where the vanilla baseline is (and isn't) |
| `02-formula-id-catalog.md` | formula ids 0x00-0x64 and their math; weapon XA table; magic; stat derivation; Level-1 vs Level-2 variable availability |
| `03-battle-data-map.md` | master variable map: every accessible field by domain, full enum vocabularies, hard limits, and how to access each |
| `04-engine-memory-model.md` | live battle-unit struct offsets; equipment block; battle actor array; hooks; damage→clamp→KO path |
| `05-reverse-engineering.md` | using classic knowledge for RE: PSX decomp, cheat-table struct offsets + AOBs, formula fingerprint sheet, attack path, Denuvo notes |
| `06-code-mod-runtime-dsl.md` | runtime code-mod: data placeholders, unit registry, event detector, context resolver, C# formula engine, HP/MP reconciler, and the proven control levers (pre-clamp damage/healing/MP, evade input-control, result selector, status/Brave, full forecast display: hit-% + HP amount number/bar) |
| `07-sprite-asset-pipeline.md` | sprite/asset extraction and replacement pipeline |

## Stable reference artifacts (this repo)

```text
work/baseline_jobs.csv            174 jobs, full stats (tools/dump_baseline.py)
work/baseline_abilities.csv       512 abilities, JP, CT/MP overrides (tools/dump_baseline.py)
work/item_catalog.csv             joined ItemData + weapon/armor/shield/accessory/equip-bonus
                                  tables for equipment mapping (tools/dump_item_catalog.py)
work/baseline_weapons.csv         verified local ItemWeaponData (tools/dump_weapons.py)
work/baseline_weapon_families.csv weapon-family summary (tools/summarize_weapon_baseline.py)
work/override_ability.sqlite      OverrideAbilityActionData extracted (the formula override table)
work/ability_en.sqlite            ability.en.nxd extracted (names, JP, flags)
tools/entd_tool.py                dump/patch ENTD encounter binaries
tools/guest_scan.py               scan ENTD for guest/ally slots
```

FF16Tools.CLI (extract/convert) is reused from
`D:\Projects\FFTModNewGame++\tools\FF16Tools.CLI-...\FF16Tools.CLI.exe`.

## Key paths (this machine)

```text
Game:        D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles
Exe:         ...\FFT_enhanced.exe        Data: ...\data\enhanced\*.pac
Mod loader:  C:\Reloaded-II\Mods\fftivc.utility.modloader  (TableData\, Nex\Layouts\ffto\)
Mod folders: FFTIVC/data/enhanced/nxd (NXD)   FFTIVC/tables/enhanced (TableData XML)
This mod:    D:\Projects\FFTGenericChronicle\mod\fftivc.generic.chronicle
FF16Tools:   D:\Projects\FFTModNewGame++\tools\FF16Tools.CLI-1.13.2-win-x64\win-x64\FF16Tools.CLI.exe
```

## External sources

```text
FFHacktics:        https://ffhacktics.com/wiki/Formulas , /Ability_Data , /Job_Data , /Battle_Stats
                   IVC board: https://ffhacktics.com/smf/index.php?board=85.0
Nenkai loader:     https://github.com/Nenkai/fftivc.utility.modloader
Nenkai FFT guide:  https://nenkai.github.io/ffxvi-modding/modding/creating_mods_fft/
Nenkai install:    https://nenkai.github.io/ffxvi-modding/modding/installing_mods_fft/
Nex layouts:       https://github.com/Nenkai/fftivc-nex-layouts
FF16Tools:         https://github.com/Nenkai/FF16Tools
Faith Framework:   https://github.com/Nenkai/FaithFramework
Community code mod:https://github.com/dicene/FFT_Egg_Control
PSX decomp:        https://github.com/Talcall/FFT-1997-Decomp
Cheat table:       https://github.com/bbfox0703/Mydev-Cheat-Engine-Tables (FFT_enhanced.CT)
Reloaded hooking:  https://reloaded-project.github.io/Reloaded-II/CheatSheet/CallingHookingGameFunctions/
AeroStar guide:    https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
```
