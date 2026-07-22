# Native policy ticket template loader

## Context

The retained admission/policy-ticket handshake can settle a native ActionInstance only after both
halves exist. Before this checkpoint, the admission callback could retain a complete action, but no
producer-side contract existed for policy tickets.

## Hypothesis

A strict, offline-loadable template layer can create policy tickets for known probe abilities
without inferring live gameplay facts. The first safe scope is DirectNumeric because its policy
shape is already covered by the single-target magic and Direct policy-source providers.

## Validation

- Added `DclCanonicalNativePolicyTicketTemplateJsonLoader` with schema revision 1.
- Added a registry/builder that materializes a `DclCanonicalNativePolicySourceTicket` from a
  retained admitted action plus an explicit DirectNumeric template.
- The builder returns `MissingTemplate` without mutation when no template exists for an admitted
  ability.
- The loader rejects malformed/unsupported templates before publication; unsupported families are
  not guessed.
- Smoke test path compiles and runs the DirectNumeric template through the retained native action
  fixture.

## Conclusion

The offline policy-ticket producer contract now exists for DirectNumeric sentinel/probe work. It is
not connected to the live admission callback yet; production live capture still needs a source for
the explicit template facts and a guarded invocation path.
