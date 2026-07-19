# DCL Interrupt live closure

## Scope

This closes the job-free pending-action cancellation mechanism. Potion was only a temporary
existing-action carrier; this result makes no production ability or job assignment.

## Live matrix

| Control | Evidence | Result |
| --- | --- | --- |
| eligible log-only | `work/1784270980-dcl-interrupt-test-a-logonly-pass.log` | Exact pending Death identity observed without mutation. |
| resisted contest | `work/1784272118-dcl-interrupt-test-b-resisted-pass.log` | Roll `3 <= 14`; Death remained and resolved. |
| no pending | `work/1784273141-dcl-interrupt-test-c-no-pending-pass.log` | Same carrier returned `condition-false`; no write. |
| exact-ability mismatch | `work/1784274722-dcl-interrupt-test-d-ability-mismatch-pass.log` | High Potion `369` did not enter the Potion `368` rule; Death resolved. |
| successful cancellation | `work/1784277689-dcl-interrupt-test-e-cancelled-pass.log` | Timer became `255`, both Charging mirrors cleared, Death left the timeline and did not resolve. |
| bounded second match | `work/1784286980-dcl-interrupt-test-f-write-cap-pass.log` | First match wrote once; second match returned `write-cap`, preserved Death, and Death resolved. |

## Recovery and ordinary-action boundary

After a verified cancellation, the same Josephine unit later received turns, queued a fresh Death,
and executed it. The cancellation therefore leaves neither the unit nor the charged-action system
stuck. The exact-ability High Potion control is stricter than an ordinary-damage identity control:
it shares the same Item action family and legal target but still cannot enter the Potion-owned
rule. Ordinary basic Attack (`ability 0`) is rejected by the same earlier exact-id branch, which is
also covered by the offline rule-matching tests.

## Safety and limits

- Live mutation was capped at one verified write; the paired run proves the cap itself fails closed.
- Condition-false and resisted outcomes do not consume the write budget.
- Settings reload reconstructs runtime state and is not allowed between paired cap attempts.
- The forced one-point pre-clamp callback used late in the pair was only a survival harness. It
  does not establish production damage or instant-KO behavior.
- The test establishes the cancellation primitive and its fail-closed boundaries. Final ability
  ownership, resistance curve, and balance remain outside this mechanism closure.

## Verdict

The charged-action Interrupt transaction is **proven live** for one exact carrier and may be
integrated into the job-free unified mechanism profile. Future live work is regression, not a
missing primitive gate.
