# W5 F5 Real-Roster Sweep Schema V0

Status: Accepted
Date: 2026-06-22
Depends on:
- `docs/job-balance/31-campaign-gameplay-validation-v1.md`
- `docs/job-balance/50-campaign-journey-bundle-v0.md`
- `docs/job-balance/63-w4-t21-populated-incidence-v0.md`
- `docs/job-balance/64-n1-real-roster-sim-bundle-v0.md`
- `work/sim-inputs-v0.2.1.json`
- `work/sim-inputs-n1-real-roster-v0.json`
- `work/w5_real_roster_sweep.py`
- `work/w5-real-roster-sweep-v0.json`

## Purpose

This document locks the W5/F5 output contract before W5 results are accepted.

The sweep tests real party/build rows, not isolated anchors. It uses W2 party rows, W4 handoff rows,
N1 Ramza/Vanguard profiles, and accepted W3 concrete-provisional rows.

## Binding Inputs

| Input | Binding |
| --- | --- |
| Formula core | `work/sim-inputs-v0.2.1.json` at `d57c4688b2c1f656ad0094cdfc47564dec87f62b671c845b619aecb5ae6a8c95` |
| Real roster extension | `work/sim-inputs-n1-real-roster-v0.json` at `7e07416a4d82789249278994f0e00fdbf3ba9ef35915f422295191146ece5d7a` |
| W4 handoff | `work/gpt-w4-t21-populated-incidence-v0.json` |
| Output schema | `w5_f5_real_roster_sweep_v0` |

`work/sim-inputs-v0.2.1.json` and N1 must remain untouched by the W5 commit.

## Output Schema

Top-level fields:

- `schema_version`
- `artifact`
- `status`
- `source_parent_bundle_sha256`
- `source_n1_extension_status`
- `review_tolerance`
- `effwp_rounding`
- `verdict_policy`
- `method`
- `rows`
- `ceiling_probe_rows`
- `summary`

Each `rows[]` entry must contain:

- `id`
- `band`
- `phase`
- `armor_mix_id`
- `armor_mix`
- `risk_register`
- `required_axes`
- `units`
- `floor_envelope`
- `proxy_axis_provenance`
- `totals`
- `top_source`
- `floor_proxy_per_weak_family`
- `weakest_combat_ratio_to_sword`
- `belief_oil_named_risk`
- `dominance_flags`
- `verdict`

Each `units[]` entry must contain:

- `name`
- `job_or_profile`
- `role`
- `primary_family`
- `action_id`
- `derivation`
- `axis_provenance`
- `engine`
- `damage`
- `area_damage`
- `sustain`
- `control`
- `mobility`
- `safety`
- `note`

## Review Tolerance

| Metric | Tolerance |
| --- | --- |
| Deterministic damage rows | Exact integer match |
| Weighted averages | `0.001` |
| Ratios | `0.001` |
| Rounding policy | `effwp_rounding = none`, inherited from parent `d57c4688` |

The output JSON must repeat the tolerance block and `effwp_rounding = none` so Claude's independent
checker can bind to the artifact itself, not only to this prose document.

## Reproducibility Conventions

The JSON `method` block must expose the following machine-checkable conventions:

- `damage_conventions`;
- `ramza_chapter_phase_map`;
- `magic_area_constants`.

### Physical Damage Convention

Physical weapon values are recomputed per armor class before any armor-mix weighting:

```text
raw_pressure = routine(attacker stats, family WP, phase scalar) * pressure/support multiplier
per_armor_damage = floor(raw_pressure * engine_multiplier * response_layers[armor])
delivered_per_action = hit_count * hit_rate * sum(armor_mix[armor] * per_armor_damage)
```

The floor happens before the armor-mix weighted average. Doublehand, Jump, Attack Boost probes, and
similar pressure changes must be inside the per-armor floor. Multi-hit engines apply `hit_count`
after the per-armor floor and armor-mix weighting. Imported accepted tables must already obey this
convention.

### Ramza Chapter Phase Map

N1 Ramza profiles are chapter-fixed, not row-band-fixed:

| Profile | Phase scalar |
| --- | --- |
| `Ramza Chapter 1` | `early` |
| `Ramza Chapter 2` | `mid` |
| `Ramza Chapter 3` | `mid` |
| `Ramza Chapter 4` | `late` |

Band D rows that use `Ramza Chapter 3` therefore still use the C3 mid profile. Band E rows that use
`Ramza Chapter 4` use the late profile.

### Action Identity And Derivation

Every unit must carry `action_id` and `derivation`.

This is mandatory when `primary_family` is a weapon label but the modeled value is a skill, setup,
or hybrid action. Examples:

- Squire starter utility: `Fundaments:Dash`, fixed 18, not a normal flail hit;
- Geomancer Oil: `Geomancy:Magma Surge/Oil setup`, not a normal axe hit;
- Ramza hybrid rows: sword family plus Spellblade, Arc Blade, or Ultima proxy;
- Archer utility rows: bow family plus aimed, pinning, or piercing action.

