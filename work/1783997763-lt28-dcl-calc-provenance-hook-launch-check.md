# LT28 calc-provenance hook launch check

## Scope

Launch-only installation smoke for
`work/1783997612-battle-runtime-settings.lt28-dcl-calc-provenance.json`.
No save was loaded and no battle behavior was exercised.

## Result

- Runtime settings validation passed with zero errors and the expected observe-only warning.
- The code mod built with zero warnings and zero errors.
- `FFT_enhanced.exe` started through Reloaded-II.
- The runtime log identified `DclCalcProvenanceProbeEnabled=True` and installed:

```text
[CALC-PROBE-HOOK] rva=0x3099AC ... probe=on ... ring=64 provenance=ON ...
```

- No `[CALC-PROBE-SKIP]`, `[CALC-PROBE-FAILED]`, runtime exception, or crash appeared.
- The process closed cleanly through `Process.CloseMainWindow()`.
- The installed LT23 reaction-commit profile was restored byte-for-byte afterward.
- No `FFT_enhanced` process remained active.

Source and installed DLL SHA-256 after the restore build:

```text
185FCE08DB7344FF893434B88118A61732A0D95CEC00B1457F25CCA1B1F2D1E6
```

## Boundary

This proves hook assembly, activation, logging configuration, clean launch, and clean shutdown only.
It does not prove caller classification, battle-state phase signatures, or Rend nested-calculation
semantics. Those remain the behavior portion of LT28.
