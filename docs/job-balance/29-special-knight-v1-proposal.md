# Special Knight V1 Proposal

Status: Accepted for provisional design
Version: V1
Date: 2026-06-21
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/job-balance/24-dragoon-samurai-v1-proposal.md`
- `docs/job-balance/28-foundation-reconciliation-v1.md`
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Special Knight, the replacement for
the Mime slot.

The display name is still open. `Special Knight` is the planning name. `Vanguard Knight`,
`Oath Knight`, or another FFT-appropriate name can be chosen later.

The proposal is concrete enough to define role, boundaries, skill roles, equipment posture, build
hooks, and validation needs. It is not final implementation data. It does not set exact JP numbers,
hit rates, CT values, damage multipliers, guard values, movement values, equipment records,
multipliers, prerequisites, or formulas.

## Replacement Thesis

Mime is removed.

Special Knight does not inherit Mime's core problem: passive copying that turns the unit into a
strange action-economy duplicate instead of a job with its own readable tactics.

Hard V1 rejection:

- no automatic action copying;
- no mimic-all-ally behavior;
- no action duplication based only on what another unit just did;
- no hidden extra turns or reaction loops;
- no job identity that is only "wear no equipment and copy better jobs."

Special Knight should be a late elite knight with special combat arts. It should be as worth
unlocking as a Holy Knight-style prestige job, but different:

- Holy Knight fantasy is holy sword pressure and dramatic special sword techniques;
- Special Knight fantasy is elite vanguard discipline, formation control, protection, and committed
  weapon discipline;
- it should win through commitment, position, equipment, and setup, not through free ranged holy
  damage.

## Group Thesis

Special Knight should be the late vanguard knight.

It should create value through:

- shield/plate formation play;
- local protection and interception;
- committed crush/guard-pressure arts that make frontline exposure matter;
- guard, armor, and offense pressure;
- setup-gated decisive strikes;
- late but bounded support pieces for frontline builds.

It should not become:

- a Holy Knight clone;
- a better generic Knight;
- a better Dragoon, Samurai, Monk, or Geomancer in their protected weapon niches;
- a strict weapon superset of Knight with higher-tier rewards;
- a universal best physical shell;
- a turn-economy duplicator like Mime.

Knight moat:

- Knight owns offensive equipment destruction and Rend-style enemy gear attrition;
- permanent or semi-permanent equipment destruction remains Knight-exclusive;
- Special Knight owns defensive projection, formation protection, and temporary exposure windows;
- Special Knight should be stronger as a late protection/setup job, not as "Knight plus more
  weapons."

## Reference Pass

Relevant vanilla records:

- Mime slot: removed as an identity; no mimic behavior is preserved.
- Knight `Rend` rows: equipment pressure vocabulary, not permanent-deletion permission.
- Unique sword rows such as `Judgment Blade`, `Cleansing Strike`, `Northswain's Strike`,
  `Hallowed Bolt`, and `Divine Ruination`: useful as a warning boundary, not a template to copy.
- `Crush Armor`, `Crush Helm`, `Crush Weapon`, `Crush Accessory`, `Duskblade`, `Shadowblade`, and
  `Unholy Darkness`: late special-knight vocabulary that proves FFT already understands dramatic
  weapon arts, but these should not be imported wholesale.
- Reaction/support vocabulary: `Parry`, `Shirahadori`, `Defense Boost`, `Equip Heavy Armor`,
  `Equip Shields`, `Equip Polearms`, `Equip Axes`, `Doublehand`, `Dual Wield`.

Trust boundary:

- the atlas tags are design classifications, not byte-accurate formulas;
- unique rows are vocabulary only unless a future implementation deliberately reuses a record;
- any special weapon art that changes damage, target count, range, armor response, or action economy
  must pass the relevant gate before concrete values.

## Dark-Line Boundary: Not Holy Knight

Special Knight can be prestigious and dramatic without becoming another Holy Knight.

Rejected V1 patterns:

- long-range holy sword nukes as the main action identity;
- large no-risk line attacks with status riders;
- unique sword skill copies with different names;
- free damage plus disable/stop/death-style riders;
- a kit that makes ordinary Knight, Dragoon, Samurai, or Monk only stepping stones.

Accepted V1 posture:

- mostly local or short-range;
- cares about current weapon family and formation;
- powerful when protecting allies, holding space, or exploiting prior guard/equipment pressure;
- weak or inefficient when played as raw damage spam.

## Special Knight

