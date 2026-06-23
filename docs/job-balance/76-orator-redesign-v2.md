# Orator Redesign V2

Status: Approved (GPT/Claude consensus) -- pending Marcelo validation
Date: 2026-06-23
Claude Review Verdict: Approved on 2026-06-23 after requiring `Call Out` to use a named visible
marker tied to the challenge/taunt model.
Scope: Orator only

Supersedes:
- prior Orator Redesign V1 from `docs/job-balance/75-orator-redesign-v1.md` in git history;
- `docs/job-balance/74-orator-good-job-rediscussion-v0.md` where this document conflicts;
- Orator rows in docs `22`, `55`, `58`, and `60` where this document conflicts.

Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/formula-balance/13-brave-faith-combat-policy-v0.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

## Purpose

This document rewrites Orator after the accepted Brave/Faith combat policy.

The goal is not "vanilla Speechcraft with larger numbers." Orator should be a real active job: a
social battlefield controller who recruits, rallies, shames, baits, exposes targets to magic,
suppresses magic, and falls back to guns when speech is the wrong turn.

This pass does not change Gil values, add equipment, finalize monster-system details, or silently
rewrite Thief. It does record the required Thief follow-up for the social charm identity.

## Vanilla Diagnosis

Vanilla Orator promises a distinctive job:

- human recruitment;
- monster communication;
- Brave and Faith manipulation;
- social statuses;
- guns as a low-stat fallback.

The job fails because most Speechcraft turns are too unreliable, too small, or too disconnected
from immediate battle value. A player rarely wants to spend a turn on tiny Brave/Faith movement or
low-odds status when attacking, healing, disabling, or killing is available.

The first V1 redesign still preserved too much of that problem. It kept the shape of the vanilla
list, made several effects battle-only but too small, and removed or deferred monster access too
aggressively. V2 keeps the FFT texture but redesigns Orator around meaningful social control.

## Dependent Global Systems

### Brave

Current policy:

- Brave is no longer the universal reaction trigger.
- Brave remains meaningful through Brave-scaling weapon families, courage reactions, Chicken,
  morale, challenge/provoke susceptibility, and named overcommitment pressure.

Design consequence:

- Orator's Brave tools must not be generic maintenance buffs.
- Orator can own morale pressure, rallying, shaming, and baiting bold enemies because those are now
  named Brave uses.

### Faith

Current policy:

- Faith remains broadly systemic and double-edged.
- Normal magic uses the v0.2 Faith floor.
- `Faith` status may be used as a visible temporary receptivity spike.
- `Atheist` may be used as a visible narrow anti-magic state.

Design consequence:

- Small Faith deltas are not enough. Orator should use visible `Faith` and `Atheist` windows.
- Orator's Faith tools are single-target speech setups; Mystic remains the deeper spiritual
  controller and caster.

### Recruitment And Monster Access

Current policy:

- Human recruitment is essential Orator texture.
- Monster recruitment, breeding, and Poach routing must not disappear by accident.
- Full monster balance is outside this specific job pass.

Design consequence:

- `Entice`, `Tame`, and `Beast Tongue` remain protected routes.
- Monster details may be deferred, but player access is not removed.

### Guns

Current policy:

- Guns use `wp_wp` missile pressure and are useful on low-stat jobs.
- `Equip Guns` is a high-risk support export because it can patch many builds.

Design consequence:

- Orator keeps guns as a fallback and identity hook.
- `Equip Guns` remains a watch item for incidence and JP timing.

## Job Compass

Orator is the social controller and recruitment specialist.

The player should use Orator when they want to:

- flip an enemy into an ally and possibly recruit them;
- pressure a high-Brave enemy into bad targeting;
- shame, intimidate, or rally morale;
- create a visible magic burst window with `Faith`;
- create a visible anti-magic window with `Atheist`;
- interrupt an important pending action;
- bring gun fallback damage on a utility job.

Orator should be better than every other job at social control, recruitment, and morale pressure.

Orator should not become:

- a Mystic replacement;
- a Time Mage replacement;
- a pure gun job;
- a permanent-stat grind chore;
- a generic hard-disable chain.

## Creative Identity Question

Question:

```text
Is there something Orator should do that would be much cooler, more tactical, or more memorable
for its identity, even though vanilla does not have it?
```

Answer: yes.

The non-vanilla identity mechanic is `Call Out`: a speech action that pressures target selection
through Brave bands.

Why it belongs:

- It uses the new Brave ecology directly.
- It makes Orator win by public pressure, shame, pride, and overcommitment.
- It creates a controller turn that is not just another status ailment.
- It is visible through the `Called Out` marker, targeting behavior, and CT/morale effects.

