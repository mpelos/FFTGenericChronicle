# DCL magic / healing pipeline — offline checkpoint

## Result

The runtime can now express and validate the DCL's shared numeric-magic spine for canonical spell
damage and healing:

```text
amount = raw MA × spell Y × Faith(caster) × Faith(target)
       × applicable affinity × applicable Shell × Zodiac × Gm
```

The executable fixture is
`work/1783983032-battle-runtime-settings.dcl-magic-pipeline-mechanism.json`; the same formulas are
covered by `TestDclMagicPipeline` in the formula-runtime smoke project.

The profile also composes the existing Magic Evade mechanism over every authored damage spell and
Rod/Staff bolt, using the unit's merged magic-evade maximum and the structural 50% cap. Healing is
excluded. This integration is offline-valid but retains LT18 as its live delivery gate.

The first implemented classification is intentionally narrow and auditable:

- ability formula `0x08`: canonical deterministic magic damage;
- ability formula `0x0C`: canonical deterministic healing;
- basic Attack (`abilityId 0`) with a right-hand Rod or Staff: zero-MP magic bolt, taking spell power
  and element from the equipped weapon metadata;
- Holy/Dark within `0x08`: spiritual, so Faith and Zodiac apply but elemental affinity and Shell do
  not;
- all other formulas preserve their existing HP debit/credit until separately classified.

This avoids silently forcing hybrid, percentage, monster, drain, or physical formulas through the
wrong spine. Shipping still needs an explicit audit of all 512 ability records.

## Fixture shape

The centered Faith term uses permanent/max Faith from `unit+0x2C`:

```text
faithPermille = 700 + floor(maxFaith × 600 / 100)
```

It therefore spans `0.70..1.30`, with Faith 50 neutral. This follows the detailed magic design and
`12-open-questions`; `08-trait-faith.md` still contains an older `0.60` floor statement and must be
reconciled as a design-document conflict before numbers lock.

Other mechanism fixtures:

| Term | Value |
| --- | ---: |
| everyday weakness | 1300 permille |
| everyday resistance/strong | 700 permille |
| native halve | 500 permille |
| Shell on elemental magic | 500 permille |
| Zodiac | same 900/1000/1100/1200 ordinary grid as physical |
| reserve stack cap | 2500 permille |
| `Gm` | 580 permille |

All terms are derived before the final amount and the reserve cap acts on their combined stack.
Integer rounding is deterministic; the conceptual multipliers are commutative, while the current DSL
necessarily rounds at each `mulDiv` boundary.

## Channel routing

- normal damage writes HP debit and no authored HP credit;
- elemental absorb writes zero debit and the same deterministic amount as HP credit;
- elemental null writes zero to both authored channels;
- normal healing writes HP credit and preserves unrelated debit;
- healing an Undead target writes the amount as HP debit and explicitly zeroes HP credit.

The explicit zero is important: the first smoke attempt correctly created Undead damage but fell
through to the old vanilla credit (`55`). The formula now owns both channels for the inverted case.
Forecast formulas mirror the same routing without depending on `dcl.oldDebit/oldCredit`.

## Offline calibration anchors

The fixture caster has raw MA 12, max Faith 60, Aries; the target has max Faith 60, Libra (Best):

| Case | Result |
| --- | ---: |
| Fire (ability 16, Y=14), neutral | 131 damage |
| Flame Rod (item 53, WP=3) basic Attack | 27 Fire damage |
| Fire + Shell | 65 damage |
| Fire + everyday weakness | 170 damage |
| Fire + absorb | 0 damage / 131 healing |
| Fire + null | 0 / 0 |
| Holy (ability 15, Y=50), Shell + Fire affinities present | 468 damage |
| Cure (ability 1, Y=14) | 131 healing |
| Cure on Undead | 131 damage / 0 healing |

Holy's result proves Shell and elemental affinity are outside the spiritual path. Cure proves healing
uses MA, both Faith terms, Zodiac and `Gm`, but no resist term.

## Evidence boundary

The unit element variables read the five bytes at `unit+0x52..+0x56` as
absorb/null/halve/weak/strengthen masks. This layout is still **Hypothesis** in the timeless memory
model. The offline mechanism is complete, but no live claim is made for those bytes. A controlled
Fire cast plus one bounded affinity-byte poke is required before relying on them as the merged
equipment/job/status authority.

Item-catalog affinity flags are independently decoded and smoke-tested, so a formula-only fallback
can aggregate equipped-item flags if the unit block is refuted. That fallback would not automatically
cover job innates or designed-content state, which is why the unit-block probe remains preferable.

## Remaining work

1. Prove the already-integrated Magic Evade decision through LT18.
2. Audit all ability formulas and author an explicit magic-kind/power map for exceptions.
3. Author range 3 and an element/power identity for every Rod/Staff SKU in data; the runtime bolt
   route itself is implemented and Flame Rod is covered offline.
4. Prove `+0x52..+0x56` live, including absorb channel routing and Oil/Shell interaction.
5. Reconcile the Faith `0.60` vs `0.70` documentation conflict and calibrate every magnitude.
6. Validate preview amounts and final HP debit/credit through LT17 plus a magic-specific live matrix.
