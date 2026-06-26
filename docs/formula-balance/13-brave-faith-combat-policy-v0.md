# Brave And Faith Combat Policy V0

Status: Accepted direction (Claude-reviewed); reaction-trigger change pending cross-phase re-sim
before final formula acceptance
Date: 2026-06-23
Review: Approved by Claude on 2026-06-23 with required scope and re-sim clarifications applied.
Depends on:
- `docs/formula-balance/09-combat-formula-design-frame.md`
- `docs/formula-balance/10-mitigation-and-scaling-policy-v0.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `docs/job-balance/10-healing-attrition-model-schema.md`
- `docs/job-balance/15-targeting-challenge-model-schema.md`

## Purpose

This document records the approved Brave/Faith direction for combat-system design.

It is not a job redesign document. It does not decide Orator, Mystic, reaction/support/move
inventories, Ramza, or any concrete job skill. It defines how Brave and Faith should behave as
global combat variables so later job and formula work can depend on a stable shared model.

## Core Decision

Use a named-symmetry model:

- Faith remains a broad systemic stat because FFT already makes Faith naturally double-edged:
  it improves magical output and magical reception.
- Brave remains an identity stat, but it is not allowed to become the universal answer for all
  physical offense, all reactions, and all defense at once.
- Brave keeps continuous damage use where the weapon family identity already depends on it:
  knight swords, katanas, and fists.
- Brave does not remain the default trigger formula for every reaction.
- Brave does not grant a passive global low-Brave evasion or high-Brave defensive immunity layer.
- Brave can create risks and advantages through named mechanics that the player can understand,
  such as morale pressure, challenge/provoke susceptibility, counter-bait, or courage-gated
  reactions.
- Faith should keep its caster/target interaction, but the floor from the v0.2 model prevents low
  Faith from making magic, healing, or magic-facing gameplay collapse into irrelevance.

The player-facing goal is still "better FFT", not a tabletop conversion. Brave and Faith should
feel like familiar FFT stats with cleaner incentives, not like a new opaque subsystem.

## Brave Identity

Brave means commitment, nerve, aggression, and willingness to hold ground.

Brave may affect:

- Brave-scaling physical routines where the family already uses it as identity;
- courage-flavored reactions such as counterattacks, stand-ground effects, intercepts, retaliation,
  and other mechanics where acting under threat is the fantasy;
- susceptibility to named morale tactics, especially effects that bait bold units into poor target
  choices or punish overcommitment;
- Chicken and other explicitly Brave-linked states if they remain part of the mod's status ecology.

Brave should not affect by default:

- generic reaction chance for every reaction in the game;
- shield, mantle, or equipment evasion;
- item reactions;
- magical reactions whose fantasy is Faith, spellcraft, or MP control;
- passive global damage reduction;
- passive global low-Brave evasion.

This preserves high-Brave as desirable for the right builds without making "raise Brave to 97" the
best answer for every unit.

## Faith Identity

Faith means magical openness, spiritual conviction, and receptivity.

Faith may affect:

- magical damage;
- magical healing;
- faith-based buffs and debuffs;
- magic-facing status accuracy or status susceptibility;
- spiritual effects where openness to supernatural force is the actual theme;
- Faith and Atheist status behavior, as explicit status overrides rather than normal-stat edge
  cases.

Faith should not affect by default:

- item healing;
- ordinary physical damage;
- physical evasion;
- physical break/steal-style success unless a specific action is explicitly supernatural;
- CT, range, movement, or turn order by itself.

Faith remains broadly systemic because it has an inherent tradeoff. A high-Faith mage should hit
harder, heal better with spells, receive stronger spell healing, and also be more vulnerable to
hostile magic and faith-facing statuses. A low-Faith fighter can resist magic better, but should not
be immune or become the best universal defensive chassis.

## Current Formula Compatibility

The v0.2 weapon and magic policy remains compatible with this decision.

The following Brave weapon routines stay legal:

```text
knight_sword: br_pa_wp
katana:       br_pa_wp
fists:        br_pa_pa
```

These routines are identity choices, not a claim that Brave should be attached to every physical
family.

The v0.2 magic model also stays legal:

```text
faith_factor = max(0.60, (casterFaith / 100) * (targetFaith / 100))
raw_magic = K * MA * faith_factor
```

The `0.60` floor is important. It keeps low-Faith play from invalidating magic, magical healing, or
magic-facing enemies. Faith still matters above the floor, especially for high-Faith casters and
targets.

## Formula Status And Re-Sim Requirement

This document is an accepted combat-system direction. It is not validated final formula data.

The v0.2 weapon and magic damage model is preserved and not reopened by this document:

- weapon families, routines, and damage types from v0.2 stay intact;
- the Faith floor stays intact;
- Two Hands, Two Swords, Attack Boost, Shell, and Protect constants stay intact.

The reaction-trigger change is formula-affecting because it changes expected reaction incidence,
incoming damage, outgoing damage, sustain, and attrition. Any later Faith soft ceiling would also be
formula-affecting.

Before this policy can be treated as final validated formula data, the affected models must pass
the cross-phase re-sim gate from the formula process, including reaction/attrition rows and
Brave/Faith stress rows.

## Reaction Trigger Policy

This policy supersedes the old shared default:

```text
trigger chance = min(Brave / 100, trigger_cap)
```

There is no longer a universal reaction trigger formula.

Every reaction must declare its trigger identity. Valid trigger identities include:

| Trigger identity | Expected scaling logic | Examples of fitting fantasies |
| --- | --- | --- |
| `brave_resolve` | Brave-influenced and capped | counter, intercept, stand-ground, retaliate |
| `critical_state` | HP state, KO state, or danger threshold | critical recovery, emergency quick, last stand |
| `item_reflex` | item availability, item tier, and reaction cap | Auto-Potion-style effects |
| `equipment_guard` | shield/evasion/equipment/profile based | parry, arrow guard, weapon block |
| `focus_training` | fixed or job/support-tuned chance | trained reflexes, discipline, specialist reactions |
| `faith_arcane` | Faith, spell type, or MP/magic context | magic counter, mana shield, spell absorb |
| `movement_reflex` | terrain, position, movement, or fall context | landing, reposition, disengage |

The exact numeric formula can vary by reaction. The important rule is that Brave is only used when
the reaction's identity earns it.

Global reaction safety rules still apply unless later superseded:

- one roll per triggering action;
- no reaction recursion;
- damage caused by reactions cannot trigger reactions;
- ordinary capped reactions keep a trigger cap or round cap where needed;
- strongest single mitigation channel applies when multiple mitigation reactions overlap.

The strongest-single-channel rule enforces the no-broad-practical-immunity principle from
`docs/job-balance/01-cross-job-build-principles.md`.

## Brave Bands For Simulation

Brave bands are validation anchors, not player-facing hard rules.

Use these anchors when stress-testing formulas and reaction policies:

| Band | Suggested value | Use |
| --- | ---: | --- |
| low | 30 | cowardice, Chicken-adjacent, low-commitment stress case |
| normal | 70 | default human benchmark |
| high | 85 | committed martial benchmark |
| extreme | 97 | optimized Brave stress case |

Damage formulas that already use Brave should read the continuous value. Morale and susceptibility
mechanics may use bands if the banding makes the mechanic easier to tune and reason about.

## Faith Bands For Simulation

Faith bands are also validation anchors.

| Band | Suggested value | Use |
| --- | ---: | --- |
| low | 30 | low-receptivity fighter or anti-magic stress case |
| normal | 70 | default human benchmark |
| high | 85 | committed caster benchmark |
| extreme | 97 | optimized caster/vulnerability stress case |

Faith calculations may use the continuous value. The v0.2 floor means low Faith still produces at
least `0.60` of the base faith factor for normal faith-based magic formulas.

## Named Brave Risk

High Brave needs risk, but not through hidden passive penalties.

Acceptable high-Brave risks:

- more susceptibility to challenge, taunt, provoke, duel-bait, or other named target-pressure
  mechanics;
- stronger likelihood of taking the obvious aggressive option under a control or morale effect;
- exposure to counter-bait if the high-Brave unit keeps choosing direct attacks into known
  retaliation;
- tactical overcommitment where the player can see why the Brave unit was manipulated.

Avoid:

- invisible global damage vulnerability;
- passive low-Brave defensive optimization;
- punishing high Brave so hard that players regret using Brave-scaling weapons.

The right feel is "bold units can be baited", not "brave units secretly have worse stats".

## Low Brave Role

Low Brave should not become a free defensive build.

Acceptable low-Brave hooks:

- Chicken or cowardice pressure when Brave is pushed too low;
- named defensive composure effects if a future job or status explicitly owns that identity;
- avoidance of some challenge/provoke effects, because the unit is not eager to engage;
- campaign or utility hooks only when they are explicit and worth the opportunity cost.

Avoid:

- a universal passive evasion bonus;
- making low Brave the optimal state for mages or tanks.

## Faith Status And Atheist Status

Normal Faith uses the shared formula floor. Status effects may override that normal behavior only
when they are explicit, visible, and worth their action cost.

Working direction:

- `Faith` status should act like a visible temporary spike in receptivity. It can increase magical
  output and magical vulnerability, and it is allowed to enable strong combo turns when setup is
  required.
- `Atheist` status should be treated as an explicit anti-magic state, not as the normal low-Faith
  floor. If kept, it may suppress or greatly reduce faith-based magic, but it must remain visible,
  counterable, and narrow enough that it does not invalidate the magic ecosystem.

`Faith`-status/Oil-style combo play is allowed. The mod should not remove fun multi-action setups
just because they produce a spike. The balance line is that a combo requiring multiple characters,
turns, statuses, positioning, and counterplay can be powerful; an easy one-action loop that deletes
core encounter balance cannot.

Open item:

- A future Faith soft ceiling for extreme caster-Faith x target-Faith products remains sim-driven
  and undecided. It should be considered only if high-Faith combos collapse encounter balance after
  the stress rows are expanded.

## Healing Implications

Faith applies to magical healing unless a later accepted healing document creates a more specific
exception.

Item healing remains outside Faith. Auto-Potion-style reactions scale through item progression and
inventory availability, not Faith.

Non-item, non-spell healing must scale with the game if it is expected to stay useful across the
campaign. That scaling can come from attributes, level bands, max HP percentage, missing HP,
equipment, or another visible lever. Fixed early-game healing values should not be accepted as
late-game combat tools unless the skill is intentionally campaign/economy utility rather than
combat sustain.

## Status Accuracy Implications

Faith should affect faith-facing magical and spiritual statuses.

It should not be the default accuracy stat for all statuses. Physical, tactical, or equipment
statuses should use the relevant delivery model: weapon accuracy, evasion, range, CT, positioning,
immunity, equipment, or explicit skill reliability.

This prevents Faith from becoming a second universal accuracy stat while preserving its identity
for spells and spiritual effects.

## Targeting And Challenge Implications

The existing targeting/challenge model remains valid but needs Brave-band rows.

Soft challenge and related target-pressure mechanics can use Brave as a susceptibility modifier.
The intended behavior:

- high-Brave units are easier to bait when the bait appeals to aggression, honor, retaliation, or
  direct confrontation;
- low-Brave units are less eager to accept direct bait, but may remain vulnerable to fear or
  panic-flavored effects;
- lethal opportunities, self-preservation, control states, forced-target immunity, and mission
  objectives can still override the bait.

This keeps Brave relevant in AI and target selection without turning it into raw defense.

## Required Follow-Up Updates

The following existing project artifacts must be updated after Claude review:

1. `docs/job-balance/01-cross-job-build-principles.md`
   - Records the no-universal-default reaction trigger-identity model (the old
     `58-physical-foundation-rsm-concrete-v0.md` producer was consolidated into the per-job
     decision docs and this principle).
2. `work/gpt-physical-foundation-rsm-concrete-v0.json`
   - Replace shared `reaction_chance_formula` assumptions with per-reaction trigger identity.
3. `work/sim-inputs-v0.2.1.json`
   - Replace `rsm_constants.reaction_policy.chance_formula` with the new no-universal-default
     policy or mark it superseded.
4. `docs/formula-balance/08-scenario-set-v0.md`
   - Expand `S-STRESS-BRAVE-FAITH` into normal/high/low/extreme Brave and Faith cases.
5. `docs/job-balance/15-targeting-challenge-model-schema.md`
   - Add future Brave-band challenge susceptibility rows.
6. `docs/job-balance/10-healing-attrition-model-schema.md`
   - Add the scaling premise for non-item healing and preserve item healing as item-progression
     based.

## Acceptance Checks

A future formula, reaction, status, or targeting proposal passes this policy only if it answers:

1. Is Brave being used because this mechanic is actually about courage, commitment, or resolve?
2. Is Faith being used because this mechanic is actually magical, spiritual, or receptive?
3. If this is a reaction, what trigger identity does it declare?
4. Can high Brave become mandatory for too many builds?
5. Can low Brave become a free defensive exploit?
6. Can low Faith invalidate magic or magical healing despite the floor?
7. Are combo spikes allowed because they require real setup, or are they easy loops?
8. Is every new risk or susceptibility visible enough for the player to understand?
