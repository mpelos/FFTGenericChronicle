# Numeric Resolution Contract

This document owns the numerical primitives shared by every DCL rule: scalar precision, rounding,
clamping, 3d6 success rolls, Quick Contests, percentage rolls, forecast enumeration, and random-draw
ownership. A formula that needs a different rule states the exception at the exact operation where
it occurs.

## Numeric domains

Primary ST, DX, and IQ are integers with minimum one after their complete attribute expression; HT
has the Brave conversion's minimum four. MaxHP and MaxMP have minimum one. Derived roll
scores such as Will, Weapon Skill, SpellScore, Dodge, Parry, Block, and resistance are signed
integers after all modifiers are applied. They have no universal gameplay cap. Values outside the
usual 3d6 band remain useful for absorbing penalties or overcoming bonuses; the automatic roll
outcomes below prevent literal certainty.

Basic Speed and other explicitly fractional characteristics use fixed-point arithmetic. Decimal
constants in design formulas are exact rational values, not binary floating-point approximations.
Persistent character growth uses signed 64-bit micro-units at scale `1,000,000`, as owned by the
growth schema. Runtime storage types must be wide enough to evaluate the full expression before the
final field clamp; intermediate overflow never wraps.

## Shared operations

```text
floor(x)               greatest integer <= x
ceil(x)                least integer >= x
roundNearest(x)        nearest integer; an exact half rounds away from zero
clamp(min, max, x)     min(max(x, min), max)
```

Integer division is never used as an implicit rounding rule. A formula writes `floor`, `ceil`, or
`roundNearest` at the boundary where conversion to an integer occurs. This matters for negative
modifiers: `floor(-0.25) = -1`, while truncation toward zero would incorrectly produce `0`.

Evaluation retains exact fixed-point or rational precision until a named rounding boundary. It does
not round each term separately. Unless an owning formula says otherwise, operations occur in the
written order, and a clamp occurs only after the value it encloses has been calculated.

Examples:

```text
roundNearest( 2.5) =  3
roundNearest(-2.5) = -3
floor(7 / 2)       =  3
ceil(7 / 2)        =  4
clamp(0, 100, 112) = 100
```

Gameplay caps and storage caps are different concepts. A gameplay formula names its own cap, such
as a probability's `0..100` range. Serialization clamps only when committing to a proven engine
field and must reject an out-of-domain authored value rather than silently changing game balance.

## Universal 3d6 success roll

A 3d6 roll is the sum of three independent fair six-sided dice. It has 216 equiprobable ordered
outcomes. A standard success roll against `EffectiveScore` is classified in this order:

1. natural 3 or 4 succeeds regardless of EffectiveScore;
2. natural 17 or 18 fails regardless of EffectiveScore;
3. otherwise the roll succeeds when `roll <= EffectiveScore`.

The universal automatic bands apply to attack rolls, active defenses, resistance rolls,
concentration, recovery checks, and any other rule that says it makes a standard success roll. A
score of 18 is therefore excellent but not certain, and a score below 3 retains the natural 3–4
chance.

### Critical classification

An attack or casting roll is a critical success when:

- the roll is 3 or 4;
- the roll is 5 and EffectiveScore is at least 15; or
- the roll is 6 and EffectiveScore is at least 16.

It is a critical failure when:

- the roll is 18;
- the roll is 17 and EffectiveScore is 15 or less; or
- the roll exceeds EffectiveScore by at least 10.

The owning action defines the consequence. Physical and External Projectile critical successes
bypass active defense; they do not automatically bypass DR. A critical failure is ordinarily a
miss unless the action declares a visible additional consequence. Standard resistance, recovery,
and active-defense rolls use the automatic success/failure bands but do not create a separate
critical-effect table.

## Quick Contests

A DCL hostile Quick Contest rolls 3d6 once for the acting score and once for the declared target
resistance. Each roll first uses the universal success classification.

```text
actingMargin = actingScore - actingRoll
targetMargin = targetScore - targetRoll
```

