# Equipment Availability And Practical-Online Timing V0

Status: Accepted as W3 equipment availability and practical-online timing framework (provisional, T1-pending)
Date: 2026-06-22
Depends on:
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/formula-balance/01-principles.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `work/gpt-equipment-availability-timing-v0.json`

## Purpose

This W3 producer defines when existing equipment families become practically online for job-balance
validation.

It turns the doc 51 equipment-unlock bands and doc 58 online-when boundaries into a family/tier
availability framework for W4/T2.1 and W5/F5. The goal is to prevent hidden power spikes or dead
zones where a job, support, or weapon family exists on paper but the relevant existing equipment is
not realistically part of the campaign texture yet.

This document does not add equipment, does not change equipment prices, does not change gil rewards,
does not change the campaign economy, does not finalize item-level records, and does not alter
weapon power or formula constants.

## Scope Policy

### No New Equipment

The mod should rebalance the existing FFT equipment ecosystem. This producer accepts no new weapon,
armor, shield, item, accessory, or thrown-equipment entry.

If a later implementation pass needs more variety inside a family, the first answer should be to
adjust existing equipment roles, timing, job access, or formula treatment. Adding an item is outside
the accepted scope.

### No Gil Or Price Edits

Existing prices, gil rewards, sell values, and shop economy are out of scope.

Equipment availability can be used as a pacing fact. Price can be mentioned only as unchanged
purchase friction from the base game. This producer must not recommend making an item cheaper, more
expensive, more farmable, or changing any gil flow to support balance.

### No Silent Value Changes

This producer makes zero equipment-value changes.

Any future change to weapon power, armor response, penetration, range, evasion, elemental flags,
status flags, routine assignment, or formula constants must be promoted through the formula bundle
or the relevant implementation data artifact, then rechecked by the affected simulations. No such
number may live only in this document.

## Atlas Consultation

The vanilla atlas was consulted for the equipment and inventory vocabulary:

- `docs/reference/README.md` as the navigation entrypoint;
- `docs/reference/fft-vanilla-ability-effect-index.md`;
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- `docs/reference/fft-vanilla-status-effect-map.md`.

Vanilla overlap checked:

| Effect family | Records checked | This producer read |
| --- | --- | --- |
| Equipment unlock supports | `Equip Heavy Armor`, `Equip Shields`, `Equip Crossbows`, `Equip Katana`, `Equip Polearms`, `Equip Guns`, `Reequip` | Retained as existing-equipment routes; broad `Equip Sword` remains rejected. |
| Inventory actions | Items, `Throw Items`, Ninja `Throw` categories | Treated as stock/inventory routes with unchanged prices and no new resources. |
| Equipment pressure | Knight rending, Thief stealing, `Safeguard`, `Reequip` | Battle-scoped equipment-state changes stay separate from permanent economy. |
| Weapon families | sword, knight sword, katana, bow, crossbow, gun, polearm, ninja blade, armor, shield | Mapped by family/tier, not individual-item rebalance. |

External WotL/PS fallback references were also consulted for shop-category and representative
availability texture:

- GameFAQs PS Shop Guide by SMaxson:
  `https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/30462`
- GameFAQs WotL Equipment List by just_call_me_ash:
  `https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/equipment-list`
- GameFAQs WotL Weapons section:
  `https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/weapons`

These external references are labeled fallback texture only. The project still needs the T1 item,
shop, stock, and reward data capture before item-level or availability-level IVC records become
authoritative.

## T1-Pending Availability Caveat

The table below is a provisional timing framework, not a verified IVC shop record.

The project does not yet have an authoritative extracted IVC item/equipment table or an
authoritative extracted IVC shop/stock/reward table. Therefore:

- family/tier timing is accepted only as a design intent for later validation;
- fallback references can explain why a family is plausible in a band, but cannot finalize the
  exact battle, location, item, price, reward, or stock path;
- W4/T2.1 may consume these bands as provisional incidence inputs;
- final implementation must replace fallback availability claims with T1-captured IVC records.

