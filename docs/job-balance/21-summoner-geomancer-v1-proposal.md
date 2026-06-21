# Summoner And Geomancer V1 Proposal

Status: Accepted for provisional design
Version: V1
Date: 2026-06-21
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/10-healing-attrition-model-schema.md`
- `docs/job-balance/11-ct-delay-model-schema.md`
- `docs/job-balance/13-armor-response-model-schema.md`
- `docs/job-balance/16-healing-timing-composition-schema.md`
- `docs/job-balance/19-white-black-mage-v1-proposal.md`
- `docs/job-balance/20-time-mystic-v1-proposal.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Summoner and Geomancer.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, spell powers, MP
costs, hit rates, CT values, status rates, area sizes, terrain tables, damage multipliers, equipment
records, stat multipliers, or prerequisites.

Summoner and Geomancer are paired because both depend on the battlefield shape.

- Summoner uses delayed large-area effects, summon categories, MP, and target clusters.
- Geomancer uses terrain, hybrid stats, mail durability, and terrain-keyed damage/status.

Both jobs should make the player care about the map without making map dependence feel like a
lottery.

Claude review verdict: conditional acceptance on 2026-06-21, with required edits applied in this
version.

## Group Thesis

Summoner and Geomancer should be the first map-shape jobs.

- Summoner is the high-commitment delayed area caster: large effects, huge MP/CT cost, visible
  payoff, and meaningful whiff risk.
- Geomancer is the hybrid terrain fighter: moderate pressure, terrain-flavored status, mail
  durability, and weapon-backed map adaptation.

They should not become generic "better Black Mage" and "fighter with free ranged magic."

## Shared Area And Terrain Notes

T11 is a hard dependency for concrete Summoner and Geomancer values.

T5 can model whether a delayed action resolves before a target moves, but it does not decide how many
units a large area catches. T8 can model target choice, but it does not decide terrain availability
or area geometry.

T11 should be authored as two independently dual-gated sub-models:

- T11A area geometry and multi-target output;
- T11B terrain availability and terrain-dependent access.

Summoner primarily consumes T11A. Geomancer primarily consumes T11B. Some skills may consume both,
but one side should not drag in the other unless the concrete skill actually needs it.

Area output must be target-count-normalized in the F4 coexistence and no-dominance metrics. The
effective output of a summon or area action is:

```text
per_target_output * expected_target_count
```

This is the area analogue of hit-count normalization for multi-hit engines. A summon cannot pass
coexistence only by having acceptable per-target damage if realistic clusters make its total output
dominate.

Concrete values therefore require:

- T11 for area shape, expected target count, cluster density, ally-safe/friendly-fire behavior, and
  terrain availability;
- T5 for delayed resolution and target movement before resolution;
- T9 for MP economy, high-cost spell availability, MP discounts, and repeated summon pacing;
- T3/T3xT5 for Moogle, Faerie, Lich, drain, healing, or defensive recovery;
- T6xPS for Golem, Carbuncle, or any defensive summon/status that stacks with mitigation;
- T4/T5/T8 for Geomancy status accuracy, terrain range/line/height, and targeting-sensitive status;
- Gate F4/F5 if summon damage, Geomancy hybrid formulas, MP economy, area hit count, or defensive
  summons drift magic/physical coexistence.

## Summoner

Job: Summoner
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: delayed area caster with elemental summons, Moogle/Faerie healing, Golem/Carbuncle
defense, Lich drain, and late premium summons such as Bahamut, Odin, Cyclops, and Zodiark.

Vanilla problems:

- summon spells can become "Black Magic but bigger" if area and MP/CT costs are not meaningful;
- long CT can make summons feel bad if targets walk away without readable setup;
- ally-safe large areas can erase positioning risk if too efficient;
- high MP costs need a real resource economy instead of being hand-waved;
- defensive summons can become invisible upkeep if they stack too efficiently with other mitigation.

Accepted high-level role: delayed area caster.

Primary role: `caster-offense`

