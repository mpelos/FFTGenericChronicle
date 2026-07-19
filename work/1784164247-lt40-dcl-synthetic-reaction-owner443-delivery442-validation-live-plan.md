# LT40 synthetic-Reaction native-validation follow-up plan

> Superseded: this plan attached Counter's typed result to the Bonecrusher-only RVA `0x283148`.
> The replacement tri-validator plan is
> `work/1784166336-lt40-dcl-synthetic-reaction-owner443-delivery442-trivalidator-live-plan.md`.

## Question

Can the native `442` delivery classifier distinguish a distant typed-helper rejection and then
allow a second, adjacent source to pass both validators, materialize, commit, and execute exactly
one Reaction transaction for owner `443`?

## Bounded setup

- Profile: `work/1784164245-battle-runtime-settings.synthetic-reaction-owner443-delivery442-validation-live.json`.
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
- Isolated Reloaded profile: mod loader plus Generic Chronicle Battle Probe only.
- Exactly two candidate writes maximum; no order rewrite and no managed effect.
- Reproduce the verified **Auto-battle > Attack Enemy > Wenyld** sequence. Wenyld attacks first from
  range; the adjacent Chocobo then uses Choco Beak.

## Required chain

1. Both delivery-validation hooks install: typed `0x283148` and final `0x28315C`.
2. Wenyld source `6` arms and stages the first `442` request for Rion table index `16`.
3. The typed validation row for source `6` is nonzero and transitions private mailbox `2->6`.
   No materialization, native commit, managed commit, or `442` effect may belong to source `6`.
4. The Chocobo action arms a new owner-`443` request and performs the second/final candidate write.
5. For that adjacent source, typed and final results are both zero with mailbox `2->2`.
6. One materialization row reports Reaction `442`, reactor `16`, the same adjacent source, native
   action type `1`, payload `0`, target mode `5`, target equal to source, and
   `syntheticDelivery=owned`.
7. One agreeing pass-2 `442` commit consumes owner-`443` cadence with
   `ownership=materialized-delivery-owned`.
8. The one accepted Reaction transaction emits the native per-strike state `0x2C` rows. Rion is a
   Dual Wield Ninja, so this fixture expects exactly two rows; both report presentation `442`,
   action `0`, and target equal to the adjacent source. Two effect rows are not two Reaction
   commits.

## Immediate stop conditions

Close without saving after the second matching state-`0x2C` row. Also stop on missing hook guards,
wrong rejection source, rejection without `2->6`, second write before a distinct action, nonzero
adjacent result, missing ownership, duplicate materialization or commit, an effect count other than
the expected two Dual Wield rows, self-target delivery, or any third write. Archive the full log,
run the validation, synthetic, materialization, and effect analyzers with expected effect count `2`,
then close Reloaded-II and restore all six external artifacts before interpretation.
