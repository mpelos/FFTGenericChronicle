# Braver under named-instant profile

This run is a control/invalid delayed-Braver trial, not evidence for delayed actor formula resolution.

Live result reported:

- Action: Cloud -> Beowulf, Braver
- Preview: 153
- UI damage: 153
- Beowulf HP: 161/314
- Extra effect: none

Active runtime settings in the log still had `PreClampDamageRewriteExpectedDebit=205`, from the named-instant Agrias skill profile. Braver reached the pre-clamp hook and incremented `[PRECLAMP-MANAGED-CALLBACK calls=1]`, but the expected-debit guard rejected the event because the staged debit was 153, not 205. No `[PRECLAMP-MANAGED-FORMULA]` line was emitted, and vanilla damage passed through unchanged.

Conclusion: this run proves the expected-debit guard is protecting unrelated actions. It does not test whether the native actor-context formula can resolve delayed Braver.
