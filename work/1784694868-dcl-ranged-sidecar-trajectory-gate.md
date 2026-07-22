# DCL ranged sidecar trajectory gate

## Context

After strengthening the native weapon line-of-fire static gate, the next offline target was the
adjacent ranged mechanism: Distance, Aim, and projectile defense legality. Existing C# smoke tests
already cover distance penalties, Aim lifecycle, ranged defense legality, native route requirements,
and coordinated Aim retention/cancellation. The remaining useful offline hardening point was the
real item catalog sidecar boundary.

## Offline result

- `tools/test_dcl_item_sidecar.py` now cross-checks generated DCL sidecar rows against
  `work/item_catalog.csv`.
- Every Bow row must retain the native `Arc` attack flag and DCL `arc_trajectory` identity.
- Every Crossbow and Gun row must retain the native `Direct` attack flag and DCL `straight_line`
  identity.
- DCL projectile range must mirror the native weapon range for Bow/Crossbow/Gun until final numeric
  authoring deliberately replaces it.
- Bow/Crossbow/Gun overcap and skill-family policy is protected from accidental drift.
- Throwing/Bomb SKUs must stay in the external Throw payload route and cannot silently enter the
  equipped native projectile path.
- `tools/report_dcl_implementation_coverage.py` now cites this catalog-side trajectory gate in the
  Ranged coverage row.
- Refreshed coverage artifacts:
  - `work/1784694905-dcl-implementation-coverage.md`
  - `work/1784694905-dcl-implementation-coverage.csv`
- Added `tools\test_dcl_item_sidecar.py` to the main offline gate.

## Validation

- `python tools\test_dcl_item_sidecar.py` passed.
- `python -m py_compile tools\test_dcl_item_sidecar.py` passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime` passed.
- `powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1 -SkipDotNet -SkipInstalledExeScan -SkipGitDiffCheck` passed after adding the sidecar test.

## Remaining boundary

This still does not invoke the live native movement/posture/trajectory/target-loss Aim bridges.
Those callbacks remain live-gated, but the catalog side of ranged trajectory identity is now guarded
offline.
