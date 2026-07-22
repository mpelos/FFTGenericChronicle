# Canonical admission probe readiness checker

## Context

The canonical admission live proof now has a log analyzer. The remaining offline risk before a live
run is deploying a stale or broadened sentinel fixture.

## Work completed

- Added `tools/analyze_dcl_canonical_admission_probe_readiness.py`.
- Added `tools/test_dcl_canonical_admission_probe_readiness.py`.
- Updated `work/1784545200-canonical-admission-live-runbook.md` to require the readiness gate before
  deployment.

## Gate

The readiness checker verifies:

- only `DclCanonicalRuntimeEnabled` and `DclCanonicalAdmissionEnabled` are `Enabled=true`;
- write/rewrite, legacy DCL, Fear, Approach, hit/status/miss, post-apply, and Reaction-completion
  switches stay disabled;
- every canonical runtime path referenced by the settings exists;
- ability `16` has the expected single-result DirectNumeric Fire binding;
- ability `16` has a DirectNumeric policy-ticket template with explicit nonnegative source/target
  tile heights.
