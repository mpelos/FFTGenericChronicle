# Character attributes — ground truth (from in-game screens + user text)

Source: 10 status/More screen photos (2026-06-24 ~22:58) + user-provided zodiac & brave/faith.
Status screens: IMG_9911 Cloud, 9912 Agrias, 9913 Beowulf, 9914 Ramza, 9915 Ninja.
More screens:   IMG_9916 Cloud, 9917 Agrias, 9918 Beowulf, 9919 Ramza, 9920 Ninja.
Purpose: authoritative per-unit ground truth to map/validate battle-unit struct offsets.

Confidence per field: ✓ read clearly · ~ probable (low-res) · ? missing.

> Level note: 4/5 image levels match the dumps (Cloud 67, Agrias 69, Beowulf 68, Ninja 71).
> **Ramza image = L76 / EXP 0 (just leveled); all Ramza dumps are L75.** For mapping, select
> Ramza's L75 dump and treat his level-sensitive stats (esp. MaxHP) as possibly off by the
> level-up delta. Personality/identity attrs (Brave/Faith/zodiac/gender/job) are unaffected.

> Jobs are re-classed caster jobs (user confirms no job change since 06-24, so the L67-71
> dumps carry these jobs). Signature command (Holy Sword / Spellblade) sits in the secondary
> skillset slot. Support ability may have varied between captures (user note) → lower trust.

Zodiac ids use the classic FFT order Aries0 Taurus1 Gemini2 Cancer3 Leo4 Virgo5 Libra6
Scorpio7 Sagittarius8 Capricorn9 Aquarius10 Pisces11 — **CONFIRMED** against the struct (+0x09 hi-nibble).
Gender — **CONFIRMED** as classic bit flags at +0x06 (Male 0x80 / Female 0x40 / Monster 0x20).
Job ids — **CONFIRMED** at +0x03 (byte); ids from `work/baseline_jobs.csv`.

> **RECONCILED:** several numeric values in the raw-screen table below were low-res OCR errors
> (MaxHP/MaxMP/Speed/Move/Jump for some units). The corrected, dump-authoritative values live in
> `work/gt-master.json` and `work/battle-unit-struct-attribute-map.md`. This file preserves the raw
> reads for traceability; trust the map doc for final numbers.

---

## Master table

| unit | id | Lvl | MaxHP | MaxMP | Move | Jump | Speed | PA | MA | Brave | Faith | Zodiac | Gender | Job |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Ramza | 0x01 | 76*/75dump | 569 | 86~ | 6 | 3 | 10~ | 20 | 9 | 97 | 70 | Virgo (5) | M (0) | Squire — job 160 (special story) |
| Beowulf | 0x1F | 68 | 514 | 180 | 5 | 3 | 8 | 5 | 17 | 97 | 65 | Libra (6) | M (0) | Summoner — job 82 ✓ |
| Agrias | 0x1E | 69 | 322 | 252 | 5 | 3 | 8 | 11 | 23 | 97 | 63 | Cancer (3) | F (1) | Black Mage — job 80 ✓ |
| Cloud | 0x32 | 67 | 428 | 89 | 4 | 4 | 9 | 14 | 13 | 97 | 65 | Aquarius (10) | M (0) | Soldier? — job **88 = Samurai** in dump ⚠ |
| Ninja/Rion | 0x80 | 71 | 377 | 41 | 6 | 4 | 8 | 15 | 7 | 97 | 72 | Leo (4) | M (0) | Ninja — job 89 ✓ |

\* Ramza image L76 but dumps L75. Brave/Faith from user text (overrides my low-res reads).
Ninja gender **DISCOVERED = Male (0x80)** via the +0x06 bit (≠ Agrias's Female 0x40).

**Job-id confirmation:** +0x03 holds the job id. Four of five units corroborate it *twice over* — both
the id and the visible primary command agree: Beowulf Summon→Summoner(82), Agrias Black Magicks→Black
Mage(80), Ninja Throw→Ninja(89), Ramza Mettle→Squire(special 160). **Cloud is the one open question:**
I read his primary command as "Limit" (the Soldier command, job 50), but his dump byte is 88 = Samurai.
Either I misread the screen or Cloud was re-classed to Samurai. The *mapping* (job@+0x03) is not in doubt
— only Cloud's class label. **TODO(user): confirm Cloud's displayed job — Soldier or Samurai?**

## Per-unit detail (incl. evasion / weapon panel from More screens)

### Ramza 0x01
EXP 0 · HP 569/569 · MP 85/86 · CT 20 · PhysEva 0% · MagEva 0% · WeaponAtk(R) 40 (Chaos Blade) ·
Shield Phys Parry 50% · Shield Magick Parry 25% (Venetian Shield).
Equip: Chaos Blade · Venetian Shield · Grand Helm · Maximillian · Bracers.
Abilities: Mettle (primary) · Movement +5. Others not clearly shown.

### Beowulf 0x1F
EXP 19 · HP 514/514 · MP 180/180 · CT 100 · WeaponAtk(R) 14 (Runeblade).
Equip: Runeblade · (empty) · Lambent Hat · Luminous Robe · Magepower Gloves.
Abilities: Summon (primary) · Spellblade (secondary) · Equip Swords (support) · Movement +5.

### Agrias 0x1E
EXP 98 · HP 0/322 (current 0, test/KO) · MP 252/252 · CT 84 · PhysEva 5% · WeaponAtk(R) 14 ·
WeaponParry(R) 15%~.
Equip: Runeblade · (empty) · Lambent Hat · Lordly Robe · Genji Gloves.
Abilities: Black Magicks (primary) · Holy Sword (secondary) · Equip Swords (support) · Movement +2.

### Cloud 0x32
EXP 98 · HP 428/428 · MP 89/89 · CT 100 · PhysEva 20% · MagEva 0% · WeaponAtk(R) 16 (Materia Blade+).
Equip: Materia Blade+ · (empty, two-handed) · Genji Helm · Genji Armor · Red Shoes.
Abilities: Limit (primary) · Iaido (secondary) · Mana Shield (reaction) · Equip Swords (support) ·
Manafont (movement).

### Ninja "Rion" 0x80
EXP 77 · HP 61/377 (current 61) · MP 41/41 · PhysEva 30% · WeaponAtk(R) 15 / (L) 15 (Iga/Koga) ·
WeaponParry(R) 10% / (L) 5%.
Equip: Iga Blade · Koga Blade · Thief's Cap · Ninja Gear · Chantage.
Abilities: Throw (primary) · Items (secondary) · Gil Snapper · Throw Items · Movement +3.
(slot classification uncertain; Support may have varied between captures.)

## Physical-Evasion % (candidate C-Ev stat — test against struct)
Cloud 20 · Agrias 5 · Ramza 0 · Ninja 30 · Beowulf ? (not read). Likely job+equip derived; test anyway.

## Still derived/not-in-image (DEFER)
- Per-element affinity, status immunities, facing/height/position: not on these screens.
- Right-panel weapon/shield/cloak parry %s are equipment-derived (catalog), not unit-struct stats.
