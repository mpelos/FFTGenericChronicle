# LT27 weapon line-of-fire hook launch check

## Scope

Launch-only smoke through Reloaded-II with LT27. No version was selected, no save was loaded, and no
weapon target was evaluated.

## Result

- `FFT_enhanced.exe` started responsive.
- Arc installed at RVA `0x28030B` with the configured 17-byte guard.
- Direct installed at RVA `0x2803A3` with the same guard.
- Neither site logged `SKIP`, `FAILED`, or a runtime error.
- The game closed through `CloseMainWindow()` and exited normally.

This proves expected-byte validation and runtime assembly/installation for both observe-only hooks.
It does not prove behavioral `candidateIdx/resultIdx` values.

## Restored state

- Installed profile is `work/1783992200-battle-runtime-settings.lt23-dcl-reaction-commit.json` and
  matches it byte-for-byte.
- Source/installed DLL SHA-256:
  `7DBB0F34875D6E9C900AB6D5C4A6FE78ACC8C6DB8AC50C8D13CDF5E7022720B7`.
- No `FFT_enhanced.exe` process remains.
