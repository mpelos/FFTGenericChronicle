# sim-inputs-v0 notes

Pinned input bundle for the dual-independent simulation (doc 05). Both simulators load
`work/sim-inputs-v0.json` verbatim so any output divergence is pure formula logic.
Regenerate with `python3 work/gen_sim_inputs.py`.

## Provenance / result class
- Effective stats derive from REAL job multipliers (`work/baseline_jobs.csv`, generic
  block 74-92) via `effective = round(raw_base[band] * multiplier / 100)`.
- Raw bases per band, weapon WP, and armor responses are **WotL-fallback /
  design-provisional** — `work/baseline_weapons.csv` is still empty (Windows 04 dump).
- Therefore every result is **"conceptually viable, pending verified-baseline re-sim"**.

## Schema (top-level keys)
- `phase_bands`: early/mid/late/stress raw bases.
- `routines`: id -> human formula for each damage routine.
- `families`: per family `{routine, damage_type, wp, penetration}`. damage_type in
  {swing, thrust, crush, missile}. penetration in [0,1].
- `armor_response`: armor_class -> {swing,thrust,crush,missile} multiplier.
- `magic`: magic axis (ignores physical armor class; Shell/element/Faith only).
- `calc`: shared calc-spec constants.
- `jobs`: anchor -> {job_id, armor_class, multipliers, bands{hp,pa,ma,spd}}.

## Key conventions both sims MUST implement identically
1. Penetration (Q2, family-fixed): `eff_resp = base + pen*(ceiling - base)` when
   `base < ceiling`, else `base`. `ceiling = calc.penetration_ceiling (1.10)`.
   This is how gun/spear/crossbow "bypass" armor — by softening the target's
   response toward neutral, not by subtraction.
2. Pipeline order (`calc.operation_order`): pressure -> type_response(with pen)
   -> protect/shell -> element -> zodiac.
3. Stacking discipline: clamp the COMBINED post-pressure multiplier to
   `calc.combined_multiplier_clamp` ([0.25, 2.5]) before flooring. Prevents
   multiplicative blowups/near-zeros.
4. Floor: `max(calc.chip_floor, floor(value))`.
5. `rdm_pa_wp` (axe/flail/bag): report min=1*WP, max=PA*WP, expected=((PA+1)/2)*WP.
6. Magic ignores `armor_response`; mitigate via `magic.shell_multiplier`, element,
   and target Faith inside the routine.

## Marcelo's plate constraint (verify it survives every iteration)
`plate`: swing 0.65, thrust 0.65, crush 1.15, missile 0.80 -> crush is the answer to
plate; gun (pen .70) reaches ~1.01 vs plate (anti-armor); sword stays 0.65.

## Starting values are deliberately coarse
The armor_response buckets, WP, and penetration values are a STARTING point. The sim
search tunes them (plus per-family routine tweaks and Q4 dominance ceilings) until the
five metrics pass in BOTH sims. Do not treat any number here as final.
