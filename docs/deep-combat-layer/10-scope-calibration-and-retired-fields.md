# Scope, Calibration, and Retired Fields

This document owns the boundaries between fixed DCL structure, authored numeric tables, excluded
GURPS subsystems, and deliberately retired FFT behavior.

## DCL boundary

The DCL defines the shared physical and supernatural combat engine and the job-authoring budget
contract. Individual job kits live outside this directory. Jobs consume the contracts here by
providing an equal-budget growth vector, an equal-budget additive stat chassis, equipment access,
Aptitude Grades, Job Tier, Job Level, tradition ownership, and explicit abilities.

The DCL does not assign individual spells or supernatural techniques to jobs, set their final JP
costs, or add equipment entries. It defines the metadata and resolution contracts those authored
records must use. The normalized logical schema is owned by
[Action and State Authoring Contract](19-action-and-state-authoring-contract.md).

## Native FFT inheritance

FFT behavior remains authoritative unless an owning DCL rule explicitly replaces it. The absence
of a DCL override is not an open design question and never authorizes a new subsystem.

This default includes native command flow, target selection, tile/path/height legality, action and
charge lifecycle, status cancellation, animation order, inventory/equipment restrictions, KO and
death flow, and other engine behavior outside the formula or state boundary being replaced. A DCL
implementation intercepts only the minimum native boundary required by its explicit rule and lets
the surrounding FFT transaction continue unchanged.

When a DCL feature composes with inherited behavior, its owner documents only the new interaction.
It does not redesign every native edge case. A change to inherited FFT behavior requires an explicit
DCL decision, implementation owner, player-facing consequence, and validation case.

## Excluded GURPS subsystems

The DCL deliberately has no:

- Fatigue Points;
- Perception characteristic or generic awareness roll;
- Size Modifier;
- Reach C or same-tile close combat;
- per-weapon Minimum ST;
- universal hit-location roster beyond Head and Body;
- bleeding;
- blunt trauma through flexible armor when DR prevents penetration;
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

- permanent Character ST = Raw PA, Character DX = Raw Speed, and Character IQ = Raw MA;
- effective ST, DX, and IQ from permanent character value plus additive job, equipment, and state
  adjustments, with positive minimum one after the complete expression;
- `HT = BraveToHT(current Brave)` with permanent Brave participating in Character Level growth;
- Brave 50 maps to HT 10, Brave 100 maps to HT 16, Brave 112 maps to HT 18, and the conversion uses
  the defined eight-Brave interval without an upper HT clamp;
- Current Brave is an open-ended HT input and not a universal percentage chance;
- every job owns an individual growth vector with the same total point-equivalent budget, regardless
  of Job Tier;
- deterministic fractional growth accumulators and no repeated growth award for a regained level;
- Faith outside Character Level and Job Level growth, changed permanently only by explicit
  reversible player-directed effects;
- additive job stat adjustments applied in full while the job is active rather than multiplicative
  PA, MA, Speed, HP, or MP scaling;
- HP from ST plus additive character/job modifiers;
- MP from the higher of HT/IQ plus additive character/job modifiers;
- MaxHP and MaxMP minimum one, pool decreases clamping current values without becoming
  damage/healing/drain, and pool increases granting no automatic refill;
- Basic Lift from ST, Will from IQ, Basic Speed from DX and HT;
- neutral Jump base 3 plus explicit job/equipment/state modifiers before encumbrance;
- Weapon/Shield Skill from DX, Difficulty, Rank, aptitude Tier, and Job Level;
- magical-tradition skill from IQ, Difficulty, Rank, aptitude Tier, and source Job Level;
- individual spells as unlocked techniques with one relative Spell Modifier;
- the Tier-by-Job-Level Rank schedule and its equivalent GURPS investment bands;
- Job Tier ordered E, D, C, B, A and separated from weapon, Shield, and tradition Aptitude Grade;
- equal Character Growth and numeric Job Modifier budgets across Job Tiers;
- increasing Command and R/S/M budgets by Job Tier, with portable value increasing proportionally
  faster than command value;
- action capacity measured from state-dependent expected contribution rather than ability count;
- attack roll followed by one active-defense roll;
- highest active defense with deterministic tie priority `Dodge > Parry > Block`;
- reusable Dodge, cumulative repeated-Parry penalty, one normal Block per defensive cycle;
- Dual Wield as one two-Strike Action with bundled DualWeaponTraining, full main-hand skill,
  off-hand skill at `-4`, and no OffHand Training;
- weapon damage from thrust/swing plus weapon modifier;
- the literal open-ended GURPS 4e ST thrust/swing table, with `+1d6` to both modes per full 10 ST
  above 100;
- positive dice-and-add normalization using `+7 -> +2d6` and `+4 -> +1d6`, without converting
  negative adds into fewer dice;