Rejected alternative:

- `Sow Discord`, a stronger enemy-versus-enemy manipulation concept, was rejected for V2 because it
  is harder to express readably, overlaps Confuse, and risks unclear duration/ownership rules.

## Identity Pillars Versus Vanilla Skill Names

Identity pillars:

- social control;
- recruitment;
- morale and Brave ecology;
- Faith/Atheist setup windows;
- speech-based tempo disruption;
- gun fallback.

Vanilla names that remain important:

- `Entice`;
- `Stall`;
- `Praise`;
- `Intimidate`;
- `Preach`;
- `Enlighten`;
- `Condemn`;
- `Insult`;
- `Mimic Darlavon`;
- `Equip Guns`;
- `Tame`;
- `Beast Tongue`.

Vanilla names that are not protected:

- `Defraud`, because Gil changes are out of scope and its vanilla economy fantasy is not useful
  enough for this pass.

New name:

- `Call Out` replaces `Defraud`. Final localized naming may choose a better speech-flavor name,
  but the accepted design point is Brave-sensitive public pressure.

## Protected Systems And Access Promises

| System | Decision | Player access | Future pass |
| --- | --- | --- | --- |
| Human recruitment | Preserved through `Entice`. | Eligible humans can be flipped in battle and recruited after battle if conditions pass. | Recruitment tables, immunities, and exact rates. |
| Monster recruitment | Preserved through `Tame` and `Beast Tongue`. | Orator-derived supports keep monster communication/recruitment routes alive. | Monster/breeding/Poach pass. |
| Poach routing | Indirectly preserved. | Monster access keeps breeding and Poach planning possible. | Thief/monster/economy pass. |
| Permanent Brave/Faith gain | Vanilla positive-change behavior preserved. | `Praise` and `Preach` can still create ordinary permanent positive drift through the game's existing rule. | No custom cap/rate tuning in this pass. |
| Permanent Brave/Faith loss | Removed. | `Intimidate` and `Enlighten` are battle-only. | None unless global policy changes. |
| Guns | Preserved. | Native guns and `Equip Guns`. | JP/incidence tuning. |
| Gil economy | Not touched. | No Gil values, rewards, or prices change. | None in this pass. |

## Thematic Ownership Decisions

| Mechanic | Decision | Reason |
| --- | --- | --- |
| Social charm / Steal Heart identity | Consolidate into Orator `Entice`. | Persuasion and allegiance pressure fit Orator better than Thief. |
| Thief `Steal Heart` | Required follow-up, not silently changed here. | Thief is a separate accepted artifact and needs its own re-review. |
| Faith status setup | Keep on Orator as speech setup, not deep Faith engine. | Orator can create a single visible window; Mystic owns broader spiritual control. |
| Brave overcommitment | Keep on Orator through `Call Out` and `Insult`. | This is the cleanest job-level use of the new Brave policy. |
| Gil theft | Do not keep on Orator. | Gil is out of scope and not worth an active slot here. |

## Skill Inventory

| Skill | Slot | Source | Decision | Value layer |
| --- | --- | --- | --- | --- |
| `Entice` | Action | vanilla | Rework | combat / campaign / roster |
| `Stall` | Action | vanilla | Rework | combat |
| `Praise` | Action | vanilla | Rework | combat / roster / build |
| `Intimidate` | Action | vanilla | Rework | combat / morale |
| `Preach` | Action | vanilla | Rework | combat / roster / build |
| `Enlighten` | Action | vanilla | Rework | combat |
| `Condemn` | Action | vanilla | Rework | combat |
| `Call Out` | Action | new replacement for `Defraud` | Replace | combat / morale / targeting |
| `Insult` | Action | vanilla | Rework | combat / morale |
| `Mimic Darlavon` | Action | vanilla | Rework | combat |
| `Fast Talk` | Reaction | prior proposal | Rework | reaction / tempo |
| `Equip Guns` | Support | vanilla | Keep | support / build |
| `Silver Tongue` | Support | prior proposal | Keep | support / build |
| `Tame` | Support | vanilla | Keep with access promise | monster / roster / campaign |
| `Beast Tongue` | Support | vanilla | Keep with access promise | monster / roster / campaign |
| `Social Positioning` | Movement | prior proposal | Keep | movement / build |

## Skill Cards

### Entice

Function:

- Eligible human, non-boss, non-protected targets only.
- On success, target becomes visible `Traitor` / allegiance-flipped for the rest of battle.
- Damage does not break the flip.
- The flip can be removed only by explicit cleanse/immunity policy.
- If the target survives and recruitment conditions pass, the target may join after battle.

