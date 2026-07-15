# DCL Reaction effect-boundary checkpoint

## Scope

This checkpoint extends the accepted-Reaction transaction from queue commit through post-VM
execution. It remains observe-only and does not choose Hex Ward's final Blind/Brave payoff.

## Offline result

- State `0x2B` owns two VM execution workers.
- State `0x2C` is the following real-code state and resolves the executed actor.
- RVA `0x212C2E` is the first instruction after that resolver, with the actor in `rax`.
- The boundary precedes the explicit `0x2C -> 0x2D` transition and real-code cleanup.
- Actor presentation id `+0x18C`, executable id `+0x142`, and target count/list
  `+0x1A9/+0x1AA` are still available.
- State `0x2D` cleanup later consumes the same executable id and target list.

The static analyzer passes every byte anchor in
`work/1784001331-dcl-reaction-effect-boundary-analysis.md`.

## Probe implementation

`DclReactionEffectProbeEnabled` installs an expected-byte-guarded observe-only hook at `0x212C2E`.
Each bounded `[DCL-REACTION-EFFECT]` row captures:

- battle state;
- actor pointer and unit index;
- presentation/reaction id `+0x18C`;
- executable/action id `+0x142`;
- incoming source index;
- target count and first eight target indices.

The probe performs no status/stat effect, target rewrite, cadence mutation, or game-memory write.

## Prepared evidence

- Profile: `work/1784001361-battle-runtime-settings.lt31-dcl-reaction-effect-boundary.json`.
- Protocol: `work/1784001361-lt31-dcl-reaction-effect-boundary-live-plan.md`.
- Hook-install smoke: `work/1784001411-lt31-dcl-reaction-effect-hook-launch-check.md`.
- Current reaction reports:
  - `work/1784001451-dcl-reaction-capabilities.md`;
  - `work/1784001451-dcl-reaction-implementation-manifest.md`.
- Current runtime anchor audit: `work/1784001451-runtime-hook-anchor-audit.md`, PASS 26/26.

## Verification

- Analyzer compiles and passes.
- Release codemod build succeeds with zero warnings and zero errors.
- Formula/runtime smoke tests pass, including invalid effect-probe settings.
- LT31 validates with zero errors.
- Reloaded-II assembles and activates the hook without AOB failure or exception.

## Installed safe state

- Installed settings remain byte-identical to LT23.
- Installed and release-build DLL SHA-256 values both equal
  `AE37AC2FBC894CFCF970B6B749DF34B84CBFEEB5B413CA41C25D74B915A3FE05`.
- `FFT_enhanced.exe` is not running.

## Remaining gate

One visible native Reaction must produce one accepted commit followed by exactly one matching
state-`0x2C` effect row. Only after that correlation can this boundary own managed effect delivery
and persistent cadence. LT23 and LT28 remain earlier prerequisites for pass ownership and
execution-only producer decisions.
