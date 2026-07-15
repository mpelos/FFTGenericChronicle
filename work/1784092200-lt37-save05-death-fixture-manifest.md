# FFT Enhanced manual-save learned-ability fixture

## Scope

This fixture is generated outside the live save directory. It enables exactly one learned
active-ability bit on one unit; FF16Tools owns the enclosing PNG and payload checksum.

## Sources

- Immutable source save: `C:\Users\mmpel\OneDrive\Documentos\My Games\FINAL FANTASY TACTICS - The Ivalice Chronicles\Steam\76561198044337912\enhanced.png`
- Source save SHA-256: `72EEBE31D0EFA64D9D710E572A2591112B27803C2336B4548931FF9C1F44A7C4`
- Source unpacked payload SHA-256: `A4BAA74CB3F20C6177EADA50AA0CCEA8A6C519C6B2B3608B91C40367CFF6626D`
- JobData: `C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData\JobData.xml`
- JobData SHA-256: `63E49670E9F237824AA0FA0B4C333280BC8CBD041440FCF1638BDB5E35CF7F2B`
- JobCommandData: `C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData\JobCommandData.xml`
- JobCommandData SHA-256: `FE5B5DCA757284159E91802FD6D2D0AFAA6BA3DF48BE50768FEA25E41165DE5B`

## Selected unit and ability

- Manual slot: `5` (zero-based index `4`)
- Unit slot: `6`; active: `true`
- Character byte: `0x81`; job: `0x50`
- Nickname: `<empty>`; CharaNameKey: `637`
- Current combat set: `0xFF`
- Accessible command ids: `11, 19`
- Ability id: `30`; selected command id: `11`
- Active-ability index: `14`; ability-flag job slot: `6`

## Exact payload delta

- Learned byte absolute offset: `0x2870D`
- Learned mask: `0x40`
- Learned byte: `0x8F` -> `0xCF`
- Repacked checksum: `0x588B56AA` over payload bytes `[0x10:]`
- All changed offsets: `0x4, 0x6, 0x7, 0x2870D`

Every payload byte outside the four-byte checksum field and the selected learned byte is
identical to the immutable source. Re-unpacking the packed PNG reproduced the audited payload
exactly; no live save file was read for writing or replaced.

## Artifacts

- Fixture PNG: `1784092200-lt37-save05-death-fixture.png`
- Fixture PNG SHA-256: `3ADD541A8A57441F1B24FCB5AF0B832E3553D8C56D22E84D1FB7A1D77B10043F`
- Re-unpacked fixture payload: `1784092200-lt37-save05-death-fixture.bin`
- Fixture payload SHA-256: `A68FCE250E09AA2118175F77C15D6AB7616536AE02D02F662C9119C12A752C40`

The fixture remains non-deployed. Copy it into the live manual-save path only behind a
separate backup/restore protocol while `FFT_enhanced.exe` is stopped.
