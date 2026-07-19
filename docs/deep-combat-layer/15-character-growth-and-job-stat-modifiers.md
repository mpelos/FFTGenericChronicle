# Character Growth and Job Stat Modifiers

This document owns permanent Character Level growth, the active job's growth vector, fractional
progress, permanent Brave and Faith policy, additive job stat modifiers, their point-equivalent
accounting, and endgame attribute envelopes. Job tiers, ability budgets, and the complete job
authoring workflow are owned by
[Job Tiers, Ability Budgets, and Authoring](16-job-tiers-ability-budgets-and-authoring.md).

Attribute meanings and derived-stat formulas are owned by
[Attributes and Derived Stats](01-attributes-and-derived-stats.md). Weapon, Shield, and magical
training are owned by [Skills and Active Defenses](03-skills-and-active-defenses.md) and
[Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md).

## Four progression layers

The final unit is assembled from four independent layers:

| Layer | Persistence | Owns |
| --- | --- | --- |
| Character growth | Permanent | ST, DX, IQ, Brave, Character HP Modifier, and Character MP Modifier. |
| Job chassis | While the job is active | Additive stat adjustments, mobility, resistance, and equipment access. |
| Job training | Permanent per learned job | Job Level, aptitude-driven Rank, and unlocked abilities. |
| Equipment and states | While present | Item properties and temporary battle modifiers. |

Character Level never enters an attack, defense, resistance, damage, HP, or MP formula directly. It
awards permanent growth. Job Level never awards raw attributes; it supplies training Rank. A job
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
| Permanent Brave | Max/base Brave | HT, explicit temperament modifiers, physical resistance, Basic Speed, and the HT route to MaxMP. |
| Character HP Modifier | Reinterpreted Raw HP | MaxHP without increasing ST. |
| Character MP Modifier | Reinterpreted Raw MP | MaxMP without increasing IQ or HT. |

Move, Jump, Dodge, Parry, Block, Will, Basic Speed, DR, Weapon Skill, Shield Skill, SpellScore,
Magic Resistance, weapon damage, and equipment Weight do not receive independent Character Level
growth. They change only when one of their owning inputs changes.

This restriction prevents double-dipping. A point of ST already improves damage, HP, and carrying
capacity; Character Level does not also award separate permanent damage or Basic Lift growth for the
same gain.

## Per-job growth vectors with equal value

Every job declares its own allocation vector:

```text
GrowthVector(job) = {
    STAllocation,
    DXAllocation,
    IQAllocation,
    BraveAllocation,
    HPModifierAllocation,
    MPModifierAllocation
}
```

A job may allocate zero to any channel and does not need to grow every attribute. Every allocation
and rate is nonnegative: ordinary level growth never reduces a permanent characteristic. Its
identity comes from the shape of the vector, but every job at every Job Tier receives the same
point-equivalent budget per Character Level:

```text
sum(GrowthAllocation[job, channel]) = UniversalGrowthBudget
```

Equivalently, when a job is authored as stat rates:

```text
sum(
    GrowthRate[job, channel]
    * PointCostPerStep[channel]
) = UniversalGrowthBudget
```

Equal budget prevents an objectively inferior leveling job; different allocation creates permanent
specialization. A unit that levels exclusively in a physical job and later becomes a caster retains
equal total development, but much of that value remains invested in physical channels. Safe total
value does not mean cost-free respecialization.

Job Tier never changes `UniversalGrowthBudget`. An advanced job receives its progression reward in
ability budgets, not permanent growth.

## Deterministic fractional level-up

Each permanent channel owns one lifetime progress accumulator. On the first award for a Character
Level, the active job distributes that level's allocation:

```text
job = ActiveJob

for each growth channel:
    GrowthPoints[channel] += JobGrowthAllocation[job][channel]

    while GrowthPoints[channel] >= PointCostPerStep[channel]:
        PermanentValue[channel] += GrowthStep[channel]
        GrowthPoints[channel] -= PointCostPerStep[channel]
```

The equivalent rate representation is:

```text
GrowthProgress[channel] += JobGrowthRate[job][channel]
gain = floor(GrowthProgress[channel])
PermanentValue[channel] += gain
GrowthProgress[channel] -= gain
```

