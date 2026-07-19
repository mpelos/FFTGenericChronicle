# Magic Resolution and Defenses

This document owns magical delivery, active-defense legality, Magic Resistance, magical damage,
DR interaction, elements, Shell, Reflect, criticals, and multi-target resolution.

## Delivery classes

The delivery class determines the gates between a successful declaration and the effect:

| Delivery | Casting gate | Target gate | DR |
| --- | --- | --- | --- |
| External Projectile | SpellScore roll | legal active defense | normally applies |
| Internal Direct | Quick Contest | HT, Will, Spiritual Resistance, or authored resistance | normally ignored |
| Beneficial | SpellScore roll | willing target does not resist | ignored unless stated |
| Touch | SpellScore attack | Dodge or Parry as authored | effect-defined |
| Area | one casting roll | per-target avoidance policy | effect-defined |
| Rider | carrier must hit | one resistance roll for the rider | carrier/effect-defined |

The DCL collapses GURPS's separate creation and DX-based Innate Attack rolls for a Missile Spell into
one IQ-based SpellScore roll. External magic already faces active defense and DR; a second attack roll
would add another failure gate without adding a useful FFT decision.

## External Projectile

```text
shared 3d6 succeeds against BaseSpellScore
-> same draw succeeds against this TargetSpellScore
-> critical success bypasses active defense
-> otherwise resolver selects the best legal Dodge or Block
-> on an undefended hit, applicable DR reduces damage
```

External Projectiles are normally Dodgeable and may be Blockable. They are not normally Parryable.
The spell profile must declare Block legality, including whether ordinary shields and Defense Bonus
apply. The spell uses FFT unit tracking rather than physical line of sight or projectile collision.

External spells may receive larger raw damage because they face casting failure, active defense, and
DR.

## Internal Direct

An Internal Direct effect acts on the target without a projectile:

```text
Quick Contest:
shared draw first succeeds against BaseSpellScore
caster then succeeds and records margin using TargetSpellScore
vs
target margin using the declared resistance
```

There is no Dodge, Block, or Parry. Internal damage normally ignores BodyDR and HeadDR. The target
participates through resistance:

| Effect | Usual resistance |
| --- | --- |
| bodily disruption, poison, internal injury | HT |
| mental coercion or loss of voluntary control | Will |
| spiritual curse or hostile supernatural alteration | SpiritualResistance |
| exceptional effect | value declared by the action |

Internal effects receive lower raw damage or a narrower effect than comparable External
Projectiles because they remove active defense and DR. This is an expected-injury tradeoff, not a
universal fixed dice ratio.

The casting roll is owned by the outer ActionInstance. A single-target Internal Direct action uses
that roll in one Quick Contest. An area or other multi-target action reuses the same caster roll and
margin against one independent resistance roll from each affected target; target enumeration never
rerolls the caster. An Internal Direct multi-hit action makes one contest per target for the entire
outer action. Winning authorizes all planned hits against that target, while losing suppresses all
of them. Authorized hits still roll their damage independently. Resistance is not an active defense
and does not create a per-Strike defense expenditure.

## Magic Resistance and retired magical evasion

There is no universal Magic Evasion percentage. Avoidance belongs to the attack's delivery:

- Dodge avoids an evadeable manifestation;
- Block stops a Blockable manifestation;
- HT resists bodily effects;
- Will resists mental effects;
- Spiritual Resistance contests spiritual effects.

```text
SpiritualResistance = Will
                      + JobMagicResistance
                      + EquipmentMagicResistance
                      + explicit status modifiers
```

Faith does not enter SpellScore or this formula through a coarse five-band modifier. A
Faith-sensitive action uses the continuous magnitude rule below or an explicit effect rule.

Legacy magical-evasion fields are reinterpreted by explicit ownership:

| Legacy field | DCL use |
| --- | --- |
| class magical evasion | JobMagicResistance or explicit Dodge-vs-magic modifier |
| shield magical parry | Block/DB against Blockable spells |
| cloak magical evasion | explicit Dodge-vs-magic or EquipmentMagicResistance |
| accessory magical evasion | explicit modifier named by the accessory |

