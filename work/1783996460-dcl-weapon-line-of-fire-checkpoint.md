# DCL weapon line-of-fire checkpoint

## Static result

- The native weapon-target evaluator begins at RVA `0x280170`.
- It reads `ITEM_WEAPON_DATA.AttackFlags` from the selected weapon-data byte `+1`.
- `Arc=0x10` dispatches to `0x3129DC`; `Direct=0x20` dispatches to `0x312ECC`;
  `Lunging=0x40` dispatches to `0x2804D8`.
- Stock guns and crossbows are Direct, bows are Arc, and poles are Lunging.
- Arc/Direct inputs are acting unit index, staged coordinate pointer, and candidate unit index.
- Arc/Direct return the unit actually reached/intercepted or a negative failure. Native inclusion
  requires nonnegative `candidate == resolverResult`.
- The Direct resolver enters protected VM code. Arc is real code. Lunging is a cardinal recursive
  terrain check and is not generic projectile LoS.

All anchors, family flags, and direct-caller checks pass in
`work/1783996316-dcl-weapon-line-of-fire-analysis.md`.

## DCL consequence

Coordinate distance is no longer the best available proxy: the exact native target-inclusion
contract is known. Countershot and a Direct/Arc-authored Rod bolt should preserve the resolver's
returned-index equality. The current `ConditionFormula` surface does not yet receive that verdict.

The calc-entry cache already keys action identity by exact target index and matching source, so it
proves the reactor participated in that action evaluation. It still lacks an execution-provenance
token separating forecast/AI evaluation from the resolving action.

## Next offline work

1. Find the safest synchronous observation point around Arc/Direct return values without calling
   either resolver from the poller.
2. Correlate that target verdict with the existing calc-entry target/source/action tuple.
3. Decide whether native reaction-delivered basic weapon orders automatically traverse the same
   evaluator; prepare an observe-only proof before adding any LoS filter.
4. Continue mapping action provenance for reaction, damage-over-time, field, and collateral AoE.
