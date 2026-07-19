# DCL Interrupt Test B — resisted result

## Scope

This is the job-free forced-resistance branch from
`work/1784262418-dcl-interrupt-live-plan.md`.

Raw capture:

- `work/1784272118-dcl-interrupt-test-b-resisted-pass.log`
- SHA-256 `837E5D769FD0D09F12AB832AB85618A3E937ECBC8FA701C68448B9583E2F59F2`

## Result

Test B passes.

- Startup proves the shared post-calc hook installed with `interruptProducer=1`.
- Rion's native Potion reaches Josephine while Death is pending.
- Exactly one Interrupt decision is emitted with `resistance=14`, forced `roll=3`, and
  `outcome=resisted`.
- The rejected transaction still reads Death `action=30`, timer `9`, action type `0x0B`, effective
  Charging `0x08`, and master Charging `0x08`.
- No cancellation or write row is produced.
- Death remains in the Combat Timeline through two later Rion turns, then enters the native
  calculator as `abilityId=30`, plays its visible execution, knocks Josephine out, and leaves the
  timeline. Resistance therefore prevents the cancellation without corrupting the pending action.

## Next control

Test C must exercise the same Potion rule while Josephine has no pending action. It must emit
`outcome=no-pending-action` and perform no write.
