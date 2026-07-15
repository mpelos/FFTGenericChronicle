# DCL Instant-KO staged data fixture

## Scope

This fixture is generated outside the deployed mod package. It neutralizes only the selected
native Instant-KO rows on top of the existing full neuter SQLite and is safe to deploy only
together with matching `DclInstantKoRules`.

## Artifacts

- Prefix: `1784090960`
- Immutable source: `D:\Projects\FFTGenericChronicle\work\override_ability.neuter.sqlite`
- Source SHA-256: `E129547257D4F28262A44F9C6DC59EF52467F39F1654E4AC19AAE9BFE94D33AA`
- Staged SQLite: `1784090960-lt37-death-neutralized.sqlite`
- Staged SQLite SHA-256: `FC353C3B7CB206DF8DCED32D97AEBAF92363C73CFF072C9A760B9E4B7B65F25C`
- Staged NXD: `1784090960-lt37-death-neutralized.nxd`
- Staged NXD SHA-256: `1A18BD5D162F10E21F99287E9182773BAA7A015C9D750BA4494FC5B9A54B90FB`
- Selected ability ids: `30`

## Exact selected-row delta

- Ability `30` before: `Formula=-1, X=-1, Y=-1, InflictStatus=-1`
- Ability `30` after: `Formula=8, X=1, Y=1, InflictStatus=0`

Every unselected row and every non-owned column in a selected row is byte-for-byte equal
at the SQLite value level. FF16Tools NXD-to-SQLite round-trip reproduces every staged row
and column exactly. The deployed mod package is not modified by this build.

## Deployment gate

Record the installed NXD backup hash before replacement, deploy this NXD only while the game
is stopped, validate the matching runtime settings, and restore the installed backup after the
probe. A profile with `DclInstantKoControlEnabled=true` must never be paired with native Death.
