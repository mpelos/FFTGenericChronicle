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
| `0x141855EE0` | `0x80` | ally | 0 | Lv70 HP276 MP41 PA15 MA7 Sp16 CT76 Mv6 Jp4 Br97 Fa72 |
| `0x1418560E0` | `0x1E` | ally | 0 | Lv69 HP322 MP232 PA11 MA23 Sp12 CT72 Mv5 Jp3 Br97 Fa63 |
| `0x141853CE0` | `0x82` | foe | 3 | Lv72 HP359 MP9 PA15 MA44 Sp10 CT80 Mv3 Jp3 Br58 Fa48 |
| `0x141853EE0` | `0x82` | foe | 3 | Lv70 HP327 MP9 PA14 MA47 Sp10 CT80 Mv3 Jp3 Br45 Fa52 |
| `0x1418544E0` | `0x82` | foe | 3 | Lv66 HP326 MP9 PA14 MA46 Sp10 CT80 Mv3 Jp3 Br54 Fa49 |
| `0x141855CE0` | `0x01` | ally | 0 | Lv75 HP567 MP85 PA20 MA9 Sp10 CT0 Mv6 Jp3 Br97 Fa70 |
| `0x1418562E0` | `0x32` | ally | 0 | Lv67 HP428 MP89 PA14 MA13 Sp9 CT64 Mv4 Jp3 Br97 Fa65 |
| `0x1418564E0` | `0x1F` | ally | 0 | Lv68 HP314 MP180 PA5 MA17 Sp9 CT84 Mv6 Jp3 Br97 Fa65 |

