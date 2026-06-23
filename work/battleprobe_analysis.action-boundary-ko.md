# Immediate Action / KO Boundary Analysis

- Source log: `work\live-captures\battleprobe_log.action-boundary-after-ramza-wait-ninja-reraise.snapshot.txt`
- Tick frequency assumption: `10000000` ticks/sec

## Key Findings
- Rush KO HP event: line 393, event `5`.
- Execution raw damage cache: `33`.
- Applied HP loss: `15`.
- HP clamp: `1`, raw overkill: `18`.
- Reraise/revive event: line 419, HP `0 -> 28`.
- The target cache changed from preview `50` to execution `33` before the HP-zero frame.
- Ramza/Rush is selected by action-age scoring while stale Cloud/Cross Slash is demoted.

## Boundary Intervals

| Marker | Line | Delta from previous | Delta from preview |
| --- | ---: | ---: | ---: |
| preview cache `50` | 356 | 0 ms | 0 ms |
| Ramza `act=147` appears | 362 | 33996 ms | 33996 ms |
| execution cache `33` | 380 | 1114 ms | 35110 ms |
| HP zero / KO flags | 385 | 65 ms | 35175 ms |
| Ramza post-hit `bb=1` | 402 | 1766 ms | 36941 ms |
| Reraise HP restore | 412 | 166388 ms | 203329 ms |

## Rush Boundary Timeline

