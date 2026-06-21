# Dragoon And Samurai V1 Proposal

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
- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Dragoon and Samurai.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, hit rates, CT
values, Jump speed, Jump range, Iaido radius, status rates, katana-break behavior, damage
multipliers, equipment records, stat multipliers, or prerequisites.

Dragoon and Samurai are paired because both are advanced physical jobs with iconic weapon identities:

- Dragoon owns spear reach, thrust pressure, verticality, and delayed airborne commitment.
- Samurai owns katana discipline, Brave-linked swing pressure, Iaido, and elite physical supports.

Both jobs should feel strong and stylish without turning into generic endgame physical defaults.

Claude review verdict: conditional acceptance on 2026-06-21, with required edits applied in this
version.

## Group Thesis

Dragoon and Samurai should be advanced martial identities, not simply stronger Knight variants.

- Dragoon should be the clean spear/thrust job: excellent into mail, good at elevation and timing,
  and uniquely able to leave the board through Jump at real CT and prediction cost.
- Samurai should be the disciplined katana job: Brave-linked weapon pressure, controlled spirit
  techniques, and high-value support/reaction pieces that reward late investment.

Both jobs can be strong late-game shells, but their strength must have shape:

- Dragoon is strongest when reach, height, timing, and mail targets matter.
- Samurai is strongest when Brave, katana access, deliberate single-weapon commitment, and measured
  area/support draws matter.

Neither job should become the best answer to every armor profile, every map shape, and every build
slot at once.

## Shared Advanced Martial Notes

Dragoon and Samurai both touch protected formula surfaces:

- `spear` is `pa_wp` thrust with anti-mail identity;
- `katana` is `br_pa_wp` swing with Brave-linked disciplined physical identity;
- `Doublehand` is the protected Two Hands stress engine at `1.80`;
- `Jump` is a T5 timing state and T4 non-evasion/targeting special case;
- `Shirahadori` is a T4/T2.1 practical-immunity risk;
- damage Iaido is routed as Brave/PA physical katana-spirit expression, not Faith-independent
  reliable magic;
- support Iaido draws can touch healing, mitigation, status, area, timing, and resource risk
  depending on final implementation.

Relevant accepted gates:

- T2.1 for secondary/reaction/support/movement incidence, especially `Doublehand`, `Shirahadori`,
  `Equip Polearms`, `Equip Katana`, `Dragonheart`, and `Ignore Elevation`;
- T4 for Jump hit behavior, non-evadable rows, line/facing exceptions, and Shirahadori;
- T5 for Jump timing, airborne untargetability duration, whiff windows, delayed landing, Haste/Slow
  interactions, and critical/revive reactions;
- T5xT8 for Jump airborne untargetability as a timed self-targetability exclusion window;
- T6/T6xT7 for any spear or katana skill that changes armor response, penetration, guard, or enemy
  offense;
- T6xPS for Kiyomori-style Protect/Shell/Wall-like mitigation;
- T10 for Masamune-style action grants, Quick-like effects, action refunds, or turn recursion;
- T8 if any Jump, challenge, mark, AI ignore, or targeting-change behavior appears;
- T11 or its accepted successor for any Iaido area or line effect where target count matters;
- Gate F4/F5 if katana/spear access, Brave manipulation, Doublehand, Iaido formulas, or movement
  changes drift the accepted formula ecology.

## Dragoon

Job: Dragoon
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: spear user with Jump, horizontal/vertical Jump unlocks, Dragonheart, Equip Polearms,
and elevation-focused movement.

Vanilla problems:

- Jump unlocks can feel like a grind ladder rather than tactical choices;
- delayed Jump can whiff when targets move, but the prediction burden is often opaque;
- airborne untargetability can become degenerate if the Dragoon spends too much time off-board;
- Dragoon can collapse into "a Knight with spears" if Jump timing and verticality are not valuable;
- Equip Polearms can erase spear ownership if it is too cheap or too broadly optimal.

Accepted high-level role: reach/jump physical specialist.

