# Thief Good-Job Rediscussion V0

Status: Accepted (GPT/Claude consensus) -- pending Marcelo validation
Date: 2026-06-23
Scope: Thief only

Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/22-thief-orator-v1-proposal.md`
- `docs/job-balance/54-monk-thief-concrete-v0.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

## Purpose

This document revisits Thief under the updated good-job premises:

- learned skills should feel useful and readable;
- direct damage should scale through weapon-relative output or formulas instead of fixed-forever
  values;
- permanent effects should be understandable, especially steals and visible statuses;
- strong setup combos are healthy when they require real positioning, speed, support-slot cost, or
  party planning;
- reactions should not all become Brave optimization problems;
- no Gil values are changed;
- no new equipment is added.

This pass supersedes the Thief rows in docs 22 and 54 where they conflict. It does not change Orator,
final JP costs, prerequisites, item prices, Gil rewards, monster/poach policy, or implementation data.

## Thief Identity

Thief is the fast precision opportunist: knife pressure, rear attacks, poison, arm/leg disruption,
low-odds high-emotion steals, and mobility.

The job should feel like a D&D thief translated into FFT. The player should care about reaching a
rear arc, poisoning a target, disabling a key enemy long enough to steal from them, or planning a
battle around stealing a specific enemy's special equipment.

Thief should be weaker in fair front-line trades, against equipmentless targets, against status
immunity, into heavy evasion without setup, and whenever leather units are punished for overextending.

## Shared Status And Equipment Vocabulary

Thief uses real visible outcomes:

| Effect | Meaning | Primary Thief source | Guardrail |
| --- | --- | --- | --- |
| `Poison` | target takes attrition over time | `Venom Knife` | Visible vanilla status; immunity respected. |
| `Disable` | target cannot act | `Arm Aim` | Visible vanilla status; low reliability. |
| `Immobilize` | target cannot move | `Leg Aim` | Visible vanilla status; no Stop. |
| `Charm` | target acts under opposing influence | `Steal Heart` | Breaks on damage; no gender restriction. |
| Equipment stolen | actual enemy equipment is removed and kept | equipment steal actions | Existing enemy equipment only; no new equipment. |

Equipment steals should not become invisible exposure statuses. If the steal succeeds, the readable
result is the actual stolen item and the enemy losing that piece.

## Consensus Package

| Skill | Slot | Value | Guardrail |
| --- | --- | --- | --- |
| `Backstab` | Action | rear weapon x1.50, hit +0.15; side x1.20, hit +0.05 | Front falls back to normal attack; no status rider. |
| `Venom Knife` | Action | weapon x0.75; Poison 60% base | Knife/sword; side/rear +10 status chance. |
| `Arm Aim` | Action | weapon x0.55; Disable 35% base | Side/rear +10 status chance; immunity respected. |
| `Leg Aim` | Action | weapon x0.55; Immobilize 45% base | Side/rear +10 status chance; immunity respected. |
| `Steal Heart` | Action | Charm 30% base | Side +5, rear +15; no gender restriction; damage breaks. |
| Equipment steals | Action | real low-odds permanent equipment theft | No damage rider; no generated items; no hidden status. |
| `Steal Gil` | Action | preserved conservatively | No Gil tuning in this pass. |
| `Steal EXP` | Action | cut/deferred | EXP/JP economy out of scope. |
| `Sticky Fingers` | Reaction | miss-trigger riposte x0.60 or x0.75 from rear | Non-Brave; no steal reward; once/round. |
| `Light Fingers` | Support | steal success +15 percentage points | Steal actions only; no broad accuracy. |
| `Poach` | Support | deferred | Monster/economy scope later. |
| `Move +2` | Movement | Move +2 | Movement-slot option; no terrain/elevation bypass. |
| `Jump +2` | Movement | Jump +2 | Movement-slot option; vertical route. |
| `Treasure Hunter` | Movement | deferred campaign/map hook | No Gil edits; no mandatory reward assumption. |

## Action Notes

### Backstab

`Backstab` is Thief's primary positional damage button.

```text
rear = weapon output x1.50, hit +0.15
side = weapon output x1.20, hit +0.05
front = normal attack fallback, no bonus
```

Rules:

- knife or sword required;
- knife is the native Thief identity;
- no status rider;
- no steal rider;
- no bonus from the front.

