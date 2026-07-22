# Native composed plan identity

## Context

Native forecast/AI composition can now project a classified native execution request into a
`DclCanonicalComposedEvaluation`. The next offline risk was policy drift: production code could
compose confirmed execution from one admitted snapshot/policy set and forecast/AI from a different
one.

## Result

Added `DclCanonicalNativeComposedPlan`, a retained pair of:

- `DclCanonicalNativeComposedExecution`
- `DclCanonicalComposedEvaluation`

`DclCanonicalNativeEvaluationComposer.ComposePlan*` builds both from the same confirmed native
composition and verifies that ability id and canonical family remain identical on both wrappers.

## Evidence

- The Direct native composition smoke uses `ComposePlanCaptured`.
- The smoke checks that execution and evaluation wrappers share the same ability-family identity.
- Stable docs and coverage now describe the shared-plan invariant.

## Remaining

Production native hooks still need policy-input providers and carrier-field bindings. This work
keeps the future hook path from splitting forecast/AI and confirmed execution into divergent
composition sources.
