# DCL Catalog Coverage — Equipment & Ability Metadata (doc-08 §3–§4)

Date: 2026-07-02. Offline data-coverage audit for the Deep Combat Layer catalog. Every source
below was actually opened and inspected; no live-game access was used.

Verdict vocabulary:

- **EXISTS** — the field is present as-is in an extracted artifact or loader table.
- **DERIVABLE** — the field can be computed mechanically from existing fields (a mapping rule,
  not new authoring judgment).
- **MUST-AUTHOR** — the game data genuinely does not contain it; DCL metadata must define it
  (possibly seeded from FFHacktics WotL reference values).

---

## 1. Sources inspected (actual schemas)

### 1.1 `work/item_catalog.csv` (261 rows, header verified)

Join of ItemData + secondary weapon/armor/shield/accessory tables + ItemEquipBonusData:

```text
item_id, name, type_flags, item_category, required_level, additional_data_id, equip_bonus_id,
price, shop_availability, secondary_kind, weapon_range, weapon_attack_flags, weapon_formula,
weapon_power, weapon_evasion, weapon_elements, weapon_options_ability_id, armor_hp_bonus,
armor_mp_bonus, shield_physical_evasion, shield_magical_evasion, accessory_physical_evasion,
accessory_magical_evasion, bonus_pa, bonus_ma, bonus_speed, bonus_move, bonus_jump,
bonus_innate_status, bonus_immune_status, bonus_starting_status, bonus_absorb_elements,
bonus_nullify_elements, bonus_halve_elements, bonus_weak_elements, bonus_strong_elements,
bonus_boost_jp
```

Observed vocabularies (from the data itself):
- `item_category` (35): Knife NinjaBlade Sword KnightSword Katana Axe Flail Rod Staff Pole
  Polearm Crossbow Bow Gun Book Instrument Bag Cloth Throwing Bomb Shield Helmet Hat
  HairAdornment Armor Clothing Robe Shoes Armguard Armlet Cloak Ring Perfume Item None
- `weapon_attack_flags` (8): Striking Lunging Direct Arc Throwable TwoHands ForcedTwoHands TwoSwords
- `weapon_formula` distribution over 130 weapon rows: F1=114, F2=7 (magic-rod proc), F3=3 (gun),
  F4=3 (magic gun), F6=2 (HP drain: Blood Sword, Bloody Strings), F7=1 (heal weapon)
- `weapon_elements`: Fire Ice Lightning Wind Water Holy (+None)

### 1.2 `work/baseline_weapons.csv` (129 rows)

`Id, Name, Range, AttackFlags, Formula, Unused_0x03, Power, Evasion, Elements, OptionsAbilityId`
— raw ItemWeaponData dump; superset already merged into item_catalog.csv.

### 1.3 `work/baseline_weapon_families.csv` (19 families)

`family, item_categories, weapon_count, wp_min/median/max, sim_v0_2_wp, sim_minus_verified_max,
formula_ids, ranges, attack_flags, elements, top_items` — an existing hand-built family rollup
(axe, bag, bow, crossbow, gun, katana, knife, knightsword, ninjablade, pole, polearm, rod,
staff, sword, book, instrument, cloth, throwing, flail-class etc.). This is already a DCL-style
authored artifact keyed by `item_category`.

### 1.4 `work/baseline_abilities.csv` (513 rows incl. header)

`Id, Name, JP, JpCost1, JpCost2, IsRandomDamage, IsRandomStatus, ov_Range, ov_EffectArea,
ov_Vertical, ov_Element, ov_Formula, ov_X, ov_Y, ov_InflictStatus, ov_CT, ov_MPCost`
— 491 named abilities. **Override-column population: ov_CT = 28 rows, ov_MPCost = 4 rows,
ALL other ov_* columns = 0 rows.** The math columns are empty because stock data inherits the
exe-hardcoded base (see 1.6).

### 1.5 `work/baseline_jobs.csv` (175 rows)

