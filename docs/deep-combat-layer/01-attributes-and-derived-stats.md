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
ST = max(1, RawPA
            + JobSTAdjustment
            + EquipmentSTAdjustment
            + StateSTAdjustment)

DX = max(1, RawSpeed
            + JobDXAdjustment
            + EquipmentDXAdjustment
            + StateDXAdjustment)

IQ = max(1, RawMA
            + JobIQAdjustment
            + EquipmentIQAdjustment
            + StateIQAdjustment)
```

Ordinary weapons never add to these attributes. A rare nonweapon effect may explicitly modify an
attribute, but it must be priced as an attribute modifier rather than hidden inside Weapon Power or
armor progression. Permanent growth and job-adjustment ownership are defined in
[Character Growth and Job Stat Modifiers](15-character-growth-and-job-stat-modifiers.md).

Brave converts to the meaningful 3d6 HT band with a neutral midpoint at Brave 50 and open-ended
heroic progression above the vanilla Brave range:

```text
HT = max(4, 10 + roundNearest((current Brave - 50) / 8))
```

`roundNearest` follows the shared
[Numeric Resolution Contract](17-numeric-resolution-contract.md#shared-operations). The anchors are:

| Brave | HT |
| ---: | ---: |
| 0 | 4 |
| 25 | 7 |
| 50 | 10 |
| 75 | 13 |
| 100 | 16 |
| 112 | 18 |
| 120 | 19 |

Current Brave is the storage from which HT is derived; it is not a universal percentage chance.
Brave above 100 therefore continues increasing HT without requiring a second probability scale.

## Secondary characteristics

```text
BaseMaxHP  = ST + CharacterHPModifier + JobHPModifier
MaxHP      = max(1, BaseMaxHP + explicit equipment/status HP modifiers)
BaseMaxMP  = max(HT, IQ) + CharacterMPModifier + JobMPModifier
MaxMP      = max(1, BaseMaxMP + explicit equipment/status MP modifiers)
Will       = IQ + explicit Will modifiers
BasicLift  = ST * ST / 5
BasicSpeed = (DX + HT) / 4 + JobBasicSpeedAdjustment + explicit Basic Speed modifiers
BasicMove  = floor(BasicSpeed) + JobMoveAdjustment + explicit Move modifiers
BaseJump   = max(1, 3 + JobJumpAdjustment + explicit equipment/status Jump modifiers)
BaseDodge  = floor(BasicSpeed) + 3
```

Basic Lift uses the GURPS curve directly and retains exact rational precision when the division by
five is not integral. Equipment Weight is authored against this scale; there is no separate lift
multiplier. HP and MP remain on the same additive numeric scale as the four attributes and their
character/job modifiers.

Basic Speed is displayed at its actual value, including `.25`, `.50`, and `.75`. It is not doubled
or converted back to the vanilla FFT Speed scale.

ST, DX, and IQ are positive primary characteristics after their complete additive expression. This
guarantees a legal ST/IQ damage-table lookup and nonzero Basic Lift without clamping the skills and
situational scores derived from them. HT retains its separate Brave-derived minimum of four.

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

When a job, item, or state changes a maximum pool during battle, the current pool reconciles without
creating an effect event:

```text
NewCurrentHP = min(OldCurrentHP, NewMaxHP)
NewCurrentMP = min(OldCurrentMP, NewMaxMP)
```

Increasing a maximum does not heal or restore the difference. A downward clamp is not Injury, HP
payment, or MP drain and triggers none of their Reactions. It still updates every dependent preview
and commitment; a voluntary change that would violate an existing cast commitment is illegal under
the resource rule. A unit already at zero HP remains in native KO.

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

The DCL has no Perception characteristic or detection subsystem. Facing, line of sight, attack
direction, and native FFT target legality determine awareness without a separate roll.

## Move and Jump

Effective Move starts from Basic Move and is reduced by encumbrance as defined in
[Equipment and Encumbrance](06-equipment-and-encumbrance.md). Its use in the turn economy is defined
by [Turns, Movement, and Actions](02-turns-movement-and-actions.md). After all ordinary Move
modifiers and encumbrance, the Critical low-HP rule halves final Move as defined by
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md#critical-low-hp).

Jump remains an FFT grid characteristic with neutral base 3. Job, ability, equipment, state, and
encumbrance modifiers change it through the BaseJump formula above. Jump is not derived from a new
GURPS characteristic and does not feed initiative, attack skill, or active defense. Critical low HP
does not halve Jump.

## Initiative and CT

Basic Speed owns initiative order but not turn frequency. The CT contract is defined in
[Turns, Movement, and Actions](02-turns-movement-and-actions.md).

## Progression ownership

Character Level, EXP, per-job growth vectors, permanent Brave growth, job stat chassis, and
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