`derivation` must name the source file or accepted row, the formula or lookup rule, and any constants
needed to recompute the value.

### Magic And Area Constants

Magic and area rows must not appear as unexplained constants. The output JSON must pin the exact
`ma`, `k`, rounding rule, per-target value, expected target count, and total for caster rows used in
W5.

Current W5 spell constants use:

```text
single-target magic neutral = round(ma * k * 0.6)
area magic per_target = round(ma * k * 0.6)
area normalized_total = per_target * expected_targets
```

The result JSON's `method.magic_area_constants` is the binding table for Black Magic, White Magic,
Summon, Meteor, and Ramza's chapter 4 Ultima proxy in this sweep.

### Proxy Axis Provenance

Non-damage axes are proxies. W5 must make that explicit instead of pretending they are exact game
formula outputs.

Each row must include `proxy_axis_provenance` for `sustain`, `control`, `mobility`, and `safety`.
Each non-zero contribution must cite the accepted concrete document or JSON row that justifies the
proxy. This is especially binding for `W5-P5-D-CONV`, whose fail call depends on `339` sustain and
`265` safety-defense.

## Required Axes

Every W5 row reports:

- damage;
- sustain;
- control;
- mobility;
- safety-defense;
- dominance risk;
- floor proxy per weak weapon family;
- named `Belief/Oil` risk;
- doc63 risk-register tags.

## Normalization Contracts

W5 uses the same normalization doctrine as `docs/formula-balance/11-validated-policy-v0.2.md`.

### Damage Engine Normalization

Weapon-family floor and ceiling comparisons are per-action delivered-output comparisons, not raw
per-hit comparisons.

Each damage-capable `units[]` entry must report:

```text
damage.per_hit
damage.hit_count
damage.engine_multiplier
damage.hit_rate
damage.action_multiplier
damage.delivered_per_action = per_hit * hit_count * engine_multiplier
```

This is a display identity for W5 unit rows, not a replacement for the per-armor physical formula
above. Reviewers must recompute physical weapon values from the per-armor formula when validating
floored weapon rows.

Interpretation rules:

- ordinary single-hit actions use `hit_count = 1` and `engine_multiplier = 1.0`;
- innate or learned dual-wield/two-hit engines use `hit_count = 2` and
  `engine_multiplier = 1.0`;
- Doublehand uses `hit_count = 1` and `engine_multiplier = 1.8`;
- action-modeled rows such as Guarded Strike, Pinning Shot, Piercing Shot, Spellblade/Ward, or
  explicit accuracy rows must expose `action_multiplier` and/or `hit_rate` in `damage` even when the
  accepted source table already has the factor baked into `delivered_per_action`;
- Attack Boost rows use their multiplier only inside `STRESS-PROBE` rows and never become canon
  ceilings in this schema;
- volatile-family self-viability may credit its own expected or maximum value, but it does not
  raise the sword baseline for unrelated families.

### Sword Baseline And Armor Mix Binding

`weakest_combat_ratio_to_sword` is bound to the W5 row's own `armor_mix`.

The object must contain:

```text
threshold
threshold_status
family
family_delivered_per_action_by_armor
sword_baseline_family
sword_baseline_phase_scalar
sword_delivered_per_action_by_armor
aggregation
ratio
```

The baseline is parent-bundle `family = sword`, same phase/scalar as the W5 row, no stress support,
and the same armor mix. The aggregation is `worst_case_across_row_armor_mix`, not average. This
keeps a family from passing the floor because it is healthy into one armor class while dead into
another armor class inside the same representative row.

The provisional review threshold is:

```text
weakest_combat_ratio_to_sword.ratio >= 0.55
threshold_status = "provisional_doc11_family_viability_lens"
```

This threshold is tied to doc 11's hit-count/engine-normalized family-viability lens and may be
revisited if F5 shows an identity-healthy family below it.

### Area Damage Normalization

`area_damage` must be an object, not a scalar:

```text
area_damage.per_target
area_damage.assumed_targets
area_damage.normalized_total = per_target * assumed_targets
area_damage.target_count_basis
```

This is required for `W5-P3-C-BELIEF-OIL`, `W5-P6-DE-PARITY`, and any summon, Iaido, performer,
global, or area HP row. A per-target-safe action can still fail dominance if its realistic target
count makes the normalized total dominate.

## Dominance And Verdict Policy

W5 inherits doc 31's majority/Pareto predicate.

The evaluated axes are:

```text
damage, sustain, control, mobility, safety
```

For the dominance `damage` axis, single-target engines use `damage.delivered_per_action`; area
engines use the realistic `area_damage.normalized_total`; a hybrid unit uses
`max(damage.delivered_per_action, area_damage.normalized_total)`.

`top_source` and `top_source.share` use that same per-unit primary damage axis. W5 must not add two
alternative actions from the same unit together when identifying the largest damage source.

A unit or build is dominant when it is best or tied in at least three of those five axes and is not
worst in any of the five axes. It is not required to be strictly best in all axes, and being best in
only one axis is not dominance.

