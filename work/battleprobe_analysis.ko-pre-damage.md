# Battle Probe Analysis

Source: `C:\Program Files (x86)\Steam\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt`
Item catalog: `C:\Users\Dante\Documents\Projects\FFTGenericChronicle\work\item_catalog.csv` (261 ids)

## Header
- `==== Generic Chronicle Battle Runtime Harness (iter 20) ====`
- `settings path: C:\Reloaded-II\Mods\fftivc.generic.chronicle.codemod\battle-runtime-settings.json`

## Warnings
- No `[RUNTIME]` lines found. For mapping, install a settings file with `RewriteObservedDamage=true` and `LogResolvedRuntimeContext=true`, then create at least one damage event.

## Units
| Ptr | Id | Faction | Team | Stats |
| --- | --- | --- | --- | --- |
| `0x141855CE0` | `0x03` | ally | 0 | Lv51 HP446 MP76 PA14 MA11 Sp9 CT1 Mv7 Jp3 Br97 Fa70 |
| `0x141855EE0` | `0x80` | ally | 0 | Lv50 HP288 MP31 PA14 MA5 Sp13 CT101 Mv6 Jp4 Br96 Fa72 |
| `0x1418548E0` | `0x80` | foe | 3 | Lv48 HP349 MP42 PA7 MA6 Sp10 CT90 Mv4 Jp3 Br52 Fa67 |
| `0x141854EE0` | `0x81` | foe | 3 | Lv46 HP341 MP37 PA8 MA7 Sp10 CT90 Mv3 Jp3 Br68 Fa59 |
| `0x141853CE0` | `0x82` | foe | 3 | Lv51 HP401 MP1 PA12 MA35 Sp9 CT73 Mv3 Jp4 Br61 Fa67 |
| `0x141853EE0` | `0x82` | foe | 3 | Lv47 HP364 MP1 PA12 MA30 Sp9 CT73 Mv3 Jp4 Br53 Fa66 |
| `0x1418560E0` | `0x1E` | ally | 0 | Lv50 HP470 MP87 PA13 MA11 Sp9 CT93 Mv5 Jp3 Br95 Fa63 |
| `0x1418562E0` | `0x32` | ally | 0 | Lv49 HP378 MP92 PA10 MA13 Sp9 CT73 Mv4 Jp3 Br91 Fa65 |
| `0x1418564E0` | `0x1F` | ally | 0 | Lv50 HP455 MP102 PA12 MA12 Sp9 CT93 Mv7 Jp3 Br89 Fa65 |
| `0x1418544E0` | `0x82` | foe | 3 | Lv50 HP247 MP7 PA13 MA34 Sp8 CT56 Mv3 Jp3 Br71 Fa48 |