## Candidate Fields
### `0x141855EE0` `0x80` ally t0
`bytes[44-83] +0x44=15 +0x45=15 +0x46=10 +0x47=5 +0x4B=30 +0x4F=9 +0x50=9 +0x51=1 +0x52=224 +0x53=64 +0x54=6 +0x55=191 +0x59=32 +0x5A=64 | words<=511 +0x50w=265 +0x5Aw=64 +0x60w=12 +0x64w=64 || bytes[84-C3] +0x84=238 +0x85=18 +0x86=3 +0x87=89 +0x88=90 +0x89=2 +0x8A=12 +0x8B=70 +0x8C=13 +0x8D=50 +0x8E=80 +0x8F=120 +0x90=43 +0x91=122 | words<=511 +0x96w=64 +0x9Aw=9 +0x9Cw=64 +0xA2w=96 +0xA8w=4 +0xAEw=255 +0xB2w=160 || bytes[C4-103] +0xC6=32 +0xC8=64 +0xC9=140 +0xCB=64 +0xCE=64 +0xCF=255 +0xD0=240 +0xD1=240 +0xD5=128 +0xE4=136 +0xE5=117 +0xE6=131 +0xE7=68 +0xE8=104 | words<=511 +0xC6w=32 +0xECw=276 +0xEEw=66 +0xF2w=349 +0xF6w=175 +0xF8w=334 +0xFAw=455 || bytes[104-143] +0x104=69 +0x105=3 +0x106=215 +0x107=2 +0x108=195 +0x109=4 +0x10A=45 +0x10B=2 +0x10C=16 +0x10D=6 +0x10E=196 +0x10F=32 +0x110=159 +0x112=12 | words<=511 +0x110w=159 +0x116w=150 +0x13Ew=159 || bytes[144-17F] +0x144=150 +0x146=244 +0x147=2 +0x148=200 +0x14C=82 +0x14D=105 +0x14E=111 +0x14F=110 +0x151=119 +0x152=117 +0x153=108 +0x154=102 | words<=511 +0x144w=150 +0x148w=200 +0x154w=102`
### `0x1418560E0` `0x1E` ally t0
`bytes[44-83] +0x44=14 +0x46=15 +0x4B=5 +0x4F=10 +0x50=9 +0x51=1 +0x52=145 +0x54=6 +0x55=255 +0x5A=48 +0x64=48 +0x7B=101 +0x7C=204 +0x7D=44 | words<=511 +0x44w=14 +0x46w=15 +0x50w=265 +0x52w=145 +0x5Aw=48 +0x64w=48 || bytes[84-C3] +0x84=8 +0x85=84 +0x86=3 +0x87=143 +0x88=223 +0x89=2 +0x8A=12 +0x8B=75 +0x8C=9 +0x8D=120 +0x8E=100 +0x8F=100 +0x90=60 +0x91=60 | words<=511 +0x98w=32 +0x9Cw=64 +0xA0w=207 +0xA2w=248 +0xA8w=255 +0xAEw=130 +0xB0w=72 || bytes[C4-103] +0xC8=64 +0xCC=128 +0xCE=64 +0xCF=255 +0xD0=240 +0xD1=240 +0xE4=134 +0xE5=132 +0xE6=82 +0xE7=99 +0xE8=85 +0xE9=50 +0xEA=66 +0xEB=88 | words<=511 +0xC8w=64 +0xCCw=128 +0xEEw=50 +0xF2w=185 +0xF6w=254 +0xFAw=279 || bytes[104-143] +0x104=213 +0x105=1 +0x106=24 +0x107=1 +0x108=172 +0x109=1 +0x10A=119 +0x10B=1 +0x10C=224 +0x10D=2 +0x10E=133 +0x10F=14 +0x110=153 +0x114=153 | words<=511 +0x104w=469 +0x106w=280 +0x108w=428 +0x10Aw=375 +0x110w=153 +0x114w=153 +0x116w=113 +0x118w=456 +0x128w=279 +0x132w=469 || bytes[144-17F] +0x144=113 +0x146=200 +0x147=1 +0x148=200 | words<=511 +0x144w=113 +0x146w=456 +0x148w=200`
### `0x141853CE0` `0x82` foe t3
`bytes[44-83] +0x4B=19 +0x50=5 +0x51=3 +0x52=128 +0x5D=4 +0x79=32 +0x7B=112 +0x7C=125 +0x7D=104 +0x7E=136 +0x7F=178 +0x80=6 +0x81=8 +0x82=72 | words<=511 +0x52w=128 || bytes[84-C3] +0x84=24 +0x85=208 +0x86=3 +0x87=41 +0x88=190 +0x89=12 +0x8A=6 +0x8B=86 +0x8C=30 +0x8D=35 +0x8E=85 +0x8F=114 +0x90=39 +0x91=103 | words<=511 +0x96w=8 || bytes[C4-103] none | words<=511 none || bytes[104-143] none | words<=511 none || bytes[144-17F] none | words<=511 none`
### `0x141853EE0` `0x82` foe t3
`bytes[44-83] +0x4B=19 +0x4F=1 +0x50=6 +0x51=3 +0x52=128 +0x5D=4 +0x79=32 +0x7B=35 +0x7C=64 +0x7D=95 +0x7E=116 +0x7F=132 +0x80=6 +0x81=152 | words<=511 +0x4Ew=256 +0x52w=128 || bytes[84-C3] +0x84=98 +0x85=154 +0x86=3 +0x87=93 +0x88=192 +0x89=13 +0x8A=6 +0x8B=86 +0x8C=30 +0x8D=35 +0x8E=85 +0x8F=114 +0x90=39 +0x91=103 | words<=511 +0x96w=8 || bytes[C4-103] none | words<=511 none || bytes[104-143] none | words<=511 none || bytes[144-17F] none | words<=511 none`
### `0x1418544E0` `0x82` foe t3
`bytes[44-83] +0x4B=19 +0x4F=2 +0x50=11 +0x51=3 +0x52=128 +0x5D=4 +0x79=32 +0x7B=106 +0x7C=219 +0x7D=94 +0x7E=87 +0x7F=131 +0x80=6 +0x81=184 | words<=511 +0x52w=128 || bytes[84-C3] +0x84=228 +0x85=127 +0x86=3 +0x87=203 +0x88=68 +0x89=13 +0x8A=6 +0x8B=86 +0x8C=30 +0x8D=35 +0x8E=85 +0x8F=114 +0x90=39 +0x91=103 | words<=511 +0x96w=8 || bytes[C4-103] none | words<=511 none || bytes[104-143] none | words<=511 none || bytes[144-17F] none | words<=511 none`
### `0x141855CE0` `0x01` ally t0
`bytes[44-83] +0x44=40 +0x46=20 +0x4A=50 +0x4E=25 +0x4F=8 +0x50=10 +0x51=1 +0x52=154 +0x53=64 +0x54=25 +0x55=254 +0x56=16 +0x5A=64 +0x5D=32 | words<=511 +0x44w=40 +0x46w=20 +0x4Aw=50 +0x50w=266 +0x56w=16 +0x5Aw=64 +0x60w=16 +0x64w=64 +0x78w=224 || bytes[84-C3] +0x84=166 +0x85=41 +0x86=3 +0x87=34 +0x88=20 +0x89=3 +0x8A=12 +0x8B=80 +0x8C=20 +0x8D=90 +0x8E=100 +0x8F=100 +0x90=40 +0x91=140 | words<=511 +0x9Cw=32 +0xA8w=255 +0xAEw=195 +0xB2w=160 +0xBAw=248 +0xC0w=16 || bytes[C4-103] +0xC6=255 +0xC7=240 +0xC8=208 +0xC9=204 +0xCB=32 +0xCE=80 +0xCF=208 +0xD1=32 +0xD5=96 +0xD7=32 +0xDE=248 +0xE0=128 +0xE4=132 +0xE5=132 | words<=511 +0xDEw=248 +0xE0w=128 +0xECw=277 +0xEEw=129 +0xF2w=456 +0xF6w=493 +0xFAw=391 || bytes[104-143] +0x104=153 +0x105=5 +0x106=191 +0x107=1 +0x108=22 +0x109=3 +0x10A=4 +0x10B=3 +0x10C=137 +0x10D=6 +0x10E=60 +0x10F=9 +0x110=167 +0x112=216 | words<=511 +0x106w=447 +0x110w=167 +0x112w=216 +0x116w=131 +0x118w=420 +0x128w=511 +0x13Ew=167 || bytes[144-17F] +0x144=131 +0x146=32 +0x147=23 +0x148=100 | words<=511 +0x144w=131 +0x148w=100`
### `0x1418562E0` `0x32` ally t0
`bytes[44-83] +0x44=16 +0x46=10 +0x4B=20 +0x4F=10 +0x50=10 +0x51=1 +0x52=148 +0x54=11 +0x55=127 +0x7B=102 +0x7C=125 +0x7D=49 +0x7E=8 +0x7F=251 | words<=511 +0x44w=16 +0x46w=10 +0x50w=266 +0x52w=148 || bytes[84-C3] +0x84=164 +0x85=219 +0x86=2 +0x87=74 +0x88=75 +0x89=2 +0x8A=12 +0x8B=75 +0x8C=14 +0x8D=90 +0x8E=100 +0x8F=100 +0x90=45 +0x91=128 | words<=511 +0x96w=1 +0x98w=32 +0xA0w=254 +0xA2w=255 +0xA6w=4 +0xA8w=193 +0xAEw=174 +0xB2w=128 || bytes[C4-103] +0xC5=16 +0xC9=128 +0xCC=188 +0xCD=128 +0xCE=240 +0xE4=131 +0xE5=99 +0xE6=99 +0xE7=70 +0xE8=84 +0xE9=35 +0xEA=50 +0xEB=133 +0xEC=18 | words<=511 +0xCEw=240 +0xECw=274 +0xEEw=49 +0xF2w=320 +0xF6w=62 +0xFAw=444 +0xFEw=53 +0x102w=334 || bytes[104-143] +0x104=39 +0x105=1 +0x106=117 +0x108=2 +0x109=2 +0x10A=212 +0x10C=80 +0x10D=4 +0x10E=182 +0x10F=5 +0x110=168 +0x112=230 +0x116=190 +0x118=44 | words<=511 +0x104w=295 +0x106w=117 +0x10Aw=212 +0x110w=168 +0x112w=230 +0x116w=190 +0x120w=510 +0x124w=462 +0x132w=295 +0x134w=467 || bytes[144-17F] +0x144=190 +0x146=44 +0x147=2 +0x148=100 | words<=511 +0x144w=190 +0x148w=100`
### `0x1418564E0` `0x1F` ally t0
`bytes[44-83] +0x44=14 +0x46=15 +0x4B=5 +0x4F=10 +0x50=8 +0x51=1 +0x52=145 +0x53=128 +0x54=6 +0x55=255 +0x7B=247 +0x7C=72 +0x7D=54 +0x7E=185 | words<=511 +0x44w=14 +0x46w=15 +0x50w=264 || bytes[84-C3] +0x84=67 +0x85=227 +0x86=2 +0x87=167 +0x88=108 +0x89=2 +0x8A=13 +0x8B=70 +0x8C=8 +0x8D=125 +0x8E=100 +0x8F=90 +0x90=70 +0x91=50 | words<=511 +0x98w=32 +0x9Cw=32 +0xAAw=16 +0xAEw=7 || bytes[C4-103] +0xC4=16 +0xCE=64 +0xD5=64 +0xD7=32 +0xE4=131 +0xE5=99 +0xE6=83 +0xE7=67 +0xE8=133 +0xE9=83 +0xEA=51 +0xEB=85 +0xEC=21 +0xED=1 | words<=511 +0xC4w=16 +0xCEw=64 +0xECw=277 +0xEEw=49 +0xF2w=253 +0xF6w=427 +0xF8w=382 +0xFAw=128 +0xFEw=232 || bytes[104-143] +0x104=104 +0x106=175 +0x108=85 +0x109=2 +0x10A=170 +0x10B=1 +0x10C=126 +0x10D=2 +0x10E=107 +0x10F=5 +0x110=197 +0x112=186 +0x116=177 +0x118=24 | words<=511 +0x104w=104 +0x106w=175 +0x10Aw=426 +0x110w=197 +0x112w=186 +0x116w=177 +0x124w=427 +0x138w=426 +0x13Ew=197 || bytes[144-17F] +0x144=177 +0x146=24 +0x147=2 +0x148=100 | words<=511 +0x144w=177 +0x148w=100`

