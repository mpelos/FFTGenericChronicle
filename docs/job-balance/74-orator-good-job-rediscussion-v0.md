# Orator Good-Job Rediscussion V0

Status: Needs revision after Marcelo validation
Date: 2026-06-23
Scope: Orator only

Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/22-thief-orator-v1-proposal.md`
- `docs/job-balance/55-orator-dragoon-concrete-v0.md`
- `docs/job-balance/60-prerequisite-tree-and-jp-cost-draft-v0.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

## Purpose

This document revisits Orator under the updated good-job premises:

- learned skills should feel useful and readable;
- direct damage should scale through ordinary formulas instead of fixed-forever values;
- persistent combat effects should use visible statuses, visible stat changes, or immediate visible
  outcomes;
- strong setup combos are healthy when they require real party planning, action cost, positioning,
  JP routing, or support-slot investment;
- reactions should not all become Brave optimization problems;
- no Gil values are changed;
- no new equipment is added;
- monsters are outside the current scope.

This pass supersedes the Orator rows in docs 22, 55, and 60 where they conflict. It does not change
Thief, Dragoon, Mystic, final JP costs, prerequisites, item prices, Gil rewards, monster policy, or
implementation data.

## Orator Identity

Orator is the social-control specialist: speech, morale, temporary allegiance pressure, confusion,
intimidation, faith setup, and gun fallback.

The job should feel like FFT Orator made playable in battle. The player should use Orator when they
want a targeted control plan, a morale window, a Faith setup or countersetup, or a reliable gun turn
when speech is resisted. Orator is not a pure damage job, a hidden stat-grind job, or a replacement
for Mystic's spiritual-control identity.

## Speechcraft Contract

Speechcraft uses readable, battle-scoped effects in this version.

Rules:

- `Silence` blocks Speechcraft.
- Speechcraft respects target immunity, boss protection, and status resistance.
- Enemy speech uses listed base rates by ability category.
- Ally morale and Faith speech may be reliable because it spends an action and remains capped.
- Ordinary Speechcraft does not use hidden level-difference or hidden Faith accuracy math.
- Speechcraft has no direct damage in this pass.
- Orator's direct damage comes only from ordinary weapon attacks using existing gun, knife, and fist
  formulas.

`Silver Tongue` may improve Speechcraft enemy success by a flat visible bonus, but no other broad
accuracy stack is accepted here.

## Shared Status And Stat Vocabulary

Orator uses visible outcomes:

| Effect | Meaning | Primary Orator source | Guardrail |
| --- | --- | --- | --- |
| `Charm` / allegiance pressure | temporary enemy control | `Entice` | One controlled action or damage break; no permanent recruit. |
| CT loss | target acts later | `Stall`, `Fast Talk` | Immediate CT pressure; no Slow status. |
| Brave change | morale confidence shift | `Praise`, `Intimidate` | Battle-scoped, capped, not permanent farming. |
| Faith change | magic receptivity shift | `Preach`, `Enlighten` | Battle-scoped and weaker than Mystic's Faith lane. |
| `Doom` | delayed KO countdown | `Condemn` | Countdown 4; no instant KO. |
| `Confuse` | random/disrupted action | `Defraud` | One confused action or damage break. |
| `Berserk` | forced basic offense | `Insult` | One forced action or damage break. |
| `Sleep` | temporary action denial | `Mimic Darlavon` | One missed action or damage break. |

## Brave Residual Role

The broader redesign is moving reactions away from universal Brave scaling. That is intentional:
Brave should not be the default optimization answer for every unit.

`Praise` and `Intimidate` remain accepted only because Brave still has a residual tactical role:

- some enemy or vanilla-style reactions may still use Brave until their own job passes rewrite them;
- Brave-keyed damage and ability routes can still exist where a job explicitly owns that ecology,
  such as fist, katana, knight-sword, morale, or other Brave-sensitive formulas retained later;
- enemy AI aggression, confidence, or morale behavior may use Brave where final data can express it;
- lowering Brave can still reduce reaction confidence or Brave-sensitive pressure without causing
  `Chicken` in this Orator package.

If later simulations show Brave has become too marginal to justify action slots, `Praise` and
`Intimidate` are the explicit first cut or merge candidates. They are not protected by nostalgia if
Brave no longer creates meaningful tactical decisions.

## Consensus Package

