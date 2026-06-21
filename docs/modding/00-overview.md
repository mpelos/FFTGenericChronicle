# Generic Chronicle - Battle Modding Knowledge Base (Overview)

Capstone summary of everything learned about modifying the battle system of **FINAL FANTASY
TACTICS - The Ivalice Chronicles** (Steam, Enhanced v1.5.0). This is the entry point; details
live in `01`-`06`.

**Current objective of this research:** determine whether Generic Chronicle can implement
*fully custom damage formulas that depend on the attributes and equipment of BOTH the attacker
and the target* - and, if so, by which mechanism. The answer is summarized immediately below.
(The mod's eventual gameplay design is being planned separately by the author; this knowledge
base is only about *what is technically possible* and *where the battle math lives*.)

## CENTRAL QUESTION: can we write fully custom formulas using attacker + target attributes + equipment?

There are two distinct levels, and the answer differs:

```text
LEVEL 1 - Use ANY attacker/target/equipment variable, by picking from the hardcoded formula
          catalog (~100 routines) and tuning 2 byte params (X, Y) + element + status.
  STATUS: YES. Pure data, no code, no reverse-engineering.
  WHY:    The hardcoded formulas already read attacker PA/MA/Brave/Faith, target stats, weapon
          Power, and equipment-derived stats. We re-point each ability's Formula/X/Y/Element via
          the OverrideAbilityActionData NXD table and retune everything else via TableData XML.
  PROVEN: The data pipeline is confirmed LIVE on this install (see "CONFIRMED LIVE" below) -
          editing JobData.xml changed every battle unit's stats in a running battle.
  LIMIT:  You cannot invent NEW math. You choose from the fixed catalog and feed only 2 byte
          parameters. No new variable combinations, no >2 params, no custom scaling curve.

LEVEL 2 - Write an ARBITRARY new math expression of our own (any combination of any attacker +
          target attributes + equipment, any number of terms, any scaling) and inject it as the
          actual damage dealt.
          STATUS: DIRECT FORMULA HOOK BLOCKED, ALTERNATIVE CODE-MOD PATH OPEN. Reading every
          attribute of any live unit is already proven via our probe. Replacing the vanilla
          formula routine directly is blocked because that routine is Denuvo-virtualized and
          cannot be located by static AOB. However, `06-code-mod-battle-runtime-architecture.md`
          lays out a viable alternative: use safe data-layer placeholder actions, observe stable
          unit HP deltas, resolve action/equipment context, then write the final HP result from
          our own C# formula engine.
  IMPLICATION: "Hook the formula dispatcher" is not the plan anymore. "Own the final battle
          outcome through a post-damage runtime reconciler" is now the code-mod research path.
          It still needs proof gates, but it is not blocked by the missing damage prologue.
```

The rest of this document and `01`-`06` back up both conclusions in detail.

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
06-code-mod-battle-runtime-architecture.md
                                   proposed alternative to direct formula hook: data placeholders +
                                   stable unit registry + event detector + context resolver +
                                   C# formula engine + HP reconciler
```

## Tools & generated artifacts (this repo)

```text
tools/dump_baseline.py      -> work/baseline_jobs.csv (174 jobs, full stats)
                               work/baseline_abilities.csv (512 abilities, JP, CT/MP overrides)
