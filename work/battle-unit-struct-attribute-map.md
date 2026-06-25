# Battle-unit struct — confidence-rated attribute map (2026-06-25)

Built autonomously from the existing `[DUMP]` captures + ground truth from 10 in-game status/More
screens (5 units: Ramza 0x01, Beowulf 0x1F, Agrias 0x1E, Cloud 0x32, Ninja "Rion" 0x80) and
user-provided zodiac/brave/faith. All numeric values reconciled to the **level-matched dumps**
(engine memory = authoritative; low-res photo OCR erred on some digits). Tools: `map_attributes.py`
(supervised offset finder, byte/word/nibble/bit, level-aware), `profile_struct.py` (unsupervised
volatile/const/varies classifier), `dump_levels.py` (level/recency filter). Ground truth in
`work/character-attributes-ground-truth.md`; per-unit values in `work/gt-master.json`; machine
result in `work/attribute-map.result.json`.

Confidence: CONFIRMED (ground-truth 5/5 this session, or prior live test) · MEDIUM (strong
structural inference) · LOW (candidate, needs more data) · DEFER (not mappable from these static
full-HP/no-status captures).

## CONFIRMED — safe to expose in formulas

| Offset | Width | Attribute | Per-unit (Ra/Be/Ag/Cl/Ni) | Note |
|---|---|---|---|---|
| +0x00 | byte | Char/unit id | 01/1F/1E/32/80 | identity |
| +0x04 | byte | Team/group id | 0/0/0/0/0 | all player team |
| +0x05 | byte | Friend/foe (bit 0x10) | — | prior live (doc05) |
| +0x06 | byte | **Gender flags** | M/M/F/M/M | bit7 0x80=Male, bit6 0x40=Female, bit5 0x20=Monster (classic FFT). Ninja=Male (discovered) |
| +0x09 | hi-nibble | **Zodiac** | 5/6/3/10/4 | classic order Aries0..Pisces11 (Virgo/Libra/Cancer/Aquarius/Leo) |
| +0x28 | byte | EXP (to next level) | volatile | changes as unit acts |
| +0x29 | byte | Level | 75/68/69/67/71 | 5/5 |
| +0x2A | byte | MaxBrave | 97/97/97/97/97 | |
| +0x2B | byte | Brave | 97/97/97/97/97 | 5/5 |
| +0x2C | byte | MaxFaith | 70/65/63/65/72 | |
| +0x2D | byte | Faith | 70/65/63/65/72 | 5/5 (best disambiguator) |
| +0x30 | word | HP (current) | volatile | damage |
| +0x32 | word | MaxHP | 567/314/322/428/277 | 5/5 |
| +0x34 | word | MP (current) | volatile | |
| +0x36 | word | MaxMP | 85/180/232/89/41 | 5/5 |
| +0x3E | byte | PA (effective) | 20/5/11/14/15 | 5/5 |
| +0x3F | byte | MA (effective) | 9/17/23/13/7 | 5/5 |
| +0x40 | byte | Speed (effective) | 10/9/12/9/16 | 5/5; drives CT. ⚠ status screen appeared to show lower values for Ag/Be/Ni in my OCR — flagged, engine value trusted |
| +0x41 | byte | CT (charge time) | volatile | prior live (doc05) |
| +0x42 | byte | Move | 6/6/5/4/6 | 5/5 |
| +0x43 | byte | Jump | 3/3/3/3/4 | 5/5 |
| +0x44 | byte | **Weapon Attack R** (effective) | 40/14/14/16/15 | matches More screens |
| +0x45 | byte | **Weapon Attack L** (effective) | 0/0/0/0/15 | Ninja dual-wield (Koga) |
| +0x46 | byte | **Weapon Parry R %** | 20/15/15/10/10 | Ninja 10 matches More |
| +0x47 | byte | **Weapon Parry L %** | 0/0/0/0/5 | Ninja 5 matches More |
| +0x4A | byte | **Shield Physical Parry %** | 50/0/0/0/0 | Ramza Venetian Shield = More |
| +0x4B | byte | **Physical Evasion %** | 0/5/5/20/30 | matches More exactly |
| +0x4E | byte | **Shield Magick Parry %** | 25/0/0/0/0 | Ramza = More |
| +0x1A | word | Equip: head | — | prior (b5c818e) |
| +0x1C | word | Equip: body | — | |
| +0x1E | word | Equip: accessory | — | |
| +0x20/+0x22 | word | Equip: R weapon / shield | — | |
| +0x24/+0x26 | word | Equip: L weapon / shield | — | empty hand 0x00FF |
| +0x61 | byte | Status (KO = bit5 0x20) | volatile | prior live |

