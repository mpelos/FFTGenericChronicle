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
| `0x141855CE0` | `0x01` | ally | 0 | Lv76 HP569 MP86 PA20 MA9 Sp10 CT0 Mv6 Jp3 Br97 Fa70 |
| `0x141853EE0` | `0x82` | foe | 3 | Lv71 HP343 MP27 PA16 MA49 Sp11 CT30 Mv6 Jp5 Br67 Fa57 |
| `0x141854AE0` | `0x82` | foe | 3 | Lv75 HP365 MP28 PA15 MA52 Sp11 CT30 Mv6 Jp5 Br58 Fa67 |
| `0x1418540E0` | `0x82` | foe | 3 | Lv76 HP489 MP13 PA16 MA56 Sp10 CT0 Mv4 Jp4 Br57 Fa49 |

## Candidate Fields
### `0x141855CE0` `0x01` ally t0
`bytes[44-83] +0x44=1 +0x46=20 +0x4A=50 +0x4E=25 +0x4F=5 +0x50=1 +0x51=2 +0x52=154 +0x53=64 +0x54=25 +0x55=254 +0x56=16 +0x5A=64 +0x5D=32 | words<=511 +0x44w=1 +0x46w=20 +0x4Aw=50 +0x56w=16 +0x5Aw=64 +0x60w=16 +0x64w=64 +0x78w=224 || bytes[84-C3] +0x84=176 +0x85=48 +0x86=3 +0x87=112 +0x88=26 +0x89=3 +0x8A=12 +0x8B=80 +0x8C=20 +0x8D=90 +0x8E=100 +0x8F=100 +0x90=40 +0x91=140 | words<=511 +0x9Cw=32 +0xA8w=255 +0xAEw=195 +0xB2w=160 +0xBAw=248 +0xC0w=16 || bytes[C4-103] +0xC6=255 +0xC7=240 +0xC8=208 +0xC9=204 +0xCB=32 +0xCE=80 +0xCF=208 +0xD1=32 +0xD5=96 +0xD7=32 +0xDE=248 +0xE0=128 +0xE4=132 +0xE5=132 | words<=511 +0xDEw=248 +0xE0w=128 +0xECw=277 +0xEEw=129 +0xF2w=456 +0xF6w=493 +0xFAw=391 || bytes[104-143] +0x104=153 +0x105=5 +0x106=191 +0x107=1 +0x108=22 +0x109=3 +0x10A=4 +0x10B=3 +0x10C=157 +0x10D=6 +0x10E=120 +0x10F=9 +0x110=167 +0x112=216 | words<=511 +0x106w=447 +0x110w=167 +0x112w=216 +0x116w=131 +0x128w=511 +0x13Ew=167 || bytes[144-17F] +0x144=131 +0x146=48 +0x147=24 +0x148=100 | words<=511 +0x144w=131 +0x148w=100`
### `0x141853EE0` `0x82` foe t3
`bytes[44-83] +0x4B=15 +0x4F=1 +0x50=10 +0x51=3 +0x52=128 +0x5D=4 +0x7B=126 +0x7C=139 +0x7D=79 +0x7E=206 +0x7F=243 +0x80=6 +0x81=126 +0x82=102 | words<=511 +0x4Ew=256 +0x52w=128 || bytes[84-C3] +0x84=59 +0x85=23 +0x86=4 +0x87=14 +0x88=15 +0x89=13 +0x8A=8 +0x8B=108 +0x8C=30 +0x8D=100 +0x8E=75 +0x8F=119 +0x90=35 +0x91=98 | words<=511 +0x96w=8 +0x9Ew=64 || bytes[C4-103] none | words<=511 none || bytes[104-143] none | words<=511 none || bytes[144-17F] none | words<=511 none`
### `0x141854AE0` `0x82` foe t3
`bytes[44-83] +0x4B=15 +0x50=2 +0x51=3 +0x52=128 +0x5D=4 +0x7B=236 +0x7C=149 +0x7D=84 +0x7E=204 +0x7F=16 +0x80=7 +0x81=82 +0x82=119 +0x83=2 | words<=511 +0x52w=128 || bytes[84-C3] +0x84=127 +0x85=243 +0x86=3 +0x87=88 +0x88=24 +0x89=14 +0x8A=8 +0x8B=108 +0x8C=30 +0x8D=100 +0x8E=75 +0x8F=119 +0x90=35 +0x91=98 | words<=511 +0x96w=8 +0x9Ew=64 || bytes[C4-103] none | words<=511 none || bytes[104-143] none | words<=511 none || bytes[144-17F] none | words<=511 none`
### `0x1418540E0` `0x82` foe t3
`bytes[44-83] +0x4B=23 +0x4F=1 +0x50=1 +0x51=3 +0x52=128 +0x5D=4 +0x79=8 +0x7B=164 +0x7C=139 +0x7D=105 +0x7E=225 +0x7F=218 +0x80=6 +0x81=232 | words<=511 +0x4Ew=256 +0x52w=128 || bytes[84-C3] +0x84=172 +0x85=76 +0x86=4 +0x87=7 +0x88=132 +0x89=15 +0x8A=6 +0x8B=116 +0x8C=30 +0x8D=50 +0x8E=85 +0x8F=116 +0x90=39 +0x91=98 | words<=511 +0x96w=8 || bytes[C4-103] none | words<=511 none || bytes[104-143] none | words<=511 none || bytes[144-17F] none | words<=511 none`

