# DCL ForcedMovement player forecast verdict

## Context

The DCL 09 ForcedMovement path already used one immutable native map verdict for forecast, AI, confirmed execution, and native projection. The remaining offline gap was the player-facing projection: it exposed chance and delivered movement count, but not the native path facts needed to present the exact forced movement before execution.

## Change

- Added direction, origin, destination, and expected moved tiles to `DclCanonicalForcedMovementForecast`.
- Routed those fields directly from `DclCanonicalForcedMovementEvaluationResult.DeliveredMovement` and `ExpectedMovedTiles`.
- Extended the canonical ForcedMovement smoke to prove the player forecast and AI projection consume the same immutable verdict and expectation.
- Promoted the durable fact to `docs/modding/06-code-mod-runtime-dsl.md`, `docs/modding/08-dcl-information-requirements.md`, and the coverage report.

## Validation

- `dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release /p:UseSharedCompilation=false`
- `dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime`

Both validations passed before this note was written.
