# Campaign Journey Bundle V0

Status: Accepted as W2 provisional campaign journey bundle
Date: 2026-06-21
Depends on:
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/32-campaign-artifacts-provisional-v0.md`
- `docs/job-balance/47-campaign-validation-readiness-v0.md`
- `docs/job-balance/49-vanguard-rename-decision-v0.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `work/gpt-campaign-journey-bundle-v0.json`

## Purpose

This is W2 from the accepted readiness map.

It instantiates provisional campaign journey rows for a normal party of Ramza plus four generic
characters. It also pins synthetic A5 encounter anchors strongly enough that later T2.1/F5 work can
refer to the same rows instead of arguing from vibes.

This document does not accept campaign balance, final JP costs, final prerequisites, final equipment
timing, physical/foundation action values, RSM values, or formula-v1 numeric outcomes.

## Vanilla Atlas Consultation

This W2 bundle treats the vanilla skill/status atlas as a mandatory consultation surface.

The atlas was used as a completeness checklist for:

- physical pressure, armor response, equipment pressure, and defensive stack rows:
  `physical`, `damage`, `defense`, `equipment_break`, `equipment_unlock`, `reaction`, `support`;
- caster and area rows: `magical`, `aoe`, `elemental`, `ct_action`, `mp`, `global`, `timing`;
- sustain and attrition rows: `healing`, `revive`, `drain`, `status_clear`, `undead`;
- control and status rows: `status_add`, `status_clear`, `brave_up`, `brave_down`, `faith_up`,
  `faith_down`, `instant_ko`, `recruit`;
- mobility and map-texture rows: `movement`, `jump`, `terrain`, `throw`, `steal`.

The status map was used as a checklist for `Protect`, `Shell`, `Haste`, `Slow`, `Stop`, `Reflect`,
`Silence`, `Faith`, `Atheist`, `Oil`, `Poison`, `Regen`, `Reraise`, `Undead`, `KO`, `Jump`,
`Performing`, `Invisible`, `Charm`, `Confuse`, `Berserk`, `Toad`, `Sleep`, `Stone`, `Blind`,
`Darkness`, `Doom`, `Death Sentence`, `Immobilize`, `Don't Move`, `Disable`, `Don't Act`, and
`Float`.

Status interactions that overlap this W2 bundle:

- `Blind` / `Darkness`: accuracy denial touches weapon viability and the P2 physical wall test.
- `Doom` / `Death Sentence`: countdown KO touches nonlethal finisher design and boss-resist rows.
- `Immobilize` / `Don't Move` and `Disable` / `Don't Act`: movement lock and action lock are
  separate denial surfaces for the P4 control/sustain row.
- `Float`: terrain bypass and earth immunity touch vertical, Geomancer, and map-texture rows.

This consultation does not make the W2 rows byte-accurate formula data. It only ensures that W2
does not miss major vanilla effect families or status interactions before W3/W4/W5 producers attach
exact JP, equipment, RSM, formula-v1, and real-roster numbers.

## Band Envelopes

These envelopes are planning bounds only. W3 must replace them with exact prerequisite, JP, and
equipment timing data.

| Band | Campaign read | Rough party level | Rough learned depth | Equipment tier | Floor constraint |
| --- | --- | --- | --- | --- | --- |
| 0 | raw start | 1-3 | starter actions only | opening gear | no hidden progression accelerator, no optimized detours |
| A | first direction | 3-7 | one shallow starter route | first shop tier | P0 proceeds under fixed JP without hidden support taxes |
| B | first specialists | 7-14 | one active specialist per unit | basic specialist weapons/armor | active jobs matter before exports |
| C | midgame branches | 14-24 | two routes, one modest RSM | midgame armor and common weapons | no full damage/sustain/control package |
| D | advanced build crafting | 24-36 | two to three routes, one premium RSM | late non-premium equipment | multiple good routes still compete |
| E | final integration | 36+ | late rewards and final Ramza | late equipment, premium checks pending | older specialists still earn rational slots |

Every A1 row below inherits its rough level, JP, and equipment envelope from this table. The row's
anchor, active jobs, and build pieces are explicit so later W3/W4/W5 work can replace the rough
envelope with exact prerequisites, JP costs, RSM values, and formula-v1 results.

## Pinned Synthetic A5 Anchors

The anchors below are not final encounter data. They are pinned synthetic stat blocks for W2/W4/W5
planning until named IVC encounters or exact ENTD/stat rows replace them.

