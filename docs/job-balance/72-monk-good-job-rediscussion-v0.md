# Monk Good-Job Rediscussion V0

Status: Accepted (GPT/Claude consensus) -- pending Marcelo validation
Date: 2026-06-23
Scope: Monk only

Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/18-monk-v1-proposal.md`
- `docs/job-balance/54-monk-thief-concrete-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

## Purpose

This document revisits Monk under the updated good-job premises:

- learned skills should feel useful and readable;
- direct damage and recovery should scale through formulas, weapon-relative output, percentage, or
  another visible progression hook instead of staying fixed forever;
- persistent combat effects should use named visible feedback instead of hidden math drift;
- strong sustain and counter-fighting combos are healthy when they require real adjacency, exposure,
  support-slot cost, job routing, or party positioning;
- reactions should not all become Brave optimization problems;
- no Gil values are changed;
- no new equipment is added.

This pass supersedes the Monk rows in docs 18, 54, and 58 where they conflict. It does not change
Thief, final JP costs, prerequisites, equipment lists, item economy, or implementation data.

## Monk Identity

Monk is the protected unarmed impact job: body discipline, fists, crush pressure, nearby recovery,
and counter-fighting.

Monk should be excellent when it can commit to close positioning, exploit plate with crush, protect a
tight formation with Chakra, and punish enemies who engage it carelessly. It should be weaker into
mail, ranged pressure, magic/status pressure, bad maps, and situations where cloth melee exposure is
too dangerous.

Monk is not allowed to become the best damage job, best healer, best reviver, best reaction donor,
and best weapon-independent shell all at once.

## Shared Status Vocabulary

Monk keeps status feedback simple:

| Status | Meaning | Primary Monk source | Guardrail |
| --- | --- | --- | --- |
| `Doom` | target is KO'd when countdown resolves unless countered | `Doom Fist` | Vanilla visible status; immunity respected. |

`Purification` removes a narrow set of statuses, but it does not create a persistent Monk-specific
status. `Chakra`, `Revive`, and `Lifefont` are immediate recovery effects with visible numbers.

## Consensus Package

| Skill | Slot | Value | Guardrail |
| --- | --- | --- | --- |
| `Pummel` | Action | fists output x1.10; hit +0.10 | Adjacent only; no status or sustain rider. |
| `Cyclone` | Action | fists output x0.80; small self-centered AoE | Exposure is the cost; not safe AoE. |
| `Aurablast` | Action | fists output x0.85; short projection | Range relief, not Archer/Dragoon replacement. |
| `Shockwave` | Action | fists output x0.95; grounded line | Map-dependent lane pressure. |
| `Doom Fist` | Action | fists output x0.60; Doom 45% base | Visible Doom; countdown 3; immunity respected. |
| `Purification` | Action | clears Poison, Blind, Silence, Immobilize, Oil | Self or adjacent ally; no HP heal. |
| `Chakra` | Action | scaling HP plus MP close-formation pulse | No revive; MP restore is a sim-watch. |
| `Revive` | Action | guaranteed adjacent revive at scaling HP | No range, no Reraise, no mass revive. |
| `Counter` | Reaction | fixed/capped 70%; post-hit fists output x0.75 | Once/round; adjacent melee only; non-Brave. |
| `First Strike` | Reaction | fixed/capped 45%; pre-hit fists output x0.70 | Non-negating; late/narrow; non-Brave. |
| `Brawler` | Support | unarmed/fist basic damage x1.20 | Fist-gated; no weapon-engine stacking. |
| `Martial Discipline` | Support | deferred | Reopen only if Monk secondary needs a hook later. |
| `Lifefont` | Movement | scaling movement HP recovery | No MP; once/turn; bounded sustain. |

## Action Notes

### Pummel

`Pummel` is Monk's reliable adjacent strike.

```text
damage = fists output x1.10
hit = normal hit +0.10
range = adjacent
```

Rules:

- fists/unarmed route only;
- no random damage variance;
- no status rider;
- no sustain rider;
- no range or AoE.

The old lower-than-basic-attack direction would make the learned strike feel dead. `Pummel` should
feel like a real learned body-tech button while staying honest: it only does adjacent fist damage.

A fixed multi-hit presentation can be reconsidered later if the implementation can make it clean and
readable. This artifact does not require multi-hit behavior.

### Cyclone

`Cyclone` is exposed close AoE.

```text
damage = fists output x0.80
area = small self-centered adjacent AoE
```

Rules:

- the Monk must stand in the cluster;
- no range safety;
- no status rider;
- no sustain rider;
- if implementation forces friendly fire, the value must be re-reviewed.

