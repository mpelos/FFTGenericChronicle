# Campaign Validation Readiness V0

Status: Accepted as authoritative campaign readiness map
Date: 2026-06-21
Depends on:
- `docs/job-balance/02-job-design-protocol.md`
- `docs/job-balance/30-roster-completion-and-infra-sprint-plan.md`
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/32-campaign-artifacts-provisional-v0.md`
- `docs/job-balance/42-white-black-mage-concrete-v0.md`
- `docs/job-balance/43-summoner-geomancer-concrete-v0.md`
- `docs/job-balance/44-time-mystic-concrete-v0.md`
- `docs/job-balance/45-necromancer-concrete-v0.md`
- `docs/job-balance/46-bard-dancer-concrete-v0.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/gpt-campaign-validation-readiness-v0.json`

## Purpose

This document is the readiness map between accepted concrete-provisional job work and the full
campaign validation goal.

It does not claim the campaign is validated.

It records:

- what evidence is already accepted;
- what producer steps are still missing;
- which campaign risks must stay in scope;
- how the doc 31 campaign artifacts map onto the next work packages;
- why `Campaign Journey Bundle V0` is the next draftable artifact even though final numeric
  acceptance still waits on T1/formula-v1.

Claude review verdict: accepted on 2026-06-21 with no required content changes after R1-R4 and
r1-r3 were applied.

## Current Milestone

The generic job design roster is complete. The concrete-provisional formula-sensitive cluster pass
is also complete and committed for:

| Cluster | Doc | Status |
| --- | --- | --- |
| White Mage / Black Mage | `docs/job-balance/42-white-black-mage-concrete-v0.md` | accepted concrete-provisional |
| Summoner / Geomancer | `docs/job-balance/43-summoner-geomancer-concrete-v0.md` | accepted concrete-provisional |
| Time Mage / Mystic | `docs/job-balance/44-time-mystic-concrete-v0.md` | accepted concrete-provisional |
| Necromancer | `docs/job-balance/45-necromancer-concrete-v0.md` | accepted concrete-provisional |
| Bard / Dancer | `docs/job-balance/46-bard-dancer-concrete-v0.md` | accepted concrete-provisional |

Those values are stable enough for planning and provisional journey rows. They are not final
implementation data.

## Evidence State

| Area | Evidence now | Readiness |
| --- | --- | --- |
| Vanilla skill/status atlas | `docs/job-balance/12-vanilla-skill-status-reference.md` and `docs/reference/*` | Ready as consultation surface. |
| Mechanic gates | Full accepted gate set across docs 08-17 and 33-41, including T2/T3/T4/T5/T6/T7/T8 and composition gates. | Ready as mechanic-level tooling. |
| Campaign model | Docs 31-32 define bands, party journeys, A1-A5 artifacts, detour risks, and synthetic encounter families. | Ready as provisional structure, not final proof. |
| Concrete cluster values | Docs 42-46 accepted. | Ready for covered clusters. |
| Physical/foundation concrete values | Squire, Chemist, Knight, Archer, Monk, Thief, Orator, Dragoon, Samurai, Ninja, and Special Knight still mostly have V1 identity, not full numeric action values. | Missing producer step. |
| Prerequisites, JP, equipment | A2 ledger is provisional and has no exact prerequisite tree, JP costs, JP gain, JP Boost impact, or equipment tier timing. | Missing producer step. |
| RSM values | Most reaction/support/movement and equipment-unlock candidates are deferred until T2.1. | Missing producer step. |
| Formula-v1 | T1 Windows weapon dump is missing. | Final numeric acceptance not ready. |
| Real-roster F5 | Not yet run on actual roster rows. | Final numeric acceptance not ready. |

## Requirement Audit

| Goal requirement | Evidence now | Status | Missing proof |
| --- | --- | --- | --- |
| Fresh game with four generics plus Ramza. | Docs 31-32 define P0/P1/P5 and wall-test party families. | Incomplete. | W2 journey bundle with per-band party rows, jobs, skills, JP envelope, equipment tier, and encounter anchor. |
| Job unlock, JP, and skill progression. | A2 ledger gives intended bands only. | Incomplete. | W3 prerequisite/JP/equipment/RSM producer data. |
| Player cannot become too strong too early. | A3/A4 identify risks. | Incomplete. | Early-band W2 rows plus T2.1, A2 pacing, and F5 checks. |
| Party options stay balanced and identities useful. | V1 docs plus concrete cluster docs define lane separation. | Incomplete. | W5 real-roster F5 five-metric comparison. |
| Real cross-job builds are evaluated. | Docs 31-32 list expected stacks and detours. | Incomplete. | W2 party builds plus W4 populated incidence. |
| Ramza is broad but not specialist-dominant. | Doc 31 names the proxy. | Incomplete. | Dedicated Ramza-vs-specialist rows in W2/W5 for every band. |
| Risks, recommendations, and adjustments are documented. | A4 and concrete docs have watch items. | Partial. | W6 campaign risk/adjustment report after W4/W5. |
| Claude approval before acceptance. | Accepted through doc 46. | Active. | Claude approval for this doc and every later campaign artifact. |

## Work Package Crosswalk

The W-series below is not a replacement for doc 31's A1-A5. It is the execution order that produces
those artifacts.

| Work package | Produces or instantiates | Purpose | Can start now |
| --- | --- | --- | --- |
| W1 - Formula-v1 data | T1 weapon dump, formula-v1 bundle, formula-v1 scenario rerun | Replace WotL-fallback formula inputs and stress references. | No, external Windows step. |
| W2 - Campaign Journey Bundle V0 | A1 party matrix, A3 five-unit stack sheet, requires A5 encounter anchors | Pin provisional 4-generics-plus-Ramza rows for P0/P1/P5/P2/P3/P4/P6. | Yes. |
| W3 - Progression And Build Input Producers | A2 unlock/JP/equipment ledger, physical/foundation concrete values, candidate RSM values | Produce the data that W4 and W5 consume. | Partly. |
| W4 - T2.1 Populated Incidence | A2 incidence checks plus secondary/reaction/support/movement/equipment-unlock pressure | Detect mandatory supports, movement defaults, and cross-job convergence. | After W2/W3. |
| W5 - Real-Roster F5 Sweep | Five-metric party/build comparison and dominance checks | Test damage, sustain, control, mobility, safety/risk on actual rows. | After W1/W2/W3. |
| W6 - Risk And Adjustment Report | A4 detour pressure report plus final risk register | Convert evidence into tuning, prerequisite, JP, or skill-change recommendations. | After W4/W5. |

W2 is the immediate next draft because it can be provisional, gives W4/W5 concrete party rows, and
directly advances the active 4-generics-plus-Ramza goal. W2 rows must include an encounter anchor:
either a named IVC encounter or a pinned synthetic A5 stat block. Without A5, W2 can define the
journey but cannot claim pass/fail.

W2/A3 also keeps the dual-independent ceiling discipline:

```text
GPT proposes strongest plausible stack
Claude proposes independent strongest plausible stack
both reconcile before the stack is accepted as a ceiling row
```

## Formula-v1 And F5 Dependency Chain

Final numeric acceptance must follow:

```text
T1 Windows weapon dump -> formula-v1 input bundle -> formula-v1 scenario rerun
-> real-roster F5 -> T2.1 populated incidence -> campaign adjustment report
```

F5 must use actual roster rows, not only anchor jobs such as Archer, Black Mage, Geomancer, or
Knight. This matters especially for Summoner, Time Mage, Bard/Dancer, Necromancer, Special Knight,
and Ramza.

## Primary Quantified F5 Dominance Vector

The only currently quantified breach vector is:

```text
Belief x Oil x fire-weak area damage
```

Current accepted layers:

```text
Belief = 1.15
Oil / fire weakness = 2.0
combined = 2.30
```

Constructible chain:

| Step | Job | Action | Role |
| ---: | --- | --- | --- |
| 1 | Mystic | `Belief` | Faith-linked amount setup. |
| 2 | Geomancer | `Magma Surge` / Oil terrain setup | Creates fire vulnerability if terrain and positioning allow. |
| 3 | Summoner | `Ifrit` or `Salamander` | Ally-safe fire area payoff. |

Current stress rows:

| Row | Neutral 3-target total | Weak/Oil x2 total | Belief x Weak/Oil total | Ratio vs 415 |
| --- | ---: | ---: | ---: | ---: |
| `Ifrit` | 243 | 486 | 558 | 1.345 |
| `Salamander` | 297 | 594 | 681 | 1.641 |

This is the highest-priority F5 row because it already has quantified dominance pressure.

Authorized first levers if it fails:

- per-cast ceiling;
- diminishing per-target scaling;
- lower fire-summon K values;
- stricter terrain/Oil availability.

Do not start by globally nerfing swords, all summons, all Faith, or all elemental weakness.

## Other F5 Watch Rows

| Row | Value | Ratio | Read |
| --- | ---: | ---: | --- |
| `Bahamut`, 3 targets | 405 | 0.976 of 415 | Can cross if T1 lowers physical ceiling or real Summoner MA rises. |
| `Meteor`, 3 targets | 402 | 0.969 of 415 | Same risk, plus Time Mage action-economy adjacency. |
| `Life's Anthem`, HP 624 cap 3 | 384 | 0.948 of Bahamut 405 | Delayed and interruptible, but close enough to watch. |
| `Gravebind` area attrition | 315 | 0.759 of 415 | Separate non-elemental percent-HP attrition; not Faith/Oil amplified. |
| `Undead Mark` healing inversion | state inversion | n/a | State semantics, not damage amplification. |

## Risk Register

| Priority | Risk | Why it remains live | Required proof |
| ---: | --- | --- | --- |
| 1 | Belief/Oil/fire-weak area compound | Already quantified at up to 681, 1.641x the 415 physical reference. | W5 F5 cluster rows. |
| 2 | JP Boost acceleration | Can move deep job power into earlier encounter bands without being a combat stat. | W2/W3/W4 with and without JP Boost. |
| 3 | Time Mage systemic compression | Haste, Quick, Swiftspell, Teleport, Reflect, Slow/Stop, and Meteor touch too many axes. | T5/T10/T2.1/F5. |
| 4 | Caster economy convergence | Swiftspell, Halve MP, Manafont, Summon Focus, and magic-damage supports can collapse caster choice. | T9/T10/F4/T2.1. |
| 5 | Mitigation stack | Plate/shield plus Protect/Shell plus reactions can erase ordinary pressure. | T6xPS plus Band C rows. |
| 6 | Early physical full-package | An early party could cover damage, sustain, range, durability, and control too soon. | W2 Band B/C ceiling rows and Full-Package Rule checks. |
| 7 | Late physical support convergence | Dual Wield, Doublehand, Attack Boost, Brawler, Concentration, premium weapons, and mobility can converge. | T2.1 and F5 physical-heavy rows. |
| 8 | Equipment-tier breakpoints | Shop/gil/weapon timing can create hidden spikes or dead zones independent of job unlocks. | A2/W3 plus F5 real-roster re-sim. |
| 9 | Mobility convergence | Teleport, Move +3, Ignore Elevation, Fly, and terrain mobility can become correct for most builds. | T2.1 movement incidence. |
| 10 | Sustain compression | Chemist, White Mage, Monk, Bard, and Necromancer recovery can overlap too cheaply. | T3/T3xT5/T3xT5xT11 plus W5 sustain axis. |
| 11 | Late-job replacement pressure | Necromancer, Special Knight, and final Ramza must not erase older specialists. | Band E W5 comparisons. |
| 12 | Bard/Dancer global performance | Global value must matter without becoming mandatory infrastructure or a gender advantage. | T11xT5/T3xT5xT11 plus P6 row. |

## Ramza Dominance Rows

Ramza must be measured separately because he is present in every party.

W2/W5 must include rows where Ramza is compared against the protected specialist in that
specialist's own lane:

| Band | Ramza check |
| --- | --- |
| 0/A | Ramza should be flexible but not invalidate Squire/Chemist floor roles. |
| B | Ramza should not beat Knight, Archer, White Mage, or Black Mage inside their first-specialist lanes. |
| C | Ramza should not outclass Time/Mystic/Geomancer/Dragoon/Orator branch identity. |
| D | Ramza should not erase Samurai/Ninja/Summoner/performer advanced identities. |
| E | Final Ramza may be top-tier broad, but older specialists still need rational final-party slots. |

## Cross-Job Timing Invariants

W3/W5 must preserve doc 31's I1-I10 timing invariants when prerequisites, JP, and equipment are
drafted. In practical terms:

- no early support route should unlock a complete damage/sustain/mobility/safety package;
- no single movement family should become correct for most jobs;
- no equipment unlock should erase a job's natural weakness before that weakness has campaign value;
- no caster economy package should make MP, CT, Faith, and fragility irrelevant at the same time;
- no late reward should become mandatory for ordinary final-party viability.

## Scope Boundaries

This readiness map does not decide final:

- JP costs;
- prerequisite tree;
- equipment shop timing;
- physical/foundation action values;
- RSM values;
- Ramza chapter values;
- Special Knight real row;
- Necromancer real row;
- campaign pass/fail.

It clears the next planning step only:

```text
draft Campaign Journey Bundle V0
```

## Claude Review Verdict

Claude accepted this document as the authoritative campaign readiness map before W2.

Accepted checks:

- R1 applied: risk register restored doc 31 risks and split early physical full-package from late
  physical support convergence.
- R2 applied: physical/foundation concrete values, A2 ledger, and candidate RSM values are explicit
  producer steps.
- R3 applied: W-to-A crosswalk is explicit and preserves A3 dual-independent ceiling discipline.
- R4 applied: Ramza dominance rows are a dedicated requirement.
- r1 applied: evidence state credits the full accepted gate set.
- r2 applied: Belief/Oil/fire is priority #1 as the quantified breach vector.
- r3 applied: cross-job timing invariants are bound to W3/W5.

Claude independently recomputed the Belief/Oil/fire rows and tight-ceiling rows with no mismatch.
