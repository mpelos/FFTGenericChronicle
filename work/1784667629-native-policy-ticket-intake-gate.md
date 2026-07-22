# Native policy ticket intake gate

## Context

The policy-source ledger can validate and retain explicit policy tickets for retained native
ActionInstances. Production live capture may discover policy facts and admission facts at different
boundaries, so the intake side also needs a no-write readiness result rather than a mandatory
exception for ordinary ordering races.

## Result

`DclCanonicalNativePolicySourceLedger.TryPublishForRetainedAdmission` now accepts a candidate ticket
only when its retained admission already exists and no ticket is already retained for that
ActionInstance.

No-write statuses:

- `MissingAdmittedAction`
- `DuplicatePolicyTicket`

Invalid policy facts still fail strictly. The try-publish path therefore tolerates live callback
ordering without hiding bad DCL policy production.

Duplicate detection is intentionally checked before candidate validation. Once a valid ticket is
already retained for an ActionInstance, later repeats are idempotent no-write outcomes even if the
repeated object is malformed.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativePolicySourceLedger.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`

## Remaining

The producer that builds complete policy tickets from proven native context is still live-gated.
