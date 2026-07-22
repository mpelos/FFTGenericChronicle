# Composed evaluation dispatch bridge

## Context

The confirmed-execution path already had a retained composed wrapper that carries ability id,
classified family, and typed family request into the dispatcher. Forecast/AI still had a direct
`abilityId + familyInput` entry point, which meant offline tests could prove evaluator selection but
not the same retained-family boundary used by confirmed execution.

## Result

Added `DclCanonicalComposedEvaluation` as the forecast/AI wrapper:

- `AbilityId`
- retained `DclCanonicalActionFamily`
- typed family input object

`DclCanonicalEvaluationDispatcher.Evaluate(runtime, composed)` re-resolves the ability family from
the runtime catalog and rejects a retained-family mismatch before invoking a canonical evaluator or
projection.

## Evidence

- Smoke test now dispatches direct numeric magic forecast/AI through the composed wrapper.
- Smoke test mutates the retained family to `PhysicalDamage` and verifies the dispatcher fails
  before evaluation.
- Stable docs and coverage matrix now describe the same boundary.

## Remaining

Native forecast/AI carrier binding remains gated. The offline contract now matches the confirmed
execution dispatcher shape; the next live-facing work is carrying this composed object from native
forecast/AI capture into the dispatcher without letting formula id, animation, or caller shape
select a different family.
