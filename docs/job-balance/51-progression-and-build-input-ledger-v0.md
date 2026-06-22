# Progression And Build Input Ledger V0

Status: Accepted as W3 A2 progression and build-input ledger (provisional producer)
Date: 2026-06-21
Depends on:
- `docs/reference/README.md`
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/05-squire-chemist-v1-proposal.md`
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/18-monk-v1-proposal.md`
- `docs/job-balance/19-white-black-mage-v1-proposal.md`
- `docs/job-balance/20-time-mystic-v1-proposal.md`
- `docs/job-balance/21-summoner-geomancer-v1-proposal.md`
- `docs/job-balance/22-thief-orator-v1-proposal.md`
- `docs/job-balance/24-dragoon-samurai-v1-proposal.md`
- `docs/job-balance/25-ninja-v1-proposal.md`
- `docs/job-balance/26-bard-dancer-v1-proposal.md`
- `docs/job-balance/27-necromancer-v1-proposal.md`
- `docs/job-balance/29-special-knight-v1-proposal.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/47-campaign-validation-readiness-v0.md`
- `docs/job-balance/49-vanguard-rename-decision-v0.md`
- `docs/job-balance/50-campaign-journey-bundle-v0.md`

## Purpose

This is the first W3 producer artifact for campaign validation.

It instantiates the A2 unlock, JP posture, equipment, and build-piece ledger that W4/T2.1 and W5/F5
must consume. It covers the reaction, support, movement, and equipment-unlock candidates from the
accepted V1 job proposals, plus high-risk action unlocks that can move campaign power between
bands.

This document does not accept final JP numbers, final prerequisite trees, final RSM values, final
equipment shop records, formula-v1 outcomes, physical/foundation concrete values, Ramza chapter
values, or campaign pass/fail.

## Atlas Consultation

This ledger treats the vanilla atlas as a mandatory pre-check surface.

The navigation entrypoint is `docs/reference/README.md`. The authoritative source surfaces are:

- `docs/reference/fft-vanilla-ability-effect-index.md`;
- `docs/reference/fft-vanilla-command-skillset-effect-map.md`;
- `docs/reference/fft-vanilla-ability-effect-tag-crosswalk.md`;
- `docs/reference/fft-vanilla-status-effect-map.md`;
- `docs/job-balance/12-vanilla-skill-status-reference.md`.

Every reaction, support, movement, equipment unlock, status-bearing action, and high-risk action
package in this A2 ledger was checked against those source atlas surfaces for completeness. The
ledger remains free to replace or narrow vanilla behavior, but it should not omit a vanilla
effect family accidentally.

## Why This Does Not Need Numeric Simulation Yet

This ledger is a producer input, not a resolved balance result.

Numeric simulation is intentionally deferred because the exact JP gain curve, prerequisite graph,
shop records, formula-v1 weapon data, and many physical/foundation action values are still pending.
The useful check at this stage is structured pacing coverage:

- every RSM and equipment unlock has a target band and risk gate before concrete JP is assigned;
- early-band full-package risks are separated from late reward risks;
- support-slot and movement-slot donor pressure is visible before T2.1 incidence;
- equipment and gil dependencies are treated as pacing levers, not flavor text;
- W4/W5 know which pieces must be tested with and without JP Boost.

If Claude accepts this ledger, later numeric passes may tune exact JP costs inside these band
constraints. If later simulation proves a band target unsafe, this ledger must be revised rather
than silently overridden.

## Band Semantics

The bands below inherit W2's campaign envelopes.

| Band | Progression meaning for this ledger |
| --- | --- |
| 0 | Raw start. Starter actions only; no JP Boost assumption and no RSM pressure. |
| A | First direction. One shallow starter route; campaign utility may appear, but no combat engine. |
| B | First specialists. Active jobs should matter before their best exports. |
| C | Midgame branches. Two routes, one modest RSM, and the first serious compression checks. |
| D | Advanced build crafting. One premium RSM or engine per route; strong pieces compete. |
| E | Final integration. Late jobs, final Ramza, premium equipment, and top rewards; older specialists still matter. |

Band targets mean "ordinary campaign acquisition without extreme grinding." Optimizer JP Boost
routing must be checked separately against the same targets.

## Power Categories

This ledger uses the JP posture categories from the job design protocol.

| Category | Ledger meaning |
| --- | --- |
| Core identity | Affordable enough that the active job functions before exports define it. |
| Tactical option | Moderate commitment; useful in matchup or build-specific situations. |
| Strong global piece | Expensive, delayed, or tightly scoped because unrelated jobs may want it. |
| Build-defining engine | High investment and explicit opportunity cost; can define a route. |
| Campaign utility | Costed against grind/economy/pacing more than direct combat strength. |

## Global Pacing Decisions

1. `JP Boost` may be early because it is part of FFT's build-planning pleasure, but W4 must test
   P0/P1/P5 both with and without it. It cannot be assumed in floor rows.
2. Basic active-job identity must appear before the job's strongest export. Archer should shoot
   well before `Concentration` matters; Knight should tank and pressure gear before `Equip Armor`
   becomes a donor route; Summoner should summon before `Grand Invocation`.
3. No Band B party may assemble a low-friction full package of high damage, sustain/revive,
   mitigation/evasion, mobility, control, low resource pressure, and low positioning risk.
4. Band C is the first compression checkpoint. `Haste`, `Chakra`, item range, armor/shield support,
   modest caster economy, and early status cannot all become cheap at once.
5. Band D is where premium RSM pieces may exist, but they must compete. `Dual Wield`, `Doublehand`,
   `Swiftspell`, `Teleport`, `Move +3`, `Shirahadori`, `Vanish`, and performer global tools cannot
   all be ordinary same-route pickups.
6. Band E late jobs may be exciting and top-tier, but `Vanguard`, `Necromancer`, and final Ramza
   cannot become mandatory for ordinary final-party viability.
