# DCL Approach corrected-index live result: selector-excluded source mailbox

Evidence archive: `work/1784350216-dcl-approach-source-mailbox-live.log`

SHA-256: `3A03F0E0484FDFA63D0133F1A8E19FB87FEE43E15B6D69C89B973EE34AE71A4B`

Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
Runtime configuration: Approach owner `443`, delivery `442`, horizontal reach `1..2`, same-layer,
one continuation write.

## Result

The physical battle-table identity correction passed its live falsifier. The bridge recognized valid
route boundaries and emitted no `invalid-boundary-fields` row. Rion's physical slot was `16`, Wenyld's
was `6`, and Janus's was `0`.

No Approach delivery committed or executed in this run. Rion's own route released without an eligible
reactor, and Wenyld did not enter the owner-controlled reach band. When Janus began moving, the bridge
released with `native-mailbox-not-empty/native-mailbox-0:442`.

Janus's initial 512-byte unit dump contains `0x0000` at `unit+0x1CE`; the `442` therefore appeared after
initial observation and before Janus's first captured movement boundary. There is no corresponding
`reactionId=442` commit, accepted-delivery, materialization, or effect row. The word is occupancy only,
not evidence that Counter occurred.

## Static reconciliation

Pass-2 selector `0x282E38` compares each scan index with source-index global `0x186AFF4` before reading
`unit+0x1CE`. The equal/source slot skips directly to the next unit; a word there persists without being
read or cleared. This is owned by `docs/modding/04-engine-memory-model.md` and independently preserved in
`work/1784157052-dcl-reaction-queue-analysis.md`.

The bridge's all-21-empty precondition was therefore too strict. Only a native word on the current
mover/source is compatible; a native word on any other slot and every private synthetic reservation
remain transaction conflicts. Native revalidation must also prove that the source is absent from the
Approach candidate mask and must preserve its word on both acceptance and rollback.

## Offline correction and gates

- Managed mailbox policy now accepts a native word only on the current mover/source.
- Managed staged-mailbox revalidation ignores only that selector-excluded source slot.
- The native game-thread shim skips that source slot, aborts if its bit is present in the candidate
  mask, and continues to require exact delivery ids for candidates and zero for every other slot.
- Source synthetic reservations remain conflicts because their coexistence has not been proven.
- Release build passes with zero warnings and zero errors.
- Formula runtime smoke tests pass, including explicit source/non-source/synthetic mailbox cases.
- `analyze_dcl_approach_interrupt.py --check-only` passes.
- `analyze_dcl_reaction_queue.py --check-only` passes.

The next live gate must reproduce Janus's source `442`, log it as
`source-mailbox-0:442-selector-excluded`, stage Rion's slot `16`, obtain one pass-2 commit/delivery/effect,
and resume the same Janus route exactly once.
