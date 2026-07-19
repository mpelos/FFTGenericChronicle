# DCL movement-convergence live analysis

- Log: `work\1784338832-dcl-movement-convergence-dual-live.log`
- Parsed convergence events: 139
- Route dispatch events: 23
- Unassociated zero-length idle observations: 111
- Actors: 5
- Complete routes with terminal confirmation: 5
- Snapshot semantics: registers are captured synchronously; actor memory is read on the next managed poll, after immediate step dispatch.

| Event | Actor | Route | Phase | Current tile | Target tile | Cursor/length | Staged byte |
| ---: | ---: | ---: | --- | --- | --- | ---: | ---: |
| 1 | `0x140D30580` | 1 | dispatch | (1,10,0) | (2,10,0) | 1/6 | `0x00` |
| 2 | `0x140D30580` | 1 | dispatch | (2,10,0) | (3,10,0) | 2/6 | `0x00` |
| 3 | `0x140D30580` | 1 | dispatch | (3,10,0) | (3,9,0) | 3/6 | `0x80` |
| 4 | `0x140D30580` | 1 | dispatch | (3,9,0) | (3,8,0) | 4/6 | `0x80` |
| 5 | `0x140D30580` | 1 | dispatch | (3,8,0) | (3,7,0) | 5/6 | `0x80` |
| 6 | `0x140D30580` | 1 | dispatch | (3,7,0) | (4,7,0) | 6/6 | `0x00` |
| 7 | `0x140D30580` | 1 | terminal | (4,7,0) | (4,7,0) | 6/0 | `0x00` |
| 8 | `0x140D30AC8` | 1 | dispatch | (1,1,0) | (2,1,0) | 1/3 | `0x00` |
| 9 | `0x140D30AC8` | 1 | dispatch | (2,1,0) | (3,1,0) | 2/3 | `0x00` |
| 10 | `0x140D30AC8` | 1 | dispatch | (3,1,0) | (4,1,0) | 3/3 | `0x00` |
| 11 | `0x140D30AC8` | 1 | terminal | (4,1,0) | (4,1,0) | 3/0 | `0x00` |
| 123 | `0x140D31010` | 1 | dispatch | (9,12,0) | (8,12,0) | 1/4 | `0x40` |
| 124 | `0x140D31010` | 1 | dispatch | (8,12,0) | (8,11,0) | 2/4 | `0x80` |
| 125 | `0x140D31010` | 1 | dispatch | (8,11,0) | (8,10,0) | 3/4 | `0x80` |
| 126 | `0x140D31010` | 1 | dispatch | (8,10,0) | (8,9,0) | 4/4 | `0x80` |
| 127 | `0x140D31010` | 1 | terminal | (8,9,0) | (8,9,0) | 4/0 | `0x80` |
| 128 | `0x140D31558` | 1 | dispatch | (2,11,0) | (2,10,0) | 1/4 | `0x80` |
| 129 | `0x140D31558` | 1 | dispatch | (2,10,0) | (3,10,0) | 2/4 | `0x00` |
| 130 | `0x140D31558` | 1 | dispatch | (3,10,0) | (3,9,0) | 3/4 | `0x80` |
| 131 | `0x140D31558` | 1 | dispatch | (3,9,0) | (3,8,0) | 4/4 | `0x80` |
| 132 | `0x140D31558` | 1 | terminal | (3,8,0) | (3,8,0) | 4/0 | `0x80` |
| 133 | `0x140D31AA0` | 1 | dispatch | (5,1,0) | (6,1,0) | 1/6 | `0x00` |
| 134 | `0x140D31AA0` | 1 | dispatch | (6,1,0) | (6,0,0) | 2/6 | `0x80` |
| 135 | `0x140D31AA0` | 1 | dispatch | (6,0,0) | (7,0,0) | 3/6 | `0x00` |
| 136 | `0x140D31AA0` | 1 | dispatch | (7,0,0) | (8,0,0) | 4/6 | `0x00` |
| 137 | `0x140D31AA0` | 1 | dispatch | (8,0,0) | (9,0,0) | 5/6 | `0x00` |
| 138 | `0x140D31AA0` | 1 | dispatch | (9,0,0) | (10,0,0) | 6/6 | `0x00` |
| 139 | `0x140D31AA0` | 1 | terminal | (10,0,0) | (10,0,0) | 6/0 | `0x00` |

## Gate

- No structural errors.
- Result: **PASS**.
