# Vanilla Skill, Status, And Effect Reference Map

Status: Accepted reference map for job-balance consultation
Date: 2026-06-22

## Purpose

Use this directory as the lookup surface for vanilla Final Fantasy Tactics skills,
statuses, and effect families before continuing Generic Chronicle job redesign.

This is a consultation map, not a compatibility cage. Generic Chronicle can replace,
merge, narrow, or reject vanilla behavior when that improves the game. The point is
to keep those choices informed by the full vanilla vocabulary rather than by memory
or by only the most famous jobs.

## Fast Lookup

| Question | Open first |
| --- | --- |
| What does this vanilla ability slot currently represent? | `fft-vanilla-ability-effect-index.md` |
| What does this vanilla command/job skillset do as a whole? | `fft-vanilla-command-skillset-effect-map.md` |
| Which skills already use this mechanic, such as `status_add`, `ct_action`, `equipment_break`, `global`, `reaction`, `undead`, or `movement`? | `fft-vanilla-ability-effect-tag-crosswalk.md` |
| What does a status do, and what validation track does it touch? | `fft-vanilla-status-effect-map.md` |
| How should job-balance proposals cite this atlas? | `../job-balance/12-vanilla-skill-status-reference.md` |

## Coverage Snapshot

The atlas covers every non-empty local ability record currently extracted from
`work/baseline_abilities.csv`.

| Surface | Coverage |
| --- | ---: |
| Local ability table rows | 512 |
| Non-empty named ability records | 491 |
| Command/job-like buckets | 32 |
| Effect tags used for design lookup | 44 |
| Vanilla status/status-like records tracked | 40 |
| Locally observed JobData status names | 38 |

The ability rows include local ID, name, JP, random flags, CT/MP overrides, command
bucket, owner classification, effect summary, and effect tags.

The 32 command/job-like buckets are 31 named command ranges plus one local placeholder
bucket. The 44 effect tags are the full crosswalk tag set; 43 currently appear on
ability rows and `caster_support` is a zero-record reserved design tag.

The local CSV does not currently expose base range, area, vertical, element, formula,
X/Y, or inflicted-status values for most abilities. Treat any formula-sensitive
claim as requiring a validation track, proof patch, external mechanics source, or
direct extracted formula data before final implementation.

## Command Skillset Palette

| Command bucket | Vanilla owner | Records | Primary design read |
| --- | --- | ---: | --- |
| White Magicks | White Mage | 15 | healing, revive, mitigation, cleanse, Holy |
| Black Magicks | Black Mage | 16 | elemental damage, Death, Toad, poison pressure |
| Time Magicks | Time Mage | 12 | Haste/Slow, Stop, movement status, Reflect, gravity, Meteor, Quick |
| Mystic Arts | Mystic | 15 | Faith/Brave manipulation, drain, spiritual status, cleanse |
| Summon | Summoner | 16 | delayed area damage/healing/defense with high MP/CT pressure |
| Iaido | Samurai | 10 | katana-driven magical/special area output vocabulary |
| Bardsong | Bard | 7 | global ally-side healing, stat, Brave, random, and instant-KO vocabulary |
| Dance | Dancer | 7 | global enemy-side HP/MP/stat/status/instant-KO vocabulary |
| Martial Arts | Monk | 8 | physical damage, self/team sustain, cleanse, revive |
| Steal | Thief | 8 | gear/economy theft and Charm-style control |
| Speechcraft | Orator | 10 | Brave/Faith, recruit, speech status, economy, Condemn |
| Geomancy | Geomancer | 12 | terrain damage with status rider vocabulary |
| Arts of War | Knight | 8 | equipment breaks, MP/speed/power/magick pressure |
| Fundaments | Squire | 4 | basic physical utility, Focus, Stone, Salve |
| Ramza Squire | Ramza | 5 | Tailwind, Steel, Shout, Ultima, hybrid hero hooks |
| Holy/Dark sword skills | Unique sword jobs | 12 | sword damage, status, drain, equipment crush, dark area attack |
| Unique magicks | Unique jobs | 14 | unique damage/status/cleanse vocabulary, proof-first |
| Enemy/unique statuses and Bio | Enemy/unique | 30 | named status tools, sap effects, Bio, special/boss vocabulary |
| Boss/guest/unique actions | Boss/guest/unique | 17 | special shots, undead seal, ja spells, unique enemy pressure |
| Templar/status actions | Templar/unique | 17 | direct status magic, Dispel, drain, Faith/Brave, Zombie |
| Dragonkin | Dragonkin | 8 | dragon recruit, heal/cleanse, buffs, breath damage |
| Limit | Cloud | 9 | charged special damage/status vocabulary |
| Monster actions | Monsters | 91 | monster vocabulary only unless explicitly reused |
| Unmapped/local placeholder | Unknown | 1 | local placeholder/unknown record, proof-first |
| Items | Chemist | 15 | consumable healing, MP, revive, status clear, undead cleanup |
| Throw | Ninja | 12 | ranged physical weapon/item consumption |
| Jump unlocks | Dragoon | 12 | vertical/range movement unlock vocabulary |
| Aim | Archer | 8 | charged physical ranged damage vocabulary |
| Arithmeticks selectors | Arithmetician | 8 | rejected Calculator-style global selectors |
| Reaction abilities | Reaction | 31 | trigger-rate, defense, recovery, counter, revive, surge effects |
| Support abilities | Support | 29 | equipment unlocks, damage boosts, economy, accuracy, MP, cross-job identity |
| Movement abilities | Movement | 24 | mobility, terrain, teleport/fly, resource-on-move, economy |

