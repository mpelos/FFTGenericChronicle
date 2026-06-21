# Time Mage And Mystic V1 Proposal

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
- `docs/job-balance/16-healing-timing-composition-schema.md`
- `docs/job-balance/19-white-black-mage-v1-proposal.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Time Mage and Mystic.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, spell powers, MP
costs, hit rates, CT values, status rates, duration values, damage multipliers, equipment records,
stat multipliers, or prerequisites.

Time Mage and Mystic are the first controller pair after the White Mage/Black Mage foundation. They
must make control feel valuable without turning FFT into a game where the best answer is always
"deny the enemy turns."

Claude review verdict: conditional acceptance on 2026-06-21, with required edits applied in this
version.

## Group Thesis

Time Mage and Mystic should be distinct controller foundations.

- Time Mage controls tempo: CT, speed states, delayed action risk, turn windows, magic routing, and
  special movement.
- Mystic controls spiritual state: Faith, Brave/morale, status vulnerability, silence/disable-style
  pressure, drain, and undead-adjacent hooks.

Both jobs can create powerful tactical plans, but neither should become a universal secondary that
every caster or every party must equip.

## Shared Controller Notes

Control is more dangerous than raw damage because it can remove decisions from the opponent.

Concrete Time Mage or Mystic values must therefore prove:

- the player can understand what happened;
- status and timing effects have counters;
- hard control is not reliable enough to replace damage, healing, and positioning;
- duration and CT windows are short enough to keep combat interactive;
- build incidence does not make one controller skillset mandatory.

Relevant accepted gates:

- T4 for spell/status accuracy, evasion, line, height, and suppression states;
- T5 for Haste, Slow, Stop, delayed resolution, cast-time changes, and duration races;
- T8 for Reflect routing, AI/targeting-sensitive control, and allegiance/behavior effects;
- T8xSR for concrete Reflect or spell-routing behavior;
- T9 for MP recovery, MP drain, MP shields, MP discounts, or resource loops;
- T10 for Quick-class action grants, turn refunds, and anti-recursion checks;
- T3/T3xT5 for drain healing, Reraise-like recovery, Regen/Poison-adjacent attrition, or sustain;
- T6xPS if a defensive status or support stacks with Protect/Shell/armor response/clamps;
- Gate F4/F5 if Faith manipulation, magic damage support, CT compression, or MP economy drifts
  magic/physical coexistence.

Action-economy effects must be split into two classes:

- speed or cast-time modifiers, such as Haste, Slow, and Swiftspell, are T5-coverable because they
  change speed, CT, or CTR timing;
- action grants, such as Quick and Critical: Quick, are not T5-coverable and require T10 before
  concrete values can be accepted.

## Time Mage

Job: Time Mage
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: tempo caster with Haste, Slow, Stop, Immobilize, Float, Reflect, Quick, Gravity,
Graviga, Meteor, Swiftspell, and Teleport-style movement.

Vanilla problems:

- Haste can become invisible mandatory upkeep if action-economy gain is too efficient;
- Slow and Stop can hard-lock enemies if accuracy/duration/immunity are too permissive;
- Quick can create action loops or become the best support action in the game;
- Meteor can be too slow to matter or too strong if target prediction is trivialized;
- Swiftspell and Teleport can become universal caster/support/movement defaults.

Accepted high-level role: time/tempo controller.

Primary role: `controller`

Secondary tags: `CT`, `staff`

Growth profile: `magical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `staff`, `fists`.

Armor class as target: `cloth`.

Supported damage modes: `crush`, `magic`.

Formula v0.2 coupling:

- Time Mage should win by changing turn windows, not by becoming another damage caster;
- staff/fists are fallback routes, not the job identity;
- Haste/Slow/Swiftspell are timing modifiers and Quick is an action grant; both categories are
  high-risk global pieces;
- Reflect is routing/control, not a simple defensive buff;
- Gravity and Meteor must not replace Black Mage or Summoner as general magical damage plans.

### Action Skillset Goals

Time Magicks should make the player plan around time windows.