The hostile effect succeeds only when:

1. the acting roll succeeds; and
2. the target roll fails, or both rolls succeed and `actingMargin > targetMargin`.

The target therefore resists on an equal margin. If the acting roll fails, the effect fails even
when the target also fails. An automatic failure is still a failure even when a very high score
would make its arithmetic margin positive. Critical classification does not replace the margin
comparison for Internal Direct magic; its owning effect may add a declared critical consequence
after the contest.

Forecasting a Quick Contest enumerates all `216 * 216 = 46,656` ordered roll pairs.

When one ActionInstance contests multiple targets, the acting roll is one shared execution draw and
each target receives one independent target roll. Forecasting retains that correlation for joint
outcomes rather than pretending the acting roll was independently rerolled. Internal Direct
multi-hit magic also shares one acting/target roll pair for all Strikes against that target.

## Percentage rolls

A rule that explicitly uses a percentage draws one uniform integer in `0..99` and succeeds when:

```text
roll < clamp(0, 100, chancePercent)
```

Thus 0 never succeeds and 100 always succeeds. Percentage mechanics do not inherit the 3d6
automatic bands. Brave is not a universal percentage mechanic; Brave-to-HT uses the separate
attribute conversion and remains open-ended.

## Exact forecast probabilities

Forecasts enumerate the same outcome classifier used by execution:

- one standard success roll enumerates 216 ordered outcomes;
- one shared casting draw classified against BaseSpellScore and multiple TargetSpellScores still
  enumerates 216 correlated caster outcomes, not `216` independently per target;
- attack plus active defense enumerates 46,656 ordered pairs, while critical attacks bypass the
  defense branch;
- a hostile Quick Contest enumerates 46,656 ordered pairs;
- independent per-target or per-strike rolls multiply the corresponding outcome spaces.

The UI rounds only the displayed percentage. Internal AI and comparison logic retains an exact
fraction or a documented fixed-point probability. A forecast never uses `5% * score`, never
subtracts an evasion percentage, and never samples RNG to approximate an exact finite space.

When a whole-number percentage is required for an engine field, use `roundNearest(100 * successes /
outcomes)` after enumeration. Permille fields use `floor(1000 * successes / outcomes)` unless their
own schema explicitly declares a different display-only rounding rule.

## Random-draw ownership

Forecast, help text, menu inspection, and AI evaluation do not consume execution randomness. They
use exact probabilities or expected values. Random draws occur only for a confirmed execution and
are cached when the engine evaluates the same result at more than one boundary.

Every action execution assigns stable identities to its random sites:

```text
(battle, action instance, source, target, strike, roll site, draw index)
```

`roll site` distinguishes attack, active defense, resistance, status selection, damage dice, and
other explicitly random operations. Re-entering forecast or apply code for the same identity reuses
the cached result; it never rerolls. A new target or strike receives a new identity only when the
action's cardinality rule says that roll is independent.

The implementation may use the engine RNG or a mod-owned generator at a particular proven hook, but
all DCL-owned rolls must satisfy these semantic requirements:

- three independent `1..6` draws for each 3d6 roll;
- uniform `0..99` for percentage rolls;
- uniform selection among candidates when a rule says `random one`;
- no dependency on UI refresh count, forecast order, polling frequency, or hash-map iteration;
- forced-roll settings exist only for tests and cannot be part of a release profile.

## Implementation invariants

An implementation is conformant only when automated checks cover at least:

- positive and negative exact-half rounding;
- natural 3–4 success below score 3;
- natural 17–18 failure at score 18 or above;
- low-score critical failure when the roll exceeds the score by 10;
- exact 216-outcome and 46,656-pair forecast counts;
- Quick Contest target-wins-ties behavior;
- 0% and 100% percentage boundaries;
- preview/AI calls leaving the confirmed execution result unchanged;
- repeated apply hooks reusing, rather than resampling, one cached outcome.
