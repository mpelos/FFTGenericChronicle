# Knight And Archer V1 Proposal

Status: Accepted for provisional design
Version: V1
Date: 2026-06-20
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/04-foundation-physical-jobs-proposal.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Knight and Archer.

The proposal is concrete enough to define skill roles, build hooks, JP posture, and required checks.
It is not final implementation data. It does not set exact JP numbers, hit rates, CT values, damage
modifiers, equipment records, stat multipliers, AI rules, or prerequisites.

Knight and Archer are formula-sensitive jobs because their fantasies naturally touch weapon output,
armor response, evasion, accuracy, range, and action economy. This proposal therefore keeps those
effects at design level and marks the harness extensions required before concrete data acceptance.

## Group Thesis

Knight and Archer should stop being "basic sword guy" and "weak Aim ladder."

Their tactical contrast should be clear:

- Knight is durable armed-control: it holds space, pressures equipment, and makes frontline
  engagement costly.
- Archer is the long-term bow/crossbow specialist: it uses range, height, timing, and target
  selection better than any borrowed-bow build.

Neither job should win by raw universal damage. Both should win by making the player care about
positioning, target profile, weapon family, armor class, and timing.

## Shared Formula Notes

The current damage harness does not model several mechanics this pair may need:

- dynamic armor response or guard-break states;
- shield/evasion state changes;
- enemy-offense changes from disarm or weapon-output reduction;
- delayed-action target movement and CT timing;
- overwatch or interrupt reactions;
- AI targeting changes from challenge/taunt effects.

Those mechanics are allowed if they are the best way to express the job identity, but concrete
versions require the relevant harness or scenario extension before final acceptance.

## Knight

Job: Knight
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: durable melee job with Battle Skill equipment breaks.

Vanilla problems:

- low-hit equipment breaks can feel like wasted turns;
- permanent equipment destruction can be too swingy or too hostile as a default tactic;
- the job often collapses into "use swords with armor" instead of a distinct control identity;
- broad armor/equipment supports can become generic tank templates.

Accepted high-level role: durable armed-control frontline.

Primary role: `melee-physical`

Secondary tags: `durable`, `weapon-break`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `knight_sword`, `sword`, `fists`.

Armor class as target: `plate`.

Supported damage modes: `swing`, `crush`.

Formula v0.2 coupling:

- Knight's natural swing damage is intentionally not the best anti-plate answer;
- Knight has generic fists/crush access, but that output must stay clearly below Monk's protected
  unarmed identity and must not turn Knight into a stealth anti-plate specialist;
- Knight may create openings against protected targets through guard-break or equipment pressure,
  but those openings are formula-affecting once numeric;
- Knight should be durable because of role and equipment, not because it creates broad immunity.

### Action Skillset Goals

Knight's action set should make armed enemies and defended positions care about the Knight.

It should convert the vanilla break fantasy from "low-accuracy permanent deletion" into a more
reliable, more tactical control kit with counters.

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Rend Weapon` | weapon-output pressure | Temporarily reduce, jam, or disarm an equipped enemy so its next attacks are weaker or constrained. | Formula-affecting for enemy offense; should have counters and should not permanently delete equipment by default. |
| `Rend Armor` | armor exposure | Temporarily expose armor or reduce durability so allies can exploit a target. | Dynamic armor-response effect; requires harness support before concrete acceptance. |
| `Shield Break` | guard pressure | Reduce shield, block, or facing protection to let the party attack a defended unit. | Accuracy/evasion formula effect; must not become universal "ignore defenses." |
| `Challenge` | frontline control | Mark, taunt, zone, or otherwise force an enemy to respect the Knight's position. | Requires AI/action-economy scenario checks; cannot hard-lock bosses or erase movement play. |
| `Guarded Strike` | reliable armed attack | Give Knight a safe melee option that deals modest damage while improving immediate frontline posture. | Not a top damage button; defensive benefit must not stack into immunity. |
| `Crushing Blow` | anti-guard impact | A crush-flavored control hit through fists or heavy impact fantasy, aimed at guard disruption rather than raw anti-plate dominance. | Must not make Knight the best crush job; Monk remains the protected unarmed/crush home. |

`Rend Weapon`, `Rend Armor`, `Shield Break`, and `Crushing Blow` are formula-risk skills. They are
directionally accepted only if the concrete implementation is modeled as dynamic enemy-offense,
armor-response, accuracy/evasion, or damage-response behavior.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Parry` | weapon/shield discipline | Let Knight reduce or avoid a narrow class of frontal weapon pressure. | No broad physical immunity; should care about facing, equipment, attack type, or Brave. |
| `Brace` | hold-ground reaction | Let Knight resist displacement, knockback, or formation-breaking pressure. | Narrow map-control defense; should not reduce all damage. |

