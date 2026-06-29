# Action Identity Evidence Coverage

This is an aggregate of existing `battleprobe` logs. It is a dated work report, not a canonical engine fact.

## Aggregate Signals

- Logs scanned: 68
- Pre-clamp actor contexts: 32 (`resolved`=19, `ambiguous`=0, unresolved positive debit=0)
- Legacy self-hit/AoE actor-context hints: 2 (candidate cases for `resolved-self` retest).
- Pending matches: 343 (`resolved`=105)
- Immediate candidate snapshots: 172 (`selected`=78)
- Formula candidates: 178
- Selector probes: 9 (with actor refs=5)
- Logs without actor-context probe evidence: 58 (mostly legacy captures; this is coverage debt, not proof of failure).

## Action IDs Seen

| Action id | Meaning | Count |
| ---: | --- | ---: |
| 0 | Basic Attack / implicit weapon | 16 |
| 1 | Cure | 43 |
| 16 | Fire | 15 |
| 158 | Hallowed Bolt | 2 |
| 159 | Divine Ruination | 30 |
| 257 | Braver | 12 |
| 258 | Cross Slash | 68 |
| 265 | Choco Beak | 16 |

## Selector Outcomes Seen

| Evade type | Count |
| ---: | ---: |
| `0x00` | 8 |
| `0x0B` | 1 |

## Selector Actor Action IDs Seen

| Action id | Meaning | Count |
| ---: | --- | ---: |
| 0 | Basic Attack / implicit weapon | 32 |
| 159 | Divine Ruination | 3 |

## Repeated Issues

- No hard action-identity issues detected by the current analyzer.

## Per-Log Matrix

