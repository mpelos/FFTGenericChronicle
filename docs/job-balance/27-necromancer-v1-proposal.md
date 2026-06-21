# Necromancer V1 Proposal

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
- `docs/job-balance/20-time-mystic-v1-proposal.md`
- `docs/job-balance/21-summoner-geomancer-v1-proposal.md`
- `docs/job-balance/23-deferred-campaign-economy-policy.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Necromancer, the replacement for the
Arithmetician/Calculator slot.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, spell powers, MP
costs, hit rates, CT values, corpse durations, undead-control durations, poison ticks, drain caps,
status rates, area sizes, damage multipliers, equipment records, stat multipliers, or prerequisites.

Necromancer is late by design. It is being designed after the ordinary caster, controller,
performer, and physical ecosystems have clearer boundaries, because its fantasy touches many
dangerous mechanics:

- death;
- Doom;
- drain;
- poison and attrition;
- undead and healing inversion;
- KO/corpse state;
- control and allegiance;
- MP/resource pressure;
- late-game support value.

## Replacement Thesis

Calculator is removed.

Necromancer does not inherit Calculator's core problem: solving the whole map through abstract global
rules.

Hard V1 rejection:

- no arithmeticks selectors;
- no global spell casting by level/CT/height/prime/multiple rules;
- no free whole-map targeting;
- no action that bypasses range, line, CT, MP, Faith, hit chance, immunity, target state, and
  positioning all at once.

Necromancer should feel like a late dark caster whose power comes from battle state:

- wounded targets;
- poisoned targets;
- doomed targets;
- KO bodies;
- undead units;
- MP-starved casters;
- risky drain windows;
- corpse or spirit manipulation.

The player should feel that the Necromancer is using what has already happened in the fight, not
ignoring the fight.

## Group Thesis

Necromancer should be the late dark-state controller.

It should create value through:

- attrition and rot;
- delayed lethal pressure;
- drain and resource pressure;
- undead conversion or interaction;
- corpse-adjacent tactical actions;
- conditional finishers;
- dark support that rewards setup.

It should not become:

- a better Black Mage;
- a better Mystic;
- a better Time Mage;
- a better Summoner `Lich`;
- a better White Mage revive engine;
- a Calculator replacement in disguise.

## Dark-State Notes

Necromancer must treat hard control and instant removal as setup rewards, not coin flips.

Relevant accepted gates:

- T2.1 for late secondary/reaction/support/movement incidence;
- T3/T3xT5 for drain, Poison, Doom countdown, KO/revive pressure, Undead healing inversion,
  timed Undead Mark interactions, Reraise interactions, and HP attrition;
- T4 for status accuracy, evasion, line, range, Faith-like delivery, Sleep, Confuse, Doom, Poison,
  Zombie/Undead, and Death Mark delivery;
- T5 for CT, status duration, countdowns, poison ticks, corpse windows, and delayed resolution;
- T8 for undead control, corpse/thrall targeting, allegiance, AI behavior, Vampire-like behavior,
  Charm/Confuse-adjacent control, and finisher eligibility;
- T9 for MP drain, Syphon, spell availability, and resource loops;
- T3xT5xT8 for the optional corpse/raise sub-kit when KO bodies, raised bodies, undead control,
  control ownership, targetability, and death-clock timing interact;
- T10 if any corpse, soul, or dark pact action grants actions, repeats turns, refunds CT, or creates
  action recursion;
- T11 for any area curse, plague cloud, or multi-target dark effect;
- T11xT5 or T3xT5xT11 if poison/rot/area attrition persists over target count and duration;
- Gate F4/F5 if dark damage, Faith manipulation, MP economy, undead inversion, or late support
  pieces drift magic/physical coexistence.

Accepted new composition gate:

```text
T3xT5xT8 - KO/Corpse/Undead State Composition
```

This gate is now part of the validation roadmap. It is scoped to the optional corpse/raise sub-kit,
not to every Necromancer dark action. It models:

- when KO bodies are eligible targets;
- how death-clock timing interacts with corpse actions;
- whether a corpse is consumed, preserved, or converted;
- who controls a raised body or undead state;
- whether the resulting object or unit can act, be targeted, be healed, be revived, or expire;
- how boss/unique/monster/guest/undead immunity policies apply.

T3/T5/T8 references remain sufficient for ordinary Poison, Doom, Drain, Syphon, and simple
Undead Mark checks. T3xT5xT8 is required when Necromancer uses a KO body, raises a body, controls
an undead-marked body, or creates an object whose targetability and expiry depend on death state.

## Necromancer

Job: Necromancer
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla slot: Arithmetician/Calculator, with arithmeticks selectors and global spell routing.

Vanilla slot problems:

- abstract selectors solve maps by rule rather than by battlefield tactics;
- global targeting ignores too much of FFT's positioning and CT play;
- once unlocked, the kit can dominate many spell schools without caring about their intended costs;
- the job's fantasy is disconnected from weapon, armor, target state, and map pressure.

Accepted high-level replacement: late dark caster/debuffer.

Primary role: `late-reward`

Secondary tags: `dark-magic`, `undead`

Growth profile: `magical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: proposed V1 direction is `book`, `pole`, `fists`.

