# Action Identity Log Analysis

Log: `work\1782757320-multihit-counter-clean-log.txt`

## Summary

- Pre-clamp actor contexts: 6 (`resolved`=5, `ambiguous`=0, `none`=1).
- Actor contexts with known ability/basic ids: 5.
- Pending matches: 22 (`resolved`=0).
- Pending target caches: 10 (`pre-apply damage candidates`=8).
- Immediate candidate snapshots: 15 (`selected`=2).
- Formula candidates: 15.
- Selector probes: 6.
- Hook-reg events: 37 (`targetcache`=4).
- Landmark hits: 0.

## Readiness Signals

- Actor context unresolved for positive-debit event(s): 1.
- Pre-apply damage target-cache candidate(s): 8. These may include interrupted/cancelled incoming actions and need register-backed source proof.
- Pre-apply target-cache source hint(s): 10 across 4 cache(s). These are line-near formula correlations, not primary proof.
- Selector fallback source hint(s): 1 unresolved positive-debit actor context(s) had a nearby selector frame with non-target source actor refs.

## Actor Context Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 5 | basic attack / implicit weapon action |

## Pending Action IDs

- No action ids observed.

## Immediate Candidate Action IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 2 | basic attack / implicit weapon action |

## Action-State IDs

| Action id | Meaning | Count | Confidence note |
| ---: | --- | ---: | --- |
| 0 | Basic Attack / implicit weapon | 51 | basic attack / implicit weapon action |

## Pending Target Caches

- Pre-apply damage candidates (`dmg1C4 > 0`, damage result flag, `bb != 2`): 8.

| Line | Kind | Target | Damage | Credit | Charge | f1E5 | bb | Candidate? |
| ---: | --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| 88 | `enter` | `0x141855CE0/0x1E` | 270 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 222 | `enter` | `0x141855CE0/0x1E` | 97 | 0 | 130 | `0x80` | 0 | pre-apply damage |
| 305 | `enter` | `0x141855EE0/0x80` | 388 | 0 | 130 | `0x80` | 1 | pre-apply damage |
| 319 | `clear` | `0x141855CE0/0x1E` | 97 | 0 | 130 | `0x81` | 2 |  |
| 330 | `clear` | `0x141855EE0/0x80` | 388 | 0 | 130 | `0x80` | 1 | pre-apply damage |
| 343 | `drop` | `0x141855CE0/0x1E` | 97 | 0 | 130 | `0x81` | 2 |  |
| 344 | `drop` | `0x141855EE0/0x80` | 388 | 0 | 130 | `0x80` | 1 | pre-apply damage |
| 375 | `enter` | `0x141855EE0/0x80` | 388 | 0 | 130 | `0x80` | 1 | pre-apply damage |
| 394 | `clear` | `0x141855EE0/0x80` | 388 | 0 | 130 | `0x80` | 1 | pre-apply damage |
| 408 | `drop` | `0x141855EE0/0x80` | 388 | 0 | 130 | `0x80` | 1 | pre-apply damage |

## Target Cache Source Hints

These rows correlate a pre-apply target cache to nearby formula candidates with the same target and damage. They are useful for narrowing interrupted-action cases, but they are not register-backed proof.

| Cache line | Formula line | Distance | Target | Damage | Attacker | Source | Action hints |
| ---: | ---: | ---: | --- | ---: | --- | --- | --- |
| 88 | 93 | 5 | `0x141855CE0/0x1E` | 270 | `0x141855EE0/id=0x80` | `ct-low` | `0` Basic Attack / implicit weapon |
| 88 | 99 | 11 | `0x141855CE0/0x1E` | 270 | `none` | `none` | `0` Basic Attack / implicit weapon |
| 88 | 103 | 15 | `0x141855CE0/0x1E` | 270 | `0x141855EE0/id=0x80` | `ct-low` | `0` Basic Attack / implicit weapon |
| 88 | 107 | 19 | `0x141855CE0/0x1E` | 270 | `none` | `none` | `0` Basic Attack / implicit weapon |
| 88 | 111 | 23 | `0x141855CE0/0x1E` | 270 | `0x141855EE0/id=0x80` | `ct-low` | `0` Basic Attack / implicit weapon |
| 222 | 227 | 5 | `0x141855CE0/0x1E` | 97 | `0x141855EE0/id=0x80` | `ct-low` | `0` Basic Attack / implicit weapon |
| 222 | 239 | 17 | `0x141855CE0/0x1E` | 97 | `none` | `none` | `0` Basic Attack / implicit weapon |
| 222 | 243 | 21 | `0x141855CE0/0x1E` | 97 | `0x141855EE0/id=0x80` | `ct-low` | `0` Basic Attack / implicit weapon |
| 305 | 310 | 5 | `0x141855EE0/0x80` | 388 | `0x141855CE0/id=0x1E` | `immediate-action` | `0` Basic Attack / implicit weapon |
| 375 | 380 | 5 | `0x141855EE0/0x80` | 388 | `none` | `none` | `0` Basic Attack / implicit weapon |

