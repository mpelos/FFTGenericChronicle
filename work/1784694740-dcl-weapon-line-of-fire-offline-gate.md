# DCL weapon line-of-fire offline gate

## Context

The next partial-live-gated DCL mechanism selected for offline progress was reach/range/height and
native weapon line-of-fire, starting from the prepared LT27 probe.

## Offline result

- `tools/analyze_dcl_weapon_line_of_fire.py` passes against the current installed Enhanced
  executable and `work/item_catalog.csv`.
- The analyzer confirms the current byte anchors for:
  - weapon `AttackFlags` read;
  - Arc flag test and resolver call;
  - Direct flag test and resolver call;
  - Lunging flag test and resolver call;
  - Direct protected-VM entry.
- The item catalog still maps guns and crossbows to Direct, bows to Arc, and poles to Lunging.
- Direct caller sets remain single-site for the mapped weapon-target evaluator:
  - Arc resolver caller: `0x280306`;
  - Direct resolver caller: `0x28039E`;
  - Lunging resolver caller: `0x2803ED`.

## Changes made

- Added `--check-only` to `tools/analyze_dcl_weapon_line_of_fire.py` so the static gate can run
  without writing a new report.
- Added `tools/test_dcl_weapon_line_of_fire.py` as a regression smoke test for the analyzer output.
- Added both the smoke test and analyzer check to `codemod/run-offline-checks.ps1`.
- Updated `tools/report_dcl_implementation_coverage.py` so the coverage row cites the static
  analyzer/test gate instead of treating LT27 as only a live-plan artifact.
- Generated evidence report:
  - `work/1784694635-dcl-weapon-line-of-fire-analysis.md`
- Refreshed coverage artifacts:
  - `work/1784694781-dcl-implementation-coverage.md`
  - `work/1784694781-dcl-implementation-coverage.csv`

## Validation

- `python tools\test_dcl_weapon_line_of_fire.py` passed.
- `python tools\analyze_dcl_weapon_line_of_fire.py --check-only` passed.
- `python -m py_compile tools\analyze_dcl_weapon_line_of_fire.py tools\test_dcl_weapon_line_of_fire.py` passed.
- `powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1 -SkipDotNet -SkipInstalledExeScan -SkipGitDiffCheck` passed.
- `python -m py_compile tools\report_dcl_implementation_coverage.py` passed.

## Remaining boundary

This does not live-prove reaction-delivered bow/gun attacks yet. The next live-only boundary for
this mechanism remains LT27: observe Arc and Direct post-resolver events in a real targeting preview
or reaction-delivered weapon path and verify intended target identity versus reached/intercepted
unit identity.
