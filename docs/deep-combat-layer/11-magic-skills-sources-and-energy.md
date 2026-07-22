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

This list establishes conceptual independence rather than the complete storage record. Required
fields, safe defaults, delivery unions, and fail-closed validation are owned by
[Action and State Authoring Contract](19-action-and-state-authoring-contract.md).

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

BaseSpellScore = TraditionSkill
                 + SpellModifier
                 + sum(applicable SpellSkillModifier values)
                 - Shock
                 - explicit caster-state penalties

TargetSpellScore = BaseSpellScore
                   + ZodiacCompatibilityModifier(caster, target)
                   - explicit target-relative penalties
```

Faith is not part of `SpellScore`. SpellScore measures knowledge, execution, and training; Faith
measures supernatural potency and receptivity where an effect declares that it is Faith-sensitive.
An action with no target-relative modifier uses TargetSpellScore equal to BaseSpellScore.
Other DCL documents use the unqualified term `SpellScore` for the final TargetSpellScore of the
target currently being resolved. They name BaseSpellScore explicitly when no target is bound.

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

Every MP-consuming action has a fixed authored `BaseMPCost`. Ability, equipment, and state cost
factors are exact positive rationals and combine multiplicatively:

```text
CombinedMPCostMultiplier = product(all applicable MPCostMultiplier values)

FinalMPCost = max(1, ceil(BaseMPCost * CombinedMPCostMultiplier))
```

An explicit zero-cost action remains zero and does not pass through the minimum. High SpellScore
does not automatically reduce MP cost. A cost modifier exists only when an ability, status, or item
declares it. A zero, negative, non-finite, or unparsable multiplier fails validation; making an
action free requires explicit BaseMPCost zero rather than a multiplier exploit. Exact precision is
retained until the one final ceiling.

FinalMPCost is calculated and stored at declaration. A later equipment, status, attribute, or target
change does not increase or reduce that ActionInstance's committed cost. This gives the player and
AI one stable payment forecast while resolution-time SpellScore, defense, resistance, Faith,
affinity, DR, and magnitude continue reading their current snapshot.

There is no generic automatic MP regeneration. Ether, Manafont, drain, reactions, equipment, and
other explicit effects may restore or transfer MP.

HP/MP ResourceChange uses an authored target credit, target debit, or target-to-source Drain route.
It directly changes the named pool and never becomes Injury: DR, wound multipliers, Shock, Major
Wound, knockback, concentration incidents, and damage-triggered Reactions do not apply. Drain
transfers only the target debit that actually fit inside the target's current pool, then caps the
source side independently; excess is lost. Target and source Undead behavior are separate explicit
table rows. The source-side effect resolves before the action's separately committed MP/HP cost.

## Cost commitment

Declaration fixes whether HP substitution is permitted for this ActionInstance. It computes and
stores:

```text
DeclarationMPCoverage = min(CurrentMPAtDeclaration, FinalMPCost)
ApprovedHPCap         = FinalMPCost - DeclarationMPCoverage
```

If ApprovedHPCap is positive, declaration is legal only when the action permits overcasting,
CurrentHP is at least ApprovedHPCap, and the player explicitly confirms the displayed HP payment.
If current MP covers the cost, ApprovedHPCap is zero and no later event may silently authorize HP.

The commitment prevents a voluntary cost or Reaction from leaving the cast unpayable within the
approved cap. Damage, MP drain, and other hostile effects are not blocked by that commitment and may
make the cast fail at resolution. No MP or HP is debited merely by entering Charging. Settlement is:

| Outcome | Cost paid |
| --- | --- |
| Success | Full cost. |
| Critical success | Full cost; as an intentional GURPS deviation, critical success never reduces or refunds MP/HP. |
| ResourceFailure at resolution | Zero; no casting roll occurs. |
| Ordinary failure | One point when FinalMPCost is positive; zero for an explicitly zero-cost action. |
| Interruption or voluntary cancellation before resolution | Zero; FFT consumes no MP or HP before the cast resolves. |
| Critical failure | Full cost; no universal random catastrophe is added. |

The settlement outcome is classified once against BaseSpellScore. TargetSpellScore, target active
defense, target resistance, area membership, and the number of targets affected never change the
resource outcome after that base classification. A base success pays full cost even when every
target later avoids or resists; a base ordinary failure pays one even when a favorable target
modifier would otherwise have produced a target-level success.

Because declaration creates an authorization rather than an escrow debit, settlement refunds no
resource that was never paid.

Immediately before the casting gate, the resolver tests whether the full FinalMPCost is payable
from current MP plus no more than ApprovedHPCap. If not, `ResourceFailure` ends Charging without a
SpellScore roll or effect. The Action and elapsed CastCT remain spent, but current MP and HP are not
debited and no damage-, Injury-, or payment-triggered Reaction is created.

## Overcasting with HP

An action that permits overcasting may pay an MP deficit with HP on the same numeric scale:

```text
CostDue        = cost selected by the settlement outcome
MPPaid         = min(CurrentMP, CostDue)
OvercastHPCost = CostDue - MPPaid
PaymentLegal   = OvercastHPCost <= ApprovedHPCap
                 and OvercastHPCost <= CurrentHP
```

MP is always consumed before HP. When CurrentMP is insufficient, every remaining point of CostDue
is no longer an MP debit and is paid with one HP instead. The baseline does not let a unit preserve
available MP by choosing to bleed instead. One missing MP costs one HP; there is no `3:1`,
percentage-of-MaxHP, or other scale conversion.

There is no nonlethal floor. OvercastHPCost may equal CurrentHP, reducing the caster to zero but
never below zero. On a successful cast, the planned spell effect completes and the payment commits
in the same outer action; a caster reduced to zero enters native KO before the Reaction window.
Ordinary failure and critical failure use their own CostDue from the settlement table and may
likewise cause KO. Interruption and voluntary cancellation before resolution pay nothing and
therefore cannot cause payment-derived KO.

This specification does not create Burned HP, a separate wound pool, or an automatic SpellScore
penalty for overcasting. `OvercastHPCost` is an HP payment rather than Injury: it directly debits
CurrentHP, ignores DR and damage type, and does not cause Shock, Major Wound, knockback, or a
Reaction whose trigger requires receiving damage or Injury. It still re-evaluates the derived
Critical state and ordinary FFT KO lifecycle because both depend on current HP.

An individual ability may prohibit overcasting through explicit metadata; MP consumption alone
does not silently answer that question.

### Overcasting forecast

Before confirmation, the cost line displays the full-success split rather than only the nominal MP
cost:

```text
Cost: MP {MPPaid} + HP {OvercastHPCost}
After cast: MP {ProjectedMP}, HP {ProjectedHP}
```

The displayed HP amount is also the ActionInstance's ApprovedHPCap. At resolution, restored MP may
reduce the actual HP payment below that cap. Lost or drained MP never raises HP payment above it; if
the current full-cost split would exceed ApprovedHPCap, the cast fails for insufficient MP exactly
as an MP-only cast does.

When OvercastHPCost is positive, the HP portion uses the interface's existing harmful-cost emphasis
and never relies on a new icon. When ProjectedHP is zero, the same preview displays an explicit
`Caster enters KO` warning. Ordinary-failure details expose their one-point split separately;
interruption and critical-failure details expose the cost selected for those outcomes. AI
evaluation consumes the same projected resource and KO result as the player forecast.
