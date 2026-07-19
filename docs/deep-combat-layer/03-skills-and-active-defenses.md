# Skills and Active Defenses

This document owns DX-based physical skills, job training, attack rolls, criticals, Dodge, Parry,
and Block.

## Skill construction

Every physical weapon family has a GURPS-style skill with an intrinsic Difficulty. The active job
supplies training Rank through its aptitude Tier and Job Level:

```text
Rank        = JobRankTable[aptitudeTier][jobLevel]
WeaponSkill = GurpsSkillScore(DX, weaponDifficulty, Rank)
              + explicit skill modifiers
```

Difficulty belongs to the weapon skill, not the job. A Bow remains Average and a Crossbow remains
Easy regardless of which job equips it. The job controls how much training it supplies.

### FFT weapon-family skills

Every equippable FFT family maps to one DCL skill. The GURPS reference identifies the handling model;
Difficulty is the value used by `GurpsSkillScore`.

| FFT family | DCL / GURPS reference skill | Difficulty |
| --- | --- | --- |
| Unarmed | Brawling | Easy |
| Knife | Knife | Easy |
| Ninja Blade | Shortsword | Average |
| Sword | Broadsword | Average |
| Knight's Sword | Broadsword | Average |
| Fell Sword | Broadsword | Average |
| Katana | Broadsword or Two-Handed Sword, selected by item hands | Average |
| Axe | Axe/Mace | Average |
| Flail | Flail | Hard |
| Rod | Smallsword | Average |
| Staff | Staff | Average |
| Pole | Staff | Average |
| Polearm | Spear | Average |
| Crossbow | Crossbow | Easy |
| Bow | Bow | Average |
| Gun | Guns | Easy |
| Book | Axe/Mace handling | Average |
| Instrument | Innate Attack (Projectile) handling | Easy |
| Bag | Flail handling | Hard |
| Cloth | Whip | Average |
| Shield | Shield | Easy |

An ability may name a different skill when its technique genuinely changes handling. For example,
a formal unarmed martial-arts ability may use Karate (Hard) instead of ordinary Brawling. Throwing
an item uses the relevant Thrown Weapon skill rather than silently reusing its melee skill.

## Difficulty and Rank

Rank represents the standard GURPS point-investment bands:

| Rank | Equivalent investment |
| ---: | ---: |
| 0 | untrained/default |
| 1 | 1 point |
| 2 | 2 points |
| 3 | 4 points |
| 4 | 8 points |
| 5 | 12 points |
| 6 | 16 points |
| 7 | 20 points |

The relative score is:

| Investment | Easy | Average | Hard | Very Hard |
| ---: | ---: | ---: | ---: | ---: |
| 1 | DX | DX-1 | DX-2 | DX-3 |
| 2 | DX+1 | DX | DX-1 | DX-2 |
| 4 | DX+2 | DX+1 | DX | DX-1 |
| 8 | DX+3 | DX+2 | DX+1 | DX |
| each additional 4 | +1 | +1 | +1 | +1 |

Rank may advance at a Job Level without crossing an integer score breakpoint. Those plateaus are a
property of the increasing GURPS training cost, not dead Job Levels.

## Aptitude tiers and Job Level

Each job declares a Tier `A` through `E` for every weapon family it can use and for Shield when it
can equip shields. The shared Rank schedule is:

| Tier / Job Level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| A | 1 | 2 | 2 | 3 | 3 | 4 | 5 | 6 |
| B | 1 | 1 | 2 | 2 | 3 | 3 | 4 | 5 |
| C | 1 | 1 | 1 | 2 | 2 | 3 | 3 | 4 |
| D | 1 | 1 | 1 | 1 | 2 | 2 | 3 | 3 |
| E | 1 | 1 | 1 | 1 | 1 | 2 | 2 | 2 |

Thus every legal proficiency begins with at least one point of training. At Job Level 8, Tiers A
through E represent 16, 12, 8, 4, and 2 equivalent GURPS points. For an Average skill, those final
scores are respectively `DX+4`, `DX+3`, `DX+2`, `DX+1`, and `DX`. Difficulty then shifts every tier
without changing the spacing between them.

Rank never decreases, a lower aptitude never overtakes a higher aptitude at equal Job Level, and
every Tier improves at least once. Job Level changes training Rank; Character Level changes DX
through the active job's growth vector in
[Character Growth and Job Stat Modifiers](15-character-growth-and-job-stat-modifiers.md). Rank 0 is
reserved for an explicitly permitted untrained/default use.

## Shield Skill

Every shield-capable job has an underlying Shield aptitude and Rank:

```text
ShieldRank  = JobRankTable[shieldTier][jobLevel]
ShieldSkill = GurpsSkillScore(DX, shieldDifficulty, ShieldRank)
              + explicit skill modifiers
```

The shield item modifies Block quality; it does not replace the wielder's Shield Skill.

## Attack roll

An attack builds Effective Skill from the trained score and situational modifiers:

```text
EffectiveSkill = WeaponSkill
                 + explicit ability/facing modifiers
                 - range/location/state penalties
```

The attack succeeds on:

```text
3d6 <= EffectiveSkill
```

Effective Skill is not capped at 16 before modifiers. Skill above 16 absorbs range, explicit target
location, Shock, and other penalties. Natural 17 and 18 still prevent automatic success.

## Critical success and failure

