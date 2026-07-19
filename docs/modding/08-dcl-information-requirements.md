# DCL runtime information requirements

This document is the canonical inventory of information the code mod must own to implement the
Deep Combat Layer (DCL). It maps each requirement to its authoritative document and confidence; it
does not duplicate field maps, hook ABIs, rule schemas, design rules, or investigation history.

The ownership boundary is:

- `03-battle-data-map.md` — editable data tables, enums, and catalog sources;
- `04-engine-memory-model.md` — live fields, unit records, actor/action state, and lifecycle;
- `05-reverse-engineering.md` — hook anchors, native/virtualized boundaries, and control surfaces;
- `06-code-mod-runtime-dsl.md` — runtime settings, formula variables, rule schemas, and transactions;
- `docs/deep-combat-layer/` — combat design and policy.

Confidence labels are **Proven**, **Strong**, **Hypothesis**, and **Refuted**. “Implemented offline”
describes code coverage, not engine confidence. A mechanism remains live-gated wherever its native
ordering or presentation has not been observed end to end.

## 1. Action identity and lifecycle

| Requirement | Required information | Coverage and owner |
| --- | --- | --- |
| Acting unit | Stable caster/attacker identity for every formula and effect. | **Proven** for ordinary, delayed, native-repeat, and accepted Reaction transactions. `04` owns attribution. CT is **Refuted** as authority. |
| Action identity | Action type, ability id, payload, and outer/nested provenance. | **Proven** at calculation entry and outer-sweep result boundaries. Formula `0x25` nested Attack identity is explicitly separated from its outer owner. See `04` and `06`. |
| Target identity | Unit or tile target, final impacted units, and AoE iteration. | **Proven** for final result targets; **Strong** for all pre-resolution shapes. See `03`, `04`, and `06`. |
| Resolution phase | Forecast, AI evaluation, confirmation, charge, execution, apply, and cleanup. | **Proven** for numeric forecast/evaluation/execution separation. Status, Reaction, and movement families carry their own confidence in `04` and `06`. |
| Native repeat | Repeat count/index and active hand/item for each native strike. | **Proven** for weapon Attack and Reaction-delivered Dual Wield. Other repeat families require explicit policy. See `04`. |
| Managed multistrike | Explicit strike list, per-strike contest, damage, and atomic resource commit. | **Implemented offline**. See `06`. |
| Reaction source | Exact equipped owner, incoming source, native delivery carrier, commit, and delivered strike. | **Proven** for the supported synthetic Counter carrier; broader families retain individual confidence. See `04` and `06`. |
| Pending action | Owner, action identity, timer, Charging mirrors, and cancellation lifecycle. | **Proven** for the Interrupt transaction and later normal recovery. See `04` and `06`. |
| Native side effects | HP/MP channels, status packet, result kind/flags, inventory effects, and special consumers. | **Partial**. Each owned effect requires an explicit producer; `+0x1A8/+0x1D0` are not generic status authority. See `04` and `06`. |

## 2. Unit and battle state

| Requirement | Required information | Coverage and owner |
| --- | --- | --- |
| Unit registry | Pointer, table index, character identity, generation, and removal. | **Proven/implemented**. `04` owns the array; `06` owns generation-aware caches. |
| Core stats | HP, MP, PA, MA, Speed, Brave, Faith, CT, Move, Jump, level, job, zodiac, and gender flags. | **Proven** for the live fields used by formulas. See `04`. |
| Raw/effective stats | Base values versus equipment/status-modified values. | **Proven** for PA, MA, and Speed. See `04`. |
| Progression | Job id, spendable JP, total JP, job level, EXP, and family skill grade. | JP arrays and threshold derivation are **Proven**; remaining packed job-level candidates retain their confidence in `04`. Job-family grades remain authored DCL policy. |
| Turn owner | Exact own-turn edge for refreshes and durations. | **Proven** through active-turn state. See `04`; `06` owns resource refresh behavior. |
| Status state | Five-byte source, immunity, effective, and durable-master arrays. | **Proven**. Direct add/remove and immunity input control are **Proven**. Engine-owned lifecycle statuses require dedicated mechanisms. See `04` and `06`. |
| Position/facing | X, Y, level, facing, committed destination, and accepted route. | **Proven**. See `04`. |
| Movement settlement | Final committed tile, route completion, and post-movement state. | Position and accepted-route fields are **Proven**. DCL gameplay does not interrupt movement between tiles; position-triggered mechanics begin only after movement finishes. See `04` and `06`. |
| Tile map | Dimensions, level, height, slope/depth, markings, flags, and occupancy. | **Proven** for the mapped table and common mark bits; unusual multi-level cases retain inline confidence in `04`. |
| Allegiance | Team, faction, friend/foe, and targeting legality. | **Proven** for the runtime signals used by DCL sanity checks. See `04`. |