Secondary tags: `AoE`, `MP`

Growth profile: `magical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `staff`, `rod`, `fists`.

Armor class as target: `cloth`.

Supported damage modes: `crush`, `magic`.

Formula v0.2 coupling:

- Summoner should beat Black Mage when clustered targets and timing justify the commitment;
- Black Mage should beat Summoner for faster, cheaper, smaller, or more flexible damage;
- Time Mage `Meteor` should compete with Summoner's delayed area role without replacing it;
- large area and ally-safe targeting are power, not cosmetic properties;
- MP cost is a real constraint and belongs to T9 before numeric acceptance.
- target-count-normalized output, not only per-target output, must stay inside coexistence and
  no-dominance expectations.

### Action Skillset Goals

Summon should create decisions around commitment.

The player should ask:

- is the enemy cluster worth the CT/MP investment?
- do I need elemental area damage, defensive setup, healing, or drain?
- can I protect the caster and hold the target zone until resolution?
- is a lower-cost spell enough, or do I need a premium summon?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Moogle` | small area recovery | Early summon healing for clustered allies. | Requires T3/T3xT5/T11; must not replace White Mage or Chemist. |
| `Faerie` | stronger area recovery | Mid support summon when grouped allies can wait for CT. | High MP/CT; must not erase delayed-heal risk. |
| `Shiva` | ice area damage | Elemental area damage through affinity and cluster setup. | CT/MP/T11; no universal best element. |
| `Ramuh` | lightning area damage | Elemental area damage through affinity and cluster setup. | CT/MP/T11; no universal best element. |
| `Ifrit` | fire area damage | Elemental area damage with Oil-style setup where relevant. | CT/MP/T11; no universal best element. |
| `Titan` | earth area damage | Grounded/terrain-flavored area damage. | CT/MP/T11; Float/terrain counters where feasible. |
| `Golem` | physical protection summon | Party-facing protection against incoming physical pressure. | Requires T6xPS/T11; cannot become mandatory defensive upkeep. |
| `Carbuncle` | reflect/routing summon | Group magic-routing setup with backfire risk. | Requires T8xSR/T11; not pure anti-magic immunity. |
| `Bahamut` | premium area burst | Summoner's on-role reliable clean area payoff for major clusters. | High CT/MP/JP; must not replace all elements. |
| `Odin` | premium decisive strike | High-cost area damage with distinct fantasy from Bahamut. | High CT/MP; no generic best capstone. |
| `Leviathan` | premium water area | Large elemental area damage if affinity/terrain supports it. | High CT/MP; target-profile dependent. |
| `Salamander` | premium fire area | Large fire pressure and Oil-style payoff where relevant. | High CT/MP; target-profile dependent. |
| `Sylph` | mixed support/offense | Lighter area pressure or spirit/wind option. | Must have a distinct niche or be consolidated. |
| `Lich` | drain/percentage pressure | Dangerous HP/drain summon for high-HP or undead-sensitive targets. | Requires T3/T9/undead rows; not normal burst. |
| `Cyclops` | late non-elemental area | High-cost area damage when elements are wrong. | Must not make elemental selection obsolete. |
| `Zodiark` | hidden ultimate summon | Extreme late/hidden reward if retained. | Not part of ordinary balance baseline; separate boss/late-proof rows. |

Summoner should preserve the recognizability of the summon list, but the final implementation may
consolidate or re-scope individual summons if area, MP, or element identity cannot keep every record
meaningful.

Bahamut and Time Mage `Meteor` must not collapse into the same premium non-elemental area button.
Default distinction: Bahamut is Summoner's on-role reliable clean area payoff; Meteor is Time Mage's
off-role capstone with a longer telegraph, worse prediction burden, or less reliable target shape.

