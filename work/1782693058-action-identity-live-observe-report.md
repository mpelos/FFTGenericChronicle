# Action Identity Log Analysis

Log: `work\1782693058-action-identity-live-observe-log.txt`

## Summary

- Pre-clamp actor contexts: 10 (`resolved`=6, `ambiguous`=0, `none`=4).
- Actor contexts with known ability/basic ids: 6.
- Pending matches: 18 (`resolved`=2).
- Immediate candidate snapshots: 14 (`selected`=4).
- Formula candidates: 15.
- Selector probes: 0.

## Readiness Signals

- No hard action-identity gaps detected in this log.

## Actor Context Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 1 | basic attack / implicit weapon action |
| 159 | Divine Ruination | 1 | matches baseline ability table |
| 257 | Braver | 2 | matches baseline ability table |
| 258 | Cross Slash | 2 | matches baseline ability table |

## Pending Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 257 | Braver | 8 | matches baseline ability table |
| 258 | Cross Slash | 4 | matches baseline ability table |

## Immediate Candidate Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 1 | basic attack / implicit weapon action |
| 159 | Divine Ruination | 1 | matches baseline ability table |
| 257 | Braver | 2 | matches baseline ability table |

## Action-State IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 124 | basic attack / implicit weapon action |
| 159 | Divine Ruination | 3 | matches baseline ability table |
| 257 | Braver | 14 | matches baseline ability table |
| 258 | Cross Slash | 9 | matches baseline ability table |

## Selector Outcomes

- No `[SELECTOR-PROBE]` records found.

## Actor Context Events

| Line | Event | Target | Caster | Action | Debit | Verdict | Equip? |
| ---: | ---: | --- | --- | --- | ---: | --- | --- |
| 55 | 1 | `0x1418564E0/0x80` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 113 | 2 | `0x141855CE0/0x1F` | `0x141855EE0/0x1E` | `0` Basic Attack / implicit weapon | 151 | `resolved` | `target+caster` |
| 210 | 3 | `0x1418564E0/0x80` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 240 | 4 | `0x141855CE0/0x1F` | `0x141855EE0/0x1E` | `159` Divine Ruination | 205 | `resolved` | `target+caster` |
| 341 | 5 | `0x1418562E0/0x01` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 365 | 6 | `0x1418564E0/0x80` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 466 | 7 | `0x141855CE0/0x1F` | `0x1418560E0/0x32` | `257` Braver | 153 | `resolved` | `target+caster` |
| 557 | 8 | `0x141855CE0/0x1F` | `0x1418560E0/0x32` | `257` Braver | 153 | `resolved` | `target+caster` |
| 626 | 9 | `0x141855CE0/0x1F` | `0x1418560E0/0x32` | `258` Cross Slash | 230 | `resolved` | `target+caster` |
| 630 | 10 | `0x1418562E0/0x01` | `0x1418560E0/0x32` | `258` Cross Slash | 187 | `resolved` | `target+caster` |

## Pending Matches

| Line | Event | Kind | Target | Caster | Action | Confidence | Score |
| ---: | ---: | --- | --- | --- | --- | --- | ---: |
| 58 | 4 | `preclamp-cache` | `0x1418564E0/0x80` | `none` | `none` | `none` |  |
| 84 | 112 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 91 | 795 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 110 | 1 | `damage` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 213 | 24247 | `preclamp-cache` | `0x1418564E0/0x80` | `none` | `none` | `none` |  |
| 253 | 24384 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 260 | 24451 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 265 | 29526 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 269 | 31558 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 282 | 2 | `healing` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 344 | 31990 | `preclamp-cache` | `0x1418562E0/0x01` | `none` | `none` | `none` |  |
| 368 | 32151 | `preclamp-cache` | `0x1418564E0/0x80` | `none` | `none` | `none` |  |
| 410 | 32311 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 417 | 33215 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 420 | 34450 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 461 | 35364 | `preclamp-cache` | `0x141855CE0/0x1F` | `0x1418560E0/0x32` | `257` Braver | `damage-cache` | 1098898 |
| 479 | 3 | `damage` | `0x141855CE0/0x1F` | `0x1418560E0/0x32` | `257` Braver | `damage-cache` | 1098829 |
| 488 | 36870 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |

## Immediate Candidates

| Line | Target | Selected caster | Action | Debit | Credit | Score | Margin |
| ---: | --- | --- | --- | ---: | ---: | ---: | ---: |
| 59 | `0x1418564E0/0x80` | `none` | `none` | 0 | 34 |  |  |
| 85 | `0x141855CE0/0x1F` | `none` | `none` | 151 | 0 |  |  |
| 92 | `0x141855CE0/0x1F` | `0x141855EE0/0x1E` | `0` Basic Attack / implicit weapon | 151 | 0 | 2050 | 2147483647 |
| 214 | `0x1418564E0/0x80` | `none` | `none` | 0 | 34 |  |  |
| 254 | `0x141855CE0/0x1F` | `0x141855EE0/0x1E` | `159` Divine Ruination | 205 | 0 | 1750 | 2147483647 |
| 261 | `0x141855CE0/0x1F` | `none` | `none` | 205 | 0 |  |  |
| 266 | `0x141855CE0/0x1F` | `none` | `none` | 205 | 0 |  |  |
| 270 | `0x141855CE0/0x1F` | `none` | `none` | 205 | 0 |  |  |
| 345 | `0x1418562E0/0x01` | `none` | `none` | 0 | 70 |  |  |
| 369 | `0x1418564E0/0x80` | `none` | `none` | 0 | 34 |  |  |
| 411 | `0x141855CE0/0x1F` | `none` | `none` | 153 | 0 |  |  |
| 418 | `0x141855CE0/0x1F` | `none` | `none` | 153 | 0 |  |  |
| 421 | `0x141855CE0/0x1F` | `0x1418560E0/0x32` | `257` Braver | 153 | 0 | 3150 | 2147483647 |
| 489 | `0x141855CE0/0x1F` | `0x1418560E0/0x32` | `257` Braver | 153 | 0 | 1750 | 2147483647 |

## Formula Candidate Sources

- `ct-low`: 3
- `ct-reset`: 1
- `immediate-action`: 4
- `none`: 6
- `pending-clear`: 1
