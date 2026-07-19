# Action and State Authoring Contract

This document owns the normalized logical records that connect authored FFT actions and states to
the DCL rules. It does not duplicate combat formulas. Each field below points to the document that
owns its meaning and resolution.

The contract is independent of the final storage format. XML, NXD, a generated sidecar, and managed
runtime objects may split the fields physically, but the loader produces one normalized record
before forecast, AI, or execution can use it.

## Normalized action profile

Every DCL-governed command resolves through one `DclActionProfile`:

```text
DclActionProfile
    ActionId
    ProfileRevision

    SourceProfile
    SkillProfile
    ResourceProfile
    TimingProfile
    TargetProfile
    DeliveryProfile
    MagnitudeProfile?
    Effects[]
    TransactionProfile
    CriticalProfile
    PresentationProfile
    AiProfile
```

`ActionId` is the stable authored identity. `ProfileRevision` participates in runtime/data freshness
validation; forecast and confirmed execution never use different revisions of the same action.

### Source and skill

```text
SourceProfile
    Source: Physical | Spell | Ki | Divine | Spiritual | Equipment | MonsterPower | Other
    Verbal: boolean

SkillProfile
    GoverningSkill: SkillId | None
    SkillModifier
    SourceJob?               // required for learned off-job traditions
    ZodiacSensitive: boolean
```

