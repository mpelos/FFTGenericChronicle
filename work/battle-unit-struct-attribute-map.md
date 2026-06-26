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
| +0x03 | byte | **Job id** | 160/82/80/88/89 | 5/5, all user/command-corroborated: 80=Black Mage, 82=Summoner, 89=Ninja, 88=Samurai (Cloud, user-confirmed), 160=Ramza special story-Squire (displays "Squire"). Classified VARIES-not-VOLATILE = stable per unit |
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
| +0x38 | byte | **raw/base PA** (pre-equip) | 17/5/7/14/15 | CONFIRMED: raw+Σ(equip bonus)==effective, 5/5 (see proof) |
| +0x39 | byte | **raw/base MA** (pre-equip) | 9/12/17/8/7 | CONFIRMED, 5/5 |
| +0x3A | byte | **raw/base Speed** (pre-equip) | 10/8/11/9/12 | CONFIRMED, 5/5 |
| +0x3E | byte | PA (effective) | 20/5/11/14/15 | 5/5 |
| +0x3F | byte | MA (effective) | 9/17/23/13/7 | 5/5 |
| +0x40 | byte | Speed (effective) | 10/9/12/9/16 | 5/5; drives CT. ⚠ status screen appeared to show lower values for Ag/Be/Ni in my OCR — flagged, engine value trusted |
| +0x41 | byte | CT (charge time) | volatile | prior live (doc05) |
| +0x42 | byte | Move | 6/6/5/4/6 | 5/5 |
| +0x43 | byte | Jump | 3/3/3/3/4 | 5/5 |
| +0x44 | byte | **Weapon Attack R** (effective) | 40/14/14/16/15 | matches More screens; == catalog weapon_power for 4/5 (Chaos Blade 40, Runeblade 14, Iga 15). Cloud's Materia Blade+ = 16 vs catalog 10 → struct holds the *effective* value incl. special/two-hand modifier |
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

### Job stat-growth block — CONFIRMED (NEW, +0x8A..0x93)

The unit struct caches the **entire growth/multiplier row of the unit's job** (from
`work/baseline_jobs.csv`). All 10 fields validate **5/5** against the job table for the 5 units'
job ids (Ramza 160, Beowulf 82 Summoner, Agrias 80 Black Mage, Cloud 88 Samurai, Ninja 89):

| Offset | Field | Per-unit (Ra/Be/Ag/Cl/Ni) |
|---|---|---|
| +0x8A | HP Growth | 12/13/12/12/12 |
| +0x8B | HP Multiplier | 80/70/75/75/70 |
| +0x8C | MP Growth | 20/8/9/14/13 |
| +0x8D | MP Multiplier | 90/125/120/90/50 |
| +0x8E | Speed Growth | 100/100/100/100/80 |
| +0x8F | Speed Multiplier | 100/90/100/100/120 |
| +0x90 | PA Growth | 40/70/60/45/43 |
| +0x91 | PA Multiplier | 140/50/60/128/122 |
| +0x92 | MA Growth | 50/50/50/50/50 (const here) |
| +0x93 | MA Multiplier | 80/125/150/90/75 |

Lower Growth = faster growth (classic FFT). This block is gold for custom formulas: it exposes a
unit's class scaling directly, no job-table lookup needed at calc time. Bonus cross-confirm: the
job table's `CharacterEvasion` (0/5/5/20/30) equals the `+0x4B` value mapped above — so **+0x4B is
the job-derived character/physical evasion**, double-confirmed.

Also identified: **+0x14C = unit display-name string (ASCII)** — Ninja's bytes spell `Rion`
(82 105 111 110); other units 0 here. Trailing bytes (~+0x151) look like stale buffer content
(`wulf`, likely left over from "Beowulf"); only the leading set name is reliable. Non-combat field.

## MEDIUM — strong structural inference, not directly ground-truthed

_(none open — the former raw-stat entries +0x38/39/3A were promoted to CONFIRMED below; see the
equipment-bonus proof.)_

### Raw-stat proof (how +0x38/39/3A were confirmed)

For all 5 units and all 3 stats, `raw(+0x38/39/3A) + Σ(equipment PA/MA/Speed bonuses) ==
effective(+0x3E/3F/40)` — 15/15 exact. Equipment item ids were read from the struct equipment
block (+0x1A..0x26) and their bonuses summed from `work/item_catalog.csv`:

| Unit | raw PA/MA/Spd | equip bonus | == effective | key gear |
|---|---|---|---|---|
| Ramza | 17/9/10 | +3/0/0 | 20/9/10 ✓ | Bracers +3 PA |
| Beowulf | 5/12/8 | +0/5/1 | 5/17/9 ✓ | Magepower Gloves, Runeblade, Lambent Hat |
| Agrias | 7/17/11 | +4/6/1 | 11/23/12 ✓ | Genji Gloves, Lordly Robe, Runeblade, Lambent Hat |
| Cloud | 14/8/9 | +0/5/0 | 14/13/9 ✓ | Materia Blade+ (+4 MA), Red Shoes |
| Ninja | 15/7/12 | +0/0/4 | 15/7/16 ✓ | Thief's Cap +2, Ninja Gear +2 Spd |

This closed loop independently re-confirms the raw offsets, the effective offsets, the equipment
block, and the item catalog at once. Full record: `work/raw-stats-equipment-proof-2026-06-25.md`.

## LOW — candidate, needs more data

| Offset | Per-unit | Likely | What's needed |
|---|---|---|---|
| +0x01 | 16/20/18/19/17 | sprite/type/portrait set? | tight 0x10-0x14 range, sequential-ish; not job |
| +0x02 | 0/14/23/32/3 | ENTD slot / formation index? | NOT job (job is +0x03, confirmed); distinct per unit |
| +0x13 | 25/69/40/41/6 | unknown | NOT job (JobCommandId hypothesis failed 3/5); distinct per unit |
| +0x08 | 242/21/173/31/210 | unit-unique / sprite id | >176 so not job |
| +0x3C | 0/5/6/5/0 | raw evade? | small |
| +0x4F/+0x50/+0x51 | — | **RESOLVED → position X / Y / facing** (see Geometry section) | promoted out of LOW |
| +0x52–0x89 | mixed | ability ids / JP / learned-ability bits | needs ability-id ground truth (R/S/M/secondary); note +0x8A–0x93 just below is now RESOLVED (job stats) |
| +0x94–0x148 | mixed | learned-ability bitfields / JP-per-job / element & status masks | dense, mostly high-byte; needs status/element/ability-varied captures |

Job **RESOLVED** this pass — see CONFIRMED (+0x03). Earlier candidates +0x02 and +0x13 are NOT job:
the +0x13=JobCommandId hypothesis matched only 2/5 (Ramza, Cloud — coincidental), and +0x02 matches
no job id. Both remain unidentified (distinct-per-unit), demoted from job hypotheses to plain unknowns.

## GEOMETRY — position & facing (NEW 2026-06-25, movement-proven, zero new captures)

Mined from the existing action-boundary capture corpus (same zero-capture method that nailed the
equipment block). Corroborated by the public CT table (`docs/modding/04-re-strategy.md`:
`+0x4F/0x50/0x51 = X/Y/Dir`). This is the single biggest blind blocker for the Deep Combat Layer
(facing → defense modifier, reach → tile distance, AoE × position) and it is now in hand.

| Offset | Meaning | Confidence | Evidence |
|---|---|---|---|
| **+0x50** | **Position Y (tile)** | **CONFIRMED (behavior)** | Changes by movement deltas on every unit that moves: 03→07, 05→08, 03→06, 02→05, 0A→09. Multi-unit, tile-range, re-sampled board-wide at each action boundary. |
| **+0x51** | **Facing / Dir (0–3)** | **CONFIRMED (behavior)** | Only ever 0/1/2/3 across the entire corpus; flips on move/turn (03→02, 01→00, 00→03). 4-direction facing. |
| **+0x4F** | **Position X (tile)** | **HIGH (pending E–W capture)** | CT-table-named X; tile-range small int (8/10/10/10/9 across units) adjacent to Y; did NOT change in any captured move (all captured moves were N–S). One east–west move closes it to 5/5. |
| +0x1B9 | turn/action lifecycle flag (candidate) | LOW–MEDIUM | Flips 00→01 on the same diffs as every position change; took 01→03 at one turn-start. Candidate substrate for the DCL guard **reset-on-turn** mechanic (`deep-combat-layer/04`). |

Remaining to fully pin: (1) one E–W movement capture → confirm +0x4F=X; (2) map which Dir value
(0/1/2/3) = which compass direction, needed for the front/side/back defense calc.

