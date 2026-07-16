# Character Growth and Job Stat Modifiers

This document owns permanent Character Level growth, the growth profile supplied by the active job,
the current job's additive stat chassis, and the point-equivalent recipe used to compare those
packages. It defines how a job is authored without assigning final values to any individual job.

Attribute meanings and derived-stat formulas are owned by
[Attributes and Derived Stats](01-attributes-and-derived-stats.md). Weapon, Shield, and magical
training are owned by [Skills and Active Defenses](03-skills-and-active-defenses.md) and
[Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md).

## Four progression layers

The final unit is assembled from four independent layers:

| Layer | Persistence | Owns |
| --- | --- | --- |
| Character growth | Permanent | ST, DX, IQ, Brave/HT, Character HP Modifier, and Character MP Modifier. |
| Job chassis | While the job is active | Additive stat adjustments, mobility, resistance, and equipment access. |
| Job training | Permanent per learned job | Job Level, aptitude-driven proficiency Rank, and unlocked abilities. |
| Equipment and states | While present | Item properties and temporary battle modifiers. |

Character Level never enters an attack, defense, resistance, damage, HP, or MP formula directly.
It awards permanent growth. Job Level never awards raw attributes; it supplies training Rank. A job
change replaces the chassis without rewriting the character's permanent values.

EXP advances Character Level and has no direct combat term. JP purchases abilities and spells but
does not increase an attribute or trained score merely by being accumulated.

## Permanent character channels

Normal Character Level growth may advance only these channels:

| Growth channel | Persistent FFT storage | Primary consequences |
| --- | --- | --- |
| Character ST | Raw PA | Thrust/swing damage, MaxHP, and Basic Lift. |
| Character DX | Raw Speed | Physical skills and one-half of Basic Speed's numerator. |
| Character IQ | Raw MA | Will, magical skills, magical power, and the IQ route to MaxMP. |
| Permanent Brave | Max/base Brave | Current HT, HT resistance, raw-Brave mechanics, and the HT route to Basic Speed and MaxMP. |
| Character HP Modifier | Reinterpreted Raw HP | MaxHP without increasing ST. |
| Character MP Modifier | Reinterpreted Raw MP | MaxMP without increasing IQ or HT. |

Move, Jump, Dodge, Parry, Block, Will, Basic Speed, DR, Weapon Skill, Shield Skill, SpellScore,
Magic Resistance, weapon damage, and equipment Weight do not receive independent Character Level
growth. They change only when one of their owning inputs changes.

This restriction prevents double-dipping. A point of ST already improves damage, HP, and carrying
capacity; Character Level does not also award separate permanent damage or Basic Lift growth for the
same gain.

## Growth profiles supplied by jobs

Every job declares exactly one shared growth profile:

- `Physical` emphasizes ST, Brave/HT, and Character HP Modifier while retaining some MP growth;
- `Magical` emphasizes IQ and Character MP Modifier while retaining some physical growth;
- `Hybrid` distributes growth between physical and magical channels without matching either
  specialist at its peaks.

Jobs assigned to the same profile use the same growth allocation. An individual job never receives
a secretly superior version of Physical, Magical, or Hybrid growth. Job identity inside a profile
comes from its current chassis, aptitudes, equipment, and abilities rather than a better permanent
level-up table.

All profiles receive the same total point-equivalent budget for the same Character Level gain. They
change only the allocation of that budget. DX receives the same growth allocation in all three
profiles: fast-job identity belongs to the current chassis, so leveling in one particular agile job
does not become the permanent optimal route for every build.

The profile allocations and total per-level budget are calibrated data. This architecture does not
require a particular job-specific amount.

## Deterministic level-up recipe

Each permanent channel owns a fractional progress accumulator. On the first award for a Character
Level, the active job's shared profile distributes that level's point-equivalent budget:

```text
profile = ActiveJob.GrowthProfile

for each growth channel:
    GrowthProgress[channel] +=
        LevelGrowthBudget * ProfileAllocation[profile][channel]

    while GrowthProgress[channel] >= GrowthCost[channel]:
        PermanentValue[channel] += GrowthStep[channel]
        GrowthProgress[channel] -= GrowthCost[channel]
```

