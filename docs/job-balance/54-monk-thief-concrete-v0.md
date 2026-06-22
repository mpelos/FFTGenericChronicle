# Monk And Thief Concrete Provisional V0

Status: Accepted as W3 Monk/Thief concrete-provisional action values
Date: 2026-06-22
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/18-monk-v1-proposal.md`
- `docs/job-balance/22-thief-orator-v1-proposal.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-monk-thief-concrete-v0.json`

## Purpose

This is the third W3 physical/foundation concrete action-value producer.

It covers Monk and Thief because they are the first specialist jobs that can easily compress too
much value through secondary actions: Monk through unarmed damage plus sustain/revive, and Thief
through speed, stealing, equipment disruption, Charm, and economy hooks.

This document sets provisional action values and boundary values for validation. It does not
finalize reaction/support/movement values, prerequisite trees, JP economy, permanent steal rewards,
monster/poach behavior, or full W4/W5 incidence verdicts.

## Atlas Consultation

The vanilla reference atlas was consulted before assigning values:

- Monk Martial Arts records in `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- Thief Steal records in `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `damage`, `physical`, `random`, `healing`, `mp`, `revive`, `status_clear`, `status_add`,
  `steal`, `economy`, `jp_exp`, `reaction`, `support`, and `movement` tags in
  `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- Charm, Doom, Charging, Sleep, Stop, Poison, Blind, Silence, Immobilize, KO, and Undead vocabulary
  in `docs/reference/fft-vanilla-status-effect-map.md`;
- `docs/reference/README.md` as the navigation layer.

The design preserves the FFT read: Monk owns body discipline and unarmed impact; Thief owns fast
precision disruption and steal flavor. It does not preserve vanilla's exact power curve or campaign
reward behavior.

## Scope Boundary

Included here:

- Monk action values;
- Monk healing/revive/status-clear boundaries;
- Thief action values;
- Thief battle-scoped steal/equipment suppression boundaries;
- Charm and Doom status boundaries;
- Band B/C damage checks.

Deferred:

- `Counter`, `First Strike`, `Brawler`, `Martial Discipline`, and `Lifefont` final RSM values;
- `Sticky Fingers`, `Light Fingers`, `Poach`, `Move +2`, and `Treasure Hunter` final values;
- permanent gil, EXP, JP, equipment, poach, treasure, or recruitment rewards;
- monster-scope behavior;
- full W4 populated T2.1 incidence and W5 real-roster dominance verdict.

## Shared Formula Contracts

Physical rows use the current v0.2.1 family model from `work/sim-inputs-v0.2.1.json`.

Monk uses the protected `fists` lane:

```text
routine = br_pa_pa
damage_type = crush
penetration = 0.15
```

Thief uses the speed-linked knife lane:

```text
routine = spd_pa_wp
damage_type = thrust
penetration = 0.10
```

Sustain rows inherit the T3/T3xT5 timing assumptions used by Squire/Chemist and White/Black Mage:
immediate healing can save a target before the next threat tick, same-tick delayed healing is
unsafe, and revive value must be judged against position and death-clock timing.

Steal economy is battle-scoped here. Permanent rewards are explicitly deferred to
`docs/job-balance/23-deferred-campaign-economy-policy.md`.

Equipment-suppression effects from `Steal Armor`, `Steal Shield`, `Steal Weapon`, and
`Steal Accessory` apply only on steal success at the steal-accuracy rate below. They do not apply on
a mere weapon hit. This is the main distinction from Knight control: Thief creates stronger
short-window bursts when the steal succeeds, while Knight creates more reliable sustained pressure.

## Baseline Damage Check

Mid first-specialist rows:

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| Monk fists | 80 | 68 | 71 | 71 |
| Thief knife | 50 | 79 | 69 | 72 |
| Thief fists fallback | 46 | 38 | 40 | 40 |

Read:

- Monk has the protected anti-plate crush lane;
- Thief has a real anti-mail thrust lane through Speed plus knife;
- Thief fallback fists are not a Monk identity;
- Band B Monk should be damage and positioning, not damage plus free sustain.

## Monk Values

Monk should be strong when it commits to body positioning, but it cannot assemble the Band B
Knight-body plus Monk damage/sustain package flagged by doc 51.

