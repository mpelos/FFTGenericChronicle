# Physical Cover route closure

## Scope

Closed the offline canonical route for Cover/Bodyguard under the revised DCL rule set.

## Conclusions

- Cover/Bodyguard is a physical protection mechanism only.
- `ExternalProjectile` and other tracked spell deliveries do not route through physical cover; they remain in the magic route/Reflect family.
- A physical action preserves the originally declared target in its `DclActionDeclaration`.
- The routed target batch is built against the planned protector when the current protection link is legal.
- Native physical composition accepts an explicit protection candidate from the synchronized batch and returns a request whose declaration targets the original unit while all final target/Strike rows target the protector.
- Captured native composition can freeze an explicit auxiliary protector row that is not part of the native selected target list, then require one policy input for that row before projection.
- Player forecast and AI planning validate the same planned final target without consuming state or RNG.
- The protection state is planned without mutation, then consumed or cancelled immediately before the first physical execution RNG site.
- A rejected or mismatched routed request consumes no protection state, no execution RNG, and no native publication.
- Native physical projection carries the exact removed protection state beside the published carrier.

## Evidence

- `dotnet build codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c ProtectionPhysicalRoute9 --no-restore -m:1 --nologo -v:minimal`
- `codemod\fftivc.generic.chronicle.codemod.smoketests\bin\ProtectionPhysicalRoute9\net9.0-windows\fftivc.generic.chronicle.codemod.smoketests.exe --test-dcl-injury-movement`
- `codemod\fftivc.generic.chronicle.codemod.smoketests\bin\ProtectionPhysicalRoute9\net9.0-windows\fftivc.generic.chronicle.codemod.smoketests.exe`
- `dotnet build codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c CapturedCoverPolicy1 --no-restore -m:1 --nologo -v:minimal`
- `codemod\fftivc.generic.chronicle.codemod.smoketests\bin\CapturedCoverPolicy1\net9.0-windows\fftivc.generic.chronicle.codemod.smoketests.exe --test-dcl-injury-movement`
- `codemod\fftivc.generic.chronicle.codemod.smoketests\bin\CapturedCoverPolicy1\net9.0-windows\fftivc.generic.chronicle.codemod.smoketests.exe`

## Next offline gate

Connect the native forecast/AI policy-input provider to the existing non-mutating physical protection plan. The canonical mechanism is closed offline, but native UI/AI capture remains live-gated with the broader family-policy input bridge.
