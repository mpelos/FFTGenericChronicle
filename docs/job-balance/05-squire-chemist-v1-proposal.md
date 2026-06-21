# Squire And Chemist V1 Proposal

Status: Accepted for provisional design
Version: V1
Date: 2026-06-20
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/04-foundation-physical-jobs-proposal.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Squire and Chemist.

The proposal is concrete enough to define skill roles, build hooks, JP posture, and required checks.
It is not final implementation data. It does not set exact JP numbers, hit rates, CT values, item
healing amounts, damage multipliers, equipment records, stat multipliers, or prerequisites.

Because it does not change equipment access, stat multipliers, growth, armor class, weapon-family
formulas, or exact support/reaction formulas, it does not trigger Gate F5 by itself. Concrete data
versions of the skills below may trigger Gate F5, especially if they alter accuracy, evasion,
action economy, damage output, item healing, or automatic healing loops.

Healing, sustain, MP recovery, revive, and attrition checks are not currently executable in the
damage-only formula harness. Any concrete First Aid, Potion-line, Field Salve, Phoenix Down,
Auto-Potion, Item Lore, or similar sustain values require a healing/sustain/attrition harness
extension before they can be accepted as implementation data.

## Group Thesis

Squire and Chemist should stay recognizable as FFT's first physical and item jobs.

Their job is not to be weak tutorials. Their job is to create the player's first build decisions:

- Squire teaches that a physical unit can use position, weapon family, and small tactical glue.
- Chemist teaches that reliability, items, inventory, and guns are real combat plans.

Both jobs may donate useful global tools, but neither should become a mandatory tax for serious
late-game builds.

## Squire

Job: Squire
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: starter physical job with Fundaments, JP Boost, and Move +1.

Vanilla problems:

- the job is often valued more for JP Boost than for active combat identity;
- Focus/Accumulate-style stat stacking can become repetitive if it is the best low-risk action;
- late-game Squire can feel like a shell for borrowed skills instead of a real tactical identity.

Accepted high-level role: flexible starter/utility physical job.

Primary role: `melee-physical`

Secondary tags: `starter`, `utility`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `sword`, `knife`, `axe`, `flail`, `fists`.

Armor class as target: `leather`.

Supported damage modes: `swing`, `thrust`, `crush`.

Formula v0.2 coupling:

- Squire is the first job that can show multiple physical damage modes without requiring advanced
  job access;
- Squire should not be the strongest user of any one family;
- if a Squire skill changes accuracy, damage response, or weapon-family behavior, the concrete
  version must run the relevant formula or mandatory-secondary checks.

### Action Skillset Goals

Squire's action set should be low complexity, broadly understandable, and tactically useful.

It should not be a pure ladder. Each action should have a clear use case and a clear reason not to
spam it.

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Throw Stone` | ranged chip / displacement | Finish weak targets, turn a unit, or create small positional pressure without committing to melee. | Low payoff; should not scale into a real ranged damage plan. |
| `Dash` | adjacent shove / body check | Push, interrupt formation, or create space when already adjacent. | Requires exposure; not a primary damage button. |
| `First Aid` | small adjacent recovery | Let a starter unit patch chip damage or stabilize an ally when no real healer is available. | Adjacent or very short range; no revive; should not replace Chemist or White Mage. |
| `Focus` | short self-preparation | Make the next basic physical action more reliable or more disciplined. | Non-stacking; expires quickly; must not become a permanent stat loop. |
| `Rally` | small ally tempo/morale glue | Help a nearby ally recover initiative, maintain pressure, or resist a morale problem. | Small effect; no broad Haste replacement; action economy cost is real. |
| `Weapon Drill` | weapon-family teaching action | Teach that swing, thrust, and crush can have different tactical texture on the same job. | Requires current weapon identity; must not become the best universal secondary action. |

`Weapon Drill` is a design-risk skill. The concrete implementation should pick one narrow behavior
per damage mode, such as swing reliability, thrust precision, or crush shove/guard pressure. If it
becomes a broad toolbox, it must run `M-SECONDARY-COUNT`.

`First Aid` is also a design-risk skill. Because it is cheap, repeatable, MP-free, Faith-free, and
available from a starter job, the concrete healing value must be bounded against v0.2 damage bands
and checked with `I-ATTRITION` plus `M-SECONDARY-COUNT`. If `Rally` restores HP, MP, CT, or another
recoverable resource, it inherits the same sustain checks.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Grit` | early defensive morale reaction | Give the starter job a simple way to stay relevant under pressure, such as a short defensive or morale response after taking damage. | Must not stack into practical immunity; must be weak or narrow enough to fall off naturally. |

