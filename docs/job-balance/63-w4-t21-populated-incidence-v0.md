# W4 T2.1 Populated Incidence V0

Status: Accepted by Claude review
Date: 2026-06-22
Depends on:
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/47-campaign-validation-readiness-v0.md`
- `docs/job-balance/50-campaign-journey-bundle-v0.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/job-balance/52-squire-chemist-concrete-v0.md`
- `docs/job-balance/53-knight-archer-concrete-v0.md`
- `docs/job-balance/54-monk-thief-concrete-v0.md`
- `docs/job-balance/55-orator-dragoon-concrete-v0.md`
- `docs/job-balance/56-samurai-ninja-concrete-v0.md`
- `docs/job-balance/57-vanguard-ramza-concrete-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/job-balance/59-equipment-availability-timing-v0.md`
- `docs/job-balance/60-prerequisite-tree-and-jp-cost-draft-v0.md`
- `docs/job-balance/61-jp-boost-removal-decision-v0.md`
- `docs/job-balance/62-w4-t21-populated-incidence-plan-v0.md`
- `work/gpt-campaign-journey-bundle-v0.json`
- `work/gpt-physical-foundation-rsm-concrete-v0.json`
- `work/gpt-equipment-availability-timing-v0.json`
- `work/gpt-prerequisite-tree-jp-cost-v0.json`
- `work/gpt-campaign-validation-readiness-v0.json`

## Purpose

This document populates the W4/T2.1 incidence matrix defined in doc 62.

The unit of analysis is:

```text
band x party_row x progression_mode
```

Each row assumes Ramza plus four generics. It checks practical online access to secondaries,
reactions, supports, movement pieces, and equipment exports under the accepted prerequisite tree,
JP envelope, slot model, and equipment timing.

This is a structural incidence pass. It does not prove final balance. Its job is to decide which
packages are online, which pieces are filtered out, and which rows must feed W5/F5.

## Source Pins

| Source | SHA-256 |
| --- | --- |
| `work/gpt-campaign-journey-bundle-v0.json` | `ac78efbcc9a90bfd788eb6712fd7d198fcebfea9d4675599b4444571d0e95235` |
| `work/gpt-physical-foundation-rsm-concrete-v0.json` | `d947ca5a6d7751f209077b7b6cde69ff71c4b5c2933bd67f00916f54d83c2fd0` |
| `work/gpt-equipment-availability-timing-v0.json` | `abf5fa10d6ca92199c13db9e5b4d5042c1e1158c3ff477a40cb6d99f4d0d4ff4` |
| `work/gpt-prerequisite-tree-jp-cost-v0.json` | `1774c2ca9f591eee04f02320b309a07def5f281962b2530e0cc1e77ad2bfd944` |
| `work/gpt-campaign-validation-readiness-v0.json` | `284f83a28b30cb83501013c6b725e2aa18bbcdc154a53e7700f0bb11f8c7002e` |

All online conclusions that depend on the provisional JP envelope or doc 59 equipment availability
are labeled `provisional_online_pending_T1`.

## Online Filter Recap

A piece is counted as online only if all relevant gates pass:

- donor job can be unlocked by the row's band and route mode;
- donor job reaches the listed depth;
- donor JP can pay the listed JP cost under doc 62's per-donor envelope;
- the build respects one reaction, one support, and one movement slot;
- one unit has only one secondary action set;
- equipment exports satisfy doc 59 practical-online timing;
- placeholder caster, performer, Necromancer, and Ramza R/S/M rows are treated as pacing slots, not
  final effect acceptance.

## Protected Convergence Incidence

| Piece | Slot | First practical online read | Mandatory-feeling | W4 read | W5/W6 routing |
| --- | --- | --- | --- | --- | --- |
| `Auto-Potion` | Reaction | B optimizer/grind by JP; C ordinary by intended pacing | high | Starter Chemist can pay it before the C target under focused routing, but 30 HP Potion-only, survivor-only, 1/round keeps it from a floor skip. | W5 sustain rows for P5-B/C and P4-C; W6 cost watch if it appears in unrelated B optimizer rows. |
| `Equip Armor` | Support | B optimizer/grind by JP; C ordinary by intended pacing | high | Strong fragility patch, but competes with every other support and does not grant mitigation math by itself. | W5 P5-C mitigation and P4-C safety. |
| `Equip Shield` | Support | B Knight-primary technical; C cross-job pressure | high | Lv3/600 is payable on a Band B Knight-primary donor, but a Knight already has native shield access. The real cross-job mitigation pressure begins in C, when non-Knight units can plan around the learned support without consuming the whole ordinary route. | W5 shield/evasion and mitigation stack rows; W6 candidate only after F5 confirms cross-job dominance. |
| `Concentration` | Support | B grind, C optimizer, D ordinary | high | High false-choice risk for weapon users facing evasion, but scoped away from spells/status and competes with Bow Mastery, Brawler, Doublehand, Dual Wield, equipment exports. | W5 P2-D/E physical accuracy and Archer active viability. |
| `Brawler` | Support | B optimizer/grind pressure; C ordinary | watch | Real unarmed route, but fists only and cannot stack with `Martial Discipline`, Dual Wield, Doublehand, or equipment exports. | W5 P2-C/D unarmed vs weapons; W6 cost watch if B optimizer/grind becomes too attractive. |
| `Halve MP` | Support | C optimizer/grind or D ordinary | high | Strong caster economy route. It competes directly with Swiftspell, Summon Focus, Elemental Focus, Arcane Strength, and defensive caster supports. | W5 P3-C/D caster economy and Belief/Oil rows. |
| `Swiftspell` | Support | C optimizer/grind or D ordinary | high | Biggest caster support convergence risk, especially with Time secondary and premium spells. Support-slot conflict prevents stacking with Halve MP or Summon Focus. | W5 P3-D/E and Time systemic compression. |
| `Equip Guns` | Support | D export; C native/job-specific only if proven | watch | PA-independent damage patch is held by doc 59 until D for exports. C Orator/Chemist native gun texture can exist without broad export. | W5 P3/P4 ranged safety and equipment breakpoint rows. |
| `Equip Polearms` | Support | C ordinary | watch | Spear export is early enough to matter, but Jump identity remains native Dragoon and support slot prevents stacking with other engines. | W5 P2-C/D weapon-family comparison. |
| `Equip Katana` | Support | D optimizer/grind or E ordinary | watch | Meaningful Brave-linked route; risk is Samurai becoming a support stop. | W5 P2-D/E Samurai active vs katana export. |
| `Doublehand` | Support | D optimizer/grind or E ordinary | high | 1.80x single-weapon engine is structurally dangerous but cannot stack with Dual Wield, Equip Katana, Equip Knight Swords, Brawler, or accuracy supports. | W5 P2-D/E physical convergence. |
| `Dual Wield` | Support | D optimizer/grind or E ordinary | high | Highest physical engine risk. Learned support cannot affect fists, Throw, Iaido, spells, reactions, specials, or Doublehand stacks. Native Ninja dual remains separate. | W5 P2-D/E and P5-D/E ceilings. |
| `Teleport` | Movement | C optimizer/grind or D ordinary | high | Movement default risk for casters/controllers; direct competitor with Move +3, Ignore Elevation, Ignore Terrain, Fly, and role movement. | W5 mobility convergence rows. |
| `Move +3` | Movement | D optimizer/grind or E ordinary | high | Late raw reach risks becoming universal but has no terrain/elevation bypass. | W5 mobility convergence rows. |
| `Equip Knight Swords` | Support | E optimizer; E ordinary only on routed Vanguard/final physical path | watch | Premium sword access is optional and cuttable. Off-job support cost prevents stacking with Doublehand or Dual Wield; native Vanguard/Ramza rows still require F5. | W5 P5-E/P2-E/Ramza/Vanguard late replacement. |

No protected piece is currently marked `fail` at W4. The main reason is slot friction: the same
support slot blocks most attractive illegal stacks. The high rows are still mandatory W5 inputs
because they may majority-dominate within their own axis.

## Party Incidence Matrix

Legend:

- `Prov` means `provisional_online_pending_T1`;
- `R/S/M` lists online reaction, support, movement pressure worth counting;
- `Filtered` lists tempting pieces that the row does not count as online.

| ID | Band | Party | Mode | Active jobs | Expected secondaries | Online R/S/M and equipment | Filtered | Mandatory flag | W5 handoff |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| W4-P0-0-O | 0 | P0 | ordinary | Ramza/Squire, 3 Squires, Chemist | Items on Chemist only | starter actions; no learned R/S/M | `Move +1`, `Throw Item`, `Auto-Potion` | none | floor-start |
| W4-P0-A-O | A | P0 | ordinary | Ramza/Squire, Chemist, Squire physical, caster trainee, ranged trainee | Items, Fundaments | `Grit`, `Move +1` Prov on Squire route | `Throw Item` not ordinary-payable; first-specialist R/S/M | none | floor-A |
| W4-P0-B-O | B | P0 | ordinary | Ramza, Knight or Archer, White or Black, Chemist, Squire/first specialist | Items, first spells, basic Arts/Aim | `Throw Item`, `Basic Training`, `Parry`/`Arrow Guard`, `Jump +1` Prov | `Auto-Potion`, `Equip Armor`, `Concentration`, `Brawler` | none | floor-B-first-specialists |
| W4-P0-C-O | C | P0 | ordinary | Ramza hybrid, older specialist, mid branch, healer/support, flex | Items, White/Black, Time/Mystic/Monk/Dragoon as routed | `Auto-Potion`, `Item Lore`, `Equip Armor`, `Equip Shield`, `Bow Mastery`, `Brawler`, `Move +2`, role movement Prov | `Concentration` ordinary short, `Swiftspell`, `Teleport`, advanced physical engines | watch | floor-C-recoverable-branch |
| W4-P0-D-O | D | P0 | ordinary | Ramza, one advanced/favorite, two older specialists, support/flex | one strong secondary, not optimized | D ordinary supports: `Concentration`, `Halve MP`, `Swiftspell`, `Teleport`; one premium physical route at most Prov | Dual Wield/Doublehand together; multi-support caster stack; Samurai/Ninja full packages | watch | floor-D-thematic |
| W4-P0-E-O | E | P0 | ordinary | final Ramza, old specialist, healer/control, late job optional, flex | late secondary possible | late R/S/M available one route at a time; `Move +3`/Teleport/advanced supports compete Prov | any claim that Vanguard/Necromancer/final Ramza are required | watch | floor-E-older-specialists |
| W4-P1-0-O | 0 | P1 | ordinary | Ramza/Squire, Squire frontline, Squire utility, Chemist, trainee | Items, Fundaments | none beyond starter actions | all R/S/M | none | baseline-start |
| W4-P1-A-O | A | P1 | ordinary | Ramza, Chemist, physical trainee, caster trainee, ranged trainee | Items and starter actions | `Grit`, `Move +1` Prov | `Throw Item` ordinary gap; early specialist R/S/M | none | baseline-A |
| W4-P1-B-O | B | P1 | ordinary | Ramza, Knight, Archer, White Mage, Black Mage | Items/spells/Aim/Arts | `Throw Item`, `Parry`, `Arrow Guard`, `Jump +1`, `Basic Training` Prov | equipment exports, Auto-Potion, Concentration | none | baseline-B-specialists |
| W4-P1-C-O | C | P1 | ordinary | Ramza, Knight/Monk/Dragoon, Archer/Thief/Orator, White/Time/Mystic, Black/Summoner | Items, Chakra or Time/Mystic, first summons | one modest R/S/M per unit: armor/shield/bow/polearm/item/movement routes Prov | Halve MP plus Swiftspell stack; Equip Armor plus Equip Shield off-job stack | watch | baseline-C-branching |
| W4-P1-D-O | D | P1 | ordinary | Ramza, Samurai or Ninja, Summoner/Time, performer optional, older specialist | Iaido/Throw/Time/Summon/Items | one high-value R/S/M per route: Swiftspell or Halve MP or Doublehand/Dual Wield or Teleport/Move +3 Prov | two premium supports on same unit; full caster economy stack | watch | baseline-D-advanced |
| W4-P1-E-O | E | P1 | ordinary | final Ramza, one late job optional, two older specialists, flex | late secondary plus specialist action sets | late jobs and old specialists all have at least one legal R/S/M route Prov | Equip Knight Swords plus Doublehand/Dual Wield learned stack | watch | baseline-E-final |
| W4-P5-0-O | 0 | P5 | ordinary | P1 start with optimized actions | Items on Chemist | no learned R/S/M | all protected rows | none | optimizer-start-floor |
| W4-P5-0-P | 0 | P5 | optimizer | P1 start with JP focus | Items, Fundaments | starter actions only; `Move +1` still depth-edge | `Move +1` not counted before Lv2 threshold is secure | none | optimizer-start-edge |
| W4-P5-0-G | 0 | P5 | grind_heavy | P1 start overgrinding starter donors | Items, Fundaments | `Move +1` grind-edge Prov | any combat engine | watch | grind-to-break-start |
| W4-P5-A-O | A | P5 | ordinary | Ramza/Squire, Chemist, Squire routes, trainees | Items/Fundaments | `Grit`, `Move +1` Prov | `Throw Item` just short for ordinary ceiling | none | A-progression-rush-floor |
| W4-P5-A-P | A | P5 | optimizer | Ramza/Squire, Chemist primary, first specialist trainee | Items/Fundaments | `Throw Item`, `Grit`, `Move +1` Prov | Auto-Potion, first-specialist deep R/S/M | watch | A-progression-rush-ceiling |
| W4-P5-A-G | A | P5 | grind_heavy | Squire/Chemist rush plus first specialist entry | Items/Fundaments | `Throw Item`, `Basic Training` edge, early Lv3 starter pressure Prov | Auto-Potion if C target is enforced; otherwise B/C sustain risk | watch | A-grind-to-break |
| W4-P5-B-O | B | P5 | ordinary | Ramza, Knight shell, Archer/Thief utility, Monk or caster trainee, Chemist/White | Items, Aim, Arts, first spells | first-specialist R/S/M: `Parry`, `Arrow Guard`, `Counter`, `Jump +1`, `Throw Item` Prov | Auto-Potion, Chakra full package, armor/shield exports | none | B-ordinary-full-package-check |
| W4-P5-B-P | B | P5 | optimizer | Ramza, Knight body, Monk route, White, Black/Archer | Monk or Items secondary, White/Black | `Auto-Potion` JP-pressure, `Brawler` not practical except grind, first defensive reactions Prov | Equip Armor/Shield blocked by equipment band C; Concentration Lv4 short | watch | B-optimizer-physical-package |
| W4-P5-B-G | B | P5 | grind_heavy | Knight/Monk/Archer/Chemist concentrated route | Monk secondary, Items | `Auto-Potion`, `Brawler` grind pressure, Lv3 first-specialist supports Prov | C equipment exports still blocked by doc59 | high | B-grind-to-break |
| W4-P5-C-O | C | P5 | ordinary | Ramza hybrid, Knight/Monk/Dragoon, Time/Mystic, Geomancer/Orator, Black/Summoner | Chakra/Items/Time/Mystic/Summon | C ordinary exports: armor, shield, bow, polearm, Brawler, Item Lore, Move +2, Manafont, role movements Prov | Concentration/Swiftspell/Teleport/Halve MP ordinary short where Lv4 is required | watch | C-compression-ordinary |
| W4-P5-C-P | C | P5 | optimizer | Ramza hybrid, mitigation frontline, Time/Mystic controller, Summoner/Black, Orator/Geomancer | Time or Mystic, Items or Chakra, Summon | Lv4 optimizer pressure: Concentration, Halve MP, Swiftspell, Teleport; mitigation supports compete Prov | support stacks: Halve MP+Swiftspell; Equip Armor+Equip Shield off-job | high | C-mitigation-caster-ceiling |
| W4-P5-C-G | C | P5 | grind_heavy | C optimizer plus deeper caster/physical donors | same as C optimizer | C grind can force many D-target pieces early by JP: Swiftspell, Teleport, Halve MP, Concentration Prov | Samurai/Ninja/Bard/Dancer job prereqs remain D-blocked; no Dual Wield/Doublehand | high | C-grind-to-break |
| W4-P5-D-O | D | P5 | ordinary | Ramza, Ninja/Samurai, Time/Summoner, performer/White, Dragoon/old specialist | Iaido/Throw/Time/Summon/Items | D ordinary supports/movement: Swiftspell, Halve MP, Teleport, Concentration; active Ninja/Samurai online one route at a time Prov | learned Dual Wield/Doublehand only if route-load proves deep donor; no learned stack | high | D-advanced-ordinary |
| W4-P5-D-P | D | P5 | optimizer | Ramza, Ninja/Samurai engine, Time/Summoner caster, performer, Dragoon/Archer/White flex | Iaido/Throw/Time/Summon | learned Dual Wield or Doublehand, Move +3 or Teleport, performer R/S/M, caster support one at a time Prov | Dual Wield+Doublehand; Teleport+Move +3; Halve MP+Swiftspell | high | D-convergence-ceiling |
| W4-P5-D-G | D | P5 | grind_heavy | D optimizer with multiple advanced donors | premium secondary routing | multiple D engines can be bought across different units, but each unit remains slot-limited Prov | E jobs, Equip Knight Swords, Necromancer/Vanguard | high | D-grind-convergence |
| W4-P5-E-O | E | P5 | ordinary | final Ramza, Vanguard/Necromancer optional, Ninja/Samurai, Time/Summoner/healer | late secondary routing | older specialist and one late job route online; premium movement/support one per unit Prov | premium learned sword stack if requiring multiple supports | high | E-late-ordinary |
| W4-P5-E-P | E | P5 | optimizer | final Ramza, Vanguard, Necromancer, Ninja/Samurai, Time/Summoner/White/Bard flex | late action sets | Equip Knight Swords, Deathcraft, Swiftspell, Dual Wield/Doublehand, Move +3/Teleport/Fly all compete by slot Prov | any single unit carrying more than one support/movement; EKS+Doublehand learned stack | high | E-late-ceiling |
| W4-P5-E-G | E | P5 | grind_heavy | E optimizer with mastery donors | late action sets | multiple top-tier routes across party; still one R/S/M per unit Prov | no new equipment, no Gil tuning, no formula constants | high | E-grind-ceiling |
| W4-P2-B-P | B | P2 | optimizer | Ramza, Knight, Archer, Monk trainee, Chemist/White | Items or Monk | first-specialist defenses and Jump +1 Prov | armor/shield exports, Brawler ordinary, Concentration | none | physical-B-foundation |
| W4-P2-C-P | C | P2 | optimizer | Ramza, Knight/Dragoon, Archer/Thief, Monk/Geomancer, Chemist/White | Jump, Steal, Items/Chakra | Equip Armor/Shield/Bow/Polearms, Brawler, Move +2, Lifefont Prov | Concentration plus Brawler same unit; armor+shield off-job | watch | physical-C-weapon-families |
| W4-P2-D-P | D | P2 | optimizer | Ramza, Ninja, Samurai/Dragoon, Archer, support flex | Throw/Iaido/Jump/Items | Dual Wield or Doublehand, Concentration, Move +3 or Teleport, Equip Katana/Polearms Prov | Dual Wield+Doublehand; EKS; universal Attack Boost unassigned | high | physical-D-ceiling |
| W4-P2-E-P | E | P2 | optimizer | final Ramza, Vanguard optional, Samurai/Ninja, Dragoon/Archer, healer/control | late physical plus support | Equip Knight Swords, Dual Wield, Doublehand, Move +3, Teleport, Vanguard R/S/M compete Prov | learned premium sword plus other support engine | high | physical-E-ceiling |
| W4-P3-B-P | B | P3 | optimizer | Ramza, White, Black, Chemist, Archer/Knight protector | Items, White/Black | Throw Item, first reactions, Jump +1 Prov | Halve MP, Swiftspell, Manafont, Summon Focus | none | caster-B-floor-pressure |
| W4-P3-C-P | C | P3 | optimizer | Ramza, White/Time, Black/Summoner, Mystic, Orator/Geomancer | Time/Mystic/Summon/Items | Halve MP or Manafont or Summon Focus or Elemental Focus; Belief/Oil setup pieces Prov | Halve MP+Swiftspell+Summon Focus same unit; repeated premium summons by MP | high | caster-C-economy-and-belief-oil |
| W4-P3-D-P | D | P3 | optimizer | Ramza, Summoner, Time, Mystic/Necromancer trainee, Bard/White | Time, Mystic, Summon, White | Swiftspell, Teleport, Halve MP, Grand Invocation, performance support one route at a time Prov | full caster economy stack; Quick recursion | high | caster-D-ceiling |
| W4-P3-E-P | E | P3 | optimizer | final Ramza, Summoner/Time, Necromancer, White/Mystic, protector | Necromancer, Time, Summon | Necromancer R/S/M, Swiftspell, Halve MP, Shadow Step/Teleport compete Prov | dark-state plus unlimited revive loop; Belief/Oil as unbounded default | high | caster-E-late |
| W4-P4-C-P | C | P4 | optimizer | Ramza, White/Time, Mystic/Orator, Knight/Geomancer, Chemist/Monk | protection, status, Items/Chakra | Auto-Potion, Item Lore, Equip Armor/Shield, Mana Shield, Move +2/Manafont role movement Prov | mitigation multiplication; armor+shield off-job | high | sustain-C-mitigation |
| W4-P4-D-P | D | P4 | optimizer | Ramza, Time, White, Bard/Dancer or Mystic, durable physical | Time/White/performance/status | Teleport, Swiftspell, Halve MP, Performance Mastery, defensive reactions Prov | global performance as mandatory if interrupted value fails; Quick loops | high | sustain-D-long-fight |
| W4-P4-E-P | E | P4 | optimizer | final Ramza, Vanguard, White/Time, Necromancer/Mystic, older specialist | Vanguard/Necro/White/Time | Intervention, Armor Discipline, Death's Door, Shadow Step, late defensive supports Prov | practical immortality; Necromancer required cleanup | high | sustain-E-late-safety |
| W4-P6-D-P | D | P6 | optimizer | Ramza, Bard or Dancer, Summoner/Time, physical specialist, healer/flex | performance plus Time/Summon/Items | Bard/Dancer shared R/S/M parity: Earplugs, Encore, Performance Mastery, Stagecraft, Performance Step Prov | gender-linked R/S/M differences; global performance without interruption cost | high | performer-D-parity |
| W4-P6-E-P | E | P6 | optimizer | final Ramza, Bard or Dancer, late job optional, old specialist, healer/control | performance plus late action set | same shared R/S/M; Fly only E/promotion if needed Prov | Bard/Dancer R/S/M divergence; performer slot mandatory in all parties | high | performer-E-parity |

## Filtered-Out Stack Rules

These are the main false stacks rejected by W4:

| Stack | Why rejected |
| --- | --- |
| `Equip Armor` + `Equip Shield` on an off-job unit | Both are supports. Native Knight/Vanguard equipment is separate, but learned exports cannot occupy two support slots. |
| `Concentration` + `Bow Mastery` | Both are supports; Archer must choose reliability or bow specialization. |
| `Brawler` + `Martial Discipline` | Both are supports; the doc 58 two-support stress row is not legal incidence. |
| `Halve MP` + `Swiftspell` + `Summon Focus` | Same support slot. Caster economy is strong, but each caster chooses one economy/damage support. |
| `Dual Wield` + `Doublehand` | Same support slot and mechanically incompatible. |
| learned `Equip Knight Swords` + `Doublehand` or `Dual Wield` | Same support slot. Native Vanguard/final Ramza access must be tested separately. |
| `Teleport` + `Move +3` | Same movement slot. |
| `Shirahadori` + `Vanish` + `Mana Shield` | Same reaction slot. |
| `Attack Boost` | Unassigned protected stress engine. Not counted online in W4. |
| `Poach`, `Tame`, `Beast Tongue` | Monster/economy route is out of current scope. |
| C export guns | Doc 59 holds learned `Equip Guns` to D unless native/job-specific C rows prove safe. |
| new equipment or Gil tuning | Out of scope by user instruction and doc 59. |

## Mandatory-Feeling Summary

| Band | Result |
| --- | --- |
| 0 | No mandatory R/S/M. Starter actions only. |
| A | No mandatory support. `Move +1` is useful but not a combat solution; `Throw Item` appears in optimizer sustain routes only. |
| B | No floor fail. Optimizer can reach early sustain/first-specialist compression, with `Auto-Potion` as the main watch item. |
| C | First serious pressure band. Armor/shield/item/bow/polearm/unarmed/caster economy pieces all appear, but support-slot conflicts stop one universal answer. |
| D | High convergence band. Physical, caster, and movement packages each have tempting defaults, but they are mutually exclusive on a unit. W5 must decide whether party-level convergence still occurs. |
| E | High late replacement pressure. Final Ramza, Vanguard, Necromancer, Ninja/Samurai, Time/Summoner, and performers all have legal routes. W5 must prove older specialists remain rational. |

## Ceiling Rows For Claude Reconciliation

These rows are the GPT-proposed strongest legal stacks for the dual-independent ceiling process.
Claude should independently build his strongest legal stacks from the same inputs; the stronger legal
stack becomes the W5 ceiling.

| Ceiling ID | Band/party | GPT strongest legal stack | Why legal | Main risk |
| --- | --- | --- | --- | --- |
| CEIL-P5-C-MIT | P5 C optimizer | Ramza C3 hybrid with `Ward`; Knight/Monk heavy frontline using native armor plus `Counter` or `Auto-Potion`; Time/Mystic controller choosing `Halve MP` or `Teleport`; Summoner/Black with `Summon Focus` or `Elemental Focus`; Orator/Geomancer utility. | One support per unit; C equipment exports only; no Swiftspell+Halve MP stack. | Mitigation plus caster setup covers too many axes. |
| CEIL-P5-D-CONV | P5 D optimizer | Ramza C3/C4 bridge; Ninja native dual with `Move +3`; Samurai with Iaido/Doublehand route; Time/Summoner caster with Swiftspell or Teleport; Bard/Dancer or White flex. | Learned Dual Wield and Doublehand are on separate units; caster chooses one support; one movement each. | Late physical plus caster plus movement convergence. |
| CEIL-P5-E-LATE | P5 E optimizer | final Ramza hybrid; Vanguard native premium/frontline; Necromancer dark-state; Ninja/Samurai engine; Time/Summoner/White/Bard flex. | E late jobs legal; Equip Knight Swords only on Vanguard/native route or support-consuming export, not stacked. | Late jobs and Ramza erase older specialists. |
| CEIL-P2-D | P2 D optimizer | Ramza C3, Ninja native dual, Samurai Doublehand/Iaido, Dragoon Jump/spear, Archer with Concentration or Bow Mastery. | Physical supports distributed across units; no two support engines on one unit. | One physical support family may dominate all weapon families. |
| CEIL-P2-E | P2 E optimizer | final Ramza, Vanguard native, Ninja/Dual Wield route, Samurai/Doublehand route, Dragoon/Archer/healer flex. | Premium sword access does not stack with learned engine supports. | Sword or two-hit route becomes the only rational final physical core. |
| CEIL-P3-C | P3 C optimizer | Ramza C3 hybrid, White/Time, Black/Summoner, Mystic, Orator/Geomancer with Belief/Oil setup. | Belief/Oil requires actions and terrain/status; each caster has one support. | Belief/Oil/fire spike and caster economy convergence. |
| CEIL-P3-D | P3 D optimizer | Summoner premium area, Time with Swiftspell or Teleport, Mystic with Halve MP or Manafont, Bard/White sustain, Ramza bridge. | No caster carries multiple economy supports. | Time systemic compression plus area damage. |
| CEIL-P3-E | P3 E optimizer | final Ramza, Summoner/Time, Necromancer, Mystic/White, physical protector. | Necromancer is E gated and state-dependent; no unbounded revive/dark loop counted. | Late caster/state package dominates boss and corpse rows. |
| CEIL-P6-D | P6 D optimizer | Bard or Dancer, Time/Summoner, physical specialist, healer/flex, Ramza. Shared performer R/S/M: Performance Mastery or Stagecraft plus Performance Step. | Bard/Dancer R/S/M parity preserved; performer still interruptible. | Global performance becomes mandatory infrastructure. |
| CEIL-P6-E | P6 E optimizer | final Ramza, Bard or Dancer, one late job, old specialist, healer/control. Fly only if promoted by later proof. | Shared R/S/M remains identical across gender-restricted jobs. | Performer or late job crowds out older specialist. |

## W5 Handoff Rows

| W5 ID | Source rows | Required axes | Risk register |
| --- | --- | --- | --- |
| W5-FLOOR-0A | P0 0/A, P1 0/A | damage, sustain, safety | 2, 10 |
| W5-FLOOR-B | P0/P1 B | first-specialist damage, emergency sustain, safety | 2, 6, 10 |
| W5-P5-B-FULL | P5 B optimizer/grind | damage, sustain, range, durability, control | 2, 6, 10 |
| W5-P5-C-MIT | P5 C optimizer, P4 C optimizer, CEIL-P5-C-MIT | sustain, mitigation, control, safety | 5, 6, 10 |
| W5-P3-C-BELIEF-OIL | P3 C optimizer, CEIL-P3-C | damage, setup cost, control, safety | 1, 4 |
| W5-P3-D-CASTER | P3 D optimizer, CEIL-P3-D | damage, MP economy, CT, mobility | 3, 4, 9 |
| W5-P2-D-PHYS | P2 D optimizer, CEIL-P2-D | damage by weapon family, accuracy, mobility, safety | 7, 8, 9 |
| W5-P5-D-CONV | P5 D optimizer/grind, CEIL-P5-D-CONV | all five axes and majority/Pareto dominance | 3, 4, 7, 9, 12 |
| W5-P6-DE-PARITY | P6 D/E optimizer, CEIL-P6-D/E | sustain/global output, interruption, gender parity, safety | 12 |
| W5-P5-E-LATE | P5 E optimizer/grind, CEIL-P5-E-LATE | late damage, sustain, control, mobility, safety | 7, 8, 11 |
| W5-RAMZA-C4-BREADTH | P1/P2/P3/P4/P5 E rows | per-axis and breadth-as-dominance | 11 |
| W5-EQUIP-BREAKPOINTS | W4 protected equipment rows | damage, defense, timing, native-before-export | 8 |

## W4 Findings

1. No floor row currently fails from a mandatory support, reaction, movement, or equipment export.
2. The first real structural danger is Band B/C equipment/sustain compression: `Equip Shield` is
   technically B on a Knight-primary route but becomes meaningful cross-job pressure in C, while
   optimizer/grind `Auto-Potion` can already pair with first-specialist bodies in B.
3. Band C is the first broad false-choice band, but slot friction prevents a single universal
   package. The W5 question is whether party-level coverage still becomes too complete.
4. Band D has the expected high convergence pressure. `Dual Wield`, `Doublehand`, `Swiftspell`,
   `Halve MP`, `Teleport`, and `Move +3` are not stackable on one unit, but they may dominate their
   axes across a five-unit party.
5. Band E is structurally healthy only if W5 proves late jobs are top-tier choices rather than
   required replacements.
6. No W6 hard adjustment is accepted from W4 alone. W4 creates W6 watch candidates:
   `Equip Shield` cost/depth, `Auto-Potion` cost/timing, `Concentration` depth, `Swiftspell`
   depth/cost, `Teleport` depth/cost, `Dual Wield` cost, `Move +3` cost, and `Equip Knight Swords`
   export scope.

## Claude Review Request

Claude should review:

- whether the online/filter calls follow doc 62's per-donor JP and route-load model;
- whether any row silently assumes illegal support, reaction, movement, or secondary stacking;
- whether the protected convergence incidence table misses any first-online pressure;
- whether the ceiling rows are valid strongest GPT stacks before Claude builds independent stacks;
- whether the W5 handoff rows are sufficient to start the real-roster sweep.
