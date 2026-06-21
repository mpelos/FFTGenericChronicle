# White Mage And Black Mage V1 Proposal

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
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for White Mage and Black Mage.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, spell powers, MP
costs, hit rates, CT values, status rates, damage multipliers, equipment records, stat multipliers,
or prerequisites.

White Mage and Black Mage are the first full caster pair. Their design must protect the magic half
of FFT while preserving the mod's broader formula goal: many viable equipment and job identities,
not one universal answer.

Claude review verdict: conditional acceptance on 2026-06-21, with required edits applied in this
version.

## Group Thesis

White Mage and Black Mage should remain the cleanest caster foundations.

- White Mage is the delayed, Faith-linked recovery and protection job.
- Black Mage is the delayed, Faith-linked elemental and high-output damage job.

They should feel stronger and more expressive than vanilla in real play, but not by erasing
Chemist, Monk, Time Mage, Mystic, Summoner, or physical weapon families.

Both jobs should teach the same caster truth from opposite directions:

- magic is powerful when the player plans CT, Faith, MP, range, and target profile;
- magic is punishable when the player ignores timing, Silence, Shell, Reflect, MP, and fragile cloth
  durability.

## Shared Caster Notes

The accepted v0.2 magic model is:

```text
K * MA * max(faith_factor_floor, (casterFaith / 100) * (targetFaith / 100))
faith_factor_floor = 0.60
```

This proposal does not change that model.

Design implications:

- high Faith can be a real caster build plan;
- low Faith cannot invalidate the whole magic system because the floor still exists;
- Shell remains a protected stress engine and must stay meaningful;
- CT delay is not flavor, it is a balancing lever;
- MP economy is a real axis and cannot be hidden inside T3/T3xT5 healing checks;
- caster cloth fragility should remain part of the cost of high-impact magic.

Concrete values must run the relevant accepted gates:

- T3 and T3xT5 for healing, revive, regen, and recovery races;
- T5 for delayed spell timing and unsafe target windows;
- T4 for spell accuracy, evasion, line, height, and status delivery;
- T6 or T6xT7 if a buff or spell changes damage response, armor exposure, or guard layers;
- `T6xPS` mitigation-stacking composition for numeric Protect, Shell, Wall, or similar defensive
  package values;
- T9 resource/MP economy for skills that alter MP recovery, efficiency, or spell availability;
- Gate F4/F5 if spell power, Faith access, Shell behavior, MA multipliers, or MP economy drift the
  magic/physical coexistence band.

`T6xPS` is a hard dependency for concrete Protect, Shell, and Wall values. It must exercise the full
operation order:

```text
armor type_response * protect_shell * element * zodiac, under the shared [0.25, 2.50] clamp
```

The gate must prove that realistic Protect plus plate, Shell plus the magic Faith floor, and Wall as
the combined Protect/Shell package do not pile at the clamp floor or manufacture de-facto immunity.

## White Mage

Job: White Mage
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: healing and protection caster with Cure, Raise, Reraise, Regen, Protect, Shell, Wall,
Esuna, and Holy.

Vanilla problems:

- delayed healing can lose to immediate items unless its payoff and timing are clearly worthwhile;
- Raise/Arise/Reraise can make death handling swingy if they are too reliable or too late;
- Protect/Shell/Wall can become invisible passive upkeep instead of tactical choices;
- Holy risks turning White Mage into a better general nuker than Black Mage if not bounded.

Accepted high-level role: Faith-linked caster support.

Primary role: `caster-support`

Secondary tags: `Faith`, `staff`

