# DCL Reaction commit/effect live correlation

Source: `work\1784107234-lt31-counter-effect-multiplicity-live.log`
Reaction: `442`

## Checks

| Check | Result |
| --- | --- |
| all three guarded commit hooks installed | PASS |
| state-0x2C effect hook installed | PASS |
| capture contains exactly one selected Reaction 442 commit | PASS |
| selected commit is pass 2 with agreeing ids | PASS |
| later effect rows preserve actor, reactor, source, and Reaction presentation id | PASS |
| effect-row count matches the expected native transaction count | PASS |
| effect executable action id matches the expected native payload | PASS |
| effect target is the incoming source | PASS |

## Selected rows

| Kind | Event | Actor | Reactor | Source | Reaction/action | Targets |
| --- | ---: | --- | ---: | ---: | --- | --- |
| commit | 8 | `0x140D31558` | 4 | 3 | `442/442` | `(3,)` |
| effect | 11 | `0x140D31558` | 4 | 3 | `442/0` | `(3,)` |
| effect | 12 | `0x140D31558` | 4 | 3 | `442/0` | `(3,)` |

## Interpretation

A passing capture proves how one accepted pass-2 Reaction expands into native execution
transactions at state `0x2C`. Presentation Reaction id, actor, reactor, and source remain
available there; executable action id, final target, and row multiplicity are properties
of the delivered native action and need not equal the earlier commit snapshot.

Overall: **PASS**
