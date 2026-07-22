# Native Direct policy provider

## Context

The retained admitted-action ledger and unit-policy provider leave family-policy inputs as the next offline integration boundary. DirectNumeric is the narrowest useful slice because it already has a captured unit-target action in smoke coverage and its remaining policy facts are explicit rather than inferred from native rows.

## Result

Added two provider boundaries:

- `DclCanonicalNativeSingleTargetMagicPolicyProvider` materializes the shared casting declaration inputs for one complete unit-targeted nonrepeat admitted action.
- `DclCanonicalNativeDirectActionPolicyProvider` extends the shared declaration with DirectNumeric-only explicit policy such as defense, status riders, Reaction candidates, Touch route verdict, state penalties, and authored movement branches.

Both providers keep family-specific resistance, materialization, timeline, revive, dispel, movement, and state policy outside the shared boundary. The Direct provider does not infer these facts from formula, result kind, animation, native rows, or target shape.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeSingleTargetMagicPolicyProvider.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeDirectActionPolicyProvider.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

The other family-policy providers are still manual or family-local. The production callback still needs a live-bound path that retrieves a retained admitted action, supplies explicit unit and family policy inputs, composes the request, and publishes it to the native apply/forecast carriers.
