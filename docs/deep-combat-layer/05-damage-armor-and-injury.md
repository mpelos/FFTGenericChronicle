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

### Canonical ST damage table

The DCL uses the literal GURPS 4e Basic Set thrust/swing table. An ST-based damage lookup requires
integer `ST >= 1`; an invalid lower value fails validation rather than selecting an invented row.

| ST | Thrust | Swing | ST | Thrust | Swing |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1–2 | 1d6-6 | 1d6-5 | 21–22 | 2d6 | 4d6-1 / 4d6 |
| 3–4 | 1d6-5 | 1d6-4 | 23–24 | 2d6+1 | 4d6+1 / 4d6+2 |
| 5–6 | 1d6-4 | 1d6-3 | 25–26 | 2d6+2 | 5d6-1 / 5d6 |
| 7–8 | 1d6-3 | 1d6-2 | 27–28 | 3d6-1 | 5d6+1 |
| 9 | 1d6-2 | 1d6-1 | 29–30 | 3d6 | 5d6+2 |
| 10 | 1d6-2 | 1d6 | 31–32 | 3d6+1 | 6d6-1 |
| 11 | 1d6-1 | 1d6+1 | 33–34 | 3d6+2 | 6d6 |
| 12 | 1d6-1 | 1d6+2 | 35–36 | 4d6-1 | 6d6+1 |
| 13 | 1d6 | 2d6-1 | 37–38 | 4d6 | 6d6+2 |
| 14 | 1d6 | 2d6 | 39–44 | 4d6+1 | 7d6-1 |
| 15 | 1d6+1 | 2d6+1 | 45–49 | 5d6 | 7d6+1 |
| 16 | 1d6+1 | 2d6+2 | 50–54 | 5d6+2 | 8d6-1 |
| 17 | 1d6+2 | 3d6-1 | 55–59 | 6d6 | 8d6+1 |
| 18 | 1d6+2 | 3d6 | 60–64 | 7d6-1 | 9d6 |
| 19 | 2d6-1 | 3d6+1 | 65–69 | 7d6+1 | 9d6+2 |
| 20 | 2d6-1 | 3d6+2 | 70–74 | 8d6 | 10d6 |
|  |  |  | 75–79 | 8d6+2 | 10d6+2 |
|  |  |  | 80–84 | 9d6 | 11d6 |
|  |  |  | 85–89 | 9d6+2 | 11d6+2 |
|  |  |  | 90–94 | 10d6 | 12d6 |
|  |  |  | 95–99 | 10d6+2 | 12d6+2 |
|  |  |  | 100–109 | 11d6 | 13d6 |

Where two expressions appear in one ranged row, the first belongs to the first ST and the second to
the second ST. Above ST 100:

```text
extraDice = floor((ST - 100) / 10)
Thrust    = 11d6 + extraDice d6
Swing     = 13d6 + extraDice d6
```

Thus ST 109 retains `11d6/13d6`, ST 110 becomes `12d6/14d6`, and the characteristics remain
open-ended. Weapon modifiers are applied only after selecting this row.

### Dice-and-add normalization

`BasicDamage` is a pair `(X, Y)` representing `Xd6+Y`. The weapon modifier is an integer added to
`Y`. Positive adds then use the GURPS dice-plus-add conversion:

```text
NormalizeDiceAndAdds(X, Y):
    while Y >= 7:
        X = X + 2
        Y = Y - 7

    while Y >= 4:
        X = X + 1
        Y = Y - 4

    return (X, Y)
```

Examples:

```text
1d6+4  -> 2d6
1d6+6  -> 2d6+2
1d6+7  -> 3d6
1d6+13 -> 4d6+2
5d6+13 -> 8d6+2
```

Negative adds remain negative and never remove dice. Every ST-table or fixed-basis input contains at
least one d6; the normalizer therefore always returns `X >= 1`. The display omits `+0` but retains
the same logical pair. Minimum basic damage is applied only after rolling the normalized expression,
not while composing it.

