# Bard And Dancer V1 Proposal

Status: Accepted for provisional design
Version: V1
Date: 2026-06-21
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/09-accuracy-evasion-model-schema.md`
- `docs/job-balance/10-healing-attrition-model-schema.md`
- `docs/job-balance/11-ct-delay-model-schema.md`
- `docs/job-balance/15-targeting-challenge-model-schema.md`
- `docs/job-balance/20-time-mystic-v1-proposal.md`
- `docs/job-balance/22-thief-orator-v1-proposal.md`
- `docs/job-balance/23-deferred-campaign-economy-policy.md`
- `docs/job-balance/25-ninja-v1-proposal.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Bard and Dancer.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, performance tick
rates, performance duration, mapwide radius, hit rates, status rates, healing values, stat deltas,
Brave/Faith deltas, movement values, damage multipliers, equipment records, stat multipliers, or
prerequisites.

Bard and Dancer are paired because they are the only intentional gender-restricted generic jobs in
the accepted direction. Their active action identities are different, but their reaction, support,
and movement rewards must be exactly shared so no gender is locked out of global build pieces.

Claude review verdict: conditional acceptance on 2026-06-21, with required edits applied and final
acceptance confirmed in the revised version.

## Group Thesis

Bard and Dancer should make performance a real battlefield plan.

- Bard is the support performer: songs, morale, recovery, party momentum, and slow global buildup.
- Dancer is the pressure performer: dances, attrition, stat pressure, resource pressure, and slow
  global disruption.

Both jobs should feel like FFT performers, not normal casters with strange animations.

Performance should be powerful because it affects many units over time. That power must be paid for
through:

- performer fragility;
- `Performing` vulnerability;
- slow tick timing;
- interruption risk;
- target-count normalization;
- low per-target values;
- clear counters through positioning, focus fire, Silence-like disruption, or finishing the battle
  before the performance compounds.

## Gender And Parity Policy

Bard remains male-only. Dancer remains female-only.

This is the only accepted generic gender restriction.

Mandatory parity rule:

```text
Bard reaction skills == Dancer reaction skills
Bard support skills  == Dancer support skills
Bard movement skills == Dancer movement skills
```

Action abilities may differ because the jobs have different fantasies. Global build pieces may not
differ. A male and female unit should have equal access to the same reaction/support/movement
toolkit through the Bard/Dancer route.

No equipment restrictions by gender are accepted. Any future equipment access differences between
Bard and Dancer must be job-identity differences, not gender superiority.

## Shared Performer Notes

Performance actions are not ordinary spells.

The accepted V1 model is:

- performance actions can be mapwide or very large area only if T11 normalizes target count;
- performance actions usually place the user in a vulnerable `Performing` state;
- `Performing` should suppress or reduce evasion and make interruption meaningful;
- performance ticks should use T5 timing rather than instant full payoff;
- global effects should be low per target and meaningful only through duration or team plan;
- sustained performance throughput must be modeled as
  `per_tick_value * expected_target_count * expected_tick_count`, not by T11 or T5 alone;
- random songs/dances must have bounded result tables and cannot hide hard control behind chance;
- instant KO/death performances are rejected as ordinary global effects.

Relevant accepted gates:

- T2.1 for performance secondary incidence and shared reaction/support/movement incidence;
- T3/T3xT5 for Seraph Song, Life's Anthem, Regen-like effects, damage-over-time, and attrition;
- T4 for Performing vulnerability, Earplugs, evasion suppression, Silence-like interruption, and
  performance-status accuracy;
- T5 for performance tick timing, duration, interruption windows, Haste/Slow effects, and performer
  turn loss;
- T6xPS for any Protect/Shell/Wall-like performance mitigation;
- T7 for Polka-style PA/output reduction or other enemy-offense stat pressure;
- T9 for Witch Hunt-style MP pressure, MP attrition, or resource loops;
- T10 for any performance that grants actions, refunds CT, or creates turn recursion;
- T11 for mapwide/global target-count normalization, ally/enemy targeting, and area shape;
- T11xT5 for sustained mapwide throughput over target count and duration;
- T3xT5xT11 for healing, regen, HP attrition, poison-like pressure, or repeated HP effects over
  target count and duration;
- Gate F4/F5 if performance stat changes, healing, global damage, movement, or weapon access drifts
  the accepted formula ecology.

## Shared Reaction, Support, And Movement

