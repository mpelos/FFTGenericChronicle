# LT40 synthetic-Reaction adjacent positive result

## Capture

- Log: `work/1784165727-lt40-dcl-synthetic-reaction-adjacent-accepted-wenyld-miss-live.log`
- SHA-256: `4E4F7F723E454CC3B98883A57E64848E1BECE780963155DC9C3875E76311F067`
- Runtime profile: `work/1784164245-battle-runtime-settings.synthetic-reaction-owner443-delivery442-validation-live.json`
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`
- External backup: `C:\Users\mmpel\AppData\Local\Temp\fftgc-lt40-validation-1784165265`

## Observed sequence

Wenyld evaluated both basic Attack and Throw Stone but executed a basic Attack that produced no HP
event. The successful-hit-survivor producer therefore did not arm for source `6`, and this capture
cannot classify the distant typed-helper rejection.

Choco Beak from adjacent source `0` hit Rion and armed the owner-`443` request. Delivery `442`
reached final validator `0x28315C` with result `0`, materialized once as native action `1/0` targeting
source `0`, committed once at pass 2, consumed managed cadence once, and emitted exactly two
state-`0x2C` rows targeting source `0`. The two rows are Rion's Dual Wield strikes, not duplicate
Reaction commits.

## Analyzer outcome

- Synthetic owner/delivery transaction: PASS.
- Special materialization and two-effect cardinality: PASS.
- Commit/effect correlation: PASS.
- Rejected-then-accepted validator chain: expected FAIL because the distant source never armed and
  the deployed build observed the wrong typed result site for Counter.

## Static correction

The selector dispatch proves that Counter `442` does not test its typed-helper result at
`0x283148`. Ids `435/436/437/442` share the call ending at result RVA `0x283019`; Bonecrusher `434`
uses the separate call ending at `0x283148`. All surviving special paths converge on final result
RVA `0x28315C`. The next build observes all three sites.

## Restoration

FFT and Reloaded-II were closed through Computer Use. DLL, PDB, runtime settings, Reloaded
AppConfig, live log, and autosave were restored byte-for-byte from the independent backup.
