# Validated Policy V0.2

Status: First validated formula-policy candidate
Date: 2026-06-20
Result class: Conceptually viable, pending verified-baseline re-sim
Depends on:
- `docs/formula-balance/05-formula-proposal-protocol.md`
- `docs/formula-balance/08-scenario-set-v0.md`
- `docs/formula-balance/10-mitigation-and-scaling-policy-v0.md`
- `work/sim-inputs-v0.2.json`
- `work/gpt-sim-v0.2-results.json`

## Purpose

This milestone records the first formula-policy candidate that passed the shared simulation gate.

It is not final implementation data. Weapon WP values are still WotL-fallback / design
provisional because `work/baseline_weapons.csv` is empty. The result must be re-simulated after
the Windows `04-proof-and-baseline-plan.md` session captures real IVC weapon data.

## Locked Combat Model

V0.2 uses the accepted C-bounded mitigation policy:

- no general subtractive point-DR armor model;
- coarse percent/type response for physical armor matchups;
- Marcelo's full-plate rule: full plate reduces both `swing` and `thrust`; its physical weakness
  is `crush`;
- four physical damage types: `swing`, `thrust`, `crush`, `missile`;
- magic/spirit damage lives on a separate response axis using Faith, Shell, element, status, and
  equipment response rather than physical armor class.

## V0.2 Parameter Set

Source artifact:

```text
work/sim-inputs-v0.2.json
```

### Family Parameters

| Family | Routine | Type | WP | Penetration |
| --- | --- | --- | ---: | ---: |
| `sword` | `pa_wp` | `swing` | 16 | 0.0 |
| `knight_sword` | `br_pa_wp` | `swing` | 20 | 0.0 |
| `katana` | `br_pa_wp` | `swing` | 18 | 0.0 |
| `knife` | `spd_pa_wp` | `thrust` | 12 | 0.1 |
| `ninja_blade` | `spd_pa_wp` | `swing` | 13 | 0.0 |
| `longbow` | `spd_pa_wp` | `missile` | 15 | 0.15 |
| `crossbow` | `pa_wp` | `missile` | 14 | 0.35 |
| `gun` | `wp_wp` | `missile` | 12 | 0.7 |
| `spear` | `pa_wp` | `thrust` | 15 | 0.1 |
| `staff` | `ma_wp` | `crush` | 14 | 0.0 |
| `rod` | `ma_wp` | `crush` | 10 | 0.0 |
| `pole` | `ma_wp` | `crush` | 13 | 0.0 |
| `axe` | `rdm_pa_wp` | `crush` | 20 | 0.0 |
| `flail` | `rdm_pa_wp` | `crush` | 24 | 0.0 |
| `fists` | `br_pa_pa` | `crush` | 0 | 0.15 |
| `instrument` | `pampa_wp` | `missile` | 10 | 0.0 |
| `book` | `pampa_wp` | `crush` | 15 | 0.0 |
| `cloth_weapon` | `pampa_wp` | `swing` | 14 | 0.0 |
| `bag` | `rdm_pa_wp` | `crush` | 20 | 0.0 |

### Armor Response

| Armor class | Swing | Thrust | Crush | Missile |
| --- | ---: | ---: | ---: | ---: |
| `plate` | 0.65 | 0.65 | 1.15 | 0.8 |
| `mail` | 0.75 | 1.1 | 0.95 | 1.1 |
| `leather` | 0.95 | 0.95 | 1.0 | 0.95 |
| `cloth` | 1.0 | 1.0 | 1.0 | 1.0 |

### Scaling And Stress Constants

```text
phase_wp_scalar:
  early: 0.50
  mid: 0.75
  late: 1.00
  stress: 1.00

stress_engines:
  two_hands: 1.80
  two_swords_hits: 2
  attack_boost: 1.3333333333333333
  high_brave: 97
  accuracy_evasive_hitrate: 0.75

magic:
  routine: K * MA * max(faith_floor, (casterFaith / 100) * (targetFaith / 100))
  faith_factor_floor: 0.60
  shell_multiplier: 0.667

combined_multiplier_clamp:
  min: 0.25
  max: 2.50
```

