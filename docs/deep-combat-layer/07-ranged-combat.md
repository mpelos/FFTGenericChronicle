# Ranged Combat

This document owns ranged Effective Skill, distance penalties, Accuracy, skill-granted Aim, native
FFT trajectories, legal ranged defenses, and forecast hit chance.

## Ranged attack pipeline

```text
RangedWeaponSkill = GurpsSkillScore(DX, weaponDifficulty, Rank)

EffectiveSkill = RangedWeaponSkill
                 + AimBonus
                 - range penalty
                 - location penalty
                 - Shock/state penalties
```

The attacker succeeds on `3d6 <= EffectiveSkill`. A noncritical success then receives one legal
active defense. A critical success bypasses defense.

Difficulty is already part of Ranged Weapon Skill and is not applied a second time during the shot.

## Distance penalty

Horizontal FFT grid distance maps directly to the GURPS range bands:

| Distance in tiles | Skill penalty |
| ---: | ---: |
| 1-2 | 0 |
| 3 | -1 |
| 4-5 | -2 |
| 6-7 | -3 |
| 8-10 | -4 |
| 11-15 | -5 |
| 16-20 | -6 |
| 21-30 | -7 |
| 31-50 | -8 |

The weapon's maximum range and FFT's native vertical/trajectory rules determine target legality.
Height and intervening terrain add no new generic hit modifier.

Skill above 16 remains valuable because it absorbs these penalties. It is not capped before the
range calculation.

## Accuracy and Aim

Accuracy belongs to the weapon and applies only when an explicit ability grants the Aim state. Aim
is not a universal command.

```text
mobile or immediate shot -> AimBonus = 0
first granted Aim step   -> AimBonus = Acc
second consecutive step  -> AimBonus = Acc + 1
third or later step      -> AimBonus = Acc + 2
```

Aim tracks one target. Changing target, losing the legal trajectory, moving before the shot, or
entering Stun or Knocked Down cancels the accumulated bonus. Injury threatens retention per Strike:

```text
AimRetentionScore = Will
                    + AimRetentionModifier
                    - explicit state penalties
```

After each Strike causing `Injury > 0`, a unit that still has Aim rolls
`3d6 <= AimRetentionScore`. Shock is not subtracted because this is a Will check. Success preserves
the complete accumulated bonus; failure clears Aim immediately. A landed hit reduced to zero Injury
by DR causes no roll. Injury plus forced movement does not create two rolls because forced movement
already cancels Aim directly. Stun, Knocked Down, KO, loss of the tracked target, and loss of legal
trajectory cancel directly without a retention roll. The remaining combo continues and its one
Reaction window remains post-action.

The granting ability declares its Action/Movement cost. Unless it states otherwise, movement before
the prepared shot cancels Aim; on the firing turn, the unit fires before moving and may use
Movement afterward.

## Free movement and shooting

A unit may use normal Movement and make an immediate ranged Attack without the GURPS Move-and-Attack
penalty, Bulk penalty, skill-9 cap, or loss of active defenses. It receives no Accuracy unless it
previously Aimed and obeys the Aim movement restriction.

This creates two possible modes when the unit has access to an Aim-granting ability:

- mobile shot: flexible position, base skill;
- aimed shot: positional commitment, Accuracy bonus.

## Trajectories

Bows, crossbows, guns, and thrown weapons use their native FFT line, height, and trajectory rules.
The DCL does not add cover bands, partial exposure, collision redirection, or a generic high-ground
modifier. Item metadata selects only a route supported by the native weapon behavior.

## Legal defenses

| Attack | Dodge | Block | Parry |
| --- | --- | --- | --- |
| Bow | Yes | Yes | No |
| Crossbow | Yes | Yes | No |
| Gun | Yes | No | No |
| Thrown weapon | Yes | Yes | No by default |

Dodge against gunfire represents reacting to the visible attacker before the shot, not outrunning
the projectile. Back attacks deny Dodge through the facing rule. Ordinary shield DB and Block do
not apply to gunfire.

Facing, native trajectory/height legality, and target locations are owned by
[Facing, Reach, and Targeting](04-facing-reach-and-targeting.md).

## Knocked Down interactions

- Ranged Skill, Dodge, and Block use the modifiers defined by
  [Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md#knockdown-and-knocked-down).
- Bows cannot normally fire while Knocked Down; crossbows and guns can, subject to their item profiles.
- Thrown attacks receive the posture penalty defined by the action.

## Head shots

Only an explicit ranged ability can target the Head. It applies its authored location penalty in
addition to range and uses only HeadDR on a hit. There is no universal head-shot command.

## Final hit chance

The forecast's headline percentage includes attack success, critical bypass, and the selected active
defense:

```text
A = probability of attack success at Effective Skill
C = probability of critical attack success
D = probability of selected defense success

FinalHitChance = C + (A - C) * (1 - D)
```

If no defense is legal, `FinalHitChance = A`.

`A`, `C`, and `D` come from exact enumeration of the 216 outcomes of 3d6, including the attack
critical rules and the defense roll's automatic success/failure rules. The resolver does not use a
linear approximation such as `5% * score`, and it never computes hit chance by subtracting an
evasion percentage from an attack percentage. Enumeration and display rounding follow the
[Numeric Resolution Contract](17-numeric-resolution-contract.md#exact-forecast-probabilities).

The detailed forecast also shows Effective Skill, distance penalty, skill-granted Aim, explicit
location modifier, selected defense, and whether that defense will be consumed or penalized.

### Worked hit-chance example

An archer has Bow Skill 15 and fires at distance 6 (`-3`). The shot is not Aimed, so Effective Skill
is 12. The target's selected legal defense is Dodge 9.

```text
A = P(attack succeeds at 12) = 160 / 216
C = P(critical attack at 12) =   4 / 216
D = P(defense succeeds at 9) =  81 / 216

FinalHitChance = C + (A - C) * (1 - D)
               = 21,924 / 46,656
               = 46.99%
```

Without a legal defense the same shot would show `160 / 216 = 74.07%`. The difference is not a
37.5-point subtraction: the defense is rolled only after a noncritical attack success.

## Deliberately omitted ranged modifiers

Ordinary ranged attacks do not use Size Modifier, target movement speed, Bulk, Rate of Fire,
Recoil, half-damage range, or a Perception pre-roll. An item or ability may define Ready/reload or
multi-hit behavior explicitly.
