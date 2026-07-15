# Fast-load atomic burst validation

## Purpose

Validate a load path that cannot lose the title-menu window to the restarting opening movie while
Codex is interpreting intermediate screenshots.

## Fixture

- Enhanced autosave container:
  `C:\Users\mmpel\OneDrive\Documentos\My Games\FINAL FANTASY TACTICS - The Ivalice Chronicles\Steam\76561198044337912\autoenhanced.png`
- SHA-256: `73DB59E2250507414FB61B5DE54EBA65DE009DA9122E0CB7271B9E3797D7A5B4`
- Starting screen: visible Enhanced/Classic selector at 1280x720.
- Reloaded-II stayed open after the game closed.

## Atomic sequence

The game received this entire sequence without an intermediate screenshot or interpretation yield:

1. Click Enhanced **Start Game** at `(360, 470)`.
2. Wait 4.2 seconds.
3. Press `Enter` to skip the opening.
4. Wait 1.6 seconds.
5. Click **Continue** at `(640, 578)`.
6. Wait 22 seconds before the first inspection.

## Result

- Continue click: 5.94 seconds after the Enhanced click.
- Loaded-state inspection: 27.99 seconds after the Enhanced click.
- Resulting state: the expected Ramza turn in the saved battle, with Ramza at full HP/MP and CT 100.
- No title-menu idle interval existed, so the opening movie could not restart before Continue.
- `Alt+F4` closed `FFT_enhanced.exe` without saving; process absence was verified and Reloaded-II
  remained running.

## Operational conclusion

Restoring the named autosave fixture plus the atomic Enhanced/skip/Continue burst is the default
repeated-test path. Manual **Load > Manual Saves > 05** is reserved for creating a new fixture or a
protocol that explicitly requires the manual-save baseline.