Primary role: `melee-physical`

Secondary tags: `thrust`, `plate`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `spear`, `fists`.

Armor class as target: `plate`.

Supported damage modes: `thrust`, `crush`.

Formula v0.2 coupling:

- Dragoon's spear lane is `pa_wp` thrust, so it is a natural anti-mail physical job;
- Dragoon and Thief share the thrust/anti-mail damage type but not the same lane: Dragoon is
  PA-scaled reach, Jump, elevation, and plate durability; Thief is Speed-scaled knife precision,
  stealing, disruption, and leather fragility;
- plate resists thrust, so Dragoon should not also be a protected anti-plate answer by default;
- generic fists fallback exists but must not compete with Monk's protected `crush` identity;
- Jump should use spear/thrust identity unless a specific future skill explicitly says otherwise;
- Jump's value is tempo, reach, elevation, and target selection, not universal top damage.

### Action Skillset Goals

Jump should become a readable delayed-commitment tool.

The player should ask:

- is the target likely to remain vulnerable until landing?
- is it worth spending CT to become temporarily untargetable?
- does this map reward vertical or horizontal reach?
- am I attacking a mail target where spear/thrust is the right answer?
- will the Dragoon be exposed or out of tempo after landing?

### Proposed Action Skills

The final data may keep the vanilla horizontal/vertical Jump records, but the design intent should
not be "buy ten boring range numbers." Jump progression should unlock tactical reach bands.

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Jump` | core delayed spear strike | Leave the board, avoid targeting briefly, then land a spear/thrust hit on a chosen panel or target. | T5 timing and whiff risk; not a normal attack replacement. |
| `Horizontal Jump +1/+2` | early reach unlock | Let Jump function on ordinary maps without requiring full mastery. | Low or moderate reach; no damage bonus by default. |
| `Horizontal Jump +3/+4` | committed reach unlock | Reward investment with stronger map control and safer approach angles. | Greater whiff/prediction exposure; must not erase Archer range. |
| `Horizontal Jump +7` | late specialist reach | Capstone reach for players who commit to Dragoon as a map-control jumper. | High JP; T5/T4 proof required so it does not become untouchable ranged melee. |
| `Vertical Jump +/-2/+/-3` | early elevation unlock | Make Dragoon usable on height maps earlier than other physical jobs. | Map-dependent; no generic damage value. |
| `Vertical Jump +/-4/+/-5` | mid elevation mastery | Let Dragoon choose targets across meaningful elevation differences. | Must still care about target movement and landing windows. |
| `Vertical Jump +/-6/+/-8` | late elevation mastery | Preserve the fantasy that no cliff is safe from a master Dragoon. | Competes with `Ignore Elevation`; high JP and T2.1 incidence checks. |

If implementation space permits a rewritten active skill list, the same design may be represented
with fewer named actions:

- a reliable short Jump;
- a long horizontal Jump;
- a high vertical Jump;
- a late master Jump.

The important rule is that the final player experience should be clear: Dragoon buys better ways to
Jump, not a pile of indistinguishable numeric tax nodes.

Jump should normally be non-evadable only if its CT and whiff windows are meaningful. Non-evasion
plus untargetability plus high damage is a dangerous combination. The accepted T4 note that Jump is
a useful non-evadable row should be treated as a validation target, not as a blank check.

Airborne untargetability is not covered by T5 or T8 alone. The accepted composition is T5xT8:

```text
T5 supplies the timed airborne duration and landing window.
T8 consumes that duration as a self-applied can_target=false exclusion window.
```

This composition is where Jump must prove it does not create an off-board loop that removes enemy
agency.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Dragonheart` | risky survival reaction | Preserve the iconic reraise/revive fantasy for a frontline jumper who risks landing in danger. | Requires T3/T5/T2.1; must not create broad practical immortality. |
| `Brace Landing` | landing-risk candidate | Optional narrow reaction or passive candidate that helps after landing from Jump. | Only if Dragonheart proves too broad; should not reduce all incoming damage. |

