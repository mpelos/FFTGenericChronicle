# Cross-Job Build Principles V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.json`

## Purpose

This document defines how Generic Chronicle should preserve the best part of FFT build crafting:
combining jobs, skills, equipment, and stats into something that feels strong and personal.

It does not define exact job kits yet. It defines what kind of strong build is healthy, what kind
of strong build is unhealthy, and how later job proposals should be judged.

This document should be approved before job role verdicts are locked.

## Core Thesis

Generic Chronicle should not flatten FFT into perfect parity.

The player should be able to build powerful characters. Some combinations should feel clever,
late-game, specialized, and even a little unfair when they are used in the right situation.

The design problem is not power. The design problem is monotony.

A build becomes unhealthy when it is too universal, too automatic, too cheap, or too disconnected
from map, target, equipment, CT, MP, Brave, Faith, armor, weapon family, and positioning.

## Build Definition

A build is the full combat package of a unit:

```text
active job
+ secondary skillset
+ reaction skill
+ support skill
+ movement skill
+ equipment access
+ current job multipliers
+ permanent growth profile
+ Brave/Faith assumptions
```

Minor equipment swaps do not create a distinct build for balance counting unless the swap changes
the tactical identity, such as switching from a sword build to a gun build or from plate armor to
robes.

## Slot Responsibilities

### Active Job

The active job is the unit's current tactical shell.

It should define:

- current stats and multipliers;
- equipment access;
- map presence;
- default durability profile;
- primary action identity;
- what the unit naturally wants to do before cross-job additions.

The active job should matter even in optimized late-game builds. If the best version of a build
does not care what active job it uses, the build is probably being carried by a global piece that
is too strong or too generic.

### Secondary Skillset

The secondary skillset is the main cross-job expression slot.

Healthy secondary usage:

- expands the active job into a hybrid plan;
- covers a weakness at real opportunity cost;
- creates a deliberate combo;
- gives the player a new tactical line without deleting the active job's identity.

Unhealthy secondary usage:

- makes the active job irrelevant;
- becomes the same default secondary for most builds;
- turns every job into the same caster, healer, or damage engine;
- wins without caring about weapon, armor, map, CT, MP, range, or target.

### Reaction Skill

Reactions should create defensive identity, retaliation identity, or tactical risk management.

They should not create practical immunity to broad categories of play.

Healthy reactions:

- are strong against a defined threat;
- have counters;
- depend on positioning, damage type, Brave, status, equipment, or target choice;
- make the opponent change tactics.

Unhealthy reactions:

- make the unit nearly untouchable against most attacks;
- stack with evasion/equipment into practical immunity;
- are correct on almost every build;
- turn enemy turns into non-decisions.

### Support Skill

Support skills are the clearest long-term build reward slot.

They can be powerful, but the single support slot must remain a meaningful opportunity cost.

Healthy supports:

- define a build direction;
- unlock a weapon/equipment strategy;
- strengthen a job identity;
- enable a late-game combo at meaningful opportunity cost.

Unhealthy supports:

- become mandatory for most serious builds;
- compress multiple identities into one slot;
- make weapon or armor restrictions meaningless without a real cost;
- repair a broken baseline instead of enhancing a viable plan.

### Movement Skill

Movement skills are map-control tools, not just stat sticks.

Healthy movement:

- changes how a unit approaches terrain, height, threat, or tempo;
- has a reason to be chosen over other movement skills;
- supports a build identity.

Unhealthy movement:

- erases terrain and positioning as FFT concepts;
- becomes the default for nearly every late-game unit;
- gives perfect mobility without risk, failure, cost, or limitation.

### Equipment Access

Equipment access is part of build identity and is tightly coupled to the formula model.

Weapon access determines which physical damage modes a job can field:

- `swing`;
- `thrust`;
- `crush`;
- `missile`.

Armor access determines what kind of target the job becomes:

- `plate`;
- `mail`;
- `leather`;
- `cloth`.

Later role-map work must reconcile each job with `work/sim-inputs-v0.2.json`. The v0.2 formula
identity only works if the job ecosystem actually gives players access to the right damage modes
and target profiles.

Examples:

- plate must create a real reason to bring crush or impact tools;
- mail must create a real reason to bring thrust or missile tools;
- guns must have a job-supported PA-independent ranged role;
- robes/cloth should remain physically fragile but tactically valuable through magic, utility,
  speed, MP, status, or equipment effects.

## Healthy Strong Builds

Strong builds are allowed when their strength has shape.

### Specialized Strength

