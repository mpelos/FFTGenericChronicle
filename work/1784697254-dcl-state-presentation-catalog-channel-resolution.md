# DCL state presentation catalog channel resolution

## Context

The canonical runtime can now load the state-presentation profile bundle, but `DclStatePresentation.ResolveChannels` still had a lower-level overload that accepted display names as an external dictionary. That left one test-only/manual seam between persisted DCL state instances and the loaded presentation catalog.

## Work completed

- Added a `ResolveChannels` overload that consumes `DclStatePresentationProfileRegistry` directly.
- The overload resolves display names from each instance's owning state definition and loaded presentation profile.
- It fails closed when:
  - the instance's persisted `PresentationId` differs from the owning definition's `PresentationProfile`;
  - the owning definition's presentation profile is absent from the loaded catalog.
- The existing lower-level dictionary overload remains available for isolated unit tests.

## Validation

- Added smoke coverage for catalog-driven channel resolution.
- Added smoke coverage for persisted `PresentationId` mismatch rejection.
- `dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release` passed.
- `dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime` passed.

## Remaining boundary

The pure resolver now consumes the loaded presentation catalog, but native UI hooks still need to request and apply the resulting channel snapshot at the actual game presentation surfaces.