`Dragonheart` is the preferred Dragoon reaction identity if it can be bounded. It is acceptable for
an advanced job to offer an exciting reaction, but repeated auto-reraise loops would erase attrition
and undermine KO/revive pressure.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Equip Polearms` | spear build unlock | Let non-Dragoon jobs deliberately build around spear/thrust identity at support-slot cost. | T2.1/F5; must not make Dragoon irrelevant as the spear home. |
| `Jump Training` | Jump specialization candidate | Improve Jump speed, range discipline, or landing reliability only for Jump actions. | Must not become broad melee accuracy, broad mobility, or broad damage. |

`Equip Polearms` is healthy only if borrowed spear access creates distinct builds rather than
turning every physical shell into a better Dragoon. The support cost must remain meaningful because
spear is the cleanest generic thrust route in the current formula map.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Jump +1` | basic elevation movement | Improve vertical map feel without erasing terrain. | Early/mid investment; not a replacement for real movement. |
| `Jump +2` | advanced elevation movement | Let a physical unit choose vertical routes more reliably. | T2.1; should remain map-dependent. |
| `Jump +3` | late elevation movement | Strong vertical mobility reward for Dragoon route investment. | Must compete with Teleport, Move +2, and Ignore Elevation. |
| `Ignore Elevation` | terrain capstone | Let a late Dragoon-style build ignore height in movement. | High incidence risk; should not become the default movement for every late build. |

Movement Jump and active Jump should stay distinct in player-facing behavior. One improves where the
unit can move. The other is a delayed attack/airborne state.

### JP Progression

JP posture:

- baseline Jump should be available early enough that active Dragoon works immediately;
- early horizontal and vertical unlocks should be reachable without excessive grind;
- high horizontal and high vertical mastery should be real investments;
- Dragonheart and Ignore Elevation should be expensive because they are powerful cross-job pieces;
- Equip Polearms should sit late enough that spear ownership is earned, not automatic.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Dragoon should remain a midgame martial progression job. It should unlock late enough that spear
mobility feels like progress, but not so late that the only remaining value is a support detour.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Dragoon donor patterns:

- a durable job spends support slot on `Equip Polearms` to become a deliberate anti-mail reach unit;
- a mobile physical unit borrows Jump for map-specific delayed attacks;
- a Jump build uses movement/elevation tools to solve vertical maps;
- a risky frontline build takes Dragonheart as a late survival identity.

Unhealthy Dragoon donor patterns:

- every physical job uses spears because thrust/reach is too efficient;
- Jump becomes the safest damage loop because the unit is off-board too often;
- Dragonheart becomes the default reaction for survival on most builds;
- Ignore Elevation or Jump +3 becomes the default movement in most late builds.

### Expected Strong Builds

- active Dragoon with spear, plate, Jump, and vertical movement;
- Knight or Samurai using `Equip Polearms` for a specific anti-mail thrust plan;
- Dragoon with Monk or Squire secondary for close-range utility when Jump is wrong;
- anti-ranged or anti-caster map build that uses Jump timing to avoid predictable retaliation.

### Expected Weaknesses

- plate resists thrust, so Dragoon should need allies or secondary tools into heavy plate;
- Jump has CT, whiff, and landing risk;
- magic, status, and speed control can punish plate Dragoon;
- fast/mobile targets can waste delayed attacks;
- map-independent fights should not make every Jump unlock equally valuable.

### Expected Counters

- Stop, Slow, Immobilize, Sleep, or other timing/control effects;
- enemies that move before Jump resolves;
- crush-focused attackers into Dragoon's plate profile;
- mail-light enemy teams where thrust is less special;
- maps where off-board timing loses tempo or exposes allies.

### Ramza / Unique-Job Interaction

Ramza may later borrow knight/mage hybrid mobility or spear-adjacent fantasy, but he should not
erase Dragoon's identity as the dedicated Jump and spear job. If Ramza receives a Jump-like action,
it should be limited, chapter-bound, or hybridized rather than a better Dragoon Jump.

