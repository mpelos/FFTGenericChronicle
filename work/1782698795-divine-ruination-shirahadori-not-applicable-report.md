# Action Identity Log Analysis

Log: `work\1782698795-divine-ruination-shirahadori-not-applicable-log.txt`

## Summary

- Pre-clamp actor contexts: 1 (`resolved`=1, `ambiguous`=0, `none`=0).
- Actor contexts with known ability/basic ids: 1.
- Pending matches: 8 (`resolved`=0).
- Pending target caches: 2 (`pre-apply damage candidates`=2).
- Immediate candidate snapshots: 7 (`selected`=1).
- Formula candidates: 7.
- Selector probes: 1.
- Hook-reg events: 9 (`targetcache`=0).
- Landmark hits: 0.

## Readiness Signals

- No hard action-identity gaps detected in this log.
- Pre-apply damage target-cache candidate(s): 2. These may include interrupted/cancelled incoming actions and need register-backed source proof.
- Pre-apply target-cache source hint(s): 6 across 2 cache(s). These are line-near formula correlations, not primary proof.

## Actor Context Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 159 | Divine Ruination | 1 | matches baseline ability table |

## Pending Action IDs

- No action ids observed.

## Immediate Candidate Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 159 | Divine Ruination | 1 | matches baseline ability table |

## Action-State IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 18 | basic attack / implicit weapon action |
| 159 | Divine Ruination | 3 | matches baseline ability table |

## Pending Target Caches

- Pre-apply damage candidates (`dmg1C4 > 0`, damage result flag, `bb != 2`): 2.

| Line | Kind | Target | Damage | Credit | Charge | f1E5 | bb | Candidate? |
| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 60 | `enter` | `0x141855CE0/0x01` | 184 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 134 | `enter` | `0x141855CE0/0x01` | 250 | 0 | 2 | `0x88` | 0 | pre-apply damage |

## Target Cache Source Hints

These rows correlate a pre-apply target cache to nearby formula candidates with the same target and damage. They are useful for narrowing interrupted-action cases, but they are not register-backed proof.

| Cache line | Formula line | Distance | Target | Damage | Attacker | Source | Action hints |
| ---: | ---: | ---: | --- | ---: | --- | --- | --- |
| 60 | 64 | 4 | `0x141855CE0/0x01` | 184 | `0x141855EE0/id=0x1E` | `ct-low` | `0` Basic Attack / implicit weapon |
| 60 | 75 | 15 | `0x141855CE0/0x01` | 184 | `none` | `none` | `0` Basic Attack / implicit weapon |
| 60 | 79 | 19 | `0x141855CE0/0x01` | 184 | `0x141855EE0/id=0x1E` | `ct-low` | `0` Basic Attack / implicit weapon |
| 60 | 83 | 23 | `0x141855CE0/0x01` | 184 | `none` | `none` | `0` Basic Attack / implicit weapon |
| 134 | 138 | 4 | `0x141855CE0/0x01` | 250 | `0x141855EE0/id=0x1E` | `ct-low` | `0` Basic Attack / implicit weapon |
| 134 | 146 | 12 | `0x141855CE0/0x01` | 250 | `0x141855EE0/id=0x1E` | `immediate-action` | `0` Basic Attack / implicit weapon |

## Register Actor Refs

## Selector Outcomes

| Evade type | Meaning | Count |
| ---: | --- | ---: |
| `0x00` | hit | 1 |

Selector-frame actor/action ids:

| Action id | Count |
| ---: | ---: |
| `0` | 5 |
| `159` | 3 |

| Line | Event | Unit | Evade | rec+1BE | rec+1C0 | rec+1C4 dmg | rec+1E5 | Actor refs | Control? |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| 169 | 1 | `0x01` | `0x00` hit | `0x01` | `0x00` | 250 | `0x80` | `actor->0x01/act=0/self`<br>`rbx->0x01/act=0/self`<br>`rdx->0x1E/act=159`<br>`r8->0x01/act=0/self`<br>`r15->0x1E/act=159`<br>`+0x8->0x01/act=0/self`<br>+2 more |  |

## Actor Context Events

| Line | Event | Target | Caster | Action | Debit | Verdict | Equip? |
| ---: | ---: | --- | --- | --- | ---: | --- | --- |
| 166 | 1 | `0x141855CE0/0x01` | `0x141855EE0/0x1E` | `159` Divine Ruination | 250 | `resolved` | `target+caster` |

## Pending Matches

| Line | Event | Kind | Target | Caster | Action | Confidence | Score |
| ---: | ---: | --- | --- | --- | --- | --- | ---: |
| 62 | 2 | `preclamp-cache` | `0x141855CE0/0x01` | `none` | `none` | `none` |  |
| 73 | 730 | `preclamp-cache` | `0x141855CE0/0x01` | `none` | `none` | `none` |  |
| 77 | 991 | `preclamp-cache` | `0x141855CE0/0x01` | `none` | `none` | `none` |  |
| 81 | 1493 | `preclamp-cache` | `0x141855CE0/0x01` | `none` | `none` | `none` |  |
| 136 | 2530 | `preclamp-cache` | `0x141855CE0/0x01` | `none` | `none` | `none` |  |
| 144 | 2739 | `preclamp-cache` | `0x141855CE0/0x01` | `none` | `none` | `none` |  |
| 163 | 1 | `damage` | `0x141855CE0/0x01` | `none` | `none` | `none` |  |
| 181 | 4735 | `preclamp-cache` | `0x141855CE0/0x01` | `none` | `none` | `none` |  |

## Immediate Candidates

| Line | Target | Selected caster | Action | Debit | Credit | Score | Margin |
| ---: | --- | --- | --- | ---: | ---: | ---: | ---: |
| 63 | `0x141855CE0/0x01` | `none` | `none` | 184 | 0 |  |  |
| 74 | `0x141855CE0/0x01` | `none` | `none` | 184 | 0 |  |  |
| 78 | `0x141855CE0/0x01` | `none` | `none` | 184 | 0 |  |  |
| 82 | `0x141855CE0/0x01` | `none` | `none` | 184 | 0 |  |  |
| 137 | `0x141855CE0/0x01` | `none` | `none` | 250 | 0 |  |  |
| 145 | `0x141855CE0/0x01` | `0x141855EE0/0x1E` | `159` Divine Ruination | 250 | 0 | 2900 | 2147483647 |
| 182 | `0x141855CE0/0x01` | `none` | `none` | 250 | 0 |  |  |

## Formula Candidate Sources

- `ct-low`: 3
- `immediate-action`: 1
- `none`: 3
