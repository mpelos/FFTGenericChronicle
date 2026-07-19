# Job Tiers, Ability Budgets, and Authoring

This document owns the Deep Combat Layer job-authoring contract: Job Tier E through A, sealed
budgets, command and portable ability capacity, Action Equivalent scoring, scenario validation,
skill cards, and acceptance gates. It defines how a job is measured without assigning a final kit or
number to any named job.

Permanent growth, additive stat modifiers, their equal budgets, and level-99 attribute envelopes are
owned by
[Character Growth and Job Stat Modifiers](15-character-growth-and-job-stat-modifiers.md). Physical
and magical resolution remain owned by their dedicated combat documents; this manual measures the
contribution produced by those rules rather than redefining them.

## Authoring objective

A valid job package satisfies all of these goals:

1. every job grants equal permanent point value per Character Level;
2. every active chassis receives equal numeric stat-modifier value;
3. deeper jobs reward acquisition with stronger command and portable ability budgets;
4. ability count never substitutes for measured battle contribution;
5. stronger kits gain coverage, reliability, efficiency, synergy, or rare tools rather than only
   larger damage numbers;
6. every action, resource cost, reaction trigger, support slot, and movement slot creates a real
   decision;
7. every job retains readable weaknesses, counters, and unfavorable matchups;
8. analytical scores remain calibration hypotheses until scenario and live-game results support
   them.

Equal permanent value does not require equal active-job power. A character may level in a basic job
for the entire campaign without losing growth points, while deliberately accepting that the basic
job's active ability package has a lower tier budget.

## Tier and aptitude vocabulary

Job Tier and Aptitude Grade use the same letters but answer different questions:

| Term | Meaning |
| --- | --- |
| Job Tier E–A | Acquisition depth and ability-budget band. |
| Aptitude Grade E–A | Training quality for a weapon family, Shield, or magical tradition. |

The Job Tier order is:

```text
E -> D -> C -> B -> A
basic                  capstone
```

A Tier E job may have Aptitude Grade A with its signature weapon. Job Tier does not alter the Rank
schedule in [Skills and Active Defenses](03-skills-and-active-defenses.md).

## What Job Tier changes

Job Tier determines:

- expected depth and prerequisite difficulty in the job tree;
- expected campaign access window;
- Command Kit Budget;
- shared Reaction/Support/Movement Catalog Budget;
- the allowed ceiling for one command or portable ability.

Job Tier does not determine:

- Character Growth Budget;
- Job Modifier Budget;
- mandatory number of skills;
- primary-attribute ceiling;
- Aptitude Grade;
- free equipment quality;
- turn frequency;
- an unconditional damage multiplier.

Acquisition depth authorizes a larger reward but never excuses an ability that breaks its per-action
ceiling, removes counterplay, or becomes the correct portable choice for every build.

## Tier identities

| Job Tier | Access and kit expectation |
| --- | --- |
| E | Initial foundation. A narrow, efficient routine, a clear contribution, and simple early build pieces. |
| D | First specialization. A protected function, one clear advantage over foundations, and a relevant situational tool. |
| C | Complete specialist. A full core loop, real fallback, reasonable coverage, and a meaningful secondary line. |
| B | Advanced job. Strong internal synergy, less common effects, higher efficiency, and tools that change team decisions. |
| A | Capstone. The largest capacity inside a defined identity, premium late portable rewards, and rare tools with explicit costs and counters. |

These descriptions do not impose skill counts. A six-skill Tier A kit can exceed a ten-skill Tier E
kit when its six actions improve the best decision across more important battle states.

## Sealed budgets

Every job is evaluated through four independent budgets:

```text
GrowthBudget(job)   = UniversalGrowthBudget
ModifierBudget(job) = UniversalModifierBudget
CommandBudget(job)  = CommandBudget[JobTier]
PortableBudget(job) = PortableBudget[JobTier]
```

Growth and modifier budgets are equal across E, D, C, B, and A. Command and portable budgets rise
with tier. A weakness or surplus in one budget never purchases value in another.

The following are prohibited cross-subsidies:

- inferior permanent growth buying better abilities;
- inferior numeric chassis buying superior permanent growth;
- filler skills compensating for one broken action;
- a negative stat irrelevant to the job buying strength on its protected axis;
- access friction, JP grind, or late availability excusing an action without counterplay.

## Initial tier-budget indices

The indices below are initial calibration hypotheses. They describe relative capacity and have no
direct one-to-one conversion to JP, number of skills, damage, or Action Equivalents.