The same value is never applied as both an active defense and Magic Resistance unless the item
explicitly pays for both properties.

## Final success chance

An External Projectile uses the same two-stage probability shape as a physical ranged attack:

```text
A = probability that the shared draw succeeds against both BaseSpellScore and this TargetSpellScore
C = probability that it passes the base gate and is critical against this TargetSpellScore
D = probability of the selected active defense succeeding

FinalSuccessChance = C + (A - C) * (1 - D)
```

If no defense is legal, `FinalSuccessChance = A`. An Internal Direct action instead enumerates the
shared casting draw and target resistance draw and succeeds only when the casting draw passes the
base gate, passes that target's score, and then wins the Quick Contest; the target resists on a tie.
A Beneficial action against a willing target uses the probability of passing both casting-score
gates.

Forecast percentages use exact 3d6 enumeration, including critical and automatic outcomes. They do
not subtract a Magic Evasion percentage from a magical hit percentage. Quick Contest ties, exact
enumeration, and rounding follow the
[Numeric Resolution Contract](17-numeric-resolution-contract.md#quick-contests).

## Magical power and raw damage

Spell accuracy and spell magnitude are separate. A scalable damaging spell treats IQ as its magical
ST and reuses the literal GURPS thrust/swing table owned by
[Damage, Armor, and Injury](05-damage-armor-and-injury.md#canonical-st-damage-table):

```text
DamageBasis = Thrust | Swing | Fixed

if DamageBasis is Thrust or Swing:
    BasicMagicDamage = STDamageTable[IQ][DamageBasis]
else:
    BasicMagicDamage = authored FixedDamageExpression

RawMagicDamage = NormalizeDiceAndAdds(
                     BasicMagicDamage
                     + SpellDamageModifier
                     + FocusDamageModifier
                     + explicit damage modifiers
                 )
```

IQ is an integer lookup characteristic and must satisfy the same table validation as ST. `Thrust`
provides the slower progression expected for lower-magnitude or armor-ignoring effects. `Swing`
provides the stronger progression expected for offensive manifestations. Those are authoring
defaults, not delivery-class locks. `Fixed` stores an explicit `Xd6+Y` and does not scale with IQ.

SpellDamageModifier, FocusDamageModifier, and other ordinary modifiers are integer adds applied
after the table lookup. They use the exact dice-and-add normalization owned by
[Damage, Armor, and Injury](05-damage-armor-and-injury.md#dice-and-add-normalization). An exceptional
spell that adds whole dice declares that property separately rather than overloading an integer
add.

`SpellScore` decides whether delivery succeeds. IQ and the spell's damage profile produce the raw
`Xd6+Y`. Increasing Job Level or tradition Rank does not silently increase both chance and damage
unless a spell explicitly converts margin of success into magnitude. Fixed damage, spell modifier,
MP, CastCT, delivery, resistance, DR, area, and selectivity remain independent balance levers.

## Faith potency and receptivity

Faith uses a continuous factor centered at Faith 50. Permanent and temporary Faith ownership is
defined by
[Character Growth and Job Stat Modifiers](15-character-growth-and-job-stat-modifiers.md#faith-remains-outside-character-growth).

Faith is bounded even though Brave/HT growth is open-ended:

```text
PermanentFaith = clamp(0, 100,
                       RecruitmentFaith + explicit permanent Faith changes)

CurrentFaith = clamp(0, 100,
                     PermanentFaith + temporary battle changes)
```

The clamp occurs after summing each complete expression, not after each modifier. It protects the
continuous factor's numeric domain without creating bands; every in-range Faith point still
changes potency and receptivity.

```text
FaithFactor(F) = 1 + 0.006 * (F - 50)
```

Reference values are:

| Faith | Factor |
| ---: | ---: |
| 0 | 0.70 |
| 10 | 0.76 |
| 30 | 0.88 |
| 50 | 1.00 |
| 70 | 1.12 |
| 90 | 1.24 |
| 100 | 1.30 |

Every Faith point changes the factor; the interface may summarize it at ten-point landmarks. For an
effect that is sensitive to both caster potency and target receptivity:

```text
FaithMagnitude = FaithFactor(casterFaith) * FaithFactor(targetFaith)
```

Thus two Faith-50 units produce `x1.00`, two Faith-70 units produce `x1.2544`, and two Faith-100
units produce `x1.69`. A spell may use only caster or only target Faith, but must declare that policy.
Faith is applied to magnitude once; it is not also hidden in SpellScore, resistance, and duration.
Within the legal domain, one selected factor ranges from `0.70` to `1.30`, and two selected factors
range from `0.49` to `1.69`.

## DR interaction and damage order

Magic declares one armor policy:

| Policy | Rule |
| --- | --- |
| `Manifestation` | Ordinary location DR applies with divisor `1`. |
| `ArmorDividing` | Ordinary location DR applies after the authored positive rational divisor. |
| `InternalSpiritual` | DR is ignored; resistance normally applies. |
| `IgnoreDR` | DR is explicitly bypassed without implying resistance or another delivery gate. |
| `None` | The effect does not enter a DR-bearing damage path. |

Manifestations use the same HeadDR, BodyDR, and combined normal DR as physical damage. There is no
separate magical HeadDR or BodyDR.

The complete order is:

```text
1. roll raw damage dice
2. select location and armor divisor
3. subtract effective DR
4. determine penetrating damage
5. apply damage-type/wound behavior
6. apply elemental affinity
7. apply FaithMagnitude when flagged
8. apply Shell when flagged
9. produce final Injury
```

An Internal effect begins after the DR steps but retains the remaining authored multipliers.

The wound stage first produces the same integer BaseInjury as physical damage. Element, Faith, and
Shell then remain exact rational values until one final magical rounding boundary:

```text
BaseInjury = 0                                      if PenetratingDamage == 0
             max(1, floor(PenetratingDamage
                          * WoundMultiplier))       otherwise

CombinedMagicMultiplier = ElementMultiplier
                          * FaithMagnitude
                          * ShellMultiplier

FinalInjury = 0                                     if BaseInjury == 0
                                                        or CombinedMagicMultiplier == 0
              max(1, floor(BaseInjury
                           * CombinedMagicMultiplier)) otherwise
```

A nonapplicable multiplier is the exact value `1`. The resolver does not floor separately after
element, Faith, and Shell. Positive damage that passed DR therefore remains at least one Injury
unless avoidance, resistance, elemental Null, or Absorb produces no Injury. Absorb leaves this
numeric path through its separate conversion rule rather than passing a negative multiplier into
FinalInjury.

## Elements and Oil

Elemental affinity is evaluated after DR so armor still matters against a manifested element.
Absorb and Null are exclusive route overrides; numeric sources otherwise combine as bounded steps:

```text
if any applicable source grants Absorb:
    TargetAffinity = Absorb
else if any applicable source grants Null:
    TargetAffinity = Null
else:
    AffinityStep = clamp(-1, 2,
                         sum(applicable numeric affinity steps))
```

| Numeric source/category | Step | Resolved multiplier |
| --- | ---: | ---: |
| Halve | `-1` | x0.5 at final step `-1` |
| Normal | `0` | x1 at final step `0` |
| Weak | `+1` | x1.5 at final step `+1` |
| Severe weakness | `+2` | x2 at final step `+2` |

Oil contributes one `+1` step to Fire while present. Elemental Exposure contributes its stored
signed step. Two Weak sources reach Severe weakness; Halve plus one Weak source returns to Normal;
additional resistance never goes below Halve and vulnerability never exceeds Severe weakness.

The source side resolves an optional boost independently:

```text
SourceElementBoost = max(1, every applicable ElementBoostMultiplier)
ElementMultiplier  = SourceElementBoost * TargetAffinityMultiplier
```

Boost multipliers are exact rationals of at least one. Multiple boosts use the strongest value
rather than multiplying with one another. Target affinity and source boost therefore cannot be
double-counted as the same property.

`Absorb` is a conversion path rather than a negative multiplier. After DR and BaseInjury are known,
it computes:

```text
AbsorbedHealing = 0                                  if BaseInjury == 0
                  max(1, floor(BaseInjury
                               * SourceElementBoost
                               * FaithMagnitude
                               * ShellMultiplier))   otherwise

AppliedHealing = min(AbsorbedHealing, MaxHP - CurrentHP)
```

Thus armor that prevents all Penetrating Damage prevents absorption, while Faith and Shell modify
the same elemental energy they would have modified as Injury. Absorb produces HP restoration, not
Injury: it causes no Shock, Major Wound, knockback, concentration incident, or damage-triggered
Reaction. It may satisfy an explicit HP-restoration trigger in the one post-action Reaction window.
It does not use the direct-healing critical rule, does not revive KO, and loses excess above MaxHP.

Oil is a visible one-shot Fire `+1` affinity step. It is consumed only when a fire effect penetrates
and would produce positive Injury after the complete elemental/Faith/Shell scale. A blocked,
dodged, resisted, nullified, absorbed, or fully DR-stopped fire attack does not consume Oil. When
Halve and Oil cancel to Normal, positive fire Injury still consumes Oil because the vulnerability
participated in that resolution.

## Shell

Shell reduces final magical injury after DR and the spell's other offensive multipliers:

```text
ShellMultiplier = 0.70
```

Shell does not also improve Magic Resistance. Separating injury mitigation from resistance avoids
granting two defensive gates through one status. An action declares whether it is Shell-sensitive;
MP consumption alone does not determine that interaction.

## Reflect

Reflect is routing, not evasion or resistance:

- only unit-targeted actions marked `Reflectable` are routed;
- fixed-tile and ordinary ground areas are not Reflectable;
- one action may reflect at most once;
- a reflected action cannot create a reflection loop;
- the deterministic default return target is the original caster;
- the forecast shows the resolved route before confirmation.

Reflect does not reroll the caster's shared casting draw and does not change BaseSpellScore, MP cost,
CastCT, magnitude profile, or area policy. Routing rebinds the target before target-relative
classification, so the new recipient supplies Zodiac compatibility, active defense or resistance,
DR, Faith, affinity, and Shell. Forecast shows that final route and TargetSpellScore before
confirmation.

## Criticals

- An External Projectile critical bypasses active defense but not DR.
- An Internal Quick Contest uses margins; an attack-side critical does not create an automatic win
  against every resistance result.
- A direct-healing critical maximizes exactly one magnitude die under
  [Magic Effects and Persistence](14-magic-effects-and-persistence.md#critical-healing). Other
  beneficial effects receive no universal magnitude or duration bonus.
- Critical success never reduces or refunds MP/HP. This is an intentional GURPS deviation: the
  critical modifies resolution, not the spell's resource economy.
- Critical failure has no universal catastrophe table. An individual spell may declare only an
  explicit, deterministic, and player-visible backlash.

## Multi-target and multi-hit gates

One action makes one casting roll. After that:

- avoidance or resistance is resolved per affected target;
- damage or healing is rolled per target;
- a rider makes one resistance roll per target after its carrier hits;
- multi-hit actions share the casting result and roll damage per strike;
- an External Projectile target receives its legal active defense per strike;
- Internal Direct makes one Quick Contest per target for the complete outer action;
- another delivery must explicitly declare whether its per-target gate is shared or repeated per
  strike.

This avoids rerolling the caster's knowledge for every unit while preserving each defender's own
position, defenses, resistance, DR, Faith, affinity, and statuses.

The area snapshot, target-order independence, staged status effects, KO short-circuit, and Reaction
window are owned by
[Action Transactions and Reactions](18-action-transactions-and-reactions.md).