The action should reward body placement and clustered enemies without becoming the safest AoE plan in
the game.

### Aurablast

`Aurablast` is limited ranged ki projection.

```text
damage = fists output x0.85
range = short projection, target 3 as the provisional value
```

Rules:

- no AoE;
- no status rider;
- no sustain rider;
- no replacement for Archer, Dragoon, or spell range.

This gives Monk a way to act on bad maps without erasing its close-range weakness.

### Shockwave

`Shockwave` is grounded line pressure.

```text
damage = fists output x0.95
shape = line or ground path
```

Rules:

- grounded/path restriction required if implementation supports it;
- Float, airborne states, height breaks, or path weirdness should counter it where possible;
- no status rider;
- no universal ranged pressure.

The purpose is to reward lanes and terrain reads, not to make Monk a general missile job.

### Doom Fist

`Doom Fist` preserves Monk's pressure-point status fantasy.

```text
damage = fists output x0.60
status = Doom, 45% base status chance
countdown = 3
range = adjacent
```

Rules:

- `Doom` is visible;
- countdown is visible;
- boss/status immunity is respected;
- `Purification` does not clear Doom in this pass;
- no hidden vulnerability or damage-over-time rider.

The 45% status chance is intentionally bold enough to matter. It must be watched later: if campaign
validation shows adjacent Doom is oppressive, the first adjustment direction is lower status chance,
around 35%, not deleting the identity.

### Purification

`Purification` is narrow body-discipline cleanup.

```text
range = self or adjacent ally
clears = Poison, Blind, Silence, Immobilize, Oil
```

Rules:

- no HP heal;
- no MP restore;
- no revive;
- no Doom cleanup;
- no Stop, Stone, Charm, Confuse, KO, Undead, Reraise, Protect, or Shell cleanup.

Oil is included deliberately. It gives Monk a readable counterplay button against fire-combo setup,
but the action cost and adjacency requirement keep Oil combos alive.

### Chakra

`Chakra` is the headline scaling fix for Monk sustain.

```text
area = self plus adjacent allies
raw_hp = 20 + floor(Level / 3) + (2 * PA)
hp_heal = min(raw_hp, floor(0.18 * target_max_hp), missing_hp)

raw_mp = 6 + floor(Level / 8) + floor(PA / 2)
mp_restore = min(raw_mp, floor(0.10 * target_max_mp), missing_mp)
```

Rules:

- no item stock;
- no revive;
- no range safety;
- close-formation pulse is the identity;
- HP output must stay below dedicated healer throughput;
- MP restore is iconic but must remain tightly capped.

Chakra should remain relevant all game without becoming a main healer replacement. The formation
requirement is not flavor text: clustering for Chakra should expose the party to AoE, status, and
positioning risk.

The MP component is accepted provisionally because it is iconic. It is the top Monk sim-watch. If it
creates caster MP loops through Monk secondary, the adjustment direction is lowering or cutting MP
restore before weakening Chakra's HP identity.

### Revive

`Revive` is risky emergency recovery.

```text
range = adjacent
success = guaranteed
revive_hp = max(20, floor(0.20 * target_max_hp))
```

Rules:

- no item stock;
- no MP;
- no range safety;
- no Reraise;
- no mass revive;
- no bonus healing after revive.

The cost is standing next to a corpse in danger. A failure roll would make the action feel bad without
adding interesting counterplay, so this pass uses guaranteed revive at fragile HP.

## Reaction Notes

### Counter

`Counter` is the core Monk reaction.

```text
trigger = fixed/capped 70%
eligible trigger = adjacent direct melee/fist/weapon hit that damages the Monk
frequency = once per unit round
effect = fists output x0.75 against the attacker
```

Rules:

- not Brave-scaled as the main lever;
- post-hit only;
- attacker must still be in fist range;
- no ranged coverage;
- no magic coverage;
- no status-only coverage;
- no recursion.

This makes engaging a Monk costly without turning Brave into the obvious answer for every unit.

### First Strike

`First Strike` is late, narrow, non-negating spice.

```text
trigger = fixed/capped 45%
eligible trigger = adjacent direct melee/fist/weapon attack before damage
frequency = once per unit round
effect = pre-hit fists output x0.70 against the attacker
```

Rules:

- not Brave-scaled as the main lever;
- does not cancel the incoming attack;
- no vanilla-style Hamedo negation;
- no ranged coverage;
- no magic coverage;
- no status-only coverage;
- no boss-special or area coverage.

The incoming attack still resolves if it remains legal. This preserves the preemptive martial fantasy
without invalidating melee weapon families.

If Monk can keep only one reaction, `Counter` is the priority.

