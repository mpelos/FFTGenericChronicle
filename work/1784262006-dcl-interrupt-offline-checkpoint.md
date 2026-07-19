# DCL Interrupt Offline Checkpoint

## Scope

This checkpoint covers only the job-free DCL combat mechanism that lets an explicit Interrupt
ability cancel one target's charged action. It does not choose a production ability, skillset, or
job assignment.

## Hypothesis under test

A charged action becomes permanently non-runnable if one synchronous execution transaction:

1. writes the native cancelled timer sentinel `unit+0x18D = 0xFF`;
2. clears only Charging bit `0x08` from durable state `unit+0x1EF`;
3. clears only Charging bit `0x08` from effective state `unit+0x61`;
4. retains pending action type/id at `unit+0x1A1/+0x1A2` as history.

## Offline evidence

`tools/analyze_dcl_pending_cancellation.py` fail-closes on exact current-build bytes for:

- the pending predicate (`+0x61 & 0x08`, timer other than `0xFF`, action type branch);
- charge-timer authoring;
- native incapacitation cancellation through result bit `0x4000` and timer `0xFF`;
- normal charged-action resolution and status cleanup;
- scheduler readiness, countdown, and timer reconstruction;
- Song/Dance performance cleanup.

The exhaustive aligned-code field scan and guarded disassembly are in
`work/1784260860-dcl-pending-cancellation-analysis.md`. The analyzer's check-only mode passes against
the current executable.

The evidence supports the three-byte transaction strongly. It also explains why the native `0xF2`
and `0xF6` masks are unsafe as a generic Interrupt implementation: both clear other lifecycle bits
besides Charging.

## Implemented mechanism

`DclInterrupt.cs` owns:

- exact per-ability/action rule matching;
- fail-closed pending-state eligibility;
- log-only observation;
- the three-byte live transaction;
- immediate read-back verification that unrelated bits and action identity were preserved.

The outer-sweep execution callback in `Mod.cs` owns hit-negative control, 3d6 resistance, bounded
live writes, logging, and pending-tracker eviction only after a verified cancellation. Runtime
settings default to disabled, log-only, and a one-write cap. Validation requires DCL action context,
authored hit knowledge, the exact outer execution boundary, one unambiguous rule owner, and a valid
resistance formula.

## Offline validation completed

- main codemod Release build: pass, zero warnings/errors;
- formula/runtime smoke suite: pass;
- unmanaged-memory transaction tests: pass;
- log-only no-write test: pass;
- unrelated-bit and action-identity preservation test: pass;
- repeat cancellation, source-owned Charging, mirror mismatch, and `0xFF` sentinel negative tests:
  pass;
- valid/invalid runtime-settings tests: pass;
- static executable anchor check: pass.

## Remaining live gate

Static analysis cannot prove presentation cleanup or exclude a protected VM-side queue mirror. One
focused live fixture remains necessary. It must use a temporary existing ability only as a probe
carrier, without making a job or production-design decision.

Required sequence:

1. log-only: target a real charging unit and observe `eligible-log-only` with correct pending type/id;
2. resistance: force a resisting 3d6 roll and prove the queued action resolves normally;
3. no pending: use the same carrier on a non-charging target and prove no write;
4. live success with `DclInterruptMaxWrites=1`: force a failed resistance, observe exactly one
   verified cancellation, prove the queued action never resolves and Charging presentation clears;
5. recovery: let the interrupted unit receive a later turn and complete an ordinary action;
6. ordinary-damage control: damage a different charging unit without the Interrupt carrier and
   prove its action still resolves.

Until all six observations pass, coverage is `partial-live-gated`, not complete.
