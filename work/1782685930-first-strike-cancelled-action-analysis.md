# First Strike / Hamedo Cancelled-Action Analysis

This is a dated work note. Canonical facts belong in `docs/modding/` only after more evidence.

## Input Evidence

Primary log:

```text
work/1782695389-reaction-nohp-selector-log.txt
```

User-observed case:

```text
Cloud -> Ninja basic attack
Ninja reaction: First Strike / counter-before
Preview: 100% hit chance
Result: counter-before reaction
```

Context from the analyzer report:

```text
work/1782695389-reaction-nohp-selector-report.md
```

The analyzer cleanly sees the reaction damage:

- target/result unit: Cloud (`id=0x32`);
- source/caster unit: Ninja (`id=0x80`);
- action id: `0` basic attack / implicit weapon;
- selector rows `event=2` and `event=3` are normal `evadeType=0x00(hit)` rows for Ninja hitting
  Cloud.

## What The Log Shows

The cancelled incoming attack appears to leave a staged target cache on Ninja before the reaction
damage resolves:

```text
line 264: [PENDING-ACTION-TARGET reenter target=.../id=0x80 ... next=dmg1C4=422 ... f1E5=128 ...]
line 265: [ACTION-STATE ptr=... id=0x80 ... dmg1C4=422 ... f1E5=128 ... bb=1]
line 273: [PENDING-ACTION-MATCH ... target=.../id=0x80 ... observed=422 ...]
line 274: [PRECLAMP-IMMEDIATE-CANDIDATES target=.../id=0x80 oldDebit=422 ... selected=.../id=0x32/act=0 ...]
line 275: [PRECLAMP-FORMULA-CANDIDATE ... ptr=... id=0x80 ... oldDebit=422 ... attacker=.../id=0x32 source=immediate-action ...]
```

Interpretation:

- `id=0x80` is the Ninja target of Cloud's attempted attack.
- `id=0x32` is Cloud, the attempted attacker.
- `dmg1C4=422`, `f1E5=128`, and `oldDebit=422` look like the attack result/cache that would have
  been applied to Ninja if the action were not interrupted.
- This cache is not enough by itself to prove final damage because the reaction interrupts before a
  final HP-change path runs.

The same target then has unstable/conflicting immediate-source candidates:

```text
line 267: target Ninja, selected Agrias (`id=0x1E`)
line 274: target Ninja, selected Cloud (`id=0x32`)
line 278: target Ninja, selected none
line 284: target Ninja, selected Cloud (`id=0x32`)
```

Interpretation:

- During First Strike/counter-before, multiple units can remain active-source-like for a short window.
- The existing immediate-source heuristic can pick the right original attacker in some samples, but
  it is not clean enough to be treated as the primary solution for interrupted incoming actions.

The reaction's own damage then resolves as an ordinary HP path:

```text
line 318: [SELECTOR-PROBE event=2 ... unit:id=0x32 ... rec+1C4(dmg)=396 ...]
line 320: [PRECLAMP-ACTOR-CTX event=5 ... target=.../id=0x32 ... caster=.../id=0x80 ... actionId=0 verdict=resolved ...]
line 331: [HP-EVENT-PROBE kind=damage event=2 ptr=... id=0x32 ... appliedHpLoss=38 ... rawForecastDamage=396 ... lethal=1 ...]
line 337: [SELECTOR-PROBE event=3 ... unit:id=0x32 ... rdx=...id=0x80...act=0 ...]
```

Interpretation:

- Reaction damage is visible to pre-clamp, selector, HP-event, and actor-context.
- The reaction attacker is the Ninja (`id=0x80`), and the reaction target is Cloud (`id=0x32`).
- This covers DCL attribution for the damage dealt by First Strike, but not yet the identity of the
  incoming action that First Strike cancelled.

## Current Conclusion

The current evidence separates First Strike / Hamedo-like preemption into two surfaces:

1. **Interrupted incoming action surface**: target cache/action-boundary on the original target,
   e.g. Ninja has `dmg1C4=422`, `f1E5=0x80`, and immediate candidates can include Cloud. This does
   not reach a clean HP apply or selector row in this log.
2. **Reaction damage surface**: ordinary HP apply/selector path where Ninja hits Cloud. This is
   already source-resolvable through actor context and selector actor refs.

The DCL can probably control the reaction damage with the existing HP/selector path. The harder
open question is whether the DCL can reliably identify and author the incoming action before the
reaction cancels it.

## Runtime Implication

For Hamedo/First Strike, a shipping resolver likely needs an upstream "interrupted incoming action"
record rather than relying only on final HP apply:

- original source/caster;
- original action id;
- original intended target;
- predicted/staged debit or outcome;
- reaction defender/source;
- whether the original action was cancelled, transformed, or continued.

The current action-boundary/target-cache probe is close, but it is not yet authoritative. It can
mis-pick source while multiple units are active-source-like, and it has only been observed for a
basic incoming action.

## Next Live Tests

Higher priority remains the named no-HP selector test:

```text
Agrias -> Ramza with Divine Ruination or Hallowed Bolt
confirm only if the preview shows Shirahadori / 0% / no-HP
desired row: selector no-HP with source act=159 or act=158
```

After that, run a dedicated First Strike/Hamedo cancellation test:

```text
Cloud or Agrias uses a named physical action into a First Strike/Hamedo defender.
Goal A: confirm whether the interrupted incoming target cache carries source actor/action id > 0.
Goal B: confirm whether there is any selector row for the cancelled incoming action, or only for the reaction damage.
Goal C: determine whether the reaction damage keeps using actionId=0 or carries a distinct reaction id.
```

The next profile for that test should log:

- selector actor refs;
- pre-clamp actor context;
- immediate candidates;
- action-boundary diffs;
- hook/register context on the target-cache event before HP apply;
- enough stack/register actor refs around the target-cache event to avoid relying on the recency
  heuristic alone.
