# W4 T2.1 Populated Incidence Plan V0

Status: Accepted by Claude review
Date: 2026-06-22
Depends on:
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/47-campaign-validation-readiness-v0.md`
- `docs/job-balance/50-campaign-journey-bundle-v0.md`
- `docs/job-balance/51-progression-and-build-input-ledger-v0.md`
- `docs/job-balance/52-squire-chemist-concrete-v0.md`
- `docs/job-balance/53-knight-archer-concrete-v0.md`
- `docs/job-balance/54-monk-thief-concrete-v0.md`
- `docs/job-balance/55-orator-dragoon-concrete-v0.md`
- `docs/job-balance/56-samurai-ninja-concrete-v0.md`
- `docs/job-balance/57-vanguard-ramza-concrete-v0.md`
- `docs/job-balance/58-physical-foundation-rsm-concrete-v0.md`
- `docs/job-balance/59-equipment-availability-timing-v0.md`
- `docs/job-balance/60-prerequisite-tree-and-jp-cost-draft-v0.md`
- `docs/job-balance/61-jp-boost-removal-decision-v0.md`
- `work/gpt-campaign-journey-bundle-v0.json`
- `work/gpt-physical-foundation-rsm-concrete-v0.json`
- `work/gpt-equipment-availability-timing-v0.json`
- `work/gpt-prerequisite-tree-jp-cost-v0.json`

## Purpose

This document defines how W4/T2.1 populated incidence will be built before any incidence matrix is
accepted.

W4 is a structural validation step. It does not prove that the campaign is balanced and does not
change skill values. Its job is to enumerate which reaction/support/movement pieces, equipment
exports, secondaries, and key build packages are realistically online in each campaign band for a
Ramza plus four generic party.

W4 answers this question:

```text
Given the accepted W2/W3 inputs, does any support, reaction, movement, secondary, equipment unlock,
or package start feeling mandatory too early?
```

The output of this plan is the contract for:

- `docs/job-balance/63-w4-t21-populated-incidence-v0.md`;
- `work/gpt-w4-t21-populated-incidence-v0.json`.

## Review Protocol

This artifact is written under the autonomous GPT/Claude workflow requested by Marcelo.

- GPT drafts the artifact.
- Claude reviews before acceptance.
- No accepted W4/W5/W6 document or commit is valid without approval by both GPT and Claude.
- Design choices are decided by GPT and Claude without asking Marcelo unless implementation becomes
  impossible, not merely difficult or debatable.
- One review round means one pinned version. If a future relay supersedes a prior version, it must
  explicitly say `SUPERSEDES <stamp> - DISCARD THAT REVIEW`.

## Source Of Truth Priority

W4 builds on accepted artifacts. It must not silently re-derive or rewrite them.

| Input | W4 use |
| --- | --- |
| Doc 50 A1 rows | Party rows: P0, P1, P5, P2, P3, P4, P6 by band. |
| Doc 50 A5 anchors | Encounter anchors for incidence context and later W5 rows. |
| Doc 50 A3 ceiling stack | Starting ceiling hypotheses; optimizer rows must still be dual-independent. |
| Doc 51 ledger | Broad build-risk map, power category, and immediate W4/W5 row list. |
| Doc 58 R/S/M | Slot model and concrete physical/foundation R/S/M values. |
| Doc 59 equipment timing | Practical-online gates for equipment exports and inventory routes. |
| Doc 60 prereq/JP | Prerequisite tree, depth thresholds, JP costs, protected convergence rows, and W4 consumption rules. |
| Doc 61 pacing | Fixed-JP ordinary/optimizer/grind-heavy routing; no JP Boost split. |

Any W4 row that diverges from doc 50 party expectations must declare that divergence explicitly.
Silent reframing is not allowed.

## Incidence Unit

The W4 unit of analysis is:

```text
band x party_row x progression_mode
```

Where:

- `band` is one of `0`, `A`, `B`, `C`, `D`, or `E`;
- `party_row` is inherited from doc 50: `P0`, `P1`, `P5`, `P2`, `P3`, `P4`, or `P6`;
- `progression_mode` is one of `ordinary`, `optimizer`, or `grind_heavy`;
- every row assumes Ramza plus four generic characters unless the doc 50 row is a wall-test family
  that narrows the active-job mix.

Each populated incidence row must enumerate, not merely describe:

- active jobs;
- expected secondaries;
- online reactions;
- online supports;
- online movements;
- equipment exports;
- key active-job packages;
- unavailable protected pieces and why they are filtered out;
- mandatory-feeling flags;
- W5 rows created by the incidence result.

## Progression Modes

W4 uses fixed JP. It does not create `with JP Boost` or `without JP Boost` splits.

| Mode | Meaning | Allowed use |
| --- | --- | --- |
| `ordinary` | A plausible non-guide route inside the band envelope. | Floor and normal campaign rows. |
| `optimizer` | A legal, focused route that prioritizes strong packages without assuming excessive grind. | Ceiling and false-choice rows. |
| `grind_heavy` | A deliberately grind-forward route that tests whether deep pieces move too early. | Grind-to-break stress only; never a floor assumption. |

`grind_heavy` is a warning mode, not a target player experience. It may justify W6 tuning when it
moves Band D/E power into ordinary Band B/C pressure, but it does not by itself prove that a piece is
too strong.

## JP Accumulation Envelope

W4 needs an explicit provisional JP accumulation model. Without it, `job depth`, `JP payable`, and
`grind_heavy` would be subjective labels instead of reproducible filters.

This envelope is T1-provisional. The project does not yet have authoritative extracted per-battle
JP reward and routing data. Like doc 59 equipment availability, this table is an accepted design
input for W4, not final implementation truth.

The values below represent plausible earned JP in a routed donor job for one unit by the end of the
band. They are checked against doc 60's depth thresholds:

| Depth | Earned JP threshold |
| --- | ---: |
| Lv1 | 0 |
| Lv2 | 250 |
| Lv3 | 650 |
| Lv4 | 1200 |
| Lv5 | 2000 |

| Band | `ordinary` donor JP | `optimizer` donor JP | `grind_heavy` donor JP | Read |
| --- | ---: | ---: | ---: | --- |
| 0 | 0-80 | 80-150 | 150-250 | Starter actions only; Lv2 is a warning edge, not a floor assumption. |
| A | 150-250 | 250-450 | 450-650 | Shallow route becomes plausible; first-specialist rush is tested here. |
| B | 350-650 | 650-900 | 900-1200 | First specialists and Lv3 commitment become plausible, but Lv4 is grind pressure. |
| C | 650-1000 | 1000-1400 | 1400-2000 | Midgame branches and first strong exports become testable. |
| D | 1000-1600 | 1600-2400 | 2400-3200 | Advanced engines are plausible one route at a time. |
| E | 1400-2200 | 2200-3400 | 3400-4500 | Late rewards and mastery routes are plausible, but still slot-limited. |

W4 applies this envelope per donor job, not as a free global pool.

Single-piece test:

```text
online if donor_jp_ceiling >= max(required_depth_threshold, piece_jp_cost)
```

Same-donor package test:

```text
online if donor_jp_ceiling >= max(required_depth_threshold, sum(piece_jp_costs_in_that_donor))
```

Multi-donor package test:

```text
each donor must pass its own test, and the row must respect route-load limits
```

Route-load limits:

| Mode | Route load |
| --- | --- |
| `ordinary` | One primary donor may use the band's ordinary ceiling; secondary donors normally use the prior band's ordinary ceiling unless the doc 50 row explicitly routes that branch. |
| `optimizer` | One primary donor and one secondary donor may use the band's optimizer ceiling; additional donors use the prior band's ordinary or optimizer ceiling as justified by the row. |
| `grind_heavy` | Multiple donors may use the band's grind-heavy ceiling, but the row is marked as grind pressure and cannot serve as a floor assumption. |

If a P0 or P1 floor row cannot reach its intended basic pieces under the ordinary envelope, W6 must
consider the doc 61 fallback lever: adjust baseline JP gain only after cost and prerequisite tuning
are insufficient.

## Online Filter

A build piece is `online` in W4 only when all applicable gates pass.

| Gate | Requirement |
| --- | --- |
| Job unlock | The donor job is reachable through the doc 60 prerequisite tree by the row's band and progression mode. |
| Job depth | The donor job can plausibly reach the listed doc 60 minimum depth. |
| JP payable | The unit can plausibly pay the listed JP cost in the row's band and progression mode. |
| Slot legal | The unit has only one reaction, one support, and one movement slot. |
| Secondary legal | The row states which secondary action set is equipped; a unit cannot carry multiple secondaries. |
| Equipment practical-online | Equipment exports satisfy doc 59 practical-online timing. |
| Availability scope | Existing equipment and item availability may gate use; no new equipment, price, gil, or economy edits are allowed. |
| Placeholder fence | Placeholder rows for unfinished caster/performer/Necromancer R/S/M may consume band, slot, depth, and JP only; W4 must not treat unresolved names/effects as final mechanics. |

Rows that fail this filter can still appear in a `filtered_out` list, but they cannot count as
online incidence.

Two online inputs are T1-gated and provisional:

1. the JP accumulation envelope above;
2. doc 59 equipment availability bands.

Rows that depend on either input should be labeled `provisional_online_pending_T1` in doc 63 and its
JSON.

## Slot Model

W4 inherits doc 58's slot model:

```text
1 reaction + 1 support + 1 movement per unit
```

This is the primary protection against false convergence. A W4 row that puts `Dual Wield` and
`Doublehand` on the same unit, or `Equip Armor` and `Brawler` on the same unit, is illegal unless a
future accepted rule explicitly creates an exception. No such exception currently exists.

## Protected Convergence Rows

W4 must enumerate these doc 60 protected rows first:

| Piece | Donor | Slot | Incidence risk |
| --- | --- | --- | --- |
| `Auto-Potion` | Chemist | Reaction | Reliable sustain if too early or too broad. |
| `Equip Armor` | Knight | Support | Fragile jobs erase armor weakness. |
| `Equip Shield` | Knight | Support | Evasion and mitigation stacks approach immunity. |
| `Concentration` | Archer | Support | Accuracy becomes a universal patch. |
| `Brawler` | Monk | Support | Fists become the default physical route. |
| `Halve MP` | Mystic | Support | Caster economy convergence. |
| `Swiftspell` | Time Mage | Support | Caster action-economy convergence. |
| `Equip Guns` | Orator | Support | Stat-poor jobs get safe damage. |
| `Equip Polearms` | Dragoon | Support | Dragoon loses spear ownership. |
| `Equip Katana` | Samurai | Support | Samurai becomes a support stop. |
| `Doublehand` | Samurai | Support | Single-weapon engine crowds other physical supports. |
| `Dual Wield` | Ninja | Support | Two-hit engine becomes default physical route. |
| `Teleport` | Time Mage | Movement | Movement family becomes default. |
| `Move +3` | Ninja | Movement | Late movement default crowds other movement. |
| `Equip Knight Swords` | Vanguard | Support | Premium sword route revives sword dominance. |

Additional rows from docs 51, 56, 57, and 59 must be tracked as watch rows even when they are not in
the protected convergence list.

## Mandatory-Feeling Flag

The old JP Boost tax check is replaced by a broader false-choice detector.

A piece or package is `mandatory_feeling` in a band when it appears across most or all legal
optimizer rows for unrelated party families in that band.

W4 uses four flag levels:

| Flag | Meaning |
| --- | --- |
| `none` | Appears only in its intended identity or a narrow set of rows. |
| `watch` | Appears in several rows but still has clear competitors or identity limits. |
| `high` | Appears across most optimizer rows in a band or patches unrelated weaknesses too efficiently. |
| `fail` | Becomes effectively required for floor play, or appears as the obvious answer across unrelated optimizer rows before its intended band. |

`fail` requires a W6 adjustment candidate. `high` requires either a W5 quantitative row, a W6 watch
recommendation, or both.

## Party Row Coverage

W4 must extend doc 50 instead of inventing a separate party map.

| Party | Required bands | Main W4 read |
| --- | --- | --- |
| P0 naive/thematic | 0-E | Floor access, no hidden route, no mandatory R/S/M. |
| P1 balanced | 0-E | Baseline route and first-specialist health. |
| P5 optimizer rush | 0-E | Ceiling, fixed-JP grind-to-break, early full-package pressure. |
| P2 physical-heavy | B-E | Weapon-family, armor response, physical support convergence. |
| P3 caster-heavy | B-E | CT, MP, Faith, Shell/Reflect, caster economy convergence. |
| P4 control/sustain | C-E | Recovery, mitigation, status, long-fight pressure. |
| P6 performer parity | D-E | Bard/Dancer R/S/M parity and global support pressure. |

W4 does not need to populate every progression mode for every party row. Minimum required rows:

1. all P0 floor rows in `ordinary`;
2. all P1 baseline rows in `ordinary`;
3. all P5 rows in `ordinary`, `optimizer`, and `grind_heavy`;
4. P2/P3/P4/P6 wall-test rows in `optimizer`;
5. any row needed to test a protected convergence piece in its first online band.

## Immediate Row Set

The first W4 population pass must include at least these rows.

| ID | Source | Required row |
| --- | --- | --- |
| W4-01 | Doc 51 | P5 Band A/B ordinary, optimizer, grind-heavy fixed-JP progression rush. |
| W4-02 | Doc 51 | P5 Band B/C Knight body plus Monk sustain/damage, Chemist item range, Archer reliability, armor/shield pressure. |
| W4-03 | Doc 51 | P3 Band C/D caster economy: `Swiftspell`, `Halve MP`, `Manafont`, `Summon Focus`, MP pressure, CT pressure. |
| W4-04 | Doc 51 | P4 Band C/D mitigation stack: `Equip Armor`, `Equip Shield`, `Parry`, `Shirahadori`, `Mana Shield`, Protect/Shell. |
| W4-05 | Doc 51 | P2 Band D/E physical convergence: `Dual Wield`, `Doublehand`, `Attack Boost`, `Brawler`, `Concentration`, premium weapons. |
| W4-06 | Doc 51 | P6 Band D/E performer global route: Bard/Dancer R/S/M parity, performance support, interruption exposure. |
| W4-07 | Doc 51 | P1/P2/P3/P4 Band E late route: final Ramza, Vanguard, Necromancer, older specialists. |
| W4-08 | Doc 59 | Band C armor/shield export route. |
| W4-09 | Doc 59 | Band C bow export route. |
| W4-10 | Doc 59 | Band C/D gun export route. |
| W4-11 | Doc 59 | Band C/D polearm export route. |
| W4-12 | Doc 59 | Band D katana export route. |
| W4-13 | Doc 59 | Band D/E dual-wield and Ninja blade route. |
| W4-14 | Doc 59 | Band D/E Throw inventory route. |
| W4-15 | Doc 59 | Band E knight-sword route. |
| W4-16 | Doc 59 | Band C consumable route: `Throw Item`, `Item Lore`, `Auto-Potion`. |

## Additional Watch Rows

W4 must carry these watch rows forward even if they are not all fully quantified until W5.

| Watch | Source | Required treatment |
| --- | --- | --- |
| Ninja innate dual late | Doc 56 | Compare active Ninja's no-support two-hit value against learned `Dual Wield` donor pull. |
| Iaido zero-MP/zero-CT area | Doc 56 | Track area/support pressure around late Iaido, especially Bizen/Kiku/Ame/Masamune-adjacent rows. |
| Masamune CT support | Doc 56 | Confirm CT +8 does not aggregate into pseudo-Haste or upkeep compression. |
| Vanguard Decisive setup loop | Doc 57 | Confirm AI and player incentives reward the setup loop; otherwise mark trap-option risk for W6. |
| Ramza C4 breadth | Doc 50/57 | Test breadth-as-dominance separately from per-axis dominance. |

## Dual-Independent Ceiling Rule

Optimizer and ceiling rows that feed W5 must be built dual-independently.

Process:

1. GPT proposes the strongest legal stack for the band using this plan's online filter.
2. Claude independently proposes the strongest legal stack from the same accepted inputs.
3. The stronger legal stack wins and becomes the ceiling row for W5.
4. If either agent finds a stronger admissible stack, that stack replaces the weaker one.
5. Genuine disagreement on legality is resolved against doc 58 slot rules, doc 59 availability, doc
   60 prerequisite/JP rules, and the JP accumulation envelope above.

This applies especially to:

- P5 Band C mitigation/compression;
- P5 Band D physical/caster/mobility convergence;
- P5 Band E late replacement;
- P2 physical-heavy D/E;
- P3 caster-heavy C/D/E;
- P6 performer D/E.

## W5 Handoff Contract

W4 must produce W5-ready rows, but W4 itself is not the quantitative proof.

Each W4 row that creates a W5 row must specify:

- party row and band;
- active jobs and Ramza chapter state;
- online R/S/M and secondaries;
- equipment profile;
- primary expected damage route;
- sustain route;
- control route;
- mobility route;
- defense/safety route;
- risk-register mapping;
- quantitative rows required in W5.

W5 must use accepted doc 31 proxies:

- diversity/dominance is the majority/Pareto proxy across damage, sustain, control, mobility, and
  safety/risk;
- floor is P0 clearing the band's A5 representative set inside the non-optimized envelope;
- Belief/Oil/fire remains the primary quantified F5 dominance vector from doc 47;
- every F5 finding maps back to the 12-risk register.

## Ramza Dominance Contract

Ramza is present in every party and must be measured separately.

W4 must create rows that distinguish:

1. per-axis dominance: Ramza beating a protected specialist inside that specialist's lane;
2. breadth-as-dominance: Chapter 4 Ramza being good enough at everything that he crowds out
   specialists even when he is not individually best at one axis.

W5 must therefore include an explicit `Ramza C4 breadth-as-dominance` row. This is separate from
the doc 50 per-band Ramza checks.

## N1 Bundle Promotion Decision

Claude recommends promoting Vanguard and Ramza chapter rows into `work/sim-inputs-v0.2.1.json`
before W5 so real-roster F5 comparisons are bundle-rooted.

This plan accepts that recommendation as the preferred path.

N1 will be a separate pinned bundle slice before W5:

- add Vanguard as a real roster row;
- add Ramza chapter rows as real roster rows or a clearly documented chapter-state map;
- record the chapter-to-phase or chapter-to-band scalar map explicitly;
- keep the existing formula constants unchanged unless a separate dual-sim pass approves otherwise;
- run bundle validation/checker scripts;
- send the pinned bundle diff to Claude before commit.

If N1 promotion fails technically, W5 may proceed with a weaker fence:

```text
Vanguard/Ramza rows are doc-57-provisional-stat-rooted, not bundle-rooted.
```

That fallback must be labeled as weaker evidence in W5/W6.

## Dual-Sim Boundary

W4 incidence is structural. It needs source review, JSON validation, and consistency checks, but not
dual simulation.

W5 quantitative rows are different:

- damage, sustain, control magnitude, mobility magnitude, mitigation magnitude, and throughput rows
  must be recomputed independently;
- GPT writes the model/tooling;
- Claude recomputes from the bundle and schema only;
- a zero-mismatch gate is required before W5 quantitative claims are accepted.

The current damage bundle pin is:

```text
work/sim-inputs-v0.2.1.json
d57c4688b2c1f656ad0094cdfc47564dec87f62b671c845b619aecb5ae6a8c95
```

If N1 changes the bundle, W5 must use the new pinned bundle hash.

## W6 Lever Set

W6 can recommend only bounded adjustments.

Allowed levers:

- raise JP cost;
- delay prerequisite depth;
- narrow skill scope;
- delay availability timing of existing equipment;
- cut a dangerous export;
- mark safe/no-action.

Not allowed in W6 without a separate approved artifact:

- Gil, price, reward, sell-value, or economy edits;
- new equipment;
- baseline stat rewrites;
- formula constant changes;
- unreviewed new skill effects.

Every W6 recommendation must cite the exact W4/W5 row that triggered it and must respect doc 61's
pacing posture: JP costs and prerequisite depth are primary, baseline JP gain is fallback.

## Overclaim Guard

W4/W5/W6 do not prove final campaign balance.

W4 proves structural incidence only. W5 quantifies dominance and pressure on accepted rows. W6
recommends adjustments based on those rows. Final implementation acceptance still requires the
future implementation data path, bundle updates, and any required dual-sim checks.

Accepted wording:

```text
This row is safe within the current provisional evidence.
```

Rejected wording:

```text
This row proves the final mod is balanced.
```

## Proposed W4 Outputs

`docs/job-balance/63-w4-t21-populated-incidence-v0.md` should contain:

- methodology recap;
- source pin table;
- online filter;
- protected convergence incidence table;
- party-row incidence matrix;
- mandatory-feeling summary;
- filtered-out deep-power rows;
- W5 handoff rows;
- Claude review verdict.

`work/gpt-w4-t21-populated-incidence-v0.json` should contain:

- source pins;
- band envelopes;
- incidence rows;
- protected convergence rows;
- mandatory-feeling flags;
- filtered-out rows;
- W5 handoff rows.

## Claude Review Request

Claude should review whether:

- this plan fully preserves doc 50 party rows and A5 anchors;
- the online filter is strict enough to prevent illegal slot/support stacking;
- progression modes correctly replace the old JP Boost split;
- protected convergence and watch rows are sufficient;
- N1 bundle promotion should remain the preferred W5 path;
- W5 and W6 overclaim boundaries are tight enough;
- this plan is sufficient to start drafting doc 63 and its JSON.