The front fallback avoids wasted clicks, but the action's real value comes from positioning. The rear
multiplier is intentionally bold; the cost is making a leather unit reach a vulnerable angle.

### Venom Knife

`Venom Knife` gives Thief readable attrition pressure.

```text
damage = weapon output x0.75
Poison chance = 60% base
side/rear status chance bonus = +10 percentage points
```

Rules:

- knife or sword required;
- visible `Poison`;
- immunity respected;
- no hidden damage-over-time variant.

This gives Thief a fantasy-correct pressure tool without copying caster burst or Mystic control.

### Arm Aim

`Arm Aim` is Thief's action-denial opportunity strike.

```text
damage = weapon output x0.55
Disable chance = 35% base
side/rear status chance bonus = +10 percentage points
```

Rules:

- knife or sword required;
- visible `Disable`;
- immunity respected;
- bounded duration follows status policy;
- lower reliability than dedicated controllers.

`Disable` is strong. The low base chance, positional bonus, and weapon damage trade are the guardrails.

### Leg Aim

`Leg Aim` is Thief's movement-denial opportunity strike.

```text
damage = weapon output x0.55
Immobilize chance = 45% base
side/rear status chance bonus = +10 percentage points
```

Rules:

- knife or sword required;
- visible `Immobilize`;
- immunity respected;
- no Stop;
- no action denial.

It is more reliable than `Arm Aim` because preventing movement is weaker than preventing actions.

### Steal Heart

`Steal Heart` keeps classic Thief charm flavor.

```text
Charm chance = 30% base
side bonus = +5 percentage points
rear bonus = +15 percentage points
```

Rules:

- no gender restriction;
- visible `Charm`;
- damage breaks Charm;
- immunity respected;
- no broad social-control package.

This preserves original Thief flavor while leaving Orator room to own deeper social control later.

### Equipment Steals

Equipment steals stay real and exciting.

Accepted equipment steal list:

- `Steal Helm`;
- `Steal Armor`;
- `Steal Shield`;
- `Steal Weapon`;
- `Steal Accessory`.

Rules:

- success removes the actual equipped item from the enemy;
- stolen equipment is kept by the player if the final data supports vanilla-style theft of that
  existing item;
- only equipment actually present on the enemy can be stolen;
- no new equipment is created;
- no Gil value, shop price, sell value, or reward table is changed;
- no damage rider;
- failure only spends the action;
- boss/protected target immunity is respected where final policy requires it.

Permanent theft is accepted because planning around stealing a specific enemy's special gear is part
of the fun. Campaign equipment pacing remains a validation watch, not a reason to make steals
battle-scoped in this pass.

### Steal Success Model

Steals should stay low-odds enough to make setup meaningful.

Base rates:

| Steal target | Base chance |
| --- | ---: |
| Helm / Armor / Shield | 35% |
| Weapon / Accessory | 25% |
| Heart | 30% |

Facing modifiers:

| Facing | Modifier |
| --- | ---: |
| Front | -10 percentage points |
| Side | +5 percentage points |
| Rear | +15 percentage points |

Speed modifier:

```text
speed_mod = clamp(3 * (user_speed - target_speed), -12, +12)
final_chance = clamp(base + facing_mod + speed_mod + support_mod, 10, 65 before special policy)
```

`Light Fingers` is the accepted support modifier. No level-difference term is included; Speed and
positioning are the readable levers.

### Steal Gil

`Steal Gil` is preserved conservatively.

Rules:

- no Gil value changes;
- no shop price changes;
- no sell value changes;
- no reward-table changes;
- no combat-power assumption in this pass.

If the vanilla action remains, it should be treated as a low-stakes flavor/economy action until the
economy policy validates it. This artifact does not tune Gil.

### Steal EXP

`Steal EXP` is cut or deferred.

EXP/JP economy is out of scope for this job artifact, and there is no accepted combat value for it in
this pass.

## Reaction Notes

### Sticky Fingers

`Sticky Fingers` becomes a non-Brave opportunity riposte.

```text
trigger = adjacent enemy misses Thief with a direct melee/fist/weapon attack
chance = fixed/capped 55%
frequency = once per unit round
effect = immediate weapon output x0.60 riposte
rear-facing trigger = weapon output x0.75
```

Rules:

- not Brave-scaled as the main lever;
- no steal reward;
- no Gil/economy hook;
- no recursion;
- no ranged, magic, status-only, or area trigger;
- attacker must be in legal weapon/fist range for the riposte.

