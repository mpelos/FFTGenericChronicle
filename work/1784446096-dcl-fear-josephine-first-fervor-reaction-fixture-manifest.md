# FFT Enhanced autosave CT-order fixture

## Scope

This non-deployed fixture changes only `unit+0x41` CT bytes in the current live battle
components. FF16Tools owns every updated member CRC and the enclosing PNG serialization.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784418805-dcl-fear-josephine-fervor-arthur-999hp-fixture-fixture.png`
- Source SHA-256: `9BFEE367863968B37FB80B67781C68E2056F066F9DE1199DF2CCA662CBBA7ADE`

## Requested CT edits

- `Josephine-snapshot` scope `snapshot`, signature `8111065000085003`: CT `104` -> `99`
- `Josephine-turn` scope `turn`, signature `8111065000085003`: CT `80` -> `99`
- `Arthur-snapshot` scope `snapshot`, signature `8010015200089003`: CT `4` -> `0`
- `Arthur-turn` scope `turn`, signature `8010015200089003`: CT `80` -> `0`

## Round-trip proof

| Member | Unit | Record | CT byte | Transition |
| --- | --- | ---: | ---: | ---: |
| `resume_en00_main.sav` | `Josephine-snapshot` | `0x83CF8` | `0x83D39` | `104` -> `99` |
| `resume_en00_main.sav` | `Arthur-snapshot` | `0x83AF8` | `0x83B39` | `4` -> `0` |
| `resume_en00_fturn.sav` | `Josephine-turn` | `0x83CF8` | `0x83D39` | `80` -> `99` |
| `resume_en00_fturn.sav` | `Arthur-turn` | `0x83AF8` | `0x83B39` | `80` -> `0` |
| `resume_enbtl_main.sav` | `Josephine-turn` | `0x83CF8` | `0x83D39` | `80` -> `99` |
| `resume_enbtl_main.sav` | `Arthur-turn` | `0x83AF8` | `0x83B39` | `80` -> `0` |

Every unlisted member is byte-identical after pack/unpack. Listed members change only
the enumerated CT bytes and their four-byte inner CRC fields. Current alias equalities and
the battle-entry id remain valid.

## Artifact

- Fixture PNG: `1784446096-dcl-fear-josephine-first-fervor-reaction-fixture.png`
- Fixture SHA-256: `8215A2DA81DC9D5A77494E656B10685215160D742A420046AC31655FBD9EA777`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
