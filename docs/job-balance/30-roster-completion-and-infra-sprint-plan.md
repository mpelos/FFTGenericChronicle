# Roster Completion And Infra Sprint Plan

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/00-high-level-direction.md`
- `docs/job-balance/01-cross-job-build-principles.md`
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/03-job-roster-and-role-map.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/job-balance/23-deferred-campaign-economy-policy.md`
- `docs/job-balance/27-necromancer-v1-proposal.md`
- `docs/job-balance/28-foundation-reconciliation-v1.md`
- `docs/job-balance/29-special-knight-v1-proposal.md`

## Purpose

This document records the completion milestone for the generic-job design roster and recommends the
next phase.

It does not define concrete numbers. It does not approve implementation data. It records that the
identity, lane-separation, and validation-gate planning pass is complete enough to stop writing more
job identity proposals and move into validation infrastructure before numeric tuning.

## Milestone

Generic-job roster design V1 is complete.

Covered jobs:

- Squire and Chemist;
- Knight and Archer;
- Monk;
- White Mage and Black Mage;
- Time Mage and Mystic;
- Summoner and Geomancer;
- Thief and Orator;
- Dragoon and Samurai;
- Ninja;
- Bard and Dancer, with shared reaction/support/movement parity;
- Necromancer as the Calculator replacement;
- Special Knight as the Mime replacement.

The foundation jobs have also been reconciled against the accepted vanilla ability/status atlas.

## What Is Now Decided

- Every generic job has a provisional role, identity boundary, and V1 skill-direction document.
- Calculator is removed and replaced by Necromancer.
- Mime is removed and replaced by Special Knight.
- Bard and Dancer remain gender-restricted, but their reaction/support/movement records must be
  identical.
- Gender-based equipment restrictions are removed outside Bard/Dancer job access.
- Monsters remain out of scope.
- Growth profiles remain simplified into physical, magical, and hybrid buckets for future numeric
  work.
- No concrete skill values are final until the relevant validation tracks pass.

## Accrued Gate Backlog

The next phase should build and dual-gate the validation infrastructure that accumulated during job
design.

| Gate | Status | Unblocks |
| --- | --- | --- |
| T2.0 build-incidence harness | Built / dual-gated on empty bundle | Harness plumbing for secondary/reaction/support/movement incidence, equip unlocks, mandatory-piece checks, late support pull. |
| T2.1 populated build incidence | Pending concrete accepted-provisional builds | Real incidence math for mandatory-piece checks and late support pull after concrete job builds exist. |
| T3/T3xT5 healing timing | Built / dual-gated | Squire/Chemist recovery, White Mage healing/revive, Monk Chakra/Revive, Auto-Potion, recovery races. |
| T4 accuracy/evasion | Built / dual-gated | Archer reliability, Knight/Special Knight guard checks, status hit rates, evasion/immunity risks. |
| T5 CT/timing | Built / dual-gated | Archer Aim/overwatch, Time Mage tempo, Dragoon timing, performance ticks, stance duration. |
| T6xT7 offense/armor composition | Built / dual-gated | Knight Rend follow-up, Special Knight temporary exposure, disarm/armor-response interactions. |
| T8 targeting/control | Built / dual-gated | Challenge, Intercede, undead control, Reflect routing setup, AI/targetability-sensitive skills. |
| T6xPS mitigation stacking | Built / dual-gated | Protect/Shell/Wall, Knight guard, Special Knight Aegis/Intervention, practical-immunity checks. |
| T8xSR spell routing | Built / dual-gated | Reflect, spell redirection, decoy/routing effects. |
| T9 resource/MP economy | Built / dual-gated | Chemist Ether, caster MP economy, Time/Mystic resources, Necromancer Syphon. |
| T10 action economy | Built / dual-gated | Quick, Critical: Quick, overwatch extra attacks, raised-body actions, Intervention if it grants extra attacks. |
| T11/T11A/T11B area and terrain | Built / dual-gated | Summoner, Geomancer, Meteor, area shape/target count, terrain availability, Archer line-pierce if multi-target. |
| T5xT8 timed untargetability | Built / dual-gated | Dragoon Jump, Vanish, Invisible/timed targetability exclusion. |
| T11xT5 sustained area throughput | Built / dual-gated | Bard/Dancer performances, auras, zones, repeated mapwide or large-area effects. |
| T3xT5xT11 sustained HP area effects | Built / dual-gated | Bard healing, Dancer attrition, repeated HP recovery/damage over target count and duration. |
| T3xT5xT8 corpse/undead state composition | Built / dual-gated | Necromancer corpse/raise sub-kit, undead targetability/control/expiry. |
| Real-roster F5 re-sim | Required during concrete value passes | Any new or materially changed job chassis, starting with Special Knight, must run on its actual roster row and not only formula anchor jobs. |

## Infra Sprint Completion