This keeps the thiefly opportunity fantasy without creating reactive economy abuse.

## Support And Movement Notes

### Light Fingers

`Light Fingers` is the committed stealing support.

```text
steal success +15 percentage points
```

Rules:

- steal actions only;
- no ordinary accuracy;
- no `Backstab`;
- no `Venom Knife`;
- no `Arm Aim`;
- no `Leg Aim`;
- no Charm unless final data classifies `Steal Heart` as a steal action for this support;
- no immunity bypass;
- no boss/protected target bypass.

This gives players a clear theft-build choice without becoming generic accuracy support.

### Poach

`Poach` is deferred.

Monsters are out of scope for the current job pass, and poach rewards touch campaign economy and item
availability. The name and fantasy are preserved for a later monster/economy pass.

### Move +2

`Move +2` remains a bold Thief movement option:

- Move +2;
- no terrain bypass;
- no elevation bypass;
- competes with every other movement skill through the movement slot.

### Jump +2

`Jump +2` remains a vertical Thief movement option:

- Jump +2;
- no horizontal Move bonus;
- supports rooftops, rear arcs, and steal routes.

`Move +2` and `Jump +2` are movement-slot options. The player chooses the mobility profile.

### Treasure Hunter

`Treasure Hunter` is deferred as a campaign/map reward hook.

Rules for the current pass:

- no Gil edits;
- no mandatory campaign reward assumption;
- no combat mobility increase beyond occupying the movement slot;
- future map/economy validation required.

## Expected Play Patterns

Healthy Thief patterns:

- Thief uses mobility to reach rear arcs for `Backstab` or steals;
- party uses `Leg Aim` or `Arm Aim` to set up a planned equipment steal;
- `Venom Knife` gives Thief visible attrition pressure;
- `Light Fingers` marks a committed steal specialist;
- stealing special enemy gear remains an emotional player-planning moment;
- `Sticky Fingers` makes missed melee engagement against Thief risky without touching economy.

Unhealthy Thief patterns to watch:

- `Backstab` becomes too easy to spam and outdamages dedicated physical jobs;
- `Arm Aim` or `Leg Aim` makes Thief a better controller than Time, Mystic, or Orator;
- `Light Fingers` plus rear positioning makes rare gear theft too reliable;
- permanent equipment theft breaks campaign equipment pacing;
- `Move +2` plus `Jump +2` makes Thief too safe on every map;
- `Sticky Fingers` creates reward abuse if later changed back into reactive stealing.

## Validation Hooks

These are later validation hooks, not blockers to this provisional rediscussion artifact:

- `T4 accuracy/evasion`: test `Backstab`, `Venom Knife`, `Arm Aim`, `Leg Aim`, `Steal Heart`, steal
  facing, and steal hit rates.
- `T5 status/duration`: test `Poison`, `Disable`, `Immobilize`, and `Charm` windows.
- `T7/T6xT7 equipment`: test permanent equipment theft, enemy fallback, and pacing impact.
- Deferred economy policy: test `Steal Gil`, `Treasure Hunter`, `Poach`, and any permanent reward
  implications without changing Gil values in this pass.
- `M-SECONDARY-COUNT`: count Steal secondary, `Light Fingers`, `Sticky Fingers`, `Move +2`, and
  `Jump +2` incidence.
- `F5 real-roster sweep`: test active Thief, non-Thief steal secondary, and parties built around
  stealing special gear.
- Control identity sweep: compare `Arm Aim`, `Leg Aim`, and `Steal Heart` against Time, Mystic, and
  Orator.

## Reviewer Notes

Claude reviewed the opening Thief package before this artifact was written and approved the core
direction:

- keep steals, including permanent vanilla-style theft of existing enemy equipment;
- do not replace steals with invisible exposure statuses;
- add D&D-style `Backstab`, poison, and arm/leg disruption;
- drop the level-difference term from steal success;
- use Speed and positioning as the readable steal levers;
- keep `Steal Gil` conservative and cut/defer `Steal EXP`;
- use `Sticky Fingers` as a non-Brave opportunity riposte, not a reward reaction;
- keep `Light Fingers` steal-only;
- keep bold movement through `Move +2` and `Jump +2`;
- defer `Poach` and `Treasure Hunter` to monster/economy validation.

Claude accepted this artifact after review. Thief is closed for this rediscussion pass pending
Marcelo validation.
