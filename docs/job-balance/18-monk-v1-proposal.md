# Monk V1 Proposal

Status: Accepted for provisional design
Version: V1
Date: 2026-06-21
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/04-foundation-physical-jobs-proposal.md`
- `docs/job-balance/10-healing-attrition-model-schema.md`
- `docs/job-balance/13-armor-response-model-schema.md`
- `docs/job-balance/16-healing-timing-composition-schema.md`
- `docs/job-balance/17-offense-armor-composition-schema.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`

## Purpose

This document proposes the first concrete skill direction for Monk.

The proposal is concrete enough to define skill roles, build hooks, validation needs, and identity
boundaries. It is not final implementation data. It does not set exact JP numbers, hit rates, CT
values, healing values, revive HP, damage multipliers, Brave scaling constants, equipment records,
stat multipliers, or prerequisites.

Monk is formula-sensitive because it touches:

- Brave-linked unarmed damage;
- protected `crush` identity;
- anti-plate pressure;
- sustain and revive;
- powerful global support/reaction pieces.

Concrete values therefore require the accepted T2, T3, T3xT5, T4, T5, T6, and T6xT7 checks named
below before final data acceptance.

Claude review verdict: conditional acceptance on 2026-06-21, with required edits applied in this
version.

## Group Thesis

Monk should be the protected unarmed impact job.

Its identity is body discipline:

- fight well without weapon equipment;
- turn Brave and positioning into physical confidence;
- pressure plate through `crush`;
- provide nearby sustain and emergency recovery;
- punish enemies that engage it carelessly.

Monk should not become the best damage job, best healer, best reviver, best reaction donor, and best
weapon-independent build shell at the same time.

## Baseline

Job: Monk
Status: Accepted for provisional design
Version: V1

Vanilla role: unarmed physical job with Martial Arts, Chakra, Revive, Counter, and strong Brave
scaling.

Vanilla problems:

- Martial Arts/Brawler can become a generic support answer if unarmed output is too broadly good;
- Chakra and Revive can compress sustain, MP support, and recovery into one secondary kit;
- ranged Martial Arts skills can make Monk too independent from map and weapon tradeoffs;
- Brave-scaling reactions can drift toward practical immunity if not bounded.

Accepted high-level role: unarmed crush/body-discipline physical job.

Primary role: `melee-physical`

Secondary tags: `crush`, `Brave`

Growth profile: `physical`

Current multipliers: unchanged in this proposal.

Proposed multipliers: no change in this proposal. Final active multipliers belong to the later
global multiplier pass.

Equipment access: inherited from role map; `fists`.

Armor class as target: `cloth`.

Supported damage mode: `crush`.

## Formula Coupling

Formula v0.2.1 treats `fists` as:

```text
routine = br_pa_pa
damage_type = crush
penetration = 0.15
```

This is Monk's protected lane.

Design implications:

- Monk should be a real anti-plate route because plate is weak to `crush`.
- Monk should not be universal physical damage because mail does not reward `crush` the same way.
- Generic fists access on other jobs is not enough to make them Monks.
- Knight's `Crushing Blow` and weapon-break fallback to fists must stay below Monk's protected
  unarmed identity unless they spend real action/setup cost.
- Brave scaling is allowed to matter, but high-Brave stress rows must prevent runaway offense or
  reaction dominance.

T6xT7 now proves why this matters: changing an attack to fists can recover damage into plate through
the `crush` matchup, but can be a bad trade into mail. Monk should own the version of this identity
that is intentional rather than accidental.

The protection mechanism is kit and scaling, not a hidden alternate fists routine. A disarmed unit
may fall back to the same raw `br_pa_pa`/`crush`/`0.15` fists routine, but it does not become a Monk
unless it also has the relevant build investment:

- no Martial Arts action set by default;
- no Monk action roles such as `Cyclone`, `Pummel`, `Aurablast`, or `Shockwave`;
- no `Brawler` or `Martial Discipline` scaling unless it spent the support slot;
- no always-on commitment to fists as the unit's primary plan.

Monk's lane is therefore raw fists plus Martial Arts actions plus deliberate unarmed support hooks.
Fallback fists are a temporary floor, not a full job identity.

## Action Skillset Goals

Martial Arts should remain versatile, but the choices should have shape.

The action set should create decisions around:

- adjacency versus short range;
- line and height constraints;
- anti-plate impact versus mail/spacing counters;
- self or nearby ally sustain;
- risky emergency revive;
- status/body discipline utility.

### Proposed Action Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Cyclone` | close area impact | Adjacent/body-centered `crush` pressure against clustered enemies. | Requires exposure; must not become the best general AoE. |
| `Pummel` | reliable close strike | Simple single-target unarmed hit when Monk wants certainty. | Lower ceiling than setup or positional options. |
| `Aurablast` | short-range force | Let Monk project limited impact without equipping a weapon. | Range/height/line limits; lower payoff than true melee. |
| `Shockwave` | line/ground impact | Reward lanes, terrain, or grounded targets with anti-plate pressure. | Map-dependent; should be worse into mail or bad lanes. |
| `Doom Fist` | telegraphed doom pressure | Keep the pressure-point fantasy by threatening Doom through a risky strike. | Must pass T4 accuracy, T5 countdown/telegraph timing, and immunity/undead checks. |
| `Purification` | body discipline cleanse | Clear a small set of statuses from self or nearby ally. | Narrow status list/range; should not replace dedicated support casters. |
| `Chakra` | nearby sustain | Restore HP through adjacency or tight formation; MP restore remains a separate open axis. | Position-limited; no infinite sustain loop; HP must pass T3/T3xT5; MP needs a future resource-economy gate. |
| `Revive` | risky emergency recovery | Bring back an adjacent or near ally in a dangerous frontline position. | Low/controlled HP return; timing and exposure matter; must pass T3xT5. |

