# Prerequisite Tree And JP Cost Draft V0

Status: Accepted by Claude review
Date: 2026-06-22
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/50-campaign-journey-bundle-v0.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/job-balance/59-equipment-availability-timing-v0.md`
- `docs/job-balance/61-jp-boost-removal-decision-v0.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `work/gpt-prerequisite-tree-jp-cost-v0.json`

## Purpose

This is the last W3 producer draft before W4/T2.1 incidence.

It gives a provisional job prerequisite tree, job-depth threshold model, and JP cost draft for the
reaction/support/movement and equipment-export pieces that drive cross-job build convergence. It
uses doc 51 for intended bands, doc 58 for R/S/M values and slot rules, and doc 59 for equipment
practical-online timing.

This document does not change equipment, prices, gil rewards, shop economy, formula constants,
weapon power, job multipliers, growth, action values, or the formula bundle. Action ability JP
values already proposed in concrete docs 42-57 remain owned by those docs; this draft focuses on
job unlock depth and exportable build pieces.

## Atlas Consultation

The vanilla atlas was consulted for progression vocabulary and ability families:

- `docs/reference/README.md`;
- `docs/reference/fft-vanilla-ability-effect-index.md`;
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- `docs/job-balance/12-vanilla-skill-status-reference.md`.

Vanilla overlap checked:

| Family | Records checked | This producer read |
| --- | --- | --- |
| Starter progression | Squire, Chemist, `Basic Training`, `Move +1`, basic Items | Starter routes stay early and useful without forcing a permanent combat support. |
| Equipment unlocks | `Equip Heavy Armor`, `Equip Shields`, `Equip Crossbows`, `Equip Katana`, `Equip Polearms`, `Equip Guns`, `Equip Swords`, `Reequip` | Existing unlock vocabulary retained where accepted; broad `Equip Sword` remains rejected. |
| Physical engines | `Brawler`, `Doublehand`, `Dual Wield`, `Attack Boost`, `Concentration` | Costed as convergence risks, not cheap flavor. |
| Defensive reactions | `Parry`, `Shirahadori`, `Reflexes`, `Auto-Potion`, `Dragonheart`, `Vanish` | Costed by band and slot pressure; no practical immunity route is accepted. |
| Movement | Move, Jump, Ignore Terrain, Ignore Elevation, Teleport, Fly, movement-heal routes | Movement costs preserve multiple late choices instead of one universal default. |

No accepted R/S/M or equipment-unlock family from docs 51, 58, and 59 is intentionally omitted.

## Review Mode

This is a structural pacing document. Claude should review it against:

- doc 31 timing invariants I1-I10;
- doc 51 intended bands and power categories;
- doc 58 slot model and R/S/M boundaries;
- doc 59 practical-online equipment bands;
- the user's fixed constraints: no new equipment and no gil/economy edits.

No dual damage simulation is expected unless a later edit touches formula values. This draft should
feed W4/T2.1 incidence and W5/F5, not replace them.

## Placeholder Fence

This producer prices pacing slots. It does not design every R/S/M effect.

Physical/foundation rows that were accepted in doc 58 can be treated as concrete-provisional
pacing inputs. Caster, performer, and Necromancer R/S/M rows are different: unless a row is a
vanilla-grounded name such as `Halve MP`, `Swiftspell`, or `Teleport`, the listed name and effect
are provisional placeholders. They are not canonical skill names, final effects, or atlas-checked
implementation records.

W4/T2.1 may still consume their band, slot, depth, and JP values for incidence, slot-conflict, and
pay-ability checks. Their names and effects must wait for a future concrete R/S/M producer.

`Armor Discipline` is already a doc 58 physical/foundation pacing row, but it is a non-vanilla
label. This draft treats its final implementation name as provisional while keeping its accepted
doc 58 slot, band, depth, and JP pacing role.

## Job-Depth Model

This draft uses five job-depth thresholds. They are provisional job-level targets, not final
implementation data.

| Depth | Meaning | Provisional earned JP threshold in that job | Design use |
| --- | --- | ---: | --- |
| Lv1 | unlocked | 0 | Active job just became available. |
| Lv2 | shallow | 250 | First specialist unlocks and first role hooks. |
| Lv3 | committed | 650 | Midgame identity and first serious exports. |
| Lv4 | deep | 1200 | Advanced route commitment and strong engines. |
| Lv5 | mastery | 2000 | Late reward jobs and capstone exports. |

