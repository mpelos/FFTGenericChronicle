# Pre-clamp managed actor formula: delayed Braver

Live result reported:

- Action: Cloud -> Beowulf, Braver
- Preview: 153
- UI damage: 89
- Beowulf final HP: 225/314
- Extra effect: none reported

Runtime log evidence:

```text
[PRECLAMP-REWRITE-HOOK] ... targetId=0x1F expectedDebit=153 ... managedCallback=1 managedActorFormula=1
[PRECLAMP-MANAGED-CALLBACK calls=1 ...]
[PRECLAMP-MANAGED-FORMULA resolved=1 ... target=0x1F caster=0x32 actor=0x140D31AA0 actionId=257 oldDebit=153 debit=89]
[DAMAGE ptr=0x141855EE0 id=0x1F] 314 -> 225 = 89 sampleAgeMs=26
```

Interpretation:

- The delayed Braver reached the native pre-clamp hook with staged debit `153`.
- The managed callback resolved the caster from native actor context at damage-apply time:
  - caster: `0x32` (Cloud)
  - target: `0x1F` (Beowulf)
  - action id: `257` (Braver)
- The proof formula produced `89`, and vanilla applied that value through its normal HP lifecycle.

Conclusion: native pre-clamp actor context is sufficient to compute a caster+target formula for a delayed single-target action at the actual HP-apply frame. This does not depend on CT or a managed pending-action tracker.
