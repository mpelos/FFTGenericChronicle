# Native template producer family-generic bridge proof

## Context

The admission-side template producer was originally smoke-proven with DirectNumeric. After the template surface expanded to every current canonical native family, the remaining question was whether the retained bridge path itself was still family-generic or only practically proven for DirectNumeric.

## Hypothesis

The admission-side template producer can build and settle any supported family ticket if the template produces the correct explicit family policy source. A standalone ForcedMovement template is a good proof case because it requires a non-Direct family provider and an explicit final native movement verdict.

## Validation

- Built a runtime carrying a standalone ForcedMovement policy-ticket template registry.
- Captured one complete ForcedMovement native admission.
- Called `TryPublishAdmissionBuildTemplateAndResolve` directly.
- Verified the admission first retained with `MissingPolicyTicket`, the template built a ticket, ticket intake published, final bridge status became `Published`, dispatch family was `ForcedMovement`, the published native action used the original ActionInstance, both retained ledgers were empty afterward, one native action remained published, and exactly the expected execution RNG was consumed.

## Result

The template producer handshake is offline-proven as family-generic for at least DirectNumeric and standalone ForcedMovement. Template intake exists for all current canonical native families; remaining production work is binding real native owners for the explicit facts, not adding special-case bridge paths per family.