| Anchor | Band | Pinned synthetic stat block | Main question |
| --- | --- | --- | --- |
| `GCV-PIN-00-RAW-MIXED` | 0 | 4 enemies at party level +0/1: 2 leather melee, 1 weak ranged unit, 1 item/support unit; no heavy armor, no hard status, no AoE. | Can raw Squire/Chemist survive without perfect routing? |
| `GCV-PIN-A-PHYSICAL` | A | 5 enemies at party level +0/1: 3 leather melee, 1 early shield/armor body, 1 ranged chip unit; low vertical pressure. | Are starter recovery and basic positioning enough? |
| `GCV-PIN-A-PROGRESSION-RUSH` | A/B | Same enemy profile as A, evaluated under ordinary, optimizer, and grind-heavy fixed-JP routing. | Does fixed-JP routing move Band B depth into Band A reality? |
| `GCV-PIN-B-FIRST-SPECIALIST` | B | 5 enemies at party level +0/2: 1 plate captain, 2 leather skirmishers, 1 cloth caster, 1 ranged unit; moderate spread. | Do Knight, Archer, White Mage, and Black Mage all have active value? |
| `GCV-PIN-B-MONK-SECONDARY` | B | 5 enemies at party level +1/2: 2 durable melee bodies, 1 evasive leather unit, 1 support unit, 1 ranged unit; sustained melee pressure. | Does early Knight-body plus Monk/Squire utility compress too much? |
| `GCV-PIN-C-MITIGATION-STACK` | C | 6 enemies at party level +1/3: 2 plate frontliners, 1 mail bruiser, 1 White/Time support, 1 archer, 1 skirmisher; ordinary offense plus buffs. | Can plate/shield/Protect/Shell/reaction stacks become practical immunity? |
| `GCV-PIN-C-CASTER-STATUS` | C | 5 enemies at party level +0/2: 2 cloth casters, 1 Mystic/Orator-style status unit, 1 guard, 1 ranged interrupter; mixed Faith pressure. | Do anti-magic, status, silence, and Faith tools create fair counters? |
| `GCV-PIN-C-VERTICAL-MAIL` | C | 5 enemies at party level +1/2 on vertical terrain: 2 mail/thrust targets, 1 high-ground ranged unit, 1 mobile leather unit, 1 caster. | Do Archer, Dragoon, Geomancer, Thief, and movement choices matter? |
| `GCV-PIN-D-CLUSTER-AREA` | D | 6 enemies at party level +1/3, clustered start: 2 plate/mail, 2 leather, 2 cloth; one protection support; high target-count temptation. | Does area/global throughput breach the top physical ceiling? |
| `GCV-PIN-D-SPREAD-RANGED` | D | 6 enemies at party level +0/2, spread formation: 3 ranged or fast units, 1 caster, 2 melee blockers; high interruption pressure. | Do performer/caster plans remain vulnerable and positional? |
| `GCV-PIN-D-LONG-FIGHT` | D | 7 enemies at party level +1/3: mixed armor, two sustain/control pieces, reinforcements simulated as extra durable bodies; long attrition. | Do sustain, MP economy, Bard/Dancer, and mitigation remain bounded? |
| `GCV-PIN-E-BOSS-RESIST` | E | 1 boss at party level +4 plus 4 adds: boss resists hard status/death, adds cover mixed armor and one caster; long fight. | Do final Ramza, Necromancer, Vanguard, and top casters avoid mandatory dominance? |
| `GCV-PIN-E-UNDEAD-CORPSE` | E | 6 enemies at party level +1/3: 3 undead/corpse-relevant bodies, 2 living guards, 1 support caster; revive inversion relevant. | Does Necromancer create interesting state play without solving the fight alone? |

## A1 Campaign Party Rows

Verdict terms:

- `provisional row only`: useful W2 planning row, no pass/fail claim.
- `stress row`: later W3/W4/W5 must explicitly test this row.
- `floor row`: P0 must eventually clear within its non-optimized envelope.

