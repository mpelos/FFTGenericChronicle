# Morning report — autonomous /loop, 2026-06-25

Bom dia! While you slept I finished the attribute-mapping loop. Everything below is offline work,
reconciled to the **level-matched** dumps (engine memory = authoritative). 3 milestones committed.

## TL;DR
- The battle-unit struct is now **comprehensively mapped** — ~40 attributes CONFIRMED, including the
  three things that were still open: **job id**, the **job growth/multiplier block**, and the
  **raw/base stats**. The MEDIUM tier is now empty (everything got promoted or stayed CONFIRMED).
- **One thing needs 5 seconds of your input:** Cloud's job. The engine says **Samurai (88)**; I had
  read his screen as "Soldier". The mapping is not in doubt — just the label. See "Needs you" below.
- The rest of what's unmapped genuinely needs **new, targeted captures** (status/element/position/
  ability-varied units) — flagged clearly for when you want to chase them.

## Committed this loop
1. `aa2f4c0` — **Job id = +0x03** (byte). 5/5; corroborated twice for 4 units by both the id and the
   visible primary command (Black Mage 80, Summoner 82, Ninja 89, Ramza special-Squire 160).
2. `7f938aa` — **Job growth/multiplier block = +0x8A..0x93** (10 fields). The struct caches the unit's
   whole job stat-scaling row from `baseline_jobs.csv`; all 10 validate 5/5. Also identified
   +0x14C = unit name (Ninja spells "Rion"), and re-confirmed +0x4B = physical/character evasion.
3. `555b414` — **Raw/base PA/MA/Speed = +0x38/39/3A** promoted to CONFIRMED via an equipment-bonus
   closed loop: raw + Σ(gear bonuses from item_catalog) == effective, 15/15 exact.

## What's CONFIRMED now (safe to expose in formulas, attacker & target)
Identity: charId(+0x00), **job(+0x03)**, gender(+0x06), zodiac(+0x09), name(+0x14C).
Vitals: Level(+0x29), EXP(+0x28), HP/MaxHP(+0x30/32), MP/MaxMP(+0x34/36).
Personality: Brave/MaxBrave(+0x2B/2A), Faith/MaxFaith(+0x2D/2C).
Combat: **rawPA/MA/Spd(+0x38/39/3A)**, effPA/MA/Spd(+0x3E/3F/40), CT(+0x41), Move(+0x42), Jump(+0x43).
Equipment ids: head/body/acc/weapons/shields (+0x1A..0x26).
Equip-derived: weapon atk R/L(+0x44/45), weapon parry R/L(+0x46/47), shield parry phys/mag(+0x4A/4E),
phys/char evasion(+0x4B).
Job scaling: HP/MP/Spd/PA/MA Growth+Mult (+0x8A..0x93).
Status: KO bit (+0x61 bit 0x20) — re-confirmed by Agrias toggling 0->32 when KO'd in a capture.

Full map: `work/battle-unit-struct-attribute-map.md`. Canonical struct: `docs/modding/05-battle-data-map.md`.
Master fixture (all 33 known offsets PASS): `work/gt-master.json`. Complete 0x00..0x180 offset
inventory: `work/struct-profile-full.txt`. Raw-stat proof: `work/raw-stats-equipment-proof-2026-06-25.md`.

## Needs you (1 quick confirm)
- **Cloud's job label.** Dump byte +0x03 = 88 = Samurai (per baseline_jobs.csv). I had labelled him
  "Soldier" (job 50, command "Limit") from the screenshot. Either I misread (his primary may actually
  be Iaido = Samurai) or he was re-classed before these captures. Next time you're in-game, glance at
  Cloud's class on the status screen and tell me Samurai or Soldier. (Doesn't change any offset — the
  other four units anchor +0x03=job.)

## Flagged for later — needs NEW targeted captures (cannot do from these dumps)
These can't reach confidence from full-HP / no-status / standing-still captures:
- **Status bitfield (full):** only the KO bit is known; all 5 units were alive/clean. → capture units
  under Poison/Haste/Protect/etc.
- **Elemental affinity** (weak/half/absorb/null per element): not located; likely derived at calc time
  from job+equip. → capture units with element-affecting gear.
- **Geometry** (position X/Y, height, facing) — needed for directional damage (back-attack +50%). Not
  in the static stat block. → positional captures.
- **Secondary / Reaction / Support / Movement ability ids:** somewhere in ~+0x52..0x89 (dense bytes).
  → need a unit whose abilities you can vary and re-dump.
- **Magic evasion / accessory evasion:** 0 for all 5 here. → units that actually have them.
- Low-confidence leftover bytes with no ground truth: +0x01, +0x02, +0x08, +0x13, +0x3C, +0x4F/0x50.

## Strategic note for the rewrite
The struct hands us, per unit, in one place: base stats, effective stats, the exact equipment
contribution (effective − base), the full job growth/multiplier curve, brave/faith, and all the
equip-derived combat percentages. That means a fully custom formula depending on **both** attacker
and target — attributes *and* equipment — is supported by the data we can already read. The natural
next phase (separate from this mapping loop) is wiring these offsets into the formula context
(attacker.* / target.*) on the C# side; say the word and I'll scope that.