## Damage resolution

After a successful attack that is not defended:

```text
MinimumBasicDamage = 0 for crushing; 1 for cutting, impaling, and piercing
RolledDamage       = max(MinimumBasicDamage, roll(Xd6 + Y))
ApplicableDR      = selectDR(attack location, target equipment)
DivisorDR         = 1 if armorDivisor < 1 and ApplicableDR == 0
                    otherwise ApplicableDR
EffectiveDR       = floor(DivisorDR / armorDivisor)
PenetratingDamage = max(0, RolledDamage - EffectiveDR)
Injury             = 0 if PenetratingDamage == 0
                     otherwise max(1, floor(PenetratingDamage * woundMultiplier))
```

The target loses Injury HP. DR is subtracted before the wound multiplier.

Direct HP payment is not Injury. Costs such as magical overcasting debit CurrentHP through their
own resource-settlement rule and do not enter this damage pipeline.

If `RolledDamage <= EffectiveDR`, both Penetrating Damage and Injury are zero. The DCL does not
apply GURPS blunt trauma through flexible armor: blocked damage never leaks HP merely because of
damage type or armor construction. An explicit ability may deal a separate effect after a blocked
hit, but that effect is not a universal armor rule.

Any positive Penetrating Damage causes at least one Injury after the wound multiplier. This matters
for multipliers below one: one point of small-piercing Penetrating Damage still causes one Injury
rather than flooring to zero.

Manifested magical damage uses the same DR and injury operations after its delivery succeeds.
Internal or spiritual magic may skip DR only when its delivery profile declares that rule.

## DR ownership

Each defensive item has one physical DR value. DR is item-specific, not damage-type-specific.
Location selection and the combined-DR rule are defined by
[Facing, Reach, and Targeting](04-facing-reach-and-targeting.md).

Shields do not normally add passive DR. They protect through Block and Defense Bonus. An accessory
or special item contributes DR only when its own profile explicitly says so.

## Armor divisors

Armor divisor belongs to the attack profile and is an exact positive rational value. The DCL does
not restrict it to an enumerated list: any authored value greater than zero is valid. Decimal
authoring is parsed as an exact rational value under the
[Numeric Resolution Contract](17-numeric-resolution-contract.md), never as an imprecise binary
floating-point value.

```text
armorDivisor > 0
EffectiveDR = floor(ApplicableDR / armorDivisor)
```

This retains the complete GURPS design space rather than limiting skill authors to a short list:

```text
divisor 0.5 -> double DR
divisor 0.2 -> five times DR
divisor 0.1 -> ten times DR
divisor 1   -> full DR
divisor 2   -> half DR
divisor 3   -> one-third DR
divisor 5   -> one-fifth DR
divisor 10  -> one-tenth DR
divisor 100 -> one-hundredth DR
```

Values between or beyond these examples remain legal. Fractional divisors model attacks with poor
armor penetration; divisors greater than one model attacks that defeat armor. This freedom is part
of skill and equipment authorship, and its combat value is charged to the corresponding capacity
budget rather than controlled by a schema whitelist.

Effective DR cannot fall below zero. The explicit floor is part of the rule because a DR value that
does not divide evenly must not produce different penetration across implementations. Zero,
negative, non-finite, missing, or unparsable divisors fail validation; they never fall back to `1`.

As in GURPS, a fractional divisor treats zero Applicable DR as DR 1 before division:

```text
DivisorDR = 1 if armorDivisor < 1 and ApplicableDR == 0
            otherwise ApplicableDR
```

Consequently an unarmored target has Effective DR 2 against divisor `0.5`, DR 5 against `0.2`, and
DR 10 against `0.1`. This minimum exists only for a numeric divisor below one; divisor `1` or
greater uses actual zero DR, while `IgnoreDR` bypasses the operation entirely.