Growth profile: `magical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `staff`, `fists`.

Armor class as target: `cloth`.

Supported damage modes: `crush`, `magic`.

Formula v0.2 coupling:

- White Mage should win through delayed recovery, protection, and holy pressure;
- staff/fists are fallback routes, not the job identity;
- healing and revive must be compared against Chemist certainty, Monk frontline sustain, and White
  Mage delayed throughput as a three-way recovery ecosystem;
- Shell and Protect must stay legible mitigation choices without creating broad immunity;
- Holy can be strong, but it must not make Black Mage's damage identity redundant.

### Action Skillset Goals

White Magicks should create timing and protection decisions.

The player should ask:

- can I afford a delayed heal, or do I need an item now?
- is prevention better than recovery?
- is this target worth a revive action?
- do I need broad protection, status cleanup, or a rare damage spell?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Cure` line | delayed HP recovery | Restore more HP than low-tier items when timing allows. | CT, MP, Faith, range, overheal, and interruption risk; must pass T3/T3xT5. |
| `Raise` | basic delayed revive | Recover a KO ally when death-clock timing and exposure allow it. | Success/timing/HP return must not erase Phoenix Down reliability or Arise investment. |
| `Arise` | premium revive | Expensive stronger revive for committed White Mage builds. | High MP/JP/CT pressure; must not trivialize death-clock races. |
| `Reraise` | preemptive safety | Protect a key unit before a likely KO. | Expensive, narrow, and timing-sensitive; cannot become permanent upkeep. |
| `Regen` | attrition prevention | Reward early casting before repeated chip damage. | Must compete with, not replace, direct healing; belongs to T3/T5 attrition rows. |
| `Protect` | physical mitigation | Reduce incoming physical pressure before danger arrives. | Requires T6xPS; must be visible, duration-bounded, and weaker than immunity. |
| `Shell` | magical mitigation | Keep magical pressure and Faith/status formulas answerable. | Requires T6xPS and F4 Shell-on rows; protected F3 engine. |
| `Wall` | focused protection | Apply the combined Protect/Shell defensive package to one target. | Requires T6xPS; must not turn into cheap universal upkeep. |
| `Esuna` | status cleanup | Remove a defined set of harmful statuses. | Cure is reactive; prevention and dedicated status jobs still matter. |
| `Holy` | focused holy damage | Give White Mage a high-investment offensive outlet. | Single-target or narrow use; should not outclass Black Mage's general damage plan. |

Revive lanes must stay legible across the whole ecosystem: Phoenix Down is cheap/certain/item-based,
Monk Revive is risky/frontline/adjacent, and White Mage Raise/Arise are delayed, Faith-linked, and
higher-throughput. T3xT5 revive-race rows are the shared gate for all three.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Divine Grace` | emergency support reaction | Small chance to improve survival or protection when the White Mage is pressured. | Must be narrow, low-incidence, and not become the default caster reaction. |

White Mage does not need a dominant reaction identity. Its active kit and secondary value are already
large. Any recovery reaction must pass T3/T3xT5 and mandatory-piece checks.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Arcane Ward` | support-magic focus | Improve healing/protection reliability or efficiency without boosting all magic damage. | Must not compress White Mage plus Black Mage support into one slot. |
| `Faithful Casting` | White Magicks specialist | Reward committed Faith-linked support builds. | Should help White Magicks specifically, not every caster action. |

