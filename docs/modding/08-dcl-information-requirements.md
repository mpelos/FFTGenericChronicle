# DCL Runtime Information Requirements

This document defines the runtime information the code mod must own in order to implement the Deep
Combat Layer (DCL). It is not a design document for the DCL itself; the rules live in
`docs/deep-combat-layer/`. It is also not a test log; probe evidence and dated investigation notes
belong in `work/`.

Use this document as the canonical inventory of required information surfaces. When a requirement
becomes a proven engine fact, promote the fact into the owning modding document:

- `03-battle-data-map.md` owns editable data tables, enums, and catalog sources.
- `04-engine-memory-model.md` owns live memory fields, unit structs, actor arrays, and engine state.
- `05-reverse-engineering.md` owns hook anchors, AOBs, RE reasoning, and native-code constraints.
- `06-code-mod-runtime-dsl.md` owns runtime settings, formula variables, and code-mod rule schemas.

Confidence labels follow the rest of the modding manual: **Proven**, **Strong**, **Hypothesis**,
**Refuted**. A label of **Strong (static)** means the finding is corroborated by offline evidence
(disassembly of real `.xcode` code, snapshot diffs, decomp/cheat-table cross-reference) but has not
yet had its one confirming live test.

**2026-07-02 offline investigation sweep:** every open front below was advanced as far as offline
analysis allows. The per-front evidence and the live-test plans live in
`work/dcl-catalog-coverage.md` (§3/§4 data coverage), `work/dcl-unit-state-candidates.md` (status
bitmap, position/facing, turn owner, JP/job-level), `work/dcl-action-id-candidates.md` (action id,
pending record, AoE batching), `work/dcl-magic-status-reaction-candidates.md` (magic evade, status
infliction, reaction dispatch), and `work/dcl-ai-and-progression-notes.md` (AI scoring model, job
progression rules). The consolidated ordered live-test checklist is
`work/dcl-live-test-master-plan.md`.

## 1. Action Identity and Lifecycle

DCL formulas are action-dependent. The runtime must know not only who lost HP, but what action is
being resolved, who owns it, and which phase of the action lifecycle is active.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Acting unit / caster pointer | Every formula needs attacker/caster stats, equipment, traits, and zodiac. | **Strong** through native-frame actor, pre-clamp, pending, and selector context. CT is not accepted as a DCL source. See `04` and `06`. |
| True action id / ability id | Damage type, spell power, status category, target mode, element, and special behavior depend on the exact action. | **Strong (static)**. Current-action global block `0x14186AF60..F4`: `word[0x14186AFF0]` = ability id, `dword[0x14186AFF4]` = caster unit index; pending record `unit+0x1A1` (type) / `+0x1A2` (ability id, byte-exact twice); executor `actor+0x142` statically proven a copy of `unit+0x1A2`. One live confirmation pending. See `work/dcl-action-id-candidates.md`. |
| Action family | Physical, magical, healing, status, reaction, item, weapon special, and monster actions use different DCL branches. | **Strong** only where data/catalog classification or action-signal rules can infer it. |
| Resolution phase | Forecast, confirmation, charge wait, execution, hit roll, HP/MP apply, status apply, and reaction windows have different writable surfaces. | **Strong** for forecast display and HP/MP pre-clamp. Roll/status/reaction phases now have **Strong (static)** anchors: magic-accuracy roll `0x304E33`, status-proc roll `0x306636`, reaction Brave-gates `0x30BE54/AC/FC`+`0x30BF48`, post-hit reaction author `0x30AA80` — all real hookable code sharing RNG `0x278EE0`. See `work/dcl-magic-status-reaction-candidates.md`. |
| Target model | Actions may target a unit, a tile epicenter, a line, self, allies, enemies, or an area. AoE targets can differ between forecast and resolution. | **Strong** for final impacted HP targets. Pre-resolution: the pending record carries the tile epicenter on the caster (`+0x1AC` X / `+0x1B0` Y, cross-proven against target coordinates); the forecast target ptr lives at `qword[0x14186AF68]`; targeting-shape filters exist in data (`AIBehaviorFlags`). See `work/dcl-action-id-candidates.md`, `work/dcl-unit-state-candidates.md`. |
| Multi-hit identity | Dual-wield, multi-strike skills, and AoE batches need per-hit/per-target context and stable batching. | **Strong** for HP-event separation. AoE resolves as one sequential pre-clamp event per victim under a constant caster/ability id; dual-wield = 2 events; the engine exposes NO hit index — batch state must be DCL-owned (group by caster+id within a resolution window). See `work/dcl-action-id-candidates.md`. |
| Reaction and counter source | Counter, Hamedo/First Strike, Blade Grasp, parry/block, and similar reactions can invert or interrupt the apparent attacker/target relation. | **Strong/partial**. HP-applying reaction damage resolves through normal actor context; First-Strike cancelled incoming source visible at target-cache time. NEW **Strong (static)**: the unit's reaction SET is bitfield `+0x94..0x97` (dispatcher `0x30B584` → reaction ids `0x1A6..0x1BE`); trigger = Brave-gate rolls. Named INCOMING action id at reaction time remains the open piece (candidate: read `0x14186AFF0` in the reaction frame — live test planned). See `work/dcl-magic-status-reaction-candidates.md`. |
| Native side-effect bundle | DCL must know whether the native action also carries status, knockback, elemental behavior, drain, MP damage, or special flags. | Data surfaces expose many fields. NEW **Strong (static)** staged-effect surface on the target: apply-mask `+0x1D0` (bit 8 = status), effect-kind `+0x1C0`, staged ailment id `+0x1A8`, staged HP/MP words `+0x1C4..0x1CA` — the runtime can read the full native effect bundle just before apply. See `work/dcl-magic-status-reaction-candidates.md`. |

