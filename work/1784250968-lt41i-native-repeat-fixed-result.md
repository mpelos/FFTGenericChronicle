# LT41I native-repeat fixed regression result

## Scope

LT41I reruns the owner-`443` / delivery-`442` synthetic Reaction vertical after admitting native
repeat execution state `0x2F` into the DCL result pipeline and repairing the automatic-length typed
hooks. It continues long enough to capture both the Counter and a later ordinary Dual Wield Attack.

Primary game-side capture:

- `work/1784250813-lt41i-dcl-native-repeat-fixed-game.log`
- SHA-256 `ECA1BDB7CDC681284AE01E54A1F1DE5D2CBED5BC2E5B3BF1D526A1F1F917A814`

Reloaded mirror capture:

- `work/1784250564-lt41i-dcl-native-repeat-fixed-live.log`
- SHA-256 `B831D0B71EF2F5034035349DCCD31EE59E45DEABD9B97BB44DAB61F54DDEC7C0`

Machine-checked analysis:

- `work/1784250814-dcl-native-repeat-provenance-live-analysis.md`
- SHA-256 `E66E73998AF941562EE3E80DC9E29A02B8486A74B3325850CB72F8F07C50C34E`

## Hook installation and Reaction delivery

All three Reaction delivery-validation hooks install together:

- typed-family `0x283019`, automatic length;
- typed-bonecrusher `0x283148`, automatic length;
- final call/test boundary `0x283157`, exact length `7`.

No hook-install failure occurs. A distant source is rejected with `-2` at typed-family. The later
adjacent Chocobo source returns zero at typed-family and final validation, materializes one Counter
`442` as native Basic Attack `1/0`, commits once, and emits exactly two effect rows against source
index `0`. `tools/analyze_dcl_reaction_materialization_live.py` passes with one materialization, one
matching pass-2 commit, and two effects.

## Counter result pair

- calc `n=17`, state `0x2A`, exact tuple `(caster=16,type=1,ability=0,payload=124,target=0)`:
  native debit `189`, DCL debit `1`, HP `332 -> 331`;
- calc `n=18`, state `0x2F`, the same tuple:
  native debit `189`, DCL debit `1`, HP `331 -> 330`.

The former second-strike escape is closed: both results independently reach managed numeric
ownership and native HP apply.

## Ordinary Attack corroboration

- calc `n=31`, state `0x2A`, exact tuple `(caster=16,type=1,ability=0,payload=18,target=6)`:
  native debit `210`, DCL debit `1`, HP `395 -> 394`;
- calc `n=32`, state `0x2F`, the same tuple:
  native debit `126`, DCL debit `1`, HP `394 -> 393`.

The differing native debits prove that native amount equality is not part of repeat identity. Each
transaction must be validated against and rewritten from its own staged pre-clamp result.

## Environment restoration

FFT Enhanced and Reloaded-II were closed through their Windows interfaces. No new FFT WER report
appeared. The independent backup restored and hash-verified all six external artifacts:

- DLL `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`;
- PDB `84E205C7B3CB81FFD034D0F0A80F9F7F9E92A56A90EF801FBB3D5EC531FB6DBB`;
- settings `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`;
- Reloaded AppConfig `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`;
- Enhanced autosave `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`;
- battle probe log `E59ADC614EB1032D0B0B733DD95887CEA6AE066FEF58C0642B4F00C90A46B63E`.

## Decision

Native repeat execution state `0x2F` remains admitted only as outer-sweep provenance. The numeric
writer, status producer, hit ownership, and apply lookup treat each result as its own DCL
transaction. Forecast, AI-only evaluation, unknown states, and nested Rend calculations remain
excluded by their existing provenance gates.