`Grit` should not be a broad evasion reaction. If it reduces expected incoming damage, it must run
the relevant immunity rows before final acceptance.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `JP Boost` | campaign economy support | Preserve FFT's build-planning pleasure by making JP planning less punishing. | No combat bonus; counted separately from combat mandatory-piece rows unless used in combat benchmark builds. |
| `Basic Training` | Squire-action support | Improve or unlock Squire's own simple utility without increasing all physical damage. | Should support Fundaments-style play, not become a universal physical support. |

`JP Boost` is allowed as a campaign/economy exception, but it must not carry combat power. Its cost
should make it attractive early without forcing late-game combat builds to equip it.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Move +1` | basic mobility floor | Preserve a familiar early movement reward and make early maps less sluggish. | Must be outclassed by later specialized movement in the right builds; not a late universal default. |

### JP Progression

JP posture:

- core action tools should be cheap and available early;
- `JP Boost` should be reachable early enough to reduce excessive grind;
- `Move +1` should stay an early build hook;
- `Basic Training` and any broad secondary enabler should cost enough to signal commitment;
- no Squire skill should require late-game grinding to make the job functional.

### Prerequisite Changes

Squire remains a starting job.

This proposal does not set downstream job prerequisites. Later tree design may change how Squire
feeds other jobs, but Squire should not become a long mandatory grind path.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Squire donor patterns:

- early units borrow `Move +1` while learning their first real build;
- campaign builds borrow `JP Boost` while training;
- physical utility builds borrow Fundaments-style actions when they want low-MP, low-Faith utility;
- some melee jobs borrow `Weapon Drill` only if their current weapon family gives a specific
  tactical reason.

Unhealthy Squire donor patterns:

- most serious builds equip `JP Boost` in real combat;
- most physical jobs use `Weapon Drill` as their best secondary regardless of weapon;
- `Focus` becomes a no-risk action loop;
- `Move +1` remains the correct movement skill deep into late-game optimization.

### Expected Strong Builds

- early Squire with any basic physical secondary;
- Knight or Archer with Squire secondary for simple utility and positioning;
- low-Faith physical unit using Squire tools instead of magic support;
- campaign training build using `JP Boost`, explicitly counted outside combat benchmarks.

### Expected Weaknesses

- lower late-game ceiling than specialized martial jobs;
- leather durability;
- no real ranged specialization;
- no deep healing, revival, status, or magic identity.

### Expected Counters

- durable plate enemies if Squire is not using a crush route;
- long-range pressure;
- dedicated control/status;
- enemies that punish adjacency.

### Ramza / Unique-Job Interaction

Ramza may inherit or echo Squire fundamentals by chapter, but Chapter 4 Ramza must not turn generic
Squire into a pointless job. Ramza's version can be broader; generic Squire should remain the clean
starter and low-complexity utility shell.

## Chemist

Job: Chemist
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: item specialist and early reliable support job.

Vanilla problems:

- item healing can become too automatic if Auto-Potion scales freely;
- the job can feel like a delivery mechanism for items rather than a tactical battlefield role;
- item access can collide with White Mage if reliability beats magic in too many situations;
- gun identity can be underused if Chemist is treated only as a healer.

Accepted high-level role: reliable item/gun specialist.

Primary role: `specialist`

Secondary tags: `item`, `gun`

Growth profile: `hybrid`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `gun`, `knife`, `fists`.

Armor class as target: `leather`.

Supported damage modes: `missile`, `thrust`, `crush`.

Formula v0.2 coupling:

- guns are PA-independent missile pressure, so Chemist can contribute even with low PA;
- item actions compete with gun attacks for turns;
- item healing, Auto-Potion, and any smoke/evasion utility can change survival and action economy,
  so concrete values require attrition and mandatory-piece checks.

### Action Skillset Goals

Chemist should remain the most reliable item user.

Its actions should feel certain and practical, but the certainty must be paid for through inventory,
gil, single-target limits, positioning, and action economy.

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Potion` line | reliable HP recovery | Restore HP without Faith/MA variance. | Item cost; single-target; exact healing values must fit damage bands. |
| `Ether` line | reliable MP recovery | Support caster-heavy parties without replacing caster MP planning. | Scarcer item economy; single-target; action cost. |
| `Phoenix Down` | reliable revive | Preserve the iconic emergency revive identity. | Revives at low HP; item cost; range must matter. |
| `Remedy Kit` | condition removal | Consolidate common condition cures into a readable item-support role. | Inventory/gil cost; no broad prevention; no AoE Esuna replacement. |
| `Field Salve` | short-range stabilization | Slow attrition, stop bleeding-style pressure, or stabilize danger without full healing. | Should be weaker than true healing; no infinite sustain loop. |
| `Smoke Bomb` | defensive field tool | Create a short-lived line-of-sight, accuracy, or targeting disruption if implementable. | Formula-affecting if it changes accuracy/evasion; must be modeled before acceptance. |
| `Quick Draw` | gun-compatible utility | Let Chemist use a gun turn tactically, such as tagging an exposed target or setting up item support. | Should not be a gun damage steroid; Orator must keep a distinct gun/control identity. |

