# Player-Facing Information

The DCL adds rolls and derived values that must remain inspectable. A rule that cannot be explained
in the status screen, equipment screen, targeting forecast, or a visible battle state is not a
complete DCL rule.

## Unit status screen

The unit screen exposes the real DCL values:

| Value | Required presentation |
| --- | --- |
| ST | Raw PA mapping and current explicit modifiers. |
| DX | Raw Speed mapping and current explicit modifiers. |
| IQ | Raw MA mapping and current explicit modifiers. |
| HT | Brave-derived 3d6 score and raw Brave. |
| Will | IQ-derived score and modifiers. |
| Faith | Current value and continuous potency/receptivity factor. |
| HP | Current/maximum and ST, Character HP Modifier, Job HP Modifier, and explicit item/status terms. |
| MP | Current/maximum and `max(HT, IQ)`, Character MP Modifier, Job MP Modifier, and explicit item/status terms. |
| Basic Speed | Actual fractional value. |
| Move/Jump | Effective values after job, equipment, state, and encumbrance. |
| Dodge | Final score and main penalties. |
| Encumbrance | Band, current Load, Basic Lift, Move multiplier, and Dodge penalty. |

The interface does not display a doubled compatibility Speed merely to resemble vanilla FFT.

## Skill screen

For each usable weapon, shield, and magical tradition, the player can inspect:

- governing attribute;
- Difficulty;
- relevant aptitude Tier and owning job;
- Job Level that supplies Rank;
- current Rank and equivalent investment band;
- final Skill;
- resulting Parry or Block.

A magical tradition also shows its source job, source Job Level, IQ-based Tradition Skill, and each
unlocked spell's relative Spell Modifier and final SpellScore. Faith is shown separately and never
hidden inside the skill breakdown.

A Job Level that raises Rank without crossing the next integer Skill breakpoint still shows that
progress.

## Equipment screen

Every item exposes only the properties it actually uses.

Weapons show damage expression, thrust/swing basis, damage type, armor divisor, Reach/range,
Accuracy, Weight, hands, Parry modifier, balance/readiness, and special properties.

Body and head equipment show their separate DR and Weight. Shields show Block modifier, Defense
Bonus, legal physical/magical coverage, and Weight. Foci and accessories show only their explicit
Spell Skill, power, concentration, CastCT, MP-cost, Faith, affinity, routing, or resistance
properties.

Equipping an item immediately previews:

- total Load and encumbrance band;
- effective Move, Jump, and Dodge;
- BodyDR and HeadDR;
- derived weapon damage and active defenses;
- MaxHP, MaxMP, Magic Resistance, spell defenses, and relevant focus modifiers;
- the next encumbrance threshold.

## Attack forecast

The forecast headline shows:

- final probability that the attack deals a hit after active defense;
- damage expression `Xd6+Y`;
- applicable DR and armor divisor;
- damage type and wound multiplier;
- possible injury range after DR.

The breakdown shows:

- base Weapon Skill;
- Effective Skill;
- range, cover, elevation, facing, location, Aim, Shock, and state modifiers;
- critical chance;
- selected active defense and its chance;
- Block availability or repeated-Parry penalty;
- Body, Head, or combined DR selection.

For ranged attacks the final percentage uses the formula owned by
[Ranged Combat](07-ranged-combat.md) rather than
subtracting an evasion percentage from an attack percentage.

For a magical action the forecast additionally shows:

- tradition skill, Difficulty, Rank, Spell Modifier, and final SpellScore;
- caster and target Faith values and resulting factor where applicable;
- Zodiac modifier when applicable;
- MP cost, reserved MP, overcasting HP, and failure/interruption cost;
- CastCT, expected resolution point, Charging state, and concentration/cancellation conditions;
- unit tracking or fixed-tile mode, range and vertical legality, and the absence of a LoS check;
- Reflect route before confirmation;
- delivery class and the target's active defense or resistance score;
- final success chance, damage/healing dice, DR/divisor policy, element, Shell, area, selectivity,
  friendly-fire policy, status chance, and duration.

## Persistent states

Prone, Stun, Don't Act, Don't Move, Aim, Ready/Unready, lost Block, repeated-Parry penalties, Shock,
Charging, reserved resources, tracked targets, fixed tiles, Reflect routes, QuickLock, global
duration/tick counters, and any other state that changes a future choice require visible
representation and a clear expiry.

The player must not need external notes to know:

- whether an equipped weapon can attack or Parry;
- whether Block is available;
- which target is being Aimed at;
- why Move or Dodge changed;
- which armor location an attack will test;
- which spell is Charging and when it resolves;
- why a spell can be Dodged, Blocked, resisted, reflected, or stopped by DR;
- whether a persistent effect Refreshes, Replaces, stacks, or is the strongest instance.
