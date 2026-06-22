# W5 F5 Real-Roster Sweep V0

Status: Accepted
Date: 2026-06-22
Depends on:
- `docs/job-balance/50-campaign-journey-bundle-v0.md`
- `docs/job-balance/57-vanguard-ramza-concrete-v0.md`
- `docs/job-balance/63-w4-t21-populated-incidence-v0.md`
- `docs/job-balance/64-n1-real-roster-sim-bundle-v0.md`
- `docs/job-balance/65-w5-f5-real-roster-sweep-schema-v0.md`
- `work/sim-inputs-v0.2.1.json`
- `work/sim-inputs-n1-real-roster-v0.json`
- `work/w5_real_roster_sweep.py`
- `work/w5-real-roster-sweep-v0.json`

## Purpose

W5/F5 tests real party/build rows instead of isolated formula anchors.

It consumes W2 party rows, W4 handoff rows, N1 Ramza/Vanguard profiles, and accepted W3 concrete
proxy rows. It reports damage, sustain, control, mobility, safety-defense, dominance risk,
weak-family floor, and named `Belief/Oil` risk under the locked doc65 contract.

## Source Pins

| Source | SHA-256 |
| --- | --- |
| `docs/job-balance/65-w5-f5-real-roster-sweep-schema-v0.md` | `dc91fcdf8b29d077fd384825d2c32c782cfaa962a9c1adc236914ae199d80c30` |
| `work/sim-inputs-v0.2.1.json` | `d57c4688b2c1f656ad0094cdfc47564dec87f62b671c845b619aecb5ae6a8c95` |
| `work/sim-inputs-n1-real-roster-v0.json` | `7e07416a4d82789249278994f0e00fdbf3ba9ef35915f422295191146ece5d7a` |
| `work/w5_real_roster_sweep.py` | `341f0fde00108b1dc7f7460c0ff108e61d943fb5d4715e52604e2981e5c470e3` |
| `work/w5-real-roster-sweep-v0.json` | `51816babc2ceaa62bc4cde7b4b90a8f8a7167bd7c922f5db025a39d75a678d4e` |

## Method

The sweep uses accepted W3 proxy rows where they exist and falls back to `tools/sim_damage.py` only
for ordinary weapon-family attacks. Non-damage axes are proxy scores from accepted concrete rows:
healing HP, mitigation/prevention, tempo/control pressure, mobility, and safety-defense.

The dominance `damage` axis follows doc65 R6:

```text
single-target engines -> damage.delivered_per_action
area engines -> area_damage.normalized_total
hybrid units -> max(damage.delivered_per_action, area_damage.normalized_total)
```

The physical damage convention is per-armor first, armor-mix second:

```text
per_armor_damage = floor(raw_pressure * engine_multiplier * response_layers[armor])
delivered_per_action = hit_count * hit_rate * sum(armor_mix[armor] * per_armor_damage)
```

This keeps Samurai Doublehand, Ninja dual-wield, Jump, and other physical engines independently
recomputable from the accepted tables. W5 does not average raw pressure before floor; it also does
not apply `hit_count` before the per-armor floor.

Ramza profiles are chapter-fixed:

| Ramza profile | W5 phase scalar |
| --- | --- |
| `Ramza Chapter 1` | `early` |
| `Ramza Chapter 2` | `mid` |
| `Ramza Chapter 3` | `mid` |
| `Ramza Chapter 4` | `late` |

So Band D rows using C3 Ramza still use the C3 mid profile, while C4 rows use the late profile.

Limitations:

- no final T1 weapon dump;
- no ENTD map geometry;
- no exact enemy AI proof;
- Vanguard `Decisive Strike` setup payoff is an assumption, not proven AI routing;
- Ramza defense class is a W5 proxy, not final equipment data.

## Magic And Area Constants

The caster rows in this sweep are recomputable from accepted concrete JSON rows. W5 uses:

```text
single-target magic neutral = round(ma * k * 0.6)
area magic per_target = round(ma * k * 0.6)
area normalized_total = per_target * expected_targets
```

