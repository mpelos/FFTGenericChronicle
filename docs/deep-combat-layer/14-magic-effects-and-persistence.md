# Magic Effects and Persistence

This document owns healing, revive, status duration, global-clock effects, Haste, Slow, Quick, Stop,
Silence, Dispel, stacking, undead interaction, and summon behavior.

## Healing

Healing uses the same IQ-based dice language as magical damage but does not enter the Injury
pipeline. A healing action declares one basis:

```text
HealingBasis = Thrust | Swing | Fixed

if HealingBasis is Thrust or Swing:
    BasicHealing = STDamageTable[IQ][HealingBasis]
else:
    BasicHealing = authored FixedHealingExpression

HealingExpression = NormalizeDiceAndAdds(
                        BasicHealing
                        + SpellHealingModifier
                        + FocusHealingModifier
                        + explicit healing modifiers
                    )

RawHealing     = max(0, roll(HealingExpression))
FinalHealing   = floor(RawHealing * HealingFaithMultiplier)
AppliedHealing = min(FinalHealing, MaxHP - CurrentHP)
```

The ST table and dice normalization are the exact shared functions owned by
[Damage, Armor, and Injury](05-damage-armor-and-injury.md#from-st-and-weapon-to-damage-dice). `Fixed`
stores an explicit `Xd6+Y` and does not scale with IQ. SpellHealingModifier,
FocusHealingModifier, and ordinary explicit modifiers are integer adds after the lookup; an
exceptional whole-die modifier is authored separately.

Each heal declares `FaithPolicy = None | Caster | Target | Both`. HealingFaithMultiplier is `1`,
the selected unit's FaithFactor, or both factors multiplied together according to that policy. The
continuous factors are defined in
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#faith-potency-and-receptivity).
Faith is not also applied through SpellScore. Healing ignores DR, wound multipliers, damage type,
elemental affinity, Shell, Shock, Major Wound, and every other Injury-stage rule. Excess healing
above MaxHP is lost; ordinary healing does not become damage or revive a KO target unless the
action's separate target/effect profile explicitly says so.

Job Level and tradition Rank improve SpellScore, not healing magnitude. A spell may convert margin
to healing only when that conversion is explicit in its own profile. The forecast shows the dice
range, expected rolled value, Faith-adjusted result, and AppliedHealing after the missing-HP cap.

### Critical healing

A critical success on a direct heal maximizes exactly one magnitude die before Faith and the
missing-HP cap:

```text
NormalHealingExpression   = Xd6 + Y
CriticalHealingExpression = (X - 1)d6 + (Y + 6), when X >= 1
```

The maximized die is replaced before rolling; the resolver does not roll every die and then choose
one opportunistically. Thus `3d6+2` becomes `2d6+8`. A zero-die expression receives no universal
magnitude bonus. Critical healing does not also increase duration or reduce MP/HP cost. A
non-healing beneficial effect has no universal critical bonus and must declare any deterministic
critical consequence in its own profile.

The DCL does not import GURPS's cumulative daily penalty for repeatedly healing the same subject.
FFT battle economy instead prices healing through Action, MP, CastCT, range, targeting, caster risk,
and the opportunity cost of not attacking or controlling.

## Revive and the native death clock

Raise, Arise, Phoenix Down, Revive, Reraise, and comparable effects operate on FFT's native KO,
death-countdown, crystal, and treasure lifecycle. They do not create GURPS negative-HP death checks.

A revive action declares:

- eligible target states;
- casting and resistance policy;
- restored HP expression;
- whether Faith modifies success or restored HP;
- interaction with Undead;
- whether the effect is immediate revive or a stored Reraise trigger.

Faith may improve revive chance or restored HP, but not both on the same action unless the action
explicitly pays for both benefits.

## Undead interaction

There is no universal inference that every heal damages Undead or every drain heals them. Each
effect family declares its interaction in an explicit table:

```text
Normal target | Undead target | Undead caster
```

The required families include direct healing, regeneration, HP drain, MP drain, Raise/Arise,
Reraise, Poison, instant KO, and restorative items. This preserves FFT's Undead identity without
letting one hidden inversion rule produce contradictions across spells and items.

## Status infliction

A hostile status normally uses a Quick Contest:

```text
caster margin = TargetSpellScore - casterRoll
target margin = ResistanceScore - targetRoll
```

The shared casting draw must first pass BaseSpellScore and then TargetSpellScore before entering the
margin comparison; the target resists on a tie. The status defines HT, Will, Spiritual Resistance,
or another explicit resistance. Immunity is checked before the contest. A rider whose carrier
already hit does not make a second caster roll; it uses one target resistance roll.

Bosses and special units use visible immunity, higher HT/Will/Magic Resistance, or duration
reduction. They do not receive hidden universal success caps.

Faith-sensitive status chance requires an explicit status-family rule. The continuous Faith
magnitude factor is not multiplied directly into a percentage and is not rounded into the rejected
five-band SpellScore modifier. Until an effect declares such a rule, Faith does not modify its Quick
Contest.

## Duration from margin

A timed status declares a base, band, minimum, and maximum:

```text
Duration = clamp(
    MinimumDuration,
    BaseDuration + floor(WinningMargin / DurationBand),
    MaximumDuration
)
```

All four duration terms use the state's declared clock unit. `DurationBand` is a positive integer;
`MinimumDuration`, `BaseDuration`, and `MaximumDuration` satisfy
`1 <= MinimumDuration <= BaseDuration <= MaximumDuration`. A successful hostile Quick Contest has
positive WinningMargin. An effect without an opposed margin uses an explicitly authored fixed
duration rather than inserting a fabricated margin. Instant effects do not create a zero-duration
persistent instance.

The contest determines whether the status lands and how firmly it lands. Duration does not also
receive an automatic Faith multiplier when Faith already affected another axis of the action.

## Global duration and tick clock

Magical durations and periodic effects use global CT units:

```text
AppliedAtGlobalCT
DurationCT
ExpiresAtGlobalCT = AppliedAtGlobalCT + DurationCT

TickIntervalCT
NextTickGlobalCT = AppliedAtGlobalCT + TickIntervalCT
```

`DurationCT` is positive for a timed effect. `TickIntervalCT` is either positive or absent; zero and
negative intervals fail authoring validation. A periodic effect does not tick immediately on
application unless its profile declares a separate immediate payload. While
`NextTickGlobalCT <= ExpiresAtGlobalCT`, the due tick resolves and advances NextTickGlobalCT by the
interval. When a tick and expiry share the same global timestamp, the tick resolves first and the
effect is removed afterward.

At one global timestamp, the DCL scheduler preserves atomic outer actions and uses this event
priority:

1. finish an outer ActionInstance already committing;
2. deliver charged actions whose CastCT completes;
3. resolve due periodic ticks in stable EffectInstance identity order;
4. expire effects due at that timestamp;
5. grant newly eligible unit turns in the CT order owned by
   [Turns, Movement, and Actions](02-turns-movement-and-actions.md).

Each delivery or tick is its own outer ActionInstance for target snapshots and the one post-action
Reaction window. Simultaneous due effects do not share mutable target state: each completes
atomically before the next stable identity. Forecast and AI use the same ordering without consuming
execution RNG.

The scheduler reserves one stable ActionInstance identity for a due tick and does not advance its
`NextTickGlobalCT` cursor until that exact action has completed target apply, source payment, its
declared Reaction policy, and settlement. Retrying a pending scheduler step reuses the reservation.
An explicitly authored immediate payload follows the same identity and settlement rule but is not a
scheduled tick and does not advance `NextTickGlobalCT`.

Haste and Slow do not change how many Poison, Regen, zone, or delayed-effect ticks occur during a
fixed global duration. They only change the number of eligible turns a unit may receive inside that
duration. Stop freezes the unit's eligibility and personal CT behavior but does not freeze global
effect timers: ordinary periodic effects continue and timed statuses may expire. An exceptional
effect may override timer suspension only through explicit metadata.

A margin-derived status duration produces DurationCT directly. It never stores a number of target
turns and is not rescaled when Haste, Slow, Stop, or Speed changes later.

## Haste and Slow under linear CT growth

Normal units receive the global CT gain defined by
[Turns, Movement, and Actions](02-turns-movement-and-actions.md). Haste and Slow multiply that gain:

```text
Normal: CTGain = GlobalCTGain        = 10
Haste:  CTGain = 1.5 * GlobalCTGain = 15
Slow:   CTGain = 0.75 * GlobalCTGain = 7.5
```

CT retains quarter-point internal precision. Haste and Slow change later eligibility without
changing initial initiative, Dodge, Basic Speed, or CastCT. Every granted turn still resets CT to
zero, including a turn in which the unit takes no action.

Haste and Slow are opposed states. Applying one removes or replaces the other according to the
stacking policy below.

## Quick and Stop

Quick grants enough CT for the target's next turn according to its authored magnitude. The target
receives a visible `QuickLock` until that granted turn resolves; Quick cannot trigger another Quick
loop while the lock is active.

Stop freezes the unit's CT progression and turn eligibility. Global durations continue. Stop does
not silently grant immunity, remove DR, or cancel every persistent effect; its source declares any
additional consequence.

## Silence

Silence blocks actions marked `Verbal`. It does not test whether the action consumes MP or causes
magical damage. Ki, items, silent techniques, and equipment powers remain usable unless they also
declare `Verbal`.

High SpellScore does not automatically remove a verbal requirement. A perk, item, status, or spell
variant must explicitly provide silent casting.

## Dispel

Every dispellable persistent effect stores an integer `EffectStrength` when created:

```text
EffectStrengthBasis = SourceEffectiveScore | Fixed

EffectStrength = max(1,
                     selected basis value
                     + EffectStrengthModifier)
```

`SourceEffectiveScore` is the final governing score used to create the effect before rolling. A
state without a governing skill uses an authored Fixed basis. WinningMargin affects duration when
the status profile says so; it is not also added to EffectStrength.

A Dispel action declares its eligible effect family and removal scope. It makes one outer SpellScore
roll. Each selected effect instance independently rolls against its stored EffectStrength in a
Quick Contest with that shared caster roll. The effect remains on a tie. Narrow and source-matched
bonuses modify the dispeller's score before the roll; immunity and undispellable flags are checked
before entering the contest.

Dispel removes only the winning instance or scope declared by the action. It does not retroactively
refund MP, undo resolved damage, rewind CT, or remove unrelated instances that reuse the same icon.

## Stacking policies

Every persistent effect declares one policy:

| Policy | Behavior |
| --- | --- |
| Replace | New instance replaces the complete old instance. |
| Refresh | Existing payload/source remains and its duration resets from the new application time. |
| StrongestWins | Higher Strength owns the complete instance; equal Strength keeps the older owner and the later expiry. |
| StackToCap | Source-bound contributions remain separate and their visible aggregate clamps to a declared cap. |
| Independent | Each instance tracks separately. |

Every state declares its StackKey, the target-local identity used to find competing instances. A
typed state includes the parameter that distinguishes it, such as element, equipment slot, or
protected unit. Reapplication follows the selected policy atomically:

- `Replace` removes the competing instance and commits the new source, payload, Strength, duration,
  cure ownership, and presentation.
- `Refresh` keeps the existing source, payload, and Strength but sets its application/expiry fields
  from the newly calculated duration. It never copies a weaker or unrelated payload into the old
  state.
- `StrongestWins` compares integer Strength. A stronger newcomer replaces the complete instance; a
  weaker newcomer changes nothing; an equal newcomer keeps the older InstanceId, source, and
  payload while choosing the later of the two expiry points. An absent expiry is later than every
  finite expiry.
- `StackToCap` requires a positive cap and a ContributionKey. Reapplying the same contribution uses
  its own declared Refresh or Replace subpolicy; distinct contributions retain individual source,
  duration, and cleanup. The mechanical magnitude is `min(StackCap, sum(active contributions))`.
- `Independent` always creates a new InstanceId. Each instance expires, cures, dispels, and loses
  its source independently even when presentation art is duplicated.

If a mechanic needs different behavior, it declares a complete named state-specific policy and its
comparison, tie, duration, source-loss, dispel, presentation, and AI rules. It cannot select
`StrongestWins` and leave only the inconvenient tie-break undefined.

Defaults are:

- the same named status Refreshes;
- mutually exclusive variants Replace one another;
- different magnitudes use StrongestWins;
- stat modifiers StackToCap only when they declare a cap;
- identical zones from the same source do not self-stack;
- opposed statuses such as Haste and Slow Replace one another.

## Summons

FFT command summons remain premium selective area actions. Their visual creature is the delivery of
one action, not a persistent allied unit. Selectivity, large area, strong magnitude, and broad target
access are paid for through MP, CastCT, Job/JP access, and the rest of the summon profile.

A persistent summoned creature would require a separate unit-lifecycle, AI, occupancy, duration,
death, and control subsystem. Ordinary Summon actions do not silently enable that subsystem.