## Candidate Fields
### `0x141855CE0` `0x03` ally t0
`bytes[00-3F] +0x00=3 +0x01=16 +0x03=3 +0x05=11 +0x06=144 +0x07=3 +0x08=242 +0x09=80 +0x12=27 +0x13=7 +0x14=195 +0x15=1 +0x16=221 +0x17=1 | words<=511 +0x14w=451 +0x16w=477 +0x18w=488 +0x1Aw=154 +0x1Cw=184 +0x1Ew=218 +0x20w=35 +0x22w=255 +0x24w=33 +0x26w=255 || bytes[40-7F] +0x40=9 +0x41=4 +0x42=7 +0x43=3 +0x44=21 +0x45=16 +0x46=35 +0x47=60 +0x4B=10 +0x4F=8 +0x50=10 +0x51=1 +0x52=216 +0x53=64 | words<=511 +0x50w=266 +0x76w=2 || bytes[80-BF] +0x80=17 +0x81=130 +0x82=66 +0x83=2 +0x84=94 +0x85=136 +0x86=2 +0x87=86 +0x88=124 +0x89=2 +0x8A=11 +0x8B=120 +0x8C=11 +0x8D=110 | words<=511 +0x9Aw=1 +0x9Cw=32 +0xA8w=255 +0xAEw=195 +0xB2w=32 +0xBAw=168 || bytes[C0-FF] +0xC0=16 +0xC3=128 +0xC6=100 +0xC8=64 +0xC9=140 +0xCB=32 +0xCE=80 +0xCF=208 +0xD1=32 +0xD5=96 +0xD7=32 +0xE4=132 +0xE5=132 +0xE6=115 | words<=511 +0xC0w=16 +0xC6w=100 +0xECw=277 +0xEEw=1 +0xF2w=456 +0xF6w=493 +0xFAw=332 || bytes[100-13F] +0x100=57 +0x101=4 +0x102=197 +0x103=2 +0x104=50 +0x105=5 +0x106=132 +0x107=1 +0x108=97 +0x10A=129 +0x10C=218 +0x10E=192 +0x10F=5 +0x110=167 | words<=511 +0x106w=388 +0x108w=97 +0x10Aw=129 +0x10Cw=218 +0x110w=167 +0x112w=102 +0x116w=131 +0x128w=452 +0x134w=488 +0x13Ew=167 || bytes[140-17F] +0x140=22 +0x141=5 +0x144=131 | words<=511 +0x144w=131 || bytes[180-1BF] +0x18D=255 +0x18F=3 +0x191=3 +0x192=27 +0x1B5=1 +0x1B6=8 +0x1B8=1 +0x1BC=16 +0x1BE=1 | words<=511 +0x192w=27 +0x1B4w=256 +0x1B6w=8 +0x1B8w=1 +0x1BCw=16 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=11 +0x1F4=23 +0x1F5=255 +0x1FC=1 | words<=511 +0x1EAw=100 +0x1EEw=11 +0x1FCw=1`
### `0x141855EE0` `0x80` ally t0
`bytes[00-3F] +0x00=128 +0x01=17 +0x02=3 +0x03=89 +0x05=8 +0x06=144 +0x07=3 +0x08=210 +0x09=64 +0x0A=221 +0x0B=1 +0x12=20 +0x13=6 +0x14=195 | words<=511 +0x0Aw=477 +0x14w=451 +0x16w=474 +0x18w=487 +0x1Aw=168 +0x1Cw=195 +0x1Ew=236 +0x20w=15 +0x22w=255 +0x24w=15 || bytes[40-7F] +0x40=13 +0x41=4 +0x42=6 +0x43=4 +0x44=13 +0x45=13 +0x46=5 +0x47=5 +0x4B=30 +0x4F=9 +0x50=9 +0x51=1 +0x52=224 +0x53=64 | words<=511 +0x50w=265 +0x5Aw=64 +0x60w=12 +0x64w=64 || bytes[80-BF] +0x80=15 +0x81=90 +0x82=75 +0x83=2 +0x84=224 +0x85=142 +0x86=2 +0x87=245 +0x88=245 +0x89=1 +0x8A=12 +0x8B=70 +0x8C=13 +0x8D=50 | words<=511 +0x88w=501 +0x9Aw=9 +0x9Cw=64 +0xA0w=207 +0xA2w=96 +0xA8w=4 +0xAEw=255 +0xB0w=248 || bytes[C0-FF] +0xC6=32 +0xC8=64 +0xC9=140 +0xCB=64 +0xCE=64 +0xCF=254 +0xD1=240 +0xE4=120 +0xE5=101 +0xE6=130 +0xE7=68 +0xE8=72 +0xE9=50 +0xEA=70 | words<=511 +0xC6w=32 +0xECw=274 +0xEEw=2 +0xF2w=325 +0xF6w=175 +0xF8w=334 +0xFAw=287 || bytes[100-13F] +0x100=35 +0x101=3 +0x102=252 +0x103=4 +0x104=106 +0x105=2 +0x106=32 +0x107=1 +0x108=2 +0x109=1 +0x10A=178 +0x10B=1 +0x10C=237 +0x10D=3 | words<=511 +0x106w=288 +0x108w=258 +0x10Aw=434 +0x110w=159 +0x112w=251 +0x116w=150 +0x128w=287 +0x134w=288 +0x13Ew=159 || bytes[140-17F] +0x140=251 +0x144=150 +0x14C=82 +0x14D=105 +0x14E=111 +0x14F=110 +0x151=119 +0x152=117 +0x153=108 +0x154=102 | words<=511 +0x140w=251 +0x144w=150 +0x154w=102 || bytes[180-1BF] +0x18D=255 +0x18F=126 +0x191=120 +0x1B5=1 +0x1B8=1 +0x1BC=17 +0x1BE=1 | words<=511 +0x1B4w=256 +0x1B8w=1 +0x1BCw=17 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=8 +0x1F4=47 +0x1F5=255 +0x1FC=101 +0x1FD=1 | words<=511 +0x1EAw=100 +0x1EEw=8 +0x1FCw=357`
### `0x1418548E0` `0x80` foe t3
`bytes[00-3F] +0x00=128 +0x01=6 +0x02=255 +0x03=74 +0x04=3 +0x05=80 +0x06=128 +0x07=3 +0x08=67 +0x09=176 +0x12=5 +0x1A=168 +0x1C=198 +0x1E=232 | words<=511 +0x12w=5 +0x1Aw=168 +0x1Cw=198 +0x1Ew=232 +0x20w=50 +0x22w=255 +0x24w=255 +0x26w=255 +0x2Ew=1 +0x30w=349 || bytes[40-7F] +0x40=10 +0x42=4 +0x43=3 +0x44=16 +0x48=25 +0x4B=5 +0x4C=25 +0x4F=2 +0x50=3 +0x51=3 +0x52=210 +0x53=64 +0x54=6 +0x55=191 | words<=511 +0x40w=10 +0x44w=16 +0x48w=25 +0x4Cw=25 +0x60w=12 || bytes[80-BF] +0x80=14 +0x81=163 +0x82=50 +0x83=2 +0x84=98 +0x85=54 +0x86=2 +0x87=235 +0x88=235 +0x89=1 +0x8A=11 +0x8B=100 +0x8C=15 +0x8D=75 | words<=511 +0x88w=491 +0xA2w=96 || bytes[C0-FF] +0xE4=33 +0xE5=17 +0xE6=17 +0xE7=17 +0xE8=17 +0xE9=17 +0xEA=17 +0xEB=17 +0xEC=17 +0xED=1 +0xF0=110 +0xF2=45 +0xF4=196 +0xF6=19 | words<=511 +0xECw=273 +0xF0w=110 +0xF2w=45 +0xF4w=196 +0xF6w=19 +0xF8w=163 +0xFAw=185 +0xFCw=138 +0xFEw=114 || bytes[100-13F] +0x100=158 +0x102=189 +0x104=199 +0x106=181 +0x108=174 +0x10A=185 +0x10C=169 +0x10E=116 +0x110=189 +0x112=155 +0x116=166 +0x11E=24 +0x11F=1 +0x120=155 | words<=511 +0x100w=158 +0x102w=189 +0x104w=199 +0x106w=181 +0x108w=174 +0x10Aw=185 +0x10Cw=169 +0x10Ew=116 +0x110w=189 +0x112w=155 || bytes[140-17F] +0x140=155 +0x144=166 | words<=511 +0x140w=155 +0x144w=166 || bytes[180-1BF] +0x18D=255 +0x18F=96 +0x191=248 +0x1B5=1 +0x1B8=1 +0x1BC=6 +0x1BE=1 | words<=511 +0x1B4w=256 +0x1B8w=1 +0x1BCw=6 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=80 +0x1F5=255 +0x1FC=154 +0x1FD=1 | words<=511 +0x1EAw=100 +0x1EEw=80 +0x1FCw=410`
### `0x141854EE0` `0x81` foe t3
`bytes[00-3F] +0x00=129 +0x01=9 +0x02=255 +0x03=77 +0x04=3 +0x05=80 +0x06=64 +0x07=3 +0x08=172 +0x09=32 +0x12=8 +0x13=5 +0x16=224 +0x17=1 | words<=511 +0x16w=480 +0x1Aw=168 +0x1Cw=198 +0x1Ew=232 +0x20w=89 +0x22w=255 +0x24w=255 +0x26w=255 +0x2Ew=1 +0x30w=341 || bytes[40-7F] +0x40=10 +0x42=3 +0x43=3 +0x44=10 +0x48=25 +0x4B=10 +0x4C=25 +0x50=5 +0x51=3 +0x52=128 +0x53=24 +0x54=22 +0x55=191 +0x5F=2 | words<=511 +0x40w=10 +0x44w=10 +0x48w=25 +0x4Cw=25 +0x60w=12 || bytes[80-BF] +0x80=14 +0x81=9 +0x82=43 +0x83=2 +0x84=80 +0x85=250 +0x86=1 +0x87=78 +0x88=90 +0x89=2 +0x8A=11 +0x8B=100 +0x8C=16 +0x8D=65 | words<=511 +0xA2w=96 || bytes[C0-FF] +0xE4=33 +0xE5=18 +0xE6=17 +0xE7=17 +0xE8=17 +0xE9=17 +0xEA=17 +0xEB=17 +0xEC=16 +0xED=17 +0xF0=116 +0xF2=5 +0xF4=110 +0xF6=148 | words<=511 +0xF0w=116 +0xF2w=5 +0xF4w=110 +0xF6w=148 +0xF8w=120 +0xFAw=128 +0xFCw=144 +0xFEw=102 || bytes[100-13F] +0x100=152 +0x102=146 +0x104=123 +0x106=133 +0x108=113 +0x10A=100 +0x10C=181 +0x10E=170 +0x110=177 +0x114=198 +0x116=131 +0x11E=30 +0x11F=1 +0x120=155 | words<=511 +0x100w=152 +0x102w=146 +0x104w=123 +0x106w=133 +0x108w=113 +0x10Aw=100 +0x10Cw=181 +0x10Ew=170 +0x110w=177 +0x114w=198 || bytes[140-17F] +0x142=198 +0x144=131 | words<=511 +0x142w=198 +0x144w=131 || bytes[180-1BF] +0x18D=255 +0x18F=103 +0x191=245 +0x1B5=1 +0x1B8=1 +0x1BC=9 +0x1BE=1 | words<=511 +0x1B4w=256 +0x1B8w=1 +0x1BCw=9 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=80 +0x1F5=255 +0x1FC=93 +0x1FD=2 | words<=511 +0x1EAw=100 +0x1EEw=80`
### `0x141853CE0` `0x82` foe t3
`bytes[00-3F] +0x00=130 +0x02=255 +0x03=109 +0x04=3 +0x05=144 +0x06=32 +0x07=3 +0x08=31 +0x09=97 +0x0E=186 +0x0F=1 +0x12=191 +0x14=186 +0x15=1 | words<=511 +0x00w=130 +0x0Ew=442 +0x12w=191 +0x14w=442 +0x22w=255 +0x26w=255 +0x2Ew=1 +0x30w=401 +0x32w=401 +0x34w=1 || bytes[40-7F] +0x40=9 +0x41=8 +0x42=3 +0x43=4 +0x4B=11 +0x50=3 +0x51=3 +0x52=128 +0x57=16 +0x5D=4 +0x5E=32 +0x5F=192 +0x61=16 +0x76=1 | words<=511 +0x52w=128 +0x76w=1 || bytes[80-BF] +0x80=5 +0x81=240 +0x82=249 +0x83=1 +0x84=130 +0x85=221 +0x86=2 +0x87=217 +0x88=121 +0x89=10 +0x8A=5 +0x8B=115 +0x8C=30 +0x8D=5 | words<=511 +0x82w=505 +0x96w=8 || bytes[C0-FF] none | words<=511 none || bytes[100-13F] none | words<=511 none || bytes[140-17F] none | words<=511 none || bytes[180-1BF] +0x18D=255 +0x18E=6 +0x18F=139 +0x191=254 +0x1B5=1 +0x1B8=1 +0x1BE=1 | words<=511 +0x1B4w=256 +0x1B8w=1 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=144 +0x1F5=255 +0x1FC=110 +0x1FD=3 | words<=511 +0x1EAw=100 +0x1EEw=144`
### `0x141853EE0` `0x82` foe t3
`bytes[00-3F] +0x00=130 +0x01=1 +0x02=255 +0x03=109 +0x04=3 +0x05=80 +0x06=32 +0x07=3 +0x08=139 +0x09=16 +0x0E=186 +0x0F=1 +0x12=191 +0x14=186 | words<=511 +0x00w=386 +0x0Ew=442 +0x12w=191 +0x14w=442 +0x22w=255 +0x26w=255 +0x2Ew=1 +0x30w=364 +0x32w=364 +0x34w=1 || bytes[40-7F] +0x40=9 +0x41=8 +0x42=3 +0x43=4 +0x4B=11 +0x4F=1 +0x50=2 +0x51=3 +0x52=128 +0x57=16 +0x5D=4 +0x5E=32 +0x5F=192 +0x61=16 | words<=511 +0x4Ew=256 +0x52w=128 +0x76w=1 || bytes[80-BF] +0x80=5 +0x81=16 +0x82=235 +0x83=1 +0x84=18 +0x85=255 +0x86=2 +0x87=59 +0x88=26 +0x89=9 +0x8A=5 +0x8B=115 +0x8C=30 +0x8D=5 | words<=511 +0x82w=491 +0x96w=8 || bytes[C0-FF] none | words<=511 none || bytes[100-13F] none | words<=511 none || bytes[140-17F] none | words<=511 none || bytes[180-1BF] +0x18D=255 +0x18E=6 +0x18F=139 +0x191=253 +0x1B5=1 +0x1B8=1 +0x1BC=1 +0x1BE=1 | words<=511 +0x1B4w=256 +0x1B8w=1 +0x1BCw=1 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=80 +0x1F5=255 +0x1FC=221 +0x1FD=3 | words<=511 +0x1EAw=100 +0x1EEw=80`
### `0x1418560E0` `0x1E` ally t0
`bytes[00-3F] +0x00=30 +0x01=18 +0x02=23 +0x03=30 +0x05=8 +0x06=80 +0x07=3 +0x08=173 +0x09=48 +0x12=40 +0x13=6 +0x14=195 +0x15=1 +0x16=209 | words<=511 +0x14w=451 +0x16w=465 +0x18w=487 +0x1Aw=155 +0x1Cw=183 +0x1Ew=216 +0x20w=34 +0x22w=255 +0x24w=255 +0x26w=140 || bytes[40-7F] +0x40=9 +0x41=8 +0x42=5 +0x43=3 +0x44=18 +0x46=30 +0x4A=43 +0x4B=25 +0x4F=10 +0x50=9 +0x51=1 +0x52=152 +0x54=27 +0x55=127 | words<=511 +0x44w=18 +0x46w=30 +0x50w=265 +0x52w=152 +0x5Aw=32 +0x64w=32 || bytes[80-BF] +0x80=21 +0x81=232 +0x82=117 +0x83=2 +0x84=149 +0x85=200 +0x86=2 +0x87=37 +0x88=106 +0x89=2 +0x8A=10 +0x8B=140 +0x8C=11 +0x8D=100 | words<=511 +0x9Cw=64 +0xA0w=207 +0xA2w=248 +0xA8w=4 +0xAEw=130 +0xB0w=72 || bytes[C0-FF] +0xC8=64 +0xCC=128 +0xCE=64 +0xCF=255 +0xD0=240 +0xD1=240 +0xE4=134 +0xE5=100 +0xE6=82 +0xE7=51 +0xE8=37 +0xE9=34 +0xEA=50 +0xEB=72 | words<=511 +0xC8w=64 +0xCCw=128 +0xEEw=1 +0xF2w=185 +0xF6w=254 +0xFAw=220 || bytes[100-13F] +0x100=132 +0x101=1 +0x102=51 +0x103=2 +0x104=110 +0x105=1 +0x106=206 +0x108=42 +0x10A=252 +0x10C=152 +0x10E=202 +0x10F=12 +0x110=153 +0x114=153 | words<=511 +0x100w=388 +0x104w=366 +0x106w=206 +0x108w=42 +0x10Aw=252 +0x10Cw=152 +0x110w=153 +0x114w=153 +0x116w=113 +0x128w=220 || bytes[140-17F] +0x142=153 +0x144=113 | words<=511 +0x142w=153 +0x144w=113 || bytes[180-1BF] +0x18D=255 +0x18F=30 +0x191=121 +0x192=40 +0x1B5=1 +0x1B6=8 +0x1B8=1 +0x1BC=18 +0x1BE=1 | words<=511 +0x192w=40 +0x1B4w=256 +0x1B6w=8 +0x1B8w=1 +0x1BCw=18 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=8 +0x1F4=33 +0x1F5=255 +0x1FC=30 | words<=511 +0x1EAw=100 +0x1EEw=8 +0x1FCw=30`
### `0x1418562E0` `0x32` ally t0
`bytes[00-3F] +0x00=50 +0x01=19 +0x02=32 +0x03=50 +0x05=8 +0x06=144 +0x07=3 +0x08=31 +0x09=160 +0x12=41 +0x13=6 +0x14=189 +0x15=1 +0x16=226 | words<=511 +0x14w=445 +0x16w=482 +0x18w=494 +0x1Aw=167 +0x1Cw=198 +0x1Ew=214 +0x20w=256 +0x22w=255 +0x24w=255 +0x26w=255 || bytes[40-7F] +0x40=9 +0x41=8 +0x42=4 +0x43=3 +0x44=16 +0x46=10 +0x4B=20 +0x4F=10 +0x50=10 +0x51=1 +0x52=144 +0x54=6 +0x55=191 +0x5D=64 | words<=511 +0x44w=16 +0x46w=10 +0x50w=266 +0x52w=144 || bytes[80-BF] +0x80=19 +0x81=112 +0x82=54 +0x83=2 +0x84=22 +0x85=105 +0x86=2 +0x87=240 +0x88=240 +0x89=1 +0x8A=11 +0x8B=125 +0x8C=10 +0x8D=100 | words<=511 +0x88w=496 +0x96w=1 +0xA0w=222 +0xA2w=76 +0xA6w=4 +0xA8w=65 +0xAEw=170 +0xB2w=128 || bytes[C0-FF] +0xC5=16 +0xC9=128 +0xCC=136 +0xCE=240 +0xE4=115 +0xE5=83 +0xE6=99 +0xE7=54 +0xE8=36 +0xE9=18 +0xEA=18 +0xEB=132 +0xEC=17 +0xED=1 | words<=511 +0xCCw=136 +0xCEw=240 +0xECw=273 +0xEEw=1 +0xF2w=296 +0xF6w=62 +0xFAw=385 +0xFCw=270 +0xFEw=53 || bytes[100-13F] +0x100=224 +0x102=66 +0x103=1 +0x104=192 +0x106=37 +0x108=128 +0x10A=89 +0x10C=145 +0x10D=1 +0x10E=220 +0x10F=2 +0x110=168 +0x112=116 +0x116=190 | words<=511 +0x100w=224 +0x102w=322 +0x104w=192 +0x106w=37 +0x108w=128 +0x10Aw=89 +0x10Cw=401 +0x110w=168 +0x112w=116 +0x116w=190 || bytes[140-17F] +0x140=116 +0x144=190 | words<=511 +0x140w=116 +0x144w=190 || bytes[180-1BF] +0x18D=255 +0x18F=50 +0x191=122 +0x192=41 +0x1B5=1 +0x1B6=12 +0x1B8=1 +0x1BC=19 +0x1BE=1 | words<=511 +0x192w=41 +0x1B4w=256 +0x1B6w=12 +0x1B8w=1 +0x1BCw=19 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=8 +0x1F4=3 +0x1F5=255 +0x1FC=50 | words<=511 +0x1EAw=100 +0x1EEw=8 +0x1FCw=50`
### `0x1418564E0` `0x1F` ally t0
`bytes[00-3F] +0x00=31 +0x01=20 +0x02=14 +0x03=31 +0x05=8 +0x06=144 +0x07=3 +0x08=21 +0x09=97 +0x12=69 +0x13=9 +0x14=195 +0x15=1 +0x16=207 | words<=511 +0x14w=451 +0x16w=463 +0x18w=488 +0x1Aw=154 +0x1Cw=184 +0x1Ew=217 +0x20w=30 +0x22w=255 +0x24w=255 +0x26w=139 || bytes[40-7F] +0x40=9 +0x41=8 +0x42=7 +0x43=3 +0x44=14 +0x46=15 +0x4A=40 +0x4B=14 +0x4E=15 +0x4F=10 +0x50=8 +0x51=1 +0x52=216 +0x54=27 | words<=511 +0x44w=14 +0x46w=15 +0x50w=264 +0x52w=216 || bytes[80-BF] +0x80=17 +0x81=208 +0x82=58 +0x83=2 +0x84=172 +0x85=129 +0x86=2 +0x87=189 +0x88=13 +0x89=2 +0x8A=10 +0x8B=122 +0x8C=11 +0x8D=145 | words<=511 +0x9Cw=32 +0xAAw=16 +0xAEw=7 +0xBAw=242 || bytes[C0-FF] +0xC0=205 +0xC1=64 +0xC2=16 +0xC3=130 +0xC4=16 +0xCE=64 +0xD5=64 +0xD7=32 +0xE4=131 +0xE5=83 +0xE6=83 +0xE7=51 +0xE8=85 +0xE9=83 | words<=511 +0xC4w=16 +0xCEw=64 +0xECw=277 +0xEEw=1 +0xF2w=229 +0xF6w=427 +0xF8w=382 +0xFAw=69 +0xFCw=155 +0xFEw=232 || bytes[100-13F] +0x100=191 +0x102=163 +0x103=4 +0x104=1 +0x106=95 +0x108=211 +0x10A=47 +0x10B=1 +0x10C=34 +0x10E=149 +0x10F=2 +0x110=197 +0x112=72 +0x116=177 | words<=511 +0x100w=191 +0x104w=1 +0x106w=95 +0x108w=211 +0x10Aw=303 +0x10Cw=34 +0x110w=197 +0x112w=72 +0x116w=177 +0x124w=427 || bytes[140-17F] +0x140=148 +0x141=4 +0x144=177 | words<=511 +0x144w=177 || bytes[180-1BF] +0x18D=255 +0x18F=31 +0x191=123 +0x192=69 +0x1B5=1 +0x1B6=8 +0x1B8=1 +0x1BC=20 +0x1BE=1 | words<=511 +0x192w=69 +0x1B4w=256 +0x1B6w=8 +0x1B8w=1 +0x1BCw=20 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=8 +0x1F4=1 +0x1F5=255 +0x1FC=31 | words<=511 +0x1EAw=100 +0x1EEw=8 +0x1FCw=31`
### `0x1418544E0` `0x82` foe t3
`bytes[00-3F] +0x00=130 +0x01=4 +0x02=255 +0x03=98 +0x04=3 +0x05=80 +0x06=32 +0x07=3 +0x08=47 +0x09=113 +0x0E=186 +0x0F=1 +0x12=180 +0x14=186 | words<=511 +0x0Ew=442 +0x12w=180 +0x14w=442 +0x22w=255 +0x26w=255 +0x2Ew=1 +0x30w=247 +0x32w=247 +0x34w=7 +0x36w=7 || bytes[40-7F] +0x40=8 +0x41=4 +0x42=3 +0x43=3 +0x4B=19 +0x4F=3 +0x50=2 +0x51=3 +0x52=128 +0x5D=4 +0x79=32 +0x7B=60 +0x7C=6 +0x7D=72 | words<=511 +0x52w=128 || bytes[80-BF] +0x80=5 +0x81=56 +0x82=246 +0x83=1 +0x84=206 +0x85=42 +0x86=3 +0x87=4 +0x88=208 +0x89=9 +0x8A=6 +0x8B=86 +0x8C=30 +0x8D=35 | words<=511 +0x82w=502 +0x96w=8 || bytes[C0-FF] none | words<=511 none || bytes[100-13F] none | words<=511 none || bytes[140-17F] none | words<=511 none || bytes[180-1BF] +0x18D=255 +0x18E=2 +0x18F=135 +0x190=1 +0x191=250 +0x1B5=1 +0x1B8=1 +0x1BC=4 +0x1BE=1 | words<=511 +0x1B4w=256 +0x1B8w=1 +0x1BCw=4 +0x1BEw=1 || bytes[1C0-1FF] +0x1EA=100 +0x1EE=80 +0x1F5=255 +0x1FC=27 +0x1FD=3 | words<=511 +0x1EAw=100 +0x1EEw=80`