7. Equipment timing is a hard pacing lever. A support that unlocks premium gear is not online until
   the campaign can reasonably supply that gear and gil.
8. `Attack Boost` remains a protected stress-engine placement question. This ledger does not assign
   it as accepted Geomancer progression.
9. Bard and Dancer must have exactly the same reaction/support/movement ledger rows. Their action
   abilities can diverge; their build access cannot.

## Doc 31 Timing Invariants

This ledger binds the doc 31 I1-I10 cross-job timing invariants for later W4/W5 work: early
packages cannot complete damage, sustain, mobility, safety, and control at low friction; movement
families must keep role-specific tradeoffs; equipment unlocks cannot erase native job weakness
before that weakness has campaign value; caster economy cannot erase MP, CT, Faith, and fragility
together; and late rewards cannot become mandatory for ordinary final-party viability.

## A2 Ledger

Columns mirror the A2 contract from doc 31.

| Job | Piece | Slot | Intended encounter band | Minimum job depth | Power category | Healthy primary users | Dangerous off-job users | Equipment or gil dependency | Required gate | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Squire | Starter fundamentals | Action | 0/A | starter | Core identity | Ramza, raw generics, trainees | none | opening weapons only | P0 floor rows | Must feel like FFT's baseline, not a trap. |
| Squire | `Grit` | Reaction | A/B | shallow starter | Tactical option | early frontliners under pressure | durable late shells if too broad | none | T3/T6xPS/T2.1 | Early morale defense; should fall off or stay narrow. |
| Squire | `JP Boost` | Support | A | shallow starter | Campaign utility | training builds, non-optimized parties catching up | optimizer routes moving deep pieces into B/C | none | W3 JP routing, W4 with/without JP Boost | No combat stats; never assumed in P0 floor. |
| Squire | `Basic Training` | Support | B/C | committed starter | Tactical option | units keeping Squire actions relevant | generic physicals if it becomes broad damage | none | T2.1/F5 if formula-affecting | Should support Squire-style actions, not all physical attacks. |
| Squire | `Move +1` | Movement | A | shallow starter | Campaign utility | early trainees and slow units | late builds if no better movement competes | none | T2.1 movement incidence | Early floor mobility; must be obsolete-by-choice, not mandatory late. |
| Chemist | Basic Items and Phoenix Down | Action | 0/A | starter | Core identity | Chemist, any party needing floor sustain | none | item stock and early gil | T3 floor sustain | Revive access must be early enough for FFT feel, but item economy matters. |
| Chemist | `Throw Item` | Support | A/B | shallow Chemist | Tactical option | item-support builds, backline sustain | every healer if range is too safe | item stock and gil | T3/T5/T2.1 | Early-mid range extender; must keep positional pressure. |
| Chemist | `Auto-Potion` | Reaction | C | committed Chemist | Strong global piece | attrition builds with visible item cost | every serious build | potion tier, stock, gil | T3/T3xT5/T2.1 | Held to C+ so Band A/B floor and first-specialist rows are not warped by automatic sustain. Must be tier-aware, capped, or inventory-constrained. |
| Chemist | `Item Lore` | Support | C | committed Chemist | Strong global piece | dedicated item healer or item economy build | all sustain builds if it doubles floor safety | item stock and gil | T3/T9/T2.1 | Pairs dangerously with Throw Item and Auto-Potion. |
| Chemist | `Safeguard` | Support | C | committed Chemist | Tactical option | anti-break or anti-steal utility users | universal safety patch if too broad | equipment-value dependent | T2.1/T6xT7 | Should matter in equipment-pressure fights, not every fight. |
| Chemist | `Reequip` | Support | C/D | committed Chemist | Tactical option | reactive equipment plans | broad armor/weapon patching routes | spare equipment and gil | T2.1/equipment timing | First Chemist support to cut or delay if slots are crowded. |
| Chemist | `Move-Find Item` | Movement | B/C | shallow/committed Chemist | Campaign utility | exploration/economy routes | combat builds if rewards become combat-optimal | map treasure tuning | economy review/T2.1 | Keep as optional campaign texture, not a combat movement default. |
| Knight | Rend and guard pressure basics | Action | B | first specialist | Core identity | active Knight | none | armor targets and weapon availability | T6xT7/F5 | Knight must own permanent or semi-permanent gear attrition. |
| Knight | `Parry` | Reaction | B/C | shallow Knight | Tactical option | shield/weapon frontliners | high-evasion shells if it stacks too well | weapon/shield state if applicable | T4/T6xPS/T2.1 | Preferred Knight reaction if only one survives. |
| Knight | `Brace` | Reaction | B/C | shallow Knight | Tactical option | tankier Knights | mitigation stacks | armor/shield tier | T6xPS/T2.1 | Alternative defensive posture; no practical immunity. |
| Knight | `Equip Armor` | Support | C | committed Knight | Strong global piece | cloth/leather units choosing slow heavy variants | every fragile job patching weakness | armor shop tier and gil | T2.1/T6xPS/F5 | Should not erase armor-class identity before fragility has value. |
| Knight | `Equip Shield` | Support | C | committed Knight | Strong global piece | guard builds and hybrids | evasion/reaction stacks | shield shop tier and gil | T2.1/T4/T6xPS | Must be checked with Parry, Shirahadori, Mana Shield, and protect effects. |
| Knight | `Defensive Training` | Support | C/D | committed Knight | Tactical option | active Knight or guard-focused builds | generic mitigation stack | armor/shield tier | T6xPS/T2.1 | Narrower than broad damage prevention. |
| Knight | `Shield March` | Movement | C | committed Knight | Tactical option | plate/shield units closing or holding formation | all slow jobs if too generic | shield or heavy posture if used | T2.1/T5 | Should solve formation tempo, not all map traversal. |
| Archer | Bow and aimed-shot basics | Action | B | first specialist | Core identity | active Archer | none | bows/crossbows and high-ground maps | T4/F5 | Archer is the only archer and must remain useful into late game. |
| Archer | `Arrow Guard` | Reaction | B/C | shallow Archer | Tactical option | ranged duelists and light units | broad missile immunity shells | bow/shield interaction if any | T4/T2.1 | Preferred reaction over broad Speed Save if only one survives. |
| Archer | `Speed Save` | Reaction | C/D | committed Archer | Strong global piece | risk-taking ranged builds | speed-snowball builds | none | T5/T10/T2.1 | Only retain if capped/redesigned away from universal tempo snowball. |
| Archer | `Equip Bow` | Support | C | committed Archer | Strong global piece | controllers or physicals building around bows | jobs patching range too cheaply | bow/crossbow shop tier | T2.1/F5 | Active Archer owns Band B bow identity before off-job bow access opens. |
| Archer | `Concentration` | Support | C/D | committed Archer | Strong global piece | aimed-shot or bounded ranged plans | most physical and magical builds if it bypasses too much | none | T4/T2.1/F5 | High mandatory-risk support; cannot mean all attacks always hit. |
| Archer | `Bow Mastery` | Support | C | committed Archer | Tactical option | active Archer and dedicated bow users | generic damage routes if broad | bows/crossbows | T2.1/F5 | Prefer narrow bow identity over universal accuracy/damage. |
| Archer | `Jump +1` | Movement | B | shallow Archer | Campaign utility | early vertical maps and bow users | late mobility if left too competitive | vertical map texture | T2.1 movement incidence | Early vertical answer; should compete, not dominate. |
| Monk | Punch Art basics | Action | B | first specialist | Core identity | active Monk damage route | Knight-body damage compression if too efficient | unarmed/body stats | T6/F5 | Band B Monk identity is damage and positioning pressure, not sustain/revive compression. |
| Monk | `Chakra` | Action | C | committed Monk | Tactical option | active Monk sustain bruiser | Knight-body full-package route if available in B | position and height limits | T3/T3xT5/T2.1 | Held to C+ so Knight-body plus Monk does not assemble Band B damage+sustain. |
| Monk | Revive/Purification-style utility | Action | C/D | committed Monk | Tactical option | Monk sustain utility | all physicals if revive is too easy | position and height limits | T3/T3xT5/T2.1 | Keep useful but not a cheap replacement for White/Chemist. |
| Monk | `Counter` | Reaction | B/C | shallow Monk | Tactical option | melee Monk/frontliners | high-damage retaliation shells | melee exposure | T4/T10/T2.1 | Preferred Monk reaction; rewards being in danger. |
| Monk | `First Strike` | Reaction | D | deep Monk | Strong global piece | late high-Brave close fighters | physical immunity/denial shells | melee exposure | T4/T10/T2.1 | If retained, late and non-negating; no universal preemptive defense. |
| Monk | `Brawler` | Support | C/D | committed Monk | Build-defining engine | deliberate unarmed builds | most physical builds if fists outscale weapons | unarmed formula and Brave | T2.1/F5 | Attractive global piece, but not an early physical tax. |
| Monk | `Martial Discipline` | Support | C | committed Monk | Tactical option | Monk-action specialists | builds compressing Brawler+sustain+defense | none | T2.1/F5 | Should affect Monk-style actions, not all physical output. |
| Monk | `Lifefont` | Movement | C | committed Monk | Tactical option | sustained melee route | low-risk sustain shells | map movement exposure | T3/T5/T2.1 | Recovery must not erase repeated mistakes. |
| White Mage | Cure, protection, Esuna basics | Action | B | first specialist | Core identity | active White Mage | none | staff/rod and MP economy | T3/F4 | Must be valuable before Arise/Reraise exports. |
| White Mage | Raise and advanced revive | Action | B/C then D/E | committed White Mage | Tactical option / strong global | healer routes | all parties if revive loops too cheap | MP, CT, Faith | T3/T3xT5/T10 | Raise can arrive earlier; Arise/Reraise are late and loop-checked. |
| White Mage | Holy | Action | D | deep White Mage | Strong global piece | offensive White Mage | caster burst packages | MP, CT, Faith | F4/F5/T9 | Late payoff, not the reason every caster goes White. |
| White Mage | `Divine Grace` | Reaction | C/D | committed White Mage | Tactical option | healers under pressure | sustain stacks if broad | Faith/MP if applicable | T3/T6xPS/T2.1 | Should preserve vulnerability to focus/interruption. |
| White Mage | `Arcane Ward` | Support | C/D | committed White Mage | Strong global piece | defensive casters | all casters if broad magic defense is best | none | F4/T6xPS/T2.1 | Watch overlap with Mystic defensive supports. |
| White Mage | `Faithful Casting` | Support | C/D | committed White Mage | Tactical option | Faith-based healer | damage casters if too broad | Faith management | F4/T2.1 | Should reinforce White identity more than universal MA scaling. |
| White Mage | `Sanctuary Step` | Movement | C | committed White Mage | Tactical option | healers repositioning near allies | all casters if too safe | ally formation | T5/T2.1 | Should solve healer positioning, not terrain globally. |
| Black Mage | Tier-I elements and mid-tier spells | Action | B/C | first specialist | Core identity | active Black Mage | none | rods, MP, Faith, CT | F4/F5/T9 | Direct caster offense must pay MP/CT/fragility from Band B onward. |
| Black Mage | Poison/Toad/Death/Flare | Action | C/D/E | committed/deep Black Mage | Tactical option / strong global | status caster or late burst route | boss bypass or hard-control routes | MP, CT, status resistance | T4/T5/F5 | Hard status and Flare are opt-in, not baseline. |
| Black Mage | `Arcane Backlash` | Reaction | C | committed Black Mage | Tactical option | fragile caster with retaliation risk | damage reflection stacks | incoming magic/MP | T4/F4/T2.1 | Must not punish ordinary enemy turns too reliably. |
| Black Mage | `Elemental Focus` | Support | C/D | committed Black Mage | Strong global piece | elemental specialists | all offensive casters if broad | elemental matchups | F4/F5/T2.1 | Safer than broad magic damage; still incidence-checked. |
| Black Mage | `Arcane Strength` | Support | D | deep Black Mage | Strong global piece | late damage caster | every caster if broad MA boost | none | F4/F5/T2.1 | Candidate only if bounded; not silently accepted as universal magic support. |
| Black Mage | `Ley Step` | Movement | C/D | committed Black Mage | Tactical option | casters using position/setups | all casters if too free | map/terrain if any | T5/T2.1 | Movement must preserve CT and interruption exposure. |
| Time Mage | Haste, Slow, delay basics | Action | C | mid branch | Core identity | active Time Mage/controller | every party if no opportunity cost | MP, CT, Faith if applicable | T5/T10/T2.1 | Tempo should create windows, not loops; basic Haste still needs action-economy review. |
| Time Mage | Stop, Reflect, Quick, Meteor | Action | D/E | deep Time Mage | Strong global piece | committed Time route | action-economy and area burst packages | MP, CT, reflect routing | T5/T10/T8xSR/F5 | Quick and Meteor are high-risk late tools. |
| Time Mage | `Critical: Quick` | Reaction | D/E | deep Time Mage | Strong global piece | risky critical tempo build | loop engines | critical HP state | T10/T3/T2.1 | Critical-only, no recursion. |
| Time Mage | `Swiftspell` | Support | D | deep Time Mage | Build-defining engine | committed slow-cast caster | most casters if mandatory | MP/CT spell set | T5/T9/T10/T2.1/F5 | One of the top caster convergence risks. |
| Time Mage | `Temporal Focus` | Support | C/D | committed Time Mage | Tactical option | Time-action specialists | broad caster tempo if too generic | none | T5/T2.1 | Prefer action-set focus over universal cast compression. |
| Time Mage | `Teleport` | Movement | D | deep Time Mage | Strong global piece | mobile controllers and committed builds | most late builds | failure/range/cost rule pending | T5/T8/T2.1 | Must not erase terrain and threat range for everyone. |
| Mystic | Silence, Belief/Disbelief, status basics | Action | C | mid branch | Core identity | active Mystic/controller | Faith-combo packages if too cheap | Faith/status resist | F4/T4/T2.1 | Belief/Oil/fire remains the quantified breach vector. |
| Mystic | Drain, hard control, Faith manipulation | Action | C/D | committed Mystic | Tactical option / strong global | anti-caster and resource routes | boss/control bypass routes | MP, Faith, status resist | T4/T9/F5 | Hard control must stay narrow and resisted. |
| Mystic | `Absorb MP` | Reaction | C/D | committed Mystic | Tactical option | anti-caster sustain | infinite MP loops | enemy MP pressure | T9/T2.1 | Bound by real MP availability and incoming spell pressure. |
| Mystic | `Mana Shield` | Reaction | C/D | committed Mystic | Strong global piece | MP-tank or anti-magic builds | broad damage immunity stacks | MP pool and restore tools | T9/T6xPS/T2.1 | Cannot pair with MP economy into practical immunity. |
| Mystic | `Halve MP` | Support | C/D | committed Mystic | Strong global piece | endurance casters | every caster | MP costs and encounter length | T9/T2.1/F4 | Dangerous caster economy piece; not a default. |
| Mystic | `Magick Defense Boost` | Support | C/D | committed Mystic | Strong global piece | anti-magic builds | universal caster defense | none | F4/T6xPS/T2.1 | Watch overlap with Shell, Arcane Ward, Mana Shield. |
| Mystic | `Mystic Focus` | Support | C | committed Mystic | Tactical option | Mystic-action specialists | broad status accuracy packages | Faith/status formula | T4/F4/T2.1 | Prefer narrow status/Mystic support. |
| Mystic | `Manafont` | Movement | C/D | committed Mystic | Tactical option | endurance casters | all casters if MP pressure disappears | MP economy | T9/T2.1 | Must not combine with Halve MP/Swiftspell into free casting. |
| Summoner | Low-tier summons and Moogle | Action | C | mid branch | Core identity | active Summoner | none | MP, CT, target count | T11/T9/F5 | Summoner must work before premium summons. |
| Summoner | Golem, Carbuncle, Faerie | Action | D | committed Summoner | Strong global piece | protection/sustain summon route | mitigation or reflect packages | MP, CT, duration | T6xPS/T8xSR/T3/F5 | Strong utility with visible CT/MP/interruption risk. |
| Summoner | Bahamut, Salamander, Zodiark | Action | D/E | deep Summoner | Strong global piece | late area caster | area burst ceiling packages | MP, CT, target count, element | T11/F5/T9 | Belief/Oil/fire and 3-target ceilings must be rechecked. |
| Summoner | `Summon Ward` | Reaction | C/D | committed Summoner | Tactical option | summon-focused caster | broad magic defense | MP/CT exposure | T6xPS/T2.1 | Should not cover all caster fragility. |
| Summoner | `Summon Focus` | Support | C/D | committed Summoner | Strong global piece | dedicated summoner | all area casters if broad | MP/CT and summon set | T11/T9/T2.1/F5 | Narrow to summons; no broad magic damage support. |
| Summoner | `Grand Invocation` | Support | D/E | deep Summoner | Build-defining engine | late summon specialist | area dominance packages | MP/CT/target count | T11/T10/T2.1/F5 | Expensive capstone if retained. |
| Summoner | `Ritual Step` | Movement | C/D | committed Summoner | Tactical option | slow caster positioning around summon setup | all casters if too safe | formation/CT exposure | T5/T2.1 | Must preserve interruption and positioning risk. |
| Geomancer | Baseline Geomancy | Action | C | mid branch | Core identity | active Geomancer | none | terrain and weapon availability | T11/T4/F5 | Hybrid terrain identity must work before supports. |
| Geomancer | Oil/terrain setup and hybrid pressure | Action | C/D | committed Geomancer | Tactical option | terrain setup builds | Belief/Oil/fire burst chain | terrain availability | T11/F5/T4 | Highest quantified setup risk when paired with Summoner. |
| Geomancer | `Nature's Wrath` | Reaction | C | committed Geomancer | Tactical option | terrain-frontline hybrids | retaliation stacks | terrain/contact | T4/T11/T2.1 | Map-dependent; no universal counter. |
| Geomancer | `Terrain Lore` | Support | C | committed Geomancer | Tactical option | active Geomancer or terrain route | generic hybrid damage if broad | terrain maps | T11/T2.1/F5 | Reinforces terrain identity. |
| Geomancer | `Attack Boost` | Support | unassigned | deferred | Build-defining engine | unknown until global support pass | most physical builds | weapon formula dependent | T2.1/F5 | Protected stress engine. No accepted band or owner yet. |
| Geomancer | `Ignore Terrain` | Movement | C/D | committed Geomancer | Tactical option | map-texture routes | late mobility if too broad | terrain-heavy maps | T2.1/T5 | Terrain answer, not Teleport or Move +3 replacement. |
| Geomancer | `Ignore Weather` | Movement | C/D | committed Geomancer | Tactical option | weather/terrain specialist | too narrow to matter or too broad if merged | weather maps | T11/T2.1 | Keep only if map texture can justify it. |
| Thief | Steal, knife, and speed disruption basics | Action | B/C | first specialist/mid branch | Core identity | active Thief | none | stealable items and speed maps | T4/T2.1/economy | Utility must matter without monster scope. |
| Thief | Charm/Steal Heart and equipment steals | Action | C/D | committed Thief | Tactical option | disruption and loot route | hard control or economy abuse | target equipment and status resist | T4/T8/economy | Equipment steals require commitment. |
| Thief | `Sticky Fingers` | Reaction | C | committed Thief | Tactical option | thief/economy builds | broad anti-attack reward | stealable context | T2.1/economy | Must not become generic defensive value. |
| Thief | `Light Fingers` | Support | C | committed Thief | Tactical option | dedicated stealing route | economy exploit routes | stealable equipment | economy/T2.1 | Build identity, not combat default. |
| Thief | `Poach` | Support | deferred | deferred | Campaign utility | monster/economy route later | out of current monster scope | monsters | monster-scope review | Monsters are out of current campaign validation scope. |
| Thief | `Move +2` | Movement | C | committed Thief | Strong global piece | speed/positioning specialists | most midgame builds | none | T2.1/T5 | Must compete with Jump, terrain movement, and later Teleport/Move +3. |
| Thief | `Treasure Hunter` | Movement | C | committed Thief | Campaign utility | exploration/economy route | combat builds if rewards too strong | map treasure tuning | economy/T2.1 | Separate economy reward from combat movement. |
| Orator | Brave/Faith speech, morale, gun basics | Action | C | mid branch | Core identity | active Orator | Faith/Brave propagation packages | guns, Faith/Brave volatility | F4/F5/T4 | Orator must keep a reason to exist beyond Equip Guns. |
| Orator | Recruitment, Condemn, broad status | Action | C/D | committed Orator | Tactical option | social/control route | hard-control or economy shortcuts | status resist, recruitment rules | T4/T8/economy | Recruitment/economy is not allowed to define current monster scope. |
| Orator | `Bravery Surge` | Reaction | C/D | committed Orator | Tactical option | morale builds | Brave-scaling physical stacks | Brave formula | F5/T2.1 | Preferred over broad Faith stacking if one reaction survives. |
| Orator | `Faith Surge` | Reaction | C/D | committed Orator | Strong global piece | caster-risk builds | Faith burst/healing/damage stacks | Faith formula | F4/F5/T2.1 | Very high risk with Belief and caster packages. |
| Orator | `Equip Guns` | Support | C/D | committed Orator | Strong global piece | deliberate gun builds | stat-starved jobs patching damage | gun shop tier and gil | T2.1/F5 | Guns are stat-independent enough to require strict incidence checks. |
| Orator | `Tame` | Support | deferred | deferred | Campaign utility | monster route later | out of scope | monsters | monster-scope review | Not part of current no-monster campaign pass. |
| Orator | `Beast Tongue` | Support | deferred | deferred | Campaign utility | monster route later | out of scope | monsters | monster-scope review | Not part of current no-monster campaign pass. |
| Orator | `Social Positioning` | Movement | C | committed Orator | Tactical option | speech/gun positioning | all ranged builds if too safe | line/range maps | T5/T2.1 | Should help Orator role, not replace Thief/Time mobility. |
| Dragoon | Jump basics | Action | C | mid branch | Core identity | active Dragoon | none | polearms, vertical maps | T4/T5/F5 | Jump must be useful before `Equip Polearms` export defines the route. |
| Dragoon | High vertical/horizontal Jump mastery | Action | C/D | committed Dragoon | Tactical option | dedicated Dragoon | untouchable delay loops | map height and timing | T4/T5/T8 | Must remain punishable through timing and positioning. |
| Dragoon | `Dragonheart` | Reaction | C/D | committed Dragoon | Strong global piece | exposed jump/frontline builds | broad survival loops | HP/revive state | T3/T10/T2.1 | Bound away from practical immortality. |
| Dragoon | `Brace Landing` | Reaction | C | committed Dragoon | Tactical option | Jump users | generic damage prevention | Jump timing | T5/T6xPS/T2.1 | Fallback if Dragonheart is too broad. |
| Dragoon | `Equip Polearms` | Support | C/D | committed Dragoon | Strong global piece | anti-mail reach builds | Knight/Samurai stealing spear home | polearm shop tier and maps | T2.1/F5 | Earned spear access; active Dragoon remains spear home. |
| Dragoon | `Jump Training` | Support | C | committed Dragoon | Tactical option | Jump specialist | generic mobility/damage if broad | polearm/Jump actions | T4/T5/T2.1 | Narrow action-set support. |
| Dragoon | `Jump +1` / `Jump +2` | Movement | B/C | shallow/committed Dragoon | Campaign utility / tactical | early vertical routes | broad movement if too cheap | vertical maps | T2.1/T5 | `Jump +1` can be early; `Jump +2` needs more commitment. |
| Dragoon | `Jump +3` | Movement | D | deep Dragoon | Strong global piece | dedicated vertical builds | late mobility default | vertical maps | T2.1/T5 | Must compete with Teleport, Move +2, Move +3, and Ignore Elevation. |
| Dragoon | `Ignore Elevation` | Movement | D/E | deep Dragoon | Strong global piece | vertical specialist | terrain erased for most builds | vertical maps | T2.1/T5 | Late and high-risk; not a generic map skip. |
| Samurai | Iaido and katana basics | Action | D | advanced branch | Core identity | active Samurai | none | katana availability and Brave | F5/T4 | Samurai must matter before Doublehand/Shirahadori exports dominate. |
| Samurai | Protection Iaido and premium draws | Action | D/E | committed/deep Samurai | Strong global piece | active Samurai and Brave build | area/support compression | katana stock, Brave | T11/F5/T2.1 | Katana stock/resource friction remains a possible limiter. |
| Samurai | `Shirahadori` | Reaction | D/E | deep Samurai | Strong global piece | high-Brave Samurai-style defense | evasion/Brave practical immunity | Brave and attack family | T4/T6xPS/T2.1 | Iconic, but needs hard Brave-scaled ceiling. |
| Samurai | `Bonecrusher` | Reaction | D | committed Samurai | Tactical option | retaliation physicals | counter-damage stacks | melee exposure | T4/T10/T2.1 | Fallback/narrower option if Shirahadori cannot be bounded. |
| Samurai | `Equip Katana` | Support | D | committed Samurai | Strong global piece | Brave-linked katana builds | Samurai reduced to support stop | katana shop tier and gil | T2.1/F5 | Meaningful route unlock, not cheap universal access. |
| Samurai | `Doublehand` | Support | D/E | deep Samurai | Build-defining engine | committed single-weapon builds | all physical builds | weapon formula and two-hand state | T2.1/F5 | Protected 1.80 engine; cannot stack with Dual Wield. |
| Samurai | `Iaido Focus` | Support | D | committed Samurai | Tactical option | active Iaido specialists | broad magic/damage if generic | katana/Iaido resource | T11/F5/T2.1 | Narrow reliability or friction support only. |
| Samurai | `Waterwalking` | Movement | C/D | committed Samurai | Tactical option | water-heavy maps | too narrow or irrelevant | map terrain | T2.1/T5 | Keep only if map texture supports it. |
| Samurai | `Blade Step` | Movement | D | committed Samurai | Tactical option | stance/position Samurai | generic Move +N replacement | melee exposure | T2.1/T5 | Non-water option if needed; not a universal mobility piece. |
| Ninja | Active Dual Wield and Throw basics | Action/innate | D | advanced branch | Core identity | active Ninja | none | throwable stock and weapon availability | F5/T4/T9 | Active Ninja may be strong; learned support is the donor risk. |
| Ninja | High-impact Throw categories | Action | D/E | committed Ninja | Strong global piece | resource-backed ranged burst | safe ranged dominance | inventory, gil, range | T4/T9/F5 | Throw must pay inventory/range/accuracy costs. |
| Ninja | `Vanish` | Reaction | D/E | deep Ninja | Strong global piece | fragile assassin route | untargetable loops and stealth-strike bypass | duration/trigger rules | T5xT8/T4/T2.1 | Preferred only if bounded; attacks from Vanish need explicit bypass rule. |
| Ninja | `Reflexes` | Reaction | D | committed Ninja | Tactical option | light evasive builds | evasion immunity stacks | evasion gear | T4/T6xPS/T2.1 | Fallback if Vanish cannot be bounded. |
| Ninja | `Dual Wield` | Support | D/E | deep Ninja | Build-defining engine | committed two-hit weapon builds | most physical builds | two one-handed weapons | T2.1/F5 | Protected 2-hit engine; no fists, spells, Iaido, Throw, or Doublehand stacking unless later approved. |
| Ninja | `Throw Mastery` | Support | D | committed Ninja | Tactical option | dedicated Throw route | ranged dominance if compressed with Dual Wield | throwable stock and gil | T4/T9/T2.1 | Narrow to Throw resource/range/categories. |
| Ninja | `Move +3` | Movement | D/E | deep Ninja | Strong global piece | elite skirmishers | most late builds | none | T2.1/T5 | Major donor-pull risk; must compete with Teleport and vertical tools. |
| Ninja | `Ignore Terrain` | Movement | D | committed Ninja | Tactical option | stealth/terrain route | broad terrain erasure | terrain maps | T2.1/T5 | Optional if Move +3 proves too universal. |
| Bard | Songs | Action | D | advanced branch | Core identity | active Bard | all parties if global value is free | CT, interruption, performer safety | T11xT5/T3xT5xT11 | Global buff/sustain must pay vulnerability and time. |
| Bard | `Earplugs` | Reaction | D | committed performer | Tactical option | Bard/Dancer parity builds | generic status defense if broad | sound/performance context | T2.1 | Must be identical to Dancer's record if retained. |
| Bard | `Encore` | Reaction | D/E | deep performer | Strong global piece | risky performance tempo | extra-action loops | performance state | T10/T11xT5/T2.1 | Must not create global-value recursion. |
| Bard | `Performance Mastery` | Support | D | committed performer | Strong global piece | active performer builds | mandatory global infrastructure | performance channel | T11xT5/T2.1 | Shared with Dancer exactly. |
| Bard | `Stagecraft` | Support | D | committed performer | Tactical option | performance positioning/safety | every support performer if too broad | performance channel | T5/T2.1 | Shared with Dancer exactly. |
| Bard | `Performance Step` | Movement | D | committed performer | Tactical option | performers managing safe performance lines | all casters if too safe | performance state/map | T5/T2.1 | Default shared movement candidate. |
| Bard | `Fly` | Movement | E/promotion | deep performer | Strong global piece | only if performers cannot function without it | most late builds | map traversal | T2.1/T5 | Promotion candidate only; high-risk mobility. |
| Dancer | Dances | Action | D | advanced branch | Core identity | active Dancer | all parties if global debuff is free | CT, interruption, performer safety | T11xT5/T3xT5xT11 | Global debuff/attrition must pay vulnerability and time. |
| Dancer | `Earplugs` | Reaction | D | committed performer | Tactical option | Bard/Dancer parity builds | generic status defense if broad | sound/performance context | T2.1 | Must be byte-identical to Bard's record. |
| Dancer | `Encore` | Reaction | D/E | deep performer | Strong global piece | risky performance tempo | extra-action loops | performance state | T10/T11xT5/T2.1 | Must be byte-identical to Bard's record. |
| Dancer | `Performance Mastery` | Support | D | committed performer | Strong global piece | active performer builds | mandatory global infrastructure | performance channel | T11xT5/T2.1 | Must be byte-identical to Bard's record. |
| Dancer | `Stagecraft` | Support | D | committed performer | Tactical option | performance positioning/safety | every support performer if too broad | performance channel | T5/T2.1 | Must be byte-identical to Bard's record. |
| Dancer | `Performance Step` | Movement | D | committed performer | Tactical option | performers managing safe performance lines | all casters if too safe | performance state/map | T5/T2.1 | Must be byte-identical to Bard's record. |
| Dancer | `Fly` | Movement | E/promotion | deep performer | Strong global piece | only if performers cannot function without it | most late builds | map traversal | T2.1/T5 | Must be byte-identical to Bard's record if promoted. |
| Necromancer | Drain, dark attrition, death mark basics | Action | E | late branch | Core identity | active Necromancer | none | corpse/state availability, MP | T3xT5xT8/T9/F5 | Late job; should not solve ordinary fights alone. |
| Necromancer | Undead Mark, corpse actions, dark finishers | Action | E | deep late branch | Strong global piece | corpse/state specialist | boss bypass and sustain inversion dominance | KO/corpse/undead state | T3xT5xT8/T8/F5 | Must respect boss resistance and corpse windows. |
| Necromancer | `Soulbind` | Reaction | E | committed Necromancer | Tactical option | state/attrition route | sustain or counter loops | KO/corpse/MP state | T3xT5xT8/T10/T2.1 | Narrow to Necromancer state play. |
| Necromancer | `Death's Door` | Reaction | E | deep Necromancer | Strong global piece | risky late caster | critical survival loops | critical HP/death state | T3/T10/T2.1 | No practical immortality. |
| Necromancer | `Dark Lore` | Support | E | committed Necromancer | Tactical option | dark-state specialist | broad magic boost if generic | dark/undead state | F4/F5/T2.1 | Narrow support; no broad magic damage. |
| Necromancer | `Deathcraft` | Support | E | deep Necromancer | Strong global piece | corpse/death specialist | universal late caster secondary | KO/corpse/undead state | T3xT5xT8/T2.1/F5 | Late capstone support if retained. |
| Necromancer | `Grave Step` | Movement | E | committed Necromancer | Tactical option | corpse/marked positioning | generic Teleport substitute | KO/corpse/marked targets | T5/T8/T2.1 | Narrow movement around state play. |
| Necromancer | `Shadow Step` | Movement | E | deep Necromancer | Strong global piece | late dark mobility | late universal movement | shadow/state/range rule pending | T5/T8/T2.1 | Must not replace Teleport/Move +3/Fly globally. |
| Vanguard | Intercede, Aegis Stance, `Breach` | Action | E | late branch | Core identity | active Vanguard | none | plate/shield/weapon posture | T6xPS/T6xT7/F5 | First post-unlock protection plus guard-pressure package. |
| Vanguard | Sunder Guard, Commanding Challenge, Decisive Strike | Action | E | deep late branch | Strong global piece | active Vanguard/setup finisher | Holy Knight clone or best physical shell | premium weapons/formation | T8/T10/F5 | No free ranged holy-sword pattern. |
| Vanguard | `Intervention` | Reaction | E | committed Vanguard | Strong global piece | local protector | global cover or extra-attack loop | formation/range | T8/T6xPS/T10/T2.1 | Preferred Vanguard reaction; no global cover. |
| Vanguard | `Last Stand` | Reaction | E | deep Vanguard | Tactical option | critical frontline survival | practical immortality | critical HP and mitigation | T3/T6xPS/T2.1 | Bounded pressure fantasy only. |
| Vanguard | `Equip Knight Swords` | Support | E | deep Vanguard | Build-defining engine | committed premium sword plan | revived sword dominance | knight sword availability and gil | T2.1/F5 | Optional and dangerous; cut or delay if it becomes default. |
| Vanguard | `Vanguard Training` | Support | E | committed Vanguard | Tactical option | active Vanguard arts | universal physical support if broad | Vanguard action set | T2.1/F5 | Improves formation/protection arts, not all physical damage. |
| Vanguard | `Armor Discipline` | Support | E | committed Vanguard | Strong global piece | plate/shield specialist | mitigation stack | heavy armor/shield tier | T6xPS/T2.1/F5 | Must stay distinct from Knight armor/shield unlocks. |
| Vanguard | `Vanguard March` | Movement | E | committed Vanguard | Tactical option | plate/shield formation play | generic late mobility patch | heavy/formation posture | T5/T2.1 | Helps hold/enter formation, not map traversal broadly. |
| Ramza | Chapter 1-3 flexible growth | Action/RSM TBD | 0-D | story progression | Core identity | Ramza | specialist lanes if too broad | story equipment and chapter state | W5 Ramza rows | Must be useful in every party without invalidating protected specialists. |
| Ramza | Chapter 4 final knight/mage hybrid | Action/RSM TBD | E | story progression | Strong global piece | final Ramza | Vanguard, casters, and old specialists if dominant | final equipment and chapter state | F4/F5/T2.1 | Top-tier broad exception, but not best-in-lane everywhere. |

