# Squire And Chemist Concrete Provisional V0

Status: Accepted as W3 Squire/Chemist concrete-provisional action values
Date: 2026-06-22
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/05-squire-chemist-v1-proposal.md`
- `docs/job-balance/16-healing-timing-composition-schema.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/47-campaign-validation-readiness-v0.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/reference/README.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-squire-chemist-concrete-v0.json`

## Purpose

This is the first W3 physical/foundation concrete action-value producer.

It covers Squire and Chemist because they define the Band 0/A campaign floor: a player starting a
fresh game with Ramza plus four generics should be able to act, recover from small mistakes, and see
real build hooks without getting an early combat engine.

This document sets provisional action values and boundary values needed for validation. It does not
finalize the full prerequisite tree, JP economy, shop timing, RSM incidence, or real-roster F5
verdict.

## Atlas Consultation

The vanilla reference atlas was consulted before assigning values:

- Squire/Fundaments and Chemist/Items records in
  `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- action, reaction, support, movement, item, status, and equipment-unlock tags in
  `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- Poison, Blind, Silence, Oil, and revive/status cleanup vocabulary in
  `docs/reference/fft-vanilla-status-effect-map.md`;
- `docs/reference/README.md` as the navigation layer, not as a replacement for source tables.

This pass preserves the FFT read of both jobs: Squire is the starter physical utility job, and
Chemist is the reliable item specialist. It changes concrete magnitudes because the new formula
ecology needs a smoother floor and stricter sustain boundaries.

## Scope Boundary

Included here:

- Squire action values;
- Chemist item action values;
- Chemist gun and `Quick Draw` warning rows;
- Auto-Potion boundary values for the later RSM producer.

Deferred:

- final `JP Boost`, `Move +1`, `Throw Item`, `Item Lore`, `Safeguard`, `Reequip`, and
  `Move-Find Item` costs/incidence;
- exact job prerequisites and JP gain pacing;
- exact item shop tiers, prices, and stock pressure;
- full W4 populated T2.1 incidence and W5 real-roster dominance verdict.

## Shared Formula Contracts

Physical rows use the current v0.2.1 family model from `work/sim-inputs-v0.2.1.json`.

Healing rows use the accepted T3/T3xT5 assumptions:

```text
effective_heal = min(raw_heal, missing_hp)
same-tick delayed healing is unsafe
Auto-Potion-like reaction healing is post-damage and survivor-only
```

This is important for Chemist. Immediate items are allowed to be reliable, but they spend stock,
gil, action economy, range, and usually single-target focus. They should beat delayed magic only
when timing makes reliability matter.

## Squire Values

Squire should remain useful without becoming a permanent default.

| Skill | Value | MP | CT | JP | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| `Throw Stone` | fixed 12 crush chip | 0 | 0 | 50 | P0/T4 | Short ranged finish/turn/position tool. No scaling damage plan. |
| `Dash` | fixed 18 crush body check | 0 | 0 | 70 | P0/T6xT7 | Adjacent only; optional small shove or facing pressure. |
| `First Aid` | 20 HP | 0 | 0 | 80 | T3/T3xT5 | Adjacent only; no revive; weaker than Potion. |
| `Focus` | next basic physical only: x1.10 pressure and +0.10 hit reliability | 0 | 0 | 120 | T4/T10 | Non-stacking; expires after one attack or one round. |
| `Rally` | adjacent ally gains 8 CT | 0 | 0 | 180 | T10/T2.1 | No self-target; once per target per round; not Haste. |
| `Weapon Drill` | current weapon strike at x0.70 pressure plus narrow family rider | 0 | 0 | 250 | F5/T4/T6xT7 | Uses equipped weapon only; not a free modal toolbox. |

`Weapon Drill` riders:

| Current weapon type | Rider |
| --- | --- |
| swing | +0.10 hit rate on this strike only |
| thrust | +0.05 penetration on this strike only |
| crush | minor shove or guard-pressure rider on this strike only |

If `Weapon Drill` becomes broadly correct as a secondary, it should be cut or deferred. The value
above deliberately keeps its damage below ordinary attacks.

### Squire Damage Checks

Early Squire ordinary attack rows from `work/gpt-squire-chemist-concrete-v0.json`:

| Family | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| sword | 20 | 24 | 30 | 32 |
| knife | 20 | 33 | 28 | 30 |
| axe expected | 28 | 23 | 25 | 25 |
| flail expected | 34 | 28 | 30 | 30 |
| fists | 9 | 7 | 8 | 8 |

Squire utility rows:

| Action | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| `Throw Stone` fixed 12 crush | 13 | 11 | 12 | 12 |
| `Dash` fixed 18 crush | 20 | 17 | 18 | 18 |
| `Weapon Drill` sword x0.70 | 14 | 16 | 21 | 22 |
| `Weapon Drill` axe x0.70 expected | 20 | 16 | 17 | 17 |
| `Weapon Drill` flail x0.70 expected | 24 | 19 | 21 | 21 |
| `Weapon Drill` knife x0.70 | 14 | 23 | 20 | 21 |

Read:

- `Throw Stone` is safely below basic weapon pressure;
- `Dash` reaches ordinary sword-into-plate value only because plate is weak to crush, but it is
  adjacent and fixed;
- `Weapon Drill` is not a better damage button than attacking;
- starter fists remain a weak fallback, preserving Monk's protected fist identity later.

## Chemist Values

Chemist should be item-first in Band 0/A. Gun identity is real, but first-gun access must be a shop
and equipment-timing question, not a raw-start assumption.

| Skill / item action | Value | MP | CT | JP | Band | Gate binding | Notes |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- |
| `Potion` | 30 HP | 0 | 0 | 30 | 0/A | T3/T3xT5 | Basic reliable item heal. |
| `Phoenix Down` | revive at 20 HP | 0 | 0 | 90 | 0/A | T3xT5 | Low-HP emergency revive. |
| basic condition items | clear Poison, Blind, or Silence | 0 | 0 | 80 | A | T4 | Reactive cure only. |
| `Field Salve` | 15 HP and clear Poison/Oil | 0 | 0 | 120 | A/B | T3/T4 | Range 1 or adjacent; worse than Potion for pure HP. |
| `Ether` | 20 MP | 0 | 0 | 150 | B | T9 | Scarce item economy; single target. |
| `Hi-Potion` | 70 HP | 0 | 0 | 200 | B/C | T3/T9 | Shop-tier gated; not Band A floor. |
| `Hi-Ether` | 50 MP | 0 | 0 | 450 | C/D | T9/T2.1 | Deep caster-support route. |
| `X-Potion` | 150 HP | 0 | 0 | 500 | D/E | T3/T9/T2.1 | Late stock and gil; not Auto-Potion eligible. |
| `Quick Draw` | gun attack at x0.70 pressure | 0 | 0 | 260 | B+ | F5/T4 | Requires gun; no gun damage steroid. |
| `Smoke Bomb` | deferred | - | - | - | - | T4/T8 | Do not accept until accuracy/targeting proof exists. |

### Gun Timing Warning

The v0.2.1 gun routine is `wp_wp` with high armor penetration. That identity is healthy later, but
dangerous if raw-start Chemist has a gun.

| Row | Plate | Mail | Leather | Cloth |
| --- | ---: | ---: | ---: | ---: |
| early normal gun | 36 | 39 | 37 | 38 |
| early `Quick Draw` x0.70 | 25 | 27 | 26 | 26 |
| mid normal gun | 81 | 89 | 85 | 86 |
| mid `Quick Draw` x0.70 | 57 | 62 | 59 | 60 |

Early normal gun damage is too high for Band 0/A because it beats or matches starter Squire weapon
texture while Chemist also owns item reliability. Therefore:

- Band 0/A Chemist is balanced around knife/fists plus Items;
- first real gun access should be Band B+ unless equipment timing proves a safer alternative;
- `Quick Draw` should not exist before guns exist;
- final shop/gil timing must retest this row before W5.

## Healing And Sustain Checks

Starter healing race, max HP 150, immediate heal before a tick-1 threat:

| Option | HP before | Incoming damage | Final HP | Survives |
| --- | ---: | ---: | ---: | --- |
| `First Aid` | 40 | 60 | 0 | no |
| `Potion` | 40 | 60 | 10 | yes |
| `First Aid` | 50 | 45 | 25 | yes |
| `Potion` | 50 | 45 | 35 | yes |
| `Hi-Potion` | 40 | 60 | 50 | yes |

Read:

- Squire can patch chip damage but does not replace Chemist;
- Potion can save a low-HP starter from a lethal next hit;
- Hi-Potion is strong enough that it must be held out of Band A floor play.

### Auto-Potion Boundary

Auto-Potion is not fully accepted here as an RSM value. This pass only pins a safe boundary for the
later RSM producer:

```text
trigger chance = 70%
eligible item = Potion only
raw heal = 30
per-round cap = 1
Item Lore interaction = none
timing = post-damage survivor-only
```

Boundary rows:

| HP before | Incoming damage | HP after damage | Reaction can resolve | Timed expected heal |
| ---: | ---: | ---: | --- | ---: |
| 100 | 60 | 40 | yes | 21 |
| 100 | 120 | -20 | no | 0 |
| 150 | 40 | 110 | yes | 21 |

This keeps Auto-Potion from solving lethal burst, prevents best-potion loops, and makes stock/gil
visible. If Claude prefers to keep all reaction values out of this producer, this section can be
moved into the future RSM document without changing the action-value rows.

## Campaign Read

Band 0/A expected texture:

- Ramza/Squire and generic Squires use ordinary weapons, `Throw Stone`, `Dash`, `First Aid`, and
  basic positioning;
- one Chemist or item-secondary unit gives reliable Potion and Phoenix Down access;
- the party can survive early mistakes without needing `JP Boost`, `Move +1`, or a hidden route;
- no starter tool gives an infinite sustain loop, a safe damage loop, or a universal combat support.

Band B+ expected texture:

- first guns can make active Chemist feel distinct without erasing Archer or Orator;
- `Quick Draw` is utility pressure, not a damage steroid;
- `Hi-Potion` and `Ether` improve item support but start item-economy pressure;
- Squire can donate utility, but not a best-in-slot secondary.

## Timing Invariant Check

| Invariant | Result |
| --- | --- |
| I1 starter tools not permanent defaults | Pass provisionally: Squire values are low and Chemist early power is item-limited. |
| I2 first specialists work before exports | Preserved: nothing here gives Knight/Archer/White/Black rewards early. |
| I3 deep secondary on shallow chassis | Watch: Items remain reliable on any chassis; W4 must count Item secondary incidence. |
| I4 build-defining supports earned | Preserved here: final support values are deferred; `JP Boost` gets no combat stat. |
| I5 strong reactions not early skips | Preserved only if Auto-Potion remains Band C with the boundary above. |
| I6 mobility remains a choice | Preserved here: `Move +1` not priced or strengthened in this pass. |
| I7 sustain has texture | Pass provisionally: Squire patch, Chemist item, White Mage delayed, and Monk proximity lanes remain distinct. |
| I8 protection not upkeep | Not touched. |
| I9 control not replacing damage | Not touched, except `Smoke Bomb` deferred. |
| I10 late jobs powerful not mandatory | Not touched. |

## Open W3 Links

This pass creates input for later W3/W4/W5 work, but does not close those gates.

Still required:

- JP/prerequisite producer must decide when these JP values are realistically reachable;
- equipment shop/gil producer must pin Potion, Phoenix Down, Hi-Potion, Ether, and gun timing;
- RSM producer must validate `JP Boost`, `Move +1`, `Throw Item`, `Auto-Potion`, `Item Lore`,
  `Safeguard`, `Reequip`, and `Move-Find Item`;
- W4 must count cross-job incidence of Items, `Throw Item`, `Auto-Potion`, and Squire utility;
- W5 must test starter and Band B parties with and without early guns.

## Claude Review Request

Claude should review whether:

- Squire values preserve starter usefulness without creating a permanent secondary;
- Chemist Band 0/A should be item-first with guns held to Band B+;
- Potion, First Aid, Phoenix Down, and Hi-Potion values preserve the sustain lanes from doc 31 I7;
- the Auto-Potion boundary belongs in this document or should be moved entirely to the future RSM
  producer;
- `Weapon Drill` is narrow enough to accept provisionally or should be deferred.
