# Native Physical policy provider

## Context

PhysicalDamage composition already supported complete native repeat identity, Cover/Bodyguard auxiliary rows, and retained weapon-skill training. The remaining offline boundary was to stop hand-authoring the declared target/fixed tile in tests and instead derive that declaration identity from the admitted native selection while leaving actual combat policy explicit.

## Result

Added `DclCanonicalNativePhysicalActionPolicyProvider`.

The provider accepts a complete contiguous PhysicalDamage admission sequence and an explicit physical policy source. It derives:

- `DeclaredTarget` from the admitted selected unit for unit-target physical actions;
- `FixedTile` from the admitted selected tile for fixed-tile physical actions.

It keeps these as explicit policy facts supplied by the caller:

- weapon item/resource identity;
- range and vertical route verdicts;
- target contexts;
- Strike contexts and defense candidates;
- Reaction candidates;
- universal-normal-attack flag;
- per-Strike weapon bindings;
- protection redirect candidate;
- skill-training policy.

The Cover/Bodyguard physical composition sentinel now uses this provider, so the native selected target remains the declaration while the final resolution target can still route to the protector.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativePhysicalActionPolicyProvider.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

Area policy provider boundaries and the production live callback bridge remain unbound. Physical still requires the live callback to feed retained admissions, synchronized unit policies, and explicit family policy into composition at runtime.
