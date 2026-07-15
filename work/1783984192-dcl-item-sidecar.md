# DCL item sidecar manifest

Source: `D:/Projects/FFTGenericChronicle/work/item_catalog.csv` (`261` records).
Row manifest: `work/1783984192-dcl-item-sidecar.csv`.

This is a complete structural classification, not final balance. Relative design tiers are
preserved as tiers; every missing numeric DCL field remains an explicit authoring gate.

## Routes

| Route | Items |
| --- | ---: |
| accessory | 33 |
| body-armor | 37 |
| consumable-external | 14 |
| equipped-weapon | 123 |
| headgear | 29 |
| reserved | 2 |
| shield | 16 |
| thrown-payload-external | 6 |
| unarmed-sentinel | 1 |

## Readiness

| Readiness | Items |
| --- | ---: |
| formula-map-required | 14 |
| identity-authoring-required | 62 |
| mechanism-ready-authoring-required | 1 |
| reserved | 2 |
| reverse-engineering | 6 |
| structure-ready-numeric-authoring-required | 176 |

## Equipped weapon families

| Family | SKUs | Type | Reach | Hands | Parry | Scale | Role |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| Axe | 3 | crush | 1 | 1H | low | PA | anti_plate_brute |
| Bag | 4 | crush | 1 | 1H | low | PA | job_utility_platform |
| Book | 4 | crush | 1 | 1H | low | PA | orator_utility_platform |
| Bow | 9 | missile | native-projectile | 2H | none | PA | arc_strength_ranged |
| Cloth | 3 | crush | 2 | 1H | low | PA | dancer_utility_platform |
| Crossbow | 6 | missile | native-projectile | 2H | none | weapon-skill | marksman_direct_ranged |
| Flail | 4 | crush | 1 | 1H | very-low | PA | anti_guard_crusher |
| Gun | 6 | missile | native-projectile | 2H | none | weapon-skill | armor_defeater_ranged |
| Instrument | 3 | crush | 1 | 2H | low | PA | bard_utility_platform |
| Katana | 10 | cut | 1 | 2H | high | PA | draw_out_2h_blade |
| Knife | 10 | thrust | 1 | 1H | low | PA | assassin_finisher |
| KnightSword | 5 | cut | 1 | 2H | medium | PA | brute_2h_blade |
| NinjaBlade | 8 | cut | 1 | 1H | medium | PA | light_dual_wield_blade |
| Pole | 8 | crush | 2 | 2H | very-high | PA | defensive_reach |
| Polearm | 8 | thrust | 2 | 2H | medium | PA | offensive_reach |
| Rod | 8 | magic | 3 | 1H | none | MA | offensive_magic_implement |
| Staff | 8 | magic | 3 | 1H | none | MA | support_magic_implement |
| Sword | 16 | cut | 1 | 1H | high | PA | defensive_1h_blade |

## Explicit incomplete numeric authoring

Equipment records requiring Weight: **238**.

| Numeric field | Unauthored rows |
| --- | ---: |
| Weight | 238 |
| armor divisor | 21 |
| per-SKU body DR | 37 |
| shield Block | 16 |
| weapon parry | 86 |
| weapon wmod | 107 |

These blanks are validation gates. They must be filled by explicit DCL calibration and native-data
authoring; the report intentionally does not copy vanilla WP/evasion/HP values into new semantics.

## Mechanism and design gates exposed by the catalog

- Helmet/Hat/Hair Adornment still need a head-slot HP/MP/DR identity and per-SKU policy.
- Accessories still need per-family/per-SKU roles, including resist, movement, and special-property policy.
- Throwing weapons, bombs, and consumables dispatch outside equipped-weapon Attack and require explicit formula routing.
- Rod/Staff bolts need explicit element and range authoring for every SKU; Staff heal-on-attack variants need an ability-level route.
- Unarmed uses the item-0 sentinel at runtime but its wmod/parry are job-derived, never item-derived.
