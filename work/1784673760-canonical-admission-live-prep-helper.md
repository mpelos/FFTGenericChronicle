# Canonical admission live prep helper

## Context

The canonical admission probe has a fixture-readiness checker and a log analyzer. The next offline
hardening step is to make the deployment path explicit and repeatable without changing Reloaded-II
enabled mods.

## Work completed

- Added `codemod/prepare-canonical-admission-live.ps1`.
- Added `tools/test_prepare_canonical_admission_live.py`.
- Added the canonical admission readiness/template/prep-helper tests to `codemod/run-offline-checks.ps1`.
- Updated the helper's post-live instruction to call
  `tools/collect_dcl_canonical_admission_live_log.py`.
- Updated `work/1784545200-canonical-admission-live-runbook.md` with the dry-run and deploy
  commands.

## Helper contract

The helper:

- accepts the current sentinel settings path by default;
- runs `tools/analyze_dcl_canonical_admission_probe_readiness.py --check-only`;
- delegates runtime settings validation, build, and deploy to `codemod/build-deploy.ps1`;
- archives the old game-side `battleprobe_log.txt` unless `-NoArchiveLog` is passed;
- prints the exact post-live analyzer command using `--require-damage`;
- does not edit Reloaded-II AppConfig or enable/disable mods.
