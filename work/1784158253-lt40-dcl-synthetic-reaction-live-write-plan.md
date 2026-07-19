# LT40 synthetic-Reaction live-write gate

## Purpose

Prove or refute the complete generic transaction after the corrected carrier fixture passed the log-only gate:

1. a committed successful hit reserves the exact equipped defender;
2. the pre-selector stages carrier `443` once at `unit+0x1CE`;
3. the accepted carrier order retains native action `type=1/id=0` and is retargeted to the incoming source;
4. the exact pass-2 native commit owns the synthetic cadence once;
5. state `0x2C` delivers carrier `443`, action `0`, to that exact source.

This is a mechanism test only. Carrier `443` has no design or job meaning.

## Fixed inputs and safety bounds

- Runtime profile: `work/1784158252-battle-runtime-settings.synthetic-reaction-live-write.json`.
- Autosave fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
- Fixture SHA-256: `415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`.
- Rion must start with equipped reaction `443`, reaction-set bytes `00 00 04 00`, and candidate `unit+0x1CE = 0`.
- Synthetic candidate staging is capped at one write.
- Accepted-order rewriting is independently capped at one matching order.
- Rewrite requires exact carrier `443` and exact original native order `actionType=1`, `actionId=0`.
- Action replacement remains disabled; only source retargeting is allowed.
- Close immediately after the carrier effect, or on any failed/skip/lost row, identity mismatch, unexpected action, unexpected target, or second write.
- Do not save the battle. Restore DLL, PDB, settings, Reloaded AppConfig, game log, and autosave from pre-test backups.

## Expected live identity

- Equipped defender/selected reactor table index: `16` (`id=0x80`, Rion).
- Incoming source table index: `6` (`id=0x81`, Wenyld).
- Runtime actor index for Rion: `4` in this fixture.
- Materialized order: `reactionId=443 actionType=1 actionId=0 targetMode=5 targetIdx=6 rewrite=wrote`.
- Native commit: `pass=2 reactionId=443 idsAgree=True`.
- Managed commit: `carrier=443 ... sourceIdx=6 cadence=consumed delivery=accepted-order-owned`.
- Effect: `reactionId=443 actionId=0 sourceIdx=6 targets=[6]`.

## Required analysis

```powershell
python tools/analyze_dcl_synthetic_reaction_live.py <log> --carrier 443 --mode live --require-startup-owner --expected-reaction-set-hex 00000400 --require-source-retarget --expected-action-type 1 --expected-action-id 0 --require-effect
python tools/analyze_dcl_reaction_materialization_live.py <log> --reaction-id 443 --reactor 16 --actor-reactor 4 --source 6 --expected-action-type 1 --expected-action-id 0 --expected-target 6 --expected-materialized-count 1 --expected-effect-count 1
python tools/analyze_dcl_reaction_effect_live.py <log> --reaction-id 443 --reactor 4 --source 6 --expected-action-id 0 --expected-effect-count 1 --expect-target-source
```

All analyzers must pass. Absence of any required row is a failed gate, not partial success.
