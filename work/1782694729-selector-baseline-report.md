# Action Identity Log Analysis

Log: `work\1782694729-selector-baseline-log.txt`

## Summary

- Pre-clamp actor contexts: 2 (`resolved`=2, `ambiguous`=0, `none`=0).
- Actor contexts with known ability/basic ids: 2.
- Pending matches: 7 (`resolved`=0).
- Immediate candidate snapshots: 4 (`selected`=2).
- Formula candidates: 4.
- Selector probes: 2.

## Readiness Signals

- No hard action-identity gaps detected in this log.

## Actor Context Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 1 | basic attack / implicit weapon action |
| 159 | Divine Ruination | 1 | matches baseline ability table |

## Pending Action IDs

- No action ids observed.

## Immediate Candidate Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 1 | basic attack / implicit weapon action |
| 159 | Divine Ruination | 1 | matches baseline ability table |

## Action-State IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 21 | basic attack / implicit weapon action |
| 159 | Divine Ruination | 3 | matches baseline ability table |

## Selector Outcomes

| Evade type | Meaning | Count |
| ---: | --- | ---: |
| `0x00` | hit | 2 |

Selector-frame actor/action ids:

| Action id | Count |
| ---: | ---: |
| `0` | 8 |
| `159` | 3 |

| Line | Event | Unit | Evade | rec+1BE | rec+1C0 | rec+1C4 dmg | rec+1E5 | Actor refs | Control? |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| 104 | 1 | `0x1F` | `0x00` hit | `0x01` | `0x00` | 151 | `0x81` | `actor->0x1F/act=0/self`<br>`rbx->0x1F/act=0/self`<br>`rdx->0x1E/act=0`<br>`r8->0x1F/act=0/self`<br>`r15->0x1E/act=0`<br>`+0x8->0x1F/act=0/self`<br>+2 more |  |
| 135 | 2 | `0x1F` | `0x00` hit | `0x01` | `0x00` | 205 | `0x80` | `rdx->0x1E/act=159`<br>`r15->0x1E/act=159`<br>`+0x90->0x1E/act=159` |  |

## Actor Context Events

| Line | Event | Target | Caster | Action | Debit | Verdict | Equip? |
| ---: | ---: | --- | --- | --- | ---: | --- | --- |
| 89 | 1 | `0x141855CE0/0x1F` | `0x141855EE0/0x1E` | `0` Basic Attack / implicit weapon | 151 | `resolved` | `target+caster` |
| 132 | 2 | `0x141855CE0/0x1F` | `0x141855EE0/0x1E` | `159` Divine Ruination | 205 | `resolved` | `target+caster` |

## Pending Matches

| Line | Event | Kind | Target | Caster | Action | Confidence | Score |
| ---: | ---: | --- | --- | --- | --- | --- | ---: |
| 39 | 1 | `damage` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 56 | 3 | `healing` | `0x141855EE0/0x1E` | `none` | `none` | `none` |  |
| 75 | 4 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 82 | 808 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 102 | 5 | `damage` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 147 | 11076 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |
| 154 | 14266 | `preclamp-cache` | `0x141855CE0/0x1F` | `none` | `none` | `none` |  |

## Immediate Candidates

| Line | Target | Selected caster | Action | Debit | Credit | Score | Margin |
| ---: | --- | --- | --- | ---: | ---: | ---: | ---: |
| 76 | `0x141855CE0/0x1F` | `none` | `none` | 151 | 0 |  |  |
| 83 | `0x141855CE0/0x1F` | `0x141855EE0/0x1E` | `0` Basic Attack / implicit weapon | 151 | 0 | 2050 | 2147483647 |
| 148 | `0x141855CE0/0x1F` | `0x141855EE0/0x1E` | `159` Divine Ruination | 205 | 0 | 2650 | 2147483647 |
| 155 | `0x141855CE0/0x1F` | `none` | `none` | 205 | 0 |  |  |

## Formula Candidate Sources

- `ct-low`: 1
- `immediate-action`: 2
- `none`: 1