Source, governing skill, and effect are independent. MP use, animation, job, or damage type never
infers that an action is a Spell. The source/skill rules are owned by
[Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md#source-delivery-and-effect-are-independent)
and [Skills and Active Defenses](03-skills-and-active-defenses.md).

`GoverningSkill = None` is legal only for a deterministic system command or effect that declares no
skill gate, such as Reequip. It never means that an unknown or missing skill should be skipped.

`Verbal` is explicit. Silence reads this source property directly; a missing boolean does not
silently mean that Silence is ineffective.

### Resource and timing

```text
ResourceProfile
    BaseMPCost
    MPCostMultiplier
    OvercastPolicy: Allowed | Forbidden

TimingProfile
    ConsumesAction: boolean
    ConsumesMovement: boolean
    BaseCastCT
    CastCTModifier
    ConcentrationRequired: boolean
```

`BaseMPCost` and `BaseCastCT` are nonnegative integers. MPCostMultiplier is an exact positive
rational and CastCTModifier a signed integer. An action with positive BaseMPCost defaults to
`OvercastPolicy = Allowed`; a prohibition is explicit. Zero-cost actions still store zero and never
pass through the minimum-cost rule. Cost commitment, outcome settlement, and HP substitution are
owned by [Magic Skills, Sources, and Energy](11-magic-skills-sources-and-energy.md#cost-commitment).

Ordinary commands consume Action and preserve Movement. A different resource shape must be explicit
and legal under [Turns, Movement, and Actions](02-turns-movement-and-actions.md#turn-resources).
`ConcentrationRequired` is true for a delayed cast unless the profile identifies a non-concentrating
timer. CastCT and disruption are owned by
[Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md).

### Target profile

```text
TargetProfile
    TargetMode: Unit | FixedTile | Caster
    AllegiancePolicy: Everyone | AlliesOnly | EnemiesOnly | CasterSide | Explicit
    EligibleTargetStates[]
    RangeMin
    RangeMax
    VerticalTolerance
    VisionRequired: boolean
    PhysicalRoute: NativeDirect | NativeArc | None

    CenterMode?              // required for area actions
    AreaShape?
    AreaSize?
    AreaDeliveryGate?
```

Ranges, area sizes, and vertical tolerances are nonnegative authored integers. Unit-targeted magic
uses FFT tracking and no spell LoS. Physical delivery uses native FFT line, trajectory, height, and
path legality. `VisionRequired` owns Blind interaction and never creates a Perception roll. The
complete rules are in [Facing, Reach, and Targeting](04-facing-reach-and-targeting.md),
[Ranged Combat](07-ranged-combat.md), and
[Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md).

An action with no area fields affects only its selected target. An area action must provide center,
shape, size, allegiance, and delivery gate together; partial area metadata is invalid. Target
membership uses the target-batch snapshot rather than enumeration-time mutation.

### Delivery profile

```text
DeliveryProfile
    Delivery: PhysicalAttack | ExternalProjectile | InternalDirect | Touch | Area | Rider |
              Beneficial | Other
    Dodgeable: boolean
    Parryable: boolean
    Blockable: boolean
    UsesDefenseBonus: boolean
    ResistanceCharacteristic?
    ArmorPolicy: Manifestation | ArmorDividing | InternalSpiritual | IgnoreDR | None
    ArmorDivisor?
    LocationPolicy: NormalCombined | Body | Head | EffectOwned
    Reflectable: boolean
    ShellSensitive: boolean
```

The delivery class fixes which combinations are legal; it does not infer missing booleans:

| Delivery | Required gate |
| --- | --- |
| Physical Attack | Per-Strike attack roll and at most one legal active defense. |
| External Projectile | One outer casting draw classified against BaseSpellScore and then TargetSpellScore; per-Strike Dodge or Block as explicitly enabled; DR policy required. |
| Internal Direct | One Quick Contest per target for the outer action; no active defense; normally InternalSpiritual armor policy. |
| Touch | One attack gate and exactly the authored Dodge/Parry legality; effect armor policy required. |
| Area | One outer casting roll plus the TargetProfile avoidance policy per target. |
| Rider | A successful carrier plus the rider's one authored resistance gate. |
| Beneficial | One casting roll; a willing legal target has no hostile target gate. |

External Projectile defaults are Dodgeable true, Parryable false, and Blockable false. Enabling
Block requires an explicit shield/Defense Bonus policy. Internal Direct rejects Dodge, Parry, and
Block fields. Armor divisor, precedence, DR, Faith, Shell, elements, and Reflect are owned by
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md) and
[Damage, Armor, and Injury](05-damage-armor-and-injury.md).

`Area` in this table is a magical or field-delivery class, not the shape itself. A physical sweep
or physical projectile burst retains `PhysicalAttack` delivery and supplies the area fields in
TargetProfile; this preserves its physical skill and defense gates for every affected target.

`ArmorDivisor` is absent unless ArmorPolicy is ArmorDividing. `IgnoreDR` is a distinct policy, never
an infinite divisor. A skill-level divisor overrides the weapon value exactly as defined by the
damage owner; profiles never combine two divisors.

### Magnitude and effect union

An action without numeric magnitude, such as pure Dispel or Reequip, omits MagnitudeProfile. A
numeric action selects exactly one primary magnitude kind:

```text
MagnitudeProfile =
    DamageMagnitude
        DamageBasis: Thrust | Swing | Fixed
        FixedExpression?
        IntegerModifier
        WholeDiceModifier?
        DamageType
        Element?
        ElementBoostMultiplier?
        FaithPolicy: None | Caster | Target | Both

  | HealingMagnitude
        HealingBasis: Thrust | Swing | Fixed
        FixedExpression?
        IntegerModifier
        WholeDiceModifier?
        FaithPolicy: None | Caster | Target | Both

  | FixedResourceMagnitude
        Resource: HP | MP | CT | Other
        Expression
```

Thrust/Swing rejects a FixedExpression; Fixed requires one. Integer adds and exceptional whole-die
adds remain distinct. Damage magnitude follows
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#magical-power-and-raw-damage)
or the physical weapon owner. Healing follows
[Magic Effects and Persistence](14-magic-effects-and-persistence.md#healing).

`Effects[]` is an ordered typed union. Legal effect kinds include Damage, Healing, ResourceChange,
StatusApplication, StatusRemoval, CTChange, Dispel, Revive, ForcedMovement, Reequip, and an explicit
mechanism-owned effect. An effect declares whether it is the carrier, rider, or independent result.
Every effect that can touch Undead declares `UndeadInteraction`; `Normal` is an explicit value, not
an omitted inference. Revive, healing, drain, status, and instant-KO effects also declare their legal
target states independently from allegiance.
The array order cannot bypass the staged-state and target-batch transaction in
[Action Transactions and Reactions](18-action-transactions-and-reactions.md).

### Transaction and critical profiles

```text
TransactionProfile
    StrikeCount
    CastingRollCardinality: PerAction | None
    TargetGateCardinality: PerAction | PerTarget | PerStrike | Explicit
    MagnitudeRollCardinality: PerTargetPerStrike | PerTarget | Shared | Explicit
    WithinActionApplication: Deferred | Immediate

CriticalProfile
    SuccessEffect: DeliveryDefault | MaximizeOneHealingDie | Explicit | None
    FailureEffect: MissOnly | ExplicitDeterministic
```

`StrikeCount` is a positive integer and belongs to the outer ActionInstance. Physical attack and
active defense are per Strike. The casting draw is normally per action, first classified against
BaseSpellScore for action/resource outcome and then against target-specific TargetSpellScores;
Internal Direct resistance is per target for the full action; magnitude is independently rolled per
target and Strike. Departures are explicit and receive unique random-site identities.

`WithinActionApplication` defaults to Deferred. Immediate setup is legal only when forecast,
capacity scoring, target snapshots, and later-Strike behavior all model it. Reactions never acquire
a per-Strike or per-target window.

Critical delivery defaults are owned by the delivery's combat document. Direct healing uses
MaximizeOneHealingDie. Critical success never modifies MP/HP cost. Failure defaults to MissOnly;
an explicit failure consequence is deterministic and visible, never a universal random table.

### Presentation and AI

```text
PresentationProfile
    ActionAnimation
    ChargePresentation?
    ResultText
    ForecastTerms[]
    StatePresentationIds[]

AiProfile
    LegalUsePolicy
    OutcomeTags[]
    ExpectedValuePolicy
    FriendlyFirePolicy
```

Presentation uses existing FFT assets and the precedence rules in
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md). Every term that can
change a legal player choice appears in forecast or selected-unit detail under
[Player-Facing Information](09-player-facing-information.md).

AI legality, target membership, exact probabilities, expected MP/HP settlement, expected magnitude,
friendly fire, lethal overcasting, and delayed timing use the same normalized profile and planning
path as the player forecast. AI metadata may assign strategic weights; it cannot replace a combat
formula or grant the AI information unavailable to the resolver.

## Normalized persistent-state definition

Every native status overlay or custom DCL state used by an action resolves through one definition:

```text
DclStateDefinition
    Kind
    NativeStatusBit?
    ResistanceGate
    ImmunityFamily
    StackPolicy
    StackKey
    StackCap?
    ContributionKey?
    DurationClock
    DurationFormula
    TickProfile?
    EffectStrengthFormula?
    RemoveOnTargetKo
    RemoveOnSourceKo
    RemoveOnSourceLoss
    CureFamilies[]
    PayloadSchema
    MechanicalRules
    PresentationProfile
    AiProfile
```

The runtime instance schema, source binding, storage lifecycle, visual reuse, and presentation
precedence are owned by
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md#native-and-dcl-storage).
Global duration/tick ordering and stacking policies are owned by
[Magic Effects and Persistence](14-magic-effects-and-persistence.md).

A state with no resistance uses an explicit `ResistanceGate = None`. A permanent/until-command
state uses the corresponding explicit duration clock rather than an absent duration. A state with
no ticks omits TickProfile. Missing data never means immune, permanent, invisible, or harmless.
StackToCap requires StackCap and ContributionKey; other policies reject those fields unless their
own named extension consumes them.

## Defaults and fail-closed validation

Only these defaults are implicit after normalization:

| Field | Default |
| --- | --- |
| BaseMPCost | `0` |
| OvercastPolicy for positive MP cost | `Allowed` |
| OvercastPolicy for zero MP cost | `Forbidden` as a normalized nonapplicable value |
| BaseCastCT / CastCTModifier | `0 / 0` |
| MPCostMultiplier | exact `1` |
| ConsumesAction / ConsumesMovement | `true / false` |
| StrikeCount | `1` |
| WithinActionApplication | `Deferred` |
| ArmorDivisor when ordinary DR applies | `1` |
| LocationPolicy | `NormalCombined` |
| FaithPolicy | `None` |
| WholeDiceModifier | `0` |
| Nonapplicable numeric multiplier | exact `1` |
| Nonapplicable optional effect | absent, never a zero-filled active effect |

All other behavior-bearing enums and booleans are explicit. In particular, the loader does not
infer Source, Delivery, Reflectable, ShellSensitive, VisionRequired, resistance, stacking,
duration, allegiance, area selectivity, status immunity, or Undead interaction from job, animation,
MP consumption, element, formula id, or native field names.

Profile normalization fails closed before battle when:

- a required field is absent or an enum value is unknown;
- mutually exclusive fields are both present;
- a number is negative, non-finite, outside its owning domain, or cannot be parsed exactly;
- an area, persistent effect, fixed expression, resistance, or delivery union is incomplete;
- native data and runtime sidecar revisions do not match;
- forecast, AI, and execution cannot consume the same normalized profile;
- an implementation carrier cannot preserve the required outer-action, target, Strike, or state
  identity.

Failure disables the authored DCL profile with an actionable validation error. It never silently
falls back to a partly vanilla and partly DCL resolution path.

## Authoring acceptance record

An action or state is ready for content assignment only when its authoring record contains:

1. the normalized profile and all referenced state definitions;
2. one worked forecast showing every gate and magnitude term;
3. boundary cases for zero/full resources, min/max range, DR zero/full stop, criticals, and KO when
   those mechanisms apply;
4. multi-target and multi-hit cardinality cases when present;
5. player, AI, and confirmed-execution parity expectations;
6. presentation assets and selected-unit detail;
7. a native-carrier mapping that preserves FFT behavior outside the explicit DCL replacement;
8. a capacity card under
   [Job Tiers, Ability Budgets, and Authoring](16-job-tiers-ability-budgets-and-authoring.md#skill-card)
   before the action enters a job kit.

Final content values remain calibration data. Missing mechanics, undefined defaults, invisible
state, or divergent forecast/AI/execution paths are implementation defects rather than balance
parameters.
