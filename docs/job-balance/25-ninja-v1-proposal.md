# Ninja V1 Proposal

Status: Accepted for provisional design
Version: V1
Date: 2026-06-21
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/09-accuracy-evasion-model-schema.md`
- `docs/job-balance/11-ct-delay-model-schema.md`
- `docs/job-balance/13-armor-response-model-schema.md`
- `docs/job-balance/15-targeting-challenge-model-schema.md`
- `docs/job-balance/17-offense-armor-composition-schema.md`
- `docs/job-balance/23-deferred-campaign-economy-policy.md`
- `docs/job-balance/24-dragoon-samurai-v1-proposal.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Ninja.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, Throw damage,
Throw range, Throw inventory cost, hit rates, evasion rates, Vanish duration, movement values,
damage multipliers, equipment records, stat multipliers, or prerequisites.

Ninja is isolated in its own proposal because it is one of the highest-risk jobs in the formula
ecosystem:

- active Ninja has innate `Dual Wield`;
- `Dual Wield` is a protected Two Swords stress engine;
- Ninja is fast and leather-fragile;
- Ninja can touch swing, thrust, and volatile crush through current weapon access;
- Throw can potentially project many weapon families at range.

Ninja should stay iconic, but its iconography is exactly where late-game physical convergence can
happen if the job is not bounded.

Claude review verdict: conditional acceptance on 2026-06-21, with required edits applied in this
version.

## Group Thesis

Ninja should be the fast multi-hit skirmisher and thrown-weapon specialist.

Its identity is:

- speed;
- dual-wield burst;
- ninja blade and knife pressure;
- single-target assassination;
- tactical Throw as finite ranged reach;
- evasive, fragile, positional combat.

Ninja should not be:

- the best shell for every physical build;
- the best user of every weapon family through Throw;
- the best answer to every armor profile;
- the universal owner of the support slot through `Dual Wield`;
- functionally untargetable through Vanish/evasion loops.

## Protected Formula Notes

Ninja touches several protected v0.2 surfaces:

- `ninja_blade` is `spd_pa_wp` swing;
- `knife` is `spd_pa_wp` thrust;
- `flail` is `rdm_pa_wp` crush;
- `fists` are `br_pa_pa` crush;
- `Dual Wield` is the protected Two Swords stress engine with `2` hits.
- Throw is a Speed-routed uniform ranged physical action, not a port of each weapon family's
  normal routine.

Design implications:

- Ninja should naturally care about Speed plus PA.
- Ninja's best normal melee should be fast single-target pressure, not universal top damage.
- Ninja can attack mail through knife thrust, but that should not erase Thief's speed/disruption
  identity or Dragoon's PA/reach/elevation spear identity.
- Ninja can access crush through flail/fists, but that should not make it the protected anti-plate
  job. Monk and Geomancer still own intentional anti-plate lanes.
- `Dual Wield` is allowed to be exciting and strong, but it must be incidence-gated and cannot
  become the default physical support.
- Throw's default V1 damage type is `missile`, so thrown flails, axes, swords, polearms, and books
  do not automatically carry their native physical damage type at range.

Relevant accepted gates:

- T2.1 for `Dual Wield`, `Move +3`, Vanish, Throw as secondary, and active Ninja incidence;
- T4 for Throw range, line, evasion, Reflexes, Vanish interaction, and any accuracy bypass;
- T5 for action timing, Vanish duration if timed, and any speed/tempo support;
- T5xT8 for Vanish, Invisible, or any timed self-targetability exclusion;
- T6/T6xT7 for any Throw or melee action that changes armor response, penetration, family, or
  enemy offense;
- T9 for finite Throw resource pacing if combat resource limits matter;
- deferred campaign policy for consumed weapons, thrown inventory costs, gil pressure, and loot
  economy;
- Gate F4/F5 if Dual Wield, Speed scaling, weapon-family access, Throw damage, or evasion support
  drifts the accepted formula ecology.

## Ninja