Provisional success shape:

```text
base success: low/moderate
bonus if target is low HP
bonus if target has low battle Brave
bonus if target was recently pressured by Intimidate or Call Out
hard cap: required
active flip cap: required
```

The exact numbers are pending T4/T8/recruitment validation. The design intent is that `Entice` is a
payoff to pressure, not a turn-one universal conversion button.

Why use this instead of attacking?

- It can remove an enemy and add an ally. That is worth more than damage when the target is
  eligible and the player has created a conversion window.

Guardrails:

- no boss/special target conversion;
- no whole-map conversion chain;
- converted unit must survive for recruitment;
- permanent recruitment remains eligibility-gated.

Verdict: keep as Orator's heavy social charm and recruitment payoff.

### Stall

Function:

- Single-target speech tempo action.
- Meaningful CT loss, with `CT -50` as the provisional design target.
- If the target is `Charging` or `Performing`, successful `Stall` interrupts/cancels the pending
  action and applies CT pressure.

Why use this instead of attacking?

- Use it when preventing a spell, Aim, performance, or imminent dangerous turn is worth more than
  Orator's gun attack.

Guardrails:

- no `Slow` status;
- no Speed stat change;
- no ongoing lock;
- immunity and Silence rules apply.

Verdict: keep as Orator's direct tempo answer.

### Praise

Function:

- Ally morale speech.
- Battle effect: Brave +20, capped below extreme optimization.
- Clears `Chicken`.
- Permanent positive Brave drift follows the game's existing rule for temporary Brave increases.

Why use this instead of attacking?

- Use it when a Brave-sensitive ally can convert the window into weapon damage, courage reactions,
  Chicken recovery, or long-term roster repair.

Guardrails:

- no permanent Brave loss exists elsewhere as a counterpart;
- this document does not add a new permanent Brave cap, rate, or per-battle limit.

Verdict: keep as Brave rally and repair.

### Intimidate

Function:

- Enemy morale pressure.
- Battle effect: Brave -20 as the provisional design target.
- Can contribute to visible `Chicken` pressure if the target is already near collapse.
- No permanent Brave loss.

Why use this instead of attacking?

- Use it against Brave-scaling weapon users, courage-reaction users, high-Brave overcommitment
  targets, or enemies being prepared for `Entice`.

Guardrails:

- battle-only;
- no invisible fear status;
- normal-Brave enemies are pressured, not automatically disabled;
- permanent roster damage is not allowed.

Verdict: keep as battle morale pressure and Entice setup.

### Preach

Function:

- Applies visible `Faith` status / strong receptivity spike.
- Ally use creates a burst or healing window.
- Enemy use creates a vulnerability window.
- Permanent positive Faith drift follows the game's existing rule for temporary Faith increases.

Why use this instead of attacking?

- Use it when the next spell, heal, or faith-facing status can exploit the target's visible
  receptivity for more value than Orator's own attack.

Guardrails:

- double-edged: the target both deals/receives stronger magic and is more vulnerable;
- single target;
- no broad Faith engine;
- no immunity bypass;
- Mystic remains the deeper spiritual controller.

Verdict: keep as visible Faith setup and permanent Faith repair.

### Enlighten

Function:

- Applies visible `Atheist` / anti-magic state.
- Ally use creates a short anti-magic shield.
- Enemy use temporarily suppresses caster, healer, or faith-facing status reliability.
- No permanent Faith loss.

Why use this instead of attacking?

- Use it when denying a caster's next meaningful spell or protecting an ally from magic matters
  more than damage.

Guardrails:

- narrow duration;
- non-stacking;
- counterable/cleansable;
- does not invalidate the magic ecosystem;
- battle-only.

Verdict: keep as visible anti-magic speech.

### Condemn

Function:

- Applies visible `Doom`.
- Countdown target: 4.
- Provisional enemy success target: around the low-to-mid hard-status range, pending T4/T5 checks.

Why use this instead of attacking?

- Use it against durable enemies that will survive ordinary damage but can be forced into a cure,
  retreat, or timed-death race.

Guardrails:

- no instant KO;
- immunity respected;
- cure policy respected;
- no damage rider.

Verdict: keep as ranged delayed lethal pressure.

### Call Out

Function:

- Replaces `Defraud`.
- Brave-band-sensitive public pressure.
- Uses the challenge/taunt model from `docs/job-balance/15-targeting-challenge-model-schema.md`.
- Applies a visible `Called Out` marker that shows the target is under target-pressure and records
  the declared challenger or protected ally.
- High-Brave targets are baited into overcommitment and target-pressure behavior.
- Low-Brave targets resist the duel bait but suffer a small visible battle-scoped shame/hesitation
  result instead.