## Candidate Item Hits
### `0x141855CE0` `0x03` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 21 Iron Sword | Weapon | Sword | weapon |
| `+0x45` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 35 Excalibur | Rare, Weapon | KnightSword | weapon |
| `+0x47` | byte | 60 White Staff | Weapon | Staff | weapon |
| `+0x4B` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x4F` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x50` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 216 Genji Gloves | Rare, Accessory | Armguard | accessory |
| `+0x53` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x54` | byte | 31 Nagnarok | Rare, Weapon | Sword | weapon |
| `+0x55` | byte | 255 | Rare | None |  |
| `+0x5A` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x5B` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x5D` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x64` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x65` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x76` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x7A` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x7B` | byte | 21 Iron Sword | Weapon | Sword | weapon |
| `+0x7C` | byte | 228 Guardian Bracelet | Accessory | Armlet | accessory |
| `+0x7D` | byte | 40 Bizen Osafune | Weapon | Katana | weapon |
| `+0x7E` | byte | 63 Mage's Staff | Weapon | Staff | weapon |
| `+0x7F` | byte | 76 Blaster | Rare, Weapon | Gun | weapon |
| `+0x80` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x81` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0x82` | byte | 66 Staff of the Magi | Rare, Weapon | Staff | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 94 Faerie Harp | Rare, Weapon | Instrument | weapon |
| `+0x85` | byte | 136 Aegis Shield | Shield | Shield | shield |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 86 Lightning Bow | Weapon | Bow | weapon |
| `+0x88` | byte | 124 Yagyu Darkrood | Weapon | Throwing | weapon |
| `+0x89` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 120 Cashmere | Weapon | Cloth | weapon |
| `+0x8C` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x8D` | byte | 110 Iron Fan | Weapon | Pole | weapon |
| `+0x8E` | byte | 95 Battle Folio | Weapon | Book | weapon |
| `+0x8F` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x90` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x91` | byte | 115 Catskin Bag | Rare, Weapon | Bag | weapon |
| `+0x92` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0x93` | byte | 115 Catskin Bag | Rare, Weapon | Bag | weapon |
| `+0x97` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x9A` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x9C` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 255 | Rare | None |  |
| `+0xA1` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xA2` | byte | 255 | Rare | None |  |
| `+0xA3` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xA4` | byte | 252 Remedy | Rare | Item |  |
| `+0xA5` | byte | 131 Round Shield | Shield | Shield | shield |
| `+0xA6` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xA7` | byte | 144 Leather Helm | Headgear | Helmet | armor |
| `+0xA8` | byte | 255 | Rare | None |  |
| `+0xAA` | byte | 240 Potion | Rare | Item |  |
| `+0xAB` | byte | 242 X-Potion | Rare | Item |  |
| `+0xAD` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAE` | byte | 195 Power Garb | Armor | Clothing | armor |
| `+0xB1` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xB2` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xB4` | byte | 140 Genji Shield | Rare, Shield | Shield | shield |
| `+0xB5` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xB7` | byte | 192 Wizard Clothing | Armor | Clothing | armor |
| `+0xBA` | byte | 168 Thief's Cap | Headgear | Hat | armor |
| `+0xBD` | byte | 174 Bronze Armor | Armor | Armor | armor |
| `+0xBF` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0xC0` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xC3` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xC6` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0xC8` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xC9` | byte | 140 Genji Shield | Rare, Shield | Shield | shield |
| `+0xCB` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xCE` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0xCF` | byte | 208 Battle Boots | Accessory | Shoes | accessory |
| `+0xD1` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xD5` | byte | 96 Bestiary | Weapon | Book | weapon |
| `+0xD7` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |

_Truncated: 148 more dump item-id hits._
### `0x141855EE0` `0x80` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 13 Kodachi | Weapon | NinjaBlade | weapon |
| `+0x45` | byte | 13 Kodachi | Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
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
| `+0x63` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x64` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x7B` | byte | 138 Platinum Shield | Shield | Shield | shield |
| `+0x7C` | byte | 66 Staff of the Magi | Rare, Weapon | Staff | weapon |
| `+0x7D` | byte | 42 Ame-no-Murakumo | Weapon | Katana | weapon |
| `+0x7E` | byte | 99 Javelin | Weapon | Polearm | weapon |
| `+0x7F` | byte | 201 Silken Robe | Armor | Robe | armor |
| `+0x80` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x81` | byte | 90 Yoichi Bow | Rare, Weapon | Bow | weapon |
| `+0x82` | byte | 75 Blaze Gun | Rare, Weapon | Gun | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 224 Diamond Bracelet | Accessory | Armlet | accessory |
| `+0x85` | byte | 142 Venetian Shield | Rare, Shield | Shield | shield |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 245 Elixir | Rare | Item |  |
| `+0x88` | byte | 245 Elixir | Rare | Item |  |
| `+0x89` | byte | 1 Dagger | Weapon | Knife | weapon |
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
| `+0x97` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x9A` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x9C` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 207 Lordly Robe | Rare, Armor | Robe | armor |
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
| `+0xB4` | byte | 200 Hempen Robe | Armor | Robe | armor |
| `+0xB5` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xBD` | byte | 255 | Rare | None |  |
| `+0xBF` | byte | 252 Remedy | Rare | Item |  |
| `+0xC6` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xC8` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xC9` | byte | 140 Genji Shield | Rare, Shield | Shield | shield |
| `+0xCB` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xCE` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xCF` | byte | 254 | Rare | None |  |
| `+0xD1` | byte | 240 Potion | Rare | Item |  |
| `+0xE4` | byte | 120 Cashmere | Weapon | Cloth | weapon |
| `+0xE5` | byte | 101 Mythril Spear | Weapon | Polearm | weapon |
| `+0xE6` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0xE7` | byte | 68 Flail of Flame | Weapon | Flail | weapon |
| `+0xE8` | byte | 72 Mythril Gun | Rare, Weapon | Gun | weapon |
| `+0xE9` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0xEA` | byte | 70 Scorpion Tail | Rare, Weapon | Flail | weapon |
| `+0xEB` | byte | 104 Holy Lance | Rare, Weapon | Polearm | weapon |
| `+0xEC` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xED` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xEE` | byte | 2 Mythril Knife | Weapon | Knife | weapon |

_Truncated: 139 more dump item-id hits._
### `0x1418548E0` `0x80` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x48` | byte | 25 Ancient Sword | Weapon | Sword | weapon |
| `+0x4B` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x4C` | byte | 25 Ancient Sword | Weapon | Sword | weapon |
| `+0x4F` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x50` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 210 Germinas Boots | Accessory | Shoes | accessory |
| `+0x53` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x54` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x55` | byte | 191 Adamant Vest | Armor | Clothing | armor |
| `+0x5F` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x60` | byte | 12 Kunai | Weapon | NinjaBlade | weapon |
| `+0x7B` | byte | 170 Barrette | Rare, Headgear | HairAdornment | armor |
| `+0x7C` | byte | 125 Flameburst Bomb | Weapon | Bomb | weapon |
| `+0x7D` | byte | 37 Chaos Blade | Rare, Weapon | KnightSword | weapon |
| `+0x7E` | byte | 245 Elixir | Rare | Item |  |
| `+0x7F` | byte | 42 Ame-no-Murakumo | Weapon | Katana | weapon |
| `+0x80` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x81` | byte | 163 Headband | Headgear | Hat | armor |
| `+0x82` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 98 Omnilex | Rare, Weapon | Book | weapon |
| `+0x85` | byte | 54 Ice Rod | Weapon | Rod | weapon |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 235 Invisibility Cloak | Rare, Accessory | Cloak | accessory |
| `+0x88` | byte | 235 Invisibility Cloak | Rare, Accessory | Cloak | accessory |
| `+0x89` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x8A` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8C` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x8D` | byte | 75 Blaze Gun | Rare, Weapon | Gun | weapon |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x90` | byte | 60 White Staff | Weapon | Staff | weapon |
| `+0x91` | byte | 90 Yoichi Bow | Rare, Weapon | Bow | weapon |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x9F` | byte | 240 Potion | Rare | Item |  |
| `+0xA2` | byte | 96 Bestiary | Weapon | Book | weapon |
| `+0xA5` | byte | 129 Buckler | Shield | Shield | shield |
| `+0xAB` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xE4` | byte | 33 Defender | Rare, Weapon | KnightSword | weapon |
| `+0xE5` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xE6` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xE7` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xE8` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xE9` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEA` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEB` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEC` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xED` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xF0` | byte | 110 Iron Fan | Weapon | Pole | weapon |
| `+0xF2` | byte | 45 Kiku-ichimonji | Weapon | Katana | weapon |
| `+0xF4` | byte | 196 Gaia Gear | Armor | Clothing | armor |
| `+0xF6` | byte | 19 Broadsword | Weapon | Sword | weapon |
| `+0xF8` | byte | 163 Headband | Headgear | Hat | armor |
| `+0xFA` | byte | 185 Maximillian | Rare, Armor | Armor | armor |
| `+0xFC` | byte | 138 Platinum Shield | Shield | Shield | shield |
| `+0xFE` | byte | 114 Whale Whisker | Rare, Weapon | Pole | weapon |
| `+0x100` | byte | 158 Plumed Hat | Headgear | Hat | armor |
| `+0x102` | byte | 189 Ringmail | Armor | Clothing | armor |
| `+0x104` | byte | 199 Rubber Suit | Rare, Armor | Clothing | armor |
| `+0x106` | byte | 181 Carabineer Mail | Armor | Armor | armor |
| `+0x108` | byte | 174 Bronze Armor | Armor | Armor | armor |
| `+0x10A` | byte | 185 Maximillian | Rare, Armor | Armor | armor |
| `+0x10C` | byte | 169 Hairband | Rare, Headgear | HairAdornment | armor |
| `+0x10E` | byte | 116 Fallingstar Bag | Rare, Weapon | Bag | weapon |
| `+0x110` | byte | 189 Ringmail | Armor | Clothing | armor |
| `+0x112` | byte | 155 Genji Helm | Rare, Headgear | Helmet | armor |
| `+0x116` | byte | 166 Gold Hairpin | Headgear | Hat | armor |
| `+0x11E` | byte | 24 Coral Sword | Weapon | Sword | weapon |
| `+0x11F` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x120` | byte | 155 Genji Helm | Rare, Headgear | Helmet | armor |
| `+0x122` | byte | 196 Gaia Gear | Armor | Clothing | armor |
| `+0x124` | byte | 169 Hairband | Rare, Headgear | HairAdornment | armor |
| `+0x126` | byte | 163 Headband | Headgear | Hat | armor |
| `+0x128` | byte | 185 Maximillian | Rare, Armor | Armor | armor |
| `+0x12A` | byte | 138 Platinum Shield | Shield | Shield | shield |
| `+0x12C` | byte | 114 Whale Whisker | Rare, Weapon | Pole | weapon |

