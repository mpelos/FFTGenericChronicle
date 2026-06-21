# Thief And Orator V1 Proposal

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
- `docs/job-balance/14-enemy-offense-disarm-model-schema.md`
- `docs/job-balance/15-targeting-challenge-model-schema.md`
- `docs/job-balance/17-offense-armor-composition-schema.md`
- `docs/job-balance/19-white-black-mage-v1-proposal.md`
- `docs/job-balance/20-time-mystic-v1-proposal.md`
- `docs/job-balance/23-deferred-campaign-economy-policy.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Thief and Orator.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, hit rates, steal
rates, status rates, Brave/Faith values, campaign rewards, damage multipliers, equipment records,
stat multipliers, or prerequisites.

Thief and Orator are paired because both are specialist jobs whose value can live outside raw damage:
stealing, recruitment, morale, equipment pressure, campaign economy, and social control.

Claude review verdict: conditional acceptance on 2026-06-21, with required edits applied in this
version.

## Group Thesis

Thief and Orator should make utility feel like combat, not like a side menu.

- Thief is the fast precision specialist: knife/thrust pressure, stealing, disruption, mobility, and
  opportunistic equipment pressure.
- Orator is the social battlefield controller: guns, Brave/Faith/morale, recruitment, speech status,
  and monster/social interactions.

Both jobs may touch campaign economy, but combat balance must not hide behind campaign value. A
skill that is weak in battle but profitable outside battle is not automatically good combat design.

## Shared Specialist Notes

Campaign economy and permanent-state effects are deferred to
`docs/job-balance/23-deferred-campaign-economy-policy.md`.

That policy track is not a combat dual-sim gate and does not belong in the validation roadmap. This
V1 accepts only battle-scoped behavior for the current job phase.

T7/T6xT7 can model what happens when a weapon or armor piece is removed during battle. The deferred
policy track decides later whether keeping that stolen item, gaining money, gaining EXP/JP,
recruiting a unit, or permanently changing Brave/Faith is healthy for the campaign.

Default policy:

- combat disruption should be temporary or battle-scoped in this phase;
- Brave/Faith changes should be battle-scoped in this phase;
- recruitment should be judged as both a control effect and a roster/economy effect;
- economy actions should not become mandatory grind chores.

Relevant accepted gates:

- T2.1 for secondary/reaction/support/movement incidence;
- T4 for steal/status accuracy, evasion, line, range, facing, and Charm/Sleep-style delivery;
- T5 for duration, control windows, speed/CT changes, and damage-to-break timing;
- T7 and T6xT7 for weapon/equipment removal and offense/armor consequences;
- T8 for Charm, Entice, recruitment, AI/control, Traitor-like behavior, and Tame/monster behavior;
- deferred campaign policy for permanent steal, recruit, Brave/Faith, gil, EXP/JP, poach, and loot
  economy;
- Gate F4/F5 if Brave/Faith, gun access, knife speed scaling, equipment disruption, or accuracy
  support drifts the accepted formula ecology.

## Thief

Job: Thief
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: fast steal job with Steal Gil, Steal Heart, equipment steals, Steal EXP, Sticky
Fingers, and Treasure Hunter-style economy/movement hooks.

Vanilla problems:

- stealing can feel like low-odds chores instead of tactical disruption;
- permanent equipment theft is too swingy if it deletes enemy identity while also paying campaign
  rewards;
- Thief can become only a prerequisite/job detour if knife pressure and speed utility are weak;
- Steal Heart/Charm can become oppressive if control is too reliable;
- Treasure/economy hooks can encourage grind behavior rather than tactical play.

Accepted high-level role: fast utility and precision job.

Primary role: `specialist`

Secondary tags: `fast`, `knife`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `knife`, `fists`.

Armor class as target: `leather`.

Supported damage modes: `thrust`, `crush`.

Formula v0.2 coupling:

- Thief's knife lane is `spd_pa_wp` thrust, so Speed is a real offensive stat;
- thrust gives Thief a natural anti-mail route because mail is weak to thrust, making Thief the
  precision complement to Monk's anti-plate crush lane;
- fists are fallback only and should not compete with Monk;
- equipment steal can temporarily change enemy offense or defense, requiring T7/T6xT7;
- speed plus steal/control must not make Thief the universal specialist secondary.

### Action Skillset Goals

Steal should become tactical disruption first and campaign reward second.

The player should ask:

- do I want a knife attack now or a steal/disrupt attempt?
- is this equipment worth spending an action on during battle?
- can I use speed/positioning to reach a valuable target?
- is Charm worth the control risk and counterplay?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Steal Gil` | campaign/economy theft | Preserve a low-stakes economy action if campaign tuning wants it. | Deferred policy; should not be a combat tax or grind chore. |
| `Steal Heart` | charm/control | Temporarily flip or disrupt an eligible target through charm. | T4/T5/T8; damage-to-break/counterplay required. |
| `Steal Helm` | armor-slot disruption | Temporarily expose head-slot durability or equipment benefit. | T6/T6xT7 plus deferred policy; no permanent deletion by default. |
| `Steal Armor` | armor-slot disruption | Temporarily expose body armor or reduce durability. | T6/T6xT7 plus deferred policy; must not erase Knight/plate identity. |
| `Steal Shield` | guard disruption | Remove or suppress shield/guard benefits. | T4/T6/T6xT7 plus deferred policy; no universal defense bypass. |
| `Steal Weapon` | offense disruption | Temporarily disarm or weaken an equipped target. | T7/T6xT7 plus deferred policy; enemy fallback must remain modeled. |
| `Steal Accessory` | utility disruption | Remove or suppress a specific accessory hook. | Deferred policy and status/equipment proof; not universal. |
| `Steal EXP` | campaign/progression theft | Preserve only if campaign economy has a clear reason. | Deferred policy; no combat-power assumption. |

