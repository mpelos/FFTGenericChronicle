# Action Identity Evidence Coverage

This is an aggregate of existing `battleprobe` logs. It is a dated work report, not a canonical engine fact.

## Aggregate Signals

- Logs scanned: 102
- Pre-clamp actor contexts: 69 (`resolved`=41, `ambiguous`=0, unresolved positive debit=3)
- Legacy self-hit/AoE actor-context hints: 2 (candidate cases for `resolved-self` retest).
- Pending matches: 443 (`resolved`=109, multi-target batches=39, max active batches=1)
- Pending target caches: 583 (`pre-apply damage candidates`=203)
- Pre-apply target-cache source hints: 69 across 27 cache(s).
- Immediate candidate snapshots: 250 (`selected`=95)
- Formula candidates: 258
- Hook-reg events: 1239 (`targetcache`=20, with unit/actor refs=18)
- Target-cache source-candidate refs: events=18/20; action ids=none seen; unit-only refs=82
- Landmark hits: 56 (with unit/actor refs=0)
- Selector probes: 34 (with actor refs=30)
- Selector no-HP outcomes: 3 (with non-target source actor refs=1)
- Selector fallback hints for unresolved positive-debit actor contexts: 3
- Logs without actor-context probe evidence: 81 (mostly legacy captures; this is coverage debt, not proof of failure).

## DCL Action-Identity Requirement Matrix

| Requirement | Coverage | Evidence / remaining gap |
| --- | --- | --- |
| HP-apply target/source/action | Missing | 41/69 actor contexts resolved; unresolved positive debit=3 |
| Selector fallback for unresolved HP actor context | Partial | selector fallback hints=3; this is diagnostic/no-HP support, not same-frame pre-clamp formula authority |
| Immediate basic attack identity | Covered | actionId 0 seen 19 actor-context time(s), 22 immediate candidate time(s) |
| Immediate named action identity | Covered | 1 Cure x20, 158 Hallowed Bolt x2, 159 Divine Ruination x29, 257 Braver x6, 265 Choco Beak x16 |
| Charged/pending action identity | Covered | 109/443 pending matches resolved; ids: 1 Cure x20, 16 Fire x13, 257 Braver x12, 258 Cross Slash x64 |
| AoE or multi-target pending batch | Partial | multi-target pending batches=39; HP target separation still needs explicit hit/batch ownership |
| Selector-frame hit identity | Covered | 30/34 selector probes have actor refs; ids: 0 Basic Attack / implicit weapon x175, 158 Hallowed Bolt x18, 159 Divine Ruination x6, 257 Braver x6 |
| Native no-HP reaction identity, basic attack | Covered | no-HP source actionId 0 count=3 |
| Native no-HP reaction identity, named action | Missing | none seen |
| Self-hit / self-AoE attribution | Partial | resolved-self=0; legacy hints=2 |
| Multiple simultaneous pending actions | Missing | max active batches observed=1 |
| Hamedo/First-Strike cancelled incoming action | Partial | reaction damage is visible when it reaches HP apply; basic incoming source has target-cache source-candidate register proof=18/20; target-cache source action ids=none seen; unit-only refs=82; named incoming action id still needs live proof |
| Tile/epicenter target reconstruction | Open | no canonical parser surface for selected tile, epicenter, facing, or final AoE membership yet |
| Cross-battle actor-array stability | Partial | 102 logs scanned; actor context resolves in aggregate, but stability is not a dedicated assertion |

## Action IDs Seen

| Action id | Meaning | Count |
| ---: | --- | ---: |
| 0 | Basic Attack / implicit weapon | 41 |
| 1 | Cure | 43 |
| 16 | Fire | 15 |
| 158 | Hallowed Bolt | 8 |
| 159 | Divine Ruination | 32 |
| 257 | Braver | 22 |
| 258 | Cross Slash | 68 |
| 265 | Choco Beak | 16 |

## Selector Outcomes Seen

| Evade type | Count |
| ---: | ---: |
| `0x00` | 33 |
| `0x0B` | 1 |

## Selector Actor Action IDs Seen

| Action id | Meaning | Count |
| ---: | --- | ---: |
| 0 | Basic Attack / implicit weapon | 175 |
| 158 | Hallowed Bolt | 18 |
| 159 | Divine Ruination | 6 |
| 257 | Braver | 6 |

## Selector No-HP Source Action IDs Seen

| Action id | Meaning | Count |
| ---: | --- | ---: |
| 0 | Basic Attack / implicit weapon | 3 |

## Target-Cache Source Action IDs Seen

- No target-cache source action ids seen.

## Repeated Issues

