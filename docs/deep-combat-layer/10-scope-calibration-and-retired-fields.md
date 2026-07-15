# Scope, Calibration, and Retired Fields

This document owns the boundaries between fixed DCL structure, authored numeric tables, excluded
GURPS subsystems, and deliberately retired FFT behavior.

## Physical-layer boundary

The physical DCL does not define:

- spell skill, spell damage, healing, or charge policy;
- MP economy;
- Faith effects;
- magical evasion, shield-magical defense, or cloak-magical defense;
- magical status resistance;
- Zodiac compatibility.

Those fields retain no implied rule from an older DCL draft. A separate magic specification must
name their owners and interactions before they participate in the new model.

Job kits also live outside this directory. Jobs consume the contracts here by providing attribute
growth, equipment access, aptitude Tiers, Job Level, and explicit abilities.

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
- automatic import of unnamed GURPS maneuvers.

An individual ability may create a visible temporary effect resembling one of these concepts without
silently enabling the full subsystem.

## Fixed formula shapes and calibrated values

The following are structural and do not change during ordinary balance calibration:

- `ST = Raw PA`, `DX = Raw Speed`, `IQ = Raw MA`, `HT = BraveToHT(Brave)`;
- Brave 50 maps to HT 10, Brave 100 maps to HT 16, and the conversion uses the defined eight-Brave
  interval and rounding rule;
- HP from ST, Basic Lift from ST, Will from IQ, Basic Speed from DX and HT;
- Weapon/Shield Skill from DX, Difficulty, Rank, aptitude Tier, and Job Level;
- the Tier-by-Job-Level Rank schedule and its equivalent GURPS investment bands;
- attack roll followed by one active-defense roll;
- reusable Dodge, cumulative repeated-Parry penalty, one normal Block per defensive cycle;
- weapon damage from thrust/swing plus weapon modifier;
- DR subtraction before wound multiplier;
- normal DR as BodyDR plus HeadDR and targeted DR as one location;
- encumbrance from Load divided by Basic Lift;
- Speed as initial initiative rather than CT growth;
- Movement plus Action, with normal Movement-and-Attack free of penalty;
- native FFT KO and death lifecycle.

The following are authored tables or constants that may change without changing the architecture:

- `HPScale` and the ST thrust/swing table bridge;
- `LiftScale` and item Weight values;
- `InitiativeSeed`, `GlobalCTGain`, and `TurnThreshold` magnitudes;
- job Move/Dodge adjustments;
- weapon modifiers, Parry modifiers, Block modifiers, DB, Accuracy, range, divisors, and readiness;
- BodyDR, HeadDR, and accessory modifiers;
- Shock bands;
- status potency and resistance modifiers;
- cover, elevation, and location penalties where the owning rule labels a reference value.

Calibration may tune these numbers but must preserve the owner and direction of every term.

## Deliberately retired FFT behavior

| Legacy field or behavior | DCL treatment |
| --- | --- |
| HPGrowth / HPMultiplier | Retired; HP derives from ST. |
| Large armor HP bonuses | Retired for ordinary armor; protection is DR. |
| Weapon PA bonus | Retired as ST growth; weapon contribution is its damage modifier. |
| Speed-based CT gain | Retired; CT gain is linear and Speed seeds initiative. |
| Vanilla physical hit chance minus evasion | Replaced by attack roll plus one active-defense roll. |
| W-Ev percentage | Reinterpreted as weapon Parry profile/derived display. |
| S-Ev percentage | Reinterpreted as shield Block/DB profile/derived display. |
| C-Ev percentage | Reinterpreted as job Dodge adjustment/derived display. |
| Accessory physical evade percentage | Reinterpreted as an explicit Dodge/DB item modifier. |
| Armor damage-type matrix | Retired; each item has one physical DR. |
| Equipment as a source of ordinary weapon skill | Retired; DX and job training own skill. |
| Character Level added directly to Weapon Skill | Retired; Level grows attributes and Job Level grows Rank. |

Retirement is intentional. Internal storage fields do not receive artificial mechanics merely to
keep them populated; player-facing values are replaced with the derived DCL values that now own the
same decisions.
