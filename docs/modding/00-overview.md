# Generic Chronicle - Battle Modding Knowledge Base (Overview)

Capstone summary of everything learned about modifying the battle system of **FINAL FANTASY
TACTICS - The Ivalice Chronicles** (Steam, Enhanced v1.5.0). This is the entry point; details
live in `01`-`05`. The goal of the mod: an ultra-hard, fully restructured battle system
(damage formulas, jobs, skills, status, items, encounters).

Crucially, this doc also states **how confident we are in each fact** and **what still needs the
game running** - see the Provenance section. Almost the entire *static data map* was obtained
without launching the game; some runtime/formula facts came from external sources or still need
in-game validation.

## The big picture: 4 access layers

```text
1. Hardcoded formula routines (FFT_enhanced.exe)
   The math each "Formula id" computes (classic WotL catalog, ids 0x00-0x64). Changing the math
   = exe/code mod. We normally just re-point abilities to existing formulas.

2. Editable data tables (the main surface, NO exe needed)
   a) Nex/NXD - per-ability action data (Formula/X/Y/Element/Range/AoE/Vertical/Status/CT/MP)
      in OverrideAbilityActionData; plus UI/text tables (ability/job/item .nxd).
   b) TableData XML (mod loader) - jobs, weapons, armor, status, skillsets, items, shops, spawns.

3. Runtime battle-unit struct (live memory)
   Per-unit HP/MP/PA/MA/Speed/Brave/Faith/Level/etc. A Tier-2 code hook can read/rewrite these
   and the computed damage. This is how arbitrary custom math is done.

4. Encounter data (ENTD, fftpack/*.bin)
   Per-battle roster: jobs, levels, gear, skills, placement. Already proven in New Game++.
```

## Key conclusions

- **~90% of a battle overhaul is data-only** (no exe): re-point every ability's formula, retune
  X/Y/Element/Status/CT/MP via `OverrideAbilityActionData`, rebuild every job via `JobData.xml`,
  rework weapons/armor/status/skillsets via TableData XML, rebuild encounters via ENTD.
- **Are we free to write arbitrary math? In data, no.** We pick from a fixed catalog of ~100
  hardcoded formula routines and feed two byte parameters (X, Y) + element/status. Truly free
  math (new variables, >2 params, new scaling) requires a **Tier-2 code mod** that hooks the
  damage routine.
- **Tier 2 is feasible but first-of-its-kind.** No modding API exposes the formula/damage hook
  (loader managers only patch data tables; Faith Framework is a live Nex editor). You write your
  own Reloaded-II `CreateHook`. The hook mechanism is proven on this exe; the cost is the
  reverse-engineering of the damage routine, which is well-scoped now (struct offsets, AOBs, a
  PSX decomp map, and the formula fingerprint sheet are all in hand).
- **The classic FFT/WotL mods help as a knowledge map, not as portable code** (PSX/PSP MIPS vs
  x64 reimplementation). They give the exact math, struct layout, dispatch architecture, and a
  validation oracle - turning the RE from blind to guided.
- **Base per-ability Formula/X/Y is NOT in any data file** - it's exe-hardcoded; the override
  table is sparse (`-1` = inherit). Use FFHacktics WotL data as the design baseline.

## Document index

```text
00-overview.md                     (this file) synthesis, index, provenance, next steps
01-battle-formula-research.md      the 4 layers; verified local paths; edit workflows; JP decode;
                                   where the vanilla baseline is (and isn't)
02-formula-id-catalog.md           formula ids 0x00-0x64 and their math; weapon XA table; magic;
                                   stat derivation; Tier-1 vs Tier-2 variable availability
03-custom-formula-feasibility.md   Tier-2 reality: no API exposes it; Faith Framework scope;
                                   Reloaded hook pattern; prior art (FFTacticsFix)
04-re-strategy.md                  using classic knowledge for RE: PSX decomp, cheat-table struct
                                   offsets + AOBs, formula fingerprint sheet, attack path, Denuvo
05-battle-data-map.md              THE master variable map: every accessible field by domain +
                                   full enum vocabularies + hard limits + how to access each
```

## Tools & generated artifacts (this repo)

```text
tools/dump_baseline.py      -> work/baseline_jobs.csv (174 jobs, full stats)
                               work/baseline_abilities.csv (512 abilities, JP, CT/MP overrides)
tools/map_battle_data.py    -> work/battle_data_inventory.md (every table's fields + enum vocab)
tools/entd_tool.py          dump/patch ENTD encounter binaries (from New Game++)
tools/guest_scan.py         scan ENTD for guest/ally slots (from New Game++)
work/override_ability.sqlite OverrideAbilityActionData extracted (the formula override table)
work/ability_en.sqlite       ability.en.nxd extracted (names, JP, flags)
```

