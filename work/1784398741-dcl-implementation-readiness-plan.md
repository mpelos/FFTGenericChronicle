# DCL implementation-readiness plan

This plan closes the gap between the Deep Combat Layer's approved design direction and an
implementation contract that a developer can execute without inventing rules. The DCL remains the
rules authority; engine facts remain in `docs/modding/`; investigation and completion evidence remain
in `work/`.

## Completion rule

The DCL is implementation-ready only when all of the following are true:

- every combat input has one canonical owner, type, unit, legal domain, default, and validation rule;
- every formula defines every lookup table, constant, rounding point, clamp, and invalid-input result;
- forecast, player execution, AI evaluation, reactions, multi-hit, area effects, and lifecycle effects
  use the same authored action definition;
- every persistent state has storage, application, stacking, expiry, cure, KO/source-loss, AI, and
  presentation behavior;
- item, action, job, growth, and state records have machine-readable schemas with fail-closed defaults;
- the current generators, validators, smoke tests, and implementation-coverage report agree with the
  owning DCL rules;
- one integrated sentinel profile passes offline validation and the documented Windows live-test
  matrix without unresolved design decisions.

Final per-job kits and final balance tuning are not prerequisites for the combat engine. Initial
reference values and complete schemas are prerequisites: a tunable value cannot remain undefined.

Native FFT behavior is inherited by default. A DCL document overrides it only when it names the
replacement explicitly; an inherited engine edge case is not reopened as a design decision merely
because the DCL composes with it.

## Task sequence

### T1 — Baseline and dependency map

Acceptance criteria:

- the full closure sequence and completion rule are recorded;
- each task names its dependencies and verification gate;
- design questions are deferred only to the first task that truly depends on them.

Verification: plan covers physical combat, magic, statuses, progression, jobs, metadata, UI/AI, and
integrated runtime validation.

Dependencies: none.

### T2 — Objective consistency repair

Acceptance criteria:

- approved DCL rules no longer contradict current tools or technical guardrails;
- the encumbrance sign convention is unambiguous;
- validators fail or report an authoring gate instead of asserting superseded design assumptions;
- implementation-coverage ownership points to the current DCL documents, including growth and job
  authoring.

Verification: targeted smoke tests, coverage validation, document-link validation, and repository
searches for superseded assumptions.

Dependencies: T1.

### T3 — Numeric and probability primitives

Acceptance criteria:

- integer, fixed-point, rational multiplier, clamp, and rounding behavior are canonical;
- 3d6 success, critical, Quick Contest, and exact forecast enumeration are canonical;
- forecast/AI evaluation cannot consume execution RNG;
- action, target, strike, status, and damage rolls have deterministic stream ownership.

Verification: worked edge cases and executable reference tests agree.

Dependencies: T2.

### T4 — Action identity and transaction order

Acceptance criteria:

- action, target batch, strike, rider, redirect, reaction, and settlement terminology is exact;
- the complete order from declaration through cleanup is defined;
- KO, damage, status, resource, redirect, and reaction boundaries are unambiguous;
- nested/native carrier actions cannot replace the authored outer identity.

Verification: representative immediate, charged, area, multi-hit, rider, reflect, counter, drain, and
revive traces each have one legal order.

Dependencies: T3.

Approved transaction decisions: areas use a resolution snapshot and plan/commit semantics;
physical multi-hit rolls attack and active defense per strike; FFT's immediate KO/falling behavior
short-circuits later combo effects against that target; statuses default to end-of-action staging;
and the native Reaction phase opens only after every target and strike finishes.

### T5 — CT and turn lifecycle

Acceptance criteria:

- `InitiativeSeed`, `GlobalCTGain`, `TurnThreshold`, eligibility ordering, retained CT, and limits have
  initial values and exact formulas;
- unused Movement/Action and vanilla Wait retention have explicit treatment;
- facing selection and turn-boundary resets are defined;
- Haste, Slow, Quick, Stop, CastCT, and global durations compose deterministically.

Verification: executable timelines cover ties and every temporal status.

Dependencies: T3–T4.

Approved timing decisions: initial CT uses rank normalization; Basic Speed keeps quarter precision;
threshold is 100; normal global gain is 10; Haste and Slow use 15 and 7.5; and every granted turn
resets CT to zero even when no resource is spent.

### T6 — Physical skills, defenses, multi-hit, and reactions

Acceptance criteria:

- `GurpsSkillScore`, Rank 0/defaults, criticals, and defense tie-breaking are complete;
- shield DB applicability and side-facing behavior are exact;
- dual wield and physical multi-hit define rolls, defense spending, damage, reactions, and cancellation
  per strike;