## Practically Online Definition

A native equipment identity is practically online when all of these are true:

1. the active job is plausibly available in ordinary campaign progression;
2. the job can equip the existing family natively;
3. the existing family has a provisional campaign availability route at that band;
4. inventory state can support ordinary use without changing prices, rewards, or economy.

An export equipment identity is practically online when all of these are true:

1. the donor job is plausibly available;
2. the support ability is plausibly learned and equipped;
3. the receiving job has a build reason to spend its support slot on that family;
4. the existing equipment family has a provisional campaign availability route at that band;
5. inventory state can support ordinary use without changing prices, rewards, or economy.

The fourth and fifth conditions are descriptive gates only. They do not authorize economy edits.

## Band Semantics For Existing Equipment

This document inherits doc 50 and doc 51 band semantics.

| Band | Existing-equipment read |
| --- | --- |
| 0 | Opening gear and basic items only. No weapon-family export route should be balanced around this band. |
| A | First shop texture. Starter recovery and simple weapon upgrades can matter, but no premium support package. |
| B | First specialist equipment. Native Knight, Archer, White Mage, Black Mage, Monk, and early utility identities can be supported. |
| C | Midgame branches. First serious equipment exports may become practical, but only with support-slot cost and family identity preserved. |
| D | Advanced build crafting. Strong physical engines, rare family exports, and inventory-backed burst routes compete. |
| E | Final integration. Premium and irregular equipment routes may exist, but older specialists still need rational slots. |

## Practical-Online Table

| Family or inventory route | Native identity protected | Export piece | Doc 51 target | Practical-online band | Provisional availability read | Boundary |
| --- | --- | --- | --- | --- | --- | --- |
| Heavy armor | Knight, Vanguard-style plate bodies | `Equip Armor` | C | C | Armor is an existing shop family across the campaign; this route changes armor class, not mitigation math. | No price edits. Cloth/leather fragility must matter before this export is common. |
| Shields | Knight and shield-frontline bodies | `Equip Shield` | C | C | Shields are an existing shop family with early-to-late tiers. | No price edits. T4/T6xPS must check evasion and mitigation stacks. |
| Bow/crossbow | Archer as the only true archer | `Equip Bow` | C | C | Bows and crossbows have ordinary existing availability routes; native Archer owns Band B ranged identity first. | No guns. No broad accuracy bypass. Export bow builds must not reduce Archer to a support stop. |
| Guns | Chemist/Orator gun texture, Orator gun route | `Equip Guns` | C/D | D for export, C only for native/job-specific rows if proven | Guns are existing equipment with narrow shop texture and PA-independent pressure in the current formula bundle. | No price edits. Learned `Equip Guns` stays a high-risk export until T2.1/F5 prove C safe. |
| Polearms | Dragoon spear and Jump identity | `Equip Polearms` | C/D | C/D | Polearms are existing weapons with reach and vertical-map relevance. | No price edits. Dragoon must remain the spear home before broad exports matter. |
| Katana | Samurai Brave-linked katana and Iaido identity | `Equip Katana` | D | D | Katana have existing trade-city/shop texture, but Samurai and Brave-linked build depth are the pacing gate. | No price edits. Export route must not make Samurai only a support stop. |
| Knight swords | Vanguard/final premium sword plans, Ramza chapter state where applicable | `Equip Knight Swords` | E | E | Knight swords are premium/irregular existing equipment, not a normal shop family in the fallback references. | No price edits. Optional and cuttable if sword dominance returns. |
| Ninja blades | Ninja active dual-wield identity | active Ninja innate two-hit; learned `Dual Wield` is separate | D/E | D for native, D/E for learned engine | Ninja blades are existing advanced weapons; active Ninja may use them before off-job two-hit engines dominate. | No price edits. Learned `Dual Wield` spends support and cannot stack with another support. |
| Throw inventory | Ninja ranged burst and resource-backed tactics | `Throw Mastery` | D/E | D for ordinary throw route, E for premium categories | Throw consumes existing inventory categories. | No new thrown items. No price edits. High-impact categories require W5 inventory-pressure rows. |
| Consumables | Chemist floor sustain and item specialist | `Throw Item`, `Item Lore`, `Auto-Potion` | A/B and C | 0/A for basic item actions, C for sustain exports | Basic items exist at the floor; stronger item tiers are availability facts but not economy tuning levers. | No price edits. Auto-Potion remains Potion-only 30 HP, post-damage survivor-only, no Hi/X-Potion, no Item Lore. |
| Tactical equipment swapping | Chemist utility and planned counterplay | `Reequip` | C/D | C/D | Uses existing spare inventory only. | No price edits. No free best-in-slot optimizer loop; final behavior deferred. |

