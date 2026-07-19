# Equipment and Encumbrance

This document owns the physical and supernatural rules vocabulary for every equipment slot and the
conversion from item Weight to encumbrance.

No new item entries are required. Existing FFT equipment receives DCL metadata and formulas.
Weapons, shields, armor, headgear, and accessories have no durability pool and cannot be destroyed
by ordinary combat resolution. Equipment breakage and persistent repair are outside the DCL.

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
| ParryLoad default | A weapon strike uses Weight when testing whether the attack is too heavy to Parry. |
| Parry modifier | Adjusts derived Parry. |
| Balance | Balanced or Unbalanced. |
| Readiness | Always Ready or Unready After Attack. |
| Ranged properties | Accuracy, maximum range, and trajectory when applicable. |
| Special properties | Element, status, innate ability, or other explicit behavior. |

There is no per-weapon Minimum ST. Job equipment access, aptitude Tier, ST distribution, Weight,
handedness, readiness, and the rest of the weapon profile provide the restrictions.

Weapon Weight does not create a breakage roll when the weapon Parries a heavier attack. An explicit
skill may apply Unready, Weapon Bound, or another temporary state, but it never mutates or destroys
the inventory item. Weight still supplies the incoming strike's default ParryLoad; the legality
formula is owned by
[Skills and Active Defenses](03-skills-and-active-defenses.md#heavy-attack-parry-limit).

A weapon that also serves as a magical focus may declare a small, coherent subset of:

- SpellSkillModifier;
- FocusDamageModifier;
- FocusHealingModifier;
- ElementBoostMultiplier;
- ConcentrationModifier;
- CastCTModifier;
- MPCostMultiplier;
- tradition compatibility.

One item does not receive every focus axis merely because it is associated with a caster. Each
property competes with the item's physical profile and other equipment opportunities.

## Hands and off-hand

- A one-handed weapon leaves the off-hand available for a shield or a legal second weapon.
- A two-handed weapon prevents shield use and dual wield.
- Dual wield includes `DualWeaponTraining`: the main-hand strike uses full Weapon Skill and the
  off-hand strike uses its own Weapon Skill at `-4`; no separate OffHand Training exists.
- Dual wield tracks weapon family, damage, attack, Parry usage, readiness, and balance separately
  per hand.
- A shield requires an explicit shield-capable job and underlying Shield aptitude.

The complete skill and Parry calculation is owned by
[Skills and Active Defenses](03-skills-and-active-defenses.md#dual-wield-and-off-hand-parry). The two
strikes remain one outer Action as defined by
[Action Transactions and Reactions](18-action-transactions-and-reactions.md#strike-resolution).

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

Armor does not require a Flexible/Rigid property. Fully stopped damage produces zero Injury; the
omitted GURPS blunt-trauma subsystem is owned by
[Damage, Armor, and Injury](05-damage-armor-and-injury.md#damage-resolution).

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
BasicLift = ST * ST / 5
Load      = sum(Weight of equipped items)
ratio     = Load / BasicLift
```

Basic Lift, Weight, Load, and the ratio comparison retain exact rational precision. Every equippable
item declares a nonnegative Weight on this scale; missing, negative, non-finite, or unparsable
Weight fails validation. There is no `LiftScale` constant: item authors calibrate Weight against the
quadratic ST curve itself.

The ratio selects the GURPS encumbrance band:

| Encumbrance | Load interval | Move multiplier | Dodge penalty magnitude |
| --- | ---: | ---: | ---: |
| None | `Load <= 1 x Basic Lift` | x1.0 | 0 |
| Light | `1 x Basic Lift < Load <= 2 x Basic Lift` | x0.8 | 1 |
| Medium | `2 x Basic Lift < Load <= 3 x Basic Lift` | x0.6 | 2 |
| Heavy | `3 x Basic Lift < Load <= 6 x Basic Lift` | x0.4 | 3 |
| Extra-heavy | `Load > 6 x Basic Lift` | x0.2 | 4 |

```text
EffectiveMove  = max(1, floor(BasicMove * encumbranceMoveMultiplier))
EffectiveJump  = max(1, floor(BaseJump * encumbranceMoveMultiplier))
EffectiveDodge = Dodge before encumbrance - encumbranceDodgePenalty
```

Move and Jump retain integer tile and elevation units after the explicit floor. Their minimum of one
applies to every load within the defined encumbrance bands.

These are the encumbrance results, not necessarily the final combat values. Critical low HP halves
fully modified Move and Dodge after this stage and leaves Jump unchanged, as defined by
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md#critical-low-hp).

Extra-heavy is the final open-ended band. Load has no maximum ratio: exceeding `10 x Basic Lift`
does not create another state, prohibit an equipment set, or reduce Move or Jump to zero. Weight
changes combat mobility and Dodge but never changes whether an otherwise job-legal item may be
equipped.

Encumbrance does not reduce CT gain, initiative, Weapon Skill, Shield Skill, or damage directly.
ST already pays for greater load tolerance through Basic Lift. For reference, ST 10 has Basic Lift
20, while ST 20 has Basic Lift 80.

## Equipment progression

FFT's frequent equipment replacement remains meaningful. Later weapons may gain larger damage
modifiers, Accuracy, armor divisors, or special properties; later armor may gain more DR. Weight and
opportunity costs remain part of those profiles, so higher-tier equipment can decisively outperform
weak equipment without making every later item identical.
