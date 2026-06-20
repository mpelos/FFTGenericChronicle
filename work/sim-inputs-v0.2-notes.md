# sim-inputs-v0.2 notes

Pinned input bundle for the first validated formula-policy candidate.

Canonical artifact:

```text
work/sim-inputs-v0.2.json
```

Generated GPT outputs:

```text
work/gpt-sim-v0.2-results.json
work/gpt-sim-v0.2-results.csv
```

## Result Class

`sim-inputs-v0.2` is conceptually viable, pending verified-baseline re-sim.

Job multipliers are derived from `work/baseline_jobs.csv`, but weapon WP values are still
WotL-fallback / design provisional because `work/baseline_weapons.csv` is empty.

## Pinned Conventions

Effective weapon power:

```text
wp_eff = wp * phase_wp_scalar
```

`wp_eff` remains a float. Do not round it before the routine calculation. Floor only at the final
damage step.

Viability is scoped by lens:

- single-hit lens;
- dual-wield lens;
- support-engine lens.

Do not use multi-hit or engine-boosted totals as the benchmark for single-hit families.

Volatile families may credit max damage for their own viability, but their max damage must not
raise the benchmark for every other family.

## Independent Verification

Claude independently verified GPT output against `work/sim-inputs-v0.2.json`.

Result:

```text
417 / 417 family rows agree
0 mismatches
all five metrics pass
```
