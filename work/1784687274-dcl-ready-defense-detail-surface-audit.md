# DCL Ready/defense detail-surface audit

## Context

The player-facing information spec requires visible representation for whether an equipped weapon can attack or Parry and whether Block is available. These states are deliberately not above-unit status icons.

## Change

Extended `TestDclStatePresentation()` with a Ready plus Block Spent scenario:

- `ready` and `block-spent` resolve with no above-unit icons;
- both stay in `DetailOnlyStateKinds`;
- the native-surface audit requires `SelectedStateDetail`;
- `ready` requires `EquipmentDetail`;
- `block-spent` requires `DefensePanel`.

This keeps technical equipment/defense state visible without polluting the native status-icon layer.

## Validation

Commands:

```powershell
dotnet build codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release /p:UseSharedCompilation=false
dotnet run --no-build --project codemod\fftivc.generic.chronicle.codemod.smoketests\fftivc.generic.chronicle.codemod.smoketests.csproj -c Release -- --test-dcl-canonical-runtime
```

Both passed.
