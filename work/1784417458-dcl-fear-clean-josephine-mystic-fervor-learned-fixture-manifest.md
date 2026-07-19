# FFT Enhanced autosave secondary-command fixture

## Scope

This non-deployed fixture changes only `unit+0x13`, the live secondary-command id,
for exact unit signatures in every current-battle command-bearing copy.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784390906-fft-autoenhanced-snapshot.png`
- Source SHA-256: `A3D96C1118088D195FBD6863ECFA9B70DDE1D5188583F68D4510512BCB572204`

## Requested edits

- `Josephine` signature `8111065000085003`: secondary command `19` -> `16`
- `Josephine-Fervor-learned` signature `8111065000085003`: unit+`0xC3` `0` -> `1`

## Round-trip proof

| Member | Unit | Record | Command byte | Transition |
| --- | --- | ---: | ---: | ---: |
| `resume_en00_main.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en00_fturn.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en01_main.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en01_fturn.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_enbtl_main.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en00_main.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_en00_fturn.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_en01_main.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_en01_fturn.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_enbtl_main.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |

All unlisted members are byte-identical after pack/unpack. Listed members change only
the enumerated command byte and their recomputed inner CRC fields.

## Artifact

- Fixture PNG: `1784417458-dcl-fear-clean-josephine-mystic-fervor-learned-fixture.png`
- Fixture SHA-256: `548D74B39306252387E66E511E6D1E605C3ADE1E69B6058B37F17A00D84429E7`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
