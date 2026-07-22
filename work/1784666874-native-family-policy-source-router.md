# Native family policy-source router

## Context

The retained-action bridge could already consume fully materialized family-policy inputs. For production callback binding, that is still too low-level: the callback should supply explicit policy sources and let the classified canonical family choose the one valid provider.

## Result

Added `DclCanonicalNativeFamilyPolicyProvider`.

The provider accepts one retained admitted action and one explicit family-policy source object. It resolves the ability's canonical family from the runtime catalog and routes only the matching source type:

- DirectNumeric -> Direct policy provider;
- PhysicalDamage -> Physical policy provider;
- AreaNumeric -> Area policy provider;
- StatusApplication -> auxiliary Status provider;
- StatusRemoval -> shared single-target magic source;
- Dispel -> auxiliary Dispel provider;
- Quick -> auxiliary Quick provider;
- Revive -> auxiliary Revive provider;
- ForcedMovement -> auxiliary ForcedMovement provider.

Wrong-source objects fail before request construction.

`DclCanonicalNativeRetainedActionBridge` now has a policy-source request overload that retrieves the retained action, routes the family-policy source, and then uses the existing compose/dispatch/publish/retire path.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeFamilyPolicyProvider.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeRetainedActionBridge.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

The live callback still needs policy-source production from actual native context. This router deliberately does not infer those policy facts.
