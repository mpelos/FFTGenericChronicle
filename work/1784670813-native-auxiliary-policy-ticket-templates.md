# Native auxiliary policy ticket templates

## Context

The admission-side policy-ticket producer could use configured templates for DirectNumeric. The
same retained admission boundary also supports auxiliary single-target magic families, but only
some of them have small enough explicit policy surfaces to add safely without live-native facts.

## Hypothesis

StatusRemoval and Dispel can share the same strict template contract without inventing live state:
StatusRemoval needs only the shared single-target magic policy, while Dispel needs that policy plus
final Dispel score and an optional selected stored-state instance id.

## Validation

- Extended `DclCanonicalNativeFamilyPolicyTicketTemplate` with `StatusRemoval` and `Dispel` arms.
- The template builder now emits a `DclCanonicalNativeSingleTargetMagicPolicySource` for
  StatusRemoval and a `DclCanonicalNativeDispelActionPolicySource` for Dispel.
- The JSON loader rejects missing auxiliary policy arms and nonpositive Dispel scores before
  publication.
- Smoke tests build retained StatusRemoval and Dispel admitted actions, materialize their tickets,
  and validate those tickets through the policy-source ledger without RNG or native carrier
  execution.

## Conclusion

DirectNumeric, StatusRemoval, and Dispel now have offline-proven policy-ticket template production.
StatusApplication, Quick, Revive, ForcedMovement, PhysicalDamage, and AreaNumeric still require
their richer explicit policy facts before they should be added to the template surface.