Doc 61 removes `JP Boost` entirely. Fixed JP pacing must be tested by W4 through ordinary,
optimizer, and grind-heavy rows instead of with/without support-tax routing.

## Job Prerequisite Tree

Prerequisites are written as minimum job depths. They intentionally avoid vanilla-style extreme
multi-job grind while preserving the FFT pleasure of planning a route through several jobs.

| Job | Band | Prerequisite draft | Why this shape |
| --- | --- | --- | --- |
| Squire | 0 | start | Baseline physical/utility floor. |
| Chemist | 0 | start | Baseline item/revive floor. |
| Knight | B | Squire Lv2 | First durable physical specialist. |
| Archer | B | Squire Lv2 | First ranged physical specialist. |
| White Mage | B | Chemist Lv2 | First dedicated support caster after item floor. |
| Black Mage | B | Chemist Lv2 | First dedicated offense caster after item floor. |
| Monk | B/C | Knight Lv2 | Physical route turns durability into exposed unarmed pressure. |
| Thief | B/C | Archer Lv2 | Speed/knife utility follows early ranged/light physical route. |
| Time Mage | C | Black Mage Lv2 | Caster route branches into CT control after basic offense. |
| Mystic | C | White Mage Lv2 | Caster route branches into Faith/status control after support. |
| Geomancer | C | Knight Lv2 + Black Mage Lv2 | Hybrid terrain route requires one physical and one caster branch. |
| Dragoon | C | Knight Lv3 + Archer Lv2 | Spear/Jump route needs committed martial depth plus range/height literacy. |
| Orator | C | Mystic Lv2 + Chemist Lv3 | Speech/gun utility follows status control plus item-support grounding. |
| Summoner | C | Black Mage Lv3 + Time Mage Lv2 | Area caster route requires offense plus CT/delay literacy. |
| Samurai | D | Knight Lv4 + Monk Lv3 + Geomancer Lv2 | Advanced disciplined physical route; avoids cheap katana/Doublehand access. |
| Ninja | D | Thief Lv4 + Monk Lv3 + Archer Lv3 | Advanced fast physical route; preserves Thief/Monk/Archer route texture. |
| Bard | D | Orator Lv3 + Geomancer Lv3 | Performer route requires social/control plus map/hybrid commitment. |
| Dancer | D | Orator Lv3 + Geomancer Lv3 | Same depth as Bard; gender restriction affects active job only, not R/S/M access depth. |
| Necromancer | E | Mystic Lv4 + Black Mage Lv4 + Time Mage Lv3 + Summoner Lv3 | Late dark/state caster; one of the last jobs, not a midgame caster shortcut. |
| Vanguard | E | Knight Lv5 + Dragoon Lv3 + Samurai Lv3 + Squire Lv3 | Late vanguard; elite physical protection without copying Holy Knight. |
| Ramza chapter job | story | chapter state, not generic prerequisites | Ramza evolves by story and borrows normal R/S/M through the shared build system. |

### Tree Guardrails

- A job's active identity should be useful at Lv1/Lv2 before its strongest export is costed.
- A strong export cannot land earlier than its doc 51/doc 59 band even if the job unlocks earlier.
- Band D/E engines must require both route depth and high JP cost.
- Bard and Dancer use the same prerequisite depth because their R/S/M access must remain equal.
- Necromancer and Vanguard are intentionally late and should not be used to patch holes in earlier
  jobs.

## JP Cost Scale

Exact numbers below are provisional W4 inputs. They are designed to be easy to tune after incidence
tests.

| Cost tier | JP range | Intended use |
| --- | ---: | --- |
| T0 | 50-120 | Starter actions and floor utility. |
| T1 | 150-250 | Early role hooks and shallow movement. |
| T2 | 300-450 | First-specialist options and narrow tactical supports. |
| T3 | 500-650 | Midgame route commitments and moderate global pieces. |
| T4 | 700-900 | Strong global pieces and first convergence risks. |
| T5 | 1000-1200 | Advanced engines, premium reactions, and late movement. |
| T6 | 1400+ | Final/premium engines that must never be cheap defaults. |

Band targets:

| Band | Normal JP posture |
| --- | --- |
| 0/A | 50-250 pieces; no combat engine. |
| B | 100-450 pieces; active specialists work before exports. |
| C | 300-700 pieces; first compression tests begin. |
| D | 600-1200 pieces; one premium engine per route is plausible, not all. |
| E | 900-1400+ pieces; late jobs can be top-tier without becoming mandatory. |

