# LT30 DCL reaction-retarget log-only plan

## Purpose

Validate that a pass-2 commit for blank carrier Hex Ward `443` still owns the incoming source index
and actor target list at the guarded commit boundary. LT30 reports retarget intent only; it does not
write the target list or deliver an effect.

## Prerequisites

- LT23 establishes pass-2 ownership and source/actor/list lifetime.
- LT28 establishes execution provenance for the producer decision.
- A separately approved one-write producer profile has already created one accepted `443` commit.
- Use `work/1784000915-battle-runtime-settings.lt30-dcl-reaction-retarget-logonly.json`.
- Keep `DclReactionRetargetLogOnly=true`.

## Required evidence

- All three commit hooks install and pass 2 reports `retarget=armed/log-only`.
- The accepted `443` row has matching `reactionId`, `actor18C`, and `actor142`.
- `sourceIdx` is within `0..20`, the native target list has at least one entry, and the row reports
  `retarget=would-write:<sourceIdx>`.
- No visible target or battle behavior changes.

## Interpretation

- A valid `would-write` row proves the exact-id/source/list context for retargeting, not effect
  delivery.
- A missing `443` commit is producer failure or wrong queue ownership, not retarget failure.
- A source outside `0..20` or empty target list blocks live retargeting.
- The later live vertical must keep the carrier exact, the write cap at one, and pair target rewrite
  with a managed Blind/Brave effect only after its own effect-commit boundary is proven.
