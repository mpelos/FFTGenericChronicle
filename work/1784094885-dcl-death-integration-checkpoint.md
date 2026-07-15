# DCL Death integration checkpoint

## Objective

Move the live-proven Death Instant-KO vertical out of its forced-roll probe profiles and into the
strict offline runtime composition without weakening the action-data safety contract.

## Result

The integration scaffold now composes three independent mechanism fragments:

- physical Weapon Skill, typed damage, armor, facing, Dodge, Parry, and Block;
- numeric magic, Faith, Zodiac, affinity, Shell, absorb/null, healing, and Undead inversion;
- Death ability `30`, using inverse-Faith 3d6 resistance and the permanent AI-facing compute-point
  writer.

The Death fragment contains no forced hit/status roll and enables no probe. Its data partner is the
LT37 round-trip-audited action table where only ability `30` changes to `Formula=8`, `X=1`, `Y=1`,
`InflictStatus=0` on top of the full neuter database.

`tools/validate_dcl_runtime_data_pair.py` makes that pairing fail closed. It verifies both artifact
hashes, requires the runtime and data ability sets to be identical, inspects every selected SQLite
row, requires the AI-facing writer, and rejects probe-only rolls and hooks. Its smoke test proves that
native Death data and a forced status roll are both rejected.

## Integrated behavior proved offline

- Basic Attack still selects the physical Weapon Skill/DR/wound route.
- Fire still selects the magic/Faith/affinity route.
- Cure still inverts debit and credit against Undead.
- Unauthored Barrage still preserves the native staged debit.
- Death remains outside ordinary magic-damage classification, preserves its neutralized HP carrier,
  evaluates inverse Faith, exposes exact probability-weighted lethal debit to AI, zeros a resisted
  result, and includes same-hit HP credit in a successful lethal debit.

## Validation

- Runtime-settings composition is fresh with one explicitly resolved `DclDamageFormula` conflict.
- The pair validator passes against the audited Death SQLite/NXD hashes.
- The runtime-settings validator reports zero errors.
- Python syntax, pairing failure tests, C# build, and C# smoke tests pass.
- The complete offline regression gate passes with installed-executable scanning and dirty-tree
  comparison intentionally skipped; it does not deploy, change saves, or launch the game.

## Remaining boundary

This remains an integration scaffold, not the final deployable DCL profile. Broader status,
reaction, lifecycle, item, job, and multistrike policy is excluded until each fragment has an exact
runtime/data contract and an integrated regression case. Death itself needs no new isolated live
probe; later whole-profile regression must cover it beside the other composed mechanisms.
