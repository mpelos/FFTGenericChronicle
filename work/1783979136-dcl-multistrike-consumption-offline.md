# DCL multistrike decision consumption — offline checkpoint

## Scope

This pass audits whether the current DCL runtime can satisfy the locked multistrike rule: every
strike gets an independent attack roll, an independent defense roll, and spends one selected finite
defense. No live game state was changed. The installed LT14 profile remains untouched.

Target executable: Steam Enhanced v1.5.1, SHA-256
`841DD4048C9C33958156422CD96EE8D064F5BEB3C5F8A0E23A68AAF2BB87B282`.

## Durable evidence already available

- Dual wield produces two separate native pre-clamp events, each with its own staged debit. This is
  proven by the old Ninja -> Agrias capture and owned by
  `docs/modding/04-engine-memory-model.md`.
- LT11 produced one clean selector delivery for its authored miss.
- LT11 installed the old `0x205B38` result-kind hook, but its raw log contains no `[DCL-KIND]`
  event. The cache's previous two-consumer handshake therefore never completed and the decision
  lingered until TTL.

## Code defect fixed offline

`DclSelectorOutcomeCallbackImpl` now marks the selector as the outcome-delivery half of the cache
handshake for both hit and miss decisions. The old kind hook marks the same idempotent side if it
ever fires. `DclHitDecisionCache` names this side `OutcomeConsumed`, rather than claiming that only
the kind-commit hook can deliver it.

The selector now also logs cached DCL hits (`decision=hit kept`). This is diagnostic coverage needed
to count delivery per strike; it does not alter the native hit record.

Offline tests cover:

- selector before pre-clamp;
- pre-clamp before selector;
- duplicate calls from either consumer;
- immediate retirement only after both sides have fired.

Build `MultiHitAudit`: 0 warnings, 0 errors. Full formula/runtime smoke suite: pass.

## Current-build static result

In v1.5.1, the single-action real-code handler begins at `0x307DF8` and has one direct call to the
calc-entry function at `0x307ED0`:

```text
307EA7  read caster +0x1A1
307EAE  read caster +0x1A2
307EB5  write action type 1
307EBC  write ability id
307EC3  rcx = caster +0x1A0 (order record)
307ECA  dl = target index global
307ED0  call 0x3099AC (calc entry)
307ED5..307EE9 restore caster action fields
```

`0x3099AC` has two direct real-code callers in the executable: the known preview/AI target sweep and
this single-action handler. The single-action handler contains no real-code loop around the call.
The VM may still re-enter the handler or generate a later strike internally, so this is not proof
that calc-entry fires once per dual-wield action. It does show that no visible real-code strike loop
provides an index or an extra call that can be safely assumed offline.

## Remaining uncertainty

The current cache key is `(target, caster, ability, actionType)`; it has no strike index. If dual
wield produces only one execution calc-entry, the second pre-clamp event may see no decision after
the first selector/pre-clamp pair retires it. If it produces two calc entries before the first
decision is retired, the second can incorrectly reuse the first roll. Both failure modes violate
independent per-strike contests.

The exact sequence among these events is not recoverable from old captures because they predate the
calc-entry/selector DCL logging:

```text
calc-entry / DCL-HIT
reaction gate
pre-clamp / DCL-GUARD
selector
second-strike calc-entry (if any)
second pre-clamp
second selector
```

No HP value, time window, or target phase byte is accepted as a synthetic strike id: those would be
heuristics, not an engine mechanism.

## Minimal live gate (folded into LT15)

Use a player-controlled dual-wield basic Attack while the deterministic LT15 guard profile is
active. LT15 forces attack roll 10, defense roll 12, Block 13 (one use), Parry 11 (one use), Dodge 8.
One dual-wield execution must therefore resolve:

1. first strike: Block succeeds, authored defense/miss, Block `1 -> 0`;
2. second strike: Parry is selected, its roll fails, native damage lands, Parry `1 -> 0`.

The raw log must show two distinct `[DCL-HIT]` decisions, two corresponding pre-clamp outcomes, two
selector deliveries, and two guard commits in strike order. Merely showing two damage applications
is insufficient.

If the gate fails, the next implementation must be based on the observed ordering:

- second calc exists after first retirement: current key is adequate once consumption is correct;
- second calc exists before first retirement: add an execution/strike generation to the cache;
- no second calc: generate the next contest at the proven per-strike pre-clamp boundary, while
  preserving the first strike's selector decision until its delivery is complete.

This live gate is mandatory before changing contest generation, because each possible ordering
requires a different safe lifecycle.