- the global Reaction lifecycle defines trigger, chance, priority, cardinality, target/source, and
  interaction with active defense.

Verification: exact probability and state-transition cases cover melee, ranged, dual wield, repeated
Parry, Block depletion, and reactions.

Dependencies: T3–T5.

Approved defense/Reaction decisions: defense ties prefer Dodge, then Parry, then Block; Reactions use
AutomaticTrigger, SkillResponse, or one authored attribute/Skill ActivationRoll and never raw Brave
percentage; the universal commands are Attack and Action-cost Reequip; Aim and Deceptive Attack
require explicit abilities. Dual Wield permanently bundles DualWeaponTraining, uses full main-hand
skill and off-hand skill at `-4`, lowers off-hand Parry through that score, has no OffHand Training,
resolves as two Strikes in one Action, and opens Reactions only after both.

### T7 — Spatial contract

Acceptance criteria:

- front/side/back and shield-side classification use exact coordinate rules;
- Reach 1/2 path, occupancy, and vertical legality are exact;
- direct/arc trajectory, intervening unit, cover band, and elevation classification are deterministic;
- Blind, Invisible, surprise, and Knocked Down spatial exceptions have owned rules.

Verification: tile fixtures cover every boundary and illegal-target case.

Dependencies: T4–T6.

Approved spatial decisions: physical attacks preserve native FFT LoS, trajectory, range, and height
legality. The DCL adds no cover bands, collision redirection, or generic elevation modifier. Head or
Body targeting and Aim exist only when an explicit ability grants them. Front and Back use symmetric
90-degree cones derived from dot/cross products; exact diagonal boundaries belong to Front or Back,
and the remaining directions are Side. Blind retains facing, applies `-6` to VisionRequired attacks
and `-4` to otherwise legal active defenses, and never inserts a Perception roll. Invisibility
preserves FFT hostile-unit targeting and area inclusion, has no detection bypass, survives Movement,
and ends on the Invisible unit's first Action. An immediate offensive first Action gains no Skill
bonus, suppresses active defense across the complete outer action, and then reveals the source; no
awareness branch exists. Native charge behavior removes Invisibility on declaration, leaving a
delayed delivery visible and normally defendable; cancellation never restores it.

### T8 — Physical damage and equipment

Acceptance criteria:

- ST thrust/swing, dice normalization, minimum damage, DR, divisors, wound multipliers, Shock, Major
  Wound, and all rounding points are complete;
- Basic Lift, Load bands, Jump penalty, and overflow behavior are complete;
- the item schema has one physical DR per item and separates handling, damage mode, damage type, and
  supernatural properties;
- every existing item has a structurally valid fail-closed sidecar row and explicit numeric authoring
  gates.

Verification: reference calculations and sidecar validation cover all item routes.

Dependencies: T3–T7.

