# LT31 DCL Reaction effect-boundary hook launch check

## Scope

Launch-only installation smoke for
`work/1784001361-battle-runtime-settings.lt31-dcl-reaction-effect-boundary.json`. No menu input,
save load, battle action, Reaction trigger, effect, status, or cadence write occurred.

## Result

- Static boundary analysis passed in
  `work/1784001331-dcl-reaction-effect-boundary-analysis.md`.
- Runtime settings validation passed with zero errors.
- Release build succeeded with zero warnings and zero errors; formula/runtime smoke tests passed.
- The current release DLL loaded through Reloaded-II.
- All three accepted-commit hooks installed.
- The state-`0x2C` effect probe installed with its expected-byte guard:

```text
[DCL-REACTION-EFFECT-HOOK] rva=0x212C2E addr=0x140212C2E maxLogs=128 expected=66 44 01 70 0C (observe-only)
```

- No assembler exception, AOB mismatch, hook failure, or crash appeared.
- `FFT_enhanced.exe` exited through `Process.CloseMainWindow()`.
- Installed settings were restored byte-for-byte to LT23.
- Installed and release-build DLL SHA-256 values both equal
  `AE37AC2FBC894CFCF970B6B749DF34B84CBFEEB5B413CA41C25D74B915A3FE05`.
- No `FFT_enhanced` process remained active.

## Boundary

This proves static placement, configuration loading, assembly, AOB validation, hook activation, and
clean shutdown only. Commit-to-effect cardinality and actor/source/target identity remain the LT31
behavior gate. The probe itself cannot apply a managed effect or consume cadence.
