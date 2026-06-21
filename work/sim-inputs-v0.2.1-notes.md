# sim-inputs-v0.2.1 notes

Pinned input bundle for the first job-balance armor-class reconciliation.

Canonical artifact:

```text
work/sim-inputs-v0.2.1.json
```

Generated GPT outputs:

```text
work/gpt-sim-v0.2.1-results.json
work/gpt-sim-v0.2.1-results.csv
```

## Change From V0.2

`sim-inputs-v0.2.1` keeps v0.2 formula constants, weapon-family parameters, stress engines, and
armor responses unchanged.

It changes only job target `armor_class` labels after reconciling against real
`work/baseline_jobs.csv` equipment access:

- Squire: `mail` -> `leather`
- Samurai: `mail` -> `plate`
- Dancer: `leather` -> `cloth`
- Bard remains `cloth`, so Bard and Dancer now share the same target armor class.

## Result Class

`sim-inputs-v0.2.1` remains conceptually viable, pending verified-baseline re-sim.

Weapon WP values are still WotL-fallback / design provisional because `work/baseline_weapons.csv`
is empty.

## GPT Scorecard

All five v0.2 metrics still pass:

- family viability: PASS
- no dominance: PASS
- scale band: PASS
- magic coexistence: PASS
- plate matchup: PASS

The scorecard is numerically unchanged from v0.2. The current formula sweep uses representative
target anchors by armor class, so changing Squire, Samurai, and Dancer target classes does not
change the 433 generated rows.

## Independent Verification

Claude independently verified GPT output against `work/sim-inputs-v0.2.1.json`.

Result:

```text
417 / 417 family rows agree
0 mismatches
all five metrics pass
```

## Follow-Up

Future party-sim and formula-v1 sweeps should target the actual in-scope job roster, using each
job's real armor class and HP, instead of only four representative armor anchors.
