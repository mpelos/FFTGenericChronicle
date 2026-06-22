# Campaign Artifacts Provisional V0

Status: Accepted for provisional campaign artifact planning
Date: 2026-06-21
Depends on:
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/61-jp-boost-removal-decision-v0.md`

## Purpose

This document instantiates the first provisional campaign artifacts required by
`docs/job-balance/31-campaign-gameplay-validation-v1.md`.

It is not final campaign balance acceptance.

It does not set exact JP costs, exact prerequisites, exact encounter stats, exact equipment timing,
or final skill numbers. It creates a structured first pass so GPT and Claude can reason about
campaign power, floor, ceiling, and detour pressure using the same rows.

## Artifact Status

Supersession note, 2026-06-22: doc 61 removes `JP Boost` from the mod. Historical rows below
that framed a `JP Boost` acceleration risk are retained as provenance, but active W4/W9 planning
must read that risk as fixed-JP ordinary, optimizer, and grind-heavy routing moving deep power into
earlier encounter bands. Active Squire support identity is `Basic Training`, not a JP accelerator.

| Artifact | Status in this document | Final acceptance state |
| --- | --- | --- |
| A1 - Campaign Party Matrix | Provisional qualitative rows. | Requires pinned encounter rows and gate-backed thresholds. |
| A2 - Unlock And JP Pacing Ledger | Provisional band placement, no exact JP. | Requires prerequisite/JP draft and T2/T2.1 incidence. |
| A3 - Five-Unit Stack Sheet | GPT first-pass plus Claude independent stack; reconciliation started. | Requires final row-by-row reconciliation before acceptance as ceiling evidence. |
| A4 - Detour Pressure Report | Provisional risk classification. | Requires T2/T2.1 and relevant gate outputs. |
| A5 - Representative Encounter Row Set | Synthetic placeholders only. | Requires named IVC encounter anchors or pinned synthetic stat blocks. |

## Source Boundaries

The campaign bands are encounter/story difficulty bands from doc 31.

The job-depth overlay is provisional. Treat it as a pacing hypothesis, not final prerequisite data.

Synthetic encounter rows in A5 are placeholders. They can support design discussion, but they cannot
produce final numeric acceptance until converted into named IVC encounters or fully pinned synthetic
stat blocks.

## A1 - Campaign Party Matrix

### Mandatory Floor And Ceiling Rows

| Party | Band | Job-depth overlay | Expected active jobs | Expected build pieces | Floor/ceiling read | Red flags | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| P0 naive/thematic | 0 | Squire/Chemist only, near-zero JP. | Ramza/Squire, 2-3 Squires, 1 Chemist. | Basic attacks, basic item use, cheapest starter actions. | Floor row. Should be playable without route knowledge. | First Aid or item recovery too weak makes raw start frustrating; too strong erases attrition. | Provisional pass if starter tools are useful but bounded. |
| P0 naive/thematic | A | Starter jobs plus visible paths to first specialists. | Ramza/Squire, Chemist, early physical, early caster trainee. | Move +1, basic Items, first cheap Squire/Chemist actions. | Floor row. Player should discover healing/range/magic route naturally. | No hidden progression support or route may punish the player. | Needs future JP envelope. |
| P0 naive/thematic | B | First specialist access. | Knight or Archer, White or Black Mage, Chemist/Squire holdover, Ramza flexible. | Basic specialist actions before strong exports. | Floor row. First specialists should feel better even without optimized RSM. | If active jobs are weak until passives are learned, floor fails. | Needs active-job viability rows. |
| P0 naive/thematic | C | Midgame branch available. | Mixed party with one branch such as Geomancer, Dragoon, Mystic, Orator, or Summoner. | Mostly on-job actions, few cross-job optimizations. | Floor row. Wrong early branch should remain recoverable. | Midgame branch should not require restarting a unit's whole route. | Needs campaign matrix expansion. |
| P0 naive/thematic | D | Advanced jobs visible but not fully mastered. | One advanced job or strong midgame job; older specialists remain. | Some RSM rewards, but not optimized stack. | Floor row. Thematic parties should become stronger, not obsolete. | If Samurai/Ninja/Time rewards are required, floor fails. | Needs A3 comparison. |
| P0 naive/thematic | E | Late rewards available if pursued. | Ramza final plus any mix of old specialists and late jobs. | Late job tools may appear, but not mandatory. | Floor row. Older favorite jobs still have rational uses. | Necromancer/Vanguard/Ramza must not be required for viability. | Needs late rows. |
| P1 balanced | 0 | Squire/Chemist only. | Ramza/Squire, Squire frontline, Squire ranged/utility, Chemist, Squire trainee. | Basic items, basic physical actions. | Baseline reference. | Raw start should not demand perfect item economy. | Provisional pass. |
| P1 balanced | A | Starter plus first unlock direction. | Ramza, Chemist, physical trainee, caster trainee, ranged/utility trainee. | Fixed-JP routing, Move +1 useful, Items reliable. | Baseline reference. | No support tax can become mandatory for normal progression. | Needs JP pacing ledger. |
| P1 balanced | B | First specialists. | Knight, Archer, White Mage, Black Mage, Ramza flexible. | One specialist action line per unit; few exports. | Baseline reference. | Archer must not be a dead-end; White/Chemist both matter. | Needs active viability checks. |
| P1 balanced | C | Midgame branch. | Knight/Monk/Dragoon or Geomancer, Archer/Thief/Orator, White/Time/Mystic, Black/Summoner, Ramza. | Early cross-job secondaries, limited supports. | Baseline reference. | Haste/Chakra/items cannot cover every weakness. | Needs T3/T5/T9/T10 later. |
| P1 balanced | D | Advanced build crafting. | Samurai or Ninja, Summoner/Time, performer optional, older specialist, Ramza. | One high-value RSM per route, not all. | Baseline reference. | One support or movement cannot become default. | Needs T2/T2.1. |
| P1 balanced | E | Late integration. | Ramza final, one late job optional, two old specialists, one flex. | Late tools plus specialist anchors. | Baseline reference. | Old jobs must still appear rationally. | Needs Band E matrix. |
| P5 optimizer rush | 0 | Squire/Chemist only. | Same as P1, but actions chosen for JP route. | Earliest JP-efficient actions. | Ceiling row. | Raw start should not be broken by repetitive safe JP farming. | Needs JP acquisition model. |
| P5 optimizer rush | A | Rush starter exports. | Multiple Squires/Chemists if JP route rewards them. | Fixed-JP optimizer routing, Move +1, basic Items, earliest recovery. | Ceiling row. | Optimizer or grind-heavy fixed-JP routing may accelerate later bands; early Auto-Potion would be dangerous. | W1/W9. |
| P5 optimizer rush | B | Rush first specialist exports. | Knight shell, Archer/Thief utility, Monk or caster trainee, Chemist/White, Ramza. | Early armor/shield, Concentration/Brawler candidates, Chakra/Revive candidates. | Ceiling row. | Deep secondary on shallow chassis; early strong reaction. | W1/W2/W4. |
| P5 optimizer rush | C | Rush midgame control/resource branches. | Knight/Monk/Dragoon, Time/Mystic, Geomancer/Orator, Black/Summoner, Ramza. | Haste/Slow, Chakra, Throw Item, Equip Guns/Bow/Polearms candidates, early MP tools. | Ceiling row. | Time snowball, sustain compression, equipment unlock detours. | W2/W3/W6/W9/W10. |
| P5 optimizer rush | D | Rush advanced RSM. | Ninja/Samurai/Time/Summoner or performer plus Ramza. | Dual Wield, Doublehand, Swiftspell, Teleport, Move +3, Shirahadori/Vanish candidates. | Ceiling row. | Physical/caster/mobility convergence. | W3/W4/W5/W6/W7. |
| P5 optimizer rush | E | Rush late replacements and Ramza final. | Ramza final, Vanguard, Necromancer, Ninja/Samurai, Time/Summoner or healer. | Late vanguard, dark-state, top RSM, premium equipment unlocks. | Ceiling row. | Late jobs become mandatory or erase specialists. | W8/W10. |

### Wall-Test Rows

| Party | Bands emphasized | Expected pressure | Why this row exists | Main gates |
| --- | --- | --- | --- | --- |
| P2 physical-heavy | B-E | Weapon-family coverage, armor response, limited magic. | Proves physical parties can be strong without deleting caster routes. | F5, T2/T2.1, T3, T4, T6xT7. |
| P3 caster-heavy | B-E | CT, MP, Faith, Shell, Reflect, Silence, cloth fragility. | Proves caster parties can be strong without making weapon planning optional. | F4, T5, T8xSR, T9, T10, T11. |
| P4 control/sustain | C-E | Recovery, mitigation, status, equipment pressure, long fights. | Proves control can win without broad immunity or action denial locks. | T3/T3xT5, T4, T5, T6xPS, T8, T10. |
| P6 performer/gender parity | D-E | Bard/Dancer global value, interruption, shared RSM parity. | Proves performance is useful without becoming a mandatory slot or gender advantage. | T11xT5, T3xT5xT11, T2/T2.1. |

## A2 - Unlock And JP Pacing Ledger

Provisional band assignments below are not exact prerequisite or JP costs. They state the earliest
band where a piece should be allowed to matter in ordinary play without overgrind.

| Job | Piece or package | Slot | Intended band | Dangerous off-job users | Required gate | Provisional note |
| --- | --- | --- | --- | --- | --- | --- |
| Squire | Basic utility actions | Action | 0/A | Any early secondary user. | T2/T3 if recovery. | Starter tools should work immediately but not dominate. |
| Squire | `Basic Training` | Support | B/C as Squire action identity | Squire secondary users if it becomes broad damage. | A2/A3/T2.1. | Supersedes the removed `JP Boost` row; Squire/Fundaments-style actions only. |
| Squire | `Move +1` | Movement | A | Most early units. | T2.1. | Healthy early mobility floor, not late default. |
| Chemist | Basic Items/Phoenix Down | Action | 0/A/B | Any low-Faith or emergency-support build. | T3/T3xT5. | Reliable early recovery; cost/range matter. |
| Chemist | `Throw Item` | Support | B/C | Any support unit. | T2.1/T3. | Should be commitment, not free positioning erasure. |
| Chemist | `Auto-Potion` / `Item Lore` | Reaction/Support | C or later | Frontliners, plate users, evasive units. | T3/T3xT5/T2.1. | Too early creates sustain compression. |
| Knight | Core Rend/Challenge tools | Action | B/C | Melee controllers. | T4/T6xT7/T8. | Active Knight should work before armor exports. |
| Knight | `Equip Armor` / `Equip Shield` | Support | C/D | Fragile casters, Ninjas, archers. | T2.1/T6xPS. | High detour pressure; not early patch. |
| Knight | `Parry` | Reaction | B/C | Shield/plate stacks. | T4/T2.1. | Narrow weapon defense only. |
| Archer | Quick/basic shot package | Action | B | Active Archer. | T4/T5. | Archer must feel useful without `Concentration`. |
| Archer | `Concentration` / `Equip Bow` / `Bow Mastery` | Support | C/D | Physical and controller shells. | T2.1/T4/F5. | Biggest risk is broad accuracy support. |
| Monk | Basic Martial Arts | Action | B/C | Physical secondaries. | F5/T3. | Must be strong as active Monk, not all-in-one secondary. |
| Monk | `Chakra` / `Revive` | Action | C/D | Frontline sustain builds. | T3/T3xT5. | Risky proximity sustain, not safe backline recovery. |
| Monk | `Brawler` / `Counter` | Support/Reaction | C/D | Most physical builds if fists overperform. | T2.1/F5/T4. | High value but not default physical route. |
| White Mage | Cure/Protect/Shell basics | Action | B/C | Support secondaries. | T3/T6xPS/F4. | Recovery/protection must retain CT/MP/Faith tradeoffs. |
| White Mage | Wall/Reraise/Holy/support focus | Action/Support | D | Caster and durable hybrid builds. | T3/T6xPS/F4/T2.1. | No mandatory prebuff or generic nuker. |
| Black Mage | Basic elements | Action | B | Caster secondaries. | F4/T5/T9. | Early offense caster foundation. |
| Black Mage | Status/Flare/Elemental Focus | Action/Support | C/D | All casters if broad. | F4/T4/T5/T2.1. | Elements should not collapse into Flare-only play. |
| Time Mage | Haste/Slow basics | Action | B/C | Every party if too efficient. | T5/T2.1; T10 only if action-granting. | Focused tempo, not invisible upkeep. Basic Haste already needs the no-permanent-upkeep check. |
| Time Mage | Stop/Reflect/Quick/Swiftspell/Teleport | Action/Support/Movement | D or later | Most casters, many physicals for Teleport. | T5/T8xSR/T10/T2.1. | Major campaign spike cluster. |
| Mystic | Soft status / anti-caster tools | Action | C | Controllers/casters. | T4/T5/T8. | Mid controller route. |
| Mystic | Faith/MP economy / Halve MP / Mana Shield / Manafont | Action/RSM | C/D | Caster-heavy parties. | F4/T9/T6xPS/T2.1. | Dangerous caster economy convergence. |
| Geomancer | Baseline Geomancy | Action | C | Hybrid secondaries. | T11/F5. | Terrain must be useful without becoming universal ranged control. |
| Geomancer | Terrain movement / Terrain Lore / Attack Boost candidate | Support/Movement | D | Physical builds, map traversal builds. | T2.1/T11/F5. | Attack Boost placement remains global deferred. |
| Thief | Basic steal/knife/speed | Action | B/C | Utility secondaries. | T4/T6xT7/T7. | Steal is tactical disruption, not chore. |
| Thief | Move +2 / Sticky Fingers / Light Fingers | RSM | C/D | Mobility/detour builds. | T2.1/T4. | Move +2 should compete, not dominate. |
| Orator | Speech/gun control | Action | C | Caster and control parties. | T4/T5/T8/F4/F5. | Mid controller with gun fallback. Brave/Faith shifts are formula propagation risks, not only speech flavor. |
| Orator | `Equip Guns` / recruit economy hooks | Support/Action | D | Stat-starved jobs, low-PA/MA units. | T2.1/F5/T8. | Gun identity unlock is high-risk. |
| Dragoon | Baseline Jump/spear | Action | C | Active Dragoon. | T5/T5xT8/F5. | Jump timing and spear identity define job. |
| Dragoon | Equip Polearms / Dragonheart / Ignore Elevation | RSM | D | Physical frontliners and mobility builds. | T2.1/T3/T5xT8/F5. | Strong, but not universal. |
| Summoner | Basic summons/Moogle | Action | C/D | Caster secondaries. | T11/T3/T9. | Area power needs CT/MP/target count. |
| Summoner | Golem/Carbuncle/premium summons/focus | Action/Support | D/E | Caster parties and defense setups. | T6xPS/T8xSR/T11xT5/T9. | No mandatory global upkeep. |
| Samurai | Katana/Iaido baseline | Action | D | Durable physical shells. | F5/T11/T3/T6xPS. | Advanced martial identity. |
| Samurai | Doublehand/Shirahadori/Equip Katana | RSM | D/E | Most physical builds if broad. | T2.1/F5/T4. | High detour pressure. |
| Ninja | Active dual-wield/Throw | Action | D | Active fast physical. | F5/T4/T9. | Strong active shell; Throw bounded. |
| Ninja | Dual Wield/Vanish/Move +3 | RSM | D/E | Most physical and mobility builds. | T2.1/F5/T4/T5xT8. | Highest physical convergence risk. |
| Bard/Dancer | Performance actions | Action | D | Long-fight party slot. | T11xT5/T3xT5xT11. | Strong over time, interruptible. |
| Bard/Dancer | Shared RSM | RSM | D/E | Non-performers if too broad. | T2.1. | Must remain performance-oriented and gender-parity-safe. |
| Necromancer | Dark state/drain/doom | Action | E | Late casters. | T3/T5/T8/T9. | Late reward, not default caster secondary. |
| Necromancer | Corpse/undead package | Action/Support | E | Control parties. | T3xT5xT8/T10 if acting bodies. | Optional sub-kit; non-acting default. |
| Vanguard | Vanguard/protection package | Action | E | Late frontlines. | T6xPS/T6xT7/T8/F5. | Late vanguard, not better everything. |
| Vanguard | Equip Knight Swords / Intervention / Armor Discipline | RSM | E | Every late frontline if too broad. | T2.1/F5/T6xPS/T10. | Cut if sword dominance returns. |
| Ramza | Chapter progression | Unique action/chassis | 0-E by chapter | Always present. | A1/A3/F5 as needed. | Broad, not specialist-dominant before final. |

## A3 - GPT First-Pass Five-Unit Optimizer Stack Sheet

This section reconciles GPT's first adversarial stack read with Claude's independent A3 read.

Shared master insight:

```text
optimizer-realistic stacks are the real design problem;
grind-only stacks are warnings but are partly self-limiting;
fixed-JP routing is the meta-breakpoint because ordinary, optimizer, or grind-heavy paths can make
deep job depth practical earlier than intended.
```

| Band | Strongest reachable optimizer stack, GPT first pass | Reachability | Likely breakpoints | Provisional verdict |
| --- | --- | --- | --- | --- |
| 0 | Ramza/Squire, Squire attackers, one Chemist or item-heavy starter support. | Floor/optimizer mostly same. | Auto-Potion would be the only true Band 0 break if reachable here. Safe JP-farming loops also need watching. | No full-package risk if Auto-Potion is not reachable. Floor must remain smooth. |
| A | Multiple Squire/Chemist routes to acquire `Move +1`, basic Items, early Squire/Chemist utility, and first specialist prerequisites. | Optimizer-realistic. | Fixed-JP progression rush is the line-mover. Auto-Potion in Band A would be a major floor-warping break. | Main risk is campaign acceleration; Auto-Potion should not be ordinary Band A power. |
| B | Durable Knight body carrying Monk damage secondary, Archer range, White/Black support/offense, Ramza flexible. | Optimizer-realistic if first specialists unlock normally. | Knight + Monk secondary can compress damage onto a durable chassis. Early `Brawler`, `Concentration`, `Equip Armor`, `Equip Shield`, or Doublehand would compound it. | Damage compression begins here. Full damage+sustain+revive compression shifts to Band C once Chakra/Revive are available. |
| C | Durable frontline with Protect/Shell/plate/shield/Parry-style stack, Time controller, Summoner/Black area or burst, Orator/Archer utility, Ramza hybrid. | Optimizer-realistic with moderate route knowledge. | Mitigation stack is the scariest low-JP break; Time Haste/Quick and Summoner target count are the next layer. | First high-priority campaign stress row is T6xPS mitigation, not late physical damage. |
| D | Ninja or Samurai physical engine, Time/Summoner caster, Bard/Dancer optional global support, older specialist or Dragoon, Ramza strong hybrid. | Optimizer-realistic if advanced jobs open here; grind-only if reached during Band C encounters. | Dual Wield, Doublehand, Attack Boost/Brawler/Concentration, Swiftspell, Teleport, Move +3, Shirahadori, Vanish, performance global value. | Highest convergence band; must be guarded by T2/T10/T11/T6xPS. |
| E | Ramza final, Vanguard vanguard, Necromancer dark control, Ninja/Samurai physical engine, Time/Summoner/White support caster or Bard. | Optimizer-realistic late. | Equip Knight Swords, Intervention, Aegis, dark-state finishers, corpse state, premium mobility, top physical support stack. | Late stack can be very strong; test shifts to whether older specialists still earn slots. |

### GPT Ceiling Hypotheses To Reconcile With Claude

1. The first dangerous ceiling is Band C, not Band D, because mitigation, Time Mage, Monk/Chemist
   sustain, and early area/control can stack before Samurai/Ninja engines arrive.
2. The first campaign stress row should be mitigation-stack immunity: plate/shield plus
   Protect/Shell and a defensive reaction against ordinary offense.
3. The second campaign stress row should be fixed-JP progression rush: under ordinary, optimizer,
   and grind-heavy routing, does Band C depth become realistic during Band B encounters?
4. The third campaign stress row should be Knight-body plus Monk-secondary full-package pressure.
5. The fourth campaign stress row should be Time action economy: Haste/Quick window versus loop.
6. The fifth campaign stress row should be physical support convergence: Dual Wield, Doublehand,
   Attack Boost, Brawler, Concentration, and premium equipment supports.
7. The most likely Band D break is not raw Ninja alone. It is Ninja/Samurai physical engine plus
   Time Mage mobility/action economy plus one global sustain/protection layer.
8. Vanguard is acceptable only if its protection tools are more formation-local than Time
   Mage/White Mage/Bard global support.
9. Necromancer is acceptable only if dark-state actions are condition-gated and not the default
   answer to bosses or durable enemies.
10. Ramza should be measured as the fifth unit in every stack, not as a separate bonus assumption.

## A4 - Detour Pressure Report

| Job | Detour pressure | Main export risk | Why | Mitigation to validate |
| --- | --- | --- | --- | --- |
| Squire | High early; fixed-JP routing remains Critical as pacing distortion. | `Basic Training`, `Move +1`. | `Basic Training` preserves Squire action identity, while ordinary/optimizer/grind-heavy routing can still move deep job power into earlier encounter bands. Early mobility is broadly attractive. | Basic Training kept narrow; fixed-JP pacing rows; Move +1 outclassed later. |
| Chemist | High. | `Auto-Potion`, `Throw Item`, `Item Lore`. | Reliable sustain is universally useful. | T3/T2.1, item cost/range, no free best-potion loop. |
| Knight | Medium/high. | `Equip Armor`, `Equip Shield`, `Parry`. | Fixes fragility and stacks with mitigation. | T6xPS/T4/T2.1; active Knight must matter. |
| Archer | Medium/high. | `Concentration`, `Equip Bow`. | Accuracy/range can patch many builds. | Bound by weapon/action type; Archer remains best bow shell. |
| Monk | High. | `Brawler`, `Chakra`, `Revive`, `Counter`. | Damage plus sustain plus revive can compress roles. | T2.1/T3/F5; proximity/risk limits. |
| White Mage | Medium. | Protection/revive/support focus. | Healing/protection secondaries are broadly useful. | CT/MP/Faith/range; no upkeep. |
| Black Mage | Medium. | Elemental offense/support focus. | Offensive secondary for casters. | F4/T9; no Flare-only or broad magic tax. |
| Time Mage | Critical. | `Swiftspell`, `Teleport`, `Quick`, Haste. | Action economy and movement are universal. | T5/T10/T2.1; late/high commitment. |
| Mystic | High. | `Halve MP`, `Mana Shield`, Faith/MP tools. | Resource and magic-state control reshape caster parties. | T9/F4/T6xPS/T2.1. |
| Geomancer | Medium, high if Attack Boost lands here. | Terrain movement, `Attack Boost` candidate. | Physical support and map movement can become default. | Keep Attack Boost globally reviewed; T2.1. |
| Thief | Medium. | Move +2, steal utility. | Speed and utility are attractive but target-dependent. | Equipmentless/status-immune counters; T2.1. |
| Orator | High, with a shared-blind-spot formula propagation risk. | Brave/Faith speech, `Equip Guns`. | Brave/Faith shifts can multiply katana, fists, Iaido, magic, healing, and reaction confidence across the formula ecology; guns also patch stat-poor jobs. | F4/F5/T4/T2.1; battle-scoped manipulation; explicit Brave/Faith propagation rows. |
| Dragoon | Medium/high. | `Equip Polearms`, Dragonheart, Ignore Elevation. | Spear reach, reraise, and mobility are broad. | T5xT8/T2.1; Dragoon remains spear home. |
| Summoner | Medium/high. | Area magic, Golem/Carbuncle, Summon Focus. | Large ally-safe effects and protection can dominate. | T11/T9/T6xPS/T8xSR. |
| Samurai | Critical. | `Doublehand`, `Shirahadori`, `Equip Katana`. | Damage and defense exports are iconic and powerful. | T2.1/F5/T4; no broad physical immunity. |
| Ninja | Critical. | `Dual Wield`, `Move +3`, `Vanish`. | Multi-hit, mobility, and untargetability can converge. | T2.1/F5/T5xT8; no default physical route. |
| Bard/Dancer | Medium. | Performance global effects, shared movement if too broad. | Party-wide effects can become default infrastructure. | T11xT5/T2.1; interruption and parity. |
| Necromancer | Medium/high late. | Dark-state secondary, drain, corpse/undead. | Late conditional control can become default if too reliable. | T3xT5xT8/T9/T8; condition-gated. |
| Vanguard | Critical late. | Protection RSM, `Equip Knight Swords`, vanguard actions. | Can patch every frontline and revive sword dominance. | T6xPS/T6xT7/T2.1/F5; formation-local. |
| Ramza | Always high by presence. | Chapter hybrid breadth. | Ramza is mandatory party member and can over-cover roles. | Per-band Ramza rows; no specialist dominance before final. |

## A5 - Representative Encounter Row Set

These are placeholder row families with synthetic source IDs. They must be replaced by named IVC
encounters or fully pinned synthetic stat blocks before final numeric acceptance.

| Source ID | Source type | Band | Row family | Enemy role reason | Main proof owner | Final status |
| --- | --- | --- | --- | --- | --- | --- |
| GCV-SYN-00-RAW-MIXED | synthetic_placeholder | 0 | early mixed pressure | Tests raw start floor with basic melee and chip damage. | A1/T3 later. | Not final. |
| GCV-SYN-A-PHYSICAL | synthetic_placeholder | A | early physical pressure | Tests starter sustain and item economy. | T3/T2.1. | Not final. |
| GCV-SYN-B-PLATE | synthetic_placeholder | B | plate-heavy enemies | Tests whether crush/guard pressure matters without being mandatory. | F5/T6xT7. | Not final. |
| GCV-SYN-B-MONK-SECONDARY | synthetic_placeholder | B | durable body plus sustain pressure | Tests Knight-body plus Monk-secondary damage/sustain/revive compression. | T3/T3xT5/F5/T2.1. | Not final. |
| GCV-SYN-B-LEATHER-SKIRM | synthetic_placeholder | B | leather skirmishers | Tests Archer/Thief/caster reliability against speed/evasion. | T4/T5. | Not final. |
| GCV-SYN-A-PROGRESSION-RUSH | synthetic_placeholder | A/B | fixed-JP progression pacing | Tests ordinary, optimizer, and grind-heavy fixed-JP routing to see whether deeper job depth becomes optimizer-realistic early. | A2/A3/T2.1. | Supersedes `GCV-SYN-A-JP-ACCEL`; not final. |
| GCV-SYN-C-MAIL | synthetic_placeholder | C | mail-heavy enemies | Tests thrust/missile demand and Dragoon/Thief/Archer roles. | F5/T4. | Not final. |
| GCV-SYN-C-MITIGATION-STACK | synthetic_placeholder | C | mitigation stack | Tests plate/shield plus Protect/Shell plus defensive reaction against ordinary offense. | T6xPS/T4/T2.1. | Not final. |
| GCV-SYN-C-CASTER | synthetic_placeholder | C | cloth caster pressure | Tests anti-magic, Silence, guns, and rushing fragile casters. | F4/T9/T8xSR. | Not final. |
| GCV-SYN-C-ORATOR-BRAVE-FAITH | synthetic_placeholder | C/D | Brave/Faith propagation | Tests Orator morale/spiritual shifts across Brave-linked physical and Faith-linked magic/healing formulas. | F4/F5/T4/T2.1. | Not final. |
| GCV-SYN-C-STATUS | synthetic_placeholder | C | status/control pressure | Tests Mystic/Orator/White Mage status ecology. | T4/T5/T8. | Not final. |
| GCV-SYN-C-VERTICAL | synthetic_placeholder | C | vertical/terrain map | Tests Archer, Dragoon, Geomancer, and movement choices. | T4/T5/T11/T2.1. | Not final. |
| GCV-SYN-D-CLUSTER | synthetic_placeholder | D | clustered formation | Tests Summoner, Bard/Dancer, Meteor, area throughput. | T11/T11xT5/T3xT5xT11. | Not final. |
| GCV-SYN-D-SPREAD-RANGED | synthetic_placeholder | D | spread ranged pressure | Tests performer/caster vulnerability and mobility convergence. | T4/T5/T2.1. | Not final. |
| GCV-SYN-D-LONG-FIGHT | synthetic_placeholder | D | long attrition fight | Tests performance, sustain, MP economy, and defensive upkeep. | T3/T9/T11xT5/T6xPS. | Not final. |
| GCV-SYN-E-BOSS-RESIST | synthetic_placeholder | E | boss-like/status-resistant target | Tests Doom, Death, hard control, late burst, and Ramza. | T4/T5/T8/F5. | Not final. |
| GCV-SYN-E-UNDEAD-CORPSE | synthetic_placeholder | E | undead/corpse-relevant target | Tests Necromancer, revive inversion, corpse windows. | T3xT5xT8/T10 if acting bodies. | Not final. |

Minimum data needed before any synthetic placeholder becomes pinned:

```text
source_type
source_id
encounter_band
enemy_stat_block_or_named_encounter
enemy_equipment_or_equipment_tier
enemy_role_reason
party_side_assumptions
required_gate
version
```

## Provisional Findings

This first artifact pass suggests these campaign risks are highest priority:

1. Time Mage is the highest cross-party systemic risk because it touches Haste, Quick, Swiftspell,
   Teleport, Reflect, Slow/Stop, and Meteor.
2. Physical convergence is concentrated in Samurai/Ninja, but the setup starts earlier through Monk,
   Knight, Archer, Dragoon, and support/equipment unlock pressure.
3. Sustain compression must be watched from the start because Chemist, Squire, White Mage, Monk, and
   later Bard/Necromancer all touch recovery from different angles.
4. Equipment-tier timing is not optional. Gun, bow, spear, katana, ninja blade, knight sword, armor,
   shield, and caster weapon availability can change campaign pacing independently of job unlocks.
5. Ramza must be present in every row. Treating him as an extra later will understate the real
   five-unit stack.

## Claude Review Request

Claude should review:

- whether A1 covers the right mandatory and wall-test party rows;
- whether A2's provisional band placement has obvious pacing mistakes;
- whether A3's GPT first-pass optimizer stack misses or misclassifies any exploit;
- whether A4 detour pressure rankings are fair;
- whether A5 synthetic placeholders are acceptable as provisional rows while we wait for named or
  pinned encounter anchors.

Claude review verdict: Accepted for provisional campaign artifact planning by claude-opus-4-8 on
2026-06-21.

Review notes:

- A1 coverage accepted for this phase: P0 floor, P1 baseline, P5 ceiling across bands 0-E, plus
  P2/P3/P4/P6 wall tests;
- A2 accepted as pacing hypothesis, with Auto-Potion held to C+ and Samurai/Ninja late enough to
  avoid early convergence;
- A3 accepted as reconciled for provisional planning, with final ceiling evidence still requiring a
  later row-by-row dual-independent pass;
- A4 accepted provisionally, with Orator Brave/Faith propagation and fixed-JP progression rush
  marked as explicit follow-up risks after doc 61 supersession;
- A5 synthetic placeholders accepted as provisional only; final numeric acceptance remains blocked
  until named IVC or pinned synthetic anchors exist.
