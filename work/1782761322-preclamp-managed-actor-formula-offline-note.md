# Pre-clamp managed actor formula offline checkpoint

## Objective

Move from the proven fixed-debit managed callback to a same-frame damage formula that derives the
caster from native battle memory, without CT, pending-action plans, or preview state.

The live proof immediately before this checkpoint showed:

- preview damage remained `151`;
- the pre-clamp managed callback forced the staged debit to `45`;
- the applied/UI damage was `45`;
- the log recorded `[PRECLAMP-MANAGED-CALLBACK calls=1 ...]`.

That proves the C# callback can run synchronously inside the native HP-apply frame and mutate the
debit before vanilla consumes it. It does not yet prove actor/caster resolution inside that callback.

## New implementation

The pre-clamp managed callback now has a second opt-in path:

```text
caster damage = caster.PA * PreClampManagedCallbackPaMultiplier - target.Faith
```

The proof path is intentionally narrow. It does not run the full DSL yet. It exists to validate the
hard part first: resolving caster + actionId directly from the native pre-clamp frame.

New runtime settings:

- `PreClampManagedCallbackActorFormulaEnabled`
- `PreClampManagedCallbackPaMultiplier`
- `PreClampManagedCallbackFormulaMinDamage`
- `PreClampManagedCallbackFormulaMaxDamage`
- `PreClampManagedCallbackStackScanBytes`

If actor resolution fails and no `PreClampManagedCallbackForcedDebit` fallback is configured, the
callback returns `-1`; the ASM hook does not write `[rbp+6]`, so vanilla damage remains intact.

## Memory path used

The callback receives:

- target unit pointer from native `rdi`;
- staged state/result pointer from native `rbp`;
- hook-save stack pointer;
- pre-clamp shared buffer pointer.

The ASM now passes the hook-save stack pointer rather than only the original stack pointer. From
that pointer, the callback can inspect:

- saved volatile registers from hook entry;
- original stack slots beginning at `hookStack + 64`.

For each candidate root, the resolver checks whether:

```text
root + PreClampActorStructUnitOffset -> registered unit pointer
```

with current defaults:

```text
PreClampActorStructUnitOffset = 0x148
PreClampActorActionIdOffset   = 0x142
```

Resolution rule:

- if exactly one distinct actor links to a registered unit different from the target, that unit is
  the caster;
- if no non-target actor exists and exactly one target-linked actor carries `actionId > 0`, resolve
  self-action/self-AoE;
- otherwise the callback treats the frame as unresolved and does not mutate damage.

## Concurrency guard

The callback must not iterate `_unitObservations` directly because the poller mutates that dictionary.
The poller now refreshes a compact immutable array snapshot:

```text
UnitObservationView[]
```

The callback only reads that array via `Volatile.Read`. The array contains the stable unit pointers
and basic stats required for candidate matching.

## Diagnostics

The poller logs callback formula outcomes through managed counters:

```text
[PRECLAMP-MANAGED-FORMULA resolved=... target=... caster=... actor=... actionId=... oldDebit=... debit=...]
[PRECLAMP-MANAGED-FORMULA unresolved=... target=... oldDebit=...]
```

This gives us a live answer even if the visible damage is ambiguous.

## Offline validation

Commands run successfully:

```powershell
dotnet build codemod\fftivc.generic.chronicle.codemod\fftivc.generic.chronicle.codemod.csproj -c Release
dotnet run --project codemod\fftivc.generic.chronicle.codemod.settingsvalidate\fftivc.generic.chronicle.codemod.settingsvalidate.csproj -c Release -- work\1782761322-preclamp-managed-actor-formula-profile.json
powershell -ExecutionPolicy Bypass -File codemod\run-offline-checks.ps1
```

The validator reports only the expected live-probe warnings for pre-clamp mutation and managed
callback use.

## Next live profile

Profile:

```text
work/1782761322-preclamp-managed-actor-formula-profile.json
```

It is guarded for the current save's known Agrias basic attack into Beowulf:

```text
target charId    = 0x1F
expected debit   = 151
forced fallback  = off
actor formula    = on
PA multiplier    = 11
stack scan bytes = 512
```

Known prior stats from the latest log:

```text
Agrias PA      = 11
Beowulf Faith  = 65
```

Expected if the callback resolves Agrias correctly:

```text
11 * 11 - 65 = 56 damage
```

Expected if actor resolution fails:

```text
151 damage
```

Either result is informative. `56` proves same-frame actor formula by native memory. `151` means the
bridge is still ABI-safe but this callback resolver missed the actor in the live frame, and the next
probe should widen what the callback can inspect.