| Party | Band | Anchors | Expected five-unit active jobs | Expected build pieces | Planning read | W2 verdict |
| --- | --- | --- | --- | --- | --- | --- |
| P0 naive/thematic | 0 | `GCV-PIN-00-RAW-MIXED` | Ramza/Squire, 2 Squires, 1 Chemist, 1 Squire trainee | basic attacks, basic Items, starter utility | Floor should feel like FFT with better bounded tools. | floor row |
| P0 naive/thematic | A | `GCV-PIN-A-PHYSICAL` | Ramza/Squire, Chemist, Squire physical, Squire/caster trainee, Squire/ranged trainee | fixed-JP floor, Move +1 optional, basic Items | No hidden support tax can punish the player for many battles. | floor row |
| P0 naive/thematic | B | `GCV-PIN-B-FIRST-SPECIALIST` | Ramza flexible, Knight or Archer, White or Black Mage, Chemist, Squire/first specialist | active specialist actions before premium exports | First specialists must feel useful before their best supports. | floor row |
| P0 naive/thematic | C | `GCV-PIN-C-VERTICAL-MAIL`, `GCV-PIN-C-CASTER-STATUS` | Ramza hybrid, one first specialist holdover, one midgame branch, one healer/support, one flex | mostly on-job actions, shallow secondaries | Wrong early branch remains recoverable through midgame identity. | provisional row only |
| P0 naive/thematic | D | `GCV-PIN-D-SPREAD-RANGED`, `GCV-PIN-D-LONG-FIGHT` | Ramza, one advanced or favorite job, two older specialists, one support/flex | one or two RSM pieces, not optimized | Thematic parties become strong without mandatory Samurai/Ninja/Time routing. | stress row |
| P0 naive/thematic | E | `GCV-PIN-E-BOSS-RESIST` | final Ramza, old favorite specialist, healer/control, late job optional, flex | late tools possible but not required | Necromancer, Vanguard, and final Ramza cannot be required for ordinary viability. | stress row |
| P1 balanced | 0 | `GCV-PIN-00-RAW-MIXED` | Ramza/Squire, Squire frontline, Squire utility, Chemist, Squire trainee | starter attacks, Items | Baseline early FFT party. | provisional row only |
| P1 balanced | A | `GCV-PIN-A-PHYSICAL`, `GCV-PIN-A-PROGRESSION-RUSH` | Ramza, Chemist, physical trainee, caster trainee, ranged/utility trainee | fixed-JP routing, Move +1 useful, basic Items | Progression breadth is tested without a support tax. | stress row |
| P1 balanced | B | `GCV-PIN-B-FIRST-SPECIALIST` | Ramza, Knight, Archer, White Mage, Black Mage | one active action line each, few exports | Archer and both mages must all look like rational active jobs. | stress row |
| P1 balanced | C | `GCV-PIN-C-MITIGATION-STACK`, `GCV-PIN-C-CASTER-STATUS` | Ramza, Knight/Monk/Dragoon or Geomancer, Archer/Thief/Orator, White/Time/Mystic, Black/Summoner | early secondaries, one modest RSM | Haste, Chakra, and Items cannot cover every weakness at once. | stress row |
| P1 balanced | D | `GCV-PIN-D-CLUSTER-AREA`, `GCV-PIN-D-SPREAD-RANGED` | Ramza, Samurai or Ninja, Summoner/Time, performer optional, older specialist | one high-value RSM per route | One support or movement cannot become the obvious default. | stress row |
| P1 balanced | E | `GCV-PIN-E-BOSS-RESIST`, `GCV-PIN-E-UNDEAD-CORPSE` | final Ramza, one late job optional, two older specialists, one flex | late tools plus specialist anchors | Final party should still invite older active jobs. | stress row |
| P5 optimizer rush | 0 | `GCV-PIN-00-RAW-MIXED` | same as P1, optimized actions | earliest JP-efficient starter actions | Safe JP farming cannot break raw start. | stress row |
| P5 optimizer rush | A | `GCV-PIN-A-PROGRESSION-RUSH` | Ramza/Squire, multiple Squire/Chemist routes, early trainees | fixed-JP optimizer route, Move +1, basic Items | Fixed-JP progression rush is the pacing line-mover. | stress row |
| P5 optimizer rush | B | `GCV-PIN-B-FIRST-SPECIALIST`, `GCV-PIN-B-MONK-SECONDARY` | Ramza, Knight shell, Archer/Thief utility, Monk or caster trainee, Chemist/White | early armor/shield, Concentration/Brawler candidates, early Chakra/Revive candidates | Watch early physical full-package formation. | stress row |
| P5 optimizer rush | C | `GCV-PIN-C-MITIGATION-STACK`, `GCV-PIN-C-CASTER-STATUS` | Ramza hybrid, Knight/Monk/Dragoon, Time/Mystic, Geomancer/Orator, Black/Summoner | Haste/Slow, Chakra, Throw Item, early MP tools, equipment unlock candidates | First likely over-compression band. | stress row |
| P5 optimizer rush | D | `GCV-PIN-D-CLUSTER-AREA`, `GCV-PIN-D-SPREAD-RANGED`, `GCV-PIN-D-LONG-FIGHT` | Ramza, Ninja/Samurai, Time/Summoner, performer or White, Dragoon/old specialist | Dual Wield, Doublehand, Swiftspell, Teleport, Move +3, Shirahadori/Vanish candidates | Highest cross-job convergence band. | stress row |
| P5 optimizer rush | E | `GCV-PIN-E-BOSS-RESIST`, `GCV-PIN-E-UNDEAD-CORPSE` | final Ramza, Vanguard, Necromancer, Ninja/Samurai, Time/Summoner or healer | late vanguard, corpse/dark-state, top RSM, premium equipment checks | Late jobs can be top-tier, not mandatory. | stress row |
| P2 physical-heavy | B | `GCV-PIN-B-FIRST-SPECIALIST`, `GCV-PIN-B-MONK-SECONDARY` | Ramza, Knight, Archer, Monk trainee, Chemist/White | armor, bows, fists, Items | Physical party needs sustain without deleting caster routes. | stress row |
| P2 physical-heavy | C | `GCV-PIN-C-VERTICAL-MAIL`, `GCV-PIN-C-MITIGATION-STACK` | Ramza, Knight/Dragoon, Archer/Thief, Monk/Geomancer, Chemist/White | thrust/missile/crush mix, shallow control | Weapon families should matter against armor/map texture. | stress row |
| P2 physical-heavy | D | `GCV-PIN-D-SPREAD-RANGED` | Ramza, Ninja, Samurai or Dragoon, Archer, support flex | one premium physical RSM, one support RSM | Avoid one physical support stack solving all weapons. | stress row |
| P2 physical-heavy | E | `GCV-PIN-E-BOSS-RESIST` | final Ramza, Vanguard optional, Samurai/Ninja, Dragoon/Archer, healer/control | premium weapons, bounded mitigation | Physical-heavy stays viable without reviving sword-only dominance. | stress row |
| P3 caster-heavy | B | `GCV-PIN-B-FIRST-SPECIALIST` | Ramza, White Mage, Black Mage, Chemist, Archer/Knight protector | basic spells, Items, protection | Caster route must pay CT/MP/fragility from first specialist band. | stress row |
| P3 caster-heavy | C | `GCV-PIN-C-CASTER-STATUS`, `GCV-PIN-C-MITIGATION-STACK` | Ramza, White/Time, Black/Summoner, Mystic, Orator/Geomancer | Faith, Haste/Slow, status, MP tools | Caster economy cannot erase MP, CT, Faith, and fragility together. | stress row |
| P3 caster-heavy | D | `GCV-PIN-D-CLUSTER-AREA`, `GCV-PIN-D-SPREAD-RANGED` | Ramza, Summoner, Time, Mystic or Necromancer trainee, Bard/White | area, tempo, status, sustain | Area power must be paid by CT/MP/position/interruption. | stress row |
| P3 caster-heavy | E | `GCV-PIN-E-BOSS-RESIST`, `GCV-PIN-E-UNDEAD-CORPSE` | final Ramza, Summoner/Time, Necromancer, White/Mystic, physical protector | premium spells, dark-state, revive cleanup | Boss resistance and corpse rules must stop one-note caster dominance. | stress row |
| P4 control/sustain | C | `GCV-PIN-C-MITIGATION-STACK`, `GCV-PIN-C-CASTER-STATUS` | Ramza, White/Time, Mystic/Orator, Knight/Geomancer, Chemist/Monk | Protect/Shell, status, Items, Chakra candidates | Control wins through planning, not immunity or denial lock. | stress row |
| P4 control/sustain | D | `GCV-PIN-D-LONG-FIGHT`, `GCV-PIN-D-SPREAD-RANGED` | Ramza, Time, White, Bard/Dancer or Mystic, durable physical | sustain, CT control, mitigation, interruption exposure | Long-fight tools must not erase repeated mistakes. | stress row |
| P4 control/sustain | E | `GCV-PIN-E-BOSS-RESIST` | final Ramza, Vanguard, White/Time, Necromancer/Mystic, older specialist | protection, revive, dark-state, boss-safe control | Defensive late party strong but still has offense and status limits. | stress row |
| P6 performer parity | D | `GCV-PIN-D-CLUSTER-AREA`, `GCV-PIN-D-SPREAD-RANGED` | Ramza, Bard or Dancer, Summoner/Time, physical specialist, healer/flex | performance channel, shared RSM, interruption coverage | Performer slot should be attractive without becoming mandatory. | stress row |
| P6 performer parity | E | `GCV-PIN-E-BOSS-RESIST`, `GCV-PIN-E-UNDEAD-CORPSE` | final Ramza, Bard or Dancer, late job optional, old specialist, healer/control | identical Bard/Dancer RSM access, different actions | Gender-restricted actions differ, but build access parity must hold. | stress row |

