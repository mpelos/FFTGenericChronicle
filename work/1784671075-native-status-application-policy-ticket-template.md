# Native StatusApplication policy ticket template

## Context

DirectNumeric, StatusRemoval, and Dispel had strict policy-ticket template producers. The next safe
auxiliary family is StatusApplication, but arbitrary state payload polymorphism would make the JSON
template too broad and easy to misuse.

## Hypothesis

A property-payload-only StatusApplication template can cover common status carriers while preserving
fail-closed policy. It should materialize a concrete `DclPropertyStatePayload` from explicit schema
and string values, not deserialize arbitrary `DclStatePayload` subclasses.

## Validation

- Added `DclCanonicalNativeStatusApplicationPolicyTemplate`.
- Added `DclCanonicalNativePropertyStateMaterializationTemplate`.
- The builder emits a `DclCanonicalNativeStatusActionPolicySource` with shared magic policy,
  resistance modifiers, and concrete property state materialization.
- The loader rejects missing materialization, negative duration/strength, missing stack
  discriminator, missing payload schema, and invalid payload key/value pairs.
- Smoke tests materialize and validate a retained StatusApplication ticket through the
  policy-source ledger without RNG or native carrier execution.

## Conclusion

Property-payload StatusApplication now has offline-proven policy-ticket template production.
Template support still excludes typed payload families such as QuickLock, Reraise, Taunt, Aim, and
map-verdict movement until each receives an explicit schema.