## Effect Family Map

| Family | Tags | Design pressure |
| --- | --- | --- |
| Damage and scaling | `damage`, `physical`, `magical`, `elemental`, `damage_boost`, `accuracy` | Formula envelope, armor response, Faith/Shell/Protect, evasion, weapon identity. |
| Area and target shape | `aoe`, `global`, `ally_buff` | Cluster pressure, friendly-fire pressure, target eligibility, summon/performance throughput. |
| Healing and attrition | `healing`, `revive`, `drain`, `undead`, `status_clear` | Sustain loops, KO pressure, death-clock, corpse/undead rules. |
| Status, morale, and control | `status_add`, `instant_ko`, `brave_down`, `brave_up`, `faith_down`, `faith_up`, `stat_down`, `stat_up`, `recruit`, `random` | Hit rate, immunity, boss resistance, player agency, AI/control edge cases, buff/debuff cadence. |
| Timing and action economy | `ct_action`, `timing`, `jump` | CT delay, interruption, turn frequency, performance cadence, untargetable windows. |
| Defense and targeting | `defense`, `reaction` | Mitigation stacking, evasion, reaction incidence, target eligibility. |
| Position and terrain | `movement`, `terrain` | Map identity, range, elevation, terrain bypass, encounter shape. |
| Equipment, economy, and slots | `equipment_break`, `equipment_unlock`, `steal`, `throw`, `economy`, `jp_exp`, `mp`, `support`, `caster_support` | Gear pressure, item/gil/JP economy, resource gating, support-slot incidence, caster throughput. |
| Proof-first vocabulary | `special`, `unique`, `local_placeholder`, `arithmeticks_selector` | Do not assume generic behavior; prove or intentionally redesign first. |

## Status Map

| Design concern | Statuses to inspect first |
| --- | --- |
| Action/control denial | `Berserk`, `Charm`, `Chicken`, `Confuse`, `Disable`, `Silence`, `Sleep`, `Stone`, `Stop`, `Toad`, `Vampire` |
| Timing and action states | `Charging`, `Defending`, `Haste`, `Jump`, `Performing`, `Slow`, `Stop` |
| Mitigation, evasion, and targeting | `Blind`, `Defending`, `Invisible`, `Protect`, `Reflect`, `Shell` |
| Attrition, healing, revive, and defeat | `Critical`, `Doom`, `KO`, `Poison`, `Regen`, `Reraise`, `Undead` |
| Movement, terrain, and position locks | `Float`, `Immobilize`, `Jump` |
| Permanent or campaign-facing states | `Chest`, `Crystal`, `Dark/Evil Looking`, `Traitor`, `Unused1`, `Wall` |
| Faith, Brave, element setup, and formula multipliers | `Atheist`, `Berserk`, `Chicken`, `Faith`, `Oil`, `Protect`, `Shell` |

Important notes:

- `Protect` and `Shell` are mitigation states, so physical and magical formula
  testing should keep them visible without making them mandatory.
- `Oil` is a high-risk element setup state because fire damage amplification can
  combine with Faith, weakness, area targeting, and caster economy.
- `Silence` is a core anti-caster state and should stay visible whenever caster
  throughput, MP economy, or spell reliability is being tuned.
- `Haste`, `Slow`, `Stop`, `Charging`, `Jump`, and `Performing` are action-economy
  states, not simple buffs/debuffs.
- `Undead`, `KO`, `Crystal`, and `Chest` are not ordinary status riders. They touch
  healing, revive, corpse timing, and campaign-facing defeat rules.
- `Dark/Evil Looking` and `Wall` are external-only dummied statuses in this atlas.
  They remain proof-first unless implementation research proves otherwise.

## Job Redesign Checklist

Before a job proposal moves to Claude review, it should state:

- which vanilla command bucket, ability rows, effect tags, and statuses were checked;
- whether the proposal preserves, narrows, replaces, combines, or rejects vanilla behavior;
- which validation tracks are required for damage, healing, status, CT/timing, global effects,
  equipment pressure, movement, AI targeting, undead, or KO/corpse behavior;
- whether any exact range, area, formula, status duration, hit rate, or implementation hook is
  missing from the local extraction and must be proven later.

## Source And Trust Model

Local extracted facts:

- `work/baseline_abilities.csv`
- `work/baseline_jobs.csv`
- `tools/dump_baseline.py`
- `tools/build_vanilla_reference.py`

External references consulted:

- GameFAQs Jobs/Abilities Chart by just_call_me_ash:
  `https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3859`
- AeroStar Battle Mechanics Guide:
  `https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876`
- GameFAQs War of the Lions status guide:
  `https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/status-effects`
- GameFAQs War of the Lions generic jobs guide:
  `https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/generic-jobs`
- Final Fantasy Wiki ability/status category pages:
  `https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_abilities`
  `https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_statuses`

Trust rule:

- local IDs, names, JP, random flags, CT overrides, and MP overrides are extracted facts;
- command owner, effect summaries, tags, status categories, and design hooks are
  researched planning classifications;
- exact mechanical formulas and implementation-sensitive behavior still require proof before
  final balance acceptance.