The concrete implementation may convert permanent steals into battle-scoped suppression plus post
battle reward only if the deferred campaign policy approves. That keeps the combat effect readable
without letting permanent inventory value dictate every battle action.

Thief stealing and Knight breaking should be distinct. Knight breaks are durable frontline control
with no reward; Thief steals are fast, positional, fragile disruption with any permanent reward
deferred. J-TH-MID-EQUIP must compare those risk/reward profiles directly.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Sticky Fingers` | counter-steal/recovery | Opportunistically punish enemies that expose themselves or attempt theft/disruption. | Narrow trigger; deferred policy if it creates permanent reward. |

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Light Fingers` | steal specialization | Improve Thief steal reliability or positioning only for steal actions. | Must not become broad accuracy support. |
| `Poach` | campaign loot candidate | Preserve the monster-loot fantasy if later monster/economy scope allows it. | Out of current monster scope; deferred policy before concrete values. |

`Poach` is documented here only as a future campaign/economy hook. Monsters are out of scope for the
current job pass, so no concrete Poach behavior should be accepted now.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Move +2` | fast specialist movement | Let Thief reach targets and play the speed/positioning game. | Must compete with Teleport, terrain movement, and later mobility choices. |
| `Treasure Hunter` | campaign/map reward movement | Preserve exploration/economy flavor if maps support it. | Deferred policy; should not be mandatory for campaign rewards. |

### JP Progression

JP posture:

- one useful steal/disrupt action and basic knife identity should be reachable early;
- equipment steals should cost enough to signal tactical commitment;
- Charm/Steal Heart should not be an early universal control answer;
- Sticky Fingers, Move +2, and Treasure Hunter should require investment;
- Thief should be playable as an active job before campaign-economy rewards are learned.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Thief should remain accessible early or midgame enough to matter as a speed/utility route, not only
as a prerequisite for Ninja.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Thief donor patterns:

- fast utility unit borrows Steal for specific equipment targets;
- knife/thrust build uses Thief active stats and speed for anti-mail precision;
- party uses Thief to disarm or expose one dangerous enemy at action cost;
- campaign build uses Treasure Hunter outside benchmark combat rows.

Unhealthy Thief donor patterns:

- every physical build takes Steal as the best control secondary;
- equipment steal replaces Knight's control role;
- Charm becomes the safest answer to hard enemies;
- Treasure/economy rewards make Thief mandatory for campaign optimization;
- Thief exists only as a detour to Ninja.

### Expected Strong Builds

- active Thief using speed, knife pressure, and targeted steal;
- Thief with Squire or Archer utility for positioning and finishers;
- physical utility unit borrowing Steal to answer specific equipment enemies;
- low-damage party using Thief disruption to reduce enemy offense.

### Expected Weaknesses

- leather durability;
- lower raw damage than dedicated physical jobs;
- steal/status accuracy and positioning requirements;
- weak value against enemies without meaningful equipment;
- campaign actions that do not help immediate combat.

### Expected Counters

- equipmentless, monster-like, or status-immune enemies;
- high evasion/facing protection if steal is positional;
- ranged or magic pressure against leather;
- bosses or protected targets with steal immunity;
- enemy formations that punish deep positioning.

### Ramza / Unique-Job Interaction

Ramza may gain utility tools, but he should not become the best thief. If Ramza gains steal-like or
disruption actions, they should be narrower and trade against his hybrid knight/mage identity.

## Orator

Job: Orator
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: Speechcraft controller with Entice, Stall, Praise, Intimidate, Preach, Enlighten,
Condemn, Defraud, Insult, Mimic Darlavon, gun access, and monster/social support hooks.

Vanilla problems:

- Orator is often perceived as weak because many speech actions are unreliable or campaign-focused;
- Brave/Faith manipulation can dominate builds if permanent or too efficient;
- recruitment can be either irrelevant or campaign-breaking;
- guns can be underused if Orator is treated only as a speech gimmick;
- social control can overlap Mystic unless morale/recruitment/gun identity is protected.

Accepted high-level role: social battlefield manipulator.

Primary role: `controller`

Secondary tags: `recruit`, `gun`

Growth profile: `hybrid`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `gun`, `knife`, `fists`.

Armor class as target: `leather`.

Supported damage modes: `missile`, `thrust`, `crush`.

Formula v0.2 coupling:

- guns are `wp_wp` missile pressure, so Orator can contribute without PA investment;
- because guns ignore PA and MA, `Equip Guns` is a high-risk universal damage patch for stat-starved
  jobs and requires explicit T2.1/F4 incidence checks;
- gun access must stay distinct from Chemist's reliable item/gun identity;
- Orator is the primary Brave/morale speech owner; Mystic remains the primary Faith/spiritual owner;
- Brave/Faith speech touches global formula assumptions and requires F4/F5 scrutiny;
- recruitment/control effects require T8, with permanent roster value deferred to campaign policy;
- Orator should win through morale, speech, and gun-backed control, not raw damage.

### Action Skillset Goals

Speechcraft should make the battlefield social state matter.

The player should ask:

- can I change morale or Faith to create a tactical window?
- is recruitment/control worth the action and campaign implications?
- should I shoot now, speak now, or set up a future caster/physical action?
- can I use a gun to stay useful when speech is resisted?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Entice` | recruit/charm control | Temporarily sway an eligible target in battle. | T8 plus deferred roster policy; battle control must be bounded. |
| `Stall` | tempo speech | Delay, distract, or speed-pressure a target through speech. | T5/T4; should not replace Time Mage Slow. |
| `Praise` | Brave increase | Raise ally Brave or confidence for a battle window. | Orator primary axis; battle-scoped; F5 if it drifts damage/reactions. |
| `Intimidate` | Brave reduction | Lower enemy Brave/reaction confidence or morale. | Battle-scoped by default; Brave stress rows required. |
| `Preach` | Faith increase | Secondary/weaker Faith setup when Orator supports casters. | Mystic owns stronger Faith control; F4/F5 required. |
| `Enlighten` | Faith reduction | Secondary/weaker Faith pressure or protection. | Mystic owns stronger Faith control; cannot invalidate magic. |
| `Condemn` | doom/death sentence | Apply delayed lethal pressure through judgment speech. | T4 accuracy, T5 countdown, immunity/undead rows; no boss/default answer. |
| `Defraud` | campaign/economy pressure | Preserve economy flavor only if campaign model supports it. | Deferred policy; not combat power by itself. |
| `Insult` | morale/status pressure | Apply anger, confusion, berserk, or similar social disruption. | T4/T5/T8; must not be broad hard control. |
| `Mimic Darlavon` | sleep/story control | Use speech to put eligible targets to sleep or lose focus. | T4/T5/T8; damage-break/counterplay required. |

