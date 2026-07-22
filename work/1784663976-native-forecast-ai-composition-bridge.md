# Native forecast/AI composition bridge

## Context

The confirmed native path can compose one retained classified execution request from captured
native admission, synchronized snapshots, and explicit family-policy inputs. Forecast/AI had the
family dispatcher and composed wrapper, but not a native-side composition bridge that reused the
same boundary.

## Result

Added `DclCanonicalNativeEvaluationComposer`.

The composer:

- consumes the same classified native admission/snapshot/policy inputs as confirmed composition;
- reuses the existing family execution composers;
- strips execution-only `ActionInstance`, Reaction candidates, sampled rolls, and commit-side state
  materialization effects;
- returns `DclCanonicalComposedEvaluation` for the RNG-free dispatcher.

The converter covers the dispatcher families that already have native composition/evaluation
requests: PhysicalDamage, DirectNumeric, AreaNumeric, StatusApplication, StatusRemoval, Dispel,
Quick, Revive, and ForcedMovement.

## Evidence

- Direct native composition smoke now builds a composed evaluation from the captured native action
  and dispatches it through `DclCanonicalEvaluationDispatcher`.
- The smoke asserts no execution RNG is consumed and no native application is published.
- The Direct sentinel carries the required frozen Injury movement branch; missing map verdicts
  remain fail-closed rather than inferred.
- A negative smoke rejects conversion when an execution request carries Reaction candidates.

## Remaining

Native hook invocation and carrier-field writers for player forecast/AI remain gated. The next
live-facing step is to route native forecast/AI capture into this composer with production
family-policy providers.
