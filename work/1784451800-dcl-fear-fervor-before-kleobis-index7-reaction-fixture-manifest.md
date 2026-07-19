# FFT Enhanced autosave CT-order fixture

## Scope

This non-deployed fixture changes only `unit+0x41` CT bytes in the current live battle
components. FF16Tools owns every updated member CRC and the enclosing PNG serialization.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784446096-dcl-fear-josephine-first-fervor-reaction-fixture.png`
- Source SHA-256: `8215A2DA81DC9D5A77494E656B10685215160D742A420046AC31655FBD9EA777`

## Requested CT edits

- `Kleobis-index7` scope `turn`, signature `8207FF5E03502003`: CT `110` -> `68`

## Round-trip proof

| Member | Unit | Record | CT byte | Transition |
| --- | --- | ---: | ---: | ---: |
| `resume_en00_fturn.sav` | `Kleobis-index7` | `0x828F8` | `0x82939` | `110` -> `68` |
| `resume_enbtl_main.sav` | `Kleobis-index7` | `0x828F8` | `0x82939` | `110` -> `68` |

Every unlisted member is byte-identical after pack/unpack. Listed members change only
the enumerated CT bytes and their four-byte inner CRC fields. Current alias equalities and
the battle-entry id remain valid.

## Artifact

- Fixture PNG: `1784451800-dcl-fear-fervor-before-kleobis-index7-reaction-fixture.png`
- Fixture SHA-256: `0D4EAC33273FA4DC02AD00E1CDCB19A7996BAF3512F5119B9E8EDA8B62896B73`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