_Truncated: 85 more dump item-id hits._
### `0x141854EE0` `0x81` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x48` | byte | 25 Ancient Sword | Weapon | Sword | weapon |
| `+0x4B` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x4C` | byte | 25 Ancient Sword | Weapon | Sword | weapon |
| `+0x50` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x53` | byte | 24 Coral Sword | Weapon | Sword | weapon |
| `+0x54` | byte | 22 Mythril Sword | Weapon | Sword | weapon |
| `+0x55` | byte | 191 Adamant Vest | Armor | Clothing | armor |
| `+0x5F` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x60` | byte | 12 Kunai | Weapon | NinjaBlade | weapon |
| `+0x7B` | byte | 177 Plate Mail | Armor | Armor | armor |
| `+0x7C` | byte | 109 Musk Pole | Weapon | Pole | weapon |
| `+0x7D` | byte | 35 Excalibur | Rare, Weapon | KnightSword | weapon |
| `+0x7E` | byte | 227 Nu Khai Armband | Accessory | Armlet | accessory |
| `+0x7F` | byte | 124 Yagyu Darkrood | Weapon | Throwing | weapon |
| `+0x80` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x81` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x82` | byte | 43 Kiyomori | Weapon | Katana | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x85` | byte | 250 Gold Needle | Rare | Item |  |
| `+0x86` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x87` | byte | 78 Knightslayer | Rare, Weapon | Crossbow | weapon |
| `+0x88` | byte | 90 Yoichi Bow | Rare, Weapon | Bow | weapon |
| `+0x89` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8C` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x8D` | byte | 65 Zeus Mace | Rare, Weapon | Staff | weapon |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x90` | byte | 45 Kiku-ichimonji | Weapon | Katana | weapon |
| `+0x91` | byte | 110 Iron Fan | Weapon | Pole | weapon |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x9B` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x9F` | byte | 240 Potion | Rare | Item |  |
| `+0xA2` | byte | 96 Bestiary | Weapon | Book | weapon |
| `+0xA5` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0xA7` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAB` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xE4` | byte | 33 Defender | Rare, Weapon | KnightSword | weapon |
| `+0xE5` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xE6` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xE7` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xE8` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xE9` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEA` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEB` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEC` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xED` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xF0` | byte | 116 Fallingstar Bag | Rare, Weapon | Bag | weapon |
| `+0xF2` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0xF4` | byte | 110 Iron Fan | Weapon | Pole | weapon |
| `+0xF6` | byte | 148 Mythril Helm | Headgear | Helmet | armor |
| `+0xF8` | byte | 120 Cashmere | Weapon | Cloth | weapon |
| `+0xFA` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xFC` | byte | 144 Leather Helm | Headgear | Helmet | armor |
| `+0xFE` | byte | 102 Partisan | Weapon | Polearm | weapon |
| `+0x100` | byte | 152 Platinum Helm | Headgear | Helmet | armor |
| `+0x102` | byte | 146 Iron Helm | Headgear | Helmet | armor |
| `+0x104` | byte | 123 Fuma Shuriken | Weapon | Throwing | weapon |
| `+0x106` | byte | 133 Golden Shield | Shield | Shield | shield |
| `+0x108` | byte | 113 Eight-fluted Pole | Weapon | Pole | weapon |
| `+0x10A` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x10C` | byte | 181 Carabineer Mail | Armor | Armor | armor |
| `+0x10E` | byte | 170 Barrette | Rare, Headgear | HairAdornment | armor |
| `+0x110` | byte | 177 Plate Mail | Armor | Armor | armor |
| `+0x114` | byte | 198 Black Garb | Armor | Clothing | armor |
| `+0x116` | byte | 131 Round Shield | Shield | Shield | shield |
| `+0x11E` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x11F` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x120` | byte | 155 Genji Helm | Rare, Headgear | Helmet | armor |
| `+0x122` | byte | 110 Iron Fan | Weapon | Pole | weapon |
| `+0x124` | byte | 248 Echo Herbs | Rare | Item |  |
| `+0x126` | byte | 120 Cashmere | Weapon | Cloth | weapon |
| `+0x128` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x12A` | byte | 144 Leather Helm | Headgear | Helmet | armor |

