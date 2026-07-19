# DCL Interrupt Test A — log-only result

## Scope

This is the job-free Test A branch from
`work/1784262418-dcl-interrupt-live-plan.md`. Native Potion is only the temporary
Interrupt carrier and native Death is only the observable charged action.

Raw capture:

- `work/1784270980-dcl-interrupt-test-a-logonly-pass.log`
- SHA-256 `97A7A6887701C28214856ABFD2E144422F45B7C820E04DA708BF6A600FDF51A3`

The first attempted launch is excluded because the Interrupt-only staged hook did not install; its
separate evidence and diagnosis remain in
`work/1784269211-dcl-interrupt-test-a-hook-install-failure.log` and
`work/1784269337-dcl-interrupt-hook-install-failure.md`.

## Result

Test A passes.

- Rion's native Potion on Josephine reaches the authored hit path as `ability=368`,
  `type=0x06`, `outcome=hit`.
- Exactly one Interrupt decision is produced:
  `resistance=14 roll=18 outcome=eligible-log-only`.
- The pending transaction is Death `action=30`, with timer `9`, effective Charging `0x08`, master
  Charging `0x08`, action type `0x0B`, and source `0x00`.
- `before` and `after` are byte-for-byte identical and `writes=0`.
- Death remains in the Combat Timeline after Potion and after Rion ends the turn.
- Death then reaches the native calculator as `abilityId=30`, its hit is produced, Josephine is
  visibly knocked out, and Death leaves the Combat Timeline. The log-only probe therefore did not
  cancel or corrupt the pending action.

The many Death calc/hit rows are native scheduler/forecast repetitions, not additional Interrupt
decisions. Interrupt cardinality remains exactly one.

## Next falsifier

Test B restores the same immutable pending fixture and forces `roll=3`. Resistance must reject the
transaction without a write, and Death must again resolve normally.