The final data version may split or merge item lines depending on ability slots. The design goal is
not to create many near-identical item buttons; it is to keep item reliability while giving Chemist
field utility that is not pure healing.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Auto-Potion` | emergency attrition reaction | Consume an actual item to blunt incoming damage. | Must be tier-aware, capped, or otherwise prevented from becoming universal survival; must run `I-ATTRITION`. |

`Auto-Potion` must never be balanced around infinite stock or free best-potion selection. It should
consume inventory and remain strongest on item-focused builds.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Throw Item` | item range support | Make item builds work on jobs that deliberately spend their support slot on item delivery. | No combat stats; competes with damage and equipment supports. |
| `Safeguard` | anti-break / anti-steal protection | Give counterplay to enemy equipment pressure and later Knight/Thief-style disruption. | Narrow protection; should not become mandatory unless equipment-break enemies are everywhere. |
| `Reequip` | tactical equipment utility | Let a build spend a support slot on in-battle gear flexibility. | Action/tempo or slot opportunity cost; cannot erase equipment identity by itself. |
| `Item Lore` | item-specialist payoff | Improve item consistency for committed item builds. | Must not make every healer or tank take it; requires mandatory-piece and attrition checks. |

If ability slots are tight, `Reequip` is the first support candidate to defer because it is useful
but not essential to Chemist's core identity.

