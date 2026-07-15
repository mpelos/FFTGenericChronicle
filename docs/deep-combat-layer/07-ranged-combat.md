# Ranged Combat

This document owns ranged Effective Skill, distance penalties, Accuracy, Aim, trajectories, legal
ranged defenses, and forecast hit chance.

## Ranged attack pipeline

```text
RangedWeaponSkill = GurpsSkillScore(DX, weaponDifficulty, Rank)

EffectiveSkill = RangedWeaponSkill
                 + AimBonus
                 + elevation modifier
                 - range penalty
                 - cover penalty
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

The weapon's maximum range still determines target legality. Height is handled separately so the
player can see whether distance, elevation, or cover caused a penalty.

Skill above 16 remains valuable because it absorbs these penalties. It is not capped before the
range calculation.

## Accuracy and Aim

Accuracy belongs to the weapon and applies only after Aim.

```text
mobile or immediate shot -> AimBonus = 0
one Aim Action           -> AimBonus = Acc
two consecutive Aims     -> AimBonus = Acc + 1
three or more Aims       -> AimBonus = Acc + 2
```

Aim tracks one target. Changing target, losing the legal trajectory, moving before the shot, or
entering Stun/Prone cancels the accumulated bonus. Damage may require a Will check to retain Aim.

Aim consumes Action and restricts Movement until the shot. On the firing turn, the unit fires
before moving and may use Movement afterward.

## Free movement and shooting

A unit may use normal Movement and make an immediate ranged Attack without the GURPS Move-and-Attack
penalty, Bulk penalty, skill-9 cap, or loss of active defenses. It receives no Accuracy unless it
previously Aimed and obeys the Aim movement restriction.

This creates two valid modes:

- mobile shot: flexible position, base skill;
- aimed shot: positional commitment, Accuracy bonus.

## Trajectories

| Trajectory | Interaction |
| --- | --- |
| Arc | May pass over units and low cover when the weapon profile permits; interacts favorably with height. |
| Direct | Intervening units and terrain block or provide cover. |

Bows normally use an arcing trajectory. Crossbows and guns normally use direct trajectories. An
individual item may override its family only through an explicit property.

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

Facing, cover, elevation, and target locations are owned by
[Facing, Reach, and Targeting](04-facing-reach-and-targeting.md).

## Prone interactions

- Ranged Skill, Dodge, and Block use the modifiers defined by
  [Status Resistance and Posture](08-status-resistance-and-posture.md).
- Bows cannot normally fire while Prone; crossbows and guns can, subject to their item profiles.
- Thrown attacks receive the posture penalty defined by the action.

## Head shots

A ranged head attack applies its location penalty in addition to range and cover. On a hit it uses
only HeadDR. Aim and high mastery are the intended ways to make such a shot reliable.

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
evasion percentage from an attack percentage.

The detailed forecast also shows Effective Skill, distance penalty, Aim, cover, elevation, location,
selected defense, and whether that defense will be consumed or penalized.

### Worked hit-chance example

An archer has Bow Skill 14, fires at distance 6 (`-3`), and has a clear `+1` elevation advantage.
The shot is not Aimed, so Effective Skill is 12. The target's selected legal defense is Dodge 9.

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
