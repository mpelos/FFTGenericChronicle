# DCL Approach final-tile pause live result

Evidence archive: `work/1784352805-dcl-approach-final-tile-live.log`

SHA-256: `1F4A9E6C5EC91EE01910D4E71CBBD11F661FDDCE099A974251096B925D7D77C9`

Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
Runtime configuration: Approach owner `443`, delivery `442`, horizontal reach `1..2`, same-layer,
one continuation write.

## Result

The source-mailbox correction removed the earlier false conflict from Janus's own route. Janus's
route produced consecutive final-step decisions at events `12` and `13`: event `12` had no eligible
reactor, then event `13` found one eligible reactor only after the shared battle state had already
left movement. The coordinator therefore released fail-closed with
`movement-state-lost/eligible`; it did not stage a mailbox or invoke the Reaction queue.

There is no Approach `reactionId=442` commit, accepted delivery, materialization, effect, or resume
row in this execution. Later `native-mailbox-0:442` conflicts belong to routes whose movers were not
slot `0`; preserving the source-owned word does not permit it to coexist when slot `0` is an
unrelated reactor for a different mover.

## Offline reconciliation

The boundary shim returns through movement-updater epilogue `0x1FE940` while publishing
`PendingDecision`. On a final route step, control then reaches the state-`0x11` post-updater
completion gate at `0x211E09`. The current completion guard skips ordinary completion only for
`QueueAccepted`. While the poller is still processing `PendingDecision`, the unguarded native tail
can therefore call `0x203ED4` and replace movement state `0x11` with `0x12`.

## Offline correction and gates

The matching-actor completion guard now retains state `0x11` while the native bridge is
`PendingDecision`, `InvokeQueue`, or `QueueAccepted`. `Release`, `QueueRejected`, and
`ResumeReleased` continue through ordinary completion so the final route can terminate normally
after a declined, failed, or completed transaction.

- Release build succeeds with zero warnings and zero errors.
- The formula-runtime smoke suite passes, including assembly assertions for all three owned
  completion states.
- `analyze_dcl_approach_interrupt.py --check-only` passes.
- `analyze_dcl_reaction_queue.py --check-only` passes.

The next live falsifier must reproduce Janus's final-step entry, stage Rion's slot `16`, obtain one
pass-2 commit/delivery/effect, replace terminal `0x28` with `0x11` exactly once, and then allow the
same final route to complete normally through `0x12`.
