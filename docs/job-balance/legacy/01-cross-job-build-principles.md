# Cross-Job Build Principles V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/deep-combat-layer/00-overview.md` (the canonical combat engine for this phase)

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

Combos are part of the fun of FFT and of Generic Chronicle. The goal is not to sand down every
strong setup until nothing feels clever. Players should be able to plan cross-job builds, coordinate
multiple characters, and feel rewarded for discovering skill interactions that produce a big payoff.

Party-level combos deserve extra tolerance. If a strong result requires several characters spending
turns, job slots, secondary slots, positioning, CT/MP, status setup, or other real opportunity costs,
that cooperation is usually healthy even when the payoff is high. The balance question is whether
the setup is so cheap, repeatable, safe, early, and broadly superior that it becomes the default
answer. Strong multi-character combos should be preserved or shaped before they are cut.

### Flavor Strength

A build may be strong because it expresses a job fantasy especially well.

Examples:

- Monk should be allowed to feel dangerous unarmed.
- Archer should be allowed to remain relevant as the real bow job.
- Orator should be allowed to win through speech, morale, recruitment, and disruption.
- Bard and Dancer should be allowed to shape the battlefield through performance.

### Skill Payoff And Readability

Good job kits should make the player feel that learned skills matter.

Small invisible modifiers are usually worse than bold, legible effects with real costs. A skill that
spends an action, CT, MP, item stock, positioning, JP, or a reaction/support/movement slot should
change the tactical situation enough for the player to notice. Generic Chronicle should avoid
over-conservative values that are technically balanced but emotionally dead.

### Skill Value And Action Economy

Every skill must justify the resource it consumes.

For every combat action that spends a unit's turn, the design must answer:

- why would the player use this instead of a basic attack, a kill attempt, a heal, or another
  available high-impact action?
- what changes in the battle immediately or predictably because this action was used?
- what target, map state, party setup, or enemy profile makes this the right action?
- if the skill were removed, what job fantasy, build route, or battle plan would meaningfully break?

For every campaign, economy, roster, monster, breeding, poach, recruitment, or long-term build
action, the design must answer a different question:

- what durable game value does this create outside the current damage race?
- why is that value worth spending a combat turn, JP, item stock, risk, or setup time?
- what prevents it from becoming mandatory grind or a dead menu option?
- what part of FFT's long-term planning would be lost if this skill disappeared?

Low numerical changes, low hit rates, delayed payoff, and setup actions are allowed only when the
payoff is large enough for the layer they belong to. A weak combat action is not repaired by being
safe, thematic, or technically balanced. A campaign action is not required to beat attacking inside
one battle, but it must produce meaningful campaign, economy, roster, monster, or build-planning
value. If a skill is neither worth a turn in battle nor meaningful outside battle, it is failing its
role.

This applies especially to controller, support, and debuff jobs. Their turns do not need to deal
damage, but a combat-facing turn must produce tactical value comparable to spending the turn on
damage: denying a dangerous action, creating a kill window, enabling a major party combo, preventing
a real loss, or changing target priority. A campaign-facing turn can instead be justified by
protecting a core long-term system such as monster recruitment, breeding, poaching, roster shaping,
or permanent build planning.

### External Mechanics Should Pay For Their Friction

Generic Chronicle should not make long-term build planning harder unless the change creates a clear
gameplay benefit.

The default balance target is inside battle: action strength, timing, CT, MP, range, accuracy,
immunity, duration, visibility, target restrictions, counterplay, job access, equipment access, JP
timing, and party setup. External systems such as permanent Brave/Faith drift, recruitment,
breeding, Poach routing, economy, and roster planning should keep their familiar FFT shape unless
changing them solves a real problem.

Do not add custom caps, custom permanent-stat rates, per-battle limits, extra grind, or campaign
friction only because a proposed combat effect is easier to balance that way. If the player pays by
having fewer ways to build the character they want, the design must name the benefit. If there is no
meaningful benefit, preserve the external mechanic and tune the battle-facing effect instead.

Persistent effects on units must be readable in battle. If a temporary effect changes damage,
movement, defense, targeting, accuracy, or vulnerability, it should normally be represented by a
visible status with clear feedback. If the engine cannot show the effect, prefer a vanilla status,
a direct stat change, an instant effect, or a simpler ability over hidden state.

### Creative Mechanics And Readability

Generic Chronicle should keep creating interesting mechanics. The lesson from early Knight and
Archer work is not "avoid new mechanics"; it is "make new mechanics legible enough for FFT play."

When a prior proposal has a good idea but too much complexity, the revision should preserve the
valuable part before cutting it. The design should identify:

- the fantasy or tactical interaction worth preserving;
- the smallest visible form that delivers that interaction;
- which extra marks, states, exceptions, or stacking rules can be removed;
- whether an existing vanilla status, direct stat change, equipment change, tile telegraph, or
  immediate effect can express the mechanic more cleanly.

Any new status, mark, charge, zone, wound, exposure, or conditional flag must be visible to the
player. If the player cannot look at the battlefield and know who is affected, what the effect means,
and when it will end or be consumed, the mechanic is too opaque for the main job design.

