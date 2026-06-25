# Raw-stat offsets confirmed via equipment-bonus closed loop (2026-06-25)

Promotes `+0x38/0x39/0x3A` (raw/base PA/MA/Speed) from MEDIUM to **CONFIRMED**.

## Claim

The unit struct stores BOTH the base (pre-equipment) stat and the effective stat:

- raw / base : `+0x38` PA, `+0x39` MA, `+0x3A` Speed
- effective  : `+0x3E` PA, `+0x3F` MA, `+0x40` Speed

and the relationship `effective = raw + Σ(equipment PA/MA/Speed bonuses)` holds exactly.

## Method (fully offline, reproducible)

1. Read each unit's effective + raw stats from its level-matched dump (already CONFIRMED offsets).
2. Read each unit's equipped item ids from the struct equipment block `+0x1A..0x26`
   (head/body/accessory/R-weapon/R-shield/L-weapon/L-shield, 16-bit words; 255/0 = empty).
3. Sum `bonus_pa`, `bonus_ma`, `bonus_speed` over those item ids from `work/item_catalog.csv`.
4. Check `raw + bonus == effective` for every unit and every stat.

Equipped item ids read from the dumps:

| Unit | head | body | accessory | weapon(s) | shield |
|---|---|---|---|---|---|
| Ramza   | 156 | 185 | 218 Bracers         | 37 Chaos Blade   | 142 Venetian |
| Beowulf | 167 | 206 | 217 Magepower Gloves | 30 Runeblade     | - |
| Agrias  | 167 | 207 | 216 Genji Gloves     | 30 Runeblade     | - |
| Cloud   | 155 | 183 | 214 Red Shoes        | 256 Materia Blade+ | (two-handed) |
| Ninja   | 168 | 197 | 236 Chantage         | 17 / 18 Iga/Koga | - |

## Result — 15/15 exact

| Unit | raw PA/MA/Spd | equip bonus PA/MA/Spd | raw+bonus | effective | verdict |
|---|---|---|---|---|---|
| Ramza   | 17/9/10 | +3/0/0 | 20/9/10 | 20/9/10 | OK |
| Agrias  | 7/17/11 | +4/6/1 | 11/23/12 | 11/23/12 | OK |
| Beowulf | 5/12/8  | +0/5/1 | 5/17/9  | 5/17/9  | OK |
| Cloud   | 14/8/9  | +0/5/0 | 14/13/9 | 14/13/9 | OK |
| Ninja   | 15/7/12 | +0/0/4 | 15/7/16 | 15/7/16 | OK |

Per-item bonuses that summed in (from `item_catalog.csv`):
Bracers +3/0/0 · Genji Gloves +2/2/0 · Lordly Robe +2/1/0 · Lambent Hat +0/1/1 ·
Runeblade +0/2/0 · Magepower Gloves +0/2/0 · Red Shoes +0/1/0 · Materia Blade+ +0/4/0 ·
Thief's Cap +0/0/2 · Ninja Gear +0/0/2.

## Why this matters

A single closed loop independently re-confirms four things at once: the raw offsets, the effective
offsets, the equipment-id block, and the item catalog's bonus columns. For the custom-formula goal
this means a formula can read a unit's **base** stats, its **effective** stats, and derive the exact
**equipment contribution** — for attacker and target alike.

Reproduce: read offsets via `tools/map_attributes.py work/gt-master.json work/live-captures/*.txt
--show 0x1A,0x1C,0x1E,0x20,0x22,0x24,0x26`, then sum `bonus_pa/ma/speed` from `work/item_catalog.csv`.
