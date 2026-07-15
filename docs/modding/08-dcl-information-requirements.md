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

**2026-07-04 construction status — LT6 ✅ + LT7 ✅ PASSED LIVE (same day):** the first end-to-end
DCL delivery is PROVEN in-game — including the first real multi-step damage model (LT7: GURPS-shaped
weapon model via `DclDerivedVariables`, 4 damage types × armor DR, spells untouched, ability
resolution proven live with Fire id 16): `DclPipelineEnabled` joins the calc-entry action-context
probe
(`0x3099AC` → per-target cache of caster/actionType/abilityId) with the pre-clamp managed callback
(`0x30A5D7`) that snapshots attacker+target, builds the full formula context (units + equipment via
`ItemCatalog` + ability metadata via `AbilityCatalog`) and rewrites the staged debit with the
config-authored `DclDamageFormula`, same-hit. This closes the "action context + both-sides context
at the damage write" plumbing for groups 1/2/3/4 of §8; outcome forcing (hit%/own-RNG) and the
authored damage-model metadata remain open. LT6 live evidence (6 scenarios, 5 attackers, dual
wield): every UI HP drop equaled the `[DCL]` `debit`, all different from vanilla `oldDebit`, zero
`[DCL-ERR]`. Two facts learned live: the forecast panel still shows the vanilla number (preview
paint not yet wired to DCL), and one `[DCL-MISS] no-calc-entry` on a hit against the attacking
Ninja shows **reaction/counter attacks do not pass through calc-entry `0x3099AC`** — they fall
through to vanilla damage safely. LT7 added: the pre-clamp refires (idempotently) during a charged
spell's evaluation loop, and charged spells log `[DCL-MISMATCH]` where the frame-side caster
pointer disagrees with the cache — the cache side is the correct one. LT8 (same day) proved the
hit-control delivery (authored 50%, own RNG, forced outcomes 13/14 swings 1:1 with the log) and
exposed the frontal-arc limit of the class-evade miss lever plus the crit ×1.2 staged multiplier,
the Mana-Shield zero-debit guard requirement, and monster basic-attack action types
(`0xB0`/`0xB3`/`0xB9`). LT9 (same day) proved the output-control miss damage-side from any angle
including monsters (rear/side forced misses all rendered 0 damage), calibrated the facing enum
(`+0x51`: 0=−y 1=−x 2=+y 3=+x), refuted `0x205B38` as a per-execution commit (it fires only for
real evade outcomes — Miss *presentation* still open), exposed the too-strict CT>100 unit-reader
guard (CT 108 legal; fixed) and the Mana-Shield MP leak on forced miss. See
`06-code-mod-runtime-dsl.md` §"DCL hit control" / §"DCL miss output-control" and the
`work/battle-runtime-settings.lt6..lt9` profiles.

## 1. Action Identity and Lifecycle