`Cyclone`, `Pummel`, `Aurablast`, `Shockwave`, and `Doom Fist` are formula-sensitive once numeric.
They must respect Monk's protected `crush` identity without making equipment families irrelevant.

`Doom Fist` should not be flattened into generic guard pressure by default. Doom is an existing FFT
effect with a clear fantasy and a natural counter profile: telegraph, countdown, evasion, immunity,
undead handling, and removal. The concrete pass may still replace it if the checks fail, but the V1
preference is to preserve Doom and bound it instead of deleting its identity.

`Chakra` and `Revive` are sustain-sensitive. Their HP and revive values must run T3 and T3xT5 so
they are compared against Chemist item reliability and White Mage delayed healing instead of judged
only by per-action amount.

Chakra's MP restoration is explicitly not covered by T3/T3xT5. If MP restore stays in the final data,
it needs a resource-economy gate because MP sustain buys extra spellcasting, indirect offense, and
utility. Until that gate exists, the safe V1 assumption is HP-only Chakra or a heavily flagged
placeholder for MP restore.

## Reaction Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Counter` | melee retaliation | Punish adjacent physical attackers and make engaging Monk costly. | No broad ranged/magic retaliation; must respect facing/range/trigger limits. |
| `First Strike` | non-negating preemptive discipline | Let a committed Monk build strike before a narrow class of direct melee attacks. | The original attack must still resolve; no vanilla-style physical negation. |

If ability slots require one reaction, prefer `Counter` as the safer default Monk identity. `First
Strike` can remain only as a late/high-investment, non-negating reaction if T4/T5 and immunity rows
prove it is bounded.

Do not retain vanilla-style negating Hamedo. Canceling incoming physical attacks regardless of weapon
family flattens the weapon-family and armor-response work this mod is trying to create. Trigger-rate
gating alone is not enough to solve that structural problem.

## Support Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Brawler` | unarmed build engine | Let non-Monk jobs deliberately build around fists at support-slot cost. | Must not become the default physical support; must not repair weak non-Monk baselines. |
| `Martial Discipline` | body-tech focus | Improve Monk-style actions or unarmed control without boosting all physical damage. | Optional candidate only; should not compress Brawler plus reaction defense plus sustain. |

`Brawler` is allowed to be attractive because FFT build crafting needs exciting global pieces. It
must still pass T2.1 incidence and formula stress rows. If many late physical builds require Brawler,
then either fists are too broad or other weapon families are underperforming.

## Movement Skills

| Skill | Role | Intended use | Main limits |
| --- | --- | --- | --- |
| `Lifefont` | body recovery movement | Preserve the classic self-recovery movement hook as terrain/position sustain. | Small bounded recovery; must not erase attrition or replace real healing. |

`Lifefont` should be evaluated as sustain over time, not as a flavor-only movement skill. Concrete
values belong to T3/T3xT5 and T2.1 incidence.

## JP Progression

JP posture:

- one reliable close-range attack should be reachable early;
- one limited-range technique should arrive early or mid so Monk does not feel trapped by bad maps;
- `Chakra` should be reachable before late game, but exact value must not erase Chemist/White Mage;
- `Revive`, `First Strike`, and any high-impact anti-plate/guard-pressure action should require
  real investment;
- `Brawler` should be meaningful but not such an early tax that every physical unit detours Monk.

## Prerequisite Changes

This proposal does not set job-tree prerequisites.

Later progression design should keep Monk accessible early enough to be a real physical route, while
ensuring its best global pieces require commitment.

## Gender/Equipment Restrictions

No gender restrictions.

No equipment access changes in this proposal.

## Cross-Job Build Hooks

Healthy Monk donor patterns:

- active Monk borrows Squire, Chemist, or Knight utility while keeping fists/crush identity;
- physical job spends support slot on `Brawler` to become a deliberate unarmed build;
- frontline unit borrows Martial Arts for sustain/revive at the cost of other secondaries;
- high-Brave build uses Monk pieces for a distinct body-discipline plan.

