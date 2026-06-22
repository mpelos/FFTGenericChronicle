# N1 Real-Roster Simulation Extension V0

Status: Accepted by Claude review
Date: 2026-06-22
Depends on:
- `docs/job-balance/57-vanguard-ramza-concrete-v0.md`
- `docs/job-balance/62-w4-t21-populated-incidence-plan-v0.md`
- `docs/job-balance/63-w4-t21-populated-incidence-v0.md`
- `work/sim-inputs-v0.2.1.json`
- `work/gpt-vanguard-ramza-concrete-v0.json`
- `work/sim-inputs-n1-real-roster-v0.json`

## Purpose

N1 is an additive real-roster extension for W5/F5.

It does not replace or mutate `work/sim-inputs-v0.2.1.json`. The parent formula bundle remains the
validated source for formula constants, armor response, weapon families, magic, RSM constants, and
stress engines. N1 only gives W5 explicit profiles for Ramza by chapter and Vanguard as a late
replacement profile.

## Source Pins

| Source | SHA-256 |
| --- | --- |
| `work/sim-inputs-v0.2.1.json` | `d57c4688b2c1f656ad0094cdfc47564dec87f62b671c845b619aecb5ae6a8c95` |
| `work/gpt-vanguard-ramza-concrete-v0.json` | `e04fc2f98c4b215fc32aea4303b01dc56454ffa72db71cec137a62fd356aeb8c` |
| `work/sim-inputs-n1-real-roster-v0.json` | `7e07416a4d82789249278994f0e00fdbf3ba9ef35915f422295191146ece5d7a` |

## Schema

`work/sim-inputs-n1-real-roster-v0.json` uses schema
`n1_real_roster_extension_v0`.

Required top-level fields:

- `source_formula_bundle`
- `parent_bundle_sha256`
- `scope_boundaries`
- `profile_policy`
- `real_roster_profiles`
- `rsm_boundary_values_from_doc57`
- `w5_required_outputs`

Forbidden in N1:

- formula constants;
- copied weapon-family data;
- copied armor-response data;
- `rsm_constants`;
- `stress_engines`;
- `jp_boost_multiplier`;
- Gil or economy fields;
- new equipment records.

## Roster Profiles

| Profile | Online bands | Stats source | Defense/equipment read | W5 use |
| --- | --- | --- | --- | --- |
| `Ramza Chapter 1` | 0/A | doc57 `chapter_1` | Leather-class simulation proxy only. | Starter Ramza floor and Squire/Chemist comparison. |
| `Ramza Chapter 2` | B | doc57 `chapter_2` | Leather-class simulation proxy only. | First-specialist comparison versus Knight, Archer, White Mage, and Black Mage. |
| `Ramza Chapter 3` | C/D | doc57 `chapter_3_mid` | Leather primary proxy; mail stress proxy for W5 equipment breakpoint checks. | Hybrid bridge comparison versus mid/advanced specialists. |
| `Ramza Chapter 4` | E | doc57 `chapter_4` plus `chapter_4_stress` | Leather primary proxy; mail stress proxy for W5 equipment breakpoint checks. | Final breadth-as-dominance checks. |
| `Vanguard` | E | doc57 `Vanguard` late row | Plate profile; existing equipment only. | Late replacement, protection, setup, and Decisive loop checks. |

Ramza profiles use explicit chapter rows. W5 must not infer Ramza through a generic job growth
curve. Vanguard is E-only, has native `sword`, `spear`, `axe`, and `fists`, and has no native
`knight_sword`. `knight_sword` access remains only through the separate `Equip Knight Swords`
export pressure.

## W5 Carry-Forward

W5/F5 must report:

- damage;
- sustain;
- control;
- mobility;
- safety-defense;
- dominance risk;
- floor proxy per weak weapon family;
- named `Belief/Oil` dominance risk column;
- doc63 W5 handoff risk-register tags.

Vanguard `Decisive Strike` setup payoff remains an N3 AI-behavior assumption for W5. It may be
modeled as an assumption, but not treated as proven AI routing.

## Claude Review Request

Claude should verify:

- `work/sim-inputs-v0.2.1.json` still hashes to the pinned parent hash;
- N1 rows match doc57 values for Ramza chapter stats, Vanguard stats, actions, exposure mark, and
  RSM boundary values;
- N1 contains no copied formula constants, RSM constants, stress engines, Gil, or new equipment;
- the W5 carry-forward axes include weak-family floor proxy and named `Belief/Oil` risk.
