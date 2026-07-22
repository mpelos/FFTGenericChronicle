# Canonical admission template log analyzer

## Context

The next live proof needs to validate the guarded native admission callback and the admission-side
policy-ticket template producer mechanically from the runtime log.

## Work completed

- Added `tools/analyze_dcl_canonical_admission_template_live.py`.
- Updated `work/1784545200-canonical-admission-live-runbook.md` with the analyzer command and the
  exact acceptance contract.
- Added a synthetic pass fixture at `work/1784673223-canonical-admission-template-live-synthetic.log`
  and generated `work/1784673223-canonical-admission-template-live-synthetic-analysis.md`.
- Added `tools/test_dcl_canonical_admission_template_live.py` to lock the analyzer's pass/fail
  contract.
- Added the analyzer to the whole-DCL coverage evidence list for the admission/composition rows.

## Analyzer contract

The analyzer requires:

- `[DCL-CANONICAL-ADMISSION-HOOK]` at `rva=0x281EF7`;
- `[DCL-STATE-RESET]` with `canonicalBattle=1`;
- exactly one completed admission matching `ability=16`, `targetCount=1`, `strikes=1`;
- the same event to report `admissionStatus=Admitted`, `templateStatus=Built`,
  `ticketStatus=Published`, and `bridgeStatus=Published`;
- no `[DCL-CANONICAL-ADMISSION-ERR]`.
- With `--require-damage`, a later positive `[DAMAGE]` event must target the same single CharId from
  the completed admission's `targets` field.

`[DAMAGE]` is emitted by the base HP sampler whenever tracked HP changes; `LogHpEventProbe` only
adds detailed raw-diff diagnostics and is not required for this gate. The visible Fire animation
remains a manual observation in the live evidence.