(Bold = newly mapped this session.)

## MEDIUM — strong structural inference, not directly ground-truthed

| Offset | Width | Guess | Per-unit | Reasoning |
|---|---|---|---|---|
| +0x38 | byte | raw/base PA | 17/5/7/14/15 | == effective PA for units with no PA gear (Be/Cl/Ni); Ra 17+Bracers≈20 |
| +0x39 | byte | raw/base MA | 9/12/17/8/7 | == effective MA where no MA gear (Ra/Ni) |
| +0x3A | byte | raw/base Speed | 10/8/11/9/12 | base before equip speed bonus |

These form a raw-stat block mirroring the effective stats at +0x3E/3F/40. doc05 predicted raw
stats exist. To confirm: ground-truth a unit's base stats (unequipped) or known gear bonuses.

## LOW — candidate, needs more data

| Offset | Per-unit | Likely | What's needed |
|---|---|---|---|
| +0x01 | 16/20/18/19/17 | sprite/type set? | tight 16-20 range; not job-spread |
| +0x02 | 0/14/23/32/3 | **job id?** | needs JobData name→id table |
| +0x13 | 25/69/40/41/6 | **job id?** | needs name→id; spread fits jobs |
| +0x08 | 242/21/173/31/210 | unit-unique / sprite id | >176 so not job |
| +0x3C | 0/5/6/5/0 | raw evade? | small |
| +0x4F/+0x50 | ~8-10 | unknown small stats | |
| +0x52–0x8F | mixed | ability ids / JP / learned-ability bits | needs ability-id ground truth (R/S/M/secondary) |

Job is the highest-value LOW item: two candidate bytes (+0x02, +0x13) each hold 5 distinct static
values consistent with job ids. Confirm by mapping each unit's job name (Squire/Summoner/Black
Mage/Soldier/Ninja) → id via the game's Job nex table, then matching.

## DEFER — not mappable from these captures (need targeted ones)

- **Status bitfield (full)**: only the KO bit (+0x61 0x20) is known. All units here are alive/
  no-status, so the bitfield doesn't vary. Need captures of units under Poison/Haste/Protect/etc.
- **Elemental affinity** (weak/half/absorb/null per element): not located. Likely derived at calc
  time from equipment+job, or in a region needing element-affinity-varied units. Investigate later.
- **Geometry**: position X/Y, height, facing (front/side/back) — not in the static stat block;
  likely volatile or in an unscanned region. Need positional captures.
- **Magic Evasion %, Cloak evasion %**: 0 for all 5 here (constant) → need units that have them.

## Reconciliation notes (image OCR vs dump)

The engine dump is authoritative. My photo OCR of low-res screens erred on: Beowulf MaxHP
(read 514,真 314), Ninja MaxHP (read 377, 真 277), Agrias MaxMP (read 252, 真 232), Beowulf Move
(read 5, 真 6), Cloud Jump (read 4, 真 3), and Speed for Agrias/Beowulf/Ninja (read 8/8/8, 真
12/9/16). The dump values are used everywhere above. Ramza is L76 in the image but L75 in all
dumps (leveled after the last capture) — his dumps are used only for level-independent attributes.

## Next steps
1. Map job (+0x02/+0x13 vs Job nex ids).
2. Expose the CONFIRMED set in the formula context (attacker.* / target.*).
3. Targeted captures for status/elemental/geometry (DEFER items).