- DR subtraction before wound multiplier;
- zero Injury whenever DR reduces Penetrating Damage to zero;
- normal DR as BodyDR plus HeadDR and targeted DR as one location;
- encumbrance from Load divided by Basic Lift;
- Speed as initial initiative rather than CT growth;
- rank-normalized initial CT, threshold 100, normal global gain 10, quarter-point CT precision, and
  zero CT after every granted turn;
- Movement plus Action, with normal Movement-and-Attack free of penalty;
- universal Attack and Action-cost Reequip, with Aim and Deceptive Attack available only through
  explicit abilities;
- Cast as an Action with Movement legal before or after declaration;
- CastCT, magical timed effects, and periodic effects on the global clock rather than personal
  Speed growth, with short tactical states using an explicitly authored turn/use/command clock;
- FFT unit tracking with range/vertical legality checked at declaration and no spell LoS check;
- source, delivery, and effect as separate ability axes;
- External Projectile magic facing active defense and DR;
- Internal Direct magic facing resistance while normally ignoring active defense and DR;
- continuous Faith potency/receptivity centered at Faith 50 rather than a five-band skill bonus;
- MP as the only extraordinary-energy pool, independent from whether an action is a Spell;
- overcasting on the same HP/MP scale, consuming MP before HP;
- native FFT KO and death lifecycle;
- logically simultaneous area snapshots, per-strike physical attack/defense, and a single native FFT
  Reaction window after every target and strike finishes;
- Reaction activation through exactly one authored AutomaticTrigger, SkillResponse, or
  ActivationRoll mode rather than raw Brave percentage;
- native FFT physical line, trajectory, range, and vertical legality without new cover or elevation
  modifiers;
- native FFT behavior for every rule not explicitly replaced by a DCL owner.

The following are authored tables or constants that may change without changing the architecture:

- total Character Level growth budget, individual job allocations, growth costs, growth steps, and
  natural endgame envelopes;
- individual job ST, DX, IQ, HP, MP, Basic Speed, Move, Jump, Dodge, Will, and Magic Resistance
  adjustments;
- numeric Job Modifier Budget and DCL-specific prices for Jump and Magic Resistance;
- Command Kit, R/S/M Catalog, per-ability, and portable-loadout tier indices;
- Action Equivalent outcome weights, benchmark scenario weights, and tier score tolerances;
- CharacterHPModifier, JobHPModifier, CharacterMPModifier, and JobMPModifier magnitudes;
- item Weight values authored against the fixed `BasicLift = ST² / 5` curve;
- weapon modifiers, Parry modifiers, Block modifiers, DB, Accuracy, range, divisors, and readiness;
- BodyDR, HeadDR, and accessory modifiers;
- tradition Difficulties, Spell Modifiers, MP costs, CastCT, range, vertical tolerance, area, and
  selectivity;
- spell DamageBasis/HealingBasis, spell/focus magnitude modifiers, and fixed magic/healing expressions;
- the Faith factor slope around its fixed Faith-50 neutral point;
- Magic Resistance, concentration, Shell, element, Oil, duration, tick, Dispel, and stacking values;
- Quick magnitude and Stop duration;
- status potency and resistance modifiers;
- explicit ability-owned location, height, and preparation modifiers.

Calibration may tune these numbers but must preserve the owner and direction of every term.

## Deliberately retired FFT behavior

| Legacy field or behavior | DCL treatment |
| --- | --- |
| Raw HP / HPGrowth | Reinterpreted as the per-character HP modifier and its growth, not a complete HP pool. |
| HPMultiplier | Replaced by an additive JobHPModifier. |
| Raw MP / MPGrowth | Reinterpreted as the per-character MP modifier and its growth, not a complete MP pool. |
| MPMultiplier or FP-like job field | Replaced by an additive JobMPModifier; no FP pool exists. |
| PA Growth / MA Growth / Speed Growth | Reinterpreted as channels in the active job's individual equal-budget Character Growth vector. |
| PA Multiplier | Replaced by additive JobSTAdjustment. |
| MA Multiplier | Replaced by additive JobIQAdjustment. |
| Speed Multiplier | Replaced by additive JobDXAdjustment and, when required, a separate fractional JobBasicSpeedAdjustment. |
| Automatic Faith growth by Character Level or Job Level | Retired; PermanentFaith changes only through an explicit roster-shaping effect. |
| Large armor HP bonuses | Retired for ordinary armor; protection is DR. |
| Weapon PA bonus | Retired as ST growth; weapon contribution is its damage modifier. |
| Speed-based CT gain | Retired; CT gain is linear and Speed seeds initiative. |
| Vanilla partial CT retention after acting or waiting | Retired; every granted turn ends at CT 0. |
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