If ability slots require one reaction, prefer `Parry` because it preserves the classic Knight
defensive fantasy. `Brace` can move to another durable job or become an innate/stanced effect later.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Equip Armor` | durability unlock | Let non-plate jobs choose a heavy-armor build at a support-slot cost. | Must run `M-EQUIP-UNLOCK`; cannot erase armor-class identity. |
| `Equip Shield` | guard unlock | Let selected builds spend support slot on shield identity. | Must run `M-EQUIP-UNLOCK` and immunity rows when stacked with reactions/evasion. |
| `Defensive Training` | frontline discipline | Improve Knight-style control or guard actions without raising all damage. | Should not become the default physical support. |

Do not add a broad `Equip Sword` support in this version. Generic sword access would work against
the mod's goal of making all weapon families matter and would risk turning Knight into a universal
sword donor again.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Shield March` | armored positioning | Help a plate/shield unit keep formation without becoming a universal mobility tool. | Should depend on heavy frontline posture or have a meaningful movement limitation. |

`Shield March` is a candidate movement identity, not a final record. It should be tested against
late movement options so plate jobs do not become too mobile and too durable at the same time.

### JP Progression

JP posture:

- one reliable control action should be reachable early so Knight works before grind;
- armor/weapon pressure should be mid-cost, not a late tax;
- `Equip Armor` and `Equip Shield` should be meaningful investments because they change build
  identity;
- any high-impact guard-break or challenge effect should be expensive enough to signal commitment;
- Knight should be playable as active job before its support skills are learned.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Later progression design should keep Knight early enough to remain a recognizable FFT foundation
job, while preventing its equipment supports from becoming the only sensible early objective.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Knight donor patterns:

- durable hybrid takes `Equip Shield` for a specific guard build;
- cloth or leather unit takes `Equip Armor` for a slow heavy variant at real support cost;
- melee controller borrows Knight actions to disable armed enemies;
- active Knight borrows Monk or Squire tools for crush/utility without losing frontline identity.

Unhealthy Knight donor patterns:

- every fragile job patches itself with `Equip Armor`;
- `Parry` plus shield plus equipment creates broad physical immunity;
- `Rend Armor` becomes the default setup for all physical parties;
- Knight becomes the best sword job and best tank at the same time.

### Expected Strong Builds

- active Knight with Battle Skill and shield/plate, controlling enemy offense;
- Knight with Squire or Monk secondary for utility or crush pressure;
- non-Knight frontline using `Equip Shield` for a specific guard concept;
- anti-armed-enemy party that uses Knight to reduce dangerous weapon output.

### Expected Weaknesses

- lower mobility than light physical jobs;
- weaker into magic and status unless built for it;
- swing damage is not naturally favored into plate;
- control actions lose value against unarmed, caster, monster-like, or status-immune targets;
- support-slot cost is high for armor/shield unlocks.

### Expected Counters

- magic pressure;
- status or morale control;
- mobile ranged enemies;
- enemies without meaningful weapon/equipment output;
- crush-focused enemies attacking plate.

### Ramza / Unique-Job Interaction

Ramza can become a knight/mage hybrid later, but he should not strictly dominate Knight's durable
armed-control niche. If Ramza gains similar control tools, they should trade against his magical
or leadership options.

## Archer

Job: Archer
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: ranged physical job with Charge/Aim.

Vanilla problems:

- many Aim ranks differ only by number;
- delayed shots can whiff because targets move without meaningful counterplay;
- Archer often becomes an early stepping stone rather than an endgame bow identity;
- ranged accuracy supports can become too universal if they work on every attack type.

Accepted high-level role: endgame-capable bow/crossbow specialist.

Primary role: `ranged-physical`

Secondary tags: `missile`, `anti-mail`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `longbow`, `crossbow`, `fists`.

Armor class as target: `leather`.

Supported damage modes: `missile`, `crush`.

Formula v0.2 coupling:

- bows and crossbows should make missile pressure matter across the whole game;
- mail naturally gives missile a target profile to care about;
- Archer must be the best native bow/crossbow shell, even when other jobs can borrow bow access.

### Action Skillset Goals

Archer's action set should replace the pure Aim ladder with situational shots.

The job should care about:

- range;
- height;
- CT timing;
- line of fire;
- target armor;
- movement prediction;
- exposed or pinned enemies.

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Quick Shot` | low-commitment shot | Act now for modest pressure when delay is unsafe. | Lower ceiling; should not replace normal attacks in every case. |
| `Aimed Shot` | delayed payoff | Spend CT/delay for a stronger or more accurate shot when the target is constrained. | Target movement and action timing must matter; requires CT scenario modeling. |
| `Pinning Shot` | movement/tempo pressure | Slow, pin, or CT-pressure an enemy that needs to cross ground. | Should not hard-lock targets; status/position counters required. |
| `Piercing Shot` | anti-mail / line pressure | Reward shooting through a line, exposed target, or mail profile. | Formula-affecting if it changes armor response or penetration; must preserve other missile users. |
| `Covering Shot` | overwatch threat | Punish exposed movement or protect a lane if implementable. | Action-economy and interrupt modeling required; cannot make enemy movement impossible. |
| `High-Ground Shot` | elevation reward | Make height advantage a real Archer identity without requiring it every turn. | Map-dependent; should not dominate flat maps. |

The final action list may collapse `Aimed Shot` and `High-Ground Shot` if ability slots are tight.
The non-negotiable part is that Archer needs at least one quick line, one delayed/high-payoff line,
and one control/positioning line.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Arrow Guard` | missile defense | Give Archer a narrow defensive identity against arrows, bolts, and similar projectile pressure. | Missile-only or otherwise narrow; no broad physical immunity. |
| `Speed Save` | pressure response | Preserve the fantasy of reacting to danger with tempo, but make it short-lived or capped. | No permanent speed snowball; must not become a universal reaction. |

If ability slots require one reaction, prefer `Arrow Guard` as Archer's clearer identity. `Speed
Save` can be moved, capped, or redesigned if it creates universal reaction pressure.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Equip Bow` | ranged build unlock | Let non-Archer jobs build around bows/crossbows at support-slot cost. | Must run `M-EQUIP-UNLOCK`; active Archer must remain the best native bow shell. |
| `Concentration` | ranged discipline | Improve reliability for bow/crossbow or aimed-shot plans. | Should not mean all attacks always hit; broad accuracy support risks mandatory use. |
| `Bow Mastery` | native ranged payoff | Reward dedicated bow/crossbow builds without helping guns or spells. | Should compete with other damage/support engines and not repair weak baseline bow formulas. |

