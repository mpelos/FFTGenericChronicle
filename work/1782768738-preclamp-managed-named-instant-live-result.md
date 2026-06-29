# Pre-clamp managed named instant action live result

## Profile

```text
work/1782768104-preclamp-managed-named-instant-profile.json
```

Important settings:

```text
PreClampDamageRewriteTargetCharId = 0x1F
PreClampDamageRewriteExpectedDebit = 205
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

## User-visible result

```text
Preview: 205
UI: 56
Beowulf: 258/314
No strange extra effect observed.
```

## Log evidence

Raw log archived:

```text
work/1782768738-preclamp-managed-named-instant-live-log.txt
```

Key lines:

```text
[PRECLAMP-REWRITE-HOOK] ... targetId=0x1F expectedDebit=205 ... managedCallback=1 managedForcedDebit=any managedActorFormula=1
[PRECLAMP-MANAGED-CALLBACK calls=1 ...]
[PRECLAMP-MANAGED-FORMULA resolved=1 ... target=0x1F caster=0x1E actor=0x140D31AA0 actionId=159 oldDebit=205 debit=56]
[DAMAGE ptr=... id=0x1F] 314 -> 258 = 56
```

## Conclusion

The native pre-clamp managed callback can resolve caster + action identity and apply same-frame
formula damage for an instant named ability. This proof uses no CT, no recent-unit heuristic, no
pending-action tracker, no queued formula plan, and no preview mutation.

The `actionId=159` value came from the caster actor struct at the same native HP-apply frame. This
is the first behavior-changing proof that the actor-context route supplies action identity, not only
caster identity.

Remaining validation targets:

- charged/delayed single-target action;
- charged/delayed AoE action with multiple HP events;
- reaction/cancel paths where the original action may be suppressed;
- crit/random/native multiplier suppression so the DCL, not vanilla, owns final variance.
