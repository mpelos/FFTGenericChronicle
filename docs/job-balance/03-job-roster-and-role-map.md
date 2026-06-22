# Job Roster And Role Map V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/baseline_jobs.csv`
- `work/sim-inputs-v0.2.1.json`

## Purpose

This document maps the in-scope job roster before detailed skill design begins.

It does not define final skill lists, JP costs, multipliers, prerequisites, or exact equipment
changes. It defines each job's provisional tactical reason to exist, growth profile, current
formula coupling, and open risk flags.

Detailed job kits must consume this map. They should not contradict it without writing a new
accepted role-map version.

## Scope

In scope:

- generic player jobs from `work/baseline_jobs.csv` rows `74-93`;
- Arithmetician as the slot replaced by Necromancer;
- Mime as the slot replaced by Vanguard;
- Ramza's unique chapter-progressing job.

Out of scope:

- non-Ramza unique character jobs;
- monsters;
- encounter/map design;
- individual equipment item tuning beyond job compatibility.

## Source Conventions

### Equipment Family Mapping

This role map uses the current formula-family mapping from v0.2.

| Baseline equip label | Formula family | Damage mode |
| --- | --- | --- |
| `Sword` | `sword` | `swing` |
| `KnightSword` | `knight_sword` | `swing` |
| `Katana` | `katana` | `swing` |
| `Knife` | `knife` | `thrust` |
| `NinjaBlade` | `ninja_blade` | `swing` |
| `Bow` | `longbow` | `missile` |
| `Crossbow` | `crossbow` | `missile` |
| `Gun` | `gun` | `missile` |
| `Polearm` | `spear` | `thrust` |
| `Staff` | `staff` | `crush` |
| `Rod` | `rod` | `crush` |
| `Pole` | `pole` | `crush` |
| `Axe` | `axe` | `crush` |
| `Flail` | `flail` | `crush` |
| `Unarmed` | `fists` | `crush` |
| `Instrument` | `instrument` | `missile` |
| `Book` | `book` | `crush` |
| `Cloth` | `cloth_weapon` | `swing` |
| `Bag` | `bag` | `crush` |

`Unarmed` appears broadly in the local job table. This map lists `fists` where the baseline allows
it, but unarmed only counts as a real job identity when the kit supports it. Monk is the protected
unarmed/crush home unless a later accepted proposal says otherwise.

### Armor-Class Source

Armor class is the current target-profile label from `work/sim-inputs-v0.2.1.json`.

It is not final implementation data. It is the starting reconciliation point Claude requested:
ratify or flag it; do not reinvent it silently.

For v0.2.1 reconciliation, a job's target `armor_class` follows the heaviest armor tier it can
actually equip:

- `plate`: heavy Armor/Helmet access, especially with Shield or heavy frontline posture;
- `mail`: partial heavy profile, shielded light profile, or mixed martial profile;
- `leather`: Clothing/Hat light profile;
- `cloth`: Clothing-only, robe/caster, performer, or unarmored fragile profile.

Deliberate exceptions must be documented in the relevant job proposal.

## Roster Summary

