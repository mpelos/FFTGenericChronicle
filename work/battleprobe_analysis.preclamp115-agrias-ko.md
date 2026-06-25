# Battle Probe Analysis

Source: `D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt`
Item catalog: `D:\Projects\FFTGenericChronicle\work\item_catalog.csv` (261 ids)

## Header
- `==== Generic Chronicle Battle Runtime Harness (iter 20) ====`
- `settings path: C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`

## Warnings
- No `[RUNTIME]` lines found. For mapping, install a settings file with `RewriteObservedDamage=true` and `LogResolvedRuntimeContext=true`, then create at least one damage event.

## Units
| Ptr | Id | Faction | Team | Stats |
| --- | --- | --- | --- | --- |
| `0x1418562E0` | `0x32` | ally | 0 | Lv67 HP428 MP89 PA14 MA13 Sp9 CT64 Mv4 Jp3 Br97 Fa65 |
| `0x1418564E0` | `0x1F` | ally | 0 | Lv68 HP314 MP180 PA5 MA17 Sp9 CT84 Mv6 Jp3 Br97 Fa65 |
| `0x1418560E0` | `0x1E` | ally | 0 | Lv69 HP322 MP232 PA11 MA23 Sp12 CT72 Mv5 Jp3 Br97 Fa63 |
| `0x141855EE0` | `0x80` | ally | 0 | Lv70 HP276 MP41 PA15 MA7 Sp16 CT76 Mv6 Jp4 Br97 Fa72 |
| `0x141855CE0` | `0x01` | ally | 0 | Lv75 HP567 MP85 PA20 MA9 Sp10 CT0 Mv6 Jp3 Br97 Fa70 |