Armor class as target: `cloth`.

Supported damage modes: `crush`, `magic`, `drain`.

Formula v0.2 coupling:

- books are `pampa_wp` crush and can express ritual/hybrid dark study without making Necromancer a
  broad weapon job;
- poles are `ma_wp` crush and give a magical caster backup route already familiar from Mystic;
- this is intentionally narrower than Mystic's spiritual-controller identity, not a replacement for
  it. Necromancer's pole access must be treated as a strict-subset coexistence problem until F4/T2.1
  proves that Mystic remains the better general spiritual controller;
- if later equipment passes give Mystic books or similar ritual weapons, Necromancer equipment must
  be revisited so it does not become Mystic with stronger dark-state actions;
- Necromancer should not inherit every staff/rod caster weapon by default, because Black Mage,
  White Mage, Time Mage, Summoner, and Mystic already occupy those spaces;
- dark damage should usually use the accepted Faith-linked magic model unless a specific drain or
  percentage effect explicitly routes elsewhere;
- drain, Doom, Poison, and undead effects are not ordinary damage and must be checked in their own
  attrition/status gates.

### Action Skillset Goals

Necromancy should reward battlefield state.

The player should ask:

- is this target worth marking for delayed death pressure?
- do I need poison/rot attrition instead of burst?
- can I convert a KO body or undead state into tactical advantage?
- should I drain HP/MP now or spend the turn on setup?
- will this dark action backfire against undead, immunity, or low-value targets?
- is a normal Black Mage, Mystic, or Time Mage action simply better here?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Poison` / `Rot` | attrition setup | Apply battle-scoped HP attrition that pressures durable targets over turns. | T3/T5; no invisible unavoidable tax; boss/immune rows required. |
| `Doom` / `Death Mark` | delayed lethal pressure | Mark an eligible target for a visible countdown or weakened-target threat. | T4/T5/T8; no cold instant kill; removal/immunity must matter. |
| `Drain` | HP drain | Deal bounded dark damage and recover HP as risky sustain. | T3/T3xT5/F4; no infinite sustain loop or top burst. |
| `Syphon` | MP drain | Pressure caster resources or fuel dark magic in a bounded way. | T9; cannot delete caster jobs or create MP loops. |
| `Zombie` / `Undead Mark` | undead-state manipulation | Temporarily make healing/revive interactions dangerous or invert a target's support profile. | T3/T3xT5/T8; battle-scoped; no permanent campaign state. |
| `Corpse Puppet` | KO-body manipulation | V1 default is a short-lived non-acting obstacle, decoy, or zone anchor created from a KO body. | T3xT5xT8; no permanent recruit, no full unit replacement, no action grant in V1. |
| `Command Undead` | undead control | Influence an undead-marked or raised body for a narrow action window if the corpse sub-kit survives. | T3xT5xT8/T8; no broad Charm replacement or monster-scope dependency. |
| `Gravebind` | corpse/zone pressure | Create a local zone around a KO body, undead target, or marked target. | T11/T5; local only, not mapwide arithmeticks. |
| `Dark Harvest` | conditional finisher | Finish a target only after prior setup such as low HP, Doom, Poison, or Undead Mark. | T4/T5/T8; boss/immune exclusion; no random hard KO. |

The exact names may change. The design roles should not.

Hard V1 capstone rule:

- `Dark Harvest` or any Death-like action must require a pre-existing weakened or marked
  precondition;
- it must have explicit boss/immune exclusions through T8 eligibility;
- it must not be a chance-based hard KO hidden behind a T4 roll;
- it must not be mapwide.

Corpse and undead actions are optional unless they can be made tactically healthy. The fantasy is
strong, but the implementation must not create hidden campaign economy, permanent recruitment,
infinite bodies, or action-recursion loops.

V1 corpse posture:

- `Corpse Puppet` is non-acting by default;
- it may block, distract, anchor `Gravebind`, or create a visible local tactical object;
- it should not take turns, use skills, trigger reactions, inherit gear, or create an extra unit;
- any acting raised body is deferred beyond V1 and would require T3xT5xT8 plus T10.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Soulbind` | dark backlash | Link a narrow portion of incoming pressure to a dark counter-drain or curse. | T3/T4/T5; no broad damage reflection or immunity. |
| `Death's Door` | critical dark survival candidate | Small critical-state reaction that buys a last dark action or applies a narrow curse. | T10 if action-granting; otherwise T3/T5; no loops. |

