# Pending-credit healing formula success

## Setup

Profile used: `work/1782675768-battle-runtime-settings-heal-formula-pending-credit-test.json`.

Intent:

- keep preview natural;
- use pending-action context for a delayed/charged healing action;
- require `action.sourcePending && action.actionId == 1 && action.creditCacheMatch`;
- formula: `-min(t.maxHp - previousHp, a.ma * 10 + t.faith)`;
- expected with Agrias/Ramza stats: `min(missing, 14 * 10 + 70) = 210`.

## User observation

- Preview: `+86HP`.
- Execution UI: `+210`.

## Log evidence

Evidence file: `work/1782676130-battleprobe-log-heal-pending-credit-success.txt`.

The pre-clamp formula candidate resolved through the pending-action tracker, not CT or immediate
freshness:

```text
[PENDING-ACTION-MATCH kind=preclamp-cache ... resolved=0x141855EE0/id=0x1E source=pending-clear batch=1 act=1 ... confidence=credit-cache ... observed=-86 ... currentCache=dmg1C4=0/cred1C6=86/chg1D8=1/f1E5=64/bb=0 ...]
[PRECLAMP-FORMULA-CANDIDATE ... oldDebit=0 oldCredit=86 forcedDebit=0 forcedCredit=210 eventKind=healing shouldStage=1 queuedPlan=1 rule=FinalDamageFormula attacker=0x141855EE0/id=0x1E source=pending-clear ...]
[PRECLAMP-FORMULA-RUNTIME ... trace.sourcepending=1 ... trace.actionid=1 ... trace.creditcachematch=1 ... trace.vanillahealing=86 ... trace.attackerma=14 ... trace.targetfaith=70 ... trace.expectedheal=210 | final=-210:FinalDamageFormula
```

The native pre-clamp rewrite authored the staged credit:

```text
[PRECLAMP-REWRITE ... hp=157/569 oldDebit=0 oldCredit=86 forcedDebit=0 forcedCredit=210 ... live=hp=367 ... cred1C6=210 ...]
```

The observed HP event matched the formula:

```text
[HEALING ptr=0x141855CE0 id=0x01] 157 -> 367 = 210
[HP-EVENT-PROBE kind=healing ... appliedHpGain=210 ... cred1C6=210 ...]
```

## Conclusions

Proven:

- Delayed/charged healing can be rewritten at execution through `+0x1C6`.
- The pending-action tracker can resolve healing via `credit-cache` just like damage via
  `damage-cache`.
- The formula context receives the true caster (`Agrias`, `id=0x1E`) and target (`Ramza`, `id=0x01`)
  without using CT.
- Natural preview and custom execution can diverge: preview stayed `+86`, execution became `+210`.

Caveat:

- After the first rewrite, the target cache reflected `cred1C6=210`, and the candidate logger queued
  another formula plan with `bb=2`. It did not produce a second observed heal in this run, but future
  profiles should set `PreClampFormulaPlanRequirePhaseZero=true` or otherwise avoid queueing plans
  after the result phase is already marked.
- The profile was restored to observe-only after the test to avoid leaving healing rewrites active.

Next hardening:

- Use `PreClampFormulaPlanRequirePhaseZero=true` for healing execution profiles.
- Consider making phase-zero gating the default for formula-backed pre-clamp plans, or adding an
  own-rewrite echo guard for staged debit/credit caches.
- Promote `credit-cache` variables into examples once the shipping action taxonomy is less test-specific
  than `actionId == 1`.