Job: Ninja
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: fast dual-wield physical job with Throw, innate Two Swords/Dual Wield, light armor,
and high mobility/evasion.

Vanilla problems:

- innate dual-wield can make active Ninja the obvious physical damage shell;
- learned `Dual Wield` can become the obvious support for most physical builds;
- Throw can become either a useless inventory sink or a ranged damage engine with too much weapon
  access;
- Ninja can touch too many damage modes if ninja blades, knives, flails, fists, and Throw all remain
  strong at once;
- evasion or Vanish-style reactions can stack with speed/leather to become frustrating or mandatory.

Accepted high-level role: fast physical pressure and multi-hit stress job.

Primary role: `melee-physical`

Secondary tags: `fast`, `dual-wield`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `ninja_blade`, `knife`, `flail`, `fists`.

Armor class as target: `leather`.

Supported damage modes: `swing`, `thrust`, `crush`.

Formula v0.2 coupling:

- Ninja's signature melee family is `ninja_blade`, using `spd_pa_wp` swing;
- knives share the same Speed/PA axis but provide thrust and anti-mail access;
- flails provide volatile crush, but that should remain an oddball side route, not Ninja's protected
  anti-plate identity;
- innate active Ninja `Dual Wield` should be evaluated under the dual-wield lens, not compared as
  if it were a single-hit job;
- learned `Dual Wield` is formula-affecting and must pass T2.1/F5 before concrete acceptance.
- Throw uses a separate Speed-routed thrown routine and `missile` response by default; it does not
  apply the normal `sword`, `katana`, `spear`, `flail`, `book`, or other family routine/type at
  range.

### Action Skillset Goals

Throw should be tactical reach, not a second equipment system that makes Ninja every job at range.

The player should ask:

- do I commit to a fast dual-wield melee turn or spend a finite Throw option?
- is this target worth using a thrown resource on?
- do I need swing, thrust, volatile crush, or a low-risk shuriken?
- can I reach a fragile or exposed target without overextending my leather unit?
- am I using Ninja's speed to solve a map problem or just defaulting to the highest damage shell?

### Proposed Action Skills

The vanilla Throw list is preserved as design vocabulary, but not every Throw category must remain
equally important. The final data may consolidate categories if broad thrown-family access becomes
unhealthy.

V1 commits to this Throw routing principle:

```text
Throw output = Speed-routed thrown routine using the thrown category's own capped WP/value.
Throw damage type = missile by default.
```

This means a thrown sword does not use `sword`'s `pa_wp` swing routine, a thrown katana does not use
`katana`'s Brave swing routine, a thrown spear does not become Dragoon's thrust route, and a thrown
flail does not become ranged crush by default. Throw remains a Ninja Speed expression with category
flavor, cost, range, and resource tuning.

Rejected V1 route:

- family-specific full-routine Throw output.

Allowed later only with explicit reapproval:

- a narrow special Throw category that carries a non-`missile` damage type or special response.
  That would trigger T6/F5 and must not replace the job that owns the original family identity.

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Throw Shuriken` | baseline ranged chip | Reliable low-cost single-target reach when Ninja cannot safely engage. | Lower ceiling than melee; finite/resource policy required. |
| `Throw Daggers` | light precision throw | Speed-routed missile throw with dagger flavor and light-resource profile. | Must not replace Thief knife play or Dragoon spear reach. |
| `Throw Ninja Blades` | signature thrown burst | High-commitment Speed-routed missile throw for exposed targets. | Cost/range/resource limits; not repeated default damage. |
| `Throw Flails` | heavy thrown option | Risky heavy throw if flail access survives. | Does not carry ranged crush by default; watch dual-wield flail melee separately. |
| `Throw Axes` | heavy volatile throw | High-risk heavy thrown strike with resource pressure. | Speed-routed missile by default; no universal finisher. |
| `Throw Swords` | generic weapon throw | Preserve if inventory policy supports ordinary thrown weapons. | Should not let Ninja borrow Knight/Squire identity for free. |
| `Throw Katana` | disciplined thrown weapon | Preserve only if it does not undermine Samurai's katana identity. | High cost or narrow role; no remote Iaido substitute. |
| `Throw Polearms` | reach-family throw | Preserve only if it does not undermine Dragoon's spear identity. | Must not become better anti-mail reach than Dragoon. |
| `Throw Poles` | odd thrown option | Preserve only if category flavor/resource profile is distinct. | Does not inherit MA-crush by default. |
| `Throw Knight's Swords` | premium weapon throw | Very late or exceptional thrown burst if retained. | Campaign/economy and F5 risk; not ordinary balance baseline. |
| `Throw Books` | oddball hybrid throw | Preserve only if it creates a distinct niche. | Candidate for consolidation if duplicate. |
| `Throw Bombs` | consumable special throw | Utility or elemental special throw if retained. | T9/campaign policy; no replacement for Black Mage/Summoner. |