| Issue | Logs/events |
| --- | ---: |
| Immediate candidate snapshots exist but none selected a source. | 3 |
| Primary evidence weak: actor context never resolved a caster. | 1 |
| Actor context unresolved for positive-debit event(s): 2. | 1 |
| Actor context unresolved for positive-debit event(s): 1. | 1 |

## Per-Log Matrix

| Log | Actor ctx | Pending | Target cache | Cache hints | Targetcache regs | Landmarks | Immediate | Formula | Selector | No-HP selector | Selector fallback | Selector actor refs | Issues |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| `work\1782686404-hallowed-bolt-shirahadori-not-applicable-log.txt` | 3/4 | 0/1 | 0/2 | 0/0 | 0/0 | 0/0 | 0/1 | 1 | 3 | 0/0 | 0 | 3 | Immediate candidate snapshots exist but none selected a source. |
| `work\1782697538-hallowed-bolt-shirahadori-not-applicable-log.txt` | 3/4 | 0/1 | 0/2 | 0/0 | 0/0 | 0/0 | 0/1 | 1 | 3 | 0/0 | 0 | 3 | Immediate candidate snapshots exist but none selected a source. |
| `work\1782731857-first-strike-named-action-log-finalcopy.txt` | 0/2 | 0/5 | 1/5 | 1/1 | 2/2 | 0/0 | 0/5 | 5 | 0 | 0/0 | 0 | 0 | Primary evidence weak: actor context never resolved a caster.; Immediate candidate snapshots exist but none selected a source. |
| `work\1782732390-basic-first-strike-clean-log.txt` | 2/6 | 0/12 | 2/11 | 2/1 | 2/2 | 0/0 | 3/8 | 8 | 4 | 0/0 | 2 | 4 | Actor context unresolved for positive-debit event(s): 2. |
| `work\1782757320-multihit-counter-clean-log.txt` | 5/6 | 0/22 | 8/10 | 10/4 | 2/4 | 0/0 | 2/15 | 15 | 6 | 0/0 | 1 | 6 | Actor context unresolved for positive-debit event(s): 1. |
| `work\1782675078-battleprobe-log-before-heal-strict-no-preview-poke.txt` | 1/1 | 6/57 | 1/34 | 4/1 | 0/0 | 0/0 | 28/52 | 52 | 0 | 0/0 | 0 | 0 | none |
| `work\1782676130-battleprobe-log-heal-pending-credit-success.txt` | 1/1 | 4/6 | 3/12 | 0/0 | 0/0 | 0/0 | 0/0 | 3 | 0 | 0/0 | 0 | 0 | none |
| `work\1782681848-battleprobe-log-magic-fire-custom-agrias-ramza-success.txt` | 1/2 | 6/6 | 1/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | none |
| `work\1782682260-battleprobe-log-before-preview-heal-negative-test.txt` | 1/2 | 6/6 | 1/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | none |
| `work\1782685331-battleprobe-log-heal-formula-preclamp-success-regen-caught.txt` | 1/1 | 6/57 | 1/33 | 4/1 | 0/0 | 0/0 | 28/52 | 52 | 0 | 0/0 | 0 | 0 | none |
| `work\1782693058-action-identity-live-observe-log.txt` | 6/10 | 2/18 | 4/21 | 6/3 | 0/0 | 0/0 | 4/14 | 15 | 0 | 0/0 | 0 | 0 | none |
| `work\1782694729-selector-baseline-log.txt` | 2/2 | 0/7 | 1/2 | 2/1 | 0/0 | 0/0 | 2/4 | 4 | 2 | 0/0 | 0 | 2 | none |
| `work\1782695389-reaction-nohp-selector-log.txt` | 2/5 | 0/14 | 5/16 | 7/2 | 0/0 | 0/0 | 8/12 | 12 | 3 | 1/1 | 0 | 3 | none |
| `work\1782698795-divine-ruination-shirahadori-not-applicable-log.txt` | 1/1 | 0/8 | 2/2 | 6/2 | 0/0 | 0/0 | 1/7 | 7 | 1 | 0/0 | 0 | 1 | none |
| `work\1782729990-first-strike-targetcache-register-log.txt` | 2/4 | 0/9 | 3/10 | 3/1 | 2/2 | 0/0 | 3/7 | 7 | 2 | 0/0 | 0 | 2 | none |
| `work\1782731913-first-strike-named-action-log-stabilized.txt` | 1/3 | 2/9 | 3/7 | 2/2 | 3/3 | 0/0 | 2/7 | 8 | 1 | 0/0 | 0 | 1 | none |
| `work\1782732167-pre-basic-first-strike-log-archive.txt` | 1/3 | 2/9 | 3/9 | 2/2 | 3/3 | 0/0 | 2/7 | 8 | 1 | 0/0 | 0 | 1 | none |
| `work\1782754715-counter-post-hit-ramza-thief-log.txt` | 2/2 | 0/12 | 2/5 | 8/2 | 2/2 | 0/0 | 2/10 | 10 | 2 | 0/0 | 0 | 2 | none |
| `work\1782755964-pre-multihit-counter-log-archive.txt` | 2/2 | 0/12 | 2/5 | 8/2 | 2/2 | 0/0 | 2/10 | 10 | 2 | 0/0 | 0 | 2 | none |
| `work\live-captures\battleprobe_log.equipment-readout-ramza-ninja.snapshot.txt` | 2/4 | 0/1 | 4/4 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | none |
| `work\live-captures\battleprobe_log.executing-action-resolver-probe-cross-slash-agrias-ninja.snapshot.txt` | 2/4 | 2/2 | 2/5 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | none |
| `work\1782675458-battleprobe-log-heal-strict-no-preview-poke-natural-result.txt` | 0/0 | 2/24 | 3/23 | 2/1 | 0/0 | 0/0 | 4/19 | 20 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782675887-battleprobe-log-before-heal-pending-credit-test.txt` | 0/0 | 2/24 | 3/23 | 2/1 | 0/0 | 0/0 | 4/19 | 20 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782677772-battleprobe-log-before-physical-custom-test.txt` | 0/0 | 1/2 | 2/5 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782678308-battleprobe-log-physical-custom-agrias-beowulf-success.txt` | 0/0 | 0/5 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782681592-battleprobe-log-before-magic-fire-custom-test.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782683501-battleprobe-log-natural-heal-preview-agrias-ramza-86hp.txt` | 0/0 | 0/2 | 2/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782683703-battleprobe-log-heal-preview-credit-poke-123-success.txt` | 0/0 | 0/2 | 2/7 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782683878-battleprobe-log-heal-preview-credit-poke-500-success.txt` | 0/0 | 0/2 | 2/8 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782684946-battleprobe-log-before-heal-formula-preclamp-test.txt` | 0/0 | 0/2 | 2/8 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782731756-first-strike-named-action-log.txt` | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\1782731817-first-strike-named-action-log-updated.txt` | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\battleprobe_log.hit-to-miss-v1-FAILED.20260626-225741.txt` | 0/0 | 0/1 | 4/5 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 3 | 0/1 | 0 | 0 | legacy/no actor probe |
| `work\battleprobe_log.hit-to-miss-v2-PASS.20260626-230526.txt` | 0/0 | 0/0 | 3/3 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 1 | 0/1 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-after-ramza-wait-ninja-reraise.snapshot.txt` | 0/0 | 2/3 | 3/10 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-baseline-cloud-active.snapshot.txt` | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-confirmed-before-cloud-wait.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-preview-cross-slash-agrias-187.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-preview-ramza-rush-ninja-50.snapshot.txt` | 0/0 | 2/2 | 2/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 2/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.action-boundary-resolved-ramza-rush-ninja-ko-before-wait.snapshot.txt` | 0/0 | 2/3 | 2/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.actor-dump-basic-agrias-beowulf.snapshot.txt` | 0/0 | 1/2 | 4/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.actor-dump-braver-agrias.snapshot.txt` | 0/0 | 1/1 | 3/4 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.executing-action-actor-dump-probe-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 4/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.executing-action-pointer-probe-full.20260624-194730.txt` | 0/0 | 2/2 | 5/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.executing-action-pointer-probe-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 5/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-after-ramza-wait-ninja-reraise.snapshot.txt` | 0/0 | 2/3 | 4/11 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-confirmed-before-cloud-wait.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-preview-cross-slash-agrias-187.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-preview-ramza-rush-ninja-50.snapshot.txt` | 0/0 | 2/2 | 2/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 2/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.immediate-ko-boundary-resolved-ramza-rush-ninja-ko-before-wait.snapshot.txt` | 0/0 | 2/3 | 2/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.instant-basic-7c-vanilla.snapshot.txt` | 0/0 | 0/1 | 0/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.instant-basic-memory-probe-agrias-beowulf.snapshot.txt` | 0/0 | 0/1 | 0/1 | 0/0 | 0/0 | 0/4 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.instant-basic-memory-probe-ninja-dual-agrias-lethal.snapshot.txt` | 0/0 | 0/4 | 1/1 | 0/0 | 0/0 | 0/8 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.instant-basic-memory-probe-ramza-beowulf-lethal.snapshot.txt` | 0/0 | 0/3 | 1/1 | 0/0 | 0/0 | 0/8 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-confirmed-before-cloud-wait.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-preview-cross-slash-agrias-187.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-preview-ramza-rush-ninja-50.snapshot.txt` | 0/0 | 2/2 | 2/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 2/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-resolved-ramza-rush-ninja-ko-followup.snapshot.txt` | 0/0 | 2/3 | 2/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.ko-pre-damage-resolved-ramza-rush-ninja-ko.snapshot.txt` | 0/0 | 2/3 | 2/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-context-dry-run-confirmed-before-cloud-wait.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-context-dry-run-preview-cross-slash-agrias-187.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-context-dry-run-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 2/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-primary-live-confirmed-before-cloud-wait.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-primary-live-preview-cross-slash-agrias-187.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-primary-live-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 2/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-tracker-live-confirmed-before-cloud-wait.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-tracker-live-preview-cross-slash-agrias-187.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pending-tracker-live-resolved-cross-slash-agrias-ninja.snapshot.txt` | 0/0 | 2/2 | 3/7 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-action-boundary-probe.20260623-162934.txt` | 0/0 | 2/3 | 4/11 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-immediate-action-ko-boundary-probe.20260623-154450.txt` | 0/0 | 2/3 | 2/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-immediate-basic-7b-deploy.20260623-222711.txt` | 0/0 | 0/2 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-immediate-basic-7c-deploy.20260623-223702.txt` | 0/0 | 0/1 | 0/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-instant-basic-memory-probe-deploy.20260623-225358.txt` | 0/0 | 0/1 | 0/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-instant-basic-memory-probe-regdump-deploy.20260623-225917.txt` | 0/0 | 0/1 | 0/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-instant-basic-repeat-deploy.20260623-231005.txt` | 0/0 | 0/1 | 0/1 | 0/0 | 0/0 | 0/4 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-ko-landmark-probe.20260623-173303.txt` | 0/0 | 2/3 | 4/11 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-ko-pre-damage-probe.20260623-135217.txt` | 0/0 | 2/2 | 2/8 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-pending-context-dry-run.20260623-130220.txt` | 0/0 | 2/2 | 2/8 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-pending-primary-deploy.20260623-112243.txt` | 0/0 | 2/2 | 3/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.pre-resolver-probe.20260624-205320.txt` | 0/0 | 1/2 | 4/6 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-candidates-confirmed-cross-slash-cloud-active.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-candidates-pending-agrias.snapshot.txt` | 0/0 | 0/0 | 2/2 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-candidates-pending-beowulf.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-candidates-pending-ninja.snapshot.txt` | 0/0 | 0/0 | 3/3 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-candidates-preview-cross-slash-agrias-115.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-candidates-resolved-cross-slash-aoe.snapshot.txt` | 0/0 | 6/6 | 5/8 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-eager-immediate-ninja-agrias-999.snapshot.txt` | 0/0 | 0/2 | 2/2 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-eager-immediate-ninja-agrias-success-87x2.snapshot.txt` | 0/0 | 0/5 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-confirmed-cross-slash-cloud-active.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-immediate-basic-7b-agrias-beowulf.snapshot.txt` | 0/0 | 0/1 | 0/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-immediate-basic-agrias-beowulf.snapshot.txt` | 0/0 | 0/2 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-immediate-ninja-dual-agrias-failed-180x2.snapshot.txt` | 0/0 | 0/1 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-lethal-braver-success.snapshot.txt` | 0/0 | 3/3 | 3/3 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-pending-cross-slash-active-agrias.snapshot.txt` | 0/0 | 0/0 | 2/2 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-pending-cross-slash-active-beowulf.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-pending-cross-slash-active-ninja.snapshot.txt` | 0/0 | 0/0 | 3/3 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-post-cross-slash-success.snapshot.txt` | 0/0 | 6/6 | 5/9 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp-plan-preview-cross-slash-agrias-115.snapshot.txt` | 0/0 | 0/0 | 1/1 | 0/0 | 0/0 | 0/0 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |
| `work\live-captures\battleprobe_log.preclamp115-resolved-cross-slash-agrias-ko.snapshot.txt` | 0/0 | 2/2 | 4/6 | 0/0 | 0/0 | 0/32 | 0/0 | 0 | 0 | 0/0 | 0 | 0 | legacy/no actor probe |

## Offline Conclusion

- Existing logs can validate the parser/tooling and several action-id surfaces.
- Existing logs are not enough to retire the live probe: they do not cover every required class under one observe-only profile.
- Missing or weak coverage remains for counters/reactions, multiple simultaneous pending actions, and cross-battle actor-array stability.
- The prepared live probe should therefore be run before promoting actor context to the runtime primary resolver.