## R/S/M And Export JP Draft

Rows are grouped by donor job. `Depth` is the minimum expected donor-job depth before a normal
route should learn the piece.

The companion JSON records the prerequisite tree, depth thresholds, and protected W4 seed rows. The
full cost table below is the human-readable source for non-protected R/S/M pieces until W4 tooling
needs a full machine-readable export.

| Job | Piece | Slot | Band | Depth | JP | Notes |
| --- | --- | --- | --- | --- | ---: | --- |
| Squire | `Grit` | Reaction | A/B | Lv2 | 180 | Early morale defense; narrow enough to appear early. |
| Squire | `Basic Training` | Support | B/C | Lv3 | 350 | Squire's sole intentional support export; Squire actions only, not broad physical output. |
| Squire | `Move +1` | Movement | A | Lv2 | 150 | Floor comfort. |
| Chemist | `Throw Item` | Support | A/B | Lv2 | 300 | Range utility; fixed item economy. |
| Chemist | `Auto-Potion` | Reaction | C | Lv3 | 700 | Potion-only 30 HP; strong global despite flat value. |
| Chemist | `Item Lore` | Support | C | Lv3 | 650 | Dedicated item route; no Auto-Potion interaction. |
| Chemist | `Safeguard` | Support | C | Lv3 | 450 | Equipment-pressure matchup utility. |
| Chemist | `Reequip` | Support | C/D | Lv3 | 250 | Tactical hook only; first cut if slot pressure is crowded. |
| Chemist | `Move-Find Item` | Movement | B/C | Lv2 | 250 | Campaign hook; no combat mobility value. |
| Knight | `Parry` | Reaction | B/C | Lv2 | 300 | Preferred Knight reaction. |
| Knight | `Brace` | Reaction | B/C | Lv2 | 250 | Weaker fallback. |
| Knight | `Equip Armor` | Support | C | Lv3 | 700 | Strong global; doc 59 practical-online C. |
| Knight | `Equip Shield` | Support | C | Lv3 | 600 | Strong global; check evasion stacks. |
| Knight | `Defensive Training` | Support | C/D | Lv3 | 500 | Guard/Arts route, not broad mitigation. |
| Knight | `Shield March` | Movement | C | Lv3 | 350 | Formation tempo, not map traversal. |
| Archer | `Arrow Guard` | Reaction | B/C | Lv2 | 300 | Missile-only defense. |
| Archer | `Speed Save` | Reaction | C/D | Lv3 | 650 | CT gain, not Speed stat snowball. |
| Archer | `Equip Bow` | Support | C | Lv3 | 650 | Export after Archer's native Band B role. |
| Archer | `Concentration` | Support | C/D | Lv4 | 850 | Accuracy convergence risk; not universal hit. |
| Archer | `Bow Mastery` | Support | C | Lv3 | 500 | Narrow bow/crossbow support. |
| Archer | `Jump +1` | Movement | B | Lv2 | 200 | Early vertical answer. |
| Monk | `Counter` | Reaction | B/C | Lv2 | 300 | Melee exposure reward. |
| Monk | `First Strike` | Reaction | D | Lv4 | 900 | Late pre-hit pressure only. |
| Monk | `Brawler` | Support | C/D | Lv3 | 750 | Build-defining unarmed route. |
| Monk | `Martial Discipline` | Support | C | Lv3 | 450 | Monk actions only. |
| Monk | `Lifefont` | Movement | C | Lv3 | 450 | Movement-exposure sustain. |
| White Mage | `Divine Grace` | Reaction | C/D | Lv3 | 500 | Healer under pressure; no immunity. |
| White Mage | `Arcane Ward` | Support | C/D | Lv3 | 700 | Placeholder caster R/S/M label; defensive caster pacing only. |
| White Mage | `Faithful Casting` | Support | C/D | Lv3 | 450 | Placeholder caster R/S/M label; White-action pacing only. |
| White Mage | `Sanctuary Step` | Movement | C | Lv3 | 350 | Placeholder caster R/S/M label; healer positioning pacing only. |
| Black Mage | `Arcane Backlash` | Reaction | C | Lv3 | 500 | Placeholder caster R/S/M label; fragile caster retaliation pacing only. |
| Black Mage | `Elemental Focus` | Support | C/D | Lv3 | 650 | Placeholder caster R/S/M label; elemental-specialist pacing only. |
| Black Mage | `Arcane Strength` | Support | D | Lv4 | 850 | Placeholder caster R/S/M label; broad MA risk pacing only. |
| Black Mage | `Ley Step` | Movement | C/D | Lv3 | 450 | Placeholder caster R/S/M label; spell-line pacing only. |
| Time Mage | `Critical: Quick` | Reaction | D/E | Lv4 | 1000 | Critical-only tempo; no loops. |
| Time Mage | `Swiftspell` | Support | D | Lv4 | 1100 | Vanilla-grounded name; effect value still pending caster R/S/M producer. |
| Time Mage | `Temporal Focus` | Support | C/D | Lv3 | 550 | Placeholder caster R/S/M label; Time-action pacing only. |
| Time Mage | `Teleport` | Movement | D | Lv4 | 1000 | Vanilla-grounded name; effect value still pending movement/caster R/S/M producer. |
| Mystic | `Absorb MP` | Reaction | C/D | Lv3 | 450 | Placeholder caster R/S/M label until caster R/S/M producer confirms final effect. |
| Mystic | `Mana Shield` | Reaction | C/D | Lv3 | 750 | Placeholder caster R/S/M label until caster R/S/M producer confirms final effect. |
| Mystic | `Halve MP` | Support | C/D | Lv4 | 850 | Vanilla-grounded name; effect value still pending caster-economy work. |
| Mystic | `Magick Defense Boost` | Support | C/D | Lv3 | 750 | Placeholder caster R/S/M label until caster R/S/M producer confirms final effect. |
| Mystic | `Mystic Focus` | Support | C | Lv3 | 450 | Placeholder caster R/S/M label; status/Mystic pacing only. |
| Mystic | `Manafont` | Movement | C/D | Lv3 | 500 | Placeholder caster R/S/M label until caster R/S/M producer confirms final effect. |
| Summoner | `Summon Ward` | Reaction | C/D | Lv3 | 600 | Placeholder caster R/S/M label; summon-defense pacing only. |
| Summoner | `Summon Focus` | Support | C/D | Lv3 | 800 | Placeholder caster R/S/M label; summon-only pacing only. |
| Summoner | `Grand Invocation` | Support | D/E | Lv4 | 1200 | Placeholder caster R/S/M label; area-dominance pacing only. |
| Summoner | `Ritual Step` | Movement | C/D | Lv3 | 500 | Placeholder caster R/S/M label; summon setup pacing only. |
| Geomancer | `Nature's Wrath` | Reaction | C | Lv3 | 450 | Terrain-dependent retaliation. |
| Geomancer | `Terrain Lore` | Support | C | Lv3 | 450 | Terrain identity. |
| Geomancer | `Attack Boost` | Support | unassigned | Deferred | 1000 placeholder | Protected stress engine only if later assigned. |
| Geomancer | `Ignore Terrain` | Movement | C/D | Lv3 | 500 | Terrain answer, not raw reach. |
| Geomancer | `Ignore Weather` | Movement | C/D | Lv3 | 350 | Keep only if map texture justifies it. |
| Thief | `Sticky Fingers` | Reaction | C | Lv3 | 350 | Battle-scoped reward response only. |
| Thief | `Light Fingers` | Support | C | Lv3 | 400 | Steal only. |
| Thief | `Poach` | Support | Deferred | Deferred | - | Monster/economy route out of scope. |
| Thief | `Move +2` | Movement | C | Lv3 | 500 | Strong mid movement; no terrain/elevation bypass. |
| Thief | `Treasure Hunter` | Movement | C | Lv3 | 350 | Campaign hook only. |
| Orator | `Bravery Surge` | Reaction | C/D | Lv3 | 500 | Battle-scoped Brave. |
| Orator | `Faith Surge` | Reaction | C/D | Lv3 | 650 | High-risk caster/vulnerability lever. |
| Orator | `Equip Guns` | Support | C/D | Lv4 | 950 | Export effectively D by doc 59. |
| Orator | `Tame` | Support | Deferred | Deferred | - | Monster route out of scope. |
| Orator | `Beast Tongue` | Support | Deferred | Deferred | - | Monster route out of scope. |
| Orator | `Social Positioning` | Movement | C | Lv3 | 350 | Speech/gun positioning only. |
| Dragoon | `Dragonheart` | Reaction | C/D | Lv4 | 850 | Once/battle Reraise pressure. |
| Dragoon | `Brace Landing` | Reaction | C | Lv3 | 450 | Jump route defense. |
| Dragoon | `Equip Polearms` | Support | C/D | Lv3 | 800 | Strong export; Dragoon remains spear home. |
| Dragoon | `Jump Training` | Support | C | Lv3 | 450 | Jump action reach only. |
| Dragoon | `Jump +1` | Movement | B/C | Lv2 | 200 | Early vertical answer. |
| Dragoon | `Jump +2` | Movement | C | Lv3 | 350 | Committed vertical answer. |
| Dragoon | `Jump +3` | Movement | D | Lv4 | 700 | Advanced vertical answer. |
| Dragoon | `Ignore Elevation` | Movement | D/E | Lv4 | 1000 | Late vertical specialist. |
| Samurai | `Shirahadori` | Reaction | D/E | Lv4 | 1200 | Iconic but capped; no broad immunity. |
| Samurai | `Bonecrusher` | Reaction | D | Lv3 | 700 | Retaliation fallback. |
| Samurai | `Equip Katana` | Support | D | Lv3 | 900 | Meaningful route unlock. |
| Samurai | `Doublehand` | Support | D/E | Lv4 | 1200 | Protected single-weapon engine. |
| Samurai | `Iaido Focus` | Support | D | Lv3 | 650 | Iaido only. |
| Samurai | `Waterwalking` | Movement | C/D | Lv3 | 350 | Map dependent. |
| Samurai | `Blade Step` | Movement | D | Lv3 | 600 | Stance/position hook. |
| Ninja | `Vanish` | Reaction | D/E | Lv4 | 1100 | One-action Invisible; no loop. |
| Ninja | `Reflexes` | Reaction | D | Lv3 | 700 | Evasion-light mitigation. |
| Ninja | `Dual Wield` | Support | D/E | Lv4 | 1400 | Highest physical convergence cost. |
| Ninja | `Throw Mastery` | Support | D | Lv3 | 700 | Throw-only support. |
| Ninja | `Move +3` | Movement | D/E | Lv4 | 1000 | Late raw reach competitor. |
| Ninja | `Ignore Terrain` | Movement | D | Lv3 | 650 | Terrain route. |
| Bard | `Earplugs` | Reaction | D | Lv3 | 450 | Placeholder performer R/S/M label; must match Dancer. |
| Bard | `Encore` | Reaction | D/E | Lv4 | 1000 | Placeholder performer R/S/M label; must match Dancer. |
| Bard | `Performance Mastery` | Support | D | Lv3 | 900 | Placeholder performer R/S/M label; must match Dancer. |
| Bard | `Stagecraft` | Support | D | Lv3 | 550 | Placeholder performer R/S/M label; must match Dancer. |
| Bard | `Performance Step` | Movement | D | Lv3 | 600 | Placeholder performer R/S/M label; must match Dancer. |
| Bard | `Fly` | Movement | E/promotion | Lv5 | 1400 | Placeholder performer R/S/M label; must match Dancer if retained. |
| Dancer | `Earplugs` | Reaction | D | Lv3 | 450 | Placeholder performer R/S/M label; byte-identical to Bard target. |
| Dancer | `Encore` | Reaction | D/E | Lv4 | 1000 | Placeholder performer R/S/M label; byte-identical to Bard target. |
| Dancer | `Performance Mastery` | Support | D | Lv3 | 900 | Placeholder performer R/S/M label; byte-identical to Bard target. |
| Dancer | `Stagecraft` | Support | D | Lv3 | 550 | Placeholder performer R/S/M label; byte-identical to Bard target. |
| Dancer | `Performance Step` | Movement | D | Lv3 | 600 | Placeholder performer R/S/M label; byte-identical to Bard target. |
| Dancer | `Fly` | Movement | E/promotion | Lv5 | 1400 | Placeholder performer R/S/M label; byte-identical to Bard if retained. |
| Necromancer | `Soulbind` | Reaction | E | Lv3 | 700 | Placeholder Necromancer R/S/M label; state/attrition pacing only. |
| Necromancer | `Death's Door` | Reaction | E | Lv4 | 1000 | Placeholder Necromancer R/S/M label; late survival pacing only. |
| Necromancer | `Dark Lore` | Support | E | Lv3 | 700 | Placeholder Necromancer R/S/M label; dark-state pacing only. |
| Necromancer | `Deathcraft` | Support | E | Lv4 | 1200 | Placeholder Necromancer R/S/M label; corpse/death-state pacing only. |
| Necromancer | `Grave Step` | Movement | E | Lv3 | 650 | Placeholder Necromancer R/S/M label; corpse/marked positioning pacing only. |
| Necromancer | `Shadow Step` | Movement | E | Lv4 | 1000 | Placeholder Necromancer R/S/M label; late dark mobility pacing only. |
| Vanguard | `Intervention` | Reaction | E | Lv3 | 1000 | Local ally protection. |
| Vanguard | `Last Stand` | Reaction | E | Lv4 | 800 | Panic survival only. |
| Vanguard | `Equip Knight Swords` | Support | E | Lv4 | 1400 | Premium sword route; optional/cuttable. |
| Vanguard | `Vanguard Training` | Support | E | Lv3 | 700 | Vanguard actions only. |
| Vanguard | `Armor Discipline` | Support | E | Lv4 | 1200 | Doc 58 pacing row; non-vanilla implementation name remains provisional. |
| Vanguard | `Vanguard March` | Movement | E | Lv3 | 750 | Formation movement only. |
| Ramza | unique exportable R/S/M | R/S/M | 0-E | - | - | None accepted; Ramza uses normal learned R/S/M. |