Do not make White Mage the default owner of a broad magic damage support in this proposal. That role
belongs to a later global support review or another caster if it survives T2.1 incidence and F4.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Sanctuary Step` | support positioning | Help a healer reach safe support positions or maintain formation. | No broad teleport/flying identity; must not erase cloth vulnerability. |

### JP Progression

JP posture:

- basic Cure and basic protection should be reachable early;
- Esuna should arrive before status-heavy encounters would otherwise feel unfair;
- Raise should be accessible before Arise/Reraise;
- Protect/Shell upgrades and Wall should require real support investment;
- Holy should be expensive enough to feel like an offensive commitment, not a free capstone.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

White Mage should remain early enough to be a foundational support route. Its best protection,
revive, and Holy tools should still require commitment.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy White Mage donor patterns:

- physical or hybrid unit borrows White Magicks for delayed support at MP/Faith/CT cost;
- Chemist active job uses White Magicks when it wants stronger but less certain recovery;
- Time Mage or Mystic borrows White Magicks to become a broader support caster;
- White Mage borrows Items for emergency immediate recovery while keeping magic as its identity.

Unhealthy White Mage donor patterns:

- every party prefers White Magicks over Items for all recovery situations;
- every caster equips the same White Mage support;
- Protect/Shell/Wall become mandatory prebuff upkeep;
- Holy becomes the best generic single-target magical damage spell.

### Expected Strong Builds

- active White Mage with high Faith and support positioning;
- White Mage with Items secondary for emergency reliability;
- Time Mage or Mystic with White Magicks secondary for support specialization;
- physical unit with White Magicks secondary only when Faith/MP investment makes sense.

### Expected Weaknesses

- cloth durability;
- CT delay and interrupt windows;
- MP attrition;
- Silence, Faith manipulation, Shell, Reflect, and status pressure;
- weaker direct offense outside Holy;
- lower reliability than items in immediate lethal windows.

### Expected Counters

- burst damage that beats delayed healing;
- Silence or MP pressure;
- Reflect/Shell or anti-magic tools;
- forced movement that breaks support range;
- low-Faith or status-resistant targets where relevant;
- enemies that punish fragile backline positioning.

### Ramza / Unique-Job Interaction

Ramza may become a knight/mage hybrid, but he should not make White Mage obsolete as the clean
support caster. Ramza can borrow or echo healing/protection, but White Mage should remain better at
dedicated recovery and protection throughput.

## Black Mage

Job: Black Mage
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: elemental and high-power magical damage caster with Fire, Thunder, Blizzard, Poison,
Toad, Death, and Flare.

Vanilla problems:

- elemental tiers can collapse into a simple bigger-number ladder;
- status and instant-KO spells can be either useless or oppressive depending on accuracy/immunity;
- Flare can crowd out elemental identity if it is always the best answer;
- caster damage must coexist with physical stress engines instead of replacing weapon planning.

Accepted high-level role: Faith-linked caster offense.

Primary role: `caster-offense`

Secondary tags: `Faith`, `rod`

Growth profile: `magical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `rod`, `fists`.

Armor class as target: `cloth`.

Supported damage modes: `crush`, `magic`.

Formula v0.2 coupling:

- Black Mage should be the early and midgame home of direct spell pressure;
- rods/fists are fallback routes, not the job identity;
- magic damage must stay bounded by F4 magic/physical coexistence;
- elements should create target and encounter decisions, not just color variants;
- Shell, Faith, Reflect, CT delay, MP, and status immunity are valid counters.

### Action Skillset Goals

Black Magicks should turn spell selection into real target selection.

The player should ask:

- is the target weak to an element or just vulnerable to magic?
- can I land the spell before the target acts or moves?
- is status pressure better than damage?
- is a large MP spell worth the payoff now?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Fire` line | elemental damage | Reliable baseline fire pressure and Oil synergy where relevant. | CT/MP/Faith/Shell/Reflect counters; no universal best element. |
| `Thunder` line | elemental damage | Lightning pressure through elemental affinity, target profile, or equipment hooks. | Same caster counters; should not just be Fire with another name. |
| `Blizzard` line | elemental damage | Ice pressure through elemental affinity, target profile, or terrain hooks. | Same caster counters; should not just be Fire with another name. |
| `Poison` | attrition status | Pressure durable targets through timed HP loss or anti-tank attrition. | Must pass status accuracy, immunity, undead, T3/T5 duration, and boss-resistance checks. |
| `Toad` | hard transformation control | Rare high-impact control against eligible targets. | Strong accuracy/immunity/CT/undead limits; cannot be a broad shutdown button. |
| `Death` | lethal risk spell | High-risk instant-KO fantasy against non-immune targets. | Accuracy, immunity, undead, CT, Faith, and encounter rules must keep it situational. |
| `Flare` | non-elemental burst | Expensive high-output spell when elements are wrong or resisted. | High MP/CT/JP; must not become the default answer to every target. |

Elemental tiers should not be only `Fire`, `Fira`, `Firaga`, `Firaja` as four raw numbers. The
implementation pass should decide whether higher tiers differ by area, CT, MP, range, secondary
effect, or target profile. If the data format only supports simple tiers cleanly, the tier ladder
must still be tuned so lower tiers remain useful through speed, MP efficiency, or overkill control.

The default mechanism for making element selection real should be elemental affinity: weak, resist,
absorb, halve, and Oil-style setup where available. That uses existing FFT vocabulary instead of
requiring a new data feature. Elemental tiers can still vary by CT, area, MP, or range, but affinity
is the primary identity engine.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Arcane Backlash` | risky caster retaliation | Punish a narrow class of nearby magical or direct attacks. | Must not answer all physical pressure; caster fragility should remain real. |

