# Native ForcedMovement policy-ticket template

## Context

After DirectNumeric, property-payload StatusApplication, StatusRemoval, Dispel, Quick, and Revive templates, the next auxiliary candidate was standalone ForcedMovement. This family is only safe as a template if the final native map verdict is explicit; it must not infer pathfinding, intermediate tiles, or live map legality from action shape.

## Hypothesis

A standalone ForcedMovement template can build `DclCanonicalNativeForcedMovementActionPolicySource` from strict JSON when it carries the shared magic policy, defense option, optional resistance/immunity facts, and one final `DclCanonicalNativeMovementVerdict`.

## Validation

- Added `DclCanonicalNativeForcedMovementActionPolicyTemplate`.
- The builder materializes `DclCanonicalNativeForcedMovementActionPolicySource` without Reaction or concentration inference.
- The loader rejects missing magic, missing verdict, invalid target identity, unresolved path verdicts, negative requested/moved tiles, movement beyond requested distance, nonzero movement for zero requested tiles, and blocked-zero verdicts whose destination differs from origin.
- Smoke coverage proves a ForcedMovement template serializes, loads, builds a ticket for a retained admitted action, publishes into the policy-source ledger, and leaves the retained native carrier unexecuted.
- Smoke coverage also proves an impossible moved-distance verdict fails during strict load.

## Result

Standalone ForcedMovement policy-ticket template production is offline-proven for explicit final native verdict facts. This does not replace the remaining native map-verdict producer gate; it only establishes the strict intake format once that verdict has a proven owner.
