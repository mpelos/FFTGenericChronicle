# Facing, Reach, and Targeting

This document owns facing, melee reach, explicit location routing, native FFT physical
line/trajectory legality, height legality, and deterministic awareness.

## Facing

Facing affects active defense rather than raw damage:

| Attack direction | Active-defense rule |
| --- | --- |
| Front | Full legal defense. |
| Side | Defense at `-2`; Block requires the shield side. |
| Back | No active defense. |

The back rule represents lack of awareness without requiring a Perception attribute. A critical hit
also removes the defense roll regardless of direction.

Facing never changes BodyDR or HeadDR. Armor still protects an undefended target.

### Exact arc classification

The native facing enum is decoded into a cardinal unit vector as documented by the engine manual.
For an attacker on a different tile:

```text
AttackVector = AttackerPosition - DefenderPosition
forward      = dot(DefenderFacingVector, AttackVector)
lateral      = cross(DefenderFacingVector, AttackVector)

Front = forward > 0 and forward >= abs(lateral)
Back  = forward < 0 and -forward >= abs(lateral)
Side  = otherwise
```

The `>=` boundary places an exact diagonal symmetrically into the Front or Back cone rather than
Side. `sign(lateral)` identifies the defender's left or right side and therefore whether a shield
occupies the protecting side. Same-tile attacks are outside the DCL because Reach C does not exist.

The native facing byte and its cardinal mapping are engine facts owned by
[Code-Mod Runtime and Formula DSL](../modding/06-code-mod-runtime-dsl.md). Forecast, execution, and
AI use the same classifier.

## Reach

FFT melee weapons use only two Reach values:

| Reach | Targeting |
| ---: | --- |
| 1 | Adjacent tile. |
| 2 | Up to two tiles, subject to path and height legality. |

There is no Reach C and no same-tile close-combat subsystem. Fists, knives, and ordinary one-tile
weapons use Reach 1. Reach 2 is an item advantage paid for through the rest of the weapon profile;
it does not automatically create GURPS grip changes, point-blank penalties, or free stop-hits.

Leaving Reach does not trigger a universal attack of opportunity. An explicit reaction or ability
may provide one.

## Target locations and DR

The physical DCL exposes two armor locations:

- Body, protected by body equipment;
- Head, protected by head equipment.

Normal attacks are abstract attacks against the protected silhouette:

```text
NormalDR = BodyDR + HeadDR
```

Only an attack or skill with an explicit location changes this:

```text
Head-targeting attack -> ApplicableDR = HeadDR
Body-targeting attack -> ApplicableDR = BodyDR
```

A location attack also applies its authored skill penalty and any explicit injury or HT-check
modifier. It does not receive an implicit skull multiplier or universal crippling effect.

Location selection is not a universal command. Only an explicit ability may declare Body or Head,
and that ability owns its skill penalty and additional benefit. There is no global `-4` head-shot
technique.

## Native FFT line and trajectory legality

Physical attacks preserve FFT's existing line-of-sight and projectile-trajectory behavior exactly.
The DCL adds no cover levels, exposure penalty, interception rule, or missed-projectile redirection
to another unit. A target or trajectory rejected by FFT remains illegal; one accepted by FFT gains
no generic cover modifier.

Weapon profiles may retain the native distinction needed to select FFT's direct or arcing route,
but the DCL does not replace either route with a new geometric simulation.

Unit-targeted magic is an explicit exception. It preserves FFT target tracking, performs no
line-of-sight or cover check, and does not use the physical range-penalty table. That complete
contract is owned by
[Casting, Charge, and Magic Targeting](12-casting-charge-and-targeting.md#unit-targets-follow-fft-tracking).

## Height

Height affects legality only through FFT's native range, vertical-tolerance, line, and trajectory
rules. Higher ground grants no generic hit bonus, and attacking upward suffers no generic hit
penalty. An explicit ability may own a height-sensitive modifier as part of that ability.

## Awareness without Perception

Awareness is deterministic:

- front and side attacks are perceived unless a status says otherwise;
- back attacks deny active defense;
- Invisible or an explicit surprise effect may replace this rule;
- no generic Vision or Perception roll is inserted into ordinary combat.

This keeps facing readable and avoids a hidden pre-roll before the attack and defense rolls.

### Blind

Blind represents combat without reliable sight; it does not make the unit unaware of every source.
Facing and the ordinary Front/Side/Back legality remain in force.

```text
Blind source using a VisionRequired attack -> Effective Skill -6
Blind defender using a legal active defense -> Dodge, Parry, or Block -4
```

A Back attack still permits no active defense. The `-4` applies only after a defense is legal; it
does not manufacture a roll against an unseen rear attack.

Every action declares whether its delivery is `VisionRequired`. Ordinary physical attacks and
visually aimed external deliveries are `VisionRequired`. FFT-tracked magic, Internal Direct
effects, self effects, and actions whose targeting does not depend on sight are not penalized unless
their profile explicitly opts in. There is no Perception roll to cancel or soften either penalty.

### Invisibility and target selection

Invisibility preserves FFT's deterministic targeting behavior:

- a hostile unit-targeted action cannot select an Invisible unit directly;
- a tile-centered, fixed-zone, or indiscriminate area can include an Invisible unit without first
  selecting that unit;
- the Invisible unit and its allies may select it normally;
- Movement alone does not end Invisibility;
- the Invisible unit's first confirmed Action ends Invisibility according to the native FFT
  lifecycle.

There is no Reveal, Detection, Perception, True Sight, or equivalent bypass. The status ends only
through its native cancellation/lifecycle rules, including the unit's first Action. Target legality
is deterministic and does not consume a roll.

### First action from Invisibility

There is no intermediate state in which a defender knows about an Invisible attacker. If the first
Action delivers an immediate offensive attack while its source is Invisible:

```text
attack roll          = normal Effective Skill; no Invisibility bonus
legal active defense = none
end of outer Action  = remove Invisibility
```

Every Strike and target belonging to that outer Action uses the same no-active-defense rule. The
source becomes visible only after the complete immediate Action, including every hit, target, and
rider, reaches its commit boundary. Invisibility does not bypass the attack roll, resistance, DR,
immunity, or another non-active-defense gate.

There is no awareness roll, Hearing roll, reduced defense, or “defender knows” branch. The rule is
binary: the Action originates while Invisible and ignores active defense, then the source appears.
A nonoffensive Action still ends Invisibility without creating an offensive benefit.

A delayed action follows FFT's native lifecycle. Declaring a charged Action consumes the first
Action and removes Invisibility at declaration completion. The source is visible while Charging, so
the later delivery uses its ordinary active-defense rules. An immediate `CastCT = 0` delivery occurs
inside the revealing Action and suppresses active defense. Cancellation, interruption, or failure
does not restore Invisibility.
