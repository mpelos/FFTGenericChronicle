# Native Revive policy-ticket template

## Context

The retained native admission bridge already accepted strict producer templates for DirectNumeric, property-payload StatusApplication, StatusRemoval, and Dispel. Revive was the next safe offline template because its required family policy is explicit data: shared magic policy, Faith multiplier, Undead interaction table, optional resistance/immunity facts, and optional Stored-Reraise materialization.

## Hypothesis

A Revive template can build a `DclCanonicalNativeReviveActionPolicySource` from explicit JSON without live memory inference, provided the loader rejects negative Faith multipliers and incomplete Undead tables before any ticket enters the retained policy-source ledger.

## Validation

- Extended the strict policy-ticket template model with `DclCanonicalNativeReviveActionPolicyTemplate`.
- Added builder support for `DclCanonicalNativeReviveActionPolicySource`, including complete `DclUndeadInteractionTable` reconstruction and optional property-payload Stored-Reraise materialization.
- Added loader validation for required magic policy, nonnegative Faith multiplier, complete Undead rules, and optional property materialization.
- Added smoke coverage proving a Revive template serializes, loads, builds a ticket for a retained admitted action, publishes into the policy-source ledger, and leaves the retained native carrier unexecuted.
- Added a negative case proving invalid Revive Faith plus an incomplete Undead table fails during load.

## Result

Revive policy-ticket template production is offline-proven for explicit producer facts. It does not solve live production of native-rich policy sources; Quick, standalone ForcedMovement, PhysicalDamage, and AreaNumeric still require richer native/map/sequence facts before they can be safely templated or produced from live context.
