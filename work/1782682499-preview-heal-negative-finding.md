# Preview Heal Negative Finding

Profile:

- `work/1782682203-battle-runtime-settings-preview-heal-negative-test.json`

Evidence:

- `work/1782682478-battleprobe-log-preview-heal-negative-result.txt`

Setup:

- Visual-only preview probe.
- `PreviewDamageForcedValue = 65436` (`0xFF9C`, two's-complement `-100`).
- `PreviewForecastSourceForcedValue = 65436`.
- No pre-clamp result rewrite.

Reported result:

- Physical Attack preview on Ramza: UI text/number showed a `100` HP recovery, but the HP ghost bar
  still showed roughly natural damage loss (`~168` HP).
- Fire preview on Ramza: UI text/number showed a `100` HP recovery, but the HP ghost bar showed a
  full HP loss (`567` HP).

Interpretation:

- The forecast display-number path interprets `0xFF9C` as signed `-100`, so it can render a heal-like
  number/label from a negative two's-complement value.
- The HP ghost bar does not treat negative `unit+0x1C4` as healing. It either keeps the natural debit
  when the source-field finalizer does not cover the action, or treats `0xFF9C` as an enormous
  unsigned debit and clamps the bar to empty when the source field is forced.
- Therefore coherent healing preview is probably not "write a negative debit to `+0x1C4`." It likely
  needs the heal/credit field (`unit+0x1C6`, i.e. forecast object `+0x8`) and/or forecast result
  metadata/flags to identify the event as healing.

Next useful probes:

1. Observe a natural Cure/Potion preview and compare `+0x1C4`, `+0x1C6`, `+0x1BE/+0x1C0`,
   `+0x1D8`, and `+0x1E5`.
2. Add a controlled preview-credit poke/finalizer test: force `+0x1C4 = 0` and `+0x1C6 = 100`, then
   check whether number and HP bar both show healing.