## Candidate Fields
### `0x1418562E0` `0x32` ally t0
`bytes[00-3F] +0x00=50 +0x01=19 +0x02=32 +0x03=88 +0x05=8 +0x06=144 +0x07=3 +0x08=31 +0x09=160 +0x12=19 +0x13=41 +0x14=189 +0x15=1 +0x16=200 | words<=511 +0x14w=445 +0x16w=456 +0x18w=494 +0x1Aw=155 +0x1Cw=183 +0x1Ew=214 +0x20w=256 +0x22w=255 +0x24w=255 +0x26w=255 || bytes[40-7F] +0x40=9 +0x41=8 +0x42=4 +0x43=3 +0x44=16 +0x46=10 +0x4B=20 +0x4F=10 +0x50=10 +0x51=1 +0x52=148 +0x54=11 +0x55=127 +0x7B=102 | words<=511 +0x44w=16 +0x46w=10 +0x50w=266 +0x52w=148 || bytes[80-BF] +0x80=24 +0x81=218 +0x82=122 +0x83=2 +0x84=164 +0x85=219 +0x86=2 +0x87=74 +0x88=75 +0x89=2 +0x8A=12 +0x8B=75 +0x8C=14 +0x8D=90 | words<=511 +0x96w=1 +0x98w=32 +0xA0w=254 +0xA2w=255 +0xA6w=4 +0xA8w=193 +0xAEw=174 +0xB2w=128 || bytes[C0-FF] +0xC5=16 +0xC9=128 +0xCC=188 +0xCD=128 +0xCE=240 +0xE4=131 +0xE5=99 +0xE6=99 +0xE7=70 +0xE8=84 +0xE9=35 +0xEA=50 +0xEB=133 +0xEC=18 | words<=511 +0xCEw=240 +0xECw=274 +0xEEw=49 +0xF2w=320 +0xF6w=62 +0xFAw=444 +0xFEw=53 || bytes[100-13F] +0x100=139 +0x101=5 +0x102=78 +0x103=1 +0x104=39 +0x105=1 +0x106=117 +0x108=2 +0x109=2 +0x10A=212 +0x10C=80 +0x10D=4 +0x10E=182 +0x10F=5 | words<=511 +0x102w=334 +0x104w=295 +0x106w=117 +0x10Aw=212 +0x110w=168 +0x112w=230 +0x116w=190 +0x120w=510 +0x124w=462 +0x132w=295 || bytes[140-17F] +0x140=230 +0x144=190 +0x146=44 +0x147=2 +0x148=100 | words<=511 +0x140w=230 +0x144w=190 +0x148w=100 || bytes[180-1BF] +0x18D=255 +0x18F=50 +0x191=122 +0x192=41 +0x1B5=1 +0x1B6=12 +0x1B8=1 +0x1BC=19 +0x1BE=1 | words<=511 +0x192w=41 +0x1B4w=256 +0x1B6w=12 +0x1B8w=1 +0x1BCw=19 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=8 +0x1F4=15 +0x1F5=255 +0x1FC=50 | words<=511 +0x1EAw=100 +0x1EEw=8 +0x1FCw=50`
### `0x1418564E0` `0x1F` ally t0
`bytes[00-3F] +0x00=31 +0x01=20 +0x02=14 +0x03=82 +0x05=8 +0x06=144 +0x07=3 +0x08=21 +0x09=97 +0x12=13 +0x13=69 +0x16=200 +0x17=1 +0x18=232 | words<=511 +0x16w=456 +0x18w=488 +0x1Aw=167 +0x1Cw=206 +0x1Ew=217 +0x20w=30 +0x22w=255 +0x24w=255 +0x26w=255 +0x2Ew=1 || bytes[40-7F] +0x40=9 +0x41=8 +0x42=6 +0x43=3 +0x44=14 +0x46=15 +0x4B=5 +0x4F=10 +0x50=8 +0x51=1 +0x52=145 +0x53=128 +0x54=6 +0x55=255 | words<=511 +0x44w=14 +0x46w=15 +0x50w=264 || bytes[80-BF] +0x80=23 +0x81=76 +0x82=127 +0x83=2 +0x84=67 +0x85=227 +0x86=2 +0x87=167 +0x88=108 +0x89=2 +0x8A=13 +0x8B=70 +0x8C=8 +0x8D=125 | words<=511 +0x98w=32 +0x9Cw=32 +0xAAw=16 +0xAEw=7 || bytes[C0-FF] +0xC0=205 +0xC1=64 +0xC2=16 +0xC3=130 +0xC4=16 +0xCE=64 +0xD5=64 +0xD7=32 +0xE4=131 +0xE5=99 +0xE6=83 +0xE7=67 +0xE8=133 +0xE9=83 | words<=511 +0xC4w=16 +0xCEw=64 +0xECw=277 +0xEEw=49 +0xF2w=253 +0xF6w=427 +0xF8w=382 +0xFAw=128 +0xFEw=232 || bytes[100-13F] +0x100=51 +0x101=7 +0x102=175 +0x103=4 +0x104=104 +0x106=175 +0x108=85 +0x109=2 +0x10A=170 +0x10B=1 +0x10C=126 +0x10D=2 +0x10E=107 +0x10F=5 | words<=511 +0x104w=104 +0x106w=175 +0x10Aw=426 +0x110w=197 +0x112w=186 +0x116w=177 +0x124w=427 +0x138w=426 +0x13Ew=197 || bytes[140-17F] +0x140=6 +0x141=5 +0x144=177 +0x146=24 +0x147=2 +0x148=100 | words<=511 +0x144w=177 +0x148w=100 || bytes[180-1BF] +0x18D=255 +0x18F=31 +0x191=123 +0x192=69 +0x1B5=1 +0x1B6=8 +0x1B8=1 +0x1BC=20 +0x1BE=1 | words<=511 +0x192w=69 +0x1B4w=256 +0x1B6w=8 +0x1B8w=1 +0x1BCw=20 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=8 +0x1F4=5 +0x1F5=255 +0x1FC=31 | words<=511 +0x1EAw=100 +0x1EEw=8 +0x1FCw=31`
### `0x1418560E0` `0x1E` ally t0
`bytes[00-3F] +0x00=30 +0x01=18 +0x02=23 +0x03=80 +0x05=8 +0x06=80 +0x07=3 +0x08=173 +0x09=48 +0x12=11 +0x13=40 +0x16=200 +0x17=1 +0x18=231 | words<=511 +0x16w=456 +0x18w=487 +0x1Aw=167 +0x1Cw=207 +0x1Ew=216 +0x20w=30 +0x22w=255 +0x24w=255 +0x26w=255 +0x2Ew=1 || bytes[40-7F] +0x40=12 +0x41=8 +0x42=5 +0x43=3 +0x44=14 +0x46=15 +0x4B=5 +0x4F=10 +0x50=9 +0x51=1 +0x52=145 +0x54=6 +0x55=255 +0x5A=48 | words<=511 +0x44w=14 +0x46w=15 +0x50w=265 +0x52w=145 +0x5Aw=48 +0x64w=48 || bytes[80-BF] +0x80=28 +0x81=177 +0x82=197 +0x83=2 +0x84=8 +0x85=84 +0x86=3 +0x87=143 +0x88=223 +0x89=2 +0x8A=12 +0x8B=75 +0x8C=9 +0x8D=120 | words<=511 +0x98w=32 +0x9Cw=64 +0xA0w=207 +0xA2w=248 +0xA8w=255 +0xAEw=130 +0xB0w=72 || bytes[C0-FF] +0xC8=64 +0xCC=128 +0xCE=64 +0xCF=255 +0xD0=240 +0xD1=240 +0xE4=134 +0xE5=132 +0xE6=82 +0xE7=99 +0xE8=85 +0xE9=50 +0xEA=66 +0xEB=88 | words<=511 +0xC8w=64 +0xCCw=128 +0xEEw=50 +0xF2w=185 +0xF6w=254 +0xFAw=279 || bytes[100-13F] +0x100=242 +0x101=4 +0x102=57 +0x103=2 +0x104=213 +0x105=1 +0x106=24 +0x107=1 +0x108=172 +0x109=1 +0x10A=119 +0x10B=1 +0x10C=224 +0x10D=2 | words<=511 +0x104w=469 +0x106w=280 +0x108w=428 +0x10Aw=375 +0x110w=153 +0x114w=153 +0x116w=113 +0x118w=456 +0x128w=279 +0x132w=469 || bytes[140-17F] +0x142=153 +0x144=113 +0x146=200 +0x147=1 +0x148=200 | words<=511 +0x142w=153 +0x144w=113 +0x146w=456 +0x148w=200 || bytes[180-1BF] +0x18D=255 +0x18F=30 +0x191=121 +0x192=40 +0x1B5=1 +0x1B6=8 +0x1B8=1 +0x1BC=18 +0x1BE=1 | words<=511 +0x192w=40 +0x1B4w=256 +0x1B6w=8 +0x1B8w=1 +0x1BCw=18 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=8 +0x1F4=51 +0x1F5=255 +0x1FC=30 | words<=511 +0x1EAw=100 +0x1EEw=8 +0x1FCw=30`
### `0x141855EE0` `0x80` ally t0
`bytes[00-3F] +0x00=128 +0x01=17 +0x02=3 +0x03=89 +0x05=8 +0x06=144 +0x07=3 +0x08=210 +0x09=64 +0x0A=221 +0x0B=1 +0x12=20 +0x13=6 +0x14=183 | words<=511 +0x0Aw=477 +0x14w=439 +0x16w=474 +0x18w=487 +0x1Aw=168 +0x1Cw=197 +0x1Ew=236 +0x20w=17 +0x22w=255 +0x24w=18 || bytes[40-7F] +0x40=16 +0x41=4 +0x42=6 +0x43=4 +0x44=15 +0x45=15 +0x46=10 +0x47=5 +0x4B=30 +0x4F=9 +0x50=9 +0x51=1 +0x52=224 +0x53=64 | words<=511 +0x50w=265 +0x5Aw=64 +0x60w=12 +0x64w=64 || bytes[80-BF] +0x80=20 +0x81=95 +0x82=161 +0x83=2 +0x84=238 +0x85=18 +0x86=3 +0x87=89 +0x88=90 +0x89=2 +0x8A=12 +0x8B=70 +0x8C=13 +0x8D=50 | words<=511 +0x96w=64 +0x9Aw=9 +0x9Cw=64 +0xA2w=96 +0xA8w=4 +0xAEw=255 +0xB2w=160 || bytes[C0-FF] +0xC0=9 +0xC1=64 +0xC3=16 +0xC6=32 +0xC8=64 +0xC9=140 +0xCB=64 +0xCE=64 +0xCF=255 +0xD0=240 +0xD1=240 +0xD5=128 +0xE4=136 +0xE5=117 | words<=511 +0xC6w=32 +0xECw=276 +0xEEw=66 +0xF2w=349 +0xF6w=175 +0xF8w=334 +0xFAw=455 || bytes[100-13F] +0x100=187 +0x101=6 +0x102=252 +0x103=4 +0x104=69 +0x105=3 +0x106=215 +0x107=2 +0x108=195 +0x109=4 +0x10A=45 +0x10B=2 +0x10C=16 +0x10D=6 | words<=511 +0x110w=159 +0x116w=150 +0x13Ew=159 || bytes[140-17F] +0x140=112 +0x141=3 +0x144=150 +0x146=244 +0x147=2 +0x148=200 +0x14C=82 +0x14D=105 +0x14E=111 +0x14F=110 +0x151=119 +0x152=117 +0x153=108 +0x154=102 | words<=511 +0x144w=150 +0x148w=200 +0x154w=102 || bytes[180-1BF] +0x18D=255 +0x18F=126 +0x191=120 +0x1B5=1 +0x1B8=1 +0x1BB=1 +0x1BC=17 +0x1BE=1 | words<=511 +0x1B4w=256 +0x1B8w=1 +0x1BAw=256 +0x1BCw=17 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=8 +0x1F1=16 +0x1F4=56 +0x1F5=255 +0x1FC=101 +0x1FD=1 | words<=511 +0x1EAw=100 +0x1EEw=8 +0x1FCw=357`
### `0x141855CE0` `0x01` ally t0
`bytes[00-3F] +0x00=1 +0x01=16 +0x03=160 +0x05=11 +0x06=144 +0x07=3 +0x08=242 +0x09=80 +0x12=224 +0x13=25 +0x16=228 +0x17=1 +0x18=232 +0x19=1 | words<=511 +0x16w=484 +0x18w=488 +0x1Aw=156 +0x1Cw=185 +0x1Ew=218 +0x20w=37 +0x22w=255 +0x24w=255 +0x26w=142 +0x2Ew=1 || bytes[40-7F] +0x40=10 +0x42=6 +0x43=3 +0x44=40 +0x46=20 +0x4A=50 +0x4E=25 +0x4F=8 +0x50=10 +0x51=1 +0x52=154 +0x53=64 +0x54=25 +0x55=254 | words<=511 +0x40w=10 +0x44w=40 +0x46w=20 +0x4Aw=50 +0x50w=266 +0x56w=16 +0x5Aw=64 +0x60w=16 +0x64w=64 +0x78w=224 || bytes[80-BF] +0x80=23 +0x81=254 +0x82=158 +0x83=2 +0x84=166 +0x85=41 +0x86=3 +0x87=34 +0x88=20 +0x89=3 +0x8A=12 +0x8B=80 +0x8C=20 +0x8D=90 | words<=511 +0x9Cw=32 +0xA8w=255 +0xAEw=195 +0xB2w=160 +0xBAw=248 || bytes[C0-FF] +0xC0=16 +0xC3=128 +0xC6=255 +0xC7=240 +0xC8=208 +0xC9=204 +0xCB=32 +0xCE=80 +0xCF=208 +0xD1=32 +0xD5=96 +0xD7=32 +0xDE=248 +0xE0=128 | words<=511 +0xC0w=16 +0xDEw=248 +0xE0w=128 +0xECw=277 +0xEEw=129 +0xF2w=456 +0xF6w=493 +0xFAw=391 || bytes[100-13F] +0x100=191 +0x101=7 +0x102=203 +0x103=2 +0x104=153 +0x105=5 +0x106=191 +0x107=1 +0x108=22 +0x109=3 +0x10A=4 +0x10B=3 +0x10C=147 +0x10D=6 | words<=511 +0x106w=447 +0x110w=167 +0x112w=216 +0x116w=131 +0x118w=420 +0x128w=511 +0x13Ew=167 || bytes[140-17F] +0x140=136 +0x141=5 +0x144=131 +0x146=32 +0x147=23 +0x148=100 | words<=511 +0x144w=131 +0x148w=100 || bytes[180-1BF] +0x18D=255 +0x18F=1 +0x191=1 +0x192=25 +0x1B5=1 +0x1B6=8 +0x1B8=1 +0x1BC=16 +0x1BE=1 | words<=511 +0x18Ew=256 +0x190w=256 +0x192w=25 +0x1B4w=256 +0x1B6w=8 +0x1B8w=1 +0x1BCw=16 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=11 +0x1F4=34 +0x1F5=255 +0x1FC=1 | words<=511 +0x1EAw=100 +0x1EEw=11 +0x1FCw=1`

