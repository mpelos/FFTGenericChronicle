# LT26 Auto-Potion consumption hook launch check

## Scope

Launch-only smoke through Reloaded-II with the LT26 observe-only profile. No version was selected,
no save was loaded, and no battle behavior was exercised.

## Result

- `FFT_enhanced.exe` started and remained responsive.
- Runtime settings reported `DclAutoPotionConsumeProbeEnabled=True`.
- The runtime logged:
  `[DCL-AUTOPOTION-CONSUME-HOOK] rva=0x2816B2 addr=0x1402816B2 maxLogs=128 expected=2A CB 43 88 8C 05 00 7C 1A 01 (observe-only)`.
- No `DCL-AUTOPOTION-CONSUME-SKIP`, `-FAILED`, or runtime error appeared.
- The game closed through `CloseMainWindow()` and exited normally.

This proves expected-byte validation and runtime assembly/installation for the hook. It does not
prove that Auto-Potion reaches the decrement site or consumes exactly one item.

## Restored state

- Installed runtime profile: `work/1783992200-battle-runtime-settings.lt23-dcl-reaction-commit.json`.
- Installed settings match that source byte-for-byte.
- Source/installed DLL SHA-256:
  `636237698F7DCC6DCCFA4FD88584A863E8C161775C9E103B67321F689B749400`.
- No `FFT_enhanced.exe` process remains.
