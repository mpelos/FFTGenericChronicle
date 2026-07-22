# DCL Reaction player forecast route

## Context

The DCL player-facing information spec requires Reaction preview to distinguish activation from the Reaction effect, show blocking reasons, and name the resolved effect source and target. The canonical Reaction evaluator already owned activation probabilities and effect routes, but the player forecast DTO exposed only chance, natural-effect-gate flag, and reason.

## Change

Extended `DclCanonicalReactionPlayerForecast` so each player row carries:

- native order;
- reactor identity;
- Reaction id;
- activation mode;
- rounded activation chance;
- natural-effect-gate flag;
- blocking reason;
- effect Action id;
- resolved effect source;
- resolved effect target.

The projection still derives from `DclCanonicalReactionEvaluationResult`, so player forecast and AI consume the same ordered RNG-free candidate evaluation.

## Validation

Extended the canonical runtime smoke to require:

- `ActivationRoll` player rows expose the correct activation mode, rounded chance, effect Action, reactor-as-source, and outer source as target;
- `SkillResponse` rows expose the natural-effect gate and reason;
- native-cardinality rejection rows expose zero chance and the blocking reason;
- AI expected mass still equals the evaluator's exact expected accepted activations.

Commands:

```powershell
dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release /p:UseSharedCompilation=false
dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime
```

Both passed.
