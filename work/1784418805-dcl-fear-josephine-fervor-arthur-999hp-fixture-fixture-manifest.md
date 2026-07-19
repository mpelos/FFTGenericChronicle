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
- `Arthur-HP-lo` signature `8010015200089003`: unit+`0x30` `199` -> `231`
- `Arthur-HP-hi` signature `8010015200089003`: unit+`0x31` `0` -> `3`
- `Arthur-MaxHP-lo` signature `8010015200089003`: unit+`0x32` `199` -> `231`
- `Arthur-MaxHP-hi` signature `8010015200089003`: unit+`0x33` `0` -> `3`

## Round-trip proof

| Member | Unit | Record | Command byte | Transition |
| --- | --- | ---: | ---: | ---: |
| `resume_en00_main.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en00_fturn.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en01_main.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en01_fturn.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_enbtl_main.sav` | `Josephine` | `0x83CF8` | `0x83D0B` | `19` -> `16` |
| `resume_en00_main.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_en00_main.sav` | `Arthur-HP-lo` | `0x83AF8` | `0x83B28` | `199` -> `231` |
| `resume_en00_main.sav` | `Arthur-HP-hi` | `0x83AF8` | `0x83B29` | `0` -> `3` |
| `resume_en00_main.sav` | `Arthur-MaxHP-lo` | `0x83AF8` | `0x83B2A` | `199` -> `231` |
| `resume_en00_main.sav` | `Arthur-MaxHP-hi` | `0x83AF8` | `0x83B2B` | `0` -> `3` |
| `resume_en00_fturn.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_en00_fturn.sav` | `Arthur-HP-lo` | `0x83AF8` | `0x83B28` | `199` -> `231` |
| `resume_en00_fturn.sav` | `Arthur-HP-hi` | `0x83AF8` | `0x83B29` | `0` -> `3` |
| `resume_en00_fturn.sav` | `Arthur-MaxHP-lo` | `0x83AF8` | `0x83B2A` | `199` -> `231` |
| `resume_en00_fturn.sav` | `Arthur-MaxHP-hi` | `0x83AF8` | `0x83B2B` | `0` -> `3` |
| `resume_en01_main.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_en01_main.sav` | `Arthur-HP-lo` | `0x83AF8` | `0x83B28` | `199` -> `231` |
| `resume_en01_main.sav` | `Arthur-HP-hi` | `0x83AF8` | `0x83B29` | `0` -> `3` |
| `resume_en01_main.sav` | `Arthur-MaxHP-lo` | `0x83AF8` | `0x83B2A` | `199` -> `231` |
| `resume_en01_main.sav` | `Arthur-MaxHP-hi` | `0x83AF8` | `0x83B2B` | `0` -> `3` |
| `resume_en01_fturn.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_en01_fturn.sav` | `Arthur-HP-lo` | `0x83AF8` | `0x83B28` | `199` -> `231` |
| `resume_en01_fturn.sav` | `Arthur-HP-hi` | `0x83AF8` | `0x83B29` | `0` -> `3` |
| `resume_en01_fturn.sav` | `Arthur-MaxHP-lo` | `0x83AF8` | `0x83B2A` | `199` -> `231` |
| `resume_en01_fturn.sav` | `Arthur-MaxHP-hi` | `0x83AF8` | `0x83B2B` | `0` -> `3` |
| `resume_enbtl_main.sav` | `Josephine-Fervor-learned` | `0x83CF8` | `0x83DBB` | `0` -> `1` |
| `resume_enbtl_main.sav` | `Arthur-HP-lo` | `0x83AF8` | `0x83B28` | `199` -> `231` |
| `resume_enbtl_main.sav` | `Arthur-HP-hi` | `0x83AF8` | `0x83B29` | `0` -> `3` |
| `resume_enbtl_main.sav` | `Arthur-MaxHP-lo` | `0x83AF8` | `0x83B2A` | `199` -> `231` |
| `resume_enbtl_main.sav` | `Arthur-MaxHP-hi` | `0x83AF8` | `0x83B2B` | `0` -> `3` |

All unlisted members are byte-identical after pack/unpack. Listed members change only
the enumerated command byte and their recomputed inner CRC fields.

## Artifact

- Fixture PNG: `1784418805-dcl-fear-josephine-fervor-arthur-999hp-fixture-fixture.png`
- Fixture SHA-256: `9BFEE367863968B37FB80B67781C68E2056F066F9DE1199DF2CCA662CBBA7ADE`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
