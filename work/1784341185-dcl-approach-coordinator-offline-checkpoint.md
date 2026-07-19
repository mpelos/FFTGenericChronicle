# DCL Approach coordinator — offline checkpoint

## Scope

This checkpoint covers the generic combat mechanism for interrupting a native movement route after
an entered tile. It does not select a job, ability owner, reach formula, balance value, damage
formula, or visual policy.

## Evidence fixed before implementation

- Native movement dispatch uses battle state `0x11`, calls updater `0x1FE59C`, and completes to
  state `0x12` through `0x203ED4`.
- RVA `0x1FE793` is the synchronous idle boundary after a completed tile and before another route
  byte is consumed. Its containing updater can return through epilogue `0x1FE940` without consuming
  the next byte.
- Native Reaction dispatch calls queue `0x206344` from state `0x1E`; accepted work sets state
  `0x29` and post-delivery cleanup chains the same queue at `0xD90CF99`.
- An empty post-Reaction chain reaches the native state-`0x28` continuation at `0xD7D0A81`.
- Movement actor resolver `0x26079C` and execution actor resolver `0x2607C0` walk the same actor
  list using separate selector globals.
- No native control-flow edge connects the movement route directly to the Reaction queue or maps
  terminal `0x28` back to movement state `0x11`.

Static evidence: `work/1784340421-dcl-approach-interrupt-analysis.md` and
`work/1784340422-dcl-reaction-queue-analysis.md`.

## Implemented offline protocol

`DclApproachCoordinator` owns a fail-closed transaction with these phases:

```text
Idle/Baseline
  -> AwaitingDecision       exact next cardinal cursor, route paused
  -> Released              no eligible reactor
  -> Armed                 identities, empty mailboxes, state and deadline agree
  -> QueueRunning          boundary and staged mailboxes revalidated synchronously
  -> AwaitingResume        native queue accepted at least one delivery
  -> Resumed               exact owned commit and terminal 0x28 become state 0x11

Any mismatch -> Aborted     no stage permission and no continuation ownership
```

The immutable boundary identity contains battle generation, native sequence, mover pointer/table
index/character id, accepted-route signature, route length/cursor, and current tile. The transaction
accepts only one cursor increment and one cardinal entered-tile edge. Repeated observations of the
same boundary are idempotent, and the first updater re-entry on a released/resumed tile is treated
as the same step rather than another Approach event.

Candidate reservations contain reactor pointer/table index/character id plus separate authored
owner and native delivery Reaction ids. The coordinator does not choose those values. It rejects
the mover as its own reactor, duplicate reactors, invalid native ids, an occupied native mailbox
set, lost movement state, expired decisions, changed route identity, stale live snapshots, staged
mailbox mismatches, foreign commit rows, and terminal states other than `0x28`.

The continuation permission requires at least one exact pass-2 commit whose source table index is
the mover and whose reactor identity is in the armed set. It is one-shot. Unit removal and battle
generation/settings resets clear the coordinator.

## Offline validation

- Release build: PASS, zero warnings and zero errors.
- Formula/runtime smoke tests: PASS.
- Dedicated Approach smoke coverage: route origin, next-step pause, replay, no-reactor release,
  same-step re-entry, later step, occupied mailbox rejection, arm/revalidation, exact commit,
  duplicate commit rejection, accepted queue, non-`0x28` rejection, one-shot `0x28 -> 0x11`,
  post-resume movement, timeout, route replacement, foreign commit, and unit removal.
- Static executable anchor check: `python tools/analyze_dcl_approach_interrupt.py --check-only` PASS.

## Remaining native bridge

No live-control write is enabled by this checkpoint. The remaining code bridge must:

1. copy the exact boundary snapshot synchronously at `0x1FE793`;
2. return through `0x1FE940` while a bounded managed decision is pending;
3. release through the original instruction stream on no-reactor, timeout, or mismatch;
4. snapshot all 21 `unit+0x1CE` mailboxes and stage only the coordinator-owned candidate set;
5. reset queue pass and call `0x206344` on the game thread after exact revalidation;
6. skip the remainder of the movement updater if the queue returns accepted;
7. stamp exact producer-owned pass-2 commits synchronously;
8. replace the write at `0xD7D0A81` from `0x28` to `0x11` only for that stamped transaction;
9. clear ownership before the route consumes another byte.

The first live control probe remains bounded to one reactor and one write budget. It must prove the
state path `0x11 -> 0x29 -> ... -> 0x11`, unchanged mover/route cursor at resumption, exactly one
entered-tile event, and unchanged native terminal `0x28` behavior outside the owned transaction.
