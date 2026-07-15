# LT40 synthetic-Reaction log-only hook failure and offline correction

## Capture

- Profile: `work/1784152418-battle-runtime-settings.synthetic-reaction-logonly.json`.
- Fixture: `work/1784152701-synthetic-reaction-carrier443-fixture.png`.
- Log: `work/1784153639-lt40-dcl-synthetic-reaction-logonly-live.log`.
- Log SHA-256: `4609A58D099524E876B5A865623972BBA8FE78C846C2015DF17C1E3A706FD8C9`.
- The profile validated with zero errors and the code mod built with zero warnings/errors.

## Result

The guarded pre-selector hook did not install:

```text
[DCL-REACTION-PRESELECT-FAILED] rva=0x2063A9 FasmException: Failed to assemble FASM Mnemonics: Error name: 7, Line Number: 2019848707, Result: OutOfMemory
```

The plan's hard-stop rule was followed. No synthetic producer, accepted-order rewrite, effect, or
cadence write could run because the owning pre-selector hook was absent. The game was closed without
saving and the capture is not a functional synthetic-Reaction proof.

## Root cause

The new dynamic producer expanded its per-unit guard into 21 nearly identical assembler blocks.
The semantic guards were correct, but the generated hook exceeded the Reloaded FASM wrapper's
default assembly budget. The earlier static analyzer checked the transaction contract but did not
bound generated hook size.

## Offline correction

`Mod.cs` now emits one indexed loop over all 21 battle-table slots. It preserves the same state
transitions, active/survivor/KO/empty-slot guards, log-only behavior, write cap, carrier staging, and
producer result code without per-slot code expansion.

The transaction analyzer now requires the compact loop/cardinality anchors and forbids the old
per-unit generated-label pattern. Its regression tests pass. The current log-only shim was also
assembled directly with Reloaded's installed `Reloaded.Assembler`/FASMX64 wrapper using the same
default 65,536-byte work budget: 80 source lines produced 310 machine-code bytes and returned PASS.
The C# Release build and smoke tests pass with zero warnings/errors.

## Restoration

The game and Reloaded-II are stopped. The six pre-test artifacts were restored byte-for-byte:

- DLL: `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`
- PDB: `84E205C7B3CB81FFD034D0F0A80F9F7F9E92A56A90EF801FBB3D5EC531FB6DBB`
- runtime settings: `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- Reloaded AppConfig: `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`
- game log: `E59ADC614EB1032D0B0B733DD95887CEA6AE066FEF58C0642B4F00C90A46B63E`
- Enhanced autosave: `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`

The next LT40 attempt may repeat the same read-only protocol with the compact hook. No live producer
write is authorized by this correction.
