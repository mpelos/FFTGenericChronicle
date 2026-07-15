# LT31 DCL Reaction effect-boundary live plan

## Purpose

Correlate one accepted native Reaction queue commit with the executed actor at state `0x2C`, after
the VM execution workers and before cleanup. This decides whether the state-`0x2C` boundary can own
managed synthetic effects and cadence commits.

## Safety

- Use `work/1784001361-battle-runtime-settings.lt31-dcl-reaction-effect-boundary.json`.
- Both probes are observe-only. No producer, replacement, retarget, effect, status, or cadence control
  is enabled.
- Stop if either hook reports an expected-byte or assembler failure.

## Route

1. Select the Reloaded profile and choose Enhanced.
2. Press Enter during the intro to skip it.
3. Choose Load, Manual Saves, and the first entry, save `05`.
4. Enter the prepared battle.
5. Trigger one visible native Reaction whose identity is known from the unit setup.
6. Perform one comparable attack that does not trigger a Reaction as a negative control.
7. Capture the bounded log and close the game through the runbook's safe-close route.

## Required correlation

For the visible Reaction:

- exactly one `[DCL-REACTION-COMMIT]` row identifies its queue pass, actor pointer/index,
  `reactionId`, `actor18C`, `actor142`, source, and targets;
- exactly one later `[DCL-REACTION-EFFECT]` row has state `0x2C`, the same actor pointer/index,
  matching presentation/action ids, and the same target list;
- the effect row occurs after the accepted commit and before any later reuse of that actor;
- the non-Reaction control produces no matching Reaction effect row.

## Decisions

- One commit to one matching effect row: state `0x2C` becomes the effect/cadence owner candidate.
- Multiple effect rows for one actor: add an idempotence token before any managed write.
- No effect row: inspect whether this reaction family uses another queue pass/state pipeline.
- Changed actor or target identity: the boundary is cleanup/presentation-adjacent but cannot directly
  own the original accepted-Reaction transaction without a retained commit token.

## Boundary

Passing LT31 does not authorize Hex Ward production or effect writes by itself. LT23/LT28 must also
prove pass ownership and execution-only producer provenance. The first managed-effect vertical must
remain exact-carrier, source-targeted, immunity-aware, and bounded to one write.