| Skill | Value | MP | CT | JP | Band | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| `Pummel` | fists x0.95, +0.10 hit | 0 | 0 | 120 | B | T4/F5 | Reliable adjacent strike; lower on-hit damage than normal attack. |
| `Cyclone` | fists x0.75 small self-centered area | 0 | 0 | 180 | B | T11/F5 | Exposure-priced area pressure; not a safe AoE plan. |
| `Aurablast` | fists x0.80 short range | 0 | 0 | 220 | B/C | T4/F5 | Limited projection for bad maps; lower than melee. |
| `Shockwave` | fists x0.90 line/grounded | 0 | 0 | 360 | B/C | T4/T11/F5 | Lane reward; grounded/line restriction keeps it from replacing Dragoon reach. |
| `Doom Fist` | fists x0.50 plus Doom 40% base | 0 | 0 | 300 | C | T4/T5/T8 | Adjacent pressure-point status; countdown 3; immunity respected. |
| `Purification` | clear Poison, Blind, or Immobilize | 0 | 0 | 240 | C | T4/T2.1 | Self or adjacent ally; not Esuna and not Chemist item breadth. |
| `Chakra` | 40 HP, HP-only | 0 | 0 | 350 | C | T3/T3xT5/T9 | Self or adjacent single target; MP restore deferred. |
| `Revive` | revive at 40 HP | 0 | 0 | 500 | C/D | T3xT5 | Adjacent only; no range safety and no item stock cost. |

`Chakra` is deliberately Band C and HP-only in this pass. MP restoration is an economy/offense
multiplier and remains deferred to T9. If final implementation requires a small MP component, it
must be added as a new T9-validated value, not assumed from vanilla.

### Monk Damage Rows

| Action | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| normal fists | 80 | 68 | 71 | 71 |
| `Pummel` fists x0.95 | 76 | 64 | 67 | 67 |
| `Cyclone` fists x0.75 | 60 | 51 | 53 | 53 |
| `Aurablast` fists x0.80 | 64 | 54 | 56 | 56 |
| `Shockwave` fists x0.90 | 72 | 61 | 63 | 63 |
| `Doom Fist` fists x0.50 | 40 | 34 | 35 | 35 |

Read:

- Monk's ordinary fist attack remains the clearest single-target damage line;
- Martial Arts trade damage for reliability, area, range, line pressure, or status;
- `Cyclone` and `Shockwave` must be judged with target count and map exposure, not only on-hit
  damage;
- `Doom Fist` is a status attempt with chip, not a damage button.

### Monk Sustain Rows

Immediate recovery race, max HP 280:

| Option | HP before | Incoming damage | Final HP | Survives |
| --- | ---: | ---: | ---: | --- |
| no heal | 70 | 90 | 0 | no |
| `Potion` 30 | 70 | 90 | 10 | yes |
| `Chakra` 40 | 70 | 90 | 20 | yes |
| `Hi-Potion` 70 | 70 | 90 | 50 | yes |

Revive comparison:

| Option | Revive HP | Range/timing read |
| --- | ---: | --- |
| `Phoenix Down` | 20 | immediate item, stock/gil, item range rules |
| `Monk Revive` | 40 | immediate adjacent, frontline exposure |
| `Raise` on 280 max HP | 70 | delayed CT spell, MP/Faith/death-clock risk |

This keeps Monk recovery useful but not a replacement for Chemist item reliability or White Mage
spell scaling.

## Thief Values

Thief should feel fast and tactically useful before Ninja. It should not become a better Knight
break user or a safer Orator charm user.

| Skill | Value | MP | CT | JP | Band | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| `Steal Gil` | knife x0.70 plus battle-scoped gil tag | 0 | 0 | 80 | B | economy/T4 | Low-stakes mug action; permanent reward deferred. |
| `Steal Heart` | knife x0.25 plus Charm 35% base | 0 | 0 | 220 | C | T4/T5/T8 | Adjacent/short range; damage breaks Charm; no gender restriction. |
| `Steal Helm` | folded into `Steal Armor` | - | - | - | - | T6/economy | No separate head/body slot value in this pass. |
| `Steal Armor` | knife x0.35 plus armor-specific response delta | 0 | 0 | 350 | C/D | T6/T2.1/economy | One physical hit or target's next turn; reward deferred. |
| `Steal Shield` | knife x0.35 plus shield layer suppressed | 0 | 0 | 350 | C/D | T4/T6/economy | One direct attack or target's next turn; shield layer only. |
| `Steal Weapon` | knife x0.35 plus weapon output x0.60 | 0 | 0 | 500 | C/D | T7/T6xT7/economy | One weapon action or target's next turn; no permanent deletion here. |
| `Steal Accessory` | battle-scoped accessory suppression | 0 | 0 | 500 | D | T2.1/economy | No numeric accessory catalog value yet. |
| `Steal EXP` | deferred economy action | - | - | - | - | economy | No combat value accepted in this pass. |

Steal action accuracy boundary:

```text
base steal rate = 55%
front penalty = -10 percentage points
side/rear bonus = +10 percentage points
Light Fingers interaction = deferred
Concentration interaction = none in this pass
boss/protected target policy = deferred immunity/resistance row
```

This keeps stealing positional and prevents a borrowed Thief secondary from becoming guaranteed
control.

Thief versus Knight control relationship:

- Thief is stronger on successful shield and weapon steals, but later, accuracy-gated, positional,
  and limited to one direct attack, one weapon action, or the target's next turn.