Throw should normally be single-target, range-limited, and physically legible. It can be powerful
because it spends resources and action choice, but its value must be measured in battle first. A
skill that is weak in battle but justified only by campaign inventory quirks is not accepted combat
design.

Default Throw policy for V1:

- Throw does not grant universal full-WP ranged access to every weapon family for free;
- Throw categories use a uniform Speed-routed thrown routine and `missile` damage type by default;
- Throw categories may vary by thrown WP/value, range, JP, rarity, or resource friction, but do not
  inherit the normal family routine/type unless a later accepted exception proves it;
- inventory consumption, thrown-item acquisition, and gil pressure belong to
  `docs/job-balance/23-deferred-campaign-economy-policy.md`;
- if the final data cannot keep a category distinct, consolidate rather than preserve a redundant
  button.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Vanish` | evasive disappearance | Let a Ninja-style build briefly escape targeting or become harder to punish after taking risk. | T5xT8/T4/T2.1; no permanent untargetable loop. |
| `Reflexes` | light evasive reaction | Narrow evasion boost for fragile skirmishers if Vanish is too broad. | Must not stack into practical immunity. |

`Vanish` is thematically strong but systemically dangerous. If retained, the design preference is a
short, breakable, visible state that ends on action, damage, or a bounded timing rule. It should not
let the player chain unanswerable turns.

Vanish also needs a stealth-strike check. If attacking out of Vanish bypasses evasion or reactions,
that opening hit can be stronger than the untargetability window itself on a high-burst dual-wield
job. T4 must explicitly decide whether attacks from Vanish bypass evasion/reactions, and any bypass
must be narrow enough that Vanish does not become the default assassin reaction.

`Reflexes` is the fallback reaction if Vanish cannot be bounded. It still requires T4/T2.1 because
evasion stacking can erase enemy turns even without full untargetability.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Dual Wield` | multi-hit physical engine | Preserve the iconic Two Swords support as a late build-defining reward. | Protected `2` hit engine; T2.1/F5; no universal physical default. |
| `Throw Mastery` | Throw specialization candidate | Improve Throw range, resource discipline, or category access only for Throw. | Optional; must not compress Dual Wield plus broad ranged dominance. |

`Dual Wield` is allowed to be one of the most attractive physical supports in the game. It fails
only if it becomes correct regardless of weapon family, active job, map, armor profile, and
opportunity cost.

Hard V1 boundaries for `Dual Wield`:

- no stacking with `Doublehand`;
- no effect on unarmed/fists, because the engine requires two weapons;
- no effect on spells, Iaido support draws, Throw actions, reactions, or non-weapon special actions
  unless a later accepted proposal explicitly proves a narrow exception;
