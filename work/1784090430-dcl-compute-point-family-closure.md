# DCL compute-point numeric family closure

## Objective

Close the offline implementation gap between the LT36 HP-damage proof and the complete numeric
surface required by the DCL: HP debit, HP credit, MP debit, MP credit, authored misses, result flags,
and instant KO.

## Numeric transaction plan

`DclComputePointNumericPlan` is now the single pure planner between formula evaluation and memory
writes. It:

- preserves every natural channel that the current profile does not author;
- clamps authored channels to the signed staged-word range before result flags are composed;
- rebuilds the numeric result bits from the complete final four-channel bundle;
- records which channels actually require a write;
- zeros all four channels for an authored miss while deliberately retaining the connected native
  result byte until the downstream miss selector consumes it;
- keeps result-flag ownership optional and explicit.

The post-calc callback applies the plan atomically with its existing rollback path. Confirmed
execution still records the exact natural and final bundle once for pre-clamp delivery.

## Instant-KO composition

Instant KO no longer conflicts with the compute-point writer.

- AI evaluation does not sample a future execution roll. It evaluates the exact 3d6 resistance
  curve and publishes the expected debit: lethal debit weighted by success probability plus the
  authored failure debit weighted by resistance probability.
- Equipment KO immunity yields zero success probability.
- Confirmed execution rolls once, converts success into `current HP + staged HP credit`, and stores
  that final debit in the ordinary compute-point result cache.
- Pre-clamp skips the legacy instant-KO evaluator when a compute-point result exists, preventing a
  second roll.
- `DclLifecycle.ComputeResolvedInstantKoDebit` is shared by the new writer and the legacy fallback,
  so success, zero-on-failure, and preserve-damage-on-failure cannot diverge.

## Offline validation

The smoke suite covers:

- each individual HP/MP channel;
- all four channels in one composite result;
- preservation of natural unowned channels;
- negative and overflow clamps;
- forced-miss zeroing and native flag retention;
- disabled result-flag ownership;
- exact 3d6 instant-KO success permille;
- expected AI debit at 100%, 50%, 0%, and immunity;
- execution debit for kill, zero-on-failure, and preserve-on-failure;
- validator-clean composition of compute-point writer, hit/output control, and a data-neutralized
  instant-KO rule.

Build and smoke tests pass with zero compiler warnings or errors. Live validation remains necessary
for representative HP credit, MP debit/credit, and the data-neutralized Death vertical; no additional
offline ambiguity remains in the numeric planner itself.
