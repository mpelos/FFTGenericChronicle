# Baseline Weapon Family Summary

Source: `work/baseline_weapons.csv` joined with `work/item_catalog.csv`.

This is verified local IVC table data. It does not prove vanilla formula behavior in
battle, but it replaces the old missing-weapon-baseline state for weapon WP, range,
flags, elements, and per-weapon formula ids.

| Family | Count | WP min/med/max | Sim v0.2 WP | Formula IDs | Ranges | Top verified items |
| --- | ---: | ---: | ---: | --- | --- | --- |
| `axe` | 3 | 9/12/16 | 20 | `1` | `1` | Slasher(WP16,F1); Giant's Axe(WP12,F1); Battle Axe(WP9,F1) |
| `bag` | 4 | 10/13/20 | 20 | `1` | `1` | Fallingstar Bag(WP20,F1); Hardscale Bag(WP14,F1); Proudhide Bag(WP12,F1); Catskin Bag(WP10,F1) |
| `book` | 4 | 7/8/11 | 15 | `1` | `3` | Omnilex(WP11,F1); Papyrus Codex(WP9,F1); Bestiary(WP8,F1); Battle Folio(WP7,F1) |
| `cloth_weapon` | 3 | 8/10/15 | 14 | `1` | `2` | Wyrmweave Silk(WP15,F1); Cashmere(WP10,F1); Damask Cloth(WP8,F1) |
| `crossbow` | 6 | 3/4/10 | 14 | `1` | `4` | Gastrophetes(WP10,F1); Hunting Crossbow(WP6,F1); Poison Crossbow(WP4,F1); Crossbow(WP4,F1); Knightslayer(WP3,F1) |
| `flail` | 4 | 9/13/23 | 24 | `1|2` | `1` | Scorpion Tail(WP23,F1); Morning Star(WP16,F1); Flail of Flame(WP11,F2); Iron Flail(WP9,F1) |
| `gun` | 6 | 6/18/22 | 12 | `3|4` | `8` | Blaster(WP22,F4); Blaze Gun(WP21,F4); Glacial Gun(WP20,F4); Stoneshooter(WP16,F3); Mythril Gun(WP8,F3) |
| `instrument` | 3 | 10/13/15 | 10 | `1|6` | `3` | Faerie Harp(WP15,F1); Bloodstring Harp(WP13,F6); Lamia's Harp(WP10,F1) |
| `katana` | 10 | 7/11/25 | 18 | `1` | `1` | Chirijiraden(WP25,F1); Masamune(WP18,F1); Kiku-ichimonji(WP15,F1); Muramasa(WP14,F1); Kiyomori(WP12,F1) |
| `knife` | 10 | 3/5/12 | 12 | `1` | `1` | Zwill Straightblade(WP12,F1); Air Knife(WP10,F1); Assassin's Dagger(WP7,F1); Orichalcum Dirk(WP7,F1); Main Gauche(WP6,F1) |
| `knight_sword` | 5 | 16/21/40 | 20 | `1` | `1` | Chaos Blade(WP40,F1); Ragnarok(WP24,F1); Excalibur(WP21,F1); Save the Queen(WP18,F1); Defender(WP16,F1) |
| `longbow` | 9 | 4/7/16 | 15 | `1|2` | `5` | Perseus Bow(WP16,F1); Yoichi Bow(WP12,F1); Artemis Bow(WP10,F1); Windslash Bow(WP8,F1); Mythril Bow(WP7,F1) |
| `ninja_blade` | 8 | 8/12/15 | 13 | `1` | `1` | Koga Blade(WP15,F1); Iga Blade(WP15,F1); Sasuke's Blade(WP14,F1); Spellbinder(WP13,F1); Ninja Longblade(WP12,F1) |
| `pole` | 8 | 6/9/16 | 13 | `1` | `2` | Whale Whisker(WP16,F1); Eight-fluted Pole(WP12,F1); Ivory Pole(WP11,F1); Gokuu's Pole(WP10,F1); Iron Fan(WP9,F1) |
| `rod` | 8 | 3/3/5 | 10 | `1|2` | `1` | Rod of Faith(WP5,F1); Dragon Rod(WP5,F1); Wizard's Rod(WP4,F1); Poison Rod(WP3,F1); Ice Rod(WP3,F2) |
| `spear` | 8 | 8/11/30 | 15 | `1|2` | `2` | Javelin(WP30,F1); Dragon Whisker(WP17,F1); Holy Lance(WP14,F2); Obelisk(WP12,F1); Partisan(WP11,F1) |
| `staff` | 8 | 3/4/7 | 14 | `1|7` | `1` | Staff of the Magi(WP7,F1); Zeus Mace(WP6,F1); Golden Staff(WP6,F1); Serpent Staff(WP5,F1); Mage's Staff(WP4,F1) |
| `sword` | 14 | 1/8/14 | 16 | `1|2|6` | `1` | Runeblade(WP14,F1); Icebrand(WP13,F2); Platinum Sword(WP12,F1); Materia Blade(WP10,F1); Diamond Sword(WP10,F1) |

Ignored non-design/extra categories: Bomb=3, None=1, Throwing=3.