These records are shared by Bard and Dancer by rule.

### Shared Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Earplugs` | anti-performance/speech defense | Resist or reduce performance, speech, sound, or morale pressure. | Narrow scope; should not become broad status immunity. |
| `Encore` | performance resilience candidate | Let a performer preserve rhythm after a narrow interruption case. | Optional; must not erase `Performing` vulnerability. |

`Earplugs` is the safer default shared reaction because it is narrow and performer-flavored. It can
also serve as counterplay if Bard/Dancer effects become important in enemy designs.

`Encore` is a candidate only if testing shows active performers are too easy to shut down. It should
not protect against all damage or all interruption. The whole point of performance is that the unit
accepts risk to gain global pressure.

### Shared Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Performance Mastery` | song/dance specialization | Improve performance tick reliability, duration discipline, or interruption resistance only for performance actions. | No broad magic/physical/stat boost. |
| `Stagecraft` | performer setup candidate | Improve initial performance setup or reduce self-risk in a narrow way. | Optional; must not compress mobility, defense, and performance output. |

No broad `Magick Boost`, `Attack Boost`, or global stat support should be assigned to Bard/Dancer in
this V1. Their active kits already have global effects, so their support skills should enhance the
performance plan rather than become generic build engines.

### Shared Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Performance Step` | narrow positioning | Help fragile performers reach or hold safer performance positions without broad map bypass. | Must not become generic mobility. |
| `Fly` | dramatic movement promotion candidate | Optional late movement if performers cannot function without stronger positioning. | High T2.1 risk; must compete with Teleport, Move +3, Jump mobility, and terrain tools. |

Bard/Dancer are primarily active/secondary performance routes, not a general RSM-donor route. Their
shared supports are intentionally performance-only and therefore have low off-job value unless the
unit is borrowing Song or Dance. The shared movement slot should follow the same posture.

Default V1 movement is `Performance Step`. `Fly` should be promoted only if T2.1 and performer
scenario rows show that fragile performers cannot reasonably reach or hold channel positions without
stronger movement. If `Fly` becomes a default export for non-performers, it fails this route's
intent.

## Bard

Job: Bard
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: male-only performer with global songs that heal, buff, raise Brave, raise magic, roll
random buffs, and threaten Finale.

Vanilla problems:

- mapwide effects can be either too weak to matter or too strong to ignore;
- slow performance can feel passive if ticks are not tactically legible;
- healing songs can replace dedicated healers if they compound safely;
- stat and Brave boosts can become mandatory pre-fight upkeep if values are too high;
- global instant KO/death is not acceptable as ordinary combat balance.

Accepted high-level role: performance support job.

Primary role: `performer`

Secondary tags: `support`, `instrument`

Growth profile: `hybrid`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `instrument`, `bag`, `fists`.

Armor class as target: `cloth`.

Supported damage modes: `missile`, `crush`.

Formula v0.2 coupling:

- instruments are `pampa_wp` missile, giving Bard a hybrid ranged support-weapon fallback;
- bags are volatile `rdm_pa_wp` crush and should remain oddball, not Bard's identity;
- Bard's real power comes from global support timing, not weapon damage;
- Brave/morale songs overlap with Orator only by theme: Bard is slow/global/performance, Orator is
  targeted/social/immediate control.

### Action Skillset Goals

Bardsong should make the player think in tempo and formation.

The player should ask:

- can I protect this performer long enough for the song to matter?
- do I need recovery, speed, Brave, magic support, random utility, or a capstone?
- will this global support outperform a direct healer/caster/action right now?
- can the enemy punish `Performing` before the value compounds?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Seraph Song` | gentle global recovery | Low per-tick healing or Regen-like recovery across allies. | T3/T3xT5/T11; must not replace direct healing. |
| `Life's Anthem` | stronger recovery song | Higher-investment healing, revive support, or KO-pressure mitigation if retained. | T3/T5/T11; no safe mapwide Arise loop. |
| `Rousing Melody` | tempo support | Slow global Speed/CT morale support. | T5/T10 if action-granting; no Quick-class recursion. |
| `Battle Chant` | Brave/morale support | Battle-scoped Brave or morale increase over time. | T2/T5; Orator remains targeted morale controller. |
| `Magickal Refrain` | magic support | Boost allied magical confidence or MA-like performance value. | F4/T2/T5; not a broad caster-tax support. |
| `Nameless Song` | bounded random support | Random ally buff table for performer flavor. | Bounded table; no hidden broad hard-control or immunity. |
| `Finale` | capstone song | Climactic support or weakened-target finisher fantasy. | T8 eligibility required; ordinary mapwide instant KO is rejected. |