_Truncated: 88 more dump item-id hits._
### `0x141853CE0` `0x82` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x4B` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x50` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x57` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x5E` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x5F` | byte | 192 Wizard Clothing | Armor | Clothing | armor |
| `+0x61` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x76` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x79` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0x7B` | byte | 242 X-Potion | Rare | Item |  |
| `+0x7C` | byte | 47 Chirijiraden | Rare, Weapon | Katana | weapon |
| `+0x7D` | byte | 87 Windslash Bow | Weapon | Bow | weapon |
| `+0x7E` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x7F` | byte | 153 Circlet | Headgear | Helmet | armor |
| `+0x80` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x81` | byte | 240 Potion | Rare | Item |  |
| `+0x82` | byte | 249 Maiden's Kiss | Rare | Item |  |
| `+0x83` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x84` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0x85` | byte | 221 Magick Ring | Accessory | Ring | accessory |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 217 Magepower Gloves | Accessory | Armguard | accessory |
| `+0x88` | byte | 121 Wyrmweave Silk | Rare, Weapon | Cloth | weapon |
| `+0x89` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x8A` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x8B` | byte | 115 Catskin Bag | Rare, Weapon | Bag | weapon |
| `+0x8C` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x8D` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x8E` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x8F` | byte | 120 Cashmere | Weapon | Cloth | weapon |
| `+0x90` | byte | 39 Kotetsu | Weapon | Katana | weapon |
| `+0x91` | byte | 108 Battle Bamboo | Weapon | Pole | weapon |
| `+0x92` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x93` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x96` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x18D` | byte | 255 | Rare | None |  |
| `+0x18E` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x18F` | byte | 139 Crystal Shield | Shield | Shield | shield |
| `+0x191` | byte | 254 | Rare | None |  |
| `+0x1B5` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B8` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | byte | 144 Leather Helm | Headgear | Helmet | armor |
| `+0x1F5` | byte | 255 | Rare | None |  |
| `+0x1FC` | byte | 110 Iron Fan | Weapon | Pole | weapon |
| `+0x1FD` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x4B` | word | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x52` | word | 128 Escutcheon | Shield | Shield | shield |
| `+0x57` | word | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x5F` | word | 192 Wizard Clothing | Armor | Clothing | armor |
| `+0x61` | word | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x75` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x76` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x79` | word | 130 Bronze Shield | Shield | Shield | shield |
| `+0x93` | word | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x96` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x18F` | word | 139 Crystal Shield | Shield | Shield | shield |
| `+0x191` | word | 254 | Rare | None |  |
| `+0x1B4` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B5` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B7` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B8` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BD` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1BE` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | word | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | word | 144 Leather Helm | Headgear | Helmet | armor |
| `+0x1F5` | word | 255 | Rare | None |  |
| `+0x1FD` | word | 3 Blind Knife | Weapon | Knife | weapon |
### `0x141853EE0` `0x82` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x4B` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x4F` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x50` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x57` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x5E` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x5F` | byte | 192 Wizard Clothing | Armor | Clothing | armor |
| `+0x61` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x76` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x79` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0x7B` | byte | 186 Clothing | Armor | Clothing | armor |
| `+0x7C` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x7D` | byte | 79 Crossbow | Weapon | Crossbow | weapon |
| `+0x7E` | byte | 168 Thief's Cap | Headgear | Hat | armor |
| `+0x7F` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x80` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x81` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x82` | byte | 235 Invisibility Cloak | Rare, Accessory | Cloak | accessory |
| `+0x83` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x84` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x85` | byte | 255 | Rare | None |  |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 59 Oak Staff | Weapon | Staff | weapon |
| `+0x88` | byte | 26 Sleep Blade | Weapon | Sword | weapon |
| `+0x89` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x8B` | byte | 115 Catskin Bag | Rare, Weapon | Bag | weapon |
| `+0x8C` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x8D` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x8E` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x8F` | byte | 120 Cashmere | Weapon | Cloth | weapon |
| `+0x90` | byte | 39 Kotetsu | Weapon | Katana | weapon |
| `+0x91` | byte | 108 Battle Bamboo | Weapon | Pole | weapon |
| `+0x92` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x93` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x96` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x18D` | byte | 255 | Rare | None |  |
| `+0x18E` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x18F` | byte | 139 Crystal Shield | Shield | Shield | shield |
| `+0x191` | byte | 253 Phoenix Down | Rare | Item |  |
| `+0x1B5` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B8` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | byte | 255 | Rare | None |  |
| `+0x1FC` | byte | 221 Magick Ring | Accessory | Ring | accessory |
| `+0x1FD` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x4B` | word | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x4E` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x52` | word | 128 Escutcheon | Shield | Shield | shield |
| `+0x57` | word | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x5F` | word | 192 Wizard Clothing | Armor | Clothing | armor |
| `+0x61` | word | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x75` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x76` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x79` | word | 130 Bronze Shield | Shield | Shield | shield |
| `+0x93` | word | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x96` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x18F` | word | 139 Crystal Shield | Shield | Shield | shield |
| `+0x191` | word | 253 Phoenix Down | Rare | Item |  |
| `+0x1B4` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B5` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B7` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B8` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BB` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1BC` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BD` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1BE` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | word | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | word | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | word | 255 | Rare | None |  |
| `+0x1FD` | word | 3 Blind Knife | Weapon | Knife | weapon |
### `0x1418560E0` `0x1E` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x4A` | byte | 43 Kiyomori | Weapon | Katana | weapon |
| `+0x4B` | byte | 25 Ancient Sword | Weapon | Sword | weapon |
| `+0x4F` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x50` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 152 Platinum Helm | Headgear | Helmet | armor |
| `+0x54` | byte | 27 Platinum Sword | Weapon | Sword | weapon |
| `+0x55` | byte | 127 Spark Bomb | Weapon | Bomb | weapon |
| `+0x5A` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x5D` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x64` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x7B` | byte | 42 Ame-no-Murakumo | Weapon | Katana | weapon |
| `+0x7C` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x7D` | byte | 34 Save the Queen | Rare, Weapon | KnightSword | weapon |
| `+0x7E` | byte | 138 Platinum Shield | Shield | Shield | shield |
| `+0x7F` | byte | 225 Jade Armlet | Accessory | Armlet | accessory |
| `+0x80` | byte | 21 Iron Sword | Weapon | Sword | weapon |
| `+0x81` | byte | 232 Elven Cloak | Accessory | Cloak | accessory |
| `+0x82` | byte | 117 Proudhide Bag | Rare, Weapon | Bag | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 149 Golden Helm | Headgear | Helmet | armor |
| `+0x85` | byte | 200 Hempen Robe | Armor | Robe | armor |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 37 Chaos Blade | Rare, Weapon | KnightSword | weapon |
| `+0x88` | byte | 106 Javelin | Rare, Weapon | Polearm | weapon |
| `+0x89` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x8B` | byte | 140 Genji Shield | Rare, Shield | Shield | shield |
| `+0x8C` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x8D` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x90` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x91` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x97` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x99` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x9C` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 207 Lordly Robe | Rare, Armor | Robe | armor |
| `+0xA2` | byte | 248 Echo Herbs | Rare | Item |  |
| `+0xA4` | byte | 252 Remedy | Rare | Item |  |
| `+0xA5` | byte | 162 Green Beret | Headgear | Hat | armor |
| `+0xA6` | byte | 28 Diamond Sword | Weapon | Sword | weapon |
| `+0xA7` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xA8` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xAA` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAB` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xAD` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAE` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0xB0` | byte | 72 Mythril Gun | Rare, Weapon | Gun | weapon |
| `+0xBD` | byte | 129 Buckler | Shield | Shield | shield |
| `+0xBF` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xC8` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xCC` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xCE` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xCF` | byte | 255 | Rare | None |  |
| `+0xD0` | byte | 240 Potion | Rare | Item |  |
| `+0xD1` | byte | 240 Potion | Rare | Item |  |
| `+0xE4` | byte | 134 Ice Shield | Shield | Shield | shield |
| `+0xE5` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0xE6` | byte | 82 Gastrophetes | Rare, Weapon | Crossbow | weapon |
| `+0xE7` | byte | 51 Rod | Weapon | Rod | weapon |
| `+0xE8` | byte | 37 Chaos Blade | Rare, Weapon | KnightSword | weapon |
| `+0xE9` | byte | 34 Save the Queen | Rare, Weapon | KnightSword | weapon |
| `+0xEA` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0xEB` | byte | 72 Mythril Gun | Rare, Weapon | Gun | weapon |
| `+0xEC` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xED` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xF0` | byte | 215 Power Gauntlets | Accessory | Armguard | accessory |
| `+0xF1` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xF2` | byte | 185 Maximillian | Rare, Armor | Armor | armor |
| `+0xF4` | byte | 23 Blood Sword | Rare, Weapon | Sword | weapon |
| `+0xF5` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xF6` | byte | 254 | Rare | None |  |
| `+0xF8` | byte | 167 Lambent Hat | Headgear | Hat | armor |