Full JobData dump: growths/multipliers, Move/Jump/CharacterEvasion, Innate/Immune/Starting
status, Absorb/Nullify/Halve/Weak elements, EquippableItems, monster graphic fields.

### 1.6 SQLite artifacts

- `work/override_ability.sqlite` — table `OverrideAbilityActionData` (368 rows):
  `Key, unused, Flags12, Flags34, Range, EffectArea, Vertical, Element, Formula, X, Y,
  InflictStatus, CT, MPCost`. Verified: **every row has Formula = -1** (0 rows with
  Formula >= 0); Flags12/Flags34 are `[]`. Only CT (28) and MPCost (4) are overridden in stock.
  Convention: `-1` = inherit exe base, `>=0` = patch (cast to byte).
- `work/ability_en.sqlite` — table `Ability-en` (512 rows): `Key, DLCFlags, IconId, Name,
  Description, …, JpCost1, JpCost2, IsRandomDamage, IsRandomStatus` (+ unknowns). UI text + JP
  only; **no math fields**.
- `work/nxd_more.sqlite` — only `UnitHelpMessage` (572 rows). Not relevant to the catalog.

### 1.7 Mod loader editable surfaces (`C:\Reloaded-II\Mods\fftivc.utility.modloader`)

`TableData\*.xml` (full editable column set, headers read):

```text
ItemData.xml          (261) Palette SpriteID RequiredLevel TypeFlags AdditionalDataId
                            ItemCategory EquipBonusId Price ShopAvailability
ItemWeaponData.xml    (128) Range AttackFlags Formula Power Evasion Elements OptionsAbilityId
ItemArmorData.xml     (64)  HPBonus MPBonus                      <- the ONLY armor stats
ItemShieldData.xml    (16)  PhysicalEvasion MagicalEvasion
ItemAccessoryData.xml (32)  PhysicalEvasion MagicalEvasion
ItemEquipBonusData.xml(85)  PABonus MABonus SpeedBonus MoveBonus JumpBonus Innate/Immune/
                            StartingStatus Absorb/Nullify/Halve/Weak/StrongElements BoostJP
ItemOptionsData.xml   (128) OptionType(AllOrNothing/Cancel/Random/Separate) Effects
ItemConsumableData.xml      Formula Z StatusEffectId
AbilityData.xml       (512) JPCost(unused) ChanceToLearn Flags AbilityType AIBehaviorFlags
AbilityTypeData.xml   (454) ChargeEffectType AnimationId BattleTextId
AbilityThrowData.xml / AbilityJumpData.xml / AbilityChargeAimData.xml / AbilityMathData.xml
StatusEffectData.xml  (40)  Order Counter CheckFlags CancelFlags NoStackFlags
AbilityActionData.xml       STUB — explicitly says: edit via Nex OverrideAbilityActionData
```