| Skill | Slot | Value | Guardrail |
| --- | --- | --- | --- |
| `Entice` | Action | temporary control 35% base | Eligible human targets; one controlled action or damage break; no permanent recruit. |
| `Stall` | Action | CT -15, 60% base | No Slow, no Speed stat change, no hard lock. |
| `Praise` | Action | ally Brave +8, cap 80 | Reliable; battle-scoped; one Orator-origin Brave-up per target. |
| `Intimidate` | Action | enemy Brave -8, floor 50, 60% base | Battle-scoped; does not cause `Chicken`. |
| `Preach` | Action | Faith +5, cap 80 | Reliable on allies; 60% base on enemies; weaker than Mystic. |
| `Enlighten` | Action | Faith -5, floor 50, 60% base on enemies | Battle-scoped anti-caster or protection setup. |
| `Condemn` | Action | `Doom` 35% base, countdown 4 | No instant KO; immunity respected. |
| `Defraud` | Action | `Confuse` 30% base | No Gil effect; one confused action or damage break. |
| `Insult` | Action | `Berserk` 30% base | One forced action or damage break. |
| `Mimic Darlavon` | Action | `Sleep` 30% base | One missed action or damage break. |
| `Fast Talk` | Reaction | fixed/capped 45%; attacker CT -10 | Non-Brave; once/round; no damage prevention. |
| `Equip Guns` | Support | existing gun access | No new guns; no damage rider; high export risk. |
| `Silver Tongue` | Support | Speechcraft enemy success +10 percentage points | Speechcraft only; no broad status accuracy. |
| `Tame` | Support | removed from current scope | Monsters are out of scope. |
| `Beast Tongue` | Support | removed from current scope | Monsters are out of scope. |
| `Social Positioning` | Movement | Move +1; Speechcraft range +1 | No gun, spell, item, or general range increase. |

## Action Notes

### Entice

`Entice` is Orator's temporary allegiance pressure.

```text
success = 35% base
duration = one controlled action or damage break
eligible targets = human, non-boss, non-protected targets
```

Rules:

- visible `Charm` or `Traitor`-like behavior, depending on final implementation;
- no gender restriction;
- no permanent recruitment in the combat package;
- no damage rider;
- immunity respected.

Classic permanent `Invitation` recruitment is deferred to a later campaign and roster pass, not
deleted. The recruit fantasy is preserved for that future policy track, while this combat package
uses only battle-scoped temporary control.

`Entice` remains distinct from Thief `Steal Heart`: Thief has short-range charm inside a fast
knife/steal kit, while Orator has speech-range control with no weapon damage.

### Stall

`Stall` is speech-based tempo pressure.

```text
success = 60% base
effect = target CT -15
```

Rules:

- no `Slow` status;
- no Speed stat change;
- no hard action lock;
- no damage rider.

Time Mage remains the real tempo specialist. `Stall` gives Orator a useful, readable low-risk action
without replacing Haste, Slow, Stop, or Quick.

### Praise

`Praise` is targeted ally morale support.

```text
effect = Brave +8
cap = 80
duration = battle-scoped
success = reliable on eligible allies
```

Rules:

- one active Orator-origin Brave-up effect per target;
- no permanent Brave increase;
- no campaign stat farming;
- no direct damage.

`Praise` exists only while Brave still matters to some morale, reaction, AI, or Brave-keyed formula
routes. If Brave loses that relevance later, `Praise` should be merged or cut before Orator gains a
dead buff slot.

### Intimidate

`Intimidate` is enemy morale pressure.

```text
success = 60% base
effect = Brave -8
floor = 50
duration = battle-scoped
```

Rules:

- does not cause `Chicken`;
- does not permanently lower Brave;
- no hidden fear status;
- no damage rider.

The intended use is reducing reaction confidence or Brave-sensitive pressure without turning Orator
into a permanent stat-maintenance job.

### Preach

`Preach` is secondary Faith setup.

```text
effect = Faith +5
cap = 80
success = reliable on allies, 60% base on enemies
duration = battle-scoped
```

Rules:

- weaker than Mystic Faith control;
- double-edged because higher Faith also makes the target more magic-receptive;
- no permanent Faith increase;
- no hidden accuracy formula.

Orator may help caster parties create a window, but Mystic remains the primary spiritual/Faith
controller.

### Enlighten

`Enlighten` is Faith countersetup.