- Knight is weaker per application on shield and weapon pressure, but earlier, more reliable, and
  lasts longer.
- Thief armor exposure is deliberately weaker than Knight's `Rend Armor`: lower deltas and cap
  1.15 instead of 1.20.

`Doom Fist` counterplay remains a T8 requirement. Doom respects immunity and the countdown is
visible, but the final campaign data must provide real removal or immunity handling before Doom is
allowed to become common enemy pressure. Purification does not clear Doom in this pass.

Forward note for Orator: `Steal Heart` occupies only the temporary Charm niche. Orator's later
social control must differentiate through recruit policy, Brave/Faith, speech range, or broader
morale axes rather than becoming a duplicate Charm button.

### Thief Damage Rows

| Action | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| normal knife | 50 | 79 | 69 | 72 |
| `Steal Gil` knife x0.70 | 35 | 55 | 48 | 50 |
| equipment steal knife x0.35 | 17 | 27 | 24 | 25 |
| `Steal Heart` knife x0.25 | 12 | 19 | 17 | 18 |

Read:

- normal knife pressure is the Thief's real damage line;
- steal actions are tactical disruption with chip, not damage upgrades;
- thrust into mail gives Thief a distinct target profile without touching Monk's plate lane.

### Thief Disruption Rows

`Steal Armor` T6 rows, base damage per hit 100:

| Armor | Type | Base response | After `Steal Armor` | Projected damage |
| --- | --- | ---: | ---: | ---: |
| plate | swing | 0.65 | 0.75 | 75 |
| plate | crush | 1.15 | 1.15 | 115 |
| mail | missile | 1.10 | 1.15 | 115 |
| leather | thrust | 0.95 | 0.99 | 99 |
| cloth | swing | 1.00 | 1.00 | 100 |

`Steal Shield` T4 rows, base hit 80, class evade 10, shield evade 30, accessory evade 10:

| Facing | Before | After |
| --- | ---: | ---: |
| front | 45 | 65 |
| side | 50 | 72 |
| rear | 72 | 72 |

`Steal Weapon` output rows:

| Incoming weapon damage | After `Steal Weapon` |
| ---: | ---: |
| 80 | 48 |
| 120 | 72 |

`Steal Heart` status rows:

| Row | Value |
| --- | ---: |
| base Charm chance | 35% |
| side/rear Charm chance | 45% |
| duration | until one controlled action or damage break |

These are strong enough to matter when Thief reaches the right target, but they are not Knight's
reliable frontline control and not Orator's eventual social-control identity.

## Timing Invariant Check

| Invariant | Result |
| --- | --- |
| I1 starter tools not permanent defaults | Preserved: Monk/Thief are specialist tools, not starter replacements. |
| I2 first specialists work before exports | Preserved: Monk damage and Thief knife/steal work before `Brawler`, `Move +2`, or `Light Fingers`. |
| I3 deep secondary on shallow chassis | Watch: Martial Arts and Steal are both attractive secondaries; W4 must count incidence. |
| I4 build-defining supports earned | Preserved here: `Brawler`, `Light Fingers`, and `Move +2` remain deferred. |
| I5 strong reactions not early skips | Preserved here: `Counter`, `First Strike`, and `Sticky Fingers` not finalized. |
| I6 mobility remains a choice | Preserved here: `Lifefont`, `Move +2`, and `Treasure Hunter` not strengthened. |
| I7 sustain has texture | Watch: `Chakra` and `Revive` are useful, but Band C+ and adjacency-bound. |
| I8 protection not upkeep | Not touched. |
| I9 control not replacing damage | Watch: `Steal Heart`, `Doom Fist`, and equipment steals create windows, not locks. |
| I10 late jobs powerful not mandatory | Not touched. |

## Open W3 Links

Still required:

- RSM producer must validate `Counter`, `First Strike`, `Brawler`, `Martial Discipline`,
  `Lifefont`, `Sticky Fingers`, `Light Fingers`, `Move +2`, and `Treasure Hunter`;
- economy producer must decide permanent gil, EXP, equipment, treasure, poach, and recruitment
  rewards;
- W4 must count Martial Arts and Steal secondary incidence, especially Knight-body plus Monk
  sustain and Thief equipment suppression;
- W5 must test Monk's high-Brave stress row, Thief anti-mail value, and no-job comparison rows.

## Claude Review Request

Claude should review whether:

- Monk Band B remains damage/positioning only, with `Chakra` and `Revive` safely held to C/C-D;
- Monk action damage preserves the protected fist identity without making every weapon family
  irrelevant;
- `Purification`, `Chakra`, and `Revive` do not replace Chemist or White Mage lanes;
- Thief steal/equipment values are useful but not stronger than Knight's frontline control;
- `Steal Heart` is bounded enough to avoid replacing Orator/Mystic/Time control;
- economy and permanent rewards are clearly deferred.
