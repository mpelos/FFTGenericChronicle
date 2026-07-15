# FFT Enhanced manual-save equipped-Reaction fixture

## Scope

This fixture is generated outside the live save directory. It replaces exactly one
equipped-Reaction word on one unit; FF16Tools owns the enclosing PNG and payload checksum.

## Sources

- Immutable source save: `D:\Projects\FFTGenericChronicle\work\1784101683-lt23-save05-josephine-counter-learned-verified-fixture.png`
- Source save SHA-256: `342EBC4F96705AB0285E502B6B94D1371DBD7FACB7B1A7D5573D8BB80AAF38D7`
- Source unpacked payload SHA-256: `C936DA5344D9D4C5815811FE7BBAE4D631FE5E11271C139542221F03EC9AFE8B`
- Ability baseline: `D:\Projects\FFTGenericChronicle\work\wotl_ability_action_baseline.csv`
- Ability baseline SHA-256: `53F73C4D9A2357A09E855FE902D4B3337D5DDF04FBA7952EEE62E5D333E0992D`

## Selected unit and Reaction

- Manual slot: `5` (zero-based index `4`)
- Unit slot: `3`; active: `true`
- Character byte: `0x80`; job: `0x59`
- Nickname: `Rion`; CharaNameKey: `357`
- Source Reaction: `439` (Gil Snapper)
- Fixture Reaction: `442` (Counter)
- Learned in job: `0x4E`; R/S/M slot: `2`
- Learned-byte proof: absolute `0x28000` = `0xF8` includes mask `0x40`

## Exact payload delta

- Equipped-Reaction word absolute offset: `0x27FC8`
- Unit-relative offset: `+0x08`
- Word: `439` -> `442`
- Repacked checksum: `0x04B9637F` over payload bytes `[0x10:]`
- All changed offsets: `0x4, 0x5, 0x6, 0x7, 0x27FC8`

Every payload byte outside the four-byte checksum field and the selected Reaction word is
identical to the immutable source. Re-unpacking the packed PNG reproduced the audited payload
exactly; no live save file was read for writing or replaced.

## Artifacts

- Fixture PNG: `1784104009-lt23-save05-rion-and-josephine-counter-fixture.png`
- Fixture PNG SHA-256: `17D2C4C66469CA685E27BDC6932D10BE3D68549444FB812D280062D97CE7BB33`
- Re-unpacked fixture payload: `1784104009-lt23-save05-rion-and-josephine-counter-fixture.bin`
- Fixture payload SHA-256: `7C6924911379A7676E9F04C2A018F74520C51C4695018EDEF822E556D1BA2D3C`

The fixture remains non-deployed. Copy it into the live manual-save path only behind a
separate backup/restore protocol while `FFT_enhanced.exe` is stopped.
