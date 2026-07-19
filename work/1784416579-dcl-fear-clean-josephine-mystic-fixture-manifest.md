# FFT Enhanced autosave secondary-command fixture

## Scope

This non-deployed fixture changes only `unit+0x13`, the live secondary-command id,
for exact unit signatures in the current battle aliases.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784390906-fft-autoenhanced-snapshot.png`
- Source SHA-256: `A3D96C1118088D195FBD6863ECFA9B70DDE1D5188583F68D4510512BCB572204`

## Requested edits

- `josephine` signature `8111065000085003`: secondary command `19` -> `16`

## Round-trip proof

| Member | Unit | Record | Command byte | Transition |
| --- | --- | ---: | ---: | ---: |
| `resume_en00_main.sav` | `josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en00_fturn.sav` | `josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_enbtl_main.sav` | `josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |

All unlisted members are byte-identical after pack/unpack. Listed members change only
the enumerated command byte and their recomputed inner CRC fields.

## Artifact

- Fixture PNG: `1784416579-dcl-fear-clean-josephine-mystic-fixture.png`
- Fixture SHA-256: `37DF566F9E6E099348ECC4A437FDFE527853D8E19DAC3FE03ED4F7390AD0FB7B`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
