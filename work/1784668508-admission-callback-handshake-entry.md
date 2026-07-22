# Admission callback handshake entry

## Context

The retained-action bridge owns symmetric admission/ticket handshakes, but the guarded admission
callback still wrote complete captured actions directly to `NativeAdmittedActions`. That bypassed
the production-ready readiness statuses and any already-retained policy ticket.

## Result

`DclCanonicalAdmissionCallbackImpl` now calls
`DclCanonicalNativeRetainedActionBridge.TryPublishAdmissionAndResolve` for complete captured native
actions.

Current behavior without a policy-ticket producer:

- complete admission is retained;
- bridge status is `MissingPolicyTicket`;
- no execution RNG or native publication occurs.

Future behavior when a validated ticket is already retained:

- the same admission callback path settles through the strict retained bridge;
- the ActionInstance is published once and then protected by the native action tombstone.

The callback log now records admission intake and bridge statuses.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/Mod.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

The policy-ticket producer remains live-gated and is still not connected to native context.