- 3 or 4 is a critical hit.
- 5 is critical when Effective Skill is at least 15.
- 6 is critical when Effective Skill is at least 16.
- A critical hit bypasses the active-defense roll.
- 18 is a critical failure; 17 is also a critical failure when Effective Skill is 15 or less.
- A roll at least 10 above Effective Skill is also a critical failure.
- A physical critical failure is a miss unless the specific weapon or ability declares another
  visible consequence.

## One defense per attack

After a noncritical successful attack, the defender receives one legal active defense. FFT does not
pause for a manual choice, so the resolver selects the highest legal current defense, including
usage penalties and facing restrictions. It never rolls Dodge, Parry, and Block sequentially for
the same strike.

Equal current scores use this deterministic priority:

```text
Dodge > Parry > Block
```

The priority preserves finite Parry and Block resources when a reusable Dodge is equally effective.
Active defense is selected and rolled separately for every Strike. Physical multi-hit sequencing is
owned by
[Action Transactions and Reactions](18-action-transactions-and-reactions.md#strike-resolution).

The selected defense succeeds on `3d6 <= current defense score`. A natural 3 or 4 succeeds even
when the modified score is lower; 17 and 18 always fail. A defense score is therefore never a flat
percentage and is not subtracted directly from attack chance. Automatic outcomes and critical
classification are owned by the
[Numeric Resolution Contract](17-numeric-resolution-contract.md#universal-3d6-success-roll).

## Dodge

```text
DodgeBeforeCritical = BaseDodge
                      + job/equipment bonuses
                      - encumbrance
                      - posture/state penalties
```

Dodge may be attempted against every attack for which the defender is aware and physically able to
evade. It does not deplete with use. The final Critical modifier, threshold, and presentation are
owned by
[Combat Statuses, States, and Presentation](08-status-resistance-and-posture.md#critical-low-hp).

## Parry

```text
Parry = floor(WeaponSkill / 2) + 3
        + weapon Parry modifier
        + legal shield DB
        - repeated-Parry penalty
        - state penalties
```

The first Parry with a given weapon or limb is unpenalized. Each additional Parry with that same
weapon or limb before the defender's next turn receives a cumulative `-4`. A skill, weapon family,
or ability may explicitly replace that repeated-Parry step.

An Unready weapon cannot Parry. An Unbalanced weapon cannot Parry after attacking until its
wielder's next turn.

Parry never rolls for weapon breakage. Weapons have no durability or quality-based structural
damage in the DCL; a successful or failed Parry leaves the equipment intact unless the incoming
ability declares a separate temporary equipment-state effect.

### Heavy-attack Parry limit

Every Parry-eligible physical attack resolves one exact nonnegative `IncomingParryLoad`:

```text
IncomingParryLoad = skill.ParryLoad if declared
                    otherwise attackingWeapon.Weight if a weapon supplies the strike
                    otherwise attacker.ST / 10 for an unarmed strike

ParryLimit = defender.BasicLift       for an unarmed or one-handed Parry
             2 * defender.BasicLift   for a two-handed Parry

ParryLegal = IncomingParryLoad <= ParryLimit
```

A skill override replaces rather than adds to weapon Weight. An explicit slam, rush, or
supernaturally heavy effect declares its own ParryLoad; it does not acquire one from visual scale.
The comparison retains exact rational precision.

When the load exceeds the limit, Parry is removed from the candidate defenses before the automatic
highest-defense selection. No Parry roll occurs and no finite Parry use is spent. Dodge and Block
remain legal if their own rules allow them. The defender does not drop or break a weapon and does
not suffer automatic knockback merely because Parry was illegal.

Ordinary missile attacks cannot be parried.

### Dual Wield and off-hand Parry

The `DualWield` capability includes `DualWeaponTraining`. This removes the common `-4` penalty for
making a coordinated two-weapon attack; it does not remove the off-hand handling penalty.

```text
MainHandWeaponSkill = WeaponSkill for the main-hand weapon
OffHandWeaponSkill  = WeaponSkill for the off-hand weapon - 4

MainHandParry = floor(MainHandWeaponSkill / 2) + 3 + applicable modifiers
OffHandParry  = floor(OffHandWeaponSkill / 2) + 3 + applicable modifiers
```

For equal underlying skills, the off-hand penalty normally lowers Parry by two. The initial DCL has
no `OffHandTraining`, Ambidexterity, or equivalent rule that removes this penalty.

Each hand tracks its own weapon family, Difficulty, damage expression, Parry modifier, repeated-
Parry counter, balance, and readiness. An unavailable or Unready hand cannot attack or Parry even
when the other hand remains legal.

## Block

```text
Block = floor(ShieldSkill / 2) + 3
        + shield Block modifier
        + legal shield DB
        - state penalties
```

Block is normally available once between the defender's turns. It protects only from legal facing
arcs and against attacks the shield can react to. Ranged Block legality is defined by
[Ranged Combat](07-ranged-combat.md).

Using Block spends the Block attempt, not the shield itself. The shield remains Ready, and its
Defense Bonus may still assist a later legal Dodge or Parry before the reset.

## Defense reset

At the start of the defender's own turn:

- repeated-Parry counters reset;
- Block becomes available;
- temporary restrictions that explicitly last until the next turn expire.

Speed does not grant additional resets because it does not grant additional turns.
