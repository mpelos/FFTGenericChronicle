# Live Forecast Scan

- PID: `19044`
- Near window: `0x800`
- Regions scanned: `760`
- Regions skipped: `2657`
- Bytes scanned: `2,610,176,000`

## Units
- `Ramza` = `0x141855CE0` (cli)
- `Ninja` = `0x141855EE0` (cli)
- `Agrias` = `0x1418560E0` (cli)
- `Cloud` = `0x1418562E0` (cli)
- `Beowulf` = `0x1418564E0` (cli)

## Pattern Hits
- `unit:Ramza:ptr64` hits `4`
- `unit:Ninja:ptr64` hits `4`
- `unit:Agrias:ptr64` hits `4`
- `unit:Cloud:ptr64` hits `12000`
- `unit:Beowulf:ptr64` hits `21`
- `value:damage:u8` hits `12000`
- `value:damage:u16` hits `12000`
- `value:damage:s16` hits `12000`
- `value:damage:u32` hits `12000`
- `value:damage:s32` hits `12000`
- `value:damage:f32` hits `219`
- `value:damage:f64` hits `13`
- `value:chance:u8` hits `12000`
- `value:chance:u16` hits `12000`
- `value:chance:s16` hits `12000`
- `value:chance:u32` hits `12000`
- `value:chance:s32` hits `12000`
- `value:chance:f32` hits `638`
- `value:chance:f64` hits `341`
- `ratio:zodiac:f32` hits `12000`
- `ratio:zodiac:f64` hits `2441`
- `text:Braver:ascii` hits `40`
- `text:Braver:utf16le` hits `0`