DCL formulas are action-dependent. The runtime must know not only who lost HP, but what action is
being resolved, who owns it, and which phase of the action lifecycle is active.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Acting unit / caster pointer | Every formula needs attacker/caster stats, equipment, traits, and zodiac. | **Strong** through native-frame actor, pre-clamp, pending, and selector context. CT is not accepted as a DCL source. See `04` and `06`. |
| True action id / ability id | Damage type, spell power, status category, target mode, element, and special behavior depend on the exact action. | **Proven** for the universal order-record path at `computeActionResult 0x3099AC`: `rcx = caster+0x1A0`, byte `0` is caster slot, byte `1` is type, word `2` is ability id, and `dl` is target index. **Strong caveat:** formula `0x25` re-enters this function with a temporary synthetic Attack record while resolving the Rend family, so caller provenance must preserve the outer identity. See `04` §6.5. |
| Action family | Physical, magical, healing, status, reaction, item, weapon special, and monster actions use different DCL branches. | **Strong** only where data/catalog classification or action-signal rules can infer it. |
| Resolution phase | Forecast, confirmation, charge wait, execution, hit roll, HP/MP apply, status apply, and reaction windows have different writable surfaces. | **Strong** for forecast display and HP/MP pre-clamp. Roll/status/reaction phases have **Strong** static anchors: magic-accuracy roll `0x304E33`, status-proc roll `0x306636`, reaction Brave-gate functions `0x30BDBC/0x30BE14/0x30BE64/0x30BEB0` with roll calls at `0x30BDEE/0x30BE44/0x30BE9A/0x30BEDA`, and post-hit reaction author block `0x30AA85..0x30AB28`. See `04` and `06`. |
| Target model | Actions may target a unit, a tile epicenter, a line, self, allies, enemies, or an area. AoE targets can differ between forecast and resolution. | **Strong** for final impacted HP targets. Pre-resolution: the pending record carries target coordinates at `+0x1AC/+0x1AE/+0x1B0`; the forecast target ptr lives at `qword[0x14186AF68]`; targeting-shape filters exist in data (`AIBehaviorFlags`). Weapon LoS has native Arc/Direct/Lunging resolvers, and Arc/Direct authorize a candidate only when the resolver's reached/intercepted unit index equals the intended index. See `04` §5.4. |
| Multi-hit identity | Dual-wield, multi-strike skills, and AoE batches need per-hit/per-target context and stable batching. | **Proven** for weapon identity: action type `1` carries the exact selected weapon item id at `orderRecord+8`, and the DCL includes it in action/hit cache identity. **Strong** for HP-event separation: AoE resolves as one sequential pre-clamp event per victim; dual-wield produces two observed apply events but native per-strike cadence remains a live gate. The engine exposes no universal hit ordinal, so other batch state remains DCL-owned. |
| Reaction and counter source | Counter, Hamedo/First Strike, Blade Grasp, parry/block, and similar reactions can invert or interrupt the apparent attacker/target relation. | **Strong/partial**. HP-applying reaction damage resolves through normal actor context; First-Strike cancelled incoming source is visible at target-cache time. The unit's reaction set is bitfield `+0x94..0x97`; the current dispatcher begins at `0x30B4EC` and stages reaction ids `0x1A6..0x1C5`. Reaction staging writes the exact evaluated reaction id to `orderRecord+2` and `word[0x14186AFF0]`. That global identifies the reaction, not the incoming action; stable incoming source/action attribution at every reaction gate remains open. |
| Native side-effect bundle | DCL must know whether the native action also carries status, knockback, elemental behavior, drain, MP damage, or special flags. | The staged HP/MP words `+0x1C4..+0x1CA`, outcome kind `+0x1C0`, and result flags `+0x1E5` expose part of the bundle. Order-record `+8` (unit `+0x1A8` while the order is staged) is an action-dependent payload: for type `1` it is the selected weapon item id, and item/inventory paths may consume it under `+0x1D0 & 0x08`. It is not status authority. Status state and immunity live in the four five-byte arrays documented in `04` §2.3. |

## 2. Unit and Battle State

