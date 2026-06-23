# Live Named Value Records

- PID: `19044`
- Required: `braverId, damage`
- Near window: `0x300`
- Max span: `0x500`
- Regions scanned: `761`
- Bytes scanned: `2,613,559,296`

## Pattern Hits
- `value:braverId:u16` hits `12000`
- `value:braverId:s16` hits `12000`
- `value:braverId:u32` hits `12000`
- `value:braverId:s32` hits `12000`
- `value:damage:u16` hits `12000`
- `value:damage:s16` hits `12000`
- `value:damage:u32` hits `12000`
- `value:damage:s32` hits `12000`
- `value:charge:u16` hits `12000`
- `value:charge:s16` hits `12000`
- `value:charge:u32` hits `12000`
- `value:charge:s32` hits `12000`
- `value:skillsetLimit:u16` hits `12000`
- `value:skillsetLimit:s16` hits `12000`
- `value:skillsetLimit:u32` hits `12000`
- `value:skillsetLimit:s32` hits `12000`

## Candidate Records
| Start | Span | Score | Names | Hits | Region |
| --- | ---: | ---: | --- | --- | --- |
| `0x75A56E` | `0x2` | `1227.5` | `damage, braverId` | `value:damage:u16@0x75A56E, value:damage:s16@0x75A56E, value:braverId:u16@0x75A570` | `base=0x6E0000 size=0xD3000 protect=0x2 type=0x40000` |
| `0x428665` | `0x3` | `1227.2` | `braverId, damage` | `value:braverId:u16@0x428665, value:braverId:s16@0x428665, value:damage:u16@0x428668` | `base=0x420000 size=0xA000 protect=0x2 type=0x40000` |
| `0x5BB3ACD` | `0x3` | `1227.2` | `damage, braverId` | `value:damage:u16@0x5BB3ACD, value:damage:s16@0x5BB3ACD, value:braverId:u16@0x5BB3AD0` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x5EF798` | `0x4` | `1227.0` | `braverId, damage` | `value:braverId:u16@0x5EF798, value:braverId:s16@0x5EF798, value:damage:u16@0x5EF79C` | `base=0x5E0000 size=0xFF000 protect=0x4 type=0x20000` |
| `0x3C910D5` | `0x4` | `1227.0` | `damage, braverId` | `value:damage:u16@0x3C910D5, value:damage:s16@0x3C910D5, value:braverId:u16@0x3C910D9` | `base=0x3BE0000 size=0x1FF000 protect=0x4 type=0x20000` |
| `0x73BE505` | `0x4` | `1227.0` | `damage, braverId` | `value:damage:u16@0x73BE505, value:damage:s16@0x73BE505, value:braverId:u16@0x73BE509` | `base=0x73BE000 size=0xC3000 protect=0x4 type=0x20000` |
| `0x743F415` | `0x4` | `1227.0` | `damage, braverId` | `value:damage:u16@0x743F415, value:damage:s16@0x743F415, value:braverId:u16@0x743F419` | `base=0x73BE000 size=0xC3000 protect=0x4 type=0x20000` |
| `0x60CF20C` | `0x5` | `1226.8` | `braverId, damage` | `value:braverId:u16@0x60CF20C, value:braverId:s16@0x60CF20C, value:damage:u16@0x60CF211` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x53D106C` | `0x7` | `1226.2` | `damage, braverId` | `value:damage:u16@0x53D106C, value:damage:s16@0x53D106C, value:braverId:u16@0x53D1073` | `base=0x5395000 size=0x7F000 protect=0x4 type=0x20000` |
| `0x5B1B4639` | `0x9` | `1225.8` | `braverId, damage` | `value:braverId:u32@0x5B1B4639, value:braverId:s32@0x5B1B4639, value:damage:u16@0x5B1B4642` | `base=0x5B1B4000 size=0x196000 protect=0x4 type=0x20000` |
| `0x53F18E36` | `0xE` | `1224.5` | `braverId, damage` | `value:braverId:u32@0x53F18E36, value:braverId:s32@0x53F18E36, value:damage:u16@0x53F18E44` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x5786C936` | `0xE` | `1224.5` | `braverId, damage` | `value:braverId:u32@0x5786C936, value:braverId:s32@0x5786C936, value:damage:u16@0x5786C944` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x301EE616` | `0xF` | `1224.2` | `damage, braverId` | `value:damage:u16@0x301EE616, value:damage:s16@0x301EE616, value:braverId:u16@0x301EE625` | `base=0x30120000 size=0x20000000 protect=0x4 type=0x40000` |
| `0x518016DC` | `0x11` | `1223.8` | `braverId, damage` | `value:braverId:u32@0x518016DC, value:braverId:s32@0x518016DC, value:damage:u16@0x518016ED` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x5B83631C` | `0x11` | `1223.8` | `braverId, damage` | `value:braverId:u32@0x5B83631C, value:braverId:s32@0x5B83631C, value:damage:u16@0x5B83632D` | `base=0x5B82A000 size=0xE1000 protect=0x4 type=0x20000` |
| `0x50E49F29` | `0x15` | `1222.8` | `braverId, damage` | `value:braverId:u16@0x50E49F29, value:braverId:s16@0x50E49F29, value:damage:u16@0x50E49F3E` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x5141118B` | `0x15` | `1222.8` | `damage, braverId` | `value:damage:u16@0x5141118B, value:damage:s16@0x5141118B, value:braverId:u16@0x514111A0` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x514B0EDB` | `0x15` | `1222.8` | `damage, braverId` | `value:damage:u16@0x514B0EDB, value:damage:s16@0x514B0EDB, value:braverId:u16@0x514B0EF0` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x5ABB6578` | `0x15` | `1222.8` | `braverId, damage` | `value:braverId:u32@0x5ABB6578, value:braverId:s32@0x5ABB6578, value:damage:u16@0x5ABB658D` | `base=0x5AAC5000 size=0x1BA000 protect=0x4 type=0x20000` |
| `0x53F1954A` | `0x1A` | `1221.5` | `braverId, damage` | `value:braverId:u32@0x53F1954A, value:braverId:s32@0x53F1954A, value:damage:u16@0x53F19564` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x5786D04A` | `0x1A` | `1221.5` | `braverId, damage` | `value:braverId:u32@0x5786D04A, value:braverId:s32@0x5786D04A, value:damage:u16@0x5786D064` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x51932D2D` | `0x1B` | `1221.2` | `damage, braverId` | `value:damage:u16@0x51932D2D, value:damage:s16@0x51932D2D, value:braverId:u32@0x51932D48` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x8130642` | `0x1E` | `1220.5` | `braverId, damage` | `value:braverId:u16@0x8130642, value:braverId:s16@0x8130642, value:damage:u16@0x8130660` | `base=0x80C0000 size=0x13E000 protect=0x4 type=0x20000` |
| `0xEFE209` | `0x1F` | `1220.2` | `damage, braverId` | `value:damage:u16@0xEFE209, value:damage:s16@0xEFE209, value:braverId:u16@0xEFE228` | `base=0xEB0000 size=0xBC000 protect=0x2 type=0x40000` |
| `0x6138AE2` | `0x26` | `1218.5` | `damage, braverId` | `value:damage:u16@0x6138AE2, value:damage:s16@0x6138AE2, value:braverId:u16@0x6138B08` | `base=0x6115000 size=0x6A000 protect=0x4 type=0x20000` |
| `0x5CA1998` | `0x28` | `1218.0` | `braverId, damage` | `value:braverId:u16@0x5CA1998, value:braverId:s16@0x5CA1998, value:damage:u16@0x5CA19C0` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x5D82DF8` | `0x28` | `1218.0` | `braverId, damage` | `value:braverId:u16@0x5D82DF8, value:braverId:s16@0x5D82DF8, value:damage:u16@0x5D82E20` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x81692A0` | `0x2C` | `1217.0` | `damage, braverId` | `value:damage:u16@0x81692A0, value:damage:s16@0x81692A0, value:braverId:u16@0x81692CC` | `base=0x80C0000 size=0x13E000 protect=0x4 type=0x20000` |
| `0x50E49C29` | `0x2D` | `1216.8` | `braverId, damage` | `value:braverId:u16@0x50E49C29, value:braverId:s16@0x50E49C29, value:damage:u16@0x50E49C56` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x53E1DDAE` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53E1DDAE, value:braverId:s32@0x53E1DDAE, value:damage:u16@0x53E1DDDC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53E1DF2E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53E1DF2E, value:braverId:s32@0x53E1DF2E, value:damage:u16@0x53E1DF5C` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53E2DEAE` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53E2DEAE, value:braverId:s32@0x53E2DEAE, value:damage:u16@0x53E2DEDC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53E2E02E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53E2E02E, value:braverId:s32@0x53E2E02E, value:damage:u16@0x53E2E05C` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53E8B68E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53E8B68E, value:braverId:s32@0x53E8B68E, value:damage:u16@0x53E8B6BC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53E8B88E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53E8B88E, value:braverId:s32@0x53E8B88E, value:damage:u16@0x53E8B8BC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53E8E4AE` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53E8E4AE, value:braverId:s32@0x53E8E4AE, value:damage:u16@0x53E8E4DC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53E8E62E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53E8E62E, value:braverId:s32@0x53E8E62E, value:damage:u16@0x53E8E65C` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53EAB88E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53EAB88E, value:braverId:s32@0x53EAB88E, value:damage:u16@0x53EAB8BC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53EABA8E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53EABA8E, value:braverId:s32@0x53EABA8E, value:damage:u16@0x53EABABC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53F1AF8E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53F1AF8E, value:braverId:s32@0x53F1AF8E, value:damage:u16@0x53F1AFBC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53F1B18E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53F1B18E, value:braverId:s32@0x53F1B18E, value:damage:u16@0x53F1B1BC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53F4B28E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53F4B28E, value:braverId:s32@0x53F4B28E, value:damage:u16@0x53F4B2BC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x53F4B48E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x53F4B48E, value:braverId:s32@0x53F4B48E, value:damage:u16@0x53F4B4BC` | `base=0x52430000 size=0x1FF9000 protect=0x404 type=0x20000` |
| `0x577DE18E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x577DE18E, value:braverId:s32@0x577DE18E, value:damage:u16@0x577DE1BC` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x577DE38E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x577DE38E, value:braverId:s32@0x577DE38E, value:damage:u16@0x577DE3BC` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x577F095E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x577F095E, value:braverId:s32@0x577F095E, value:damage:u16@0x577F098C` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x577F0BDE` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x577F0BDE, value:braverId:s32@0x577F0BDE, value:damage:u16@0x577F0C0C` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x5781132E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x5781132E, value:braverId:s32@0x5781132E, value:damage:u16@0x5781135C` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x578114AE` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x578114AE, value:braverId:s32@0x578114AE, value:damage:u16@0x578114DC` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x5781E58E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x5781E58E, value:braverId:s32@0x5781E58E, value:damage:u16@0x5781E5BC` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x5781E78E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x5781E78E, value:braverId:s32@0x5781E78E, value:damage:u16@0x5781E7BC` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x5786EA8E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x5786EA8E, value:braverId:s32@0x5786EA8E, value:damage:u16@0x5786EABC` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x5786EC8E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x5786EC8E, value:braverId:s32@0x5786EC8E, value:damage:u16@0x5786ECBC` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x578718AE` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x578718AE, value:braverId:s32@0x578718AE, value:damage:u16@0x578718DC` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0x57871A2E` | `0x2E` | `1216.5` | `braverId, damage` | `value:braverId:u32@0x57871A2E, value:braverId:s32@0x57871A2E, value:damage:u16@0x57871A5C` | `base=0x56430000 size=0x1FF0000 protect=0x404 type=0x20000` |
| `0xEBBF12` | `0x2F` | `1216.2` | `damage, braverId` | `value:damage:u16@0xEBBF12, value:damage:s16@0xEBBF12, value:braverId:u16@0xEBBF41` | `base=0xEB0000 size=0xBC000 protect=0x2 type=0x40000` |
| `0xEC3612` | `0x2F` | `1216.2` | `damage, braverId` | `value:damage:u16@0xEC3612, value:damage:s16@0xEC3612, value:braverId:u16@0xEC3641` | `base=0xEB0000 size=0xBC000 protect=0x2 type=0x40000` |
| `0x5101A4D8` | `0x30` | `1216.0` | `braverId, damage` | `value:braverId:u16@0x5101A4D8, value:braverId:s16@0x5101A4D8, value:damage:u16@0x5101A508` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x5125C8A8` | `0x30` | `1216.0` | `damage, braverId` | `value:damage:u16@0x5125C8A8, value:damage:s16@0x5125C8A8, value:braverId:u16@0x5125C8D8` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x6E30C8` | `0x31` | `1215.8` | `damage, braverId` | `value:damage:u16@0x6E30C8, value:damage:s16@0x6E30C8, value:braverId:u16@0x6E30F9` | `base=0x6E0000 size=0xD3000 protect=0x2 type=0x40000` |
| `0x5136AD32` | `0x36` | `1214.5` | `damage, braverId` | `value:damage:u16@0x5136AD32, value:damage:s16@0x5136AD32, value:braverId:u16@0x5136AD68` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x50E2D1B8` | `0x3C` | `1213.0` | `braverId, damage` | `value:braverId:u16@0x50E2D1B8, value:braverId:s16@0x50E2D1B8, value:damage:u16@0x50E2D1F4` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x5B1B667D` | `0x3C` | `1213.0` | `damage, braverId` | `value:damage:u16@0x5B1B667D, value:damage:s16@0x5B1B667D, value:braverId:u32@0x5B1B66B9` | `base=0x5B1B4000 size=0x196000 protect=0x4 type=0x20000` |
| `0x768C98E` | `0x3E` | `1212.5` | `braverId, damage` | `value:braverId:u16@0x768C98E, value:braverId:s16@0x768C98E, value:damage:u16@0x768C9CC` | `base=0x75C8000 size=0x1AB000 protect=0x4 type=0x20000` |
| `0x530DD80` | `0x45` | `1210.8` | `damage, braverId` | `value:damage:u16@0x530DD80, value:damage:s16@0x530DD80, value:braverId:u16@0x530DDC5` | `base=0x52D5000 size=0xBF000 protect=0x4 type=0x20000` |
| `0x5D0F788` | `0x45` | `1210.8` | `braverId, damage` | `value:braverId:u16@0x5D0F788, value:braverId:s16@0x5D0F788, value:damage:u16@0x5D0F7CD` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x5D43218` | `0x45` | `1210.8` | `braverId, damage` | `value:braverId:u16@0x5D43218, value:braverId:s16@0x5D43218, value:damage:u16@0x5D4325D` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x5125C818` | `0x48` | `1210.0` | `braverId, damage` | `value:braverId:u16@0x5125C818, value:braverId:s16@0x5125C818, value:damage:u16@0x5125C860` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x5D0F7CD` | `0x4B` | `1209.2` | `damage, braverId` | `value:damage:u16@0x5D0F7CD, value:damage:s16@0x5D0F7CD, value:braverId:u16@0x5D0F818` | `base=0x5A72000 size=0x6A2000 protect=0x4 type=0x20000` |
| `0x22C03D6` | `0x4E` | `1208.5` | `damage, braverId` | `value:damage:u16@0x22C03D6, value:damage:s16@0x22C03D6, value:braverId:u16@0x22C0424` | `base=0x22C0000 size=0x2000 protect=0x4 type=0x20000` |
| `0x23B03D6` | `0x4E` | `1208.5` | `damage, braverId` | `value:damage:u16@0x23B03D6, value:damage:s16@0x23B03D6, value:braverId:u16@0x23B0424` | `base=0x23B0000 size=0x2000 protect=0x4 type=0x20000` |
| `0x50F4775A` | `0x4E` | `1208.5` | `damage, braverId` | `value:damage:u16@0x50F4775A, value:damage:s16@0x50F4775A, value:braverId:u16@0x50F477A8` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x50F524FA` | `0x4E` | `1208.5` | `damage, braverId` | `value:damage:u16@0x50F524FA, value:damage:s16@0x50F524FA, value:braverId:u16@0x50F52548` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0xC762226` | `0x4F` | `1208.2` | `damage, braverId` | `value:damage:u16@0xC762226, value:damage:s16@0xC762226, value:braverId:u16@0xC762275` | `base=0xC760000 size=0x3000 protect=0x2 type=0x40000` |
| `0xA6A30D4` | `0x54` | `1207.0` | `damage, braverId` | `value:damage:u16@0xA6A30D4, value:damage:s16@0xA6A30D4, value:braverId:u16@0xA6A3128` | `base=0xA660000 size=0x210000 protect=0x4 type=0x20000` |
| `0x5125C044` | `0x54` | `1207.0` | `damage, braverId` | `value:damage:u16@0x5125C044, value:damage:s16@0x5125C044, value:braverId:u16@0x5125C098` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x5125C164` | `0x54` | `1207.0` | `damage, braverId` | `value:damage:u16@0x5125C164, value:damage:s16@0x5125C164, value:braverId:u16@0x5125C1B8` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x723126B` | `0x55` | `1206.8` | `damage, braverId` | `value:damage:u16@0x723126B, value:damage:s16@0x723126B, value:braverId:u16@0x72312C0` | `base=0x71FA000 size=0x41000 protect=0x4 type=0x20000` |
| `0x5130F42D` | `0x55` | `1206.8` | `braverId, damage` | `value:braverId:u16@0x5130F42D, value:braverId:s16@0x5130F42D, value:damage:u16@0x5130F482` | `base=0x50E20000 size=0xF44000 protect=0x4 type=0x20000` |
| `0x7A85B1` | `0x59` | `1205.8` | `braverId, damage` | `value:braverId:u16@0x7A85B1, value:braverId:s16@0x7A85B1, value:damage:u16@0x7A860A` | `base=0x6E0000 size=0xD3000 protect=0x2 type=0x40000` |