Growth persistence uses signed 64-bit micro-units:

```text
GrowthScale = 1,000,000
one growth micro-unit = 1 / GrowthScale of a channel step

ScaledGrowthRate = exactDecimalToInteger(JobGrowthRate * GrowthScale)

GrowthProgressMicro[channel] += ScaledGrowthRate
gain = floor(GrowthProgressMicro[channel] / GrowthScale)
PermanentValue[channel] += gain
GrowthProgressMicro[channel] -= gain * GrowthScale
```

Authoring decimals have at most six fractional places and convert exactly; a value requiring
rounding fails validation. The accumulator is serialized with the character and never reconstructed
from displayed values. Intermediate arithmetic uses a checked width sufficient for the complete
level range; overflow fails validation rather than wrapping.

A fraction is retained across Character Levels and job changes, so separately rounded job histories
never destroy progress. The same multiset of levels in the same jobs produces the same permanent
result regardless of order.

The process has no random stat roll. A Character Level grants growth only once. Losing a Character
Level does not remove earned growth, and regaining an already-awarded level does not award growth
again. Delevel/relevel cycles cannot manufacture attributes.

Job Level, JP, gender, and Job Tier never change an allocation, cost, step, or accumulator.

### Persistent growth record and save migration

Every roster character stores one versioned DCL growth record:

```text
DclGrowthState
    GrowthSchemaRevision
    HighestAwardedCharacterLevel
    GrowthProgressMicro[ST, DX, IQ, Brave, HPModifier, MPModifier]
```

`HighestAwardedCharacterLevel` is monotonic in `1..99`. A level-up grants growth only when the new
Character Level is greater than this stored value; after the award, the field advances to that
level. Deleveling never lowers it. If an engine operation crosses more than one previously unearned
level, each crossed level is awarded once in ascending order using the job active for that level-up
transaction; an operation that cannot identify that job fails rather than guessing a history.

When a pre-DCL save has no growth record, migration preserves its current permanent PA, Speed, MA,
Brave, reinterpreted HP modifier, and reinterpreted MP modifier as the character's complete imported
baseline. It creates zero progress in every channel and sets `HighestAwardedCharacterLevel` to the
current Character Level. Past levels are neither reconstructed from an unknowable job history nor
awarded again. New growth begins with the next never-before-earned level.

The growth record and the corresponding permanent-stat changes commit atomically with the level-up
save transaction. A known older schema revision uses an explicit migration; an unknown newer
revision disables new growth for that character with a visible compatibility error and never resets
progress or silently re-awards levels.

## Earned, realized, and latent value

Integer attribute breakpoints separate the budget already earned from the value currently active in
battle:

```text
EarnedGrowthValue   = every point allocated by earned Character Levels
RealizedGrowthValue = point value of permanent integer gains already crossed
LatentGrowthValue   = retained fractional progress toward later gains
```

Equal earned value alone is insufficient. Every job is evaluated at representative Character
Levels to ensure that costly or widely spread allocations do not leave an excessive share of their
budget latent for most of the campaign. Nonzero channels also obey a calibrated maximum interval
between visible gains. Concentrating a vector is valid; hiding its value behind unusably slow
breakpoints is not.

## Brave growth and open-ended HT

Brave is the persistent storage for HT growth:

```text
PermanentBrave = RecruitmentBrave
                 + GrowthBrave
                 + explicit permanent Brave changes

CurrentBrave = PermanentBrave + temporary battle changes
HT           = BraveToHT(CurrentBrave)
```

The conversion has no upper clamp at Brave 100:

```text
HT = max(4, 10 + roundNearest((CurrentBrave - 50) / 8))
```

Brave 50 maps to HT 10, Brave 100 maps to HT 16, and Brave 112 maps to HT 18. Growth is awarded in
raw Brave steps rather than hidden HT steps; crossing a conversion breakpoint changes HT and all of
its derived consequences. Current Brave may also enter an explicitly authored temperament modifier,
but it is not a universal percentage chance.

The initial point price begins from `+1 HT = 10` and approximately eight Brave per HT, or `1.25`
points per raw Brave. Calibration measures any explicitly authored temperament interaction in the
ability or state that uses it rather than pricing a second global Brave probability mechanic.

