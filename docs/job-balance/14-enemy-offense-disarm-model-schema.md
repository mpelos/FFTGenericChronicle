# Enemy-Offense And Disarm Model Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `docs/job-balance/06-knight-archer-v1-proposal.md`
- `docs/job-balance/07-validation-infrastructure-roadmap.md`
- `docs/job-balance/12-vanilla-skill-status-reference.md`
- `docs/job-balance/13-armor-response-model-schema.md`
- `docs/formula-balance/11-validated-policy-v0.2.md`
- `work/sim-inputs-v0.2.1.json`
- `work/t7-enemy-offense-scenarios-v0.json`

## Purpose

This document starts T7, the enemy-offense and disarm validation track.

T7 exists to validate skills that can reduce, jam, remove, or replace an enemy's offensive weapon
family. The immediate consumers are:

- Knight `Rend Weapon`;
- counterplay such as Chemist `Safeguard`;
- future Thief, Orator, Vanguard, or controller effects that pressure enemy equipment or
  weapon output.

## Source Notes

T7.0 reuses the accepted formula v0.2.1 family routines on the attacker side. It does not invent a
second damage model for enemies.

Static baseline:

```text
work/sim-inputs-v0.2.1.json
families[weapon_family].routine
families[weapon_family].damage_type
families[weapon_family].wp
jobs[job].bands[phase]
calc.phase_wp_scalar[phase]
```

The current family data is copied into the T7 bundle. If formula policy v0.2.1 changes, the bundle
must be regenerated and re-verified through Gate F5 or its accepted successor.

T7.0 models enemy output before target armor response, Protect/Shell, element, Zodiac, evasion, hit
chance, CT, AI targeting, or party follow-up. Resulting weapon family and damage type are recorded
so later T6xT7 composition can feed the new family/type into armor-response checks.

## Vanilla Reference Consultation

T7 should consult:

- `docs/reference/fft-vanilla-ability-effect-index.md`
- `docs/reference/fft-vanilla-status-effect-map.md`

The vanilla reference is a creative palette, not a compatibility cage. Relevant entries include:

- `Rend Weapon` ID 141;
- `Crush Weapon` ID 162;
- `Steal Weapon` ID 113;
- `Safeguard` ID 475;
- `Sticky Fingers` ID 450;
- weapon-output-adjacent control statuses such as `Berserk`, `Charm`, `Sleep`, and `Don't Act`.

T7 uses these references as vocabulary:

| T7 concept | Vanilla inspiration | Reference tags | Design interpretation |
| --- | --- | --- | --- |
| Temporary output pressure | `Rend Weapon` fantasy, stat-down adjacent `Rend Power` | `equipment_break`, `stat_down` | Reduce enemy weapon output for a bounded duration without deleting equipment. |
| Temporary jam | disarm/control fantasy | `equipment_break`, `status_add` | The weapon remains known, but the current offensive application is zeroed. |
| Permanent weapon break | `Rend Weapon` ID 141, `Crush Weapon` ID 162 | `equipment_break`, `damage` | Remove or replace the enemy's current family, modeled separately from temporary pressure. |
| Weapon steal/removal | `Steal Weapon` ID 113, `Sticky Fingers` ID 450 | `steal`, `reaction` | Equipment removal can use the same family-replacement machinery if later assigned to Thief. |
| Break counterplay | `Safeguard` ID 475 | `defense`, `equipment_break`, `support` | Some disruption can be blocked without making all enemy-offense pressure invalid. |

## Pinned Bundle

Pinned input bundle:

```text
work/t7-enemy-offense-scenarios-v0.json
```

The bundle defines formula routines, job stat bands, scenario rows, and expected values for the
first dual-independent T7 run.

## Formula Contract

T7.0 is per-application and per-hit. It does not multiply by duration, enemy action frequency,
accuracy, evasion, AI targeting, or party follow-up. `duration_ticks` is context only for later
T7xT5 composition.

### Step 1 - Base Enemy Family

```text
base_family = attack.weapon_family
base_raw_output_per_hit = routine(base_family, attacker_stats, phase_scaled_wp)
```

The routine is the same v0.2.1 routine used by the family damage harness:

| Routine | T7.0 calculation |
| --- | --- |
| `pa_wp` | `PA * WP` |
| `br_pa_wp` | `floor(PA * Brave / 100) * WP` |
| `spd_pa_wp` | `floor((PA + Speed) / 2) * WP` |
| `ma_wp` | `MA * WP` |
| `wp_wp` | `WP * WP` |
| `br_pa_pa` | `floor(PA * Brave / 100) * PA` |
| `pampa_wp` | `floor((PA + MA) / 2) * WP` |