## Band Pressure By Slot

### Reactions

Band B/C reactions should be narrow and readable: `Grit`, `Auto-Potion`, `Parry`, `Arrow Guard`,
`Counter`, and early caster reactions can exist only if they do not create practical immunity.

Band D/E reactions are where the dangerous engines live: `Shirahadori`, `Vanish`, `Critical:
Quick`, `Encore`, `Dragonheart`, `Intervention`, and late death/corpse reactions. These require
T2.1 incidence plus their mechanic-specific immunity or action-economy gates.

### Supports

Support timing is the main campaign-control lever.

- Band A supports may be campaign utility or narrow starter support only.
- Band B/C supports may open first-specialist identity, but not universal repair kits.
- Band D/E supports may be powerful engines if they demand opportunity cost and preserve competing
  routes.

The protected support-engine set for W4 is:

```text
JP Boost
Auto-Potion
Throw Item
Equip Armor
Equip Shield
Equip Bow
Concentration
Brawler
Swiftspell
Halve MP
Magick Defense Boost
Summon Focus
Grand Invocation
Attack Boost
Equip Guns
Equip Polearms
Equip Katana
Doublehand
Dual Wield
Performance Mastery
Equip Knight Swords
```

`Attack Boost` is listed here as a protected stress engine, not as an accepted progression reward.

