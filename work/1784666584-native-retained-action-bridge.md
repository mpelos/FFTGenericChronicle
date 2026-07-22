# Native retained action bridge

## Context

The admission callback can capture a complete native outer action and publish it into the retained admitted-action ledger. Offline family policy providers now materialize the explicit policy shapes for Unit, Direct, auxiliary single-target magic, Physical, and Area. The next production-facing gap was a single bridge from retained ActionInstance to canonical execution publication.

## Result

Added `DclCanonicalNativeRetainedActionBridge`.

The bridge accepts:

- one retained `ActionInstanceId`;
- explicit unit policy sources;
- one exact family-policy input object.

It then:

1. retrieves the retained admitted action from the battle ledger;
2. materializes unit policy inputs through `DclCanonicalNativeUnitPolicyProvider`;
3. composes the classified family request through `DclCanonicalNativeConfirmedRequestComposer`;
4. dispatches confirmed execution through `DclCanonicalConfirmedExecutionDispatcher`;
5. verifies the native publication uses the same ActionInstance;
6. retires the retained admitted action only after successful publication.

A failed policy validation leaves the retained admission in place and does not draw RNG or publish a native action.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeRetainedActionBridge.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

The live admission callback still needs production policy-source capture from the real native context before it can call this bridge safely. Presentation, forecast/AI carrier binding, CT/turn integration, movement-settled handling, and accepted-Reaction production/dispatch remain live-gated or partially bound.