The infra sprint is complete as of 2026-06-21.

The final sprint pass added the missing composition gates in `docs/job-balance/33-41` and their
pinned bundles. Each completed gate has:

- a schema document;
- a GPT-owned canonical checker or generated output;
- a Claude-owned independent checker or independent recomputation path;
- `0` mismatches on the accepted pinned rows.

Completed sprint gates:

| Gate | Schema | Accepted GPT rows |
| --- | --- | ---: |
| T6xPS mitigation stacking | `docs/job-balance/33-mitigation-stack-composition-schema.md` | 14 |
| T11 area/terrain | `docs/job-balance/34-area-terrain-model-schema.md` | 20 |
| T9 resource/MP economy | `docs/job-balance/35-resource-economy-model-schema.md` | 24 |
| T10 action economy | `docs/job-balance/36-action-economy-model-schema.md` | 21 |
| T3xT5xT8 KO/corpse/undead state | `docs/job-balance/37-ko-corpse-undead-state-composition-schema.md` | 21 |
| T8xSR spell routing/Reflect | `docs/job-balance/38-spell-routing-reflect-composition-schema.md` | 15 |
| T5xT8 timed untargetability | `docs/job-balance/39-timed-untargetability-composition-schema.md` | 18 |
| T11xT5 sustained area throughput | `docs/job-balance/40-sustained-area-throughput-composition-schema.md` | 17 |
| T3xT5xT11 area HP over time | `docs/job-balance/41-area-hp-over-time-composition-schema.md` | 15 |

This does not finalize job numbers. It means the mechanics needed by concrete skill-value passes now
have accepted validation tracks.

## Infra Sprint Directive

Marcelo selected the infra sprint first. That sprint has now been completed. Future concrete-number
passes should use the accepted gates above instead of inventing local proof rules inside job prose.

The completed sprint followed this directive:

- build the missing pinned input bundles for the highest-priority gates;
- keep the dual-independent GPT/Claude discipline;
- require `0` mismatches before a gate output can accept concrete skill values;
- promote shared assumptions into the relevant schema docs rather than hiding them in job prose;
- defer numeric JP, hit rate, CT, recovery, mitigation, and damage values until their gates exist.

This repeats the same infra-first decision Marcelo already accepted earlier in the job phase: do not
pretend the damage-only formula harness can answer mechanics it does not model.

The executed sprint order was:

1. Build T6xPS mitigation stacking first because it has the highest leverage across Knight, Special
   Knight, White Mage, Time Mage, Mystic, and late defensive builds.
2. Build T11/T11A/T11B next because T11xT5 and T3xT5xT11 depend on stable area/terrain assumptions.
3. Build T9, T10, and T3xT5xT8 after the defensive and area foundations are stable.
4. Build T8xSR and T5xT8 after the main routing, timing, and targetability assumptions are explicit.
5. Build T11xT5 and T3xT5xT11 after area and timing assumptions can compose cleanly.
6. Apply real-roster F5 re-sim discipline during concrete values whenever a new or materially
   changed chassis is used in a formula-sensitive check.

## Next Phase

The next phase is concrete values for jobs and skill packages.

Concrete passes should:

- choose a small job pair or mechanics cluster;
- bind every proposed number to the relevant accepted gates;
- run the cross-phase formula re-sim when Gate F5 triggers;
- keep the dual-independent GPT/Claude review discipline;
- remain provisional until the Windows weapon dump and formula-balance v1 unblock final numeric
  acceptance.

High-leverage first candidates are caster, area, performer, and late-state jobs that drove the most
new gates:

- White Mage and Black Mage;
- Summoner and Geomancer;
- Bard and Dancer;
- Necromancer.

## External Dependency

T1 remains the one external dependency for final formula values:

```text
Windows weapon dump via tools/dump_weapons.py -> work/baseline_weapons.csv
```

Design work can continue without it, but final numeric acceptance cannot.

## Deferred Campaign Policy

T12/campaign economy remains policy-only for now.

`docs/job-balance/23-deferred-campaign-economy-policy.md` controls this area. Do not turn campaign
economy into a dual-sim gate until the user explicitly reopens that phase.

## Claude Review

Claude should review whether:

- the roster-complete claim is accurate;
- the gate backlog is complete enough for the next phase;
- the real-roster re-sim protocol edit belongs in `docs/job-balance/02-job-design-protocol.md`;
- this is the right handoff point before concrete numbers.

Claude review verdict: accepted by claude-opus-4-8 on 2026-06-21.

Review notes:

- roster-complete claim accepted;
- gate backlog accepted as sufficient for the next phase;
- real-roster re-sim protocol edit accepted for `docs/job-balance/02-job-design-protocol.md`;
- infra-sprint-first direction accepted as the correct handoff point before concrete numbers;
- T1 Windows weapon dump remains deferred;
- T12/campaign economy remains policy-only until the user explicitly reopens it.