`Finale` must be rewritten away from normal global instant death.

If Finale remains a finisher, it must satisfy all three rules:

1. explicit boss/immune exclusion through T8 eligibility;
2. pre-existing weakened precondition, such as an HP-fraction or status threshold created by prior
   team work;
3. no chance-based hard KO hidden behind a T4 roll.

The kill, if any, must be conditional on setup. It cannot be a cold mapwide press.

Other safe directions include:

- ending the current song for a large but nonlethal support pulse;
- a morale reset or cleanse-like climax.

The final implementation can choose one later. This V1 rejects ordinary mapwide instant KO as a
baseline Bard action.

### Cross-Job Build Hooks

Healthy Bard donor patterns:

- support unit borrows Bardsong to provide slow global team value while protected;
- caster team uses Bard for a deliberate long-fight setup;
- Bard active job uses instrument range and performance positioning;
- party chooses Bard over White Mage when slow global recovery is better than direct healing.

Unhealthy Bard donor patterns:

- every party wants one Bard song running by default;
- Bardsong replaces White Mage/Chemist recovery;
- Battle Chant or Magickal Refrain becomes mandatory upkeep;
- Finale creates global instant-win pressure;
- Bard's shared movement/support becomes a gender-locked build advantage.

### Expected Strong Builds

- protected Bard with Bardsong secondary support and defensive positioning;
- instrument Bard contributing ranged chip while waiting for a performance window;
- Bard plus Time Mage/White Mage party that plans around long fights;
- party morale build using Bard for global Brave/magic support while Orator handles targeted
  morale/control.

### Expected Weaknesses

- fragile cloth profile;
- `Performing` vulnerability and interruption;
- low immediate payoff;
- poor direct burst;
- Silence, Stop, Sleep, displacement, or focus fire can break the plan;
- short fights may end before songs compound.

### Expected Counters

- aggressive enemies that pressure the performer;
- Silence or anti-performance reactions such as Earplugs;
- high mobility enemies that punish backline positioning;
- burst damage before healing ticks matter;
- fights where direct action economy is more valuable than global upkeep.

### Ramza / Unique-Job Interaction

Ramza may later gain leadership or morale effects, but he should not become a better Bard. His
leadership should be more immediate or hybrid-personal, while Bard owns slow global performance.

## Dancer

Job: Dancer
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: female-only performer with global dances that damage, drain MP, lower speed/stats,
roll random statuses, and threaten Last Waltz.

Vanilla problems:

- mapwide debuffs can become invisible mandatory upkeep;
- global HP damage can replace actual offense if it stacks safely;
- random status dances can hide hard control behind variance;
- MP/stat damage can become either useless or encounter-warping;
- global instant KO/death is not acceptable as ordinary combat balance.

Accepted high-level role: performance debuff/pressure job.

Primary role: `performer`

Secondary tags: `debuff`, `cloth_weapon`

Growth profile: `hybrid`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `cloth_weapon`, `bag`, `knife`, `fists`.

Armor class as target: `cloth`.

Supported damage modes: `swing`, `crush`, `thrust`.

Formula v0.2 coupling:

- cloth weapons are `pampa_wp` swing, giving Dancer a hybrid light-weapon route;
- knives are Speed/PA thrust, but Dancer should not replace Thief's precision/disruption role;
- bags are volatile crush and should remain oddball;
- Dancer's real power comes from global pressure timing, not weapon damage;
- Dancer overlaps with Mystic and Time Mage only by debuff theme: Dancer is slow/global/performance,
  Mystic/Time Mage are targeted control specialists.

### Action Skillset Goals

Dance should create pressure over time.

The player should ask:

- can I keep the Dancer safe long enough for pressure to compound?
- do I need HP attrition, MP pressure, Speed pressure, PA/MA pressure, random disruption, or a
  capstone?
