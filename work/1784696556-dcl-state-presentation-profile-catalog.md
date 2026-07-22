# DCL state presentation profile catalog

## Context

The DCL docs require state presentation to resolve through loaded symbolic catalogs rather than hardcoded texture coordinates or native status-bit side effects. The runtime had state definitions pointing at a `PresentationProfile` string, but no validated presentation-profile catalog existed yet.

## Work completed

- Added `DclStatePresentationProfile`, `DclStatePresentationProfileContract`, and `DclStatePresentationProfileRegistry`.
- A presentation profile now owns:
  - display name;
  - mechanical-effect text;
  - symbolic unit icon, timeline icon, position, palette, and entry-feedback references;
  - selected-unit detail terms;
  - source/magnitude/expiry/stacking visibility;
  - cure-family detail.
- Validation rejects texture-coordinate-style asset references and accepts only symbolic references such as `NativeStatusIcon(...)`, `NativePosition(...)`, `NativePalette(...)`, `DclPalette(...)`, `NativeFeedback(...)`, or `DclFeedback(...)`.
- The registry fails closed when a state definition references a presentation profile that is not loaded.
- The registry can build the display-name map consumed by the pure channel resolver.

## Validation

- `dotnet run --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime` passed.

## Remaining boundary

The catalog is validated and usable by pure runtime code, but native UI binding still needs to consume these symbolic presentation profiles at the actual above-unit icon, CT timeline, selected-detail, pose, palette, and feedback surfaces.
