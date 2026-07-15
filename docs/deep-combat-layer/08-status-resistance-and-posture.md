# Status Resistance and Posture

This document owns physical and mental resistance characteristics, Stun, Knockdown, Prone, Stand
Up, and the distinction between posture and temporary action restrictions.

## Resistance ownership

| Effect family | Resistance characteristic |
| --- | --- |
| Physical health | HT |
| Mental coercion | Will |
| Magical | Outside this physical layer |

Typical HT-resisted effects include Poison, disease, physical Stun, Knockdown, and Major-Wound
collapse. Typical Will-resisted effects include Charm, Confuse, imposed Berserk, Fear, Taunt, and
other loss of voluntary control.

The formulas for HT and Will are owned by
[Attributes and Derived Stats](01-attributes-and-derived-stats.md).

The inflicting action defines whether the target makes a resistance roll, a Quick Contest, or no
roll. Delivery method does not silently change a physical affliction into a mental one.

## Brave and reactions

Raw Brave remains available to a reaction whose authored trigger is a Brave percentage. That roll
is distinct from the converted HT score. A reaction must declare whether it uses raw Brave, HT,
Will, a flat chance, or no random gate.

## Stun

Stun represents a temporary inability to act coherently. Its source defines whether HT or Will
resists it. Physical Major-Wound Stun uses HT.

A Stunned unit cannot take its normal Action and receives `-4` to active defenses until it
recovers. Recovery uses the characteristic named by the source. Stun is temporary and does not
create permanent crippling.

## Knockdown

Knockdown places the unit in the Prone posture. It may come from:

- failure of the HT check caused by a Major Wound;
- an explicit weapon or skill effect;
- a fall or other physical rule that names Knockdown.

Knockdown is not identical to Don't Move. It changes posture and grants the option to Stand Up.

## Prone

While Prone:

| Rule | Effect |
| --- | ---: |
| Dodge | -3 |
| Parry | -2 |
| Block | Unavailable |
| Melee attack | -4 Weapon Skill |
| Movement | Crawl up to 1 tile |
| Enemy melee attack | Reference bonus +2 |
| Enemy ranged attack | -2 Ranged Skill |

The unit remains alive, targetable, and subject to normal facing and equipment rules. A weapon may
declare that it cannot be used from Prone.

Prone must have a visible posture/animation and status indicator. It cannot exist only as hidden
modifiers.

## Stand Up

The [Stand Up action](02-turns-movement-and-actions.md#stand-up) removes Prone. Its resource cost and
the point at which standing defenses return are defined with the turn economy.

## Don't Act and Don't Move

Don't Act and Don't Move remain temporary skill/status effects rather than anatomical crippling:

- Don't Act removes the Action resource while leaving legal Movement;
- Don't Move removes the Movement resource while leaving legal Action;
- neither automatically applies Prone;
- a source may explicitly combine one of them with Stun or Prone, but the UI must show every state.

## Poison and disease

HT resists initial physical infliction and any explicit recovery check. Armor DR does not improve HT
and does not make the wearer biologically resistant. Equipment immunity still prevents the named
status before a resistance roll.

## Awareness statuses

Blind, Invisible, or an explicit surprise state may change deterministic facing awareness. There is
no general Perception roll. The source must state how it affects attack legality, Effective Skill,
or active defense.