tools/map_battle_data.py    -> work/battle_data_inventory.md (every table's fields + enum vocab)
tools/dump_item_catalog.py  -> work/item_catalog.csv (joined ItemData + weapon/armor/shield/
                               accessory/equip-bonus tables for equipment DR mapping)
tools/dump_weapons.py       -> work/baseline_weapons.csv (verified local ItemWeaponData)
tools/summarize_weapon_baseline.py
                            -> work/baseline_weapon_families.csv and
                               work/baseline_weapon_summary.md
tools/build_verified_wp_bundle.py
                            -> work/sim-inputs-v0.2-verified-wpmax.json stress audit bundle
tools/build_runtime_settings_from_sim.py
                            -> work/battle-runtime-settings.v0.2.generated.json and
                               docs/modding/examples/battle-runtime-settings.v0.2.generated.example.json
                               plus a matrix-response variant with `--response-mode matrix`
                               that stores armor-vs-damage response in `FormulaMatrices`
                               and emits formula trace variables for base/final/response values
                               plus scan-mode variants with `--slot-mode scan` and a
                               vanilla-preserving live mapping profile with
                               `--profile live-noop`
tools/build_runtime_simulation_matrix.py
                            -> docs/modding/examples/runtime-simulation-matrix.v0.2.example.json
                               base scenarios for weapon-family/action/armor-response regression
                               coverage; expectations are attached by the C# simulator. The
                               matrix-response variant lives at
                               docs/modding/examples/runtime-simulation-matrix-response.v0.2.example.json
tools/find_memtable_candidates.py
                            -> work/memtable_rip_candidates.csv (offline PE scan for
                               RIP-relative table-reference candidates near a configurable
                               stride; emits contextual AOBs and match counts; current use is
                               MemoryTableProbes research)
tools/build_memtable_probe_settings.py
                            -> work/memtable-probe-candidates.disabled.json (turns unique
                               scanner rows into disabled MemoryTableProbes settings using the
                               contextual AOB offsets)
docs/modding/examples/battle-runtime-settings.memtable-probe.disabled.example.json
                            -> disabled scaffold for persistent memory-table research; includes
                               the probe shape but no real signature
tools/analyze_battleprobe_log.py -> work/battleprobe_analysis.md (pointer-keyed unit probe
                               summary; cross-references full dump/candidate values with
                               item_catalog.csv; includes `[RUNTIME]` resolved slot/action/
                               response traces when enabled; summarizes action/slot/response
                               observations and recommends stable exact slot offsets; includes
                               `[MEMTABLE]` probe tables/rows/issues; includes an HP write-proof
                               check using `finalDamage=1`, rewrite failures, and `sampleAgeMs`;
                               includes an MP rewrite check for `[MPLOSS]`, `[MPGAIN]`,
                               `[MP-REWRITE*]`, and `[RUNTIME-MP]` evidence;
                               parses and summarizes action variables such as `swing`, `thrust`,
                               `sentinellow`, and `wp`;
                               parses runtime formula trace variables from `[RUNTIME] ... vars=...`;
                               includes a neuter placeholder check that flags large
                               `vanillaDamage` deltas during HP rewrites;
                               includes a Death Gate Outcome check that classifies HP-only,
                               zombie-candidate, and killflag evidence after lethal HP rewrites;
                               includes a DR/Response Proof Check that cross-checks slot,
                               equipment DR, response, final-rule, and formula-trace evidence;
                               warns when the log is from an old harness build)
tools/promote_runtime_offsets.py
                            -> work/battle-runtime-settings.v0.2.exact-from-log.json (promotes
                               stable `[RUNTIME]` scan observations into exact `Offset`/`Width`
                               runtime settings; `--also-policy` writes a matching exact policy
                               settings file)
tools/test_runtime_tooling.py smoke-tests the runtime log analyzer, memory-table log parsing,
                              watcher state, and exact-offset promoter
tools/test_memtable_candidates.py smoke-tests the offline RIP-relative scanner and contextual
                                 AOB/settings conversion
tools/test_neuter_data.py    smoke-tests the data-layer neuter placeholder artifacts: weapon
                              Power XML, charge/aim Power XML, damaging ability classification,
                              OverrideAbilityActionData X/Y sqlite edits, high-id fallback
                              coverage, and NXD presence
tools/test_runtime_formula_context.py
                            -> smoke-tests `work/runtime_formula_context.md` for staleness and
                               critical formula/DR/response capabilities
tools/test_runtime_profiles.py
                            -> smoke-tests `work/runtime_profile_audit.md` for staleness and
                               critical profile-role invariants: live-noop, dry-run, death gate,
                               GURPS DR, policy, and MEMTABLE-disabled safety
tools/report_neuter_coverage.py
                            -> work/neuter_coverage.md (human-readable neuter coverage report:
                               weapon count, charge/aim count, ability count, spot checks,
                               high-id fallback actions, and residual runtime risks)
tools/report_runtime_formula_context.py
                            -> work/runtime_formula_context.md (generated formula capability
                               catalog: expression operators/functions, top-level HP/MP vars,
                               target/attacker/action/slot/item metadata vars, response/result
                               vars, and settings-defined extension points)
tools/report_runtime_profiles.py
                            -> work/runtime_profile_audit.md (generated runtime-settings profile
                               audit: role, live mutation risk, and invariant checks for the
                               profiles used in live mapping, dry-run evaluation, death gates,
                               DR/response policy, GURPS proof, and MEMTABLE probing)
tools/watch_live_mapping.py waits for fresh `[RUNTIME]` live evidence, reports memory-table probe
                            evidence plus optional action-signal/action-variable, `[REWRITE]`,
                            target/attacker slot-present evidence, positive equipment DR,
                            response-rule evidence, formula trace variables, placeholder-sized HP
                            damage rewrite, HP healing rewrite, MP loss/gain rewrite, lethal HP
                            rewrite, and `[DEATH-*]` evidence when requested;
                            can fail fast on rewrite failures, death-write failures, unexpected
                            death events, or large vanilla HP rewrites; supports a short settle
                            window for negative death-gate checks;
                            can also wait for `[MEMTABLE-FOUND]`/`[MEMTABLE-ROW]`,
                            then run analysis and exact-offset promotion
codemod/prepare-live-mapping.ps1 builds/deploys the code mod with the scan live-noop profile,
                               validates runtime settings first, archives stale game-side battle
                               logs before a live mapping run, supports `-DryRun`, and prints the
                               slot/response watcher + promotion commands
codemod/build-deploy.ps1 explicitly deploys the code mod into Reloaded-II. Plain `dotnet build`
                         now writes only to `codemod/_build/`, so offline tests do not touch an
                         open Reloaded-II install. Any `-RuntimeSettings` input is validated
                         before it is copied unless `-SkipRuntimeSettingsValidation` is used.
codemod/check-live-readiness.ps1 read-only diagnostic for app config, loaded process modules,
                                 installed code-mod DLL hash vs local build, installed runtime
                                 settings hash vs known live-mapping profiles, generated `modded`
                                 packs, runtime log, and the next watcher/analyzer commands
codemod/check-death-gate-readiness.ps1
                            -> read-only Test 2b diagnostic: validates neuter artifacts, death
                               runtime settings, AppConfig enabled mods, installed data-mod hashes,
                               installed code-mod DLL hash vs local build, installed settings
                               presence, identifies which death-gate runtime profile is installed
                               by hash, and prints watcher commands
codemod/prepare-dry-run-evaluation.ps1
                            -> explicit live-safe dry-run proof helper: validates/simulates the
                               dry-run HP/MP profile, deploys only the code mod/runtime settings
                               when run without `-DryRun`, archives the old log, refuses to run
                               while Reloaded-II/FFT is open, and prints the HP/MP watcher command;
                               `-DryRun` still runs the read-only validations/simulation
codemod/prepare-death-gate.ps1
                            -> explicit Test 2b preparation helper: rebuilds neuter NXD, validates
                               artifacts/settings, deploys data mod + code mod with the selected
                               death profile, archives the old log, and refuses to run while
                               Reloaded-II/FFT is open; `-DryRun` runs read-only validations/simulation
                               and prints the plan without copying/deploying/rebuilding
codemod/restore-fft-reloaded-mods.ps1 restores the known FFT Reloaded-II enabled mod list, but
                                      refuses to edit while Reloaded-II or FFT is running
codemod/fftivc.generic.chronicle.codemod.smoketests
                            -> offline formula-runtime smoke tests for expression math,
                               catalog-backed DR, optional attacker context, attacker weapon
                               metadata, configurable formula tables/matrices/maps, deterministic dice
                               helpers, event-seeded dice rolls, integer ratio/wounding helpers,
                               raw signed/unsigned memory reads, bitfield extraction, and
                               status flag helpers,
                               ordered pre-action/pre-response/derived formula variables,
                               formula-gated HP rewrite ownership,
                               lazy if/and/or guards for optional context,
                               signed HP/damage/healing events, MP-aware unit context,
                               sentinel action inference,
                               slot-aware formula-coded action signals, action-typed equipment DR,
                               formula-gated damage/DR rules, resolved runtime trace logging with
                               custom formula trace variables, rewrite echo suppression,
                               recent-unit attacker inference,
                               configurable memory-table probe parsing/row reads, analyzer
                               summaries, runtime settings validation, and settings-load failure
                               safety
codemod/fftivc.generic.chronicle.codemod.settingsvalidate
                            -> offline runtime settings validator. Loads JSON with the same
                               RuntimeSettings parser, validates formulas/tables/matrices/
                               probes/ranges/slots, and evaluates synthetic damage/healing
                               contexts plus missing-attacker/action fallback risk before any
                               live test.
codemod/fftivc.generic.chronicle.codemod.settingssimulate
                            -> offline BattleFormulaEngine simulator. Runs runtime settings
                               against JSON scenarios and reports finalDamage/desiredHp/runtime
                               trace without launching the game. Scenario "expect" blocks turn
                               it into a regression gate for generated balance policies.
codemod/run-offline-checks.ps1
                            -> offline-only regression runner. Runs Python syntax/tooling tests,
                               neuter artifact QA, JSON parsing, C# build/smoke tests, settings validation,
                               short + matrix scenario simulation, scan-slot matrix comparison,
                               matrix-response simulation, GURPS-DR proof simulation, MP
                               simulation, static DR proof simulation, sentinel-band action
                               proof simulation, neuter spot-check simulation, death-gate HP/KO
                               profile simulation, dry-run HP/MP simulation, live helper dry-runs,
                               runtime formula-context/profile-audit report staleness, and git
                               diff whitespace checks
                               without deploying to Reloaded-II or launching the game.
docs/modding/examples/battle-runtime-settings.gurps-dr.example.json
                            -> subtractive DR proof profile: GURPS-like swing/thrust tables,
                               real item-catalog armor DR, post-DR wound multipliers, and trace
                               variables for each intermediate.
docs/modding/examples/battle-runtime-settings.static-dr.example.json
                            -> simplest static DR proof profile: `FlatDamageReduction` reduces
                               positive HP damage without attacker/equipment context.
docs/modding/examples/battle-runtime-settings.mp.example.json
                            -> MP rewrite proof profile: signed MP loss/gain formulas, MP
                               action-signal classification, and MpRules selection.
docs/modding/examples/battle-runtime-settings.sentinel-bands.example.json
                            -> ActionSignalRules proof profile: placeholder-sized vanilla
                               damage bands become action variables that drive final formulas,
                               while unknown bands fall back to vanillaDamage.
docs/modding/examples/battle-runtime-settings.dry-run.example.json
                            -> live-safety profile for mapping/evaluation. Computes HP/MP rewrite
                               decisions and traces while `DryRunRewrites` prevents memory writes.
docs/modding/examples/runtime-simulation-scenarios.example.json
                            -> sample simulator scenarios for sword/leather damage,
                               no-attacker vanilla fallback, and signed healing event coverage,
                               with assertions for v0.2 generated settings.
docs/modding/examples/runtime-simulation-matrix.v0.2.example.json
                            -> 20-scenario v0.2 regression matrix covering swing/thrust/crush/
                               missile/fists against plate/mail/leather/cloth.
docs/modding/examples/runtime-simulation-matrix-response.v0.2.example.json
                            -> same 20-scenario matrix, but asserting the single
                               `FormulaMatrices`-backed `DamageResponse(matrix response)` route.
docs/modding/examples/runtime-simulation-gurps-dr.example.json
                            -> five-scenario proof that subtractive armor DR can reduce,
                               partially absorb, or fully stop swing/thrust/fist damage before
                               GURPS-like wound multipliers.
docs/modding/examples/runtime-simulation-static-dr.example.json
                            -> simulator assertions for `FlatDamageReduction`: normal damage,
                               chip damage clamped to 0, and healing left alone.
docs/modding/examples/runtime-simulation-mp.example.json
                            -> MP simulator scenarios with assertions for signed MP loss/gain
                               rewrites and MpRules fallback behavior.
docs/modding/examples/runtime-simulation-sentinel-bands.example.json
                            -> simulator assertions that vanilla damage bands select low/mid/high
                               sentinel actions and preserve unknown-band fallback behavior.
docs/modding/examples/runtime-simulation-dry-run.example.json
                            -> simulator assertions for the live-safe dry-run profile covering
                               HP damage, HP healing, MP loss, and MP gain rewrite decisions.
docs/modding/examples/runtime-simulation-neuter-spotcheck.example.json
                            -> dry-run neuter spot-check scenarios asserting placeholder damage
                               is preserved as `finalDamage=1`, including ally observation and
                               healing skip behavior.
docs/modding/examples/runtime-simulation-death-gate.example.json
                            -> death-gate simulator scenarios asserting that the HP-only and
                               KO-flag profiles force foes to 0 HP from neuter placeholder
                               damage while preserving allies and healing events.
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

### CONFIRMED LIVE on this install (probe code mod, high confidence)
```text
- Runtime battle-unit struct offsets ALL verified live (probe iter 3): HP +0x30, MaxHP +0x32,
  MP +0x34, MaxMP +0x36, PA +0x3E, MA +0x3F, Speed +0x40, Move +0x42, Jump +0x43, Level +0x29,
  Brave +0x2B, Faith +0x2D, team +0x04, foe bit +0x05&0x10 - all read sane, consistent values.
- 5 of 7 AOBs are stable real-.text anchors (battle_base_ptr 0x226D98, damage_mult_2 0x30A685,
  jp 0x283754, xp 0x283767, min_spd_jmp_mov 0x36027F). We can read any unit live + measure damage
  via HP deltas (Denuvo-proof). See 04-re-strategy.md.
```

### From external sources, NOT verified on this build (medium confidence)
```text
- The damage-routine itself: damage_multiplier AOB is UNSTABLE (Denuvo-relocated) - cannot be
  reliably AOB-hooked. Tier-2 custom math needs runtime tracing, not static AOB. See 04.
- Formula-id math, weapon XA table, stat divisor 1638400, Zodiac/Protect ratios - FFHacktics/
  AeroStar (PSX/WotL); assumed preserved in IVC.
- IVC rebalances (enemies ~-30% dmg, allies ~+20%, CT/MP/JP changes) - community-reported.
- read-site VA 0x14EEA6E50 - from Nenkai's layout comment, tagged "patch 1"; build-specific.
- PSX decomp (Talcall) - conceptual map only; addresses/packing differ on x64.
```

### RESOLVED since first draft (now proven live on this install)
```text
- TableData XML pipeline PROVEN LIVE (2026-06-20): a JobData.xml proof patch (all jobs Move/Jump
  edited) changed every battle unit from vanilla (Mv5/Jp3) to the modded values mid-battle,
  observed via the probe harness. The data-merge layer works end-to-end.
- OverrideAbilityActionData Formula/X/Y READ PROVEN LIVE (2026-06-21): an NXD with Y=99 on
  Cure/Fire/Thunder/Blizzard made Fire damage explode in battle (JobData Move=9 as positive
  control). The exe reads override Formula/X/Y for damage magnitude, not just CT/MP. So the ENTIRE
  Level-1 data formula layer is confirmed: JobData + OverrideAbilityActionData + ItemWeaponData are
  all re-pointable with pure data. See 07-live-findings.md (Test D).
- Struct offsets CONFIRMED LIVE (probe harness): all unit fields read sane/consistent values.
  Caveat observed: edited Move=9/Jump=9 showed as Mv9-10/Jp7 in-battle - there are additional
  modifiers/clamps stacked on the JobData base (support abilities like Move+1, and/or an engine
  Jump clamp; genericjobs was also loaded). The edit clearly took effect; the exact final value
  is post-processed by the engine.
```

### Genuinely requires the game / reverse-engineering (open)
```text
- Vanilla baseline Formula/X/Y per ability (exe-hardcoded; not in data).
- The formula-dispatch switch/jump table (downstream of the read-site) - not located.
- Exact IVC truncation order / damage-variance variant.
- LEVEL 2 (arbitrary custom math): the damage routine is Denuvo-virtualized and could not be
  located by static AOB scan. Locating it (if possible) needs runtime debugger tracing. This is
  the blocker for writing our own formula algorithm. See 03/04.
```

## Open questions / recommended next steps

```text
1. [DONE] Proof patch validating the data pipeline (JobData.xml Move/Jump) - confirmed live.
2. [DONE] Confirm the EXE reads override Formula/X/Y from OverrideAbilityActionData - Live Test D
   proved damage magnitude changes through the override NXD.
3. [NEXT - LEVEL 2 death gate] Continue `06`/`07`: with weapon + ability + charge/aim neuter
   deployed, run
   Test 2b to prove whether a runtime lethal result can kill by writing HP=0 alone, or whether it
   must also write the mapped KO flag (`BattleUnit +0x61 |= 0x20`). This decides whether the
   runtime can own lethal outcomes after vanilla damage is made harmless.
4. [OFFLINE/LIVE BRIDGE] Continue the independent roster/unit-table research branch suggested by
   community prior art: the configurable `MemoryTableProbes` framework now exists, but it still
   needs an independently discovered/validated pattern and a live correlation pass to connect
   battle-unit pointers with persistent roster/ENTD slots. The analyzer/watcher now understand
   `[MEMTABLE]` evidence, so the next live pass should produce a readable report instead of raw
   log spelunking.
5. Use `tools/dump_item_catalog.py` + `tools/analyze_battleprobe_log.py` after a restarted
   current-DLL probe session to identify which unknown unit offsets contain equipped item ids.
6. Run the offline formula smoke test after code changes:
   `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release`.
7. Fill the ability design baseline with FFHacktics WotL Formula/X/Y values (the data lacks them).
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
