# Facing, Reach, and Targeting

This document owns spatial modifiers that determine whether an attack is legal and which defenses
and armor locations apply.

## Facing

Facing affects active defense rather than raw damage:

| Attack direction | Active-defense rule |
| --- | --- |
| Front | Full legal defense. |
| Side | Defense at `-2`; Block requires the shield side. |
| Back | No active defense. |

The back rule represents lack of awareness without requiring a Perception attribute. A critical hit
also removes the defense roll regardless of direction.

Facing never changes BodyDR or HeadDR. Armor still protects an undefended target.

## Reach

FFT melee weapons use only two Reach values:

| Reach | Targeting |
| ---: | --- |
| 1 | Adjacent tile. |
| 2 | Up to two tiles, subject to path and height legality. |

There is no Reach C and no same-tile close-combat subsystem. Fists, knives, and ordinary one-tile
weapons use Reach 1. Reach 2 is an item advantage paid for through the rest of the weapon profile;
it does not automatically create GURPS grip changes, point-blank penalties, or free stop-hits.

Leaving Reach does not trigger a universal attack of opportunity. An explicit reaction or ability
may provide one.

## Target locations and DR

The physical DCL exposes two armor locations:

- Body, protected by body equipment;
- Head, protected by head equipment.

Normal attacks are abstract attacks against the protected silhouette:

```text
NormalDR = BodyDR + HeadDR
```

Only an attack or skill with an explicit location changes this:

```text
Head-targeting attack -> ApplicableDR = HeadDR
Body-targeting attack -> ApplicableDR = BodyDR
```

A location attack also applies its authored skill penalty and any explicit injury or HT-check
modifier. It does not receive an implicit skull multiplier or universal crippling effect.

## Head attacks

A generic head-targeting technique uses a substantial Weapon Skill penalty; `-4` is the physical
layer's reference value. Its primary reward is bypassing BodyDR. A particular skill may additionally
increase injury or make a resulting Major Wound harder to resist, but those benefits must be stated
on that skill.

## Line of sight and cover

A physical attack with no valid trajectory is illegal. Partial exposure instead modifies Effective
Skill:

| Cover | Skill modifier |
| --- | ---: |
| Light | -1 |
| Significant | -2 |
| Strong | -4 |
| Total | Illegal target |

Direct trajectories interact with intervening units and terrain. Arcing trajectories may pass over
units and low cover when the weapon profile permits it.

Unit-targeted magic is an explicit exception. It preserves FFT target tracking, performs no
line-of-sight or cover check, and does not use the physical range-penalty table. That complete
contract is owned by
[Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md#unit-targets-follow-fft-tracking).

## Elevation

Height affects target legality through FFT's vertical rules and may modify Effective Skill:

| Relative height | Reference modifier |
| --- | ---: |
| Attacker clearly above target | +1 |
| Similar elevation | 0 |
| Target clearly above attacker | -1 |
| Trajectory outside vertical limits | Illegal target |

Elevation is separate from horizontal range penalty so that the forecast can explain both terms.

## Awareness without Perception

Awareness is deterministic:

- front and side attacks are perceived unless a status says otherwise;
- back attacks deny active defense;
- Blind, Invisible, or an explicit surprise effect may replace this rule;
- no generic Vision or Perception roll is inserted into ordinary combat.

This keeps facing readable and avoids a hidden pre-roll before the attack and defense rolls.
