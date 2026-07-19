# FFT Enhanced autosave CT-order fixture

## Scope

This non-deployed fixture changes only `unit+0x41` CT bytes in the current live battle
components. FF16Tools owns every updated member CRC and the enclosing PNG serialization.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784157011-synthetic-reaction-carrier443-consistent-fixture.png`
- Source SHA-256: `415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`

## Requested CT edits

- `Choco` scope `main`, signature `8200FF5E03502003`: CT `77` -> `100`
- `Wenyld` scope `main`, signature `8106FF4D03504003`: CT `84` -> `0`
- `ChocoAttack` scope `attack`, signature `8200FF5E03502003`: CT `0` -> `100`

## Round-trip proof

| Member | Unit | Record | CT byte | Transition |
| --- | --- | ---: | ---: | ---: |
| `resume_en00_main.sav` | `Choco` | `0x81AF8` | `0x81B39` | `77` -> `100` |
| `resume_en00_main.sav` | `Wenyld` | `0x826F8` | `0x82739` | `84` -> `0` |
| `resume_en00_fturn.sav` | `Choco` | `0x81AF8` | `0x81B39` | `77` -> `100` |
| `resume_en00_fturn.sav` | `Wenyld` | `0x826F8` | `0x82739` | `84` -> `0` |
| `resume_enbtl_main.sav` | `Choco` | `0x81AF8` | `0x81B39` | `77` -> `100` |
| `resume_enbtl_main.sav` | `Wenyld` | `0x826F8` | `0x82739` | `84` -> `0` |
| `resume_en00_attack.sav` | `ChocoAttack` | `0x81AF8` | `0x81B39` | `0` -> `100` |
| `resume_enbtl_attack.sav` | `ChocoAttack` | `0x81AF8` | `0x81B39` | `0` -> `100` |

Every unlisted member is byte-identical after pack/unpack. Listed members change only
the enumerated CT bytes and their four-byte inner CRC fields. Current alias equalities and
the battle-entry id remain valid.

## Artifact

- Fixture PNG: `1784171803-dcl-dual-wield-fast-attack-ct-order-fixture.png`
- Fixture SHA-256: `02700D2FA95B00EAA55262FE88BFD77FF2ED8610F0ECB57C4D1BFD59C099D5F2`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