An ordinary job chassis does not grant `JobBraveAdjustment` or `JobHTAdjustment`. The active job
shapes future Brave through its growth vector but does not instantly rewrite courage or health.

Any repeatable effect that changes Permanent Brave declares its legal range or convergence rule.
Open-ended growth storage does not silently authorize an unlimited permanent-stat farming loop.

## Faith remains outside Character Growth

Faith is a two-sided roster trait rather than a monotonic level-up attribute:

```text
PermanentFaith = RecruitmentFaith + explicit permanent Faith changes
CurrentFaith   = PermanentFaith + temporary battle changes
```

Both values use the `0..100` domain owned by
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#faith-potency-and-receptivity).
Permanent changes are summed and clamped to produce PermanentFaith; temporary battle changes are
then summed with that value and clamped to produce CurrentFaith. Faith does not inherit Brave's
open-ended storage.

Character Level, Job Level, growth vector, and ordinary job chassis do not raise or lower Faith.
Permanent Faith changes require an explicit, reversible, player-directed roster-shaping effect.
Hostile or ordinary combat manipulation changes CurrentFaith only, so an enemy cannot silently ruin
a roster unit after battle.

Faith has no positive Character Growth point cost. Its high and low values exchange supernatural
potency and receptivity rather than forming a universal improvement ladder. The continuous combat
factor is owned by
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#faith-potency-and-receptivity).

## Additive job stat chassis

The active job supplies fixed additive adjustments:

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
| Job HP Modifier | MaxHP only, after job-adjusted ST. |
| Job MP Modifier | MaxMP only, after the higher-of-HT/IQ contribution. |
| Job Basic Speed Adjustment | Fractional initiative, Move, and Dodge without increasing DX skills. |
| Job Move Adjustment | Horizontal movement without changing initiative or Dodge. |
| Job Jump | Vertical FFT traversal without changing the other mobility axes. |
| Job Dodge Adjustment | Active evasion without increasing initiative or attack skill. |
| Job Will Modifier | Mental resistance without increasing IQ skill or magical power. |
| Job Magic Resistance | Spiritual resistance without changing Will or magical Dodge. |

A `+0.25` Job Basic Speed Adjustment remains fractional until a formula explicitly floors Basic
Speed. A job receiving both an attribute adjustment and a direct derived-stat adjustment receives
both effects and pays for both.

### Values not owned by an ordinary chassis

An ordinary job chassis does not directly modify:

- permanent character ST, DX, IQ, HP Modifier, MP Modifier, Brave, or Faith;
- HT independently from Brave;
- DR, which belongs to equipped body and head items;
- weapon damage modifiers, Reach, Accuracy, or readiness, which belong to weapons;
- global MP cost, CastCT, duration, or Faith factor;
- Character Level, EXP, JP, or learned Job Level.

An explicit ability, state, or item can modify one of these values when its own rule says so. That
exception is not converted into a silent baseline property of every job.

## Equal stat-modifier budget

Every job receives the same point-equivalent numeric modifier budget regardless of Job Tier:

```text
ModifierBudget(job) = UniversalModifierBudget
```

The initial calibration hypothesis uses approximately `90` points per job chassis. This value is a
starting envelope, not proof of equal field performance. Examples of equal-cost numeric packages
include:

| Example chassis | Point value |
| --- | ---: |
| `+3 IQ`, `+10 MP` | 90 |
| `+3 ST`, `+30 HP` | 90 |
| `+2 ST`, `+1 DX`, `+25 HP` | 90 |
| `+2 IQ`, `+15 MP`, `+1 Will` | 90 |

A negative modifier is a real weakness but never buys unlimited value on an axis irrelevant to the
job's actual play. Equipment access, aptitude breadth, shields, armor, weapon families, innates, and
ability packages also influence battle power; they remain explicit inputs to the job-capacity audit
rather than hidden stat modifiers.

## Point-equivalent accounting

Growth vectors and numeric job modifiers use GURPS costs as their common comparison currency:

