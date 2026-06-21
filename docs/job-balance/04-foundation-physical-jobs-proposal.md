# Foundation Physical Jobs Proposal V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first detailed job-group direction for the foundation physical and
utility jobs:

- Squire;
- Chemist;
- Knight;
- Archer;
- Monk.

This is a design-direction proposal, not final implementation data. It does not set exact formulas,
JP costs, multipliers, prerequisites, hit rates, CT values, or equipment changes.

Because it does not change multipliers, growth, equipment compatibility, weapon-family formulas, or
armor-class mappings, this proposal does not trigger Gate F5 by itself. Later numeric versions of
these jobs may trigger Gate F5.

## Group Role

These jobs should establish the first layer of FFT build crafting:

- Squire teaches basic physical/utility play and remains a useful low-complexity shell.
- Chemist keeps reliable item support and PA-independent gun value.
- Knight defines durable armed pressure and equipment control.
- Archer becomes the real endgame-capable bow/crossbow job.
- Monk defines unarmed crush/Brave/body discipline.

Together they must teach the player that physical jobs are not all sword variants.

## Shared Design Rules

### No Early Dead Ends

These jobs appear early or define early build planning. They must be usable without waiting for
late-game supports.

### No Mandatory Foundations

They may donate useful global pieces, but no single reaction/support/movement from this group may
become mandatory for most serious builds.

### Preserve Build Hooks

Each job should provide at least one attractive reason to borrow from it later:

- Squire: campaign utility and simple tactical glue;
- Chemist: item reliability and gun support;
- Knight: armor/equipment control and defensive discipline;
- Archer: ranged accuracy/positioning tools;
- Monk: unarmed/crush/Brave engine and body sustain.

### Keep Action Skills Tactical

Action skills should not be pure ladders. Each action should answer:

- why use this now?
- what target does it prefer?
- what does it cost or risk?
- what situation makes another action better?

### Formula-Affecting Tactical Effects

This document does not set concrete mechanics, so it does not trigger Gate F5 by itself.
However, later concrete versions must treat the following as formula-affecting:

- temporary armor exposure, armor shred, guard break, type-resistance changes, damage multiplier
  changes, evasion shifts, or accuracy shifts;
- weapon-break, disarm, or weapon-output reduction effects that change enemy offense.

These effects are allowed if they support job identity, but they must not be accepted as flavor-only
status effects. They require the appropriate Gate F5 or encounter-simulation coverage. If the
current harness cannot represent dynamic armor response or enemy-offense changes, the harness must
be extended before accepting the concrete mechanic.

## Squire

### Accepted Role

```text
Primary role: melee-physical
Secondary tags: starter, utility
Growth profile: physical
Armor class: leather
Native modes: swing, thrust, crush
```

### Design Intent

Squire should be the clean starter job and a useful lightweight physical shell later. It should not
exist only for JP Boost or early grinding.

The job should teach:

- simple weapon use;
- positioning;
- basic self/ally setup;
- small tactical utility;
- low-cost actions that always feel useful but rarely dominate.

### Action Skill Direction

Keep the "fundamentals" fantasy, but make each action tactically distinct.

Proposed action categories:

- basic ranged chip or displacement;
- self-focus or stance setup;
- ally rally or tempo support;
- simple first-aid utility;
- weapon-family teaching action that works differently with swing/thrust/crush access.

Avoid:

- a generic permanent stat-stack loop;
- a turn-wasting JP grind button as the main identity;
- a support action that makes Squire the best universal secondary.

### Reaction/Support/Move Direction

Squire can keep campaign-quality utility, but combat dominance must be separated from grind
convenience.

Candidates to preserve or reframe:

- JP Boost as campaign utility, not combat power;
- Move +1 as a basic mobility floor;
- a simple defensive or morale reaction that is useful early but not a late-game immunity engine.

Open review need:

- decide whether JP Boost remains a support skill, becomes cheaper, moves elsewhere, or is treated
  as a campaign/economy exception.
- if the weapon-family teaching action becomes a broad secondary toolbox, run `M-SECONDARY-COUNT`
  to ensure it does not become a universal secondary package.

## Chemist

### Accepted Role

```text
Primary role: specialist
Secondary tags: item, gun
Growth profile: hybrid
Armor class: leather
Native modes: missile, thrust, crush
```

### Design Intent

