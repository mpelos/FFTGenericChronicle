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

The declaration stores the caster identity, selected spell and authored profile, target mode,
tracked unit or fixed tile, declaration tile and passed legality checks, `ApprovedHPCap`, CastCT,
and outer ActionInstance identity. It does not freeze the caster's or target's combat statistics.
Current values are read when the cast resolves, as defined by
[Action Transactions and Reactions](18-action-transactions-and-reactions.md#declaration-and-resolution-state).
Delivery and effect resolution are owned by
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md).

Immediately before a charged cast rolls its casting gate, it recomputes the full-cost MP/HP split
from current pools. If required HP exceeds ApprovedHPCap or current HP, the spell fails for
insufficient resources without silently increasing HP substitution. MP restored after declaration
may reduce the HP portion; it never forces the caster to pay HP that is no longer necessary.

This outcome is `ResourceFailure`: it performs no SpellScore roll, applies no effect, consumes no
MP or HP, clears Charging, and leaves the already spent Action and elapsed CastCT spent. It is not
an ordinary casting failure or an interruption-cost settlement.

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

Each action declares nonnegative BaseCastCT. Applicable integer modifiers combine additively at
declaration:

```text
CastCT = max(0, BaseCastCT + sum(applicable CastCTModifier values))

CastCT = 0 -> resolve during the declaration turn
CastCT > 0 -> enter Charging and resolve when the cast timer completes
```

CastCT advances on the global CT clock. It does not use the caster's Speed as a personal growth rate,
and ordinary Haste or Slow does not modify CastCT. A spell or property that changes CastCT must name
that interaction explicitly. CastCT is stored in the ActionInstance and never rescaled after
declaration when equipment, states, Speed, Haste, or Slow change. Missing, non-integral, non-finite,
or unparsable modifiers fail validation rather than changing timer precision.

Starting a delayed cast consumes the Action immediately. While Charging, the unit cannot declare a
second Action. It retains ordinary active defenses; Charging does not automatically remove Dodge,
Parry, or Block.

If the caster was Invisible, declaration is the first Action and ends Invisibility through FFT's
native status lifecycle. A `CastCT > 0` delivery therefore resolves from a visible source and grants
its ordinary active defense. A `CastCT = 0` delivery remains inside the immediate revealing Action
and uses the no-defense rule owned by
[Facing, Reach, and Targeting](04-facing-reach-and-targeting.md#first-action-from-invisibility).
Interruption or cancellation does not restore the expired status.

## Concentration and interruption

Concentration is an intentional DCL extension to FFT's charging lifecycle. It gives hostile Injury
and forced movement a way to threaten a delayed cast without treating every successful hit as an
automatic cancellation. Effects that make continued casting impossible still cancel directly.

Injury, forced movement, or another explicitly disruptive event causes a concentration check when
the caster remains capable of continuing:

```text
ConcentrationScore = Will
                     - 3
                     + sum(applicable ConcentrationModifier values)
                     - explicit state penalties
```

The check succeeds on `3d6 <= ConcentrationScore`. Injury magnitude does not replace or scale the
fixed `-3`, and Shock is not subtracted again. Larger injuries already threaten concentration
through Major Wound, Stun, Knocked Down, and KO. Voluntary Movement does not trigger the check.
Stun, KO, Silence applied to a Verbal spell, and Don't Act cancel a cast directly when their
definitions prevent remaining concentration; no concentration roll precedes or rescues that
cancellation. Voluntary cancellation is legal. Resource settlement is defined in
[Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md#cost-commitment). FFT
consumes MP or HP only when the cast resolves, so interruption or voluntary cancellation before
resolution debits neither pool. The declaration Action and all elapsed CastCT remain spent.

### Disruption granularity

Concentration is tested per resolved Strike, not per outer ActionInstance and not in the Reaction
window. After a Strike commits its target-local result for a caster that is still Charging:

1. KO, Stun, Knocked Down, or another result that prevents continued casting clears Charging
   directly and creates no concentration roll.
2. Otherwise, the Strike creates one concentration incident when it caused `Injury > 0` or a
   nonzero forced displacement.
3. One incident makes exactly one concentration roll, even when the same Strike caused both Injury
   and displacement.
4. A hit that landed but produced zero Injury after DR and no displacement creates no incident.

Each damaging Strike of a multi-hit action is evaluated independently and can therefore require a
separate roll. Failure clears Charging immediately. It does not cancel the attacking action, skip
its later Strikes, or create an early Reaction window. The remaining combo continues under its
ordinary FFT lifecycle.

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
AllegiancePolicy: Everyone | AlliesOnly | EnemiesOnly | CasterSide | Explicit
```

Indiscriminate areas are the baseline. Selective areas pay for that advantage through MP, CastCT,
area, magnitude, access, or another visible tradeoff. Summons preserve their premium selective-area
identity as defined in [Magic Effects and Persistence](14-magic-effects-and-persistence.md#summons).

Center behavior and target avoidance are separate axes. An area declares one delivery gate:

```text
AreaDeliveryGate: None | Dodge | QuickContest
```

`None` grants no delivery-avoidance roll. `Dodge` grants each affected target one Dodge roll for
each Strike delivered to that target; it never silently enables Parry, Block, or movement to another
tile. A critical casting success bypasses this Dodge under the ordinary External Projectile rule.
`QuickContest` reuses the action's one caster roll against one independent authored resistance roll
per target; ties resist, and a multi-hit result shares that contest for all hits against the target.

This gate controls delivery only. A landed carrier may still contain a status rider with its own one
target-resistance roll. CenterMode TrackedUnit does not imply QuickContest, and CenterMode FixedTile
does not imply an unavoidable effect. Area size and center tracking never infer the gate.

At resolution, final membership and every target's starting state are captured in one logical area
snapshot. Each target still receives its own avoidance, resistance, DR, Faith, affinity, damage,
healing, and status result. Enumeration order cannot change another target's result. The plan/commit
contract is owned by
[Action Transactions and Reactions](18-action-transactions-and-reactions.md#target-batch-snapshot).

## Zodiac compatibility

FFT's native caster–target relationship supplies one compatibility category. A Zodiac-sensitive
action converts it to the 3d6 score scale:

| Native compatibility | TargetSpellScore modifier |
| --- | ---: |
| Best | `+2` |
| Good | `+1` |
| Neutral | `0` |
| Bad | `-1` |
| Worst | `-2` |

Compatibility modifies TargetSpellScore only. It does not also multiply damage, healing, duration,
Faith, or resistance. Applying it once avoids turning one roster relationship into several stacked
bonuses.

One ActionInstance still makes one casting draw. The resolver first classifies it against
BaseSpellScore. A base failure ends delivery and selects the failure settlement owned by
[Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md#cost-commitment). On a base
success, the same draw is classified independently against each target's TargetSpellScore. It can
therefore miss one target, succeed against another, and enter the target critical band against a
third when their scores differ. A favorable modifier cannot rescue a failed base cast.

Internal Direct uses each target-specific score to compute the caster margin against that target's
resistance roll. Forecast enumerates these correlated outcomes rather than pretending the caster
rerolls for each unit. Resource settlement depends only on the base classification, never on how
many targets pass the second classification.
