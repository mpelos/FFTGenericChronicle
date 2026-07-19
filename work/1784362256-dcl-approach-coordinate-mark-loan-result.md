# DCL Approach coordinate-and-mark loan live result

## Scope

Validate the terminal-step Approach bridge after replacing the invalid mark-only loan with a bounded
loan of the mover's entered coordinates and target-map mark through native queue pass 2.

Fixture and route:

- Enhanced battle fixture with Rion set to Auto-battle against Wenyld.
- Rion advances and attacks Wenyld; Wenyld acts; Janus then advances toward Rion.
- Approach owner reaction `443`, native delivery carrier `442`, reach `1-2`.

Raw evidence: `1784362189-dcl-approach-coordinate-mark-loan-live.log`

SHA-256: `ECE631E08CA5553D36998494CD9BC1FB5A5B0668049AAFDCCE492E328906B29D`

Machine gate:

`python tools/analyze_dcl_approach_live.py work/1784362189-dcl-approach-coordinate-mark-loan-live.log --event 15 --minimum-effects 2`

Result: `Approach live evidence PASS`.

## Result

The bridge succeeds at terminal movement event `15`:

- entered route tile: `5,3,0` at cursor `3/3`;
- one eligible candidate: mask `0x10000`;
- queue pass 2 accepts exactly once: `accepted=1`, `commitMask=0x10000`;
- target mark is restored byte-exactly: `0x20 -> 0x60 -> 0x20`;
- the stale battle-unit tuple is restored byte-exactly:
  `2,3,0/raw51=0x03 -> 5,3,0/raw51=0x03 -> 2,3,0/raw51=0x03`;
- all bridge diagnostics complete through stage `9`.

Native delivery evidence:

- typed-family validator accepts `442` with `result=0`;
- final validator accepts `442` with `result=0`;
- the special order materializes as native action `1/0`, item `124`, target mode `5`, target index `0`;
- the producer-owned pass-2 commit is unique;
- two state-`0x2C` effect rows occur, matching Rion's Dual Wield delivery;
- the owned continuation substitutes native `0x28` with movement `0x11` once;
- resume audit passes with `writes=1`, then the same battle continues normally.

The live test therefore proves the coordinate-and-mark loan is sufficient for Counter `442` to pass
both native validators, materialize, execute, and return control to the paused terminal route without
leaking either borrowed coordinate or target-map state.

## Mailbox observation

The source unit already carries mailbox value `442` before the Approach candidate is staged. Pass 2
skips the source index, so the value remains present during this transaction. Once later movers use a
different source index, the conservative Approach mailbox policy reports
`native-mailbox-0:442` and releases those scans without staging another candidate. This is not
evidence that the coordinate/mark bridge leaked a mailbox write; the same value is logged before the
successful event as `source-mailbox-0:442-selector-excluded`. Its longer native lifecycle remains a
separate investigation.

The raw capture later reaches another accepted Approach at event `45`, but the game is closed before
that transaction emits its resume rows. The proof above deliberately isolates complete event `15`;
the truncated event `45` is neither counted as a successful resume nor used to support the durable
claim.
