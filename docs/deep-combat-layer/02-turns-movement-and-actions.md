# Turns, Movement, and Actions

This document owns physical turn cadence and the shared Movement-plus-Action economy.

## Initiative through initial CT

Speed determines the unit's starting position in the CT timeline:

```text
InitialCT = InitiativeSeed(BasicSpeed)
```

`InitiativeSeed` is strictly monotonic: a higher Basic Speed always produces at least as much
starting CT. Fractional Basic Speed is retained for tie-breaking even when CT storage is integral.

After initialization, every unit receives the same CT gain per global tick:

```text
CT += GlobalCTGain
```

Speed never changes `GlobalCTGain`. A fast unit acts earlier, not more often. When a unit reaches
the action threshold, resolving its turn subtracts the threshold rather than assigning a
Speed-dependent reset:

```text
CT -= TurnThreshold
```

Equal gain and equal cycle length preserve the initial phase ordering unless an explicit CT effect
changes it. Ties use unrounded Basic Speed, then DX, then a stable unit index.

Timed magical effects may consume the global CT clock without restoring Speed-based turn
frequency. Their timing rules are owned by
[Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md) and
[Magic Effects and Persistence](14-magic-effects-and-persistence.md).

## Turn resources

A normal turn grants:

```text
one Movement resource
one Action resource
```

Movement and Action are separate. A normal Attack is compatible with full Movement and receives no
skill penalty, skill cap, or defense loss for moving. The DCL therefore does not expose a separate
GURPS `Move and Attack` maneuver.

The unit may use Movement and Action in the order permitted by the FFT turn interface. A rule that
consumes both resources says so explicitly.

## Movement

Movement allows travel up to Effective Move subject to terrain, occupancy, Jump, and path legality.
Movement does not itself change Weapon Skill, Shield Skill, Parry, Block, or Dodge.

Because Attack is compatible with Movement:

```text
melee threat range = EffectiveMove + weapon Reach
ranged threat range = EffectiveMove + legal projectile range
```

Leaving an enemy's adjacency does not cause a universal free attack. A reaction, Wait-like ability,
or other explicit rule may create such an attack.

## Attack

Attack consumes the Action resource. It may target through the equipped weapon, unarmed skill, or
an ability that declares itself a physical attack. Movement before the attack does not apply the
GURPS `-4` Move-and-Attack penalty or the effective-skill-9 cap.

An Attack does not normally remove active defenses. A maneuver or weapon property that sacrifices
defense must declare that consequence.

## Cast

Cast consumes the Action resource. The unit may use Movement before starting the cast or start the
cast and then use any unspent Movement. All other casting, concentration, delayed-action,
resource-commitment, and unit/tile target-tracking rules are owned by
[Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md); this file owns only the
shared Movement-plus-Action budget.

## Ready

Ready is an Action used only by equipment with an explicit readiness property.

```text
Attack with UnreadyAfterAttack weapon -> weapon becomes Unready
Ready                               -> weapon becomes Ready
```

An Unready weapon cannot attack or parry. Ready consumes Action but not Movement, allowing the unit
to reposition while preparing the weapon.

`Unbalanced` is a separate property. An Unbalanced weapon remains Ready, but after it attacks it
cannot Parry until the wielder's next turn. This supplies a defensive cost without imposing an
every-other-turn attack cadence.

Both states must be visible to the player.

## Aim

Aim is the ranged preparation Action defined in [Ranged Combat](07-ranged-combat.md). It grants the selected
weapon's Accuracy against one tracked target. Aim restricts Movement until the aimed shot is fired;
movement before the shot cancels the accumulated aim.

## Stand Up

Stand Up removes Prone and consumes both resources:

```text
Action = consumed
Movement = consumed
```

The unit ends the turn standing and uses normal standing defenses afterward. Prone and its
penalties are owned by [Status Resistance and Posture](08-status-resistance-and-posture.md).

## Maneuver ownership

Only maneuvers defined by the DCL acquire universal rules. GURPS names such as All-Out Attack,
All-Out Defense, Evaluate, Feint, Wait, and Do Nothing do not silently inherit their tabletop
behavior. Casting uses the explicit DCL concentration contract rather than importing the complete
GURPS Concentrate maneuver. A job ability or later global rule may implement another maneuver while
obeying the Movement-plus-Action economy.