## Candidate Item Hits
### `0x141855CE0` `0x01` ally t0 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x44` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x46` | byte | 20 Longsword | Weapon | Sword | weapon |
| `+0x4A` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x4E` | byte | 25 Ancient Sword | Weapon | Sword | weapon |
| `+0x4F` | byte | 5 Platinum Dagger | Weapon | Knife | weapon |
| `+0x50` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x51` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
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
| `+0x7B` | byte | 211 Rubber Boots | Rare, Accessory | Shoes | accessory |
| `+0x7C` | byte | 72 Mythril Gun | Rare, Weapon | Gun | weapon |
| `+0x7D` | byte | 57 Dragon Rod | Rare, Weapon | Rod | weapon |
| `+0x7E` | byte | 201 Silken Robe | Armor | Robe | armor |
| `+0x7F` | byte | 234 Featherweave Cloak | Accessory | Cloak | accessory |
| `+0x80` | byte | 23 Blood Sword | Rare, Weapon | Sword | weapon |
| `+0x81` | byte | 211 Rubber Boots | Rare, Accessory | Shoes | accessory |
| `+0x82` | byte | 162 Green Beret | Headgear | Hat | armor |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 176 Mythril Armor | Armor | Armor | armor |
| `+0x85` | byte | 48 Battle Axe | Weapon | Axe | weapon |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 112 Ivory Pole | Rare, Weapon | Pole | weapon |
| `+0x88` | byte | 26 Sleep Blade | Weapon | Sword | weapon |
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
| `+0x97` | byte | 4 Mage Masher | Weapon | Knife | weapon |
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