Approved damage decision: the DCL omits GURPS blunt trauma and the Flexible/Rigid armor property.
When DR reduces Penetrating Damage to zero, Injury is zero regardless of damage type.
Positive damage adds use the GURPS dice-plus-add normalization: each `+7` becomes `+2d6`, then each
remaining `+4` becomes `+1d6`; negative adds never remove dice.
ST damage uses the literal GURPS 4e table through ST 100 and adds `+1d6` to thrust and swing per full
10 ST above 100; item progression changes weapon modifiers rather than rescaling that curve.
Armor divisor is any exact positive rational value rather than an enumerated whitelist, preserving
the full authoring space for poorly penetrating and armor-defeating skills. Invalid divisors fail
validation, while `IgnoreDR` is a separate armor interaction that sets Effective DR to zero. Every
damage component resolves exactly one armor interaction: a skill-declared divisor or `IgnoreDR`
replaces the weapon divisor; otherwise the weapon divisor is inherited. Divisors never combine.
For a fractional divisor only, zero Applicable DR is treated as DR 1 before division, matching the
GURPS poor-penetration rule; all divisors of one or greater use actual zero DR.
Positive Penetrating Damage always produces at least one Injury after the wound multiplier; only
zero Penetrating Damage produces zero Injury.
Critical replaces FFT's native low-HP threshold: while `CurrentHP > 0` and
`3 × CurrentHP < MaxHP`, fully modified Move and Dodge are halved with ceiling. Critical does not
change Jump, Basic Speed, initiative, or CT gain, and healing re-evaluates it immediately.
Basic Lift uses the fixed `ST² / 5` GURPS curve with exact rational precision; no `LiftScale`
parameter exists, and equipment Weight is calibrated against that curve.
Encumbrance applies the same band multiplier to Basic Move and Base Jump; each result floors once
and has a minimum of one while the load remains inside a defined band.
Extra-heavy is the open-ended maximum band for every Load above `6 × Basic Lift`; encumbrance never
blocks equipment and never reduces Move or Jump below one.
A surviving target that suffers one Injury greater than half MaxHP makes one HT roll; failure
applies Stun and Knocked Down. Reaching 0 HP enters native KO without a Major-Wound roll, and Head
has no universal check modifier beyond one explicitly authored by the skill.
Shock stores all positive Injury received during its active window. Its unit is
`max(1, floor(MaxHP / 10))`, its penalty is `min(3, floor(accumulated Injury / unit))`, and it
affects DX- and IQ-based action rolls rather than active defenses or resistance. The accumulator
clears at the end of the victim's next turn that began with Shock active.
The DCL runs the GURPS basic-damage-versus-ST knockback formula only on critical success and caps
the result at one tile. Crushing is eligible regardless of penetration; cutting is eligible only
when it fails to penetrate; other types are ineligible. A normal success never checks knockback,
while authored forced movement remains a separate skill effect.
Post-knockback balance rolls are omitted: displacement never tests DX and never applies Knocked
Down. Edge movement, landing, and fall damage preserve native FFT spatial behavior.
Unarmed attacks never cause automatic self-injury from the target's DR. Recoil Injury, HP costs,
and other self-effects exist only when explicitly authored by the skill.
Equipment has no durability or quality-based breakage. Parry and critical failure never destroy an
item; temporary weapon disablement exists only through an explicitly authored combat effect.
Parry is illegal when incoming ParryLoad exceeds defender Basic Lift for an unarmed/one-handed
Parry or twice Basic Lift for a two-handed Parry. Skills override ParryLoad; otherwise weapon Weight
or `attacker.ST / 10` for unarmed attacks supplies it. Illegal Parry is removed before defense
selection without spending a use, breaking/dropping equipment, or causing knockback.

