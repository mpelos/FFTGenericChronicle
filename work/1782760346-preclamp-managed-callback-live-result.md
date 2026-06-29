# Pre-clamp managed callback live result

## Test

Profile:

`work/1782759291-preclamp-managed-callback-abi-profile.json`

Action:

Agrias used a basic attack on Beowulf.

Guard:

- target char id: `0x1F` (Beowulf)
- expected staged debit: `151`
- managed callback forced debit: `45`
- CT attribution disabled
- formula plans disabled
- data mod not required

## Result

User-visible result:

- preview damage: `151`
- applied/UI damage: `45`

Runtime log evidence:

```text
[PRECLAMP-REWRITE-HOOK] ... targetId=0x1F expectedDebit=151 ... planEnabled=0 ... managedCallback=1 managedForcedDebit=45
[PRECLAMP-MANAGED-CALLBACK calls=1 now=63816005872]
```

Archived raw log:

`work/1782760346-preclamp-managed-callback-abi-log.txt`

## Conclusion

The Reloaded reverse-wrapper callback path works for a guarded HP-apply pre-clamp frame: C# was
called from the native hook and the return value changed the same hit's staged debit before vanilla
HP application. This is not CT, not a prequeued plan, and not the late HP reconciler.

The proven bridge currently returns a fixed debit. The next useful implementation step is to resolve
the caster actor and action id inside the callback from the original pre-clamp stack, then return a
simple attacker/target-stat formula result from the same native frame.
