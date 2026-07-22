# ForcedMovement Aim lifecycle closure

## Question

The settled movement carrier already derived `CancelAim = true` only for positive final
displacement. The open question was whether standalone and Area execution could remove the exact
Aim state atomically without creating another random site or per-tile callback.

## Implementation

- Standalone ForcedMovement plans `PlanCancelOwner` after delivered movement has a positive final
  displacement.
- Pure Area ForcedMovement performs the same target-local plan only for a delivered, nonimmune
  target.
- Both paths retain the exact Aim `InstanceId`, pass the target revision guard, remove that instance
  inside `AfterCommitBeforeReaction`, and fail if the state changed before commit.
- Native projection merges the exact Aim removal with the same final movement carrier. No numeric
  channel, retention roll, intermediate tile, or second Reaction window is created.
- Zero-tile, resisted, immune, and KO-suppressed movement cannot remove Aim.

## Fail-closed fixture correction

The first full run reached native auxiliary planning and correctly rejected the sentinel's Aim
removal because the test had inserted an ad-hoc Aim definition only into the battle registry. The
native projector requires every referenced state kind to exist in the canonical authoring catalog.
The fixture now registers the typed Aim definition before building each runtime; production
validation was not weakened.

## Validation

```text
dotnet build codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c MovementAimFinal2 --no-restore --nologo -nodeReuse:false -p:UseSharedCompilation=false -maxcpucount:1
Build succeeded. 0 Warning(s). 0 Error(s).

dotnet codemod/fftivc.generic.chronicle.codemod.smoketests/bin/MovementAimFinal2/net9.0-windows/fftivc.generic.chronicle.codemod.smoketests.dll
formula runtime smoke tests passed
```

The build and suite were unusually slow in this environment. The first green compile took about
four minutes and the final suite about one minute; no game or live test was required.

## Remaining boundary

Positive movement still only exposes `CreatesConcentrationIncident`; it does not yet bind that
incident to the target's charged-action timeline or consume the one concentration roll. That is the
next distinct lifecycle vertical. The native map callback, critical knockback, and authored
Damage-plus-ForcedMovement composition remain later boundaries.