Brave and Faith changes should be battle-scoped unless the deferred campaign policy deliberately
accepts permanent manipulation. The player should enjoy build planning, not feel forced to run
permanent stat chores.

Orator and Mystic split the morale/spiritual axis:

- Orator owns Brave/morale as primary through Praise and Intimidate;
- Mystic owns Faith/spiritual state as primary through Belief and Disbelief;
- each may touch the other's axis only as a weaker or more situational secondary line.

This prevents Speechcraft and Mystic Arts from becoming two interchangeable Faith/Brave setup
secondaries.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Bravery Surge` | morale reaction candidate | Let Orator-style morale respond under pressure. | Must not become universal Brave stacking; T2.1/F5/deferred policy. |
| `Faith Surge` | faith reaction candidate | Let Orator-style faith respond under pressure. | High F4/F5 risk; optional and likely narrower than vanilla. |

If ability slots require one reaction, prefer a narrower morale reaction over broad Faith stacking.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Equip Guns` | gun identity unlock | Let selected builds spend support slot to access guns. | Must not erase Chemist/Orator gun identity; T2.1. |
| `Tame` | monster/social control | Preserve monster interaction if later monster scope allows it. | Monsters currently out of scope; T8/deferred policy before concrete values. |
| `Beast Tongue` | monster speech access | Let speech interact with monster-like targets if retained. | Monsters out of current scope; no concrete values now. |

