# DCL v4 canonical live-regression matrix

## Scope

The canonical regression is job-free and binds only technical DCL mechanisms. It makes no final balance, ability-assignment, or job-design claim and does not write a save.

## Artifacts

- Matrix: `work/1784401467-dcl-v4-live-regression-matrix.json`
- Validator: `tools/validate_dcl_live_regression_matrix.py`
- Negative tests: `tools/test_validate_dcl_live_regression_matrix.py`
- Exact pair: `work/1784399746-dcl-unified-sentinel-v4-runtime-data-pair.json`
- Process-free installation gate: `tools/validate_dcl_live_install.py`
- Transactional installer: `tools/install_dcl_live_bundle.py`

## Coverage contract

The matrix contains eighteen dependency-ordered cases covering forty-four required technical tags:

- exact installation preflight;
- player forecast, execution, and AI scoring;
- physical, magic, healing, MP, and atomic result transactions;
- dual-wield, managed multistrike, and native multistrike;
- ordinary, retained, post-calc, grouped, RandomFire, performance, duration, and reskin status paths;
- Fear, Taunt, Interrupt, instant KO, revive, and corpse lifecycle;
- real, VM-internal, and synthetic Reaction families;
- Approach, shared reservation arbitration, LoS, Weight/movement, special actions, item metadata, presentation, and battle-generation reset.

The validator requires the canonical tag set exactly, binds the pair and settings hashes, validates the nested runtime/data pair, checks every case against the paired settings, rejects later dependencies, constrains ability ids to the 512-record catalog, and requires `job_free=true` plus `writes_save=false` for every case. The pair now also binds the runtime item and ability catalogs; the live preflight verifies those catalogs plus both XML tables, action NXD, settings, DLL, app target, and enabled-mod set.

## Verification

- `python tools/test_validate_dcl_live_regression_matrix.py`: PASS.
- `python tools/validate_dcl_live_regression_matrix.py`: PASS; eighteen cases and forty-four mechanism tags.
- `python tools/report_dcl_implementation_coverage.py --check-only`: PASS; forty mechanisms.
- `python tools/check_docs_timeless.py`: PASS.
- `python tools/test_install_dcl_live_bundle.py`: PASS.
- `codemod/run-offline-checks.ps1`: PASS on the complete second run, 1,574 output lines in 103.3 seconds.

The first complete-suite attempt stopped in the pre-existing autosave-Reaction fixture because one external unpack invocation returned nonzero after an earlier successful round trip. The same test passed immediately in isolation and the complete suite then passed; no DCL or matrix assertion failed.

## Remaining gate

The matrix is defined and verified but not executed. The installed Reloaded profile still fails the exact-v4 preflight, so no further live result is admissible until settings, action data, XML tables, runtime catalogs, code DLL, and enabled-mod set all match the bound pair. The dry-run-by-default installer backs up every destination, applies atomically, rolls back on failure, and demands a successful exact post-install preflight. Once the preflight passes, execute the matrix in dependency order and preserve authoritative evidence per case.
