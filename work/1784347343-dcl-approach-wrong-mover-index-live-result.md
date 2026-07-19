# DCL Approach live result: wrong mover table-index source

## Scope

This run exercised the installed Approach boundary hook during ordinary player and AI movement with
owner carrier `443` and delivery carrier `442`. The captured runtime log is
`1784347343-dcl-approach-wrong-mover-index-live.log` (SHA-256
`37860F0F4F1DC5AB63305CFF17ACB3AA42CD71DA4F2801DDA55CA41C5449C824`).

## Evidence

- The hook installed at boundary `0x1FE793`, completion gate `0x211E09`, and resume site
  `0xD7D0A81` without an install failure.
- Real routes reached the bridge. Rion produced route cursors `0..5` over a five-step route; later
  movers produced complete `1..3` and `1..5` sequences with cardinal tile changes.
- Every boundary was released as `invalid-boundary-fields` before eligibility or queueing.
- The recorded mover unit pointers and character ids were coherent, but `MoverTableIndex` was read
  from `unit+8`: Rion yielded `210`, while the same record is physical table slot `16`; the other
  observed records yielded `119` and `88`, while their physical slots are `6` and `0`.
- The unit record's byte `+1` equals its physical table slot in all three rows (`16`, `6`, `0`) and
  agrees with `(unitPtr - unitTableBase) / 0x200`.
- No Approach decision, queue command, owned pass-2 commit, continuation write, abort, or
  `reactionId=442` occurred. The visible battle therefore cannot validate an Approach/Counter
  execution.

## Conclusion

The boundary and route capture are live-confirmed, but the first control run was fail-open because
the bridge used the movement actor convention `actor+8` on the pointed battle-unit record. The
bridge must use `unit+1` for mover and reactor physical table identity and independently verify the
record pointer against the table base and `0x200` stride before it may own a native Reaction.

The run does not prove delivery `442`, Stop-hit execution, or movement resume.