_Truncated: 121 more dump item-id hits._
### `0x1418562E0` `0x32` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x4B` | byte | 20 Longsword | Weapon | Sword | weapon |
| `+0x4F` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x50` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 144 Leather Helm | Headgear | Helmet | armor |
| `+0x54` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x55` | byte | 191 Adamant Vest | Armor | Clothing | armor |
| `+0x5D` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x5F` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x7B` | byte | 212 Winged Boots | Rare, Accessory | Shoes | accessory |
| `+0x7C` | byte | 31 Nagnarok | Rare, Weapon | Sword | weapon |
| `+0x7D` | byte | 38 Ashura | Weapon | Katana | weapon |
| `+0x7E` | byte | 42 Ame-no-Murakumo | Weapon | Katana | weapon |
| `+0x7F` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x80` | byte | 19 Broadsword | Weapon | Sword | weapon |
| `+0x81` | byte | 112 Ivory Pole | Rare, Weapon | Pole | weapon |
| `+0x82` | byte | 54 Ice Rod | Weapon | Rod | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 22 Mythril Sword | Weapon | Sword | weapon |
| `+0x85` | byte | 105 Dragon Whisker | Rare, Weapon | Polearm | weapon |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 240 Potion | Rare | Item |  |
| `+0x88` | byte | 240 Potion | Rare | Item |  |
| `+0x89` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x8A` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x8B` | byte | 125 Flameburst Bomb | Weapon | Bomb | weapon |
| `+0x8C` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x8D` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x90` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x91` | byte | 110 Iron Fan | Weapon | Pole | weapon |
| `+0x92` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x93` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x96` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x9B` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x9D` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 222 Cursed Ring | Rare, Accessory | Ring | accessory |
| `+0xA2` | byte | 76 Blaster | Rare, Weapon | Gun | weapon |
| `+0xA4` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xA5` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0xA6` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0xA8` | byte | 65 Zeus Mace | Rare, Weapon | Staff | weapon |
| `+0xAD` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xAE` | byte | 170 Barrette | Rare, Headgear | HairAdornment | armor |
| `+0xB2` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xB4` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xB5` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xB7` | byte | 160 Headgear | Headgear | Hat | armor |
| `+0xB9` | byte | 96 Bestiary | Weapon | Book | weapon |
| `+0xBD` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xBF` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0xC5` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xC9` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0xCC` | byte | 136 Aegis Shield | Shield | Shield | shield |
| `+0xCE` | byte | 240 Potion | Rare | Item |  |
| `+0xE4` | byte | 115 Catskin Bag | Rare, Weapon | Bag | weapon |
| `+0xE5` | byte | 83 Longbow | Weapon | Bow | weapon |
| `+0xE6` | byte | 99 Javelin | Weapon | Polearm | weapon |
| `+0xE7` | byte | 54 Ice Rod | Weapon | Rod | weapon |
| `+0xE8` | byte | 36 Ragnarok | Rare, Weapon | KnightSword | weapon |
| `+0xE9` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEA` | byte | 18 Koga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xEB` | byte | 132 Mythril Shield | Shield | Shield | shield |
| `+0xEC` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xED` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xEE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xF0` | byte | 196 Gaia Gear | Armor | Clothing | armor |
| `+0xF1` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0xF2` | byte | 40 Bizen Osafune | Weapon | Katana | weapon |
| `+0xF3` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xF4` | byte | 83 Longbow | Weapon | Bow | weapon |
| `+0xF5` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0xF6` | byte | 62 Serpent Staff | Weapon | Staff | weapon |
| `+0xF8` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0xF9` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0xFA` | byte | 129 Buckler | Shield | Shield | shield |

