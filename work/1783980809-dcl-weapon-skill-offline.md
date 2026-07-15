# DCL Weapon Skill — offline mechanism checkpoint

## Result

The runtime can now calculate the locked Weapon Skill shape independently for both equipped hands
from live battle-unit inputs:

```text
job id × weapon family -> grade -> base/rate
Job Level + character level -> grade-weighted growth
equipped/innate Sword Master -> provisional +2
raw skill -> capped attack skill + over-cap excess
```

The mechanism profile is
`work/1783980809-battle-runtime-settings.dcl-weapon-skill-mechanism.json`. It deliberately contains
only one authored grade cell (`Ninja × Ninja Blade = A`) and uses grade F as the sparse-map fallback.
It proves the routing without pretending the full job×family balance matrix is decided.

## Ability-slot mapping hardened

Extended snapshots resolve these little-endian ability-id words in the battle-unit struct:

| Offset | Meaning | Evidence |
| --- | --- | --- |
| `+0x0A/+0x0C/+0x0E/+0x10` | innate ability ids 1–4 | Ninja `+0x0A = 477`, matching both Dual Wield in the ability catalog and Ninja `JobData.InnateAbilityId1` |
| `+0x14` | equipped Reaction id | Ninja `439` = Gil Snapper; Cloud `445` = Mana Shield |
| `+0x16` | equipped Support id | Ramza `484`; Ninja `474`; Agrias/Cloud/Beowulf `456` = Equip Swords |
| `+0x18` | equipped Movement id | Ramza/Beowulf `488`; Ninja/Agrias `487`; Cloud `494` = Manafont |

All five units agree with `docs/reference/fft-vanilla-ability-effect-index.md`. The previous
`unit+0x98` candidate is a derived/effect bitfield, not the canonical Support slot. The timeless
owner is updated in `docs/modding/04-engine-memory-model.md`; the formula surface is documented in
`docs/modding/06-code-mod-runtime-dsl.md`.

This makes Sword Master detection data-driven: compare the configured ability id against the
equipped Support word and the four innate words. Ability id `481` is an unused Support record in the
current data and is a clean candidate, but it is not a final design assignment until its data and
text are authored.

## Formula encoding

Family codes reserve a collision-free five-bit range for the sparse composite key
`jobId * 32 + family`:

```text
0 none/unarmed, 1 knife, 2 ninja blade, 3 sword, 4 knight sword, 5 fell sword,
6 katana, 7 axe, 8 flail, 9 rod, 10 staff, 11 pole, 12 polearm, 13 crossbow,
14 bow, 15 gun, 16 book, 17 instrument, 18 bag, 19 cloth, 20 throwing, 21 bomb
```

`Bomb` and `Throwing` are now part of the catalog's stable category-variable surface. Grade codes are
`F=0, D=1, C=2, B=3, A=4`; tables encode base `[5,7,9,11,13]` and rate-permille
`[200,320,500,720,1000]`.

To preserve the DCL equation with integer math:

```text
investmentMilli = 2500*(jobLevel-1) + floor(250*jobLevel*(charLevel-1)/8)
growth = floor(investmentMilli * ratePermille / 1,000,000)
raw = base + growth + SwordMasterBonus
skill = min(cap, raw)
excess = max(0, raw-cap)
```

Expected calibration points covered by smoke tests:

| Case | Raw | Capped | Excess |
| --- | ---: | ---: | ---: |
| Grade A, Job 8, Level 99 | 55 | 16 | 39 |
| same + provisional Sword Master | 57 | 16 | 41 |
| Grade F, Job 8, Level 99 | 13 | 13 | 0 |
| Grade A, Job 1, Level 99 | 16 | 16 | 0 |

## Remaining gates

- Author the complete job×family grade matrix and decide monster/special-job policy.
- Author Sword Master in data/text, then replace the candidate id if the assignment changes.
- Decide and calibrate the over-cap conversion rates. The mechanism exposes separate pre-conversion
  damage units for Crossbow and penetration units for Gun without pretending those rates are locked.
- Resolve active-hand identity for mixed-family Dual Wield. Both hands now have independent family,
  grade, raw/capped skill, excess, damage-input, and over-cap routes. The compatibility aliases still
  point to the right hand; they must not silently govern a left-hand follow-up in a shipping profile.
- Live validation waits until the unmodified game reaches its menu again; no live result is claimed
  by this checkpoint.

## Physical-pipeline continuation

The profile now also carries the modernized LT7 physical damage spine, including skill-primary
Crossbow/Gun input, separate overcap conversion routes, typed DR, wound, Brave, and Zodiac. The
offline proof and fixture outputs are owned by
`work/1783982225-dcl-physical-pipeline-modernization-offline.md`.
