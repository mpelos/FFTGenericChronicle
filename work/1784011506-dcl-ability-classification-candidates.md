# DCL ability classification manifest

Source: `D:/Projects/FFTGenericChronicle/work/wotl_ability_action_baseline.csv` (`512` records).
Row manifest: `work/1784011506-dcl-ability-classification-candidates.csv`.

This report classifies routing and implementation readiness, not final balance. A route marked
`authoring-required` or `reverse-engineering` must preserve vanilla until its explicit DCL rule exists.
The `candidate_*` columns are conservative technical candidates. `authoring-open` rows are not
runtime defaults and must not be promoted without an explicit per-ability decision.

## Readiness

| Readiness | Abilities |
| --- | ---: |
| authoring-required | 89 |
| data-authoring | 128 |
| data-or-authoring-required | 4 |
| formula-map-required | 99 |
| native-periodic-preserved | 14 |
| native-pool-mapped | 1 |
| native-special-mapped | 8 |
| native-special-preserved | 15 |
| reserved-inert | 2 |
| surface-ready | 106 |
| wired | 46 |

## Metadata candidate coverage

| Certainty | Abilities |
| --- | ---: |
| authoring-open | 180 |
| candidate-complete | 332 |

| Blocking scope | Abilities |
| --- | ---: |
| closed | 332 |
| design | 135 |
| mixed | 45 |

| Open field | Abilities |
| --- | ---: |
| action_kind | 22 |
| avoidance_policy | 95 |
| damage_type | 76 |
| side_effect_policy | 45 |
| status_category | 34 |

## Routes

| Route | Abilities |
| --- | ---: |
| basic_attack | 1 |
| command_meta | 40 |
| dragon_gated_support | 4 |
| drain | 12 |
| equipment_break | 8 |
| gender_gated_status | 4 |
| golem_pool | 1 |
| hp_mp_healing | 1 |
| item_command | 14 |
| level_gain | 1 |
| magic_damage | 39 |
| magic_healing | 6 |
| magical_status | 38 |
| missing_hp_or_self_destruct | 7 |
| mp_damage | 5 |
| mp_healing | 1 |
| multihit_magic | 17 |
| multihit_physical | 2 |
| noncanonical_healing | 3 |
| noncanonical_magic_or_hybrid_damage | 45 |
| passive | 88 |
| physical_ability_damage | 20 |
| physical_damage | 17 |
| physical_status | 13 |
| recoil_physical_damage | 4 |
| reserved_or_unknown | 2 |
| revive_or_percent_heal | 4 |
| sacrifice_heal | 2 |
| song_or_dance | 14 |
| special_hp_damage | 8 |
| stat_or_trait_buff | 5 |
| stat_or_trait_debuff | 12 |
| status_or_buff | 46 |
| steal | 15 |
| talk_trait_or_status | 10 |
| turn_control | 2 |
| unit_transformation | 1 |

## Formula groups

