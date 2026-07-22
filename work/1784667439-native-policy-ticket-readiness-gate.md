# Native policy ticket readiness gate

## Context

The retained native execution bridge already had a strict path that consumes a validated
policy-source ticket and publishes the canonical ActionInstance. A production callback also needs a
non-mutating readiness check because policy-ticket production is still a separate live-gated owner.

## Result

`DclCanonicalNativeRetainedActionBridge.TryResolvePublishAndRetirePolicyTicket` now returns an
explicit status:

- `MissingAdmittedAction`: no retained admission exists for the ActionInstance.
- `MissingPolicyTicket`: the retained admission exists, but no validated policy-source ticket is
  ready.
- `Published`: the ticket was consumed through the strict bridge path.

The first two statuses are no-write outcomes: they do not draw RNG, publish native carriers, retire
admitted actions, or mutate the policy-source ledger.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclCanonicalNativeRetainedActionBridge.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `docs/modding/06-code-mod-runtime-dsl.md`
- `docs/modding/08-dcl-information-requirements.md`
- `tools/report_dcl_implementation_coverage.py`

## Remaining

The live production callback still needs to create policy-source tickets from proven native
context. Until then, callback invocation remains gated.