Job: Special Knight
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla slot: Mime, with mimic behavior and no normal equipment identity.

Vanilla slot problems:

- strange rules and poor readability;
- little equipment or build-expression identity;
- power depends on copying other jobs instead of expressing its own tactical role;
- can create action-economy ambiguity rather than an interesting late job plan.

Accepted high-level replacement: late special knight comparable in value to Holy Knight but not a
clone.

Primary role: `late-reward`

Secondary tags: `elite-knight`, `vanguard`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Weapon access: proposed V1 direction is `sword`, `spear`, `axe`, `fists`.

Shield access: yes.

Armor access: heavy armor/helmet.

Premium support access: `knight_sword` only through `Equip Knight Swords`, if that support survives
T2.1/F5.

Armor class as target: `plate`.

Supported damage modes: `swing`, `thrust`, `crush`.

Formula v0.2 coupling:

- `sword` lets the job satisfy elite knight fantasy without native knight-sword dominance;
- `spear` gives thrust reach as a normal weapon route without making Special Knight a Dragoon;
- `axe` gives an honest volatile crush option through `rdm_pa_wp`, which is self-limiting and does
  not threaten Monk's reliable Brave-linked crush identity by itself;
- `knight_sword` is deliberately support-gated, not native. If that support revives sword dominance,
  it should be cut or moved;
- the job must not become the best generic user of every weapon mode. Dragoon remains the spear
  home, Samurai remains the katana/Brave discipline home, Monk remains unarmed crush, and Knight
  remains early durable armed control;
- vanguard weapon arts are formula-sensitive and must run F5/T2.1 before concrete values;
- plate/shield protection must run T6xPS/T2.1 so the job does not create practical immunity.

### Action Skillset Goals

Special Knight should reward the player for asking:

- what formation do I need to hold?
- which ally must be protected this turn?
- is this target guarded, armored, armed, or exposed?
- am I using my current weapon for normal matchup value, or committing to guard pressure?
- should I spend the turn on protection, guard pressure, or a decisive strike?
- am I using Special Knight because its vanguard tools matter, or only because it has strong gear?

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Vanguard Break` | committed crush/guard art | Use heavy frontline posture, axe/fists, or shield discipline for guard pressure and temporary exposure. | T4/T6xT7/F5/T2.1; not a modal swing/thrust/crush toolbox. |
| `Intercede` | ally protection | Mark or guard a nearby ally against a limited incoming threat. | T8/T6xPS/T5; no broad invulnerability or global cover. |
| `Aegis Stance` | hold formation | Trade offense or mobility for a short-lived defensive posture around the knight. | T6xPS/T5/T2.1; must not stack into immunity. |
| `Sunder Guard` | guard/armor pressure | Break a shield, guard state, or armor response window for party follow-up. | T4/T6xT7; setup tool, not top damage. |
| `Commanding Challenge` | local enemy pressure | Make a nearby enemy respect the knight through zone, mark, or counter-pressure. | T8/T5; no boss hard-lock or forced AI script dependency. |
| `Decisive Strike` | setup finisher | Hit harder or more reliably only against challenged, guard-broken, or exposed targets. | F5/T4/T6; no instant KO, no Holy Sword clone. |

The exact names may change. The role pattern should not.

V1 rejects a free modal `Vanguard Art`.

A single action that changes its rider automatically for swing, thrust, and crush would be too often
correct regardless of weapon and would make Special Knight a universal secondary risk.

Accepted V1 posture:

- Special Knight gets one committed crush/guard-pressure art: `Vanguard Break`;
- it should be strongest when the job is using axe/fists, shield discipline, or heavy frontline
  posture;
- it should not grant spear-thrust scaling bonuses or sword-swing reliability riders;
- if a later design wants separate swing/thrust/crush arts, each mode must be a separate learned
  action with independent JP cost and its own specialist-preservation row.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Intervention` | protective reaction | React to a nearby ally being attacked by reducing, redirecting, or punishing a narrow threat. | T8/T6xPS/T10 if it grants extra attacks; no global cover. |
| `Last Stand` | critical frontline survival | Preserve the elite knight fantasy under pressure with a bounded critical response. | T3/T6xPS/T2.1; no practical immortality. |

