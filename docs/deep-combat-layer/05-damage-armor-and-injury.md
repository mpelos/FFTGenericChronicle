# Damage, Armor, and Injury

This document owns physical damage dice, DR application, wound conversion, immediate injury effects,
and the boundary with FFT's KO lifecycle. Magical power, delivery, Faith, Shell, elements, and the
point at which magic enters this shared injury pipeline are owned by
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md).

## From ST and weapon to damage dice

Every physical attack selects one ST damage mode:

- thrust for direct thrusting attacks;
- swing for swung cutting or crushing attacks;
- an explicit fixed weapon basis for mechanisms that do not use the user's ST.

```text
BasicDamage     = STDamageTable[ST][mode]
WeaponDamage    = BasicDamage + weapon modifier
DamageExpression = NormalizeDiceAndAdds(WeaponDamage)
```

The result is displayed and rolled as `Xd6+Y`. Large heroic weapon modifiers are permitted; the
structure follows GURPS while item progression follows FFT's equipment cadence.

Weapons do not increase ST to achieve this damage. Their contribution is the weapon modifier,
damage type, armor divisor, and other profile properties.

## Damage resolution

After a successful attack that is not defended:

```text
MinimumBasicDamage = 0 for crushing; 1 for cutting, impaling, and piercing
RolledDamage       = max(MinimumBasicDamage, roll(Xd6 + Y))
ApplicableDR      = selectDR(attack location, target equipment)
EffectiveDR       = floor(ApplicableDR / armorDivisor)
PenetratingDamage = max(0, RolledDamage - EffectiveDR)
Injury             = floor(PenetratingDamage * woundMultiplier)
```

The target loses Injury HP. DR is subtracted before the wound multiplier.

Manifested magical damage uses the same DR and injury operations after its delivery succeeds.
Internal or spiritual magic may skip DR only when its delivery profile declares that rule.

## DR ownership

Each defensive item has one physical DR value. DR is item-specific, not damage-type-specific.
Location selection and the combined-DR rule are defined by
[Facing, Reach, and Targeting](04-facing-reach-and-targeting.md).

Shields do not normally add passive DR. They protect through Block and Defense Bonus. An accessory
or special item contributes DR only when its own profile explicitly says so.

## Armor divisors

Armor divisor belongs to the attack profile:

```text
divisor 1 -> full DR
divisor 2 -> half DR, rounded down
higher divisor -> correspondingly smaller DR
```

Effective DR cannot fall below zero. The rounding direction is part of the rule because an odd DR
value must not produce different penetration across implementations.

Penetrating weapons therefore defeat equipment without requiring a separate DR value for piercing,
crushing, or cutting damage.

## Damage types and wound multipliers

Damage type changes injury after penetration:

| Type | Reference multiplier |
| --- | ---: |
| Crushing | x1 |
| Cutting | x1.5 |
| Impaling | x2 |
| Small piercing | x0.5 |
| Piercing | x1 |
| Large piercing | x1.5 |
| Huge piercing | x2 |

Weapon balance considers the complete package: thrust or swing basis, modifier, multiplier, armor
divisor, Reach, Accuracy, readiness, handedness, Weight, and skill Difficulty. A larger wound
multiplier is not evaluated in isolation.

## Critical hits

A critical hit bypasses active defense. It rolls the weapon's normal damage expression unless that
weapon or ability explicitly defines a visible critical effect.

## Shock

Injury applies short-lived Shock through the formula, duration, stacking, and numeric Doom-icon
presentation owned by
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md#shock).

## Major Wounds

A single injury greater than half of MaxHP is a Major Wound:

```text
if Injury > MaxHP / 2:
    roll 3d6 against effective HT
```

Success avoids the physical collapse. Failure applies Stun and Knockdown according to
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md). A
location-specific skill may modify this HT check.

## Zero HP and death

The DCL does not use negative-HP consciousness rolls or GURPS death thresholds. At 0 HP, native FFT
KO occurs and the existing death countdown, revival, crystal, and treasure rules remain in force.

There is no physical rule that preserves a unit at 1 HP through an HT roll.

## Excluded injury subsystems

Physical damage does not create bleeding, permanent limb crippling, or grappling states. Temporary
loss of Action or Movement belongs to explicit statuses and skills rather than anatomical damage.