Overcasting debits HP as a resource payment rather than Injury. It bypasses the damage pipeline and
does not cause Shock, Major Wound, knockback, or damage-triggered Reactions, while still
re-evaluating Critical and native KO from the resulting current HP.
Every settlement consumes available MP first and replaces the remaining cost one-for-one with HP.
Lethal overcasting is legal: the completed effect commits, then a zero-HP caster enters native KO
before Reactions. Preview and AI show/use the exact MP/HP split, projected pools, and KO outcome.
Declaration stores `ApprovedHPCap = FinalMPCost - min(CurrentMP, FinalMPCost)` after explicit player
confirmation when positive. Current MP at resolution may reduce actual HP payment but never raise it
above that cap; an MP-only declaration has cap zero and fails if MP later becomes insufficient.
Such an insufficiency is `ResourceFailure`: no casting roll, effect, MP payment, HP payment, or
Reaction occurs; Charging ends while the declaration Action and elapsed CastCT remain spent.
An ordinary SpellScore failure pays exactly one resource point when FinalMPCost is positive, or
zero for an explicitly zero-cost action. Settlement still spends current MP first and substitutes
HP only within the ActionInstance's ApprovedHPCap.
Interruption or voluntary cancellation before resolution preserves FFT's resource timing: it
clears Charging without consuming MP or HP. The declaration Action and elapsed CastCT remain spent.
Unlike GURPS Magic, a critical casting success pays the full cost. Critical classification changes
resolution but never reduces or refunds MP or overcasting HP.
Critical casting failure pays the full cost and produces no universal random-catastrophe roll. A
spell-specific backlash exists only when authored explicitly, deterministically, and visibly.
Concentration is an intentional DCL addition to FFT. Hostile Injury and forced movement threaten a
charged cast through a concentration check rather than automatic cancellation; Stun, KO, Silence,
Don't Act, and another effect that prevents continued casting still cancel directly.
The concentration score is `Will - 3 + FocusConcentrationModifier - explicit state penalties`.
Injury magnitude does not scale the fixed penalty and Shock is not applied again; Major Wound and
incapacitating results already provide the severity path.
Concentration incidents are per Strike. A Strike causing positive Injury or nonzero forced
movement creates one check; both together still create one. Zero Injury after DR with no movement
creates none. Direct incapacity clears Charging without a roll. Failure clears Charging immediately
but never stops the remaining combo or creates a Reaction before the one post-action window.
Scalable magical damage uses IQ as magical ST and performs the existing literal ST damage-table
lookup with a spell-authored Thrust or Swing DamageBasis. Fixed stores an explicit non-scaling dice
expression. SpellDamageModifier, FocusDamageModifier, and explicit integer adds apply afterward
through the shared dice-and-add normalization. Job Level and tradition Rank improve SpellScore, not
damage, unless an individual spell explicitly converts margin into magnitude.
Healing parallels that IQ lookup through HealingBasis Thrust, Swing, or Fixed and applies authored
healing adds before rolling. FaithPolicy explicitly selects neither, caster, target, or both Faith
factors. Final healing floors after the selected factor and then caps at missing HP; it bypasses the
entire Injury pipeline. Preview exposes the dice distribution, Faith result, and effective capped
healing.
A direct-healing critical replaces one d6 with a fixed 6 before Faith and the missing-HP cap:
`Xd6+Y -> (X-1)d6+(Y+6)`. Zero-die heals gain no universal bonus. The critical does not also change
duration or cost, and non-healing beneficial effects require an explicit deterministic consequence.
Magical damage first creates integer BaseInjury through the shared post-DR wound rule. Element,
Faith, and Shell multipliers then remain exact rational values and receive one final floor together.
Positive scaled damage retains minimum Injury 1; Null and Absorb are explicit zero/conversion paths.
Absorb converts the post-DR BaseInjury after Faith and Shell into capped HP restoration. It creates
no Injury, damage rider, concentration incident, Oil consumption, or direct-healing critical bonus;
zero penetration heals zero and excess cannot revive or exceed MaxHP.
PermanentFaith and CurrentFaith are each clamped after their complete expression to `0..100`.
FaithFactor therefore remains `0.70..1.30`, or `0.49..1.69` when caster and target factors combine;
every in-range point remains continuous rather than entering a band.
Internal Direct uses one outer casting roll and one independent resistance roll per target. Areas
reuse the caster roll across targets; internal multihit reuses the same contest result for every hit
against that target while rolling hit magnitudes independently. Ties resist, and no active defense
or DR gate is added.
Timed and periodic effects use global CT. Haste, Slow, Stop, and Speed changes do not rescale their
duration or tick count. A tick due exactly at expiry resolves before removal. At one timestamp,
atomic action completion precedes charged delivery, periodic ticks, expiry, and new-turn grants;
stable effect identity orders simultaneous ticks.
Every DCL-governed action and persistent state normalizes into the logical schemas in document 19.
Only safe numeric/transaction defaults are implicit; delivery, source, defense, resistance,
visibility, Faith, Reflect, Shell, area, state lifecycle, presentation, and AI behavior are explicit.
Invalid or revision-mismatched profiles fail closed before battle and never mix partial vanilla and
DCL resolution.
Area CenterMode and AreaDeliveryGate are independent. The gate is exactly None, per-Strike Dodge,
or one Quick Contest per target/action; effect riders retain their own resistance. Dispellable
states store strength from the creating score or a fixed basis plus an authored modifier. One shared
Dispel roll contests each selected instance independently, with ties preserving the effect.
Zodiac uses native Best/Good/Neutral/Bad/Worst categories mapped to TargetSpellScore modifiers
`+2/+1/0/-1/-2`. Multi-target magic shares one caster draw but classifies it against each target's
score, preserving target-specific FFT hit chances and correlated AoE outcomes without repeated
caster rolls.
That draw first passes BaseSpellScore, which alone selects success/failure resource settlement. A
base success is then classified against every TargetSpellScore; target results cannot change cost,
and favorable compatibility cannot rescue a technically failed base cast.
Effective ST/DX/IQ clamp to minimum one after the complete additive expression; HT retains minimum
four. MaxHP and MaxMP clamp to at least one, and Jump has neutral base three. Runtime
maximum-pool decreases clamp current pools without becoming damage, healing, or drain; increases do
not refill them.
Aim retention uses an unpenalized Will-based score plus explicit modifiers after each positive-
Injury Strike. Zero Injury causes no roll; forced movement, Stun, Knocked Down, KO, target loss, or
trajectory loss cancels directly. Failure clears Aim without interrupting the attacking combo.
Applicable MP-cost multipliers are exact positive rationals multiplied once and ceiled at the final
cost; CastCT modifiers are signed integers summed before a zero floor. FinalMPCost and CastCT freeze
at declaration so preview/approval remain stable, while target-relative resolution values remain
live.
Elemental Absorb overrides Null, which overrides a bounded `-1..+2` sum of Halve/Weak/Exposure/Oil
steps. Steps map to `x0.5/x1/x1.5/x2`; Oil supplies one Fire step and is consumed only by positive
Fire Injury. Source element boosts use the strongest exact multiplier and also scale absorbed HP.
Stacking now has exact source/duration ties: Replace swaps the full instance, Refresh keeps payload
and resets duration, StrongestWins replaces only on greater Strength and keeps older owner/later
expiry on ties, StackToCap aggregates independently expiring source contributions, and Independent
creates separate instances.
Persistent growth stores signed 64-bit micro-units at scale one million. Authoring accepts at most
six exact decimal places, never binary floats or silent rounding, and serializes each channel's
remainder across levels and job changes.

