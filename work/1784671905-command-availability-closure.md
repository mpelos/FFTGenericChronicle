# Universal command availability closure

## Context

The `Ready, Reequip, Stand Up, and equipment action state` coverage row still had an offline/live seam around command availability. The confirmed semantic coordinators already rejected stale state, Taunt, invalid Reaction windows, and illegal command state, but there was no pure pre-selection owner for what UI/AI should offer before confirmed execution.

## Conclusion

- Added `DclCanonicalCommandAvailabilityResolver`.
- Added `DclCanonicalBattleRuntime.CaptureWeaponStates` so command presentation can use immutable per-weapon snapshots without mutating registered weapon state.
- Ready availability is per registered weapon resource.
- Reequip availability consumes an explicit `equipmentPolicyAllows` verdict and does not infer job, slot, hand, or inventory legality.
- Stand Up availability requires the current Knocked Down state plus both Action and Movement.
- Taunt suppresses Ready, Reequip, and Stand Up before UI/AI selection because they consume Action and are not the universal normal Attack.
- The remaining live/integration work is binding native menu rows, selected equipment presentation, native turn grants, and native command/result carriers to this pure command-availability contract.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalCommandAvailability.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalBattleRuntime.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `dotnet build codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c CommandAvailability1 --no-restore -m:1 --nologo -v:minimal`
- `codemod\fftivc.generic.chronicle.codemod.smoketests\bin\CommandAvailability1\net9.0-windows\fftivc.generic.chronicle.codemod.smoketests.exe`
