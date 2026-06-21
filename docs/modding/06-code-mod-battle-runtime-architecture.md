# Code-Mod Battle Runtime Architecture

Status: implemented offline/runtime architecture, with live-proven HP rewrite, engine-owned death,
and CT-based attacker resolution. Remaining live gates are action-identity calibration, exact
equipment mapping, hook-register context clues, and any future same-hit/pre-damage path.

## Verdict

The most viable code-mod route is no longer "replace the vanilla damage formula function". On
this build, that function is not a stable x64 target. The viable route is:

```text
Own the final battle result, not the virtualized formula routine.
```

Generic Chronicle can still become a flexible battle-mechanics code mod through the runtime layer
now in this repo:

1. use the data mod to make vanilla actions safe and predictable;
2. observe real battle events through stable, non-virtualized unit touchpoints;
3. resolve attacker/target/action/equipment context as far as current evidence allows;
4. compute custom formulas in C#;
5. reconcile HP/MP after vanilla applies its placeholder result, while leaving true death/KO to
   the engine (`MinHpFloor=1`) until a pre-damage/same-hit path is found.

This is still a code mod. The custom math lives in our Reloaded-II C# runtime. The difference is
that we avoid the Denuvo-virtualized formula dispatcher and use stable data/runtime boundaries.

## Current evidence

Confirmed locally:

- `battle_base_ptr` at `module+0x226D98` is stable and hookable. `rcx` is a live battle-unit
  pointer. The current harness reads HP, MaxHP, MP, PA, MA, Speed, CT, Move, Jump, Brave, Faith,
  level, team, and foe bit from that pointer.
- HP deltas are observable without touching the damage routine.
- HP rewrites work, but direct HP=0/KO-flag writes do **not** trigger real death. The accepted
  architecture is "engine owns death, runtime owns the number": custom lethal results floor at
  1 HP and a later vanilla chip lets the engine perform KO.
- The attacker is resolved live by CT reset (`+0x41`), with a counter-inversion fallback for
  reaction attacks that do not consume CT.
- Coarse action identity is implemented offline through an opt-in sentinel data mode and
  `ActionSignalRules`; live calibration is pending.
- A short observe-only hook-register probe can log x64 register snapshots at `battle_base_ptr`
  to look for a current actor/action-context pointer without touching virtualized damage code.
- The mod loader publishes table managers as Reloaded-II controllers. Our code mod can fetch
  modded item/job/weapon/armor tables instead of duplicating all static data.
- `ItemData` exposes `ItemCategory`, `AdditionalDataId`, and `EquipBonusId`; `ItemArmorData`
  exposes HP/MP bonus; `ItemWeaponData` exposes weapon formula/power; `ItemEquipBonusData`
  exposes PA/MA/Speed/Move/Jump/status/element bonuses. This is enough to host a custom DR table
  keyed by item id, item category, or equip bonus id once the unit's equipped item ids are mapped.
- The live log showed repeated `id=0x80` enemies with different stats. Therefore unit identity
  must be the unit pointer, not the character id. The harness has been updated accordingly.

New local probe update:

- `Mod.cs` now copies `rcx` into the buffer and keys unit history by pointer.
- `Mod.cs` now copies `0x00..0x17F` instead of only the known stat block, so the next run can
  compare a wider suspected equipment/status/pointer/action region.
- `Mod.cs` now has an opt-in HP rewrite proof gate. By default it is off and the harness remains
  observational. If `battle-runtime-settings.json` is placed next to the installed code mod with
  `RewriteObservedDamage=true`, the polling thread can rewrite observed HP after vanilla damage.
- `Mod.cs` now treats HP changes as signed events: damage is positive final damage, healing is
  negative final damage. Healing rewrite is opt-in through `RewriteObservedHealing=true`, so
  existing damage-only settings keep their old behavior.
- `Mod.cs` now also observes MP deltas and can optionally reconcile them through
  `RewriteObservedMpLoss` / `RewriteObservedMpGain`. MP remains off by default and uses a signed
  `FinalMpChangeFormula`: negative values spend/drain MP, positive values restore MP.
- After Live Test 1 proved HP writes but exposed stale turn-sampled baselines, `Mod.cs` now keeps
  a registry of every battle-unit pointer seen by the hook and directly polls every registered
  pointer about every 25 ms. The hook still supplies recent touch timestamps for the experimental
  attacker resolver, but HP/MP event detection now uses fresh live reads instead of a single
  hook-fed buffer snapshot.
- `Mod.cs` now routes each observed HP event through a small C# formula engine:
  `UnitSnapshot -> DamageEvent -> DamageRule/BattleFormulaEngine -> DamageResult -> HP write`.
  Live events are still target-only until attacker/action context is mapped, but `DamageEvent`
  now has an optional attacker slot and the engine shape is the one we will use for real formulas.
- `Mod.cs` now supports configurable integer formulas through `FinalDamageFormula` globally or
  per `DamageRules` entry. The expression context currently includes signed vanilla damage,
  vanilla healing, target stats, optional attacker stats, optional action signal variables,
  target/attacker raw bytes/words, configured equipment slots, custom constants, and computed
  `equipmentDr` plus computed percent/type response variables such as
  `damageResponse.permille` and `boundedResponse.permille`. This
  moves the proof runtime from hardcoded rules toward a real formula layer.
- `RuntimeSettings` now supports `RewriteConditionFormula`, a formula gate evaluated after
  action, equipment DR, response, and derived variables are prepared but before HP is rewritten.
  A zero result intentionally preserves vanilla HP for that event.
- `RuntimeSettings` now supports `ActionSignalRules`. This is the implemented sentinel-channel
  bridge: a controlled vanilla damage or MP delta can classify an observed event as
  `action.swing`, `action.thrust`, `action.cut`, `action.spell`, etc., before a true ability-id
  hook exists.
- `ActionSignalRules` can now use `EventKind`, `ConditionFormula`, and `VariableFormulas`, so
  sentinel/context events can be limited to HP damage, healing, MP loss, or MP gain, and can
  derive action metadata such as power, wound multiplier, or healing amount from the observed
  HP/MP event, attacker weapon, target equipment, and item catalog metadata instead of only
  installing fixed constants.
- `DamageEvent` now has an optional attacker slot. The live runtime has an opt-in experimental
  recent-unit attacker resolver: by default it only logs `[CTX]` candidates; if
  `InferAttackerFromRecentUnits=true`, the best recent candidate is passed into formulas as
  `a.*`/`attacker.*`. This is a bridge for controlled tests, not the final action-context hook.
- `RuntimeSettings` now supports `AttackerEquipmentSlots`. When attacker context exists, formulas
  can read attacker weapon/item metadata through `aslot.*` / `attackerSlot.*`. When attacker
  context is absent, configured attacker slots resolve to safe zero/default variables instead of
  crashing the formula.
- `EquipmentSlots` and `AttackerEquipmentSlots` now support a conservative catalog scan mode.
  If a slot has no fixed `Offset`, it can search a raw snapshot range for exactly one item id
  matching filters such as `SecondaryKind=weapon`, `TypeFlag=Armor`, or `ItemCategory=Sword`.
  Ambiguous scans expose `slot.<name>.scanMatches` and do not mark the slot present unless
  `AllowAmbiguousSearchMatch=true`.
- `RuntimeSettings` now supports `FormulaTables`, integer lookup tables callable from formulas.
  This is the intended path for GURPS-like swing/thrust tables without hardcoding those tables in
  C#.
- `RuntimeSettings` now supports `FormulaMaps`, sparse integer lookup maps callable from
  formulas. This is the compact path for per-item DR, per-weapon damage class, special item
  tags, or other id-keyed mechanics where a dense `FormulaTables` array would be noisy.
- `RuntimeSettings` now supports `FormulaDerivedVariables`, an ordered list of expression-backed
  temporary variables. This keeps complex formulas readable, e.g. derive `penetrating`,
  `wound.num`, and `wound.den`, then make `FinalDamageFormula` simply reference `finalDamage`.
- `RuntimeSettings` now supports `FormulaPreActionVariables`, an ordered list of
  expression-backed variables evaluated after unit slots are read but before `ActionSignalRules`
  choose action metadata. Use this for reusable action-selection tags such as `pre.weaponBlade`,
  `pre.unarmed`, or `pre.targetBodyArmor`.
- `RuntimeSettings` now supports `FormulaPreResponseVariables`, an ordered list of
  expression-backed variables evaluated after slots/action context exist but before equipment DR,
  percent/type response rules, and final damage formulas. This is the preferred way to derive
  reusable tags such as `armor.plate` or `armor.mail` without duplicating item filters in every
  response rule.
- `DamageRules` now support `EventKind`, and `DamageRules` / `EquipmentDrRules` support
  `ConditionFormula`, so rule selection itself can be limited to damage or healing and then be
  formula-driven using attacker, target, action, slot, item, table, and derived variables instead
  of relying only on fixed match fields.
- `RuntimeSettings` now supports `DamageResponseRules`, the C-bounded mitigation path for the
  accepted v0.2 policy. Response rules multiply a combined permille value, expose it to formulas,
  can be auto-applied with a clamp/chip floor, and can match action variables plus target
  equipment/item metadata. This is now the preferred armor/type model; flat DR remains available
  for rare effects and proofs.
- The expression engine now has deterministic dice helpers (`diceMin`, `diceMax`, `diceAvg`,
  `diceAvgRound`, `diceAvgCeil`) so a swing/thrust table can store dice count and modifier
  separately, e.g. `2d+1`, while keeping formula evaluation reproducible.
- The expression engine now also has event-seeded dice/random helpers (`diceRoll`, `rand`) for
  real GURPS-like variance. Rolls are pseudo-random but deterministic for a given damage event
  seed, making logs and smoke tests reproducible. Use `randAt` or `diceRollAt` when a roll needs
  a stable numeric stream that does not depend on formula evaluation order.
- The expression engine now has integer ratio helpers (`floorDiv`, `ceilDiv`, `roundDiv`,
  `mulDiv`, `mulDivCeil`, `mulDivRound`) so GURPS-style wound multipliers such as cutting
  `x1.5` can be expressed without ad hoc arithmetic.
- `Mod.cs` now logs unknown-field mapping helpers for the suspected equipment/status/action
  region. First sight of each unit emits `[DUMP ...]` plus `[CANDIDATES ...]` for non-zero bytes
  and plausible 16-bit ids, and later changes emit `[DIFF ...]` by offset. The analyzer parses
  the full dump, so item-id hits are no longer limited to the truncated candidate summary.
- `Mod.cs` now has an inactive-by-default equipment DR path. Once equipment offsets are mapped,
  `EquipmentSlots` can read item ids from target raw bytes and `EquipmentDrRules` can subtract DR
  by item id before HP is rewritten.
- `tools/dump_item_catalog.py` now generates `work/item_catalog.csv`, a joined static catalog of
  global item ids, item categories, weapon/armor/shield/accessory secondary data, and equip
  bonuses.
- The code-mod project copies `work/item_catalog.csv` into the installed Reloaded-II mod folder as
  `item_catalog.csv` when present. `Mod.cs` loads that catalog at startup and can use it for DR
  rules and formula variables.
- `Mod.cs` now hot-reloads `battle-runtime-settings.json` and `item_catalog.csv` roughly once per
  second while the harness is running. Invalid settings JSON logs `[SETTINGS-RELOAD-FAILED]` and
  keeps the previous runtime snapshot instead of replacing it with a broken one.
