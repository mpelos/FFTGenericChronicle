# DCL Fear pre-confirm actor probe checkpoint

## Trigger

The latest live attempt established two independent outcomes. Josephine's attack successfully
transformed the target into Chicken and did not crash. The process then became progressively slower
until the game had to be closed.

The progressive slowdown was already isolated to the Chicken dispatcher hook replaying the native
status test without preserving its conditional branch, which allowed non-Chicken units into Chicken
planning. The installed correction relocates the exact six-byte `test` plus `je`, preserves both
successors explicitly, and uses a stack-aligned managed frame.

## Pre-confirm investigation

Static call-graph analysis shows:

- affected-target builder `0x282754` direct callers: `0x281EC3`, `0xEF48AD6`;
- target helper `0x281C24` direct callers: `0x281E1A`, `0xEF53E92`;
- universal per-target calculator `0x3099AC` direct callers: `0x281F0D`, `0x307ED0`, `0xEF53F0F`.

The secondary builder caller is not the player forecast path. It consumes the first expanded target
and proceeds to reaction-order materialization. The player forecast implementation instead derives a
temporary caster order at `unit+0x1A0` and calculates the primary target only. Aggregating forecast
calculator fires is therefore not an exact AoE list.

At the voluntary-confirm call `0x20C55F`, `rbx` still contains the actor returned by `0x2607C0` at
`0x20C341`. The confirm shim now passes this actor to managed code and logs:

- actor pointer;
- linked unit at `actor+0x148`;
- executable and presentation ids at `+0x142/+0x18C`;
- target count/list at `+0x1A9/+0x1AA`, bounded to 21 entries.

This is observe-only. It does not use the actor list to reject an action yet, and the existing player
confirmation remains fail-open unless its earlier exact decision cache is valid. AI and execution
continue to use the proven native expanded-target bridge.

## Offline validation

- `python tools/analyze_dcl_fear_preconfirm.py --check-only`: pass.
- codemod release smoke tests: pass.
- `powershell -ExecutionPolicy Bypass -File codemod/run-offline-checks.ps1`: pass.

Windows still enumerates four old `FFT_enhanced.exe` process objects from earlier launches, but each
has zero threads, zero handles, and a 32 KiB working set. They are terminated process remnants rather
than live game instances and do not own deploy locks.

## Installed observation build

The release build was deployed with the existing live Fear profile after settings validation:

- installed DLL SHA-256: `41FBB15154EA03A9C4E8222490B6DF3AD6B61B17C102D3C705ECFAF7FD6AEE5B`;
- installed/source settings SHA-256: `5DCB667A7245FEC8F7EF73EDC720162B79E4279536E143C07B9F2D57792F1400`.

No new game session was launched during this checkpoint.

## Next falsifier

Capture the confirm row for one single-target action and one known AoE action. If the actor list is
current and contains all affected targets before the state transition, it becomes the exact player
authorization source. If it is empty, stale, or primary-only, the next candidate is a synchronous
forecast-time native builder call while the temporary order record is still staged.
