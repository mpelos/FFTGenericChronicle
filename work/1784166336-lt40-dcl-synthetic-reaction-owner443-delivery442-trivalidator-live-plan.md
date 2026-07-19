# LT40 synthetic-Reaction tri-validator live plan

## Question

Does Counter `442` reject a distant synthetic delivery at its actual shared typed-family result RVA
`0x283019`, then accept an adjacent delivery through `0x283019` and final RVA `0x28315C`?

## Bounded setup

- Profile: `work/1784166334-battle-runtime-settings.synthetic-reaction-owner443-delivery442-trivalidator-live.json`.
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
- Isolated Reloaded profile: mod loader plus Generic Chronicle Battle Probe only.
- Exactly two candidate writes maximum; no Reaction order rewrite and no managed Reaction effect.
- Hit chance is deterministically `100` with fixed roll `0`; native equipment/class evade inputs are
  zeroed through the already validated hit-control baseline.
- DCL damage is fixed to `1` only for this probe, keeping Rion alive through both incoming actions.
- Reproduce **Auto-battle > Attack Enemy > Wenyld**. Wenyld acts from range; the adjacent Chocobo
  follows with Choco Beak.

## Required chain

1. Three hooks install: typed-family `0x283019`, typed-Bonecrusher `0x283148`, and final `0x28315C`.
2. Wenyld source `6` hits, survives the one-point control, arms, and stages the first `442` for Rion
   table index `16`.
3. Counter's typed-family row for source `6` is nonzero and transitions private mailbox `2->6`.
   No final row, materialization, native commit, managed commit, or `442` effect belongs to source
   `6`.
4. Choco Beak from source `0` hits for one point, arms a distinct request, and performs the
   second/final candidate write.
5. Counter's typed-family and final rows for source `0` both return zero with mailbox `2->2`.
6. One owned materialization reports Reaction `442`, reactor `16`, source `0`, action `1/0`, target
   mode `5`, and target `0`.
7. Exactly one agreeing pass-2 commit and one managed cadence commit occur.
8. Exactly two Dual Wield state-`0x2C` rows retain presentation `442`, action `0`, and target `0`.

## Stop and validation

Close without saving after the second matching `442` effect. Stop early on any hook guard failure,
wrong source, missing `2->6`, a final row for rejected source `6`, nonzero adjacent result, duplicate
materialization/commit, effect count other than two, self-targeting, or any third candidate write.
Archive the log, run validator-chain, synthetic, materialization, and effect analyzers, close
Reloaded-II, and restore all six external artifacts before interpretation.