## Samurai

Job: Samurai
Status: Accepted for provisional design
Version: V1

### Baseline

Vanilla role: katana user with Iaido/Draw Out, Shirahadori, Equip Katana, and Doublehand.

Vanilla problems:

- Iaido can feel detached from Samurai's physical identity if it behaves like free magic attached
  to a plate job;
- katana break or inventory costs can make the kit awkward if the campaign economy is not tuned;
- several Iaido entries risk becoming a damage ladder instead of distinct tactical effects;
- Shirahadori can approach practical physical immunity if left too broad;
- Doublehand can become the universal physical support if opportunity cost is too low.

Accepted high-level role: disciplined Brave/katana physical job.

Primary role: `melee-physical`

Secondary tags: `katana`, `Brave`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `katana`, `fists`.

Armor class as target: `plate`.

Supported damage modes: `swing`, `crush`.

Formula v0.2 coupling:

- Samurai's katana lane is `br_pa_wp` swing, so Brave should matter in a visible but bounded way;
- katana swing stays familiar and strong, but plate resists swing and mail only partially rewards
  it;
- Samurai should not solve plate by default unless an Iaido or support investment explicitly
  creates a bounded alternate route;
- Doublehand is the protected Two Hands engine and must remain a meaningful build reward;
- damage Iaido is not allowed to remain Faith-independent reliable magic on a plate job;
- V1 commits damage Iaido to a Brave/PA physical katana-spirit route that uses katana identity and
  armor response rather than the caster `K * MA * Faith` model;
- support Iaido draws are separate gated effects, not proof that damage Iaido can bypass caster
  counterplay.

### Action Skillset Goals

Iaido should feel like drawing the spirit of the katana, not like buying a list of unrelated spells.

The player should ask:

- do I want direct katana pressure or a spirit draw effect?
- is this a Brave/katana build or am I borrowing Iaido from another shell?
- do I need a small area hit, support draw, curse draw, or premium finisher?
- is the target profile good for swing, or am I relying on a special effect instead?
- does the support/reaction package justify the Samurai route investment?

### Proposed Action Skills

The vanilla reference locally tags Iaido entries as magical special-case damage, but the extracted
table does not expose enough formula data to treat those tags as final formula authority. This V1
therefore preserves the Iaido name list as a design palette while committing to a routing principle:
damage Iaido is Brave/PA physical katana-spirit expression, not Faith-independent reliable magic.

Exact constants and shape are later, but the accepted V1 direction is:

- damage draws scale from Samurai's physical/Brave/katana identity rather than the ordinary caster
  Faith model;
- damage draws are subject to armor response through an explicit physical damage type, with `swing`
  as the default katana-spirit route unless a specific draw earns a bounded alternate type through
  T6/F5;
