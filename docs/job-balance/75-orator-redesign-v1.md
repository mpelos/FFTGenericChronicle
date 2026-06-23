# Orator Redesign V1

Status: Accepted (GPT/Claude consensus) -- pending Marcelo validation
Date: 2026-06-23
Scope: Orator only

Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/74-orator-good-job-rediscussion-v0.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

## Proposal Header

Job: Orator
Status: Accepted (GPT/Claude consensus) -- pending Marcelo validation
Version: V1

Vanilla role: Speechcraft controller, human recruiter, monster communicator, Brave/Faith shaper,
and gun user.

Vanilla problems:
- many Speechcraft actions are low-odds turns that are usually worse than attacking;
- several actions overlap as weak single-target status buttons;
- Brave/Faith shaping is campaign-useful but can become tedious bookkeeping;
- recruitment and monster access are essential FFT texture but easy to delete by accident;
- Orator often becomes an `Equip Guns` detour instead of a job the player wants to field.

Accepted high-level role: social battlefield controller with durable recruitment and roster-shaping
value.

Primary role: `controller`

Secondary tags: `recruit`, `gun`

Growth profile: hybrid

Current multipliers: inherited from the current role map until a later numeric pass.

Proposed multipliers: no multiplier change in this artifact.

Equipment access: leather profile; guns, knives, and fists remain the supported weapon identities
from the current role map.

Armor class as target: leather.

Supported damage modes: missile through guns, thrust through knives, crush through fists.

Formula v0.2 coupling:
- guns keep the `wp_wp` missile identity and remain a high-risk support export;
- knives and fists keep their current formula-family identities;
- Brave/Faith point shifts are stat changes, not damage or healing formulas, so they stay flat and
  readable rather than scaling numerically.

## Vanilla Diagnosis

Orator promises a social specialist: a unit who can recruit enemies, speak to monsters, reshape
Brave and Faith, inflict social statuses, and fall back to guns. That fantasy is distinctive and
worth preserving.

The vanilla job fails mainly through action economy. Spending a full turn on a low-reliability,
low-impact speech effect often loses to attacking, healing, or killing the target. At the same time,
Orator's campaign value is real: recruitment, monster access, and permanent Brave/Faith correction
are meaningful long-term FFT systems.

The rejected V0 package corrected some readability problems but overcorrected by removing or
weakening essential value:

- `Tame` and `Beast Tongue` were removed as if monster systems could be ignored;
- permanent Brave/Faith repair disappeared, leaving bad recruits permanently bad;
- `Stall`, `Praise`, `Intimidate`, `Preach`, and `Enlighten` were too small to justify their turns;
- several combat statuses remained too low-impact to compete with attacking.

## Job Compass

Orator is the social controller and recruitment specialist.

The player should want Orator when they want to:

- recruit humans;
- preserve a route into monster recruitment, breeding, and Poach setup;
- repair or shape Brave/Faith over the campaign;
- bend one important enemy turn through speech;
- support a caster or anti-caster plan through Faith pressure;
- use guns as a reliable fallback when speech is resisted.

Orator is better than any other job at recruitment and permanent Brave/Faith shaping.

Orator should not become:

- a pure out-of-battle stat chore;
- a better Mystic;
- a better Time Mage;
- a universal hard-disable job;
- a raw damage job carried only by guns.

## Protected Systems And Access Promises