Chemist should remain the reliable item job. It should be valuable because it is certain, fast to
understand, and resilient to Faith/MA variance.

Chemist should also protect the early gun identity:

- gun damage is PA-independent;
- the job can contribute even with low PA;
- item turns and ranged turns compete for the same action economy.

### Action Skill Direction

Item use can stay recognizable, but the job needs more tactical texture than "best potion loop."

Proposed action categories:

- reliable single-target healing;
- reliable revive with item cost;
- condition removal;
- limited thrown-item range identity;
- field tools such as smoke, salve, antidote, or stabilizing actions if implementable;
- gun-compatible utility, not pure gun damage buffs.

Avoid:

- item access becoming the best answer to every healing problem;
- free infinite sustain;
- invalidating White Mage as a support caster;
- making Auto-Potion the default survival reaction again.

### Reaction/Support/Move Direction

Chemist's global pieces should be useful because they make item builds work, not because they solve
survival for every job.

Candidates to preserve or reframe:

- Throw Item as range/positioning support for item builds;
- Auto-Potion as capped, tier-aware, or resource-aware reaction;
- item economy or item reliability support;
- gun-related support only if it does not erase Orator/Chemist gun identity.

Required later checks:

- Auto-Potion must run `I-ATTRITION` and mandatory-piece rows;
- any broad item support must run mandatory-piece rows;
- gun support must check Chemist/Orator identity preservation.

## Knight

### Accepted Role

```text
Primary role: melee-physical
Secondary tags: durable, weapon-break
Growth profile: physical
Armor class: plate
Native modes: swing, crush
```

### Design Intent

Knight should be the durable armed-control job, not merely the default sword platform.

Its identity should be:

- holding space;
- punishing equipped enemies;
- shield/armor presence;
- weakening enemy offense or defense;
- forcing enemies to care about frontline engagement.

### Action Skill Direction

The vanilla break fantasy is strong, but the "low accuracy or wasted turn" feel should be improved.

Proposed action categories:

- weapon pressure: disarm, weaken, or lower weapon output;
- armor pressure: expose plate/mail targets or reduce durability;
- guard pressure: shield/evasion disruption;
- stance or challenge action that draws pressure or controls enemy movement;
- heavy strike that is reliable but not the best damage button.

Avoid:

- instant permanent equipment deletion as the default best tactic;
- low-hit casino turns as the entire kit;
- making Knight the universal best sword job;
- giving Knight the best anti-plate answer unless it uses a real crush/guard-break route.

Formula note:

- disarm, weapon-output reduction, armor exposure, guard break, shield disruption, evasion shifts,
  and defense-down effects are formula-affecting once made concrete. They must run Gate F5 or the
  successor encounter/offense simulation, and dynamic armor-response mechanics require harness
  support before acceptance.

### Reaction/Support/Move Direction

Knight can donate durability and equipment identity, but must not create universal tank templates.

Candidates to preserve or reframe:

- Parry-style reaction as weapon/shield discipline, not broad immunity;
- Equip Armor or Equip Shield as identity unlock with real support-slot cost;
- defensive support that helps frontline jobs but does not stack into practical immunity.

Required later checks:

- any Parry/Shirahadori-adjacent defense runs immunity rows;
- equip-supports run `M-EQUIP-UNLOCK`;
- armor pressure actions run plate/mail scenario checks.

## Archer

### Accepted Role

```text
Primary role: ranged-physical
Secondary tags: missile, anti-mail
Growth profile: physical
Armor class: leather
Native modes: missile, crush
```

### Design Intent

Archer is the only real bow job and must remain useful through the whole game.

The job should not be "weak ranged attack plus boring Aim ladder." It should be the job that best
understands:

- height;
- line of fire;
- target selection;
- delayed shots;
- anti-mail missile pressure;
- ranged control.

### Action Skill Direction

Replace the pure Aim ladder with situational shots.

Proposed action categories:

- quick shot: low commitment, lower payoff;
- aimed shot: CT or delay cost for higher payoff;
- pinning shot: movement/CT pressure;
- piercing shot: anti-mail or line pressure;
- covering shot: punish movement or exposed targets if implementable;
- high-ground shot: rewards elevation without becoming mandatory.

Avoid:

- several Aim ranks that only differ by number;
- delayed attacks that miss because targets casually walk away with no counterplay;
- making bows only good when borrowed by another job;
- making Archer irrelevant once guns or spells arrive.