- damage draws may use area or line shapes only after T11 target-count normalization;
- damage draws must not be balanced by katana inventory breakage or campaign economy;
- Faith-independent magic damage on Samurai's plate body is rejected as the default route.

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Ashura` | early draw strike | Low-cost close spirit draw that teaches Iaido range and target selection. | Low ceiling; should not beat direct katana attacks by default. |
| `Kotetsu` | disciplined draw damage | Reliable midline Iaido damage for ordinary use. | Must have range/area/resource limits; no universal best button. |
| `Bizen Osafune` | focused pressure | Single-target or narrow-area katana spirit pressure with higher payoff. | Must not erase basic katana attack or Black Mage damage role. |
| `Murasame` | restorative draw | Preserve the classic support side of Iaido as a small area heal or recovery draw. | T3/T3xT5/T11; should not replace White Mage or Chemist. |
| `Ame-no-Murakumo` | area draw pressure | Mid/high Iaido damage or element-flavored pressure when positioning lines up. | T11 target-count normalization; not generic AoE superiority. |
| `Kiyomori` | protective draw | Protect/Shell/Wall-like discipline effect for nearby allies if retained. | Requires T6xPS; no invisible mandatory mitigation upkeep. |
| `Muramasa` | curse draw | Curse/status-flavored Iaido such as Confuse, Doom, Slow, or morale pressure. | T4/T5/status immunity; no broad hard-control package. |
| `Kiku-ichimonji` | line or reach draw | Reward formation and lane reading with a distinctive katana spirit line. | Needs geometry/target-count proof; not Archer/Summoner replacement. |
| `Masamune` | decisive support draw | Late discipline support such as Haste/Regen/Reraise-style momentum if retained. | T5/T3/T6xPS/T10 as applicable; must not become mandatory pre-buff. |
| `Chirijiraden` | premium draw finisher | High-JP capstone Iaido payoff for committed Samurai builds. | High cost/risk; must not make lower Iaido entries obsolete. |

Iaido should not be a pure vertical damage ladder. If two named Iaido actions cannot be given
distinct tactical roles after T11/T3/T5/T6xPS/T10 proof, the final data should consolidate or
repurpose one instead of preserving duplicate buttons.

Support draws are routed independently from damage draws:

- `Murasame`-style healing belongs to T3/T3xT5/T11;
- `Kiyomori`-style Protect/Shell/Wall belongs to T6xPS/T11;
- `Masamune`-style Haste/Regen/Reraise belongs to T5/T3/T11, and any Quick-like action grant,
  action refund, or turn recursion belongs to T10 before numeric acceptance.

Katana break or inventory consumption is not accepted as the balancing lever in this V1. It may be
revisited in the deferred campaign/economy policy if implementation wants it, but combat balance
should first make each Iaido action healthy on battle terms.

### Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Shirahadori` | disciplined weapon defense | Let a high-Brave Samurai-style build read and avoid a narrow class of weapon attacks. | T4/T2.1; no broad physical immunity, no all-range/all-family block. |
| `Bonecrusher` | critical retaliation candidate | Punish enemies when Samurai is near defeat, preserving the dangerous wounded-swordsman fantasy. | T3/T5 and action-risk proof; should not be the best general counter. |

`Shirahadori` is iconic and should be retained if possible, but not in a form that invalidates
physical enemies. Preferred bounds:

- weapon-family, facing, range, or direct-attack limits;
- no reliable protection against magic, status, area effects, guns, or non-weapon special attacks;
- Brave scaling must have a hard block-chance ceiling regardless of Brave, so Brave 97 cannot push
  Shirahadori toward practical physical immunity;
- lower or conditional value when stacked with shields, high class evasion, or Invisible-style
  effects.

The exact ceiling belongs to T4/T2.1, but uncapped Brave-as-block-rate is rejected in V1. The cap is
a design invariant, not a tuning suggestion.

### Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Equip Katana` | katana build unlock | Let non-Samurai jobs deliberately build around Brave-linked katana swing. | T2.1/F5; must not make Samurai only a support stop. |
| `Doublehand` | single-weapon power engine | Preserve the protected Two Hands `1.80` stress engine as a late physical reward. | T2.1/F5; must compete with Dual Wield, Attack Boost, Brawler, and equipment unlocks. |
| `Iaido Focus` | action-set specialization candidate | Improve Iaido reliability, radius discipline, or resource friction only for Iaido. | Optional; should not compress Doublehand plus broad magic damage plus defense. |

`Doublehand` should remain exciting. A late-game support can be very strong if it pushes a specific
build direction and costs the only support slot. It fails only if most serious physical builds take
it regardless of weapon family, armor profile, active job, or encounter.

### Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Waterwalking` | disciplined terrain movement | Preserve a flavorful terrain route if map distribution makes it meaningful. | Map-dependent; should not be valued as broad mobility. |
| `Blade Step` | stance/position candidate | Optional movement candidate if Samurai needs a non-water map identity. | Must not become a generic Move +N or Teleport replacement. |

The safer default is to preserve a terrain-flavored movement piece rather than give Samurai broad
mobility. Samurai already has high-risk global value through `Shirahadori` and `Doublehand`.

