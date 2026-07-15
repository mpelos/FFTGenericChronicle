# DCL accepted Reaction order rewrite checkpoint

## Scope

This checkpoint follows LT39's live proof of the post-materialization/pre-actor boundary and records
the first fail-closed order-level replacement/retarget implementation. No second live run was used.

## Implemented controller

`DclReactionOrderRewriteEnabled` shares the exact-byte-guarded materialization hook at RVA
`0x2063BD`. It is disabled and log-only by default. One exact native Reaction carrier may:

- replace executable order type `+0x01` and payload `+0x02`;
- retarget delivery to the incoming source by writing mode `5`, source unit index, and source
  x/layer/y coordinates;
- perform both changes atomically before actor construction.

The source coordinates use the same native fields mapped from the typed-order helper:

- x: `unit+0x4F` -> order `+0x0C`
- layer: `unit+0x51.bit7` -> order `+0x0E`
- y: `unit+0x50` -> order `+0x10`

## Fail-closed contract

- The materialization probe and its expected-byte guard are mandatory.
- Carrier id is exact and restricted to native Reaction ids `422..453`.
- Selected and source unit-table indices must remain within `0..20`.
- Live mode requires exact expected native action type and payload; wildcard `-1` is log-only.
- Live writes have an independent `1..32` cap.
- Invalid index, original-order mismatch, invalid source, and exhausted cap preserve the native
  order and emit distinct audit dispositions.
- Each row retains the original four-byte order head and the complete final 20-byte order.

## Verification

- `tools/analyze_dcl_reaction_order_rewrite.py` passes every implementation/validator anchor.
- `tools/test_dcl_reaction_order_rewrite.py` removes each anchor in turn and proves the gate fails.
- Runtime settings smoke tests reject invalid, unguarded live settings and accept one fully guarded
  live shape.
- Release build succeeds with zero warnings and zero errors.
- The full offline gate passes, including all Python checks, JSON validation, C# build, and formula
  runtime smoke tests.
- Current reports:
  - `work/1784111051-dcl-reaction-order-rewrite-analysis.md`
  - `work/1784111086-dcl-implementation-coverage.md`

## Remaining live gate

Carrier `443` Hex Ward is the first intended consumer. Its smallest bounded live vertical is one
known active reactor, one pre-selector producer write, exact expected generic order type `0` /
payload `443`, one source-retarget write at `0x2063BD`, one pass-2 commit, and one state-`0x2C`
effect audit. Managed Blind/Brave delivery and once-per-Reaction cadence remain separate offline
work and should be completed before broad behavior testing.

The installed game, Reloaded-II profile, runtime DLL/settings/log, and autosave remain restored to
their pre-LT39 hashes; this offline implementation was not deployed.
