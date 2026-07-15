# FFT Enhanced manual-save equipped-Reaction fixture

## Scope

This fixture is generated outside the live save directory. It replaces exactly one
equipped-Reaction word on one unit; FF16Tools owns the enclosing PNG and payload checksum.

## Sources

- Immutable source save: `D:\Projects\FFTGenericChronicle\work\1784095048-lt38-save05-death-raise-fixture.png`
- Source save SHA-256: `7E58FE8EC1A623C709EC090C58A81B5849F729A421557D4C86E3C1184637BA08`
- Source unpacked payload SHA-256: `50529598F679FE845C3A3E8E2FF3157EC36023FCD47B2564795415B9C01FD938`
- Ability baseline: `D:\Projects\FFTGenericChronicle\work\wotl_ability_action_baseline.csv`
- Ability baseline SHA-256: `53F73C4D9A2357A09E855FE902D4B3337D5DDF04FBA7952EEE62E5D333E0992D`

## Selected unit and Reaction

- Manual slot: `5` (zero-based index `4`)
- Unit slot: `6`; active: `true`
- Character byte: `0x81`; job: `0x50`
- Nickname: `<empty>`; CharaNameKey: `637`
- Source Reaction: `451` (Shirahadori)
- Fixture Reaction: `442` (Counter)
- Learned in job: `0x4E`; R/S/M slot: `2`
- Learned-byte proof: absolute `0x28708` = `0x40` includes mask `0x40`

## Exact payload delta

- Equipped-Reaction word absolute offset: `0x286D0`
- Unit-relative offset: `+0x08`
- Word: `451` -> `442`
- Repacked checksum: `0xE843AB50` over payload bytes `[0x10:]`
- All changed offsets: `0x4, 0x6, 0x7, 0x286D0`

Every payload byte outside the four-byte checksum field and the selected Reaction word is
identical to the immutable source. Re-unpacking the packed PNG reproduced the audited payload
exactly; no live save file was read for writing or replaced.

## Artifacts

- Fixture PNG: `1784101683-lt23-save05-josephine-counter-learned-verified-fixture.png`
- Fixture PNG SHA-256: `342EBC4F96705AB0285E502B6B94D1371DBD7FACB7B1A7D5573D8BB80AAF38D7`
- Re-unpacked fixture payload: `1784101683-lt23-save05-josephine-counter-learned-verified-fixture.bin`
- Fixture payload SHA-256: `C936DA5344D9D4C5815811FE7BBAE4D631FE5E11271C139542221F03EC9AFE8B`

The fixture remains non-deployed. Copy it into the live manual-save path only behind a
separate backup/restore protocol while `FFT_enhanced.exe` is stopped.