Provisional behavior:

| Target Brave band | Result direction |
| --- | --- |
| low | Resists target bait; suffers minor visible CT loss as shame/hesitation. |
| normal | Soft challenge pressure toward the declared challenger. |
| high | Strong target-pressure / overcommitment toward the declared challenger. |
| extreme | Stress case for hard overcommitment, still respecting immunity and T8 overrides. |

Why use this instead of attacking?

- Use it to pull a dangerous enemy away from a fragile target, into a defender, or into a planned
  counter-bait/kill pocket.

Guardrails:

- not Confuse;
- not Berserk;
- not raw Brave reduction;
- no permanent effect;
- `Called Out` marker must be visible and must identify the pressure target;
- lethal opportunities, self-preservation, objectives, forced-target immunity, and control states
  remain T8 constraints.

Verdict: keep as Orator's new signature combat-control action.

### Insult

Function:

- Applies visible `Berserk`.
- Success is Brave-band-sensitive: bold targets are easier to bait into reckless offense.
- Duration should be one forced action or the shortest readable equivalent.

Why use this instead of attacking?

- Use it when the enemy's spell, support, item, or control action is more dangerous than its basic
  attack.

Guardrails:

- can backfire against enemies with dangerous attacks;
- no permanent effect;
- no target selection ownership, which keeps it distinct from `Call Out`;
- no Brave stat reduction, which keeps it distinct from `Intimidate`.

Verdict: keep as forced-offense disruption.

### Mimic Darlavon

Function:

- Applies visible `Sleep`.
- Target misses one action or wakes on damage.

Why use this instead of attacking?

- Use it to isolate a target the party does not want to damage yet, or to buy one setup turn.

Guardrails:

- damage break;
- immunity respected;
- single target;
- no multi-turn lock as the default design.

Verdict: keep as clean pause control.

### Fast Talk

Slot: reaction

Trigger identity: `focus_training`

Function:

- Triggers when the unit is targeted by a direct hostile action and can still speak.
- Fixed/capped chance.
- Attacker loses CT after resolution.
- No once-per-round clause.

Why use this reaction?

- It gives Orator and speech-secondary builds tempo friction instead of evasion, healing, or
  counter-damage.

Guardrails:

- does not use Brave;
- does not prevent damage;
- does not counterattack;
- does not trigger if Silenced or otherwise unable to speak;
- global one-roll-per-triggering-action and no-reaction-recursion rules still apply.

Open watch:

- If no round cap plus CT loss creates a focus-fire CT-lock engine, the fixed chance or CT bite is
  the tuning dial. Do not re-add a once-per-round clause unless simulation proves it necessary.

Verdict: keep as Orator's speech reaction.

### Equip Guns

Slot: support

Function:

- Grants existing gun access.

Why use this support?

- Gives low-stat or utility jobs PA/MA-independent missile pressure.

Guardrails:

- no new guns;
- no gun formula change;
- no damage rider;
- competes with all other support slots;
- high JP / late export posture remains likely.

Verdict: keep as major Orator export, with incidence watch.

### Silver Tongue

Slot: support

Function:

- Improves Speechcraft reliability within category caps.

Why use this support?

- Active Orator or Speechcraft-secondary builds can spend the support slot to make social control
  more reliable.

Guardrails:

- Speechcraft only;
- no immunity bypass;
- does not affect Mystic Arts, Steal, spells, items, guns, or ordinary attacks;
- recruitment and hard-status caps still apply.

Verdict: keep as Orator specialization support.

### Tame

Slot: support

Function:

- Preserves a monster recruitment/breeding access route.

Current decision:

- Keep as a protected Orator-derived monster route.
- Exact monster trigger, eligibility, and recruitment behavior are deferred to the monster pass.

Guardrail:

- Do not reuse this record for unrelated support effects unless another accepted document preserves
  monster access somewhere else.

Verdict: keep with access promise.

### Beast Tongue

Slot: support

Function:

- Preserves Speechcraft interaction with monsters.

Current decision:

- Keep as a protected route for monster communication and monster recruitment setup.
- Exact monster Speechcraft tables are deferred.

Guardrail:

- This route must remain compatible with later breeding and Poach planning.

Verdict: keep with access promise.

### Social Positioning

Slot: movement

Function:

- Move +1.
- Speechcraft range +1.

Why use this movement?

- It lets an active Orator reach the correct speech line without becoming a general mobility job.

Guardrails:

- no gun range increase;
- no spell range increase;
- no item range increase;
- no terrain or elevation bypass.

Verdict: keep.

## Kit Assembly