Unhealthy Monk donor patterns:

- most physical builds take `Brawler`;
- Martial Arts secondary becomes the best generic damage plus healing plus revive package;
- `Counter` or `First Strike` becomes correct on most units;
- Monk sustain makes attrition and item economy irrelevant;
- unarmed damage makes weapon-family planning irrelevant.

## Expected Strong Builds

- active Monk with fists, high Brave, and Martial Arts as primary pressure;
- active Monk with Squire or Knight secondary for utility/control;
- non-Monk `Brawler` build that gives up another support to become a real unarmed specialist;
- close-formation party using Chakra/Revive as risky frontline recovery rather than safe backline
  healing.

## Expected Weaknesses

- cloth durability;
- limited weapon/equipment flexibility;
- lower value into mail or enemies that punish `crush`;
- exposure risk when using adjacency or body-centered actions;
- weaker into magic/status unless built for discipline/cleanse;
- sustain and revive constrained by range, timing, and danger.

## Expected Counters

- mail-armored or crush-resistant targets;
- ranged pressure and kiting;
- magic/status pressure;
- forced movement or terrain that breaks formation;
- low-Brave pressure or Brave manipulation;
- enemies that punish adjacency.

## Ramza / Unique-Job Interaction

Ramza may become a knight/mage hybrid later, but he should not strictly dominate Monk's protected
unarmed/crush niche. If Ramza gains body-discipline or unarmed tools, they should trade against his
leadership or magic options rather than becoming a superior Monk shell.

## Samurai Forward-Collision Note

Future Samurai also likely cares about Brave, but its Brave expression should differ from Monk.
Monk is weapon-independent, unarmed, and `crush`-driven. Samurai should be weapon-dependent and tied
to swing, draw, spirit, or katana identity rather than becoming another Brave/fists shell.

## Scenario/Check Plan

Required later:

- `J-EARLY-SELF`: Monk with one reliable close action and fists identity.
- `J-MID-SELF`: Monk with range/line option, sustain, and anti-plate pressure online.
- `J-MID-PARTY`: Monk in a normal party with item and caster support alternatives.
- `J-LATE-SELF`: active Monk remains valuable without only donating Brawler.
- `J-LATE-BUILD`: at least one strong high-Brave Monk or Brawler build.
- `J-STRESS-COUNTER`: Monk pressured by mail targets, ranged kiting, magic/status, and low-Brave.
- `J-STRESS-HIGH-BRAVE`: Monk at practical high Brave, around 97, tests `br_pa_pa` offense into
  plate and mail plus `Counter` trigger pressure.
- `J-STRESS-ARMOR-SPREAD`: same signature Monk action into plate and mail targets to prove the
  intended anti-plate gain and mail penalty are both real.
- `J-PARTY-NO-JOB`: comparable party plan that does not field active Monk.
- `M-RSM-COUNT-LATE`: `Counter`, `First Strike`, `Brawler`, `Lifefont`.
- `M-SECONDARY-COUNT`: Martial Arts secondary incidence.
- `I-PHYS-*`: `Counter`, `First Strike`, high-Brave retaliation, and evasion stacks.
- `I-ATTRITION`: `Chakra`, `Revive`, `Lifefont`, and any automatic recovery loops.

Conditional:

- T3 and T3xT5 for `Chakra`, `Revive`, `Lifefont`, and any recovery reaction.
- T6 and T6xT7 for anti-plate, guard-break, or response-changing Monk techniques.
- T4/T5 for `First Strike`, interruption, range, evasion, and line/height behavior.
- Gate F5 if concrete values alter fists damage, Brave scaling, armor response, recovery timing, or
  reaction immunity.

## Formula Re-Sim Requirement

No immediate Gate F5 re-sim is required for this document.

The concrete data pass must re-evaluate this if it:

- changes Monk stat multipliers;
- changes fists, Brave, or `br_pa_pa` formula assumptions;
- changes `crush` armor response;
- changes Brawler/Martial Arts support behavior;
- changes healing/revive amount, timing, range, or resource behavior;
- changes reaction timing, immunity, or trigger conditions.

## Implementation Assumptions

- Data modding can create or repurpose ability records.
- Skill names are placeholders until the implementation pass verifies text, record limits, and
  localization constraints.
- Preserve recognizable Monk flavor where possible: impact, wave/line force, Chakra, Revive,
  Purification, Counter, and Brawler.
- Exact values must be decided after validation, not inside this V1 proposal.

## Open Proof Needs

- Exact Monk action values after real weapon baseline T1 lands.
- Whether `Doom Fist` should stay as a status move, become guard pressure, or be replaced.
- Whether non-negating `First Strike` can be bounded enough to stay on Monk.
- Whether `Brawler` incidence is healthy once more job slices exist.
- Whether a Monk armor-cracker/team-exposure action using the validated `response_delta` channel
  should exist without stepping on Knight or Squire.
