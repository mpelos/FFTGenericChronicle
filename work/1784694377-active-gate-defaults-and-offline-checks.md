# Active gate defaults and offline checks

## Result

The active integrated DCL live gates now default to the active pair/matrix instead of requiring manual paths or falling back to historical clean-v1 artifacts.

## Changes

- `tools/validate_dcl_live_install.py` now defaults to `work/1784683300-dcl-active-integrated-runtime-data-pair.json`.
- `tools/install_dcl_live_bundle.py` inherits that active default.
- `tools/validate_dcl_live_regression_matrix.py` now defaults to `work/1784683300-dcl-active-integrated-live-regression-matrix.json`.
- `tools/report_live_gate_plan.py` writes `work/<timestamp>-live-gate-plan.md` by default.
- `tools/report_offline_readiness.py` writes `work/<timestamp>-offline-readiness-audit.md` by default and reads the latest timestamped live-gate plan.
- The untimestamped generated files `work/live_gate_plan.md` and `work/offline_readiness_audit.md` were removed.

## Validation

- `python tools\validate_dcl_live_regression_matrix.py`
- `python tools\install_dcl_live_bundle.py`
- `python tools\test_validate_dcl_live_install.py`
- `python tools\test_install_dcl_live_bundle.py`
- `python tools\test_validate_dcl_live_regression_matrix.py`
- `python tools\test_validate_dcl_live_proof_sequence.py`
- `python tools\validate_dcl_runtime_data_pair.py work\1784683300-dcl-active-integrated-runtime-data-pair.json`
- `python tools\validate_dcl_live_proof_sequence.py`
- `python tools\analyze_dcl_status_duration_frontier.py --check-only`
- `python tools\report_live_gate_plan.py --check`
- `python tools\report_offline_readiness.py --check`
- `python tools\check_docs_timeless.py docs\modding`
- `powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1 -SkipInstalledExeScan -SkipGitDiffCheck -SkipDotNet`
- `dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj -c Release`
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime`

## Notes

- A first Python-only gate attempt exited with native code `-1073741819` after the conditional-status producer analyzer. The suspected next analyzer and the rest of the pipeline passed when isolated, and the full Python-only gate passed on rerun.
- A first parallel .NET build/smoke attempt caused a self-inflicted output lock in `obj\Release`; rerunning the smoke after the build passed.
- The active live install still intentionally fails until the active bundle is applied, because installed settings and DLL hashes differ from the active pair/current Release DLL.