Complexity should be budgeted. A job can have a small signature vocabulary, but it should not ask the
player to track many overlapping custom states with different durations, triggers, owners, and
consumption rules. If a design needs that much bookkeeping, merge states, reuse vanilla statuses, or
convert the mechanic into an immediate effect.

Damage and recovery skills should keep some relevance across the game. Direct skill damage or
healing should not be a literal fixed value forever unless a later design pass explicitly approves
that exception. Even starter utility such as chip damage, shove damage, basic healing, or
Chakra-style recovery should scale modestly with attributes, level band, max HP, weapon tier, or
another visible progression hook while staying bounded for the current chapter. Item-based recovery
is different: reactions or skills that consume items can scale through item progression itself. This
is why the vanilla `Auto-Potion` idea works when it follows the player's available item tier instead
of using a flat late-game value.

Reaction design should not make one permanent stat the obvious answer for every unit. If all
important reactions scale only from Brave, the correct late-game behavior becomes raising Brave on
everyone. Reactions should instead have varied defensive identities and costs: Brave, equipment,
shield access, evasion, armor profile, damage type, positioning, CT, HP threshold, status context, or
job identity can all matter, as long as the resulting behavior remains understandable.

### Core System Preservation

Do not remove a core FFT subsystem by accidentally treating it as out of scope.

If a skill supports a larger campaign system, the redesign must preserve a route for that system or
explicitly move the whole system to a named later pass. The current skill may be reworked, delayed,
or moved to another job, but the proposal must state how the player will still access the system.

Protected systems include:

- monster recruitment and breeding;
- monster-facing speech such as `Tame` and `Beast Tongue`;
- `Poach` and monster-item routing;
- Brave and Faith recruitment/build variance;
- permanent roster and campaign-facing choices that are part of FFT's build-planning pleasure.

"Monsters are out of current combat scope" means monster combat balance may wait. It does not mean
monster recruitment, breeding, Beast Tongue, Tame, Poach, or their access route can silently vanish
from the mod.

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

## Reaction, Mitigation, and Slot Mechanics

These are the durable build-level mechanics that every job kit shares. They are engine-neutral rules;
the deep combat layer owns the concrete combat-side implementation (reaction taxonomy, active-defense
math, damage-type DR), and any v0.2 magnitudes that once expressed them are provisional.

### One slot each, and two supports never coexist

Each unit has exactly one reaction slot, one support slot, and one movement slot. A build that would
need **two support-slot abilities at once is not a legal single-unit build** — it is only a
theoretical stress probe, valid only when one part is innate/native to the active job.

Consequences (examples):

- the legal unarmed support ceiling is `Brawler` alone; `Brawler + Martial Discipline` is a probe,
  not an equippable build;
- a job cannot equip both an `Equip <weapon>` unlock and `Doublehand` and count the result as one of
  its own legal builds;
- a **native** active job whose engine is innate (e.g. Ninja's two-hit melee) can still spend its
  support slot on something else; a non-native job that must *learn* that engine as a support cannot.

Convergence stress rows that combine two supports are kept only as ceiling probes and must be labelled
as such, never presented as real builds.

### Strongest single mitigation channel

Defensive reactions, defensive supports, and defensive statuses that reduce an incoming hit all belong
to **one mitigation channel**. When several apply to the same hit, the unit gets the **single
strongest** applicable result — the channel members **do not multiply**.

This is what keeps stacked defenses from manufacturing practical immunity: an incoming 120 reduced by
the best applicable channel member (say ×0.60 → 72) is correct; multiplying every applicable defense
together (→ 36) is rejected. Armor class is **separate** from this channel — wearing heavier armor
changes the base damage-type response, it does not add a free extra mitigation layer on top.

The deep combat layer expresses this through depleting active defenses (Parry/Block) plus subtractive
DR by damage type; the no-multiply principle is the timeless rule.

### Reaction discipline

Reactions obey a fixed shared discipline regardless of which job owns them:

- one reaction roll per triggering action;
- no reaction recursion;
- damage *caused by* a reaction cannot itself trigger reactions;
- ordinary capped reactions trigger at most once per round unless a stricter cap is stated;
- the strongest-single-mitigation-channel rule applies to any defensive reaction.

Every reaction must declare a **trigger identity** (what condition fires it) before a final chance is
acceptable; there is no universal trigger formula, and in particular no universal "scale every reaction
off Brave." The deep combat layer's reaction taxonomy (courage / caution / neutral) is the canonical
classification; a reaction that is deliberately off-Brave is a neutral reaction.

### Movement has no universal late default

Movement pieces are ranked by role, not on one ladder. No single movement skill is accepted as the
correct late-game default: raw distance does not bypass terrain or elevation, terrain-ignore does not
grant height or range, and height-ignore does not grant terrain or horizontal reach. Each movement
choice should solve one thing and leave another unsolved.

## Cross-Phase Combat-Engine Protection

Job balance must not silently invalidate the deep combat layer (the canonical combat engine for
this phase).

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