Fractional progress is retained, so a level that does not cross an integer breakpoint is not a dead
level. The process contains no random stat roll: identical permanent starting values and identical
profile histories produce identical results.

A level grants growth only the first time that character earns it. Losing a Character Level does
not remove already-earned growth, and regaining a previously awarded level does not award growth a
second time. Delevel/relevel cycles therefore cannot manufacture permanent attributes.

Gender never changes a profile, allocation, cost, step, accumulator, or job chassis modifier.

## Brave growth and current HT

Brave is the persistent storage for HT growth:

```text
PermanentBrave = RecruitmentBrave
                 + GrowthBrave
                 + explicit permanent Brave changes

CurrentBrave = PermanentBrave + temporary battle changes
HT           = BraveToHT(CurrentBrave)
```

Growth is awarded in raw Brave steps rather than hidden HT steps. Every Brave point can matter to a
mechanic that explicitly uses Brave percentage, while crossing a conversion breakpoint changes HT
and all of HT's derived consequences.

The point budget begins from the GURPS value of `+1 HT = 10` and the DCL's approximate eight-Brave
conversion interval. The calibrated cost of raw Brave also accounts for its independent percentage
uses; those uses are not free merely because the next HT breakpoint has not been reached.

An ordinary job chassis does not grant `JobBraveAdjustment` or `JobHTAdjustment`. The active job can
shape future Brave growth through its shared profile, but equipping a job does not instantly rewrite
the character's courage or health.

## Faith remains outside Character Growth

Faith is a two-sided roster trait rather than a monotonic level-up attribute:

```text
PermanentFaith = RecruitmentFaith + explicit permanent Faith changes
CurrentFaith   = PermanentFaith + temporary battle changes
```

Character Level, Job Level, growth profile, and ordinary job chassis do not raise or lower Faith.
Permanent Faith changes require an explicit, reversible, player-directed roster-shaping effect.
Hostile or ordinary combat manipulation changes CurrentFaith only, so an enemy cannot silently ruin
a roster unit after battle.

Faith therefore has no positive Character Growth point cost. Its high and low values exchange
supernatural potency and receptivity rather than forming a universal improvement ladder. The
continuous combat factor remains owned by
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#faith-potency-and-receptivity).

## Additive job stat chassis

The active job supplies fixed additive adjustments. The generic composition rule is:

```text
EffectiveValue = CharacterValue
                 + JobAdjustment
                 + EquipmentAdjustment
                 + StateAdjustment
```

The exact derived formulas and rounding points remain in
[Attributes and Derived Stats](01-attributes-and-derived-stats.md). Job adjustments apply in full as
soon as the job is active and do not scale with Job Level. Changing jobs removes the previous
chassis and applies the new one without changing permanent character storage.

Every unspecified adjustment is zero. A job may use these chassis axes:

| Job axis | Consequences |
| --- | --- |
| Job ST Adjustment | Damage, MaxHP, and Basic Lift through ST. |
| Job DX Adjustment | Physical skills and Basic Speed through DX. |
| Job IQ Adjustment | Will, magical skill, magical power, and possibly MaxMP through IQ. |
| Job HP Modifier | MaxHP only, after the job-adjusted ST contribution. |
| Job MP Modifier | MaxMP only, after the higher-of-HT/IQ contribution. |
| Job Basic Speed Adjustment | Fractional initiative, Move, and Dodge without increasing DX-based skills. |
| Job Move Adjustment | Horizontal movement without changing initiative or Dodge. |
| Job Jump | Vertical FFT traversal without changing the other mobility axes. |
| Job Dodge Adjustment | Active evasion without increasing initiative or attack skill. |
| Job Will Modifier | Mental resistance without increasing IQ-based skills or magical power. |
| Job Magic Resistance | Spiritual resistance without changing Will or ordinary magical Dodge. |

A `+0.25` Job Basic Speed Adjustment is meaningful and remains fractional until a formula explicitly
floors Basic Speed. A job that receives both an attribute adjustment and a direct derived-stat
adjustment receives both effects and pays for both; for example, Job DX plus Job Dodge is not treated
as one bonus written twice.