The player should ask:

- is it worth spending a turn now to create more turns later?
- can I slow or pin the correct enemy before it matters?
- can I safely route magic with Reflect?
- is Quick worth the action cost and loop risk?
- can Meteor land before the battlefield changes?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Haste` | focused tempo buff | Give one ally a short, visible turn-window advantage. | Requires T5/T2.1; cannot become permanent upkeep. |
| `Hasteja` | area tempo buff | Expensive party tempo setup for committed Time Mage play. | High CT/MP/JP; must not make Haste mandatory every fight. |
| `Slow` | focused tempo denial | Delay one enemy's turn plan and create a punish window. | Accuracy, duration, immunity, and T5 limits. |
| `Slowja` | area tempo denial | Riskier multi-target tempo pressure against clustered enemies. | High CT/MP/JP; must not hard-lock encounters. |
| `Stop` | hard tempo stop | High-impact denial against eligible targets. | Strong accuracy/immunity/duration limits; no boss/default answer. |
| `Immobilize` | position lock | Stop movement while allowing actions, creating ranged or terrain play. | Counters melee movement, not all threat; T4/T5 required. |
| `Float` | terrain/element utility | Bypass specific terrain or earth-style hazards without full mobility erasure. | Narrow utility; not a replacement for movement skills. |
| `Reflect` | magic routing | Redirect reflectable spells as both protection and risk. | Requires T4/T5/T8xSR composition; can backfire; not pure immunity. |
| `Quick` | action-window grant | Let an ally act sooner for a decisive setup or rescue. | Requires T10; no loops; high MP/JP/CT. |
| `Gravity` | proportional pressure | Damage high-HP targets without killing or replacing burst magic. | Immunity/boss rules; poor finisher; must not outclass Black Mage. |
| `Graviga` | area proportional pressure | Pressure clustered high-HP enemies with visible limits. | High MP/CT; immunity/boss rules; not a general AoE nuke. |
| `Meteor` | delayed area impact | Huge telegraphed payoff when the player controls timing and space. | Very slow, expensive, interruptible/predictable; Summoner comparison required. |

`Gravity` and `Graviga` are proportional HP pressure, not ordinary MA/Faith damage spells. Their
balance belongs to percent cap, immunity, boss, finishing, and encounter-role checks rather than the
normal F4 magic/physical coexistence ratio.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Critical: Quick` | emergency tempo reaction | Let a wounded Time Mage occasionally steal a recovery or escape window. | Requires T10; critical-only, no loops, no broad immunity. |

`Critical: Quick` is optional. If it causes loop risk, universal caster adoption, or opaque turn
swings, cut it or move it to a later job.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Swiftspell` | cast-time specialization | Let a committed caster trade support slot for faster slow actions. | High T2.1/F4/F5 risk; must not become mandatory for all casters. |
| `Temporal Focus` | Time Magicks specialist | Improve timing reliability or duration for Time Magicks only. | Should not accelerate every spell school. |

`Swiftspell` is the dangerous global piece. It can remain because FFT build crafting needs exciting
late supports, but it must be priced and bounded as a major support-slot choice, not a repair patch
for every delayed spell.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Teleport` | special positioning | Preserve iconic map-bending movement for committed builds. | Failure/range/cost or other limits; cannot erase terrain for everyone. |

Teleport-style movement requires T2.1, T4/T5 exposure checks, and map traversal review. It should
feel special, not like a universal late-game movement default.

### JP Progression

JP posture:

- Haste, Slow, and one terrain/control tool should be reachable early enough for identity;
- Stop, Reflect, Quick, Hasteja/Slowja, Meteor, Swiftspell, and Teleport require real investment;
- Swiftspell and Teleport should be expensive enough to preserve build planning;
- Time Mage should be useful before learning its global support/movement rewards.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Time Mage should remain early or mid enough to be a real controller route, while its best global
pieces should require commitment.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Time Mage donor patterns:

- White Mage borrows Time Magicks to improve healing windows;
- Black Mage or Summoner borrows Time Magicks for setup instead of raw damage;
- Archer or Dragoon benefits from Slow/Immobilize timing setups;
- caster spends support slot on Swiftspell for a deliberate fast-caster build;
- mobile controller spends movement slot on Teleport as a major map-control choice.

Unhealthy Time Mage donor patterns:

- most casters require Swiftspell;
- most late builds require Teleport;
- Haste becomes prebuff upkeep in most fights;
- Quick creates action loops or invalidates Speed;
- Stop/Slow become the safest way to solve hard enemies;
- Reflect turns magic matchups into non-decisions.

### Expected Strong Builds

- active Time Mage controlling CT windows with Haste/Slow/Stop;
- Time Mage with White Magicks or Black Magicks secondary for timed support or damage;
- caster with Swiftspell as a high-investment support plan;
- party using Slow/Immobilize to set up Archer, Dragoon, Meteor, or Summon timing.

### Expected Weaknesses

- cloth durability;
- low direct damage outside Gravity/Meteor;
- MP and CT pressure;
- status immunity and boss resistance;
- Silence and anti-caster pressure;
- high value only when timing windows are exploited.

### Expected Counters

- fast pressure before setup resolves;
- Silence or MP denial;
- status immunity or duration reduction;
- Reflect routing risk;
- spread enemies that deny area setup;
- builds that can act effectively while Immobilized.

### Ramza / Unique-Job Interaction

Ramza may become a knight/mage hybrid, but he should not become the best tempo controller. If Ramza
gets action-window tools, they should trade against his physical, leadership, or support choices.

## Mystic

Job: Mystic
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: spiritual/status controller with Mystic Arts, Faith/Brave manipulation, drain,
Silence/sleep/disable-style pressure, Undead interaction, and broad MA-crush equipment access.

Vanilla problems:

- many status spells are either low-value or oppressive depending on hit rate and immunity;
- Faith manipulation can dominate the entire magic system if it is too permanent or too efficient;
- drain and MP effects can become hidden resource engines;
- broad rod/staff/pole/book crush access can blur caster and anti-plate identities;
- Mystic can feel like a grab bag instead of a clear spiritual controller.

Accepted high-level role: spiritual/status controller with MA-crush access.

Primary role: `controller`

Secondary tags: `Faith`, `crush`

Growth profile: `magical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `pole`, `book`, `staff`, `rod`, `fists`.

Armor class as target: `cloth`.

Supported damage modes: `crush`, `magic`.

Formula v0.2 coupling:

- Mystic's unusually broad MA-crush weapon access is part of its identity, but must not eclipse Monk
  as unarmed crush or Geomancer as physical terrain hybrid;
- the moat against Monk is the stat axis: Mystic's staff, rod, pole, and book routes are
  `ma_wp`/`pampa_wp` crush, while Monk fists are `br_pa_pa` Brave/PA crush;
- Faith manipulation affects every caster and therefore carries F4/F5 risk;
- status and hard control require T4/T5/T8 proof;
- drain, MP damage, Mana Shield, Manafont, or Halve MP-like effects require T9;
- Mystic should win through spiritual state and matchup preparation, not raw damage.

### Action Skillset Goals

Mystic Arts should make status and spiritual state feel like deliberate battlefield work.

The player should ask:

- should I lower a target's Faith to weaken magic, or raise an ally's Faith to empower it?
- is a status worth the hit chance and immunity risk?
- is drain better than direct healing or damage here?
- can I set up future Necromancer or undead interactions without stealing that late job's identity?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Chant` | spiritual focus | Low-cost Mystic setup or minor self/ally focus if a record is needed. | Must not become generic stat stacking. |
| `Umbra` | perception/status pressure | Blind, shadow, or accuracy-style disruption against eligible targets. | T4 accuracy/evasion rows; not broad defense. |
| `Empowerment` | MP drain | Attack or siphon MP to pressure casters and fuel resource play. | Requires T9; must not create infinite spell loops. |
| `Invigoration` | HP drain | Spiritual life drain that mixes damage and self/ally recovery. | Requires T3/T3xT5/F4; cannot replace White Mage or Black Mage. |
| `Belief` | Faith increase | Raise ally or target magic receptivity for a planned window. | Battle-scoped or tightly bounded; F4/F5 required. |
| `Disbelief` | Faith reduction | Weaken enemy magic or protect an ally from Faith-based pressure. | Battle-scoped or tightly bounded; cannot invalidate magic. |
| `Corruption` | undead/spiritual inversion | Apply or exploit undead-adjacent state for specific interactions. | Must not preempt Necromancer; undead rows required. |
| `Quiescence` | anti-caster silence | Shut down spell-like actions against eligible targets. | T4/T5 immunity and duration limits. |
| `Fervor` | morale pressure | Push a target toward risky aggression or Brave/morale distortion. | Must not become broad AI control; T8-sensitive if behavior changes. |
| `Trepidation` | Brave reduction | Reduce physical confidence/reaction reliability in a bounded window. | Avoid permanent Brave grief; Brave formula stress rows required. |
| `Delirium` | confusion/control | Create risky misplay pressure against eligible targets. | T8-sensitive; cannot be reliable hard control. |
| `Harmony` | spiritual cleanup | Clear or normalize a defined set of spiritual/status problems. | Does not replace Esuna as general status cleanup. |
| `Hesitation` | action constraint | Disable or constrain selected actions without full shutdown. | T4/T5 immunity and duration limits. |
| `Repose` | sleep/rest control | Temporarily remove a target with damage-break counterplay. | Damage should break or counter it; not boss/default answer. |
| `Induration` | stone/hard seal | Rare high-risk hard control or petrification-style seal. | Very strong immunity/accuracy/CT limits; no generic encounter solution. |