| Line | t+ms | Kind | Unit | Summary |
| ---: | ---: | --- | --- | --- |
| 356 | 0 | `PENDING-TARGET enter` | `0x141855EE0/0x80` | now=15517179266940 touch=0 dmg1C4=50/chg1D8=130/f1E5=128/bb=2 |
| 357 | 0 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=15 ct=101 act=0 dmg1C4=50 chg1D8=130 f1E5=128 s61=0 f1EF=0 b8=0 ba=0 bb=2 |
| 358 | 0 | `ACTION-BOUNDARY` | `0x141855EE0/0x80` | reason=forecast-damage-change,forecast-charge-change,forecast-flag-change hookAgeMs=365 prev=hp=15/ct=101/s61=0/t18D=255/act=0/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=0/ba=0/bb=2 curr=hp=15/ct=101/s61=0/t18D=255/act=0/dmg1C4=50/chg1D8=130/f1E5=128/f1EF=0/b8=0/ba=0/bb=2 diff=+0x1C4:00->32 +0x1D8:00->82 +0x1E5:00->80 |
| 362 | 33996 | `ACTION-STATE` | `0x141855CE0/0x03` | hp=446 ct=1 act=147 dmg1C4=0 chg1D8=0 f1E5=0 s61=0 f1EF=0 b8=0 ba=1 bb=0 |
| 363 | 33996 | `ACTION-BOUNDARY` | `0x141855CE0/0x03` | reason=action-id-change hookAgeMs=33959 prev=hp=446/ct=1/s61=0/t18D=255/act=0/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=1/ba=0/bb=0 curr=hp=446/ct=1/s61=0/t18D=255/act=147/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=0/ba=1/bb=0 diff=+0x1A0:00->10 +0x1A1:00->1B +0x1A2:00->93 +0x1B8:01->00 +0x1B9:01->03 +0x1BA:00->01 |
| 366 |  | `PENDING-TARGET update` | `0x141855EE0/0x80` | age=33996ms prev=dmg1C4=50/chg1D8=130/f1E5=128/bb=2 next=dmg1C4=50/chg1D8=130/f1E5=128/bb=0 touch=0 |
| 367 | 33996 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=15 ct=101 act=0 dmg1C4=50 chg1D8=130 f1E5=128 s61=0 f1EF=0 b8=0 ba=0 bb=0 |
| 368 | 33996 | `ACTION-BOUNDARY` | `0x141855EE0/0x80` | reason=phase-change,death-state-change hookAgeMs=33959 prev=hp=15/ct=101/s61=0/t18D=255/act=0/dmg1C4=50/chg1D8=130/f1E5=128/f1EF=0/b8=0/ba=0/bb=2 curr=hp=15/ct=101/s61=0/t18D=255/act=0/dmg1C4=50/chg1D8=130/f1E5=128/f1EF=0/b8=0/ba=0/bb=0 diff=+0x1BB:02->00 |
| 371 | 33996 | `ACTION-STATE` | `0x1418560E0/0x1E` | hp=283 ct=93 act=0 dmg1C4=0 chg1D8=0 f1E5=0 s61=0 f1EF=0 b8=0 ba=0 bb=0 |
| 372 | 33996 | `ACTION-BOUNDARY` | `0x1418560E0/0x1E` | reason=phase-change,death-state-change hookAgeMs=33959 prev=hp=283/ct=93/s61=0/t18D=255/act=0/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=0/ba=0/bb=2 curr=hp=283/ct=93/s61=0/t18D=255/act=0/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=0/ba=0/bb=0 diff=+0x1BB:02->00 |
| 375 | 33996 | `ACTION-STATE` | `0x1418562E0/0x32` | hp=378 ct=73 act=258 dmg1C4=0 chg1D8=0 f1E5=0 s61=0 f1EF=0 b8=0 ba=1 bb=0 |
| 376 | 33996 | `ACTION-BOUNDARY` | `0x1418562E0/0x32` | reason=phase-change,death-state-change hookAgeMs=33959 prev=hp=378/ct=73/s61=0/t18D=255/act=258/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=0/ba=1/bb=1 curr=hp=378/ct=73/s61=0/t18D=255/act=258/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=0/ba=1/bb=0 diff=+0x1BB:01->00 |
| 379 |  | `PENDING-TARGET update` | `0x141855EE0/0x80` | age=35110ms prev=dmg1C4=50/chg1D8=130/f1E5=128/bb=0 next=dmg1C4=33/chg1D8=130/f1E5=128/bb=0 touch=0 |
| 380 | 35110 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=15 ct=101 act=0 dmg1C4=33 chg1D8=130 f1E5=128 s61=0 f1EF=0 b8=0 ba=0 bb=0 |
| 381 | 35110 | `ACTION-BOUNDARY` | `0x141855EE0/0x80` | reason=forecast-damage-change hookAgeMs=35073 prev=hp=15/ct=101/s61=0/t18D=255/act=0/dmg1C4=50/chg1D8=130/f1E5=128/f1EF=0/b8=0/ba=0/bb=0 curr=hp=15/ct=101/s61=0/t18D=255/act=0/dmg1C4=33/chg1D8=130/f1E5=128/f1EF=0/b8=0/ba=0/bb=0 diff=+0x1C4:32->21 |
| 384 |  | `PENDING-TARGET update` | `0x141855EE0/0x80` | age=35175ms prev=dmg1C4=33/chg1D8=130/f1E5=128/bb=0 next=dmg1C4=33/chg1D8=130/f1E5=128/bb=1 touch=0 |
| 385 | 35175 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=0 ct=101 act=0 dmg1C4=33 chg1D8=130 f1E5=128 s61=32 f1EF=32 b8=0 ba=0 bb=1 |
| 386 | 35175 | `ACTION-BOUNDARY` | `0x141855EE0/0x80` | reason=hp-zero,phase-change,status-pending-change,death-state-change hookAgeMs=35138 prev=hp=15/ct=101/s61=0/t18D=255/act=0/dmg1C4=33/chg1D8=130/f1E5=128/f1EF=0/b8=0/ba=0/bb=0 curr=hp=0/ct=101/s61=32/t18D=255/act=0/dmg1C4=33/chg1D8=130/f1E5=128/f1EF=32/b8=0/ba=0/bb=1 diff=+0x30:0F->00 +0x61:00->20 +0x63:21->20 +0x18C:00->01 +0x1BB:00->01 +0x1DB:00->20 +0x1EF:00->20 +0x1F1:01->00 +0x1F5:FF->10 |
| 390 |  | `DEATH-DIFF` | `0x141855EE0/0x80` | alive->dead +0x30:0F->00 +0x61:00->20 +0x63:21->20 +0x18C:00->01 +0x1BB:00->01 +0x1DB:00->20 +0x1EF:00->20 +0x1F1:01->00 +0x1F5:FF->10 |
| 391 |  | `DAMAGE` | `0x141855EE0/0x80` | 15 -> 0 = 15 |
| 393 |  | `HP-EVENT-PROBE` | `0x141855EE0/0x80` | event=5 damage prev=15 current=0 applied=15 raw=33 lethal=1 hpClamp=1 rawOverkill=18 |
| 399 | 35175 | `IMMEDIATE-CANDIDATES` | `0x141855EE0/0x80` | event=5 candidates=10 |
| 402 | 36941 | `ACTION-STATE` | `0x141855CE0/0x03` | hp=446 ct=1 act=147 dmg1C4=0 chg1D8=0 f1E5=0 s61=0 f1EF=0 b8=0 ba=1 bb=1 |
| 403 | 36941 | `ACTION-BOUNDARY` | `0x141855CE0/0x03` | reason=phase-change,death-state-change hookAgeMs=36904 prev=hp=446/ct=1/s61=0/t18D=255/act=147/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=0/ba=1/bb=0 curr=hp=446/ct=1/s61=0/t18D=255/act=147/dmg1C4=0/chg1D8=0/f1E5=0/f1EF=0/b8=0/ba=1/bb=1 diff=+0x1BB:00->01 |
| 407 |  | `DIFF` | `0x141855CE0/0x03` | +0x28:46->59 +0x51:00->03 +0xF0:2B->59 +0x11E:35->63 +0x1A0:00->10 +0x1A1:00->1B +0x1A2:00->93 +0x1AA:00->05 +0x1AC:00->09 +0x1B0:00->09 +0x1B8:01->00 +0x1B9:01->03 |
| 411 |  | `PENDING-TARGET clear` | `0x141855EE0/0x80` | age=203329ms lastSeen=14ms prev=dmg1C4=33/chg1D8=130/f1E5=128/bb=1 touch=0 |
| 412 | 203329 | `ACTION-STATE` | `0x141855EE0/0x80` | hp=28 ct=1 act=0 dmg1C4=0 chg1D8=0 f1E5=72 s61=0 f1EF=0 b8=1 ba=0 bb=2 |
| 413 | 203329 | `ACTION-BOUNDARY` | `0x141855EE0/0x80` | reason=hp-change,forecast-damage-change,forecast-charge-change,forecast-flag-change,phase-change,status-pending-change,death-state-change hookAgeMs=166268 prev=hp=0/ct=101/s61=32/t18D=255/act=0/dmg1C4=33/chg1D8=130/f1E5=128/f1EF=32/b8=0/ba=0/bb=1 curr=hp=28/ct=1/s61=0/t18D=255/act=0/dmg1C4=0/chg1D8=0/f1E5=72/f1EF=0/b8=1/ba=0/bb=2 diff=+0x30:00->1C +0x61:20->00 +0x63:20->21 +0x1B8:00->01 +0x1BB:01->02 +0x1C4:21->00 +0x1C6:00->1C +0x1D8:82->00 +0x1DB:20->00 +0x1DD:00->01 +0x1E0:00->20 +0x1E5:80->48 +0x1EF:20->00 +0x1F1:00->01 +0x1F5:10->FF |
| 417 |  | `HEALING` | `0x141855EE0/0x80` | 0 -> 28 = 28 |
| 419 |  | `HP-EVENT-PROBE` | `0x141855EE0/0x80` | event=6 healing prev=0 current=28 applied=28 raw=0 lethal=0 hpClamp=0 rawOverkill=0 |

