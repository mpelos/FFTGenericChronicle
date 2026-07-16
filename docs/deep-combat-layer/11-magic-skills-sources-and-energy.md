# Magic Skills, Sources, and Energy

This document owns the DCL vocabulary for supernatural abilities, IQ-based magical training, MP,
and overcasting. It separates the resource an action consumes from the rules that classify and
resolve that action.

The design combines several GURPS 4e approaches:

- standard `GURPS Magic` supplies fixed, IQ-based spell skills, energy costs, casting time, and
  resisted effects;
- `Magical Styles` supplies the idea of coherent traditions with distinct training;
- `Powers` supplies explicit delivery and limitation metadata;
- ritual and incantation systems are references for exceptional long-preparation effects, not the
  baseline casting engine.

FFT retains a fixed command list, MP, CT, unit/tile targeting, and equipment slots. It does not gain
free-form spell construction during battle.

## Source, delivery, and effect are independent

Every supernatural action declares three separate axes:

| Axis | Question | Examples |
| --- | --- | --- |
| Source | What kind of ability produces the action? | Spell, Ki, Divine, Spiritual, Equipment, Monster Power. |
| Delivery | How does it reach and challenge the target? | External Projectile, Internal Direct, Area, Touch, Beneficial, Rider. |
| Effect | What happens after delivery succeeds? | Damage, Healing, Status, CT change, Dispel, Drain, Revive. |

An action is not a Spell merely because it consumes MP or produces magical damage. Iaido, Ki,
Geomancy, a sword technique, and an item may share an effect while retaining different governing
skills and interactions.

Every action may therefore declare independently:

```text
ResourceCost
Source
GoverningSkill
Delivery
Effect
AffectedByFaith
Verbal
Reflectable
Blockable
UsesDR
ResistanceCharacteristic
```

Silence, Reflect, Faith, Magic Resistance, and spell-skill rules read this metadata instead of
guessing from MP consumption or damage type.

## Magical traditions as controlling skills

Standard GURPS treats each spell as a separate IQ-based Hard or Very Hard skill. FFT has individual
spell unlocks but no natural per-spell proficiency track. The DCL therefore uses a controlling-skill
adaptation:

- Black Magick, White Magick, Time Magick, Summon, and comparable command traditions are skills;
- each tradition has IQ as its controlling attribute and an intrinsic Difficulty;
- Job Level advances the tradition's Rank;
- JP unlocks individual spells;
- each unlocked spell behaves like a technique derived from the controlling tradition.

The shared Rank investment bands and Tier-by-Job-Level schedule are defined in
[Skills and Active Defenses](03-skills-and-active-defenses.md). The same `GurpsSkillScore` function
accepts IQ instead of DX:

```text
TraditionRank  = JobRankTable[traditionAptitudeTier][sourceJobLevel]
TraditionSkill = GurpsSkillScore(IQ, traditionDifficulty, TraditionRank)
                 + explicit tradition-skill modifiers

SpellScore = TraditionSkill
             + SpellModifier
             + FocusSkillModifier
             + ZodiacCompatibilityModifier
             - Shock
             - explicit state penalties
```

Faith is not part of `SpellScore`. SpellScore measures knowledge, execution, and training; Faith
measures supernatural potency and receptivity where an effect declares that it is Faith-sensitive.

## Spell Difficulty and Spell Modifier

Difficulty belongs to the controlling tradition. A spell does not receive a second Easy/Average/
Hard/Very-Hard progression on top of it. Instead, each spell has one authored relative modifier:

```text
ordinary technique      -> SpellModifier  0
more difficult technique -> SpellModifier -1
advanced technique       -> SpellModifier -2
exceptional technique    -> SpellModifier -3 or lower
```

The values are calibration categories rather than mandatory power tiers. MP, CastCT, range, area,
selectivity, delivery, resistance, and effect size also pay for a spell's power. A large effect does
not automatically require every possible drawback.

## Source Job Level and off-job casting

Equipping a magical command as a secondary command preserves the training acquired in the job that
teaches that tradition:

```text
controlling attribute -> current IQ from the active character and job state
training Rank          -> Job Level of the tradition's source job
equipment modifiers    -> currently equipped items
```

Changing to a physical job may change IQ, MaxMP, equipment access, and concentration tools, but it
does not erase learned Black Magick or replace Black Mage Job Level with the active job's level.

The source job normally has the highest aptitude for its own tradition. A second native owner may
declare another aptitude Tier explicitly. Merely equipping a learned command does not invent a new
aptitude table for the active job.

JP purchases access; it does not add directly to SpellScore after the spell is learned. There is no
separate Magery attribute. Access, tradition aptitude, Job Level, JP unlocks, IQ, Faith, and
equipment divide the functions that Magery or Power Talent might otherwise combine.

## MP is universal extraordinary energy

MP is the only player-facing supernatural energy pool. The DCL has no Fatigue Points. The MaxMP
formula and ownership of its terms are defined in
[Attributes and Derived Stats](01-attributes-and-derived-stats.md#mp).

The `max(HT, IQ)` term means that a unit channels extraordinary energy through its stronger route:
bodily vigor or intellect. It does not combine both attributes. The lower attribute may improve
without changing MaxMP until it becomes the higher one.

`JobMPModifier` is the canonical DCL name. An implementation may reuse a field named after GURPS FP,
but that storage name does not create an FP pool.

All jobs receive at least the attribute-based portion of MaxMP. A physical job may spend MP on Ki,
spiritual techniques, weapon effects, or another authored source without turning those actions into
Spells. Source metadata determines whether Faith, Silence, Reflect, and Magic Resistance apply.

## MP costs and recovery

Every MP-consuming action has a fixed authored `BaseMPCost`:

```text
FinalMPCost = max(1, ceil(BaseMPCost * MPCostMultiplier))
```

An explicit zero-cost action remains zero and does not pass through the minimum. High SpellScore
does not automatically reduce MP cost. A cost modifier exists only when an ability, status, or item
declares it.

There is no generic automatic MP regeneration. Ether, Manafont, drain, reactions, equipment, and
other explicit effects may restore or transfer MP.

## Cost commitment

The full possible resource cost is shown and reserved when casting begins. Settlement is:

| Outcome | Cost paid |
| --- | --- |
| Success | Full cost. |
| Critical success | Full cost; critical success does not refund MP. |
| Ordinary failure | 25% of FinalMPCost, rounded up, minimum 1 when the action costs MP. |
| Interruption or voluntary cancellation | Same as ordinary failure. |
| Critical failure | Full cost. |

Reservation prevents the unit from spending the same MP on another effect while Charging. A refund
returns only the unpaid reserved portion.

## Overcasting with HP

An action that permits overcasting may pay an MP deficit with HP on the same numeric scale:

```text
MPDeficit     = max(0, FinalMPCost - CurrentMP)
OvercastHPCost = MPDeficit
```

MP is always consumed before HP. The baseline does not let a unit preserve available MP by choosing
to bleed instead. One missing MP costs one HP; there is no `3:1`, percentage-of-MaxHP, or other scale
conversion.

This specification does not create Burned HP, a separate wound pool, or an automatic SpellScore
penalty for overcasting. HP paid this way enters the ordinary FFT HP and KO lifecycle. An individual
ability may prohibit overcasting through explicit metadata; MP consumption alone does not silently
answer that question.
