# LT39 DCL Reaction materialization live plan

## Purpose

Correlate one accepted native Counter order at RVA `0x2063BD` with its pass-2 commit and delivered
state-`0x2C` effects. The test decides whether the complete order at the accepted post-selector,
pre-actor boundary is the authoritative action/target input consumed downstream.

## Offline prerequisite

- `tools/analyze_dcl_reaction_materialization.py` passes every byte and call-graph anchor against the
  current Enhanced executable.
- `tools/test_dcl_reaction_materialization.py` and
  `tools/test_dcl_reaction_materialization_live.py` pass.
- The code mod builds with zero warnings/errors and its smoke tests pass.

## Safety

- Use `work/1784108723-battle-runtime-settings.lt39-dcl-reaction-materialization.json`.
- All three probes are observe-only. No producer, action replacement, retarget, status/effect write,
  or cadence mutation is enabled.
- Stop if the materialization hook reports an expected-byte or assembler failure.
- Restore the exact DLL, runtime settings, Reloaded AppConfig, autosave, and log after capture.

## Fixture and route

1. Restore `work/1784104894-fft-autoenhanced-snapshot.png` with the autosave helper while the game is
   closed.
2. Launch through Reloaded-II, choose Enhanced, and use the atomic intro-skip/Continue sequence from
   the control runbook.
3. Use Rion's visible Ninja Counter fixture. Trigger one Counter; retry a clean restored run if the
   Brave roll fails.
4. Capture the bounded log and close the game.

## Required correlation

- exactly one `[DCL-REACTION-MATERIALIZED]` row for Rion's accepted Counter `442`;
- `reactorIdx`/`sourceIdx` agree with the later pass-2 commit and state-`0x2C` rows;
- the materialized order has `casterIdx=reactor`, `actionType=1`, `actionId=0`, `targetMode=5`, and
  `targetIdx=source`;
- exactly one accepted pass-2 commit exists for that Reaction;
- the Ninja Counter produces two effect rows, both executable action `0` and target `[source]`;
- the materialized row precedes commit/effects in the log.

## Decision

Passing LT39 promotes `0x2063BD` from Proven-static to Proven-live action/target ownership. The next
write vertical may retarget only one exact custom carrier by rewriting the complete order target index
and coordinates at this boundary, with a one-write cap and commit/effect correlation. Failure keeps
all order-level controls disabled and returns the investigation to selector/actor data flow.
