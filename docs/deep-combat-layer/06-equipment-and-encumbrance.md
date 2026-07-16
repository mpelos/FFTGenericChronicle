# Equipment and Encumbrance

This document owns the physical and supernatural rules vocabulary for every equipment slot and the
conversion from item Weight to encumbrance.

No new item entries are required. Existing FFT equipment receives DCL metadata and formulas.

## Weapon schema

Every physical weapon profile declares:

| Property | Function |
| --- | --- |
| Skill family | Selects the DX-based Weapon Skill. |
| Difficulty | Easy, Average, Hard, or Very Hard; intrinsic to the skill family. |
| Damage mode | Thrust, swing, or an explicit fixed basis. |
| Damage modifier | Produces the final `Xd6+Y` with the ST damage table. |
| Damage type | Selects the post-DR wound multiplier. |
| Armor divisor | Reduces applicable DR before subtraction. |
| Reach/range | Reach 1/2 for melee or projectile range for ranged weapons. |
| Hands | One-handed or two-handed. |
| Weight | Contributes to carried Load. |
| Parry modifier | Adjusts derived Parry. |
| Balance | Balanced or Unbalanced. |
| Readiness | Always Ready or Unready After Attack. |
| Ranged properties | Accuracy, maximum range, and trajectory when applicable. |
| Special properties | Element, status, innate ability, or other explicit behavior. |

There is no per-weapon Minimum ST. Job equipment access, aptitude Tier, ST distribution, Weight,
handedness, readiness, and the rest of the weapon profile provide the restrictions.

A weapon that also serves as a magical focus may declare a small, coherent subset of:

- SpellSkillModifier;
- FocusPowerModifier;
- ElementBoost;
- ConcentrationModifier;
- CastCTModifier;
- MPCostModifier;
- tradition compatibility.

One item does not receive every focus axis merely because it is associated with a caster. Each
property competes with the item's physical profile and other equipment opportunities.

## Hands and off-hand

- A one-handed weapon leaves the off-hand available for a shield or a legal second weapon.
- A two-handed weapon prevents shield use and dual wield.
- Dual wield tracks skill, attack, Parry usage, readiness, and balance separately per hand.
- A shield requires an explicit shield-capable job and underlying Shield aptitude.

## Shield schema

Each shield declares:

- Shield Skill family and Difficulty;
- Block modifier;
- Defense Bonus, with facing and projectile restrictions;
- Weight;
- any explicit status, element, or equipment property.

A shield may additionally declare Block or Defense Bonus against Blockable spell manifestations.
This does not protect against Internal Direct effects and does not become a universal Magic
Resistance bonus.

The wielder's DX, Shield Rank, and Job Level produce Shield Skill. The shield item modifies the
result rather than supplying an independent percentage chance.

Shields normally protect through active defense, not passive DR. Projectile-specific legality is
defined by [Ranged Combat](07-ranged-combat.md).

## Body equipment

Body equipment declares:

- BodyDR;
- Weight;
- explicit status or element properties;
- any rare attribute or mobility modifier.

It may also declare explicit HP/MP, IQ, Will, Magic Resistance, status immunity, or elemental
affinity. These are authored properties rather than automatic benefits of armor class.

Body equipment does not normally add HP. Its defensive identity is mitigation through BodyDR paid
for through Weight and equipment access.

## Head equipment

Head equipment declares:

- HeadDR;
- Weight;
- explicit status or element properties;
- any rare attribute or mobility modifier.

It may also declare explicit HP/MP, IQ, Will, Magic Resistance, status immunity, or elemental
affinity. HeadDR remains physical protection; the item does not gain a separate magical DR.

HeadDR contributes to normal protection and is the only equipment DR used by a head-targeting
attack. Hats may trade DR for nonphysical properties without changing the location rule.

## Accessories

Accessories may supply:

- status or element properties;
- explicit Dodge, Defense Bonus, Move, Jump, attribute, HP, or MP modifiers;
- Dodge against evadeable spells or EquipmentMagicResistance;
- Faith, concentration, CastCT, MP-cost, affinity, Reflect, or tradition modifiers;
- Weight, normally low;
- explicit DR in exceptional cases.

An accessory never receives a generic pile of unrelated bonuses merely to keep a legacy field live.
The item must state whether a defensive modifier affects active defense, resistance, injury, or
routing; those layers do not substitute for one another.

## Basic Lift and Load

```text
BasicLift = LiftScale(ST * ST / 5)
Load      = sum(Weight of equipped items)
ratio     = Load / BasicLift
```

The ratio selects the GURPS encumbrance band:

| Encumbrance | Maximum Load | Move multiplier | Dodge penalty |
| --- | ---: | ---: | ---: |
| None | 1 x Basic Lift | x1.0 | 0 |
| Light | 2 x Basic Lift | x0.8 | -1 |
| Medium | 3 x Basic Lift | x0.6 | -2 |
| Heavy | 6 x Basic Lift | x0.4 | -3 |
| Extra-heavy | 10 x Basic Lift | x0.2 | -4 |

```text
EffectiveMove = max(1, floor(BasicMove * encumbranceMoveMultiplier))
EffectiveDodge = Dodge before encumbrance - encumbranceDodgePenalty
```

Jump also receives an authored encumbrance penalty appropriate to FFT's vertical scale.

Encumbrance does not reduce CT gain, initiative, Weapon Skill, Shield Skill, or damage directly.
ST already pays for greater load tolerance through Basic Lift.

## Equipment progression

FFT's frequent equipment replacement remains meaningful. Later weapons may gain larger damage
modifiers, Accuracy, armor divisors, or special properties; later armor may gain more DR. Weight and
opportunity costs remain part of those profiles, so higher-tier equipment can decisively outperform
weak equipment without making every later item identical.
