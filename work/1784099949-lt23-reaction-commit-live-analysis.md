# LT23 reaction-commit live analysis

Source: `work\1784099229-lt23-dcl-reaction-commit-live.log`

## Checks

| Check | Result |
| --- | --- |
| all three guarded hooks installed | PASS |
| capture contains exactly two raw queue events | PASS |
| both raw events came from pass 1 | PASS |
| first event carried blank id 0 | PASS |
| second event carried ordinary Claw id 280 | PASS |
| no event carried a native Reaction id | PASS |
| no pass-2 Reaction commit was captured | PASS |

## Interpretation

Pass 1 at `0x206743` is not Reaction-specific. It fired for blank action id `0` and
ordinary ability id `280` (Claw), both without targets. The capture contains no accepted
native Reaction id (`422..453`), so it does not prove a Counter or Auto-Potion commit.
The universal three-pass accepted-Reaction hypothesis is refuted; pass 2 remains the
mapped real-code Reaction path and still needs a visible queued-Reaction correlation.

Overall: **PASS**
