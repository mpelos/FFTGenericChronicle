# Scope, Calibration, and Retired Fields

This document owns the boundaries between fixed DCL structure, authored numeric tables, excluded
GURPS subsystems, and deliberately retired FFT behavior.

## DCL boundary

The DCL defines the shared physical and supernatural combat engine. Job kits live outside this
directory. Jobs consume the contracts here by providing attribute growth, HP/MP modifiers,
equipment access, aptitude Tiers, Job Level, tradition ownership, and explicit abilities.

The DCL does not assign individual spells or supernatural techniques to jobs, set their final JP
costs, or add equipment entries. It defines the metadata and resolution contracts those authored
records must use.

## Excluded GURPS subsystems

The DCL deliberately has no:

- Fatigue Points;
- Perception characteristic or generic awareness roll;
- Size Modifier;
- Reach C or same-tile close combat;
- per-weapon Minimum ST;
- universal hit-location roster beyond Head and Body;
- bleeding;
- negative-HP consciousness or GURPS death checks;
- permanent anatomical crippling;
- grappling subsystem;
- universal attack of opportunity for leaving Reach;
- automatic import of unnamed GURPS maneuvers;
- free-form spell construction during battle;
- automatic high-skill reduction of MP cost or CastCT;
- universal Magic Evasion percentage;
- separate magical HeadDR or BodyDR;
- spell line-of-sight and cover checks for tracked FFT unit targets;
- persistent summoned creatures as part of the ordinary Summon command.

An individual ability may create a visible temporary effect resembling one of these concepts without
silently enabling the full subsystem.

## Fixed formula shapes and calibrated values

The following are structural and do not change during ordinary balance calibration:

- `ST = Raw PA`, `DX = Raw Speed`, `IQ = Raw MA`, `HT = BraveToHT(Brave)`;
- Brave 50 maps to HT 10, Brave 100 maps to HT 16, and the conversion uses the defined eight-Brave
  interval and rounding rule;
- HP from ST plus additive character/job modifiers;
- MP from the higher of HT/IQ plus additive character/job modifiers;
- Basic Lift from ST, Will from IQ, Basic Speed from DX and HT;
- Weapon/Shield Skill from DX, Difficulty, Rank, aptitude Tier, and Job Level;
- magical-tradition skill from IQ, Difficulty, Rank, aptitude Tier, and source Job Level;
- individual spells as unlocked techniques with one relative Spell Modifier;
- the Tier-by-Job-Level Rank schedule and its equivalent GURPS investment bands;
- attack roll followed by one active-defense roll;
- reusable Dodge, cumulative repeated-Parry penalty, one normal Block per defensive cycle;
- weapon damage from thrust/swing plus weapon modifier;
- DR subtraction before wound multiplier;
- normal DR as BodyDR plus HeadDR and targeted DR as one location;
- encumbrance from Load divided by Basic Lift;
- Speed as initial initiative rather than CT growth;
- Movement plus Action, with normal Movement-and-Attack free of penalty;
- Cast as an Action with Movement legal before or after declaration;
- CastCT and persistent durations on the global clock rather than personal Speed growth;
- FFT unit tracking with range/vertical legality checked at declaration and no spell LoS check;
- source, delivery, and effect as separate ability axes;
- External Projectile magic facing active defense and DR;
- Internal Direct magic facing resistance while normally ignoring active defense and DR;
- continuous Faith potency/receptivity centered at Faith 50 rather than a five-band skill bonus;
- MP as the only extraordinary-energy pool, independent from whether an action is a Spell;
- overcasting on the same HP/MP scale, consuming MP before HP;
- native FFT KO and death lifecycle.

The following are authored tables or constants that may change without changing the architecture:

- CharacterHPModifier, JobHPModifier, CharacterMPModifier, and JobMPModifier progressions;
- the ST thrust/swing table bridge;
- `LiftScale` and item Weight values;
- `InitiativeSeed`, `GlobalCTGain`, and `TurnThreshold` magnitudes;
- job Move/Dodge adjustments;
- weapon modifiers, Parry modifiers, Block modifiers, DB, Accuracy, range, divisors, and readiness;
- BodyDR, HeadDR, and accessory modifiers;
- tradition Difficulties, Spell Modifiers, MP costs, CastCT, range, vertical tolerance, area, and
  selectivity;
- `MagicPowerScale`, healing scale, spell power, focus modifiers, and magical damage/healing tables;
- the Faith factor slope around its fixed Faith-50 neutral point;
- Magic Resistance, concentration, Shell, element, Oil, duration, tick, Dispel, and stacking values;
- `HasteCarry`, `SlowDebt`, Quick magnitude, and Stop duration;
- status potency and resistance modifiers;
- cover, elevation, and location penalties where the owning rule labels a reference value.

Calibration may tune these numbers but must preserve the owner and direction of every term.

## Deliberately retired FFT behavior

| Legacy field or behavior | DCL treatment |
| --- | --- |
| Raw HP / HPGrowth | Reinterpreted as the per-character HP modifier and its growth, not a complete HP pool. |
| HPMultiplier | Replaced by an additive JobHPModifier. |
| Raw MP / MPGrowth | Reinterpreted as the per-character MP modifier and its growth, not a complete MP pool. |
| MPMultiplier or FP-like job field | Replaced by an additive JobMPModifier; no FP pool exists. |
| Large armor HP bonuses | Retired for ordinary armor; protection is DR. |
| Weapon PA bonus | Retired as ST growth; weapon contribution is its damage modifier. |
| Speed-based CT gain | Retired; CT gain is linear and Speed seeds initiative. |
| Vanilla physical hit chance minus evasion | Replaced by attack roll plus one active-defense roll. |
| W-Ev percentage | Reinterpreted as weapon Parry profile/derived display. |
| S-Ev percentage | Reinterpreted as shield Block/DB profile/derived display. |
| C-Ev percentage | Reinterpreted as job Dodge adjustment/derived display. |
| Accessory physical evade percentage | Reinterpreted as an explicit Dodge/DB item modifier. |
| Class magical evasion | Reinterpreted as JobMagicResistance or explicit Dodge-vs-magic. |
| Shield magical evasion/parry | Reinterpreted as Block/DB against explicitly Blockable spells. |
| Cloak/accessory magical evasion | Reinterpreted as an explicit Dodge-vs-magic or Magic Resistance property. |
| Universal magical hit chance minus M-Ev | Replaced by SpellScore plus the delivery's active defense or resistance gate. |
| Faith folded into magical skill | Retired; Faith modifies declared potency/receptivity rather than technical SpellScore. |
| Armor damage-type matrix | Retired; each item has one physical DR. |
| Equipment as a source of ordinary weapon skill | Retired; DX and job training own skill. |
| Character Level added directly to Weapon Skill | Retired; Level grows attributes and Job Level grows Rank. |

Retirement is intentional. Raw HP and Raw MP survive because they have purposeful character-modifier
roles. Other internal storage fields do not receive artificial mechanics merely to keep them
populated; player-facing values are replaced with the derived DCL values that now own the same
decisions.