FF16Tools.CLI (extract/convert) is reused from `D:\Projects\FFTModNewGame++\tools\FF16Tools...`.

## Provenance & confidence (what's solid vs. what still needs the game)

### Verified directly from files on this machine (high confidence)
```text
- All TableData XML schemas + enum vocabularies (mod loader templates).
- All Nex table column schemas (.layout files).
- Real NXD values: OverrideAbilityActionData (368 rows, sparse -1; vanilla overrides only CT x28,
  MP x4), ability.en (512 rows).
- Baseline job stats (174) and ability JP/flags (512).
- JP decode: JP = JpCost1 + 256*JpCost2 (matched known WotL costs).
- Table-size caps and field types/ranges.
- Mod folder paths (FFTIVC/data/enhanced/nxd, FFTIVC/tables/enhanced) - sample mod confirms.
```

### From external sources, NOT verified on this build (medium confidence)
```text
- Runtime battle-unit struct offsets (HP +0x30, PA +0x3E, ...) - from a public cheat table
  (Steam v1.0 Oct 2025). Build-specific; re-scan to confirm on the current install.
- AOB patterns / damage-multiplier site / [rax+0x06] store - same cheat table.
- Formula-id math, weapon XA table, stat divisor 1638400, Zodiac/Protect ratios - FFHacktics/
  AeroStar (PSX/WotL); assumed preserved in IVC.
- IVC rebalances (enemies ~-30% dmg, allies ~+20%, CT/MP/JP changes) - community-reported.
- read-site VA 0x14EEA6E50 - from Nenkai's layout comment, tagged "patch 1"; build-specific.
- PSX decomp (Talcall) - conceptual map only; addresses/packing differ on x64.
```

### Genuinely requires the game / reverse-engineering (open)
```text
- Vanilla baseline Formula/X/Y per ability (exe-hardcoded; not in data).
- The formula-dispatch switch/jump table (downstream of the read-site) - not located.
- Exact IVC truncation order / damage-variance variant.
- End-to-end proof that editing OverrideAbilityActionData / TableData XML actually changes the
  running game (no proof patch deployed/tested yet for this mod).
- Live confirmation of the struct offsets on this install.
```

## Open questions / recommended next steps

```text
1. Proof patch (validates the whole data pipeline): edit one harmless ability's Formula/X/Y in
   OverrideAbilityActionData (or one job/weapon via XML), deploy, test in a save.
2. Fill the ability design baseline with FFHacktics WotL Formula/X/Y values (the data lacks them).
3. Tier-2 skeleton: minimal Reloaded-II C# mod, AOB-scan the damage site, log attacker/target via
   the struct offsets to confirm them live and start locating the formula dispatch.
4. Only then start designing the actual battle system on top of this map.
```

## Key paths (this machine)

```text
Game:        D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles
Exe:         ...\FFT_enhanced.exe        Data: ...\data\enhanced\*.pac
Mod loader:  C:\Reloaded-II\Mods\fftivc.utility.modloader  (TableData\, Nex\Layouts\ffto\)
This mod:    D:\Projects\FFTGenericChronicle\mod\fftivc.generic.chronicle
FF16Tools:   D:\Projects\FFTModNewGame++\tools\FF16Tools.CLI-1.13.2-win-x64\win-x64\FF16Tools.CLI.exe
```

## External sources

```text
FFHacktics:        https://ffhacktics.com/wiki/Formulas , /Ability_Data , /Job_Data , /Battle_Stats
                   IVC board: https://ffhacktics.com/smf/index.php?board=85.0
Nenkai loader:     https://github.com/Nenkai/fftivc.utility.modloader
Nenkai FFT guide:  https://nenkai.github.io/ffxvi-modding/modding/creating_mods_fft/
Nex layouts:       https://github.com/Nenkai/fftivc-nex-layouts
FF16Tools:         https://github.com/Nenkai/FF16Tools
Faith Framework:   https://github.com/Nenkai/FaithFramework
PSX decomp:        https://github.com/Talcall/FFT-1997-Decomp
Cheat table:       https://github.com/bbfox0703/Mydev-Cheat-Engine-Tables (FFT_enhanced.CT)
Reloaded hooking:  https://reloaded-project.github.io/Reloaded-II/CheatSheet/CallingHookingGameFunctions/
AeroStar guide:    https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
```