| System | Current job-pass decision | How the player still accesses it | Future pass required | What must not be removed now |
| --- | --- | --- | --- | --- |
| Human recruitment | Preserved on `Entice`. | Eligible humans can still be recruited through Orator's social kit. | Recruitment accuracy and target restrictions can be tuned later. | The permanent human recruit route. |
| Monster recruitment and breeding | Preserved through `Tame` and `Beast Tongue`. | Player still uses Orator-derived monster supports to access monster teams. | Monster combat and breeding details belong to a later monster pass. | `Tame`, `Beast Tongue`, and a working monster access route. |
| Poach routing | Not changed here. | Poach remains outside Orator, but monster access keeps Poach/breeding teams viable. | Thief/monster/economy pass. | Monster access cannot vanish in a way that indirectly kills Poach planning. |
| Permanent Brave/Faith roster shaping | Restored and owned by Orator. | `Praise`, `Intimidate`, `Preach`, and `Enlighten` create bounded permanent drift. | Later playtest can tune drift rate. | Repairing bad recruits and shaping Brave/Faith builds. |
| Gun identity | Preserved. | Orator remains a gun home and can export `Equip Guns`. | Incidence and roster simulations. | Gun fallback and gun-build route. |
| Gil economy | Not touched. | Ordinary Gil sources remain. | None in this job pass. | No item prices, rewards, or Gil values are changed. |

## Mechanic Preservation List

| Mechanic | Why it was good | What was too complex or wrong | Simpler visible form | Decision |
| --- | --- | --- | --- | --- |
| Silence blocks Speechcraft | Clear counterplay. | No issue. | Keep. | Keep |
| Visible vanilla statuses | Player can read the fight. | Old broad status list was too weak. | Use stronger single-purpose visible statuses. | Keep |
| Brave/Faith speech | Core Orator identity and recruit repair. | V0 made it battle-only and too small. | Large battle shift plus bounded permanent drift. | Rework |
| Recruitment | Core FFT campaign texture. | V0 deferred too much. | `Entice`, `Tame`, and `Beast Tongue` preserve routes now. | Rework |
| Condemn vs Doom Fist | Two jobs can own different Doom delivery. | Needs explicit distinction. | Orator: ranged/no damage/slower; Monk: adjacent/chip/faster. | Keep |
| Fast Talk | Non-Brave reaction diversity. | Old CT bite risked being flavor only. | Larger capped CT bite, once per round. | Rework |
| Equip Guns | Strong build hook and Orator fallback. | Export can patch too many builds. | Keep high-risk watch and late/high-JP posture. | Keep |
| Defraud | Vanilla Gil flavor. | Gil is out of scope and Confuse is redundant. | Remove from this kit. | Remove |

## Skill Inventory

| Skill | Source | Decision | Value layer | Reason |
| --- | --- | --- | --- | --- |
| `Entice` | Vanilla | Rework | combat / campaign / roster | Preserves human recruitment and battle allegiance pressure. |
| `Stall` | Vanilla | Rework | combat | Becomes real CT denial and charge interruption. |
| `Praise` | Vanilla | Rework | combat / roster / build | Brave-up setup and permanent repair. |
| `Intimidate` | Vanilla | Rework | combat / roster / build | Brave-down pressure and permanent shaping. |
| `Preach` | Vanilla | Rework | combat / roster / build | Faith-up setup and permanent repair. |
| `Enlighten` | Vanilla | Rework | combat / roster / build | Faith-down countersetup and permanent shaping. |
| `Condemn` | Vanilla | Rework | combat | Ranged delayed KO pressure. |
| `Defraud` | Vanilla | Remove | none | Gil changes are out of scope; Confuse overlaps the control kit. |
| `Insult` | Vanilla | Rework | combat | Berserk as anti-caster/support disruption. |
| `Mimic Darlavon` | Vanilla | Rework | combat | Sleep as a one-target pause. |
| `Fast Talk` | Prior proposal | Rework | reaction | Non-Brave verbal tempo reaction. |
| `Equip Guns` | Vanilla | Keep | support / build | Gun identity and export hook. |
| `Silver Tongue` | Prior proposal | Keep | support / build | Speechcraft specialization. |
| `Tame` | Vanilla | Keep with access promise | campaign / monster / build | Monster recruitment and breeding route. |
| `Beast Tongue` | Vanilla | Keep with access promise | campaign / monster / build | Speech interaction with monsters. |
| `Social Positioning` | Prior proposal | Keep | movement / build | Orator-specific positioning. |

## Skill Cards

### Entice