`Lich` and any proportional HP summon sit outside the ordinary MA/Faith damage band, like Time
Mage's `Gravity`. Their balance belongs to percent cap, immunity, boss, undead, drain, and resource
rows rather than the normal F4 magic/physical coexistence ratio.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Summon Ward` | channeling defense | Narrow reaction that helps a Summoner survive while committing to slow spells. | Must not become broad caster immunity; T4/T6xPS/T2.1 if defensive. |

Summoner does not need a dominant reaction identity. Its active kit is already high ceiling.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Summon Focus` | summon specialization | Improve summon reliability, MP efficiency, or area discipline only for Summon. | Requires T9/T11; not a broad magic booster. |
| `Grand Invocation` | premium summon focus | Late support for players who want a true summoner build. | Must not compress MP discount, damage boost, and CT fix into one mandatory slot. |

Do not give Summoner a broad magic damage support in this proposal. Black Mage, Mystic, and global
support review already carry enough magic-support risk.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Ritual Step` | channel positioning | Help the Summoner set up a cast line or hold safe formation. | Must not erase cloth fragility or CT commitment. |

### JP Progression

JP posture:

- one low-tier elemental summon and Moogle should be reachable early after job unlock;
- elemental coverage should arrive gradually so target planning matters;
- Golem/Carbuncle and Faerie should require real support investment;
- Bahamut/Odin/Leviathan/Salamander/Cyclops should be expensive late commitments;
- Zodiark, if retained, is an exceptional hidden/ultimate reward, not a normal balance anchor.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Summoner should remain advanced enough that large delayed area magic feels earned, but not so late
that the job only exists as a grind trophy.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Summoner donor patterns:

- Time Mage helps Summoner hold targets or improve timing windows;
- White Mage or Mystic borrows a small summon package for area support at high MP cost;
- Summoner borrows Black Magicks for faster, smaller damage when area commitment is wrong;
- MP-focused builds use Summon Focus only when they want the area-caster identity.

Unhealthy Summoner donor patterns:

- Summon becomes the best secondary for all casters;
- one premium summon replaces the whole element list;
- ally-safe area targeting removes positioning tradeoffs;
- Golem/Carbuncle become mandatory prebuff upkeep;
- MP supports make high-cost summons routine every fight.

### Expected Strong Builds

- active Summoner with Time Magicks support for setup;
- active Summoner with White Magicks or Mystic tools for defensive utility;
- high-MA/high-MP caster committing to area damage through Summon;
- party built around clustering enemies and protecting a slow caster.

### Expected Weaknesses

- cloth durability;
- high MP attrition;
- CT delay and target movement;
- low flexibility when enemies spread out;
- Silence, Shell, Reflect/routing risk, Faith manipulation, and elemental resistance;
- overkill and wasted area when clusters are poor.

### Expected Counters

- fast rushdown before the spell resolves;
- spread formations;
- Silence or MP denial;
- Shell/Reflect and elemental resistance;
- forced movement breaking target clusters;
- enemies that punish fragile backline channeling.

### Ramza / Unique-Job Interaction

Ramza may become a knight/mage hybrid, but he should not become the best Summoner. If Ramza gains
large area magic, it should trade against his physical, leadership, or support options.

## Geomancer

Job: Geomancer
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: hybrid physical job with terrain-dependent Geomancy, weapon access, mail-like
durability, and Nature's Wrath-style terrain retaliation.

Vanilla problems:

- Geomancy can feel random or map-dependent in a bad way if the terrain table is opaque;
- terrain damage/status can become free ranged pressure if it has no cost or counterplay;
- hybrid PA/MA identity can collapse into generic melee if Geomancy is weak;
- Attack Boost-style global pieces can make the job a support tax instead of an active hybrid;
- terrain movement can become universal if it erases map traversal too cheaply.

Accepted high-level role: PA/MA terrain hybrid.

Primary role: `hybrid`

Secondary tags: `terrain`, `mail`

Growth profile: `hybrid`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `axe`, `sword`, `fists`.

Armor class as target: `mail`.

Supported damage modes: `crush`, `swing`.

Formula v0.2 coupling:

- Geomancer is the roster's main mail-armored hybrid target;
- sword/axe/fists give swing/crush access, but active identity should come from terrain;
- axe/flail-style volatility and crush routes must not replace Monk's protected unarmed lane;
- Geomancy should default to the already validated `pampa_wp` hybrid routine where a WP-like terrain
  power can be represented;
- inventing a new terrain-keyed routine would be a formula-layer change requiring routine-table
  updates in both simulators plus formula dual-sim reapproval, not just job-level Gate F5;
- terrain status pressure must be useful without becoming universal ranged control.

### Action Skillset Goals

Geomancy should make terrain matter without making the player memorize hidden tables.

The player should ask:

- what terrain am I standing on or targeting through?
- is this a damage, status, or control opportunity?
- should I fight with weapon pressure or use terrain pressure?
- is this map good enough for active Geomancer, or should I bring another plan?

### Proposed Action Skills

Geomancy can preserve the vanilla skill names as terrain expressions rather than as a flat list of
nearly identical attacks.

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Sinkhole` | ground pressure | Terrain-based damage plus positional or movement pressure. | Requires T11 terrain availability; not universal ranged damage. |
| `Torrent` | water pressure | Water terrain damage/status, useful on maps where water matters. | Dead-map risk must be bounded by fallback tools. |
| `Tanglevine` | plant/root control | Soft movement or action pressure through vegetation terrain. | Status accuracy/immunity and terrain limits. |
| `Contortion` | rough terrain disruption | Terrain-specific disruption or accuracy pressure. | Must have visible map logic. |
| `Tremor` | earth impact | Grounded crush/impact pressure, especially into plate when terrain supports it. | Must not eclipse Monk anti-plate. |
| `Wind Slash` | wind line pressure | Terrain-flavored ranged chip or line pressure. | Must not replace Archer missile identity. |
| `Will-o'-the-Wisp` | spirit/fire pressure | Terrain/status pressure with magic-adjacent flavor. | Must not replace Black Mage elements. |
| `Quicksand` | movement trap | Slow, immobilize, or terrain lock pressure where terrain supports it. | T4/T5/T11 required; no hard-lock loop. |
| `Sandstorm` | vision/disruption | Blind, evasion, or accuracy-style pressure in sandy/dusty terrain. | T4 required; not broad defense. |
| `Snowstorm` | cold terrain pressure | Ice/slow-style terrain status where terrain supports it. | Element/status counters required. |
| `Wind Blast` | displacement/wind pressure | Wind terrain pressure for spacing or chip. | No universal shove/ranged dominance. |
| `Magma Surge` | fire/earth pressure | High-risk terrain pressure on lava/fire maps. | Map-limited; should feel powerful only where terrain earns it. |

