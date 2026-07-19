# FFT Enhanced autosave equipped-Reaction fixture

## Scope

This non-deployed fixture changes the selected owner in the current battle aliases only.
Stale `en01`, `en02`, and `enma` members remain byte-identical to the source container.
FF16Tools recomputes each changed member CRC and owns the enclosing PNG serialization.

## Source and identity

- Immutable source autosave: `D:\Projects\FFTGenericChronicle\work\1784104894-fft-autoenhanced-snapshot.png`
- Source autosave SHA-256: `3A6DDE7F777690F3095FB64CC36CAB190E9AB47B0371192FC3551C0435A41CC7`
- Battle entry id: `158`
- Owner: `Rion`; character `0x80`; job `0x59`
- Roster index: `3`; CharaNameKey: `357`
- Live fingerprint: level `71`, Brave `97`, max HP `277`
- Reaction: `442` (Counter) -> `443` (<Unknown>  )
- Live reaction-set transition: `unit+0x96 & 0x08` -> `unit+0x96 & 0x04`; unrelated bits are preserved
- Learned-state policy: explicit test-only bypass for reserved Reaction 443

## Current-member proof

The source preserves the current alias equalities `en00_world == enbtl_world == enwm_main`,
`en00_fturn == enbtl_main`, and `en00_attack == enbtl_attack`. They remain equal after
the audited edit. Current battle-state members agree on the same entry id.

| Member | Targets | Source CRC | Fixture CRC | Changed offsets |
| --- | --- | ---: | ---: | --- |
| `resume_en00_attack.sav` | roster@0x85D3C | `0x04A12136` | `0x1A7F88A3` | 0x4, 0x5, 0x6, 0x7, 0x85D3C |
| `resume_en00_fturn.sav` | roster@0x85D3C, live@0x83B0C/set@0x83B8E, live@0x8430C/set@0x8438E | `0xAF81F409` | `0x53A36293` | 0x4, 0x5, 0x6, 0x7, 0x83B0C, 0x83B8E, 0x8430C, 0x8438E, 0x85D3C |
| `resume_en00_main.sav` | roster@0x85D3C, live@0x83B0C/set@0x83B8E, live@0x8430C/set@0x8438E | `0x1777476F` | `0xEB3CD713` | 0x4, 0x5, 0x6, 0x7, 0x83B0C, 0x83B8E, 0x8430C, 0x8438E, 0x85D3C |
| `resume_en00_world.sav` | roster@0xD8C | `0xC0342E4C` | `0x9EB201A3` | 0x4, 0x5, 0x6, 0x7, 0xD8C |
| `resume_enbtl_attack.sav` | roster@0x85D3C | `0x04A12136` | `0x1A7F88A3` | 0x4, 0x5, 0x6, 0x7, 0x85D3C |
| `resume_enbtl_main.sav` | roster@0x85D3C, live@0x83B0C/set@0x83B8E, live@0x8430C/set@0x8438E | `0xAF81F409` | `0x53A36293` | 0x4, 0x5, 0x6, 0x7, 0x83B0C, 0x83B8E, 0x8430C, 0x8438E, 0x85D3C |
| `resume_enbtl_world.sav` | roster@0xD8C | `0xC0342E4C` | `0x9EB201A3` | 0x4, 0x5, 0x6, 0x7, 0xD8C |
| `resume_enwm_main.sav` | roster@0xD8C | `0xC0342E4C` | `0x9EB201A3` | 0x4, 0x5, 0x6, 0x7, 0xD8C |

Every member not listed in the table is byte-identical after pack/unpack. Every listed
member changes only its four-byte CRC field, the enumerated Reaction words, and the
enumerated live reaction-set bytes. The packed
PNG was unpacked again before these claims were emitted.

## Artifact

- Fixture PNG: `1784157011-synthetic-reaction-carrier443-consistent-fixture.png`
- Fixture PNG SHA-256: `415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`

The fixture remains non-deployed. Restore it only with the stopped-process autosave protocol.
