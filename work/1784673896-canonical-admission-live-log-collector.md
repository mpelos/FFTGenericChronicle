# Canonical admission live log collector

## Context

The runbook still had a manual copy-then-analyze step for `battleprobe_log.txt`.

## Work completed

- Added `tools/collect_dcl_canonical_admission_live_log.py`.
- Added `tools/test_collect_dcl_canonical_admission_live_log.py`.
- Updated `codemod/prepare-canonical-admission-live.ps1` and
  `work/1784545200-canonical-admission-live-runbook.md` to use the collector.

## Collector contract

The collector:

- reads the live game log without moving or deleting it;
- copies it to `work/<timestamp>-raw-canonical-admission-live.log`;
- writes `work/<timestamp>-canonical-admission-template-live-analysis.md`;
- runs the canonical admission analyzer with positive target damage required by default.