## 2. Unit and Battle State

DCL formulas require a stable live unit model plus enough transient battle state to interpret the
current action.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Unit pointer and identity | Formula context, HP/MP application, equipment reads, and target matching are pointer-based. | **Proven**. See `04`. |
| Core stats | PA, MA, Speed, Brave, Faith, level, HP, MP, CT, Move, Jump, job, zodiac, and gender flags feed formulas or classification. | **Proven** for the main unit struct fields. See `04` and `06`. |
| Raw vs effective stats | DCL may use raw stats for traits and effective stats for equipment-modified output. | **Proven** for PA/MA/Speed raw and effective fields. |
| Job and job-level data | Weapon skill progression and job-family grades depend on job identity and progression. | Job id is **Proven**. Job level = cumulative JP vs thresholds; thresholds live in Nex `GeneralJob.RequiredJobExp[8]` (**Strong**). Per-unit per-job JP: **Hypothesis** — two 23-word arrays at `unit+0xF0`/`+0x11E` indexed `jobId-0x4A`, job-level nibbles at `+0xE4..0xEE` (snapshot-diff evidence). Live confirm planned. See `work/dcl-unit-state-candidates.md`, `work/dcl-ai-and-progression-notes.md`. |
| Active turn owner | Defense depletion resets on a defender's own turn, and wait/action behavior affects timing without always implying an attack. | **Strong (static)**: `unit+0x1B8 == 1` marks the active-turn unit — exactly-one invariant across all 40 archived snapshots, follows the turn sequence, independent of `+0x1BA` (in-flight action owner). Live poll test planned. See `work/dcl-unit-state-candidates.md`. |
| Pending action state | Charged spells, Limits, delayed effects, and queued actions require caster/action state between confirmation and resolution. | **Strong** for observed charged damage/healing; full pending record layout is not owned. |
| Active source marker | Immediate actions need a source signal that does not rely on CT timing. | **Strong/Hypothesis** depending on action type. See actor context in `04`. |
| Status flags | Shell, Protect, Haste, Slow, Don't Act, Don't Move, Undead, Petrify, charging, defending, and similar states affect formulas or legality. | **Strong (static)**: three parallel 5-byte status arrays — source `+0x57..0x5B`, immunity `+0x5C..0x60` (Hypothesis), effective `+0x61..0x65`, durable master `+0x1EF..0x1F3` — with `effective == master \| source` holding across all 40 snapshots; bit layout matches classic PSX verbatim (11 bits also confirmed by the community cheat table); durations at `+0x66..`. Live sweep planned. See `work/dcl-unit-state-candidates.md`. |
| Position and facing | Front/side/back, reach, line of attack, and terrain rules require X/Y/facing. | **Strong (static)**: X `+0x4F`, Y `+0x50`, facing `+0x51` (cheat-table + per-unit-distinct snapshot values + pending-epicenter cross-proof `+0x1AC/+0x1B0` == target's `+0x4F/+0x50`). Live confirm planned. See `work/dcl-unit-state-candidates.md`. |
| Height and tile occupancy | Vertical tolerance, AoE membership, reach, and some skills depend on map tiles. | Height is NOT stored in the unit struct (map-derived); tile epicenter of a pending action is on the caster record (`+0x1AC` X / `+0x1B0` Y). Bridge-tile live test planned. See `work/dcl-unit-state-candidates.md`. |
| Team/faction/allegiance | Friendly fire, enemy targeting, healing legality, and AI scoring depend on allegiance. | **Proven** for stable team/faction signals used in sanity checks and context. |

## 3. Equipment and Item Metadata

DCL converts existing FFT equipment into a richer rules vocabulary. Runtime formulas need both live
equipped item ids and a catalog that translates those ids into DCL concepts.

**Coverage audit complete (2026-07-02, `work/dcl-catalog-coverage.md`):** the audit classifies every
row below as EXISTS (in `work/item_catalog.csv` / Nex layouts), DERIVABLE, or MUST-AUTHOR. EXISTS:
item-id→row (full 261-row table), weapon reach/range, W-EV parry, S-EV block, weapon element + full
absorb/null/halve/weak/strong affinity sets, status innate/immune/starting, throwable/two-hands/
dual-wield flags, magic-rod procs and weapon procs. DERIVABLE: weapon family (35 categories → 11 DCL
families), drain/heal/gun behavior (via native formula id). MUST-AUTHOR: damage type
(cut/thrust/crush/missile/magic — absent from all data), Weight (no weight field exists on any IVC
item surface), armor class + DR (ItemArmorData exposes only HP/MP bonuses), DCL weapon-modifier
values, and synthetic rows for monster/fist "weapons". Known code gap: `ItemCatalogEntry` currently
drops columns already present in the CSV (weapon_range, attack_flags, elements, options_ability_id,
status/affinity columns).

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Equipment slots | Every unit formula needs current weapon, shield, armor, accessory, and off-hand data. | **Proven**. Slot offsets are owned by `04`. |
| Item id to catalog row | Live slot ids must resolve to deterministic metadata. | **Strong** through `work/item_catalog.csv` and `ItemCatalog`. |
| Weapon family | Sword, knife, spear, bow, crossbow, gun, staff, rod, fist, monster, and special families drive skill and damage mode. | Partially represented through catalog category fields. |
| Damage type | Cut, thrust, crush, missile, magic, status, and special categories drive DR and wound multipliers. | Needs explicit DCL catalog mapping. |
| Weapon modifier | DCL weapon damage adds a family/item modifier to thrust/swing/base damage. | Needs explicit DCL catalog mapping. |
| Reach and range | Reach-1, reach-2, ranged weapons, point-blank weakness, and outrange rules require range metadata. | Some data exists in item/ability tables; runtime exposure is incomplete. |
| Parry value | Weapon parry is an active defense and can deplete. | Live byte is **Proven**; DCL depletion state is not an item-catalog fact. |
| Shield block value | Shields become finite Block resources. | Live evade bytes are **Proven**; DCL block capacity/value mapping needs catalog ownership. |
| Armor class and DR | Armor supplies type-specific DR instead of only HP. | Needs DCL catalog mapping for every body/head/accessory class. |
| Weight | Weight affects Move/Dodge and possibly defense economy. | Needs DCL catalog mapping and formula exposure. |
| Elements and affinities | Magic and elemental weapons need element, absorb/nullify/halve/weak/strong, and boost data. | CSV sources contain candidate fields; `ItemCatalogEntry` does not expose the full surface. |
| Status immunities and innate statuses | Status formulas need target immunities and granted statuses. | CSV sources contain candidate fields; runtime formula exposure is incomplete. |
| Special weapon behavior | Magic rods/staves, random weapons, drain, thrown weapons, and weapon-proc skills need DCL handling. | Data can identify some cases; complete runtime classification is incomplete. |

## 4. Ability, Spell, and Effect Metadata

DCL cannot rely only on native formula ids. Each ability needs a DCL-facing definition that the
runtime can read at forecast and execution time.

**Coverage audit complete (2026-07-02, `work/dcl-catalog-coverage.md`):** EXISTS: ability id/name
(512 ids, 491 named), JP, AbilityType (Normal/Reaction/Support/Movement/Item/Throw/Jump/Aim/Math),
27 AIBehaviorFlags (ally/enemy/map targeting filters), Reflectable/Evadeable/AffectedByFaith/Linear
flags. MUST-AUTHOR: the per-ability BASE action math — native Formula/X/Y, Element, InflictStatus,
Range, AoE, Vertical, base CT, base MP — is exe-hardcoded; `OverrideAbilityActionData` (368 rows) is
a sparse patch layer whose math columns are all `-1` in stock. **Baseline EXTRACTED (2026-07-02):**
`work/wotl_ability_action_baseline.csv` — 512 rows, all 368 Normal abilities fully populated
(Formula/X/Y/Range/AoE/Vertical/CT/MP/element/status + 32 action flags) from FFTPatcher's vanilla
WotL binaries; id alignment PSX/WotL/IVC verified 1:1 three independent ways (row count + field set,
432/491 name matches with zero shifts, all 29 stock CT/MP overrides coherent at the same id). Notes
and regenerator: `work/wotl-ability-baseline-notes.md`, `work/wotl_ability_baseline_extract.py`.
Still MUST-AUTHOR: DCL action kind, spell/heal power, status category, hit/avoidance policy.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Ability id and name | The runtime needs a stable key for DCL formula tables and debug logs. | Names/data are available in NXD baselines; live action-id mapping is partial. |
| Native formula id/X/Y | Useful as baseline classification and for placeholder/neuter behavior. | Data surfaces are documented in `01`, `02`, and `03`; inherited native defaults are harder. |
| DCL action kind | Physical damage, magic damage, healing, status, reaction, item, summon, terrain, and special actions branch differently. | Needs a DCL metadata table. |
| Spell power / heal power | Magic and healing formulas require DCL power values. | Needs a DCL metadata table. |
| Element | Magic, weapon elements, resistance, absorb, and weakness depend on element identity. | Data surfaces exist; runtime formula exposure is incomplete. |
| Damage type | Physical formulas require cut/thrust/crush/missile/etc. | Needs DCL metadata by weapon/action. |
| Status category | Status contests depend on mental, physical, magical, taunt/fear, or special categories. | Needs DCL metadata by status action. |
| MP cost and CT/charge time | Pending tracker, forecast, and action economy need cost/charge data. | Data surfaces exist; live pending semantics need complete ownership. |
| Targeting shape | Unit target, tile target, radius, vertical tolerance, line, cone, self, ally/enemy filters, and random targeting affect AoE resolution. | Data surfaces exist; runtime target reconstruction is incomplete. |
| Hit/avoidance policy | Some actions should use physical defense, magic evade, no defense, guaranteed hit, or special checks. | Needs DCL metadata and runtime authority. |
| Side effects | Knockback, drain, MP damage, healing inversion, undead inversion, revive, death sentence, and similar effects need explicit classification. | Partial through native data/status fields; not complete as a DCL surface. |

## 5. Formula Inputs and Derived Variables

The DSL can express arbitrary math, but the DCL needs specific variables to be present and stable.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Thrust/swing/base tables | Physical damage uses GURPS-like thrust/swing curves and weapon-family branches. | DSL tables/maps can express this. |
| DR matrix | Target armor class and damage type select subtractive DR. | DSL maps/matrices can express this once catalog metadata exists. |
| Wound multipliers | Cut/thrust/crush/missile/magic categories need multipliers after DR. | DSL formulas can express this. |
| Penetration floor | DCL can force minimum penetrating injury after DR. | DSL formulas can express this. |
| Brave curve | Brave affects physical offense, active defense, reactions, and composure. | Brave is **Proven**; DCL curve tables can live in settings. |
| Faith curve | Faith affects magic output, magic vulnerability, healing, and magic status vulnerability. | Faith is **Proven**; DCL curve tables can live in settings. |
| Zodiac multiplier | Attacker-target zodiac compatibility affects damage and possibly hit/status. | Zodiac is exposed; compatibility matrix can live in settings. |
| Weapon skill | Hit rolls and some weapon damage depend on job/family skill. | Formula maps can express it; job-level/family-grade inputs need ownership. |
| Crit/fumble thresholds | DCL 3d6 rolls need deterministic event-seeded critical/fumble handling. | DSL random helpers exist; result-authority integration is incomplete. |
| Rounding and clamp policy | Preview and execution must use identical integer rounding/clamping. | DSL can express this; shared formula use is required. |

## 6. Outcome Authority

DCL is not only a damage formula. It replaces several native combat decisions. The runtime must be
able to author the final outcome and make forecast match execution.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Forecast damage/heal number | DCL requires preview/result parity. | **Proven** through forecast poke/source paths. See `05` and `06`. |
| Forecast HP ghost bar | The player must see the same HP loss/gain implied by the formula. | **Proven** through forecast object fields. |
| Forecast hit percent | DCL hit/defense math must surface before confirmation. | **Proven** as display control; formula-driven shipping integration is incomplete. |
| Runtime HP damage | DCL damage must land through native HP/KO lifecycle. | **Proven** through native pre-clamp staged debit; same-frame managed callbacks can write fixed debit and caster/target-stat formulas for basic, instant named, and delayed named damage before vanilla applies HP. |
| Runtime healing | DCL healing must land through native credit path and explicit heal context. | **Proven** for direct and delayed explicit heals. |
| Runtime MP | DCL can alter MP costs, MP damage, and MP restoration. | **Proven/Strong** through staged MP fields and settings. |
| Physical miss/block/parry | DCL defense rolls must suppress or transform physical hits. | Proof hooks exist; per-action formula authority is incomplete. |
| Magic evade | DCL magic avoidance is separate from physical parry/block. | **Strong (static)**: Faith accuracy path = function `0x304DF0`, roll at `0x304E33` — spell hit-% (`obj+0x2C`) × target-Faith snapshot (`g_7B079D`) ÷100 → shared RNG `roll(range,chance)` at `0x278EE0`; miss stamps evade-kind `+0x1C0 = 0x06`. Explains why physical evade bytes never gate magic. Always-hit lever candidate: hook `0x304E2B`, force `edx=100`. Live confirm planned. See `work/dcl-magic-status-reaction-candidates.md`. |
| Status infliction | DCL status contests must replace native status odds. | **Strong (static)**: status-proc roll = function `0x3065F0`, roll at `0x306636` — `roll(100, g_7B07AC)` where `g_7B07AC` (`0x1407B07AC`) is the staged status hit-%; pass sets apply-mask `+0x1D0 \|= 8`, kind `+0x1C0 = 8`, ailment id staged at target `+0x1A8`. Levers: poke `g_7B07AC` (data write, 0 or 100) or hook `0x30662C`. Live confirm planned. See `work/dcl-magic-status-reaction-candidates.md`. |
| Reaction triggering | DCL reactions depend on Brave/courage/caution and action context. | Brave gating **Proven**; structure now **Strong (static)**: the reaction SET is a 4-byte bitfield `unit+0x94..0x97`; dispatcher `0x30B584` maps bits to reaction ids `0x1A6..0x1BE`; four Brave-gates (`0x30BE54/AC/FC`, `0x30BF48`) each do `roll(100, Brave[+0x2B])`; post-hit author walks the bitfield at `0x30AA80`. Bitfield + gates are real hookable code. Live confirm planned. See `work/dcl-magic-status-reaction-candidates.md`. |
| Native side-effect suppression | Placeholder/native effects must not apply before DCL decides the final result. | Damage neuter model exists; status and special side effects need complete handling. |

## 7. DCL Runtime State

Some DCL mechanics are not pure reads from the game. They require code-mod state that remains
consistent with native turns, hits, and reactions.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Defense depletion | Parry and Block deplete per incoming hit and reset on defender turn. | Requires custom state keyed by unit pointer and battle lifecycle. |
| Hit batch identity | Multi-hit and AoE actions must consume defenses and apply formulas per hit/target. | HP events provide target separation; explicit batch state is needed. |
| Pending action cache | Charged actions need caster/action/target metadata between confirmation and resolution. | **Strong** for damage/healing; complete schema is incomplete. |
| Event seed | Random DCL rolls must be deterministic per action/hit for preview/execution parity where possible. | DSL random helpers exist; event identity needs ownership. |
| Forecast cache | Preview formulas and execution formulas need shared context or equivalent recomputation. | Damage/heal forecast control exists; full DCL context parity is incomplete. |
| Battle reset/lifecycle | Custom state must clear on battle changes and unit pointer reuse. | Runtime registry has bounds and tracking; DCL state lifecycle needs explicit schema. |
| AI scoring view | Enemies should evaluate rewritten damage, hit, status, and healing rather than placeholder values. | **Hypothesis (favored)**: the AI evaluates the SAME real formulas as the forecast (PSX-era evidence: AI classifies computed per-target results, honors elemental absorb/undead inversion; formula hacks steer the AI for free). Implication: INPUT-side writes (evade bytes, stats) are already AI-visible; display-paint and apply-time pre-clamp rewrites are NOT (AI blind spot) — full parity needs a hook firing during enemy think time. Candidate anchor: the high-frequency "roll-verdict burst" site (~8-11 fires/unit/CT tick). See `work/dcl-ai-and-progression-notes.md`. |

## 8. Minimum Complete Information Set

The DCL formula runtime is complete only when these information groups are owned by canonical docs
and exposed to formula settings or runtime code:

1. **Action context**: caster, target or tile, action id, action kind, hit index, target batch, and
   pending/immediate/reaction source.
2. **Action metadata**: damage type, element, spell/heal power, status category, targeting shape,
   native side effects, hit/avoidance policy, MP/CT data.
3. **Equipment metadata**: weapon family, weapon modifier, reach, damage type, parry, block, armor
   class, DR, Weight, elemental affinities, status immunities, special behavior.
4. **Unit state**: stats, traits, job/progression inputs, statuses, faction, CT/turn ownership,
   position, facing, height/tile occupancy.
5. **Outcome control**: forecast hit/damage/heal, runtime damage/heal/MP, physical outcome,
   magic avoidance, status application, reaction behavior.
6. **DCL state**: defense depletion, action batches, event seeds, pending action cache, battle
   lifecycle, and AI-facing scoring data.

The numeric formula layer is available before this full set is complete. The full DCL combat model
requires all six groups to be reliable.
