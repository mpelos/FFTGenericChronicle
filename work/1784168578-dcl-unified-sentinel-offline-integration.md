# Unified DCL sentinel offline integration

## Purpose

This closes the previous `integration-missing` gap with one job-free technical profile. Every value
is a bounded regression fixture, not final balance policy, and no job assignment or new job effect is
present.

## Composed runtime

- Composition manifest: `work/1784168025-dcl-runtime-composition-manifest.json`
- Generated settings: `work/1784168025-battle-runtime-settings.dcl-unified-sentinel.json`
- Settings SHA-256: `F63ECEC25A62738AA45283D16A6D235145975632067347B994FB9E3B530F7BC7`
- Composition freshness: PASS with `18` explicit conflict resolutions.
- Runtime settings validator: PASS with zero errors; warnings describe enabled technical/live gates.

The profile composes these mechanism families:

- deterministic physical damage, weapon metadata, facing, reach variables, Dodge, Parry, Block,
  guard depletion, and preview/result parity;
- magic damage/healing, Faith, Zodiac, elements/affinities, Undead inversion, Magic Evade, and AI
  numeric scoring;
- Pummel ability `101` as a three-strike managed-multistrike sentinel with exact metadata routing;
- atomic HP/MP debit and result-flag reconstruction using a target-scoped partial-Mana-Shield
  sentinel;
- Instant KO ability `30` through native HP/KO lifecycle;
- `30` status rules across retained native carriers, data-suppressed riders, execution-only
  performance and RandomFire producers, a support transaction, and a split-result conditional
  transaction;
- four Reaction chance rules covering real-code courage, real-code neutral, VM-owned caution, and a
  blank structural owner;
- the live-proven owner-`443` / delivery-`442` synthetic transaction with all five guarded ownership
  and execution boundaries.

## Isolated data and metadata pair

- Ability metadata: `work/1784168025-dcl-integration-ability-metadata.csv`
- SQLite: `work/1784168025-dcl-unified-sentinel-overrideabilityactiondata.sqlite`
- SQLite SHA-256: `7FD98A1F08A758C8E4132CD6BD7AE67592BA145839D10492B59C86EADD57C4E1`
- NXD: `work/1784168025-dcl-unified-sentinel-overrideabilityactiondata.nxd`
- NXD SHA-256: `A172B0C41B7F526CB4D508A1837B11315E1DC46C60A954AE402D14F2CA37E63A`
- Pair manifest: `work/1784168025-dcl-unified-sentinel-runtime-data-pair.json`
- ItemWeaponData SHA-256: `1C4EA8BBE087D960A2BF4D89696EFEC71DB866EB44CB31E01C1A9E66D7C0BE03`
- AbilityChargeAimData SHA-256: `34915B0939782D844D60484B960CE9613D5DC6CE66A91813FDC3C5794712E9E9`

`tools/build_dcl_integration_data_pair.py` generates the SQLite from the base override table,
neutralizes the exact native riders, builds the NXD, and round-trips the complete NXD table back to
SQLite before success. The isolated artifact changes no production data-mod file.

The fail-closed pair validator proves:

- Instant-KO runtime/data set: `30`;
- native status-rider suppression set: `219,252,277,357`;
- status rule abilities: `81,84,91,173,219,252,277,357`;
- all three status carrier policies are represented;
- managed multistrike metadata set: `101`;
- Reaction rule set: `442,443,445,451`;
- synthetic Reaction identity: `443->442` with commit, pre-selector, tri-validator,
  materialization, and effect boundaries;
- non-empty atomic HP and MP debit formulas with result-flag control;
- exact hashes for settings, SQLite, NXD, and ability metadata.

## Coverage effect

The whole-DCL matrix now contains no `integration-missing` row. Unified profile integration is
`mechanism-ready`; the remaining global gate is the integrated live regression. Individual
`partial-live-gated` rows remain honest until their representative live cases pass.

## Deployment boundary

Do not copy this profile or its NXD into Reloaded-II without a separate timestamped live plan and an
independent six-artifact backup. The profile intentionally enables multiple write mechanisms at once
and is not the final mod configuration.
