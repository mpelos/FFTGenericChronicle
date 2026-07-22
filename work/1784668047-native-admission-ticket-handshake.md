# Native admission-side ticket handshake

## Context

The ticket-side handshake can accept a candidate policy ticket and settle when a retained admission
already exists. The live callback also needs the opposite entry point: a complete admitted action
may arrive before the policy ticket, or a duplicate admission callback may occur after the ticket is
ready.

## Result

`DclCanonicalNativeAdmittedActionLedger.TryPublishForPolicyTicket` now returns explicit intake
status:

- `Published`
- `DuplicateAdmittedAction`

`DclCanonicalNativeRetainedActionBridge.TryPublishAdmissionAndResolve` combines admission intake
with the existing policy-ticket readiness gate:

- Admission first: retain admission and return `MissingPolicyTicket` without RNG/publication.
- Duplicate admission after ticket publication: settle the already-retained ActionInstance once.
- Admission replay after native carrier publication: return `ActionAlreadyPublished` and do not
  retain or resurrect the ActionInstance.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeAdmittedActionLedger.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeActionLedger.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeRetainedActionBridge.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`

## Remaining

The live producer still has to construct complete policy-source tickets from proven native context.
This handshake only defines safe ordering and replay behavior for admission/ticket arrival.