Black Mage reaction identity should be optional. The job's core value is action pressure, not
defense.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Elemental Focus` | black-magic specialization | Improve elemental spell planning without boosting every magical action. | Must not become the default support for all casters. |
| `Arcane Strength` | broad magic damage candidate | Possible late support if the ecosystem needs a classic magic-damage booster. | High T2/F4 risk; should be deferred or sharply bounded. |

The safer V1 direction is `Elemental Focus`, not a broad magic damage support. A generic magic
damage booster can exist only if T2.1 incidence and F4 coexistence prove it does not become
mandatory.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Ley Step` | caster positioning | Support spell line/range setup without turning Black Mage into a mobile skirmisher. | Must not erase CT, range, and cloth-fragility counters. |

### JP Progression

JP posture:

- first-tier elemental spells should be cheap and immediately useful;
- mid-tier elements should create practical upgrades without invalidating low-tier efficiency;
- Poison/Toad/Death should be opt-in control investments, not required damage progression;
- Flare should be expensive and late enough to feel like a capstone;
- any broad magic support should be late and expensive if it survives at all.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Black Mage should remain early enough to be the clear offensive caster foundation. Its best burst
and control spells should require commitment.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Black Mage donor patterns:

- White Mage, Time Mage, or Mystic borrows Black Magicks for offense at MP/Faith/CT cost;
- Black Mage borrows Time or Mystic tools to improve setup or control instead of raw damage;
- hybrid Ramza or Geomancer borrows a limited Black Magicks package when MA/Faith investment
  supports it;
- active Black Mage chooses between elements, status, and Flare based on target and timing.

Unhealthy Black Mage donor patterns:

- Black Magicks becomes the best secondary for most casters regardless of target;
- Flare replaces elemental choice;
- broad magic damage support becomes mandatory for every caster;
- Death/Toad becomes a universal answer to hard enemies;
- magic damage makes physical weapon-family planning feel optional.

### Expected Strong Builds

- active Black Mage with high Faith, rods, and elemental planning;
- Black Mage with Time Mage or Mystic secondary for setup/control;
- White Mage or Mystic with Black Magicks secondary for offensive coverage;
- high-MA build using elemental efficiency instead of always spending on Flare.

### Expected Weaknesses

- cloth durability;
- CT delay and target movement;
- MP attrition;
- Silence, Shell, Reflect, Faith manipulation, and elemental resistance;
- status/instant-KO immunity;
- lower value when enemies spread out or force movement.

### Expected Counters

- anti-magic pressure and Shell;
- Reflect or magic redirection;
- silence/MP denial;
- fast enemies that exploit CT windows;
- low-Faith or resistant targets where relevant;
- physical rushdown against cloth casters.

### Ramza / Unique-Job Interaction

Ramza may become a knight/mage hybrid, but he should not become the best Black Mage. If his hybrid
kit includes offensive magic, it should trade against his physical, leadership, or support options.

## Shared Scenario/Check Plan

Required later:

- `J-WM-EARLY-SELF`: White Mage with basic Cure and Protect/Shell role online.
- `J-WM-MID-PARTY`: White Mage compared against Chemist and Monk recovery in a normal party.
- `J-WM-LATE-SUPPORT`: White Mage remains the best dedicated delayed recovery/protection caster.
- `J-WM-STRESS-LETHAL`: item versus delayed Cure/Raise in lethal and nonlethal timing windows.
- `J-WM-STRESS-UPKEEP`: Protect/Shell/Wall do not become mandatory prebuff loops.
- `J-WM-STRESS-MITIGATION-STACK`: Protect plus plate, Shell plus faith-floor magic, and Wall as both
  statuses do not pile at the shared clamp floor.
