# Action Identity Log Analysis

Log: `work\1782729990-first-strike-targetcache-register-log.txt`

## Summary

- Pre-clamp actor contexts: 4 (`resolved`=2, `ambiguous`=0, `none`=2).
- Actor contexts with known ability/basic ids: 2.
- Pending matches: 9 (`resolved`=0).
- Pending target caches: 10 (`pre-apply damage candidates`=3).
- Immediate candidate snapshots: 7 (`selected`=3).
- Formula candidates: 7.
- Selector probes: 2.
- Hook-reg events: 30 (`targetcache`=2).
- Landmark hits: 0.

## Readiness Signals

- No hard action-identity gaps detected in this log.
- Pre-apply damage target-cache candidate(s): 3. These may include interrupted/cancelled incoming actions and need register-backed source proof.
- Pre-apply target-cache source hint(s): 3 across 1 cache(s). These are line-near formula correlations, not primary proof.

## Actor Context Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 2 | basic attack / implicit weapon action |

## Pending Action IDs

- No action ids observed.

## Immediate Candidate Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 3 | basic attack / implicit weapon action |

## Action-State IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 51 | basic attack / implicit weapon action |

## Pending Target Caches

- Pre-apply damage candidates (`dmg1C4 > 0`, damage result flag, `bb != 2`): 3.