- no double-Throw behavior;
- no assumption that a two-hit total is compared against single-hit families without the dual-wield
  lens.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Move +3` | elite skirmisher mobility | Let committed Ninja-route builds reach flanks and fragile targets. | High T2.1 incidence risk; must compete with Teleport, Move +2, Jump mobility, and terrain tools. |
| `Ignore Terrain` | stealth route candidate | Optional terrain identity if broad Move +3 proves too universal. | Map-dependent; not a default mobility patch. |

Ninja already has strong active mobility through job stats. Movement rewards should support
skirmishing without making every optimized unit take Ninja only to buy movement.

The key `Move +3` risk is asymmetric: active Ninja may gain modest marginal value because the job is
already fast/mobile, while other jobs may gain huge donor value from dipping Ninja only to export the
movement skill. T2.1 should measure on-job value and off-job donor pull separately.

### JP Progression

JP posture:

- active Ninja should work immediately through fast melee and innate dual-wield identity;
- `Throw Shuriken` and one meaningful weapon Throw should be reachable early after unlock;
- high-impact Throw categories should be mid/high JP and resource-sensitive;
- `Dual Wield`, `Vanish`, and `Move +3` should be late/high-JP rewards;
- Ninja should be attractive as an active job before the player learns its global support package.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Ninja should remain an advanced physical progression job. Its active shell and support rewards are
too strong to appear early without pulling the whole physical ecosystem toward Ninja.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal, and flail access stays high-risk pending checks.

Flail is self-limiting because it is `rdm_pa_wp` crush: volatile and PA-routed, which is off-axis
for Ninja's preferred Speed/PA `ninja_blade` and `knife` lanes. The specific risk is not ordinary
single-flail use. The real risk is dual-wield flail melee: two volatile crush rolls reduce variance
and could become a sneaky reliable anti-plate plan. If concrete checks show Ninja covering
swing/thrust/crush too efficiently, the later equipment pass should either remove flail from Ninja
or make flail a weak/volatile side route rather than a supported anti-plate plan.

### Cross-Job Build Hooks

Healthy Ninja donor patterns:

- fast physical build uses `Dual Wield` for a deliberate two-hit weapon plan;
- fragile skirmisher borrows Throw for finite ranged reach;
- active Ninja uses Thief, Squire, or Samurai secondary for a specific single-target plan;
- a speed build uses Ninja active stats to assassinate cloth/leather targets.

Unhealthy Ninja donor patterns:

- most physical builds take `Dual Wield` as the default support;
- active Ninja is the best shell for every weapon-family plan;
- Throw gives Ninja the best ranged version of every weapon family;
- Vanish or Reflexes creates practical untargetability or practical evasion immunity;
- Move +3 becomes the default late movement choice on most builds.

### Expected Strong Builds

- active Ninja with ninja blades, innate dual-wield, and high-speed single-target pressure;
- active Ninja using knife/thrust to punish mail when Dragoon/Thief are not present;
- a committed non-Ninja physical build spending support slot on `Dual Wield`;
- Ninja with finite Throw options for maps where direct approach is dangerous;
- fragile assassin build using Vanish or Reflexes if the reaction is bounded.

### Expected Weaknesses

- leather durability and low sustain;
- poor broad mitigation into area magic, status, and counter-pressure;
- swing damage should not dominate plate;
- flail/crush routes should be volatile or secondary rather than protected;
- finite Throw resources and campaign policy should prevent thoughtless ranged spam;
- if Ninja cannot reach or isolate a valuable target, its burst loses value.

### Expected Counters

- area magic and status that bypasses single-target evasion;
- durable plate targets without a prepared crush plan;
- enemy formations that punish overextension;
- anti-evasion tools, accuracy pressure, or unavoidable effects;
- resource pressure if Throw is overused;
- Brave/Speed/control disruption if later values depend heavily on those axes.

### Ramza / Unique-Job Interaction

Ramza can later use speed, hybrid melee, or leadership tools, but he should not become a better
Ninja. If Ramza receives dual-hit, throw, or stealth-like effects, they should be narrow chapter or
hybrid expressions rather than a direct replacement for Ninja's fast multi-hit identity.

## Scenario And Check Plan

Minimum provisional rows before concrete values:

| Scenario ID | Purpose | Required gates |
| --- | --- | --- |
| `J-NIN-ACTIVE-DUAL` | Active Ninja innate dual-wield is strong without becoming the default shell. | formula harness/T2.1/F5 |
| `J-NIN-SUPPORT-DUAL` | Learned `Dual Wield` incidence stays below mandatory-piece thresholds. | T2.1/F5 |
| `J-NIN-MAIL` | Knife/thrust route helps into mail without replacing Thief or Dragoon. | formula harness/T4 |
| `J-NIN-PLATE` | Flail/fists crush side routes do not replace Monk or Geomancer anti-plate. | formula harness/T6 |
| `J-NIN-THROW-SHURIKEN` | Baseline Throw provides reach without replacing Archer/Chemist/Orator ranged roles. | T4/F5 |
| `J-NIN-THROW-FAMILY` | Speed-routed missile Throw categories do not grant universal full-power ranged family access. | T4/T6/T9/F5 |
| `J-NIN-VANISH` | Vanish does not create untargetable loops or unbounded stealth-strike bypass. | T5xT8/T4/T2.1 |
| `J-NIN-MOVE` | Move +3 donor pull stays bounded against on-job value, Teleport, Move +2, and terrain movement. | T2.1/T5 |
| `J-NIN-RESOURCE` | Throw resource pacing is useful in battle without creating campaign grind pressure. | T9/deferred policy |

These are scenario requirements, not final scenario data.

## Formula Re-Sim Requirement

This proposal triggers formula review when values become concrete because it touches:

- active dual-wield hit count;
- learned `Dual Wield` as a support engine;
- Speed-scaling ninja blades and knives;
- flail/crush access on a dual-wield job;
- Speed-routed missile Throw output;
- high-mobility skirmisher exposure;
- Vanish/evasion effects that can reduce incoming damage frequency.

The strongest available status before real weapon data and the required gates is:

```text
Accepted for provisional design
```

No concrete implementation data should be marked final until the affected T-gates pass and formula
v1 or its accepted successor reconciles real weapon values.

## Implementation Assumptions

- Data mod scope can rewrite Throw values, category access, JP costs, range, inventory behavior,
  support behavior, and movement records where needed.
- Active Ninja may keep innate dual-wield if the dual-wield lens and incidence checks preserve
  ecosystem health.
- Final Throw may consolidate vanilla categories if preserving every category creates duplicate or
  unhealthy buttons.
- Campaign/inventory costs for thrown weapons are deferred policy questions, not the first combat
  balance lever.

## Open Proof Needs

- Exact active Ninja dual-wield damage bands by phase and armor profile.
- Exact `Dual Wield` incidence threshold and comparison against `Doublehand`, `Attack Boost`,
  `Brawler`, and equipment unlocks.
- Whether flail access should survive the later equipment pass.
- Exact Speed-routed Throw constants, category caps, range, and resource costs.
- Throw resource policy inside battle versus campaign economy outside battle.
- Whether Vanish can be bounded better than Reflexes as Ninja's reaction identity.
- Whether Move +3 can exist without becoming the default late movement choice.

## Claude Review Request

Claude should review whether:

- active Ninja can keep innate dual-wield under this identity without making the job the universal
  physical shell;
- learned `Dual Wield` is bounded enough as a protected late support engine;
- Throw is constrained enough to avoid giving Ninja every weapon family at range;
- flail access should be accepted as high-risk pending checks or removed already at V1;
- Vanish should remain the preferred reaction candidate or be replaced with Reflexes in the V1;
- Move +3 is acceptable as Ninja's late movement hook or too universal;
- the scenario/check plan names the right validation gates before concrete values.

Claude review verdict: Conditional accepted (claude-opus-4-8, 2026-06-21).

Required edit applied:

- Throw now has an explicit V1 routing principle: uniform Speed-routed missile output, not
  family-specific full-routine output.

Recommended edits also applied:

- flail risk is grounded in PA/off-axis volatility, with dual-wield flail variance reduction called
  out as the real anti-plate risk;
- Vanish now includes an explicit T4 stealth-strike/evasion/reaction bypass check;
- Move +3 incidence is framed as asymmetric on-job value versus off-job donor pull;
- Dual Wield now explicitly excludes unarmed/fists and double-Throw behavior.
