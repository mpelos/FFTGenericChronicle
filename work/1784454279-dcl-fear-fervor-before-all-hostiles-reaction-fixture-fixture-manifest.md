# FFT Enhanced autosave CT-order fixture

## Scope

This non-deployed fixture changes only `unit+0x41` CT bytes in the current live battle
components. FF16Tools owns every updated member CRC and the enclosing PNG serialization.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784446096-dcl-fear-josephine-first-fervor-reaction-fixture.png`
- Source SHA-256: `8215A2DA81DC9D5A77494E656B10685215160D742A420046AC31655FBD9EA777`

## Requested CT edits

- `Phanes-index0` scope `turn`, signature `8200FF6103902003`: CT `90` -> `0`
- `Polybos-index1` scope `turn`, signature `8201FF5E03502003`: CT `10` -> `0`
- `Chocobo-index4` scope `turn`, signature `8204FF6703502003`: CT `100` -> `0`
- `Chocobo-index6` scope `turn`, signature `8206FF6703502003`: CT `100` -> `0`
- `Kleobis-index7` scope `turn`, signature `8207FF5E03502003`: CT `110` -> `0`

## Round-trip proof

| Member | Unit | Record | CT byte | Transition |
| --- | --- | ---: | ---: | ---: |
| `resume_en00_fturn.sav` | `Phanes-index0` | `0x81AF8` | `0x81B39` | `90` -> `0` |
| `resume_en00_fturn.sav` | `Polybos-index1` | `0x81CF8` | `0x81D39` | `10` -> `0` |
| `resume_en00_fturn.sav` | `Chocobo-index4` | `0x822F8` | `0x82339` | `100` -> `0` |
| `resume_en00_fturn.sav` | `Chocobo-index6` | `0x826F8` | `0x82739` | `100` -> `0` |
| `resume_en00_fturn.sav` | `Kleobis-index7` | `0x828F8` | `0x82939` | `110` -> `0` |
| `resume_enbtl_main.sav` | `Phanes-index0` | `0x81AF8` | `0x81B39` | `90` -> `0` |
| `resume_enbtl_main.sav` | `Polybos-index1` | `0x81CF8` | `0x81D39` | `10` -> `0` |
| `resume_enbtl_main.sav` | `Chocobo-index4` | `0x822F8` | `0x82339` | `100` -> `0` |
| `resume_enbtl_main.sav` | `Chocobo-index6` | `0x826F8` | `0x82739` | `100` -> `0` |
| `resume_enbtl_main.sav` | `Kleobis-index7` | `0x828F8` | `0x82939` | `110` -> `0` |

Every unlisted member is byte-identical after pack/unpack. Listed members change only
the enumerated CT bytes and their four-byte inner CRC fields. Current alias equalities and
the battle-entry id remain valid.

## Artifact

- Fixture PNG: `1784454279-dcl-fear-fervor-before-all-hostiles-reaction-fixture-fixture.png`
- Fixture SHA-256: `5C55CBEB8F4B9D0F691D48254AA369EAD14D07CE9BE5965C4C68AE3E84CC552A`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