```text
success = 60% base on enemies
effect = Faith -5
floor = 50
duration = battle-scoped
```

Rules:

- weaker than Mystic Faith control;
- cannot invalidate ordinary magic by itself;
- no permanent Faith decrease;
- no Atheist conversion.

The intended use is anti-caster pressure, anti-healing pressure, or temporary protection planning.

### Condemn

`Condemn` is ranged delayed lethal pressure.

```text
success = 35% base
status = Doom
countdown = 4
```

Rules:

- visible `Doom`;
- no instant KO;
- no damage rider;
- boss and protected target immunity respected;
- cure/immunity policy follows final status rules.

This intentionally coexists with Monk `Doom Fist`.

| Skill | Job | Delivery | Damage | Countdown | Read |
| --- | --- | --- | --- | ---: | --- |
| `Doom Fist` | Monk | adjacent fist pressure | fists output x0.60 | 3 | Close, risky, physical pressure point. |
| `Condemn` | Orator | ranged speech | none | 4 | Safer reach, slower payoff, no chip. |

Two Doom sources are acceptable because they ask for different jobs, positions, risks, and timing.

### Defraud

`Defraud` is repurposed away from Gil and economy.

```text
success = 30% base
status = Confuse
duration = one confused action or damage break
```

Rules:

- no Gil value changes;
- no stolen money;
- no campaign reward;
- visible `Confuse`;
- immunity respected.

The name can be read as tricking the target into bad action. If final implementation renames the
skill, the accepted design point is the effect: Orator gets a deception/confusion lever without an
economy hook.

### Insult

`Insult` is forced-offense disruption.

```text
success = 30% base
status = Berserk
duration = one forced enemy action or damage break
```

Rules:

- visible `Berserk`;
- no damage rider;
- immunity respected;
- no multi-turn lock by default.

This is strongest against casters and support units, but it can backfire if the target's basic
attack is dangerous.

### Mimic Darlavon

`Mimic Darlavon` is a short sleep window.

```text
success = 30% base
status = Sleep
duration = one missed action or damage break
```

Rules:

- visible `Sleep`;
- single target only in this pass;
- immunity respected;
- damage breaks.

It should create a tactical pause, not a reliable encounter lock.

## Reaction Notes

### Fast Talk

`Fast Talk` replaces the earlier Brave/Faith surge reaction candidates.

```text
trigger = targeted by a single-target enemy action and remains able to act
success = fixed/capped 45%
limit = once per round
effect = attacker CT -10 after resolution
```

Rules:

- does not use Brave;
- does not prevent damage;
- does not counterattack;
- does not apply a status;
- does not create Gil, recruitment, or steal rewards;
- target immunity and final reaction rules apply.

The purpose is a verbal riposte identity. It gives Orator a defensive tempo texture without becoming
Shirahadori, Auto-Potion, or a universal survival reaction.

## Support Notes

### Equip Guns

`Equip Guns` remains Orator's major export support.

Rules:

- uses existing guns only;
- no new equipment is added;
- no damage rider;
- no gun formula change in this document;
- high JP and late export posture should remain unless incidence testing proves it safe earlier.

`Equip Guns` is the largest Orator export risk because gun damage uses `wp_wp` and ignores PA/MA.
That makes it useful for low-stat utility jobs, but it can also patch too many builds if it is too
cheap or too early.

### Silver Tongue

`Silver Tongue` is Orator's Speechcraft specialization support.

```text
effect = +10 percentage points to enemy Speechcraft success rates
```

Rules:

- Speechcraft only;
- does not affect Mystic Arts, Steal, spells, items, guns, ordinary attacks, or non-Orator status;
- does not alter permanent recruitment policy;
- does not bypass immunity.

It is accepted because spending a support slot to become a better Orator is healthy. It should not
become the generic best status support for the whole roster.

### Tame And Beast Tongue

`Tame` and `Beast Tongue` are removed from the current Orator package because monsters are outside
the current scope.

If implementation requires reusing their records, those records should be reused for current Orator
support effects such as `Silver Tongue`, not for monster behavior.

## Movement Notes

### Social Positioning

`Social Positioning` helps Orator use speech without becoming a general mobility job.

```text
effect = Move +1
bonus = Speechcraft range +1
```

Rules:

- no gun range increase;
- no spell range increase;
- no item range increase;
- no global status range increase;
- no terrain or elevation bypass.