| Job | Direction | Primary role | Secondary tags | Growth | Armor class | Native formula families | Modes | Role reason |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Squire | Keep fantasy, rewrite kit | `melee-physical` | `starter`, `utility` | physical | `leather` | `sword`, `knife`, `axe`, `flail`, `fists` | swing/thrust/crush | Baseline flexible physical job; should teach weapon identity without becoming obsolete only as JP filler. |
| Chemist | Keep fantasy, rewrite kit | `specialist` | `item`, `gun` | hybrid | `leather` | `gun`, `knife`, `fists` | missile/thrust/crush | Reliable item support plus PA-independent ranged route; should remain useful through action certainty and utility. |
| Knight | Keep fantasy, rewrite kit | `melee-physical` | `durable`, `weapon-break` | physical | `plate` | `knight_sword`, `sword`, `fists` | swing/crush | Durable armed control; should pressure equipment and hold ground without being only "best sword user." |
| Archer | Keep fantasy, rewrite kit | `ranged-physical` | `missile`, `anti-mail` | physical | `leather` | `longbow`, `crossbow`, `fists` | missile/crush | The main bow job; should remain relevant to endgame through range, targeting, height, and missile identity. |
| Monk | Keep fantasy, rewrite kit | `melee-physical` | `crush`, `Brave` | physical | `cloth` | `fists` | crush | Unarmed impact specialist; should be a real anti-plate/body-discipline route without needing weapons. |
| White Mage | Keep fantasy, rewrite kit | `caster-support` | `Faith`, `staff` | magical | `cloth` | `staff`, `fists` | crush/magic | Healing/protection caster; staff gives a minor MA/crush fallback, but support magic is the identity. |
| Black Mage | Keep fantasy, rewrite kit | `caster-offense` | `Faith`, `rod` | magical | `cloth` | `rod`, `fists` | crush/magic | Main magical damage job; should preserve spell pressure and interact cleanly with Faith/Shell/element. |
| Time Mage | Keep fantasy, rewrite kit | `controller` | `CT`, `staff` | magical | `cloth` | `staff`, `fists` | crush/magic | Time/tempo controller; value should come from CT, speed, delay, and support timing instead of raw spell damage. |
| Summoner | Keep fantasy, rewrite kit | `caster-offense` | `AoE`, `MP` | magical | `cloth` | `staff`, `rod`, `fists` | crush/magic | Delayed area caster; should trade CT/MP and fragility for powerful, readable large-scale effects. |
| Thief | Keep fantasy, rewrite kit | `specialist` | `fast`, `knife` | physical | `leather` | `knife`, `fists` | thrust/crush | Fast utility and precision job; should use Speed, stealing/disruption, and thrust identity without becoming pure damage. |
| Orator | Keep fantasy, rewrite kit | `controller` | `recruit`, `gun` | hybrid | `leather` | `gun`, `knife`, `fists` | missile/thrust/crush | Social battlefield manipulator; should matter in combat through morale, speech, recruitment, Brave/Faith/status, and guns. |
| Mystic | Keep fantasy, rewrite kit | `controller` | `Faith`, `crush` | magical | `cloth` | `pole`, `book`, `staff`, `rod`, `fists` | crush/magic | Spiritual/status controller with unusually broad MA-crush access; must not eclipse dedicated casters or crush specialists. |
| Geomancer | Keep fantasy, rewrite kit | `hybrid` | `terrain`, `mail` | hybrid | `mail` | `axe`, `sword`, `fists` | crush/swing | PA/MA terrain hybrid; should connect physical formula identity to map state without becoming generic melee. |
| Dragoon | Keep fantasy, rewrite kit | `melee-physical` | `thrust`, `plate` | physical | `plate` | `spear`, `fists` | thrust/crush | Reach/jump physical specialist; should be the clean spear/thrust job and a natural anti-mail route. |
| Samurai | Keep fantasy, rewrite kit | `melee-physical` | `katana`, `Brave` | physical | `plate` | `katana`, `fists` | swing/crush | Disciplined Brave/katana job; should be strong and stylish without becoming another universal sword answer. |
| Ninja | Keep fantasy, rewrite kit | `melee-physical` | `fast`, `dual-wield` | physical | `leather` | `ninja_blade`, `knife`, `flail`, `fists` | swing/thrust/crush | Fast physical pressure and multi-hit stress job; must stay iconic without making all physical optimization converge on it. |
| Arithmetician | Replace with Necromancer | `late-reward` | `dark-magic`, `undead` | magical | `cloth` | current slot: `pole`, `book`, `fists`; final TBD | crush/magic/spirit/drain TBD | Calculator is removed. Necromancer should be a late dark caster/debuffer; exact kit is deferred. |
| Bard | Keep fantasy, rewrite kit | `performer` | `support`, `instrument` | hybrid | `cloth` | `instrument`, `bag`, `fists` | missile/crush | Performance support job; action identity differs from Dancer, but reaction/support/move must match Dancer exactly. |
| Dancer | Keep fantasy, rewrite kit | `performer` | `debuff`, `cloth_weapon` | hybrid | `cloth` | `cloth_weapon`, `bag`, `knife`, `fists` | swing/crush/thrust | Performance pressure job; action identity differs from Bard, but reaction/support/move must match Bard exactly. |
| Mime | Replace with Vanguard | `late-reward` | `elite-knight`, `TBD` | physical | old slot has no normal armor; final TBD | current slot: `fists`; final TBD | TBD | Mime is removed. Replacement should be a late vanguard comparable in value to Holy Knight but not a clone; exact kit deferred. |
| Ramza | Rewrite unique job | `protagonist` | `hybrid`, `leadership` | hybrid | chapter-dependent TBD | knight/mage hybrid access TBD | swing/crush/thrust/magic TBD | Ramza should evolve by chapter and reach top-tier value by Chapter 4 through flexibility, not specialist dominance. |

## Formula Ecology Checks

### Damage Mode Supply

Current native access from the roster supplies every v0.2 physical mode:

- `swing`: Squire, Knight, Geomancer, Samurai, Ninja, Dancer, Ramza TBD;
- `thrust`: Squire, Chemist, Thief, Orator, Dragoon, Ninja, Dancer, Ramza TBD;
- `crush`: Squire, Chemist, Monk, mage staves/rods/poles/books, Geomancer, Ninja, Bard, Dancer,
  Necromancer slot TBD, Vanguard TBD, Ramza TBD; generic `fists` access is not enough by
  itself to claim job identity;
- `missile`: Chemist, Archer, Orator, Bard.

Initial verdict: no damage mode is absent, but physical `crush` needs an explicit late-game identity
so plate's weakness does not live only in oddball or caster-adjacent routes.

### Armor Target Distribution

Current v0.2.1 target armor classes:

| Armor class | Jobs |
| --- | --- |
| `plate` | Knight, Dragoon, Samurai |
| `mail` | Geomancer |
| `leather` | Squire, Chemist, Archer, Thief, Orator, Ninja |
| `cloth` | Monk, White Mage, Black Mage, Time Mage, Summoner, Mystic, Arithmetician/Necromancer slot, Bard, Dancer |

Initial verdict:

- plate targets exist in three martial jobs after Samurai is reconciled to heavy armor access;
- mail is currently narrow and centered on Geomancer's mixed shield/light profile;
- leather covers most agile or support jobs;
- cloth contains most casters plus Monk, Bard, and Dancer.

The final role map should still verify whether Bard/Dancer active stat and equipment differences
create unwanted gender optimization pressure. Their global reaction/support/move parity is
mandatory, and v0.2.1 reconciles their target armor class to the same `cloth` profile.

### Protected Formula Identities

The role map should preserve:

- Archer as the primary bow/crossbow home;
- Chemist and Orator as gun homes;
- Dragoon as spear/thrust home;
- Monk as unarmed crush home;
- Geomancer as the main hybrid terrain physical route;
- Mystic as the broad MA-crush/status route;
- Bard/Dancer as performer jobs with shared global build pieces;
- Necromancer and Vanguard as late jobs deferred until the base ecosystem is clear.

## Risk Flags For Later Design

### R1 - Crush Must Become A Real Anti-Plate Plan

Formula v0.2 gives plate a crush weakness. The roster has many crush-capable families, but not yet
a clearly protected late-game anti-plate job identity besides Monk/Geomancer/Ninja-adjacent routes.

Later job design must ensure at least one non-gimmick crush build is attractive into plate.

### R2 - Archer Must Not Be A Midgame Dead End

Archer is the only real bow job. Its role map therefore treats it as endgame-relevant by default.

If bows/crossbows are strong only as borrowed equipment on other jobs, Archer fails its role.

### R3 - Gun Access Must Stay Distinct

Guns are PA-independent missile pressure. Chemist and Orator currently own this route.

If gun access becomes too broad, Chemist/Orator lose identity. If it stays too weak, the formula
model's gun role is wasted.

### R4 - Caster Crush Access Needs Boundaries

Staff, rod, pole, and book are all `crush` in v0.2. This gives casters useful physical-ish backup
and plate interaction.

Later design must prevent MA-crush access from making dedicated physical crush jobs irrelevant.

### R5 - Bard/Dancer Parity Needs Explicit Implementation

Bard and Dancer remain gender-restricted, but reaction/support/move parity is a hard invariant.

The v0.2.1 bundle reconciles both jobs to `cloth` target profiles because their real armor access
is Clothing/Hat. Detailed Bard/Dancer design still must check active stat and equipment differences
against the user's gender policy.

### R6 - Replacements Must Be Designed Late

Necromancer and Vanguard are deliberately deferred.

Designing them before the generic ecosystem is mapped would risk using them to patch holes that
basic jobs should solve.

### R7 - Ramza Must Be Broad, Not Dominant

Ramza can be broadly strong by Chapter 4.

He must not strictly dominate any generic job inside that generic job's signature niche.

## Deferred Detailed Work

The following are intentionally not decided here:

- exact active skill lists;
- exact reaction/support/move lists;
- exact JP costs;
- exact prerequisite tree;
- exact final multipliers;
- exact growth values inside the physical/magical/hybrid profiles;
- exact Bard/Dancer active stat and equipment parity;
- exact Necromancer kit;
- exact Vanguard kit;
- exact Ramza chapter-by-chapter kit.

Next accepted step after this role map should be targeted job-group proposals, beginning with the
basic and intermediate jobs before late replacements.