Source: vanilla
Decision: rework
Value layer: combat / campaign / roster

Function:

- Eligible human, non-boss targets only.
- Battle effect: visible `Charm` or `Traitor`-style allegiance pressure for one controlled action or
  until damage breaks it.
- Campaign effect: preserves the permanent human recruitment route.
- Battle Charm is the in-combat effect; permanent recruitment resolves through final recruit
  eligibility, target, and end-of-battle rules.

Why the player uses it:

- In combat, a dangerous enemy turn may be worth more controlled than damaged.
- In campaign, recruitment is durable roster value.

Immediate or durable impact:

- Immediate enemy action redirection.
- Possible permanent roster gain when recruitment conditions pass.

Visibility:

- Uses visible allegiance/control behavior.

Cost and constraints:

- Full action, target restrictions, immunity/protection checks, and a lower reliability ceiling than
  ordinary soft-control because recruitment has campaign value.

What prevents spam or mandatory grind:

- Boss/protected targets are immune.
- Accuracy and eligibility restrict snowballing.
- Recruitment is valuable but not required to clear the game.

What breaks if removed:

- Human recruitment stops being an Orator signature.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it when the target's next action is more valuable than a normal attack's damage, or when the
  long-term recruit payoff is worth the current turn.

Verdict: keep as Orator's human recruitment and allegiance-control action.

### Stall

Source: vanilla
Decision: rework
Value layer: combat

Function:

- Speech-based tempo bite.
- Target loses 50 CT.
- If the target is `Charging` or `Performing`, `Stall` interrupts or cancels the pending action and
  then applies CT pressure.
- Carries a base success rate like other enemy-control Speechcraft. It is not automatic.

Why the player uses it:

- To deny a spell, Aim, performance, or immediate enemy turn.

Immediate or durable impact:

- Immediate tempo swing.

Visibility:

- CT change is visible through turn-order delay; interrupted action state is visible by action
  cancellation.

Cost and constraints:

- Full action, single target, no damage, immunity/protection rules.
- Because it spends the Orator's full turn, suppressing one target is a 1:1 tempo trade.
- Sub-100% success means a fast target still acts sometimes; this is a strong tempo hit, not free
  perpetual denial.

What prevents spam or mandatory grind:

- It is not `Slow`, `Stop`, `Quick`, or Speed manipulation.
- It does not create an ongoing tempo engine.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it when delaying or interrupting the next enemy action prevents more harm than one Orator gun
  attack would create.

Verdict: keep as a real one-shot tempo action, not a Time Mage replacement.

### Praise

Source: vanilla
Decision: rework
Value layer: combat / roster / build

Function:

- Ally morale speech.
- Battle effect: Brave +20 up to the normal battle ceiling.
- Clears `Chicken` if present.
- Permanent effect: the first successful Brave-up speech on a roster unit per battle creates +3
  permanent Brave after battle, capped at 85.

Why the player uses it:

- To enable Brave-sensitive attacks, reactions, morale recovery, and Brave builds.
- To repair low-Brave recruits over the campaign.

Immediate or durable impact:

- Immediate Brave window.
- Durable roster correction.

Visibility:

- Battle Brave is visible on the unit.
- Permanent drift is visible in roster Brave after battle and should be surfaced in battle results.

Cost and constraints:

- Full action, ally-targeted setup, one permanent same-axis drift per unit per battle.

What prevents spam or mandatory grind:

- Permanent cap 85 prevents Brave 97 degeneracy.
- Per-battle permanent cap prevents one battle from becoming stat bookkeeping.
- The battle effect is strong, but it costs a turn and needs Brave-sensitive payoff.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it when an ally's Brave-sensitive output/reaction value this round or next beats the Orator's
  own attack, or when the campaign repair value is the reason for the turn.

Verdict: keep as Brave-up combat setup and bounded permanent repair.

### Intimidate

Source: vanilla
Decision: rework
Value layer: combat / roster / build

Function:

- Morale pressure.
- Battle effect: Brave -20 down to the normal battle floor.
- If the target reaches `Chicken` range, visible Chicken behavior follows final status rules.
- Permanent effect: the first successful Brave-down speech on a roster unit per battle creates -3
  permanent Brave after battle, floored at 40.

Why the player uses it:

- To reduce Brave-sensitive output, reaction confidence, and aggression pressure.
- To intentionally shape low-Brave roster builds where those remain useful.

Immediate or durable impact:

- Immediate morale pressure.
- Durable roster shaping.

Visibility:

- Battle Brave and `Chicken` behavior are visible.
- Permanent drift is visible in roster Brave after battle.

Cost and constraints:

- Full action, target restrictions, one permanent same-axis drift per unit per battle.

What prevents spam or mandatory grind:

- Permanent floor 40 prevents accidental roster ruin.
- Normal-Brave enemies are pressured, not automatically disabled.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it against Brave-keyed enemies, reaction-heavy targets, or already-low-Brave units where morale
  collapse is better than chip damage. Use on roster units when campaign shaping is the value.

Verdict: keep as Brave-down pressure and bounded permanent shaping.

### Preach

Source: vanilla
Decision: rework
Value layer: combat / roster / build

Function:

- Faith-up speech.
- Battle effect: Faith +20 up to the normal battle ceiling.
- Permanent effect: the first successful Faith-up speech on a roster unit per battle creates +3
  permanent Faith after battle, capped at 85.

Why the player uses it:

- To create a real caster burst or healing window.
- To repair low-Faith recruits and support magic builds.

Immediate or durable impact:

- Immediate Faith-scaling payoff.
- Durable magic-build correction.

Visibility:

- Battle Faith is visible on the unit.
- Permanent drift is visible in roster Faith after battle.

Cost and constraints:

- Full action, single target, double-edged because the target becomes more receptive to magic.

What prevents spam or mandatory grind:

- Permanent cap 85 prevents extreme Faith degeneracy.
- Orator remains weaker than Mystic at broad in-battle spiritual control.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it when an ally caster or healer can capitalize immediately or next round for more total value
  than the Orator's attack, or when the campaign repair value is the point of the turn.

Verdict: keep as Faith-up setup and bounded permanent repair.

### Enlighten

Source: vanilla
Decision: rework
Value layer: combat / roster / build

Function:

- Faith-down speech.
- Battle effect: Faith -20 down to the normal battle floor.
- If a visible `Faith` status is used elsewhere, `Enlighten` can remove or counter it.
- Permanent effect: the first successful Faith-down speech on a roster unit per battle creates -3
  permanent Faith after battle, floored at 35.

Why the player uses it:

- To weaken enemy magic, healing, and Faith-based status reliability.
- To shape low-Faith physical builds over the campaign.

Immediate or durable impact:

- Immediate anti-caster or anti-healer pressure.
- Durable roster shaping.

Visibility:

- Battle Faith is visible on the unit.
- Permanent drift is visible in roster Faith after battle.

Cost and constraints:

- Full action, single target, does not apply permanent `Atheist`.

What prevents spam or mandatory grind:

- Permanent floor 35 prevents near-magic-immunity degeneracy.
- It weakens one target; it does not invalidate magic globally.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it when the target's Faith-scaled magic, healing, or status is the main threat, or when
  campaign Faith shaping is the value.

Verdict: keep as Faith-down countersetup and bounded permanent shaping.

### Condemn

Source: vanilla
Decision: rework
Value layer: combat

Function:

- Applies visible `Doom`.
- Countdown target: 4.

Why the player uses it:

- To pressure high-HP or heavily armored enemies that cannot be burst efficiently.

Immediate or durable impact:

- Delayed KO threat and forced response.

Visibility:

- Visible `Doom`.

Cost and constraints:

- Full action, no damage rider, immunity/protection checks.

What prevents spam or mandatory grind:

- Doom can be cured or raced.
- Boss/protected targets can be immune.
- No instant KO.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it when the enemy will live through ordinary damage but a countdown forces a better tactical
  outcome.

Verdict: keep as ranged delayed lethal pressure.

Condemn vs Monk `Doom Fist`:

| Skill | Job | Delivery | Damage | Countdown | Read |
| --- | --- | --- | --- | ---: | --- |
| `Condemn` | Orator | ranged speech | none | 4 | Safer reach, slower payoff, no chip. |
| `Doom Fist` | Monk | adjacent fist pressure | chip damage | faster | Riskier physical pressure point. |

### Insult

Source: vanilla
Decision: rework
Value layer: combat

Function:

- Applies visible `Berserk` for one forced enemy action or until damage breaks it.

Why the player uses it:

- To turn a caster, healer, or controller into a basic attacker.

Immediate or durable impact:

- Converts a dangerous command turn into a simpler attack turn.

Visibility:

- Visible `Berserk`.

Cost and constraints:

- Full action, single target, immunity/protection checks.

What prevents spam or mandatory grind:

- Can backfire against targets with dangerous basic attacks.
- One forced action or damage break prevents long soft-lock chains.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it when the target's spell or support action is more dangerous than its basic attack.

Verdict: keep as forced-offense disruption.

### Mimic Darlavon

Source: vanilla
Decision: rework
Value layer: combat

Function:

- Applies visible `Sleep`.
- The target misses one action or wakes on damage.

Why the player uses it:

- To isolate a dangerous unit that the party does not intend to damage immediately.

Immediate or durable impact:

- Temporary action denial and repositioning window.

Visibility:

- Visible `Sleep`.

Cost and constraints:

- Full action, single target, immunity/protection checks.

What prevents spam or mandatory grind:

- Damage breaks it.
- One missed action prevents encounter locks.

Why use this instead of attacking, killing, healing, or moving to safety?

- Use it when pausing the target creates a better setup or survival window than chip damage.

Verdict: keep as the clean pause control.

### Fast Talk

Source: prior proposal
Decision: rework
Value layer: reaction

Function:

- Triggers when targeted by a single-target enemy action and the Orator remains able to respond.
- Fixed/capped 50% success.
- Attacker loses 20 CT after resolution.
- Once per round.

Why the player uses it:

- To punish repeated single-target pressure without relying on Brave.

Immediate or durable impact:

- Defensive tempo drag.

Visibility:

- Attacker turn delay is visible through turn order.

Cost and constraints:

- Reaction slot, no damage prevention, no counterattack, no status, no Brave scaling.

What prevents spam or mandatory grind:

- Once per round.
- Does not stop the incoming action.

Why is this worth the slot?

- Orator and other utility builds can choose tempo friction over raw evasion, healing, or
  counter-damage.

Which builds should not want it?

- Builds that need survival prevention, damage retaliation, or broader defensive coverage.

Verdict: keep as non-Brave reaction texture.

### Equip Guns

Source: vanilla
Decision: keep
Value layer: support / build

Function:

- Grants existing gun access.

Why the player uses it:

- To give stat-poor or utility jobs PA/MA-independent missile pressure.

Immediate or durable impact:

- Build route and fallback damage.

Visibility:

- Equipment access is visible.

Cost and constraints:

- Support slot, high JP / late export posture, no new guns, no formula change.

What prevents spam or mandatory grind:

- Single support slot competes with core build supports.
- Incidence testing must catch universal-export risk.

Why is this worth the slot?

- It creates a deliberate gun build, not a passive damage multiplier.

Which builds should not want it?

- Jobs with strong weapon identity, strong spell identity, or better support-slot demands.

Verdict: keep, but watch as the largest Orator export risk.

### Silver Tongue

Source: prior proposal
Decision: keep
Value layer: support / build

Function:

- Speechcraft specialization.
- Enemy Speechcraft success +15 percentage points.
- Does not change ally permanent Brave/Faith drift frequency.

Why the player uses it:

- To commit the support slot to reliable speech control.

Immediate or durable impact:

- Better enemy-control reliability.

Visibility:

- Support choice is visible; success rates should be documented.

Cost and constraints:

- Support slot, Speechcraft only, no immunity bypass.

What prevents spam or mandatory grind:

- Does not affect non-Speechcraft status, weapons, spells, items, or monster access by itself.

Which builds should not want it?

- Builds using Orator only for `Equip Guns`, monster access, or another secondary plan.

Verdict: keep as Orator self-synergy support.

### Tame

Source: vanilla
Decision: keep with access promise
Value layer: campaign / monster / build

Function:

- Preserves monster recruitment and breeding access.
- Enables eligible weakened monster taming/recruitment according to final monster rules.

Why the player uses it:

- To build monster teams and support Poach/breeding routes.

Immediate or durable impact:

- Durable roster and item-routing value.

Visibility:

- Recruitment result is visible through roster change.

Cost and constraints:

- Support slot, monster eligibility rules, no monster combat rebalance in this pass.

What prevents spam or mandatory grind:

- It competes with other support skills and only matters if the player wants the monster system.

What breaks if removed:

- Monster recruitment, breeding, and Poach planning lose an essential access route.

Verdict: keep as protected monster-system access.

### Beast Tongue

Source: vanilla
Decision: keep with access promise
Value layer: campaign / monster / build

Function:

- Preserves speech interaction with monsters.
- Lets Speechcraft target eligible monsters and/or enables monster recruitment speech where final
  monster rules allow it.

Why the player uses it:

- To make Orator's speech identity apply to non-human targets.

Immediate or durable impact:

- Monster-team access and monster-control texture.

Visibility:

- Target eligibility and recruitment/control outcomes are visible.

Cost and constraints:

- Support slot, monster eligibility rules, no hidden monster overhaul.

What prevents spam or mandatory grind:

- Only useful for monster-facing builds and competes with `Tame`, `Silver Tongue`, and `Equip Guns`.

What breaks if removed:

- Orator loses the monster-speech part of its FFT identity.

Verdict: keep as protected monster speech access.

### Social Positioning

Source: prior proposal
Decision: keep
Value layer: movement / build

Function:

- Move +1.
- Speechcraft range +1.

Why the player uses it:

- To keep Orator in speech range without turning the job into a mobility specialist.

Immediate or durable impact:

- Better speech positioning.

Visibility:

- Movement and range are visible.

Cost and constraints:

- Movement slot.
- No gun range, spell range, item range, global status range, terrain bypass, or elevation bypass.

Which builds should not want it?

- Builds that need raw traversal, teleport-like movement, terrain bypass, or jump/elevation tools.

Verdict: keep as Orator-specific movement.

## Kit Assembly

The accepted action kit for this V1 review is:

- `Entice`
- `Stall`
- `Praise`
- `Intimidate`
- `Preach`
- `Enlighten`
- `Condemn`
- `Insult`
- `Mimic Darlavon`

`Defraud` is removed from this kit. Its Gil identity is out of scope, and a Confuse replacement would
overlap too much with Charm, Berserk, and Sleep while giving the player a less readable outcome.

The support set asks for real choices:

- `Equip Guns` for gun builds;
- `Silver Tongue` for better enemy Speechcraft reliability;
- `Tame` for monster recruitment and breeding access;
- `Beast Tongue` for monster-facing Speechcraft.

The active Orator should play as a gun-backed single-target controller. A healthy Orator alternates
between a fallback gun turn and a speech action chosen for a specific target state.

## Visibility And Complexity Audit