| Line | Kind | Target | Damage | Credit | Charge | f1E5 | bb | Candidate? |
| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 65 | `enter` | `0x141855CE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 73 | `clear` | `0x141855CE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 115 | `reenter` | `0x141855CE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 123 | `clear` | `0x141855CE0/0x80` | 0 | 34 | 0 | `0x40` | 1 |  |
| 164 | `reenter` | `0x141855CE0/0x80` | 403 | 0 | 130 | `0x80` | 1 | pre-apply damage |
| 191 | `clear` | `0x141855CE0/0x80` | 403 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 198 | `enter` | `0x141855EE0/0x32` | 396 | 0 | 130 | `0x80` | 2 |  |
| 244 | `clear` | `0x141855EE0/0x32` | 396 | 0 | 130 | `0x80` | 2 |  |
| 251 | `drop` | `0x141855CE0/0x80` | 403 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 252 | `drop` | `0x141855EE0/0x32` | 396 | 0 | 130 | `0x80` | 2 |  |

## Target Cache Source Hints

These rows correlate a pre-apply target cache to nearby formula candidates with the same target and damage. They are useful for narrowing interrupted-action cases, but they are not register-backed proof.

| Cache line | Formula line | Distance | Target | Damage | Attacker | Source | Action hints |
| ---: | ---: | ---: | --- | ---: | --- | --- | --- |
| 164 | 169 | 5 | `0x141855CE0/0x80` | 403 | `0x141855EE0/id=0x32` | `ct-low` | `0` Basic Attack / implicit weapon |
| 164 | 175 | 11 | `0x141855CE0/0x80` | 403 | `none` | `none` | `0` Basic Attack / implicit weapon |
| 164 | 178 | 14 | `0x141855CE0/0x80` | 403 | `0x141855EE0/id=0x32` | `immediate-action` | `0` Basic Attack / implicit weapon |

## Target-Cache Register Verdict

- Target-cache hook events with source-candidate refs: 2/2.
- Strong candidate proof: at least one target-cache hook saw actor refs for a unit other than the target. In First Strike/Hamedo captures, this is the signal we need for the interrupted incoming source.

| Line | Event | Hook ptr | Target | Source refs | Target/self refs |
| ---: | ---: | --- | --- | --- | --- |
| 165 | 90 | `0x141855EE0` | `0x141855CE0/0x80` | `rcx->0x32/unit`<br>`rdi->0x32/unit`<br>`r8->0x32/unit`<br>`+0x40->0x32/unit`<br>`+0x70->0x32/unit` |  |
| 183 | 2666 | `0x141855EE0` | `0x141855CE0/0x80` | `rcx->0x32/unit`<br>`rdi->0x32/unit`<br>`r8->0x32/unit`<br>`+0x90->0x32/unit` |  |

## Register Actor Refs

Target-cache hook events:

| Line | Event | Target | Actor refs |
| ---: | ---: | --- | --- |
| 165 | 90 | `0x141855CE0/0x80` | `rcx->0x32/unit`<br>`rsi->0x82/unit`<br>`rdi->0x32/unit`<br>`r8->0x32/unit`<br>`+0x40->0x32/unit`<br>`+0x70->0x32/unit` |
| 183 | 2666 | `0x141855CE0/0x80` | `rcx->0x32/unit`<br>`rsi->0x82/unit`<br>`rdi->0x32/unit`<br>`r8->0x32/unit`<br>`+0x90->0x32/unit` |

## Selector Outcomes

| Evade type | Meaning | Count |
| ---: | --- | ---: |
| `0x00` | hit | 2 |

Selector-frame actor/action ids:

| Action id | Count |
| ---: | ---: |
| `0` | 16 |

| Line | Event | Unit | Evade | rec+1BE | rec+1C0 | rec+1C4 dmg | rec+1E5 | Actor refs | Control? |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| 210 | 1 | `0x32` | `0x00` hit | `0x01` | `0x00` | 396 | `0x80` | `actor->0x32/act=0/self`<br>`rbx->0x32/act=0/self`<br>`rdx->0x80/act=0`<br>`r8->0x32/act=0/self`<br>`r15->0x80/act=0`<br>`+0x8->0x32/act=0/self`<br>+2 more |  |
| 229 | 2 | `0x32` | `0x00` hit | `0x01` | `0x00` | 396 | `0x80` | `actor->0x32/act=0/self`<br>`rbx->0x32/act=0/self`<br>`rdx->0x80/act=0`<br>`r8->0x32/act=0/self`<br>`r15->0x80/act=0`<br>`+0x8->0x32/act=0/self`<br>+2 more |  |

## Actor Context Events

| Line | Event | Target | Caster | Action | Debit | Verdict | Equip? |
| ---: | ---: | --- | --- | --- | ---: | --- | --- |
| 64 | 1 | `0x141855CE0/0x80` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 114 | 2 | `0x141855CE0/0x80` | `none` | `-1` UNKNOWN | 0 | `no-caster-actor` |  |
| 188 | 3 | `0x141855EE0/0x32` | `0x141855CE0/0x80` | `0` Basic Attack / implicit weapon | 396 | `resolved` | `target+caster` |
| 212 | 4 | `0x141855EE0/0x32` | `0x141855CE0/0x80` | `0` Basic Attack / implicit weapon | 396 | `resolved` | `target+caster` |

## Pending Matches

| Line | Event | Kind | Target | Caster | Action | Confidence | Score |
| ---: | ---: | --- | --- | --- | --- | --- | ---: |
| 67 | 3 | `preclamp-cache` | `0x141855CE0/0x80` | `none` | `none` | `none` |  |
| 117 | 52 | `preclamp-cache` | `0x141855CE0/0x80` | `none` | `none` | `none` |  |
| 167 | 91 | `preclamp-cache` | `0x141855CE0/0x80` | `none` | `none` | `none` |  |
| 173 | 509 | `preclamp-cache` | `0x141855CE0/0x80` | `none` | `none` | `none` |  |
| 176 | 2663 | `preclamp-cache` | `0x141855CE0/0x80` | `none` | `none` | `none` |  |
| 193 | 2788 | `preclamp-cache` | `0x141855EE0/0x32` | `none` | `none` | `none` |  |
| 208 | 1 | `damage` | `0x141855EE0/0x32` | `none` | `none` | `none` |  |
| 215 | 2902 | `preclamp-cache` | `0x141855EE0/0x32` | `none` | `none` | `none` |  |
| 227 | 2 | `damage` | `0x141855EE0/0x32` | `none` | `none` | `none` |  |

## Immediate Candidates

| Line | Target | Selected caster | Action | Debit | Credit | Score | Margin |
| ---: | --- | --- | --- | ---: | ---: | ---: | ---: |
| 68 | `0x141855CE0/0x80` | `none` | `none` | 0 | 34 |  |  |
| 118 | `0x141855CE0/0x80` | `none` | `none` | 0 | 34 |  |  |
| 168 | `0x141855CE0/0x80` | `none` | `none` | 403 | 0 |  |  |
| 174 | `0x141855CE0/0x80` | `none` | `none` | 403 | 0 |  |  |
| 177 | `0x141855CE0/0x80` | `0x141855EE0/0x32` | `0` Basic Attack / implicit weapon | 403 | 0 | 2050 | 2147483647 |
| 194 | `0x141855EE0/0x32` | `0x141855CE0/0x80` | `0` Basic Attack / implicit weapon | 396 | 0 | 2300 | 2147483647 |
| 216 | `0x141855EE0/0x32` | `0x141855CE0/0x80` | `0` Basic Attack / implicit weapon | 396 | 0 | 2050 | 2147483647 |

## Formula Candidate Sources

- `ct-low`: 2
- `immediate-action`: 3
- `none`: 2
