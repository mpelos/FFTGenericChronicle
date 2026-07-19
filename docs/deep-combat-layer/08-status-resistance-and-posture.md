# Combat Statuses, States, and Presentation

This document owns the DCL combat-state model, resistance ownership, custom-state lifecycle,
status presentation, palette and pose precedence, and the mechanics of Stun, Knockdown, Knocked
Down, Shock, Taunt, and Fear. It also defines the implementation contract for other persistent
combat states that reuse this infrastructure.

Outer-action staging, per-strike application, KO short-circuiting, and the post-action Reaction
window are owned by
[Action Transactions and Reactions](18-action-transactions-and-reactions.md). Status delivery by
magic, duration derived from a resistance margin, global CT duration, and the general stacking
policies are owned by
[Magic Effects and Persistence](14-magic-effects-and-persistence.md). Turn-resource costs for
Ready, Aim, and Stand Up are owned by
[Turns, Movement, and Actions](02-turns-movement-and-actions.md).

The rules in this document are global combat rules. They do not assign a state to a job or assume
that any particular job can apply it.

## State taxonomy

The implementation distinguishes the persistent condition from the event or command that creates
or removes it.

| Category | Examples | Persistence |
| --- | --- | --- |
| Native status | Poison, Haste, Charm, KO | Native 40-bit status state and native lifecycle. |
| DCL status | Stun, Taunt, Fear, Guard Broken | DCL runtime instance with explicit expiry and presentation. |
| Derived health state | Critical | Re-evaluated from current and maximum HP after every HP change. |
| Posture | Knocked Down | Persists until Stand Up or an explicit lift effect. |
| Preparation or stance | Aim, Overwatch, Bulwark, Cover | Persists until its declared action, trigger, cancellation, or expiry. |
| Equipment state | Unready, Weapon Bound | Bound to an equipment slot and removed by its declared reset. |
| Defensive resource | spent Block, repeated-Parry count | Runtime counter reset by the defender's turn. |
| Instant event | Knockdown, Major Wound, Shove | Resolves immediately and does not occupy a status slot. |
| Defeat state | KO | Uses the native FFT death, revive, crystal, and treasure lifecycle. |

`Knockdown` is the event that applies `KnockedDown`. `MajorWound` is an injury event that may apply
Stun and Knocked Down. `KO` is not a second DCL unconscious state. At 0 HP, the native FFT KO
lifecycle begins.

