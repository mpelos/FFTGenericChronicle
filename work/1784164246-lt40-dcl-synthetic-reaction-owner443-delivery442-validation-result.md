# LT40 owner-443 / delivery-442 native-validation result

> Superseded correction: Counter `442` uses shared typed-family result RVA `0x283019`.
> RVA `0x283148` belongs to Bonecrusher `434`'s separate typed call. See
> `work/1784166335-lt40-dcl-synthetic-reaction-owner443-delivery442-adjacent-positive-result.md`.

## Capture

- Log: `work/1784163675-lt40-dcl-synthetic-reaction-owner443-delivery442-deviated-live.log`
- SHA-256: `35396781651172F02752AC91E006D859C4D95FD246FC684796E9EB8DA4782B43`
- Historical profile: `work/1784162789-battle-runtime-settings.synthetic-reaction-owner443-delivery442-live.json`
- Fixture owner: Rion `0x80`, equipped Reaction `443`, reaction set `00000400`, empty startup candidate.

## Observed chain

1. All configured hooks installed with exact byte guards and the isolated Reloaded profile loaded
   only the mod loader and Generic Chronicle Battle Probe.
2. Rion's Throw Shuriken produced the expected empty-candidate control.
3. Wenyld at source table index `6` hit the surviving Rion. Owner `443` passed its forced taxonomy
   roll and armed one `delivery=442` request.
4. The next pre-selector found no native candidates and staged `442` for defender table index `16`.
5. No `0x2831BD` materialization, pass-2 `442` commit, managed cadence commit, or `442` effect
   followed. The strict analyzer failed on every missing acceptance/delivery requirement.
6. Choco Beak later created a distinct accepted owner gate from source `0`, but the profile's exact
   one-write cap left mailbox state `3` and performed no second candidate write.

## Corrected interpretation

The selector clears `unit+0x1CE` before special-delivery validation. Counter/Bonecrusher call the
typed source/order helper and require `eax == 0` at `0x283148`; a succeeding order then requires the
final VM-owned validator to return zero at `0x28315C`. Either nonzero result restores the previous
order at `0x283160` and resumes scanning without reaching accepted materialization.

The distant Wenyld source is therefore direct live proof that candidate staging/consumption is not
delivery acceptance. Native Counter range rejection is the strong explanation: the DCL reach model
already requires a reach-1 defender not to counter a ranged attack, while native `442` previously
accepted the adjacent Chocobo. The old binary hook set cannot identify which validator returned
nonzero, so that specific cause remains unproven.

## Offline correction

- Added disabled-by-default typed/final validation-result capture at `0x283148` and `0x28315C`.
- A nonzero result changes only the exact staged mod-private mailbox from `2` to rejected state `6`;
  it never changes game order, candidate, cadence, or effect state.
- Live synthetic profiles now require this classifier.
- The follow-up profile permits two bounded writes so the known ranged rejection can be followed by
  one adjacent Chocobo attempt in the same deterministic battle sequence.

## External restoration

The game and Reloaded-II were closed before restoration. The six external artifacts match their
independent pre-gate hashes:

- DLL: `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`
- PDB: `84E205C7B3CB81FFD034D0F0A80F9F7F9E92A56A90EF801FBB3D5EC531FB6DBB`
- runtime settings: `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- Reloaded AppConfig: `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`
- live log: `E59ADC614EB1032D0B0B733DD95887CEA6AE066FEF58C0642B4F00C90A46B63E`
- autosave: `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`