DCL formulas require a stable live unit model plus enough transient battle state to interpret the
current action.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Unit pointer and identity | Formula context, HP/MP application, equipment reads, and target matching are pointer-based. | **Proven**. See `04`. |
| Core stats | PA, MA, Speed, Brave, Faith, level, HP, MP, CT, Move, Jump, job, zodiac, and gender flags feed formulas or classification. | **Proven** for the main unit struct fields. See `04` and `06`. |
| Raw vs effective stats | DCL may use raw stats for traits and effective stats for equipment-modified output. | **Proven** for PA/MA/Speed raw and effective fields. |
| Job and job-level data | Weapon skill progression and job-family grades depend on job identity and progression. | **Proven (LT1 2026-07-02)**: per-job JP arrays live-confirmed — Agrias's Fire granted +48 at index 6 = `jobId 0x50 − 0x4A`, Beowulf +53 at index 8 = `0x52 − 0x4A`, spillover +9/10 to allies at the same index; `JP1 (+0xF0)` = spendable, `JP2 (+0x11E)` = total → job level = `JP2[j]` vs Nex `GeneralJob.RequiredJobExp[8]` thresholds. `+0x28` increments with actions (EXP candidate). Job-level nibbles `+0xE4..0xEE` remain Hypothesis. See `work/lt1-mega-probe-plan.md`. |
| Active turn owner | Defense depletion resets on a defender's own turn, and wait/action behavior affects timing without always implying an attack. | **Proven (LT1 2026-07-02)**: `unit+0x1B8 == 1` tracked every player and AI turn live, exactly-one invariant held throughout; `+0x1BA` confirmed as in-flight action owner (set at confirm); byte `+0x2E` also flips at turn grant (companion marker). See `work/lt1-mega-probe-plan.md`. |
| Pending action state | Charged spells, Limits, delayed effects, and queued actions require caster/action state between confirmation and resolution. | **Strong** for observed charged damage/healing; full pending record layout is not owned. |
| Active source marker | Immediate actions need a source signal that does not rely on CT timing. | **Strong/Hypothesis** depending on action type. See actor context in `04`. |
| Status flags | Shell, Protect, Haste, Slow, Don't Act, Don't Move, Undead, Petrify, charging, defending, and similar states affect formulas or legality. | **Proven (LT1/LT2 2026-07-02)**: the four 5-byte arrays are real — Blind landed as `eff[1]\|=0x20 [+Darkness]` + master (classic bit exact); Charging `0x08` tracked the Fire charge; equipment immunities appear in `+0x5C..0x60` (Ramza Darkness+Sleep, Ninja DM+DA); **writing an immunity bit is a live-proven INPUT-CONTROL lever** (poked Darkness onto a clean unit → forecast 0% → miss). See `work/lt1-mega-probe-plan.md`. |
| Position and facing | Front/side/back, reach, line of attack, and terrain rules require X/Y/facing. | **Proven (LT1 2026-07-02)**: every move/facing change tracked live at `+0x4F/+0x50/+0x51`, coherent with the tile under the unit. See `work/lt1-mega-probe-plan.md`. |
| Height and tile occupancy | Vertical tolerance, AoE membership, reach, and some skills depend on map tiles. | **Proven (LT1 2026-07-02)**: tile table at VA `0x140D8DCB0` read live — dims `13x13` correct, heights/terrain/slope plausible under every unit, and the `+5` mark byte showed **move-range highlight = bit `0x20`, cursor/target = `0x40/0xC0`** → range/AoE membership is a direct memory read. Layout: 8 bytes/tile, 256/level, 2 levels; index `(level<<8) + y*width + x`, dims at `0x140C6AD6A/6B`; `+2` height, `+3` slope/depth, `+4` corner weights, `+6` flags; level bit = unit `+0x51` bit 7 (bridge case still untested). See `work/dcl-tilemap-candidates.md`. |
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
values, and synthetic rows for monster/fist "weapons". `ItemCatalogEntry` exposes weapon range,
attack flags, elements, options ability, status/affinity families, and physical/magical evasion to
the formula context. DCL-only Weight, armor class/DR, and weapon modifiers remain authored maps.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Equipment slots | Every unit formula needs current weapon, shield, armor, accessory, and off-hand data. | **Proven**. Slot offsets are owned by `04`. |
| Item id to catalog row | Live slot ids must resolve to deterministic metadata. | **Strong** through `work/item_catalog.csv` and `ItemCatalog`. |
| Weapon family | Sword, knife, spear, bow, crossbow, gun, staff, rod, fist, monster, and special families drive skill and damage mode. | Partially represented through catalog category fields. |
| Damage type | Cut, thrust, crush, missile, magic, status, and special categories drive DR and wound multipliers. | Needs explicit DCL catalog mapping. |
| Weapon modifier | DCL weapon damage adds a family/item modifier to thrust/swing/base damage. | Needs explicit DCL catalog mapping. |
| Reach and range | Reach-1, reach-2, ranged weapons, point-blank weakness, and outrange rules require range metadata. | Some data exists in item/ability tables; runtime exposure is incomplete. |
| Parry value | Weapon parry is an active defense and can deplete. | Live byte is **Proven**; DCL depletion state is not an item-catalog fact. |
| Shield block value | Shields become finite Block resources. | **Updated (RE 2026-07-03)**: shield evade has **NO live single-byte lever** — unlike class `+0x4B` (copied 1:1 to the forecast record and honored live), the shield field is a *derived* record value `MAX([unit+0x4A],[unit+0x49])` packed at builders `0x284BC0/0x3600DC/0x3962F0` before the roll, so a live `+0x4A` write is ignored (LT5-A2 FAIL). Neutralize shield in **DATA** (zero the shield item's evade columns in `item_catalog.csv`) or hook the record MAX. See `work/dcl-shield-evade-read-path.md`. |
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
The runtime accepts those authored fields through the opt-in, approval-gated
`DclAbilityMetadataPath` overlay defined in `06`. The vanilla baseline remains the source of native
facts; DCL action kind, spell/heal power, damage type, status category, hit/avoidance policy, and
side-effect policy belong to the separate overlay.

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
| Hit/avoidance policy | Some actions should use physical defense, magic evade, no defense, guaranteed hit, or special checks. | Needs DCL metadata (per-action policy). **Runtime authority ✅ PROVEN LT5-A4 (2026-07-03)** — `work/dcl-miss-block-parry-DEFINITIVE-2026-07-03.md`: equipment evade killed at the SOURCE via `ItemTableEvadeZero` (loaded item stat tables at fixed VAs — weapon `0x80F690`+5, shield `0x80FA90`, accessory `0x80FB30`; live: 50%-shield/parry targets → preview 100%, 12/12 hits); class evade via `+0x4B` live write; reactions via Brave `+0x2B` (Shirahadori roll is VM-internal, hook REFUTED). The mod rolls its own hit% and forces the binary outcome. See `05-reverse-engineering.md §Item-table evade kill`. |
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
| Forecast hit percent | DCL hit/defense math must surface before confirmation. | **Proven, including formula integration:** `DclPreviewHitPctEnabled` mirrors the percentage from the same cached DCL decision used for execution into a per-target native buffer, then maps `forecast object = target+0x1BE` to the correct slot at the UI copy hook. A 50% authored preview and deterministic roll 90 produced the matching miss, zero damage, and no Counter. |
| Runtime HP damage | DCL damage must land through native HP/KO lifecycle. | **Proven:** the permanent numeric writer publishes the normalized HP/MP result at the post-calculation boundary `0x281F12`. AI evaluations are transient; confirmed execution is keyed by caster, target, action type, ability, and payload, then the exact cached result is consumed at the apply pre-clamp `0x30A5D7`. This preserves native HP/KO lifecycle and prevents a second formula evaluation from rewritten staged inputs. Native non-apply outcomes remain authoritative unless DCL hit/output control owns them. |
| Runtime healing | DCL healing must land through native credit path and explicit heal context. | **Proven** for direct and delayed explicit heals. |
| Runtime MP | DCL can alter MP costs, MP damage, MP restoration, and Mana Shield redirection. | **Proven/Strong** through staged MP fields and settings. Native Mana Shield transfers the whole HP debit to MP whenever current MP is nonzero, so the final DCL needs an authored ratio/floor policy plus coherent result-flag presentation when it replaces that redirect. See `04` §6.5 and `06`. |
| Physical miss/block/parry | DCL defense rolls must suppress or transform physical hits. | **Proven/implemented:** one per-target physical contest computes Attack Skill versus Dodge and one finite Parry/Block roll, caches the decision, suppresses damage/reactions on failure, and presents the native miss/block/parry outcome kind. Guard use commits once through the selector/pre-clamp handshake and resets on the defender's own-turn edge. Remaining evidence is live calibration and presentation coverage for each defense kind. See `05` and `06`. |
| Magic evade | DCL magic avoidance is separate from physical parry/block. | **Implemented offline:** an explicit applicability formula and equipment-derived Magic Evade formula feed the same cached DCL hit decision, respect an anti-immunity cap, paint forecast hit%, and deliver an authored miss through the clean output path after native equipment evade is neutralized. The pure multistrike resolver owns the DCL default of one spell-level roll per final target and also supports an explicit per-strike exception. **Strong:** `RandomFire` selects and calculates one target per native repeat; the runtime retains the spell-level decision across repeats and reuses it if the same target is selected again. Live validation remains for apply/presentation ordering. |
| Status infliction | DCL status contests must replace native status odds. | **Implemented offline / live-gated:** per-action rules own 3d6 resistance, immunity, the native five-byte add/remove packet, and target-turn duration. Ordinary riders use data suppression or retained carriers. The 82 conditional actions, two performance actions, and two RandomFire Void actions use a catalog-verified post-calc producer gated by outer-sweep provenance plus battle state `0x2A`; one cached decision is reused at pre-clamp, nested Rend cannot replace the outer identity, and Song/Dance preserves the native caster-Sleep gate. RandomFire produces a fresh exact random-one packet per native repeat. Performance cadence/cleanup and live RandomFire integration ordering remain gates. Native forecast/AI probability remains read-only until authored status-percentage presentation/scoring is integrated. The retired `+0x1A8/+0x1D0` path is unrelated to status. |
| Reaction triggering | DCL reactions depend on Brave/courage/caution and action context. | **Proven/Strong:** the four real-code Brave-roll call sites expose the exact evaluated reaction id and accept an authored chance; a cached DCL miss suppresses their chance before the native roll. VM-owned avoidance such as Shirahadori receives the exact id from `orderRecord+2` and can consume a scoped virtual Brave value that is restored at the sole calculation exit. Reaction set bitfield `unit+0x94..0x97` and dispatcher `0x30B4EC` are Strong static. Pass-2 commit cardinality and state-`0x2C` per-strike delivery are Proven live. The accepted post-selector boundary `0x2063BD` is Proven live to expose Counter's complete type-`1`/payload-`0` source-targeted order before actor construction; its exact-byte-guarded probe also proves that selected unit-table and later actor indices are separate namespaces. A fail-closed order replacement/source-retarget controller owns that boundary. Hex Ward's offline-tested composed controller closes the native blank-443 gap with an exact-owner successful-result reservation, known-hit Caution roll, dynamic empty-slot producer, source retarget, pass-2 cadence commit, and immunity-aware Blind or floored current-Brave delivery; the composed vertical remains live-gated. Vigilance still needs a transient DCL Dodge reservation/commit, and the complete hybrid taxonomy needs live vertical slices. See `04` and `06`. |
| Native side-effect suppression | Placeholder/native effects must not apply before DCL decides the final result. | Damage neuter and durable status-output controls exist. Nature's Wrath proves the remaining provenance requirement: its native selector reuses ordinary Geomancy payload `126..137`, so reaction-only rider suppression must distinguish the reaction execution from an active cast of the same action id. Special side effects still need complete handling. |

## 7. DCL Runtime State

Some DCL mechanics are not pure reads from the game. They require code-mod state that remains
consistent with native turns, hits, and reactions.

| Requirement | Why the DCL needs it | Current canonical coverage |
| --- | --- | --- |
| Defense depletion | Parry and Block deplete per incoming hit and reset on defender turn. | **Implemented offline / live-gated:** `DclGuardPool` is keyed by unit pointer/character identity, refreshes on the proven own-turn active edge, exposes remaining resources to formulas, and spends a selected defense idempotently only at execution delivery. |
| Hit batch identity | Multi-hit and AoE actions must consume defenses and apply formulas per hit/target. | Per-target calc and hit-decision caches separate AoE victims; pending batches count delayed events. For weapon Attack, `orderRecord+8` provides exact active-weapon identity and is part of the cache key; different right/left ids identify the side, while identical ids are harmlessly side-ambiguous. Managed physical multistrikes own an aggregate strike loop and atomic Guard commit. **Strong:** `RandomFire` uses a native repeat count/index, selects exactly one target, and performs one ordinary calculation per repeat. Barrage delegates one equipped-weapon calculation to the normal-attack postprocessor. Native dual-wield and Barrage transaction cadence remain live gates; a universal native hit index does not exist, so final per-family batching remains explicit DCL policy. |
| Pending action cache | Charged actions need caster/action/target metadata between confirmation and resolution. | **Proven for universal numeric and instant-KO results:** final per-target calculation supplies exact caster, action type, ability, payload, and target at the outer-sweep compute point. A charged Death transaction preserves this identity from confirmation through AI evaluation and state-`0x2A` resolution, then delivers the cached result at pre-clamp. Numeric/status execution ownership therefore does not depend on guessing the most recent pending caster. The older pending tracker remains diagnostic/fallback state and still owns charge/timing observations; a complete pending schema is not required for result families that reach the universal calculation sweep. |
| Event seed | Random DCL rolls must be deterministic per action/hit for preview/execution parity where possible. | Event seeds are stable formula inputs and hit decisions are cached per target/action so forecast and execution reuse the authored roll. Nested Rend calculation provenance and universal multi-hit identity remain live gates. |
| Forecast cache | Preview formulas and execution formulas need shared context or equivalent recomputation. | Damage/heal and hit forecast controls exist; per-target hit decisions are shared with execution. Complete context parity still depends on LT28 outer/nested provenance and action-family metadata. |
| Battle reset/lifecycle | Custom state must clear on battle changes and unit pointer reuse. | Runtime registry has bounds and tracking; DCL state lifecycle needs explicit schema. |
| AI scoring view | Enemies should evaluate rewritten damage, hit, status, and healing rather than placeholder values. | **Proven/implemented for numeric results:** enemy think-time candidate sweeps traverse `0x281CE8` and the shared per-target `computeActionResult 0x3099AC`. The protected AI consumer ranks targets from the normalized staged bundle exposed after `0x281F12`. The permanent writer publishes the complete HP/MP bundle and result flags there, leaves ordinary AI evaluations transient, caches confirmed execution once, and reuses that exact result at `0x30A5D7`. Instant KO publishes exact 3d6 expected lethal debit during scoring and samples its contest only at confirmed execution. Authored status probability scoring and representative healing/MP/action-family regression remain separate coverage gates. |

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
