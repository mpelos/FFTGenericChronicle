# DCL status post-calc producer checkpoint

## Scope

This checkpoint closes the offline implementation pass that began with LT28 calc-provenance evidence and prepares LT32 as the next focused live gate.

## LT28 evidence consolidated

- Raw capture: `work/1784070150-lt28-dcl-calc-provenance-live.log`.
- Result: `work/1784070430-lt28-dcl-calc-provenance-live-result.md`.
- Analyzer output: `work/1784070400-dcl-calc-provenance-analysis.md`.
- Formula calculation has three observed direct origins:
  - outer action sweep returns at RVA `0x281F12`;
  - nested Rend calculation returns at RVA `0x307ED5`;
  - forecast calculation returns at RVA `0xEF53F14`.
- Forecast Focus produced battle state `0x19`; confirmed Focus execution produced battle state `0x2A` and outer return RVA `0x281F12`.
- The forecast pointer is not a sufficient execution classifier.

## Implemented offline

- Calc-origin classification and confirmed-execution gating in `DclActionContextCache`.
- Preservation of the parent action context across nested Rend calculations.
- One prepared status decision per target in `DclStatusPlanCache`, so the post-calc producer and pre-clamp consumer share one 3d6 roll.
- A catalog-derived post-calc producer for all 82 conditional status actions using formulas `0x0A`, `0x0B`, `0x29`, `0x2A`, `0x33`, `0x3D`, `0x3F`, `0x40`, `0x41`, `0x50`, `0x51`, and `0x5A`.
- Exact native status-bit mapping for the 30 DCL status tokens used by the catalog.
- Runtime policy `replaced-post-calc`, accepted only when the catalog proves a complete packet, matching add/remove operation, and compatible contest mode/group.
- A single composed hook at RVA `0x281F12` stages the native packet after confirmed execution and avoids same-site hook conflicts.
- Forecast and non-confirmed contexts fail closed; a cached DCL miss suppresses the native conditional packet.
- New analyzer `tools/analyze_dcl_status_conditional_producer.py` is part of `codemod/run-offline-checks.ps1`.

## Offline validation

- Main C# build: 0 warnings, 0 errors.
- Formula/runtime smoke tests passed, including forecast-vs-execution gating, nested Rend preservation, single-roll status-plan reuse, and complete/incomplete producer validation.
- Conditional producer analyzer passed for all 82 catalog actions and all 12 supported formulas.
- LT32 runtime settings validation passed with 0 errors.
- Full `codemod/run-offline-checks.ps1` suite passed in 43.6 seconds.

## Next live gate

- Profile: `work/1784071391-battle-runtime-settings.lt32-dcl-status-postcalc-producer.json`.
- Plan: `work/1784071392-lt32-dcl-status-postcalc-producer-live-plan.md`.
- Primary action: Wall, ability 13, with Protect + Shell as an all-or-nothing packet, forced roll 18, and resistance 0.
- The gate must prove that forecast does not stage the packet, confirmed execution stages it once, the packet reaches the target, and the pre-clamp consumer reuses the prepared decision.
- Additional live evidence still needed later: AI-origin classification, a nested Rend execution capture, and the four special transactions (Nameless Song, Forbidden Dance, Celestial Void, Corporeal Void).

## Installed environment restored

- Stable profile restored: `work/1783992200-battle-runtime-settings.lt23-dcl-reaction-commit.json`.
- Installed profile is byte-identical to the source.
- Source/installed SHA-256: `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`.
- Installed code-mod DLL SHA-256: `9A225141E3D115531941092B74E8E3FA1B5D7A616C3525180F41B052FF3DE8E9`.
- The remaining `Reloaded-II` process record reports `HasExited=True`, zero threads, and zero handles; it cannot retain the deployed files.

## LT32 launch attempt

- The LT32 profile deployed successfully and startup logged `[POST-CALC-HOOK] rva=0x281F12 ... statusProducer=1` together with the managed pre-clamp and calc hooks.
- Two clean Reloaded-II launches reached the known black screen with the feather cursor before any usable game menu. The second launch followed an `Alt+F4` close and fresh official `--launch` request.
- No battle or action occurred, so this capture neither validates nor refutes the producer.
- Both game processes were closed with `Alt+F4` without saving, and LT23 was restored afterward.
- Continue offline work while this launch anomaly is present; repeat LT32 only after the Enhanced menu reaches save 05 normally.

## Performance-status closure

- Nameless Song `91` and Forbidden Dance `98` now reuse the execution-only post-calc producer.
- Their formula `0x1C/0x1D` catalog entries prove complete five/eight-member `Random` packets; validation requires every native bit in one `random-one` group.
- The callback preserves the handlers' native caster-Sleep eligibility gate and refuses to stage a packet while `caster.effective[4] & 0x10` is set.
- Smoke tests cover exact Nameless Song packet ownership, settings validation, and awake/asleep eligibility behavior.
- Updated status authority has 82 conditional producers, two performance producers, and only two remaining special transactions: the RandomFire riders on Celestial Void and Corporeal Void.
- LT33 profile/plan: `work/1784072793-battle-runtime-settings.lt33-dcl-performance-status-producer.json` and `work/1784072794-lt33-dcl-performance-status-producer-live-plan.md`.
- Updated reports: `work/1784072795-dcl-status-postcalc-producer-analysis.md`, `work/1784072796-dcl-status-authority.md`, and `work/1784072797-dcl-performance-state-analysis.md`.
- The full offline suite passed after this change in 43.4 seconds. The updated DLL was deployed with the stable LT23 settings, which remain byte-identical to their source.