## Native Before Export Rules

Equipment exports must not erase native job identity.

| Family | Native-before-export requirement |
| --- | --- |
| Bow/crossbow | Archer must have a useful Band B active ranged role before non-Archers borrow bows. |
| Polearm | Dragoon must have useful Jump/reach play before non-Dragoons build around spears. |
| Katana | Samurai must have active katana/Iaido value before `Equip Katana` is a generic route. |
| Guns | Orator and Chemist gun texture must not turn into a universal low-stat damage patch. |
| Heavy armor/shield | Knight/Vanguard-style durability must not become the default patch for every fragile build. |
| Knight swords | Premium sword access must not recreate the old late-game sword monopoly. |
| Ninja blades/two-hit | Active Ninja may be strong, but learned `Dual Wield` cannot become the only rational physical engine. |

## W4 And W5 Consumption

W4/T2.1 should treat an equipment export as present only in rows where the practical-online band has
been reached.

Required incidence rows:

1. Band C armor/shield route: fragile or evasive jobs spending support on `Equip Armor` or
   `Equip Shield`.
2. Band C bow route: non-Archer controller or physical shell spending support on `Equip Bow`.
3. Band C/D gun route: low-PA or utility jobs considering `Equip Guns`.
4. Band C/D polearm route: durable physical shells considering `Equip Polearms`.
5. Band D katana route: Brave-linked non-Samurai builds considering `Equip Katana`.
6. Band D/E dual-wield route: active Ninja versus learned `Dual Wield` off-job.
7. Band D/E Throw route: ordinary Throw inventory versus premium Throw categories.
8. Band E knight-sword route: Vanguard, final Ramza, and any off-job `Equip Knight Swords` plan.
9. Band C consumable route: `Throw Item`, `Item Lore`, and Auto-Potion with unchanged item economy.

W5/F5 should re-sim jobs whose practical equipment profile changes. That especially includes
Orator with guns, Dragoon and off-job polearms, Samurai and off-job katana, Ninja blades and learned
two-hit routes, Archer bow exports, Knight armor/shield exports, and Vanguard/Ramza premium sword
profiles.

## N1 And Bundle Boundary

Doc 57 left Vanguard and Ramza bundle promotion open. This producer does not promote them into
`work/sim-inputs-v0.2.1.json`.

When their final equipment access is promoted, the bundle must record whether the job has native
or support-gated access to swords, knight swords, shields, heavy armor, and any hybrid caster gear.
That is an N1 bundle update, not a doc-only timing decision.

## Open Follow-Up

- Capture verified IVC item/equipment records through the T1 data path.
- Replace fallback item examples with IVC-authoritative family availability when available.
- Draft the prerequisite-tree and JP-cost producer using these practical-online bands.
- Populate W4/T2.1 incidence with equipment exports only at or after their practical-online bands.
- Re-run W5/F5 for every job whose real equipment profile changes.

## Claude Review Request

Claude should review:

- whether the no-new-equipment and no-gil/price-edit constraints are strict enough;
- whether family/tier abstraction avoids pretending we have verified IVC item data;
- whether practical-online bands preserve native job identity before exports;
- whether `Equip Guns` and `Equip Knight Swords` are delayed enough to avoid universal patching and
  sword dominance;
- whether this artifact is sufficient input for the prerequisite-tree and JP-cost draft.
