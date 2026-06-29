# Area And Terrain Model Schema V0

Status: Accepted
Date: 2026-06-21
Depends on:
- `work/t11-area-terrain-scenarios-v0.json`

## Purpose

This document starts T11/T11A/T11B, the area and terrain validation gate.

T11 exists because many proposed job identities depend on hitting more than one relevant target,
using terrain, or paying for the safety of large coverage:

- Summoner area magic and ally-safe pressure;
- Geomancer terrain-dependent attacks;
- Time Mage `Meteor` or other large delayed areas;
- Samurai Iaido-like area pressure;
- Archer line-pierce if a future bow action becomes multi-target;
- Bard/Dancer, auras, zones, and sustained global effects in later T11xT5 compositions.

This gate does not decide final damage, CT, MP, hit rates, durations, or JP costs. It validates the
counting and terrain machinery that later gates need before those values can mean anything.

## Pinned Bundle

Pinned input bundle:

```text
work/t11-area-terrain-scenarios-v0.json
```

Expected GPT output:

```text
work/gpt-t11-area-terrain-v0.json
```

Canonical GPT checker:

```text
tools/check_area_terrain.py
```

## Formula Contract

T11 V0 is a per-action targeting snapshot. It selects the best target panel from a fixed list of
candidate panels, then reports target count, collateral, terrain validity, and the selected affected
unit IDs.

### Step 1 - Candidate Validity

A candidate panel is valid if:

```text
can_target_panel
and line_of_effect
and terrain requirement is satisfied, if one exists
```

`can_target_panel` and `line_of_effect` default to `true`.

Terrain requirements may use either:

- `target_panel` scope, where each candidate panel's terrain is checked independently;
- `origin_panel` scope, where the acting unit's panel terrain gates all candidates.

T11 V0 only uses `exclude` behavior for invalid terrain candidates. Later versions may model weak
fallbacks or terrain substitution if we want Geomancy to work more broadly.

### Step 2 - Area Membership

T11 V0 supports four shape families:

```text
single:  same x/y panel, within vertical tolerance
diamond: Manhattan distance <= radius, within vertical tolerance
square:  Chebyshev distance <= radius, within vertical tolerance
line:    axis-aligned ray from origin through target panel, within length and vertical tolerance
mapwide: all targetable units in the snapshot
```

For `line`, the target panel defines the ray direction. T11 V0 rejects non-axis-aligned line
candidates by treating them as no-hit candidates.

Line rows also pin these conventions:

- vertical tolerance is measured against the origin panel's `z`, because the ray emanates from the
  acting unit;
- line length is the axis distance from origin, inclusive at `distance <= length`;
- the origin panel itself is excluded from line hits.

### Step 3 - Target Group And Ally Safety

Each scenario chooses a target group:

```text
enemies
allies
all_units
```

`all_units` can hit both teams. If `ally_safe` is true, allied units are excluded from an
`all_units` hostile area. This models the practical advantage of ally-safe summons or similar
effects without deciding their final power.

`ally_safe` is inert for `enemies` and `allies` target groups in T11 V0 because those groups already
filter team eligibility directly.

Untargetable units are excluded from area counts unless a later special case explicitly reopens
them.

### Step 4 - Risk-Adjusted Area Score

Each affected unit contributes:

```text
priority * enemy_weight if enemy
priority * ally_weight  if ally
```

`priority` defaults to `1.0`. The selected score is rounded to six decimals.

This score is not final damage. It is a deterministic proxy for whether an area can find good
multi-target value without ignoring collateral or terrain constraints.

### Step 5 - Deterministic Panel Selection

For each valid candidate panel:

```text
score = sum(affected unit contributions)
```

The selected panel is the valid candidate with the highest score. Ties use earliest candidate order.

If no candidate is valid:

```text
selected_panel_id = none
no_selection_reason = no_valid_candidate
```

If the best score is below `minimum_score_to_select`:

```text
selected_panel_id = none
no_selection_reason = below_minimum_score
```

`minimum_score_to_select` defaults to `0`, but hostile rows should generally pin it at `1` so a
zero-hit area is not treated as a useful action.

## Scenario Set

The first bundle includes rows for:

- diamond radius target counts with friendly collateral;
- ally-safe exclusion for the same cluster;
- best-panel selection avoiding friendly fire;
- vertical tolerance excluding elevated targets;
- square shape diagonal coverage;
- axis-aligned line pierce;
- mapwide targetable enemy counts;
- deterministic tie-breaks;
- minimum-score no-selection behavior;
- target-panel terrain filtering;
- no-valid-terrain behavior;
- terrain availability ratio;
- ally support areas excluding enemies;
- origin-panel terrain gating.
- non-default unit priority weighting;
- non-axis line rejection as valid-but-no-hit;
- radius greater than `1`;
- line length upper boundary and origin exclusion;
- line-of-effect candidate invalidity independent of terrain;
- line vertical tolerance measured from the origin panel.

These rows validate area/terrain machinery. They do not set final values for Summoner, Geomancer,
Time Mage, Samurai, Archer, Bard, Dancer, Ramza, or any unique skill.

## Expected Counter Output

GPT and Claude T11 counters should produce:

- one row per scenario;
- `scenario_id`;
- `selected_panel_id`;
- selected score and target count fields;
- affected unit IDs;
- valid candidate counts and terrain availability ratio;
- no-selection reason, if any;
- validation errors, if any.

The dual-independent gate applies:

- same pinned bundle;
- independent GPT and Claude implementations;
- `0` row mismatches before T11 output can be used by T11xT5, T3xT5xT11, or concrete area-skill
  proposals.

## What T11 V0 Does Not Decide

Still open:

- exact area radius, vertical tolerance, CT, MP, JP, and damage values for specific skills;
- whether ally-safe area effects require larger CT/MP costs than unsafe areas;
- exact Geomancy terrain source if a future design chooses origin terrain, target terrain, or a
  hybrid rule;
- Reflect, spell routing, and target redirection;
- sustained area throughput over time;
- mapwide Bard/Dancer tick values;
- AI movement into or out of area effects.

## Claude Review Request

Claude should review whether:

- the area-shape contract is specific enough for independent implementation;
- `ally_safe` should exclude allied units only for `all_units` rows in T11 V0;
- terrain invalid candidates should be excluded rather than scored as weak fallback rows;
- the scenario set covers the first Summoner/Geomancer/Meteor/Iaido/line-pierce needs;
- any additional row is required before accepting T11 V0.

Claude review verdict: accepted.

Claude independently reran `work/t11_area_terrain_check.py` against the expanded 20-row bundle and
`work/gpt-t11-area-terrain-v0.json`. The reviewer result was `scenario_count=20` and
`mismatch_count=0` against both the bundle expected values and GPT's calculated output.

The accepted row set includes the required post-review branches:

- `t11_priority_weighting` proves non-default unit priority participates in the score.
- `t11_line_non_axis_no_hit` proves non-axis line candidates remain valid candidates but produce no
  hits and fail by `below_minimum_score`.

It also includes rows for radius greater than `1`, line length boundary/origin exclusion,
`line_of_effect=false`, and line vertical tolerance from origin height.

T11 gate #2 is cleared.
