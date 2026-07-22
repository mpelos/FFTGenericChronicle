# Canonical admission template path warning

## Context

The guarded native admission callback now enters the retained bridge through the admission-side template producer. If canonical admission is enabled without a policy-ticket template path, complete admissions can still be retained, but they will wait for an externally retained policy ticket or report `MissingTemplate`.

## Hypothesis

Settings validation should warn, not error, when `DclCanonicalAdmissionEnabled` is true and `DclCanonicalPolicyTicketTemplatesPath` is empty. A warning is appropriate because external ticket injection remains a valid test route, but the normal template-producer route would otherwise appear configured while producing no tickets.

## Validation

- Added a validator warning scoped to `DclCanonicalPolicyTicketTemplatesPath`.
- Extended the canonical runtime smoke test to require that warning when admission is enabled with the four required runtime artifact paths but no policy-ticket template path.
- Verified build and smoke.

## Result

Canonical admission settings now surface the operational gap between retaining complete admissions and supplying policy tickets. The runtime still permits external-ticket tests, while profiles that intend template production receive an explicit configuration warning.
