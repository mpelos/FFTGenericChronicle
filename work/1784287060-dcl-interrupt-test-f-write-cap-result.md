# DCL Interrupt test F — write-cap preservation

## Purpose

Validate that the bounded Interrupt producer fails closed after its configured write budget is
spent and leaves the next charged action intact.

## Paired protocol

The decisive pair ran under one runtime initialization and without Retry, Continue, or settings
reload between the two matched attempts:

1. Josephine queued self-targeted Death (`ability 30`, timer `5`).
2. Rion used Potion on Josephine; the rule cancelled Death and recorded `writes=1`.
3. Rion later used Potion on himself. The rule returned `condition-false`; this did not consume an
   Interrupt write.
4. Josephine queued self-targeted Death again with the same timer and action identity.
5. Rion used Potion on Josephine again; the producer returned `write-cap` and preserved the pending
   state.
6. Rion ended his turn. Death remained in the timeline, executed its native animation, debited 24
   MP from Josephine, cleared the pending bit, and disappeared from the timeline.

## Evidence

- Archived raw log: `work/1784286980-dcl-interrupt-test-f-write-cap-pass.log`
- Bytes: `202676`
- SHA-256: `1507DF10E3892F4651958882270C99EDC73CC83774585D4D9183839F3B7DE402`
- First matched attempt, line 931:

  ```text
  [DCL-INTERRUPT] rule=Potion temporary Interrupt probe carrier caster=0x80 target=0x81 ability=368 resistance=14 roll=18 outcome=cancelled before=effective=0x08/source=0x00/timer=5/type=0x0B/action=30/master=0x08 after=effective=0x00/source=0x00/timer=255/type=0x0B/action=30/master=0x00 writes=1
  ```

- Intervening negative control, line 1008:

  ```text
  [DCL-INTERRUPT] rule=Potion temporary Interrupt probe carrier caster=0x80 target=0x80 ability=368 outcome=condition-false pending=effective=0x00/source=0x00/timer=255/type=0x06/action=368/master=0x00
  ```

- Second matched attempt, line 1176:

  ```text
  [DCL-INTERRUPT] rule=Potion temporary Interrupt probe carrier caster=0x80 target=0x81 ability=368 resistance=14 roll=18 outcome=write-cap pending=effective=0x08/source=0x00/timer=5/type=0x0B/action=30/master=0x08
  ```

- Death execution reaches a non-cached calculation for `caster=0x81 target=0x81 abilityId=30`, the
  pending bit changes `0x08 -> 0x00`, and the charged action leaves the timeline.

## Harness limitation

`PreClampManagedCallbackForcedDebit=1` was hot-reloaded only as a survival harness. It forced
pre-clamp debits to one, and the existing zero-reduction path restored those one-point writes. The
target therefore remained alive. This run proves that write-cap preserves the queued transaction
through native execution; it is not evidence about final Death damage, instant-KO policy, or normal
combat magnitude.

## Verdict

Pass. With `DclInterruptMaxWrites=1`, the first matched cancellation consumes the only mutation and
the second matched attempt returns `write-cap` without changing the pending owner. A non-matching
attempt between them does not consume the budget.
