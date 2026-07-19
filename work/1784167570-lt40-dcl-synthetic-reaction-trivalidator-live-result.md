# LT40 synthetic-Reaction tri-validator live result

## Capture

- Log: `work/1784167467-lt40-dcl-synthetic-reaction-owner443-delivery442-trivalidator-live.log`
- SHA-256: `64045D256CF485B0FEDE93036956EBD8275880FD3695FCA21AB3EAA4805082FD`
- Runtime profile:
  `work/1784166334-battle-runtime-settings.synthetic-reaction-owner443-delivery442-trivalidator-live.json`
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`
- External backup: `C:\Users\mmpel\AppData\Local\Temp\fftgc-lt40-trivalidator-1784166663`

## Observed chain

1. All three guarded validation hooks installed: shared typed-family `0x283019`, typed-Bonecrusher
   `0x283148`, and final `0x28315C`. Materialization, commit, and effect hooks also installed without
   any failure or skip row.
2. Wenyld at source table index `6` hit the surviving Rion. Owner `443` reserved and staged delivery
   `442` for defender table index `16`.
3. Counter's shared typed-family helper returned `-2` for source `6`, marked only the private
   mailbox `2->6`, and rejected the order. No final-validator, materialization, commit, or `442`
   effect row belongs to source `6`.
4. Choco Beak from adjacent source `0` hit Rion and reserved a distinct request.
5. Counter's typed-family and final validators both returned `0` for source `0`, preserving mailbox
   state `2->2`.
6. The selector materialized exactly one owned Reaction `442`: reactor table index `16`, source `0`,
   native action `1/0`, target mode `5`, target index `0`.
7. Exactly one agreeing pass-2 native commit and one managed cadence commit consumed the accepted
   request.
8. Exactly two state-`0x2C` effect rows retained presentation id `442`, executable action `0`, and
   source target `0`. They are the two Dual Wield strikes of one Reaction transaction.

## Analyzer outcome

- Rejected-then-accepted tri-validator chain: PASS (`3` hooks, `3` relevant result rows, no extras).
- Synthetic owner/delivery transaction: PASS (`2` gates, `1` materialization, `1` native commit,
  `1` managed commit, `2` delivery effects).
- Materialization and exact effect cardinality: PASS.
- Commit/effect source correlation: PASS.

## Interpretation

Counter `442` uses the shared typed-family result site `0x283019`; Bonecrusher `434` alone owns
`0x283148`. The distant Counter order is rejected at the path-specific typed-family helper before
the common final validator. The adjacent order passes both zero-result gates and reaches native
delivery. Candidate staging and consumption are therefore not evidence of acceptance, and the
specific Counter reach/legality rejection boundary is now live-proven.

The owner/delivery split is also closed as a technical vertical: a blank equipped owner can reserve
an authored taxonomy event, a supported special-family carrier can receive the native delivery, and
cadence commits only after exact accepted materialization. The mechanism embeds no job policy or
managed effect.

## Control boundary

This capture is not a natural-damage battle. Its profile explicitly sets `DclDamageFormula` to
`1`, and the log records Rion at `277 -> 276 -> 275` HP across the Wenyld and Choco Beak hits. The
accepted `442` therefore proves delivery and validator behavior only for a surviving defender.
It does not prove that Counter can execute after a lethal hit; the runtime rejects such a request
using the final staged equation `HP + credit - debit <= 0`.

The user-reported visual sequence in which Choco Beak killed Rion cannot be identified with this
capture: this log continues with Rion alive and taking another turn. Do not use that visual account
as corroboration of this file, and do not claim a visible Counter from the log alone.

## External restoration

FFT and Reloaded-II were closed before restoration. The six external artifacts match their
independent pre-gate hashes:

- DLL: `9F6F5E68CB5E970633D21C816BC4C0F1ADE8E8FE8C75827C69FF9F5D52D4E9EF`
- PDB: `84E205C7B3CB81FFD034D0F0A80F9F7F9E92A56A90EF801FBB3D5EC531FB6DBB`
- runtime settings: `BD6857DC2219BAAC3A9769C5F4C040B1F762081FAE66AA192D8B8755964CC624`
- Reloaded AppConfig: `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`
- battle-probe log: `E59ADC614EB1032D0B0B733DD95887CEA6AE066FEF58C0642B4F00C90A46B63E`
- Enhanced autosave: `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`

## Next technical gate

The synthetic producer/materialization/commit mechanism no longer needs another isolated live
probe. The next investigation should return offline to the whole-DCL coverage matrix and advance
the highest-impact remaining combat mechanism that is still `partial-live-gated`, while reserving
the unified sentinel integration regression for the final composition phase.