_Truncated: 150 more dump item-id hits._
### `0x141853EE0` `0x82` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x4B` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x4F` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x50` | byte | 10 Zwill Straightblade | Rare, Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x7B` | byte | 126 Snowmelt Bomb | Weapon | Bomb | weapon |
| `+0x7C` | byte | 139 Crystal Shield | Shield | Shield | shield |
| `+0x7D` | byte | 79 Crossbow | Weapon | Crossbow | weapon |
| `+0x7E` | byte | 206 Luminous Robe | Armor | Robe | armor |
| `+0x7F` | byte | 243 Ether | Rare | Item |  |
| `+0x80` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x81` | byte | 126 Snowmelt Bomb | Weapon | Bomb | weapon |
| `+0x82` | byte | 102 Partisan | Weapon | Polearm | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 59 Oak Staff | Weapon | Staff | weapon |
| `+0x85` | byte | 23 Blood Sword | Rare, Weapon | Sword | weapon |
| `+0x86` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x87` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x88` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x89` | byte | 13 Kodachi | Weapon | NinjaBlade | weapon |
| `+0x8A` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x8B` | byte | 108 Battle Bamboo | Weapon | Pole | weapon |
| `+0x8C` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x8D` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8E` | byte | 75 Blaze Gun | Rare, Weapon | Gun | weapon |
| `+0x8F` | byte | 119 Damask Cloth | Weapon | Cloth | weapon |
| `+0x90` | byte | 35 Excalibur | Rare, Weapon | KnightSword | weapon |
| `+0x91` | byte | 98 Omnilex | Rare, Weapon | Book | weapon |
| `+0x92` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x93` | byte | 94 Faerie Harp | Rare, Weapon | Instrument | weapon |
| `+0x96` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x9E` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x18D` | byte | 255 | Rare | None |  |
| `+0x18E` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x18F` | byte | 134 Ice Shield | Shield | Shield | shield |
| `+0x191` | byte | 253 Phoenix Down | Rare | Item |  |
| `+0x1B5` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B8` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | byte | 255 | Rare | None |  |
| `+0x1FC` | byte | 197 Ninja Gear | Rare, Armor | Clothing | armor |
| `+0x1FD` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x4B` | word | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x4E` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x52` | word | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | word | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x93` | word | 94 Faerie Harp | Rare, Weapon | Instrument | weapon |
| `+0x96` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x9E` | word | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x18F` | word | 134 Ice Shield | Shield | Shield | shield |
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
### `0x141854AE0` `0x82` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x4B` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x50` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x7B` | byte | 236 Chantage | Rare, Accessory | Perfume | accessory |
| `+0x7C` | byte | 149 Golden Helm | Headgear | Helmet | armor |
| `+0x7D` | byte | 84 Silver Bow | Weapon | Bow | weapon |
| `+0x7E` | byte | 204 White Robe | Armor | Robe | armor |
| `+0x7F` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x80` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x81` | byte | 82 Gastrophetes | Rare, Weapon | Crossbow | weapon |
| `+0x82` | byte | 119 Damask Cloth | Weapon | Cloth | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 127 Spark Bomb | Weapon | Bomb | weapon |
| `+0x85` | byte | 243 Ether | Rare | Item |  |
| `+0x86` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x87` | byte | 88 Mythril Bow | Weapon | Bow | weapon |
| `+0x88` | byte | 24 Coral Sword | Weapon | Sword | weapon |
| `+0x89` | byte | 14 Ninja Longblade | Weapon | NinjaBlade | weapon |
| `+0x8A` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x8B` | byte | 108 Battle Bamboo | Weapon | Pole | weapon |
| `+0x8C` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x8D` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x8E` | byte | 75 Blaze Gun | Rare, Weapon | Gun | weapon |
| `+0x8F` | byte | 119 Damask Cloth | Weapon | Cloth | weapon |
| `+0x90` | byte | 35 Excalibur | Rare, Weapon | KnightSword | weapon |
| `+0x91` | byte | 98 Omnilex | Rare, Weapon | Book | weapon |
| `+0x92` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x93` | byte | 94 Faerie Harp | Rare, Weapon | Instrument | weapon |
| `+0x96` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x9E` | byte | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x18D` | byte | 255 | Rare | None |  |
| `+0x18E` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x18F` | byte | 134 Ice Shield | Shield | Shield | shield |
| `+0x191` | byte | 247 Eye Drops | Rare | Item |  |
| `+0x1B5` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B8` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x1BE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | byte | 255 | Rare | None |  |
| `+0x1FC` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1FD` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x4B` | word | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x52` | word | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | word | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x93` | word | 94 Faerie Harp | Rare, Weapon | Instrument | weapon |
| `+0x96` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x9E` | word | 64 Golden Staff | Weapon | Staff | weapon |
| `+0x18F` | word | 134 Ice Shield | Shield | Shield | shield |
| `+0x191` | word | 247 Eye Drops | Rare | Item |  |
| `+0x1B4` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B5` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B7` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B8` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | word | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x1BD` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1BE` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | word | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | word | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | word | 255 | Rare | None |  |
| `+0x1FD` | word | 3 Blind Knife | Weapon | Knife | weapon |
### `0x1418540E0` `0x82` foe t3 dump scan
| Offset | Width | Item | Type | Category | Secondary |
| --- | --- | --- | --- | --- | --- |
| `+0x4B` | byte | 23 Blood Sword | Rare, Weapon | Sword | weapon |
| `+0x4F` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x50` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x51` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x52` | byte | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x7B` | byte | 164 Celebrant's Miter | Headgear | Hat | armor |
| `+0x7C` | byte | 139 Crystal Shield | Shield | Shield | shield |
| `+0x7D` | byte | 105 Dragon Whisker | Rare, Weapon | Polearm | weapon |
| `+0x7E` | byte | 225 Jade Armlet | Accessory | Armlet | accessory |
| `+0x7F` | byte | 218 Bracers | Accessory | Armguard | accessory |
| `+0x80` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x81` | byte | 232 Elven Cloak | Accessory | Cloak | accessory |
| `+0x82` | byte | 86 Lightning Bow | Weapon | Bow | weapon |
| `+0x83` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x84` | byte | 172 Leather Armor | Armor | Armor | armor |
| `+0x85` | byte | 76 Blaster | Rare, Weapon | Gun | weapon |
| `+0x86` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x87` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x88` | byte | 132 Mythril Shield | Shield | Shield | shield |
| `+0x89` | byte | 15 Spellbinder | Weapon | NinjaBlade | weapon |
| `+0x8A` | byte | 6 Main Gauche | Weapon | Knife | weapon |
| `+0x8B` | byte | 116 Fallingstar Bag | Rare, Weapon | Bag | weapon |
| `+0x8C` | byte | 30 Runeblade | Weapon | Sword | weapon |
| `+0x8D` | byte | 50 Slasher | Weapon | Axe | weapon |
| `+0x8E` | byte | 85 Ice Bow | Weapon | Bow | weapon |
| `+0x8F` | byte | 116 Fallingstar Bag | Rare, Weapon | Bag | weapon |
| `+0x90` | byte | 39 Kotetsu | Weapon | Katana | weapon |
| `+0x91` | byte | 98 Omnilex | Rare, Weapon | Book | weapon |
| `+0x92` | byte | 7 Orichalcum Dirk | Weapon | Knife | weapon |
| `+0x93` | byte | 91 Perseus Bow | Rare, Weapon | Bow | weapon |
| `+0x96` | byte | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x9C` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x9D` | byte | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x18D` | byte | 255 | Rare | None |  |
| `+0x18E` | byte | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x18F` | byte | 137 Diamond Shield | Shield | Shield | shield |
| `+0x191` | byte | 252 Remedy | Rare | Item |  |
| `+0x1B5` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B8` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | byte | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x1BE` | byte | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | byte | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | byte | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | byte | 255 | Rare | None |  |
| `+0x1FC` | byte | 218 Bracers | Accessory | Armguard | accessory |
| `+0x1FD` | byte | 3 Blind Knife | Weapon | Knife | weapon |
| `+0x4B` | word | 23 Blood Sword | Rare, Weapon | Sword | weapon |
| `+0x4E` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x4F` | word | 257 Akademy Blade | Rare, Weapon | Sword | weapon |
| `+0x52` | word | 128 Escutcheon | Shield | Shield | shield |
| `+0x5D` | word | 4 Mage Masher | Weapon | Knife | weapon |
| `+0x79` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x93` | word | 91 Perseus Bow | Rare, Weapon | Bow | weapon |
| `+0x96` | word | 8 Assassin's Dagger | Weapon | Knife | weapon |
| `+0x9D` | word | 16 Sasuke's Blade | Rare, Weapon | NinjaBlade | weapon |
| `+0x18F` | word | 137 Diamond Shield | Shield | Shield | shield |
| `+0x191` | word | 252 Remedy | Rare | Item |  |
| `+0x1B4` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B5` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1B7` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1B8` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1BC` | word | 2 Mythril Knife | Weapon | Knife | weapon |
| `+0x1BD` | word | 256 Materia Blade Plus | Rare, Weapon | Sword | weapon |
| `+0x1BE` | word | 1 Dagger | Weapon | Knife | weapon |
| `+0x1EA` | word | 100 Spear | Weapon | Polearm | weapon |
| `+0x1EE` | word | 80 Poison Crossbow | Weapon | Crossbow | weapon |
| `+0x1F5` | word | 255 | Rare | None |  |
| `+0x1FD` | word | 3 Blind Knife | Weapon | Knife | weapon |

## Diff Offset Frequency
| Offset | Count |
| --- | ---: |
| `+0x4F` | 3 |
| `+0x50` | 2 |
| `+0x46` | 1 |
| `+0x4A` | 1 |
| `+0x4E` | 1 |
| `+0x51` | 1 |

## Unit Diffs
### `0x141853EE0` `0x82`
- `+0x4F:01->04 +0x50:0A->07`
### `0x141854AE0` `0x82`
- `+0x4F:00->03 +0x50:02->04`
### `0x1418540E0` `0x82`
- `+0x4F:01->04`
### `0x141855CE0` `0x01`
- `+0x46:14->00 +0x4A:32->00 +0x4E:19->00`
- `+0x51:02->01`

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
- Damage events: 0
- Healing events: 0
- Context events: 0
- Runtime context events: 0
- Rewrite events: 0

### Neuter Placeholder Check
- No parsed HP rewrite lines with `vanillaDamage`.
- Verdict: **no HP placeholder evidence**

### HP Write Proof Check
- Concrete HP rewrites: 0
- Concrete `finalDamage=1` rewrites: 0/0
- HP rewrite failures: 0
- Baseline sample age: missing
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