## Selector Fallback Hints

These rows correlate an unresolved positive-debit `[PRECLAMP-ACTOR-CTX]` with a nearby `[SELECTOR-PROBE]` for the same target and staged damage. The selector frame runs too late to compute that same pre-clamp rewrite, but it is strong evidence for no-HP outcomes, reaction/cancel diagnostics, and fallback design.

| Actor line | Selector line | Distance | Target | Debit | Selector | Source actor refs | Target/self refs | Actions |
| ---: | ---: | ---: | --- | ---: | --- | --- | --- | --- |
| 373 | 391 | 18 | `0x80` | 388 | `0x00` hit | `rdx->0x1E/act=0`<br>`r15->0x1E/act=0`<br>`+0xA0->0x1E/act=0` | `actor->0x80/act=0/self`<br>`rbx->0x80/act=0/self`<br>`r8->0x80/act=0/self`<br>`+0x8->0x80/act=0/self`<br>`+0xA8->0x80/act=0/self` | `0` Basic Attack / implicit weapon x3 |

## Target-Cache Register Verdict

- Target-cache hook events with source-candidate refs: 2/4.
- Strong candidate proof: at least one target-cache hook saw source-candidate unit/actor refs for a unit other than the target. In First Strike/Hamedo captures, this is the signal we need for the interrupted incoming source.
- Named incoming action proof: not present in this capture; source-candidate refs are basic/implicit or direct unit refs (`unit-only` refs=10).

| Line | Event | Hook ptr | Target | Source refs | Target/self refs |
| ---: | ---: | --- | --- | --- | --- |
| 89 | 4 | `0x141855EE0` | `0x141855CE0/0x1E` | `rcx->0x80/unit`<br>`rdi->0x80/unit`<br>`r8->0x80/unit`<br>`+0x40->0x80/unit`<br>`+0x70->0x80/unit` |  |
| 223 | 5386 | `0x141855EE0` | `0x141855CE0/0x1E` | `rcx->0x80/unit`<br>`rdi->0x80/unit`<br>`r8->0x80/unit`<br>`+0x40->0x80/unit`<br>`+0x70->0x80/unit` |  |

## Register Unit/Actor Refs

Target-cache hook events:

| Line | Event | Target | Unit/actor refs |
| ---: | ---: | --- | --- |
| 89 | 4 | `0x141855CE0/0x1E` | `rcx->0x80/unit`<br>`rdi->0x80/unit`<br>`r8->0x80/unit`<br>`+0x40->0x80/unit`<br>`+0x70->0x80/unit` |
| 223 | 5386 | `0x141855CE0/0x1E` | `rcx->0x80/unit`<br>`rdi->0x80/unit`<br>`r8->0x80/unit`<br>`+0x40->0x80/unit`<br>`+0x70->0x80/unit` |
| 306 | 9871 | `0x141855EE0/0x80` |  |
| 376 | 10073 | `0x141855EE0/0x80` |  |

## Selector Outcomes

| Evade type | Meaning | Count |
| ---: | --- | ---: |
| `0x00` | hit | 6 |

Selector-frame actor/action ids:

| Action id | Count |
| ---: | ---: |
| `0` | 48 |

| Line | Event | Unit | Evade | rec+1BE | rec+1C0 | rec+1C4 dmg | rec+1E5 | Actor refs | Control? |
| ---: | ---: | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| 279 | 1 | `0x1E` | `0x00` hit | `0x01` | `0x00` | 97 | `0x81` | `actor->0x1E/act=0/self`<br>`rbx->0x1E/act=0/self`<br>`rdx->0x80/act=0`<br>`r8->0x1E/act=0/self`<br>`r15->0x80/act=0`<br>`+0x8->0x1E/act=0/self`<br>+2 more |  |
| 297 | 2 | `0x1E` | `0x00` hit | `0x01` | `0x00` | 97 | `0x81` | `actor->0x1E/act=0/self`<br>`rbx->0x1E/act=0/self`<br>`rdx->0x80/act=0`<br>`r8->0x1E/act=0/self`<br>`r15->0x80/act=0`<br>`+0x8->0x1E/act=0/self`<br>+2 more |  |
| 325 | 3 | `0x80` | `0x00` hit | `0x01` | `0x00` | 388 | `0x80` | `actor->0x80/act=0/self`<br>`rbx->0x80/act=0/self`<br>`rdx->0x1E/act=0`<br>`r8->0x80/act=0/self`<br>`r15->0x1E/act=0`<br>`+0x8->0x80/act=0/self`<br>+2 more |  |
| 363 | 4 | `0x1E` | `0x00` hit | `0x01` | `0x00` | 97 | `0x80` | `actor->0x1E/act=0`<br>`rbx->0x1E/act=0`<br>`rdx->0x80/act=0`<br>`r8->0x1E/act=0`<br>`r15->0x80/act=0`<br>`+0x8->0x1E/act=0`<br>+2 more |  |
| 371 | 5 | `0x1E` | `0x00` hit | `0x01` | `0x00` | 97 | `0x80` | `actor->0x1E/act=0`<br>`rbx->0x1E/act=0`<br>`rdx->0x80/act=0`<br>`r8->0x1E/act=0`<br>`r15->0x80/act=0`<br>`+0x8->0x1E/act=0`<br>+2 more |  |
| 391 | 6 | `0x80` | `0x00` hit | `0x01` | `0x00` | 388 | `0x80` | `actor->0x80/act=0/self`<br>`rbx->0x80/act=0/self`<br>`rdx->0x1E/act=0`<br>`r8->0x80/act=0/self`<br>`r15->0x1E/act=0`<br>`+0x8->0x80/act=0/self`<br>+2 more |  |

