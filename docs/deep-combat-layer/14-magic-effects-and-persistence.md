# Magic Effects and Persistence

This document owns healing, revive, status duration, global-clock effects, Haste, Slow, Quick, Stop,
Silence, Dispel, stacking, undead interaction, and summon behavior.

## Healing

Healing has a magnitude path separate from damage:

```text
HealingPower = HealingScale(IQ)
               + SpellHealingPower
               + FocusHealingModifier
               + explicit healing modifiers

RawHealing = HealingTable(HealingPower, spellHealingProfile)
```

Healing ignores DR and wound multipliers. A Faith-sensitive heal applies the Faith potency and
receptivity factors defined in
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#faith-potency-and-receptivity).
It does not receive a second Faith benefit through SpellScore.

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
caster margin = SpellScore - casterRoll
target margin = ResistanceScore - targetRoll
```

The caster must first succeed against SpellScore and must then win the margin comparison; the target
resists on a tie. The status defines HT, Will, Spiritual Resistance, or another explicit resistance.
Immunity is checked before the contest. A rider whose carrier already hit does not make a second
caster roll; it uses one target resistance roll.

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

The contest determines whether the status lands and how firmly it lands. Duration does not also
receive an automatic Faith multiplier when Faith already affected another axis of the action.

## Global duration and tick clock

Magical durations and periodic effects use global CT units:

```text
DurationCT
TickIntervalCT
```

Haste and Slow do not change how many Poison, Regen, zone, or delayed-effect ticks occur during a
fixed global duration. Stop freezes the unit's eligibility and personal CT behavior but does not
freeze global effect timers unless an effect explicitly says so.

## Haste and Slow under linear CT growth

All units continue to receive the same `GlobalCTGain`. Haste and Slow change retained CT when the
unit acts rather than restoring Speed-based CT growth:

```text
Normal: CT -= TurnThreshold
Haste:  CT -= TurnThreshold - HasteCarry
Slow:   CT -= TurnThreshold + SlowDebt
```

`HasteCarry` and `SlowDebt` are calibrated constants. They modify later eligibility without changing
initial initiative, Dodge, Basic Speed, or CastCT.

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

Every dispellable persistent effect stores `EffectStrength` when created. Dispel uses a Quick
Contest between its SpellScore and that stored strength, with explicit bonuses for narrow or
source-matched dispels. Immunity and undispellable flags are checked before the contest.

Dispel removes the effect; it does not retroactively refund MP, undo resolved damage, or rewind CT.

## Stacking policies

Every persistent effect declares one policy:

| Policy | Behavior |
| --- | --- |
| Replace | New instance removes the old one. |
| Refresh | Existing magnitude remains and duration resets. |
| StrongestWins | Higher magnitude remains; duration follows the effect's authored rule. |
| StackToCap | Magnitudes add up to a visible cap. |
| Independent | Each instance tracks separately. |

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