Orator V2 has four real tactical lanes:

1. Recruitment lane: `Intimidate` / damage / `Call Out` pressure into `Entice`.
2. Brave lane: `Praise`, `Intimidate`, `Call Out`, and `Insult`.
3. Faith lane: `Preach` and `Enlighten` as visible `Faith` / `Atheist` windows.
4. Control lane: `Stall`, `Condemn`, `Insult`, `Mimic Darlavon`, and `Call Out`.

The fallback lane is guns.

This gives Orator useful turns without making every turn the same status button. The player chooses
between flipping, delaying, baiting, exposing, shielding, forcing offense, sleeping, dooming, or
shooting.

## Permanent And Roster-Affecting Policy

Allowed:

- ordinary permanent Brave gain from positive `Praise` changes, using the game's existing rule;
- ordinary permanent Faith gain from positive `Preach` changes, using the game's existing rule;
- permanent human recruitment from `Entice` when eligibility and survival conditions pass;
- monster access routes through `Tame` and `Beast Tongue`.

Not allowed:

- permanent Brave loss from `Intimidate`;
- permanent Faith loss from `Enlighten`;
- accidental roster ruin;
- Gil reward or Gil value changes.

## Visibility And Complexity Audit

V2 prefers visible vanilla states:

- `Traitor`;
- `Chicken`;
- `Faith`;
- `Atheist`;
- `Doom`;
- `Berserk`;
- `Sleep`;
- visible `Called Out` marker showing target-pressure and declared pressure target;
- CT movement;
- targeting behavior from the challenge/taunt policy.

The only new conceptual behavior is `Call Out`, and it is expressed through a visible `Called Out`
marker plus targeting pressure and minor CT/morale hesitation rather than hidden marks.

No skill should require the player to track many invisible counters. Caps such as active `Entice`
flips are balance constraints for the system, not tactical states the player must optimize every
turn. This document does not add custom permanent Brave/Faith gain counters.

## Expected Player Use

Healthy Orator patterns:

- pressure a dangerous enemy with `Call Out`, then punish their overcommitment;
- weaken a target with `Intimidate`, damage them, then attempt `Entice`;
- use `Preach` to set up a high-value spell or healing window;
- use `Enlighten` to protect a key ally from magic or shut down an enemy caster window;
- use `Stall` to cancel a charge or performance;
- use guns when speech is resisted or the right control target is absent.

Unhealthy patterns to watch:

- `Entice` becoming a turn-one conversion lottery against every normal enemy;
- `Call Out` becoming a universal hard taunt;
- `Enlighten` invalidating magic-heavy encounters;
- `Fast Talk` creating a CT-lock engine under focus fire;
- `Equip Guns` becoming a default support on too many builds.

## Weaknesses And Counters

Orator remains pressured by:

- Silence;
- status immunity;
- boss/protected target policy;
- low-value control targets;
- fast enemies that punish setup;
- ranged pressure against leather durability;
- maps where gun fallback is acceptable but not fight-carrying;
- enemies whose basic attacks are still dangerous after `Insult`.

## Validation Gates

| Gate | V2 status |
| --- | --- |
| Vanilla diagnosis | Pass. |
| Dependent systems | Pass: Brave, Faith, recruitment, monsters, guns named. |
| Creative identity question | Pass: `Call Out` accepted as the non-vanilla identity mechanic for review. |
| Protected systems | Pass: human recruitment, monster access, Poach route, and vanilla positive permanent Brave/Faith drift. |
| Thematic ownership | Pass with required Thief follow-up for `Steal Heart`. |
| Skill value | Pass. |
| Combat action value | Pass: every action names the battle window that beats attacking. |
| Campaign value | Pass: recruitment, monster access, and vanilla positive permanent Brave/Faith drift preserved. |
| Permanent-effect policy | Pass: no permanent loss. |
| Visibility | Pass: `Call Out` uses a visible `Called Out` marker tied to the challenge/taunt model. |
| Complexity budget | Watch: `Entice` setup and cap must stay simple. |
| Build texture | Pass: Orator has active, secondary, support, movement, and gun hooks. |

## Required Follow-Ups

1. Reopen Thief separately to remove or replace the committed `Steal Heart` identity; do not let
   Thief and Orator both own the same social charm niche without a deliberate distinction.
2. Later validation must test `Entice`, `Call Out`, `Fast Talk`, `Preach`, and `Enlighten` in T4,
   T5, T8, F5, and campaign/recruitment rows.

## Reviewer Notes

Claude approved V2 on 2026-06-23 after requiring `Call Out` to use a named visible marker rather
than abstract targeting behavior. That requirement is incorporated through `Called Out`.
