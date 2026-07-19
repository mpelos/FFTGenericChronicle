# LT40 synthetic-Reaction live rewrite result

## Scope

The bounded gate used carrier `443` on the corrected Rion fixture and profile
`1784159380-battle-runtime-settings.synthetic-reaction-live-rewrite.json`. The profile allowed one
synthetic carrier write and one accepted-order rewrite. It required native order `0/443`, replaced
the executable action with `1/0`, and copied the incoming source tuple into the materialized order.

## Capture

- Live log: `1784160120-lt40-dcl-synthetic-reaction-live-rewrite-target-mismatch.log`
- SHA-256: `C9BFF4EF2E82CD4E486B24AEA17CD28BF9C566DFD841A1487EDE3DE32F3F7F3F`
- Incoming source: Wenyld, unit-table index `6`
- Rion survived Wenyld's basic attack: the accepted gate recorded one committed hit for source `6`.

## Observed transaction

1. The synthetic gate accepted exactly one hit and armed the mailbox.
2. The pre-selector staged carrier `443` exactly once.
3. The pass-2 commit accepted `443` with `idsAgree=True`, reactor actor index `4`, source `6`, and
   exact battle-unit record `0x141855CE0`.
4. The accepted-order hook matched original `actionType=0`, `actionId=443` and wrote final
   `actionType=1`, `actionId=0`, `targetMode=5`, `targetIdx=6`, plus Wenyld's coordinates. It reported
   `rewrite=wrote` and `rewriteWrites=1`.
5. The managed commit consumed cadence exactly once with
   `delivery=accepted-order-owned`.
6. State `0x2C` preserved presentation Reaction `443` and executable action id `0`, but its target
   list was `[16]`, Rion, rather than `[6]`, Wenyld. The calculator likewise resolved caster Rion to
   target Rion with a zero result.

## Analyzer result

The strict synthetic analyzer passed the gate, staging, materialization, original/final action,
pass-2 commit, managed commit, write cap, and startup-owner checks. It failed only
`delivery_effects=0`: the effect was not delivered to the exact incoming source.

The materialization analyzer independently confirmed the materialized order target `6` and failed
only the delivered target-list check. The commit/effect correlator confirmed one exact pass-2
transaction and one state-`0x2C` row, then failed `effect target is the incoming source` because the
effect row was `[16]`.

## Conclusion

The accepted-order boundary at RVA `0x2063BD` is sufficient to replace the executable action and to
publish rewrite ownership for cadence. Its order target tuple is not the final target authority for
generic carrier `443`: actor construction or a later delivery stage rebuilds/retains a self target.
The next investigation must locate that second target representation offline before another live
write. Loosening the exact guards would not address the observed mismatch.

## Restoration

The game and Reloaded-II were closed immediately after the one permitted rewrite. The external DLL,
PDB, runtime settings, Reloaded application config, probe log, and autosave were restored from the
independent pre-gate backup. Their restored SHA-256 values are:

- DLL: `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`
- PDB: `84E205C7B3CB81FFD034D0F0A80F9F7F9E92A56A90EF801FBB3D5EC531FB6DBB`
- settings: `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- AppConfig: `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`
- probe log: `E59ADC614EB1032D0B0B733DD95887CEA6AE066FEF58C0642B4F00C90A46B63E`
- autosave: `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`
