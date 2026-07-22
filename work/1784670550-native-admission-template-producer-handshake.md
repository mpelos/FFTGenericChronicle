# Native admission template producer handshake

## Context

The canonical runtime can carry optional DirectNumeric policy-ticket templates. The admission
callback still needed a fail-closed way to use those templates after it captures a complete native
action.

## Hypothesis

The retained-action bridge can publish the admission first, then build and submit a template ticket
only when the admission is waiting on `MissingPolicyTicket`. This keeps no-template behavior
unchanged and lets configured DirectNumeric probe abilities settle without a separate producer
hook.

## Validation

- Added `TryPublishAdmissionBuildTemplateAndResolve`.
- The method returns the original admission intake, optional template-build result, optional
  ticket intake, and a final bridge status.
- No-template path retains the admission, reports `MissingTemplate`, consumes no RNG, and publishes
  no native carrier.
- Configured DirectNumeric template path builds a policy ticket, publishes it through the ticket
  handshake, consumes the retained pair, publishes the native carrier, and retires the admission
  and ticket.
- The guarded admission callback now uses this producer path and logs admission/template/ticket/final
  bridge statuses.

## Conclusion

The DirectNumeric sentinel/probe path now has an offline-proven admission-side policy-ticket
producer. Production policy-ticket capture for richer live-native facts remains separate work.
