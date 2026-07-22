# Active pair added to offline gate

## Result

The broad offline gate now validates the active integrated DCL pair and active integrated regression matrix, then performs a dry-run of the active live-bundle installer.

## Change

- `codemod/run-offline-checks.ps1` now runs:
  - `python tools\validate_dcl_runtime_data_pair.py work\1784683300-dcl-active-integrated-runtime-data-pair.json`
  - `python tools\validate_dcl_live_regression_matrix.py`
  - `python tools\install_dcl_live_bundle.py`

## Validation

- `powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1 -SkipInstalledExeScan -SkipGitDiffCheck -SkipDotNet`

## Notes

- The active dry-run still changes no files.
- The installed runtime settings and installed code-mod DLL remain stale relative to the active pair/current Release DLL. The current Release DLL hash observed by the dry-run is `06A8C9BAFD3E6263A8D1BD4CBC52A207B6022E1999B244B61FCB7447E517D9EA`.