## Candidate Item Hits
### `0x1418562E0` `0x32` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x4B` | byte | 20 Longsword | Weapon | Sword | weapon |
| `+0x4F` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x50` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 148 Mythril Helm | Headgear | Helmet | armor |
| `+0x54` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x55` | byte | 127 Spark Bomb | Weapon | Bomb | weapon |
| `+0x7B` | byte | 102 Partisan | Weapon | Polearm | weapon |
| `+0x7C` | byte | 125 Flameburst Bomb | Weapon | Bomb | weapon |
| `+0x7D` | byte | 49 Giant's Axe | Weapon | Axe | weapon |
| `+0x7E` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x7F` | byte | 251 Holy Water | Rare | Item |  |
| `+0x80` | byte | 24 Coral Sword | Weapon | Sword | weapon |
| `+0x81` | byte | 218 Bracers | Accessory | Armguard | accessory |
| `+0x82` | byte | 122 Shuriken | Weapon | Throwing | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 164 Celebrant's Miter | Headgear | Hat | armor |
| `+0x85` | byte | 219 Reflect Ring | Rare, Accessory | Ring | accessory |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 74 Glacial Gun | Rare, Weapon | Gun | weapon |
| `+0x88` | byte | 75 Blaze Gun | Rare, Weapon | Gun | weapon |
| `+0x89` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 12 Kunai | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 75 Blaze Gun | Rare, Weapon | Gun | weapon |
| `+0x8C` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x8D` | byte | 90 Yoichi Bow | Rare, Weapon | Bow | weapon |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x90` | byte | 45 Kiku-ichimonji | Weapon | Katana | weapon |
| `+0x91` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 90 Yoichi Bow | Rare, Weapon | Bow | weapon |
| `+0x96` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x98` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x9D` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 254 | Rare | None |  |
| `+0xA2` | byte | 255 | Rare | None |  |
| `+0xA4` | byte | 252 Remedy | Rare | Item |  |
| `+0xA5` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0xA6` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xA8` | byte | 193 Brigandine | Armor | Clothing | armor |
| `+0xAA` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAB` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xAD` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAE` | byte | 174 Bronze Armor | Armor | Armor | armor |
| `+0xB2` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xB4` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xB5` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xB7` | byte | 160 Headgear | Headgear | Hat | armor |
| `+0xB9` | byte | 96 Bestiary | Weapon | Book | weapon |
| `+0xBD` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xBF` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xC5` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xC9` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xCC` | byte | 188 Leather Plate | Armor | Clothing | armor |
| `+0xCD` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xCE` | byte | 240 Potion | Rare | Item |  |
| `+0xE4` | byte | 131 Round Shield | Shield | Shield | shield |
| `+0xE5` | byte | 99 Javelin | Weapon | Polearm | weapon |
| `+0xE6` | byte | 99 Javelin | Weapon | Polearm | weapon |
| `+0xE7` | byte | 70 Scorpion Tail | Rare, Weapon | Flail | weapon |
| `+0xE8` | byte | 84 Silver Bow | Weapon | Bow | weapon |
| `+0xE9` | byte | 35 Excalibur | Rare, Weapon | KnightSword | weapon |
| `+0xEA` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0xEB` | byte | 133 Golden Shield | Shield | Shield | shield |
| `+0xEC` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xED` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xEE` | byte | 49 Giant's Axe | Weapon | Axe | weapon |
| `+0xF0` | byte | 57 Dragon Rod | Rare, Weapon | Rod | weapon |
| `+0xF1` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0xF2` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xF3` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xF4` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xF5` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xF6` | byte | 62 Serpent Staff | Weapon | Staff | weapon |
| `+0xF8` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0xF9` | byte | 2 Mythril Knife | Weapon | Knife | weapon |

_Truncated: 128 more dump item-id hits._
### `0x1418564E0` `0x1F` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x4B` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x4F` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x50` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 145 Bronze Helm | Headgear | Helmet | armor |
| `+0x53` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x54` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x55` | byte | 255 | Rare | None |  |
| `+0x7B` | byte | 247 Eye Drops | Rare | Item |  |
| `+0x7C` | byte | 72 Mythril Gun | Rare, Weapon | Gun | weapon |
| `+0x7D` | byte | 54 Ice Rod | Weapon | Rod | weapon |
| `+0x7E` | byte | 185 Maximillian | Rare, Armor | Armor | armor |
| `+0x7F` | byte | 19 Broadsword | Weapon | Sword | weapon |
| `+0x80` | byte | 23 Blood Sword | Rare, Weapon | Sword | weapon |
| `+0x81` | byte | 76 Blaster | Rare, Weapon | Gun | weapon |
| `+0x82` | byte | 127 Spark Bomb | Weapon | Bomb | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 67 Iron Flail | Weapon | Flail | weapon |
| `+0x85` | byte | 227 Nu Khai Armband | Accessory | Armlet | accessory |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 167 Lambent Hat | Headgear | Hat | armor |
| `+0x88` | byte | 108 Battle Bamboo | Weapon | Pole | weapon |
| `+0x89` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 13 Kodachi | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 70 Scorpion Tail | Rare, Weapon | Flail | weapon |
| `+0x8C` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x8D` | byte | 125 Flameburst Bomb | Weapon | Bomb | weapon |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 90 Yoichi Bow | Rare, Weapon | Bow | weapon |
| `+0x90` | byte | 70 Scorpion Tail | Rare, Weapon | Flail | weapon |
| `+0x91` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 125 Flameburst Bomb | Weapon | Bomb | weapon |
| `+0x98` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x9C` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 254 | Rare | None |  |
| `+0xA1` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xA2` | byte | 255 | Rare | None |  |
| `+0xA3` | byte | 254 | Rare | None |  |
| `+0xA4` | byte | 252 Remedy | Rare | Item |  |
| `+0xA5` | byte | 192 Wizard Clothing | Armor | Clothing | armor |
| `+0xA6` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xA7` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAA` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAE` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0xB1` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xB3` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xB4` | byte | 200 Hempen Robe | Armor | Robe | armor |
| `+0xB5` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xB9` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xBA` | byte | 255 | Rare | None |  |
| `+0xBB` | byte | 159 Red Hood | Headgear | Hat | armor |
| `+0xC0` | byte | 205 Black Robe | Armor | Robe | armor |
| `+0xC1` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xC2` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xC3` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0xC4` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xCE` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xD5` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xD7` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xE4` | byte | 131 Round Shield | Shield | Shield | shield |
| `+0xE5` | byte | 99 Javelin | Weapon | Polearm | weapon |
| `+0xE6` | byte | 83 Longbow | Weapon | Bow | weapon |
| `+0xE7` | byte | 67 Iron Flail | Weapon | Flail | weapon |
| `+0xE8` | byte | 133 Golden Shield | Shield | Shield | shield |
| `+0xE9` | byte | 83 Longbow | Weapon | Bow | weapon |
| `+0xEA` | byte | 51 Rod | Weapon | Rod | weapon |
| `+0xEB` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0xEC` | byte | 21 Iron Sword | Weapon | Sword | weapon |
| `+0xED` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xEE` | byte | 49 Giant's Axe | Weapon | Axe | weapon |
| `+0xF0` | byte | 52 Thunder Rod | Weapon | Rod | weapon |
| `+0xF1` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0xF2` | byte | 253 Phoenix Down | Rare | Item |  |
| `+0xF4` | byte | 226 Japa Mala | Accessory | Armlet | accessory |
| `+0xF5` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0xF6` | byte | 171 Ribbon | Rare, Headgear | HairAdornment | armor |

