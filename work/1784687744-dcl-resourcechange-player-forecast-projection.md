# DCL ResourceChange player forecast projection

## Context

The player-facing DCL spec requires ResourceChange forecast to name HP or MP, target credit/debit or Drain, the complete magnitude range, both pool caps, explicit target/source Undead routes, expected target loss, expected source gain or loss, no-delivery, KO probabilities, and excess lost to caps.

The ResourceChange resolver and evaluator already owned the exact correlated outcome space, but there was no reusable player-facing projection carrying all of those labels and summary values together.

## Change

Added `DclCanonicalResourceChangeForecastProjection` plus `DclCanonicalResourceChange.ProjectForecast(...)`.

The projection derives from the normalized action profile, exact target/source pool snapshots, and the RNG-free evaluation result. It exposes:

- resource kind;
- route;
- target/source Undead routes;
- rolled magnitude range after zero clamp;
- target/source current and maximum pool caps;
- expected target/source debit or credit;
- no-delivery, target KO, source KO, and rejection probabilities;
- expected excess lost to target cap and source cap.

## Validation

Extended `TestDclCanonicalResourceChange()` using the existing HP Drain sentinel:

- target HP 5/10;
- source HP 8/10;
- magnitude `1d6`;
- target debit expectation `10/3`;
- source credit expectation `11/6`;
- target KO probability `1/3`;
- target excess lost to cap `1/6`;
- source excess lost to cap `3/2`.

Commands:

```powershell
dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release /p:UseSharedCompilation=false
dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime
```

Both passed.