## Actor Context Events

| Line | Event | Target | Caster | Action | Debit | Verdict | Equip? |
| ---: | ---: | --- | --- | --- | ---: | --- | --- |
| 264 | 1 | `0x141855CE0/0x1E` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 97 | `resolved` | `target+caster` |
| 284 | 2 | `0x141855CE0/0x1E` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 97 | `resolved` | `target+caster` |
| 302 | 3 | `0x141855EE0/0x80` | `0x141855CE0/0x1E` | `0` Basic Attack / implicit weapon | 388 | `resolved` | `target+caster` |
| 360 | 4 | `0x141855CE0/0x1E` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 97 | `resolved` | `target+caster` |
| 368 | 5 | `0x141855CE0/0x1E` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 97 | `resolved` | `target+caster` |
| 373 | 6 | `0x141855EE0/0x80` | `none` | `-1` UNKNOWN | 388 | `no-caster-actor` | `target` |

## Pending Matches

- Max pending contention: active=0, trackedPending=0, trackedResolving=0.
- Rows under contention: 0; resolved under contention: 0.

| Line | Event | Kind | Target | Caster | Action | Confidence | Score | Active | Pending | Resolving |
| ---: | ---: | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: |
| 59 | 1 | `damage` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 91 | 5 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 97 | 512 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 101 | 631 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 105 | 1164 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 109 | 1418 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 115 | 1942 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 119 | 2079 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 123 | 2587 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 182 | 3 | `healing` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 202 | 5 | `damage` | `0x141855EE0/0x80` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 225 | 5387 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 237 | 6293 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 241 | 7245 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 254 | 8444 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 258 | 9388 | `preclamp-cache` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 277 | 7 | `damage` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 295 | 8 | `damage` | `0x141855CE0/0x1E` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 308 | 9872 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 320 | 9 | `damage` | `0x141855EE0/0x80` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 378 | 10074 | `preclamp-cache` | `0x141855EE0/0x80` | `none` | `none` | `none` |  | 0 | 0 | 0 |
| 389 | 10 | `damage` | `0x141855EE0/0x80` | `none` | `none` | `none` |  | 0 | 0 | 0 |

## Immediate Candidates

| Line | Target | Selected caster | Action | Debit | Credit | Score | Margin |
| ---: | --- | --- | --- | ---: | ---: | ---: | ---: |
| 92 | `0x141855CE0/0x1E` | `none` | `none` | 270 | 0 |  |  |
| 98 | `0x141855CE0/0x1E` | `none` | `none` | 270 | 0 |  |  |
| 102 | `0x141855CE0/0x1E` | `none` | `none` | 270 | 0 |  |  |
| 106 | `0x141855CE0/0x1E` | `none` | `none` | 270 | 0 |  |  |
| 110 | `0x141855CE0/0x1E` | `none` | `none` | 270 | 0 |  |  |
| 116 | `0x141855CE0/0x1E` | `none` | `none` | 270 | 0 |  |  |
| 120 | `0x141855CE0/0x1E` | `none` | `none` | 270 | 0 |  |  |
| 124 | `0x141855CE0/0x1E` | `none` | `none` | 270 | 0 |  |  |
| 226 | `0x141855CE0/0x1E` | `none` | `none` | 97 | 0 |  |  |
| 238 | `0x141855CE0/0x1E` | `none` | `none` | 97 | 0 |  |  |
| 242 | `0x141855CE0/0x1E` | `none` | `none` | 97 | 0 |  |  |
| 255 | `0x141855CE0/0x1E` | `none` | `none` | 97 | 0 |  |  |
| 259 | `0x141855CE0/0x1E` | `0x141855EE0/0x80` | `0` Basic Attack / implicit weapon | 97 | 0 | 2300 | 2147483647 |
| 309 | `0x141855EE0/0x80` | `0x141855CE0/0x1E` | `0` Basic Attack / implicit weapon | 388 | 0 | 2050 | 2147483647 |
| 379 | `0x141855EE0/0x80` | `none` | `none` | 388 | 0 |  |  |

## Formula Candidate Sources

- `ct-low`: 6
- `immediate-action`: 2
- `none`: 7
