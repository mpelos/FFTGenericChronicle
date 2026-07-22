# Native policy ticket template runtime load

## Context

The DirectNumeric policy-ticket template loader existed as an isolated contract. The canonical
runtime still needed an opt-in way to carry those templates together with the rest of the immutable
authoring snapshot.

## Hypothesis

`DclCanonicalRuntimeCatalog` can safely carry an optional policy-ticket template registry. Empty
configuration should produce an empty registry, while a configured JSON artifact should load in the
same all-or-nothing path as authoring, item metadata, ability bindings, and Reaction bindings.

## Validation

- Added optional `PolicyTicketTemplatesPath` to `DclCanonicalRuntimePaths`.
- Added `DclCanonicalPolicyTicketTemplatesPath` to runtime settings and reload path normalization.
- Runtime reload now watches the optional path/write timestamp and reports
  `policyTicketTemplates=<count>` in the canonical runtime load log.
- `DclCanonicalRuntimeLoader.LoadJson` accepts optional policy-ticket template JSON and publishes
  an empty registry when absent.
- Smoke tests prove both empty-registry and one-template registry loading through the canonical
  runtime loader.

## Conclusion

The canonical runtime can now carry DirectNumeric policy-ticket templates offline without changing
live callback behavior. The remaining production gap is a guarded producer invocation that turns
loaded templates plus retained admissions into ticket-side handshake calls at the right native
boundary.