`Reequip` is formula-affecting if it can change weapon family, damage mode, armor class, or target
profile mid-combat. Any concrete version that allows those swaps must be modeled before acceptance.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Move-Find Item` | campaign/economy exploration | Preserve the treasure-hunting identity without becoming a combat mobility default. | No combat mobility increase; map/item economy value only. |

### JP Progression

JP posture:

- basic HP/status items should be cheap so Chemist works immediately;
- `Phoenix Down` should be reachable early enough to preserve FFT's early revive texture;
- `Throw Item` should be an early-to-mid commitment, not a free default;
- `Auto-Potion` and `Item Lore` should be mid or expensive because they affect attrition;
- `Safeguard` can be mid-cost and becomes more valuable once enemy disruption exists;
- `Move-Find Item` can stay cheap or mid-cost because it is mostly campaign/economy value.

### Prerequisite Changes

Chemist remains a starting job.

This proposal does not set downstream job prerequisites. Later tree design may use Chemist as an
entry point for item, gun, support, or specialist jobs.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Chemist donor patterns:

- White Mage or other support caster takes Items as secondary for reliable emergency backup;
- Archer or Orator-style ranged unit borrows item support when it wants ranged utility instead of
  pure damage;
- durable frontline spends the support slot on `Throw Item` or `Safeguard` for a specific plan;
- low-Faith unit uses Items because Faith-based healing is unreliable.

Unhealthy Chemist donor patterns:

- every serious build wants `Auto-Potion`;
- every healer prefers Items over White Magic in most situations;
- `Throw Item` plus item healing makes positioning irrelevant;
- `Item Lore` becomes the default support for survival builds;
- Chemist gun utility erases Orator's later gun/control niche.

### Expected Strong Builds

- active Chemist with gun and Items, trading damage ceiling for reliability;
- low-Faith support unit using Items as secondary;
- frontline unit using `Safeguard` against equipment-disruption encounters;
- caster party using Chemist to manage MP and emergency revive without relying on Faith.

### Expected Weaknesses

- inventory and gil dependence;
- mostly single-target actions;
- limited damage scaling outside guns;
- leather durability;
- no broad AoE healing or protection;
- weak if the party refuses to invest in item stock.

### Expected Counters

- item scarcity or long attrition maps;
- enemies that pressure multiple allies at once;
- enemies resistant to gun positioning or line of fire;
- effects that demand prevention rather than cure;
- high burst that exceeds low-HP revive or reaction recovery.

### Ramza / Unique-Job Interaction

Ramza may use Items as a secondary like any other unit. His hybrid protagonist job should not gain
free Chemist reliability without paying the same secondary/support/action costs.

## Scenario/Check Plan

This proposal is accepted only as a provisional design direction until the concrete values pass the
relevant rows.

### Squire Rows

Required later:

- `J-EARLY-SELF`: Squire using only its own kit in early-game stats and equipment.
- `J-EARLY-PARTY`: Squire beside Chemist and one early specialist, checking that utility matters.
- `J-MID-SELF`: Squire with core actions but no late borrowed engine.
- `J-LATE-BUILD`: at least one credible late build using Squire as active or donor.
- `M-RSM-COUNT-LATE`: `JP Boost`, `Move +1`, and `Grit` incidence.
- `M-SECONDARY-COUNT`: Fundaments/Squire secondary incidence, especially if `Weapon Drill` is broad.
- `I-ATTRITION`: `First Aid`, and `Rally` if it restores HP, MP, CT, or other recoverable resources.
- `I-PHYS-*` rows if `Grit` reduces expected incoming physical damage.

Skip for now:

- formula re-sim rows, because this document does not set concrete numeric modifiers.

### Chemist Rows

Required later:

- `J-EARLY-SELF`: active Chemist with items and early weapon access.
- `J-EARLY-PARTY`: Chemist as early reliable support beside physical units.
- `J-MID-PARTY`: Chemist item/gun utility with plausible secondaries.
- `J-LATE-BUILD`: item/gun or item-support build that remains useful without being mandatory.
- `J-PARTY-NO-JOB`: comparable party that does not field active Chemist.
- `M-RSM-COUNT-LATE`: `Auto-Potion`, `Throw Item`, `Item Lore`, `Safeguard`, `Move-Find Item`.
- `I-ATTRITION`: `Auto-Potion`, item healing bands, and any sustain loop.
- `U-ARCHETYPE-COVERAGE`: item reliability across damage, status, and burst archetypes.

Conditional:

- Gate F5 or successor encounter simulation if `Smoke Bomb`, `Quick Draw`, `Auto-Potion`, or item
  values change accuracy, evasion, outgoing damage, action economy, or survival curves.
- formula modeling if `Reequip` changes weapon family, damage mode, armor class, or target profile
  during combat.

## Formula Re-Sim Requirement

No immediate Gate F5 re-sim is required for this document.

The concrete data pass must re-evaluate this if it:

- changes Squire or Chemist stat multipliers;
- changes equipment access;
- changes item healing amounts in a way that alters damage-to-HP scale;
- changes gun damage or gun support;
- changes accuracy, evasion, defense, damage response, CT, or action economy;
- changes automatic recovery behavior.

The current `tools/sim_damage.py` harness is damage-only. Before accepting concrete sustain data,
the project needs an explicit healing/sustain/attrition model for rows such as `I-ATTRITION`. This
is a separate harness gap from the dynamic armor/accuracy response modeling flagged in
`docs/job-balance/04-foundation-physical-jobs-proposal.md`.

## Implementation Assumptions

- Data modding can create or repurpose ability records.
- If ability slots are constrained, preserve job identity before preserving every named candidate.
- Skill names are placeholders until the implementation pass verifies text, record limits, and
  localization constraints.
- Campaign/economy skills still matter, but combat benchmark rows must not count training-only
  builds as evidence of combat mandatory usage.

## Open Proof Needs

- Whether `JP Boost` stays as Squire support exactly, becomes cheaper, or receives special
  campaign treatment.
- Whether the sustain/attrition harness should be built before or after the next physical-control
  job slices.
- Whether `Item Lore` can be healthy without making Chemist a mandatory donor.
- Whether `Auto-Potion` should be capped by potion tier, once-per-round behavior, inventory logic,
  Brave, or another data-friendly limiter.
- Whether `Smoke Bomb` is implementable without creating unmodeled evasion/accuracy drift.
- Whether `Weapon Drill` is narrow enough to avoid broad secondary dominance.

## Claude Review Verdict

Claude reviewed whether this proposal:

- preserves Squire and Chemist identity;
- keeps the jobs useful without making them mandatory;
- correctly defers formula-affecting details;
- names the right check rows;
- should be accepted as provisional design, revised, or blocked.

Claude review verdict: approved as provisional design after required patches A/B/C.
