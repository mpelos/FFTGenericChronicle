# DCL final-tile negative live pass

## Inputs

- Runtime profile: `work/1784469176-battle-runtime-settings.dcl-final-tile-convergence-observe.json`.
- Runtime-log SHA-256: `65532E0E96DFBDD25C807D9472B0BFBA11E1AE28C2E1A5F2155DC4CE65388C46`.
- Baseline controlled-unit event: event `3`, ally index `17`, character `129`.

## Sequence

The controlled unit finished its prior move and ended the turn. On the next controlled turn, Move
was opened, a different preview tile was selected, and Move was cancelled without confirmation.
The unit then completed the turn with Wait without moving.

Five subsequent final-tile events were published, all for enemy units:

| Event | Unit index | Cursor/length | Final tile |
| ---: | ---: | ---: | --- |
| 4 | 1 | `2/0` | `6,7,0` |
| 5 | 7 | `5/0` | `3,4,0` |
| 6 | 4 | `4/0` | `8,9,0` |
| 7 | 6 | `4/0` | `5,10,0` |
| 8 | 0 | `3/0` | `8,10,0` |

No event after baseline `3` belongs to allied unit index `17`. No hook failure, skip, ring loss,
crash, or movement slowdown appeared.

## Conclusions

- **Proven live:** Move preview and cancellation do not reach finalizer convergence.
- **Proven live:** Wait without movement does not publish a final-tile event.
- **Proven live:** ordinary consecutive enemy movements each publish once.
- **Proven live:** post-finalizer route hash is not a route identity. Events `1` and `4` for the
  same actor have different destinations but the same hash because cleared/stale route bytes are
  reused. Cross-movement hash/tile deduplication can suppress a later legitimate return movement
  and is removed.

An explicit `cursor=0/length=0` live settlement remains unobserved; the validator rejects it
offline as `no-movement`.
