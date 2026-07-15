# DCL status transaction checkpoint

## Question

Determine whether DCL status control can preserve the native status carrier instead of relying on
direct durable/effective writes, and identify which actions can suppress their native rider through
data without losing the action result.

## Offline evidence

- Thirteen real-code formula handlers converge on the protected common status finalizer at
  `0x306988`.
- The selected target stages paired five-byte packets at `+0x1DB..+0x1DF` (add) and
  `+0x1E0..+0x1E4` (remove).
- The validator filters add bits against effective state and immunity and preserves
  innate/equipment source bits on remove.
- Ordinary result apply calls the native status transaction at `0x30C878`. Its consumers clear or
  OR the durable master bits, invoke native per-status consequences, and rebuild effective state.
- All 150 abilities with catalog status metadata fit the 368-row action override table.

## Data-first boundary

Thirty-two status records carry hostile HP AI metadata, but that metadata is not sufficient proof
of an independent ordinary damage result. After excluding instant KO, RandomFire, status-only,
self/caster, and unmapped custom carriers, 21 actions form the conservative ordinary
damage-plus-rider set.

`tools/build_neuter_data.py --dcl-status-rider-neuter <ids>` now accepts only those 21 ids and changes
only `InflictStatus=0`. It fails closed for status-only Petrify and dedicated instant-KO abilities.
The neuter smoke suite proves selective writes and preservation of every other override column.
Each `DclStatusRule` must now declare `NativeRiderPolicy=absent` or `suppressed-by-data`; missing or
unknown ownership fails settings validation.

## Remaining boundary

Clearing `InflictStatus` on a status-only action can erase its only result before the execution
commit is reached. The remaining families need their dedicated lifecycle/cardinality owner or a
managed packet producer after calc provenance distinguishes forecast, AI scoring, and execution.
They must not be converted into fake zero-damage actions.

The exact executable anchors and inventory are reproducible in
`work/1784017925-dcl-status-transaction-analysis.md`.

## Runtime implementation advance

The ordinary apply path reaches the existing managed pre-clamp callback before the mapped native
status validator/commit. The conservative 21-action subset therefore needs no additional hook:

- `DclStatusPacket.Compose` preserves unrelated add/remove bits and gives each authored status bit
  one owner across the paired packet.
- A successful add/remove is staged in exactly one half; immune or resisted outcomes clear the
  managed bit from both halves.
- Result bit `0x08` follows packet non-emptiness without disturbing numeric or unrelated effect
  bits.
- Packet writes occur inside the same fail-open rollback boundary as HP/MP debit and credit.
- The runtime no longer writes durable/effective status arrays for these action rules. Native
  validation, commit, source handling, presentation, and per-status side effects remain downstream.

Pure smoke tests cover add, remove, resistance, immunity, unrelated-bit preservation, result-bit
composition, invalid packet width, and duplicate packet ownership. Live validation remains bounded
to one ordinary damage-plus-rider action; status-only and special carriers remain outside this
mechanism.

The complete offline regression passes in 36.8 seconds with a clean C# build, all Python/C# smoke
tests, 27 established settings profiles, simulators, current-executable anchor scans, deploy-helper
dry runs, and the diff whitespace gate. The dedicated LT32 profile also validates independently.
No Reloaded deployment or game-state mutation occurs during this run.

Prepared live gate:

- `work/1784019532-battle-runtime-settings.lt32-dcl-native-status-packet.json`
- `work/1784019532-lt32-dcl-native-status-packet-live-plan.md`

The profile uses basic Attack with `NativeRiderPolicy=absent`, so it isolates packet production and
native commit without changing action data. Runtime logs include the staged add/remove byte and
final result flags for direct correlation with the visible Blind state.
