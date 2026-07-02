# WotL Ability Action Baseline — Sourcing Notes

Companion to `work/wotl_ability_action_baseline.csv` (generated 2026-07-02).

## Source

**Primary (used for every field): FFTPatcher vanilla binary resources** —
github.com/Glain/FFTPatcher @ `ca955fce` (2026-06-04):

| File (under `PatcherLib.Resources/Resources/`) | Provided |
|---|---|
| `PSP/bin/Abilities.bin` (9,414 B) | the complete vanilla **WotL (PSP)** ability table, all 512 ids — every action field + flags in the CSV |
| `PSX-US/bin/Abilities.bin` | vanilla PSX table, used only for the `psx_action_diff` column |
| `PSP/bin/InflictStatuses.bin` (0x80 × 6 B) | decode of `inflict_status_hex` → mode + status names |
| `PSP/Abilities/Abilities.xml`, `PSX-US/Abilities/Abilities.xml` | `name_wotl`, `name_psx` |
| `AbilityFormulas.xml` | `formula_text` (FFHacktics community formula notation; same file serves both PSX and PSP contexts) |

FFTPatcher is itself the machine-readable form of the FFHacktics wiki "Ability Data"
tables, so source 1 and source 2 of the task converge here. ffhacktics.com was not
needed (and is 403-hostile to fetchers).

`name_ivc` comes from `work/ability_en.sqlite` (`Ability-en.Name`, 512 rows, keyed 0–511).

Extraction script (layout + bit semantics encoded from FFTPatcher's
`Datatypes/Abilities/Ability.cs` + `AbilityAttributes.cs`):
`work/wotl_ability_baseline_extract.py` (point `FFTP` at a FFTPatcher checkout's
`PatcherLib.Resources/Resources`); the binary layout is also documented below.

## Binary layout of `Abilities.bin` (identical PSX/PSP)

- `0x0000`: 512 × 8 B common data — JP cost (u16 LE), learn rate, byte3 = learn
  flags (hi bits) + **ability type** (lo nibble), bytes 4–6 AI flags, byte7 misc flags.
- `0x1000`: 368 × 14 B **action data** for "Normal" abilities 0x000–0x16F:
  `Range, Effect(AoE), Vertical, Flags1, Flags2, Flags3, Flags4, Element, Formula, X, Y, InflictStatus, CT(ticks), MPCost`
- `0x2420` items (1 B item id, 0x170–0x17D) · `0x2430` throw (1 B ItemSubType,
  0x17E–0x189) · `0x243C` jump (2 B H/V, 0x18A–0x195) · `0x2454` charge (2 B
  CT/power, 0x196–0x19D) · `0x2464` arithmetick (1 B, 0x19E–0x1A5) · `0x246C`
  other/R-S-M (1 B, 0x1A6–0x1FF) → these land in the CSV `type_specific` column.

Flag bits (MSB→LSB per byte; `!` = stored inverted, CSV holds the *logical* value):
- Flags1: ForceSelfTarget, Blank7, WeaponRange, VerticalFixed, VerticalTolerance, WeaponStrike, Auto, !TargetSelf
- Flags2: !HitEnemies, !HitAllies, TopDownTarget, !FollowTarget, RandomFire, LinearAttack, ThreeDirections, !HitCaster
- Flags3: Reflectable, Arithmetickable (math-skill-able), !Silenceable, !Mimicable, NormalAttack, Persevere, ShowQuote, AnimateOnMiss
- Flags4: CounterFlood, CounterMagic, Direct, Shirahadori (counter-grasp-able), RequiresSword, RequiresMateriaBlade, Evadeable, !Targeting
- Element: Fire 0x80, Lightning 0x40, Ice 0x20, Wind 0x10, Earth 0x08, Water 0x04, Holy 0x02, Dark 0x01