## Candidate Item Hits
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

_Truncated: 157 more dump item-id hits._
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
### `0x141853CE0` `0x82` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x4B` | byte | 19 Broadsword | Weapon | Sword | weapon |
| `+0x50` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x7B` | byte | 112 Ivory Pole | Rare, Weapon | Pole | weapon |
| `+0x7C` | byte | 125 Flameburst Bomb | Weapon | Bomb | weapon |
| `+0x7D` | byte | 104 Holy Lance | Rare, Weapon | Polearm | weapon |
| `+0x7E` | byte | 136 Aegis Shield | Shield | Shield | shield |
| `+0x7F` | byte | 178 Golden Armor | Armor | Armor | armor |
| `+0x80` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x81` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x82` | byte | 72 Mythril Gun | Rare, Weapon | Gun | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 24 Coral Sword | Weapon | Sword | weapon |
| `+0x85` | byte | 208 Battle Boots | Accessory | Shoes | accessory |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 41 Murasame | Weapon | Katana | weapon |
| `+0x88` | byte | 190 Mythril Vest | Armor | Clothing | armor |
| `+0x89` | byte | 12 Kunai | Weapon | NinjaBlade | weapon |
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
| `+0x191` | byte | 254 | Rare | None |  |
| `+0x1B5` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B8` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | byte | 144 Leather Helm | Headgear | Helmet | armor |
| `+0x1F5` | byte | 255 | Rare | None |  |
| `+0x1FC` | byte | 123 Fuma Shuriken | Weapon | Throwing | weapon |
| `+0x1FD` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x4B` | word | 19 Broadsword | Weapon | Sword | weapon |
| `+0x52` | word | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | word | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | word | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x93` | word | 87 Windslash Bow | Weapon | Bow | weapon |
| `+0x96` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
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
| `+0x4B` | byte | 19 Broadsword | Weapon | Sword | weapon |
| `+0x4F` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x50` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x7B` | byte | 35 Excalibur | Rare, Weapon | KnightSword | weapon |
| `+0x7C` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x7D` | byte | 95 Battle Folio | Weapon | Book | weapon |
| `+0x7E` | byte | 116 Fallingstar Bag | Rare, Weapon | Bag | weapon |
| `+0x7F` | byte | 132 Mythril Shield | Shield | Shield | shield |
| `+0x80` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x81` | byte | 152 Platinum Helm | Headgear | Helmet | armor |
| `+0x82` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 98 Omnilex | Rare, Weapon | Book | weapon |
| `+0x85` | byte | 154 Crystal Helm | Headgear | Helmet | armor |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 93 Bloodstring Harp | Weapon | Instrument | weapon |
| `+0x88` | byte | 192 Wizard Clothing | Armor | Clothing | armor |
| `+0x89` | byte | 13 Kodachi | Weapon | NinjaBlade | weapon |
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
| `+0x191` | byte | 253 Phoenix Down | Rare | Item |  |
| `+0x1B5` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B8` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | byte | 255 | Rare | None |  |
| `+0x1FC` | byte | 49 Giant's Axe | Weapon | Axe | weapon |
| `+0x1FD` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x4B` | word | 19 Broadsword | Weapon | Sword | weapon |
| `+0x4E` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x52` | word | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | word | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | word | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x93` | word | 87 Windslash Bow | Weapon | Bow | weapon |
| `+0x96` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
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
### `0x1418544E0` `0x82` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x4B` | byte | 19 Broadsword | Weapon | Sword | weapon |
| `+0x4F` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x50` | byte | 11 Ninja Blade | Weapon | NinjaBlade | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | byte | 32 Materia Blade | Rare, Weapon | Sword | weapon |
| `+0x7B` | byte | 106 Javelin | Rare, Weapon | Polearm | weapon |
| `+0x7C` | byte | 219 Reflect Ring | Rare, Accessory | Ring | accessory |
| `+0x7D` | byte | 94 Faerie Harp | Rare, Weapon | Instrument | weapon |
| `+0x7E` | byte | 87 Windslash Bow | Weapon | Bow | weapon |
| `+0x7F` | byte | 131 Round Shield | Shield | Shield | shield |
| `+0x80` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x81` | byte | 184 Mirror Mail | Rare, Armor | Armor | armor |
| `+0x82` | byte | 49 Giant's Axe | Weapon | Axe | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 228 Guardian Bracelet | Accessory | Armlet | accessory |
| `+0x85` | byte | 127 Spark Bomb | Weapon | Bomb | weapon |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 203 Chameleon Robe | Rare, Armor | Robe | armor |
| `+0x88` | byte | 68 Flail of Flame | Weapon | Flail | weapon |
| `+0x89` | byte | 13 Kodachi | Weapon | NinjaBlade | weapon |
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
| `+0x1FC` | byte | 250 Gold Needle | Rare | Item |  |
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

