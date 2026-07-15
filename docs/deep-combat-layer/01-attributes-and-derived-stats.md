# Attributes and Derived Stats

This document owns the mapping from FFT character data to the DCL's GURPS-like characteristics.

## Primary attributes

| DCL attribute | FFT source | Physical-combat role |
| --- | --- | --- |
| ST | Raw PA | Thrust/swing damage, HP, and Basic Lift. |
| DX | Raw Speed | Weapon Skill, Shield Skill, and Basic Speed. |
| IQ | Raw MA | Will and the future magic skill axis. |
| HT | `BraveToHT(current Brave)` | Basic Speed and physical resistance checks. |

Raw PA, Raw Speed, and Raw MA are the character's pre-equipment stats. Ordinary weapons never add
to these attributes. A rare nonweapon effect may explicitly modify an attribute, but it must be
priced as an attribute modifier rather than hidden inside Weapon Power or armor progression.

Brave converts to the meaningful 3d6 HT band with a neutral midpoint at Brave 50 and a superhuman
ceiling at HT 16:

```text
HT = clamp(4, 16, 10 + roundNearest((current Brave - 50) / 8))
```

`roundNearest` rounds to the nearest integer, with exact halves away from zero. The anchors are:

| Brave | HT |
| ---: | ---: |
| 0 | 4 |
| 25 | 7 |
| 50 | 10 |
| 75 | 13 |
| 100 | 16 |

Raw Brave remains available to mechanics that expressly use Brave percentage; those rolls do not
replace HT checks.

## Secondary characteristics

```text
MaxHP      = HPScale(ST)
Will       = IQ + explicit Will modifiers
BasicLift  = LiftScale(ST * ST / 5)
BasicSpeed = (DX + HT) / 4
BasicMove  = floor(BasicSpeed) + JobMoveAdjustment + explicit Move modifiers
BaseDodge  = floor(BasicSpeed) + 3
```

`HPScale` and `LiftScale` bridge GURPS-shaped values to FFT's heroic HP and equipment-Weight scales.
They do not change which attribute owns each result.

Basic Speed is displayed at its actual value, including `.25`, `.50`, and `.75`. It is not doubled
or converted back to the vanilla FFT Speed scale.

Because HT is Brave-derived, Brave affects Basic Speed, Basic Move, and Dodge through HT's
one-quarter share. This is the GURPS attribute package, not a second direct Brave bonus: four points
of HT are required to add one full point of Basic Speed before flooring. Raw Speed/DX owns the other
half of the formula and also owns every physical skill.

## HP

HP derives from ST rather than from a separate base-HP progression. A stronger character therefore
has three linked benefits that belong to the GURPS ST package:

- stronger thrust and swing damage;
- a larger HP pool;
- greater carrying capacity.

Armor protects through DR and does not normally increase the character's body. An item that truly
grants vitality must say that it modifies HP; a normal body armor or helmet does not disguise DR as
an HP bonus.

## Will and omitted Perception

Will is the resistance characteristic for mental coercion and loss of voluntary control. It derives
from IQ, not Brave, because Brave already owns HT. This prevents Brave from simultaneously owning
physical health and every mental resistance.

The physical DCL has no Perception characteristic. Facing, line of sight, and attack direction
determine awareness without a separate detection roll. A later subsystem may define explicit
detection mechanics without changing the physical attribute map.

## Move and Jump

Effective Move starts from Basic Move and is reduced by encumbrance as defined in
[Equipment and Encumbrance](06-equipment-and-encumbrance.md). Its use in the turn economy is defined
by [Turns, Movement, and Actions](02-turns-movement-and-actions.md).

Jump remains an FFT grid characteristic. It controls vertical traversal and receives job, ability,
equipment, state, and encumbrance modifiers. Jump is not derived from a new GURPS characteristic and
does not feed initiative, attack skill, or active defense.

## Initiative and CT

Basic Speed owns initiative order but not turn frequency. The CT contract is defined in
[Turns, Movement, and Actions](02-turns-movement-and-actions.md).

## Job and campaign progression

| FFT field | DCL role |
| --- | --- |
| Character Level | Produces permanent Raw PA, Raw Speed, and Raw MA growth; it is not added directly to combat rolls. |
| EXP | Advances Character Level; it has no direct battle modifier. |
| Job Level | Determines the training Rank supplied by the active job for Weapon and Shield skills. |
| JP | Purchases abilities; it does not directly increase Weapon or Shield Skill. |

This separation prevents double-dipping: Character Level grows attributes, while Job Level grows
training relative to those attributes.

## Point-equivalent balance weights

Job chassis and stat growth use GURPS point costs as a common accounting unit:

| Improvement | Point-equivalent cost |
| --- | ---: |
| +1 ST | 10 |
| +1 DX | 20 |
| +1 IQ | 20 |
| +1 HT | 10 |
| +1 HP independent of ST | 2 |
| +1 Will independent of IQ | 5 |
| +0.25 Basic Speed independent of DX/HT | 5 |
| +1 Basic Move independent of Basic Speed | 5 |
| +1 Dodge independent of Basic Speed | 15 |
| +1 Parry for one weapon family | 5 |
| +1 Parry for every weapon family | 10 |
| +1 Block | 5 |

These weights compare cumulative expected gains at the same Character Level; raw FFT growth
coefficients are not compared directly. Attribute consequences are included in the attribute's
cost. For example, +1 DX costs 20 total even though it also improves skills and contributes to
Basic Speed; those derived gains are not charged again. A direct job Move or Dodge adjustment is
charged separately because it is not part of an attribute increase.

HT is normally changed through Brave rather than job growth. Any permanent Brave manipulation must
still be evaluated as the corresponding HT change because it also changes resistance and Basic
Speed.

## Evasion and equipment-derived display fields

The vanilla evasion fields become inputs or outputs of the active-defense model rather than
independent percentage rolls:

| FFT field | DCL ownership |
| --- | --- |
| Class/physical evasion | Job Dodge adjustment. |
| Weapon evasion/parry | Weapon Parry modifier and displayed derived Parry. |
| Shield physical evasion | Shield Block modifier/DB and displayed derived Block. |
| Accessory physical evasion | Explicit Dodge or Defense Bonus supplied by that accessory. |
| Weapon Attack R/L | Derived `Xd6+Y` expression for the corresponding equipped weapon. |

Magical evasion fields are outside this physical layer.
