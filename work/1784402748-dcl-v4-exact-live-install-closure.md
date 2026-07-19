# DCL v4 exact live-install closure

## Gap found

The initial live preflight bound settings, action NXD, enabled mods, Enhanced target, and code DLL, but did not verify every paired data surface consumed by the v4 mechanism profile. `ItemWeaponData.xml`, `AbilityChargeAimData.xml`, `item_catalog.csv`, and `wotl_ability_action_baseline.csv` could therefore be stale while the gate still described the installation as exact.

## Closure

- The v4 runtime/data pair now binds the two runtime catalogs in addition to its existing action data and XML tables.
- The runtime/data-pair validator verifies both catalog hashes and requires the paired settings to select their filenames.
- The live-install preflight verifies all six pair-bound runtime/data files, the current Release DLL, the Enhanced app target, and the exact three-mod enabled set.
- `tools/install_dcl_live_bundle.py` is dry-run by default and performs no process inspection.
- Its `--apply` transaction backs up every destination, replaces files atomically, restores backups on any failure, and accepts the installation only after the same exact preflight passes.
- Tests cover successful dry run/apply, preservation of old bytes in backups, exact enabled-mod replacement, and rejection of stale source hashes. Separate preflight tests reject stale action data, both XML tables, both runtime catalogs, missing/extra mods, and stale DLLs.

## Current dry-run result

The read-only installer plan found six required changes:

1. enable `fftivc.generic.chronicle` in the exact three-mod profile;
2. replace runtime settings with SHA-256 `D7DA5E42D498C60DBA5596F9528F40F19973B05C8E269AD6E4A411D0F078E278`;
3. replace action NXD with SHA-256 `44B1E65F33FA5AF1C0A075645B898C5BDCC543F5D2DDF832017571B5C12741A9`;
4. replace `ItemWeaponData.xml` with SHA-256 `1C4EA8BBE087D960A2BF4D89696EFEC71DB866EB44CB31E01C1A9E66D7C0BE03`;
5. replace `AbilityChargeAimData.xml` with SHA-256 `34915B0939782D844D60484B960CE9613D5DC6CE66A91813FDC3C5794712E9E9`;
6. replace the installed code-mod DLL with the current Release build.

The installed item and ability catalogs already match their paired hashes. No files changed during this dry run.

## Verification

- Runtime/data-pair tests: PASS.
- Exact v4 pair validation: PASS.
- Live-install preflight tests: PASS.
- Transactional installer tests: PASS.
- Canonical regression-matrix tests and validation: PASS, eighteen cases / forty-four tags.
- Whole-DCL coverage check: PASS, forty mechanisms.
- Complete offline suite: PASS, 1,577 output lines in 104.5 seconds with the installer test integrated.
- Docs timeless check: PASS.

## Apply gate

Run `python tools/install_dcl_live_bundle.py --apply` only after FFT and Reloaded are visibly closed. A new live session is admissible only when the command completes with `DCL live installation preflight PASS`.