`Concentration` is the highest mandatory-risk support in this job. A concrete version should be
limited by weapon class, action type, facing, evasion source, or another meaningful boundary.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Jump +1` | height access | Preserve an early elevation movement hook and make Archer care about terrain. | Vertical utility only; should not outclass broader late movement. |

### JP Progression

JP posture:

- `Quick Shot` and one basic aimed/control option should be cheap enough that Archer feels better
  than vanilla immediately;
- `Piercing Shot`, `Covering Shot`, and strong height/CT tools should be mid or expensive because
  they affect scenario control;
- `Equip Bow` and `Concentration` should be meaningful investments, not early mandatory pickups;
- Archer should not need an expensive support to make basic bow attacks worth using.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Later progression design should keep Archer available early enough to be the natural bow job, while
ensuring its late tools arrive through commitment rather than passive job access.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Archer donor patterns:

- active Archer uses another secondary while keeping bow/crossbow action identity;
- a controller borrows bows through `Equip Bow` for a specific ranged build;
- a physical job borrows `Concentration` only if its attack plan matches the support's boundaries;
- Archer borrows Knight or Squire utility to control lines while staying the best native ranged
  shell.

Unhealthy Archer donor patterns:

- `Concentration` becomes the obvious support for most physical and magical builds;
- borrowed bows outperform active Archer with the same equipment;
- `Covering Shot` makes enemy movement non-viable in too many maps;
- `Piercing Shot` makes all mail targets trivial and erases crossbow/gun distinctions.

### Expected Strong Builds

- active Archer with bow/crossbow, using quick/delayed/control shots by map state;
- Archer with Squire secondary for light utility and positioning;
- mail-punishing missile build that excels against medium armor without dominating plate or cloth;
- high-ground map specialist with strong but map-dependent output.

### Expected Weaknesses

- leather durability;
- line-of-fire and height dependency;
- weaker when forced into close melee;
- lower burst than dedicated late melee stress engines;
- reduced value against projectile-resistant or terrain-protected targets.

### Expected Counters

- fast melee gap closers;
- shielded or evasive targets if `Concentration` is bounded;
- maps with blocked lines or low height contrast;
- magic pressure;
- enemies that punish delayed actions.

### Ramza / Unique-Job Interaction

Ramza may use bows only if a later chapter design explicitly supports that build. He must not
become a better native bow specialist than Archer. If Ramza gains broad ranged access, Archer still
needs superior bow/crossbow action texture.

## Scenario/Check Plan

This proposal is accepted only as a provisional design direction until the concrete values pass the
relevant rows.

### Knight Rows

Required later:

- `J-EARLY-SELF`: Knight with one reliable control action and baseline sword/plate identity.
- `J-MID-SELF`: Knight with core weapon/armor pressure but no late borrowed engine.
- `J-MID-PARTY`: Knight enabling allies without becoming mandatory.
- `J-LATE-BUILD`: at least one credible late Knight active or donor build.
- `J-PARTY-NO-JOB`: comparable frontline party plan that does not include active Knight.
- `M-RSM-COUNT-LATE`: `Parry`, `Brace`, `Equip Armor`, `Equip Shield`, `Shield March`.
- `M-EQUIP-UNLOCK`: `Equip Armor` and `Equip Shield`.
- `I-PHYS-*`: `Parry`, shield stacks, and defensive stance effects.
- `I-MIXED-ROUND`: plate/shield Knight into mixed physical and magic pressure.

Conditional:

- dynamic armor-response modeling for `Rend Armor`, `Shield Break`, and `Crushing Blow`;
- enemy-offense encounter modeling for `Rend Weapon`;
- action-economy or AI modeling for `Challenge`;
- Gate F5 if any concrete values alter damage, armor response, accuracy, evasion, or durability.

### Archer Rows

Required later:

- `J-EARLY-SELF`: Archer with quick and basic aimed shot using early bow/crossbow.
- `J-MID-SELF`: Archer with core control/height identity online.
- `J-MID-PARTY`: Archer in a normal party with frontline and caster allies.
- `J-LATE-SELF`: active Archer remains valuable without relying on borrowed-bow cheese.
- `J-LATE-BUILD`: strong bow/crossbow build using Archer as active or key donor.
- `J-STRESS-COUNTER`: Archer pressured by gap closers, shields/evasion, and blocked lines.
- `J-PARTY-NO-JOB`: comparable party plan that does not field active Archer.
- `M-RSM-COUNT-LATE`: `Arrow Guard`, `Speed Save`, `Equip Bow`, `Concentration`, `Jump +1`.
- `M-EQUIP-UNLOCK`: `Equip Bow`.
- `M-SECONDARY-COUNT`: Aim/Archer secondary incidence.
- `U-ARCHETYPE-COVERAGE`: quick/delayed/control bow plan across plate, mail, leather, cloth,
  ranged, magic, evasive, boss-like, and bad-map archetypes.

Conditional:

- CT/delayed-action modeling for `Aimed Shot`;
- action-economy/interrupt modeling for `Covering Shot`;
- dynamic armor or penetration modeling for `Piercing Shot`;
- accuracy/evasion modeling for `Concentration`, `High-Ground Shot`, or `Arrow Guard`;
- Gate F5 if any concrete values alter damage, armor response, accuracy, evasion, reach, or action
  economy.

## Formula Re-Sim Requirement

No immediate Gate F5 re-sim is required for this document.

The concrete data pass must re-evaluate this if it:

- changes Knight or Archer stat multipliers;
- changes equipment access;
- changes sword, knight-sword, bow, crossbow, fists, or armor-class formulas;
- changes weapon output or enemy offense;
- changes armor response, evasion, shield behavior, accuracy, or line-of-fire behavior;
- changes delayed-action, overwatch, interrupt, or AI targeting behavior;
- changes movement enough to alter exposure or reach.

## Implementation Assumptions

- Data modding can create or repurpose ability records.
- If ability slots are constrained, preserve identity before preserving every named candidate.
- Skill names are placeholders until the implementation pass verifies text, record limits, and
  localization constraints.
- Permanent equipment deletion should not be the default Knight outcome unless a later data pass
  proves it is healthier than temporary control.
- Archer delayed actions must be designed with the player's ability to predict target behavior in
  mind; otherwise they repeat vanilla Aim's feel problem.

## Open Proof Needs

- Whether dynamic armor-response and enemy-offense harness extensions should be built before
  concrete Knight values.
- Whether delayed/overwatch action modeling should be built before concrete Archer values.
- Whether `Concentration` can be preserved as a bounded ranged discipline support.
- Whether `Equip Armor`, `Equip Shield`, and `Equip Bow` can remain attractive without erasing job
  equipment identity.
- Whether `Speed Save` should remain on Archer, move elsewhere, or be replaced by a narrower
  ranged reaction.
- Whether `Challenge` is implementable in a way that feels like FFT and does not depend on brittle
  enemy AI assumptions.

## Claude Review Verdict

Claude reviewed whether this proposal:

- preserves Knight and Archer identity;
- keeps Knight from becoming universal sword/tank;
- keeps Archer as the best native bow/crossbow job;
- correctly flags formula-affecting mechanics and harness gaps;
- names the right check rows;
- should be accepted as provisional design, revised, or blocked.

Claude review verdict: approved as provisional design after the Knight fists/crush clarification.