| Job Tier | Command Kit Budget | R/S/M Catalog Budget |
| --- | ---: | ---: |
| E | 1.00 | 1.00 |
| D | 1.15 | 1.20 |
| C | 1.32 | 1.45 |
| B | 1.52 | 1.75 |
| A | 1.75 | 2.10 |

Portable value grows proportionally faster than command value. This preserves FFT's progression
reward in which basic jobs teach foundations while the best Reaction, Support, and Movement choices
appear in the deepest tiers. The difference is controlled rather than as extreme as vanilla FFT.

Jobs inside one tier target a narrow score band rather than an exact integer. The initial tolerance
is approximately `±5%`; this avoids adding filler or weakening coherent abilities solely to land on
an arbitrary exact total.

## Per-ability ceilings

A larger total kit budget does not authorize one action to absorb the entire increase:

| Job Tier | Command per-action ceiling | Portable per-ability ceiling | Portable loadout contribution |
| --- | ---: | ---: | ---: |
| E | 1.00 | 1.00 | 1.00 |
| D | 1.05 | 1.12 | 1.15 |
| C | 1.10 | 1.28 | 1.35 |
| B | 1.17 | 1.50 | 1.60 |
| A | 1.25 | 1.80 | 1.95 |

The command ceiling grows more slowly than the full command budget, so advanced capacity is bought
primarily through better coverage, reliability, fallback, synergy, and resource use rather than one
unconditional attack. The portable ceiling grows more aggressively so the strongest individual
R/S/M rewards can live in Tier B and A.

## What a better kit means

A better kit does not inherently mean:

- more skills;
- larger damage on every action;
- numerically superior copies of old skills;
- automatic replacement of every earlier option;
- absence of MP, HP, charge, setup, positioning, accuracy, or target restrictions;
- a good matchup against every enemy and map.

A higher budget may buy:

- useful actions in more battle states;
- higher reliability where reliability matters;
- better range, area, selectivity, or target access;
- better action or resource economy;
- stronger internal synergy;
- effects that other jobs cannot reproduce;
- a more valuable fallback when the primary plan is countered;
- a premium capstone with real tradeoffs;
- more meaningful pre-battle choices.

An earlier vertical option may be intentionally superseded on one axis. `Move+3` may be strictly
better than `Move+1` for raw horizontal distance because it is a late progression reward. It does
not thereby become superior to Teleport, Float, Ignore Height, Fly, or another movement tool in all
maps and builds. Horizontal alternatives retain value by solving different access problems.

## Action Equivalent

The common contribution currency is the Action Equivalent:

```text
1.0 AE = expected battle impact of one level-appropriate reference action
```

An implementation may display the same unit as `100 Battle Contribution Points`. AE compares
damage, healing, prevention, control, tempo, positioning, resource changes, and equipment effects
without claiming that they produce identical play.

The score is an offline authoring instrument. The player does not see AE during battle.

## Action value

For action `a` in battle state `s`:

```text
ActionValue(a, s) =
    ExpectedOutcomeValue(a, s)
  - ResourceCost(a, s)
  - DelayCost(a, s)
  - SetupCost(a, s)
  - ExposureRisk(a, s)
  - OpportunityCost(a, s)
```

For every affected target:

```text
TargetOutcomeValue =
    ResolutionProbability
  * OutcomeMagnitude
  * TargetImportance
  * TimingFactor
```

Only applicable resolution gates are multiplied:

```text
ResolutionProbability =
    P(execution succeeds)
  * P(delivery succeeds)
  * P(active defense fails)
  * P(resistance fails)
```

Physical attack, ranged, magical delivery, active-defense, and resistance formulas remain owned by
[Skills and Active Defenses](03-skills-and-active-defenses.md),
[Ranged Combat](07-ranged-combat.md), and
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md). This manual consumes their
exact forecast probabilities.

## Outcome currencies

Every battle effect declares the outcome dimensions it changes:

| Dimension | Value source |
| --- | --- |
| Damage | Expected effective injury relative to the reference attack. |
| KO | Enemy future actions removed after the target leaves play. |
| Healing | Effective HP restored after excluding overheal. |
| Prevention | Expected injury or status loss actually prevented. |
| Revive | Restored HP plus expected future allied actions recovered. |
| Control | Enemy actions or action quality removed during effective duration. |
| Buff/debuff | Change per affected future action times the number of relevant actions. |
| CT/tempo | Fraction of an allied action gained or enemy action denied. |
| Mobility | Actions saved, attack opportunities created, and exposure avoided. |
| Resource | Future action value enabled or denied by MP, HP, or item changes. |
| Equipment | Change in the target's best future actions or defenses. |
| Allegiance/routing | Enemy actions removed, allied actions gained, and targeting changed. |