### Values not owned by an ordinary chassis

An ordinary job chassis does not directly modify:

- permanent character ST, DX, IQ, HP Modifier, MP Modifier, Brave, or Faith;
- HT independently from Brave;
- DR, which belongs to equipped body and head items;
- weapon damage modifiers, Reach, Accuracy, or readiness, which belong to weapons;
- Parry or Block when the same identity can be expressed by aptitude, weapon, or shield;
- global MP cost, CastCT, duration, or Faith factor;
- Character Level, EXP, JP, or learned Job Level.

An explicit ability, status, innate, or item can modify one of these values when its own rule says so.
That exception is not converted into a silent baseline property of every job.

## Aptitudes and access are separate from stat modifiers

A complete job package also declares:

- weapon-family aptitude Tiers;
- Shield aptitude when shields are legal;
- magical-tradition aptitude Tiers;
- equipment access;
- innate and command abilities.

These properties influence combat power but are not disguised as attribute modifiers. Job Level
uses aptitude to supply Rank; it does not amplify the job's ST, DX, IQ, HP, MP, Speed, Move, Jump,
Dodge, Will, or Magic Resistance chassis.

## Point-equivalent accounting

Growth profiles and job stat chassis use GURPS costs as a common comparison currency:

| Improvement | Point-equivalent cost |
| --- | ---: |
| +1 ST | 10 |
| +1 DX | 20 |
| +1 IQ | 20 |
| +1 HT | 10 |
| +1 HP through a character/job modifier | 2 |
| +1 MP capacity through a character/job modifier | 3 |
| +1 Will independent of IQ | 5 |
| +0.25 Basic Speed independent of DX/HT | 5 |
| +1 Basic Move independent of Basic Speed | 5 |
| +1 Dodge independent of Basic Speed | 15 |
| +1 Parry for one weapon family | 5 |
| +1 Parry for every weapon family | 10 |
| +1 Block | 5 |

An attribute's price includes its normal derived consequences. `+1 DX` is charged once even though
it can improve skills and Basic Speed. A direct Job Dodge or Job Basic Speed Adjustment is charged
separately because it is not part of an attribute increase.

Jump and Magic Resistance are DCL-specific job axes rather than direct purchases from the GURPS
attribute table. Their job-budget weights come from scenario calibration and are not inferred from
an unrelated attribute cost.

Point equivalence is an accounting gate, not proof that two jobs play equally. Equipment access,
aptitude breadth, action economy, range, armor, innates, and ability packages require their own
scenario validation. A negative chassis adjustment is a real weakness but does not automatically
buy an unlimited amount of strength on axes irrelevant to the job's intended play.

## Job-authoring recipe

Each job definition follows the same order:

1. Assign `Physical`, `Magical`, or `Hybrid` growth; do not author a private growth table.
2. State the current-job strengths and weaknesses the chassis must express.
3. Add only the smallest necessary ST, DX, IQ, HP, MP, Basic Speed, Move, Jump, Dodge, Will, and
   Magic Resistance adjustments; every other axis remains zero.
4. List every derived value changed by those adjustments so no consequence is counted or hidden
   twice.
5. Declare weapon, Shield, and magical-tradition aptitudes separately from the stat chassis.
6. Declare equipment access and innate/ability value separately from point-equivalent stat totals.
7. Compare jobs at the same Character Level, profile-history assumption, equipment tier, and Job
   Level before judging their chassis.
8. Validate the job in favorable and unfavorable matchups; equal point totals do not excuse a job
   with no practical weakness or one that is never worth fielding.

This recipe determines how future job numbers are produced without fixing those numbers in the
global combat specification.

## Player-facing requirements

The unit screen separates permanent values from current-job and equipment/state adjustments. The
job-change preview shows every resulting change to ST, DX, IQ, HT, HP, MP, Will, Basic Speed, Move,
Jump, Dodge, Weapon/Shield skills, Parry, Block, Magic Resistance, Basic Lift, and encumbrance.

A Character Level result identifies the active growth profile, every permanent integer gain, and
retained fractional progress toward later gains. Faith changes appear only when an explicit
Faith-shaping effect occurs; they never appear as an unexplained level-up result.
