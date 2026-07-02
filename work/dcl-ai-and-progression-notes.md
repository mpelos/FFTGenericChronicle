# DCL research notes — AI scoring view (§7) + job-level/progression inputs (§2)

Date: 2026-07-02. Offline internet + repo-data research only (no live game). Feeds
`docs/modding/08-dcl-information-requirements.md` §2 (job/progression) and §7 (AI scoring view).

Confidence labels: **Proven** / **Strong** / **Hypothesis** / **Refuted** (manual convention).

Access note: `ffhacktics.com` (all subdomains: www/rc/dom/sb/ww, wiki + smf forum) sits behind a
Cloudflare JS challenge and returns 403 to non-browser fetchers; the Wayback Machine has almost none
of the wiki AI pages archived. Wiki/forum facts below were extracted through search-engine snippets
of the live pages (each claim cites its page). Full-page verification needs a real browser session.

---

## GOAL A — How the FFT AI decides actions (AI scoring view)

### A.1 The PSX decomp repo does NOT contain the AI

**Proven** (repo tree enumerated via GitHub API, 2026-07-02).
https://github.com/Talcall/FFT-1997-Decomp covers only `SCUS_942.21` (the boot executable): LIBC/
LIBCD/LIBGPU/... stubs, a "SCUS_942_21 RAM" folder (file loader, `BATTLE_ENTRY_DEPRECATED`,
`CallInitialiseGameState`, `Draw_NowLoading`, ...) and the "SUZUKI Music Library". 74 tree entries
total. **All battle logic — formulas, forecast, AI — lives in `BATTLE.BIN`, which this repo has not
decompiled at all.** The decomp therefore cannot answer the same-calc question; the FFHacktics wiki
BATTLE.BIN disassembly is the primary source instead.

### A.2 PSX AI architecture per the FFHacktics wiki disassembly

Sources (wiki disassembly pages, snippet-verified):
- https://ffhacktics.com/wiki/AI_ability_use_control_routine
- https://ffhacktics.com/wiki/AI_Ability_Use_Decisions
- https://ffhacktics.com/wiki/AI_Ability_Data_Setting
- https://ffhacktics.com/wiki/AI_Initial_Targeting_Selection
- https://ffhacktics.com/wiki/Set_AI_ability_considerations_for_all_units_and_self
- https://ffhacktics.com/wiki/Targeting_routine (rc. mirror)
- Forum threads (not fetchable offline): "FFT AI Documentation" topic 10985; "How the AI Works"
  topic 11590; "AI and AoE ASMs" topic 12518 (partially recovered via Wayback printpage capture).

What the snippets establish (**Strong**, multi-page corroboration; full-page read still pending):

