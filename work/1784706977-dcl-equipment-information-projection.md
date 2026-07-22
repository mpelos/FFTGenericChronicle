# DCL equipment information projection

## Context

DCL 09 requires the equipment screen to show only properties an item actually uses and to preview equipment changes through DCL mechanics. Item metadata and encumbrance rules already existed, but there was no single player-facing projection for item detail and before/after equipment previews.

## Change

- Added `DclCanonicalEquipmentInformationProjection`.
- Item detail now exposes normalized authored properties for weapons, shields, foci, armor/accessories, elements, immunities, and special properties.
- Weapon detail derives final damage expression from preview ST for ST-based weapons or from the fixed authored expression.
- Equipment-change preview compares two selected-unit projections and exposes Load, encumbrance band, next threshold, effective Move/Jump/Dodge, BodyDR/HeadDR, MaxHP/MaxMP, Magic Resistance, and optional focus modifiers.
- Added smoke coverage for weapon damage/readiness/ParryLoad, body armor visible-property filtering, and equipment preview values derived from selected-unit projections.
- Promoted the durable fact to `docs/modding/06-code-mod-runtime-dsl.md`, `docs/modding/08-dcl-information-requirements.md`, and the coverage report.

## Validation

- `dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release /p:UseSharedCompilation=false`
- `dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime`

Both validations passed before this note was written.
