# Action Transactions and Reactions

This document owns the outer-action transaction: identity, resolution snapshots, target batches,
strikes, staged effects, KO short-circuiting, and the Reaction window. Individual formulas remain
owned by their physical, magical, damage, and status documents.

## Resolution vocabulary

The resolver uses these identities:

```text
ActionInstance  one confirmed use of an action by one source
TargetBatch     the ordered set of targets selected for that ActionInstance
TargetResult    the complete prospective result for one target
Strike          one attack/delivery attempt inside the ActionInstance
Rider           an effect conditional on its carrier's result
ReactionWindow  the native FFT response phase after the ActionInstance finishes
```

`ActionInstanceId`, `TargetUnitKey`, and `StrikeIndex` form the minimum random and diagnostic
identity. A carrier, animation callback, reflected route, or nested native action never replaces the
outer `ActionInstanceId`.

## Declaration and resolution state

Declaration stores action identity and commitments, not a frozen combatant stat sheet:

```text
source identity
action identity and authored profile
target mode and tracked unit or fixed tile
declaration tile and passed range/vertical checks
resource commitment, ApprovedHPCap, and CastCT
outer ActionInstanceId
```

An immediate action proceeds directly to resolution. A charged action retains the stored identity
and targeting metadata while it waits. At resolution it reads the current source and target state,
including attributes, skills, equipment, HP/MP, defenses, Faith, statuses, facing, affinity, DR, and
other applicable modifiers. This preserves FFT's delayed-action behavior: declaration fixes what is
being attempted and where it is routed, while changes before impact can change its result.

Unit tracking, fixed tiles, declaration range, and no-line-of-sight magic are owned by
[Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md).

## Target-batch snapshot

At the start of resolution, after routing such as Reflect, the action constructs one logical
snapshot of:

- the final affected-target set;
- each target's current battlefield and combat state;
- the source state shared by the action;
- every finite defense resource available before the first strike.

Every target still receives an individual result. Hit chance, active defense, resistance, DR,
Faith, affinity, damage, healing, status eligibility, and other target-owned values can therefore
differ within one area.

The snapshot makes an area logically simultaneous. Target enumeration order must not change area
membership or the prospective result of another target. Animation and native writes may be emitted
sequentially, but the resolver first plans the entire target batch and then commits it in a stable
engine order. Effects that explicitly chain, jump, spread, or consume a shared finite resource must
declare their own sequential policy instead of relying on enumeration order.

## Strike resolution

A physical multi-hit action treats every hit as an independent Strike:

1. build that Strike's current Effective Skill;
2. roll its attack roll;
3. on an ordinary success, select and roll one active defense;
4. spend a finite Parry or Block attempt whether that attempted defense succeeds or fails;
5. resolve damage and riders for a landed Strike;
6. resolve any target-local concentration cancellation or check created by that Strike;
7. advance finite defense state before the next Strike.

Attack rolls and active-defense rolls are therefore per Strike. Dodge remains reusable; repeated
Parry penalties and Block availability change between Strikes according to
[Skills and Active Defenses](03-skills-and-active-defenses.md).

A Dual Wield Attack is one outer Action containing one main-hand Strike followed by one off-hand
Strike. Each hand uses its own skill, damage, readiness, and Parry state. Active defense occurs per
Strike; the Reaction window opens only after both Strikes and follows the same KO short-circuit as
every other combo. DualWeaponTraining and the permanent off-hand penalty are owned by
[Skills and Active Defenses](03-skills-and-active-defenses.md#dual-wield-and-off-hand-parry).

Magical multi-hit actions preserve the spell's one casting/knowledge roll and declare which
delivery or target gate is repeated per Strike. Their defaults are owned by
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md).

## KO during a combo

Damage and native KO transition follow FFT behavior. When a target reaches 0 HP during a combo:

- the unit falls immediately and enters the native KO lifecycle;
- the combo's remaining animation may continue;
- later visual hits strike the empty air;
- those later hits perform no attack or defense roll and apply no damage, status, rider, resource
  transfer, or Reaction trigger to the KO target.

This is a target-local short circuit. Other targets in the same outer action continue resolving
from the target-batch plan.

## Effects created inside the action

By default, a status, debuff, posture change, or forced-movement result produced by one Strike is
staged until the outer ActionInstance commits. It does not improve later Strikes in that same
action. This prevents target enumeration and combo order from creating hidden self-combos.

An action may opt into sequential setup only through explicit metadata:

```text
WithinActionApplication = Deferred | Immediate
```