| Improvement | Initial point-equivalent cost |
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
it improves physical skills and Basic Speed. A direct Job Dodge or Job Basic Speed Adjustment is
charged separately because it is not part of an attribute increase.

Jump and Magic Resistance are DCL-specific axes. Their weights come from scenario calibration rather
than an unrelated GURPS purchase. The GURPS table is an initial balance currency; DCL playtests may
change a price when the adapted attribute package proves systematically more or less valuable.

## Endgame calibration envelope

The initial Character Level baseline is:

```text
Raw ST                = 10
Raw DX                = 10
Raw IQ                = 10
Brave                 = 50  -> HT 10
Character HP Modifier = 20
Character MP Modifier = 5
```

The initial growth hypothesis awards `4` point-equivalent units per Character Level. The 98 awards
from Character Level 1 to 99 therefore provide:

```text
Level99GrowthBudget = 98 * 4 = 392 points
```

This budget targets a heroic/cinematic endgame while retaining meaningful 3d6 ranges. The natural
endgame envelope includes permanent growth plus the active job chassis, but excludes equipment and
temporary states:

| Final value | Ordinary level-99 band | Initial natural ceiling |
| --- | ---: | ---: |
| ST | 11–18 | 21–22 |
| DX | 12–16 | 18 |
| IQ | 10–17 | 18 |
| Brave | job-shaped | at least 112 |
| HT | 10–16 | 18 as the first specialist target; no formula clamp |
| HP | 70–140 | approximately 155–160 |
| MP | 30–80 | approximately 90–95 |
| Basic Speed | 6–7.5 | approximately 8 |
| Move | 5–7 | approximately 8 |
| Dodge before equipment | 9–10 | approximately 11–12 |
| Will | normally IQ-derived | approximately 18–19 |

Natural ceilings are authoring targets rather than engine clamps. Equipment and temporary states
may exceed them when explicitly authored. Each ceiling names at least one owner whose growth rate
and job modifier align, so a pure career in that job reaches the intended maximum. A mixed history
cannot exceed the owner's raw growth rate merely by averaging other jobs.

### Black Mage endpoint example

The calibration model for an IQ-ceiling owner uses a provisional `+3 Job IQ` and `+10 Job MP`
chassis. Its 392 growth points allocate:

| Channel | Level-99 permanent gain | Point value |
| --- | ---: | ---: |
| ST | +1 | 10 |
| DX | +3 | 60 |
| IQ | +5 | 100 |
| Brave | +8 | 10 |
| Character HP Modifier | +43 | 86 |
| Character MP Modifier | +41 | 123 |
| Realized total |  | 389 |
| Latent fractional value |  | 3 |
| Earned total |  | 392 |

The resulting unequipped level-99 state is:

```text
ST = 11
DX = 13
IQ = 15 + 3 = 18
Brave = 58 -> HT 11
HP = 11 + 63 = 74
MP = max(11, 18) + 46 + 10 = 74
Basic Speed = 6
Base Dodge = 9
Will = 18
```

The corresponding exact six-decimal rates are `0.010205 ST`, `0.030613 DX`, `0.051021 IQ`,
`0.081636 Brave`, `0.438777 HP Modifier`, and `0.428557 MP Modifier`. After 98 awards their floors
produce the integer gains above. Using the point prices in this document, the six rates sum to
exactly `4` point-equivalent units per award; the unspent fractions retain the remaining three
points as latent value rather than inventing an extra integer gain.

The endgame comparison suite also includes pure physical, agile, vitality, MP, and hybrid histories.
It evaluates Character Levels 1, 10, 20, 40, 60, 80, and 99 rather than accepting a correct endpoint
with a broken midgame curve.

## Player-facing requirements

The unit screen separates permanent values from current-job and equipment/state adjustments. The
job-change preview shows every resulting change to ST, DX, IQ, HT, HP, MP, Will, Basic Speed, Move,
Jump, Dodge, Weapon/Shield skills, Parry, Block, Magic Resistance, Basic Lift, and encumbrance.

A Character Level result identifies the active job's allocation, equal point budget, every
permanent integer gain, and retained fractional progress. Faith changes appear only when an explicit
Faith-shaping effect occurs; they never appear as unexplained level-up growth.
