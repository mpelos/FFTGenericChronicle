# Unified DCL sentinel U0 startup plan

## Question

Can the exact paired unified profile boot with all data/runtime mechanisms enabled, without hook,
metadata, or data-pair interference?

## Bounded setup

- Runtime: `work/1784168025-battle-runtime-settings.dcl-unified-sentinel.json`.
- Pair contract: `work/1784168025-dcl-unified-sentinel-runtime-data-pair.json`.
- Ability NXD: `work/1784168025-dcl-unified-sentinel-overrideabilityactiondata.nxd`.
- Item/Aim tables: the two repository files hashed by the pair manifest.
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
- Reloaded profile: mod loader, Generic Chronicle data package, and Generic Chronicle Battle Probe
  only. The data package contains only technical weapon/Aim/action placeholders; it contains no job
  files.
- Back up DLL, PDB, settings, AppConfig, battle log, autosave, installed ability NXD,
  ItemWeaponData, and AbilityChargeAimData before deployment.

## Startup gate

1. The deployed settings, NXD, ItemWeaponData, and AbilityChargeAimData match the pair-manifest
   hashes.
2. The runtime reports the data mod loaded and the ability metadata overlay accepted.
3. Pre-clamp, compute-point, hit/physical, Magic-Evade, status post-calc, Instant-KO, result-flags,
   Reaction commit/pre-selector/tri-validator/materialization/effect, and synthetic-Reaction hooks
   install without any `FAILED`, `SKIP`, or byte-guard mismatch.
4. No combat write occurs before entering the fixture.

## Stop rule

Stop after the startup log proves all required hooks and metadata/data pairing, before issuing a
combat command. Archive the raw battle-probe log and restore all nine external artifacts
byte-for-byte before interpretation.

This gate proves startup coexistence only. The old combat sequence is not a valid unified Reaction
gate because Wenyld plus Choco Beak may be lethal under the integrated damage model; a lethal result
correctly cannot reserve `successful-hit-survivor`. Reaction regression requires a separate U1
fixture or bounded nonlethal overlay whose HP equation is verified before the action. U0 does not
promote Reaction, status, Pummel, Magic Evade, atomic HP/MP, or Instant-KO behavior.