Necromancer does not need a universal defensive reaction. Its active kit is already high-risk and
high-impact.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Dark Lore` | necromancy specialization | Improve dark-state actions, drain caps, or undead/corpse reliability only for Necromancy. | No broad magic boost; T2.1/F4 required. |
| `Deathcraft` | corpse/undead specialization candidate | Improve corpse or undead interactions if that sub-kit survives. | Optional; worthless outside Necromancy by design. |

Necromancer is late and can offer powerful build pieces, but they should be narrow. Do not give
Necromancer the default broad magic damage support unless a later global support pass explicitly
chooses it and proves F4/T2.1.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Grave Step` | corpse/marked-target positioning | Help Necromancer reposition around KO bodies, marked targets, or undead states. | Narrow; not Teleport, Fly, or Move +3 replacement. |
| `Shadow Step` | dark positioning candidate | Optional fallback if corpse positioning is not implementable. | Must stay limited and caster-fragility preserving. |

Necromancer should not become a mobility donor. Its late-job reward should come from dark-state
play, not universal movement.

### JP Progression

JP posture:

- one attrition spell and one drain/resource spell should be reachable soon after unlock;
- Doom/Death Mark should require meaningful investment;
- Zombie/Undead Mark should be mid/high investment because it alters core healing/revive rules;
- corpse actions should be expensive and late if retained;
- Dark Harvest or Death-like finishers should be late capstones, never early problem solvers;
- supports and movement should be narrow enough that players choose Necromancer for the dark kit,
  not for universal passive rewards.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Necromancer should remain late. It replaces one of the most system-warping jobs in vanilla, and its
new identity still touches dangerous late-game mechanics.

### Gender/Equipment Restrictions

No gender restrictions.

No gender-based equipment restrictions.

Equipment direction is provisional. `book`/`pole`/`fists` is the safe V1 posture because it keeps the
job in dark study and MA-crush space without handing it every caster weapon.

This is a Mystic coexistence risk, not a solved fact. Necromancer shares the pole MA-crush surface
with Mystic but should be a strict subset in general spiritual-control use: Mystic should remain the
cleaner broad status/spiritual controller, while Necromancer only wins when dark-state, undead,
drain, Doom, or corpse conditions are actually relevant.

### Cross-Job Build Hooks

Healthy Necromancer donor patterns:

- late controller borrows Necromancy to pressure durable or already-wounded enemies over time;
- Black Mage uses Necromancy when burst damage is wrong but dark attrition is right;
- Mystic uses Necromancy for undead/corpse state while keeping Mystic Arts as spiritual control;
- White Mage or Chemist party plans around Undead Mark as a risky inversion tool;
- Necromancer active job uses books/poles and dark state to punish long fights.

Unhealthy Necromancer donor patterns:

- Necromancy becomes the default late caster secondary;
- Death/Doom solves bosses or hard enemies more safely than damage;
- drain replaces healing and resource economy;
- corpse actions create extra actions or permanent units;
- Undead Mark turns healing/revive rules into opaque traps;
- Necromancer becomes Calculator again through global or abstract targeting.

### Expected Strong Builds

- active Necromancer applying poison/doom/drain pressure in long fights;
- late party using Necromancer to punish enemies after one or two KOs happen;
- caster build using Syphon/Drain to manage resources at real action cost;
- dark controller using Zombie/Undead Mark to disrupt enemy healing or revive plans;
- conditional finisher build that rewards prior setup rather than chance.

### Expected Weaknesses

- cloth durability;
- low immediate burst compared to Black Mage;
- weaker into immune, boss-like, undead-safe, or status-resistant targets;
- MP and CT pressure;
- poor value before the battle has wounds, poison, doom, KO bodies, or undead states;
- Silence, pressure, and fast kills can prevent setup.

### Expected Counters

- status immunity and boss exclusions;
- Esuna/Holy Water-style cleansing if retained in final item/support ecology;
- fast burst before attrition matters;
- undead-specific reversal or immunity;
- low-value targets that make drain/resource actions wasteful;
- spread formations if Gravebind or plague effects use area.

### Ramza / Unique-Job Interaction

Ramza may later gain dark or holy-adjacent hybrid options only if they support his protagonist
identity. He should not become a better Necromancer. Necromancer owns corpse, undead, dark-state,
and delayed death pressure among generic jobs.