## Diff Offset Frequency
No `[DIFF]` lines found.

## Unit Diffs
No per-unit diffs.

## Memory Table Probes
No parsed `[MEMTABLE]` lines.

## Hook Register Probe
No parsed `[HOOK-REGS]` lines.

## Runtime Context Summary
No parsed `[RUNTIME]` lines.

## Actor Probe CT Summary
No parsed `[ACTOR-PROBE]` lines.

## Death State
No parsed `[DEATH-*]` lines.

## Death Gate Outcome
- Lethal HP rewrites (`finalDamage>=9999`, HP -> 0): 0
- Concrete lethal HP writes: 0
- Dry-run lethal HP decisions: 0
- Placeholder-sized lethal rewrites: 0/0
- Death events: 0
- KO flag diffs (`+0x61`): 0
- KO flag writes (`+0x61`): 0
- HP rewrite failures: 0
- Death-write failures: 0
- Verdict: **no death-gate lethal HP rewrite evidence**

## HP Events / Rewrite
- Damage events: 2
- Healing events: 0
- HP sample age: min=7ms max=7ms
- Damage by unit: `0x141855EE0`=1, `0x1418560E0`=1
- Context events: 2
- Context resolved: 2/2
  - `0x141855EE0` resolved=0x1418562E0 source=pending-clear pending=batch=1/act=258/event=1/16/confidence=damage-cache/score=1098834 fallback=none ctCandidates=none ctLowCandidates=none ctObserved=ptr=0x1418560E0/id=0x1E/ally/t0/CT=48/drop=75929ms:76/seen=75710ms/PA=11 ptr=0x1418562E0/id=0x32/ally/t0/CT=46/drop=none/seen=161392ms/PA=14 ptr=0x141853CE0/id=0x82/foe/t3/CT=60/drop=none/seen=359122ms/PA=15 ptr=0x141853EE0/id=0x82/foe/t3/CT=60/drop=none/seen=357767ms/PA=14 ... +3 more attackerCandidates=none
  - `0x1418560E0` resolved=0x1418562E0 source=pending-clear pending=batch=1/act=258/event=2/16/confidence=damage-cache/score=1098834 fallback=none ctCandidates=none ctLowCandidates=none ctObserved=ptr=0x141855EE0/id=0x80/ally/t0/CT=44/drop=32774ms:100/seen=32539ms/PA=15 ptr=0x1418562E0/id=0x32/ally/t0/CT=46/drop=none/seen=161392ms/PA=14 ptr=0x141853CE0/id=0x82/foe/t3/CT=60/drop=none/seen=359122ms/PA=15 ptr=0x141853EE0/id=0x82/foe/t3/CT=60/drop=none/seen=357767ms/PA=14 ... +3 more attackerCandidates=none
- Runtime context events: 0
- Rewrite events: 0

### Neuter Placeholder Check
- No parsed HP rewrite lines with `vanillaDamage`.
- Verdict: **no HP placeholder evidence**

### HP Write Proof Check
- Concrete HP rewrites: 0
- Concrete `finalDamage=1` rewrites: 0/0
- HP rewrite failures: 0
- Baseline sample age: min=7ms max=7ms
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