Campaign, economy, roster, recruitment, Poach, treasure, and permanent equipment-acquisition value
use a separate campaign score. They never purchase Battle Contribution.

## Initial outcome weights

These reference weights are calibration hypotheses:

| Result | Initial conversion |
| --- | --- |
| Effective damage equal to one reference attack | 1.0 AE |
| One average enemy action completely denied | `1.0 AE * target threat` |
| One allied action created or preserved | `1.0 AE * ally capacity` |
| Effective healing equal to reference damage | 0.8–1.0 AE |
| Effective prevention equal to reference damage | 0.9–1.0 AE |
| CT change | Affected fraction of the next relevant action |
| Buff or debuff | Change per action times expected affected actions |
| Revive | Restored HP value plus recovered future actions |
| Movement | Actions saved plus opportunities and avoided exposure |
| MP change | Future action value enabled or denied |
| Equipment break | Reduction in future best actions or defenses |

Damage beyond the target's remaining HP does not receive full value. Damage that produces a KO adds
the expected future action loss rather than receiving an arbitrary flat execution bonus.

## Delivery properties and double-count prevention

Range, area, charge, hit chance, and friendly-fire policy modify the state-dependent action value;
they do not automatically receive a second flat bonus:

| Property | Mathematical effect |
| --- | --- |
| Range | Target availability, movement saved, target selection, and exposure avoided. |
| Hit chance | Multiplies the outcome through exact 3d6 resolution probability. |
| Charge | Delays impact and adds relevance, incapacity, and interruption risk. |
| Area | Sums signed outcome value across every affected target. |
| Friendly fire | Adds negative allied-target terms. |
| Selective/no friendly fire | Removes allied terms and increases useful area opportunities. |
| Ignore DR | Changes expected injury against protected targets. |
| Remove active defense | Removes that delivery gate. |
| Remove resistance | Removes that contest gate. |
| Target tracking | Prevents movement from invalidating a tracked target. |
| No spell LoS | Increases legal target availability under the DCL magic contract. |
| Head targeting | Changes the DR route and any authored location consequence. |
| Setup requirement | Adds prior action, state, equipment, or positioning cost. |

For an area action:

```text
AreaValue = sum(EnemyTargetValue) - sum(AllyTargetValue)
```

A selective area removes the allied sum. It does not also receive a duplicate selectivity bonus.

Under FFT unit tracking, charged magic does not lose its target merely because the target moves.
Charge still pays for later resolution, the caster or target becoming irrelevant, incapacity,
dedicated interruption, and committed resources:

```text
ChargedValue =
    ImmediateOutcomeValue
  * P(caster remains capable)
  * P(target remains relevant)
  * TimeDiscount
  - InterruptionLoss
```

## Status, buff, and duration value

A status receives value from the action quality it changes, not its label:

```text
StatusValue =
    P(infliction)
  * sum(future affected turns) {
        P(status remains active)
        * DifferenceInBestAction
        * TimeDiscount
    }
```

Don’t Act can remove a full action. Silence removes only the value of casts the target would
otherwise choose. Don’t Move changes access and positioning rather than automatically denying an
action. Knocked Down combines its combat modifiers with the Movement and Action resources consumed
by Stand Up.

Buffs and debuffs use:

```text
BuffValue =
    ExpectedAffectedActions
  * ImprovementPerAction
  * P(those actions occur)
```

An effect with long nominal duration earns no value for turns that never occur before KO, cure,
Dispel, or battle end.

## Resource and anti-spam value

MP, HP, items, charge, accuracy, setup, self-risk, targeting restrictions, and exposure are normal
tradeoffs. A technique does not require a custom cooldown merely because it is repeatable. It may be
spammable when its repeated AE remains appropriate, or self-limit through its ordinary resource and
state economics.

MP has a shadow price derived from future actions:

```text
MPValue = ExpectedFutureActionValueEnabledByMP
```

An MP drain earns only the future action value it actually denies. Overcasting prices HP through the
ordinary KO risk created by that expenditure.

## Command kit capacity

One turn chooses one action, so the command budget never equals the sum of every skill's isolated
maximum:

```text
BestJobAction(state) = max(ActionValue(action, state))

CommandKitScore = average over benchmark states {
    BestJobAction(state)
}
```

The marginal value of a new skill is:

```text
MarginalSkillValue =
    Score(Kit + NewSkill) - Score(Kit)
```

