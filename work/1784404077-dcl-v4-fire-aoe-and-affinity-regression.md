# DCL v4 Fire AoE authority and affinity regression

## Live action

Josephine centered Fire `16` on Arthur, with Leona inside the area. The Fire animation visited both
targets. Neither target displayed damage or a miss message.

## Runtime evidence

The fresh v4 log identifies Josephine as caster slot `18`, Leona as target slot `16`, and Arthur as
target slot `17`. The voluntary-confirm private builder reports
`expandedTargetCount=2 expandedTargets=[16,17] listAuthoritative=1`. The execution bridge reports the
same two affected targets. This closes the pending Fear AoE target-authority probe.

Both hit decisions are 100% hits. Native calculation stages HP damage with `oldDebit=8` for Leona
and `oldDebit=12` for Arthur and result flag `0x80`. The DCL compute-point transaction then changes
both HP debits to zero and clears the numeric result flag. The symptom is therefore a deterministic
DCL rewrite, not a miss, target-list, animation, or native Fire failure.

## Root cause

The v4 magic formulas read `t.element.null.fire` from the hypothesized battle-unit block
`+0x52..+0x56`. Leona carries `+0x53=0x82` and Arthur carries `+0x53=0x80`, so the formula labels both
as Fire-null. Their canonical job rows (`85` and `82`) and all seven equipment slots contain no Fire
nullification. Native Fire independently produces positive HP-damage results for both. The proposed
affinity interpretation of the memory block is refuted.

## Offline correction

The v5 profile resolves Absorb/Null/Halve/Weak from the canonical job baseline and all seven target
equipment slots; Strong comes from item bonus data. Oil remains an explicit Fire-weak source. New
`*Resolved` variables keep the corrected chain separate from the legacy memory hypothesis.

The exact regression fixtures retain Leona's `+0x53=0x82`, Arthur's `+0x53=0x80`, their observed job
ids and equipment, Josephine's caster stats, native Fire result flag `0x80`, and native debits `8/12`.
Both fixtures now resolve no Fire affinity and produce positive DCL damage. Settings validation,
generator tests, smoke tests, runtime/data pairing, and status-duration pairing pass offline.

## v5 live closure

The exact repeated Fire again expands to `[16,17]` at both pre-confirm and execution. Both decisions
are 100% hits. Leona changes from native debit `8` to final DCL debit `127`; Arthur changes from
native debit `12` to final DCL debit `143`. Both final result flags remain HP damage `0x80`. The user
visually confirms damage on both units. The affinity correction and AoE authority gate are closed.

## Operational rule

Live-test execution remains autonomous. A test is handed to the user only when the user explicitly
offers to perform it; that exception does not transfer later tests.