### Movement

The intended movement ladder is:

| Stage | Movement pieces | Constraint |
| --- | --- | --- |
| Early | `Move +1`, `Jump +1` | Floor comfort and map texture; not late defaults. |
| Mid | `Shield March`, `Sanctuary Step`, `Ley Step`, `Manafont`, `Ritual Step`, `Ignore Terrain`, `Move +2`, `Social Positioning`, `Jump +2` | Role-specific movement should compete by map and build. |
| Advanced | `Jump +3`, `Blade Step`, `Performance Step`, `Move +3`, `Teleport`, `Ignore Elevation` | No single late movement default. |
| Late/promotion | `Fly`, `Shadow Step`, `Vanguard March` | Strong but narrow; must not replace all other map solutions. |

W4 must explicitly test whether `Teleport`, `Move +3`, `Ignore Elevation`, or `Fly` becomes the
default late movement choice.

### Equipment Unlocks

Equipment unlocks must be tested at the shop/gil tier where the gear is actually available.

| Unlock | Earliest healthy band | Dependency | Main failure |
| --- | --- | --- | --- |
| `Equip Armor` | C | heavy armor shop tier and gil | Fragile jobs lose meaningful weakness too early. |
| `Equip Shield` | C | shield shop tier and gil | Evasion/mitigation stacks approach immunity. |
| `Equip Bow` | C | bow/crossbow shop tier and map range | Archer becomes only a support stop. |
| `Equip Guns` | C/D | gun shop tier and gil | Stat-starved jobs get safe damage patch. |
| `Equip Polearms` | C/D | polearm shop tier and vertical/thrust maps | Dragoon loses spear ownership. |
| `Equip Katana` | D | katana shop tier, Brave relevance, gil | Samurai loses katana ownership. |
| `Equip Knight Swords` | E | premium knight sword access and gil | Sword dominance returns. |