Every resolved damage component uses exactly one armor interaction. The weapon supplies its armor
divisor by default. When the executing skill declares an armor divisor, the skill's value replaces
the weapon's value. The two values are never added or multiplied:

```text
ResolvedArmorDivisor = skill.ArmorDivisor if declared
                       otherwise weapon.ArmorDivisor
```

An attack that conceptually ignores armor declares `IgnoreDR` instead of emulating that property
with an extremely large divisor. A skill's declared `IgnoreDR` likewise takes precedence over the
weapon divisor. `IgnoreDR` sets Effective DR to zero and is a distinct armor interaction, not a
numeric armor divisor.

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
weapon or ability explicitly defines a visible critical effect. It is also the only universal
entry point to the capped GURPS knockback calculation defined below.

## Shock

Injury applies short-lived Shock through the formula, duration, stacking, and numeric Doom-icon
presentation owned by
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md#shock).

## Major Wounds

A single injury greater than half of MaxHP is a Major Wound only when the target remains above zero
HP after that injury:

```text
RemainingHP = max(0, HPBeforeInjury - Injury)

if RemainingHP > 0 and Injury > MaxHP / 2:
    roll 3d6 against effective HT
```

Success avoids the physical collapse. Failure applies Stun and Knockdown according to
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md). A
location-specific skill may modify this HT check.

If RemainingHP is zero, native FFT KO begins immediately and no Major-Wound HT roll occurs. Head
targeting has no universal HT penalty; only the executing skill may declare a modifier to this
check.

## Knockback boundary

The DCL uses GURPS's damage-versus-ST knockback calculation only on a critical success. A normal
success never checks universal knockback regardless of damage magnitude. A surviving target of a
critical success is eligible only when:

- the damage type is crushing; or
- the damage type is cutting and `PenetratingDamage == 0`.

The check uses basic Rolled Damage before subtracting Effective DR and before applying the wound
multiplier:

```text
KnockbackUnit = 1 if TargetST <= 3
                otherwise TargetST - 2

GurpsKnockbackTiles = floor(RolledDamage / KnockbackUnit)
DclKnockbackTiles   = min(1, GurpsKnockbackTiles)
```

Thus a critical crushing hit rolling at least `TargetST - 2` against ST greater than 3 moves the
target one tile; additional multiples never increase displacement beyond one. A qualifying roll of
zero moves no one. Impaling and piercing damage never trigger universal knockback. A critical hit
that reduces the target to 0 HP enters native KO immediately and does not displace the fallen unit.

The DCL omits GURPS's post-knockback balance roll. A displaced target makes no DX check and
knockback never applies Knocked Down. Tile legality, displacement over an edge, landing, and fall
damage retain native FFT behavior; such vertical movement is not the DCL Knocked Down posture.

Forced movement exists only when the resolved skill or attack property declares it explicitly. It
is a separate effect component rather than a consequence inferred from damage magnitude. The
authored component supplies its direction, distance, and interaction with invalid destinations,
occupied tiles, height changes, falls, and action transaction ordering. Its tactical value is
charged to the ability's capacity budget.

## Zero HP and death

The DCL does not use negative-HP consciousness rolls or GURPS death thresholds. At 0 HP, native FFT
KO occurs and the existing death countdown, revival, crystal, and treasure rules remain in force.

There is no physical rule that preserves a unit at 1 HP through an HT roll.

## Excluded injury subsystems

Physical damage does not create blunt trauma through fully protective armor, knockback on an
ordinary success, bleeding, permanent limb crippling, or grappling states. Temporary loss of Action
or Movement belongs to explicit statuses and skills; forced displacement belongs to an explicit
effect or the capped critical-only rule above.

The DCL also omits GURPS's automatic self-injury for striking armored targets with an unarmed body
part. Target DR never reflects damage into the attacker merely because the attack is unarmed. A
skill may declare recoil Injury, an HP cost, or another self-effect explicitly; that authored effect
does not derive from the target's DR unless its own formula says so.