| Effect | Representation | Duration | Stack rule | Complexity verdict |
| --- | --- | --- | --- | --- |
| Allegiance pressure | visible `Charm` / `Traitor` behavior | one controlled action or damage break | no stacking | readable |
| CT pressure | turn-order delay / interrupted action | immediate | no persistent mark | readable |
| Brave battle shift | visible Brave value | battle-scoped | normal stat bounds | readable |
| Faith battle shift | visible Faith value | battle-scoped | normal stat bounds | readable |
| Permanent Brave/Faith drift | post-battle roster value | campaign | once per unit per axis per battle | readable |
| Doom | visible `Doom` | countdown | normal status rules | readable |
| Berserk | visible `Berserk` | one forced action or damage break | no stacking | readable |
| Sleep | visible `Sleep` | one missed action or damage break | no stacking | readable |

No new hidden mark, wound, token, social stack, or invisible party flag is introduced.

Permanent Brave/Faith floors and caps are intentional anti-degeneracy limits:

- Brave raise cap 85 avoids the old extreme Brave optimization becoming mandatory.
- Brave lower floor 40 avoids accidental roster ruin.
- Faith raise cap 85 avoids extreme Faith volatility.
- Faith lower floor 35 prevents near-magic-immunity exploits.

The battle effect remains allowed to be strong. The permanent bounds constrain campaign drift, not
the immediate battle window.

## Reaction Skills

Accepted reaction for this Orator package:

- `Fast Talk`: non-Brave verbal tempo reaction, fixed/capped success, CT bite, once per round.

Rejected reaction direction:

- Brave/Faith surge reactions are not used here. They risk reintroducing Brave/Faith optimization as
  passive upkeep rather than deliberate Orator actions.

## Support Skills

Accepted supports:

- `Equip Guns`
- `Silver Tongue`
- `Tame`
- `Beast Tongue`

Support-slot pressure is intentional. Orator should create several different build routes, not one
obvious support choice.

## Movement Skills

Accepted movement:

- `Social Positioning`: Move +1 and Speechcraft range +1 only.

This helps Orator act without replacing stronger movement packages.

## JP Progression

Final JP costs are not accepted in this artifact. Directional progression:

- early: one usable speech action and basic Brave/Faith repair access;
- mid: stronger control choices and Social Positioning;
- late/committed: `Equip Guns`, `Silver Tongue`, and monster supports as major build choices.

`Equip Guns` should remain expensive and late enough that it is a deliberate build path, not a free
damage patch.

## Prerequisite Changes

No prerequisite change is accepted here. Current draft context has Orator as a mid-branch controller
with Mystic/Chemist grounding. This may be revisited after the revised job kit is approved.

## Gender And Equipment Restrictions

No gender restriction changes in this artifact.

No equipment is added.

No Gil values are changed.

## Cross-Job Build Hooks

Expected strong builds:

- Orator active with guns and selected Speechcraft control;
- caster party using `Preach` for a burst/healing window;
- anti-caster plan using `Enlighten`;
- Brave-sensitive physical build using `Praise`;
- anti-reaction or anti-Brave enemy plan using `Intimidate`;
- monster-route party using `Tame` or `Beast Tongue`;
- utility job using `Equip Guns` as a support-slot commitment.

Expected weaknesses:

- Silence;
- status immunity;
- boss/protected target rules;
- leather durability;
- low raw damage;
- bad value when enemies ignore Brave, Faith, recruitment, and control;
- action economy pressure.

Expected counters:

- focus fire on the leather Orator;
- immune or protected targets;
- fast threats that punish setup;
- formations with no high-value single control target;
- magic or ranged pressure that forces Orator to defend instead of speak.

## Ramza / Unique-Job Interaction

Ramza may gain leadership, morale, and hybrid knight/mage identity. He should not become better than
Orator at recruitment, broad speech control, monster speech, or permanent Brave/Faith shaping.

Unique sword jobs may remain stronger at direct damage and flashy tactical pressure. Orator wins
through roster, speech, and targeted social leverage.

## Scenario And Check Plan

No numeric simulations are required before Marcelo validates the job concept. Later checks should
watch:

