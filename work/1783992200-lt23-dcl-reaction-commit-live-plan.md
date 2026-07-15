# LT23 — accepted-reaction action-queue commit probe

## Purpose

Confirm which of the three Strong static queue boundaries (`0x2066AE` pass 0, `0x206743` pass 1,
`0x206421` pass 2) fires once when the engine commits an accepted Reaction to execution. This test
is observe-only: it does not change chance, damage, cadence, actor fields, or any other game state.

## Inputs

- Settings: `work/1783992200-battle-runtime-settings.lt23-dcl-reaction-commit.json`
- Static audit: latest `work/*-dcl-reaction-commit-analysis.md`
- Runtime anchor audit: latest `work/*-runtime-hook-anchor-audit.md`
- Game route: Enhanced → Start Game → `Enter` to skip intro → Load → Manual Saves → first row, save 05

## Preflight

1. Require all static reaction-commit checks to pass.
2. Require the current executable's `reaction-commit-p0/p1/p2` AOB anchors to pass.
3. Validate the LT23 settings file and build the code mod in Release with zero warnings/errors.
4. Isolate the Reloaded-II profile to the mod loader and Generic Chronicle Battle Probe.
5. Deploy the Release build and LT23 settings only after the four checks above pass.
6. Require three `[DCL-REACTION-COMMIT-HOOK]` rows tagged `pass=0`, `pass=1`, and `pass=2` before entering battle.

## Capture sequence

1. Load save 05 and enter a battle containing a unit with a known native Reaction. Prefer Counter
   (`442`) for the first event because its visible follow-up action makes event matching unambiguous.
2. Open and cancel the forecast for the provoking action three times without executing it.
   There must be no `[DCL-REACTION-COMMIT]` event from forecast-only evaluation.
3. Execute the provoking action. If the Reaction does not trigger, repeat ordinary turns without
   changing any chance setting until one visible trigger occurs.
4. Match each visible queued Reaction to the bounded log. Record event number, reaction id, reactor
   pass, index, source index, `idsAgree`, record pointer, target count/list, and whether the event preceded
   the visible reaction execution.
5. If available in save 05, repeat with one non-counter family such as Auto-Potion (`441`) or Mana
   Shield (`445`). An unavailable family is recorded as untested, never silently substituted.
6. Execute one action against a target known not to have a compatible Reaction. It must not emit a
   reaction-commit event.
7. Close the game through Options → Exit Game, or use `Alt+F4` after logs/screenshots are retained.

## Pass gate

- Forecast-only evaluation emits zero commit events.
- Every captured event has `idsAgree=True` and a native Reaction id in `422..453`.
- Every captured event has a valid queue tag `pass=0/1/2`; record which family owns which pass.
- A visible queued Reaction has exactly one matching event with the correct reactor/source direction.
- The staged target list names the visible reaction target; Counter should target the original source.
- An action with no compatible accepted Reaction emits no event.
- At least Counter and one distinct family pass before the boundary is promoted to **Proven** for
  general cadence consumption. Counter alone proves only the Counter path.

## Hard fail / interpretation stop

- Hook AOB mismatch, hook-install failure, crash, or changed battle behavior.
- Duplicate events for one visible queued Reaction.
- Event during forecast cancellation.
- Any disagreement among `reactionId`, `actor18C`, and `actor142`.
- Reversed or invalid reactor/source indices.

On a hard fail, retain the full bounded log and do not enable cadence mutation or reaction
replacement at this boundary.

## Evidence

Copy the runtime log and screenshots into new timestamp-prefixed `work/` files. Record the exact
game executable hash, Reloaded profile, observed reaction family, expected/actual event count, and
pass/refutation conclusion.