## Support And Movement Notes

### Brawler

`Brawler` remains Monk's portable build hook.

```text
unarmed/fist basic damage x1.20
```

Rules:

- support slot;
- unarmed/fist route only;
- no weapon attacks;
- does not improve `Chakra`;
- does not improve `Revive`;
- does not improve `Purification`;
- does not stack with premium weapon engines such as `Dual Wield`, `Doublehand`, or broad
  `Attack Boost` unless a later incidence pass explicitly accepts that interaction.

`Brawler` should be attractive because FFT build planning needs exciting support routes. Its boundary
is that it asks the unit to give up weapons and the support slot.

### Martial Discipline

`Martial Discipline` is deferred in this first Monk rediscussion artifact.

The Monk kit already carries:

- strong active fist pressure;
- close AoE;
- projection;
- line pressure;
- Doom pressure;
- cleanse;
- Chakra;
- Revive;
- two reactions;
- `Brawler`;
- `Lifefont`.

Adding another support that improves Monk actions risks making Martial Arts secondary too complete.
If later W4/W5 incidence shows Monk secondary lacks a satisfying support hook, a narrow
Monk-command-only support can be reopened.

### Lifefont

`Lifefont` becomes a scaling movement heal.

```text
trigger = after moving at least 1 tile
raw_heal = 8 + floor(Level / 6) + PA
heal = min(raw_heal, floor(0.08 * max_hp), missing_hp)
frequency = once per unit turn
```

Rules:

- no MP restore;
- no item stock;
- no revive;
- does not trigger on forced movement if implementation can distinguish it;
- does not trigger more than once per turn.

This keeps the classic movement-sustain hook while preventing it from erasing attrition by itself.

## Expected Play Patterns

Healthy Monk patterns:

- active Monk uses fists and `Pummel` for close pressure;
- `Cyclone`, `Aurablast`, and `Shockwave` solve different map states instead of forming one damage
  ladder;
- party clusters for `Chakra` when the formation risk is worth it;
- `Revive` saves adjacent allies in dangerous frontline positions;
- non-Monk builds spend the support slot on `Brawler` to become deliberate unarmed specialists;
- `Counter` punishes melee engagement without Brave optimization.

Unhealthy Monk patterns to watch:

- `Chakra`, `Revive`, and `Brawler` make Martial Arts the default secondary on too many bodies;
- Knight-body plus Monk secondary compresses damage, sustain, revive, and durability too early;
- `Lifefont` plus `Chakra` erases attrition;
- `First Strike` feels like old Hamedo and invalidates melee weapon families;
- `Brawler` becomes the default physical support over weapon-family builds;
- MP `Chakra` fuels caster loops too efficiently;
- `Doom Fist` becomes oppressive in ordinary encounters.

## Validation Hooks

These are later validation hooks, not blockers to this provisional rediscussion artifact:

- `T3 healing/attrition`: test `Chakra`, `Revive`, and `Lifefont` across early, mid, late, and stress
  HP bands.
- `T3xT5 revive timing`: compare adjacent guaranteed `Revive` against Phoenix Down and White Mage
  revive timings.
- `T4 accuracy/evasion`: test `Pummel`, `Doom Fist`, `Counter`, and `First Strike`.
- `T5 CT/status`: test Doom countdown, `First Strike` timing, and counterplay windows.
- `T6 armor response`: confirm fist/crush pressure keeps anti-plate identity without erasing mail or
  weapon-family planning.
- `T9 MP economy`: test `Chakra` MP restore against caster sustain loops.
- `M-SECONDARY-COUNT`: count Martial Arts secondary, `Brawler`, `Counter`, `First Strike`, and
  `Lifefont` incidence.
- `F5 real-roster sweep`: test active Monk, Knight-body with Monk secondary, caster with Monk
  secondary, and non-Monk `Brawler` builds.

## Reviewer Notes

Claude reviewed the opening Monk package before this artifact was written and approved the core
direction:

- `Chakra` must scale and should remain a bold close-formation pulse;
- `Chakra` keeps MP restore provisionally, with MP-loop validation called out;
- `Revive` should be guaranteed at fragile scaling HP, not a failure roll;
- `Counter` and `First Strike` are non-Brave-centered;
- `First Strike` is acceptable only because it does not negate the incoming attack;
- `Doom Fist` keeps visible vanilla Doom;
- `Purification` includes Oil;
- `Brawler` remains the portable Monk hook;
- `Martial Discipline` is deferred;
- `Lifefont` scales modestly.

Claude accepted this artifact after review. Monk is closed for this rediscussion pass pending
Marcelo validation.
