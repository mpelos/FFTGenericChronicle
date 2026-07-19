# FFT Enhanced autosave status-clear fixture

## Scope

This non-deployed fixture clears only the requested bits from the effective and durable
status arrays of exact live-unit signatures. Status-source bytes are preserved.

## Source

- Source autosave: `D:\Projects\FFTGenericChronicle\work\1784171803-dcl-dual-wield-fast-attack-ct-order-fixture.png`
- Source SHA-256: `02700D2FA95B00EAA55262FE88BFD77FF2ED8610F0ECB57C4D1BFD59C099D5F2`

## Requested clears

- `RionInvisible` signature `8010035900089003`: status byte `2`, mask `0x10`

## Round-trip proof

| Member | Unit | Record | Effective | Master | Source preserved |
| --- | --- | ---: | ---: | ---: | ---: |
| `resume_en00_main.sav` | `RionInvisible` | `0x83AF8` | `0x83B5B` | `0x83CE9` | `0x83B51` |
| `resume_en00_fturn.sav` | `RionInvisible` | `0x83AF8` | `0x83B5B` | `0x83CE9` | `0x83B51` |
| `resume_enbtl_main.sav` | `RionInvisible` | `0x83AF8` | `0x83B5B` | `0x83CE9` | `0x83B51` |

All unlisted members are byte-identical after pack/unpack. Listed members change only
the enumerated effective/master bytes and their recomputed inner CRC fields.

## Artifact

- Fixture PNG: `1784172418-dcl-dual-wield-fast-visible-fixture.png`
- Fixture SHA-256: `B4DC074EA2344168D3F88CBDAB487C381D0A79A457B89A39BC8FF96FA66E0FED`

The fixture remains non-deployed. Restore it only while `FFT_enhanced` is stopped.