## 3. Equipment and item metadata

The DCL reinterprets existing items; it does not add new equipment SKUs.

| Requirement | Required information | Coverage and owner |
| --- | --- | --- |
| Equipped slots | Main hand, off hand, shield, head, body, and accessory ids. | **Proven**. `04` owns live offsets. |
| Catalog resolution | Stable item-id to row and runtime metadata mapping. | **Strong/implemented** through `ItemCatalog`; data source is owned by `03`. |
| Active weapon | Exact native hand, side, item, family, and repeat provenance. | **Proven** for supported weapon transactions. See `04` and `06`. |
| Weapon family/reach | Family, native range, reach policy, projectile/line behavior, and unarmed route. | Native fields are mapped; DCL classification remains explicit catalog policy. See `03` and `06`. |
| Damage type/modifier | Cut, thrust, crush, missile, magic, power modifier, and special route. | Must be authored in DCL metadata because stock tables do not expose the complete vocabulary. |
| Parry/block | Native item values plus finite DCL Guard capacity. | Native source tables are **Proven**; finite depletion is runtime state. Shield evade has no universal late unit-byte lever. See `05` and `06`. |
| Armor/DR | Armor class and type-specific subtractive DR. | Must be authored in DCL metadata. |
| Weight | Item Weight and its Move/Dodge policy. | Must be authored; no stock IVC item Weight field is mapped. |
| Elements/affinities | Element, absorb, nullify, halve, weak, strong, and boost. | Source fields exist; runtime exposure is partial. See `03` and `06`. |
| Status grants/immunities | Innate, initial, and immunity sets. | Source fields exist; live status authority is owned by `04`. |
| Special behavior | Proc, drain, thrown, gun, rod/staff, random-power, and monster/fist routes. | Partial. Every special family requires explicit catalog and runtime policy. |

## 4. Ability and effect metadata

| Requirement | Required information | Coverage and owner |
| --- | --- | --- |
| Ability identity | Stable id/name and ability category. | Cataloged. See `03`. |
| Native action row | Formula, X/Y, element, range, area, vertical, status, CT, MP, and flags. | The override table is sparse. The WotL baseline is a design aid; IVC overrides and observed behavior remain authoritative. See `03` and `05`. |
| DCL effect kind | Damage, heal, status, movement, Reaction, Interrupt, instant KO, MP, or special. | Must be explicitly authored in `AbilityCatalog`/runtime rules. See `06`. |
| Power/scaling | Spell power, heal power, weapon scaling, Faith/Brave policy, and damage-type mapping. | Must be explicitly authored. DSL support exists. |
| Hit policy | Physical contest, Magic Evade, guaranteed outcome, status resistance, or special check. | Runtime authority is implemented for physical and magic binary outcomes; per-action selection remains metadata. See `05` and `06`. |
| Status nature and duration | Per-source physical/base-HP, mental/Brave, magical/inverse-Faith, beneficial, lifecycle, or campaign policy plus a positive owned duration where applicable. | The DCL defines the principal category families and source-specific Disable/Immobilize split. Darkness, Silence, BloodSuck, Oil, and Undead have no selected harmful-status nature; Disease has no assigned native carrier or ongoing-effect mechanism. These are explicit design inputs, independent of job assignment. |
| Native rider policy | Absent, suppressed by data, retained carrier, or engine-owned. | Required and fail-closed for managed status rules. See `06`. |
| Targeting/AI policy | Ally/enemy/map filters, shape, scoring, and transient evaluation behavior. | Native flags and numeric AI writer are mapped. Authored status/heal/special scoring remains partial. See `03`, `04`, and `06`. |

## 5. Formula inputs and derived variables