## Scenario And Check Plan

Minimum provisional rows before concrete values:

| Scenario ID | Purpose | Required gates |
| --- | --- | --- |
| `J-NEC-NO-CALC` | Necromancer has no arithmeticks selectors or whole-map abstract casting. | data/design check |
| `J-NEC-POISON` | Poison/Rot attrition helps long fights without replacing direct damage. | T3/T5/T11xT5 if area |
| `J-NEC-DOOM` | Doom/Death Mark creates delayed pressure with countdown and removal counterplay. | T4/T5/T8 |
| `J-NEC-DRAIN` | Drain sustains without replacing healing or becoming top burst. | T3/T3xT5/F4 |
| `J-NEC-SYPHON` | MP drain pressures casters without deleting MP jobs or creating loops. | T9 |
| `J-NEC-UNDEAD` | Zombie/Undead Mark changes healing/revive interactions legibly and battle-scoped. | T3/T3xT5/T8 |
| `J-NEC-CORPSE` | Corpse Puppet/Command Undead creates tactical value without permanent units or action recursion. | T3xT5xT8; T10 if action-granting |
| `J-NEC-FINISHER` | Dark Harvest is setup-gated, not random hard KO or boss solver. | T4/T5/T8 |
| `J-NEC-RSM` | Dark Lore, Deathcraft, Soulbind, Death's Door, and Grave Step incidence stays bounded. | T2.1/T3/T10 as applicable |
| `J-NEC-EQUIP` | Book/pole access does not eclipse Mystic or other caster crush identities. | F4/T2.1; F5 after real weapon data |

These are scenario requirements, not final scenario data.

## Formula Re-Sim Requirement

This proposal triggers formula review when values become concrete because it touches:

- late caster damage and dark attrition;
- drain and MP resource loops;
- Faith/magic coexistence;
- Undead healing/revive inversion;
- KO/corpse targetability;
- conditional hard-removal effects;
- book/pole MA-crush access;
- late support/reaction/movement incidence.

The strongest available status before real weapon data and the required gates is:

```text
Accepted for provisional design
```

No concrete implementation data should be marked final until the affected T-gates pass and formula
v1 or its accepted successor reconciles real weapon values.

## Implementation Assumptions

- Data mod scope can replace Calculator records with Necromancer records.
- Necromancer actions should be local, targeted, area-bounded, or state-bounded; not global
  arithmeticks.
- Corpse, undead, and recruitment-adjacent behavior is battle-scoped in this phase.
- Monsters remain out of current scope. Undead/corpse design should work without requiring monster
  job design.
- If corpse manipulation cannot be made healthy, Necromancer should still function through poison,
  doom, drain, MP pressure, undead mark, and conditional finishers.

## Open Proof Needs

- Exact T3xT5xT8 behavior for the optional corpse/raise sub-kit.
- Exact poison/rot tick timing and target-count behavior.
- Exact Doom/Death Mark countdown, accuracy, removal, and immunity rules.
- Exact drain cap and resource loop boundaries.
- Exact battle-scoped Undead Mark behavior with healing, revive, KO, Reraise, and Holy Water-like
  effects.
- Whether Corpse Puppet remains a non-acting body/zone/decoy, or is cut.
- Whether `book`/`pole`/`fists` equipment is enough identity without overloading Mystic.

## Claude Review Request

Claude should review whether:

- Necromancer has enough identity without recreating Calculator;
- the no-arithmeticks/no-global-abstract rule is strong enough;
- the action roles are late and dark enough without becoming a grab bag of every hard status;
- corpse and undead manipulation are framed safely enough for V1;
- the new T3xT5xT8 corpse/undead-state composition is scoped correctly to optional corpse/raise
  behavior;
- book/pole/fists is the right provisional equipment posture;
- Dark Harvest's finisher principle matches the accepted anti-instant-KO approach;
- the scenario/check plan names the right gates before concrete values.

Claude review verdict: Accepted after revision (claude-opus-4-8, 2026-06-21).

Claude accepted the V1 proposal after the revision:

- T3xT5xT8 is formalized in the roadmap and scoped to the optional corpse/raise sub-kit;
- `J-NEC-CORPSE` is bound to T3xT5xT8, with T10 required only if action-granting returns later;
- Mystic coexistence is explicitly treated as a strict-subset equipment risk requiring F4/T2.1;
- `J-NEC-EQUIP` is bound to F4/T2.1, with F5 after real weapon data;
- Undead Mark is bound to T3xT5 where timing affects healing/revive inversion;
- `Corpse Puppet` is non-acting by default in V1.