`dominance_flags` must report:

- `best_or_tied_axes`;
- `worst_axes`;
- `majority_pareto_dominant`;
- `stress_probe_involved`;
- `assumption_gated_involved`.

`verdict` must use:

| Verdict | Meaning |
| --- | --- |
| `pass` | No majority/Pareto dominance, floor envelope holds where applicable, and named risk vectors stay below their bound. |
| `watch` | No immediate fail, but a high-value axis, stress probe, assumption-gated payoff, or identity floor needs W6 attention. |
| `fail` | Majority/Pareto dominance, floor failure, or a named quantified risk breach. |

The `summary` block must segregate canon rows, `STRESS-PROBE` rows, and
`AI-BEHAVIOR-ASSUMPTION` rows. Stress-probe and assumption-gated rows feed W6 candidates but never
become binding canon ceilings by themselves.

## Ceiling Probe Rules

P2-E and P5-E must report both candidate families before choosing a ceiling:

- Ninja innate dual without Attack Boost;
- Ninja innate dual plus Attack Boost as `STRESS-PROBE`;
- Samurai Doublehand katana;
- Vanguard `Decisive Strike` setup only where relevant, tagged `AI-BEHAVIOR-ASSUMPTION`.

Attack Boost is still an unassigned doc58 probe. Any result that depends on Attack Boost is not a
canon ceiling until a later artifact assigns it.

Vanguard `Decisive Strike` setup payoff is assumption-gated because N3 AI/reward routing is not
proven.

## Weak-Family Floor Rule

The sweep reports all weak-family proxies, but `instrument` is raw-damage exempt. Bard value is
measured through performance utility, not raw damage.

A family can still become a W6 lever candidate even if it passes this threshold, especially if it
stagnates across bands or lacks a tactical identity.

## Floor Envelope

`W5-FLOOR-0A` and `W5-FLOOR-B` test floor viability, not optimizer-relative damage.

Those rows must include `floor_envelope`:

| Field | Required value |
| --- | --- |
| `party_model` | `P0_naive_thematic` |
| `routing` | `ordinary_non_optimized` |
| `jp_boost` | `removed_not_available` |
| `guide_routing` | `false` |
| `optimized_rsm` | `false` |
| `deep_secondaries_on_shallow_chassis` | `false` |
| `equipment_policy` | existing equipment only, no gil/economy assumptions |

Band-specific floor bounds:

| Row | Bands | Anchors | Rough level | Ordinary donor JP | Allowed active jobs and pieces |
| --- | --- | --- | --- | ---: | --- |
| `W5-FLOOR-0A` | 0/A | `GCV-PIN-00-RAW-MIXED`, `GCV-PIN-A-PHYSICAL` | 1-7 | 0: `0-80`; A: `150-250` | Ramza/Squire, Squires, Chemist, trainees; starter attacks, basic Items, starter utility, `Move +1` optional in A only. |
| `W5-FLOOR-B` | B | `GCV-PIN-B-FIRST-SPECIALIST` | 8-15 | B: `350-650` | Ramza flexible, Knight or Archer, White or Black Mage, Chemist, Squire/first specialist; active specialist actions before premium exports. |

Floor passes only if the P0 naive/thematic party can clear the representative row inside this
envelope without optimizer-tier R/S/M, excessive grind, or a specific hidden route.

## Belief/Oil Quantification

`belief_oil_named_risk` must be an object when a Belief/Oil-capable caster is present:

```text
belief_oil_named_risk.present
belief_oil_named_risk.fire_multiplier
belief_oil_named_risk.resulting_total
belief_oil_named_risk.vs_baseline
belief_oil_named_risk.source_chain
```

When absent, the object still appears with `present = false`.

The quantified doc 47 reference is:

| Row | Neutral 3-target total | Weak/Oil x2 total | Belief x Weak/Oil total | Ratio vs 415 |
| --- | ---: | ---: | ---: | ---: |
| `Ifrit` | 243 | 486 | 558 | 1.345 |
| `Salamander` | 297 | 594 | 681 | 1.641 |

The source chain is Mystic `Belief`, Geomancer `Magma Surge` or equivalent Oil setup, and Summoner
fire area payoff. A bare boolean is insufficient for W5.

## Planned W5 Rows

The accepted doc63 W5 handoff rows are preserved:

- `W5-FLOOR-0A`
- `W5-FLOOR-B`
- `W5-P5-B-FULL`
- `W5-P5-C-MIT`
- `W5-P3-C-BELIEF-OIL`
- `W5-P3-D-CASTER`
- `W5-P2-D-PHYS`
- `W5-P5-D-CONV`
- `W5-P6-DE-PARITY`
- `W5-P5-E-LATE`
- `W5-RAMZA-C4-BREADTH`
- `W5-EQUIP-BREAKPOINTS`

## Claude Review Request

Claude should approve or revise:

- the output schema;
- the review tolerances;
- the weak-family threshold and `instrument` exemption;
- the ceiling probe labels;
- whether the W5 result doc can bind to this schema after review.
