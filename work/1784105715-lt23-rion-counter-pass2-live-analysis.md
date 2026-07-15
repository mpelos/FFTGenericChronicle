# LT23 reaction-commit live analysis

Source: `work\1784105542-lt23-rion-counter-pass2-live.log`
Scenario: `counter-pass2`

## Checks

| Check | Result |
| --- | --- |
| all three guarded hooks installed | PASS |
| capture contains native Reaction 442 | PASS |
| every native Reaction commit came from pass 2 | PASS |
| all captured Counter id copies agree | PASS |
| Rion Counter commit owns reactor 4, source 0, target [0] | PASS |
| Rion survived the immediately preceding damage transaction | PASS |
| Counter commit is followed by two chained damage transactions | PASS |
| generic non-Reaction queue traffic remains isolated as pass-1 noise | PASS |

## Native Reaction events

| Event | Pass | Reaction | Reactor | Source | Targets | IDs agree |
| ---: | ---: | ---: | ---: | ---: | --- | --- |
| 3 | 2 | 442 | 4 | 0 | `(0,)` | True |
| 5 | 2 | 442 | 1 | 16 | `()` | True |

## Interpretation

The primary event is event 3: reactor 4 survived `277 -> 37` HP, then pass 2 committed Reaction 442 for target `0`.
Before the next queue event, the same target received chained damage `192 -> 3` and `3 -> 0`, matching Rion's dual-wield native Counter execution.
Pass 2 at `0x206421` is therefore the accepted native Reaction commit boundary for Counter;
pass-1 ordinary queue traffic remains outside Reaction ownership.

Overall: **PASS**