## DEFER — not mappable from these captures (need targeted ones)

- **Status bitfield (full)**: only the KO bit (+0x61 0x20) is known. All units here are alive/
  no-status, so the bitfield doesn't vary. Need captures of units under Poison/Haste/Protect/etc.
- **Elemental affinity** (weak/half/absorb/null per element): not located. Likely derived at calc
  time from equipment+job, or in a region needing element-affinity-varied units. Investigate later.
- **Geometry**: X/Y/facing are **RESOLVED** (see Geometry section: +0x4F/+0x50/+0x51). Still
  DEFER: **height/elevation** (not yet located — needed for height-aware ranged identity) and the
  Dir→compass mapping.
- **Magic Evasion %, Cloak evasion %**: 0 for all 5 here (constant) → need units that have them.

## Cross-validation across all 8 captured units (gender / zodiac / job)

Beyond the 5 ground-truthed units, three more unit ids appear in the dumps. Their +0x06/+0x09/+0x03
values are self-consistent with the confirmed encodings, which independently corroborates them:

| Unit id | +0x06 gender | +0x09 hi-nib zodiac | +0x03 job | Notes |
|---|---|---|---|---|
| 0x01 Ramza | Male (0x80) | 5 Virgo | 160 (story-Squire) | GT |
| 0x1F Beowulf | Male | 6 Libra | 82 Summoner | GT |
| 0x1E Agrias | Female (0x40) | 3 Cancer | 80 Black Mage | GT |
| 0x32 Cloud | Male | 10 Aquarius | 88 Samurai | GT (zodiac confirms unit identity) |
| 0x80 Ninja | Male | 4 Leo | 89 Ninja | GT |
| 0x03 | Male | (Virgo) | — | non-GT, L51 (different session) |
| 0x81 | Female | (Gemini) | — | non-GT, L46 |
| 0x82 | **Monster (0x20)** | (Sagittarius) | — | reused monster id; gender bit5 confirms the Monster flag |

The monster unit (0x82) carrying gender bit5 (0x20) is the key cross-check: it confirms the classic
FFT three-flag gender encoding (Male 0x80 / Female 0x40 / Monster 0x20) holds across unit types, not
just the 5 player units.

## Reconciliation notes (image OCR vs dump)

The engine dump is authoritative. My photo OCR of low-res screens erred on: Beowulf MaxHP
(read 514,真 314), Ninja MaxHP (read 377, 真 277), Agrias MaxMP (read 252, 真 232), Beowulf Move
(read 5, 真 6), Cloud Jump (read 4, 真 3), and Speed for Agrias/Beowulf/Ninja (read 8/8/8, 真
12/9/16). The dump values are used everywhere above. Ramza is L76 in the image but L75 in all
dumps (leveled after the last capture) — his dumps are used only for level-independent attributes.

**Cloud job — RESOLVED (user-confirmed Samurai).** The +0x03=job mapping is now corroborated *twice
over* for **all 5 units** — the stored id AND the visible primary command agree: Beowulf Summon→82,
Agrias Black Magicks→80, Ninja Throw→89, Ramza Mettle→special-Squire 160, and Cloud Iaido→Samurai 88.
My earlier screenshot read had Cloud's primary/secondary swapped — his **primary is Iaido** (Samurai)
and **Limit is his secondary** (signature command), matching the re-class pattern of the others
(Beowulf Spellblade, Agrias Holy Sword as secondaries). The engine value (88) was right all along.

## Next steps
1. ~~Map job~~ — DONE: job = +0x03 (CONFIRMED, clean 5/5; Cloud's Samurai user-confirmed).
2. Expose the CONFIRMED set in the formula context (attacker.* / target.*) — the project's core goal.
3. Geometry **mostly unlocked** (X/Y/facing at +0x4F/+0x50/+0x51, zero-capture). Pending: one E–W
   move to confirm X, the Dir→compass map, and locating height/elevation.
4. Targeted captures for status bitfield / elemental affinity (remaining DEFER items).
5. **Hit/miss outcome flag** — the one true blocker for the DCL two-roll core. The vanilla hit/evade
   calc is Denuvo-virtualized (can't hook the roll), but its *staged outcome* should be overridable
   the same way damage is (pre-clamp). Needs a controlled miss+hit capture to locate the flag.
