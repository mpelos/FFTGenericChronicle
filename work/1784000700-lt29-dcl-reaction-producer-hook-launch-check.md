# LT29 DCL reaction-producer hook launch check

## Scope

Launch-only installation smoke for
`work/1784000422-battle-runtime-settings.lt29-dcl-reaction-producer-logonly.json`. No menu input,
save load, battle action, candidate staging, or behavior test occurred.

## Result

- The current codemod DLL was deployed and loaded by Reloaded-II.
- The runtime settings line identified `DclReactionProducer=True/log-only`.
- All three LT23 commit hooks installed with their expected-byte guards.
- The pass-2 producer host installed successfully:

```text
[DCL-REACTION-PRESELECT-HOOK] rva=0x2063A9 addr=0x1402063A9 maxLogs=128 expected=48 8D 4D D2 E8 86 CA 07 00 producer=log-only
```

- No pre-selector failure, assembler exception, AOB mismatch, or crash appeared.
- `FFT_enhanced.exe` exited through `Process.CloseMainWindow()`.
- The installed settings were restored byte-for-byte to LT23.
- The installed and release-build DLL SHA-256 values both equal
  `220B95C4129945C53966237333A6A3892599E894C4490F18CE003503963B733C`.
- No `FFT_enhanced` process remained active.

The first polling attempt reported a timeout because it loaded the previously installed DLL. After
deploying the release build, a second poll raced the final log visibility and also reported a timeout,
but the post-close log contains the complete new runtime initialization and successful hook line
above. Both attempts restored LT23 in `finally`; neither entered a menu or battle.

## Boundary

This proves configuration loading, hook assembly, AOB validation, activation, and clean shutdown.
It does not prove pre-selector fire cadence, unit-index ownership, empty-slot observation, carrier
acceptance, source lifetime, retargeting, effect delivery, or cadence consumption. Those remain
behind LT23/LT28 and the LT29 behavior plan.