| Requirement | Coverage and owner |
| --- | --- |
| Thrust/swing/base tables | Supported by formula tables/maps in `06`. |
| Armor DR and wound multipliers | Supported once item damage type and armor metadata are authored. |
| Penetration floor and clamps | Supported by the DSL; preview and execution must call the same policy. |
| Brave/Faith curves | Live values are **Proven**; authored curves live in settings. |
| Zodiac compatibility | Live identity is exposed; authored matrix lives in settings. |
| Weapon skill | Formula maps support it; job/family grade ownership remains required. |
| Deterministic contest rolls | Event/transaction identity and cached decisions provide preview/execution parity for owned families. |
| Native result context | HP/MP channels, active weapon, result kind/flags, pending state, and DCL state are exposed by the relevant runtime contexts. |

## 6. Outcome authority

| Requirement | Coverage and owner |
| --- | --- |
| Forecast hit percentage | **Proven/implemented** through the per-target cached percentage and native display-copy hook. See `05` and `06`. |
| Forecast HP amount/bar | **Proven/implemented** through the underlying staged forecast fields. See `05` and `06`. |
| Runtime HP/MP amount | **Proven/implemented** at the normalized compute point and exact pre-clamp consumption. See `04` and `06`. |
| Healing/revive/inversion | Direct and delayed explicit healing are **Proven**; native result-kind gates prevent cancelled results from being resurrected. See `06`. |
| Physical miss/parry/block | **Implemented** with one cached contest and finite Guard commit. Representative presentation calibration remains live-gated. |
| Magic Evade | **Implemented offline** with shared forecast/execution decision and native-repeat policy. Live apply/presentation ordering remains family-gated. |
| Status application | **Implemented offline / partially live-proven** through owned packets, immunity, resistance, and carrier policy. Exclusive duration ownership is implemented for the shared Disable/Immobilize flags; other timed statuses retain the native/DCL dual-clock boundary. Performance and random-repeat cadence retain explicit gates. |
| Reaction triggering | Exact native and VM-owned trigger routes are partially **Proven**; supported synthetic delivery is **Proven**. See `04` and `06`. |
| Final-tile triggers | DCL position-triggered abilities evaluate only after ordinary movement completes. Mid-route interruption is excluded from gameplay. |
| Charged-action Interrupt | Timer/Charging cancellation transaction is **Proven**. Production ability assignment is design metadata. |
| Native side-effect suppression | Numeric and durable status ownership exist. Every remaining special consumer requires explicit provenance and suppression policy. |

## 7. DCL-owned runtime state

| State | Required lifecycle | Coverage and owner |
| --- | --- | --- |
| Hit/result caches | Keyed by generation and full action/target identity; cleared on hard boundaries. | Implemented in `06`. |
| Guard pools | Refresh on defender own-turn; spend once at exact delivery. | Implemented offline; mechanism coverage in `06`. |
| Pending actions | Persist from confirmation to resolution/cancellation without “latest caster” guesses. | Proven for owned result/Interrupt families. |
| Status durations | Count target turns, clear only owned bits, and neutralize a native counter only with complete producer ownership. | Implemented. Direct status fields are **Proven**. The validated Disable/Immobilize pair owns all fourteen add producers; the remaining ordinary timed statuses require authored duration profiles before exclusive transfer. Doom and the system `Empty_32` row are excluded from the generic transaction. See `06`. |
| MP budget/trickle | Per-battle pool, own-turn edge, bounded writes, and reset. | Implemented offline; live policy coverage remains explicit. |
| Reaction cadence | Reserve, validate, commit, and deliver exactly once at the declared cadence. | Supported synthetic path is Proven. |
| Movement transaction | Exact final destination and movement-complete boundary without mid-route mutation. | Position and route ownership are mapped; gameplay effects must wait for route completion. |
| Battle lifecycle | Generation start/end, pointer reuse, unit removal, and settings reload. | Implemented offline with central reset. |
| AI evaluation | Publish authored transient numeric results without consuming execution state. | Numeric scoring path is Proven/implemented; other effect families are partial. |

## 8. Completion criterion

The DCL runtime is technically complete only when all six information groups are owned and exposed:

1. action context;
2. action/effect metadata;
3. equipment metadata;
4. unit and map state;
5. outcome authority;
6. generation-safe DCL runtime state.

An offline implementation does not upgrade a native boundary to Proven. Each requirement keeps its
own confidence until the corresponding engine behavior is validated and promoted into its owning
document.
