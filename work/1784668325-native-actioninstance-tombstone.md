# Native ActionInstance tombstone

## Context

Admission-side replay was guarded while a native ActionInstance was still present in the native
action ledger. Once native cleanup settled and retired the application, a late replay needed an
explicit battle-generation tombstone to prevent the same ActionInstance from being retained or
published again.

## Result

`DclCanonicalNativeActionLedger` now keeps a battle-scoped retired ActionInstance set.

- `ContainsPublishedOrRetired` checks both active applications and retired ids.
- `Publish` rejects ids that are either active or retired.
- `Retire` adds the settled id to the tombstone set.
- `TryPublishAdmissionAndResolve` checks the published-or-retired identity set before retaining an
  admission.

The tombstone lasts for the battle runtime generation and is discarded only with the runtime.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeActionLedger.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeRetainedActionBridge.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

This closes late replay within a battle generation. Cross-generation replay remains rejected by
existing battle-generation identity checks.
