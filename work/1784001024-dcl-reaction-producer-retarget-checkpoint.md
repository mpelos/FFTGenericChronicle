# DCL reaction producer and retarget checkpoint

## Scope

This checkpoint records the first disabled-by-default synthetic-reaction transaction controls after
the offline carrier mapping. It does not claim battle behavior or authorize live writes.

## Implemented verticals

### Pass-2 producer

- `DclReactionProducerEnabled` rides the AOB-guarded pre-selector hook at RVA `0x2063A9`.
- It targets one explicit battle-unit index `0..20`.
- It requires the unit to be active and `unit+0x1CE` to be empty.
- Log-only mode records `would-stage`; live mode can stage one configured Reaction id `422..453`.
- Live writes have an independent `1..32` cap and require the three-pass commit probe.
- Defaults are disabled and log-only.

### Pass-2 retarget

- `DclReactionRetargetEnabled` rides the guarded pass-2 commit boundary at RVA `0x206421`.
- It requires an exact configured carrier, at least one native target, and incoming source index
  `0..20`.
- Log-only mode records the source as `would-write`; live mode can set target count to one and the
  first target byte to that source.
- Live writes have a separate `1..32` cap.
- Defaults are disabled and log-only.

Together these controls cover the structural carrier and target-direction parts of Hex Ward `443`.
They deliberately do not decide eligibility, consume cadence, or deliver Blind/Brave effects.

## Prepared evidence

- LT29 profile and plan:
  - `work/1784000422-battle-runtime-settings.lt29-dcl-reaction-producer-logonly.json`
  - `work/1784000422-lt29-dcl-reaction-producer-logonly-live-plan.md`
- LT29 hook-install evidence:
  - `work/1784000700-lt29-dcl-reaction-producer-hook-launch-check.md`
- LT30 profile and plan:
  - `work/1784000915-battle-runtime-settings.lt30-dcl-reaction-retarget-logonly.json`
  - `work/1784000915-lt30-dcl-reaction-retarget-logonly-live-plan.md`
- LT30 hook-install evidence:
  - `work/1784000964-lt30-dcl-reaction-retarget-hook-launch-check.md`
- Current capability and implementation reports:
  - `work/1784000997-dcl-reaction-capabilities.md`
  - `work/1784000997-dcl-reaction-implementation-manifest.md`

## Verification

- Release build succeeds with zero warnings and zero errors.
- Formula/runtime smoke tests pass, including invalid and valid log-only settings cases.
- LT29 and LT30 validate with zero errors.
- Runtime anchor audit passes all 25 anchors in
  `work/1784001012-runtime-hook-anchor-audit.md`.
- Both new assembly branches install through Reloaded-II with their expected-byte guards.
- `git diff --check` passes; the existing `work/runtime_formula_context.md` line-ending warning is
  unrelated.

## Installed safe state

- Installed settings remain byte-identical to LT23.
- Installed and release-build DLL SHA-256 values both equal
  `675E2D06A6EE11866520518800F937530EDC9431D7B5DB1EC09530B03A33D88F`.
- `FFT_enhanced.exe` is not running.
- The user's successful Reloaded launch remains the operational health control.

## Remaining transaction boundary

LT23 and LT28 still precede behavior use. After they establish pass ownership and execution-only
provenance, the smallest Hex Ward proof is: one known active reactor, one bounded `443` producer
write, accepted pass-2 commit, log-only source retarget, then one bounded retarget write. Managed
Blind/Brave delivery and cadence commit require their own effect boundary and remain offline work.