- `J-BM-EARLY-SELF`: Black Mage with first-tier elements as useful offense.
- `J-BM-MID-ELEMENT`: elemental selection matters against at least two target profiles.
- `J-BM-LATE-BURST`: Flare is valuable but not a universal replacement for elements.
- `J-BM-STRESS-STATUS`: Poison, Toad, and Death respect accuracy, CT, immunity, and boss-like rows.
- `J-BM-STRESS-UNDEAD`: Death, Poison, Toad, and elemental magic behave correctly against undead or
  undead-like targets.
- `J-BM-STRESS-SHELL-ON`: magic/physical coexistence is checked with Shell active, not only against
  unmitigated magic.
- `J-CASTER-COUNTER`: Silence, Shell, Reflect, MP pressure, low Faith, and CT movement counters work.
- `M-SECONDARY-COUNT`: White Magicks and Black Magicks secondary incidence.
- `M-RSM-COUNT-LATE`: caster reactions, caster supports, and caster movement skills.
- `I-ATTRITION`: Regen, Poison, MP attrition, and repeated spell use.
- `I-TIMING`: delayed heal, delayed damage, revive, and target movement rows.

Conditional:

- T3 and T3xT5 for Cure, Raise, Arise, Reraise, Regen, and any recovery reaction.
- T5 for all delayed spell CT, target movement, and revive/death-clock races.
- T4 for spell accuracy, status delivery, evasion, line, height, Reflect/Shell hit behavior, and
  interruption assumptions.
- T6/T6xT7 if Protect, Shell, Wall, Poison, or any spell changes response, mitigation, armor
  exposure, or party follow-up damage.
- T6xPS for numeric Protect, Shell, Wall, or similar mitigation stacks across armor response,
  protect/shell, element, zodiac, and the shared clamp.
- T9 for MP recovery, MP efficiency, spell-availability loops, or resource-economy supports.
- Gate F4/F5 if spell power, Faith access, Shell multiplier, MP economy, MA multipliers, or magic
  support skills drift the magic/physical coexistence target.

## Formula Re-Sim Requirement

No immediate Gate F5 re-sim is required for this document.

The concrete data pass must re-evaluate this if it:

- changes MA or MP multipliers;
- changes Faith floor, Faith access, Faith manipulation, or Faith scaling;
- changes Shell, Protect, Wall, or mitigation behavior;
- changes spell power, CT, MP cost, range, area, or status rate;
- changes broad magic damage support;
- changes healing/revive values or timing;
- changes resource economy, MP recovery, MP efficiency, or spell availability;
- changes Reflect, Silence, or status-immunity assumptions.

## Implementation Assumptions

- Data modding can create or repurpose ability records.
- Skill names are placeholders where they are not vanilla names.
- Preserve recognizable FFT caster flavor: Cure, Raise, Protect, Shell, Esuna, Holy, elements,
  Poison, Toad, Death, and Flare should remain recognizable unless a later proof shows a specific
  effect cannot be bounded.
- Exact values must be decided after validation, not inside this V1 proposal.

## Open Proof Needs

- Exact spell power, MP, CT, range, and area values.
- Exact Protect/Shell/Wall values after the T6xPS mitigation-stacking gate exists.
- Exact MP economy values after T9 exists.
- Whether broad magic damage support can exist without mandatory incidence.
- How far elemental affinity, Oil-style setup, CT, area, MP, and range can carry distinct elemental
  tactical hooks inside feasible data limits.
- Whether Death and Toad should remain Black Mage actions after immunity and boss-row testing.
- How Reflect should interact with redesigned targeting and status delivery.

## Claude Review Notes

Claude conditionally accepted this proposal on 2026-06-21 after requiring one hard gate change:
Protect, Shell, and Wall numeric values must depend on a named mitigation-stacking composition gate.

Recommended review additions also applied:

- T9 resource/MP economy added to the roadmap;
- elemental affinity named as Black Mage's primary element-identity engine;
- undead rows added for Death, Poison, Toad, and relevant magic interactions;
- White Mage revive positioned against Phoenix Down and Monk Revive as a three-way ecosystem;
- Shell-on magic coexistence row added to prevent hidden magic over-nerfing.
