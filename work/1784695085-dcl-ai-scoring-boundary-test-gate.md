# DCL AI-scoring boundary test gate

## Context

The next offline target was the Forecast/AI boundary. `tools/analyze_dcl_ai_scoring_boundary.py`
already validated the current compute-point and AI-scoring evidence, and the main offline gate ran
that analyzer directly. It lacked a small regression test that asserts the report still contains
the expected contract.

## Offline result

- Added `tools/test_dcl_ai_scoring_boundary.py`.
- The test validates the analyzer's byte-anchor rows, call-ownership result, runtime-source checks,
  LT35 live comparison markers, and LT36 permanent-writer/cache markers.
- Added the test to `codemod/run-offline-checks.ps1`.
- Updated `tools/report_dcl_implementation_coverage.py` so the Forecast row cites the analyzer/test
  gate and names the protected compute-point contract:
  - post-calc staged bundle boundary at `0x281F12`;
  - downstream pre-clamp cache consumer at `0x30A5D7`;
  - confirmed execution cache reuse rather than double evaluation.
- Updated the AI-visible rewritten scoring coverage row to cite the new regression test.
- Refreshed coverage artifacts:
  - `work/1784695085-dcl-implementation-coverage.md`
  - `work/1784695085-dcl-implementation-coverage.csv`

## Validation

- `python tools\test_dcl_ai_scoring_boundary.py` passed.
- `python tools\analyze_dcl_ai_scoring_boundary.py --check-only` passed.
- `python -m py_compile tools\test_dcl_ai_scoring_boundary.py tools\analyze_dcl_ai_scoring_boundary.py` passed.
- `python -m py_compile tools\report_dcl_implementation_coverage.py tools\test_dcl_ai_scoring_boundary.py` passed.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime` passed after the coverage/test edits.
- After these checks, the local Python runner began timing out even for `python -c "print('ok')"`;
  the full offline gate could not be re-run in that transient runner state.

## Remaining boundary

This strengthens the existing offline/live-evidence gate for the AI scoring compute point, but it
does not bind every canonical correlated forecast result into the native player forecast and AI
valuation surfaces. That integration remains partial-live-gated.
