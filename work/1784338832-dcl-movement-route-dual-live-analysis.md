# DCL movement-route live analysis

- Log: `work\1784338832-dcl-movement-convergence-dual-live.log`
- Parsed hook events: 139
- Real tile arrivals: 23
- Filtered terminal same-tile echoes: 116
- Actors: 5
- Complete routes: 5

| Event | Actor | Implementation | Old tile | Target tile | Cursor/length | Route byte | Linked record |
| ---: | ---: | --- | --- | --- | ---: | ---: | ---: |
| 1 | `0x140D30580` | `dcl_movement_convergence_native` | (1,10,0) | (2,10,0) | 1/6 | `0x00` | `0x141853EE0` |
| 2 | `0x140D30580` | `dcl_movement_convergence_native` | (2,10,0) | (3,10,0) | 2/6 | `0x00` | `0x141853EE0` |
| 3 | `0x140D30580` | `dcl_movement_convergence_native` | (3,10,0) | (3,9,0) | 3/6 | `0x80` | `0x141853EE0` |
| 4 | `0x140D30580` | `dcl_movement_convergence_native` | (3,9,0) | (3,8,0) | 4/6 | `0x80` | `0x141853EE0` |
| 5 | `0x140D30580` | `dcl_movement_convergence_native` | (3,8,0) | (3,7,0) | 5/6 | `0x80` | `0x141853EE0` |
| 6 | `0x140D30580` | `dcl_movement_convergence_native` | (3,7,0) | (4,7,0) | 6/6 | `0x00` | `0x141853EE0` |
| 8 | `0x140D30AC8` | `dcl_movement_convergence_native` | (1,1,0) | (2,1,0) | 1/3 | `0x00` | `0x1418540E0` |
| 9 | `0x140D30AC8` | `dcl_movement_convergence_native` | (2,1,0) | (3,1,0) | 2/3 | `0x00` | `0x1418540E0` |
| 10 | `0x140D30AC8` | `dcl_movement_convergence_native` | (3,1,0) | (4,1,0) | 3/3 | `0x00` | `0x1418540E0` |
| 123 | `0x140D31010` | `dcl_movement_convergence_native` | (9,12,0) | (8,12,0) | 1/4 | `0x40` | `0x1418544E0` |
| 124 | `0x140D31010` | `dcl_movement_convergence_native` | (8,12,0) | (8,11,0) | 2/4 | `0x80` | `0x1418544E0` |
| 125 | `0x140D31010` | `dcl_movement_convergence_native` | (8,11,0) | (8,10,0) | 3/4 | `0x80` | `0x1418544E0` |
| 126 | `0x140D31010` | `dcl_movement_convergence_native` | (8,10,0) | (8,9,0) | 4/4 | `0x80` | `0x1418544E0` |
| 128 | `0x140D31558` | `dcl_movement_convergence_native` | (2,11,0) | (2,10,0) | 1/4 | `0x80` | `0x1418548E0` |
| 129 | `0x140D31558` | `dcl_movement_convergence_native` | (2,10,0) | (3,10,0) | 2/4 | `0x00` | `0x1418548E0` |
| 130 | `0x140D31558` | `dcl_movement_convergence_native` | (3,10,0) | (3,9,0) | 3/4 | `0x80` | `0x1418548E0` |
| 131 | `0x140D31558` | `dcl_movement_convergence_native` | (3,9,0) | (3,8,0) | 4/4 | `0x80` | `0x1418548E0` |
| 133 | `0x140D31AA0` | `dcl_movement_convergence_native` | (5,1,0) | (6,1,0) | 1/6 | `0x00` | `0x141855CE0` |
| 134 | `0x140D31AA0` | `dcl_movement_convergence_native` | (6,1,0) | (6,0,0) | 2/6 | `0x80` | `0x141855CE0` |
| 135 | `0x140D31AA0` | `dcl_movement_convergence_native` | (6,0,0) | (7,0,0) | 3/6 | `0x00` | `0x141855CE0` |
| 136 | `0x140D31AA0` | `dcl_movement_convergence_native` | (7,0,0) | (8,0,0) | 4/6 | `0x00` | `0x141855CE0` |
| 137 | `0x140D31AA0` | `dcl_movement_convergence_native` | (8,0,0) | (9,0,0) | 5/6 | `0x00` | `0x141855CE0` |
| 138 | `0x140D31AA0` | `dcl_movement_convergence_native` | (9,0,0) | (10,0,0) | 6/6 | `0x00` | `0x141855CE0` |

## Gate

- No structural errors.
- Result: **PASS**.