| Log | Actor ctx | Pending | Immediate | Formula | Selector | Selector actor refs | Issues |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| `work\1782675078-battleprobe-log-before-heal-strict-no-preview-poke.txt` | 1/1 | 6/57 | 28/52 | 52 | 0 | 0 | none |
| `work\1782676130-battleprobe-log-heal-pending-credit-success.txt` | 1/1 | 4/6 | 0/0 | 3 | 0 | 0 | none |
| `work\1782681848-battleprobe-log-magic-fire-custom-agrias-ramza-success.txt` | 1/2 | 6/6 | 0/0 | 0 | 0 | 0 | none |
| `work\1782682260-battleprobe-log-before-preview-heal-negative-test.txt` | 1/2 | 6/6 | 0/0 | 0 | 0 | 0 | none |
| `work\1782685331-battleprobe-log-heal-formula-preclamp-success-regen-caught.txt` | 1/1 | 6/57 | 28/52 | 52 | 0 | 0 | none |
| `work\1782693058-action-identity-live-observe-log.txt` | 6/10 | 2/18 | 4/14 | 15 | 0 | 0 | none |
| `work\1782694729-selector-baseline-log.txt` | 2/2 | 0/7 | 2/4 | 4 | 2 | 2 | none |
| `work\1782695389-reaction-nohp-selector-log.txt` | 2/5 | 0/14 | 8/12 | 12 | 3 | 3 | none |
| `work\live-captures\battleprobe_log.equipment-readout-ramza-ninja.snapshot.txt` | 2/4 | 0/1 | 0/0 | 0 | 0 | 0 | none |
| `work\live-captures\battleprobe_log.executing-action-resolver-probe-cross-slash-agrias-ninja.snapshot.txt` | 2/4 | 2/2 | 0/0 | 0 | 0 | 0 | none |
| `work\1782675458-battleprobe-log-heal-strict-no-preview-poke-natural-result.txt` | 0/0 | 2/24 | 4/19 | 20 | 0 | 0 | legacy/no actor probe |
| `work\1782675887-battleprobe-log-before-heal-pending-credit-test.txt` | 0/0 | 2/24 | 4/19 | 20 | 0 | 0 | legacy/no actor probe |
| `work\1782677772-battleprobe-log-before-physical-custom-test.txt` | 0/0 | 1/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\1782678308-battleprobe-log-physical-custom-agrias-beowulf-success.txt` | 0/0 | 0/5 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\1782683501-battleprobe-log-natural-heal-preview-agrias-ramza-86hp.txt` | 0/0 | 0/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\1782683703-battleprobe-log-heal-preview-credit-poke-123-success.txt` | 0/0 | 0/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\1782683878-battleprobe-log-heal-preview-credit-poke-500-success.txt` | 0/0 | 0/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\1782684946-battleprobe-log-before-heal-formula-preclamp-test.txt` | 0/0 | 0/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\battleprobe_log.hit-to-miss-v1-FAILED.20260626-225741.txt` | 0/0 | 0/1 | 0/0 | 0 | 3 | 0 | legacy/no actor probe |
| `work\battleprobe_log.hit-to-miss-v2-PASS.20260626-230526.txt` | 0/0 | 0/0 | 0/0 | 0 | 1 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-after-ramza-wait-ninja-reraise.snapshot.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-baseline-cloud-active.snapshot.txt` | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-confirmed-before-cloud-wait.snapshot.txt` | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-preview-cross-slash-agrias-187.snapshot.txt` | 0/0 | 0/0 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-preview-ramza-rush-ninja-50.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-resolved-ramza-rush-ninja-ko-before-wait.snapshot.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.actor-dump-basic-agrias-beowulf.snapshot.txt` | 0/0 | 1/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.actor-dump-braver-agrias.snapshot.txt` | 0/0 | 1/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.executing-action-actor-dump-probe-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.executing-action-pointer-probe-full.20260624-194730.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.executing-action-pointer-probe-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-after-ramza-wait-ninja-reraise.snapshot.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-preview-ramza-rush-ninja-50.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-resolved-ramza-rush-ninja-ko-before-wait.snapshot.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.instant-basic-7c-vanilla.snapshot.txt` | 0/0 | 0/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.instant-basic-memory-probe-agrias-beowulf.snapshot.txt` | 0/0 | 0/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.instant-basic-memory-probe-ninja-dual-agrias-lethal.snapshot.txt` | 0/0 | 0/4 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.instant-basic-memory-probe-ramza-beowulf-lethal.snapshot.txt` | 0/0 | 0/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-preview-ramza-rush-ninja-50.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-resolved-ramza-rush-ninja-ko-followup.snapshot.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-resolved-ramza-rush-ninja-ko.snapshot.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-context-dry-run-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-primary-live-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-tracker-live-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-action-boundary-probe.20260623-162934.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-immediate-action-ko-boundary-probe.20260623-154450.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-immediate-basic-7b-deploy.20260623-222711.txt` | 0/0 | 0/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-immediate-basic-7c-deploy.20260623-223702.txt` | 0/0 | 0/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-instant-basic-memory-probe-deploy.20260623-225358.txt` | 0/0 | 0/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-instant-basic-memory-probe-regdump-deploy.20260623-225917.txt` | 0/0 | 0/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-instant-basic-repeat-deploy.20260623-231005.txt` | 0/0 | 0/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-ko-landmark-probe.20260623-173303.txt` | 0/0 | 2/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-ko-pre-damage-probe.20260623-135217.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-pending-context-dry-run.20260623-130220.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-pending-primary-deploy.20260623-112243.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-resolver-probe.20260624-205320.txt` | 0/0 | 1/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-candidates-resolved-cross-slash-aoe.snapshot.txt` | 0/0 | 6/6 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-eager-immediate-ninja-agrias-999.snapshot.txt` | 0/0 | 0/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-eager-immediate-ninja-agrias-success-87x2.snapshot.txt` | 0/0 | 0/5 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-immediate-basic-7b-agrias-beowulf.snapshot.txt` | 0/0 | 0/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-immediate-basic-agrias-beowulf.snapshot.txt` | 0/0 | 0/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-immediate-ninja-dual-agrias-failed-180x2.snapshot.txt` | 0/0 | 0/1 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-lethal-braver-success.snapshot.txt` | 0/0 | 3/3 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-post-cross-slash-success.snapshot.txt` | 0/0 | 6/6 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp115-resolved-cross-slash-agrias-ko.snapshot.txt` | 0/0 | 2/2 | 0/0 | 0 | 0 | 0 | legacy/no actor probe |

## Offline Conclusion

- Existing logs can validate the parser/tooling and several action-id surfaces.
- Existing logs are not enough to retire the live probe: they do not cover every required class under one observe-only profile.
- Missing or weak coverage remains for counters/reactions, multiple simultaneous pending actions, and cross-battle actor-array stability.
- The prepared live probe should therefore be run before promoting actor context to the runtime primary resolver.
