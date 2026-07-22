# DCL overcast player forecast projection

## Context

The DCL player-facing information spec requires an overcast-enabled action with insufficient MP to display the exact `MP + HP` payment split, projected pools, and a textual KO warning when the HP payment reaches zero. HP substitution must not be hidden behind the nominal MP cost or represented only by an icon.

The existing resource layer already calculated declaration commitment and settlement, including ApprovedHPCap and lethal overcast. The missing piece was a dedicated forecast projection carrying the UI-facing fields together.

## Change

Added `DclOvercastForecastProjection` and `DclMagicResources.ProjectOvercastForecast(...)`.

The projection exposes:

- final MP cost;
- declaration-time MP payment;
- declaration-time HP payment;
- ApprovedHPCap;
- projected MP/HP;
- confirmation requirement;
- legality;
- reason;
- textual KO warning when the approved HP payment reduces the caster to zero HP.

## Validation

Extended `TestDclMagicResourcesAndCasting()`:

- MP 6 / HP 20 / cost 10 forecasts MP 6 + HP 4, projected pools 0/16, no KO warning;
- MP 0 / HP 4 / cost 4 forecasts and settles lethal overcast, retaining the no-nonlethal-floor rule and emitting a textual 0 HP warning.

Commands:

```powershell
dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release /p:UseSharedCompilation=false
dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime
```

Both passed.
