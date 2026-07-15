# LT31 Counter effect multiplicity checkpoint

## Question

Does one accepted native Reaction commit map 1:1 to the state-`0x2C` effect boundary, and are the
commit-time executable id and target list final?

## Controls

- The LT31 profile enables only the three guarded commit hooks and the observe-only state-`0x2C`
  effect hook.
- The audited Rion Counter autosave loads directly through the atomic Enhanced/Continue route.
- `work/1784106834-lt31-counter-no-trigger-control-live.log` captures a 97-Brave Counter roll that
  did not trigger; it contains no false native Reaction commit.
- The successful capture is
  `work/1784107234-lt31-counter-effect-multiplicity-live.log`, SHA-256
  `FF7CF8E3C0FE77B6ECB13226761D74FFDFF14738F2C1D3F5E4829AB58940CBB0`.

## Successful correlations

Rion's first selected Counter transaction is:

- commit event 8: pass 2, actor `0x140D31558`, reactor 4, source 3, presentation/executable
  `442/442`, target `[3]`;
- effect events 11 and 12: the same actor/reactor/source and presentation `442`, executable Basic
  Attack `0`, final target `[3]`.

Rion's second selected Counter transaction is the target-lifetime control:

- commit event 10: pass 2, actor `0x140D31558`, reactor 4, source 0, presentation/executable
  `442/442`, stale target `[3]`;
- effect events 14 and 15: the same actor/reactor/source and presentation `442`, executable Basic
  Attack `0`, final target `[0]`.

Both automated analyses pass:

- `work/1784107652-lt31-rion-counter-source3-effect-analysis.md`;
- `work/1784107653-lt31-rion-counter-stale-commit-target-analysis.md`.

## Conclusions

1. Pass 2 owns accepted-Reaction cardinality: one native Counter produces one commit.
2. State `0x2C` owns delivered execution/strike cardinality: Ninja Dual Wield produces two effect
   rows for that one commit.
3. Native carrier delivery converts presentation Reaction `442` into executable Basic Attack `0`.
4. The commit-time target list is not final. It can be empty or stale and is replaced downstream
   with the incoming source target.
5. Commit-time action replacement and retarget mutation are not final mechanisms. Both live modes
   are retired/fail-closed; their log-only diagnostics remain available.
6. Once-per-Reaction cadence belongs at pass-2 commit. Per-strike effects can observe state `0x2C`.
   A new post-materialization/pre-execution boundary is required for executable replacement and
   final retargeting.

The next offline investigation is the carrier-specific code between `0x206421` and state `0x29` VM
execution, looking for a shared point after typed-order/target materialization and before execution.
