# DCL unit presentation snapshot

## Context

The runtime could load state-presentation profiles and resolve state channels from the catalog. The remaining pure-runtime seam was body presentation: pose, palette, invisibility, and channels were still resolved as separate lower-level calls rather than one unit-level presentation request.

## Work completed

- Added `DclNativePresentationInput`.
- Added `DclUnitPresentationSnapshot`.
- Added `DclStatePresentation.ResolveUnitPresentation`.
- The unit snapshot composes:
  - active DCL state instances;
  - the loaded state-presentation profile catalog;
  - native presentation facts such as KO, transformation, Critical, action-state pose, Invisibility, and native palette states.
- The snapshot resolves:
  - body position;
  - body palette;
  - invisibility transparency layering;
  - unit-icon channel;
  - CT timeline-icon channel;
  - selected-state details;
  - detail-only technical state kinds.
- Persisted `PresentationId` drift is rejected before resolving body/channels.
- Native KO and transformation keep priority over DCL pose requests.
- KO suppresses temporary DCL palettes without deleting the underlying state instances.

## Validation

- Added smoke coverage for Stun/Taunt profile composition, Critical/action-state/native Poison interaction, KO palette suppression, transformation priority, selected-detail ordering, and catalog-backed channel resolution.
- `dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release` passed.
- `dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime` passed.

## Remaining boundary

The snapshot is pure runtime data. The native game still needs UI/render hooks to consume the snapshot and apply its symbolic body, icon, CT timeline, selected-detail, equipment/defense-panel, and command-disablement requests.
