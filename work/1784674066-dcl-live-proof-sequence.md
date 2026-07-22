# DCL live proof sequence

## Context

The historical clean-v1 live regression matrix exists, but it does not represent the newer
canonical admission template bridge probe because that probe uses a separate sentinel runtime
profile. The clean-v1 runtime/data pair also depends on the retired Disable/Immobilize
duration-transfer experiment, so it is not an active future live gate.

## Work completed

- Added `work/1784674066-dcl-live-proof-sequence.json`.
- Added `tools/validate_dcl_live_proof_sequence.py`.
- Added `tools/test_validate_dcl_live_proof_sequence.py`.
- Added the proof-sequence test to `codemod/run-offline-checks.ps1`.
- Added the proof sequence and validator/test evidence to `tools/report_dcl_implementation_coverage.py`.
- Generated coverage snapshot `work/1784685502-dcl-implementation-coverage.md`.
- Repointed `integrated-clean-regression` away from the historical clean-v1 runtime/data pair. It
  now requires a future active integrated runtime/data pair and matrix after the canonical admission
  proof succeeds.
- Added validator coverage that rejects historical inactive runtime/data and status-duration pair
  paths if they are reintroduced as active proof-sequence artifacts.

## Sequence

The sequence makes the current ordering explicit:

1. `canonical-admission-template-bridge` is ready for live proof.
2. `integrated-clean-regression` remains blocked by the prior proof.

Both entries are job-free, write no saves, and keep retired Fear/Approach compatibility controls out
of scope.

## Validation

- `python tools\validate_dcl_live_proof_sequence.py` passes.
- `python tools\test_validate_dcl_live_proof_sequence.py` passes.
- `python tools\analyze_dcl_status_duration_frontier.py --check-only` passes.
- `python tools\report_dcl_implementation_coverage.py --check` passes with 50 mechanisms.
- The Python-only offline gate was updated so historical Immobilize/Disable duration-transfer
  artifacts no longer masquerade as current DCL truth. The status-duration frontier now defaults to
  no active duration-pair manifest and leaves unresolved status natures blocked by category.
- `powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1 -SkipDotNet
  -SkipInstalledExeScan` reaches the late static analyzers. One full run exited with Python access
  violation `-1073741819` after `analyze_dcl_status_conditional_producer.py`; the immediately
  following pending-cancellation analyzer and the remaining final analyzers all passed when run
  independently. Treat the access violation as a transient tooling/runtime crash unless it
  reproduces consistently.
