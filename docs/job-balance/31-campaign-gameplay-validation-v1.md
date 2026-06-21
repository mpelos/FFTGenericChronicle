# Campaign Gameplay Validation V1

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
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
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`

## Purpose

This document validates the job ecosystem as a campaign experience.

Previous job documents validated jobs mostly as isolated tactical identities. That is not enough.
Generic Chronicle also has to feel healthy when a player starts a fresh game, develops four generic
characters plus Ramza, unlocks jobs over time, and combines skills across jobs.

The question here is:

```text
Does a full playthrough create varied, satisfying, non-obvious party and build development?
```

This document validates player-power pacing against the fixed existing FFT/IVC encounter curve.

It does not redesign encounters, monsters, or maps. It does not set exact JP costs, prerequisite
levels, shop prices, item prices, equipment stats, or final numeric skill values. It defines the
campaign validation model those later numbers must satisfy.

Concrete pass/fail thresholds depend on the validation infrastructure sprint in
`docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`. This document therefore defines
scenario rows, falsifiable proxies, and red-flag-to-gate bindings, not final numeric acceptance.

## Player Model

Primary validation player:

- starts a fresh game;
- uses four generic characters plus Ramza as the ordinary party frame;
- understands FFT build planning and intentionally hunts useful cross-job pieces;
- accepts some grind because that is part of FFT;
- should not need excessive grind to keep jobs functional;
- should feel rewarded for making a strong build;
- should not find one early route that makes most other routes foolish.

Stress validation player:

- knows the job tree in advance;
- rushes the strongest reaction/support/movement and equipment-unlock pieces;
- is willing to detour through jobs for build pieces;
- tries to stack recovery, mitigation, mobility, action economy, and high damage earlier than the
  intended campaign curve.

Both players matter. The primary player protects feel. The stress player protects the system from
obvious power spikes.

Floor validation player:

- does not know the optimal job tree;
- picks jobs by theme or immediate need;
- may keep starter jobs too long;
- may branch "wrong" before correcting course;
- should still have a viable and enjoyable game without being forced into a guide route.

The floor player protects the campaign from becoming balanced only for optimizers.

## Campaign Band Model

The exact prerequisite tree is still open. These bands are therefore a provisional prerequisite and
JP-pacing proposal, not final implementation data.

The bands are defined by encounter/story difficulty, not by job unlock depth. Job unlock depth is a
separate overlay. This matters because the main campaign failure is when an optimizer can grind into
a deep job, reaction/support/movement piece, or equipment unlock while still facing an encounter band
that was supposed to be fought with shallower tools.

Red flag definition:

```text
build power reachable ahead of the encounter band that should gate it
```

| Encounter band | Campaign feel | Expected job-depth overlay | Floor check | Ceiling check |
| --- | --- | --- | --- | --- |
| 0 - Raw start | First battles with near-zero JP. | Squire/Chemist only, with only the cheapest starter tools. | A non-optimized party can act, recover small damage, and learn the system without grind. | No starter action creates repeatable safe damage, sustain, or control. |
| A - Starter onboarding | The player learns basic actions, items, weapon texture, and recovery. | Squire, Chemist, first access toward Knight/Archer/White Mage/Black Mage. | The game is not too hard or too slow before first specialization. | No infinite sustain, no combat JP Boost tax, no one-button starter dominance. |
| B - First specialization | The player picks first real party roles. | Knight, Archer, White Mage, Black Mage, Monk, Thief, early Time Mage tools. | Several first-specialist choices are viable without guide routing. | Roles become distinct before global build pieces arrive. |
| C - Midgame branching | The party can branch into hybrid, control, reach, speech, and larger magic plans. | Time Mage, Mystic, Geomancer, Orator, Dragoon, Summoner. | Physical, magical, hybrid, and control parties all have a plausible path. | No single branch solves every need or skips intended weaknesses. |
| D - Advanced build crafting | High-value support/reaction/movement pieces and late combat identities appear. | Samurai, Ninja, Bard, Dancer, deeper Time/Mystic/Summoner rewards. | A thematic party can improve without rebuilding every unit from scratch. | Strong builds feel earned; no universal support/movement/reaction convergence. |
| E - Late reward integration | Replacement jobs and Ramza's final identity enter the top tier. | Necromancer, Special Knight, Chapter 4 Ramza. | Older specialist jobs still have reasons to appear in final parties. | Late jobs are exciting without erasing generic specialist identities. |

The intended curve is not "early jobs bad, late jobs good." The intended curve is:

```text
early jobs give foundations;
mid jobs create identity branches;
advanced jobs offer build-defining rewards;
late jobs add top-tier complexity and payoff.
```

## Pacing Levers

Campaign pacing is controlled by four separate levers.

| Lever | Why it matters | Deferred decision |
| --- | --- | --- |
| Job prerequisite depth | Determines when a job can appear without grinding. | Exact prerequisite tree. |
| JP acquisition rate | Determines how quickly a unit can buy deep skills after unlock. | Exact JP gain, JP costs, and whether `JP Boost` changes practical pacing. |
| Equipment and gil tier | Determines whether a job's equipment identity is actually available when the job unlocks. | Shop/chapter tier, prices, item stock, and special equipment timing. |
| Slot friction | Determines whether a powerful secondary/reaction/support/movement can be carried by a shallow active job. | Exact skill costs, slot boundaries, and equipment unlock scope. |

`JP Boost` is therefore not only a campaign convenience. It is a pacing lever. If it accelerates
deep job access enough to move Band D or E power into Band B or C encounters, the campaign curve
fails even if `JP Boost` has no direct combat stats.

Equipment and gil are also pacing levers. A job with broad equipment access is not fully online
until the relevant shop tier and item economy can support it. Special Knight, Ninja, Samurai,
Dragoon, Archer, Chemist, Orator, and caster weapon access all need equipment-tier checks before
final campaign acceptance.

## Full-Package Rule

No early or early-mid build should assemble all of these at once:

- high damage;
- reliable sustain or revive;
- strong mitigation or evasion;
- strong mobility;
- strong control;
- low resource pressure;
- low positioning risk.

A build may be strong in two or three areas if it has clear weaknesses. It becomes a campaign
problem when a five-unit party can cover every weakness too early with low JP and obvious detours.

## Falsifiable Proxies

Campaign feel cannot be accepted by vibe. Every later campaign claim needs a proxy that can be
checked in a table, scenario row, or simulator output.

| Claim | Required proxy |
| --- | --- |
| Party diversity exists. | At least three distinct party journey rows can clear the representative band scenario set, and no single journey majority-dominates the others. A journey majority-dominates if it is best or tied in at least three of five axes while not worst in any: damage, sustain, control, mobility, and safety/risk. |
| Floor is healthy. | P0 clears each band's representative encounter set within a pinned non-optimized level/JP/equipment envelope, without assuming `JP Boost`, guide routing, or optimizer-tier RSM pieces. |
| A job is viable as an active job. | The job appears as an expected active job in at least one party journey row for its intended band before its strongest exportable support/reaction/movement is assumed. |
| A support is not mandatory. | T2/T2.1 incidence does not show the support appearing across most unrelated archetypes in its band. |
| A movement skill is not the default. | T2/T2.1 incidence shows role-specific movement choices still competing in the same band. |
| A reaction is not practical immunity. | T4/T6xPS/T3xT5 rows preserve meaningful enemy hit, damage, status, or attrition pressure. |
| Sustain is healthy. | T3/T3xT5 rows show the party can recover from ordinary pressure but cannot erase repeated mistakes at low action/resource cost. |
| Action economy is healthy. | T5/T10 rows show Haste, Quick, overwatch, intervention, and similar tools create windows rather than loops. |
| Area/global effects are healthy. | T11/T11xT5/T3xT5xT11 rows show target count and duration are paid for through CT, vulnerability, interruption, MP, or low per-tick output. |
| A late job is powerful but not mandatory. | Band E party rows still include older specialist active jobs as rational choices in at least some representative scenarios. |
| Ramza is broad but not dominant. | In every band, Ramza has a strong role but does not beat the protected specialist in that specialist's own row. |

Concrete numeric thresholds are intentionally deferred until the relevant gate exists. The proxy
names the evidence type the later threshold must use.

## Party Journey Scenarios

Every future prerequisite, JP, and equipment-tier draft should be checked against these party
journeys.

Three journeys are mandatory:

- P0 naive/thematic floor party;
- P1 balanced FFT reference party;
- P5 optimizer rush ceiling party.

The other journeys are required wall tests because they expose lopsided-composition failures.

### P0 - Naive Or Thematic Floor Party

Shape:

- Ramza plus four generics chosen by theme or immediate need;
- the player may keep Squire/Chemist too long;
- the player may delay learning an optimal support or secondary;
- the player may pick a favorite weapon or job fantasy before knowing the future tree.

Expected healthy path:

- Band 0: the party can survive first battles with basic Squire/Chemist tools and ordinary attacks.
- Band A: the party discovers at least one clear route into healing, ranged pressure, or magic
  without a guide.
- Band B: first specialist jobs feel like improvements even if the player does not know the best
  future RSM pieces.
- Band C: a "wrong" early branch can still be recovered through midgame jobs without restarting a
  unit's whole identity.
- Band D/E: thematic parties can become strong through build planning, even if they are not the
  optimizer's best route.

Floor failure signs:

- the early game becomes too hard, too slow, or too grindy before first specialization;
- a player who misses `JP Boost` or a specific early support feels punished for many battles;
- Squire/Chemist feel useless before the player can unlock specialists;
- an early job branch traps a unit in a weak campaign path;
- the only practical advice is to follow one optimized route.

Floor proxy:

- P0 must clear each band's representative encounter set inside a pinned non-optimized envelope;
- the envelope must state level range, rough JP budget, available jobs, available equipment/gil tier,
  and assumed learned skills;
- the envelope must not assume `JP Boost`, optimized RSM detours, deep secondaries on shallow
  chassis, or guide-only prerequisite routing;
- floor fails if the P0 party needs optimizer-tier RSM pieces, excessive grind, or a specific hidden
  route to clear ordinary band rows.

### P1 - Balanced FFT Party

Shape:

- Ramza as flexible physical/support trainee;
- one frontline physical;
- one ranged or fast physical;
- one healer/support;
- one offensive caster or controller.

Expected healthy path:

- Band 0: the party can use basic attacks, starter recovery, and early positioning without needing
  optimized JP routing.
- Band A: Squire/Chemist carry basic actions while one unit starts toward healing and one toward
  offense.
- Band B: Knight, Archer, White Mage, Black Mage, Monk, and Thief all offer real first-specialist
  choices.
- Band C: the player can branch into Time/Mystic control, Dragoon reach, Geomancer hybrid, Orator
  guns/speech, or Summoner area magic without abandoning the whole party plan.
- Band D: Samurai/Ninja/Bard/Dancer add powerful but role-specific rewards.
- Band E: Necromancer, Special Knight, and Ramza final form are top-tier options, not mandatory
  replacements for the whole party.

Failure signs:

- the balanced party always rushes the same support path before it can function;
- White Mage or Chemist fully deletes the other's recovery reason to exist;
- Archer becomes only an early unlock step;
- Ramza becomes the best general answer before Chapter 4.

### P2 - Physical-Heavy Party

Shape:

- Ramza plus four mostly physical generics;
- limited magic investment;
- recovery mostly through Chemist, Monk, Squire, or borrowed White Magicks.

Healthy strengths:

- strong weapon-family coverage;
- clear use for swing, thrust, crush, and missile;
- durable frontline plans;
- satisfying late build crafting through Doublehand, Dual Wield, Brawler, equipment unlocks, or
  specialist movement.

Required weaknesses:

- vulnerable to status and magic without deliberate counter-building;
- sustain should cost actions, positioning, items, or risky Monk proximity;
- no early physical route should get top damage plus top durability plus top movement.

Main stress rows:

- Monk secondary must not provide best damage plus best sustain plus revive for most physical units.
- Knight armor/shield unlocks must not become the default patch for every fragile physical job.
- Dragoon spear access must not become the universal anti-mail route outside Dragoon.
- Samurai `Doublehand` and Ninja `Dual Wield` must enter late enough and cost enough that Band B/C
  physical parties still care about Knight, Archer, Monk, Thief, Geomancer, and Dragoon.

### P3 - Caster-Heavy Party

Shape:

- Ramza plus multiple magical or hybrid generics;
- White Mage, Black Mage, Time Mage, Mystic, Summoner, possibly Necromancer later;
- Chemist or Orator may provide gun/item support.

Healthy strengths:

- strong planning around CT, Faith, MP, range, status, and area;
- meaningful difference between direct damage, delayed area, healing, protection, time control,
  spiritual control, and dark-state pressure;
- late caster builds can feel powerful when they solve MP and CT constraints deliberately.

Required weaknesses:

- cloth fragility remains real;
- MP and Silence matter;
- CT delay and target movement matter;
- Shell, Reflect, low Faith, immunity, and spread formations remain valid counters;
- no single support such as `Swiftspell`, `Halve MP`, or broad magic damage boost becomes required
  for every serious caster.

Main stress rows:

- White Mage protection cannot turn into mandatory prebuff upkeep.
- Black Mage cannot make weapon-family planning optional through easy magic dominance.
- Time Mage cannot make Haste/Quick/Swiftspell/Teleport the default caster package.
- Mystic cannot make Faith/MP economy manipulation mandatory.
- Summoner cannot become Black Mage plus bigger ally-safe area by default.
- Necromancer cannot become Calculator again through abstract or global dark targeting.

### P4 - Control And Sustain Party

Shape:

- Knight, Chemist, Monk, Orator, Mystic, Time Mage, or similar control/sustain roles;
- lower raw burst, higher disruption and recovery.

Healthy strengths:

- can win long fights through disruption, morale/spiritual control, recovery, and targeted
  shutdowns;
- makes status and enemy equipment matter;
- creates a distinct playstyle from raw damage parties.

Required weaknesses:

- should be slower than burst parties;
- should struggle against immunities, bosses, spread formations, or enemies with low-value equipment;
- should not create broad practical immunity through stacked mitigation, evasion, auto-heal, and
  action denial.

Main stress rows:

- Auto-Potion plus item support plus armor must not erase attrition.
- Protect/Shell/Wall plus plate/shield/Parry/Aegis must not erase enemy offense.
- Stop/Sleep/Charm/Induration/Condemn/Doom must not become the safest boss plan.
- Bard/Dancer global support or debuff pressure must not make a passive long-fight party the default
  answer.

### P5 - Optimizer Rush Party

Shape:

- the player deliberately routes each unit through jobs for the strongest exportable pieces;
- the player accepts detours if the payoff is strong.

Mandatory rush targets to test:

| Category | Candidate pieces |
| --- | --- |
| Campaign economy | `JP Boost`, treasure/economy movements. |
| Early sustain | `Auto-Potion`, `Item Lore`, `Throw Item`, `First Aid`, `Chakra`, `Revive`, `Lifefont`. |
| Defensive stack | `Equip Armor`, `Equip Shield`, `Parry`, `Shirahadori`, `Mana Shield`, `Dragonheart`, `Vanish`, `Aegis Stance`, `Intervention`. |
| Damage engines | `Brawler`, `Attack Boost`, `Doublehand`, `Dual Wield`, `Bow Mastery`, `Concentration`, `Elemental Focus`. |
| Equipment unlocks | `Equip Bow`, `Equip Guns`, `Equip Polearms`, `Equip Katana`, `Equip Knight Swords`. |
| Mobility | `Move +1`, `Move +2`, `Move +3`, `Teleport`, `Ignore Terrain`, `Ignore Elevation`, `Fly`, jump movement. |
| Action economy | `Haste`, `Quick`, `Critical: Quick`, overwatch, raised-body actions, intervention attacks. |
| Global effects | Bardsong, Dance, Golem, Carbuncle, Wall-like effects. |

Pass condition:

- the optimizer can make strong builds, but cannot cheaply converge all five units on the same small
  set of obvious exports before the advanced campaign bands.

Failure signs:

- one support appears on most physical builds;
- one support appears on most caster builds;
- one movement appears on most late builds regardless of role;
- one reaction appears on most survival builds;
- one secondary becomes the default for most active jobs;
- Ramza plus four generics can assemble full-package power before Band D.

### P6 - Performer And Gender Parity Party

Shape:

- one route includes Bard;
- one equivalent route includes Dancer;
- both are checked for shared reaction/support/movement parity and distinct action identity.

Pass condition:

- Bard and Dancer remain different active jobs through Song versus Dance;
- neither gender gets better reaction/support/movement access;
- no party feels forced to choose gender for mechanical RSM superiority;
- performance is strong in long fights but punishable through pressure, interruption, Silence,
  positioning, or short-fight tempo.

Failure signs:

- Bard or Dancer is mandatory for every optimized party;
- performance global effects replace ordinary healing, damage, or control;
- shared movement/support becomes a gender-locked general build advantage;
- Finale or Last Waltz creates ordinary mapwide instant-win pressure.

## Ramza Campaign Check

Ramza is always present in the validation party, so his progression has to be checked separately.

Expected posture:

- early Ramza can be flexible but should still rely on generic jobs for specialist depth;
- midgame Ramza can bridge physical, support, and magic without becoming the best in each lane;
- Chapter 4 Ramza may be top-tier and comparable to the strongest jobs;
- Ramza's final hybrid identity should not invalidate Knight, White Mage, Black Mage, Time Mage,
  Monk, Archer, or Special Knight inside their protected niches.

Required checks by band:

| Band | Ramza question |
| --- | --- |
| 0 | Does raw-start Ramza feel like a familiar FFT protagonist without carrying the first battles alone? |
| A | Does Ramza preserve starter FFT feel without trivializing Squire/Chemist? |
| B | Can Ramza support the party without replacing the first specialist choices? |
| C | Does Ramza's hybrid path create options without being the best control/caster/frontline at once? |
| D | Does Ramza still need cross-job build planning, or does his chapter kit solve too much alone? |
| E | Is final Ramza broad and powerful but still meaningfully different from Special Knight and generic casters? |

## Cross-Job Timing Invariants

These are not exact numeric rules. They are acceptance tests for future JP and prerequisite drafts.

### I1 - Starter Tools Must Not Become Permanent Defaults

`JP Boost`, `Move +1`, basic Items, Squire utility, and early recovery should help the player get
started. They fail if optimized combat builds still feel wrong without them late in the game.

### I2 - First Specialist Jobs Must Work Before Their Exports

Knight, Archer, White Mage, Black Mage, Monk, and Thief should be useful as active jobs before the
player buys their strongest reaction/support/movement rewards.

If a job is only used to unlock a support and then discarded, its active kit failed the campaign
validation.

### I3 - Deep Secondary On Shallow Chassis Is A Campaign Red Flag

The scariest campaign spike is not always a deep active job. It is often a shallow, durable, fast,
or convenient active job carrying a deep secondary plus strong reaction/support/movement pieces.

Examples to test:

- early durable body carrying a deep caster secondary;
- fast leather body carrying advanced control;
- plate body carrying a late defensive reaction plus strong recovery secondary;
- Ramza carrying a deep generic secondary before his chapter identity should support that breadth.

This is not automatically banned. It is a red flag that must appear in the campaign party matrix and
detour pressure report.

### I4 - Build-Defining Supports Must Be Earned

The following pieces should never be cheap early pickups:

- `Dual Wield`;
- `Doublehand`;
- `Attack Boost`;
- `Brawler`;
- broad caster acceleration or MP economy pieces;
- broad equipment unlocks that erase job identity;
- `Equip Knight Swords`;
- any support that improves both offense and defense for many jobs.

They can be strong. The issue is timing, breadth, and opportunity cost.

### I5 - Strong Reactions Must Not Arrive As Early Difficulty Skips

An early strong reaction changes the whole campaign floor because it works while the player is still
learning the system.

Auto-heal, broad counterattack, evasion, preemptive strike, reraise, MP shield, vanish, or cover-like
reactions should not appear early enough to let the party ignore positioning, damage bands, or enemy
turns.

This applies even when the reaction sounds narrow in an isolated job document.

### I6 - Mobility Must Remain A Choice

The player should keep choosing between mobility identities:

- simple early movement;
- Thief speed movement;
- Dragoon vertical movement;
- Geomancer terrain movement;
- Time Mage teleportation;
- Ninja skirmisher mobility;
- performer positioning;
- Special Knight formation movement.

If one movement skill is correct for most builds, campaign traversal and job identity collapse.

### I7 - Sustain Must Have Texture

Recovery should stay differentiated:

- Chemist is immediate, reliable, item-based, usually single-target;
- White Mage is stronger support throughput with CT, MP, Faith, and vulnerability;
- Monk is risky proximity sustain and revive;
- Bard is slow global support over time;
- Necromancer drain is late, conditional, and dark-state oriented;
- Ramza may support but should not replace dedicated sustain jobs.

No route should become "the only correct healer."

### I8 - Protection Must Not Become Upkeep

Protect, Shell, Wall, Golem, Carbuncle, Aegis, Intercede, Kiyomori-like draws, Mana Shield, Parry,
Shirahadori, Vanish, and Dragonheart all touch survival.

They are healthy when they answer a threat. They fail if the player should maintain them by default
before every serious fight.

### I9 - Control Must Not Replace Damage

Slow, Stop, Sleep, Charm, Doom, Death Mark, Condemn, Induration, Repose, Challenge, Intercede,
Pinning Shot, Steal Heart, and speech/status pressure should create tactical windows.

They fail if the safest answer to hard enemies is usually to deny actions until the fight is solved.

### I10 - Late Jobs Must Be Powerful, Not Mandatory

Necromancer and Special Knight are allowed to be exciting late rewards.

They fail if:

- every caster wants Necromancy as secondary;
- every frontline wants Special Knight protection tools;
- either job becomes the best generic shell for multiple older identities;
- either job is required to make late-game parties feel complete.

## Early Power Spike Watchlist

These combinations need explicit validation before final progression acceptance.

The table below binds each campaign risk to the validation gate that must eventually quantify it.

| Watch | Primary gates | Campaign question |
| --- | --- | --- |
| W1 - Starter sustain compression | T3/T3xT5, T2/T2.1 | Can early recovery erase attrition before the player has real specialist tradeoffs? |
| W2 - Early physical full package | F5, T2/T2.1, T3, T4, T6xT7 | Can a physical party cover damage, sustain, range, durability, and control too early? |
| W3 - Time Mage snowball | T5, T10, T2/T2.1 | Do Haste, Quick, Swiftspell, Teleport, and timing tools create loops or default routing? |
| W4 - Defensive practical immunity | T6xPS, T4, T3xT5, T2/T2.1 | Do mitigation, evasion, revive, and shield/armor stacks preserve enemy agency? |
| W5 - Advanced physical convergence | T2/T2.1, F5, T6xT7 | Do Dual Wield, Doublehand, Attack Boost, Brawler, or premium equipment unlocks dominate physical builds? |
| W6 - Caster economy convergence | T9, T10, F4, T2/T2.1 | Do caster speed, MP, and damage supports collapse into one best package? |
| W7 - Global performer default | T11xT5, T3xT5xT11, T2/T2.1 | Does a Bard/Dancer slot become default party infrastructure? |
| W8 - Late replacement overreach | T3xT5xT8, T6xPS, T6xT7, T8, F5 | Do Necromancer, Special Knight, or late Ramza delete older specialist reasons to exist? |
| W9 - Grind-to-break pacing | A1/A2/A3/A4, T2/T2.1 | Can JP acquisition or `JP Boost` move deep RSM power into early encounter bands? |
| W10 - Equipment-tier breakpoints | A1/A2/A3/A4, F5 | Does shop/gil/equipment access bring late weapon or armor identity online too early? |

`A1` through `A4` are the campaign artifacts defined later in this document. They are not currently
implemented harness gates, but they are required campaign-level evidence before exact prerequisite
or JP acceptance.

### W1 - Starter Sustain Compression

Risk:

```text
Chemist Items + Auto-Potion + Item Lore + Throw Item
Squire First Aid
Monk Chakra / Revive
White Mage Cure / Raise
```

Why it matters:

If these tools arrive too early and stack too easily, Band A/B attrition disappears and the player
can play carelessly.

Required proof:

- T3/T3xT5 rows for early damage bands;
- T2/T2.1 incidence for `Auto-Potion`, `Item Lore`, and `Throw Item`;
- party-level rows where only one unit is the healer and rows where two units share recovery.

### W2 - Early Physical Full Package

Risk:

```text
Knight durability + Monk damage/sustain + Archer range + Thief speed + Squire utility
```

Why it matters:

This is the first point where a physical-heavy player may stop caring about caster routes.

Required proof:

- physical-heavy party journey through Bands B/C;
- armor matchup coverage check so plate/mail/leather/cloth still ask for different answers;
- no early access to `Brawler`, `Equip Armor`, `Equip Shield`, `Concentration`, or high movement in
  a way that makes one unit solve too many roles.

### W3 - Time Mage Snowball

Risk:

```text
Haste + Slow + Quick + Swiftspell + Teleport
```

Why it matters:

Time Mage can accidentally become the best job line for every caster and many physical jobs.

Required proof:

- T5 timing rows;
- T10 action-economy rows;
- T2.1 incidence for `Swiftspell` and `Teleport`;
- explicit "no permanent upkeep" rows for Haste effects.

### W4 - Defensive Practical Immunity

Risk:

```text
plate/shield + Protect/Shell/Wall + Parry/Shirahadori/Mana Shield/Vanish/Dragonheart/Aegis
```

Why it matters:

The party should be able to build durable units, but broad practical immunity ruins enemy turns.

Required proof:

- T6xPS mitigation stacking;
- T4 evasion/reaction rows;
- T3/T5 revive/survival rows;
- T2.1 incidence for defensive reactions and armor/shield unlocks.

### W5 - Advanced Physical Convergence

Risk:

```text
Dual Wield / Doublehand / Attack Boost / Brawler / Equip Knight Swords
```

Why it matters:

These can make all physical planning collapse into a small set of late supports.

Required proof:

- T2/T2.1 build incidence with physical archetypes separated by weapon family;
- F5 re-sims for real roster rows, not only formula anchor jobs;
- no support should dominate across swing, thrust, crush, and missile physical plans.

### W6 - Caster Economy Convergence

Risk:

```text
Swiftspell + Halve MP + Manafont + Summon Focus + broad magic damage supports
```

Why it matters:

If these stack too efficiently, caster builds stop choosing between speed, endurance, damage, and
utility.

Required proof:

- T9 MP/resource economy rows;
- T10 if action grants or recursion exist;
- F4 magic/physical coexistence;
- T2.1 incidence across White, Black, Time, Mystic, Summoner, Necromancer, and Ramza.

### W7 - Global Performer Default

Risk:

```text
one Bard or Dancer running global value every fight
```

Why it matters:

Global effects can become invisible mandatory party infrastructure.

Required proof:

- T11xT5 sustained area throughput;
- T3xT5xT11 HP recovery/damage rows;
- interruption and pressure scenario rows;
- party journeys with and without a performer.

### W8 - Late Replacement Overreach

Risk:

```text
Necromancer becomes default late caster secondary
Special Knight becomes default late frontline shell
Ramza becomes best generalist before final campaign
```

Why it matters:

Late jobs should reward progression without deleting the ecosystem built before them.

Required proof:

- Band E party rows with old specialist jobs still present;
- T3xT5xT8 for corpse/undead mechanics;
- T6xPS/T6xT7/T8 for Special Knight protection and guard pressure;
- Ramza chapter rows against protected generic niches.

### W9 - Grind-To-Break Pacing

Risk:

```text
JP Boost or ordinary grind lets the player reach deep jobs/RSM pieces while still facing early
encounter bands.
```

Why it matters:

The job tree can look balanced on paper and still fail if JP acquisition makes Band D/E power
practical during Band B/C encounters.

Required proof:

- A2 unlock and JP pacing ledger;
- A3 five-unit stack sheet for optimizer rush;
- T2/T2.1 incidence for early access to high-value RSM pieces;
- explicit rows with `JP Boost` learned early and rows without `JP Boost`.

### W10 - Equipment-Tier Breakpoints

Risk:

```text
a job unlocks at the intended time, but its strongest weapon, armor, shield, item, or shop tier
arrives earlier or later than the job's tactical role assumes.
```

Why it matters:

Equipment timing can create hidden spikes or hidden dead zones. A job with strong access but weak
available gear may feel bad. A job with premium gear too early may overperform even with modest
skills.

Required proof:

- A2 pacing ledger includes equipment/gil tier for major equipment identities;
- F5 real-roster re-sim for jobs whose equipment or armor profile changes;
- party rows where the job is unlocked before its premium equipment is available;
- party rows where the equipment arrives before the intended advanced job or support gate.

## Required Validation Artifacts

Before final prerequisite or JP acceptance, create these artifacts.

### A1 - Campaign Party Matrix

A table with one row per party journey and campaign band.

Columns:

```text
party_id
encounter_band
job_depth_overlay
ramza_chapter_state
available_jobs
expected_active_jobs
expected_secondaries
expected_reactions
expected_supports
expected_movements
equipment_gil_tier
main_damage_modes
recovery_plan
control_plan
mobility_plan
resource_pressure
floor_or_ceiling
proxy
required_gate
red_flags
verdict
```

### A2 - Unlock And JP Pacing Ledger

A table listing every job's major exports by expected acquisition band.

Columns:

```text
job
piece
slot
intended_encounter_band
minimum_job_depth
power_category
healthy_primary_users
dangerous_off-job_users
equipment_or_gil_dependency
required_gate
notes
```

This should include every support, reaction, movement, and equipment unlock before concrete JP
numbers are accepted.

### A3 - Five-Unit Stack Sheet

For each party journey and band, record the strongest realistic five-unit stack the optimizer can
assemble.

Minimum categories:

- strongest damage unit;
- strongest sustain unit;
- strongest mitigation unit;
- strongest control unit;
- strongest mobility unit;
- Ramza's contribution.

The sheet should mark whether any unit carries more than three major strengths at once.

It should also mark whether the stack is:

- floor-realistic: likely for a non-optimizer;
- optimizer-realistic: reachable by a knowledgeable player without extreme grind;
- grind-only: reachable only by intentionally overgrinding earlier encounters.

Because A3 is adversarial, final ceiling claims should be constructed dual-independently:

- GPT constructs the strongest reachable five-unit stack for a band;
- Claude constructs an independent strongest reachable stack for the same band;
- differences are reconciled before declaring that a ceiling row is safe.

### A4 - Detour Pressure Report

For each job, answer:

```text
Would a player detour through this job mostly for a passive export?
Would that detour happen before the job is fun as an active job?
Does the export help the job's own identity more than it helps unrelated shells?
What other support/reaction/movement competes with it?
```

Jobs with high detour pressure need later JP/prerequisite friction or narrower effects.

### A5 - Representative Encounter Row Set

The campaign matrix needs representative encounter rows, even before a full battle simulator exists.

Minimum row families:

- early physical pressure;
- early mixed pressure;
- plate-heavy enemies;
- mail-heavy enemies;
- leather skirmishers;
- cloth caster pressure;
- ranged pressure;
- magic pressure;
- status/control pressure;
- spread formation;
- clustered formation;
- vertical/terrain map;
- boss-like or status-resistant target;
- undead or corpse-relevant target when Necromancer is in scope.

Each concrete row must have a source anchor.

Allowed anchors:

- a named vanilla IVC encounter row from the fixed encounter curve;
- a synthetic enemy stat block pinned in the same style as formula input bundles.

Minimum anchor fields:

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

Each row should state which existing or sprint gate owns the relevant proof. If no gate exists, the
row is design-only and cannot produce final numeric acceptance.

## Validation Sequencing

This document can be accepted as a framework before the infra sprint finishes.

It cannot accept final campaign balance numbers before the gates exist.

Sequence:

1. Accept this campaign framework after Claude review.
2. Build the campaign artifacts A1 through A5 with provisional qualitative rows.
3. Continue the infra sprint from `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`.
4. As each gate becomes executable, replace qualitative row verdicts with gate-backed outputs.
5. Only after the required gates exist, accept concrete prerequisite, JP, equipment-tier, and skill
   numeric tables.

This keeps campaign validation tied to the infra sprint instead of creating a parallel vibes-only
process.

A1 through A5 stay local to this document for now. They should not be promoted into
`docs/job-balance/07-validation-infrastructure-roadmap.md` until a specific artifact becomes an
executable dual-gated track with a pinned bundle, independent checker, and mismatch policy.

## Provisional Campaign Verdict

The V1 ecosystem is directionally healthy but not yet campaign-proven.

Healthy signals:

- every major tactical lane has at least one intended home;
- early jobs have reasons to exist beyond prerequisites;
- Archer, Monk, Dragoon, Samurai, Ninja, Chemist, Orator, Bard/Dancer, Necromancer, and Special
  Knight all have distinct protected fantasies;
- no accepted document intentionally preserves Calculator or Mime's unhealthy identity;
- most dangerous effects are already tied to named validation gates instead of being silently
  accepted.

Main unresolved campaign risks:

- global support convergence for physical builds;
- global support convergence for caster builds;
- mobility convergence around Teleport, Move +3, Ignore Elevation, or Fly;
- sustain compression from Items, Monk, White Mage, Bard, and reactions;
- defensive practical immunity from mitigation/evasion/revive stacks;
- Time Mage action-economy snowball;
- performer global effects becoming a default party slot;
- Ramza and late replacement jobs over-covering older roles.

Therefore, the next accepted step should be campaign-matrix validation before exact prerequisite and
JP tables are finalized.

## Claude Review Request

Claude should review:

- whether the campaign bands correctly represent the intended player journey;
- whether the party journey scenarios cover the important ways a player will build a five-unit team;
- whether any early or midgame power spike is missing;
- whether the required validation artifacts are sufficient before exact JP/prerequisite work;
- whether this document should update `docs/job-balance/07-validation-infrastructure-roadmap.md`
  with new campaign-level tracks.

Claude review verdict: Accepted as campaign gameplay validation framework by claude-opus-4-8 on
2026-06-21.

Review notes:

- campaign bands are accepted as encounter/story difficulty bands with job-depth overlay separated;
- Band 0, floor checks, and P0 naive/thematic party are accepted;
- falsifiable proxies are accepted, including the majority-dominance party diversity rule;
- red-flag-to-gate map W1-W10 is accepted;
- pacing levers for prerequisites, JP acquisition, equipment/gil tier, and slot friction are
  accepted;
- A1-A5 remain local to this document until a specific artifact becomes executable and dual-gated;
- concrete pass/fail thresholds remain deferred to the infra sprint gates.
