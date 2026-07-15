# LT23 reaction-commit live analysis

Source: `work\1784106049-lt23-auto-potion-pass2-live.log`
Scenario: `auto-potion-pass2`

## Checks

| Check | Result |
| --- | --- |
| all three guarded hooks installed | PASS |
| capture contains native Reaction 441 | PASS |
| every native Reaction commit came from pass 2 | PASS |
| all captured Auto-Potion id copies agree | PASS |
| Josephine Auto-Potion commit owns reactor 2, source 17, and no explicit target | PASS |
| generic non-Reaction queue traffic remains isolated as pass-1 noise | PASS |

## Native Reaction events

| Event | Pass | Reaction | Reactor | Source | Targets | IDs agree |
| ---: | ---: | ---: | ---: | ---: | --- | --- |
| 2 | 2 | 441 | 2 | 17 | `()` | True |

## Interpretation

The accepted Auto-Potion event carries Reaction id `441` in both actor id fields and
commits at pass 2 with Josephine as reactor and the attacking unit as source. Its empty
target list is native behavior for this self-directed reaction, not a missing commit.
Together with Counter, this establishes pass 2 at `0x206421` as the accepted native
Reaction commit boundary across both an offensive response and an item-based response.

Overall: **PASS**