Exact status mapping can follow the vanilla records where feasible, but the design priority is
clarity: each Mystic status needs a distinct reason to use it and a distinct reason not to.

Faith and Brave changes should be battle-scoped unless a later progression document deliberately
accepts permanent morale/religion manipulation. Permanent stat grief would fight the user's growth
policy and could make build planning worse, not better.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Absorb MP` | anti-caster resource reaction | Recover or deny MP when hit by eligible magical pressure. | Requires T9; no infinite MP loop. |
| `Mana Shield` | spiritual barrier | Spend MP to soften a dangerous hit. | Requires T9/T6xPS; cannot become broad immunity. |

If ability slots require one reaction, prefer `Mana Shield` only if MP cost is real and visible.
Otherwise prefer `Absorb MP` as a narrower anti-caster identity.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Halve MP` | resource specialization | Let a committed caster spend support slot for more spell endurance. | Requires T9/T2.1; cannot become mandatory for every caster. |
| `Magick Defense Boost` | anti-magic posture | Improve survival against magical pressure. | Requires T6xPS/F4; should not stack into magic immunity. |
| `Mystic Focus` | Mystic Arts specialist | Improve status reliability or spiritual actions only. | Should not boost every status and magic action in the game. |

`Halve MP` and `Magick Defense Boost` are dangerous global pieces. They can exist only if T2.1 and
the relevant formula/resource gates prove they create real build choices rather than mandatory caster
taxes.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Manafont` | resource movement | Recover a small amount of MP through movement or terrain flow. | Requires T9; must not erase MP attrition. |

### JP Progression

JP posture:

- one anti-caster tool and one soft status should be reachable early;
- drain and Faith manipulation should require mid investment;
- hard control such as sleep/stone-style effects should be expensive and narrow;
- Halve MP, Mana Shield, Magick Defense Boost, and Manafont require commitment because they can
  reshape the caster economy;
- Mystic should be playable as active controller without needing its global resource pieces.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Mystic should remain a mid controller route, not a late replacement. Necromancer later owns the
deeper undead/dark-magic fantasy; Mystic only opens spiritual and undead-adjacent hooks.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Mystic donor patterns:

- White Mage or Black Mage borrows Mystic Arts for Faith setup or anti-caster tools;
- Time Mage borrows Mystic status to create tempo-control combos at MP/accuracy cost;
- Mystic borrows White or Black Magicks after setting Faith windows;
- caster spends support slot on Halve MP for a clear endurance build;
- anti-magic unit uses Mana Shield or Magick Defense Boost with visible resource or support cost.

Unhealthy Mystic donor patterns:

- every caster wants Halve MP;
- every magic-heavy party requires Belief/Disbelief setup;
- Mana Shield plus MP economy creates broad damage immunity;
- Induration/Repose/Delirium become the safest answer to hard enemies;
- Mystic's broad crush access makes Monk, Geomancer, or caster weapon choices irrelevant.

### Expected Strong Builds

- active Mystic controlling Faith and status windows;
- Mystic with Black Magicks for setup into damage;
- Mystic with Time Magicks for layered control;
- anti-caster support build using Silence/Disbelief/MP pressure;
- endurance caster using Halve MP or Manafont after T9 proves it is bounded.

### Expected Weaknesses

- cloth durability;
- lower direct damage than Black Mage or Summoner;
- status immunity and boss resistance;
- accuracy, Faith, and CT constraints;
- MP pressure before T9-supported economy comes online;
- weaker value when enemies are immune, spread, or already low threat.

### Expected Counters

- status immunity or short duration;
- low-Faith or anti-magic targets when Mystic needs Faith formulas;
- fast physical rushdown;
- Silence or MP denial;
- enemies that do not care about magic, Faith, Brave, or status;
- undead rules that invert expected drain/healing outcomes.

### Ramza / Unique-Job Interaction

Ramza may become a knight/mage hybrid, but he should not outclass Time Mage's CT control or Mystic's
spiritual/status control. If Ramza gains Faith, tempo, or status tools, they should be narrower or
trade against his hybrid action economy.

## Shared Scenario/Check Plan

Required later:

- `J-TM-EARLY-SELF`: Time Mage with Haste/Slow and one utility control tool online.
- `J-TM-MID-TEMPO`: Time Mage creates a meaningful timing window without mandatory Haste upkeep.
- `J-TM-LATE-QUICK`: Quick creates rescue/setup value without action loops.
- `J-TM-STRESS-HASTE-UPKEEP`: Haste/Hasteja do not become mandatory prebuff loops.
- `J-TM-STRESS-QUICK-LOOP`: Quick, Critical: Quick, and Swiftspell cannot create recursive turns.
- `J-TM-STRESS-REFLECT`: Reflect routing includes backfire and target-selection risk.
- `J-TM-STRESS-TELEPORT`: Teleport is strong but not the default movement for most builds.
- `J-TM-STRESS-METEOR`: Meteor competes with Black Mage and Summoner without replacing either.
- `J-MY-EARLY-SELF`: Mystic has one useful soft status and one anti-caster tool.
- `J-MY-MID-FAITH`: Belief/Disbelief create tactical windows without invalidating magic ecology.
- `J-MY-MID-DRAIN`: Empowerment/Invigoration create pressure without infinite resource loops.
- `J-MY-LATE-CONTROL`: hard statuses remain situational and counterable.
- `J-MY-STRESS-BREADTH`: Mystic Arts as a secondary does not win by always having some relevant
  status into every matchup.
- `J-MY-STRESS-UNDEAD`: Corruption, drain, Death-like interactions, and healing inversion rows work.
- `J-MY-STRESS-MA-CRUSH`: Mystic MA-crush, such as rod/pole/book/staff, is compared against Monk
  PA/Brave-crush fists into plate to prove distinct lanes.
- `J-MY-STRESS-DISBELIEF-COEXISTENCE`: Disbelief-active magic damage does not push magic past the
  coexistence ceiling.
- `J-CONTROLLER-COUNTER`: Silence, MP pressure, immunity, boss rows, and rushdown counters work.
- `M-SECONDARY-COUNT`: Time Magicks and Mystic Arts secondary incidence.
- `M-RSM-COUNT-LATE`: Critical: Quick, Absorb MP, Mana Shield, Swiftspell, Halve MP, Teleport,
  Manafont, Magick Defense Boost, and Mystic/Temporal specialist supports.
- `I-TIMING`: Haste, Slow, Stop, Quick, Meteor, Reflect, and delayed control windows.
- `I-RESOURCE`: MP drain, Mana Shield, Manafont, Halve MP, and repeated spell availability.

Conditional:

- T5 for speed-state, CT, cast-time, Haste, Slow, Stop, Meteor, duration, and delayed resolution
  behavior.
- T10 for Quick, Critical: Quick, action grants, turn refunds, and anti-recursion behavior.
- T4 for status accuracy, evasion suppression, line, height, Silence, Sleep, Toad-like, Stop-like,
  and Stone-like states.
- T8 for Reflect routing, Confuse/Delirium, Fervor, allegiance/AI-like behavior, Invisible-like
  interactions, and any targeting-sensitive control.
- T8xSR for Reflect as a concrete spell-routing composition gate.
- T9 for MP drain, MP recovery, Mana Shield, Manafont, Halve MP, and resource loops.
- T3/T3xT5 for Invigoration, drain healing, Reraise-like effects, Regen/Poison-adjacent attrition,
  or sustain timing.
- T6xPS/F4 for Magick Defense Boost, Mana Shield mitigation, Shell/Reflect-adjacent defense, or any
  mitigation stack.
- Gate F4/F5 if Faith manipulation, Disbelief-active damage, Swiftspell, Quick, MP economy, MA
  multipliers, spell power, or broad magic supports drift magic/physical coexistence.

## Formula Re-Sim Requirement

No immediate Gate F5 re-sim is required for this document.

The concrete data pass must re-evaluate this if it:

- changes Speed, CT, action grants, cast time, or duration values;
- changes Faith or Brave manipulation;
- changes MA, MP, staff/rod/pole/book access, or MA-crush output;
- changes spell power, Gravity/Meteor damage, or magic coexistence;
- changes MP economy, MP drain, MP recovery, Halve MP, Manafont, or Mana Shield;
- changes status accuracy, status immunity, hard-control duration, or boss rows;
- changes Reflect routing, Teleport movement, or targeting behavior;
- changes mitigation stacking or magic-defense support behavior.

## Implementation Assumptions

- Data modding can create or repurpose ability records.
- Skill names are placeholders where they are not vanilla names.
- Preserve recognizable FFT controller flavor: Haste, Slow, Stop, Reflect, Quick, Gravity, Meteor,
  Swiftspell, Teleport, Faith manipulation, drain, Silence/control statuses, Mana Shield, Halve MP,
  and Manafont should remain recognizable unless proof shows a specific effect cannot be bounded.
- Exact values must be decided after validation, not inside this V1 proposal.

## Open Proof Needs

- Exact Haste/Slow/Stop duration and CT behavior.
- Exact T10 action-grant and anti-recursion model before Quick-class values.
- Exact Swiftspell, Teleport, and Critical: Quick boundaries.
- Exact Reflect routing after T8xSR exists.
- Exact Gravity/Graviga/Meteor damage, CT, area, and immunity rules.
- Exact Mystic status mappings and duration/accuracy profiles.
- Whether Faith/Brave changes can be safely battle-scoped in data.
- Exact MP economy values after T9 exists.
- Whether Mana Shield, Magick Defense Boost, and Halve MP can exist without mandatory incidence.
- Whether Mystic's MA-crush equipment breadth needs later equipment-access adjustment.

## Claude Review Notes

Claude conditionally accepted this proposal on 2026-06-21 after requiring one hard gate change:
Quick-class action grants cannot rely on T5 and must depend on a named turn-grant/action-economy
model.

Recommended review additions also applied:

- Mystic MA-crush is distinguished from Monk PA/Brave-crush by stat axis;
- Disbelief-active magic coexistence row added;
- Reflect labeled as a future T8xSR spell-routing composition gate;
- Gravity/Graviga labeled as proportional HP pressure outside the normal MA/Faith damage band;
- Mystic Arts breadth added as a measured secondary-incidence risk.
