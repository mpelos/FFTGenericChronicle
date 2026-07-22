# Native AreaNumeric policy-ticket template

## Context

AreaNumeric is the first remaining NativeRepeat family after auxiliary one-target templates. It is not a simple unit-target template: it requires one complete admitted NativeRepeat sequence plus an explicit area policy source containing per-target facts.

## Hypothesis

An AreaNumeric template can safely materialize a policy ticket if it carries the exact `DclCanonicalNativeAreaActionPolicySource` and the admitted action is a complete NativeRepeat sequence. The loader should reject empty or structurally invalid target policy rather than inferring area membership.

## Validation

- Added `AreaNumeric` as an explicit family-policy source in the strict policy-ticket template surface.
- Added loader validation for required tradition, nonnegative tradition skill, nonnegative caster penalty, nonnegative fixed-tile height when present, at least one target policy, valid target identities, nonnegative target penalty magnitudes, nonnegative authored displacement, and valid nested ForcedMovement verdicts.
- Added smoke coverage that captures a complete three-sweep Area NativeRepeat action, builds an AreaNumeric policy ticket from JSON, publishes it into the retained policy-source ledger, and leaves the retained native carrier unexecuted.
- Added a negative smoke case proving an AreaNumeric template with no target facts fails during strict load.

## Result

AreaNumeric policy-ticket template production is offline-proven for explicit per-target area policy facts and complete NativeRepeat admissions. PhysicalDamage remains the last large native family without template coverage, because its policy source also owns weapon, defense, protection, strike-weapon, and skill-training facts.
