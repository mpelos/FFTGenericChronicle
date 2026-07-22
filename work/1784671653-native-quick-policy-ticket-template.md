# Native Quick policy-ticket template

## Context

After Revive, the remaining auxiliary policy-template candidates were Quick and ForcedMovement. ForcedMovement requires a native map verdict, while Quick can be represented with explicit non-map facts: shared magic policy, target CT, empty QuickLock controller, and lock materialization.

## Hypothesis

A Quick template can build a `DclCanonicalNativeQuickActionPolicySource` from explicit JSON without live timeline inference if the loader restricts target CT to the pre-turn range and materializes only the property-payload QuickLock state.

## Validation

- Added `DclCanonicalNativeQuickActionPolicyTemplate`.
- The builder reconstructs a `DclCtState` from explicit target CT, creates a fresh `DclQuickLockController`, and materializes the QuickLock payload through the existing property-state template path.
- The loader rejects missing magic policy, target CT outside `[0, 100)`, and invalid lock materialization before ticket publication.
- Smoke coverage proves a Quick template serializes, loads, builds a ticket for a retained admitted action, publishes into the policy-source ledger, and leaves the retained native carrier unexecuted.
- Smoke coverage also proves an out-of-range target CT fails during strict load.

## Result

Quick policy-ticket template production is offline-proven for explicit producer facts. It still does not bind real native CT/timeline production; that remains a native callback/integration gate. ForcedMovement remains unsuitable for this simple template pass because its safe policy source needs an immutable native movement verdict.
