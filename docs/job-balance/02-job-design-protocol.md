# Job Design Protocol V0

Status: Accepted
Date: 2026-06-20
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/formula-balance/05-formula-proposal-protocol.md`
- `docs/formula-balance/08-scenario-set-v0.md`
- `docs/formula-balance/10-mitigation-and-scaling-policy-v0.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.json`

## Purpose

This protocol defines how Generic Chronicle job proposals must be written, reviewed, simulated,
and accepted.

It protects two things at the same time:

- the user's accepted high-level direction for job balance;
- the already validated formula-balance v0.2 policy.

No job redesign should be accepted because it sounds good in isolation. It must preserve FFT build
crafting, respect the weapon/armor formula model, and survive representative checks.

## Source Of Truth Order

When documents conflict, use this order until a later accepted document supersedes it:

1. `docs/job-balance/00-high-level-direction.md`
2. `docs/job-balance/01-cross-job-build-principles.md`
3. `docs/formula-balance/11-validated-policy-v0.2.md`
4. `docs/formula-balance/10-mitigation-and-scaling-policy-v0.md`
5. `docs/formula-balance/08-scenario-set-v0.md`
6. `work/sim-inputs-v0.2.json`

The job phase inherits formula v0.2 as a constraint. It does not get to quietly re-litigate the
combat model.

Authority is split by kind. For design intent and direction, the order above applies. For numeric
or simulation values such as multipliers, WP, penetration, armor response, stress-engine constants,
and clamps, the pinned bundle `work/sim-inputs-v0.2.json` and its accepted successors are
authoritative over prose docs. A prose/bundle numeric conflict is a documentation bug to fix in
the prose, not a reason to change validated numbers.

## Status Labels

Job-balance artifacts should use these statuses:

| Status | Meaning |
| --- | --- |
| `Exploratory` | Idea or note only; not ready for review. |
| `Proposed for Claude review` | Written by GPT and ready for blocking review. |
| `Needs revision` | Reviewed, but must change before acceptance. |
| `Needs simulation` | Concept approved, but representative checks are missing. |
| `Accepted for provisional design` | Approved by Claude and valid against current provisional formula data. |
| `Accepted` | Approved after the required verified-baseline re-sim gates are satisfied. |
| `Rejected` | Violates direction, fails checks, or cannot be repaired without redesign. |

Until the Windows `04` baseline captures real IVC weapon data and formula-balance v1 exists,
detailed numeric job proposals should not claim final implementation acceptance.

## Required Proposal Shape

Every job proposal must use a consistent structure.

```text
Job:
Status:
Version:
Depends on:

Vanilla role:
Vanilla problems:
Accepted high-level role:
Primary role:
Secondary tags:
Growth profile: physical / magical / hybrid
Current multipliers:
Proposed multipliers:
Equipment access:
Armor class as target:
Supported damage modes:
Formula v0.2 coupling:

Action skillset goals:
Mechanic preservation list:
Per-skill turn-value audit:
Visibility and complexity audit:
Reaction skills:
Support skills:
Movement skills:
JP progression:
Prerequisite changes:
Gender/equipment restrictions:

Cross-job build hooks:
Expected strong builds:
Expected weaknesses:
Expected counters:
Ramza / unique-job interaction:

Scenario/check plan:
Formula re-sim requirement:
Implementation assumptions:
Open proof needs:
Claude review verdict:
```

Role-map documents can use a shorter form, but detailed job proposals must answer the full
template.

## Compact Role Taxonomy

Role maps should stay readable.

Each job gets:

- one primary role;
- up to two secondary tags;
- one growth profile.

Recommended primary roles:

| Role | Meaning |
| --- | --- |
| `melee-physical` | Direct physical combat and weapon pressure. |
| `ranged-physical` | Projectile pressure, range control, or ranged reliability. |
| `caster-offense` | Magical damage or spell pressure. |
| `caster-support` | Healing, buffs, protection, or MP/Faith utility. |
| `controller` | Status, debuff, displacement, CT, morale, or action denial. |
| `hybrid` | Meaningful PA/MA or physical/magical blend. |
| `performer` | Bard/Dancer-style performance pressure or support. |
| `specialist` | Narrow but important unique tactical niche. |
| `late-reward` | Late-game job whose value is complexity, support power, or broad build payoff. |
| `protagonist` | Ramza-only exception role. |

Secondary tags should be concrete, such as `crush`, `missile`, `anti-plate`, `anti-mail`, `fast`,
`durable`, `evasive`, `Faith`, `Brave`, `undead`, `recruit`, `terrain`, or `economy`.

Avoid broad tag soup. If every job has the same tags, the taxonomy is not doing work.

## Formula-Balance Hard Gates

### Gate F1 - Weapon Family Identity

The v0.2 weapon families are inherited constraints:

| Family | Routine | Type | Design obligation |
| --- | --- | --- | --- |
| `sword` | `pa_wp` | `swing` | Familiar stable physical pressure. |
| `knight_sword` | `br_pa_wp` | `swing` | Premium Brave-linked physical pressure. |
| `katana` | `br_pa_wp` | `swing` | Brave-linked disciplined physical identity. |
| `knife` | `spd_pa_wp` | `thrust` | Fast precision and anti-mail access. |
| `ninja_blade` | `spd_pa_wp` | `swing` | Fast melee burst with dual-wield lens. |
| `longbow` | `spd_pa_wp` | `missile` | Ranged positional pressure. |
| `crossbow` | `pa_wp` | `missile` | Direct ranged armor pressure. |
| `gun` | `wp_wp` | `missile` | PA-independent ranged consistency. |
| `spear` | `pa_wp` | `thrust` | Reach and anti-mail physical pressure. |
| `staff` | `ma_wp` | `crush` | MA-based impact route. |
| `rod` | `ma_wp` | `crush` | Caster backup impact route. |
| `pole` | `ma_wp` | `crush` | MA/reach impact route. |
| `axe` | `rdm_pa_wp` | `crush` | Volatile impact route. |
| `flail` | `rdm_pa_wp` | `crush` | Higher-ceiling volatile impact route. |
| `fists` | `br_pa_pa` | `crush` | Brave-linked unarmed impact route. |
| `instrument` | `pampa_wp` | `missile` | Performer ranged/support route. |
| `book` | `pampa_wp` | `crush` | Hybrid impact route. |
| `cloth_weapon` | `pampa_wp` | `swing` | Hybrid light swing route. |
| `bag` | `rdm_pa_wp` | `crush` | Volatile oddball impact route. |

Job design may adjust which jobs can use which families, but it must not reroute a family to a
different routine, type, or penetration identity without triggering formula re-simulation and
Claude reapproval.

### Gate F2 - Armor Matchup Identity

The armor response model is inherited:

| Armor class | Swing | Thrust | Crush | Missile |
| --- | ---: | ---: | ---: | ---: |
| `plate` | 0.65 | 0.65 | 1.15 | 0.80 |
| `mail` | 0.75 | 1.10 | 0.95 | 1.10 |
| `leather` | 0.95 | 0.95 | 1.00 | 0.95 |
| `cloth` | 1.00 | 1.00 | 1.00 | 1.00 |

Job equipment access must support this ecology:

- plate must create demand for crush;
- mail must create demand for thrust and missile;
- leather should not erase identity through excessive mitigation;
- cloth should remain physically vulnerable but tactically valuable through magic, speed, MP,
  status, evasion, or utility.

### Gate F3 - Protected Stress Engines

Formula v0.2 validated coexistence with these stress engines:

- Two Hands: `1.80`;
- Two Swords: `2` hits;
- Attack Boost: `1.3333333333333333`;
- high Brave stress: `97`;
- magic Faith floor: `0.60`;
- Shell multiplier: `0.667`.

Job design must not nerf, remove, or redefine these engines without explicit reapproval from
Claude and a new formula re-sim.

This does not mean these skills must stay in the same jobs, JP costs, or exact learn path. It
means their combat role in the validated formula model cannot be changed casually.

### Gate F4 - Magic/Physical Coexistence

The v0.2 coexistence reference is:

```text
magic / top physical = 281 / 415 = 0.6771
```

Job proposals that change MA multipliers, Faith access, spell availability, Shell access, Arcane
Strength-like effects, MP economy, or physical stress ceilings must report whether this coexistence
ratio plausibly drifts.

Large drift is allowed only with a written reason and re-simulation.

### Gate F5 - No Silent Formula Drift

Any change to the following is formula-affecting until proven otherwise:

- job multipliers for HP, PA, MA, Speed, or MP;
- job equipment compatibility;
- growth/effective-stat model;
- weapon-family access;
- armor-class access;
- Brave/Faith manipulation;
- support engines that alter damage, hit rate, durability, or action economy;
- reaction engines that change expected incoming or outgoing damage;
- movement engines that change exposure or reach enough to affect scenario outcomes.

Formula-affecting proposals must run the cross-phase re-sim gate before final acceptance.

## Cross-Phase Re-Sim Gate

The re-sim gate is mandatory whenever Gate F5 is triggered.

Minimum required evidence:

1. Run the formula-balance scenario set from `docs/formula-balance/08-scenario-set-v0.md`, or its
   accepted successor.
2. Use `tools/sim_damage.py`, or a documented successor harness, against the current pinned input
   bundle.
3. Recompute the v0.2 scorecard metrics:
   - family viability;
   - no dominance;
   - scale band;
   - magic coexistence;
   - plate matchup.
4. Preserve the dual-independent review gate:
   - GPT harness output;
   - Claude independent checker or independent recomputation path;
   - `0` mismatches on agreed rows, or documented reconciliation before acceptance.
5. Label all outputs with the formula input version, scenario-set version, and job proposal version.
6. If a proposal creates a new job chassis or changes a job's equipment, armor class, or multipliers,
   include that job's actual roster row in the re-sim bundle. Anchor jobs are useful controls, but
   they are not sufficient to prove no-dominance for a new or materially changed chassis.

If real IVC weapon data is still missing, the strongest possible status is:

```text
Accepted for provisional design
```

After `work/baseline_weapons.csv` exists and formula-balance v1 is accepted, affected job
proposals must be rechecked before they can become final `Accepted` implementation data.

## Job-Balance Check Matrix

Not every proposal needs the same evidence. Use the smallest check that covers the risk.

| Proposal type | Minimum evidence |
| --- | --- |
| Pure lore/flavor wording | Claude review only. |
| Role-map classification | Consistency check against docs 00/01/11 and `work/sim-inputs-v0.2.json`. |
| JP-only cost change | Progression review plus mandatory-skill risk check if it affects global pieces. |
| Prerequisite-only change | Progression timing review plus early-combo risk check. |
| Action skill redesign | Scenario check for target role, CT/MP/range/status/accuracy, and counters. |
| Reaction/support/move redesign | Mandatory-piece, immunity, universal-build, and opportunity-cost checks. |
| Equipment access change | Formula ecology check and usually cross-phase re-sim. |
| Job multiplier change | Cross-phase re-sim. |
| Growth profile change | Growth fairness check and cross-phase re-sim if effective stats change. |
| Weapon family formula/type change | Formula proposal protocol plus cross-phase re-sim. |

## General Hard Gates

These gates apply to every detailed job proposal.

### Gate G1 - FFT Build Texture

A proposal fails if it removes the core FFT pleasure of cross-job planning.

Red flags:

- the active job stops mattering;
- the same secondary/reaction/support/movement package becomes correct for most jobs;
- the job's best build ignores its equipment identity;
- the proposal makes build planning simpler by making one answer obvious.

### Gate G2 - Not Allowed List

A proposal fails if it creates a build, skill, or job that broadly ignores:

- map structure;
- target profile;
- CT;
- MP;
- hit chance;
- Faith;
- Brave;
- armor class;
- weapon family;
- equipment access;
- positioning.

A proposal also fails if it makes most other choices feel foolish or creates a reaction/support/
movement skill that becomes mandatory for serious builds.

### Gate G3 - No Mandatory Job

No generic job should become mandatory for a healthy party.

A job may own a unique role, such as Archer being the main bow job, but the game should not imply
that every serious party must field that job. The role should be desirable and distinct, not a
required tax.

Detailed job proposals must show at least one viable party plan that uses the job and at least
one viable party plan that does not, unless the job is Ramza-only or explicitly story-bound.

### Gate G4 - Skill Value

A proposal fails if a skill consumes a turn, slot, JP, item stock, or campaign opportunity without
creating meaningful value in its intended layer.

Combat-facing action skills must answer:

- what is the immediate or predictable battle impact?
- why is that impact worth the turn cost?
- what common situation makes this better than attacking?
- what counter, cost, miss risk, or target restriction keeps it from becoming automatic?
- what system, job identity, or build route would break if the skill were removed?

Campaign-facing action skills must answer:

- what long-term value is created for economy, roster, recruitment, breeding, poach, unlocks, or
  build planning?
- why is that value worth the turn, JP, risk, or opportunity cost?
- why does the action not become mandatory grind?
- why does the action not become a dead option once combat optimization is understood?
- what system would become poorer or disappear if the skill were removed?

If the answer is only "it is thematic", "it might matter after several low-impact uses", "it is
safe", or "it has a small modifier", the skill fails the gate. Combat setup is allowed only when it
creates a meaningful payoff window that a reasonable player would plan around. Campaign setup is
allowed when it protects or enriches a real long-term FFT system, even if it is not the strongest
combat action in the current battle.

### Gate G5 - Core System Preservation

A proposal fails if it removes an existing FFT campaign or roster subsystem without naming the
replacement route.

The job pass may defer monster balance, economy tuning, or permanent-state tuning, but it must not
delete access to systems such as:

- monster recruitment and breeding;
- `Tame`;
- `Beast Tongue`;
- `Poach`;
- permanent recruit routes;
- Brave/Faith variance and long-term build planning.

When a skill is cut, moved, or deferred, the proposal must state one of:

- the subsystem remains available through another named skill or job;
- the subsystem is moved to a named future pass with an explicit access promise;
- the user explicitly approved removing that subsystem.

### Gate G5a - External Mechanics Must Justify Friction

A proposal fails if it changes external or campaign-facing mechanics mainly as a balance shortcut.

Do not add custom permanent-stat rates, caps, per-battle limits, recruitment restrictions, economy
restrictions, or extra grind friction unless the proposal names the player-facing benefit and
explains why battle-facing tuning is insufficient. If the rule mostly makes desired builds harder to
assemble, preserve the familiar FFT external mechanic and tune the combat effect, access timing,
visibility, cost, reliability, target restrictions, or counterplay instead.

### Gate G6 - Preserve Good Mechanics Through Revision

A proposal fails if a revision discards useful mechanics only because the previous version was too
complex.

When revising or rejecting an earlier package, the new proposal must include a mechanic preservation
list:

- mechanics worth preserving;
- why each mechanic was valuable;
- what made the old expression too complex, opaque, weak, or dominant;
- the simpler visible expression being kept, merged, moved, or reworked;
- mechanics being fully cut, with a reason stronger than "this is easier."

Knight and Archer are the reference warning: their early redesigns contained valuable ideas, but
some status and mark tracking could become hard for the player to follow. Future passes should
simplify those ideas, not erase the creativity.

### Gate G7 - Visible State And Complexity Budget

A proposal fails if it depends on hidden marks, invisible state, or too many overlapping custom
tracking rules.

Every new status, mark, wound, exposure, charge, tile zone, forced-target rule, delayed threat, or
conditional flag must define:

- the player-visible representation;
- who or what carries it: unit, tile, equipment slot, action, or party state;
- duration or expiration timing;
- stacking rules;
- consumption rules;
- whether multiple units can apply or consume it;
- what happens if the target dies, moves, becomes illegal, is cured, or the source changes jobs.

If the engine cannot show the state clearly, the proposal must use one of these alternatives:

- reuse an existing visible vanilla status;
- use a direct stat, CT, HP, MP, equipment, or position change;
- make the effect immediate instead of persistent;
- fold the mechanic into a simpler existing state;
- cut the mechanic.

As a default readability budget, a normal job should use only a small signature vocabulary of custom
visible states. Exceeding that budget requires an explicit explanation of why the job remains easy to
read in battle.

## Benchmark Population Integrity

Percentage gates are only meaningful if the benchmark populations are representative.

### Accepted Distinct Strong Build Set

The strong build set used for mandatory-skill checks must include at least one credible late/stress
build for every active primary role in the current role map.

Before job role-map v0 exists, the provisional required coverage is:

- melee physical;
- ranged physical;
- caster offense;
- caster support;
- controller;
- hybrid;
- performer;
- specialist;
- late-reward;
- Ramza/protagonist.

It must also cover:

- every armor class as a target profile: `plate`, `mail`, `leather`, `cloth`;
- every physical damage mode: `swing`, `thrust`, `crush`, `missile`;
- magic/spirit pressure;
- at least one Brave-sensitive build;
- at least one Faith-sensitive build;
- at least one Speed-sensitive build.

The set must not be curated to make a favored reaction, support, movement, job, or weapon pass.

### Scenario Archetype Set

The universal-build check must cover at least:

- plate-heavy physical enemies;
- mail-heavy physical enemies;
- leather skirmishers;
- cloth casters;
- ranged pressure;
- magic pressure;
- evasive targets;
- status-resistant targets;
- boss-like targets;
- undead or special targets where relevant.

Each archetype should have early, mid, late, and stress variants when the proposal can affect that
phase. If a phase is not applicable, the proposal must say why.

### Benchmark Scope Discipline

Do not compare unlike lenses as if they were one population.

Examples:

- single-hit builds are not judged against dual-wield totals unless the lens is explicitly
  dual-wield;
- volatile maximum damage does not raise the benchmark for stable families;
- support-engine results are not used to declare unsupported baselines invalid;
- Ramza's protagonist-flexibility check is separate from generic-job niche dominance.

## Required Scenario Rows

Detailed job proposals must declare which of these rows apply. A row can be skipped only with a
written reason.

### Phase Rows

| Row id | Purpose |
| --- | --- |
| `J-EARLY-SELF` | Job using its own kit at first plausible access, no late borrowed support. |
| `J-EARLY-PARTY` | Job in a normal early party, checking whether it contributes without grinding. |
| `J-MID-SELF` | Job with its core identity online but not optimized. |
| `J-MID-PARTY` | Job with a plausible secondary and normal party support. |
| `J-LATE-SELF` | Job's native late-game value without relying on one mandatory external piece. |
| `J-LATE-BUILD` | A strong cross-job build using the job as active or key donor. |
| `J-STRESS-ENGINE` | Dominant support/reaction/movement engines stacked where legal. |
| `J-STRESS-COUNTER` | The same strong build against its intended counters or bad matchups. |
| `J-SOLO-WITH-NPC` | Candidate job as the only optimized player unit alongside baseline allies/NPC-like support. |
| `J-PARTY-NO-JOB` | A viable comparable party plan that does not include the candidate job. |

### Mandatory-Piece Rows

| Row id | Purpose |
| --- | --- |
| `M-RSM-COUNT-LATE` | Count reaction/support/movement incidence across accepted late strong builds. |
| `M-RSM-COUNT-STRESS` | Count incidence across stress builds. |
| `M-SECONDARY-COUNT` | Count repeated secondary skillset use across strong builds. |
| `M-EQUIP-UNLOCK` | Check whether one equip support erases too many job equipment identities. |

Fail/warning thresholds come from `01-cross-job-build-principles.md`.

### Universal-Build Rows

| Row id | Purpose |
| --- | --- |
| `U-ARCHETYPE-COVERAGE` | Run candidate strong build across all required archetypes. |
| `U-TOP-SHARE` | Count where the build is top or within 95% of top. |
| `U-COUNTERPLAY` | Verify the build has at least one practical counter or weak matchup. |
| `U-RAMZA-NICHE` | For Chapter 4 Ramza, verify no generic job's signature niche is strictly dominated. |

Ramza's Chapter 4 job is allowed broad flexibility, but not specialist dominance.

### Immunity Rows

| Row id | Pressure checked |
| --- | --- |
| `I-PHYS-SWING` | Expected negation against swing. |
| `I-PHYS-THRUST` | Expected negation against thrust. |
| `I-PHYS-CRUSH` | Expected negation against crush. |
| `I-PHYS-MISSILE` | Expected negation against missile. |
| `I-MAGIC` | Expected negation against magic damage. |
| `I-SPIRIT-STATUS` | Expected negation against spirit/status pressure. |
| `I-MIXED-ROUND` | Survival against a mixed physical/magic enemy round. |
| `I-ATTRITION` | Whether automatic healing or resource loops erase damage over time. |

Broad negation above the accepted fail threshold is not allowed. True immunity is only acceptable
for narrow, explicit, counterable cases.

### Formula Re-Sim Rows

When Gate F5 triggers, use the formula rows from `08-scenario-set-v0.md` and preserve the five
v0.2 metrics. At minimum, include:

- early, mid, late, and stress phase coverage;
- average and strong physical attackers;
- fast attacker;
- ranged attacker;
- magic attacker;
- hybrid attacker;
- light, durable, magic-relevant, and evasive targets;
- stress rows for Two Hands, Two Swords, Attack Boost, Martial Arts/Brawler, Brave/Faith, and
  accuracy where legal.

## Skill Design Gates

### Action Abilities

Action abilities must be tactical choices.

Each action should state at least one reason to choose it and one reason not to choose it:

- CT;
- MP;
- range;
- area;
- vertical tolerance;
- accuracy;
- status chance;
- elemental profile;
- armor matchup;
- Brave/Faith interaction;
- weapon requirement;
- positional exposure;
- friendly-fire or target restrictions.

Each action must also include a skill-value audit. Use the combat lens for combat-facing actions and
the campaign lens for campaign/economy/roster actions.

| Question | Required answer |
| --- | --- |
| If combat-facing, why use this instead of attacking? | Name the target profile, battle state, or party setup where this is the better turn. |
| If campaign-facing, why spend resources on this? | Name the economy, roster, monster, breeding, poach, recruitment, unlock, or long-term build value. |
| What immediate, predictable, or durable impact does it create? | Damage swing, denied action, kill window, protection, positioning change, combo setup, item access, recruit access, breeding access, poach access, or another explicit campaign-system payoff. |
| What makes the payoff visible? | Visible status, clear number, CT movement, equipment change, HP/MP swing, obvious AI/control change, roster change, item acquisition, unlock, or other durable feedback. |
| What prevents spam or mandatory grind? | Accuracy, immunity, CT/MP/item cost, range, position, target limits, duration, counterplay, opportunity cost, encounter limits, roster limits, or campaign pacing. |
| What breaks if this skill is removed? | Job identity, monster/breeding/poach route, build route, counterplay route, economy route, or nothing. If nothing breaks, cut or merge it. |

Debuffs and setup actions need special scrutiny. A low-impact debuff that does not change the next
turns enough to beat attacking is not a tactical choice if it is presented as a combat action. If a
caster can be killed faster by damage than by spending a turn on a tiny Faith change, that Faith
action is underdesigned unless it has a separate, explicit long-term build purpose.

Actions that create new states must also pass Gate G7. A clever action that requires the player to
remember invisible marks, hidden durations, or several similar custom flags is not acceptable even if
the math is balanced.

Pure vertical replacement is a warning unless the action is intentionally a late-game upgrade with
a real cost.

### Reaction Skills

Reaction proposals must answer:

- What triggers it?
- What does not trigger it?
- What counters it?
- Does Brave affect it?
- Does equipment/evasion stack with it?
- Does it create broad practical immunity?
- Does it become correct on most builds?

Any broad reaction that can approach immunity must run the immunity rows.

### Support Skills

Support proposals must answer:

- What build direction does it enable?
- What does the unit give up by not taking another support?
- Does it repair a broken baseline?
- Does it erase equipment restrictions?
- Does it stack with protected stress engines?
- Does it become mandatory across roles?

Broad power supports require mandatory-piece and universal-build checks.

### Movement Skills

Movement proposals must answer:

- What map problem does it solve?
- What map problem does it not solve?
- Does it erase terrain?
- Does it erase threat range?
- Does it make retaliation impossible too often?
- Does it become mandatory late game?

Teleport-style movement requires universal-build and immunity/exposure checks.

## JP And Prerequisite Gates

JP and prerequisites are design levers, not afterthoughts.

JP proposals must classify each skill:

| Skill class | JP posture |
| --- | --- |
| Core identity | Affordable enough to experience the job without excessive grind. |
| Tactical option | Moderate cost; should encourage experimentation. |
| Strong global piece | Expensive and/or later access. |
| Build-defining engine | High investment and explicit opportunity cost. |
| Campaign utility | Costed separately from combat dominance checks. |

Prerequisite changes should be reviewed for:

- thematic progression;
- complexity at unlock time;
- early access to universal combos;
- delayed access to essential identity tools;
- whether a job is required only as a grind tax.

## Growth And Multiplier Gates

Growth profiles are limited to:

- physical;
- magical;
- hybrid.

No job inside a growth profile should have secretly superior permanent growth that makes leveling
elsewhere a trap.

Job multipliers may be sharply different and are expected to carry job feel. Changing multipliers
triggers formula risk because it changes effective PA, MA, Speed, HP, or MP.

Every multiplier proposal must state:

- which current stats the job should be good at;
- which current stats it should be bad at;
- which formulas become stronger or weaker as a result;
- whether the formula re-sim gate is triggered.

## Gender And Bard/Dancer Gate

Gender must not affect:

- equipment access;
- stats;
- growth;
- multipliers;
- global build pieces.

The only job-access exception is:

- Bard remains male-only;
- Dancer remains female-only.

Their action abilities may differ.

Their reaction, support, and movement skills must satisfy the byte-parity invariant from
`01-cross-job-build-principles.md`.

Any mismatch is a hard failure.

## Replacement And Deferred Jobs

Do not design these kits before the generic ecosystem is mapped:

- Necromancer, replacing Calculator;
- Vanguard, replacing Mime;
- Ramza's final Chapter 4 form.

They should appear in the role map with placeholder role and status, but detailed skill design is
deferred.

Ramza chapter progression can be outlined earlier, but Chapter 4 power must be checked against the
bounded exception:

```text
Ramza may be broadly strong, but no generic job's signature niche may be strictly dominated by him.
```

## Review And Acceptance Workflow

1. GPT drafts the proposal in `docs/job-balance/`.
2. GPT labels it `Proposed for Claude review`.
3. GPT runs the smallest check matrix that covers the proposal risk.
4. GPT relays the proposal and evidence to Claude.
5. Claude reviews as a blocking reviewer.
6. If the proposal contains non-trivial arithmetic, Claude must recompute a representative subset
   independently.
7. If Claude requests changes, GPT revises and re-sends.
8. Only after Claude approval may GPT mark the document accepted and commit it.

No detailed job proposal is accepted by silence.

## Required Output Artifacts

For any non-trivial job proposal, include or reference:

```text
docs/job-balance/<nn>-<job-or-system>.md
work/job-balance/<proposal-version>-inputs.json
work/job-balance/<proposal-version>-results.csv
work/job-balance/<proposal-version>-results.json
```

If the proposal does not need numeric simulation, it must say why and provide the structured
scenario/check reasoning instead.

## Open Items For Future Protocol Versions

Still open:

- exact strong-build catalog after the role map exists;
- exact scenario archetype definitions after role map v0;
- exact implementation format for `work/job-balance/` artifacts;
- real IVC weapon baseline from the Windows `04` session;
- formula-balance v1 re-sim after verified IVC weapon data;
- party/formula-v1 sweeps over the actual in-scope job roster, using each job's real armor class
  and HP, instead of only the current four representative armor anchors;
- final job implementation acceptance criteria after data-mod feasibility is confirmed.
