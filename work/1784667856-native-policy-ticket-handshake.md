# Native policy ticket handshake

## Context

The retained-admission ledger and policy-source ledger now expose no-write readiness statuses.
Production still needs a single callback-facing handshake that can accept one policy-ticket
candidate and settle the retained ActionInstance only when both halves exist.

## Result

`DclCanonicalNativeRetainedActionBridge.TryPublishTicketAndResolve` combines policy-ticket intake
with retained-action publication:

- If the retained admission is missing, the ticket is not retained and no execution occurs.
- If the admission exists, the ticket is validated and retained.
- Once the ticket is present, the ActionInstance resolves through the strict policy-ticket bridge.
- After success, both the policy ticket and admitted action are retired.
- A replay after settlement returns the missing-admission no-write status and cannot resurrect the
  published ActionInstance.

This is still offline infrastructure. It does not produce native policy facts and does not enable a
live hook by itself.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeRetainedActionBridge.cs`
- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativePolicySourceLedger.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`

## Remaining

The missing live piece is still production policy-ticket construction from proven native context.
The handshake only defines what happens after a complete candidate ticket exists.