## A3 GPT First-Pass Ceiling Stack

Claude must build an independent ceiling stack before any A3 ceiling row becomes accepted.

| Band | GPT strongest plausible five-unit stack | Reachability | Main breakpoints to reconcile |
| --- | --- | --- | --- |
| 0 | Ramza/Squire, Squire melee, Squire ranged/utility, Chemist, Squire trainee. | floor-realistic | Auto-Potion too early, safe JP loops, Ramza self-buff snowball, Chemist throw-range safety |
| A | Ramza/Squire, Chemist, one fixed-JP optimizer route, one early specialist reached through ordinary or optimizer routing, one trainee. | optimizer-realistic | first-specialist acceleration under fixed JP, early Auto-Potion placement |
| B | Ramza flexible, Knight body, Monk primary sustain/damage engine, White Mage sustain, Black Mage offense; Archer pressure remains an alternate ceiling. | optimizer-realistic if first specialist unlocks are ordinary | Knight-body durability plus Monk Chakra/Punch Art compression, early Archer/White compression |
| C | Ramza hybrid, Knight/Monk or Dragoon frontline, Time/Mystic controller, Summoner/Black area, Orator/Geomancer utility. | optimizer-realistic | mitigation stack, Haste/Slow, Chakra/Items, Belief/Oil setup seeds |
| D | Ramza chapter hybrid, Ninja or Samurai physical engine, Time/Summoner caster, Bard/Dancer global support, Dragoon/Archer/White flex. | optimizer-realistic late, grind-only if forced into Band C | Dual Wield/Doublehand/Swiftspell/Quick/Teleport/Move +3/Shirahadori/Vanish convergence, Dancer global stat-down |
| E | final Ramza, Vanguard frontline, Necromancer state control, Ninja/Samurai engine, Time/Summoner/White/Bard flex. | optimizer-realistic late | late replacement overreach, corpse-state dominance, Reraise/Arise sustain loops, premium equipment and Knight Sword access |