1. **Decision pipeline.** The AI ability-use control routine runs a staged decision loop: the Main
   Routine (decision 7) *initializes Ability Data*, then determines Random Use, **Target Map**, and
   Crystal/Treasure Check; a Positioning Check (decision 8) finishes Target Map evaluation ("is the
   unit in the right place to correctly use this ability"); Math Skill Evaluation (decision 6) loops
   per candidate until complete. Each ability is evaluated then selected/rejected, setting a
   "Decision Value" that seeds the next ability's evaluation.
2. **The decision reads the computed effect on the target, not a static table.** `AI Ability Use
   Decisions` returns r2 ∈ {0 = Unusable, 1 = OK to use, 2 = Not OK}; the control routine stores it
   as "Current Ability Decision" where **0 = Cannot Effect Unit, 1 = usable, 2 = Heals Target**.
   "Heals Target" as a *computed* classification (it must catch undead HP-inversion, absorption,
   etc.) requires actually evaluating the ability's formula against that target — a static ability
   flag cannot know whether *this* target heals.
3. **Per-target working tables.** `AI Ability Data Setting` fills an AI data block based at
   `0x8019f3c4` (rows at +0x0ef0, +0x1778, ID info at +0x198c) — i.e. the AI materializes per-
   ability/per-target evaluation data into its own tables before deciding. It also contains
   hardcoded inventory logic (Item/katana/shuriken counts, "forces Jump as hostile with AoE
   nullified for item inventory") and known bugs ("Throw Item counted as a Move", "short CT/no CT
   not correctly checked").
4. **The AI evaluation applies real combat math, including elemental interaction.** Aerostar's
   Battle Mechanics Guide (GameFAQs FAQ 3876, recovered via Wayback capture 2022-02-01): "the AI
   thinks that HOLY SWORD skills are Holy-elemental... if you wear Holy-absorbing or nullifying
   equipment, then the AI will not target HOLY SWORD skills on you." So the AI's evaluation runs
   target elemental absorb/nullify exactly like the damage calc — and the *bug* (vanilla Holy Sword
   damage is weapon-elemental, not Holy) shows the AI layer has its own thin classification wrapper
   that can diverge from execution in specific hardcoded spots.
5. **Community practice corroborates same-formula evaluation.** Formula ASM hacks on FFHacktics are
   shipped without companion "teach the AI the new damage" patches, and the dedicated AI ASM work
   that does exist targets the *priority/scoring* layer, not damage amounts — e.g. Dokurider's
   "Stat and Golem Priority" hack (topic 12518, Wayback-recovered) adds priority rules for stat-
   break abilities precisely because the vanilla AI lacks *scoring* code for that class of effect.
   I.e. damage/heal magnitude comes to the AI "for free" from the shared calculation; what is
   AI-specific is the priority arithmetic layered on top, plus per-ability **AI behavior flags**
   (the FFTPatcher "AI: ..." flags) that pre-classify abilities for the decision layer.

**Conclusion for the ORIGINAL engine (Strong):** the PSX AI evaluates candidate (ability, target)
pairs by computing the ability's actual effect — same formula family the forecast and execution
use — into AI-owned working tables, then applies a separate priority/decision layer guided by
per-ability AI flags. It is NOT an independent approximate evaluator; but it is also NOT literally
"the player forecast": it has its own driver, its own storage, and a thin classification wrapper
with known divergences (Holy Sword elementality; inventory/CT bugs).
The one claim I could not pin offline to an instruction-level citation is that the AI's evaluator
enters the exact same routine address as the player forecast (vs. a parallel copy) — **Hypothesis
(strongly favored)**; a browser session on the wiki pages above would settle it.

### A.3 Most plausible IVC model and DCL implications

IVC (`FFT_enhanced.exe`, Faith engine) is a faithful WotL-ruleset reimplementation (docs/modding/05
§1), and the data side carries the PSX AI design forward: `AbilityData.xml` exposes
`AIBehaviorFlags` per ability (docs/modding/03 §B; work/dcl-catalog-coverage.md row "Targeting
flags" maps TargetAllies/TargetEnemies/TargetMap/... to it; work/1782680077 notes it exposes
high-level AI metadata like `PhysicalAttack`, `HP`, `LinearAttack`). Carrying AI-hint data forward
only makes sense if the AI decision layer was also carried forward.

**Most plausible IVC model (Hypothesis, favored): same-calc.** The AI evaluation sweep invokes the
same (Denuvo-virtualized) calculation the forecast/execution use, writing per-candidate results
into engine-owned staging, then scores them with the flag/priority layer.

Implications for DCL (assuming same-calc):

- **What already steers the AI today: input-side writes.** The VM calc reads *live memory*, not a
  cached copy (docs/modding/05 §: evade-byte writes honored by the roll; "The VM read live memory,
  not a cached forecast copy"). If the AI calls the same calc, it sees the same rewritten inputs —
  so the proven input-control surfaces (defender evade bytes, stats, equipment) steer AI hit/evade
  expectations with zero extra work. This is the one DCL lever that is plausibly AI-visible *now*.
- **What does NOT steer the AI:** (a) the forecast hit-% display hook (`0x227FFE` painting display
  buffer `0x7832C0`) — that is retained-mode UI paint; no AI would read a screen-text buffer;
  (b) pre-clamp/staged-result rewrites at apply time — they fire after the decision was made.
  Output-control therefore creates an AI blind spot: enemies evaluate native numbers, players see
  DCL numbers.
- **What would be needed:** a hook on the shared calc entry (or on the staged-result write) that
  fires during AI evaluation, applying the same DCL formula rewrite the player forecast gets. Then
  same-calc gives AI parity for free, exactly like PSX formula hacks.
- **Detecting AI evaluation (Hypothesis-level plan):**
  1. Instrument staged-result writes (`+0x1C4`/`+0x1C6` HP, `+0x1C0` evade-type, `+0x1BE`) and the
     pre-clamp-adjacent calc sites during an ENEMY's think phase (after its CT>=100, before any
     targeting UI/animation). If staged fields pulse across multiple candidate targets before the
     enemy commits, that is the AI sweep — and the write site is the steering hook.
  2. Candidate anchor already in hand: the refuted "roll-verdict" site (`0x30F4A7` / callee
     `0x30FA34`) fires in bursts of ~8–11 per unit per CT tick and was classified as "a per-unit
     turn/CT/AI evaluation that returns 0" (work/roll-verdict-override.md). Re-purpose that probe:
     correlate its bursts with enemy decision timing and with any staged-field activity.
  3. Discriminator player-vs-AI context: the forecast display path (`0x227FFE`) fires only for the
     player UI; a calc invocation with NO display-object dirtying during an AI unit's turn is AI
     evaluation. Actor-context classification (native-frame actor = AI unit, team byte) gives the
     second signal.

---

## GOAL B — Job level / per-job JP progression inputs

### B.1 Classic rule and threshold table

Job level is derived from **cumulative JP earned in that job** crossing fixed thresholds
(**Strong**; GameFAQs WotL board threads "Guide to JP per job level" / "How much JP is needed?",
IVC-era guides — RPG Site jobs guide, GameFAQs IVC "Changes to JP, Abilities" FAQ — confirm the
WotL values carry into IVC):

| Job level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Total JP required | 0 | 200 | 400 | 700 | 1100 | 1600 | 2200 | 3000 |

JP gain per action (already recorded in docs/modding/05-reverse-engineering.md:272):
`8 + 2*JobLevel + Level/4`, shared JP = 1/4 of that. Note this means the ENGINE evaluates JobLevel
at runtime on every JP award — a live JP-gain event is a usable RE tracer for where job level lives.

**IVC data-side owner of the thresholds (Strong):** `GeneralJob` (Nex, 21 rows, largely hardcoded)
has `RequiredJobExp|int[]` — "shorts stored as ints, should always be 8 elements"
(C:\Reloaded-II\Mods\fftivc.utility.modloader\Nex\Layouts\ffto\GeneralJob.layout). Eight elements =
eight job levels; this is almost certainly the threshold table above as shipped data. DCL should
read thresholds from `GeneralJob.RequiredJobExp` rather than hardcode them. (Same table:
`RequiredJobIds[]` / `RequiredJobLevels[]` / `RequiredJobPositions[]` = the job-tree unlock rules,
not unit state.)

### B.2 What the modding surfaces expose today

- `work/baseline_jobs.csv` (header inspected): stat curves/multipliers, move/jump/evasion, innates,
  elements, equippables — **no progression state** (job data is class definition, not unit state).
- `docs/modding/03-battle-data-map.md` §C: job tree/unlock = `GeneralJob` + `JobNeedLevelData.xml`;
  §E (ENTD): encounter units carry "level (1-99 fixed, 100+ = party-relative), bravery, faith, job,
  **job level**, secondary skillset...".
- `OverrideEntryData.layout` (ENTD override, Nex): `Level|short` at +0x7C and **`JobLevel|short` at
  +0x7E** — spawn-time patch fields for generated encounter units (auto-learn/JP seeding), NOT a
  runtime per-unit progression surface. (**Proven** as a layout fact.)
- Per-ability JP costs exist (`ability.en.nxd` JpCost1/2; decode `JP = JpCost1 + 256*JpCost2`,
  docs/modding/01) — costs, not unit state.
- Nex layout sweep (C:\Reloaded-II\...\Nex\Layouts\ffto\, 250+ files): **no table carries per-unit
  per-job JP** — roster/save state is not a Nex surface at all.
- `docs/modding/04-engine-memory-model.md`: live battle-unit struct spans ≥ `0x200` bytes; grep for
  JP/JobLevel/job-level over the doc: **zero hits**. No JP or job-level offset is currently owned.

### B.3 Conclusion — can DCL compute jobLevel at runtime today?

**No (Proven for the documented surfaces).** The threshold table is (very likely) readable from
`GeneralJob.RequiredJobExp`, and the JP→level rule is known, but the *input* — this unit's
cumulative JP in job J (or its current job level) — has no owned runtime location. PSX precedent
says it exists per unit: PSX formation/roster unit data stores JP and Total JP per job as parallel
16-bit arrays (FFHacktics BATTLE.BIN/formation data docs; **Hypothesis** for exact IVC shape), and
the IVC menu displays per-job JP and job level, so the data is definitely in process memory —
likely in a roster/save unit block that the ≥0x200 battle struct references or was copied from.

### B.4 Minimal live-test plan (find per-job JP / job level)

1. **Menu-anchored scan (cheapest).** In the party menu, record one unit's (current JP, total JP)
   for 3+ jobs. Scan process memory for consecutive u16 (then u32) arrays matching the JP sequence
   across jobs; the total-JP twin array should sit at a fixed stride from it. Expect a roster block
   per unit; recover unit stride from two units' arrays.
2. **Delta confirmation.** Earn JP in battle with a known action (predict via
   `8 + 2*JL + Level/4`); re-scan: current-JP cell and total-JP cell both move by the prediction
   (crossing a `RequiredJobExp` threshold should tick the displayed job level).
3. **Battle-struct byte sweep (parallel cheap probe).** Current-job job level is a 1–8 value; sweep
   unexplored bytes of the 0x200 battle struct across 4+ units against menu-displayed job levels.
   If found, DCL gets current-job jobLevel without the roster join; other-job levels still need (1).
4. **Roster→battle join.** Once the roster block is found, locate the pointer/index linking battle
   unit → roster unit (spawn copies name/brave/faith/equipment — scan for shared identity bytes).
   Then `jobLevel(unit, job) = levelFromThresholds(rosterJP[job], GeneralJob.RequiredJobExp)`.
5. **Fallback if RE stalls:** settings-side jobLevel table keyed by (unit name, job) in DCL config,
   or a proxy from learned-ability count in the job's skillset (both lossy; last resort).

---

## Citations

- https://github.com/Talcall/FFT-1997-Decomp (tree enumerated via GitHub API; no BATTLE.BIN code)
- https://ffhacktics.com/wiki/AI_ability_use_control_routine — decision stages, Decision Value
- https://ffhacktics.com/wiki/AI_Ability_Use_Decisions — r2 return 0/1/2; "Heals Target" decision
- https://ffhacktics.com/wiki/AI_Ability_Data_Setting — AI tables @0x8019f3c4; inventory logic; bugs
- https://ffhacktics.com/wiki/AI_Initial_Targeting_Selection, .../Set_AI_ability_considerations_for_all_units_and_self, rc.ffhacktics.com/wiki/Targeting_routine
- https://ffhacktics.com/smf/index.php?topic=10985.0 (FFT AI Documentation), topic=11590.0 (How the
  AI Works), topic=12518.0 (AI and AoE ASMs; Wayback printpage 2020-10-28 recovered)
- Aerostar, Battle Mechanics Guide — https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
  (Wayback 2022-02-01 capture; AI Holy Sword elemental note)
- JP thresholds: https://gamefaqs.gamespot.com/boards/937312-final-fantasy-tactics-the-war-of-the-lions/61790942 ;
  https://gamefaqs.gamespot.com/boards/937312-final-fantasy-tactics-the-war-of-the-lions/60857948 ;
  https://www.rpgsite.net/guide/18138-final-fantasy-tactics-the-ivalice-chronicles-jobs-guide ;
  https://gamefaqs.gamespot.com/pc/538659-final-fantasy-tactics-the-ivalice-chronicles/faqs/82197/changes-to-jp-abilities-and-special-characters
- Local: docs/modding/03-battle-data-map.md (§B/§C/§E), 01-data-editing-surfaces.md (JP decode),
  04-engine-memory-model.md (unit struct ≥0x200; no JP offsets), 05-reverse-engineering.md:272 (JP
  gain formula), work/roll-verdict-override.md (per-unit turn/CT/AI evaluation bursts),
  work/dcl-catalog-coverage.md (AIBehaviorFlags), C:\Reloaded-II\Mods\fftivc.utility.modloader\Nex\
  Layouts\ffto\GeneralJob.layout + OverrideEntryData.layout.