_Truncated: 124 more dump item-id hits._
### `0x1418560E0` `0x1E` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x4B` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x4F` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x50` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 145 Bronze Helm | Headgear | Helmet | armor |
| `+0x54` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x55` | byte | 255 | Rare | None |  |
| `+0x5A` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0x64` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0x7B` | byte | 101 Mythril Spear | Weapon | Polearm | weapon |
| `+0x7C` | byte | 204 White Robe | Armor | Robe | armor |
| `+0x7D` | byte | 44 Muramasa | Weapon | Katana | weapon |
| `+0x7E` | byte | 224 Diamond Bracelet | Accessory | Armlet | accessory |
| `+0x7F` | byte | 141 Kaiser Shield | Rare, Shield | Shield | shield |
| `+0x80` | byte | 28 Diamond Sword | Weapon | Sword | weapon |
| `+0x81` | byte | 177 Plate Mail | Armor | Armor | armor |
| `+0x82` | byte | 197 Ninja Gear | Rare, Armor | Clothing | armor |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x85` | byte | 84 Silver Bow | Weapon | Bow | weapon |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 143 Escutcheon | Rare, Shield | Shield | shield |
| `+0x88` | byte | 223 Angel Ring | Rare, Accessory | Ring | accessory |
| `+0x89` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 12 Kunai | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 75 Blaze Gun | Rare, Weapon | Gun | weapon |
| `+0x8C` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x8D` | byte | 120 Cashmere | Weapon | Cloth | weapon |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x90` | byte | 60 White Staff | Weapon | Staff | weapon |
| `+0x91` | byte | 60 White Staff | Weapon | Staff | weapon |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 150 Close Helmet | Headgear | Helmet | armor |
| `+0x98` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x9C` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 207 Lordly Robe | Rare, Armor | Robe | armor |
| `+0xA2` | byte | 248 Echo Herbs | Rare | Item |  |
| `+0xA4` | byte | 252 Remedy | Rare | Item |  |
| `+0xA5` | byte | 162 Green Beret | Headgear | Hat | armor |
| `+0xA6` | byte | 28 Diamond Sword | Weapon | Sword | weapon |
| `+0xA7` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xA8` | byte | 255 | Rare | None |  |
| `+0xAA` | byte | 240 Potion | Rare | Item |  |
| `+0xAB` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xAD` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAE` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0xB0` | byte | 72 Mythril Gun | Rare, Weapon | Gun | weapon |
| `+0xB4` | byte | 136 Aegis Shield | Shield | Shield | shield |
| `+0xB5` | byte | 132 Mythril Shield | Shield | Shield | shield |
| `+0xBD` | byte | 129 Buckler | Shield | Shield | shield |
| `+0xBF` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xC8` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xCC` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xCE` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xCF` | byte | 255 | Rare | None |  |
| `+0xD0` | byte | 240 Potion | Rare | Item |  |
| `+0xD1` | byte | 240 Potion | Rare | Item |  |
| `+0xE4` | byte | 134 Ice Shield | Shield | Shield | shield |
| `+0xE5` | byte | 132 Mythril Shield | Shield | Shield | shield |
| `+0xE6` | byte | 82 Gastrophetes | Rare, Weapon | Crossbow | weapon |
| `+0xE7` | byte | 99 Javelin | Weapon | Polearm | weapon |
| `+0xE8` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0xE9` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0xEA` | byte | 66 Staff of the Magi | Rare, Weapon | Staff | weapon |
| `+0xEB` | byte | 88 Mythril Bow | Weapon | Bow | weapon |
| `+0xEC` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xED` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEE` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0xF0` | byte | 171 Ribbon | Rare, Headgear | HairAdornment | armor |
| `+0xF1` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0xF2` | byte | 185 Maximillian | Rare, Armor | Armor | armor |
| `+0xF4` | byte | 96 Bestiary | Weapon | Book | weapon |
| `+0xF5` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xF6` | byte | 254 | Rare | None |  |
| `+0xF8` | byte | 167 Lambent Hat | Headgear | Hat | armor |
| `+0xF9` | byte | 3 Blind Knife | Weapon | Knife | weapon |

