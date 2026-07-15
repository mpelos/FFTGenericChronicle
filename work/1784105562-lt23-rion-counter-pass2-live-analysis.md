# LT23 reaction-commit live analysis

Source: `work\1784105542-lt23-rion-counter-pass2-live.log`

## Checks

| Check | Result |
| --- | --- |
| all three guarded hooks installed | PASS |
| capture contains exactly two raw queue events | PASS |
| both raw events came from pass 1 | FAIL |
| first event carried blank id 0 | FAIL |
| second event carried ordinary Claw id 280 | FAIL |
| no event carried a native Reaction id | FAIL |
| no pass-2 Reaction commit was captured | FAIL |

## Interpretation

Pass 1 at `0x206743` is not Reaction-specific. It fired for blank action id `0` and
ordinary ability id `280` (Claw), both without targets. The capture contains no accepted
native Reaction id (`422..453`), so it does not prove a Counter or Auto-Potion commit.
The universal three-pass accepted-Reaction hypothesis is refuted; pass 2 remains the
mapped real-code Reaction path and still needs a visible queued-Reaction correlation.

Overall: **FAIL**
