# Native auxiliary magic policy provider

## Context

The single-target magic declaration provider made it possible to stop hand-authoring the declared target and common casting facts for several native unit-target families. The next offline step was to wrap each auxiliary family without inferring its family-specific policy from native rows or result shape.

## Result

Added `DclCanonicalNativeAuxiliaryMagicPolicyProvider` with explicit wrappers for:

- StatusApplication;
- StatusRemoval;
- Quick;
- Revive;
- Dispel;
- ForcedMovement.

Each wrapper derives the shared magic declaration from the admitted selected unit and carries only explicit family-owned policy onward: status materialization, target CT and QuickLock controller, Undead interaction table, Dispel selected instance, final DispelScore, defense, final native movement verdict, Reaction candidates, and concentration context.

The smoke sentinels for StatusApplication, StatusRemoval, Quick, Revive, Dispel, and ForcedMovement now use the provider boundary and include `SelectedUnit` in their admitted native requests.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeAuxiliaryMagicPolicyProvider.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeSingleTargetMagicPolicyProvider.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

Physical and Area still need production-grade policy provider boundaries. The live callback still needs to retrieve retained admitted actions, supply explicit unit/family policies, compose the request, and publish it through the native execution/forecast carriers.