`Shove` and other authored forced-movement events exist only when an ability declares them. The
only universal displacement is the capped critical-only knockback check; the damage-layer boundary
is owned by
[Damage, Armor, and Injury](05-damage-armor-and-injury.md#knockback-boundary).

## Native and DCL storage

### Native status storage

**Proven:** FFT stores its 40 native status flags in five-byte arrays:

| Unit offset | Purpose |
| --- | --- |
| `+0x57..+0x5B` | Innate and equipment status-source bits. |
| `+0x5C..+0x60` | Status-immunity bits. |
| `+0x61..+0x65` | Effective status mirror read by native behavior and presentation. |
| `+0x1EF..+0x1F3` | Durable status-master bits. |

A native status application uses the staged add packet at `+0x1DB..+0x1DF`; a native removal uses
the staged remove packet at `+0x1E0..+0x1E4`. Direct runtime force/cure operations update the durable
master and effective mirror together. Removing an innate or equipment source additionally requires
an intentional change to the source array.

`StatusEffectData.xml` owns native counter, check, cancel, and no-stack flags. The table contains a
fixed 40-slot vocabulary. `uistatuseffect.en.nxd` supplies native status text and icon assets.
`AbilityTypeData.xml` owns action animation, charge effect, and battle text selection.

### Custom DCL state storage

A DCL state does not acquire the semantics of a native status merely because it reuses that
status's icon. Taunt, for example, reuses the Berserk icon asset but is not stored as Berserk and
does not activate native Berserk behavior.

The code mod maintains DCL state instances in a battle-scoped registry. The minimum logical schema
is:

```text
DclStateInstance
    InstanceId
    Kind
    TargetUnitKey
    SourceUnitKey?          // required by source-bound states
    AppliedAtGlobalCT
    AppliedBeforeTurnSerial
    ExpiresAtGlobalCT?      // global-clock effects
    ExpiresAfterTurnSerial? // unit-turn effects
    RemainingUses?
    Strength?
    WinningMargin?
    StackPolicy
    CureFamilies[]
    Payload
    PresentationId
```

`UnitKey` contains the battle generation, unit-table slot, and stable character identity available
for that slot. A raw pointer is not sufficient because the engine may reuse a unit slot or pointer.

The payload is a typed record owned by the state kind. Examples include Taunt/Fear source identity,
Shock penalty, Elemental Exposure element and magnitude, Aim target and accumulated bonus, Weapon
Bound equipment slot, and Cover protected-unit identity.

### Lifecycle invariants

The runtime follows these invariants:

1. Battle start creates a new battle generation and clears all DCL state.
2. Unit spawn or slot reuse clears instances whose `UnitKey` no longer matches.
3. Application resolves immunity, delivery, resistance, and stacking before staging or committing
   state through the outer-action transaction.
4. Forecast and execution use the same committed or prospective state definition.
5. A source-bound state validates its source whenever target legality is evaluated.
6. KO removes states marked `RemoveOnTargetKo` and terminates source-bound states whose source is KO.
7. Crystal, treasure, removal from battle, and battle end clear every DCL state on that unit.
8. Native status changes remain in the native arrays; DCL instances do not overwrite unrelated
   native bits to obtain presentation.

Each state definition declares:

```text
Resistance
Immunity family
Stack policy
Duration clock
KO/source-loss behavior
Cure families
Mechanical payload
AI legality/scoring behavior
Forecast behavior
Icon asset
Pose
Palette
Application animation
```

An instance is invalid if one of these fields affects play but is unspecified.

## Application order

For each prospective target, a status-producing effect resolves in this order:

1. Validate target, state-specific eligibility, and explicit immunity.
2. Resolve the carrier's hit/delivery rule.
3. Resolve the named resistance roll or Quick Contest.
4. Determine duration and `EffectStrength` where applicable.
5. Apply the state-specific stacking policy.
6. Stage or commit the native status packet or DCL state instance at the boundary declared by the
   outer action.
7. Commit the application animation, pose, palette, icon, command restrictions, and forecast state.
8. Expose the same result to AI evaluation.

A rider whose carrier already hit does not roll its carrier a second time. It uses the resistance
rule authored for that rider.

This target-local order does not create Reactions or permit one target's newly applied state to
change another target's planned result. Those transaction boundaries are defined by
[Action Transactions and Reactions](18-action-transactions-and-reactions.md).

## Resistance ownership

| Effect family | Resistance characteristic |
| --- | --- |
| Physical health | HT |
| Mental coercion | Will |
| Magical bodily effect | HT |
| Magical mental effect | Will |
| Magical spiritual effect | Spiritual Resistance |

Typical HT-resisted effects include Poison, disease, physical Stun, Knockdown, and Major-Wound
collapse. Typical Will-resisted effects include Charm, Confuse, imposed Berserk, Fear, Taunt, and
other losses of voluntary control.

Magical source does not replace effect ownership: magical poison still normally tests HT, while a
magical compulsion tests Will. Spiritual Resistance and the removal of universal Magic Evasion are
owned by
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#magic-resistance-and-retired-magical-evasion).

The formulas for HT and Will are owned by
[Attributes and Derived Stats](01-attributes-and-derived-stats.md). The source defines whether the
target makes a resistance roll, a Quick Contest, or no roll.

### Brave as temperament

Raw Brave remains available to a rule that explicitly models temperament. It does not replace Will
as mental resistance.

Taunt and Fear use opposite Brave modifiers:

```text
BraveTemperamentModifier = roundNearest((Brave - 50) / 20)

TauntResistance = Will - BraveTemperamentModifier
FearResistance  = Will + BraveTemperamentModifier
```

High Brave therefore makes a unit harder to frighten but easier to provoke. Low Brave makes a unit
easier to frighten but harder to provoke. Other mental statuses use Will without this modifier
unless their global definition explicitly says otherwise.

`roundNearest` uses the shared
[Numeric Resolution Contract](17-numeric-resolution-contract.md#shared-operations).

## Duration and stacking

Every persistent state selects one clock:

| Clock | Use |
| --- | --- |
| Global CT | Magical durations, periodic effects, and effects that must ignore Haste/Slow turn count. |
| Target turns | Short tactical states tied to the victim's next opportunity to act. |
| Source turns | Stances maintained by their owner. |
| Uses/triggers | Aim discharge, Cover intercept, Block availability, prepared reactions. |
| Explicit command | Ready and Stand Up. |

For a target-turn duration, only a turn that begins with the state active decrements the remaining
turn count at completion. Applying a state during the target's current turn does not consume one of
its turns immediately. Source-turn duration follows the same rule using the source's turns.

The duration rule from a resistance margin and the global CT clock are defined in
[Magic Effects and Persistence](14-magic-effects-and-persistence.md#duration-from-margin).

Each state selects one of the policies owned by
[Magic Effects and Persistence](14-magic-effects-and-persistence.md#stacking-policies). A
state-specific rule below defines any comparison or tie-break required by that policy.

## Native visual vocabulary

The DCL does not require new icon designs. Custom states reference existing status-icon assets.
Reusing an asset does not merge the states' mechanics.

### Available persistent positions and animations

| Asset | Native visual |
| --- | --- |
| Charging | Legs-open preparation position. |
| Casting | Hands raised overhead. |
| Defending | Defensive stance. |
| Performing | Performance stance. |
| Confusion | Arms move up and down in a loop. |
| Critical | Critical-health position. |
| KO | Defeated ground position. |

### Existing blue icons

`Float`, `Reraise`, `Invisibility`, `Regen`, `Protect`, `Shell`, `Haste`, `Atheist`, `Faith`, and
`Reflect` use the existing blue icon family.

### Existing red icons

`Sleep`, `Poison`, `Blindness`, `Oil`, `Stone`, `Confusion`, `Silence`, `Doom`, `Toad`, `Slow`,
`Immobilize`, `Disable`, `Vampire`, `Undead`, `Chicken`, `Charm`, and `Traitor` use the existing red
icon family. Doom additionally supplies the numeric `1`, `2`, and `3` icon assets.

### Existing native palettes

| State | Character palette |
| --- | --- |
| Oil | Dark. |
| Berserk | Orange. |
| Undead | Purple-red. |
| Vampire | Purple. |
| Poison | Green. |

## Presentation resolver

### Icon policy

- No DCL state requires a newly designed icon.
- A custom state references an existing icon asset by semantic presentation id.
- Two simultaneous states may display duplicate icon art. Their state-list rows remain distinct.
- Native status bits are not repurposed solely to display an icon if doing so would activate native
  behavior.
- Selecting a state exposes its real name, mechanics, source when relevant, magnitude, and expiry.
- A map icon is never the sole explanation for a custom restriction.

Presentation references assets symbolically rather than copying a native status bit:

```text
IconAssetRef    = NativeStatusIcon(StatusToken)
PositionAssetRef = NativePosition(PositionToken)
PaletteAssetRef  = NativePalette(StatusToken) | DclPalette(PaletteToken)
```

For example, Taunt stores `NativeStatusIcon(Berserk)` while its mechanical state remains Taunt.
The implementation resolves these references through the loaded status/presentation catalogs and
does not hardcode texture-atlas coordinates into state logic.

### Application animation versus persistent position

The applying action owns its delivery animation. The resulting state owns only the target's entry
feedback and persistent presentation. Different physical, verbal, or magical actions may therefore
apply the same state without being forced to share one source animation.

| State | Target entry feedback | Persistent presentation |
| --- | --- | --- |
| Stun | Existing hit/flinch response, then state transition. | Critical position, Disable icon, pale-yellow palette. |
| Knocked Down | Existing fall/KO transition without entering the KO lifecycle. | KO position and Immobilize icon. |
| Shock | Existing injury response; numeric icon updates after Injury is known. | Doom `1`, `2`, or `3` counter only. |
| Taunt | Brief intense-red palette pulse after successful application. | Berserk icon and intense-red palette. |
| Fear | Brief blue-gray palette pulse after successful application. | Chicken icon and blue-gray palette. |
| Guard Broken | Applying action's ordinary target response. | Disable icon and defense-panel changes. |
| Weapon Bound | Applying action's ordinary target response. | Disable icon and equipment-command changes. |
| Bulwark | Immediate transition to Defending. | Protect icon and Defending position. |
| Aim | Immediate transition to Charging. | Charging position and tracked-target cue. |
| Overwatch | Immediate transition to Charging. | Charging position and threatened-tile cue. |
| Cover/Bodyguard | Immediate transition to Defending. | Protect icon, Defending position, and protected-unit link. |
| Elemental Exposure | Applying action's elemental response. | Oil icon and element palette. |

The KO transition reused by Knocked Down must not write the KO status bit, set HP to zero, start the
death counter, suppress the unit's turn, or invoke crystal/treasure behavior.

### Position precedence

Only one persistent character position is rendered. The highest applicable position wins:

```text
KO
> transformation state
> Knocked Down
> Stun or Confusion
> Critical
> Charging, Casting, Performing, or Defending
> normal
```

An action state that cannot coexist mechanically is cancelled before presentation is resolved.
Knocked Down and Stun cancel concentration-dependent preparations. KO cancels all preparations and
stances.

### Character-palette precedence

Only one character palette is rendered. The highest active entry wins:

| Priority | State | Palette reference |
| ---: | --- | --- |
| 1 | Stun | Pale yellow, reference sRGB `#E9D96E`. |
| 2 | Berserk | Existing orange. |
| 3 | Taunt | Intense red, reference sRGB `#D82020`. |
| 4 | Fear | Blue-gray, reference sRGB `#7D8FA3`. |
| 5 | Vampire | Existing purple. |
| 6 | Undead | Existing purple-red. |
| 7 | Poison | Existing green. |
| 8 | Oil | Existing dark palette. |
| 9 | Elemental Exposure | Palette associated with the stored element. |

Palette precedence is presentation-only. Lower-priority states continue to function, expire, and
display their icons. When a higher-priority state ends, the resolver immediately restores the next
active palette.

Invisible is a transparency/rendering layer rather than an entry in palette precedence. The
transparency is applied after selection of the winning palette. Toad, Stone, and other form/material
changes retain their native presentation layer. KO suppresses temporary DCL palettes while the unit
is defeated.

### Required selected-unit detail

Every custom state row shows:

```text
Display name
Exact mechanical effect
Source unit, if source-bound
Magnitude, if numeric
Remaining CT, turns, uses, or removal command
Stacking behavior
Cure family
```

The movement and action interfaces disable illegal choices before confirmation. The forecast names
every state modifier that changes skill, defense, DR, damage, resistance, target legality, element,
or outcome.

## Stun

Stun represents temporary inability to act coherently. The source names HT or Will as its recovery
characteristic. Physical Major-Wound Stun uses HT.

While Stunned:

- the unit retains its Movement resource;
- the unit cannot spend its normal Action resource;
- Dodge, Parry, and Block each receive `-4`;
- resistance rolls are not penalized by Stun;
- Stun does not create permanent crippling.

At the end of each turn that began with the unit Stunned, roll `3d6` against the named recovery
characteristic. Success removes Stun. Failure preserves it for the next turn. Stun applied during
the unit's current turn therefore cannot recover immediately. There is no automatic maximum
duration and Stun does not stack.

Presentation:

| Channel | Value |
| --- | --- |
| Icon | Existing `Disable`. |
| Position | Existing `Critical`. |
| Palette | Pale yellow, highest palette priority. |
| Detail | `Stun — no Action; defenses -4; recovery: HT/Will N at end of turn.` |

Critical alone uses the Critical position with the normal winning palette. Stun is distinguished by
the Disable icon and pale-yellow palette. If Critical and Stun coexist, the detail list shows both.

## Critical low HP

The DCL replaces FFT's native approximately-one-fifth Critical threshold with the GURPS reeling
threshold. Critical is a derived state rather than a timed status:

```text
Critical = CurrentHP > 0 and 3 * CurrentHP < MaxHP
```

The integer comparison is exact and means strictly less than one-third MaxHP. At exactly one-third,
the unit is not Critical. Reaching 0 HP enters KO instead.

Critical applies after all ordinary Move and Dodge modifiers:

```text
FinalMove  = max(1, ceil(MoveBeforeCritical / 2))
FinalDodge = ceil(DodgeBeforeCritical / 2)
```

It does not modify Jump, Basic Speed, the initiative seed, or global CT gain. Healing immediately
re-evaluates the state; reaching or exceeding one-third MaxHP removes both reductions. Any Reaction
or ability condition that names Critical uses this DCL threshold rather than the retired native
threshold.

Presentation uses the existing Critical position, no icon, and the currently winning palette. Unit
details show `Critical — Move and Dodge halved.` The pose follows the normal precedence table and
does not conceal a higher-priority Stun or Knocked Down presentation.

## Knockdown and Knocked Down

Knockdown is an instant event. On success it applies the `KnockedDown` posture. Sources include:

- failure of the HT check caused by a Major Wound;
- an explicit physical effect;
- a fall or other rule that names Knockdown.

Critical knockback is not a Knockdown source. Its target makes no balance roll and remains in its
current posture after horizontal displacement. Native FFT edge and fall behavior does not silently
apply the DCL Knocked Down posture.

Knocked Down is not Don't Move. The unit remains alive, targetable, and able to crawl or Stand Up.

While Knocked Down:

| Rule | Effect |
| --- | ---: |
| Dodge | `-3` |
| Parry | `-2` |
| Block | Unavailable |
| Melee attack | `-4` Weapon Skill |
| Movement | Crawl up to 1 tile |
| Enemy melee attack | Reference bonus `+2` |
| Enemy ranged attack | `-2` Ranged Skill |

A weapon may declare that it cannot be used while Knocked Down. The
[Stand Up action](02-turns-movement-and-actions.md#stand-up) removes the posture and owns its Action
and Movement cost. A posture-specific lift effect may also remove it explicitly. Ordinary Dispel or
Esuna does not remove a physical posture.

Presentation:

| Channel | Value |
| --- | --- |
| Icon | Existing `Immobilize`. |
| Position | Existing `KO`. |
| Palette | None. |
| Command | Stand Up is exposed whenever legal. |
| Detail | `Knocked Down — crawl 1; Stand Up consumes Action and Movement.` |

Native KO is distinguished by 0 HP, the native death counter, the absence of a normal turn, and the
absence of the Immobilize icon. If a Knocked Down unit reaches 0 HP, KO presentation and lifecycle
replace Knocked Down. An injury that reaches 0 HP enters KO directly and never makes the
Major-Wound HT check that could apply this posture.

## Shock

Each injury may apply short-lived Shock:

```text
if Injury > 0:
    ShockUnit             = max(1, floor(MaxHP / 10))
    UnexpiredShockInjury  = UnexpiredShockInjury + Injury
    ShockPenalty          = min(3, floor(UnexpiredShockInjury / ShockUnit))
```

Shock reduces DX- and IQ-based action rolls through the end of the victim's next turn that begins
with Shock active. It does not reduce active defenses or resistance checks. Shock clears at the end
of that turn. Injuries received while the same Shock window is active accumulate, including
independent strikes of a multi-hit action. The stored accumulator is Injury rather than an already
rounded penalty, so multiple smaller wounds may cross a ShockUnit boundary together. The penalty
never exceeds `-3`. Rolled Damage that produces zero Injury after DR and wound conversion adds no
Shock.

Examples:

```text
MaxHP 15 -> ShockUnit 1
MaxHP 20 -> ShockUnit 2
MaxHP 80 -> ShockUnit 8
```

For MaxHP 80, accumulated Injury 7 produces no Shock penalty, 8 produces `-1`, 16 produces `-2`,
and 24 or more produces the capped `-3`.

Presentation:

| Penalty | Icon |
| ---: | --- |
| `-1` | Existing Doom counter `1`. |
| `-2` | Existing Doom counter `2`. |
| `-3` | Existing Doom counter `3`. |

Shock uses no persistent position or palette. The forecast and skill breakdown display the signed
penalty and expiry. No numeric icon is displayed while accumulated Injury remains below ShockUnit;
the accumulator still remains active until the window expires. The general Doom icon is not
displayed for Shock.

## Taunt

Taunt is a source-bound mental compulsion. For one turn it restricts the unit to the universal
normal `Attack` command against the provocateur, without handing control of the unit to AI and
without converting the unit to Berserk.

Application tests the source's authored Taunt score against `TauntResistance`. The resulting state
stores the provoking `SourceUnitKey`.

While Taunted:

- Movement remains legal;
- the only legal use of the Action resource is the universal normal `Attack` command with the
  provocateur as its sole target;
- action skills, spells, items, Reequip, Ready, Stand Up, defensive commands, self-targeted actions,
  allied actions, and attacks against any other unit are illegal;
- if Movement does not produce a position from which the provocateur is a legal normal-Attack
  target, the unit cannot spend its Action resource that turn;
- the player still controls Movement and facing; Taunt never asks AI to choose a path or action.

Taunt ends when:

- the unit completes the first turn that began with Taunt active, whether it attacked or lost its
  Action;
- the provocateur is KO, removed from battle, or no longer has a valid `UnitKey`;
- a matching mental cleanse removes it.

Taunt uses `Replace`: a successful new Taunt replaces the previous source and resets the one-turn
duration. A failed application does not disturb the existing instance. Applying Taunt during the
victim's current turn does not consume that turn; the duration is the next turn that begins with
Taunt active, following the target-turn clock rule.

Presentation:

| Channel | Value |
| --- | --- |
| Icon | Existing `Berserk`. |
| Position | None. |
| Palette | Intense red. |
| Source cue | Selecting the victim highlights the provocateur with the existing target outline. |
| Targeting | Every Action except normal Attack against the provocateur is disabled. |
| Detail | `Taunted by NAME — this turn: only normal Attack against NAME.` |

Native Berserk retains its existing orange palette and loss-of-control behavior. The palette and
detail text distinguish it from Taunt even though the icon asset is shared.

## Fear

Fear is a source-bound mental state that constrains voluntary approach while preserving control of
the unit.

Application tests the source's authored Fear score against `FearResistance`. The state stores the
frightening `SourceUnitKey` and its winning margin.

While Feared:

- a voluntary Movement endpoint must be at least three horizontal tiles from every active enemy;
- for each enemy, distance is `abs(enemyX - endpointX) + abs(enemyY - endpointY)` and ignores
  height, so an endpoint at distance `0`, `1`, or `2` is illegal;
- the restriction tests only the final tile: a legal path may pass within two tiles of an enemy;
- voluntary teleport and relocation obey the same endpoint rule;
- Movement remains optional and is never selected by AI on the player's behalf;
- if no reachable endpoint satisfies the rule, the Movement command has no legal destination and
  the unit remains on its current tile;
- hostile actions against the fear source receive `-2` Effective Skill;
- hostile actions against other targets are unaffected;
- the unit remains under its normal controller and is never forced to flee.

Fear ends after the victim completes its second turn, when the source is KO/removed/invalid, or when
a matching mental cleanse removes it.

Fear uses `StrongestWins`. The instance with the larger winning margin supplies the source and
duration; a tie keeps the instance with the longer remaining duration. Multiple sources never
create simultaneous movement constraints.

Presentation:

| Channel | Value |
| --- | --- |
| Icon | Existing `Chicken`. |
| Position | None. |
| Palette | Blue-gray. |
| Source cue | Selecting the victim highlights the fear source. |
| Movement | Every destination within two tiles of any enemy is disabled in the movement preview. |
| Forecast | Attacks against the source show `Fear -2`. |
| Detail | `Feared by NAME — Movement must end 3+ tiles from every enemy; attacks against NAME -2.` |

Native Chicken retains its native behavior. The humanoid unit, blue-gray palette, source cue, and
detail text distinguish Fear from Chicken.

## Don't Act and Don't Move

Don't Act and Don't Move remain temporary action restrictions rather than anatomical crippling:

- Don't Act removes the Action resource while leaving legal Movement;
- Don't Move removes the Movement resource while leaving legal Action;
- neither applies Knocked Down;
- a source may combine one with Stun or Knocked Down only if every state remains visible.

Their existing Disable and Immobilize presentation remains native. Stun and Knocked Down reuse
those icon assets without acquiring these native semantics.

## Guard Broken

Guard Broken is a target-turn DCL status that weakens only authored guard channels. Dodge is never
affected implicitly.

The state payload is:

```text
SuppressBlock
ParryPenalty
ExpiresAfterTurnSerial
```

The applying effect must provide `SuppressBlock` and an integer `ParryPenalty`; the state engine
does not invent a magnitude. The state uses `StrongestWins`, comparing Block suppression first and
then absolute Parry penalty. It expires at the declared target-turn boundary or through a matching
martial cleanse.

Presentation:

- existing `Disable` icon;
- no position or palette;
- Block and Parry entries show their exact disabled/penalized result;
- Dodge remains unchanged and visible;
- the forecast names `Guard Broken` beside each affected defense.

## Weapon Bound

Weapon Bound is an equipment-slot state. It never disables an unrelated weapon, shield, spell, or
unarmed action.

The payload is:

```text
EquipmentSlot
SuppressAttack
SuppressParry
SuppressWeaponReactions
WeaponSkillPenalty
ExpiresAfterTurnSerial
```

The applying effect must author every suppression flag and the signed Weapon Skill penalty.
`StrongestWins` applies per equipment slot. Removing or replacing the affected equipment clears the
instance unless the source explicitly transfers it to the replacement slot.

Presentation uses the existing `Disable` icon, no position, and no palette. The affected weapon and
its commands are disabled or display the exact penalty. Identical Disable icons may coexist for
Stun, Guard Broken, and Weapon Bound; their detail rows remain separate.

Weapon Bound and Unready are temporary combat states, not item durability. They never mark an item
as broken, require post-battle repair, or destroy an inventory entry.

## Bulwark

Bulwark is a self-owned defensive stance. Its payload is:

```text
BlockModifier
DrModifier
DisplacementResistance
PassabilityPolicy
ExpiresAfterSourceTurnSerial
```

The source action authors the numeric modifiers and passability policy. Moving voluntarily, becoming
Knocked Down, Stun that cancels a stance, or KO ends Bulwark. Reapplication uses `Refresh` unless the
source defines `StrongestWins` for magnitude.

Presentation uses the existing Protect icon and Defending position. Selection and path preview show
the passability rule and exact defensive modifiers. There is no palette.

## Aim

Aim mechanics, target tracking, accumulated Accuracy, movement restriction, and cancellation are
owned by [Ranged Combat](07-ranged-combat.md#accuracy-and-aim).

Presentation uses the Charging position, no custom icon, and no palette. The existing target cursor
marks the tracked unit. Selected-unit detail shows the target, accumulated Aim bonus, and every
cancellation condition.

## Overwatch

Overwatch is a prepared-action state. Its payload is:

```text
WeaponSlot
TriggerArcOrTiles
TriggerCondition
RemainingTriggers
ExpiresAfterSourceTurnSerial
```

The prepared action reserves its Action when declared. Movement, weapon invalidation, Stun,
Knocked Down, KO, or expiration cancels it. Trigger resolution validates the weapon, source, target,
range, trajectory, and remaining trigger count again before firing.

Presentation uses the Charging position, no custom icon, and no palette. Existing tile highlighting
shows the threatened arc or tiles. Selected-unit detail shows the trigger and expiry.

## Cover and Bodyguard

Cover/Bodyguard is a source-owned protection state bound to one protected unit. Its payload is:

```text
ProtectedUnitKey
EligibleDeliveryClasses
AdjacencyOrRangeRule
RemainingIntercepts
ExpiresAfterSourceTurnSerial
```

The redirect validates both unit keys, range/adjacency, delivery class, source ability to receive
the hit, and remaining intercepts at trigger time. Invalid protection ends without redirecting the
attack.

Presentation uses the existing Protect icon and Defending position, with no palette. Selecting
either unit highlights both members of the link with existing target outlines. Detail text names the
other unit, eligible attacks, remaining intercepts, and expiry.

## Elemental Exposure

Elemental Exposure is a typed vulnerability state:

```text
Element
AffinityStep          // normally +1 or +2
ExpiresAtGlobalCT or ExpiresAfterTurnSerial
```

AffinityStep is a signed integer consumed by the bounded affinity calculation in
[Magic Resolution and Defenses](13-magic-resolution-and-defenses.md#elements-and-oil). Exposure is
normally a vulnerability and therefore authors `+1` or `+2`; a resistance effect uses its own named
state rather than disguising a negative step as Exposure. The applying effect selects exactly one
duration clock. Instances use `StrongestWins` per element; different elements are independent only
when the source explicitly allows multi-element exposure. The damage forecast shows the stored
element and step before confirmation.

Presentation uses the existing Oil icon, no position, and the character palette associated with the
stored element. Elemental Exposure has the lowest palette priority. The implementation uses the
closest available palette to these references:

| Element family | Palette reference |
| --- | --- |
| Fire | Red-orange. |
| Ice | Cyan. |
| Lightning | Yellow. |
| Water | Blue. |
| Earth | Ochre-brown. |
| Wind | Teal. |
| Holy | White-gold. |
| Dark | Black-purple. |

## Ready, Unready, and temporary Parry loss

Ready/Unready and Unbalanced weapon rules are owned by
[Turns, Movement, and Actions](02-turns-movement-and-actions.md#ready). Their status presentation
uses no map position or palette. The selected equipment row shows Ready/Unready, the Attack command
is disabled when required, and Parry shows why it is unavailable. The existing Disable icon may be
used in equipment detail; it is not required above the unit.

## Block and repeated-Parry state

Spent Block and repeated-Parry counters are defensive resources, not ailments. Their mechanics and
reset are owned by
[Skills and Active Defenses](03-skills-and-active-defenses.md#defense-reset).

They use no status icon, position, or palette. The defensive panel and forecast show Block as
available/spent and show the exact repeated-Parry penalty. The player never infers these resources
from animation alone.

## QuickLock

QuickLock mechanics are owned by
[Magic Effects and Persistence](14-magic-effects-and-persistence.md#quick-and-stop). It is a technical
turn-state guard rather than an ailment.

QuickLock uses no character position or palette. The granted turn and lock appear in the CT
timeline with the existing Haste icon asset. Quick targeting explains why a locked target is
ineligible.

## Poison, disease, and awareness statuses

HT resists initial physical Poison/disease infliction and any explicit recovery check. Armor DR
does not improve HT. Equipment immunity prevents the named status before a resistance roll.

Blind uses the fixed attack and active-defense penalties owned by
[Facing, Reach, and Targeting](04-facing-reach-and-targeting.md#blind) without replacing facing
awareness. Invisibility uses FFT target selection, has no detection subsystem, and ends on the
Invisible unit's first Action as defined by
[Facing, Reach, and Targeting](04-facing-reach-and-targeting.md#invisibility-and-target-selection).
An immediate offensive first Action receives no active-defense roll and then removes Invisibility;
it receives no attack-skill bonus. There is no general Perception roll or awareness branch.

## Implementation acceptance contract

A state implementation is complete only when all of these agree:

- forecast and execution mechanics;
- player and AI target/movement legality;
- resistance, immunity, stacking, duration, and cure behavior;
- battle start, unit reuse, KO, source loss, revive, and battle-end cleanup;
- native status arrays or DCL registry ownership;
- icon, position, palette, application animation, and selected-unit detail;
- command disablement and exact forecast breakdown;
- deterministic serialization of the instance inside the battle runtime.

**Proven:** native status arrays and native status application/removal are writable runtime
surfaces. **Strong:** existing animation and icon assets can be selected through their native data
owners. **Hypothesis:** arbitrary custom state rows, duplicate reused icons, palette selection, and
source-link highlighting require dedicated UI/render integration. These presentation hooks must be
validated before a custom state is considered shippable; invisible fallback mechanics are not
accepted.
