# Immediate Action / KO Boundary Analysis

- Source log: `C:\Users\Dante\Documents\Projects\FFTGenericChronicle\work\live-captures\battleprobe_log.immediate-ko-boundary-after-ramza-wait-ninja-reraise.snapshot.txt`
- Tick frequency assumption: `10000000` ticks/sec

## Key Findings
- Rush KO HP event: line 225, event `3`.
- Execution raw damage cache: `33`.
- Applied HP loss: `15`.
- HP clamp: `1`, raw overkill: `18`.
- Reraise/revive event: line 245, HP `0 -> 28`.
- The target cache changed from preview `50` to execution `33` before the HP-zero frame.
- Ramza/Rush and stale Cloud/Cross Slash tie under the old score, but action-age scoring separates them.

## Boundary Intervals

| Marker | Line | Delta from previous | Delta from preview |
| --- | ---: | ---: | ---: |
| preview cache `50` | 208 | 0 ms | 0 ms |
| Ramza `act=147` appears | 212 | 108798 ms | 108798 ms |
| execution cache `33` | 218 | 1108 ms | 109906 ms |
| HP zero / KO flags | 220 | 62 ms | 109968 ms |
| Ramza post-hit `bb=1` | 234 | 1671 ms | 111639 ms |
| Reraise HP restore | 241 | 142114 ms | 253752 ms |

## Rush Boundary Timeline

| Line | t+ms | Kind | Unit | Summary |
| ---: | ---: | --- | --- | --- |
| 208 | 0 | `PENDING-TARGET enter` | `0x141855EE0/0x80` | now=15485029991186 touch=0 dmg1C4=50/chg1D8=130/f1E5=128/bb=2 |
| 209 | 0 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=15 ct=101 act=0 dmg1C4=50 chg1D8=130 f1E5=128 s61=0 f1EF=0 b8=0 ba=0 bb=2 |
| 212 | 108798 | `ACTION-STATE` | `0x141855CE0/0x03` | hp=446 ct=1 act=147 dmg1C4=0 chg1D8=0 f1E5=0 s61=0 f1EF=0 b8=0 ba=1 bb=0 |
| 213 |  | `PENDING-TARGET update` | `0x141855EE0/0x80` | age=108798ms prev=dmg1C4=50/chg1D8=130/f1E5=128/bb=2 next=dmg1C4=50/chg1D8=130/f1E5=128/bb=0 touch=0 |
| 214 | 108798 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=15 ct=101 act=0 dmg1C4=50 chg1D8=130 f1E5=128 s61=0 f1EF=0 b8=0 ba=0 bb=0 |
| 215 | 108798 | `ACTION-STATE` | `0x1418560E0/0x1E` | hp=283 ct=93 act=0 dmg1C4=0 chg1D8=0 f1E5=0 s61=0 f1EF=0 b8=0 ba=0 bb=0 |
| 216 | 108798 | `ACTION-STATE` | `0x1418562E0/0x32` | hp=378 ct=73 act=258 dmg1C4=0 chg1D8=0 f1E5=0 s61=0 f1EF=0 b8=0 ba=1 bb=0 |
| 217 |  | `PENDING-TARGET update` | `0x141855EE0/0x80` | age=109906ms prev=dmg1C4=50/chg1D8=130/f1E5=128/bb=0 next=dmg1C4=33/chg1D8=130/f1E5=128/bb=0 touch=0 |
| 218 | 109906 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=15 ct=101 act=0 dmg1C4=33 chg1D8=130 f1E5=128 s61=0 f1EF=0 b8=0 ba=0 bb=0 |
| 219 |  | `PENDING-TARGET update` | `0x141855EE0/0x80` | age=109968ms prev=dmg1C4=33/chg1D8=130/f1E5=128/bb=0 next=dmg1C4=33/chg1D8=130/f1E5=128/bb=1 touch=0 |
| 220 | 109968 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=0 ct=101 act=0 dmg1C4=33 chg1D8=130 f1E5=128 s61=32 f1EF=32 b8=0 ba=0 bb=1 |
| 222 |  | `DEATH-DIFF` | `0x141855EE0/0x80` | alive->dead +0x30:0F->00 +0x61:00->20 +0x63:21->20 +0x18C:00->01 +0x1BB:00->01 +0x1DB:00->20 +0x1EF:00->20 +0x1F1:01->00 +0x1F5:FF->10 |
| 223 |  | `DAMAGE` | `0x141855EE0/0x80` | 15 -> 0 = 15 |
| 225 |  | `HP-EVENT-PROBE` | `0x141855EE0/0x80` | event=3 damage prev=15 current=0 applied=15 raw=33 lethal=1 hpClamp=1 rawOverkill=18 |
| 231 | 109968 | `IMMEDIATE-CANDIDATES` | `0x141855EE0/0x80` | event=3 candidates=10 |
| 234 | 111639 | `ACTION-STATE` | `0x141855CE0/0x03` | hp=446 ct=1 act=147 dmg1C4=0 chg1D8=0 f1E5=0 s61=0 f1EF=0 b8=0 ba=1 bb=1 |
| 236 |  | `DIFF` | `0x141855CE0/0x03` | +0x28:46->59 +0x51:00->02 +0xF0:2B->59 +0x11E:35->63 +0x1A0:00->10 +0x1A1:00->1B +0x1A2:00->93 +0x1AA:00->05 +0x1AC:00->09 +0x1B0:00->09 +0x1B8:01->00 +0x1B9:01->03 |
| 240 |  | `PENDING-TARGET clear` | `0x141855EE0/0x80` | age=253752ms lastSeen=11ms prev=dmg1C4=33/chg1D8=130/f1E5=128/bb=1 touch=0 |
| 241 | 253752 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=28 ct=1 act=0 dmg1C4=0 chg1D8=0 f1E5=72 s61=0 f1EF=0 b8=1 ba=0 bb=2 |
| 243 |  | `HEALING` | `0x141855EE0/0x80` | 0 -> 28 = 28 |
| 245 |  | `HP-EVENT-PROBE` | `0x141855EE0/0x80` | event=4 healing prev=0 current=28 applied=28 raw=0 lethal=0 hpClamp=0 rawOverkill=0 |
| 251 | 253752 | `IMMEDIATE-CANDIDATES` | `0x141855EE0/0x80` | event=4 candidates=10 |
| 254 |  | `DIFF` | `0x141855EE0/0x80` | +0x30:20->1C +0x31:01->00 +0x41:09->01 +0x63:20->21 +0xF0:5C->64 +0x11E:7C->84 +0x18C:00->01 +0x1BB:01->02 +0x1C6:00->1C +0x1DD:00->01 +0x1E0:00->20 +0x1E5:00->48 |
| 255 |  | `PENDING-TARGET drop` | `0x141855EE0/0x80` | reason=stale age=30004ms last=dmg1C4=33/chg1D8=130/f1E5=128/bb=1 |