No broad `Equip Sword` support is accepted.

## Immediate W4/W5 Rows Created By This Ledger

The first populated incidence and real-roster rows should use the accepted W2 party rows, in this
order:

1. P5 Band A/B with and without `JP Boost`, testing whether first-specialist depth moves too early.
2. P5 Band B/C physical full-package route: Knight body plus Monk sustain, Chemist item range,
   Archer reliability, and early armor/shield support.
3. P3 Band C/D caster economy route: `Swiftspell`, `Halve MP`, `Manafont`, `Summon Focus`, and
   `Belief`/Oil/fire setup.
4. P2 Band D/E physical support route: `Doublehand`, `Dual Wield`, `Brawler`, `Concentration`,
   equipment unlocks, premium weapons, and movement.
5. P6 Band D/E performer route: Bard/Dancer action difference with identical R/S/M access.
6. P4 Band C/D sustain-control route: Protect/Shell, items, Chakra, Haste/Slow, status, and
   mitigation reactions.
7. P1/P2/P3/P4 Band E late-job route: final Ramza, Vanguard, Necromancer, and older specialists in
   the same party envelope.

## Open W3 Producers After This Ledger

This A2 ledger is not all of W3. The remaining W3 producer artifacts are:

- physical/foundation concrete action values for Squire, Chemist, Knight, Archer, Monk, Thief,
  Orator, Dragoon, Samurai, Ninja, Vanguard, and Ramza chapter jobs;
- candidate RSM numeric values or hard boundaries for the pieces marked above;
- a prerequisite-tree and JP-cost draft that maps these bands to actual unlock depth;
- equipment shop/gil timing records that prove when each equipment unlock is practically online.

## Claude Review Request

Claude should review:

- whether every reaction, support, movement, and equipment unlock from the current job proposals is
  represented;
- whether any band target would allow Band D/E power to become ordinary Band B/C power;
- whether any job's active identity appears too late relative to its exportable RSM;
- whether the movement ladder preserves real choices;
- whether the equipment unlock table is strict enough to prevent sword, gun, armor, shield, bow,
  spear, katana, and knight-sword leakage;
- whether `Attack Boost`, `Fly`, monster-related supports, and Ramza are correctly left as
  unresolved producers rather than silently accepted data.