GPT ceiling hypotheses for Claude review:

1. Band C is still the first truly dangerous campaign compression point.
2. P5 Band B/C should be the first W4/T2.1 populated incidence target after W3 producers exist.
3. P3 Band C/D should be the first caster economy convergence target.
4. P6 Band D/E should be the performer parity and global-value target.
5. P1/P2 Band E should decide whether older specialists keep rational final-party slots.

## Ramza Dominance Rows

| Band | Protected comparison | W2 expectation | Later proof |
| --- | --- | --- | --- |
| 0/A | Squire and Chemist | Ramza can be flexible, but should not be the only good starter body or item user. | A1 floor rows plus W3 Ramza chapter values |
| B | Knight, Archer, White Mage, Black Mage | Ramza supports or flexes; each first specialist has a clearer lane in its own row. | W5 real-roster comparison |
| C | Time Mage, Mystic, Geomancer, Dragoon, Orator | Ramza bridges lanes but does not become best control, best frontline, and best caster at once. | W4 incidence plus W5 five-metric comparison |
| D | Samurai, Ninja, Summoner, Bard/Dancer | Ramza remains strong but still borrows identity through planned secondary/RSM choices. | A3 dual-independent ceiling reconciliation |
| E | Vanguard, Necromancer, older specialists | final Ramza can be top-tier broad, but does not make late jobs or older specialists ornamental. | W5 Band E and W6 adjustment report |

## Handoff To Later Work

W2 produces rows, not conclusions.

The next producer work is W3:

- exact A2 unlock/JP/equipment pacing ledger;
- concrete physical/foundation action values;
- candidate reaction/support/movement values.

After W3, W4 should populate T2.1 incidence using the party rows above. W5 should run real-roster
F5 using these same rows plus formula-v1 data.

## Claude Review Request

Claude should review:

- whether the pinned synthetic A5 anchors are specific enough for W2 planning;
- whether A1 covers P0/P1/P5 across bands 0-E and the P2/P3/P4/P6 wall tests;
- whether the A3 GPT ceiling stack misses any stronger plausible stack;
- whether Ramza dominance rows preserve the accepted specialist-protection rule;
- whether any W2 row makes a premature pass/fail claim that should stay deferred.
