# Canonical admission managed duplicate suppression

The live hook anchor `0x281EFA` is stable and no longer crashes, but it executes inside the native
21-slot target loop. A single Fire against one unit therefore produced 21 identical completed
admissions when every hook invocation called the managed admission path.

The first attempted assembly guard, `rbx == 0`, was too strict in live execution: the hook installed
but produced no admission events. The runtime now keeps the hook shim simple and handles duplication
in managed code instead.

`DclCanonicalAdmissionDuplicateSuppressor` implements the short-window suppression, and `Mod`
builds a canonical admission key from:

- observed source UnitKey
- action type and ability id
- battle state
- native repeat count and index
- selected unit
- selected tile
- active weapon item/hand
- raw 21-byte TargetBatch

If the same key appears again inside a 250 ms window, the callback returns without allocating a sweep
serial or publishing admission. This suppresses the repeated calls from the same native target loop
without suppressing a later player action.

Offline evidence:

- `python tools/test_dcl_canonical_admission_template_live.py` passes.
- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj --no-restore -- --test-dcl-canonical-runtime` passes and directly checks that the duplicate suppressor skips only identical keys inside the short window, allows the same key after the window, allows distinct keys, and clears on reset.
- `python tools/audit_runtime_hook_anchors.py` passes and writes `work/1784691444-runtime-hook-anchor-audit.md`.
- `python tools/test_prepare_canonical_admission_live.py` passes.
- `python tools/test_dcl_canonical_admission_probe_readiness.py` passes.
- `python tools/validate_dcl_live_proof_sequence.py` passes.

Live status:

- `work/1784690576-raw-canonical-admission-live.log` proves the moved `0x281EFA` hook no longer
  crashes, but without dedupe it produced 21 admissions for one Fire.
- `work/1784690828-raw-canonical-admission-live.log` proves the direct `rbx == 0` guard was too
  restrictive: the hook installed but produced zero admissions.
- A clean live proof for the managed duplicate filter still needs a stable pre-action battle save.
  The current first autosave was overwritten after Fire, and Manual Save 05 is a world-map save.
- `codemod/prepare-canonical-admission-live.ps1` no longer instructs the next live run to load
  Manual Save 05 directly; it now calls for a verified pre-action battle fixture and describes
  Manual Save 05 as a world-map baseline only.