## Immediate Candidate Re-Rank

### Event 5 `damage` line 399

| Rank(old) | Rank(new) | Unit | Role | Old score | New score | act | action age | active age | Flags |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 1 | `0x141855CE0/0x03` | `source-like` | 2150 | 2150 | 147 | 1179 | 1179 | `freshAct,freshActive` |
| 2 | 2 | `0x141855EE0/0x80` | `target` | 300 | 300 | 0 | - | - | `lethalClamp` |
| 3 | 3 | `0x141853CE0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 4 | 4 | `0x141853EE0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 5 | 5 | `0x1418544E0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 6 | 6 | `0x1418548E0/0x80` | `context` | 0 | 0 | 0 | - | - | `` |
| 7 | 7 | `0x141854EE0/0x81` | `context` | 0 | 0 | 0 | - | - | `` |
| 8 | 8 | `0x1418560E0/0x1E` | `context` | 0 | 0 | 0 | - | - | `` |
| 10 | 10 | `0x1418562E0/0x32` | `source-like` | -250 | -250 | 258 | 434376 | 434376 | `staleAct,staleActive` |

### Event 6 `healing` line 425

| Rank(old) | Rank(new) | Unit | Role | Old score | New score | act | action age | active age | Flags |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 1 | `0x141855EE0/0x80` | `target` | 450 | 450 | 0 | - | - | `` |
| 2 | 2 | `0x141853CE0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 3 | 3 | `0x141853EE0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 4 | 4 | `0x1418544E0/0x82` | `context` | 0 | 0 | 0 | - | - | `` |
| 5 | 5 | `0x1418548E0/0x80` | `context` | 0 | 0 | 0 | - | - | `` |
| 6 | 6 | `0x141854EE0/0x81` | `context` | 0 | 0 | 0 | - | - | `` |
| 7 | 7 | `0x1418560E0/0x1E` | `context` | 0 | 0 | 0 | - | - | `` |
| 8 | 8 | `0x1418564E0/0x1F` | `context` | 0 | 0 | 0 | - | - | `` |
| 9 | 9 | `0x141855CE0/0x03` | `source-like` | -250 | -250 | 147 | 169333 | 169333 | `staleAct,staleActive` |
| 10 | 10 | `0x1418562E0/0x32` | `source-like` | -250 | -250 | 258 | 602530 | 602530 | `staleAct,staleActive` |

## Probe Implications

- `actionIdAgeMs` and `activeActionAgeMs` are now directly logged and validate the immediate-action scorer.
- The next KO-boundary search should focus on the short interval between target cache `33` and the HP-zero/death diff frame.
- Reraise should stay classified as revive-state evidence rather than ordinary healing attribution.