- permanent Brave/Faith drift rate: +3 may still be too slow for recruit repair;
- `Equip Guns` export incidence;
- `Stall` CT -50 plus charge interruption;
- `Preach` and `Enlighten` overlap with Mystic;
- control-chain reliability across `Entice`, `Condemn`, `Insult`, and `Mimic Darlavon`;
- recruitment snowball risk;
- whether `Defraud` removal leaves enough Speechcraft texture.

## Formula Re-Sim Requirement

No formula re-sim is required for this conceptual Orator review unless later changes alter weapon
formula families, gun timing, job multipliers, or broad Faith/Brave formula behavior.

## Implementation Assumptions

- Silence can block Speechcraft.
- Speechcraft can respect target immunity and boss/protected flags.
- Brave/Faith battle shifts and permanent roster drift can be represented by data/mod scripting.
- Existing status vocabulary can represent Charm/Traitor, Doom, Berserk, and Sleep.
- Monster access can remain available without balancing monster combat in this pass.

If an implementation assumption fails later, the design intent should be preserved with the closest
data-supported equivalent rather than silently deleting the system.

## Validation Gates

| Gate | Verdict | Notes |
| --- | --- | --- |
| Vanilla diagnosis | Pass | The proposal names Orator's real fantasy and vanilla failures. |
| Job compass | Pass | Social controller, recruitment, permanent Brave/Faith shaping, gun fallback. |
| Protected systems | Pass | Human recruit, monster route, Poach-adjacent access, Brave/Faith, guns preserved. |
| Skill value | Pass | Each skill has a stated combat or campaign reason. |
| Combat action value | Pass | Each combat action states when it beats attacking. |
| Campaign value | Pass | Recruitment, monster access, and roster shaping are explicit. |
| Visibility | Pass | Uses visible statuses, visible stat values, or roster numbers. |
| Complexity budget | Pass | No custom hidden states; `Defraud` removed to reduce overlap. |
| Mechanic preservation | Pass | Good V0 mechanics kept, rejected removals reversed. |
| Build texture | Pass | Supports and secondary usage create distinct routes. |
| Weaknesses | Pass | Silence, immunity, boss protection, leather, action economy. |
| No hidden removal | Pass | Essential FFT systems have explicit access promises. |

## Expected Player Use

The player uses Orator when they want something other than raw damage:

- recruit this enemy;
- shape this unit's Brave/Faith over time;
- stop this spell or performance now;
- make this caster waste a turn attacking;
- put this target to sleep because the party will not hit it yet;
- Doom this durable target because burst damage is inefficient;
- fire a gun when speech is not the right answer.

The job should feel like FFT Orator made playable, not like a different class dropped into FFT.

## Weaknesses And Counters

Orator remains fragile and answerable:

- Silence shuts down the core kit;
- status immunity and boss protection shut down many high-value actions;
- leather durability makes positioning matter;
- action cost means every setup must be chosen carefully;
- enemies that do not care about Brave, Faith, control, or recruitment reduce Orator to gun fallback;
- broad raw damage jobs remain better at killing.

## Open Risks

- `Stall` may be too strong if CT -50 plus interruption creates a repeatable lock.
- +3 permanent drift may be too slow for satisfying recruit repair.
- `Preach`/`Enlighten` may overlap Mystic if Mystic's final kit is not stronger or broader.
- `Equip Guns` may become too attractive for stat-poor jobs.
- Monster access is preserved, but final monster rules still need a later pass.
- Removing `Defraud` is clean design, but Marcelo may prefer preserving every recognizable
  Speechcraft name.

## Claude Review Verdict

Approved (GPT/Claude consensus) -- pending Marcelo validation.

## Human Validation Notes

This artifact responds to Marcelo's 2026-06-23 rejection of V0:

- `Tame` and `Beast Tongue` are no longer removed.
- Permanent Brave/Faith repair returns.
- `Stall` is no longer a small CT nudge.
- Brave/Faith speeches are no longer tiny buffs/debuffs.
- Combat actions now state why they can beat attacking.
- Campaign-facing actions justify long-term value instead of pretending to be combat turns.