_Truncated: 126 more dump item-id hits._
### `0x141855EE0` `0x80` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x45` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x47` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x4B` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x4F` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x50` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 224 Diamond Bracelet | Accessory | Armlet | accessory |
| `+0x53` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x54` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x55` | byte | 191 Adamant Vest | Armor | Clothing | armor |
| `+0x59` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x5A` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x60` | byte | 12 Kunai | Weapon | NinjaBlade | weapon |
| `+0x63` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0x64` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x7B` | byte | 246 Antidote | Rare | Item |  |
| `+0x7C` | byte | 184 Mirror Mail | Rare, Armor | Armor | armor |
| `+0x7D` | byte | 55 Poison Rod | Weapon | Rod | weapon |
| `+0x7E` | byte | 154 Crystal Helm | Headgear | Helmet | armor |
| `+0x7F` | byte | 189 Ringmail | Armor | Clothing | armor |
| `+0x80` | byte | 20 Longsword | Weapon | Sword | weapon |
| `+0x81` | byte | 95 Battle Folio | Weapon | Book | weapon |
| `+0x82` | byte | 161 Wizard's Hat | Headgear | Hat | armor |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 238 Septième Sens | Rare, Accessory | Perfume | accessory |
| `+0x85` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 89 Artemis Bow | Weapon | Bow | weapon |
| `+0x88` | byte | 90 Yoichi Bow | Rare, Weapon | Bow | weapon |
| `+0x89` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 12 Kunai | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 70 Scorpion Tail | Rare, Weapon | Flail | weapon |
| `+0x8C` | byte | 13 Kodachi | Weapon | NinjaBlade | weapon |
| `+0x8D` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x8E` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x8F` | byte | 120 Cashmere | Weapon | Cloth | weapon |
| `+0x90` | byte | 43 Kiyomori | Weapon | Katana | weapon |
| `+0x91` | byte | 122 Shuriken | Weapon | Throwing | weapon |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 75 Blaze Gun | Rare, Weapon | Gun | weapon |
| `+0x96` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x9A` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x9C` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 255 | Rare | None |  |
| `+0xA1` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0xA2` | byte | 96 Bestiary | Weapon | Book | weapon |
| `+0xA4` | byte | 156 Grand Helm | Rare, Headgear | Helmet | armor |
| `+0xA5` | byte | 254 | Rare | None |  |
| `+0xA6` | byte | 60 White Staff | Weapon | Staff | weapon |
| `+0xA7` | byte | 248 Echo Herbs | Rare | Item |  |
| `+0xA8` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xAA` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAB` | byte | 196 Gaia Gear | Armor | Clothing | armor |
| `+0xAD` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAE` | byte | 255 | Rare | None |  |
| `+0xB0` | byte | 248 Echo Herbs | Rare | Item |  |
| `+0xB1` | byte | 200 Hempen Robe | Armor | Robe | armor |
| `+0xB2` | byte | 160 Headgear | Headgear | Hat | armor |
| `+0xB4` | byte | 200 Hempen Robe | Armor | Robe | armor |
| `+0xB5` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xBD` | byte | 255 | Rare | None |  |
| `+0xBF` | byte | 252 Remedy | Rare | Item |  |
| `+0xC0` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0xC1` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xC3` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xC6` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xC8` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xC9` | byte | 140 Genji Shield | Rare, Shield | Shield | shield |
| `+0xCB` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xCE` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xCF` | byte | 255 | Rare | None |  |
| `+0xD0` | byte | 240 Potion | Rare | Item |  |
| `+0xD1` | byte | 240 Potion | Rare | Item |  |
| `+0xD5` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xE4` | byte | 136 Aegis Shield | Shield | Shield | shield |
| `+0xE5` | byte | 117 Proudhide Bag | Rare, Weapon | Bag | weapon |
| `+0xE6` | byte | 131 Round Shield | Shield | Shield | shield |

_Truncated: 159 more dump item-id hits._
### `0x141855CE0` `0x01` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 40 Bizen Osafune | Weapon | Katana | weapon |
| `+0x46` | byte | 20 Longsword | Weapon | Sword | weapon |
| `+0x4A` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x4E` | byte | 25 Ancient Sword | Weapon | Sword | weapon |
| `+0x4F` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x50` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 154 Crystal Helm | Headgear | Helmet | armor |
| `+0x53` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x54` | byte | 25 Ancient Sword | Weapon | Sword | weapon |
| `+0x55` | byte | 254 | Rare | None |  |
| `+0x56` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x5A` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x5D` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x60` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x64` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x78` | byte | 224 Diamond Bracelet | Accessory | Armlet | accessory |
| `+0x7B` | byte | 46 Masamune | Rare, Weapon | Katana | weapon |
| `+0x7C` | byte | 162 Green Beret | Headgear | Hat | armor |
| `+0x7D` | byte | 56 Wizard's Rod | Weapon | Rod | weapon |
| `+0x7E` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x7F` | byte | 171 Ribbon | Rare, Headgear | HairAdornment | armor |
| `+0x80` | byte | 23 Blood Sword | Rare, Weapon | Sword | weapon |
| `+0x81` | byte | 254 | Rare | None |  |
| `+0x82` | byte | 158 Plumed Hat | Headgear | Hat | armor |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 166 Gold Hairpin | Headgear | Hat | armor |
| `+0x85` | byte | 41 Murasame | Weapon | Katana | weapon |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 34 Save the Queen | Rare, Weapon | KnightSword | weapon |
| `+0x88` | byte | 20 Longsword | Weapon | Sword | weapon |
| `+0x89` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 12 Kunai | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x8C` | byte | 20 Longsword | Weapon | Sword | weapon |
| `+0x8D` | byte | 90 Yoichi Bow | Rare, Weapon | Bow | weapon |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x90` | byte | 40 Bizen Osafune | Weapon | Katana | weapon |
| `+0x91` | byte | 140 Genji Shield | Rare, Shield | Shield | shield |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x9B` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x9C` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 255 | Rare | None |  |
| `+0xA1` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xA2` | byte | 254 | Rare | None |  |
| `+0xA3` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0xA4` | byte | 252 Remedy | Rare | Item |  |
| `+0xA5` | byte | 195 Power Garb | Armor | Clothing | armor |
| `+0xA6` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xA7` | byte | 144 Leather Helm | Headgear | Helmet | armor |
| `+0xA8` | byte | 255 | Rare | None |  |
| `+0xAA` | byte | 240 Potion | Rare | Item |  |
| `+0xAB` | byte | 242 X-Potion | Rare | Item |  |
| `+0xAD` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAE` | byte | 195 Power Garb | Armor | Clothing | armor |
| `+0xB1` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xB2` | byte | 160 Headgear | Headgear | Hat | armor |
| `+0xB4` | byte | 236 Chantage | Rare, Accessory | Perfume | accessory |
| `+0xB5` | byte | 232 Elven Cloak | Accessory | Cloak | accessory |
| `+0xB6` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xB7` | byte | 194 Jujitsu Gi | Armor | Clothing | armor |
| `+0xBA` | byte | 248 Echo Herbs | Rare | Item |  |
| `+0xBD` | byte | 174 Bronze Armor | Armor | Armor | armor |
| `+0xBF` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0xC0` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xC3` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xC6` | byte | 255 | Rare | None |  |
| `+0xC7` | byte | 240 Potion | Rare | Item |  |
| `+0xC8` | byte | 208 Battle Boots | Accessory | Shoes | accessory |
| `+0xC9` | byte | 204 White Robe | Armor | Robe | armor |
| `+0xCB` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xCE` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0xCF` | byte | 208 Battle Boots | Accessory | Shoes | accessory |
| `+0xD1` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xD5` | byte | 96 Bestiary | Weapon | Book | weapon |
| `+0xD7` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xDE` | byte | 248 Echo Herbs | Rare | Item |  |

_Truncated: 156 more dump item-id hits._

## Diff Offset Frequency
| Offset | Count |
| --- | ---: |
| `+0x61` | 2 |
| `+0x18D` | 2 |
| `+0x1A0` | 1 |
| `+0x1A1` | 1 |
| `+0x1A2` | 1 |
| `+0x1A3` | 1 |
| `+0x1AA` | 1 |
| `+0x1AC` | 1 |
| `+0x1B0` | 1 |
| `+0x1BA` | 1 |
| `+0x1EF` | 1 |
| `+0x28` | 1 |
| `+0x41` | 1 |
| `+0x51` | 1 |
| `+0x10C` | 1 |
| `+0x13A` | 1 |
| `+0x13B` | 1 |
| `+0x1B8` | 1 |
| `+0x1BB` | 1 |
| `+0x1BE` | 1 |
| `+0x1C0` | 1 |

## Unit Diffs
### `0x1418562E0` `0x32`
- `+0x61:00->08 +0x18D:FF->03 +0x1A0:00->13 +0x1A1:00->29 +0x1A2:00->02 +0x1A3:00->01 +0x1AA:00->05 +0x1AC:00->0A +0x1B0:00->09 +0x1BA:00->01 +0x1EF:00->08`
- `+0x28:32->48 +0x41:08->2E +0x51:01->00 +0x61:08->00 +0x10C:50->84 +0x13A:D8->0C +0x13B:17->18 +0x18D:03->FF +0x1B8:01->00 +0x1BB:00->01 +0x1BE:01->00 +0x1C0:00->08`

## Memory Table Probes
No parsed `[MEMTABLE]` lines.

## Hook Register Probe
- Snapshots: 11
- Kinds: `continuous`=11
- Touched ids: `0x32`=7, `0x1F`=1, `0x1E`=1, `0x80`=1, `0x01`=1
- Register classifications: `unreadable`=44, `zero`=44, `unit:touched`=23, `module+0x7832A0`=22, `module+0x1853CE0`=11, `readable`=11, `module+0x1856671`=11, `module+0x18564E0`=6, `unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer`=4, `unit:id=0x1F:team=0:hp=314:ct=66`=3, `unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792AF0:module+0x3792AF0,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792AF0:module+0x3792AF0,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xC:unreadable,+0xE8=0x143792AF0:module+0x3792AF0,+0xF0=0x417BF12C88:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer`=2, `unreadable,+0x10=0xFFBB0000:unreadable,+0x18=0xFFBB:unreadable,+0x70=0x863BCC558EEF:unreadable,+0x78=0x436A357948:readable,+0x90=0x1418562E0:unit:touched,+0xC8=0x143792AF0:module+0x3792AF0,+0xD8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xE0=0x14078C708:module+0x78C708,+0xE8=0x140788790:module+0x788790,+0xF0=0x143792AF0:module+0x3792AF0`=1

### First Snapshots
| Kind | Event | Hook Count | Ptr | Hook Ptr | Id | Registers |
| --- | ---: | ---: | --- | --- | --- | --- |
| continuous |  | 4 | `0x1418562E0` | `` | `0x32` | `rax=0x1AC:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418562E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418562E0:unit:touched rbp=0x0:zero rsp=0x12A06FDC0:readable r8=0x1418564E0:module+0x18564E0 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCFA58EEF:unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 6 | `0x1418562E0` | `` | `0x32` | `rax=0x1AC:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418562E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418562E0:unit:touched rbp=0x0:zero rsp=0x129F6FDC0:readable r8=0x1418564E0:module+0x18564E0 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCC558EEF:unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792AF0:module+0x3792AF0,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792AF0:module+0x3792AF0,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xC:unreadable,+0xE8=0x143792AF0:module+0x3792AF0,+0xF0=0x417BF12C88:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 8 | `0x1418562E0` | `` | `0x32` | `rax=0x1AC:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418562E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418562E0:unit:touched rbp=0x0:zero rsp=0x129F6FDC0:readable r8=0x1418564E0:module+0x18564E0 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCC558EEF:unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792AF0:module+0x3792AF0,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792AF0:module+0x3792AF0,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xC:unreadable,+0xE8=0x143792AF0:module+0x3792AF0,+0xF0=0x417BF12C88:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 12 | `0x1418562E0` | `` | `0x32` | `rax=0x1AC:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418562E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418562E0:unit:touched rbp=0x0:zero rsp=0x12A06FDC0:readable r8=0x1418564E0:module+0x18564E0 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCFA58EEF:unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 14 | `0x1418562E0` | `` | `0x32` | `rax=0x1AC:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418562E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418562E0:unit:touched rbp=0x0:zero rsp=0x129F6FDA0:readable r8=0x1418564E0:module+0x18564E0 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x0=0xF100000000030000:unreadable,+0x10=0xFFBB0000:unreadable,+0x18=0xFFBB:unreadable,+0x70=0x863BCC558EEF:unreadable,+0x78=0x436A357948:readable,+0x90=0x1418562E0:unit:touched,+0xC8=0x143792AF0:module+0x3792AF0,+0xD8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xE0=0x14078C708:module+0x78C708,+0xE8=0x140788790:module+0x788790,+0xF0=0x143792AF0:module+0x3792AF0` |
| continuous |  | 18 | `0x1418562E0` | `` | `0x32` | `rax=0x1AC:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418562E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418562E0:unit:touched rbp=0x0:zero rsp=0x12A06FDC0:readable r8=0x1418564E0:module+0x18564E0 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCFA58EEF:unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 22 | `0x1418564E0` | `` | `0x1F` | `rax=0x13A:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418564E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418564E0:unit:touched rbp=0x0:zero rsp=0x12A06FDC0:readable r8=0x1418564E0:unit:touched r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCFA58EEF:unreadable,+0x58=0x1418564E0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 26 | `0x1418560E0` | `` | `0x1E` | `rax=0x142:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418560E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418560E0:unit:touched rbp=0x0:zero rsp=0x12A06FDC0:readable r8=0x1418564E0:unit:id=0x1F:team=0:hp=314:ct=66 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCFA58EEF:unreadable,+0x58=0x1418560E0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 30 | `0x141855EE0` | `` | `0x80` | `rax=0x114:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x141855EE0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x141855EE0:unit:touched rbp=0x0:zero rsp=0x12A06FDC0:readable r8=0x1418564E0:unit:id=0x1F:team=0:hp=314:ct=66 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCFA58EEF:unreadable,+0x58=0x141855EE0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 32 | `0x1418562E0` | `` | `0x32` | `rax=0x1AC:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x1418562E0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x1418562E0:unit:touched rbp=0x0:zero rsp=0x12A06FDC0:readable r8=0x1418564E0:unit:id=0x1F:team=0:hp=314:ct=66 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCFA58EEF:unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |
| continuous |  | 36 | `0x141855CE0` | `` | `0x01` | `rax=0x237:unreadable rbx=0x1407832A0:module+0x7832A0 rcx=0x141855CE0:unit:touched rdx=0x1407832A0:module+0x7832A0 rsi=0x141853CE0:module+0x1853CE0 rdi=0x141855CE0:unit:touched rbp=0x0:zero rsp=0x12A06FDC0:readable r8=0x1418564E0:unit:id=0x1F:team=0:hp=314:ct=84 r9=0x141856671:module+0x1856671 r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0x863BCFA58EEF:unreadable,+0x58=0x141855CE0:unit:touched,+0xA8=0x143792C10:module+0x3792C10,+0xB8=0x1402F3799:module+0x2F3799:near-ko-stack-mid-0x9,+0xC0=0x143792C10:module+0x3792C10,+0xD8=0x157D0651E:module+0x17D0651E,+0xE0=0xD:unreadable,+0xE8=0x143792C10:module+0x3792C10,+0xF0=0x417BF12CE8:readable,+0xF8=0x1402F2EC1:module+0x2F2EC1:ko-stack-outer` |