Final Geomancy may consolidate, rotate, or map these records differently if terrain availability
cannot make all twelve actions meaningful. The important identity is readable terrain interaction,
not preserving twelve indistinguishable buttons.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Nature's Wrath` | terrain retaliation | Retaliate or punish attacks through local terrain. | Terrain-dependent, not broad counter damage; T11/T2.1 required. |

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Terrain Lore` | Geomancy specialization | Improve terrain action reliability or let Geomancer read/use more terrain. | Should help Geomancy, not all magic or all weapon damage. |
| `Attack Boost` | physical damage candidate | Existing protected stress engine; possible ownership if progression needs it. | F3/T2.1/F5 risk; not Geomancer's only reason to exist. |

`Attack Boost` is not accepted here as a concrete Geomancer reward. It is a protected v0.2 stress
engine and must be placed by the later global support/progression pass.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Ignore Terrain` | terrain specialist movement | Let a committed Geomancer exploit bad ground and map texture. | Must not become default movement for all jobs. |
| `Ignore Weather` | terrain/weather utility | Preserve niche terrain/weather identity if implementation supports it. | Narrow enough not to become a generic movement pick. |

Terrain movement should be attractive on specific maps and builds, not a universal answer to map
design.

### JP Progression

JP posture:

- baseline Geomancy should work early after job unlock;
- Terrain Lore and Nature's Wrath should require commitment;
- terrain movement should be mid or late enough to preserve Squire/Time/other movement choices;
- Attack Boost placement is deferred to global support progression;
- Geomancer should be viable as an active hybrid before learning any global support.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Geomancer should sit in the midgame as a real hybrid branch, not a late reward and not a pure support
detour.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Geomancer donor patterns:

- physical unit borrows Geomancy for map-specific utility at secondary-slot cost;
- hybrid unit uses Terrain Lore to specialize in map-aware play;
- Geomancer borrows Squire, Knight, Monk, or Mystic tools while keeping terrain as primary identity;
- mail hybrid uses terrain movement on maps where it clearly matters.

Unhealthy Geomancer donor patterns:

- Geomancy is the best secondary because it always has some ranged damage/status;
- Attack Boost makes Geomancer a mandatory support detour;
- Ignore Terrain becomes the correct movement for most builds;
- terrain pressure makes Archer, Monk, Black Mage, or Mystic unnecessary;
- Geomancer is useless on too many maps because terrain access is too narrow.

### Expected Strong Builds

- active Geomancer on terrain-rich maps;
- hybrid Geomancer with Squire/Knight/Monk secondary for weapon and terrain pressure;
- map-aware party using terrain movement and terrain statuses;
- mail hybrid that survives longer than cloth casters while applying moderate control.

### Expected Weaknesses

- map dependence;
- lower ceiling than dedicated physical or magical specialists;
- mail vulnerability profile;
- status immunity and terrain mismatch;
- less reliable range than Archer and less raw magic than Black Mage/Summoner.

### Expected Counters

- maps with poor terrain for the chosen Geomancy profile;
- flying/float/terrain-immune targets where relevant;
- status-resistant enemies;
- strong ranged or magical pressure against mail;
- enemies that can reposition away from terrain traps.

### Ramza / Unique-Job Interaction

Ramza may become a knight/mage hybrid, but he should not become the best terrain controller. If Ramza
gains terrain or area tools, they should trade against his leadership, physical, or magic options.

## Shared Scenario/Check Plan

Required later:

- `J-SU-EARLY-SELF`: Summoner with one low-tier damage summon and Moogle online.
- `J-SU-MID-AREA`: Summoner beats Black Mage only when cluster/timing/MP justify area commitment.
- `J-SU-LATE-PREMIUM`: premium summons are valuable but do not erase elemental choice.
- `J-SU-STRESS-SPREAD`: Summoner loses value against spread enemies.
- `J-SU-STRESS-MP`: repeated summons are bounded by T9 resource economy.
- `J-SU-STRESS-DEFENSE`: Golem/Carbuncle do not become mandatory prebuffs or pure immunity.
- `J-SU-STRESS-LICH`: Lich/drain/undead behavior is bounded and readable.
- `J-GE-EARLY-SELF`: Geomancer has a useful terrain action and weapon fallback.
- `J-GE-MID-TERRAIN`: Geomancy creates map-specific value without hidden-table frustration.
- `J-GE-LATE-HYBRID`: active Geomancer remains a real mail hybrid, not only an Attack Boost donor.
- `J-GE-STRESS-DEAD-MAP`: Geomancer's sword/axe/fists fallback on hybrid stats is a playable floor
  on low-terrain or bad-terrain maps, not merely a theoretical fallback.
- `J-GE-STRESS-UNIVERSAL-MAP`: Geomancy is not always good regardless of terrain.
- `J-GE-STRESS-MA-PA`: Geomancy hybrid routine does not eclipse Monk, Black Mage, or Archer lanes.
- `M-SECONDARY-COUNT`: Summon and Geomancy secondary incidence.
- `M-RSM-COUNT-LATE`: Summon Focus, Grand Invocation, Nature's Wrath, Terrain Lore, Attack Boost,
  Ignore Terrain, Ignore Weather, and Ritual Step.
