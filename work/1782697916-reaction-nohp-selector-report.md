# Action Identity Log Analysis

Log: `work\1782695389-reaction-nohp-selector-log.txt`

## Summary

- Pre-clamp actor contexts: 5 (`resolved`=2, `ambiguous`=0, `none`=3).
- Actor contexts with known ability/basic ids: 2.
- Pending matches: 14 (`resolved`=0).
- Pending target caches: 16 (`pre-apply damage candidates`=5).
- Immediate candidate snapshots: 12 (`selected`=8).
- Formula candidates: 12.
- Selector probes: 3.

## Readiness Signals

- No hard action-identity gaps detected in this log.
- Selector no-HP outcomes with non-target source actor refs: 1/1.
- Pre-apply damage target-cache candidate(s): 5. These may include interrupted/cancelled incoming actions and need register-backed source proof.
- Pre-apply target-cache source hint(s): 7 across 2 cache(s). These are line-near formula correlations, not primary proof.

## Actor Context Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 2 | basic attack / implicit weapon action |

## Pending Action IDs

- No action ids observed.

## Immediate Candidate Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 8 | basic attack / implicit weapon action |

## Action-State IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 84 | basic attack / implicit weapon action |

## Pending Target Caches

- Pre-apply damage candidates (`dmg1C4 > 0`, damage result flag, `bb != 2`): 5.