## Runtime Context Summary
No parsed `[RUNTIME]` lines.

## Actor Probe CT Summary
- Actor probe events: 2
- Resolved actor probes: 2/2
- Sources: `ct-lowest`=2
- Rule: exclude target, prefer largest recent CT drop, else lowest absolute CT.

| # | Line | Target | Attacker | Source | Top candidates |
| --- | ---: | --- | --- | --- | --- |
| 1 | 136 | 0x1E | 0x80 | `ct-lowest` | `0x80(ct=44,prev=-,drop=0,spd=16) 0x32(ct=46,prev=-,drop=0,spd=9) 0x1F(ct=66,prev=-,drop=0,spd=9)` |
| 2 | 152 | 0x80 | 0x32 | `ct-lowest` | `0x32(ct=46,prev=46,drop=0,spd=9) 0x1E(ct=48,prev=48,drop=0,spd=12) 0x1F(ct=66,prev=66,drop=0,spd=9)` |

## Death State
- Events: `DEATH-DUMP`=1, `DEATH-DIFF`=1
- Status: dump=1, diff=1
- Death diff offsets: `+0x30`=1, `+0x31`=1, `+0x61`=1, `+0x18C`=1, `+0x1BB`=1, `+0x1C4`=1, `+0x1C5`=1, `+0x1DB`=1, `+0x1EF`=1, `+0x1F5`=1
- Verdict: **mapping-candidate: death diffs include KO flag +0x61**