- `I-AREA`: T11A area shape, target count, cluster, ally-safe/friendly-fire, target-count-normalized
  output, and overkill rows.
- `I-TERRAIN`: T11B terrain availability, terrain mismatch, terrain movement, and map archetype
  rows.
- `I-RESOURCE`: high-cost summon MP pacing, Summon Focus, and repeated summon availability.

Conditional:

- T11A for all area, multi-target, ally-safe/friendly-fire, expected target count, and
  target-count-normalized output behavior.
- T11B for all terrain availability, terrain-dependent access, dead-map, universal-map, and map
  archetype behavior.
- T5 for summon CT, delayed resolution, target movement before resolution, and Meteor comparisons.
- T9 for summon MP costs, Summon Focus, Grand Invocation, Lich resource behavior, and repeated
  summon pacing.
- T3/T3xT5 for Moogle, Faerie, Lich drain/healing, and any area recovery timing.
- T6xPS for Golem, Carbuncle, defensive summon stacks, and any terrain defense effect.
- T8xSR for Carbuncle/Reflect routing or any summon that redirects spells.
- T4/T5/T8 for Geomancy status accuracy, line/height, movement locks, blind/sleep/slow-style
  effects, and targeting-sensitive status.
- Gate F4/F5 if summon damage, target-count-normalized area output, Geomancy formulas, MP economy,
  defensive summons, or Attack Boost placement drift magic/physical coexistence or physical damage
  stress.

## Formula Re-Sim Requirement

No immediate Gate F5 re-sim is required for this document.

The concrete data pass must re-evaluate this if it:

- changes MA, PA, MP, Speed, or hybrid stat multipliers;
- changes summon spell power, CT, MP cost, area, targeting, or element behavior;
- changes Geomancy formula, terrain mapping, status rate, or damage type;
- changes Geomancy away from the already validated `pampa_wp` routine;
- changes axe/sword/fists access or mail armor classification;
- changes Golem, Carbuncle, defensive status, or Reflect routing behavior;
- changes MP economy, Summon Focus, Grand Invocation, or Lich resource behavior;
- changes Attack Boost ownership, progression, or effect;
- changes movement/terrain traversal enough to affect map exposure.

## Implementation Assumptions

- Data modding can create or repurpose ability records.
- Skill names are placeholders where they are not vanilla names.
- Preserve recognizable FFT flavor: named summons, terrain-based Geomancy, Golem, Carbuncle,
  Lich, Nature's Wrath, and terrain movement should remain recognizable unless proof shows a
  specific effect cannot be bounded.
- Exact values must be decided after validation, not inside this V1 proposal.

## Open Proof Needs

- Exact summon power, CT, MP, area, target filter, and element values after T11/T9.
- Exact target-count-normalized Summoner output after T11A.
- Whether ally-safe summoning can remain without making area too efficient.
- Exact Golem/Carbuncle mitigation and routing after T6xPS/T8xSR.
- Exact Lich drain/undead/resource behavior.
- Whether Zodiark is retained as a normal spell, hidden reward, or excluded from normal balance.
- Exact Geomancy terrain mapping after T11B, with `pampa_wp` as the default hybrid routine.
- Whether all twelve Geomancy records can remain distinct without hidden-table frustration.
- Whether Attack Boost belongs to Geomancer, another job, or a global progression decision.
- Whether Geomancer terrain movement creates too much universal traversal value.

## Claude Review Notes

Claude conditionally accepted this proposal on 2026-06-21 after requiring one hard rule:
area output must feed coexistence and no-dominance through expected target count, not per-target
damage alone.

Recommended review additions also applied:

- T11 split into independently gated T11A area-geometry and T11B terrain-availability sub-models;
- Geomancy defaults to the already validated `pampa_wp` hybrid routine;
- Bahamut and Meteor are differentiated by role and reliability;
- Lich/proportional damage is labeled outside the ordinary MA/Faith damage band;
- dead-map Geomancer must prove its weapon fallback is actually playable.
