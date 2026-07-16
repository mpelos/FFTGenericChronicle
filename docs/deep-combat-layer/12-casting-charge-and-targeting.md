# Casting, Charge, and Magic Targeting

This document owns declaration, CastCT, concentration, movement during casting, unit tracking,
tile targeting, spell range, and the deliberate absence of line-of-sight checks for magic.

## Declaring a cast

A cast consumes the Action resource. At declaration the resolver validates:

- the action is learned and usable by its source;
- Silence and other source-specific prohibitions;
- MP plus any legal overcasting capacity;
- target kind, allegiance policy, range, vertical tolerance, and current target state;
- any item, terrain, or prerequisite required by the action.

The declaration stores the caster, selected spell, target mode, tracked unit or fixed tile, casting
score, reserved resources, CastCT, and relevant effect metadata. Delivery and effect resolution are
owned by [Magic Resolution and Defenses](13-magic-resolution-and-defenses.md).

## Movement before or after casting

Movement and Action remain separate. A caster may:

- move and then declare the cast; or
- declare the cast and then use any unspent Movement.

When the cast is declared before movement, range and vertical legality use the caster's declaration
tile. Moving afterward does not cancel the cast or cause a second range check. Voluntary movement by
the caster does not interrupt concentration.

The usual resource rules still apply. Stand Up consumes both resources, and Don't Move removes the
Movement resource without preventing an otherwise legal cast.

## CastCT and the global clock

Each action declares `CastCT`:

```text
CastCT = 0 -> resolve during the declaration turn
CastCT > 0 -> enter Charging and resolve when the cast timer completes
```

CastCT advances on the global CT clock. It does not use the caster's Speed as a personal growth rate,
and ordinary Haste or Slow does not modify CastCT. A spell or property that changes CastCT must name
that interaction explicitly.

Starting a delayed cast consumes the Action immediately. While Charging, the unit cannot declare a
second Action. It retains ordinary active defenses; Charging does not automatically remove Dodge,
Parry, or Block.

## Concentration and interruption

Injury, forced movement, or another explicitly disruptive event causes a concentration check:

```text
ConcentrationScore = Will
                     + FocusConcentrationModifier
                     - InjuryPenalty
                     - explicit state penalties
```

The check succeeds on `3d6 <= ConcentrationScore`. Voluntary Movement does not trigger it. Stun, KO,
Silence applied to a Verbal spell, and Don't Act cancel a cast when their definitions prevent the
remaining concentration. Voluntary cancellation is legal. Resource settlement is defined in
[Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md#cost-commitment).

## Unit targets follow FFT tracking

A spell declared against a unit tracks that unit until resolution:

- horizontal range and vertical tolerance are checked only at declaration;
- movement by the target does not break the lock;
- movement by the caster does not break the lock;
- moving outside the original range does not create a new check;
- the effect resolves at the target's current tile;
- a unit-centered area resolves around the tracked unit's current tile.

There is no magic line-of-sight check at declaration or resolution. Terrain, intervening units, and
physical cover do not intercept a tracked spell. A legal unit target is reached if the casting and
delivery tests succeed.

Range is binary legality, not an accuracy modifier:

```text
inside authored spell range  -> legal, no range penalty to SpellScore
outside authored spell range -> illegal declaration
```

The physical cover, trajectory, and distance-penalty rules do not apply to spell targeting.

## Fixed tile targets

A tile-targeted action stores the selected tile rather than a unit. Its area remains fixed while the
cast charges. Units may enter or leave before resolution. Fixed zones and delayed ground effects are
normally not Reflectable because no unit-targeted route exists to redirect.

Unit tracking and tile targeting are distinct authored modes. A spell cannot switch between them
after declaration merely because its original target moved or became undesirable.

## Area target policies

Every area action declares both a center mode and an allegiance policy:

```text
CenterMode: TrackedUnit | FixedTile | Caster
TargetPolicy: Everyone | AlliesOnly | EnemiesOnly | CasterSide | Explicit
```

Indiscriminate areas are the baseline. Selective areas pay for that advantage through MP, CastCT,
area, magnitude, access, or another visible tradeoff. Summons preserve their premium selective-area
identity as defined in [Magic Effects and Persistence](14-magic-effects-and-persistence.md#summons).

An area also declares its avoidance policy:

```text
FixedGround | EvadeableArea | TrackedArea | ResistedArea | UnavoidableArea
```

The policy determines whether each affected unit receives an active defense, resistance contest, or
no avoidance roll. Area size alone does not answer that question.

## Zodiac compatibility

Zodiac compatibility remains a moderate modifier to SpellScore when the action declares that it is
Zodiac-sensitive. It does not also multiply damage, healing, and duration. Applying compatibility
once avoids turning a roster relationship into several stacked bonuses on the same spell.
