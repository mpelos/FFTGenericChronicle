# Native unit policy provider

## Context

The retained native admission ledger can now preserve a complete captured outer action under its ActionInstance. The next offline gap was the explicit per-unit policy input boundary used by snapshot projection: composition should not silently infer job modifiers, tile height, eligibility, or defense resource keys from native effective stats.

## Result

Added `DclCanonicalNativeUnitPolicyProvider` as the provider boundary that materializes `DclCanonicalNativeUnitPolicyInput` from explicit `DclCanonicalNativeUnitPolicySource` records.

The provider requires exactly one source per frozen native row retained by the admitted action, ordered by `DclUnitKey`, and rejects:

- missing retained rows;
- duplicate UnitKeys;
- negative tile height;
- prepopulated equipment attribute channels.

This keeps equipment modifiers owned by equipment projection and keeps job/tile/eligibility/defense facts owned by their future production providers instead of inferred from the frozen native row.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeUnitPolicyProvider.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

Production providers still need to feed family policy inputs from live native callback context. This provider closes only the per-unit snapshot-policy boundary used by retained native composition.