## Protected Convergence Rows

These rows must be watched first in W4/T2.1.

| Piece | Job | Band | JP | Why it is protected |
| --- | --- | --- | ---: | --- |
| `Auto-Potion` | Chemist | C | 700 | Reliable global sustain if too early or too broad. |
| `Equip Armor` | Knight | C | 700 | Can erase fragile job weakness. |
| `Equip Shield` | Knight | C | 600 | Can stack with evasion/reactions. |
| `Concentration` | Archer | C/D | 850 | Accuracy support can become mandatory. |
| `Brawler` | Monk | C/D | 750 | Unarmed route can outscale weapons if too cheap. |
| `Halve MP` | Mystic | C/D | 850 | Caster economy convergence. |
| `Swiftspell` | Time Mage | D | 1100 | Caster action-economy convergence. |
| `Equip Guns` | Orator | C/D | 950 | PA-independent damage patch; export effectively D. |
| `Equip Polearms` | Dragoon | C/D | 800 | Can steal Dragoon's spear home. |
| `Equip Katana` | Samurai | D | 900 | Can reduce Samurai to a support stop. |
| `Doublehand` | Samurai | D/E | 1200 | Protected x1.80 engine. |
| `Dual Wield` | Ninja | D/E | 1400 | Highest physical convergence risk. |
| `Teleport` | Time Mage | D | 1000 | Late movement default risk. |
| `Move +3` | Ninja | D/E | 1000 | Raw reach default risk. |
| `Equip Knight Swords` | Vanguard | E | 1400 | Premium sword dominance risk. |