| Formula | Count | Route | Readiness | Examples |
| --- | ---: | --- | --- | --- |
| 0x00 | 1 | physical_damage | formula-map-required | 40:<Unknown>  |
| 0x01 | 16 | physical_damage | formula-map-required | 265:Choco Beak; 270:Tackle; 275:Bite; 280:Claw |
| 0x08 | 39 | magic_damage | wired | 15:Holy; 16:Fire; 17:Fira; 18:Firaga |
| 0x09 | 3 | special_hp_damage | authoring-required | 42:Gravity; 43:Graviga; 73:Lich |
| 0x0A | 38 | magical_status | surface-ready | 28:Poison; 29:Toad; 34:Slow; 35:Slowja |
| 0x0B | 15 | status_or_buff | surface-ready | 7:Reraise; 8:Regen; 9:Protect; 10:Protectja |
| 0x0C | 6 | magic_healing | wired | 1:Cure; 2:Cura; 3:Curaga; 4:Curaja |
| 0x0D | 3 | basic_attack, revive_or_percent_heal | authoring-required, wired | 0:<Nothing>; 5:Raise; 6:Arise |
| 0x0E | 1 | special_hp_damage | authoring-required | 30:Death |
| 0x0F | 2 | drain | authoring-required | 47:Empowerment; 235:Syphon |
| 0x10 | 2 | drain | authoring-required | 48:Invigoration; 236:Drain |
| 0x12 | 1 | turn_control | authoring-required | 41:Quick |
| 0x14 | 1 | golem_pool | native-pool-mapped | 65:Golem |
| 0x15 | 1 | turn_control | authoring-required | 233:Return |
| 0x16 | 1 | mp_damage | surface-ready | 231:Disempower |
| 0x17 | 1 | special_hp_damage | authoring-required | 222:Gravija |
| 0x1A | 3 | stat_or_trait_debuff | authoring-required | 197:Speedsap; 198:Powersap; 199:Mindsap |
| 0x1B | 1 | mp_damage | surface-ready | 196:Magicksap |
| 0x1C | 7 | song_or_dance | native-periodic-preserved | 86:Seraph Song; 87:Life's Anthem; 88:Rousing Melody; 89:Battle Chant |
| 0x1D | 7 | song_or_dance | native-periodic-preserved | 93:Witch Hunt; 94:Mincing Minuet; 95:Slow Dance; 96:Polka |
| 0x1E | 7 | multihit_magic | authoring-required | 169:Heaven's Wrath; 170:Ashura; 171:Adamantine Blade; 172:Maelstrom |
| 0x1F | 6 | multihit_magic | authoring-required | 175:Hell's Wrath; 176:Nether Ashura; 177:Nether Blade; 178:Nether Maelstrom |
| 0x20 | 6 | noncanonical_magic_or_hybrid_damage | formula-map-required | 76:Ashura; 77:Kotetsu; 80:Ame-no-Murakumo; 82:Muramasa |
| 0x21 | 1 | mp_damage | surface-ready | 78:Bizen Osafune |
| 0x22 | 2 | status_or_buff | surface-ready | 81:Kiyomori; 84:Masamune |
| 0x23 | 1 | noncanonical_healing | formula-map-required | 79:Murasame |
| 0x24 | 12 | noncanonical_magic_or_hybrid_damage | formula-map-required | 126:Sinkhole; 127:Torrent; 128:Tanglevine; 129:Contortion |
| 0x25 | 4 | equipment_break | native-special-mapped | 138:Rend Helm; 139:Rend Armor; 140:Rend Shield; 141:Rend Weapon |
| 0x26 | 10 | steal | native-special-preserved | 110:Steal Helm; 111:Steal Armor; 112:Steal Shield; 113:Steal Weapon |
| 0x27 | 3 | steal | native-special-preserved | 108:Steal Gil; 307:Glitterlust; 359:Plunder Gil |
| 0x28 | 2 | steal | surface-ready | 115:Steal EXP; 366:Plunder EXP |
| 0x29 | 4 | gender_gated_status | authoring-required | 109:Steal Heart; 201:Charm; 311:Snort; 360:Plunder Heart |
| 0x2A | 10 | talk_trait_or_status | authoring-required | 116:Entice; 117:Stall; 118:Praise; 119:Intimidate |
| 0x2B | 3 | stat_or_trait_debuff | authoring-required | 143:Rend Speed; 144:Rend Power; 145:Rend Magick |
| 0x2C | 1 | mp_damage | surface-ready | 142:Rend MP |
| 0x2D | 5 | physical_ability_damage | formula-map-required | 155:Judgment Blade; 156:Cleansing Strike; 157:Northswain's Strike; 158:Hallowed Bolt |
| 0x2E | 4 | equipment_break | native-special-mapped | 160:Crush Armor; 161:Crush Helm; 162:Crush Weapon; 163:Crush Accessory |
| 0x2F | 1 | drain | authoring-required | 164:Duskblade |
| 0x30 | 1 | drain | authoring-required | 165:Shadowblade |
| 0x31 | 10 | physical_ability_damage | formula-map-required | 100:Cyclone; 102:Aurablast; 103:Shockwave; 225:Bomblet |
| 0x32 | 1 | multihit_physical | authoring-required | 101:Pummel |
| 0x33 | 1 | physical_status | surface-ready | 105:Purification |
| 0x34 | 1 | hp_mp_healing | surface-ready | 106:Chakra |
| 0x35 | 2 | revive_or_percent_heal | authoring-required | 107:Revive; 312:Squeal |
| 0x36 | 2 | stat_or_trait_buff | authoring-required | 146:Focus; 323:Beef Up |
| 0x37 | 4 | physical_ability_damage | formula-map-required | 147:Rush; 148:Throw Stone; 281:Cat Scratch; 336:Tail Sweep |
| 0x38 | 22 | status_or_buff | surface-ready | 149:Salve; 181:Petrify; 182:Shadowbind; 183:Suffocate |
| 0x39 | 1 | stat_or_trait_buff | authoring-required | 150:Tailwind |
| 0x3A | 1 | stat_or_trait_buff | authoring-required | 151:Steel |
| 0x3B | 1 | stat_or_trait_buff | authoring-required | 153:Shout |
| 0x3C | 2 | sacrifice_heal | authoring-required | 152:Chant; 355:Energize |
| 0x3D | 3 | status_or_buff | surface-ready | 282:Blaster; 288:Mind Blast; 304:Doom |
| 0x3F | 2 | physical_status | surface-ready | 213:Leg Shot; 214:Arm Shot |
| 0x40 | 1 | physical_status | surface-ready | 215:Seal Evil |
| 0x41 | 1 | status_or_buff | surface-ready | 168:Celestial Stasis |
| 0x42 | 4 | recoil_physical_damage | authoring-required | 351:Destroy; 352:Compress; 353:Dispose; 354:Pulverize |
| 0x43 | 5 | missing_hp_or_self_destruct | authoring-required | 185:Revengeance; 256:Vengeance; 259:Blade Burst; 333:Almagest |
| 0x44 | 1 | mp_damage | surface-ready | 186:Manaburn |
| 0x45 | 1 | special_hp_damage | authoring-required | 260:Ascension |
| 0x47 | 2 | drain | authoring-required | 200:Blood Drain; 284:Blood Drain |
| 0x4C | 2 | noncanonical_healing | formula-map-required | 269:Choco Cure; 318:Life Nymph |
| 0x4D | 2 | drain | authoring-required | 274:Bloodfeast; 298:Drain Touch |
| 0x4E | 25 | noncanonical_magic_or_hybrid_damage | formula-map-required | 248:Ice Breath; 249:Fire Breath; 250:Thunder Breath; 257:Braver |
| 0x4F | 1 | missing_hp_or_self_destruct | authoring-required | 271:Goblin Punch |
| 0x50 | 9 | physical_status | surface-ready | 104:Doom Fist; 273:Eye Gouge; 283:Venom Fang; 286:Ink |
| 0x51 | 3 | status_or_buff | surface-ready | 268:Choco Esuna; 316:Guardian Nymph; 317:Shell Nymph |
| 0x52 | 1 | missing_hp_or_self_destruct | authoring-required | 277:Self-Destruct |
| 0x53 | 2 | special_hp_damage | authoring-required | 332:Twister; 341:Tri-Breath |
| 0x54 | 1 | mp_healing | surface-ready | 319:Magick Nymph |
| 0x55 | 1 | stat_or_trait_debuff | authoring-required | 309:Peck |
| 0x56 | 1 | stat_or_trait_debuff | authoring-required | 303:Beam |
| 0x57 | 1 | level_gain | native-special-preserved | 314:Bequeath Bacon |
| 0x58 | 1 | unit_transformation | native-special-preserved | 329:Malboro Spores |
| 0x59 | 1 | stat_or_trait_debuff | authoring-required | 289:Level Drain |
| 0x5A | 1 | dragon_gated_support | data-or-authoring-required | 251:Dragon's Charm |
| 0x5B | 1 | dragon_gated_support | data-or-authoring-required | 252:Dragon's Gift |
| 0x5C | 1 | dragon_gated_support | data-or-authoring-required | 253:Dragon's Might |
| 0x5D | 1 | dragon_gated_support | data-or-authoring-required | 254:Dragon's Speed |
| 0x5E | 3 | multihit_magic | authoring-required | 342:Tri-Thunder; 343:Tri-Flame; 344:Dark Whisper |
| 0x5F | 1 | multihit_magic | authoring-required | 349:Nanoflare |
| 0x61 | 2 | stat_or_trait_debuff | authoring-required | 54:Trepidation; 242:Chicken |
| 0x62 | 1 | stat_or_trait_debuff | authoring-required | 302:Dread Gaze |
| 0x65 | 1 | drain | authoring-required | 45:Chant |
| 0x66 | 1 | drain | authoring-required | 184:Infernal Strike |
| 0x67 | 1 | physical_ability_damage | formula-map-required | 219:Crushing Blow |
| 0x68 | 1 | noncanonical_magic_or_hybrid_damage | formula-map-required | 220:Abyssal Blade |
| 0x69 | 1 | noncanonical_magic_or_hybrid_damage | formula-map-required | 357:Unholy Sacrifice |
| 0x6A | 1 | multihit_physical | authoring-required | 358:Barrage |
| (blank) | 144 | command_meta, item_command, passive, reserved_or_unknown | data-authoring, formula-map-required, reserved-inert | 368:Potion; 369:High Potion; 370:X-Potion; 371:Ether |

## Gate summary

- Wired now: `46` ability records.
- Formula/status/channel surface ready: `106` records.
- Ability/formula/data authoring still required: `192` records.
- Reverse engineering still required: `0` records.
- Metadata blocked only by design authoring: `135` records.
- Metadata blocked by technical investigation: `0` records.
- Metadata blocked by both mechanism and design: `45` records.
- High-risk special or externally dispatched records: `157`.
- `unclassified` must remain zero; the tool fails when a new nonblank Formula lacks a route.