Community-name mapping for the task's flag list: Direct/Arc → `Direct` (set =
direct/no-arc trajectory); Stop at Obstacle → `LinearAttack`; Counter-able →
`Shirahadori` (also gates Counter/Counter Tackle-class reactions); Weapon
Strike/Weapon Range → `WeaponStrike`/`WeaponRange`; Performing →
`Persevere` (don't interrupt song/dance). AI flag bytes (`ai_flags_hex`) are kept
raw — they are AI *hints*, not action mechanics.

**InflictStatus caveat:** for formula `0x02` ("cast spell") the InflictStatus byte
is a *spell id*, not a status entry — the CSV marks these `inflict_status_mode =
CastSpell` and puts the spell name in `inflict_statuses`. Mode flags otherwise:
AllOrNothing / Random / Separate / Cancel (Cancel = the "Cancel Status" case).

## Id alignment: PSX = WotL = IVC, 1:1 (VERIFIED)

The three id spaces line up 1:1 across all 512 rows. Evidence:

1. **Structural:** IVC's `OverrideAbilityActionData` (work/override_ability.sqlite)
   has exactly 368 rows (= Normal abilities 0x000–0x16F) with exactly the WotL
   14-byte field set (`Flags12/Flags34/Range/EffectArea/Vertical/Element/Formula/X/Y/InflictStatus/CT/MPCost`).
2. **Names:** 491 of 512 IVC rows are named; 432 match the WotL name after
   whitespace-strip. All 59 remaining are localization renames at the *same id*
   (IVC 0x101 "Braver" = WotL "Brave Slash", 0x103 "Blade Burst" = "Blade Beam",
   0x104 "Ascension" = "Climhazzard", 0x07B "Defraud" = "Beg", 0x0C8 "Blood
   Drain" = "Vampire", "Throw X" prefixes, "Horizontal Jump +N" renumbering, etc.)
   — zero cases of a name appearing at a shifted id.
3. **Override cross-check (29 CT/MP overrides, all coherent):** every IVC
   CT/MP override patches an ability WotL had *slowed vs PSX*, restoring PSX-like
   speed at the same id — e.g. Protect base CT 4 → override 3; Shiva/Ramuh/Ifrit
   base CT 7 → override 4 (= exact PSX value); Meteor base CT 20 → 10; all eight
   Cloud Limits sped up. This only makes sense if IVC's hardcoded base table IS
   the WotL action table keyed by the same ids.

Spot-checked well-known abilities (id → WotL/IVC, formula, values all sane):
Cure 0x001 (f=0x0C, Y=14, rng 4, AoE 1, CT 4, MP 6) · Fire 0x010 (f=0x08, Y=14,
Fire elem, Reflectable+CounterMagic+Evadeable) · Flare 0x01F (Y=46, MP 60) ·
Tail Sweep 0x150 · Dispose 0x161 · Aim +1..+20 0x196–0x19D (charge CT
4/5/6/8/10/14/20/35) · Throw ids 0x17E–0x189 (type decode matches names).

## WotL-vs-PSX action-data differences (55 ids, flagged `psx_action_diff=1`)

- **Summon rebalance 0x03C–0x04B:** PSP raised CTs (Shiva 4→7, Bahamut 10→15,
  Zodiark 10→17) and trimmed some Y (Bahamut 46→42). Graviga/Induration/Meteor CT also changed.
- **WotL-added abilities in blank PSX slots:** Dark Knight (0x02D Sanguine Sword,
  0x0B8 Infernal Strike, 0x0DB Crushing Blow, 0x0DC Abyssal Blade, 0x165 Unholy
  Sacrifice) and multiplayer Thief kit (0x166 Barrage, 0x167–0x16E Plunder *).
- **Crush/Shellbust line 0x0A0–0x0A3:** Y 0→5/4/3/2 (WotL made them do damage).
- **Nether/holy sword tier 0x0A9–0x0B4:** X 6→10, Y raised (WotL buff).
- **Cloud Limits 0x101–0x108:** Y reduced (Cross Slash 22→12, Omnislash 40→30…), Holy Breath X 4→10.

## IVC-specific caveats

- IVC has **no** Dark Knight/multiplayer content: ids 0x0B8, 0x0DB, 0x0DC,
  0x165–0x16E are unnamed (NULL) in `ability_en.sqlite`; 0x02D is repurposed as
  "Chant" (duplicate of 0x098's name). Treat the WotL rows for those ids as
  design reference only — behavior in IVC unverified.
- 0x1E3 = "A483" and 0x1FC = "A508" are IVC placeholder names (WotL "CT 0"/"Stealth").
  0x1B8 carries an IVC "MARKED FOR DELETION" debug name.
- IVC Enhanced's intended deltas from this baseline live in
  `OverrideAbilityActionData` (sparse; -1 / `[]` = inherit base). The baseline CSV +
  that override layer = effective IVC values.

## Gaps / unresolved

- `type_specific` rows (0x170–0x1FF) have no Range/AoE/CT/MP by design — the
  engine derives Item/Throw/Jump ranges from item data or job Jump stats.
- Reaction/Support/Movement (0x1A6+) only carry `other_id` (internal effect index);
  their trigger data is code, not table data.
- `aoe=255` = "all map" (songs/dances); `vertical=255` = unlimited.
- WotL PSP slowdown-mode quirks (spell speed vs PSX timer) are engine-level, not
  in this table. IVC's Enhanced CT overrides already compensate (see above).