## Candidate Clusters
| Start | Span | Score | Kinds | Hits | Region |
| --- | ---: | ---: | --- | --- | --- |
| `0x5B58D08` | `0x88` | `766.5` | `unit, value` | `value:chance:u32@0x5B58D08, value:chance:s32@0x5B58D08, unit:Cloud:ptr64@0x5B58D40, unit:Cloud:ptr64@0x5B58D60, unit:Cloud:ptr64@0x5B58D78, unit:Beowulf:ptr64@0x5B58D90` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x5B58D08` | `0x38` | `746.5` | `unit, value` | `value:chance:u32@0x5B58D08, value:chance:s32@0x5B58D08, unit:Cloud:ptr64@0x5B58D40` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x5B58D08` | `0x58` | `744.5` | `unit, value` | `value:chance:u32@0x5B58D08, value:chance:s32@0x5B58D08, unit:Cloud:ptr64@0x5B58D40, unit:Cloud:ptr64@0x5B58D60` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x129DDE468` | `0x17C` | `726.2` | `unit, value` | `unit:Cloud:ptr64@0x129DDE468, unit:Cloud:ptr64@0x129DDE4C0, value:chance:u32@0x129DDE5E4` | `base=0x129D80000 size=0x60000 protect=0x4 type=0x20000` |
| `0x129DDE468` | `0x17C` | `726.2` | `unit, value` | `unit:Cloud:ptr64@0x129DDE468, unit:Cloud:ptr64@0x129DDE4C0, value:chance:u32@0x129DDE5E4, value:chance:s32@0x129DDE5E4` | `base=0x129D80000 size=0x60000 protect=0x4 type=0x20000` |
| `0x9E0F1E0` | `0x480` | `678.0` | `unit, value` | `value:damage:u16@0x9E0F1E0, value:damage:s16@0x9E0F1E0, unit:Cloud:ptr64@0x9E0F660` | `base=0x9E07000 size=0x9000 protect=0x4 type=0x20000` |
| `0x9E0F1E0` | `0x680` | `646.0` | `unit, value` | `value:damage:u16@0x9E0F1E0, value:damage:s16@0x9E0F1E0, unit:Cloud:ptr64@0x9E0F660, unit:Cloud:ptr64@0x9E0F860` | `base=0x9E07000 size=0x9000 protect=0x4 type=0x20000` |
| `0x15E4D3EE0` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E4D3EE0, unit:Cloud:ptr64@0x15E4D3FF0, unit:Cloud:ptr64@0x15E4D41A8, unit:Cloud:ptr64@0x15E4D42B8, value:chance:f32@0x15E4D4466, unit:Cloud:ptr64@0x15E4D4470, unit:Cloud:ptr64@0x15E4D4580` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E4D41A8` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E4D41A8, unit:Cloud:ptr64@0x15E4D42B8, value:chance:f32@0x15E4D4466, unit:Cloud:ptr64@0x15E4D4470, unit:Cloud:ptr64@0x15E4D4580, unit:Cloud:ptr64@0x15E4D4738, unit:Cloud:ptr64@0x15E4D4848` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E713D18` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E713D18, unit:Cloud:ptr64@0x15E713E28, unit:Cloud:ptr64@0x15E713FE0, unit:Cloud:ptr64@0x15E7140F0, unit:Cloud:ptr64@0x15E7142A8, value:damage:f32@0x15E7142FE, unit:Cloud:ptr64@0x15E7143B8` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E713FE0` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E713FE0, unit:Cloud:ptr64@0x15E7140F0, unit:Cloud:ptr64@0x15E7142A8, value:damage:f32@0x15E7142FE, unit:Cloud:ptr64@0x15E7143B8, unit:Cloud:ptr64@0x15E714570, unit:Cloud:ptr64@0x15E714680` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7142A8` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E7142A8, value:damage:f32@0x15E7142FE, unit:Cloud:ptr64@0x15E7143B8, unit:Cloud:ptr64@0x15E714570, unit:Cloud:ptr64@0x15E714680, unit:Cloud:ptr64@0x15E714838, unit:Cloud:ptr64@0x15E714948` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E733EB0` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E733EB0, unit:Cloud:ptr64@0x15E733FC0, unit:Cloud:ptr64@0x15E734178, unit:Cloud:ptr64@0x15E734288, value:damage:f32@0x15E734436, unit:Cloud:ptr64@0x15E734440, unit:Cloud:ptr64@0x15E734550` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E734178` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E734178, unit:Cloud:ptr64@0x15E734288, value:damage:f32@0x15E734436, unit:Cloud:ptr64@0x15E734440, unit:Cloud:ptr64@0x15E734550, unit:Cloud:ptr64@0x15E734708, unit:Cloud:ptr64@0x15E734818` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E793D48` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E793D48, unit:Cloud:ptr64@0x15E793E58, unit:Cloud:ptr64@0x15E794010, unit:Cloud:ptr64@0x15E794120, unit:Cloud:ptr64@0x15E7942D8, value:chance:f32@0x15E79432E, unit:Cloud:ptr64@0x15E7943E8` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E794010` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E794010, unit:Cloud:ptr64@0x15E794120, unit:Cloud:ptr64@0x15E7942D8, value:chance:f32@0x15E79432E, unit:Cloud:ptr64@0x15E7943E8, unit:Cloud:ptr64@0x15E7945A0, unit:Cloud:ptr64@0x15E7946B0` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7942D8` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E7942D8, value:chance:f32@0x15E79432E, unit:Cloud:ptr64@0x15E7943E8, unit:Cloud:ptr64@0x15E7945A0, unit:Cloud:ptr64@0x15E7946B0, unit:Cloud:ptr64@0x15E794868, unit:Cloud:ptr64@0x15E794978` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7B3EE0` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E7B3EE0, unit:Cloud:ptr64@0x15E7B3FF0, unit:Cloud:ptr64@0x15E7B41A8, unit:Cloud:ptr64@0x15E7B42B8, value:chance:f32@0x15E7B4466, unit:Cloud:ptr64@0x15E7B4470, unit:Cloud:ptr64@0x15E7B4580` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7B41A8` | `0x6A0` | `644.0` | `unit, value` | `unit:Cloud:ptr64@0x15E7B41A8, unit:Cloud:ptr64@0x15E7B42B8, value:chance:f32@0x15E7B4466, unit:Cloud:ptr64@0x15E7B4470, unit:Cloud:ptr64@0x15E7B4580, unit:Cloud:ptr64@0x15E7B4738, unit:Cloud:ptr64@0x15E7B4848` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E734436` | `0x6AA` | `643.4` | `unit, value` | `value:damage:f32@0x15E734436, unit:Cloud:ptr64@0x15E734440, unit:Cloud:ptr64@0x15E734550, unit:Cloud:ptr64@0x15E734708, unit:Cloud:ptr64@0x15E734818, unit:Cloud:ptr64@0x15E7349D0, unit:Cloud:ptr64@0x15E734AE0` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7B4466` | `0x6AA` | `643.4` | `unit, value` | `value:chance:f32@0x15E7B4466, unit:Cloud:ptr64@0x15E7B4470, unit:Cloud:ptr64@0x15E7B4580, unit:Cloud:ptr64@0x15E7B4738, unit:Cloud:ptr64@0x15E7B4848, unit:Cloud:ptr64@0x15E7B4A00, unit:Cloud:ptr64@0x15E7B4B10` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x9E0F660` | `0x6DA` | `640.4` | `unit, value` | `unit:Cloud:ptr64@0x9E0F660, unit:Cloud:ptr64@0x9E0F860, unit:Cloud:ptr64@0x9E0FA28, unit:Cloud:ptr64@0x9E0FB20, value:chance:u32@0x9E0FD3A` | `base=0x9E07000 size=0x9000 protect=0x4 type=0x20000` |
| `0x9E0F660` | `0x6DA` | `640.4` | `unit, value` | `unit:Cloud:ptr64@0x9E0F660, unit:Cloud:ptr64@0x9E0F860, unit:Cloud:ptr64@0x9E0FA28, unit:Cloud:ptr64@0x9E0FB20, value:chance:u32@0x9E0FD3A, value:chance:s32@0x9E0FD3A` | `base=0x9E07000 size=0x9000 protect=0x4 type=0x20000` |
| `0x15E4D3D28` | `0x73E` | `634.1` | `unit, value` | `unit:Cloud:ptr64@0x15E4D3D28, unit:Cloud:ptr64@0x15E4D3EE0, unit:Cloud:ptr64@0x15E4D3FF0, unit:Cloud:ptr64@0x15E4D41A8, unit:Cloud:ptr64@0x15E4D42B8, value:chance:f32@0x15E4D4466` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E733CF8` | `0x73E` | `634.1` | `unit, value` | `unit:Cloud:ptr64@0x15E733CF8, unit:Cloud:ptr64@0x15E733EB0, unit:Cloud:ptr64@0x15E733FC0, unit:Cloud:ptr64@0x15E734178, unit:Cloud:ptr64@0x15E734288, value:damage:f32@0x15E734436` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7B3D28` | `0x73E` | `634.1` | `unit, value` | `unit:Cloud:ptr64@0x15E7B3D28, unit:Cloud:ptr64@0x15E7B3EE0, unit:Cloud:ptr64@0x15E7B3FF0, unit:Cloud:ptr64@0x15E7B41A8, unit:Cloud:ptr64@0x15E7B42B8, value:chance:f32@0x15E7B4466` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E4D3FF0` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E4D3FF0, unit:Cloud:ptr64@0x15E4D41A8, unit:Cloud:ptr64@0x15E4D42B8, value:chance:f32@0x15E4D4466, unit:Cloud:ptr64@0x15E4D4470, unit:Cloud:ptr64@0x15E4D4580, unit:Cloud:ptr64@0x15E4D4738` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E4D42B8` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E4D42B8, value:chance:f32@0x15E4D4466, unit:Cloud:ptr64@0x15E4D4470, unit:Cloud:ptr64@0x15E4D4580, unit:Cloud:ptr64@0x15E4D4738, unit:Cloud:ptr64@0x15E4D4848, unit:Cloud:ptr64@0x15E4D4A00` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E713E28` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E713E28, unit:Cloud:ptr64@0x15E713FE0, unit:Cloud:ptr64@0x15E7140F0, unit:Cloud:ptr64@0x15E7142A8, value:damage:f32@0x15E7142FE, unit:Cloud:ptr64@0x15E7143B8, unit:Cloud:ptr64@0x15E714570` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7140F0` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E7140F0, unit:Cloud:ptr64@0x15E7142A8, value:damage:f32@0x15E7142FE, unit:Cloud:ptr64@0x15E7143B8, unit:Cloud:ptr64@0x15E714570, unit:Cloud:ptr64@0x15E714680, unit:Cloud:ptr64@0x15E714838` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E733CF8` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E733CF8, unit:Cloud:ptr64@0x15E733EB0, unit:Cloud:ptr64@0x15E733FC0, unit:Cloud:ptr64@0x15E734178, unit:Cloud:ptr64@0x15E734288, value:damage:f32@0x15E734436, unit:Cloud:ptr64@0x15E734440` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E733FC0` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E733FC0, unit:Cloud:ptr64@0x15E734178, unit:Cloud:ptr64@0x15E734288, value:damage:f32@0x15E734436, unit:Cloud:ptr64@0x15E734440, unit:Cloud:ptr64@0x15E734550, unit:Cloud:ptr64@0x15E734708` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E734288` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E734288, value:damage:f32@0x15E734436, unit:Cloud:ptr64@0x15E734440, unit:Cloud:ptr64@0x15E734550, unit:Cloud:ptr64@0x15E734708, unit:Cloud:ptr64@0x15E734818, unit:Cloud:ptr64@0x15E7349D0` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E793E58` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E793E58, unit:Cloud:ptr64@0x15E794010, unit:Cloud:ptr64@0x15E794120, unit:Cloud:ptr64@0x15E7942D8, value:chance:f32@0x15E79432E, unit:Cloud:ptr64@0x15E7943E8, unit:Cloud:ptr64@0x15E7945A0` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E794120` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E794120, unit:Cloud:ptr64@0x15E7942D8, value:chance:f32@0x15E79432E, unit:Cloud:ptr64@0x15E7943E8, unit:Cloud:ptr64@0x15E7945A0, unit:Cloud:ptr64@0x15E7946B0, unit:Cloud:ptr64@0x15E794868` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7B3FF0` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E7B3FF0, unit:Cloud:ptr64@0x15E7B41A8, unit:Cloud:ptr64@0x15E7B42B8, value:chance:f32@0x15E7B4466, unit:Cloud:ptr64@0x15E7B4470, unit:Cloud:ptr64@0x15E7B4580, unit:Cloud:ptr64@0x15E7B4738` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E7B42B8` | `0x748` | `633.5` | `unit, value` | `unit:Cloud:ptr64@0x15E7B42B8, value:chance:f32@0x15E7B4466, unit:Cloud:ptr64@0x15E7B4470, unit:Cloud:ptr64@0x15E7B4580, unit:Cloud:ptr64@0x15E7B4738, unit:Cloud:ptr64@0x15E7B4848, unit:Cloud:ptr64@0x15E7B4A00` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E4D4466` | `0x74A` | `633.4` | `unit, value` | `value:chance:f32@0x15E4D4466, unit:Cloud:ptr64@0x15E4D4470, unit:Cloud:ptr64@0x15E4D4580, unit:Cloud:ptr64@0x15E4D4738, unit:Cloud:ptr64@0x15E4D4848, unit:Cloud:ptr64@0x15E4D4A00, unit:Cloud:ptr64@0x15E4D4BB0` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x9E0F660` | `0x76A` | `631.4` | `unit, value` | `unit:Cloud:ptr64@0x9E0F660, unit:Cloud:ptr64@0x9E0F860, unit:Cloud:ptr64@0x9E0FA28, unit:Cloud:ptr64@0x9E0FB20, value:chance:u32@0x9E0FD3A, value:chance:s32@0x9E0FD3A, value:chance:u32@0x9E0FDCA` | `base=0x9E07000 size=0x9000 protect=0x4 type=0x20000` |
| `0x15E713B60` | `0x79E` | `628.1` | `unit, value` | `unit:Cloud:ptr64@0x15E713B60, unit:Cloud:ptr64@0x15E713D18, unit:Cloud:ptr64@0x15E713E28, unit:Cloud:ptr64@0x15E713FE0, unit:Cloud:ptr64@0x15E7140F0, unit:Cloud:ptr64@0x15E7142A8, value:damage:f32@0x15E7142FE` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
| `0x15E793B90` | `0x79E` | `628.1` | `unit, value` | `unit:Cloud:ptr64@0x15E793B90, unit:Cloud:ptr64@0x15E793D48, unit:Cloud:ptr64@0x15E793E58, unit:Cloud:ptr64@0x15E794010, unit:Cloud:ptr64@0x15E794120, unit:Cloud:ptr64@0x15E7942D8, value:chance:f32@0x15E79432E` | `base=0x15E000000 size=0x1601000 protect=0x4 type=0x20000` |