If only one reaction survives, prefer `Intervention`. It differentiates Special Knight from generic
self-defense jobs and keeps the role focused on vanguard protection.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Equip Knight Swords` | prestige weapon unlock candidate | Let a committed late build access premium knight-sword identity at support-slot cost. | High T2.1/F5 risk; must not recreate sword dominance. |
| `Vanguard Training` | kit specialization | Improve Special Knight's own protection/formation arts without boosting all physical damage. | Narrow; should not become a universal physical support. |
| `Armor Discipline` | plate/shield specialization | Reward heavy frontline builds without making every job want plate. | T2.1/T6xPS; no broad immunity. |

`Equip Knight Swords` is optional and dangerous. It exists because late jobs can own strong support
skills, but it should be cut or moved if T2.1/F5 shows it pulls too many builds back toward swords.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Vanguard March` | formation movement | Help a plate/shield unit enter or hold formation without becoming a universal mobility tool. | T2.1/T5; should depend on frontline posture or formation value. |

Special Knight should not donate the best movement in the game. Its movement reward should help
heavy formation play, not replace Ninja, Dragoon, Time Mage, or performer mobility decisions.

### JP Progression

JP posture:

- one protection action and `Vanguard Break` should be reachable soon after unlock;
- `Sunder Guard` and `Commanding Challenge` should require meaningful commitment;
- `Decisive Strike` should be a late capstone gated by setup;
- `Equip Knight Swords`, if retained, should be expensive and late;
- no support should make the active job irrelevant by itself.

### Prerequisite Changes

This proposal does not set the job-tree prerequisites.

Special Knight should remain late. It replaces Mime and should feel like a high-end generic reward,
not an early correction to Knight.

### Gender/Equipment Restrictions

No gender restrictions.

No gender-based equipment restrictions.

Equipment direction is provisional and intentionally broad enough to test the vanguard idea. It must
be narrowed if it erases Dragoon, Knight, Monk, Samurai, or Geomancer.

### Cross-Job Build Hooks

Healthy Special Knight donor patterns:

- plate frontline borrows `Intercede` or `Aegis Stance` for a protection build;
- axe, fists, or shield-heavy build borrows `Vanguard Break` only when guard pressure matters;
- late physical build spends support slot on `Equip Knight Swords` for a clear premium-weapon plan;
- active Special Knight borrows Knight, Dragoon, or Samurai tools while keeping vanguard protection
  as its own identity.

Unhealthy Special Knight donor patterns:

- every physical build wants `Vanguard Break`;
- every durable build wants `Armor Discipline`;
- `Equip Knight Swords` becomes the default late support;
- `Intervention` creates practical global cover;
- Special Knight becomes a better Knight, Dragoon, Samurai, and Monk at once.

### Expected Strong Builds

- active Special Knight holding a chokepoint or protecting a fragile caster;
- plate/shield vanguard using Intercede and Aegis Stance to control local pressure;
- frontline build using sword/spear/axe/fists for normal matchup value while reserving Vanguard
  Break for guard pressure;
- setup party that exposes guard or armor before using Decisive Strike;
- late physical build that chooses one premium support from Special Knight at real opportunity cost.

### Expected Weaknesses

- lower value when no ally needs protection or no formation can be held;
- lower raw range than Holy Knight-style sword skills;
- vulnerable to magic/status if built only for plate/shield physical defense;
- equipment breadth creates risk of mediocre identity if Vanguard Break and protection tools are not
  distinct;
- expensive late job path.

### Expected Counters

- magic and status pressure that bypasses ordinary guard posture;
- spread enemies that refuse local formation fights;
- mobile ranged enemies;
- targets with no meaningful guard, armor, or weapon pressure to exploit;
- anti-plate crush pressure.

### Ramza / Unique-Job Interaction

Ramza's Chapter 4 job may become a knight/mage hybrid. Special Knight must not steal that hybrid
identity.

Special Knight is the generic late vanguard: weapons, plate, shields, formation, and protection.
Ramza can be broader and more protagonist-flavored later, but if he gains similar protection tools,
he should trade against his mage and leadership options.

## Scenario And Check Plan

Minimum provisional rows before concrete values:

| Scenario ID | Purpose | Required gates |
| --- | --- | --- |
| `J-SPK-NO-MIME` | Special Knight has no mimic/copy/automatic duplicate-action behavior. | data/design check |
| `J-SPK-NO-HOLY-CLONE` | Special Knight does not copy Holy Sword's long-range damage/status pattern. | data/design check/F5 if similar |
| `J-SPK-VS-KNIGHT` | Special Knight does not become Knight plus more weapons; permanent or semi-permanent equipment destruction remains Knight-exclusive. | design check/T2.1/T6xT7 |
| `J-SPK-WEAPON-ACCESS` | Sword, spear, axe, fists, and support-gated knight_sword access do not erase Dragoon, Monk, Knight, or Samurai. Must run on Special Knight's actual roster row, not only formula anchor jobs. | F5/T2.1/real-roster no-dominance |
| `J-SPK-GUARD` | Intercede, Aegis Stance, Intervention, and Armor Discipline do not create practical immunity. | T6xPS/T8/T2.1 |
| `J-SPK-SUNDER` | Sunder Guard creates temporary tactical exposure without destroying gear or replacing Knight's Rend identity. | T4/T6xT7 |
| `J-SPK-CHALLENGE` | Commanding Challenge creates local pressure without boss hard-lock or AI failure. | T8/T5 |
| `J-SPK-FINISHER` | Decisive Strike is setup-gated and does not become top raw damage. | F5/T4/T6 |
| `J-SPK-SUPPORT` | Equip Knight Swords, Vanguard Training, and Armor Discipline incidence stays bounded. | T2.1/F5/T6xPS |
| `J-SPK-MOVE` | Vanguard March helps formation play without replacing elite movement options. | T2.1/T5 |
| `J-SPK-RAMZA` | Special Knight stays distinct from Ramza's future knight/mage hybrid role. | design check/F4 if magic-like |

These are scenario requirements, not final scenario data.

## Formula Re-Sim Requirement

This proposal triggers formula review when values become concrete because it touches:

- premium weapon access, including support-gated knight_sword access;
- swing/thrust/crush mode coverage;
- plate/shield mitigation;
- armor and guard pressure;
- support skills that can alter physical build incidence;
- possible cover/intercept action economy;
- late physical damage ceilings.

Special Knight must be re-simulated as its own roster entry.

The formula-dominance check cannot rely only on the existing anchor jobs. Before concrete values are
accepted, the F5/no-dominance run must include Special Knight's actual roster row with its real
equipment, armor class, and multipliers. This is required because a new plate job with access to
sword, spear, axe, fists, and possible support-gated knight_sword can create dominance that the
anchor-only sweep would not see.

The strongest available status before real weapon data and required gates is:

```text
Accepted for provisional design
```

No concrete implementation data should be marked final until the affected T-gates pass and formula
v1 or its accepted successor reconciles real weapon values.

## Implementation Assumptions

- Data mod scope can replace Mime records with Special Knight records.
- No mimic behavior is retained unless a later user decision explicitly reopens it.
- Special Knight actions should be local, formation-bound, equipment-bound, or setup-bound.
- Monsters remain out of current scope.
- If broad equipment access proves too identity-erasing, the final implementation should narrow
  access rather than inflate action power.

## Open Proof Needs

- Final display name.
- Whether `sword`, `spear`, `axe`, `fists`, shields, heavy armor, and support-gated knight_sword are
  too broad together.
- Whether `Equip Knight Swords` can survive without reviving sword dominance.
- Whether Intercede/Intervention is data-moddable as targeting, mitigation, counter-pressure, or
  zone behavior.
- Whether `Vanguard Break` should remain one committed crush/guard-pressure skill or be cut.
- Whether a later split family-art package is worth reopening despite the V1 rejection of a free
  modal skill.
- Whether Special Knight needs any magic-adjacent defense, or whether that belongs to Ramza/Mystic/
  White Mage instead.

## Claude Review Request

Claude should review whether:

- Special Knight replaces Mime with a real late-job identity;
- the proposal is strong enough to be comparable to Holy Knight without copying Holy Sword;
- equipment access is too broad or correctly provisional;
- Vanguard Break is a healthy committed guard-pressure identity or a future universal secondary risk;
- the support/reaction/movement hooks are late-reward quality without becoming mandatory;
- the scenario/check plan names the right gates before concrete values;
- accepting this closes the generic-job roster design phase.

Claude review verdict: Accepted after revision (claude-opus-4-8, 2026-06-21).

Claude accepted the V1 proposal after revision:

- Knight keeps permanent/semi-permanent equipment destruction and Rend-style gear attrition;
- Special Knight is defensive projection, formation protection, and temporary exposure, not
  "Knight plus more weapons";
- `J-SPK-WEAPON-ACCESS` requires Special Knight's actual roster row in F5/no-dominance checks, not
  only the formula anchor jobs;
- V1 rejects a free modal `Vanguard Art` and uses committed `Vanguard Break` instead;
- native `knight_sword` access is removed and left only behind the optional, cuttable
  `Equip Knight Swords` support;
- axe is documented as the volatile crush option rather than a Monk replacement.