### T9 — Magic skill, energy, casting, and targeting

Acceptance criteria:

- tradition skill, spell criticals, Zodiac modifier, MP multipliers, commitment, settlement, and
  overcasting are exact;
- pending-cast storage and all interruption/cancellation cases are exact;
- unit/tile tracking, invalid targets, area geometry, target order, and no-LoS behavior are exact.

Verification: cast traces cover success, failure, criticals, insufficient MP, lethal boundaries,
movement, interruption, target loss, and area membership.

Dependencies: T3–T5 and T7.

### T10 — Magic resolution and effects

Acceptance criteria:

- magical power, damage, healing, Faith, resistance, DR, elements, Shell, Oil, absorb, Reflect, and
  rounding order are complete;
- revive, drain, periodic effects, Dispel, stacking, and Undead interaction have explicit family
  matrices;
- area and magic multi-hit defense/resistance defaults are exact.

Verification: formula vectors and interaction matrices cover every delivery/effect family.

Dependencies: T4, T8, and T9.

### T11 — Native and custom state closure

Acceptance criteria:

- every native status declares its retained lifecycle and DCL mechanical interaction;
- every custom state has a typed payload, cure family, stacking comparator, expiry, KO/source-loss,
  forecast, AI, and presentation contract;
- Overwatch, Cover, Bulwark, Elemental Exposure, Taunt, Fear, Stun, Knocked Down, Guard Broken, Weapon
  Bound, Aim, Unready, and QuickLock have complete edge-case rules;
- presentation feasibility gates are proven or explicitly block shipment.

Verification: state-policy manifest has zero unresolved nature or lifecycle rows.

Dependencies: T4–T10.

### T12 — Character growth and persistence

Acceptance criteria:

- growth steps/costs, Brave pricing, fixed-point scale, overflow, persistence, delevel protection, and
  save migration are exact;
- MaxHP/MaxMP changes define current-pool behavior;
- every job growth/modifier record can be validated against equal budgets;
- level-99 and intermediate envelope simulations are reproducible.

Verification: order-independent mixed-job histories and delevel/relevel cases pass.

Dependencies: T3, T5, T8, and T10.

### T13 — Reproducible job-capacity authoring

Acceptance criteria:

- Action Equivalent has a concrete reference action and benchmark-state dataset;
- scenario, target-importance, time, resource, opportunity, and exposure weights have initial values;
- Command and R/S/M scores are reproducible from the same inputs;
- current job-design process and tier vocabulary reference the current DCL owners without importing
  legacy job kits.

Verification: two independent calculations produce the same score for reference synthetic kits.

Dependencies: T6–T12.

### T14 — Machine-readable schemas and UI/AI contract

Acceptance criteria:

- item, action, job, growth, and state schemas expose every field required by the owning rules;
- defaults fail closed and validation errors identify the missing owner/field;
- UI screens and forecasts have exact data bindings and unavailable-hook behavior;
- AI consumes the same legality, probability, magnitude, resource, and state results as player
  forecast/execution.

Verification: schema fixtures and one representative record per mechanism validate end to end.

Dependencies: T3–T13.

### T15 — Integrated readiness proof

Acceptance criteria:

- all document links, owners, schemas, generated manifests, tests, and coverage rows agree;
- the canonical sentinel profile contains every mechanism without final job/content balance;
- offline tests pass and the Windows live-test matrix has no unresolved implementation gate;
- the DCL completion rule at the top of this plan is satisfied.

Verification: final implementation-readiness audit and integrated regression report.

Dependencies: T1–T14.

## Decision policy

An objective correction proceeds without interruption when it directly follows an already approved
DCL rule. Work pauses for human direction when two or more plausible choices would materially change
gameplay, progression, player information, or content authoring. The blocking task presents the
alternatives, the GURPS reference behavior, the FFT consequences, and one recommended option.
