# Magic Resolution and Defenses

This document owns magical delivery, active-defense legality, Magic Resistance, magical damage,
DR interaction, elements, Shell, Reflect, criticals, and multi-target resolution.

## Delivery classes

The delivery class determines the gates between a successful declaration and the effect:

| Delivery | Casting gate | Target gate | DR |
| --- | --- | --- | --- |
| External Projectile | SpellScore roll | legal active defense | normally applies |
| Internal Direct | Quick Contest | HT, Will, Spiritual Resistance, or authored resistance | normally ignored |
| Beneficial | SpellScore roll | willing target does not resist | ignored unless stated |
| Touch | SpellScore attack | Dodge or Parry as authored | effect-defined |
| Area | one casting roll | per-target avoidance policy | effect-defined |
| Rider | carrier must hit | one resistance roll for the rider | carrier/effect-defined |

The DCL collapses GURPS's separate creation and DX-based Innate Attack rolls for a Missile Spell into
one IQ-based SpellScore roll. External magic already faces active defense and DR; a second attack roll
would add another failure gate without adding a useful FFT decision.

## External Projectile

```text
3d6 <= SpellScore
-> critical success bypasses active defense
-> otherwise resolver selects the best legal Dodge or Block
-> on an undefended hit, applicable DR reduces damage
```

External Projectiles are normally Dodgeable and may be Blockable. They are not normally Parryable.
The spell profile must declare Block legality, including whether ordinary shields and Defense Bonus
apply. The spell uses FFT unit tracking rather than physical line of sight or projectile collision.

External spells may receive larger raw damage because they face casting failure, active defense, and
DR.

## Internal Direct

An Internal Direct effect acts on the target without a projectile:

```text
Quick Contest:
caster succeeds and records margin using SpellScore
vs
target margin using the declared resistance
```

There is no Dodge, Block, or Parry. Internal damage normally ignores BodyDR and HeadDR. The target
participates through resistance:

| Effect | Usual resistance |
| --- | --- |
| bodily disruption, poison, internal injury | HT |
| mental coercion or loss of voluntary control | Will |
| spiritual curse or hostile supernatural alteration | SpiritualResistance |
| exceptional effect | value declared by the action |

Internal effects receive lower raw damage or a narrower effect than comparable External
Projectiles because they remove active defense and DR. This is an expected-injury tradeoff, not a
universal fixed dice ratio.

## Magic Resistance and retired magical evasion

There is no universal Magic Evasion percentage. Avoidance belongs to the attack's delivery:

- Dodge avoids an evadeable manifestation;
- Block stops a Blockable manifestation;
- HT resists bodily effects;
- Will resists mental effects;
- Spiritual Resistance contests spiritual effects.

```text
SpiritualResistance = Will
                      + JobMagicResistance
                      + EquipmentMagicResistance
                      + explicit status modifiers
```

Faith does not enter SpellScore or this formula through a coarse five-band modifier. A
Faith-sensitive action uses the continuous magnitude rule below or an explicit effect rule.

Legacy magical-evasion fields are reinterpreted by explicit ownership:

| Legacy field | DCL use |
| --- | --- |
| class magical evasion | JobMagicResistance or explicit Dodge-vs-magic modifier |
| shield magical parry | Block/DB against Blockable spells |
| cloak magical evasion | explicit Dodge-vs-magic or EquipmentMagicResistance |
| accessory magical evasion | explicit modifier named by the accessory |

The same value is never applied as both an active defense and Magic Resistance unless the item
explicitly pays for both properties.

## Final success chance

An External Projectile uses the same two-stage probability shape as a physical ranged attack:

```text
A = probability of SpellScore success
C = probability of critical casting success
D = probability of the selected active defense succeeding

FinalSuccessChance = C + (A - C) * (1 - D)
```

If no defense is legal, `FinalSuccessChance = A`. An Internal Direct action instead enumerates both
3d6 rolls and succeeds only when the caster first succeeds and then wins the Quick Contest; the
target resists on a tie. A Beneficial action against a willing target uses only the SpellScore
probability.

Forecast percentages use exact 3d6 enumeration, including critical and automatic outcomes. They do
not subtract a Magic Evasion percentage from a magical hit percentage.

## Magical power and raw damage

Spell accuracy and spell magnitude are separate:

```text
MagicPower = MagicPowerScale(IQ)
             + SpellPower
             + FocusPowerModifier
             + explicit power modifiers

RawDamage = MagicDamageTable(MagicPower, spellDamageProfile)
```

`SpellScore` decides whether delivery succeeds. `MagicPower` produces the authored `Xd6+Y`. Increasing
training does not silently increase both chance and damage unless a spell explicitly converts margin
of success into magnitude.

## Faith potency and receptivity

Faith uses a continuous factor centered at Faith 50:

```text
FaithFactor(F) = 1 + 0.006 * (F - 50)
```

Reference values are:

| Faith | Factor |
| ---: | ---: |
| 0 | 0.70 |
| 10 | 0.76 |
| 30 | 0.88 |
| 50 | 1.00 |
| 70 | 1.12 |
| 90 | 1.24 |
| 100 | 1.30 |

Every Faith point changes the factor; the interface may summarize it at ten-point landmarks. For an
effect that is sensitive to both caster potency and target receptivity:

```text
FaithMagnitude = FaithFactor(casterFaith) * FaithFactor(targetFaith)
```

Thus two Faith-50 units produce `x1.00`, two Faith-70 units produce `x1.2544`, and two Faith-100
units produce `x1.69`. A spell may use only caster or only target Faith, but must declare that policy.
Faith is applied to magnitude once; it is not also hidden in SpellScore, resistance, and duration.

## DR interaction and damage order

Magic declares one armor policy:

| Policy | Rule |
| --- | --- |
| Manifestation | ordinary location DR applies |
| Armor-dividing | ordinary DR applies after the authored divisor |
| Internal/Spiritual | DR is ignored; resistance normally applies |

Manifestations use the same HeadDR, BodyDR, and combined normal DR as physical damage. There is no
separate magical HeadDR or BodyDR.

The complete order is:

```text
1. roll raw damage dice
2. select location and armor divisor
3. subtract effective DR
4. determine penetrating damage
5. apply damage-type/wound behavior
6. apply elemental affinity
7. apply FaithMagnitude when flagged
8. apply Shell when flagged
9. produce final Injury
```

An Internal effect begins after the DR steps but retains the remaining authored multipliers.

## Elements and Oil

Elemental affinity is evaluated after DR so armor still matters against a manifested element:

| Affinity | Multiplier |
| --- | ---: |
| Absorb | converts eligible injury into healing |
| Null | x0 |
| Halve | x0.5 |
| Normal | x1 |
| Weak | x1.5 |
| Severe weakness | x2 |

Oil is a visible one-shot fire vulnerability. It is consumed only when a fire effect penetrates and
would produce positive injury. A blocked, dodged, resisted, nullified, or fully DR-stopped fire
attack does not consume Oil.

## Shell

Shell reduces final magical injury after DR and the spell's other offensive multipliers:

```text
ShellMultiplier = 0.70
```

Shell does not also improve Magic Resistance. Separating injury mitigation from resistance avoids
granting two defensive gates through one status. An action declares whether it is Shell-sensitive;
MP consumption alone does not determine that interaction.

## Reflect

Reflect is routing, not evasion or resistance:

- only unit-targeted actions marked `Reflectable` are routed;
- fixed-tile and ordinary ground areas are not Reflectable;
- one action may reflect at most once;
- a reflected action cannot create a reflection loop;
- the deterministic default return target is the original caster;
- the forecast shows the resolved route before confirmation.

Reflect changes the recipient, not the original SpellScore, MP cost, CastCT, magnitude, or area
policy. The new recipient applies its own defense, resistance, DR, Faith, affinity, and Shell.

## Criticals

- An External Projectile critical bypasses active defense but not DR.
- An Internal Quick Contest uses margins; an attack-side critical does not create an automatic win
  against every resistance result.
- A beneficial critical may maximize one healing die or increase authored duration, not both by
  default.
- Critical success does not refund MP.
- Critical failure has no universal catastrophe table; a spell may declare a visible consequence.

## Multi-target, area, and multi-hit resolution

One action makes one casting roll. After that:

- avoidance or resistance is resolved per affected target;
- damage or healing is rolled per target;
- a rider makes one resistance roll per target after its carrier hits;
- multi-hit actions share the casting result and roll damage per strike;
- a target receives a defense per strike only when the action explicitly declares that policy.

This avoids rerolling the caster's knowledge for every unit while preserving each defender's own
position, defenses, resistance, DR, Faith, affinity, and statuses.