## Immediate Candidate Re-Rank

### Event 3 `damage` line 231

| Rank(old) | Rank(new) | Unit | Role | Old score | New score | act | action age | active age | Flags |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 1 | `0x141855CE0/0x03` | `source-like` | 1300 | 2150 | 147 | 1170 | 1170 | `freshAct,freshActive` |
| 3 | 2 | `0x141855EE0/0x80` | `target` | 550 | 300 | 0 | - | - | `lethalClamp` |
| 4 | 3 | `0x141853CE0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 5 | 4 | `0x141853EE0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 6 | 5 | `0x1418544E0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 7 | 6 | `0x1418548E0/0x80` | `context` | 0 | 0 | 0 | - | - | `` |
| 8 | 7 | `0x141854EE0/0x81` | `context` | 0 | 0 | 0 | - | - | `` |
| 9 | 8 | `0x1418560E0/0x1E` | `context` | 0 | 0 | 0 | - | - | `` |
| 2 | 10 | `0x1418562E0/0x32` | `source-like` | 1300 | -250 | 258 | 452841 | 452841 | `staleAct,staleActive` |

### Event 4 `healing` line 251

| Rank(old) | Rank(new) | Unit | Role | Old score | New score | act | action age | active age | Flags |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 3 | 1 | `0x141855EE0/0x80` | `target` | 700 | 450 | 0 | - | - | `` |
| 4 | 2 | `0x141853CE0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 5 | 3 | `0x141853EE0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 6 | 4 | `0x1418544E0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 7 | 5 | `0x1418548E0/0x80` | `context` | 0 | 0 | 0 | - | - | `` |
| 8 | 6 | `0x141854EE0/0x81` | `context` | 0 | 0 | 0 | - | - | `` |
| 9 | 7 | `0x1418560E0/0x1E` | `context` | 0 | 0 | 0 | - | - | `` |
| 10 | 8 | `0x1418564E0/0x1F` | `context` | 0 | 0 | 0 | - | - | `` |
| 1 | 9 | `0x141855CE0/0x03` | `source-like` | 1300 | -250 | 147 | 144955 | 144955 | `staleAct,staleActive` |
| 2 | 10 | `0x1418562E0/0x32` | `source-like` | 1300 | -250 | 258 | 596626 | 596626 | `staleAct,staleActive` |

## Probe Implications

- A next live probe should log `actionIdAgeMs` and `activeActionAgeMs` directly so the ranking can be validated without recomputing from prior lines.
- The next KO-boundary search should focus on the short interval between target cache `33` and the HP-zero/death diff frame.
- Reraise should stay classified as revive-state evidence rather than ordinary healing attribution.