`Nex\Layouts\ffto\` relevant layouts: `Ability.layout` (text/JP/voice only),
`OverrideAbilityActionData.layout` (the patch columns above; PC handler @
`FFT_enhanced.exe+eea6e50`), `Item.layout` (text/UI only).

`AbilityData.xml` vocabularies verified from the file:
- `AbilityType` (counts): Normal 367, Reaction 32, Support 32, Movement 24, Item 14,
  Throwing 12, Jumping 12, Aim 8, Math 8, None 3.
- `AIBehaviorFlags` (27): TargetEnemies TargetAllies TargetMap OnlyHitsEnemies
  OnlyHitsAlliesOrSelf HP MP Stats Silence AddStatus CancelStatus Unequip UndeadReverse
  PhysicalAttack MagicalAttack Melee3Directions Ranged3Directions LinearAttack NonSpearAttack
  StopAtObstacle Evadeable EvadeWithMotion Reflectable RandomHits CheckCT_Target
  AffectedByFaith UsableByAI.

### 1.8 Runtime `ItemCatalog.cs` (codemod)

`codemod/fftivc.generic.chronicle.codemod/ItemCatalog.cs` parses item_catalog.csv but
`ItemCatalogEntry` currently drops: `weapon_range`, `weapon_attack_flags`, `weapon_elements`,
`weapon_options_ability_id`, all `bonus_*_status`, all `bonus_*_elements`, `bonus_boost_jp`.
It exposes id/level/power/formula/evasions/HP-MP bonus/PA-MA-Speed-Move-Jump bonus plus
`category_*` / `type_*` / `is*` indicator variables.

---

## 2. Coverage table — doc-08 §3 Equipment and Item Metadata

| Doc-08 requirement | Source (file + column) | Verdict | Notes |
| --- | --- | --- | --- |
| Equipment slots (live ids) | runtime unit struct (doc 04) | EXISTS (proven) | Out of scope here; catalog key = global `item_id`. |
| Item id → catalog row | `item_catalog.csv.item_id` | EXISTS | 261 rows = full table (cap 261). |
| Weapon family | `item_catalog.csv.item_category` (+ `baseline_weapon_families.csv.family`) | EXISTS / DERIVABLE | 20 weapon categories in data; DCL's 11-family model (sword/knife/spear/bow/crossbow/gun/staff/rod/fist/monster/special) is a category→family MAPPING to author once (e.g. Polearm→spear, Katana+KnightSword→sword-family grade, Book/Instrument/Bag/Cloth/Throwing/Bomb→special; fist = unarmed, no item row; monster = no item, jobs 94+). |
| Damage type (cut/thrust/crush/missile/magic) | — none — | MUST-AUTHOR | Confirmed absent from every item surface. Seedable per family (knife→thrust, sword→cut, flail/axe→crush, bow/crossbow/gun→missile, rod/staff→crush or magic), with per-item overrides (e.g. estoc-like). |
| Weapon modifier (DCL damage add) | `weapon_power` as seed | MUST-AUTHOR | WP exists (0–40) but DCL's family/item modifier is a new balance number; `baseline_weapon_families.csv` already computes WP stats + sim columns for this purpose. |
| Reach / range | `item_catalog.csv.weapon_range` / `ItemWeaponData.Range` | EXISTS | 1/2 melee reach, 3–8 ranged. `Arc` vs `Direct` in `weapon_attack_flags` distinguishes trajectory (bow vs crossbow/gun). |
| Parry value per weapon | `weapon_evasion` (W-EV) | EXISTS (seed) | Native W-EV per weapon. DCL parry VALUE can derive from it; parry DEPLETION is runtime state, not catalog. |
| Shield block value | `shield_physical_evasion`, `shield_magical_evasion` | EXISTS (seed) | Native S-EV. DCL block capacity = authored transform of these. |
| Armor class per body/head piece | — none — (`ItemArmorData` has ONLY `HPBonus`/`MPBonus`) | MUST-AUTHOR | No AC/DR concept anywhere in data. DCL must author class (cloth/leather/chain/plate/robe…) + DR per damage type per item or per armor tier. HPBonus is a usable tiering seed. |
| Weight | — none — | MUST-AUTHOR | Confirmed: no weight column in ItemData, secondaries, equip bonus, or CSVs. IVC = classic FFT, weightless. Fully DCL-authored. |
| Elements & affinities per item | `weapon_elements` (attack element) + `bonus_absorb/nullify/halve/weak/strong_elements` (equip affinities, via EquipBonusId) | EXISTS | Full 8-element vocab. Note `StrongElements` (boost) exists on items but NOT on jobs. NOT yet parsed by ItemCatalogEntry → wiring gap, not data gap. |
| Status immunities / innate statuses | `bonus_innate_status`, `bonus_immune_status`, `bonus_starting_status` | EXISTS | 40-status vocab. Same ItemCatalogEntry wiring gap. |
| Special behavior: drain | `weapon_formula` = 6 (HP drain), 7 (heal) | DERIVABLE | Blood Sword, Bloody Strings identified in data. |
| Special behavior: thrown | `weapon_attack_flags` contains `Throwable` (+ `AbilityThrowData.ItemType`) | EXISTS | |
| Special behavior: magic rod formula | `weapon_formula` = 2 + `weapon_options_ability_id` = spell id | EXISTS | e.g. Flame Rod → ability 16. Formula 3/4 = gun/magic-gun. |
| Special behavior: weapon proc / inflict | `weapon_options_ability_id` → `ItemOptionsData` (OptionType + Effects) when formula ≠ 2 | EXISTS | Cancel/AllOrNothing/Random/Separate status effects. |
| Special behavior: two-hands | flags `TwoHands` (allowed) / `ForcedTwoHands` | EXISTS | |
| Special behavior: dual-wieldable | flag `TwoSwords` | EXISTS | |
| Special behavior: melee style | flags `Striking` / `Lunging` (spear thrust) | EXISTS | Useful damage-type seed (Lunging→thrust). |

## 3. Coverage table — doc-08 §4 Ability / Spell / Effect Metadata

| Doc-08 requirement | Source | Verdict | Notes |
| --- | --- | --- | --- |
| Ability id + name | `ability_en.sqlite Ability-en.{Key,Name}` / `baseline_abilities.csv` | EXISTS | 512 ids, 491 named. Stable key. |
| Native Formula / X / Y | base = exe-hardcoded; `OverrideAbilityActionData.{Formula,X,Y}` is WRITE-ONLY patch (all -1 stock, verified) | MUST-AUTHOR (baseline) | The single biggest gap. Two fill paths: (a) seed from FFHacktics WotL Ability_Data (same ruleset; some IVC rebalance), (b) RE-extract the base table from `FFT_enhanced.exe` (most accurate; handler @ +0xeea6e50 gives the base-table locus). DCL needs its own copy either way. |
| DCL action kind | `AbilityData.xml.AbilityType` (Normal/Item/Throwing/Jumping/Math/Aim/Reaction/Support/Movement) + `AIBehaviorFlags` (HP/MP/Stats, PhysicalAttack/MagicalAttack, AddStatus/CancelStatus) | DERIVABLE (coarse) → MUST-AUTHOR (final) | Data classifies enough for a first-pass kind; DCL's physical/magic/heal/status split per ability should still be authored (heals vs damage both flag `HP`). |
| Spell power / heal power | native Y (see Formula row) | MUST-AUTHOR | Native Y is a seed once the baseline exists; DCL power is a new balance value regardless. |
| Element | base hardcoded; `ov_Element` writable | MUST-AUTHOR (baseline) | Not readable from data; FFHacktics element per ability is reliable (Fire/Ice/etc. never rebalanced). |
| Damage type per action | — none — | MUST-AUTHOR | Same authored axis as weapons. |
| Status list / InflictStatus | base hardcoded; `ov_InflictStatus` writable; status VOCAB exists (`StatusEffectData.xml`, 40 slots) | MUST-AUTHOR (baseline) | Which statuses an ability inflicts is not readable; the status table itself + interactions IS data. |
| Status category (mental/physical/…) | — none — | MUST-AUTHOR | Pure DCL concept. |
| MP cost | `ov_MPCost` (4 stock rows only); base hardcoded | MUST-AUTHOR (baseline) | Seed from FFHacktics + the 4 IVC overrides. |
| CT / charge time | `ov_CT` (28 stock rows); base hardcoded | MUST-AUTHOR (baseline) | The 28 IVC CT rebalances are IN data and must win over FFHacktics values. |
| Range / AoE / Vertical | base hardcoded; `ov_Range/ov_EffectArea/ov_Vertical` writable (0 stock rows) | MUST-AUTHOR (baseline) | Same seeding paths as Formula/X/Y. |
| Targeting flags: ally/enemy/self/map | `AIBehaviorFlags`: TargetAllies/TargetEnemies/TargetMap/OnlyHitsEnemies/OnlyHitsAlliesOrSelf | EXISTS | AI-facing but mirrors action semantics. |
| Targeting flags: linear/direct/arc/3-dir | `AIBehaviorFlags`: LinearAttack/StopAtObstacle/Melee3Directions/Ranged3Directions/NonSpearAttack | EXISTS (partial) | Weapon arc/direct lives on the weapon (`Arc`/`Direct` flags). |
| Reflectable | `AIBehaviorFlags: Reflectable` | EXISTS | |
| Faith-scaled | `AIBehaviorFlags: AffectedByFaith` | EXISTS | |
| Evadeable | `AIBehaviorFlags: Evadeable` (+ EvadeWithMotion) | EXISTS | |
| Math-skill-able (Arithmeticks) | base AbilityFlags1–4 hardcoded (patchable via `Flags12/Flags34`, not readable) | MUST-AUTHOR (baseline) | Not in AIBehaviorFlags; seed from FFHacktics flag bits. `AbilityMathData.xml` covers only the 8 Arithmeticks selector abilities. |
| Random damage/status | `Ability-en.IsRandomDamage/IsRandomStatus` + `AIBehaviorFlags: RandomHits` | EXISTS | |
| Learn-on-hit / JP / learn chance | `AbilityData.xml` + JP decode (JpCost1+256*JpCost2) | EXISTS | |
| Secondary behavior (Throw/Jump/Aim/Math params) | `AbilityThrowData/AbilityJumpData/AbilityChargeAimData/AbilityMathData` | EXISTS | |
| Side effects (knockback, drain, revive…) | formula id implies (0x06 drain, 0x0D revive, 0x37 knockback…) | DERIVABLE (after baseline) | Formula-id → side-effect map is doc-02 knowledge; needs the per-ability formula baseline first. |

---

## 4. DCL catalog build plan

### 4.1 Two-file model

1. **Extracted layer (regenerable, never hand-edited)** — `work/item_catalog.csv` (done) plus a
   new `work/ability_catalog.csv` joining `Ability-en` (id/name/JP/random flags) +
   `AbilityData.xml` (AbilityType, Flags, AIBehaviorFlags) + `OverrideAbilityActionData`
   (the 28 CT + 4 MP stock overrides) + Throw/Jump/Aim/Math secondaries. Everything in it is
   EXISTS-grade.
2. **Authored DCL metadata (hand-owned JSON, shipped with the mod)** —
   `dcl/items.dcl.json` + `dcl/abilities.dcl.json`, holding every MUST-AUTHOR field, seeded
   mechanically then curated. Key by `item_id` / `ability_id`; support family-level defaults
   with per-id overrides so ~19 family entries cover ~130 weapons.

### 4.2 Seeding order for the ability baseline (the big gap)

1. Scrape FFHacktics WotL ability data (Formula/X/Y/Element/InflictStatus/Range/AoE/Vertical/
   CT/MP/flag bits) into `work/ffh_ability_baseline.csv`.
2. Apply the 32 stock IVC overrides (28 CT, 4 MP) on top — IVC values win.
3. Mark every row `confidence: seeded`; promote to `verified` via targeted live checks or a
   later exe base-table extraction (RE task; anchor: override handler `FFT_enhanced.exe+eea6e50`).

### 4.3 Proposed JSON schema sketch

```jsonc
// dcl/items.dcl.json
{
  "version": 1,
  "familyDefaults": {
    "sword":  { "family": "sword",  "damageType": "cut",    "reach": 1, "weight": 3.0,
                 "parryBase": "wEv", "modifier": 0 },
    "polearm":{ "family": "spear",  "damageType": "thrust", "reach": 2, "weight": 4.0 },
    "bow":    { "family": "bow",    "damageType": "missile","pointBlankPenalty": true },
    "rod":    { "family": "rod",    "damageType": "magic" }
    // ... one per item_category, mapped onto the 11 DCL families
  },
  "armorClassDefaults": {
    "Clothing": { "class": "cloth",  "dr": { "cut": 1, "thrust": 0, "crush": 1, "missile": 0, "magic": 0 } },
    "Armor":    { "class": "plate",  "dr": { "cut": 4, "thrust": 3, "crush": 2, "missile": 3, "magic": 0 } },
    "Robe":     { "class": "robe",   "dr": { "magic": 2 } }
    // Helmet/Hat/HairAdornment for head slots
  },
  "items": {                          // per-item overrides only; key = global item_id
    "23": { "note": "Blood Sword", "special": ["drain"] },       // drain also derivable (formula 6)
    "40": { "damageType": "thrust" }
  }
}