- is a slow global debuff better than a targeted Mystic, Time Mage, Orator, or Black Mage action?
- can the enemy punish `Performing` before the dance pays off?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Witch Hunt` | MP/resource pressure | Slow mapwide pressure against casters or MP-dependent enemies. | T9/T11; no hidden campaign drain or caster deletion. |
| `Mincing Minuet` | HP attrition | Low per-tick global damage that pressures long fights. | T3/T5/T11/F4; must not replace Black Mage/Summoner. |
| `Slow Dance` | tempo debuff | Slow enemy Speed/CT over time. | T5/T11; Time Mage remains targeted tempo specialist. |
| `Polka` | physical offense pressure | Lower PA/output or physical confidence over time. | T7/T11; no permanent stat damage. |
| `Heathen Frolic` | magical offense pressure | Lower MA/Faith-like offense or caster confidence over time. | F4/T9/T11; Mystic remains targeted Faith/status controller. |
| `Forbidden Dance` | bounded random disruption | Random debuff/status table for performer flavor. | Bounded table; no broad hard-control lottery. |
| `Last Waltz` | capstone dance | Climactic pressure or weakened-target finisher fantasy. | T8 eligibility required; ordinary mapwide instant KO is rejected. |

`Last Waltz` must be rewritten away from normal global instant death.

If Last Waltz remains a finisher, it must satisfy all three rules:

1. explicit boss/immune exclusion through T8 eligibility;
2. pre-existing weakened precondition, such as an HP-fraction or status threshold created by prior
   team work;
3. no chance-based hard KO hidden behind a T4 roll.

The kill, if any, must be conditional on setup. It cannot be a cold mapwide press.

Other safe directions include:

- ending the current dance for a large but nonlethal pressure pulse;
- a final global debuff with explicit duration and counterplay.

The final implementation can choose one later. This V1 rejects ordinary mapwide instant KO as a
baseline Dancer action.

### Cross-Job Build Hooks

Healthy Dancer donor patterns:

- pressure unit borrows Dance for long-fight attrition or debuff setup;
- Dancer active job uses fragile positioning and performance timing to pressure the enemy team;
- party chooses Dancer over Black Mage when slow global pressure is better than direct burst;
- party chooses Dancer over Mystic/Time Mage when broad slow pressure is better than targeted
  status/tempo control.

Unhealthy Dancer donor patterns:

- every party wants one Dance running by default;
- global damage replaces normal offense;
- random status dance becomes a mapwide hard-control slot machine;
- Last Waltz creates global instant-win pressure;
- Dancer's shared movement/support becomes a gender-locked build advantage.

### Expected Strong Builds

- protected Dancer pressuring long fights through HP/MP/stat attrition;
- Dancer with movement setup to perform from safe angles;
- hybrid performer using cloth weapon or knife only when performance is unsafe;
- party debuff plan that uses Dancer for global pressure while Mystic/Time Mage handle key targets.

### Expected Weaknesses

- fragile cloth profile;
- `Performing` vulnerability and interruption;
- low immediate payoff;
- poor direct burst;
- Silence, Stop, Sleep, displacement, or focus fire can break the plan;
- short fights may end before dances compound.

### Expected Counters

- aggressive enemies that pressure the performer;
- Silence or anti-performance reactions such as Earplugs;
- high mobility enemies that punish backline positioning;
- direct burst that ends the fight before attrition matters;
- status resistance or boss immunity for random disruption/capstone effects.

### Ramza / Unique-Job Interaction

Ramza may later gain leadership or hybrid support/debuff tools, but he should not become a better
Dancer. Dancer owns slow global pressure; Ramza should express protagonist identity through more
direct hybrid action.

## Shared Scenario And Check Plan

Minimum provisional rows before concrete values:

| Scenario ID | Purpose | Required gates |
| --- | --- | --- |
| `J-PERF-PARITY` | Bard and Dancer reaction/support/movement records are identical by exact data-record set equality. | data equality assertion/T2.1 |
| `J-PERF-GLOBAL` | Mapwide/global performance target count and sustained throughput are normalized. | T11/T11xT5 |
| `J-PERF-VULN` | `Performing` vulnerability and interruption remain meaningful. | T4/T5 |
| `J-PERF-RSM` | Earplugs, Performance Mastery, Stagecraft, Performance Step, and any Fly promotion incidence stays bounded. | T2.1 |
| `J-BARD-HEAL` | Seraph Song/Life's Anthem do not replace White Mage/Chemist after target count and duration. | T3xT5xT11 |
| `J-BARD-MORALE` | Battle Chant/Magickal Refrain do not become mandatory upkeep or erase Orator/caster balance. | T2/T11xT5/F4 |
| `J-BARD-RANDOM` | Nameless Song random table and random ally targeting are bounded. | T4/T5/T8/T11 |
| `J-BARD-FINALE` | Finale does not become ordinary mapwide instant KO and satisfies finisher eligibility rules. | T4/T5/T8/T11 |
| `J-DANCER-ATTRITION` | Mincing Minuet does not replace Black Mage/Summoner offense after target count and duration. | T3xT5xT11/F4 |
| `J-DANCER-RESOURCE` | Witch Hunt pressures MP without deleting casters or becoming campaign economy. | T9/T11xT5 |
| `J-DANCER-STATDOWN` | Slow Dance/Polka/Heathen Frolic debuffs do not replace Time Mage/Mystic/Knight roles. | T7/T9/T11xT5/F4 |
| `J-DANCER-RANDOM` | Forbidden Dance random table is bounded. | T4/T5/T8/T11 |
| `J-DANCER-LASTWALTZ` | Last Waltz does not become ordinary mapwide instant KO and satisfies finisher eligibility rules. | T4/T5/T8/T11 |

These are scenario requirements, not final scenario data.

## Formula Re-Sim Requirement

This proposal triggers formula review when values become concrete because it touches:

- global target-count-normalized healing, damage, buffs, and debuffs;
- sustained area throughput over target count and duration;
- Brave, MA, PA, Speed, Faith-like, and MP pressure;
- instrument missile and cloth-weapon swing access;
- shared movement/reaction/support incidence;
- `Performing` vulnerability and evasion suppression;
- global random status/effect tables;
- capstone instant-KO legacy effects.

The strongest available status before real weapon data and the required gates is:

```text
Accepted for provisional design
```

No concrete implementation data should be marked final until the affected T-gates pass and formula
v1 or its accepted successor reconciles real weapon values.

## Implementation Assumptions

- Data mod scope can rewrite performance ticks, timing, target sets, effect tables, JP costs,
  skill names, gender job access, and shared reaction/support/movement records where needed.
- Bard and Dancer may keep different action skillsets.
- Bard and Dancer must share identical reaction/support/movement records.
- The data pass must assert Bard and Dancer R/S/M equality mechanically by comparing exact record
  sets, not only by prose review.
- Performance actions should be battle-scoped in this phase; permanent Brave/Faith/campaign
  economy effects are deferred.
- If ordinary mapwide instant KO cannot be made healthy, Finale and Last Waltz should be
  repurposed rather than preserved literally.

## Open Proof Needs

- Exact performance tick timing, duration, and interruption rules.
- Whether `Performing` suppresses all evasion, only some evasion, reactions, movement, or other
  defensive layers.
- Expected target count for global/mapwide songs and dances.
- Exact T11xT5 and T3xT5xT11 throughput envelopes for mapwide songs and dances.
- Whether `Performance Step` is enough as shared movement or `Fly` must be promoted.
- Whether Earplugs is useful enough as shared reaction without becoming broad immunity.
- Whether random performance tables can be bounded and player-legible.
- Exact battle-scoped Brave/Faith/stat pressure and whether any permanent component survives later
  campaign policy.

## Claude Review Request

Claude should review whether:

- the Bard/Dancer parity rule is explicit enough;
- Bard and Dancer should keep exactly these shared reaction/support/movement candidates;
- `Performance Step` as default shared movement and `Fly` as promotion candidate is acceptable;
- performance global effects are sufficiently routed through T11/T5/T4 and the sustained-throughput
  compositions before values;
- Finale and Last Waltz are handled correctly by rejecting ordinary mapwide instant KO;
- Bard overlaps too much with Orator/White Mage/Time Mage;
- Dancer overlaps too much with Mystic/Time Mage/Black Mage;
- the scenario/check plan names the right validation gates before concrete values.

Claude review verdict: Accepted (claude-opus-4-8, 2026-06-21).

Required edits applied:

- sustained performance throughput is now routed through T11xT5, with T3xT5xT11 for healing and
  HP attrition;
- Finale and Last Waltz now require T8 eligibility, a pre-existing weakened precondition,
  boss/immune exclusion, and no chance-based hard KO behind a T4 roll.

Recommended edits also applied:

- `Performance Step` is the default shared movement, with `Fly` only as a promotion candidate;
- Bard/Dancer are framed as active/secondary performance routes rather than broad RSM-donor routes;
- Nameless Song random ally targeting now includes T8;
- Bard/Dancer R/S/M parity is a data-record set equality assertion.
