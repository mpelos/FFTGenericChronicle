# CT Delay And Interrupt Model Schema V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/09-accuracy-evasion-model-schema.md`
- `docs/job-balance/10-healing-attrition-model-schema.md`
- `work/t5-ct-delay-scenarios-v0.json`

## Purpose

This document starts T5, the CT/delay/overwatch/interrupt validation track.

T5 exists to validate skills that can alter:

- turn timing;
- delayed action resolution;
- target movement before delayed resolution;
- overwatch and interrupt windows;
- short-lived speed/CT reactions;
- delayed-action predictability.

The immediate consumers are:

- Archer `Aimed Shot`, `Covering Shot`, `Pinning Shot`, and `Speed Save`;
- Squire `Rally` if it changes CT;
- the later T3xT5 composition for item/spell/reaction reliability.

## Source Notes

T5.0 uses the Final Fantasy Tactics Battle Mechanics Guide by AeroStar as the timing baseline:

- CT reaches an active turn at `100`;
- each clocktick increases a unit's CT by its Speed;
- after an active turn, CT is reduced by action/move choice;
- slow actions use CTR, related to spell speed by round-up of `100 / spell_speed`;
- fast actions resolve immediately.

Reference URL:

```text
https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
```

T5.0 uses the accepted Generic Chronicle phase Speed bands from `work/sim-inputs-v0.2.1.json`, not
the missing Windows weapon baseline. T1 is therefore not required for T5.0.

## Pinned Bundle

Pinned input bundle:

```text
work/t5-ct-delay-scenarios-v0.json
```

The bundle defines timing formulas, scenario rows, and expected values for the first
dual-independent T5 run.

## Formula Contract

Turn readiness:

```text
ticks_to_turn = 0 if current_ct >= 100
ticks_to_turn = ceil((100 - current_ct) / speed) otherwise
```

Post-turn CT:

```text
move_and_act: new_ct = current_ct - 100
move_only:    new_ct = current_ct - 80
act_only:     new_ct = current_ct - 80
wait:         new_ct = current_ct - 60
```

Slow-action CTR:

```text
ctr = ceil(100 / spell_speed)
ticks_to_resolution = ctr
```

Jump-like timing:

```text
jump_ticks = ceil(50 / speed)
```

Delayed target safety:

```text
target_safe = ticks_to_resolution < target_ticks_to_turn
target_can_act_before_resolution = not target_safe
```

T5.0 intentionally uses the conservative rule that a tie is unsafe for panel-targeted delayed
actions. A later gameflow-accurate model can replace this if the exact phase order is needed.

Overwatch/interrupt:

```text
interrupts_before_resolution = interrupt_tick < ticks_to_resolution
```

Predictability:

```text
whiff_window_ticks = max(0, ticks_to_resolution - target_ticks_to_turn)
predictability = safe if target_safe else unsafe
```

`whiff_window_ticks` measures lateness magnitude. It does not decide safe versus unsafe by itself,
because a same-tick tie has `whiff_window_ticks = 0` but is still unsafe under the T5.0 conservative
same-tick policy.

## Scenario Set

The first bundle includes arithmetic rows for:

- tick-to-turn from Speed and current CT;
- post-turn CT reduction;
- spell speed to CTR;
- delayed action safe and unsafe target windows;
- same-tick conservative unsafe behavior;
- overwatch/interrupt before resolution;
- short-lived speed increase effect on next turn timing;
- Jump-like timing.

These rows validate timing machinery. They do not yet balance final Archer or Time Mage values.

## Expected Counter Output

GPT and Claude T5 counters should produce:

- one row per scenario;
- `scenario_id`;
- `model`;
- calculated timing fields relevant to that model;
- validation errors, if any.

The dual-independent gate from document 07 applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T5 output can be used to accept or reject skill values.

## What T5.0 Does Not Decide

Still open for later T5 versions:

- exact Aimed Shot CTR values;
- exact Covering Shot/overwatch trigger conditions;
- whether Speed Save remains on Archer;
- whether Pinning Shot changes CT, movement, or status;
- full battlefield turn-order queue with many units;
- exact phase-order behavior for same-tick resolution versus target active turn;
- interaction with Haste, Slow, Stop, Quick, or Short Charge-like effects.

## Claude Review Request

Claude should review whether:

- T5.0 can proceed without T1/Windows baseline;
- the timing formulas are specific enough for independent implementation;
- the conservative same-tick unsafe rule is acceptable for T5.0;
- the scenario rows cover the first Archer tempo needs;
- any additional row is required before implementing counters.

Claude review verdict: Accepted (claude-opus-4-8, 2026-06-20).

Acceptance notes:

- Claude verified the pinned bundle has no duplicate object keys.
- Claude's independent counter produced 14 rows and 0 mismatches.
- GPT's canonical counter must also produce 14 rows and 0 mismatches before this track is used by
  later skill-value proposals.