- `tools/analyze_battleprobe_log.py` summarizes the probe log into
  `work/battleprobe_analysis.md`, including units, candidate fields, candidate item-id hits, diff
  offset frequencies, damage events, rewrite events, parsed `[RUNTIME]` context, and
  `[MEMTABLE]` probe results.
- `dotnet build -c Release` now builds offline into `codemod/_build/` by default. Deploying into
  Reloaded-II is explicit through `codemod/build-deploy.ps1`, which passes
  `DeployToReloaded=true`. This prevents routine smoke-test builds from touching an open
  Reloaded-II install. When `build-deploy.ps1` receives `-RuntimeSettings`, it validates that
  settings file before build/copy unless `-SkipRuntimeSettingsValidation` is explicitly used.

Community/source research update:

- Nenkai's `fftivc.utility.modloader` source confirms the loader is intentionally table/packs
  oriented. It publishes table managers as Reloaded-II controllers for abilities, item data,
  weapon/armor/shield data, jobs, commands, statuses, spawns, and pack management. That supports
  our static-data layer and catalog bridge, but it does not expose battle action context or a
  custom damage callback.
- The public `FFT_Egg_Control` code mod is useful prior art for safe IVC code-mod style: it uses
  Reloaded-II controllers, startup signature scans, and `Reloaded.Memory` reads to locate a
  persistent unit table. It reads up to 55 units with a stride of `0x258`, including fields such
  as sprite set, unit index, job, and a second index-like byte. We should treat this as a
  community clue, not copy AGPL code or signatures into this project.
- Internet/community search on 2026-06-21 still surfaced `FFT_Egg_Control` as the only public
  code-mod lead relevant to live unit-table access. Other visible results were trainers, Cheat
  Engine tables, data mods, texture/config mods, or Reloaded-II setup issues. I did not find a
  public IVC modding API or code mod that exposes a custom battle-formula callback, DR hook, or
  resolved attacker/target equipment context.
- New offline hypothesis: a future independent/configurable `RosterUnitData` probe could map
  battle units back to persistent roster/ENTD slots. If confirmed, it may solve equipment lookup
  more cleanly than scanning each live `BattleUnit` dump for item ids.
- Implemented follow-up: `RuntimeSettings.MemoryTableProbes` is now an opt-in, configurable
  RIP-relative memory-table probe framework. It is disabled by default, takes patterns from JSON,
  checks target pages with `VirtualQuery` before reading, and can log named fields from rows with
  configurable `Count`, `Stride`, `Offset`, `Width`, and `EmptyValue`. No community signature is
  embedded in the codebase.
- Nenkai's FFT install guide and loader README both state that generated `modded` files in the
  game's `data` folders are the on-disk mod packs. This matches the local behavior observed after
  launching with the modloader. Deleting/removing those packs is a valid cleanup step, but normal
  launches with the modloader regenerate them.

Live memory classification from the current running process:

```text
module+0x226D98     battle_base_ptr       PAGE_EXECUTE_READ       stable low .text area
module+0xEEA6E50    Override read-site    PAGE_EXECUTE_WRITECOPY  high image region
```

The override read-site remains a useful lead, but it should not be our primary hook until proven
stable across launches. The stable unit touchpoint is our current base.

## Architecture

### 1. Data layer: safe vanilla placeholder

The data mod should eventually set damaging abilities to a controlled vanilla result where
possible:

- low nonlethal damage for damage actions;
- low nonlethal healing for healing actions;
- predictable action flags/range/area/element/status;
- vanilla formulas kept close enough that AI/preview are not totally absurd during early tests.

Reason: if vanilla can kill, crystallize, trigger reaction chains, or apply the wrong side effect
before our code corrects HP, the post-damage engine will be fighting state that already changed.
A placeholder result gives us a clean event signal and leaves the real outcome to our runtime.

Open gate: CLOSED. Live Test D (2026-06-21) proved the exe reads `OverrideAbilityActionData
Formula/X/Y` for damage magnitude, not only CT/MP (see `07-live-findings.md`). The data lever for
the placeholder exists end to end.

Status of the placeholder (FIX PASS 1, 2026-06-21):

- **Weapon half BUILT.** `tools/build_neuter_data.py` ->
  `mod/fftivc.generic.chronicle/FFTIVC/tables/enhanced/ItemWeaponData.xml` forces every weapon
  `Power=1`, so weapon-power attacks become tiny/non-lethal. Removes the death race + flicker for
  human physical attacks.
- **Ability/spell + monster half BUILT.** Same generator builds the
  `OverrideAbilityActionData` NXD neuter. It does NOT need the exe-hardcoded base formulas: it
  classifies damaging offensive abilities from `AbilityData.xml` `AIBehaviorFlags`
  (`HP` + `TargetEnemies`, not `TargetAllies`) and forces `X=1, Y=1` on those rows (keeping base
  `Formula`/element/CT/MP at inherit). Since Test D proved `Y` is read for magnitude, `X=Y=1`
  collapses stat*Y / stat+X damage to ~one stat (non-lethal) regardless of which parameter the
  routine uses. 168 abilities neutered (Fire/Bolt/Ice lines, monster skills); heals (Cure =
  `TargetAllies`) correctly untouched.
- **High-id fallback half BUILT.** The 32 classified IDs outside the 368-row override table are
  Throw 382-393, Jump 394-405, and Aim/Charge 406-413. Throw and Jump are expected to be covered by
  the weapon `Power=1` neuter because the formula catalog maps them through WP; Jump's secondary
  table only exposes range/vertical. Aim/Charge has its own secondary `Power`, so the generator
  also emits `AbilityChargeAimData.xml` forcing Aim +2..+20 to `Power=1` while leaving CT/Ticks
  inherited. Residual risk: Throw/Jump still need a live spot-check, and formulas that ignore
  X/Y/WP/charge Power (e.g. `%`-damage, Gravity) are not shrunk by these levers.
- **Death after neuter:** Tests 2b/2c proved direct byte writes do **not** cause real KO. HP=0
  alone creates a zombie-like state, and HP=0 plus `+0x61|=0x20` still creates a zombie-like state.
  The accepted runtime architecture is engine-owned death: custom lethal formulas clamp to
  `MinHpFloor=1`, never write 0, and let the engine's own neutered chip deliver the real KO on a
  later hit. `CauseDeathOnZeroHp` + `DeathStateWrites[]` remain only as historical/refuted probes.

### 2. Runtime unit registry

Use the stable `battle_base_ptr` hook as a read-only hot-path sampler and unit-pointer discovery
source:

```text
rcx -> BattleUnit*
BattleUnit +0x30 HP
BattleUnit +0x32 MaxHP
BattleUnit +0x3E PA
BattleUnit +0x3F MA
BattleUnit +0x40 Speed
BattleUnit +0x2B Brave
BattleUnit +0x2D Faith
```

The managed polling thread maintains:

- unit pointer -> last full snapshot;
- unit pointer -> last HP/MP;
- unit pointer -> team/faction;
- unit pointer -> first-seen hex dump for unknown-field mapping.

Once a pointer has been seen by the hook, the poller reads that unit directly from process memory
each tick through `ReadableMemoryRange` + `ReadProcessMemory`. This is the important post-Live
Test 1 change: HP/MP baselines should now be only about one poll interval old rather than "last
time this unit got its own turn." Hook samples still update the recent-unit touch list used by the
temporary attacker resolver, so direct polling does not make every unit look like a fresh attacker
candidate.

Runtime knobs:

- `UnitPollIntervalMs` defaults to `25`. Lower it for tighter live proof timing; keep it positive.
- `MaxTrackedBattleUnits` defaults to `64`. New unit pointers beyond the cap are skipped with a
  `[UNIT-SKIP]` log instead of growing the registry without bound.

This registry is the foundation for target detection, DR lookup, stat reads, and HP/MP correction.

### 3. Event detector

The first event detector can be deliberately simple:

```text
if currentHP < previousHP for the same registered unit pointer:
    create DamageObserved(targetPtr, vanillaDelta, timestamp)
```

Then we harden it:

- separate damage from poison/regen/status ticks by timing and active-action context;
- support healing (`currentHP > previousHP`);
- support MP deltas;
- support multi-hit actions by grouping several deltas in a short window;
- ignore deltas caused by our own reconciliation writes.

### 4. Context resolver

The resolver is no longer a single missing piece. It is a set of context dimensions with different
evidence levels:

```text
target unit      solved: the HP/MP delta unit pointer
target stats     solved: unit struct +0x00..+0x43
attacker unit    live-proven: CT reset at +0x41 (`ct-reset`)
counters         offline-implemented fallback: invert the previous resolved HP-damage pair
action family    offline-implemented bridge: sentinel bands -> ActionSignalRules
true action id   still open: needs a stable action/pre-damage/preview context source
equipment ids    partially implemented: scan/exact slot probes + item catalog; live offset proof pending
same-hit death   still open: engine-owned death works, direct KO writes are refuted
```

Candidate context sources, in preferred order:

1. **CT reset attacker resolution.** Track `+0x41` history per registered unit pointer. At a damage
   event, attacker = non-target unit with a recent CT drop/reset. Formula context exposes
   `a.present`, `a.ct`, `a.sourceCt`, plus target equivalents. This is the current primary path.
2. **Counter inversion.** Reaction attacks do not necessarily reset CT. If unit B was just damaged
   by unit A and then A immediately loses HP, infer B as the counter attacker and expose
   `a.sourceCounter`.
3. **Current action state in normal memory.** Use pointer-stable dumps, `[HOOK-REGS]` snapshots,
   controlled battles, and future probes to find active unit pointer, selected ability id, target
   list, and queued action data outside the virtualized formula dispatcher.
4. **Stable UI/preview/action-selection sites.** Damage preview and action-menu code may hold
   attacker, target, and ability before the virtualized damage routine runs. These hooks would also
   solve preview mismatch later.
5. **Data sentinel channel.** If direct ability id capture stays hard, use the data layer to make
   groups of actions produce distinguishable placeholder deltas. Runtime `ActionSignalRules` decode
   vanilla deltas into variables such as `action.swing`, `action.thrust`, `action.missile`, or
   `action.magical`. `sentinel-coarse-v1` is the checked-in live calibration candidate.
6. **Controlled encounter inference.** For early tests, ENTD can make attacker/weapon/ability known.
   This proves the HP reconciler and DR math before full generic action context exists.
7. **Persistent roster/unit-table correlation.** Community prior art (`FFT_Egg_Control`) has a
   persistent unit table lead with 55 entries and a `0x258` stride. The project now has independent,
   disabled-by-default `MemoryTableProbes` so reviewed candidates can be tested without embedding
   third-party signatures.
8. **Recent-unit heuristic.** Kept as a fallback/debug source (`sourceRecent`), not the primary
   attacker model.

The first flexible runtime does not need perfect UI/AI. It needs true final outcomes. Preview/AI
can be patched after the result engine is proven.

### 4.1. Hook-register probe

`HookRegisterProbe` is an observe-only RE aid for the stable `battle_base_ptr` hook. The assembly
hook stores a short x64 register snapshot in the existing native buffer; the polling thread logs it
later as `[HOOK-REGS]`, with classifications such as `unit:touched`, `unit:id=...`, `readable`, or
`unreadable`.

Profile:

```text
work/battle-runtime-settings.hook-register-probe.json
```

Watch command:

```powershell
python tools\watch_live_mapping.py --runtime-events 0 --hook-regs 12 --skip-analyze
```