This is useful for active Orator and Speechcraft-secondary builds, but it should lose to stronger
movement choices when the player wants raw traversal.

## Expected Player Use

Healthy Orator patterns:

- active Orator alternates gun fallback with targeted speech control;
- physical party uses `Praise` or `Intimidate` to shape Brave-sensitive turns at action cost;
- caster party uses `Preach` or `Enlighten` as a small setup or countersetup, while Mystic owns the
  stronger Faith plan;
- controller party chooses a specific social status instead of pressing one universal disable;
- utility build spends a support slot on `Equip Guns` or `Silver Tongue`, not both for free.

Unhealthy Orator patterns:

- `Equip Guns` becomes the obvious support for every stat-poor job;
- `Praise` or `Intimidate` becomes mandatory upkeep despite the battle-scoped rule;
- `Preach` and `Enlighten` make Mystic redundant;
- `Entice`, `Defraud`, `Insult`, `Mimic Darlavon`, and `Condemn` chain into safe soft-lock loops;
- Orator is only useful for campaign recruitment or economy.

## Simulation Watch List

Later validation should watch these risks, but they do not block this artifact:

- `Equip Guns` export: gun damage ignores PA/MA and can patch stat-poor jobs too efficiently.
- Control-chain reliability and length: `Entice`, `Defraud`, `Insult`, `Mimic Darlavon`, and
  `Condemn` must not chain into safe soft locks; one-action expiry and damage break are the guard.
- Mystic overlap: `Preach` and `Enlighten` must stay weaker than Mystic's Faith control.
- Brave efficiency thresholds: `Praise` and `Intimidate` must matter without becoming mandatory
  picks; if Brave becomes marginal, these are first cut or merge candidates.
- Thief overlap: `Entice` must not erase Thief's single bounded `Steal Heart` niche.

## Expected Weaknesses

- leather durability;
- Silence;
- status immunity;
- boss or protected target policies;
- lower raw damage than Archer, Dragoon, Black Mage, or dedicated weapon jobs;
- reliance on action economy for setup;
- weaker value against enemies that do not care about morale, Faith, or control.

## Expected Counters

- Silence and anti-command pressure;
- Charm, Sleep, Berserk, Confuse, Doom, or Faith/Brave immunity;
- fast enemies that punish setup turns;
- formations with low-value control targets;
- ranged pressure against leather units;
- encounters where gun fallback is acceptable but not enough to carry the job alone.

## Ramza / Unique-Job Interaction

Ramza may gain leadership and morale tools, but Orator remains the dedicated social controller.

Ramza can inspire or command as part of a hybrid knight/mage identity. He should not become better
than Orator at recruitment pressure, broad social statuses, Speechcraft accuracy, or gun-backed
control.

## Reviewer Notes

Claude approved the Orator package for artifact writing on 2026-06-23, requiring four inclusions:

- explain why Brave still matters after reactions move away from universal Brave scaling;
- state that permanent classic recruitment is deferred to a later campaign/roster pass, not deleted;
- document the simulation watch list for gun export, control chains, Mystic overlap, and
  Praise/Intimidate thresholds;
- distinguish Orator `Condemn` from Monk `Doom Fist`.

Those requirements are incorporated in this artifact.

Marcelo rejected this package on 2026-06-23. The rejection is not a request for numeric tuning only;
it identifies a process failure:

- `Tame` and `Beast Tongue` cannot be removed in a way that deletes monster recruitment, breeding,
  and `Poach` access. Those systems are essential FFT texture.
- `Stall` at CT -15 looks too weak for spending an Orator turn.
- `Praise`, `Intimidate`, `Preach`, and `Enlighten` are too small to justify action cost as written,
  and the broader permanent Brave/Faith policy must be reopened so recruited characters are not
  permanently advantaged or disadvantaged by starting Brave/Faith.
- Every Orator combat action must be re-audited against the basic question: why spend the turn on
  this instead of attacking, killing, healing, or taking another high-impact action? Orator
  campaign/roster actions must instead justify their long-term value to recruitment, monster
  systems, breeding, poach, or build planning.
- Future Orator revision must preserve the social-control identity while making each turn produce
  meaningful battle impact.

This document remains useful as a record of the rejected GPT/Claude proposal, but it is not accepted
for the current mod direction.