`Deferred` is the default. `Immediate` means the authored effect becomes visible to later Strikes
against the same target and must be included in the ability's capacity score and forecast. Native
damage, HP loss, and the FFT KO transition remain immediate regardless of this metadata. Charging
cancellation and concentration checks are also target-local lifecycle consequences. They resolve
once per originating Strike under
[Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md#disruption-granularity)
without making the originating status or forced movement improve later Strikes.

Invisibility is an outer-action source rule rather than a target debuff. An immediate offensive
Action confirmed while its source is Invisible suppresses active defense for all of its Strikes and
targets, then removes Invisibility after the outer commit. The attack still performs every other
authored gate. Target selection and the complete binary rule are owned by
[Facing, Reach, and Targeting](04-facing-reach-and-targeting.md#first-action-from-invisibility).

Damage-plus-status ordering, native cancellation, and native KO/status interactions preserve FFT's
engine behavior. A native ordering that has not yet been mapped is an implementation proof gate;
the runtime does not invent a replacement order.

## Commit boundary

The outer action follows this transaction shape:

```text
declare and reserve
-> resolve route and target-batch snapshot
-> plan target results and Strikes
-> commit damage, healing, resources, and staged states in native-compatible order
-> complete every target and Strike
-> open the Reaction window
-> settle cleanup and presentation
```

An HP resource payment is committed in the resource portion of this transaction and is not emitted
as a damage or Injury event. It may change the payer's Critical/KO state, but it cannot manufacture
a damage-triggered Reaction window or injury rider.

When successful overcasting consumes the caster's last HP, the already planned action effect still
commits. The caster then enters native KO before the post-action Reaction window; HP does not become
negative and KO does not retroactively cancel the released effect.

Magic `ResourceFailure` occurs before the target-batch snapshot and casting roll. It commits no
effect and no MP/HP debit, clears the pending cast, and cannot open a damage or resource Reaction
window. The declaration Action and elapsed CastCT are not restored.

Forecast and AI evaluation execute the same planning path without committing state and without
consuming execution RNG. Numeric and random ownership are defined by the
[Numeric Resolution Contract](17-numeric-resolution-contract.md).

## Reaction timing and cardinality

Active defenses are part of Strike resolution; Reactions are not. No Reaction resolves between
Strikes or between targets. The Reaction window opens once only after all targets and all Strikes of
the outer ActionInstance finish.

Concentration is not a Reaction. A Strike that injures or forcibly displaces a Charging unit may
test and clear Charging before the next Strike, while the complete attacking action still finishes
before the single Reaction window opens.

Within that window, eligible reactors, cardinality, ordering, source/target binding, and native
reaction dispatch preserve FFT behavior. One outer action never creates extra Reaction windows
merely because it has multiple hits or targets. Native details that are not yet proven remain live
compatibility gates rather than receiving speculative DCL behavior.

## Reaction activation modes

Every Reaction declares exactly one activation mode:

| Mode | Gate |
| --- | --- |
| `AutomaticTrigger` | The declared trigger is sufficient; there is no activation roll. |
| `SkillResponse` | The Reaction's natural attack, skill, resistance, or effect roll is its only gate. |
| `ActivationRoll` | One explicit DX-, HT-, IQ-, Will-, or named-Skill roll gates activation. |

There is no universal raw-Brave percentage and no universal extra HT roll. An `ActivationRoll`
declares exactly one controlling reference plus its explicit modifier; it never selects the best of
several attributes. A learned Reaction does not scale directly from the current job's Job Level. It
can improve only through the attribute or named Skill that its own rule declares.

An `AutomaticTrigger` may have no MP, HP, item, or cooldown cost. Its reliability is paid for by the
single Reaction slot, trigger narrowness, effect strength, acquisition budget, and opportunity cost.
One hundred percent reliability is part of its capacity score rather than a hidden free benefit.

Every Reaction record declares:

```text
Trigger
ActivationMode
ActivationReference?  // required only by ActivationRoll
ActivationModifier
Eligibility and awareness
Source and target binding
Effect and legal delivery
Once-per-window/native cardinality behavior
Costs and finite uses
Failure behavior
Presentation
```

## Universal action choices

`Attack` and `Reequip` are the universal command choices added or retained by the DCL. `Reequip`
consumes Action, leaves Movement available before or after it, and obeys the active job's ordinary
equipment permissions, slots, hands, and inventory rules. Reequipping does not refresh spent Block,
repeated-Parry counters, readiness, or any other defensive resource.

Aim and Deceptive Attack are not universal commands. Accuracy can be activated only by an explicit
ability that grants the Aim preparation state, and any Deceptive-Attack behavior belongs to its
authored ability.