## Pinned Simulation Conventions

### Effective Weapon Power

Effective weapon power is:

```text
wp_eff = wp * phase_wp_scalar
```

`wp_eff` is a float. Do not round it before the routine calculation. Floor only at the final
damage step.

This matters for independent verification. Rounding `wp_eff` early caused reviewer-side
off-by-two rows in early-band checks.

### Viability Lenses

Family viability is hit-count- and engine-normalized.

Use separate lenses:

- single-hit lens: no dominant support engine;
- dual-wield lens: `Two Swords` families are judged with their expected hit count;
- engine lens: support engines are compared against the relevant support-engine context.

Do not mix multi-hit or engine-boosted totals into the single-hit benchmark. That falsely makes
normal single-hit families look weak.

Volatile families may credit maximum damage for their own viability, but their maximum damage
does not raise the benchmark for every other family.

## Scorecard

Simulation artifacts:

```text
work/gpt-sim-v0.2-results.json
work/gpt-sim-v0.2-results.csv
```

Dual-independent gate:

```text
GPT harness: tools/sim_damage.py
Claude checker: independent reviewer implementation reported through the AI relay
Agreement: 417 / 417 family rows, 0 mismatches
```

### Metric Results

| Metric | Result | Evidence |
| --- | --- | --- |
| Family viability | PASS | all non-exempt combat families viable under scoped lenses; no missing families |
| No dominance | PASS | top family share: sword 9/25 = 36%, below 50% limit |
| Scale band | PASS | max damage / target HP = 1.0333, below 1.25 limit |
| Magic coexistence | PASS | magic / top physical = 281 / 415 = 0.6771 |
| Plate matchup | PASS | plate crush average 144.1 > swing 115.7 and thrust 115.0; mail thrust 182.3 > swing 133.7 |

Late/stress best-count distribution:

| Family | Best-count |
| --- | ---: |
| `sword` | 9 |
| `spear` | 4 |
| `ninja_blade` | 3 |
| `fists` | 2 |
| `knight_sword` | 2 |
| `staff` | 2 |
| `crossbow` | 1 |
| `knife` | 1 |
| `pole` | 1 |

## Interpretation

V0.2 is the first candidate where the policy behaves like the mod's stated goal:

- swords stay good, especially into soft targets, but no longer dominate every target class;
- plate gives crush/impact families a real signature target;
- mail gives thrust and missile routes a real identity;
- guns keep a PA-independent anti-armor role without breaking early damage, because phase WP
  scaling prevents late WP from being used in early scenarios;
- magic becomes competitive by applying the accepted F2-lite policy directly instead of only
  lowering physical ceilings;
- Two Hands remains strong and recognizable at `1.80`, but no longer collapses the coexistence
  metric.

## Open Items

This milestone does not close implementation or final balance.

Still open:

- Run the Windows `04` session and capture real IVC `work/baseline_weapons.csv`.
- Re-simulate v0.2 after replacing WotL-fallback WP with verified IVC weapon values.
- Improve accuracy and evasion modeling. V0.2 only uses one coarse `0.75` evasive row.
- Exercise status, element, Shell/Protect, and Zodiac layers beyond neutral/default cases.
- Simulate per-family identity beyond damage, including range, CT, status, job-lock payoff, and
  support utility.
- Revisit exact player-facing presentation after formulas stabilize.

## Next Track

The natural next track is the Windows proof/baseline session from
`04-proof-and-baseline-plan.md`.

After real weapon values are captured, the project should produce:

```text
work/baseline_weapons.csv
work/sim-inputs-v1.json
docs/formula-balance/12-validated-policy-v1.md
```

Only after that re-simulation should any formula be promoted beyond
`Conceptually viable, pending verified-baseline re-sim`.