Use this to hunt for a current actor/action-context pointer at the non-virtualized hook. It does not
rewrite HP/MP and intentionally caps log count through `HookRegisterProbeMaxLogs`. See
`09-hook-register-context-probe.md`.

### 4.2. Persistent memory-table probes

`MemoryTableProbes` are a new offline-prepared/live-observed tool for context hunting. They are
not required for the current HP reconciler, and they are disabled unless explicitly configured in
`battle-runtime-settings.json`.

Purpose:

- test whether a stable persistent roster/unit table can be resolved from normal memory;
- log row fields such as unit index, job, roster slot, or later equipment fields;
- correlate those rows with observed `BattleUnit*` snapshots, ENTD slots, or known controlled
  party state;
- avoid baking a third-party/community signature into this project before we independently
  validate it.

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

A disabled copyable scaffold lives at:

```text
docs/modding/examples/battle-runtime-settings.memtable-probe.disabled.example.json
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

Runtime behavior:

- Each enabled probe registers a startup scan through Reloaded-II's `IStartupScanner`.
- The scan result resolves `table = instruction + InstructionLength + disp32 + TargetAddend`,
  with optional `DereferenceCount`.
- Before reading the displacement, dereferences, or table rows, the runtime checks the memory
  page with `VirtualQuery`.
- Rows log as `[MEMTABLE-ROW ...]` only when `LogRows=true` and their field presence score meets
  `MinPresenceScore`.
- `tools/analyze_battleprobe_log.py` now renders a `Memory Table Probes` report section with
  configured/enabled counts, found table bases, logged rows, and probe issues such as
  `[MEMTABLE-NOTFOUND]`, `[MEMTABLE-FAILED]`, and `[MEMTABLE-ROW-SKIP]`.
- `tools/watch_live_mapping.py` also counts `[MEMTABLE-FOUND]` and `[MEMTABLE-ROW]` evidence in
  its status output, and can explicitly wait for it with `--memtable-found` and
  `--memtable-rows`. For a pure table-probe pass, use `--runtime-events 0` so the watcher does
  not require a damage event before reporting readiness.
- `tools/find_memtable_candidates.py` is an offline PE scanner for independent candidate
  discovery. It scans `FFT_enhanced.exe` for RIP-relative `LEA`/`MOV` table references, scores
  candidates near a configurable stride (`0x258` by default), and writes CSV output to
  `work/memtable_rip_candidates.csv`. By default it only scans normal code sections
  (`.xcode`/`.text`) and skips huge/noisy low-value sections such as `.edata`; use
  `--include-all-executable` and, only intentionally, `--include-low-value-executable` for wider
  research passes.
- The scanner now emits both the short probe `pattern` and a wider `context_pattern` with the
  RIP displacement wildcarded. `context_matches=1` is the first safety filter for turning a
  candidate into a temporary `MemoryTableProbes` setting. If using `context_pattern`, use the
  paired `context_rip_relative_offset` and `context_instruction_length` columns, not the short
  `rip_relative_offset`/`instruction_length` pair; the contextual match starts before the
  instruction. On this install, the current top static pass produced several unique-context
  `.xcode -> .rodata` candidates near `0x258`, but no normal-code candidate pointing to
  mutable-looking sections. A mutable-section pass excluding low-value `.edata` found zero
  candidates. Treat these as addresses to investigate, not signatures to enable blindly.
- `tools/build_memtable_probe_settings.py` converts unique scanner rows into
  `work/memtable-probe-candidates.disabled.json`. It intentionally writes every probe with
  `Enabled=false`, carries the CSV evidence in `_candidate`, and uses the contextual AOB offsets
  so a future live pass can enable one reviewed candidate at a time without hand-copying the wrong
  RIP-relative parameters.
- This is evidence collection only. Formula variables are not yet populated from these rows; that
  comes after a live correlation is proven.

Example wait command for a future reviewed candidate:

```powershell
python tools\watch_live_mapping.py --runtime-events 0 --memtable-found 1 --memtable-rows 1
```

### 5. Formula engine

Once a `DamageObserved` event has enough context, compute our own result:

```text
attack = ResolveAttack(attacker, action, weapon)
basePressure = CustomFormula(attack, action, weapon, config)
response = ResolveDamageResponse(target, armor, statuses, element, zodiac)
finalDamage = Clamp(ApplyResponse(basePressure, response), 0, targetHP)
```

The engine can be arbitrary C# math:

- integer tables;
- dice-style curves;
- GURPS-like swing/thrust tables;
- DR by armor slot;
- percent/type response layers for the accepted v0.2 armor model;
- per-damage-type armor divisors;
- armor penetration;
- injury multipliers;
- custom status/status-resistance logic.

Important: keep the formula engine pure and deterministic. Feed it snapshots and static table
data, return a result. The runtime layer handles memory reads/writes.

Current live expression layer can read target stats, resolved attacker stats, optional action signal
variables, equipment slot probes, item-catalog metadata, response variables, and raw bytes/words.
Live formulas should still guard optional context, but the primary attacker path is now CT-based:
use `a.present`, `a.sourceCt`, and `a.ct` for normal actions, and `a.sourceCounter` for the
counter-inversion fallback. `a.sourceRecent` remains only a legacy/debug fallback. Action family
context can be supplied through `ActionSignalRules`, which map controlled vanilla deltas to
`action.*` variables.

Global formula example:

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

Supported expression features:

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
             attacker.* or a.*:
               present inferred sourceRecent
               charId level hp maxHp mp maxMp team isFoe isAlly pa ma speed move jump brave faith
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

For a generated, source-derived catalog of current expression functions and variable families,
use:

```powershell
python tools\report_runtime_formula_context.py
```

The checked output is `work/runtime_formula_context.md`, and `run-offline-checks.ps1` fails if it
is stale. This is the quickest reference before writing a new formula/DR/response profile.

Raw reads are little-endian. `Byte`/`Word`/`DWord` read unsigned values, while `SByte` and
`Short` sign-extend memory fields that are stored as signed values. Attacker raw reads are
implemented, but they intentionally fail the rewrite when attacker context is absent. `if(...)`,
`&&`, and `||` are lazy, so guards like `if(a.present, attackerByte(0x44), 0)`,
`a.present && attackerByte(0x44)`, and `!a.present || attackerByte(0x44)` can safely wrap raw
attacker reads until action context is mapped.

Bit helpers are intended for mapped status/element/equipment flag fields. Example once a status
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

`hasBit` accepts bit indexes `0..62`. `bitExtract`/`signedBitExtract` accept widths `1..62` as
long as the requested range stays within bit `62`. Mask and bitfield helpers reject negative
values so a bad formula fails closed instead of interpreting signed integers as flag sets.

Experimental live attacker candidate settings:

```json
{
  "LogAttackerCandidates": true,
  "InferAttackerFromRecentUnits": false,
  "RecentAttackerWindowMs": 1500,
  "PreferOpposingTeamAttacker": true,
  "MaxAttackerCandidatesToLog": 4
}
```

Keep `InferAttackerFromRecentUnits=false` while mapping. The harness will still emit lines like
`[CTX ...] resolved=none attackerCandidates=...`. Turn it on only for controlled battles where a
wrong attacker candidate is acceptable risk. When enabled, formulas can require the heuristic with
`a.sourceRecent`, for example `if(a.sourceRecent, a.pa * 2, vanillaDamage)`.

Current CT/counter settings:

```json
{
  "ResolveAttackerByCt": true,
  "CtDropWindowMs": 4000,
  "ResolveCounterFromRecentDamage": true,
  "CounterEventWindowMs": 1500
}
```

Formula examples:

```text
if(a.sourceCt, max(1, a.pa * 10 - t.faith), vanillaDamage)
if(a.sourceCounter, a.pa * 3, if(a.sourceCt, a.pa * 4, vanillaDamage))
```

Resolved runtime-context logging:

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

When a damage/healing event passes the rewrite gates, this emits `[RUNTIME ...]` lines that show
the resolved attacker, action signal, target slots, attacker slots, equipment DR, percent/type
response, and final formula result. This is meant for live mapping, especially when testing scan
probes. `FormulaTraceVariables` entries are diagnostic-only formula probes appended to the trace
as `vars=...`; if one fails, it logs `ERR(...)` and does not cancel the HP rewrite.

```text
[RUNTIME ...] event=damage | attacker=0x1000:recent-unit | action=sword from attacker weapon:source=vanilla-damage:signal=101:vars=swing=1,wp=14 | targetSlots=body(present,id=172:Leather Armor,off=0x70,width=Byte,matches=1) | attackerSlots=weapon(present,id=19:Broadsword,off=0x50,width=Byte,matches=1) | equipmentDr=0:NoEquipmentDR | response=raw950/permille950/rules1/clamped0:DamageResponse(leather swing) | vars=gross=192,penetrating=192,result.final=182 | final=182:FinalDamageFormula+DamageResponse(leather swing)
```

For a no-op mapping pass, keep `RewriteObservedDamage=true`, set `FinalDamageFormula` to
`vanillaDamage`, set `ApplyDamageResponseRules=false`, and enable
`LogResolvedRuntimeContext=true`. That still lets the runtime resolve slots/action/response and
log what it saw while preserving the vanilla HP outcome. The generated live-noop profile below
does this automatically.

Sentinel action classification example:

```json
{
  "RewriteObservedDamage": true,
  "ActionSignalRules": [
    {
      "Name": "Sword swing placeholder",
      "VanillaDamage": 7,
      "Signal": 41,
      "Variables": { "swing": 1, "cut": 1 }
    },
    {
      "Name": "Spear thrust placeholder",
      "VanillaDamage": 8,
      "Signal": 42,
      "Variables": { "thrust": 1, "impale": 1 }
    }
  ],
  "FinalDamageFormula": "if(action.swing, tableClamp(swing, a.pa), tableClamp(thrust, a.pa)) - equipmentDr"
}
```

Every variable declared in `ActionSignalRules.Variables` or
`ActionSignalRules.VariableFormulas` is installed with a default value of `0`. That makes
expressions like `action.swing`, `action.thrust`, and `action.power` safe even when no signal
matched. `ActionSignalRules` match in order and currently support `Faction`, `EventKind`, `Team`,
`CharId`, `MinLevel`, `MaxLevel`, `VanillaDamage`, `MinVanillaDamage`, `MaxVanillaDamage`, and
`ConditionFormula` for HP events. They also support `VanillaMpChange`,
`MinVanillaMpChange`, and `MaxVanillaMpChange` for MP events. Damage-filtered rules do not match
MP events, and MP-filtered rules do not match HP events. `EventKind` accepts `Any`,
`HP`/`HpChange`, `Damage`/`HpLoss`, `Healing`/`HpGain`, `MP`/`MpChange`, `Loss`/`MpLoss`, and
`Gain`/`MpGain`. Use it for condition-only rules so an HP-damage signal does not accidentally
classify a healing or MP event. These formulas run after configured `EquipmentSlots` and
`AttackerEquipmentSlots` are read, so they can use `slot.*`, `targetSlot.*`, `aslot.*`, and
`attackerSlot.*` item metadata, plus any `FormulaPreActionVariables` tags.

Condition-only controlled-context signal:

```json
{
  "ActionSignalRules": [
    {
      "Name": "controlled sword swing",
      "EventKind": "Damage",
      "ConditionFormula": "a.sourceRecent && aslot.weapon.category_sword",
      "Variables": { "swing": 1 },
      "VariableFormulas": { "wp": "aslot.weapon.weaponPower" }
    }
  ],
  "FinalDamageFormula": "vanillaDamage + action.swing * action.wp"
}
```

Pre-action classification example:

```json
{
  "FormulaPreActionVariables": [
    { "Name": "pre.weaponBlade", "Formula": "aslot.weapon.category_sword || aslot.weapon.category_katana" },
    { "Name": "pre.targetBodyArmor", "Formula": "slot.body.isArmor" }
  ],
  "ActionSignalRules": [
    {
      "Name": "Blade swing",
      "ConditionFormula": "a.present && pre.weaponBlade && pre.targetBodyArmor",
      "Variables": { "swing": 1, "cut": 1 },
      "VariableFormulas": { "weaponBonus": "aslot.weapon.weaponPower" }
    }
  ]
}
```

Formula-coded sentinel example:

```json
{
  "RewriteObservedDamage": true,
  "ActionSignalRules": [
    {
      "Name": "Heavy swing placeholder band",
      "ConditionFormula": "event.isDamage && vanillaDamageAbs >= 10 && vanillaDamageAbs <= 19",
      "Signal": 100,
      "Variables": { "swing": 1, "cut": 1 },
      "VariableFormulas": {
        "power": "vanillaDamageAbs - 10",
        "woundNum": "3",
        "woundDen": "2"
      }
    }
  ],
  "FinalDamageFormula": "mulDiv(max(0, tableClamp(swing, a.pa) + action.power - equipmentDr), action.woundNum, action.woundDen)"
}
```

The checked-in regression version of this idea is
`docs/modding/examples/battle-runtime-settings.sentinel-bands.example.json` plus
`docs/modding/examples/runtime-simulation-sentinel-bands.example.json`. It proves three
placeholder-sized HP-damage bands, trace output for the resolved action variables, and a
no-signal fallback to `vanillaDamage`.

Weapon-derived action signal example:

```json
{
  "RewriteObservedDamage": true,
  "AttackerEquipmentSlots": [
    { "Name": "Weapon", "Offset": 80, "Width": "Byte" }
  ],
  "EquipmentSlots": [
    { "Name": "Body", "Offset": 112, "Width": "Byte" }
  ],
  "ActionSignalRules": [
    {
      "Name": "Sword swing from attacker weapon",
      "ConditionFormula": "a.present && aslot.weapon.category_sword",
      "Variables": { "swing": 1, "cut": 1 },
      "VariableFormulas": {
        "weaponPower": "aslot.weapon.weaponPower",
        "targetArmorBonus": "slot.body.armorHpBonus"
      }
    }
  ],
  "FinalDamageFormula": "max(0, tableClamp(swing, a.pa) + action.weaponPower - equipmentDr)"
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

Table names can be bare identifiers or quoted strings:

```text
tableClamp(swing, a.pa)
tableClamp("gurps.swing", a.pa)
```

GURPS-style shape with action signals, once live attacker is supplied by a resolver:

```json
{
  "RewriteObservedDamage": true,
  "FormulaTables": {
    "thrustDice": [0, 1, 1, 1, 1, 1, 1, 1],
    "thrustAdds": [0, -4, -3, -2, -1, 0, 1, 2],
    "swingDice":  [0, 1, 1, 1, 1, 1, 1, 2],
    "swingAdds":  [0, -3, -2, -1, 0, 1, 2, -1]
  },
  "ActionSignalRules": [
    { "VanillaDamage": 7, "Signal": 1, "Variables": { "swing": 1 } },
    { "VanillaDamage": 8, "Signal": 2, "Variables": { "thrust": 1 } }
  ],
  "FinalDamageFormula": "mulDiv(max(0, if(action.swing, diceAvg(tableClamp(swingDice, a.pa), 6, tableClamp(swingAdds, a.pa)), diceAvg(tableClamp(thrustDice, a.pa), 6, tableClamp(thrustAdds, a.pa))) - equipmentDr), if(action.swing, 3, if(action.thrust, 2, 1)), if(action.swing, 2, 1))"
}
```

Those numbers are example placeholders. The real GURPS swing/thrust progression should live in
`FormulaTables` after we choose the game's ST-to-stat mapping. Use `diceAvgRound` for stable
average-damage proofs, `diceAvg` when floor-average behavior is intentional, and `diceRoll` when
actual GURPS-style variance is desired. `diceRoll` and `rand` use `event.seed` plus a per-formula
call counter, so the same event evaluates to the same rolled result while separate events can
vary. Prefer `diceRollAt(index,dice,sides,adds)` or `randAt(index,min,max)` for important named
rolls such as base weapon roll, crit roll, or status rider roll; these indexed streams stay stable
if the formula is refactored or another random helper is inserted earlier. The example applies DR
before the wound multiplier: cutting uses
`mulDiv(penetrating, 3, 2)` for x1.5, while thrust/impale can use x2.

The same formula is easier to maintain with derived variables:

```json
{
  "RewriteObservedDamage": true,
  "FormulaVariables": { "grossDamage": 20 },
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

Derived variables are evaluated in order after target/attacker/action/slot variables and
`equipmentDr` are known. If any derived formula fails, the runtime skips the HP rewrite instead
of applying a partial result.

Use `FormulaMatrices` when a rule is naturally two-dimensional. For example, action-derived
`damageTypeIndex` and armor-derived `armorClassIndex` can index a damage-type/armor-class
response table directly:

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

Use `FormulaMaps` when the rule is sparse and keyed by ids discovered in the catalog or live
memory. JSON keys can be decimal or `0x` hex strings:

```json
{
  "FormulaMaps": {
    "armorDrByItem": {
      "172": 1,
      "174": 6
    },
    "weaponModeByItem": {
      "19": 1,
      "0x2A": 2
    }
  },
  "FormulaDerivedVariables": [
    { "Name": "armor.dr", "Formula": "mapOr(armorDrByItem, slot.body.itemId, 0)" },
    { "Name": "weapon.mode", "Formula": "mapOr(weaponModeByItem, aslot.weapon.itemId, 0)" }
  ]
}
```

`map(name,key)` is strict and skips the HP rewrite if the map or key is absent. `mapOr` is the
safe live-mapping form when unknown or unequipped items should use a fallback.

If `ApplyEquipmentDr=true`, the runtime subtracts `equipmentDr` after the formula result. If the
formula already subtracts `equipmentDr`, leave `ApplyEquipmentDr=false` to avoid double DR.

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

Example weapon-family switch, once attacker weapon offset is confirmed:

```json
{
  "RewriteObservedDamage": true,
  "FormulaTables": {
    "thrust": [0, 1, 2, 3, 4, 5, 6],
    "swing":  [0, 2, 4, 6, 8, 10, 12]
  },
  "AttackerEquipmentSlots": [
    { "Name": "Weapon", "Offset": 80, "Width": "Byte" }
  ],
  "EquipmentSlots": [
    { "Name": "Body", "Offset": 112, "Width": "Byte" }
  ],
  "FinalDamageFormula": "if(aslot.weapon.category_sword, tableClamp(swing, a.pa), tableClamp(thrust, a.pa)) + aslot.weapon.weaponPower - equipmentDr"
}
```

The offsets above are examples, not confirmed. `AttackerEquipmentSlots` only becomes live-useful
after action context gives the formula engine an attacker snapshot.

Offline verification:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release
```

This smoke test validates:

- attacker-aware formula evaluation with `a.pa`;
- attacker weapon metadata through `AttackerEquipmentSlots` / `aslot.weapon.*`;
- catalog-backed armor DR through `item.armorHpBonus`;
- configurable table lookup for a swing/thrust-style formula;
- deterministic dice helpers for a `2d-1` style formula;
- event-seeded `diceRoll` / `rand` helpers for reproducible variable damage;
- integer ratio helpers and GURPS-style post-DR wound multipliers;
- ordered derived formula variables for readable multi-step damage formulas;
- fallback formulas with no attacker context via `a.present`, including lazy `if(...)`, `&&`, and
  `||` guards around unavailable attacker raw reads;
- signed HP event formulas: positive `finalDamage` hurts, negative `finalDamage` heals, with
  healing gated by `RewriteObservedHealing`;
- signed MP event formulas: negative `finalMpChange` spends/drains MP, positive
  `finalMpChange` restores MP, separately gated by `RewriteObservedMpLoss` and
  `RewriteObservedMpGain`;
- action sentinel variables through `ActionSignalRules`, including HP-delta and MP-delta
  sentinels, safe zero defaults, and `DamageRules` selection by action variable;
- event-kind and formula-coded action signals through `ActionSignalRules.EventKind`,
  `ActionSignalRules.ConditionFormula`, and `ActionSignalRules.VariableFormulas`;
- action-typed equipment DR through `EquipmentDrRules.ActionSignal` and
  `EquipmentDrRules.RequiredActionVariable`;
- formula-gated `DamageRules`, `MpRules`, and `EquipmentDrRules.ConditionFormula`, including
  attacker weapon gates, action/armor/resource gates, and safe skip on invalid condition formulas;
- formula trace variables that expose selected intermediates in `[RUNTIME]` lines without
  affecting HP rewrite success;
- percent/type response rules through `DamageResponseRules`, including manual formula use,
  automatic application, clamp/chip floor behavior, and a v0.2-style plate/mail smoke case;
- generated v0.2 runtime settings from `tools/build_runtime_settings_from_sim.py`, including
  weapon-family action rules, routine formulas, penetration-aware response multipliers, and a
  concrete sword/leather evaluation through the normal formula engine;
- a matrix-response generated profile that moves the armor-class x damage-type response table
  into `FormulaMatrices` and applies it through one `DamageResponseRule`;
- generated formula trace variables for base pressure, response permille, final damage, desired
  HP, and matrix indices/per-mille values when using the matrix-response profile;
- catalog-scanned equipment slots, including unique match, ambiguous-blocked, and
  explicitly-allowed ambiguous match behavior;
- recent-unit attacker source variables (`a.inferred`, `a.sourceRecent`) and resolver selection;
- runtime settings JSON load success/failure handling, which is the safety base for hot reload;
- safe rewrite skipping when `attackerByte(...)` is used without attacker context or a table lookup
  is invalid.

Runtime settings validation:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -c Release
```

With no explicit file arguments, this validates the repo's generated/example settings plus the
disabled MEMTABLE candidate settings. It uses the same `RuntimeSettings` JSON loader and formula
parser as the code mod, checks formula expressions, slot/probe ranges, response denominators, and
then runs synthetic damage/healing evaluations through `BattleFormulaEngine`. Use this before
copying any `battle-runtime-settings.json` into the Reloaded-II mod folder.

The validator also runs a missing-context damage evaluation with no attacker and no explicit
action. If a settings file would skip the HP rewrite because it uses optional context unsafely,
the validator emits a `WARN` under `MissingContextDamageEvaluation`. That warning does not block
validation, because some profiles may intentionally require attacker/action context, but live-safe
policies should guard with `a.present` / `action.present` or fall back to `vanillaDamage`.

Offline regression runner:

```powershell
.\codemod\run-offline-checks.ps1
```

This is the default no-game gate before changing combat formulas. It runs Python syntax/tooling
tests, strict JSON parsing for the checked-in examples/work files, the C# build, smoke tests,
runtime settings validation, scenario simulation fixtures with `expect` assertions, the v0.2
matrix against both exact-slot and scan-slot generated policy settings, the matrix-response
fixture, the GURPS-DR fixture, the static DR fixture, the MP fixture, the sentinel-band action
fixture, the neuter spot-check fixture, the death-gate HP/KO profile fixture, the dry-run HP/MP
fixture, and the read-only/live-safe helper dry-runs for live mapping, dry-run evaluation, and the
death gate, plus the runtime formula-context/profile-audit report staleness checks and
`git diff --check`. It deliberately does not deploy to Reloaded-II, edit AppConfig, touch saves,
archive logs, or launch FFT.

The profile audit is generated with:

```powershell
python tools\report_runtime_profiles.py
```

The checked output is `work/runtime_profile_audit.md`. It records each important settings profile's
role, whether it can mutate HP/MP/death state if deployed, and the invariants that must stay true
before live use.

Runtime settings simulation:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.v0.2.generated.json docs\modding\examples\runtime-simulation-scenarios.example.json
```

This runs the actual C# `BattleFormulaEngine` against editable JSON scenarios. It is not a live
proof, but it is stronger than the Python policy sim because it exercises the same runtime
settings parser, action rules, equipment slots, response rules, formula expressions, and trace
format that the code mod uses in battle. The current sample `sword-vs-leather-default` scenario
resolves Broadsword + Leather Armor through the generated v0.2 settings as:

```text
rewrite=True vanillaDamage=20 hp=250->230->68 finalDamage=182 rule=FinalDamageFormula+DamageResponse(leather swing)
expect=pass
```

Scenario files may include an `expect` block with `shouldRewrite`, `finalDamage`, `desiredHp`,
`finalMpChange`, `desiredMp`, `ruleName`, and `traceContains` assertions. When any assertion
fails, the simulator returns exit code `3` unless `--ignore-expectations` is passed. This makes
`docs/modding/examples/runtime-simulation-scenarios.example.json` a regression fixture for the
current v0.2 generated settings, not just sample input. The short fixture covers the main
sword/leather policy path, no-attacker damage fallback to vanilla, and signed healing skip.

The broader v0.2 matrix fixture covers swing/thrust/crush/missile/fists against
plate/mail/leather/cloth:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.v0.2.generated.json docs\modding\examples\runtime-simulation-matrix.v0.2.example.json
```

To intentionally refresh that matrix after changing the policy formulas:

```powershell
python tools\build_runtime_simulation_matrix.py
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.v0.2.generated.json docs\modding\examples\runtime-simulation-matrix.v0.2.example.json --write-scenarios-with-expectations docs\modding\examples\runtime-simulation-matrix.v0.2.example.json
```

The matrix-response fixture uses the same 20 scenario inputs, but its expectations assert the
single `DamageResponse(matrix response)` route backed by `FormulaMatrices`:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.v0.2.matrix.generated.json docs\modding\examples\runtime-simulation-matrix-response.v0.2.example.json
```

The static DR fixture is the smallest possible DR canary. It uses `FlatDamageReduction=10`,
requires no attacker/equipment mapping, asserts normal damage reduction, clamps chip damage to 0,
and leaves healing untouched:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- docs\modding\examples\battle-runtime-settings.static-dr.example.json docs\modding\examples\runtime-simulation-static-dr.example.json
```

The MP fixture uses `eventKind: "mp"` and asserts signed `finalMpChange` / `desiredMp`:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- docs\modding\examples\battle-runtime-settings.mp.example.json docs\modding\examples\runtime-simulation-mp.example.json
```

The dry-run fixture exercises the live-safe evaluation profile across HP damage, HP healing, MP
loss, and MP gain. `DryRunRewrites` is a live-application guard, so the simulator proves the
rewrite decisions and desired HP/MP targets while the live profile still prevents memory writes:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- docs\modding\examples\battle-runtime-settings.dry-run.example.json docs\modding\examples\runtime-simulation-dry-run.example.json
```

For a matching live dry-run pass, trigger one representative HP damage, HP healing, MP loss, and
MP gain event, then let the watcher wait for all four dry-run rewrite decisions:

```powershell
codemod\prepare-dry-run-evaluation.ps1 -DryRun
codemod\prepare-dry-run-evaluation.ps1
python tools\watch_live_mapping.py --runtime-events 0 --placeholder-rewrites 1 --hp-healing-rewrites 1 --mp-loss-rewrites 1 --mp-gain-rewrites 1 --max-rewrite-failures 0
```

The helper's `-DryRun` mode still validates and simulates the selected profile; it only skips the
code-mod deploy and log move.

The analyzer report should then include both `HP Write Proof Check` and `MP Rewrite Check`. The MP
section summarizes `[MPLOSS]`, `[MPGAIN]`, `[MP-REWRITE]`/`[MP-REWRITE-DRY-RUN]`, MP rewrite
failures, signed `finalMpChange`, desired MP ranges, and `sampleAgeMs` baselines.

The sentinel-band fixture proves the current fallback path for action classification when true
ability/action IDs are still unavailable at the hook. It maps placeholder-sized vanilla damage
bands to `action.sentinelLow`, `action.sentinelMid`, and `action.sentinelHigh`, lets those
variables drive `FinalDamageFormula`, and asserts that an unknown damage band falls back to
`vanillaDamage`:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- docs\modding\examples\battle-runtime-settings.sentinel-bands.example.json docs\modding\examples\runtime-simulation-sentinel-bands.example.json
```

For a matching live sentinel pass, the watcher can now wait for nonzero action signals, specific
sentinel signals, or the normalized action variables that appear in the log:

```powershell
python tools\watch_live_mapping.py --runtime-events 0 --action-signals 3 --require-action-signal 301 --require-action-signal 302 --require-action-signal 303 --require-action-var sentinellow --require-action-var sentinelmid --require-action-var sentinelhigh
```

The neuter spot-check fixture is the safe placeholder sanity profile. It uses
`DryRunRewrites=true` and `FinalDamageFormula="vanillaDamage"` so live damage events produce
`[REWRITE-DRY-RUN]` lines with the observed vanilla delta, but no HP write. Use this before a
custom-formula, sentinel, or engine-owned-death run if the installed data mod needs a quick sanity
check across attack/spell/Throw/Jump/Aim families:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.neuter-spotcheck.json docs\modding\examples\runtime-simulation-neuter-spotcheck.example.json
```

For the matching live pass, the watcher can wait for placeholder-sized HP rewrites directly:

```powershell
python tools\watch_live_mapping.py --runtime-events 0 --placeholder-rewrites 3 --max-placeholder-damage 30 --max-large-vanilla-rewrites 0 --max-rewrite-failures 0
```

The death-gate fixtures are preserved as historical/refuted diagnostics. They assert the
offline-computable setup for the old HP=0 and HP=0+KO-bit probes: placeholder damage against foes
is rewritten to HP 0, allies are preserved, and healing stays ignored. Live tests already proved
those writes create zombie-like states, so these profiles are **not** the mainline success path.

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.death-test.json docs\modding\examples\runtime-simulation-death-gate.example.json
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.death-test-killflag.json docs\modding\examples\runtime-simulation-death-gate.example.json
```

If re-auditing the refuted byte-write path, wait for the stronger Death Gate evidence instead of
just any rewrite:

```powershell
python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --death-events 1 --max-rewrite-failures 0
python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --max-death-events 0 --settle-seconds 2 --max-rewrite-failures 0
python tools\watch_live_mapping.py --runtime-events 0 --lethal-hp-rewrites 1 --death-events 1 --death-writes 1 --max-rewrite-failures 0 --max-death-write-failures 0
```

`tools/analyze_battleprobe_log.py` emits `Death Gate Outcome`, a cross-check that classifies logs
as HP-only evidence, zombie-candidate evidence, killflag-branch evidence, or failed/ambiguous
runs. Current balance/live profiles should instead use `MinHpFloor=1` and avoid byte-level KO
ownership.

As with the dry-run evaluation helper, `prepare-death-gate.ps1 -DryRun` still runs the neuter
artifact tests plus settings validation/simulation; it skips the NXD rebuild, data/code deploy, and
game-log archive.

JSON output can be captured for regression comparison:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- work\battle-runtime-settings.v0.2.generated.json docs\modding\examples\runtime-simulation-scenarios.example.json --json
```

### 6. Percent/type response implementation

The accepted v0.2 balance policy uses coarse percent/type responses, not default point DR.
`DamageResponseRules` implement that model in the runtime.

Each matching response rule returns a nonnegative permille multiplier:

```text
1000 = neutral x1.00
 650 = resist x0.65
1150 = vulnerable x1.15
```

The runtime multiplies every matching rule into `combinedResponse.permille`, clamps it into
`boundedResponse.permille`, then exposes both values to formulas. If
`ApplyDamageResponseRules=true`, the bounded response is applied after the formula result and
optional equipment DR. `DamageResponseChipFloor` can preserve a visible minimum for positive
damage that would floor to zero.

Response rules can be global or slot/item-backed. A global rule has no `Slot` or item filters and
fires once when its action/condition matches. A slot-backed rule can match `Slot`, `ItemId`,
`ItemCategory`, `SecondaryKind`, `NameContains`, armor HP bonus, weapon power, action signal, or a
formula condition. Rule formulas get the same target/action/slot/item variables as the main
formula, plus `item.*` for the currently matched item.

Use `FormulaPreResponseVariables` when several rules need the same classification. They run after
target and attacker slots plus action signals are available, so a settings file can derive
`armor.plate`, `armor.mail`, `armor.light`, or similar tags once and reuse those tags in
`EquipmentDrRules`, `DamageResponseRules`, and the final damage formula.

V0.2-style armor response shape:

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
  "EquipmentSlots": [
    { "Name": "Body", "Offset": 112, "Width": "Byte" }
  ],
  "FormulaPreResponseVariables": [
    { "Name": "armor.plate", "Formula": "slot.body.category_armor && slot.body.armorHpBonus >= 60" },
    { "Name": "armor.mail", "Formula": "slot.body.category_armor && slot.body.armorHpBonus >= 40 && slot.body.armorHpBonus < 60" },
    { "Name": "armor.light", "Formula": "slot.body.category_clothing || (slot.body.category_armor && slot.body.armorHpBonus < 40)" },
    { "Name": "armor.cloth", "Formula": "slot.body.category_robe" }
  ],
  "DamageResponseRules": [
    { "Name": "plate swing", "ConditionFormula": "armor.plate && action.swing", "MultiplierPermille": 650 },
    { "Name": "plate thrust", "ConditionFormula": "armor.plate && action.thrust", "MultiplierPermille": 650 },
    { "Name": "plate crush", "ConditionFormula": "armor.plate && action.crush", "MultiplierPermille": 1150 },
    { "Name": "plate missile", "ConditionFormula": "armor.plate && action.missile", "MultiplierPermille": 800 },
    { "Name": "mail swing", "ConditionFormula": "armor.mail && action.swing", "MultiplierPermille": 750 },
    { "Name": "mail thrust", "ConditionFormula": "armor.mail && action.thrust", "MultiplierPermille": 1100 },
    { "Name": "mail crush", "ConditionFormula": "armor.mail && action.crush", "MultiplierPermille": 950 },
    { "Name": "mail missile", "ConditionFormula": "armor.mail && action.missile", "MultiplierPermille": 1100 },
    { "Name": "light swing", "ConditionFormula": "armor.light && action.swing", "MultiplierPermille": 950 },
    { "Name": "light thrust", "ConditionFormula": "armor.light && action.thrust", "MultiplierPermille": 950 },
    { "Name": "light missile", "ConditionFormula": "armor.light && action.missile", "MultiplierPermille": 950 }
  ]
}
```

The same shape is also saved as a starting point at:

```text
docs/modding/examples/battle-runtime-settings.v0.2-response.example.json
```

The class formulas above are provisional and use current catalog categories plus armor HP bonus as
a convenient offline approximation. They are safer than scattering item ids across every response
rule, but final class boundaries still need design review.

For smoke tests and exact-item experiments, these ids are real `work/item_catalog.csv` ids from
this install:

```text
175 Chainmail
177 Plate Mail
```

They are safe for offline smoke tests, but the live runtime still needs confirmed equipment
offsets before this can be trusted in battle.

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

Use `MultiplierPermille` for exact policy constants like `650` or `1150`. Use
`MultiplierFormula` when the response needs to depend on item metadata, action variables, status
probes, or `FormulaPreResponseVariables`.

### 7. Generated policy-to-runtime settings

`tools/build_runtime_settings_from_sim.py` converts a simulation bundle into a runtime settings
JSON shape. This is the bridge from balance policy to code-mod implementation.

Default command:

```powershell
python tools\build_runtime_settings_from_sim.py
```

Default outputs:

```text
work\battle-runtime-settings.v0.2.generated.json
docs\modding\examples\battle-runtime-settings.v0.2.generated.example.json
```

Scan-mode proof outputs:

```powershell
python tools\build_runtime_settings_from_sim.py --slot-mode scan --output work\battle-runtime-settings.v0.2.scan.generated.json --docs-example docs\modding\examples\battle-runtime-settings.v0.2.scan.generated.example.json
```

```text
work\battle-runtime-settings.v0.2.scan.generated.json
docs\modding\examples\battle-runtime-settings.v0.2.scan.generated.example.json
```

Live no-op scan profile:

```powershell
python tools\build_runtime_settings_from_sim.py --slot-mode scan --profile live-noop --output work\battle-runtime-settings.v0.2.scan.live-noop.json --docs-example docs\modding\examples\battle-runtime-settings.v0.2.scan.live-noop.example.json
```

```text
work\battle-runtime-settings.v0.2.scan.live-noop.json
docs\modding\examples\battle-runtime-settings.v0.2.scan.live-noop.example.json
```

This profile is for live mapping, not balance testing. It enables recent-attacker inference,
catalog slot scanning, and `[RUNTIME]` traces, but preserves HP by using
`FinalDamageFormula="vanillaDamage"` and `ApplyDamageResponseRules=false`. Damage response rules
are still resolved and logged; they are just not applied to the HP rewrite.

Matrix-response policy output:

```powershell
python tools\build_runtime_settings_from_sim.py --response-mode matrix --output work\battle-runtime-settings.v0.2.matrix.generated.json --docs-example docs\modding\examples\battle-runtime-settings.v0.2.matrix.generated.example.json
```

```text
work\battle-runtime-settings.v0.2.matrix.generated.json
docs\modding\examples\battle-runtime-settings.v0.2.matrix.generated.example.json
```

This variant keeps the same v0.2 damage numbers as the explicit-rule profile, but represents
armor response as a 4x4 `armorResponsePermille` matrix. It derives `armor.index` and
`damage.index` in `FormulaPreResponseVariables`, then one `DamageResponseRule` reads
`response.matrixPermille`. Use this shape when tuning a whole response table is more important
than naming every individual plate/swing/mail/thrust rule.

All generated policy profiles also include `FormulaTraceVariables`. The plain rule profile logs
base pressure, bounded response permille, final damage, desired HP, and rewrite decision. The
matrix-response profile additionally logs armor index, damage index, base matrix response, and
penetration-adjusted matrix response. These values appear in `[RUNTIME] ... vars=...` traces and
in simulator traces, without affecting HP rewrite success.

Build and install the live no-op profile into Reloaded-II:

```powershell
.\codemod\build-deploy.ps1 -RuntimeSettings work\battle-runtime-settings.v0.2.scan.live-noop.json
```

The script validates the selected runtime settings file before copying it. If
`battle-runtime-settings.json` already exists in the installed mod folder, the script writes a
timestamped `.bak-*` copy before replacing it.

Preferred clean-session helper:

```powershell
.\codemod\prepare-live-mapping.ps1
```

This builds/deploys the code mod, installs `work\battle-runtime-settings.v0.2.scan.live-noop.json`,
validates that settings file first, and moves any existing game-side `battleprobe_log.txt` to a
timestamped `.bak-*` file. Use it before a live mapping run so an old `iter 8` log cannot be
mistaken for current evidence. Use `-DryRun` to validate and print the deploy/log plan without
copying, editing AppConfig, or moving logs.
If `FFT_enhanced` is already running, the helper warns that the current process will not load the
fresh DLL/settings until restarted and skips game-log archiving.

Before launching the game, the read-only readiness helper can compare the installed code-mod DLL
against the local build output, identify which known live-mapping runtime profile is installed by
hash, validate any available live-mapping profiles, and print the exact watcher/analyzer commands:

```powershell
.\codemod\check-live-readiness.ps1
```

Then restart the game through Reloaded-II and create one controlled damage event. The runtime log
is written next to the game executable as `battleprobe_log.txt` (`AppContext.BaseDirectory`), not
inside the Reloaded-II mod folder. On this machine the known path is:

```text
D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt
```

Analyze it with:

```powershell
python tools\analyze_battleprobe_log.py
```

Or let the watcher wait until the fresh log contains runtime evidence:

```powershell
python tools\watch_live_mapping.py --runtime-events 1
```

For slot/response-aware mapping, the watcher can require present target/attacker slots, a
response rule, and a formula trace variable:

```powershell
python tools\watch_live_mapping.py --runtime-events 3 --target-slots-present 3 --attacker-slots-present 3 --response-events 1 --require-trace-var trace.finaldamage
```

For a longer controlled mapping pass, it can analyze and promote offsets automatically once enough
events exist:

```powershell
python tools\watch_live_mapping.py --runtime-events 3 --promote --also-policy --min-events 3
```

If the report warns that the log is an old validation harness or only has old-format
`[UNIT id=...]` lines, the current DLL was not loaded; restart through Reloaded-II after building.
For a successful mapping pass, expect pointer-keyed `[UNIT ptr=...]` lines plus `[RUNTIME]` lines
showing `targetSlots`, `attackerSlots`, `action`, and `response`.

The analyzer now turns `[RUNTIME]` lines into a `Runtime Context Summary`:

- `Actions` groups the action signal name, signal id, source, and raw nonzero variables;
- `Action Variables` parses action `vars=...` into per-variable counts, numeric ranges, and top
  values; this is the quick check for `swing`, `thrust`, `sentinellow`, `wp`, and similar action
  classification output;
- `Formula Trace Variables` parses `[RUNTIME] ... vars=...` and summarizes each traced formula
  variable's count, numeric range, and most common values;
- `Slots` groups target/attacker slot states, item ids, offsets, widths, and scan match counts;
- `Slot Recommendations` calls out when a slot looks stable enough to convert from scan mode to
  exact `Offset`/`Width`, or when the scan is missing/ambiguous;
- `Responses` and `Final Damage` show which percent/type response was resolved and whether the
  no-op profile preserved `FinalDamageFormula`.
- `DR/Response Proof Check` cross-checks target/attacker slot presence, ambiguous scans, positive
  `equipmentDr`, response-rule count, final-rule linkage, and final trace variables
  (`result.final` / normalized `trace.finaldamage`) into a single proof-oriented verdict.

Once the report shows stable slots across multiple controlled events, promote the scan profile to
an exact-offset settings file:

```powershell
python tools\promote_runtime_offsets.py --min-events 3 --base-settings work\battle-runtime-settings.v0.2.scan.live-noop.json --output work\battle-runtime-settings.v0.2.live-noop.exact-from-log.json --also-policy --policy-base-settings work\battle-runtime-settings.v0.2.scan.generated.json --policy-output work\battle-runtime-settings.v0.2.policy.exact-from-log.json
```

The promotion tool only accepts slots where every observation is `present`, `matches=1`, and the
same `Offset`/`Width` appears at least `--min-events` times. It writes `_promotedOffsets` metadata
into the JSON for review, while the runtime ignores that unknown field. With `--also-policy`, the
same promotions are applied to the scan policy settings too. Install and validate the exact no-op
output first; after it still logs the right item ids, install the exact policy output to test the
actual formula/response layer with the confirmed equipment offsets.

Tooling smoke test:

```powershell
python tools\test_runtime_tooling.py
```

This covers `[RUNTIME]` parsing, `[MEMTABLE]` parsing/rendering, watcher memory-table counts,
summary rendering, stable offset promotion, partial rejection of ambiguous slots, old-log
warnings, and reading runtime lines from a log file.

The generated file includes:

- one `ActionSignalRules` entry per weapon family, currently classified from
  `aslot.weapon.category_*`;
- family routine flags such as `action.routine_pa_wp`, `action.routine_wp_wp`, and
  `action.routine_pampa_wp`;
- `action.wp`, `action.vanillaWp`, and `action.penetrationPermille`;
- a generated `basePressure` formula matching the sim routine names;
- `DamageResponseRules` generated from the sim bundle's armor response matrix;
- penetration-aware `MultiplierFormula` values that move resisted responses toward the configured
  penetration ceiling;
- `LogResolvedRuntimeContext=true`, so live proof runs emit `[RUNTIME]` lines showing the slot,
  action, response, and final-damage context resolved for each rewritten event.

Important limitation: this generated settings file is not a final live configuration. Its
attacker weapon/body offsets or scan ranges are placeholders, and weapon-family classification is
only an early bridge until true action/ability context is mapped. It is still useful because it
proves that the design policy can be expressed in the actual runtime settings language and
evaluated by the same C# formula engine used in battle.

### 8. Catalog-scanned equipment slots

Fixed offsets remain preferred once confirmed. Before that, slot probes can search the raw
snapshot for catalog-backed item ids:

```json
{
  "EquipmentSlots": [
    {
      "Name": "Body",
      "SearchStart": 68,
      "SearchEnd": 383,
      "SearchWidth": "Byte",
      "SecondaryKind": "armor",
      "TypeFlag": "Armor",
      "AllowAmbiguousSearchMatch": false
    }
  ],
  "AttackerEquipmentSlots": [
    {
      "Name": "Weapon",
      "SearchStart": 68,
      "SearchEnd": 383,
      "SearchWidth": "Byte",
      "SecondaryKind": "weapon",
      "TypeFlag": "Weapon",
      "AllowAmbiguousSearchMatch": false
    }
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
several matches are found and ambiguity is not allowed, the slot is not present; formulas can
still read `slot.body.scanMatches`, `slot.body.ambiguous`, and `slot.body.offset` to help mapping.

This is a proof aid, not a substitute for real offsets. Use it in controlled battles to discover
or validate candidate equipment fields, then replace scan ranges with fixed offsets when stable.

### 9. DR implementation

DR is feasible if either of these is solved:

```text
A. Runtime unit -> equipped item ids are mapped.
B. Runtime unit -> ENTD/roster slot mapping is mapped, and slot -> equipment is known.
```

The preferred model:

```text
target equipment ids -> item table -> item category/equip bonus -> Generic Chronicle DR table
```

The DR table should be our own data, not forced into vanilla HP bonus fields. Example:

```text
itemId 161  body armor  DR 4
itemId 172  heavy armor DR 7
itemCategory Shield     block DR +2 against frontal physical
```

This lets armor reduce incoming damage without abusing MaxHP. MaxHP can remain a separate design
stat.

Current configurable equipment DR path:

```json
{
  "RewriteObservedDamage": true,
  "ItemCatalogPath": "item_catalog.csv",
  "ApplyEquipmentDr": true,
  "EquipmentSlots": [
    { "Name": "Body", "Offset": 112, "Width": "Byte" }
  ],
  "EquipmentDrRules": [
    { "Name": "Body item 161 DR 4", "Slot": "Body", "ItemId": 161, "DamageReduction": 4 }
  ]
}
```

`Offset` is decimal in JSON; `112` is `0x70`. This example is only a shape, not a confirmed
offset. Use `[CANDIDATES]`, `[DIFF]`, and `tools/analyze_battleprobe_log.py` before trusting any
slot offset.

Catalog-backed DR rules can avoid listing every armor id:

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
    {
      "Name": "Body armor DR from HP bonus",
      "Slot": "Body",
      "ItemCategory": "Armor",
      "DamageReductionFormula": "max(1, item.armorHpBonus / 20)"
    },
    {
      "Name": "Any shield DR",
      "Slot": "Shield",
      "SecondaryKind": "shield",
      "DamageReduction": 2
    }
  ]
}
```

Action-typed DR is also implemented. This lets the same equipped armor reduce different damage
families differently once `ActionSignalRules` or a future true action resolver supplies
`action.*`:

```json
{
  "RewriteObservedDamage": true,
  "FinalDamageFormula": "max(0, grossDamage - equipmentDr)",
  "FormulaVariables": { "grossDamage": 20 },
  "ActionSignalRules": [
    { "VanillaDamage": 7, "Signal": 10, "Variables": { "cut": 1 } },
    { "VanillaDamage": 8, "Signal": 11, "Variables": { "impale": 1 } }
  ],
  "EquipmentSlots": [
    { "Name": "Body", "Offset": 112, "Width": "Byte" }
  ],
  "EquipmentDrRules": [
    {
      "Name": "Body armor cut DR",
      "Slot": "Body",
      "ItemCategory": "Armor",
      "RequiredActionVariable": "cut",
      "DamageReduction": 5
    },
    {
      "Name": "Body armor impale DR",
      "Slot": "Body",
      "ItemCategory": "Armor",
      "ActionSignal": 11,
      "DamageReduction": 2
    }
  ]
}
```

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

`RewriteConditionFormula` is the coarse event-level gate. Use it when a settings file should only
own specific classified events instead of falling back to a vanilla-preserving formula:

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

The checked-in subtractive DR proof profile is:

```text
docs/modding/examples/battle-runtime-settings.gurps-dr.example.json
docs/modding/examples/runtime-simulation-gurps-dr.example.json
```

It uses Broadsword, Javelin, Nothing Equipped, Leather Armor, and Plate Mail from
`work/item_catalog.csv`. It also uses `RewriteConditionFormula` to only own events whose action
was classified. The fixture proves the runtime can compute:

```text
base dice damage + weapon bonus -> gross damage
gross damage - equipmentDr      -> penetrating damage
penetrating damage * wound mod  -> final damage
```

Current expected examples:

```text
sword/cut vs Leather Armor: gross 10 - DR 1 = 9, x1.5 -> 13
sword/cut vs Plate Mail:    gross 10 - DR 6 = 4, x1.5 -> 6
spear/impale vs Plate Mail: gross 11 - DR 6 = 5, x2   -> 10
fists vs Plate Mail:        gross 3  - DR 6 = 0, x1   -> 0
```

Run it directly with:

```powershell
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingssimulate\fftivc.generic.chronicle.codemod.settingssimulate.csproj -c Release -- docs\modding\examples\battle-runtime-settings.gurps-dr.example.json docs\modding\examples\runtime-simulation-gurps-dr.example.json
```

Static item reference:

```powershell
python tools\dump_item_catalog.py
```

Default output:

```text
work\item_catalog.csv
```

The battle log analyzer automatically reads this catalog when present and reports candidate item
hits. A hit such as `+0x72 byte -> 172 Leather Armor` is not proof by itself; it becomes strong
evidence when the same offset changes predictably across controlled units with known equipment.

`dotnet build` also copies the generated CSV to:

```text
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\item_catalog.csv
```

### 10. Reconciler

After vanilla applies placeholder damage or healing:

```text
vanillaHP = target.HP
wantedHP = previousHP - finalDamage
write target.HP = Clamp(wantedHP, 0, target.MaxHP)
```

`finalDamage` is signed in the runtime: positive values reduce HP, negative values restore HP.
Damage rewrites are gated by `RewriteObservedDamage` and clamp to nonnegative final damage;
healing rewrites are separately gated by `RewriteObservedHealing` and clamp to nonpositive final
damage. This preserves old damage formulas while allowing explicit healing formulas.

The same polling path now has an opt-in MP reconciler:

```text
vanillaMP = target.MP
wantedMP = previousMP + finalMpChange
write target.MP = Clamp(wantedMP, 0, target.MaxMP)
```

`finalMpChange` is signed: negative values spend/drain MP, positive values restore MP. MP loss
rewrites are gated by `RewriteObservedMpLoss`; MP gain rewrites are gated by
`RewriteObservedMpGain`. If `FinalMpChangeFormula` is empty, the proof settings
`ProofFinalMpLoss` and `ProofFinalMpGain` provide fixed signed changes.

The write is done from managed code outside the asm hook and is guarded with:

- the destination `HP`/`MP` word is checked as writable through `VirtualQuery` before applying;
- the actual write uses `WriteProcessMemory` against the current process, so write failures can be
  logged as `[REWRITE-FAILED]` / `[MP-REWRITE-FAILED]` instead of relying on a raw pointer write;
- tracking HP is advanced to the desired post-rewrite HP after a successful write;
- tracking MP is advanced to the desired post-rewrite MP after a successful write;
- `DryRunRewrites=true` evaluates formulas and logs `[REWRITE-DRY-RUN]` /
  `[MP-REWRITE-DRY-RUN]`, but does not call `WriteProcessMemory`, does not arm the echo guard,
  and keeps HP/MP tracking on the observed live value;
- `ValueRewriteEchoGuard` suppresses an exact delayed echo of our own HP/MP write within
  `SuppressOwnRewriteEchoWindowMs` (default `1000`);
- clamps for 0..MaxHP;
- clamps for 0..MaxMP;
- per-event id/seed for deterministic formula evaluation.

Remaining reconciler work: full KO/status correctness. For the first live proof, prove that HP can
be reliably rewritten after a normal hit, then prove MP can be rewritten after a controlled
cost/drain/restore event. Then add KO/status once action context is stable.

Current opt-in settings path:

```text
C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json
```

The code mod watches this file and the configured `ItemCatalogPath` during the polling loop.
Changes are picked up about once per second, so formula constants, tables, DR rules, action
signals, and catalog data can be iterated without rebuilding the DLL. If a settings edit is not
valid JSON, the runtime logs `[SETTINGS-RELOAD-FAILED]` and keeps using the last valid settings.
`UnitPollIntervalMs` and `MaxTrackedBattleUnits` are also runtime settings, so polling sensitivity
and registry cap can be adjusted for a live test without rebuilding.

Fixed final-damage proof:

```json
{
  "RewriteObservedDamage": true,
  "ProofFinalDamage": 1,
  "FlatDamageReduction": 0,
  "AffectAllies": true,
  "AffectFoes": true
}
```

Fixed healing proof:

```json
{
  "RewriteObservedHealing": true,
  "ProofFinalHealing": 4,
  "AffectAllies": true,
  "AffectFoes": true
}
```

Formula-driven healing proof:

```json
{
  "RewriteObservedHealing": true,
  "FinalDamageFormula": "if(event.isHealing, -min(t.maxHp - previousHp, vanillaHealing + t.ma), vanillaDamage)"
}
```

Formula-driven MP proof:

```json
{
  "RewriteObservedMpLoss": true,
  "RewriteObservedMpGain": true,
  "FinalMpChangeFormula": "if(event.isMpLoss, -min(previousMp, vanillaMpLoss + t.ma), min(t.maxMp - previousMp, vanillaMpGain + t.ma))",
  "MpRewriteConditionFormula": "event.isMpChange"
}
```

Dry-run mapping/evaluation proof:

```json
{
  "DryRunRewrites": true,
  "RewriteObservedDamage": true,
  "RewriteObservedHealing": true,
  "RewriteObservedMpLoss": true,
  "RewriteObservedMpGain": true,
  "LogResolvedRuntimeContext": true,
  "FinalDamageFormula": "if(event.isHealing, -min(t.maxHp - previousHp, vanillaHealing + t.ma), max(0, vanillaDamage - min(vanillaDamage, t.ma)))",
  "FinalMpChangeFormula": "if(event.isMpLoss, -min(previousMp, vanillaMpLoss + t.ma), min(t.maxMp - previousMp, vanillaMpGain + t.ma))"
}
```

The checked-in version is
`docs/modding/examples/battle-runtime-settings.dry-run.example.json`. Use it when the next live
question is "does the runtime classify the event and compute the formula I expect?" rather than
"can we write the corrected HP/MP value yet?". A dry-run pass still does not prove that the final
memory write sticks; it only proves detection, context, formula evaluation, and the planned
rewrite decision.

MP sentinel action proof:

```json
{
  "RewriteObservedMpLoss": true,
  "ActionSignalRules": [
    {
      "Name": "MP cost sentinel",
      "EventKind": "MpLoss",
      "VanillaMpChange": -8,
      "Signal": 201,
      "Variables": { "spell": 1 },
      "VariableFormulas": { "mpCost": "vanillaMpLoss" }
    }
  ],
  "FinalMpChangeFormula": "-min(previousMp, vanillaMpLoss + action.mpcost)",
  "MpRewriteConditionFormula": "event.isMpLoss && action.spell"
}
```

MP rule-engine proof:

```json
{
  "RewriteObservedMpLoss": true,
  "RewriteObservedMpGain": true,
  "ActionSignalRules": [
    {
      "Name": "MP cost sentinel",
      "EventKind": "MpLoss",
      "VanillaMpChange": -8,
      "Variables": { "spell": 1 }
    }
  ],
  "MpRules": [
    {
      "Name": "Spell MP loss rule",
      "EventKind": "Loss",
      "RequiredActionVariable": "spell",
      "FinalMpChangeFormula": "-min(previousMp, vanillaMpLoss + t.ma)"
    },
    {
      "Name": "Small gain cap",
      "EventKind": "Gain",
      "MaxFinalMpChange": 5
    }
  ],
  "FinalMpChangeFormula": "vanillaMpChange"
}
```

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

If one or more `MpRules` match the MP event, the first matching rule takes precedence over the
global `FinalMpChangeFormula`. If no rule matches, `FinalMpChangeFormula`, `ProofFinalMpLoss`, or
`ProofFinalMpGain` are used.

Static DR-style proof:

```json
{
  "RewriteObservedDamage": true,
  "ProofFinalDamage": 1,
  "FlatDamageReduction": 10,
  "AffectAllies": true,
  "AffectFoes": true
}
```

Rule-engine proof:

```json
{
  "RewriteObservedDamage": true,
  "AffectAllies": true,
  "AffectFoes": true,
  "DamageRules": [
    {
      "Name": "Foes have DR 10",
      "EventKind": "Damage",
      "Faction": "Foe",
      "FlatDamageReduction": 10,
      "MinFinalDamage": 0
    },
    {
      "Name": "Ally proof clamp",
      "EventKind": "Damage",
      "Faction": "Ally",
      "FinalDamage": 1
    }
  ]
}
```

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

`FinalDamageFormula` and `FinalDamage` are signed. Positive values are damage; negative values are
healing. Damage events clamp the result to `0..9999`; healing events clamp it to `-9999..0`.
`FlatDamageReduction` and equipment DR are only applied to damage events.

Use `EventKind` when a rule should not cross HP event types. For example, a `Damage` rule will not
match a healing event, and a `Healing` rule can safely return a negative `FinalDamageFormula`
without becoming a damage fallback.

If `FlatDamageReduction` is greater than zero, it takes precedence and computes:

```text
finalDamage = max(0, vanillaDamage - FlatDamageReduction)
target.HP = previousHP - finalDamage
```

Otherwise the proof uses:

```text
finalDamage = ProofFinalDamage
target.HP = previousHP - finalDamage
```

If one or more `DamageRules` match the target, the first matching rule takes precedence over the
global `FlatDamageReduction` / `ProofFinalDamage` fallback.

Rule-specific formula example:

```json
{
  "RewriteObservedDamage": true,
  "DamageRules": [
    {
      "Name": "Foes use expression damage",
      "Faction": "Foe",
      "FinalDamageFormula": "max(1, vanillaDamage / 2 + t.level - equipmentDr)",
      "MaxFinalDamage": 999
    }
  ],
  "EquipmentSlots": [
    { "Name": "Body", "Offset": 112, "Width": "Byte" }
  ],
  "EquipmentDrRules": [
    { "Slot": "Body", "ItemId": 172, "DamageReduction": 3 }
  ]
}
```

Action-specific rule example using the sentinel channel:

```json
{
  "RewriteObservedDamage": true,
  "ActionSignalRules": [
    { "VanillaDamage": 7, "Signal": 1, "Variables": { "swing": 1, "cut": 1 } },
    { "VanillaDamage": 8, "Signal": 2, "Variables": { "thrust": 1, "impale": 1 } }
  ],
  "DamageRules": [
    {
      "Name": "Swing table",
      "RequiredActionVariable": "swing",
      "FinalDamageFormula": "max(0, tableClamp(swing, a.pa) - equipmentDr)"
    },
    {
      "Name": "Thrust table",
      "RequiredActionVariable": "thrust",
      "FinalDamageFormula": "max(0, tableClamp(thrust, a.pa) - equipmentDr)"
    }
  ]
}
```

Unknown-field logging settings:

```json
{
  "LogUnknownFieldDiffs": true,
  "UnknownDiffStart": 68,
  "UnknownDiffEnd": 383,
  "MaxUnknownDiffsPerUnit": 160
}
```

The decimal range above is `0x44..0x17F`. This is the current suspected area for fields that are
not yet mapped, including equipment/status/action-state candidates. The next controlled test
should compare `[DUMP]`, `[CANDIDATES]`, and `[DIFF]` lines for units with known different
equipment.

Log analysis:

```powershell
python tools\dump_item_catalog.py
python tools\analyze_battleprobe_log.py
```

Default output:

```text
work\battleprobe_analysis.md
```

If the report says no pointer-keyed `[UNIT]` lines were found, the game is still running an old
loaded assembly. Restart through Reloaded-II so the current DLL loads.

For the next `hp-write-proof` rerun, the analyzer also emits an `HP Write Proof Check`. With the
continuous-polling DLL, `[DAMAGE]` / `[HEALING]` lines include `sampleAgeMs=<n>`, the age of the
previous HP sample for that same unit pointer. A useful proof log should show concrete
`[REWRITE]` events with `finalDamage=1`, no `[REWRITE-FAILED]` lines, and a small maximum
`sampleAgeMs` (the analyzer treats `<=150ms` as a pass-candidate threshold). Missing
`sampleAgeMs` means the game did not load the current instrumented DLL.

To wait specifically for HP-write proof evidence instead of mapping evidence:

```powershell
python tools\watch_live_mapping.py --runtime-events 0 --rewrite-events 1
```

The watcher still requires the current runtime header, then runs the analyzer unless
`--skip-analyze` is passed.

For MP live checks, the analyzer emits an `MP Rewrite Check`. It parses `[MPLOSS]` / `[MPGAIN]`
sample ages, `[RUNTIME-MP]` traces, signed `finalMpChange`, desired MP, dry-run decisions,
concrete writes, skips, and `[MP-REWRITE-FAILED]` lines.

## What this gives us

Achievable with this architecture:

- arbitrary math in C#;
- config-defined target-side integer formulas now;
- attacker-aware formulas at the engine level now, with recent-unit attacker inference available
  for controlled tests;
- action-family variables now through `ActionSignalRules`, with true ability/action ids still to
  be mapped;
- GURPS-like swing/thrust damage tables;
- armor DR as true damage reduction;
- C-bounded percent/type armor responses for v0.2-style `swing` / `thrust` / `crush` /
  `missile` matchups;
- per-weapon/per-skill damage types;
- custom global damage scaling;
- custom ally/enemy rules;
- future support for custom MP, status, reaction, and AI/preview patches.

Not solved by the first version:

- exact damage preview text;
- AI scoring based on our formula;
- full KO lifecycle, crystal/treasure timing, reaction/status side effects;
- perfect ability id capture;
- multi-hit/reaction chains.

Those are follow-up layers, not reasons to abandon the code-mod path.

## Proof plan

1. **Pointer-stable harness.** Restart game, enter a battle, confirm duplicate enemy `char id`s
   no longer corrupt damage deltas because each unit is keyed by pointer.
2. **Unknown field map.** Use the new `0x180` hex dumps plus `[CANDIDATES]`, `[DIFF]`, and
   optional `[MEMTABLE]` lines with controlled units to map equipment, status, CT/action state,
   and possible roster slot fields. Generate `work/item_catalog.csv` first so the analyzer can
   cross-reference full dump byte/word values with item names/categories in
   `work/battleprobe_analysis.md`.
3. **Dry-run evaluation proof.** Implemented in code, unverified live. Enable
   `DryRunRewrites=true` with HP/MP rewrite gates and `LogResolvedRuntimeContext=true`. Confirm
   `[REWRITE-DRY-RUN]` and `[MP-REWRITE-DRY-RUN]` lines appear for controlled events while HP/MP
   remain untouched by the code mod. The watcher can now require HP damage, HP healing, MP loss,
   and MP gain dry-run decisions in one pass; the analyzer's `MP Rewrite Check` summarizes the MP
   side. This proves event detection, context resolution, and formula decisions before any memory
   write risk.
4. **HP write proof.** Implemented in code and partially proven live. Live Test 1 proved HP writes;
   Live Test 1b proved continuous polling fixes non-lethal timing, but also proved reactive polling
   cannot prevent vanilla lethal death. From here, HP rewrite tests must run with the data-layer
   neuter placeholder when the expected custom result can be lethal. The analyzer's
   `HP Write Proof Check` still verifies concrete rewrites, rewrite failures, and fresh
   `sampleAgeMs` baselines for non-lethal proof logs.
5. **Death/KO ownership.** Completed for the current architecture: byte writes are refuted and
   engine-owned death via `MinHpFloor=1` is the accepted path. The remaining live check is no
   longer "can HP=0/KO-bit writes kill?", but whether each real custom-formula/sentinel profile
   preserves this floor and lets the engine deliver KO cleanly on a later vanilla chip.
6. **Healing write proof.** Implemented in code, unverified live. Enable
   `RewriteObservedHealing=true` with `ProofFinalHealing` or a negative `FinalDamageFormula`, then
   verify a vanilla heal can be corrected by the runtime.
7. **Static DR proof.** Implemented in code and covered offline by
   `runtime-simulation-static-dr.example.json`, unverified live. Enable `FlatDamageReduction=10`
   or a matching `DamageRules` entry. Verify hits are reduced after vanilla applies damage.
8. **Equipment DR proof.** Runtime support is implemented, unverified live. Map equipped armor,
   configure `EquipmentSlots` + `EquipmentDrRules`, and verify hits are reduced by item DR.
   Start with exact `ItemId`, then test catalog-backed rules such as `ItemCategory=Armor` plus
   `DamageReductionFormula`. Use `LogResolvedRuntimeContext=true` to confirm whether `Body`
   resolved as present, missing, or ambiguous before trusting any HP outcome. The watcher can
   require slot + DR evidence before accepting the run:

   ```powershell
   python tools\watch_live_mapping.py --runtime-events 0 --target-slots-present 1 --equipment-dr-events 1 --rewrite-events 1 --max-rewrite-failures 0
   ```

9. **Percent/type response proof.** Runtime support is implemented and covered offline. After
   equipment offsets are mapped, configure `DamageResponseRules` for plate/mail/leather/cloth
   and verify that `swing`, `thrust`, `crush`, and `missile` placeholder actions produce the v0.2
   response pattern in live HP outcomes. The `[RUNTIME]` line should show the expected
   `response=raw.../permille...` value for each hit. The watcher can require action, slot,
   response, and trace-variable evidence in one pass:

   ```powershell
   python tools\watch_live_mapping.py --runtime-events 0 --action-signals 1 --target-slots-present 1 --attacker-slots-present 1 --response-events 1 --require-trace-var trace.finaldamage --max-rewrite-failures 0
   ```

10. **Action context proof.** First, use the new `[CTX]` recent-unit evidence in controlled
   battles to see whether attacker touch order is useful. In parallel, use `[RUNTIME]` lines to
   test `ActionSignalRules` by making two ability families produce distinct placeholder deltas
   and verifying formulas see `action.swing`/`action.thrust`. Then capture true active attacker
   and action family/ability through either a current-action structure or UI/preview hook.
11. **Formula engine proof.** Target-side expression formulas are implemented in code, unverified
   live. Offline smoke tests prove the expression engine can already consume attacker PA,
   target armor DR, catalog-backed item metadata, and sentinel action variables. The remaining
   full proof is to feed it live attacker PA/weapon class/action type after step 8 maps that
   context.

If equipment-slot confirmation and the live DR/response checks pass, the answer to "can we put DR
in armor?" is yes for real HP outcomes. Lethal outcomes remain engine-owned through
`MinHpFloor=1` until a future pre-damage/same-hit hook is found.

## Sources

- Local: `docs/modding/00-overview.md`, `03-custom-formula-feasibility.md`,
  `04-re-strategy.md`, `05-battle-data-map.md`
- Local: `codemod/fftivc.generic.chronicle.codemod/Mod.cs`
- Local: `C:\Reloaded-II\Mods\fftivc.utility.modloader\fftivc.utility.modloader.Interfaces.xml`
- Nenkai FFT mod loader: https://github.com/Nenkai/fftivc.utility.modloader
- Nenkai FFT mod install docs: https://nenkai.github.io/ffxvi-modding/modding/installing_mods_fft/
- Faith Framework: https://github.com/Nenkai/FaithFramework
- Reloaded-II hook docs: https://reloaded-project.github.io/Reloaded-II/CheatSheet/CallingHookingGameFunctions/
- Reloaded.Hooks asm hook docs: https://reloaded-project.github.io/Reloaded.Hooks/AssemblyHooks/
