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
| T2/T2.1 build incidence | Built / dual-gated | Secondary/reaction/support/movement incidence, equip unlocks, mandatory-piece checks, late support pull. |
| T3/T3xT5 healing timing | Built / dual-gated | Squire/Chemist recovery, White Mage healing/revive, Monk Chakra/Revive, Auto-Potion, recovery races. |
| T4 accuracy/evasion | Built / dual-gated | Archer reliability, Knight/Special Knight guard checks, status hit rates, evasion/immunity risks. |
| T5 CT/timing | Built / dual-gated | Archer Aim/overwatch, Time Mage tempo, Dragoon timing, performance ticks, stance duration. |
| T6xT7 offense/armor composition | Built / dual-gated | Knight Rend follow-up, Special Knight temporary exposure, disarm/armor-response interactions. |
| T8 targeting/control | Built / dual-gated | Challenge, Intercede, undead control, Reflect routing setup, AI/targetability-sensitive skills. |
| T6xPS mitigation stacking | To build this sprint | Protect/Shell/Wall, Knight guard, Special Knight Aegis/Intervention, practical-immunity checks. |
| T8xSR spell routing | To build this sprint | Reflect, spell redirection, decoy/routing effects. |
| T9 resource/MP economy | To build this sprint | Chemist Ether, caster MP economy, Time/Mystic resources, Necromancer Syphon. |
| T10 action economy | To build this sprint | Quick, Critical: Quick, overwatch extra attacks, raised-body actions, Intervention if it grants extra attacks. |
| T11/T11A/T11B area and terrain | To build this sprint | Summoner, Geomancer, Meteor, area shape/target count, terrain availability, Archer line-pierce if multi-target. |
| T5xT8 timed untargetability | To build this sprint | Dragoon Jump, Vanish, Invisible/timed targetability exclusion. |
| T11xT5 sustained area throughput | To build this sprint | Bard/Dancer performances, auras, zones, repeated mapwide or large-area effects. |
| T3xT5xT11 sustained HP area effects | To build this sprint | Bard healing, Dancer attrition, repeated HP recovery/damage over target count and duration. |
| T3xT5xT8 corpse/undead state composition | To build this sprint | Necromancer corpse/raise sub-kit, undead targetability/control/expiry. |
| Real-roster F5 re-sim | To apply this sprint | Any new or materially changed job chassis, starting with Special Knight, must run on its actual roster row and not only formula anchor jobs. |

## Infra Sprint Directive

Marcelo selected the infra sprint first. Run one focused validation-infrastructure sprint before any
concrete numbers.

The sprint should:

- build the missing pinned input bundles for the highest-priority gates;
- keep the dual-independent GPT/Claude discipline;
- require `0` mismatches before a gate output can accept concrete skill values;
- promote shared assumptions into the relevant schema docs rather than hiding them in job prose;
- defer numeric JP, hit rate, CT, recovery, mitigation, and damage values until their gates exist.

This repeats the same infra-first decision Marcelo already accepted earlier in the job phase: do not
pretend the damage-only formula harness can answer mechanics it does not model.

Recommended sprint order:

1. Build T6xPS mitigation stacking first because it has the highest leverage across Knight, Special
   Knight, White Mage, Time Mage, Mystic, and late defensive builds.
2. Build T11/T11A/T11B next because T11xT5 and T3xT5xT11 depend on stable area/terrain assumptions.
3. Build T9, T10, and T3xT5xT8 after the defensive and area foundations are stable.
4. Build T11xT5 and T3xT5xT11 after area and timing assumptions can compose cleanly.
5. Build T8xSR and T5xT8 after the main routing, timing, and targetability assumptions are explicit.
6. Apply real-roster F5 re-sim discipline in parallel whenever a new or materially changed chassis is
   used in a formula-sensitive check.

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