A skill inferior in every relevant state contributes approximately zero. A situational skill can
contribute substantial value when it becomes the best action in an important state. Five redundant
attacks never count as five complete actions of capacity.

Every command set records:

- Command Kit Score;
- Core Loop Score;
- Command Catalog Score;
- Per-Action Ceiling;
- Burst Ceiling;
- Control Ceiling;
- Resource Efficiency;
- value as a secondary command;
- incidence of every skill as the best action.

```text
SkillIncidence =
    benchmark states where the skill is best
    / benchmark states where it is legal
```

Zero incidence marks a dead or redundant action unless a protected campaign function justifies its
separate value layer. Very high incidence triggers a dominance review.

## Reaction, Support, and Movement capacity

R/S/M share one tier budget:

```text
PortableBudget =
    ReactionContribution
  + SupportContribution
  + MovementContribution
```

The job may concentrate this value in any category. It does not need one premium ability in each
slot and never adds filler to satisfy a count.

Portable capacity uses three distinct measurements:

```text
PortableCatalogScore
PortableAbilityCeiling
PortableLoadoutContribution
```

Catalog Score measures the pre-battle choice set. Ability Ceiling measures the strongest individual
export. Loadout Contribution measures the best legal combination of Reaction, Support, and Movement
that the job adds to the global build pool.

Two alternatives in the same slot are not simultaneous. A job teaching `Move+3` and Float earns the
equipped value of the better option in the current state plus the coverage created by choosing
between them before battle.

Every portable ability also records:

```text
IntrinsicPortableValue = value when equipped on its strongest legal host

UnlockDelta =
    best legal value after learning the ability
  - best legal value before learning the ability
```

Intrinsic value prevents a late ability from hiding its full equipped strength behind an earlier
version. Unlock Delta measures the actual campaign reward and planned vertical replacement.

The strongest portable abilities live in high tiers. An earlier raw upgrade may become obsolete on
its exact axis, but one late ability does not become the universal answer across unrelated axes,
maps, equipment, enemies, and job identities.

## Complete active-job capacity

The active job's battle capacity includes:

```text
JobBattleCapacity = function(
    native command,
    basic attack,
    numeric chassis,
    equipment access,
    aptitudes,
    innates,
    reaction,
    support,
    movement,
    secondary command,
    scenario
)
```

These inputs interact and are not blindly added. DX, for example, changes attack skill, Basic Speed,
Move, Dodge, and active defenses through the owning formulas. Equipment access can change DR,
readiness, range, Block, and encumbrance simultaneously.

Every job is measured in three build states:

| Build | Purpose |
| --- | --- |
| Native Floor | Job, appropriate equipment, native command, and no required foreign ability. Proves the job functions as a team piece. |
| Expected Build | Representative secondary, R/S/M, and equipment available at the job's campaign window. Measures ordinary use. |
| Legal Ceiling | Strongest legal combination and host. Exposes abusive synergies and donor value. |

The command set is evaluated both natively and as a secondary. The job is not required to solo every
encounter; it is required to make a clear team contribution and retain a fallback when its primary
plan is countered.

## Capacity vector

The tier scalar never replaces the diagnostic vector:

```text
Damage
Sustain
Control
Tempo
Mobility
Reliability
TargetCoverage
ResourceEfficiency
Survivability
PortableValue
```

A specialist may have a high favorable-scenario peak and a low unfavorable-scenario floor. A job
that scores highly on every axis is suspect even when its aggregate budget appears legal.

## Benchmark scenarios

The calibration suite includes:

- open and cramped maps;
- low and high elevation;
- clustered and dispersed formations;
- one durable target and many weak targets;
- low and high DR;
- high Dodge, Parry, and Block;
- physical, mental, and spiritual resistance;
- status-vulnerable and status-immune enemies;
- melee pressure and ranged pressure;
- short burst battles and long attrition battles;
- allies mixed into enemy areas;
- low-resource states;
- an ally threatened with KO and an ally already KO'd;
- enemies dependent on important equipment;
- protection, approach, survival, and elimination objectives.

Each scenario records Native Floor, expected contribution, Legal Ceiling, unfavorable-case floor,
resource use, and skill incidence. The final validation is counterfactual:

```text
BattleImpact(job) =
    P(victory with job)
  - P(victory with reference unit)
```

Analytical scores seed implementation and simulation. Live-game results recalibrate outcome weights,
tier indices, per-ability ceilings, and scenario weights without changing the sealed-budget
architecture.

## Job compass and protected systems

Before pricing a skill, the job defines:

- its battle and campaign fantasy;
- when the player wants to field it;
- what it does better than the rest of the roster;
- weaknesses and bad matchups it retains;
- what it must not become;
- FFT systems it carries, such as recruitment, Poach, stealing, item economy, equipment destruction,
  Brave/Faith shaping, monster access, or treasure;
- whether a non-vanilla mechanic expresses the identity more clearly than preserving the old list.

Changing a weak vanilla skill never silently deletes the campaign, build, roster, economy, or
monster system it carried. The author keeps, moves, replaces, or explicitly routes that system
elsewhere.

## Skill card

Every command, reaction, support, and movement ability records:

```text
Name
Source: vanilla / revised / new
Decision: keep / rework / merge / replace / move / remove
Value layer: battle / campaign / economy / roster / monster / build / R/S/M
Function and player use
Governing attribute and skill
Delivery and effect
Legal targets, range, vertical tolerance, area, and friendly-fire policy
VisionRequired targeting/delivery flag
Execution, active-defense, and resistance gates
Outer-action structure: targets, strikes, riders, within-action application, and Reaction window
DR and hit-location policy
Magnitude and duration
CastCT or readiness
MP, HP, item, action, movement, setup, and exposure costs
Counterplay and unfavorable states
Visibility and expiry
Native value
Secondary or portable value
Best legal host
Skill incidence
What prevents spam
What prevents universal adoption
Protected systems carried
```

Every battle-action skill card references a valid normalized `DclActionProfile`; every persistent
effect references a valid `DclStateDefinition`. The schemas and defaults are owned by
[Action and State Authoring Contract](19-action-and-state-authoring-contract.md). The skill card
adds design intent, capacity, and job-fit evidence rather than restating that runtime record.

A Reaction additionally records its `Trigger`, `ActivationMode`, optional single
`ActivationReference`, activation modifier, source/target binding, native cardinality, and
once-per-window behavior. The allowed activation modes and absence of a universal Brave chance are
owned by
[Action Transactions and Reactions](18-action-transactions-and-reactions.md#reaction-activation-modes).

A battle action also states when it is preferable to attacking, killing, healing, or moving to
safety. A campaign action states why its durable value justifies the turn, JP, risk, or opportunity
cost without becoming mandatory grind.

## Authoring recipe

Each job definition follows this order:

1. declare Job Tier and tree position;
2. write the job compass independently from the vanilla skill list;
3. inventory protected battle, campaign, roster, economy, equipment, and monster systems;
4. identify stronger non-vanilla mechanics worth considering;
5. declare the per-job Growth Vector and prove the universal budget;
6. simulate the pure growth history through Character Level 99 and intermediate milestones;
7. name every natural ceiling the job owns;
8. declare numeric Job Modifiers and prove the universal modifier budget;
9. list every derived value affected by those modifiers;
10. declare equipment access, weapon/Shield/tradition Aptitude Grades, and innates;
11. audit every retained, changed, moved, merged, or removed vanilla mechanic;
12. write a skill card for every proposed ability;
13. calculate action value, skill incidence, marginal value, and per-action ceilings;
14. calculate Command Kit Score natively and as a secondary;
15. calculate R/S/M Catalog, Ability Ceiling, Loadout Contribution, and Unlock Delta;
16. test Native Floor, Expected Build, and Legal Ceiling;
17. test favorable and unfavorable scenarios and enemy use;
18. verify visibility, complexity, engine feasibility, and protected-system continuity;
19. compare the job against its tier bands and roster neighbors;
20. recalibrate analytical weights with battle evidence.

## Acceptance gates

A job package fails review when any of these conditions holds:

- permanent growth is above or below the universal budget;
- numeric job modifiers hide unequal point value;
- equipment or aptitude power is omitted from capacity evaluation;
- the command kit misses its tier band or exceeds a per-action ceiling;
- portable value misses its tier band or creates a universal default;
- a skill never becomes the best action and carries no protected non-battle system;
- a skill's nominal cost is irrelevant to its actual user;
- a high-tier reward removes all alternatives rather than one intended vertical predecessor;
- the native job cannot contribute without borrowing another job's identity;
- the job has no meaningful weakness, counter, or bad matchup;
- a changed vanilla skill silently removes a campaign or roster system;
- persistent state, trigger, expiry, or limitation is not visible;
- the kit is balanced only by invisible cooldowns or special-case clauses;
- equal paper points conceal dominant performance across the scenario suite;
- the player cannot understand the job without hidden external notes.

Passing the mathematical budgets is necessary but never sufficient. The job is accepted only when
its numbers, battle behavior, build texture, campaign role, visibility, and counterplay agree.