| Action | Source row | Constants | W5 value |
| --- | --- | --- | ---: |
| `Black Magic:Fira` mid | `work/gpt-wm-bm-concrete-v0.json` `black_phase_rows[action=Fira,phase=mid]` | `ma=12`, `k=18` | `130` |
| `Black Magic:Firaga` mid | `work/gpt-wm-bm-concrete-v0.json` `black_phase_rows[action=Firaga,phase=mid]` | `ma=12`, `k=21` | `151` |
| `Black Magic:Flare` late | `work/gpt-wm-bm-concrete-v0.json` `black_phase_rows[action=Flare,phase=late]` | `ma=15`, `k=30` | `270` |
| `Summon:Titan` mid | `work/gpt-summoner-geomancer-concrete-v0.json` `summon_phase_rows[skill=Titan,phase=mid]` | `ma=10`, `k=10`, targets `2.1` | `126` |
| `Summon:Ifrit` mid | `work/gpt-summoner-geomancer-concrete-v0.json` `summon_phase_rows[skill=Ifrit,phase=mid]` | `ma=10`, `k=9`, targets `2` | `108` |
| `Summon:Bahamut` late | `work/gpt-summoner-geomancer-concrete-v0.json` `summon_phase_rows[skill=Bahamut,phase=late]` | `ma=12`, `k=15`, targets `2.4` | `259.2` |
| `Time Magic:Meteor` late | `work/gpt-time-mystic-concrete-v0.json` `time_meteor_phase_rows[skill=Meteor,phase=late]` | `ma=13`, `k=14`, targets `1.8` | `196.2` |
| `Ramza:Ultima proxy` late | `work/gpt-vanguard-ramza-concrete-v0.json` `ramza_magic_rows_faith_70_70.chapter_4_ultima_K22_late` | accepted K22 proxy | `158` |

The JSON repeats these in `method.magic_area_constants`, and each unit has `action_id` plus
`derivation` so a reviewer can distinguish normal weapon hits from Dash, Geomancy/Oil, Jump,
Spellblade, Arc Blade, summons, and other modeled actions.

## Scorecard

| Result | Count | Rows |
| --- | ---: | --- |
| Pass | 8 | `W5-FLOOR-0A`, `W5-FLOOR-B`, `W5-P5-B-FULL`, `W5-P5-C-MIT`, `W5-P3-D-CASTER`, `W5-P2-D-PHYS`, `W5-P6-DE-PARITY`, `W5-EQUIP-BREAKPOINTS` |
| Watch | 2 | `W5-P5-E-LATE`, `W5-RAMZA-C4-BREADTH` |
| Fail | 2 | `W5-P3-C-BELIEF-OIL`, `W5-P5-D-CONV` |

No row triggers majority/Pareto dominance under doc65. Both failures are named-risk failures. Both
watch rows are Ramza breadth boundary watches, not dominance failures.

## Fail Rows

| Row | Flags | Evidence | W6 direction |
| --- | --- | --- | --- |
| `W5-P3-C-BELIEF-OIL` | `belief_oil_dominance` | The row itself is not majority/Pareto dominant, but the accepted Belief/Oil/fire vector reaches `681` against the `415` top physical reference, ratio `1.641`. | Narrow the compound vector. First levers should target Oil/fire/area payoff, not all summons, all Faith, or all magic. |
| `W5-P5-D-CONV` | `belief_oil_dominance`, `sustain_safety_compression` | The same `681 / 415 = 1.641` vector remains live inside a stronger D convergence party. The row also carries `339` sustain and `265` safety-defense. | Narrow Belief/Oil and test whether performer/White sustain plus durable bodies needs pacing, scope, interruption, or JP friction. |

## Proxy Provenance For `W5-P5-D-CONV`

The second fail is not a hidden exact formula claim. Its sustain and safety-defense values are W5
proxy scores with explicit accepted-source backing in the JSON.

Sustain:

| Unit | Value | Source |
| --- | ---: | --- |
| Ramza C3 | `35` | `docs/job-balance/57-vanguard-ramza-concrete-v0.md`, Ramza Chapter 3 hybrid bridge row. |
| Performer | `304` | `work/gpt-bard-dancer-concrete-v0.json`, `simulations.bard_hp_over_time[target_max_hp=390].seraph_cap4_full`. |
| Total | `339` | Sum of above. |

Safety-defense:

| Unit | Value | Source |
| --- | ---: | --- |
| Ramza C3 | `60` | `docs/job-balance/57-vanguard-ramza-concrete-v0.md`, hybrid safety below frontliners. |
| Ninja/Samurai | `45` | `docs/job-balance/56-samurai-ninja-concrete-v0.md`, fast but fragile active physical identity. |
| Time/Summoner | `30` | `docs/job-balance/43-summoner-geomancer-concrete-v0.md`, fragile area caster context. |
| Performer | `40` | `docs/job-balance/46-bard-dancer-concrete-v0.md`, interruptible performance model. |
| Dragoon/Archer flex | `90` | `docs/job-balance/39-timed-untargetability-composition-schema.md`, Jump exposure window. |
| Total | `265` | Sum of above. |