### JP Progression

JP posture:

- direct katana identity and one early Iaido draw should be reachable soon after unlock;
- support/healing/protection Iaido should cost enough to signal secondary-role investment;
- `Shirahadori` and `Doublehand` should be late/high-JP rewards;
- `Equip Katana` should be a meaningful route unlock, not cheap universal access;
- premium Iaido should be expensive without making early Iaido irrelevant.

### Prerequisite Changes

This proposal does not set job-tree prerequisites.

Samurai should remain advanced. Its support and reaction pieces are too build-defining to appear
early without distorting physical progression.

### Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

### Cross-Job Build Hooks

Healthy Samurai donor patterns:

- a physical unit spends support slot on `Doublehand` to become a committed single-weapon build;
- a Brave-focused build borrows `Equip Katana` for a disciplined swing plan;
- a durable unit borrows Iaido for a small area/support draw at the cost of its secondary slot;
- a high-risk frontline takes Shirahadori to answer a specific class of weapon pressure.

Unhealthy Samurai donor patterns:

- `Doublehand` becomes the default support for all physical builds;
- Shirahadori plus evasion creates practical immunity;
- Iaido becomes better Black Magic on a plate body;
- Equip Katana makes Samurai active shell irrelevant;
- premium Iaido replaces Summoner or Black Mage area roles without CT/MP/Faith/target-count tradeoffs.

### Expected Strong Builds

- active Samurai with katana, plate, Brave investment, and direct weapon pressure;
- Samurai with Iaido secondary plus a movement skill that positions for small-area draws;
- single-weapon build using `Doublehand` to push a deliberate high-commitment weapon identity;
- Brave-focused durable unit using Shirahadori against weapon-heavy enemies.

### Expected Weaknesses

- plate resists swing, so Samurai should not automatically dominate other plate jobs;
- lower reach than Dragoon and lower range than Archer;
- magical/status pressure can bypass weapon-defense identity;
- low Brave or Brave pressure should reduce katana and Shirahadori confidence;
- Iaido cannot be balanced only by JP if it ignores CT, MP, Faith, area, and weapon limits.

### Expected Counters

- magic and status actions that bypass Shirahadori;
- missile or gun pressure if Shirahadori is bounded away from broad projectile coverage;
- crush-focused attackers into Samurai's plate profile;
- Brave reduction or morale disruption;
- dispersed enemy formations that reduce Iaido area value.

### Ramza / Unique-Job Interaction

Ramza may later become an elite knight/mage hybrid, but Samurai should remain the generic katana and
Doublehand home. If Ramza gets a Brave-linked sword or spirit technique, it should express
protagonist hybrid identity rather than replacing Iaido or katana discipline.

## Scenario And Check Plan

Minimum provisional rows before concrete values:

| Scenario ID | Purpose | Required gates |
| --- | --- | --- |
| `J-DRG-EARLY-JUMP` | Early Dragoon uses baseline Jump against ordinary leather/cloth targets. | T4/T5/F5 |
| `J-DRG-MID-MAIL` | Spear attack and Jump prove Dragoon's anti-mail identity without universal dominance. | formula harness/T5 |
| `J-DRG-LATE-PLATE` | Dragoon into plate shows thrust resistance and need for allies/secondary plans. | formula harness/T6 |
| `J-DRG-WHIFF` | Target acts before Jump landing and exposes whiff/tempo cost. | T5 |
| `J-DRG-AIRBORNE` | Airborne untargetability does not create a loop that removes enemy agency. | T5xT8 |
| `J-DRG-RSM` | Dragonheart, Equip Polearms, Jump +3, and Ignore Elevation incidence stays bounded. | T2.1/T3 |
| `J-SAM-KATANA` | Katana direct pressure with normal/high Brave remains strong but shaped. | formula harness/F5 |
| `J-SAM-DOUBLEHAND` | Doublehand remains a protected strong engine without becoming universal. | T2.1/F5 |
| `J-SAM-SHIRAHADORI` | Shirahadori avoids practical immunity under evasion/Brave stress. | T4/T2.1 |
| `J-SAM-IAIDO-DAMAGE` | Damage Iaido is compared against Black Mage, Summoner, and direct katana attacks. | F4/T11/T5 |
| `J-SAM-IAIDO-SUPPORT` | Murasame/Kiyomori/Masamune-style support does not replace White Mage/Chemist or turn economy. | T3/T3xT5/T6xPS/T10/T11 |
| `J-SAM-IAIDO-CURSE` | Muramasa-style status/curse is bounded by accuracy, duration, immunity, and counterplay. | T4/T5 |