Random-output routines are intentionally not included in the T7.0 scenario set. They should be
added when a concrete axe, flail, or bag disruption value needs acceptance.

### Step 2 - Permanent Family Replacement

Permanent weapon break, weapon steal, or disarm effects are evaluated as equipment-state changes:

```text
if effect is blocked by Safeguard-like counterplay:
  ignore that break effect for this application
else if effect provides fallback_family:
  resulting_family = fallback_family
else:
  resulting_family = none
```

If `resulting_family = none`, the current application has no weapon-family routine and projects
zero output.

If `resulting_family = fists`, the enemy keeps attacking through the v0.2.1 `fists` routine. This
matters because breaking a weapon should not be assumed to equal full action denial.

T7.0 assumes at most one unblocked permanent replacement effect per application. If a later skill or
encounter can apply multiple unblocked break, steal, or disarm effects at once, that later scenario
must define precedence before acceptance.

### Step 3 - Temporary Output Pressure

Temporary output pressure is applied after permanent replacement:

```text
effective_output_multiplier = clamp(product(output_multiplier effects), 0.0, 1.0)
```

This means a broken enemy who falls back to `fists` can also be temporarily weakened, but both
effects are recorded separately.

`output_multiplier` stacking is multiplicative, not additive. Two temporary output-down effects of
`0.60` and `0.50` produce `0.30`.

### Step 4 - Temporary Jam Or Output To Zero

Temporary jam and no-fallback weapon removal zero the current application:

```text
if temporary_jam or resulting_family = none:
  effective_output_multiplier = 0.0
```

Temporary jam keeps the known family/type for later composition notes. No-fallback removal changes
the resulting family/type to `none`.

### Step 5 - Output Projection

```text
output_per_hit = floor(round(resulting_raw_output_per_hit * effective_output_multiplier, 6))
total_output = output_per_hit * hit_count
```

The `round(..., 6)` step is required for deterministic cross-implementation behavior. Products such
as `100 * 0.29` can land at `28.999999999999996` in IEEE double arithmetic, and a naive floor would
incorrectly produce `28` instead of the mathematically intended `29`.

## Scenario Set

The first bundle includes rows for:

- unchanged base enemy output;
- temporary output-down;
- multiplicative stacking of two temporary output-down effects;
- partial jam/output reduction;
- temporary output-to-zero jam;
- permanent weapon break falling back to `fists`;
- permanent weapon break with no fallback;
- Safeguard-like counterplay blocking a break;
- multi-hit output reduction;
- a Brave-sensitive weapon routine;
- a float-safe floor boundary;
- permanent replacement followed by temporary output pressure.

These rows validate T7 machinery. They do not set final values for Knight, Thief, Orator, Special
Knight, Safeguard, or any other job skill.

## Expected Counter Output

GPT and Claude T7 counters should produce:

- one row per scenario;
- `scenario_id`;
- `model`;
- calculated base and resulting family fields;
- output projection fields;
- validation errors, if any.

The dual-independent gate from document 07 applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T7 output can be used to accept or reject skill values.

## What T7.0 Does Not Decide

Still open for later T7 versions:

- exact Knight `Rend Weapon` values;
- exact Thief steal/disarm values;
- exact Safeguard rules;
- whether permanent equipment deletion should exist in normal player skills;
- break, steal, or jam hit rates;
- AI targeting after losing a weapon;
- CT timing, duration tickdown, refresh behavior, and action frequency;
- whether enemy inventory can re-equip another weapon;
- boss, monster, unique-unit, or immune target policy;
- T6xT7 composition after weapon-family changes;
- T2 incidence thresholds for disarm, weapon break, Safeguard, or anti-break tools.

## Design Guardrails

- Temporary offense pressure and permanent equipment deletion must stay separate in validation.
- Disarm or weapon break must not become a mandatory party tax. T2 must flag any accepted build
  population where one disarmer or one anti-break support appears too often.
- Weapon break should be valuable against armed enemies and much weaker against unarmed, caster,
  monster-like, or no-weapon targets.
- Fallback attacks matter. A broken sword user falling back to `fists` is different from an enemy
  whose output is fully zeroed.
- T7 should preserve the FFT feel of tactical equipment pressure without making normal encounters
  about stripping every enemy before playing the fight.

## Claude Review Request

Claude should review whether:

- the attacker-side reuse of v0.2.1 routines is explicit enough;
- permanent replacement before temporary output pressure is the right order;
- temporary jam and no-fallback removal are separated clearly enough;
- Safeguard-like counterplay should be represented in this first bundle;
- the float-safe floor row is sufficient;
- any additional row is required before accepting T7.0.

Claude review verdict: Accepted after adding the multiplicative stacking scenario requested during
review (claude-opus-4-8, 2026-06-21).
