# DCL movement-route live analysis

- Log: `work\1784336577-dcl-movement-route-live.log`
- Parsed hook events: 16
- Real tile arrivals: 13
- Filtered terminal same-tile echoes: 3
- Actors: 3
- Complete routes: 0

| Event | Actor | Implementation | Old tile | Target tile | Cursor/length | Route byte | Linked record |
| ---: | ---: | --- | --- | --- | ---: | ---: | ---: |
| 7 | `0x140D30580` | `dcl_movement_arrival_trace_a` | (8,11,0) | (8,10,0) | 2/4 | `0x80` | `0x1418540E0` |
| 8 | `0x140D30580` | `dcl_movement_arrival_trace_a` | (8,10,0) | (8,9,0) | 3/4 | `0x80` | `0x1418540E0` |
| 9 | `0x140D30580` | `dcl_movement_arrival_trace_a` | (8,9,0) | (8,8,0) | 4/4 | `0x80` | `0x1418540E0` |
| 1 | `0x140D30AC8` | `dcl_movement_arrival_trace_a` | (5,11,0) | (5,10,0) | 2/6 | `0x80` | `0x1418544E0` |
| 2 | `0x140D30AC8` | `dcl_movement_arrival_trace_a` | (5,10,0) | (5,9,0) | 3/6 | `0x80` | `0x1418544E0` |
| 3 | `0x140D30AC8` | `dcl_movement_arrival_trace_a` | (5,9,0) | (5,8,0) | 4/6 | `0x80` | `0x1418544E0` |
| 4 | `0x140D30AC8` | `dcl_movement_arrival_trace_a` | (5,8,0) | (6,8,0) | 5/6 | `0x00` | `0x1418544E0` |
| 5 | `0x140D30AC8` | `dcl_movement_arrival_trace_a` | (6,8,0) | (6,7,0) | 6/6 | `0x80` | `0x1418544E0` |
| 11 | `0x140D31010` | `dcl_movement_arrival_trace_a` | (6,1,0) | (6,0,0) | 2/6 | `0x80` | `0x141855CE0` |
| 12 | `0x140D31010` | `dcl_movement_arrival_trace_a` | (6,0,0) | (7,0,0) | 3/6 | `0x00` | `0x141855CE0` |
| 13 | `0x140D31010` | `dcl_movement_arrival_trace_a` | (7,0,0) | (8,0,0) | 4/6 | `0x00` | `0x141855CE0` |
| 14 | `0x140D31010` | `dcl_movement_arrival_trace_a` | (8,0,0) | (9,0,0) | 5/6 | `0x00` | `0x141855CE0` |
| 15 | `0x140D31010` | `dcl_movement_arrival_trace_a` | (9,0,0) | (10,0,0) | 6/6 | `0x00` | `0x141855CE0` |

## Gate

- ERROR: actor 0x140D30AC8: first captured cursor is 2, expected 1
- ERROR: actor 0x140D30580: first captured cursor is 2, expected 1
- ERROR: actor 0x140D31010: first captured cursor is 2, expected 1
- REQUIREMENT: expected at least 1 complete routes, found 0
- Result: **FAIL**.
