# DCL calc-provenance checkpoint

## Offline result

`tools/analyze_dcl_calc_provenance.py` verifies the current executable and generated
`work/1783997368-dcl-calc-provenance-analysis.md`; every anchor and call-graph check passes.

Strong facts:

- `computeActionResult` begins at RVA `0x3099AC` and has exactly two real-code direct callers.
- RVA `0x281F0D` is the ordinary affected-target sweep call; the sweep entry at `0x281CE8` is
  VM-invoked and has no real-code direct caller.
- RVA `0x307ED0` is a nested call from formula handler `0x25` at `0x307E70`.
- Formula `0x25` belongs to Rend Helm `138`, Rend Armor `139`, Rend Shield `140`, and Rend Weapon
  `141`. The handler saves the outer type/id, writes Attack `(type=1,id=0)`, re-enters calc for the
  same target, then restores the outer record.
- A latest-per-target cache that records every entry can replace the outer Rend identity with that
  synthetic inner Attack identity.
- The UI forecast pointer is published only after forecast computation returns, so it cannot
  classify the calc phase synchronously at entry.
- State `0x15` apply, pre-clamp `0x30A5D7`, and result selector `0x205210` are execution-only
  downstream authorities. They are safe commit/consumption points but cannot classify preview,
  charge polling, or AI scoring at the earlier calc entry.

The current-build address migration is also applied across `docs/modding/`: calc entry `0x3099AC`,
sweep `0x281CE8/0x281F0D`, staged-bundle post-call `0x281F12`, apply entry `0x30A484`, and pre-clamp
`0x30A5D7`.

## LT28 implementation

The calc ring now has 48-byte slots. When `DclCalcProvenanceProbeEnabled=true`, the existing
calc-entry hook additionally captures, at fire time:

- return address / caller RVA;
- shared battle state;
- turn-owner index;
- reaction-source index;
- UI forecast pointer;
- the existing caster/type/ability/target tuple.

The poller emits bounded `[DCL-CALC-PROVENANCE]` rows and classifies the two known return RVAs as
`outer-sweep` and `nested-rend-attack`. The feature is observe-only and does not change the action
cache, hit-decision cache, native order record, formulas, outcomes, or battle state.

Validation completed:

- codemod build: zero warnings/errors;
- smoketests: pass;
- LT28 settings validation: zero errors, one expected observe-only warning;
- runtime hook anchor audit: PASS `25/25` in
  `work/1783997661-runtime-hook-anchor-audit.md`;
- launch-only hook smoke: pass in
  `work/1783997763-lt28-dcl-calc-provenance-hook-launch-check.md`;
- installed/source DLLs match SHA-256
  `185FCE08DB7344FF893434B88118A61732A0D95CEC00B1457F25CCA1B1F2D1E6`;
- LT23 is restored byte-for-byte and the game is closed.

## Remaining live gate

Run `work/1783997612-lt28-dcl-calc-provenance-live-plan.md`. The smallest useful coverage is one
forecast-only Attack, one confirmed Attack, one AI turn, and one Rend action if the loaded party has
it. Do not suppress or separately consume the nested entry until the Rend execution establishes
whether native fallback damage expects the outer decision, an inner decision, or both.
