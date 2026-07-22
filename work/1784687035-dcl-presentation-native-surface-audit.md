# DCL presentation native-surface audit

## Context

The DCL presentation work already had a pure unit-presentation snapshot: active state instances plus the loaded presentation catalog resolve body position, palette, invisibility layering, above-unit icons, CT timeline icons, selected-state details, and detail-only states.

The remaining risk was semantic drift between “the canonical snapshot knows what should be shown” and “the native UI/presentation hook for that surface is proven”. If we only checked the pure snapshot, it would be too easy to mark presentation complete before proving native pose/palette/icon/detail bindings.

## Change

Added `DclPresentationNativeSurfaces.cs`.

The new audit derives native presentation requirements from a `DclUnitPresentationSnapshot`:

- unit body position;
- unit body palette;
- unit transparency;
- above-unit status icons;
- CT timeline icons;
- selected-state detail;
- selected-equipment detail;
- defensive-resource panel;
- command disablement;
- entry feedback.

Each required surface must have an explicit binding row. Missing rows fail closed. `PureSnapshotReady` fails for native surfaces because it is not a native UI proof. `NativeBindingLiveGated` is accepted as an honest incomplete state. `NativeBindingProven` requires a stable proof id.

## Validation

Extended `TestDclStatePresentation()` with the Stun/Taunt unit-presentation scenario:

- live-gated bindings pass but do not set `AllNativeBindingsProven`;
- removing the palette binding fails;
- marking a surface proven with a blank proof id fails.

Commands:

```powershell
dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release /p:UseSharedCompilation=false
dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime
```

Both passed.

Note: the first build attempt without `/p:UseSharedCompilation=false` crashed inside Roslyn with `AccessViolationException`. The retry without shared compilation passed.
