# LT25 — pass-2 reaction pre-selector snapshot

## Purpose

Observe the execution-only window immediately before selector `0x282E38` consumes/clears exact
candidate words at `unit+0x1CE`. This settles whether Counter `442` appears on the expected reactor,
whether source global RVA `0x186AFF4` still names the attacker, and whether ordinary actions reach
the same window with an empty candidate set. The probe never stages a carrier or writes game memory.

## Gate order

1. LT23 must install all three commit hooks and classify at least Counter by queue pass.
2. Validate/build LT25 with zero errors and require the `reaction-preselector-p2` anchor to pass.
3. Deploy LT25 alone; do not combine it with action replacement or any chance/control profile.
4. Require `[DCL-REACTION-PRESELECT-HOOK] ... (observe-only)` before entering battle.

## Capture sequence

1. Load Enhanced save 05 through Manual Saves.
2. Open/cancel the same forecast three times. Expect no pre-selector event.
3. Execute an action against a unit with Counter `442` until the visible Reaction triggers.
4. Match the provoking action to `[DCL-REACTION-PRESELECT]` and record:
   source index, eval Reaction id, incoming actor/index/action ids, record index, and every
   `unitIndex:candidateId:active` tuple.
5. Execute the same action against a unit without a compatible Reaction and capture the empty/no
   candidate case.
6. If available, capture one status-applying action without a Reaction to test whether `+0x1CE`
   carries unrelated status-mask data at this exact queue phase.
7. Exit normally and retain the complete bounded log.

## Pass gate

- Forecast-only evaluation emits no pre-selector event.
- The Counter case snapshots exactly one `reactorIndex:442:active=True` tuple before the matching
  LT23-style pass-2 commit.
- `sourceIdx` points to the original attacker and differs from the reactor.
- The incoming actor/record context is readable and directionally consistent.
- A no-Reaction action reaches the window without an exact Reaction candidate.
- Any non-Reaction value at `unit+0x1CE` is classified before the field is considered safe for a
  future producer write.

## Hard fail

Hook mismatch/failure, crash, changed action behavior, forecast event, invalid source direction,
multiple unexplained candidate ids, or evidence that the same `+0x1CE` word still owns an unrelated
effect at this phase. On failure, retain evidence and do not implement carrier staging.

## Follow-up only after pass

Prepare a separate log-only producer plan that reports `would-stage unit=<reactor> carrier=442`
from an already cached DCL trigger decision. A live write remains a later one-shot gate.
