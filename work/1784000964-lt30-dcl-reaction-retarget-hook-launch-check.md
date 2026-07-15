# LT30 DCL reaction-retarget hook launch check

## Scope

Launch-only installation smoke for
`work/1784000915-battle-runtime-settings.lt30-dcl-reaction-retarget-logonly.json`. No menu input,
save load, battle action, carrier production, retarget event, or effect delivery occurred.

## Result

- Runtime settings validation passed with zero errors.
- The release codemod built with zero warnings and zero errors; formula/runtime smoke tests passed.
- The current release DLL was deployed and loaded by Reloaded-II.
- The guarded pass-2 commit hook installed with the retarget branch armed log-only:

```text
[DCL-REACTION-COMMIT-HOOK] pass=2 rva=0x206421 addr=0x140206421 actor=rbx maxLogs=64 expected=40 88 B3 D3 01 00 00 replacement=off/log-only retarget=armed/log-only (guarded commit probe/control)
```

- No assembler exception, AOB mismatch, hook failure, or crash appeared.
- `FFT_enhanced.exe` exited through `Process.CloseMainWindow()`.
- The installed settings were restored byte-for-byte to LT23.
- The installed release DLL SHA-256 is
  `675E2D06A6EE11866520518800F937530EDC9431D7B5DB1EC09530B03A33D88F`.
- No `FFT_enhanced` process remained active.

## Boundary

This proves only configuration loading, assembly, AOB validation, hook activation, and clean
shutdown. Exact-id matching, source-index validity, native target-list contents, `would-write`
logging, and live target rewrite remain behind LT23/LT28 and the LT30 behavior plan.
