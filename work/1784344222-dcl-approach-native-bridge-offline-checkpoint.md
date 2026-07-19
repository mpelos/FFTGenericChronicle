# DCL Approach native bridge offline checkpoint

## Scope

This checkpoint closes the offline implementation surface for the job-free entered-reach
movement interruption. It does not assign a job, final Reaction identity, damage policy, or
presentation policy.

## Implemented transaction

- The synchronous boundary at `0x1FE793` publishes the exact movement actor/unit, accepted
  128-byte route, cursor, and entered tile before another route byte is consumed.
- The managed coordinator recognizes only the next cardinal edge on the same battle generation,
  mover identity, and route signature. Repeated callbacks are idempotent.
- Candidate selection requires an opposing living exact owner, an existing equipped weapon, and an
  outside-to-inside transition into the configured weapon-reach band.
- All 21 native Reaction mailboxes and private synthetic states must be empty before staging.
- The game thread revalidates the complete paused boundary and exact mailbox set, forces only queue
  pass 2, and uses the queue's boolean return to accept or release the transaction.
- The shared pass-2 commit hook stamps only an exact armed delivery, reactor, source index, and unit
  table identity.
- Accepted work bypasses the final-step state-`0x12` overwrite at `0x211E09`.
- The post-Reaction shim after `0xD7D0A81` substitutes terminal `0x28` with `0x11` only after an
  owned commit, unchanged mover route/cursor/tile, exact terminal state, and bounded write budget.
- `ResumeWritten` and retained `ResumeReleased` both close the managed audit, preventing a polling
  interval from losing confirmation of a native one-shot resume.
- Battle/reset/disposal cleanup rolls back only exact staged delivery values, clears the unmanaged
  block, clears coordinator ownership, and releases all three hook references/buffer memory.

## Offline evidence

- `dotnet build` succeeds with zero warnings and zero errors.
- The complete formula-runtime smoke suite passes, including Approach coordinator, assembly layout,
  eligibility, idempotency, timeout, foreign commit, lifecycle, and positive/negative settings
  validation.
- `analyze_dcl_reaction_queue.py --check-only` passes every queue anchor and executable caller.
- `analyze_dcl_approach_interrupt.py --check-only` passes every movement, state, queue, completion,
  and resume anchor.
- `codemod/run-offline-checks.ps1 -SkipGitDiffCheck` completes with `Offline checks passed`.

## Remaining live gate

Static evidence cannot prove that the direct queue call preserves every live VM/trace continuation
field. The next test therefore enables one bounded owner-`443` / delivery-`442` entered-reach event
against the audited Rion fixture and requires the complete owned path
`0x11 -> 0x29 -> ... -> 0x11`, the same mover and route cursor, one continuation write, and normal
route completion. Any hook failure, abort, audit mismatch, duplicate commit, write count above one,
crash, or stalled battle is a hard stop.
