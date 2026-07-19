# FFT Enhanced autosave CT-order fixture

## Scope

This non-deployed fixture changes only `unit+0x41` CT bytes in the current live battle
components. FF16Tools owns every updated member CRC and the enclosing PNG serialization.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784412728-fft-autoenhanced-pre-v7.png`
- Source SHA-256: `D74BC0E4DCC1140529B49CA875302B96D7357E6F2B12127B33D24A5D3956B2CC`

## Requested CT edits

- `enemy0-snapshot` scope `snapshot`, signature `8200FF6103902003`: CT `20` -> `0`
- `enemy3-snapshot` scope `snapshot`, signature `8203FF5E03502003`: CT `60` -> `0`
- `enemy5-snapshot` scope `snapshot`, signature `8205FF6103502003`: CT `20` -> `0`
- `leona-snapshot` scope `snapshot`, signature `8110045500085003`: CT `80` -> `0`
- `arthur-snapshot` scope `snapshot`, signature `8011015200009003`: CT `0` -> `50`
- `josephine-snapshot` scope `snapshot`, signature `8112065000085003`: CT `60` -> `100`
- `enemy0-turn` scope `turn`, signature `8200FF6103902003`: CT `90` -> `0`
- `enemy3-turn` scope `turn`, signature `8203FF5E03502003`: CT `10` -> `0`
- `enemy5-turn` scope `turn`, signature `8205FF6103502003`: CT `90` -> `0`
- `leona-turn` scope `turn`, signature `8110045500085003`: CT `100` -> `0`
- `arthur-turn` scope `turn`, signature `8011015200089003`: CT `80` -> `50`
- `josephine-turn` scope `turn`, signature `8112065000085003`: CT `80` -> `100`

## Round-trip proof

| Member | Unit | Record | CT byte | Transition |
| --- | --- | ---: | ---: | ---: |
| `resume_en00_main.sav` | `enemy0-snapshot` | `0x81AF8` | `0x81B39` | `20` -> `0` |
| `resume_en00_main.sav` | `enemy3-snapshot` | `0x820F8` | `0x82139` | `60` -> `0` |
| `resume_en00_main.sav` | `enemy5-snapshot` | `0x824F8` | `0x82539` | `20` -> `0` |
| `resume_en00_main.sav` | `leona-snapshot` | `0x83AF8` | `0x83B39` | `80` -> `0` |
| `resume_en00_main.sav` | `arthur-snapshot` | `0x83CF8` | `0x83D39` | `0` -> `50` |
| `resume_en00_main.sav` | `josephine-snapshot` | `0x83EF8` | `0x83F39` | `60` -> `100` |
| `resume_en00_fturn.sav` | `enemy0-turn` | `0x81AF8` | `0x81B39` | `90` -> `0` |
| `resume_en00_fturn.sav` | `enemy3-turn` | `0x820F8` | `0x82139` | `10` -> `0` |
| `resume_en00_fturn.sav` | `enemy5-turn` | `0x824F8` | `0x82539` | `90` -> `0` |
| `resume_en00_fturn.sav` | `leona-turn` | `0x83AF8` | `0x83B39` | `100` -> `0` |
| `resume_en00_fturn.sav` | `arthur-turn` | `0x83CF8` | `0x83D39` | `80` -> `50` |
| `resume_en00_fturn.sav` | `josephine-turn` | `0x83EF8` | `0x83F39` | `80` -> `100` |
| `resume_enbtl_main.sav` | `enemy0-turn` | `0x81AF8` | `0x81B39` | `90` -> `0` |
| `resume_enbtl_main.sav` | `enemy3-turn` | `0x820F8` | `0x82139` | `10` -> `0` |
| `resume_enbtl_main.sav` | `enemy5-turn` | `0x824F8` | `0x82539` | `90` -> `0` |
| `resume_enbtl_main.sav` | `leona-turn` | `0x83AF8` | `0x83B39` | `100` -> `0` |
| `resume_enbtl_main.sav` | `arthur-turn` | `0x83CF8` | `0x83D39` | `80` -> `50` |
| `resume_enbtl_main.sav` | `josephine-turn` | `0x83EF8` | `0x83F39` | `80` -> `100` |

Every unlisted member is byte-identical after pack/unpack. Listed members change only
the enumerated CT bytes and their four-byte inner CRC fields. Current alias equalities and
the battle-entry id remain valid.

## Artifact

- Fixture PNG: `1784412967-dcl-fear-v7-josephine-first-fixture.png`
- Fixture SHA-256: `D395B5AC909AA4491F0F693663D114BB590A2E02F612DB19A49F1C128D8BD2A6`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
