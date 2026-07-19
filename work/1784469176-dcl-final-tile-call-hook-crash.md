# DCL final-tile five-byte call-hook crash

## Scope

The observe-only final-tile producer was live-tested with `ExecuteAfter` over the exact five-byte
terminal finalizer call at RVA `0x1FE93B`. The player selected a movement destination and confirmed
it. The game crashed before any managed final-tile row was drained.

## Evidence

- The runtime log installed `[DCL-FINAL-TILE-HOOK]` at RVA `0x1FE93B` and contained no
  `[DCL-FINAL-TILE]` row before process termination.
- Windows recorded `0xC0000005` with exception address `0x0` and execute flag `8`.
- Dump `C:\Users\mmpel\AppData\Local\CrashDumps\FFT_enhanced.exe.57368.dmp` identifies faulting
  thread `0x1086C`, `RIP=0`, and access information `[8, 0]`.
- The original terminal call is the only direct caller of finalizer thunk `0x1FD2D0`; the vanilla
  game completes the same movement when the hook is absent.

## Conclusion

The `ExecuteAfter` hook that relocates only the terminal five-byte `call` is rejected. The failure
is control-flow/return corruption, not a movement-data validation failure. Runtime settings
validation permanently rejects RVA `0x1FE93B` for the final-tile producer.

The replacement boundary is the finalizer's single convergence point at RVA `0xD45A2A2`, after
canonical position synchronization and before the security-cookie check and epilogue. It uses
`ExecuteFirst` with instruction-length selection left to Reloaded Hooks; it does not relocate the
terminal call.

## Safety state

The installed runtime settings were immediately replaced with a profile that omits
`DclFinalTileEventProbeEnabled`, so the rejected hook is not armed for the next launch.