## W4 Consumption Rules

W4/T2.1 should treat a build piece as reachable only when all of these are true:

1. the donor job is unlocked by the prerequisite tree;
2. the donor job has the listed minimum depth;
3. the unit can plausibly pay the listed JP cost in the campaign band;
4. support-slot, reaction-slot, or movement-slot conflict is respected;
5. equipment exports also satisfy doc 59 practical-online timing;
6. fixed vanilla economy remains unchanged.

If ordinary or optimizer fixed-JP routing can move a Band D/E protected piece into ordinary Band B/C
play, the first allowed tuning levers are:

1. raise donor depth;
2. raise JP cost within the same band;
3. add or strengthen cross-route prerequisite depth;
4. narrow the piece's effect in its owning concrete doc;
5. cut the piece only if the previous levers fail.

## Open Follow-Up

- Claude review of prerequisite depth and JP cost consistency.
- W4/T2.1 populated incidence using this draft, doc 58 R/S/M values, and doc 59 equipment timing.
- W5/F5 real-roster sweep for jobs whose practical equipment or R/S/M profile changes.
- Later implementation merge of action JP values from docs 42-57 with this R/S/M/export JP table.

## Claude Review Request

Claude should review:

- whether any job unlock lands earlier or later than its intended campaign role;
- whether any protected convergence piece is too cheap for its band;
- whether Bard/Dancer parity is preserved;
- whether Necromancer and Vanguard are late enough without requiring excessive grind;
- whether this draft is sufficient to close W3 and start W4/T2.1.