### Reaction/Support/Move Direction

Archer should donate ranged build pieces without creating universal accuracy fixes.

Candidates to preserve or reframe:

- Concentration-style support should not mean "all attacks never miss";
- Equip Bow can exist as a build hook if Archer itself remains the best native bow shell;
- ranged evasion, overwatch, or positioning movement can be explored.

Required later checks:

- accuracy supports run mandatory-piece and universal-build rows;
- any overwatch/countershot reaction runs action-economy checks;
- Archer native builds must be compared against borrowed-bow builds.

## Monk

### Accepted Role

```text
Primary role: melee-physical
Secondary tags: crush, Brave
Growth profile: physical
Armor class: cloth
Native modes: crush
```

### Design Intent

Monk should be the protected unarmed/crush home.

Its identity should be:

- body discipline;
- Brave-linked confidence;
- impact damage;
- self-sustain;
- limited but meaningful range through martial techniques;
- strong floor without equipment dependency.

### Action Skill Direction

Martial Arts should remain a versatile kit, but not answer every problem.

Proposed action categories:

- close-range impact strike;
- line or wave technique with height/position limits;
- self/ally sustain such as Chakra, but with resource/position limits;
- revive or recovery with risk and range constraints;
- anti-plate or guard-breaking body technique;
- situational status cleanse or morale/body discipline tool.

Avoid:

- being the best damage, sustain, revive, range, and utility job at once;
- making gear irrelevant in a way that beats all weapon families;
- unlimited sustain loops;
- Brave scaling into practical offense or reaction immunity without caps.

Formula note:

- anti-plate, guard-breaking, damage-response, accuracy, or defense-down body techniques are
  formula-affecting once made concrete. They must be modeled in the appropriate harness before they
  can be accepted.

### Reaction/Support/Move Direction

Monk's global pieces are allowed to be attractive because this is a core FFT build route.

Candidates to preserve or reframe:

- Brawler/Martial Arts as the unarmed build engine;
- Counter as melee retaliation;
- First Strike/Hamedo-style defense with clear trigger limits;
- HP/critical recovery with anti-loop constraints.

Required later checks:

- Brawler/Martial Arts must preserve formula v0.2's fists identity;
- Counter/First Strike must run immunity/action-denial rows;
- Chakra/revive/sustain must run attrition rows.

## Group-Level Risk Review

### R1 - Early Global Pieces

This group contains several classic global pieces:

- JP Boost;
- Auto-Potion;
- Equip Armor/Shield/Bow;
- Concentration;
- Martial Arts/Brawler;
- Counter/First Strike;
- Move +1.

These are valuable FFT build hooks, but each must be checked against mandatory-piece rules.

### R2 - Healing Role Collision

Chemist and Monk both have sustain/recovery identity, while White Mage owns caster support.

Later design must separate:

- Chemist: reliable item/resource support;
- Monk: body-centered, positional or self/nearby sustain;
- White Mage: Faith/MA/CT/MP support magic.

### R3 - Anti-Plate Route

Monk is the clearest early protected crush identity. Knight may have guard-break pressure, and
Squire/Geomancer/Ninja can touch crush through equipment.

Later numeric design must ensure plate has a real early/mid answer without making crush universal.

### R4 - Ranged Identity

Archer must be the best native bow/crossbow job. Chemist and Orator should own gun identity.

Borrowed ranged access can be fun, but should not erase the native ranged jobs.

## Check-Matrix Status

This proposal is directional and does not yet change:

- multipliers;
- growth values;
- equipment access;
- weapon-family formulas;
- armor_class labels;
- JP costs;
- exact reaction/support/move effects.

Therefore:

- Gate F5 re-sim is not triggered by this document;
- no numeric harness output is required for this proposal;
- later concrete skill versions must run the rows named in each job section.
- later concrete armor-response, evasion/accuracy, defense-down, weapon-break, disarm, or
  enemy-offense effects are formula-affecting and must trigger the appropriate Gate F5 or successor
  encounter-simulation path.

## Acceptance Request

If accepted, this document should authorize GPT and Claude to produce concrete V1 skill lists for
these five jobs in smaller slices:

1. Squire/Chemist economy and early utility.
2. Knight/Archer weapon-control and ranged identity.
3. Monk unarmed/crush/sustain identity.

Each concrete slice should include JP posture, skill descriptions, risks, and required check rows.