_Truncated: 123 more dump item-id hits._
### `0x1418564E0` `0x1F` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x46` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x4A` | byte | 40 Bizen Osafune | Weapon | Katana | weapon |
| `+0x4B` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x4E` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x4F` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x50` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x51` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x52` | byte | 216 Genji Gloves | Rare, Accessory | Armguard | accessory |
| `+0x54` | byte | 27 Platinum Sword | Weapon | Sword | weapon |
| `+0x55` | byte | 127 Spark Bomb | Weapon | Bomb | weapon |
| `+0x5B` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x5D` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x65` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x7B` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x7C` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0x7D` | byte | 42 Ame-no-Murakumo | Weapon | Katana | weapon |
| `+0x7E` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x7F` | byte | 160 Headgear | Headgear | Hat | armor |
| `+0x80` | byte | 17 Iga Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x81` | byte | 208 Battle Boots | Accessory | Shoes | accessory |
| `+0x82` | byte | 58 Rod of Faith | Rare, Weapon | Rod | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 172 Leather Armor | Armor | Armor | armor |
| `+0x85` | byte | 129 Buckler | Shield | Shield | shield |
| `+0x86` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 189 Ringmail | Armor | Clothing | armor |
| `+0x88` | byte | 13 Kodachi | Weapon | NinjaBlade | weapon |
| `+0x89` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x8B` | byte | 122 Shuriken | Weapon | Throwing | weapon |
| `+0x8C` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x8D` | byte | 145 Bronze Helm | Headgear | Helmet | armor |
| `+0x8E` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8F` | byte | 105 Dragon Whisker | Rare, Weapon | Polearm | weapon |
| `+0x90` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0x91` | byte | 125 Flameburst Bomb | Weapon | Bomb | weapon |
| `+0x92` | byte | 45 Kiku-ichimonji | Weapon | Katana | weapon |
| `+0x93` | byte | 105 Dragon Whisker | Rare, Weapon | Polearm | weapon |
| `+0x97` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x99` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x9C` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x9F` | byte | 255 | Rare | None |  |
| `+0xA0` | byte | 254 | Rare | None |  |
| `+0xA1` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xA2` | byte | 127 Spark Bomb | Weapon | Bomb | weapon |
| `+0xA3` | byte | 252 Remedy | Rare | Item |  |
| `+0xA4` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
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
| `+0xBA` | byte | 242 X-Potion | Rare | Item |  |
| `+0xC0` | byte | 205 Black Robe | Armor | Robe | armor |
| `+0xC1` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xC2` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xC3` | byte | 130 Bronze Shield | Shield | Shield | shield |
| `+0xC4` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0xCE` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xD5` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0xD7` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0xE4` | byte | 131 Round Shield | Shield | Shield | shield |
| `+0xE5` | byte | 83 Longbow | Weapon | Bow | weapon |
| `+0xE6` | byte | 83 Longbow | Weapon | Bow | weapon |
| `+0xE7` | byte | 51 Rod | Weapon | Rod | weapon |
| `+0xE8` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0xE9` | byte | 83 Longbow | Weapon | Bow | weapon |
| `+0xEA` | byte | 34 Save the Queen | Rare, Weapon | KnightSword | weapon |
| `+0xEB` | byte | 67 Iron Flail | Weapon | Flail | weapon |
| `+0xEC` | byte | 21 Iron Sword | Weapon | Sword | weapon |
| `+0xED` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xEE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0xF0` | byte | 73 Stoneshooter | Rare, Weapon | Gun | weapon |
| `+0xF1` | byte | 2 Mythril Knife | Weapon | Knife | weapon |

_Truncated: 129 more dump item-id hits._
### `0x1418544E0` `0x82` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x4B` | byte | 19 Broadsword | Weapon | Sword | weapon |
| `+0x4F` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x50` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x7B` | byte | 60 White Staff | Weapon | Staff | weapon |
| `+0x7C` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x7D` | byte | 72 Mythril Gun | Rare, Weapon | Gun | weapon |
| `+0x7E` | byte | 154 Crystal Helm | Headgear | Helmet | armor |
| `+0x7F` | byte | 182 Crystal Mail | Armor | Armor | armor |
| `+0x80` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x81` | byte | 56 Wizard's Rod | Weapon | Rod | weapon |
| `+0x82` | byte | 246 Antidote | Rare | Item |  |
| `+0x83` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x84` | byte | 206 Luminous Robe | Armor | Robe | armor |
| `+0x85` | byte | 42 Ame-no-Murakumo | Weapon | Katana | weapon |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x88` | byte | 208 Battle Boots | Accessory | Shoes | accessory |
| `+0x89` | byte | 9 Air Knife | Weapon | Knife | weapon |
| `+0x8A` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x8B` | byte | 86 Lightning Bow | Weapon | Bow | weapon |
| `+0x8C` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x8D` | byte | 35 Excalibur | Rare, Weapon | KnightSword | weapon |
| `+0x8E` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x8F` | byte | 114 Whale Whisker | Rare, Weapon | Pole | weapon |
| `+0x90` | byte | 39 Kotetsu | Weapon | Katana | weapon |
| `+0x91` | byte | 103 Obelisk | Weapon | Polearm | weapon |
| `+0x92` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x93` | byte | 87 Windslash Bow | Weapon | Bow | weapon |
| `+0x96` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x18D` | byte | 255 | Rare | None |  |
| `+0x18E` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x18F` | byte | 135 Flame Shield | Shield | Shield | shield |
| `+0x190` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x191` | byte | 250 Gold Needle | Rare | Item |  |
| `+0x1B5` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B8` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x1BE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | byte | 255 | Rare | None |  |
| `+0x1FC` | byte | 27 Platinum Sword | Weapon | Sword | weapon |
| `+0x1FD` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x4B` | word | 19 Broadsword | Weapon | Sword | weapon |
| `+0x52` | word | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | word | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | word | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x93` | word | 87 Windslash Bow | Weapon | Bow | weapon |
| `+0x96` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x191` | word | 250 Gold Needle | Rare | Item |  |
| `+0x1B4` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B5` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B7` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B8` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | word | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x1BD` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1BE` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | word | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | word | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | word | 255 | Rare | None |  |
| `+0x1FD` | word | 3 Blind Knife | Weapon | Knife | weapon |

## Diff Offset Frequency
| Offset | Count |
| --- | ---: |
| `+0x51` | 8 |
| `+0x1B9` | 7 |
| `+0x50` | 6 |
| `+0x41` | 4 |
| `+0xF0` | 3 |
| `+0x11E` | 3 |
| `+0x61` | 2 |
| `+0x18D` | 2 |
| `+0x1A0` | 2 |
| `+0x1A1` | 2 |
| `+0x1A2` | 2 |
| `+0x1AA` | 2 |
| `+0x1AC` | 2 |
| `+0x1B0` | 2 |
| `+0x1EF` | 2 |
| `+0x1BB` | 2 |
| `+0x28` | 2 |
| `+0x1B8` | 2 |
| `+0x1A3` | 1 |
| `+0x1BA` | 1 |
| `+0x1BE` | 1 |
| `+0x1C0` | 1 |
| `+0x4F` | 1 |

## Unit Diffs
### `0x1418548E0` `0x80`
- `+0x50:03->07 +0x51:03->02 +0x1B9:00->01`
### `0x141854EE0` `0x81`
- `+0x50:05->08 +0x51:03->02 +0x1B9:00->01`
### `0x141853CE0` `0x82`
- `+0x50:03->06 +0x51:03->02 +0x1B9:00->01`
### `0x141853EE0` `0x82`
- `+0x50:02->05 +0x51:03->02 +0x1B9:00->01`
### `0x1418562E0` `0x32`
- `+0x61:00->08 +0x18D:FF->02 +0x1A0:00->13 +0x1A1:00->29 +0x1A2:00->02 +0x1A3:00->01 +0x1AA:00->05 +0x1AC:00->0A +0x1B0:00->09 +0x1BA:00->01 +0x1EF:00->08`
- `+0x28:35->40 +0x41:08->25 +0x51:01->00 +0x61:08->00 +0xF0:C4->F0 +0x11E:9A->C6 +0x18D:02->FF +0x1B8:01->00 +0x1BB:00->01 +0x1BE:01->00 +0x1C0:00->08 +0x1EF:08->00`
### `0x141855CE0` `0x03`
- `+0x41:04->09`
- `+0x41:09->01 +0xF0:23->2B +0x11E:2D->35`
- `+0x4F:08->09 +0x50:0A->08 +0x51:01->00 +0x1B9:00->01`
- `+0x28:46->59 +0x51:00->02 +0xF0:2B->59 +0x11E:35->63 +0x1A0:00->10 +0x1A1:00->1B +0x1A2:00->93 +0x1AA:00->05 +0x1AC:00->09 +0x1B0:00->09 +0x1B8:01->00 +0x1B9:01->03`
### `0x141855EE0` `0x80`
- `+0x41:04->09 +0x1BB:00->01`
### `0x1418544E0` `0x82`
- `+0x50:02->05 +0x51:03->02 +0x1B9:00->01`

## Memory Table Probes
No parsed `[MEMTABLE]` lines.

