# LT40 synthetic-Reaction pre-target-build live result

## Capture

- Log: `work/1784161607-lt40-dcl-synthetic-reaction-pre-target-build-live.log`
- SHA-256: `E1692309AD06B0EA3EB47C3FAF8AFB63E9CF299B75F6DCF5F06D6665832A9DD0`
- Profile: `work/1784160993-battle-runtime-settings.synthetic-reaction-pre-target-build-live.json`
- Planned boundary: `0x2831BD`, immediately before call `0x2831C0`.

## Observed chain

1. Startup installed the pass-2, pre-selector, materialization, and state-`0x2C` hooks with their
   exact byte guards.
2. Wenyld's survived hit armed exactly one synthetic carrier-`443` reservation for defender table
   index `16` and source table index `6`.
3. The pre-selector producer staged that reservation and the native queue accepted one pass-2 actor
   with agreeing Reaction ids `443/443`.
4. The hook at `0x2831BD` produced zero materialization rows and therefore performed zero writes.
5. State `0x2C` delivered the untouched generic carrier as action `443` to target `[16]` (the
   reactor), not action `0` to source `[6]`.
6. The strict synthetic, materialization, and effect analyzers all failed on the missing
   materialization/rewrite and wrong delivered action/target. There were no hook failures or lost
   ring events.

## Refuted hypothesis

`0x2831BD` is not a common accepted-order boundary. Static disassembly shows that it belongs to the
special validated branch used by Counter-like carriers. Generic carrier `443` takes the branch at
`0x282F73`, builds its self-target order, and jumps from `0x283003` directly to common finalization
at `0x2831CC`, skipping both `0x2831BD` and the call at `0x2831C0`.

The earlier static report correctly described the Counter branch but incorrectly generalized it to
all accepted Reaction ids. The live absence of a materialization event is direct evidence of that
path distinction.

## Next offline hypothesis

The common selector-finalization sequence is:

```text
generic carrier 443: 0x283003 -> 0x2831CC
special accepted path: 0x2831BD -> call 0x282234 -> 0x2831C5 -> 0x2831CC
0x2831C5: load exact Reaction id into esi
0x2831CC: copy si to the selector's output record
0x2831D0: return the selected unit index
```

For a synthetic blank carrier, a guarded controller at the common `0x2831CC` boundary can identify
original `si == 443`, require producer state `staged`, transform the order, and—if needed—transform
the delivered native Reaction id before the caller constructs the actor. This must be proven by a
new static path analysis and full offline suite before another live write is considered.

## External restoration

The game and Reloaded-II were closed before restoration. The six external artifacts were restored
to their independent pre-gate hashes:

- DLL: `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`
- PDB: `84E205C7B3CB81FFD034D0F0A80F9F7F9E92A56A90EF801FBB3D5EC531FB6DBB`
- runtime settings: `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- Reloaded AppConfig: `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`
- live log: `E59ADC614EB1032D0B0B733DD95887CEA6AE066FEF58C0642B4F00C90A46B63E`
- autosave: `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`