Control and mobility provenance are also present per unit under
`rows[].proxy_axis_provenance.control` and `rows[].proxy_axis_provenance.mobility`.

## Watch Rows

| Row | Flags | Evidence | W6 direction |
| --- | --- | --- | --- |
| `W5-P5-E-LATE` | `ramza_breadth_watch` | Final Ramza is useful inside the late party, but the top damage source is the Summoner flex at `259.2`, share `0.284`. The row does not prove Ramza dominance. | Keep Ramza broad, but avoid giving him exportable R/S/M or native premium equipment that turns breadth into replacement pressure. |
| `W5-RAMZA-C4-BREADTH` | `ramza_breadth_watch` | In the direct breadth row, Black Mage leads the damage axis at `270`, share `0.246`, while Vanguard leads protection/control and Ninja leads mobility. | No immediate Ramza nerf. Preserve the boundary that protected specialists still lead their lanes. |

## Pass Reads

- Floor rows pass inside the explicit P0 naive/thematic, non-optimized envelope. They do not require
  early support taxes, guide routing, JP Boost, or equipment exports.
- Band B full-package pressure is real but does not produce majority/Pareto dominance.
- Band C mitigation/control is strong but does not become practical immunity in this proxy.
- D physical rows preserve rational lanes for Ninja, Samurai, Dragoon, and Archer.
- Performer parity passes with area/global value normalized by target count. Bard/Dancer still need
  interruption and action-cost proof in later rows, but W5 does not show mandatory performer
  infrastructure.
- Final Ramza breadth does not fail under the R6 damage-axis rule, but remains a watch because Ramza
  is always present and can accumulate broad value if later R/S/M or equipment expands too far.
- Equipment breakpoints pass as long as illegal support stacks stay illegal and `Equip Knight
  Swords` remains optional/cuttable.

Rows involving Vanguard `Decisive Strike` setup are assumption-gated and segregated in the JSON.
Attack Boost rows are stress probes only and are excluded from canon ceilings.

## Weak-Family Floor

The combat weak-family proxy passes in every row after separating `instrument` from raw-damage
checks. `instrument` remains tracked, but it is raw-damage exempt because Bard value is measured
through performance utility.

| Metric | Value |
| --- | ---: |
| Lowest combat weak-family ratio to sword reference | `0.600` |
| Highest weakest-combat-family ratio across rows | `0.625` |
| Weakest combat families | `flail` in early/mid rows; `axe` in late rows |
| Raw-damage exempt family | `instrument` |

This is a good W5 signal for the mod goal: weaker weapon families are not matching swords one for
one, but they stay above the provisional floor where job identity, range, terrain, control, or
support texture can justify them.

## Ceiling Probe Summary

| Context | Highest numeric | Canon/assumption ceiling read |
| --- | --- | --- |
| `P2-E` | Ninja native dual + Attack Boost stress probe, `395.1` | Samurai Doublehand katana, `255.5`; Attack Boost remains non-canon. |
| `P5-E` | Ninja native dual + Attack Boost stress probe, `346.5` | Samurai Doublehand katana, `224.05`; Vanguard Decisive setup is assumption-gated at `190.15`. |

The W5 result therefore does not canonize Attack Boost and does not use Vanguard setup as a binding
ceiling.

## W6 Handoff

W6 should convert this sweep into concrete adjustments. Current W6 candidates:

1. `Belief/Oil/fire` compound payoff.
2. D convergence sustain plus safety-defense compression.
3. Preserve Ramza C4 breadth boundaries without adding exportable R/S/M or native premium equipment.
4. Preserve weak-family floor; do not globally nerf weak weapon families.
5. Preserve no-new-equipment and no-Gil-change scope.

## Claude Review Request

Claude should run the independent checker against:

- doc65 hash `dc91fcdf8b29d077fd384825d2c32c782cfaa962a9c1adc236914ae199d80c30`;
- parent bundle hash `d57c4688b2c1f656ad0094cdfc47564dec87f62b671c845b619aecb5ae6a8c95`;
- N1 extension hash `7e07416a4d82789249278994f0e00fdbf3ba9ef35915f422295191146ece5d7a`;
- tool hash `341f0fde00108b1dc7f7460c0ff108e61d943fb5d4715e52604e2981e5c470e3`;
- result JSON hash `51816babc2ceaa62bc4cde7b4b90a8f8a7167bd7c922f5db025a39d75a678d4e`.

The requested decision is whether the W5 numbers and verdict calls are accepted as the input to W6.