`Equip Guns` is a major identity lever. If too broad, Chemist and Orator lose their gun homes. If
too narrow or weak, gun identity is wasted.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Social Positioning` | speaker spacing | Help Orator maintain safe line/range for speech and guns. | Placeholder only; no universal mobility. |

Orator does not need a strong movement identity unless speech range/line constraints make the job
unplayable.

### JP Progression

JP posture:

- one Brave/Faith or morale tool and one combat-relevant control option should be reachable early;
- gun identity should be available early enough that Orator can contribute while speech is resisted;
- recruitment, Condemn, broad status, and Equip Guns should require real investment;
- campaign/economy speech should not be required for combat progression.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Orator should remain a mid specialist/controller route. It should not be buried so late that its
recruitment and speech fantasy never matters.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Orator donor patterns:

- caster borrows Orator for battle-scoped Faith setup at action cost;
- physical unit borrows Intimidate/Praise for Brave/reaction planning;
- gun utility build uses Orator active job to combine safe missile pressure with speech;
- controller party uses Entice/Insult/Mimic Darlavon for specific targets.

Unhealthy Orator donor patterns:

- every caster wants Preach/Enlighten setup;
- permanent Brave/Faith manipulation becomes a campaign chore;
- Equip Guns becomes the obvious support for too many jobs;
- Entice/recruit breaks encounter or roster progression;
- Orator is only useful outside combat.

### Expected Strong Builds

- active Orator with gun pressure and speech control;
- Orator with Items or Time Magicks secondary for utility/control;
- caster party using battle-scoped Faith setup;
- anti-caster or anti-reaction plan using Enlighten/Intimidate.

### Expected Weaknesses

- leather durability;
- speech accuracy and status immunity;
- lower raw damage than Archer/Chemist gun plans if speech does not matter;
- weak value against immune, mindless, or boss-like enemies;
- campaign actions competing with combat turns.

### Expected Counters

- speech/status immunity;
- Silence or anti-caster-like command denial if speech is spell-like;
- fast pressure before speech setup matters;
- low-value targets with no morale/recruitment leverage;
- enemy ranged pressure against leather.

### Ramza / Unique-Job Interaction

Ramza's future leadership identity may overlap with morale. He can be broadly inspirational, but
Orator should remain the dedicated social controller with recruitment, guns, and speech manipulation.

## Shared Scenario/Check Plan

Required later:

- `J-TH-EARLY-SELF`: Thief has useful knife pressure and one steal/disrupt action.
- `J-TH-MID-EQUIP`: equipment steals create tactical value without replacing Knight.
- `J-TH-STRESS-KNIGHT-BREAK`: Thief steal and Knight break have distinct cost/risk/reward profiles.
- `J-TH-LATE-UTILITY`: active Thief remains valuable beyond Ninja prerequisite planning.
- `J-TH-STRESS-CHARM`: Steal Heart has counterplay and does not solve hard enemies.
- `J-TH-STRESS-EQUIPLESS`: Thief still has a role against enemies without stealable equipment.
- `J-TH-STRESS-CAMPAIGN`: Steal Gil/EXP/Treasure Hunter are not mandatory grind chores.
- `J-OR-EARLY-SELF`: Orator has gun pressure and one useful speech control option.
- `J-OR-MID-MORALE`: Brave/Faith speech creates tactical windows without permanent chores.
- `J-OR-LATE-CONTROL`: Orator remains useful in combat, not only campaign/recruitment.
- `J-OR-STRESS-RECRUIT`: Entice/recruit is bounded as control and campaign value.
- `J-OR-STRESS-GUN`: Equip Guns and Orator gun identity do not erase Chemist.
- `J-OR-STRESS-EQUIP-GUNS`: `Equip Guns` does not become a universal damage patch for stat-starved
  jobs because guns are `wp_wp`.
- `J-OR-STRESS-MYSTIC-AXIS`: Orator's Brave/morale primary lane and Mystic's Faith/spiritual primary
  lane do not duplicate each other.
- `J-OR-STRESS-IMMUNE`: Orator has fallback value when speech/status is resisted.
- `M-SECONDARY-COUNT`: Steal and Speechcraft secondary incidence.
- `M-RSM-COUNT-LATE`: Sticky Fingers, Light Fingers, Poach, Move +2, Treasure Hunter, Bravery
  Surge, Faith Surge, Equip Guns, Tame, Beast Tongue, and Social Positioning.
- `I-EQUIPMENT`: steal/disarm, equipment suppression, armor response, enemy offense, and fallback.
- `I-CONTROL`: Charm, recruit, speech status, sleep/berserk/confuse-like outcomes, and AI behavior.
- `I-CAMPAIGN-POLICY`: deferred permanent loot, recruit, Brave/Faith, gil, EXP/JP, and
  campaign-grind policy rows.

Conditional:

- T7 and T6xT7 for Steal Weapon, Steal Armor, Steal Shield, and equipment/offense/armor outcomes.
- T4 for steal accuracy, status accuracy, line/range/facing, Charm, Sleep, Berserk, and speech hit
  assumptions.
- T5 for duration, damage-to-break, Slow/Stall, Condemn/Doom timing, and control windows.
- T8 for Charm, Entice, recruitment, AI/control, Tame, Beast Tongue, Insult, and Mimic Darlavon.
- Deferred campaign policy for Steal Gil, Steal EXP, permanent equipment theft, permanent
  Brave/Faith, recruitment, Defraud, Poach, Treasure Hunter, and campaign economy. These are not
  combat dual-sim gates.
- Gate F4/F5 if Brave/Faith manipulation, knife Speed scaling, gun access, equipment disruption, or
  accuracy supports drift the accepted formula ecology.

## Formula Re-Sim Requirement

No immediate Gate F5 re-sim is required for this document.

The concrete data pass must re-evaluate this if it:

- changes knife, gun, fist, or equipment access;
- changes Speed, PA, Brave, Faith, or hit formulas;
- changes steal accuracy, steal effect, or equipment-removal duration;
- changes gun support availability or `Equip Guns`;
- changes Brave/Faith values, permanence, or scaling;
- changes Charm, recruit, status, or AI-control assumptions;
- changes campaign economy enough to alter normal progression or grind.

## Implementation Assumptions

- Data modding can create or repurpose ability records.
- Skill names are placeholders where they are not vanilla names.
- Preserve recognizable FFT flavor: Steal, Steal Heart, equipment theft, Sticky Fingers,
  Speechcraft, Brave/Faith speech, Entice, Condemn, Mimic Darlavon, guns, Tame, and Beast Tongue
  should remain recognizable unless proof shows a specific effect cannot be bounded.
- Exact values must be decided after validation, not inside this V1 proposal.

## Open Proof Needs

- Exact steal rates, position rules, and equipment suppression duration.
- Whether steals are battle-scoped, permanent, or battle-scoped with post-battle reward.
- Exact Charm/recruit status behavior and damage-to-break rules.
- Whether Poach/Tame/Beast Tongue wait until monster scope opens.
- Exact Orator speech accuracy, range, line, and status mappings.
- Whether Brave/Faith changes can be safely battle-scoped in data.
- Whether `Equip Guns` can exist without broad gun-access identity loss.
- Exact deferred campaign economy policy for steal/recruit/economy skills.

## Claude Review Notes

Claude conditionally accepted this proposal on 2026-06-21 after requiring one scope change:
campaign economy and permanent-state policy must be deferred out of the combat dual-sim roadmap.

Recommended review additions also applied:

- Thief's `spd_pa_wp` thrust lane is called out as anti-mail precision, complementing Monk's
  anti-plate crush lane;
- Orator owns Brave/morale primarily while Mystic owns Faith/spiritual state primarily;
- `Equip Guns` is flagged as a high-risk stat-independent damage patch because guns are `wp_wp`;
- Thief steal is compared directly against Knight break as distinct risk/reward profiles;
- `Condemn` follows Doom discipline with T4 accuracy, T5 countdown, immunity, and undead rows.
