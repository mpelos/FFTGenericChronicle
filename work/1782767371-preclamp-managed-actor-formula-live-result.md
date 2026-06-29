# Pre-clamp managed actor formula live result

## Profile

```text
work/1782761322-preclamp-managed-actor-formula-profile.json
```

Important settings:

```text
PreClampDamageRewriteTargetCharId = 0x1F
PreClampDamageRewriteExpectedDebit = 151
PreClampManagedCallbackEnabled = true
PreClampManagedCallbackForcedDebit = -1
PreClampManagedCallbackActorFormulaEnabled = true
PreClampManagedCallbackPaMultiplier = 11
ResolveAttackerByCt = false
TrackPendingActions = false
PreClampFormulaPlanEnabled = false
```

Formula under test:

```text
caster.PA * 11 - target.Faith
```

Known stats in the log:

```text
Agrias 0x1E PA = 11
Beowulf 0x1F Faith = 65
Expected formula debit = 11 * 11 - 65 = 56
```

## User-visible results

Attempt 1:

```text
Preview: 151
UI: 168 (critical)
Beowulf: 146/314
```

Attempt 2:

```text
Preview: 151
UI: 56
Beowulf: 258/314
```

## Log evidence

Raw log archived:

```text
work/1782767371-preclamp-managed-actor-formula-live-log.txt
```

Key lines:

```text
[PRECLAMP-REWRITE-HOOK] ... targetId=0x1F expectedDebit=151 ... managedCallback=1 managedForcedDebit=any managedActorFormula=1
[PRECLAMP-MANAGED-CALLBACK calls=1 ...]
[DAMAGE ptr=... id=0x1F] 314 -> 146 = 168
[PRECLAMP-MANAGED-CALLBACK calls=2 ...]
[PRECLAMP-MANAGED-FORMULA resolved=1 ... target=0x1F caster=0x1E actor=0x140D31AA0 actionId=0 oldDebit=151 debit=56]
```

Attempt 1 is explained by the guard: the attack crit changed the staged debit from the expected
`151` to `168`, so the callback returned `-1` before actor formula evaluation and vanilla damage
passed through.

Attempt 2 passed the guard, resolved the caster from the native pre-clamp actor context, and wrote
`56` to `[rbp+6]` before vanilla applied HP.

## Conclusion

The runtime can now execute a same-frame managed damage formula inside the native HP-apply frame
using native memory for caster and target identity. This proof uses no CT, no recent-unit heuristic,
no pending-action tracker, no formula plan, and no preview state.

This is still a narrow proof:

- basic attack resolved with `actionId=0`;
- delayed actions, AoE, reactions, status side effects, and healing need separate same-frame formula
  proofs;
- the current callback formula is a temporary built-in proof path, not the final DSL integration.
