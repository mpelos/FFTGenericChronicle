# LT40 synthetic-Reaction rewrite-owned live gate

## Proven input from the first write gate

Carrier `443` was staged once and accepted by pass 2. Its unmodified accepted order was:

- `reactorIdx=16`;
- `actionType=0`, `actionId=443`;
- `targetMode=5`, `targetIdx=16` (the carrier owner);
- state-`0x2C` effect `actionId=443`, `targets=[16]`.

The exact `1/0` original guard rejected this order as `blocked-original`; there was no accepted-order
write and no managed cadence commit. That negative control is
`work/1784159016-lt40-dcl-synthetic-reaction-live-write-blocked-original.log`.

The same capture proved that the pass-2 commit actor is not a battle-unit pointer. Its captured
`actor+0x148` record is the exact carrier-owner battle-unit pointer. The runtime now uses that record
and requires a native state handshake set only after a successful accepted-order write.

## Purpose

Prove one complete generic transaction:

1. one survived successful incoming hit reserves Rion (`id=0x80`, table index `16`);
2. the pre-selector stages `443` once;
3. the accepted order must originally be exactly `0/443`;
4. the guarded controller replaces it with basic action `1/0` and retargets the complete target to
   the exact incoming source;
5. successful rewrite advances staged state `2` to rewrite-owned state `5`;
6. the exact pass-2 commit resolves `actor+0x148`, consumes cadence once, and clears the state;
7. state `0x2C` delivers `443` / action `0` to that same source.

## Fixed safety bounds

- Profile: `work/1784159380-battle-runtime-settings.synthetic-reaction-live-rewrite.json`.
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
- Fixture SHA-256: `415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`.
- Startup owner: equipped `443`, reaction set `00 00 04 00`, candidate `unit+0x1CE=0`.
- Candidate writes: maximum one.
- Accepted-order writes: maximum one.
- Original order guard: exactly `actionType=0`, `actionId=443`.
- Final action: exactly `actionType=1`, `actionId=0`.
- Final target: `targetMode=5`, `targetIdx=sourceIdx`, including source coordinates.
- Target Wenyld for Rion's opening Auto-battle action; unlike Herkyna, Wenyld does not expose a
  native Counter in the forecast.
- The incoming source is dynamic: Wenyld (`6`) if her attack lands, otherwise a later surviving
  hostile hit such as Choco Beak (`0`) may own the transaction. The log must use one source
  consistently across gate, materialization, managed commit, and effect.
- Close immediately after the carrier effect or on any failed/skip/lost, blocked/capped rewrite,
  duplicate cadence, target mismatch, or second write. Do not save.

## Required strict gate

```powershell
python tools/analyze_dcl_synthetic_reaction_live.py <log> --carrier 443 --mode live --require-startup-owner --expected-reaction-set-hex 00000400 --require-source-retarget --expected-action-type 1 --expected-action-id 0 --expected-original-action-type 0 --expected-original-action-id 443 --require-effect
```

After reading the one observed source index, also run the materialization and effect analyzers with
that exact source, target, reactor table index `16`, and actor index `4`. Every analyzer must pass.

Restore DLL, PDB, settings, AppConfig, game log, and autosave to their pre-test hashes before any
interpretation is promoted.
