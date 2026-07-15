# LT29 DCL reaction-producer log-only plan

## Purpose

Validate that the pass-2 pre-selector boundary can identify one active, empty reaction-candidate
slot and express a bounded `would-stage` decision without changing battle memory. This is an
installation/context gate for blank carrier Hex Ward `443`, not a behavior test.

## Prerequisites

- Complete LT23 reaction-commit ownership and LT28 calc-provenance first.
- Use `work/1784000422-battle-runtime-settings.lt29-dcl-reaction-producer-logonly.json`.
- Confirm the startup line reports `producer=log-only`.
- Keep `DclReactionProducerLogOnly=true`; this plan does not authorize a live write.

## Route

1. Select the Reloaded profile and choose Enhanced.
2. Press Enter during the intro to skip it.
3. Choose Load, then Manual Saves, then the first entry, save `05`.
4. Enter the prepared battle and perform one ordinary targeted action so the reaction queue runs.
5. Capture the bounded log and close the game using the runbook's normal safe-close route.

## Required evidence

- `[DCL-REACTION-PRESELECT-HOOK] ... producer=log-only` installs with the expected-byte guard.
- At least one `[DCL-REACTION-PRESELECT]` event records native candidate words and the source/actor
  context.
- If battle-unit index `0` is active and its candidate word is empty, the event reports
  `producer=would-stage:unit=0:carrier=443`.
- No producer-caused `[DCL-REACTION-COMMIT] ... reactionId=443` occurs.
- Repeated snapshots keep index `0` empty unless the native game independently stages a reaction;
  visible battle behavior is unchanged.

## Interpretation

- `would-stage` proves the fixed-index active/empty guard and pre-selector timing only.
- `producer=none` with index `0` inactive means the profile needs a known active unit index; it is
  not evidence against the boundary.
- Any id `443` commit or behavior change is a failure because LT29 is log-only.
- A later one-write live profile is permitted only after LT23 proves pass-2 ownership and the target
  unit index is known from the same save/battle.