| Line | Kind | Target | Damage | Credit | Charge | f1E5 | bb | Candidate? |
| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 91 | `enter` | `0x141855EE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 100 | `clear` | `0x141855EE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 116 | `enter` | `0x141855CE0/0x02` | 201 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 128 | `drop` | `0x141855EE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 138 | `clear` | `0x141855CE0/0x02` | 201 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 221 | `reenter` | `0x141855CE0/0x02` | 0 | 71 | 0 | `0x40` | 1 |  |
| 229 | `clear` | `0x141855CE0/0x02` | 0 | 71 | 0 | `0x40` | 1 |  |
| 242 | `enter` | `0x141855EE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 250 | `clear` | `0x141855EE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 264 | `reenter` | `0x141855EE0/0x80` | 422 | 0 | 130 | `0x80` | 1 | pre-apply damage |
| 272 | `drop` | `0x141855CE0/0x02` | 0 | 71 | 0 | `0x40` | 1 |  |
| 299 | `clear` | `0x141855EE0/0x80` | 422 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 306 | `enter` | `0x1418562E0/0x32` | 396 | 0 | 130 | `0x80` | 2 |  |
| 345 | `clear` | `0x1418562E0/0x32` | 396 | 0 | 130 | `0x80` | 2 |  |
| 382 | `drop` | `0x141855EE0/0x80` | 422 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 383 | `drop` | `0x1418562E0/0x32` | 396 | 0 | 130 | `0x80` | 2 |  |

## Target Cache Source Hints

These rows correlate a pre-apply target cache to nearby formula candidates with the same target and damage. They are useful for narrowing interrupted-action cases, but they are not register-backed proof.

| Cache line | Formula line | Distance | Target | Damage | Attacker | Source | Action hints |
| ---: | ---: | ---: | --- | ---: | --- | --- | --- |
| 116 | 120 | 4 | `0x141855CE0/0x02` | 201 | `0x1418560E0/id=0x1E` | `ct-reset` | `0` Basic Attack / implicit weapon |
| 116 | 127 | 11 | `0x141855CE0/0x02` | 201 | `none` | `none` | `0` Basic Attack / implicit weapon |
| 116 | 131 | 15 | `0x141855CE0/0x02` | 201 | `0x1418560E0/id=0x1E` | `immediate-action` | `0` Basic Attack / implicit weapon |
| 264 | 268 | 4 | `0x141855EE0/0x80` | 422 | `0x1418560E0/id=0x1E` | `immediate-action` | `0` Basic Attack / implicit weapon |
| 264 | 275 | 11 | `0x141855EE0/0x80` | 422 | `0x1418562E0/id=0x32` | `immediate-action` | `0` Basic Attack / implicit weapon |
| 264 | 279 | 15 | `0x141855EE0/0x80` | 422 | `none` | `none` | `0` Basic Attack / implicit weapon |
| 264 | 285 | 21 | `0x141855EE0/0x80` | 422 | `0x1418562E0/id=0x32` | `immediate-action` | `0` Basic Attack / implicit weapon |

## Selector Outcomes

| Evade type | Meaning | Count |
| ---: | --- | ---: |
| `0x00` | hit | 2 |
| `0x0B` | blade-grasp | 1 |

Selector-frame actor/action ids:

| Action id | Count |
| ---: | ---: |
| `0` | 24 |

No-HP selector context:

| Event | Evade | Unit | Source actor refs | Target/self refs |
| ---: | --- | --- | --- | --- |
| 1 | `0x0B` blade-grasp | `0x02` | `rdx->0x1E/act=0`<br>`r15->0x1E/act=0`<br>`+0xA0->0x1E/act=0` | `actor->0x02/act=0/self`<br>`rbx->0x02/act=0/self`<br>`r8->0x02/act=0/self`<br>`+0x8->0x02/act=0/self`<br>`+0xA8->0x02/act=0/self` |

| Line | Event | Unit | Evade | rec+1BE | rec+1C0 | rec+1C4 dmg | rec+1E5 | Actor refs | Control? |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| 142 | 1 | `0x02` | `0x0B` blade-grasp | `0x00` | `0x0B` | 0 | `0x00` | `actor->0x02/act=0/self`<br>`rbx->0x02/act=0/self`<br>`rdx->0x1E/act=0`<br>`r8->0x02/act=0/self`<br>`r15->0x1E/act=0`<br>`+0x8->0x02/act=0/self`<br>+2 more |  |
| 318 | 2 | `0x32` | `0x00` hit | `0x01` | `0x00` | 396 | `0x80` | `actor->0x32/act=0/self`<br>`rbx->0x32/act=0/self`<br>`rdx->0x80/act=0`<br>`r8->0x32/act=0/self`<br>`r15->0x80/act=0`<br>`+0x8->0x32/act=0/self`<br>+2 more |  |
| 337 | 3 | `0x32` | `0x00` hit | `0x01` | `0x00` | 396 | `0x80` | `actor->0x32/act=0/self`<br>`rbx->0x32/act=0/self`<br>`rdx->0x80/act=0`<br>`r8->0x32/act=0/self`<br>`r15->0x80/act=0`<br>`+0x8->0x32/act=0/self`<br>+2 more |  |

## Actor Context Events

| Line | Event | Target | Caster | Action | Debit | Verdict | Equip? |
| ---: | ---: | --- | --- | --- | ---: | --- | --- |
| 90 | 1 | `0x141855EE0/0x80` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 220 | 2 | `0x141855CE0/0x02` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 241 | 3 | `0x141855EE0/0x80` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 296 | 4 | `0x1418562E0/0x32` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 396 | `resolved` | `target+caster` |
| 320 | 5 | `0x1418562E0/0x32` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 396 | `resolved` | `target+caster` |

## Pending Matches

| Line | Event | Kind | Target | Caster | Action | Confidence | Score |
| ---: | ---: | --- | --- | --- | --- | --- | ---: |
| 93 | 3 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  |
| 118 | 43 | `preclamp-cache` | `0x141855CE0/0x02` | `none` | `none` | `none` |  |
| 125 | 534 | `preclamp-cache` | `0x141855CE0/0x02` | `none` | `none` | `none` |  |
| 129 | 2997 | `preclamp-cache` | `0x141855CE0/0x02` | `none` | `none` | `none` |  |
| 223 | 3027 | `preclamp-cache` | `0x141855CE0/0x02` | `none` | `none` | `none` |  |
| 244 | 3083 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  |
| 266 | 3147 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  |
| 273 | 6638 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  |
| 277 | 6639 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  |
| 283 | 6641 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  |
| 301 | 6807 | `preclamp-cache` | `0x1418562E0/0x32` | `none` | `none` | `none` |  |
| 316 | 1 | `damage` | `0x1418562E0/0x32` | `none` | `none` | `none` |  |
| 323 | 6965 | `preclamp-cache` | `0x1418562E0/0x32` | `none` | `none` | `none` |  |
| 335 | 2 | `damage` | `0x1418562E0/0x32` | `none` | `none` | `none` |  |

## Immediate Candidates

| Line | Target | Selected caster | Action | Debit | Credit | Score | Margin |
| ---: | --- | --- | --- | ---: | ---: | ---: | ---: |
| 94 | `0x141855EE0/0x80` | `none` | `none` | 0 | 34 |  |  |
| 119 | `0x141855CE0/0x02` | `none` | `none` | 201 | 0 |  |  |
| 126 | `0x141855CE0/0x02` | `none` | `none` | 201 | 0 |  |  |
| 130 | `0x141855CE0/0x02` | `0x1418560E0/0x1E` | `0` Basic Attack / implicit weapon | 201 | 0 | 2050 | 2147483647 |
| 224 | `0x141855CE0/0x02` | `0x1418560E0/0x1E` | `0` Basic Attack / implicit weapon | 0 | 71 | 1750 | 2147483647 |
| 245 | `0x141855EE0/0x80` | `0x1418560E0/0x1E` | `0` Basic Attack / implicit weapon | 0 | 34 | 1750 | 2147483647 |
| 267 | `0x141855EE0/0x80` | `0x1418560E0/0x1E` | `0` Basic Attack / implicit weapon | 422 | 0 | 1750 | 2147483647 |
| 274 | `0x141855EE0/0x80` | `0x1418562E0/0x32` | `0` Basic Attack / implicit weapon | 422 | 0 | 2050 | 300 |
| 278 | `0x141855EE0/0x80` | `none` | `none` | 422 | 0 |  |  |
| 284 | `0x141855EE0/0x80` | `0x1418562E0/0x32` | `0` Basic Attack / implicit weapon | 422 | 0 | 2300 | 300 |
| 302 | `0x1418562E0/0x32` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 396 | 0 | 2300 | 550 |
| 324 | `0x1418562E0/0x32` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 396 | 0 | 2050 | 300 |

## Formula Candidate Sources

- `ct-reset`: 1
- `immediate-action`: 8
- `none`: 3
