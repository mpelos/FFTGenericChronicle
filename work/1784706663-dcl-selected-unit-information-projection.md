# DCL selected-unit information projection

## Context

DCL 09 requires the unit status screen to explain derived combat values instead of leaving the player to infer them from vanilla labels. The runtime already had pure characteristic, equipment, encumbrance, and Critical resolvers, but the player-facing selected-unit surface did not have one canonical projection that assembled those facts from the same snapshot used by mechanics.

## Change

- Added `DclCanonicalUnitInformationProjection`.
- The projection consumes a synchronized `DclCanonicalNativeUnitProjection`, `DclCanonicalEquipmentSnapshot`, explicit non-equipment characteristic inputs, raw PA/Speed/MA, and current Brave.
- It exposes ST/DX/IQ component breakdowns, Brave-derived HT, Faith, HP/MP current/base/final terms, Basic Lift, Basic Speed, exact encumbrance, and Critical final Move/Dodge adjustment.
- Added a smoke covering channel separation, equipment modifiers, HP/MP terms, Light encumbrance, equipment Dodge, and Critical mobility adjustment.
- Promoted the durable fact to `docs/modding/06-code-mod-runtime-dsl.md`, `docs/modding/08-dcl-information-requirements.md`, and the coverage report.

## Validation

- `dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release /p:UseSharedCompilation=false`
- `dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime`

Both validations passed.
