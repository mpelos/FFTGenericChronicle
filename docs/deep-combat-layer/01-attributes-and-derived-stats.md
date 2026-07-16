# Attributes and Derived Stats

This document owns the mapping from FFT character data to the DCL's GURPS-like characteristics.

## Primary attributes

| DCL attribute | Permanent FFT source | Physical-combat role |
| --- | --- | --- |
| ST | Raw PA plus current modifiers | Thrust/swing damage, the base of HP, and Basic Lift. |
| DX | Raw Speed plus current modifiers | Weapon Skill, Shield Skill, and Basic Speed. |
| IQ | Raw MA plus current modifiers | Will, magical-tradition skills, magical power, and one possible MP base. |
| HT | `BraveToHT(current Brave)` | Basic Speed, physical resistance, and one possible MP base. |

Raw PA, Raw Speed, and Raw MA are the character's permanent pre-job and pre-equipment attributes.
The active job, equipment, and states apply explicit additive adjustments without rewriting those
raw values:

```text
ST = RawPA    + JobSTAdjustment + EquipmentSTAdjustment + StateSTAdjustment
DX = RawSpeed + JobDXAdjustment + EquipmentDXAdjustment + StateDXAdjustment
IQ = RawMA    + JobIQAdjustment + EquipmentIQAdjustment + StateIQAdjustment
```

Ordinary weapons never add to these attributes. A rare nonweapon effect may explicitly modify an
attribute, but it must be priced as an attribute modifier rather than hidden inside Weapon Power or
armor progression. Permanent growth and job-adjustment ownership are defined in
[Character Growth and Job Stat Modifiers](15-character-growth-and-job-stat-modifiers.md).

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
BaseMaxHP  = ST + CharacterHPModifier + JobHPModifier
MaxHP      = BaseMaxHP + explicit equipment/status HP modifiers
BaseMaxMP  = max(HT, IQ) + CharacterMPModifier + JobMPModifier
MaxMP      = BaseMaxMP + explicit equipment/status MP modifiers
Will       = IQ + explicit Will modifiers
BasicLift  = LiftScale(ST * ST / 5)
BasicSpeed = (DX + HT) / 4 + JobBasicSpeedAdjustment + explicit Basic Speed modifiers
BasicMove  = floor(BasicSpeed) + JobMoveAdjustment + explicit Move modifiers
BaseDodge  = floor(BasicSpeed) + 3
```

`LiftScale` bridges GURPS-shaped ST to FFT's equipment-Weight scale. HP and MP remain on the same
additive numeric scale as the four attributes and their character/job modifiers.

Basic Speed is displayed at its actual value, including `.25`, `.50`, and `.75`. It is not doubled
or converted back to the vanilla FFT Speed scale.

Because HT is Brave-derived, Brave affects Basic Speed, Basic Move, and Dodge through HT's
one-quarter share. This is the GURPS attribute package, not a second direct Brave bonus: four points
of HT are required to add one full point of Basic Speed before flooring. Raw Speed/DX owns the other
half of the formula and also owns every physical skill.

## HP

HP starts from ST and receives signed character and job modifiers:

```text
BaseMaxHP = ST + CharacterHPModifier + JobHPModifier
```

The per-unit Raw HP storage is reinterpreted as `CharacterHPModifier`; it is no longer a complete HP
pool. `JobHPModifier` belongs to the current job's chassis. This keeps unit growth separate from the
bonus or penalty gained by changing jobs.

A stronger character therefore has three linked benefits that belong to the GURPS ST package:

- stronger thrust and swing damage;
- a larger HP pool;
- greater carrying capacity.

Armor protects through DR and does not normally increase the character's body. An item that truly
grants vitality must say that it modifies HP; a normal body armor or helmet does not disguise DR as
an HP bonus. Character, job, equipment, and status HP modifiers are additive and may be positive or
negative.

## MP

MP is the only extraordinary-energy pool:

```text
BaseMaxMP = max(HT, IQ) + CharacterMPModifier + JobMPModifier
```

The per-unit Raw MP storage is reinterpreted as `CharacterMPModifier`; it is no longer a complete MP
pool. `JobMPModifier` is the canonical DCL name for the job's FP-like capacity adjustment. It does
not create a separate Fatigue Point characteristic.

The higher of HT and IQ supplies the base. A bodily specialist may power Ki or another supernatural
technique through HT; an intellectual specialist may power spells through IQ. The lower attribute
does not contribute until it becomes the higher one. This represents the unit's best energy channel,
not a combined average of body and mind.

Every job therefore has some MP. Consuming MP does not classify an action as a Spell: source,
governing skill, Faith, Silence, Reflect, resistance, and damage behavior are declared independently
in [Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md).

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

## Progression ownership

Character Level, EXP, shared growth profiles, permanent Brave growth, job stat chassis, and
point-equivalent balance weights are owned by
[Character Growth and Job Stat Modifiers](15-character-growth-and-job-stat-modifiers.md). Job Level
training and JP ownership are defined with their physical and magical skills in
[Skills and Active Defenses](03-skills-and-active-defenses.md) and
[Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md).

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

Magical evasion fields are reinterpreted by delivery, Magic Resistance, and explicit equipment
properties as defined in
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#magic-resistance-and-retired-magical-evasion).