## Hook Register Probe
- Snapshots: 39
- Kinds: `continuous`=39
- Touched ids: `0x03`=17, `0x32`=8, `0x82`=6, `0x80`=4, `0x81`=2, `0x1E`=1, `0x1F`=1
- Register classifications: `readable`=181, `unreadable`=157, `zero`=154, `unit:touched`=81, `unit:id=0x82:team=3:hp=401:ct=73`=15, `unit:id=0x1F:team=0:hp=455:ct=93`=15, `unit:id=0x82:team=3:hp=401:ct=28`=11, `unreadable,+0x58=0x141855CE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable`=7, `unreadable,+0x58=0x141855CE0:unit:touched,+0xA8=0x143792AF0:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792AF0:readable,+0xD0=0x143792AF0:readable,+0xE0=0xC:unreadable,+0xE8=0x143792AF0:readable,+0xF0=0x417BF12688:readable,+0xF8=0x1402F2EC1:readable`=6, `unit:id=0x82:team=3:hp=401:ct=37`=5, `unit:id=0x1F:team=0:hp=455:ct=57`=5, `unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable`=4

### First Snapshots
| Kind | Event | Hook Count | Ptr | Hook Ptr | Id | Registers |
| --- | ---: | ---: | --- | --- | --- | --- |
| continuous |  | 4 | `0x141855CE0` | `` | `0x03` | `rax=0x1BE:unreadable rbx=0x1407832A0:readable rcx=0x141855CE0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:readable rdi=0x141855CE0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x141855CE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 8 | `0x141855EE0` | `` | `0x80` | `rax=0x120:unreadable rbx=0x1407832A0:readable rcx=0x141855EE0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:readable rdi=0x141855EE0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x141855EE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 12 | `0x1418548E0` | `` | `0x80` | `rax=0x15D:unreadable rbx=0x1407832A0:readable rcx=0x1418548E0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:readable rdi=0x1418548E0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x1418548E0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 16 | `0x1418548E0` | `` | `0x80` | `rax=0x15D:unreadable rbx=0x1407832A0:readable rcx=0x1418548E0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:readable rdi=0x1418548E0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x1418548E0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 20 | `0x141854EE0` | `` | `0x81` | `rax=0x155:unreadable rbx=0x1407832A0:readable rcx=0x141854EE0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:readable rdi=0x141854EE0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x141854EE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 24 | `0x141854EE0` | `` | `0x81` | `rax=0x155:unreadable rbx=0x1407832A0:readable rcx=0x141854EE0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:readable rdi=0x141854EE0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x141854EE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 28 | `0x141853CE0` | `` | `0x82` | `rax=0x191:unreadable rbx=0x1407832A0:readable rcx=0x141853CE0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:unit:touched rdi=0x141853CE0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x141853CE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 32 | `0x141853CE0` | `` | `0x82` | `rax=0x191:unreadable rbx=0x1407832A0:readable rcx=0x141853CE0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:unit:touched rdi=0x141853CE0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x8=0xF100000000030000:unreadable,+0x18=0xFF6100F0:unreadable,+0x20=0xF0FF61:readable,+0x30=0xF100000000030000:unreadable,+0x40=0xFFBB00F0:unreadable,+0x48=0xF0FFBB:readable,+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x141853CE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 36 | `0x141853EE0` | `` | `0x82` | `rax=0x16C:unreadable rbx=0x1407832A0:readable rcx=0x141853EE0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:unit:id=0x82:team=3:hp=401:ct=28 rdi=0x141853EE0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x8=0xF100000000030000:unreadable,+0x18=0xFF6100F0:unreadable,+0x20=0xF0FF61:readable,+0x30=0xF100000000030000:unreadable,+0x40=0xFFBB00F0:unreadable,+0x48=0xF0FFBB:readable,+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x141853EE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 40 | `0x141853EE0` | `` | `0x82` | `rax=0x16C:unreadable rbx=0x1407832A0:readable rcx=0x141853EE0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:unit:id=0x82:team=3:hp=401:ct=28 rdi=0x141853EE0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x141853EE0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 44 | `0x1418560E0` | `` | `0x1E` | `rax=0x1D6:unreadable rbx=0x1407832A0:readable rcx=0x1418560E0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:unit:id=0x82:team=3:hp=401:ct=28 rdi=0x1418560E0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x1418560E0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |
| continuous |  | 48 | `0x1418562E0` | `` | `0x32` | `rax=0x17A:unreadable rbx=0x1407832A0:readable rcx=0x1418562E0:unit:touched rdx=0x1407832A0:readable rsi=0x141853CE0:unit:id=0x82:team=3:hp=401:ct=28 rdi=0x1418562E0:unit:touched rbp=0x0:zero rsp=0x13DDDFD90:readable r8=0x1418564E0:readable r9=0x141856671:readable r10=0x14:unreadable r11=0x7B:unreadable r12=0x0:zero r13=0x0:zero r14=0x0:zero r15=0x1:unreadable stack=+0x50=0xDCD77E5E2CE8:unreadable,+0x58=0x1418562E0:unit:touched,+0xA8=0x143792C10:readable,+0xB8=0x1402F3799:readable,+0xC0=0x143792C10:readable,+0xD8=0x157D0651E:readable,+0xE0=0xD:unreadable,+0xE8=0x143792C10:readable,+0xF0=0x417BF126E8:readable,+0xF8=0x1402F2EC1:readable` |

## Runtime Context Summary
No parsed `[RUNTIME]` lines.

## Actor Probe CT Summary
- Actor probe events: 3
- Resolved actor probes: 3/3
- Sources: `ct-drop`=2, `ct-lowest`=1
- Rule: exclude target, prefer largest recent CT drop, else lowest absolute CT.

| # | Line | Target | Attacker | Source | Top candidates |
| --- | ---: | --- | --- | --- | --- |
| 1 | 154 | 0x80 | 0x82 | `ct-lowest` | `0x82(ct=24,prev=-,drop=0,spd=8) 0x32(ct=37,prev=-,drop=0,spd=9) 0x82(ct=37,prev=-,drop=0,spd=9) 0x82(ct=37,prev=-,drop=0,spd=9) 0x03(ct=49,prev=-,drop=0,spd=9) +3 more` |
| 2 | 166 | 0x1E | 0x80 | `ct-drop` | `0x80(ct=49,prev=50,drop=1,spd=13) 0x82(ct=24,prev=24,drop=0,spd=8) 0x32(ct=37,prev=37,drop=0,spd=9) 0x82(ct=37,prev=24,drop=0,spd=9) 0x82(ct=37,prev=24,drop=0,spd=9) +4 more` |
| 3 | 229 | 0x80 | 0x03 | `ct-drop` | `0x03(ct=1,prev=49,drop=48,spd=9) 0x82(ct=56,prev=24,drop=0,spd=8) 0x32(ct=73,prev=37,drop=0,spd=9) 0x82(ct=73,prev=24,drop=0,spd=9) 0x82(ct=73,prev=24,drop=0,spd=9) +3 more` |

## Death State
- Events: `DEATH-DUMP`=1, `DEATH-DIFF`=1
- Status: dump=1, diff=1
- Death diff offsets: `+0x30`=1, `+0x61`=1, `+0x63`=1, `+0x18C`=1, `+0x1BB`=1, `+0x1DB`=1, `+0x1EF`=1, `+0x1F1`=1, `+0x1F5`=1
- Verdict: **mapping-candidate: death diffs include KO flag +0x61**

### Death Diffs
- line 227: `DEATH-DIFF` `0x141855EE0` alive->dead +0x30:0F->00 +0x61:00->20 +0x63:21->20 +0x18C:00->01 +0x1BB:00->01 +0x1DB:00->20 +0x1EF:00->20 +0x1F1:01->00 +0x1F5:FF->10

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
- Damage events: 3
- Healing events: 0
- HP sample age: min=16ms max=19ms
- Damage by unit: `0x141855EE0`=2, `0x1418560E0`=1
- Context events: 3
- Context resolved: 2/3
  - `0x141855EE0` resolved=0x1418562E0 source=pending-clear pending=batch=1/act=258/event=1/16/confidence=damage-cache/score=1098827 fallback=0x1418544E0 fallbackSource=ct-low ctCandidates=none ctLowCandidates=ptr=0x1418544E0/id=0x82/foe/t3/CT=24/seen=2920ms/PA=13
  - `0x1418560E0` resolved=0x1418562E0 source=pending-clear pending=batch=1/act=258/event=2/16/confidence=damage-cache/score=1098827 fallback=0x1418544E0 fallbackSource=ct-low ctCandidates=none ctLowCandidates=ptr=0x1418544E0/id=0x82/foe/t3/CT=24/seen=2920ms/PA=13
  - `0x141855EE0` resolved=none ctCandidates=none ctLowCandidates=none ctObserved=ptr=0x141855CE0/id=0x03/ally/t0/CT=1/drop=227443ms:48/seen=118730ms/PA=14 ptr=0x1418544E0/id=0x82/foe/t3/CT=56/drop=none/seen=236612ms/PA=13 ptr=0x141853CE0/id=0x82/foe/t3/CT=73/drop=none/seen=476230ms/PA=12 ptr=0x141853EE0/id=0x82/foe/t3/CT=73/drop=none/seen=472360ms/PA=12 ... +5 more attackerCandidates=none
- Runtime context events: 0
- Rewrite events: 0

### Neuter Placeholder Check
- No parsed HP rewrite lines with `vanillaDamage`.
- Verdict: **no HP placeholder evidence**

### HP Write Proof Check
- Concrete HP rewrites: 0
- Concrete `finalDamage=1` rewrites: 0/0
- HP rewrite failures: 0
- Baseline sample age: min=16ms max=19ms
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

