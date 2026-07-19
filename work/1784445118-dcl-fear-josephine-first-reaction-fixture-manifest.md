# FFT Enhanced autosave CT-order fixture

## Scope

This non-deployed fixture changes only `unit+0x41` CT bytes in the current live battle
components. FF16Tools owns every updated member CRC and the enclosing PNG serialization.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784436517-dcl-fear-josephine-fervor-battlebyte-correct-arthur-999hp-fixture.png`
- Source SHA-256: `09795A0D12766DFFADC3CD90DAFA4402F6FFC43C2D45DA2B3FFC14FEACC2BF3F`

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

- Fixture PNG: `1784445118-dcl-fear-josephine-first-reaction-fixture.png`
- Fixture SHA-256: `60F9509832AB2EC9ED0F1090A87228C07F284D645088479265049374E5C2D2E6`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
