# FFT Enhanced manual-save learned-ability fixture

## Scope

This fixture is generated outside the live save directory. It enables exactly one learned
active-ability bit on one unit; FF16Tools owns the enclosing PNG and payload checksum.

## Sources

- Immutable source save: `D:\Projects\FFTGenericChronicle\work\1784092200-lt37-save05-death-fixture.png`
- Source save SHA-256: `3ADD541A8A57441F1B24FCB5AF0B832E3553D8C56D22E84D1FB7A1D77B10043F`
- Source unpacked payload SHA-256: `A68FCE250E09AA2118175F77C15D6AB7616536AE02D02F662C9119C12A752C40`
- JobData: `C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData\JobData.xml`
- JobData SHA-256: `63E49670E9F237824AA0FA0B4C333280BC8CBD041440FCF1638BDB5E35CF7F2B`
- JobCommandData: `C:\Reloaded-II\Mods\fftivc.utility.modloader\TableData\JobCommandData.xml`
- JobCommandData SHA-256: `FE5B5DCA757284159E91802FD6D2D0AFAA6BA3DF48BE50768FEA25E41165DE5B`

## Selected unit and ability

- Manual slot: `5` (zero-based index `4`)
- Unit slot: `1`; active: `true`
- Character byte: `0x80`; job: `0x52`
- Nickname: `Arthur`; CharaNameKey: `336`
- Current combat set: `0xFF`
- Accessible command ids: `13, 10`
- Ability id: `5`; selected command id: `10`
- Active-ability index: `4`; ability-flag job slot: `5`

## Exact payload delta

- Learned byte absolute offset: `0x27B51`
- Learned mask: `0x10`
- Learned byte: `0xEC` -> `0xFC`
- Repacked checksum: `0x1079ABB3` over payload bytes `[0x10:]`
- All changed offsets: `0x4, 0x5, 0x6, 0x7, 0x27B51`

Every payload byte outside the four-byte checksum field and the selected learned byte is
identical to the immutable source. Re-unpacking the packed PNG reproduced the audited payload
exactly; no live save file was read for writing or replaced.

## Artifacts

- Fixture PNG: `1784095048-lt38-save05-death-raise-fixture.png`
- Fixture PNG SHA-256: `7E58FE8EC1A623C709EC090C58A81B5849F729A421557D4C86E3C1184637BA08`
- Re-unpacked fixture payload: `1784095048-lt38-save05-death-raise-fixture.bin`
- Fixture payload SHA-256: `50529598F679FE845C3A3E8E2FF3157EC36023FCD47B2564795415B9C01FD938`

The fixture remains non-deployed. Copy it into the live manual-save path only behind a
separate backup/restore protocol while `FFT_enhanced.exe` is stopped.