A build may be excellent against a target class, armor profile, map type, or enemy plan.

Example pattern:

```text
anti-plate crush attacker
anti-mail missile attacker
low-Faith anti-magic unit
high-Faith burst caster
high-mobility objective unit
```

The build should lose value or require adaptation outside its preferred context.

### Investment Strength

A build may be very strong if it requires serious JP, late job access, equipment investment, or
multi-job planning.

Investment is not enough by itself. Expensive universal dominance is still unhealthy.

### Risk Strength

A build may have a high ceiling if it carries real risk:

- volatile damage;
- accuracy risk;
- CT delay;
- MP pressure;
- positioning exposure;
- element/status counters;
- Brave/Faith downside;
- lower durability.

### Combo Strength

A build may become powerful through a deliberate interaction between active job, secondary,
reaction, support, movement, and equipment.

The key question is whether the combo creates a distinct playstyle or just becomes the obvious
best way to play every physical, magical, or hybrid unit.

### Flavor Strength

A build may be strong because it expresses a job fantasy especially well.

Examples:

- Monk should be allowed to feel dangerous unarmed.
- Archer should be allowed to remain relevant as the real bow job.
- Orator should be allowed to win through speech, morale, recruitment, and disruption.
- Bard and Dancer should be allowed to shape the battlefield through performance.

## Unhealthy Build Patterns

### Universal Answer

A build is unhealthy if it remains one of the best answers across most scenario archetypes.

Scenario archetypes should eventually include at least:

- plate-heavy physical enemies;
- mail-heavy physical enemies;
- leather skirmishers;
- cloth casters;
- ranged pressure;
- magic pressure;
- evasive targets;
- status-resistant targets;
- boss-like targets;
- undead or special targets where relevant.

### Mandatory Global Piece

A reaction, support, or movement skill is unhealthy if most strong builds want it regardless of
their role.

This applies especially to:

- broad damage boosts;
- broad evasion or immunity;
- broad mobility;
- broad action-economy compression;
- equipment unlocks that erase job identity.

### Practical Immunity

Practical immunity is not allowed as a broad defensive state.

This includes:

- near-perfect physical avoidance;
- near-perfect magic avoidance;
- reaction/evasion stacks that invalidate enemy turns;
- automatic healing loops that erase attrition;
- movement that makes retaliation impossible in most maps.

Narrow immunity can exist as a tactical case when it is explicit, counterable, and not broad.

Examples of acceptable narrow cases:

- element nullify or absorb against one element;
- undead-specific behavior;
- status immunity from a rare item;
- a defensive stance that has real cost or limited duration.

### Engine Shadowing

A support engine should not be required to make a weapon family or job baseline viable.

Examples of engines:

- Two Swords;
- Two Hands;
- Martial Arts/Brawler;
- Attack Boost;
- Arcane Strength;
- Shirahadori-style defense;
- Teleport-style movement.

The engine should enhance a viable identity, not rescue an otherwise failed one.

This mirrors formula v0.2's separate viability lenses: single-hit, dual-wield, and support-engine
contexts must not be collapsed into one benchmark.

### Same Build, Different Costume

If several jobs converge on the same best secondary, same support, same reaction, same movement,
and same equipment plan, the job system has lost texture even if the numbers are technically
balanced.

Late-game optimization should produce multiple strong shapes, not one solved template.

## Testable Acceptance Criteria

These criteria are provisional and should be refined in `02-job-design-protocol.md`, but every
job proposal should already be written with them in mind.

Every percentage-based criterion here depends on two future benchmark populations:

- the accepted distinct strong build set;
- the scenario archetype set.

If either set is biased, cherry-picked, or too narrow, the percentages become meaningless.
`02-job-design-protocol.md` must define both populations with explicit representativeness rules.
The build set must cover every primary role, every armor class as a target profile, every physical
damage mode, and magic. The archetype set must cover the scenario list in this document. Neither
set may be curated to make a favored build pass.

This follows the same lesson as formula v0.2's viability lenses: mixing dual-wield or support-engine
totals into a single-hit benchmark made normal single-hit families look falsely weak. Job benchmarks
must be scoped as carefully as formula benchmarks.

### No Mandatory Global Piece

In the late/stress benchmark set, no single reaction, support, or movement skill should appear in
more than 50% of accepted distinct strong builds.

Warning threshold: more than 35%.

Counting rules:

- count combat builds, not grind-only training builds;
- count distinct build identities, not small equipment variants;
- Bard and Dancer parity duplicates count as the same global piece;
- if a skill intentionally exceeds the warning threshold, the proposal must justify why it is not
  functionally mandatory.