// dcl/abilities.dcl.json
{
  "version": 1,
  "abilities": {
    "16": {                            // key = ability id (matches ability_en / override table)
      "name": "Fire",                  // denormalized for debugging only
      "kind": "magic_damage",          // physical|magic_damage|heal|status|reaction|item|special
      "power": 14,                     // DCL spell/heal power (seed: native Y)
      "element": "fire",
      "damageType": "magic",
      "statusInflict": [],             // seed: native InflictStatus
      "statusCategory": null,          // mental|physical|magical|taunt|special
      "mp": 6, "ct": 4,
      "range": 5, "aoe": 1, "vertical": 2,
      "targeting": { "allies": false, "enemies": true, "self": true, "map": true,
                      "linear": false, "reflectable": true, "arithmeticks": true },
      "avoidance": "magic_evade",      // physical|magic_evade|none|guaranteed|special
      "faithScaled": true,
      "native": { "formula": 8, "x": 0, "y": 14 },   // baseline for neuter/placeholder logic
      "sideEffects": [],
      "confidence": "seeded"           // seeded | verified
    }
  }
}
```

### 4.4 Runtime wiring backlog (code, not data)

- Extend `ItemCatalogEntry` to parse + expose the columns it currently drops (range,
  attack flags, elements, options ability, statuses, affinities) — data already in the CSV.
- Add `AbilityCatalog` (mirror of `ItemCatalog`) loading `ability_catalog.csv` +
  `abilities.dcl.json`, exposing `action.*` formula variables.
- Family/AC defaults resolver: item → per-id override → family default → hard default.

---

## 5. Explicit gap list (data genuinely cannot provide)

1. **Per-ability base action math** — Formula, X, Y, Element, InflictStatus, Range, EffectArea,
   Vertical, base CT, base MP, AbilityFlags1–4 (incl. Arithmeticks-usable bit). Hardcoded in
   `FFT_enhanced.exe`; the Nex override layer is write-only patching (verified all -1 in stock
   except 28 CT / 4 MP). Must be authored or RE-extracted.
2. **Damage type** (cut/thrust/crush/missile/magic) for weapons AND abilities — no such axis
   exists anywhere in FFT data. Fully DCL-authored (family-seedable).
3. **Weight** — no field on any item surface. Fully DCL-authored.
4. **Armor class / DR matrix** — armor pieces carry only HPBonus/MPBonus. Fully DCL-authored
   (category + HP tier are usable seeds).
5. **DCL power / weapon modifier values** — new balance numbers by definition (WP and native Y
   are seeds only).
6. **Status categories** (mental/physical/magical/taunt/fear) — pure DCL concept.
7. **Hit/avoidance policy per ability** — partially derivable (Evadeable/MagicalAttack flags),
   but the DCL policy enum needs authoring.
8. **Monster "weapons"** — monsters have no item rows; fist/monster families need synthetic
   catalog entries keyed by job id (baseline_jobs.csv provides the monster job list).
9. **Defense depletion / block capacity semantics** — runtime state, deliberately NOT catalog
   data (values seed from W-EV / S-EV).
