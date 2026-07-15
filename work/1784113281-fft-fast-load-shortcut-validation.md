# FFT Enhanced fast-load shortcut validation

## Goal

Remove avoidable UI navigation and eliminate the race in which the title intro restarts before an
unattended controller reaches **Load Game** or **Continue**.

## Offline evidence

- `FFT_enhanced.exe` contains and parses `/deactiveplay %d`.
- The formatter reference is at current-build RVA `0x1E643B`; the parser reference is at RVA
  `0x1E67FA`.
- The parser converts the following integer to a boolean. This is not an autoload or intro-skip
  switch.
- Reloaded-II supports `--launch <FFT_enhanced.exe>`, which preserves the application profile while
  bypassing navigation through Reloaded's **Configure Mods** page.

## Live validation

- `/deactiveplay 1` left the Enhanced/Classic selector visible and disabled the Classic start
  button. The selector-bypass hypothesis is refuted.
- Reloaded's `--launch` request started `FFT_enhanced.exe` directly from the configured profile.
- One uninterrupted Computer Use call performed: click Enhanced, wait 4.2 seconds, press Enter,
  wait 1.6 seconds, click Continue.
- The input burst completed in 5.938 seconds.
- The restored battle was visibly actionable about 14.94 seconds after the Enhanced click.
- The game was closed without saving.

## Preserved external state

- Enhanced autosave SHA-256 after the test:
  `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`.
- Reloaded `AppConfig.json` SHA-256 after exact restoration:
  `1AC3F6DD2FB38FBA2C65687CA9EC701B768425B318818F9E7338DFDC4C033B0B`.
- `FFT_enhanced.exe` and Reloaded-II were both closed after validation.

## Operational decision

Repeated tests use the named autosave plus `tools/launch_fft_enhanced_test.ps1`, then the single
Enhanced-to-Continue input burst. **Load Game > Manual Saves > 05** remains a one-time fixture
construction route, not the normal repeated-test route.
