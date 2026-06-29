# Action Identity Log Analysis

Log: `work\1782697538-hallowed-bolt-shirahadori-not-applicable-log.txt`

## Summary

- Pre-clamp actor contexts: 4 (`resolved`=3, `ambiguous`=0, `none`=1).
- Actor contexts with known ability/basic ids: 3.
- Pending matches: 1 (`resolved`=0).
- Pending target caches: 2 (`pre-apply damage candidates`=0).
- Immediate candidate snapshots: 1 (`selected`=0).
- Formula candidates: 1.
- Selector probes: 3.

## Readiness Signals

- Immediate candidate snapshots exist but none selected a source.

## Actor Context Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 158 | Hallowed Bolt | 3 | matches baseline ability table |

## Pending Action IDs

- No action ids observed.

## Immediate Candidate Action IDs

- No action ids observed.

## Action-State IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 14 | basic attack / implicit weapon action |
| 158 | Hallowed Bolt | 9 | matches baseline ability table |
| 159 | Divine Ruination | 1 | matches baseline ability table |

## Pending Target Caches

| Line | Kind | Target | Damage | Credit | Charge | f1E5 | bb | Candidate? |
| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 19 | `enter` | `0x141855EE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 27 | `clear` | `0x141855EE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |

## Selector Outcomes

| Evade type | Meaning | Count |
| ---: | --- | ---: |
| `0x00` | hit | 3 |

Selector-frame actor/action ids:

| Action id | Count |
| ---: | ---: |
| `158` | 9 |

| Line | Event | Unit | Evade | rec+1BE | rec+1C0 | rec+1C4 dmg | rec+1E5 | Actor refs | Control? |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| 61 | 1 | `0x02` | `0x00` hit | `0x01` | `0x00` | 259 | `0x80` | `rdx->0x1E/act=158`<br>`r15->0x1E/act=158`<br>`+0x90->0x1E/act=158` |  |
| 86 | 2 | `0x02` | `0x00` hit | `0x01` | `0x00` | 259 | `0x88` | `rdx->0x1E/act=158`<br>`r15->0x1E/act=158`<br>`+0x90->0x1E/act=158` |  |
| 110 | 3 | `0x02` | `0x00` hit | `0x01` | `0x00` | 259 | `0x88` | `rdx->0x1E/act=158`<br>`r15->0x1E/act=158`<br>`+0x90->0x1E/act=158` |  |

## Actor Context Events

| Line | Event | Target | Caster | Action | Debit | Verdict | Equip? |
| ---: | ---: | --- | --- | --- | ---: | --- | --- |
| 18 | 1 | `0x141855EE0/0x80` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 58 | 2 | `0x141855CE0/0x02` | `0x1418560E0/0x1E` | `158` Hallowed Bolt | 259 | `resolved` | `target+caster` |
| 83 | 3 | `0x141855CE0/0x02` | `0x1418560E0/0x1E` | `158` Hallowed Bolt | 259 | `resolved` | `target+caster` |
| 107 | 4 | `0x141855CE0/0x02` | `0x1418560E0/0x1E` | `158` Hallowed Bolt | 259 | `resolved` | `target+caster` |

## Pending Matches

| Line | Event | Kind | Target | Caster | Action | Confidence | Score |
| ---: | ---: | --- | --- | --- | --- | --- | ---: |
| 21 | 1 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  |

## Immediate Candidates

| Line | Target | Selected caster | Action | Debit | Credit | Score | Margin |
| ---: | --- | --- | --- | ---: | ---: | ---: | ---: |
| 22 | `0x141855EE0/0x80` | `none` | `none` | 0 | 34 |  |  |

## Formula Candidate Sources

- `none`: 1