### No Universal Build

No build should be top-performing, or within 95% of the top-performing build, in 50% or more of
scenario archetypes.

Warning threshold: more than 35%.

This is not only a damage rule. A build can be universal through survival, mobility, status,
healing, action economy, or map control.

Ramza's Chapter 4 job is a sanctioned bounded exception to these universal-build thresholds because
his protagonist identity is broad knight/mage flexibility. The exception does not allow Ramza to
strictly dominate generic jobs inside their own signature niches.

Ramza may be broadly strong, but he should not be:

- a better anti-plate crusher than the dedicated crush job or build;
- a better burst caster than the dedicated caster;
- a better ranged specialist than the dedicated ranged job;
- a better status/controller than the dedicated control job;
- free from normal secondary, reaction, support, movement, equipment, CT, MP, and positioning
  opportunity costs.

The testable framing is: no generic job's signature niche should be strictly dominated by Ramza.

### No Practical Broad Immunity

No ordinary reaction/support/movement/equipment stack should produce broad practical immunity.

Initial check:

- broad defensive negation above 85% across multiple physical or magical attack modes is a fail;
- broad defensive negation above 75% is a warning;
- true 100% immunity is only acceptable for narrow, explicit, counterable cases.

The final protocol should define the exact simulation rows for physical, magical, status, and
mixed-pressure checks.

### No Baseline Repair By Mandatory Engine

If a job or weapon family is only viable with one specific support or reaction, the job or family
baseline is incomplete.

Allowed exception:

- a job whose identity is explicitly built around an innate or signature engine.

Even then, the engine must be part of that job's identity, not a generic patch every other job
also needs.

### Bard/Dancer Parity Invariant

Bard and Dancer are the only remaining gender-restricted jobs.

Their reaction, support, and movement skills must be byte-identical in:

- internal ability records, or duplicate records if the engine requires duplication;
- display names;
- effect behavior;
- formula IDs;
- JP costs;
- prerequisites;
- learnability;
- help text meaning.

Their action abilities may differ.

If the engine requires separate Bard and Dancer records, the only allowed record differences are
the minimum structural fields needed to attach the same skill to different jobs.

Any mechanical, cost, prerequisite, or help-text mismatch in reaction/support/movement is a hard
failure.

### Growth Does Not Create Hidden Superiority

The three growth profiles are:

- physical;
- magical;
- hybrid.

No job proposal should depend on hidden permanent level-up optimization to be good.

Current job multipliers, equipment access, and skills may differ sharply. Permanent growth should
not create secret "level in this job or ruin the unit" pressure.

### Active Job Still Matters

For every accepted strong build, the proposal should explain why the chosen active job matters.

If the same build works almost identically on many active jobs, the design must identify whether
that is acceptable general utility or a sign that a global piece is overpowering job identity.

## Cross-Phase Formula Protection

Job balance must not silently invalidate formula-balance v0.2.

This principles document does not define the full re-simulation protocol; `02-job-design-protocol.md`
must do that. The principle is already fixed:

- changing job multipliers can change PA, MA, Speed, HP, and therefore formula outcomes;
- changing equipment access can change which jobs can deliver `swing`, `thrust`, `crush`, or
  `missile`;
- changing growth/effective-stat assumptions can change all scenario bands;
- changing support engines can change stress-case ceilings.

Any such change must be treated as formula-affecting until proven otherwise.

## Review Labels

Later job proposals should use these labels during review:

| Label | Meaning |
| --- | --- |
| `healthy-strong` | Strong, distinct, and has clear costs or counters. |
| `needs-sim` | Plausible, but must be tested before acceptance. |
| `mandatory-risk` | May become too common across strong builds. |
| `immunity-risk` | May invalidate too many enemy turns or attack modes. |
| `universal-risk` | May be too good across too many scenario archetypes. |
| `identity-risk` | Makes multiple jobs/builds collapse into the same plan. |
| `formula-risk` | May invalidate formula v0.2 assumptions or metrics. |
| `reject` | Violates a hard principle or cannot be made healthy without redesign. |

## What This Document Does Not Decide

Still open for later documents:

- exact benchmark build list;
- exact party simulation harness;
- exact job role map;
- exact JP costs;
- exact skill lists;
- exact support/reaction/movement replacements;
- exact multipliers and growth values;
- exact prerequisite tree;
- exact thresholds after the first job simulations prove whether these initial limits are too
  loose or too strict.