### Death Diffs
- line 134: `DEATH-DIFF` `0x1418560E0` alive->dead +0x30:42->00 +0x31:01->00 +0x61:00->20 +0x18C:00->01 +0x1BB:00->01 +0x1C4:73->0F +0x1C5:00->27 +0x1DB:00->20 +0x1EF:00->20 +0x1F5:FF->13

## Death Gate Outcome
- Lethal HP rewrites (`finalDamage>=9999`, HP -> 0): 0
- Concrete lethal HP writes: 0
- Dry-run lethal HP decisions: 0
- Placeholder-sized lethal rewrites: 0/0
- Death events: 2
- KO flag diffs (`+0x61`): 1
- KO flag writes (`+0x61`): 0
- HP rewrite failures: 0
- Death-write failures: 0
- Verdict: **no death-gate lethal HP rewrite evidence**

## HP Events / Rewrite
- Damage events: 2
- Healing events: 0
- HP sample age: min=6ms max=6ms
- Damage by unit: `0x1418560E0`=1, `0x141855EE0`=1
- Context events: 2
- Context resolved: 2/2
  - `0x1418560E0` resolved=0x1418562E0 source=pending-clear pending=batch=1/act=258/event=1/16/confidence=damage-cache-lethal-clamp/score=1098832 fallback=none ctCandidates=none ctLowCandidates=none ctObserved=ptr=0x141855EE0/id=0x80/ally/t0/CT=44/drop=none/seen=20189ms/PA=15 ptr=0x1418562E0/id=0x32/ally/t0/CT=46/drop=none/seen=71378ms/PA=14 ptr=0x1418564E0/id=0x1F/ally/t0/CT=66/drop=none/seen=22523ms/PA=5 attackerCandidates=none
  - `0x141855EE0` resolved=0x1418562E0 source=pending-clear pending=batch=1/act=258/event=2/16/confidence=damage-cache/score=1098832 fallback=none ctCandidates=none ctLowCandidates=none ctObserved=ptr=0x1418562E0/id=0x32/ally/t0/CT=46/drop=none/seen=71378ms/PA=14 ptr=0x1418564E0/id=0x1F/ally/t0/CT=66/drop=none/seen=22523ms/PA=5 attackerCandidates=none
- Runtime context events: 0
- Rewrite events: 0

### Neuter Placeholder Check
- No parsed HP rewrite lines with `vanillaDamage`.
- Verdict: **no HP placeholder evidence**

### HP Write Proof Check
- Concrete HP rewrites: 0
- Concrete `finalDamage=1` rewrites: 0/0
- HP rewrite failures: 0
- Baseline sample age: min=6ms max=6ms
- Verdict: **no concrete HP rewrites found**

### MP Rewrite Check
- MP events: loss=0, gain=0
- Parsed MP rewrite decisions: 0
- Concrete MP writes: 0
- Dry-run MP decisions: 0
- MP loss rewrites: 0
- MP gain rewrites: 0
- MP rewrite failures: 0
- MP rewrite skips/echo-skips: 0
- Verdict: **no MP rewrite evidence**