These are scenario requirements, not final scenario data.

## Formula Re-Sim Requirement

This proposal triggers formula review when values become concrete because it touches:

- spear and katana equipment access;
- Jump as delayed non-evadable physical damage;
- Jump as timed self-targetability exclusion through T5xT8;
- Doublehand as a protected physical stress engine;
- Shirahadori as a defensive reaction with a hard Brave-scaling ceiling;
- damage Iaido as Brave/PA physical katana-spirit behavior;
- support Iaido as healing/mitigation/status/timing area behavior;
- high-Brave Samurai stress rows;
- Dragoon and Samurai as plate target profiles.

The strongest available status before real weapon data and the required gates is:

```text
Accepted for provisional design
```

No concrete implementation data should be marked final until the affected T-gates pass and formula
v1 or its accepted successor reconciles real weapon values.

## Implementation Assumptions

- Data mod scope can rewrite skill names, targeting, JP costs, equipment access, and job properties
  where needed.
- If data records force the vanilla Jump unlock shape, design should still tune JP/range/timing so
  unlocks feel like meaningful map capability rather than mandatory filler.
- If Iaido cannot be implemented with healthy distinct effects for every named katana, duplicate
  entries should be consolidated, repurposed, or made into utility variants rather than kept as a
  damage ladder.
- Campaign/inventory costs for katana break are deferred; the current V1 balances Iaido as battle
  behavior first.

## Open Proof Needs

- Exact Jump targeting, panel-vs-unit tracking, and same-tick landing behavior under T5.
- Whether Jump non-evasion is acceptable after CT/whiff/airborne rows are checked.
- Whether Dragonheart can remain exciting without causing revive loops.
- Whether `Equip Polearms` produces healthy anti-mail builds or too much spear leakage.
- Exact T4/T2.1 ceiling for Shirahadori's Brave-scaled block chance.
- Whether Doublehand incidence stays below mandatory-piece thresholds.
- Exact damage Iaido constants inside the accepted Brave/PA physical katana-spirit route.
- Whether Iaido area geometry and target count can coexist with Summoner and Black Mage.

## Claude Review Request

Claude should review whether:

- Dragoon's Jump identity is specific enough without committing to final timing/range values;
- treating Jump unlocks as tactical reach bands solves the vanilla grind-ladder problem;
- Dragoon has enough active-job value beyond `Equip Polearms` and Dragonheart;
- Samurai's Iaido direction preserves identity without becoming free area magic on a plate job;
- Shirahadori and Doublehand are bounded enough as provisional design;
- the scenario/check plan names the right validation gates before concrete values;
- any role collision with Knight, Archer, Monk, Black Mage, Summoner, or Geomancer should be fixed
  before acceptance.

Claude review verdict: Conditional accepted (claude-opus-4-8, 2026-06-21).

Required edits applied:

- damage Iaido now has an explicit V1 routing principle: Brave/PA physical katana-spirit behavior,
  not Faith-independent reliable magic on a plate job;
- Shirahadori now requires a hard Brave-scaled block-chance ceiling regardless of Brave.

Recommended edits also applied:

- Jump airborne untargetability is labeled as T5xT8 composition;
- Dragoon and Thief thrust identities are explicitly separated by stat axis, role, and durability;
- Masamune-style action grants or turn recursion route to T10 before numeric acceptance.
