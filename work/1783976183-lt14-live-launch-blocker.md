# LT14 live attempt — base-game launch blocker

## Result

The LT14 status profile was built, validated, deployed, and initialized on the current executable. The runtime log confirms the current pre-clamp hook at `0x30A5D7`, calc-entry hook at `0x3099AC`, and battle-base hook at `0x226D20`. No guarded-hook skip, managed exception, or status error occurred before the game-mode transition.

The live battle did not begin. Selecting **Enhanced > Start Game** enters a black screen with the feather cursor and remains there. `Enter` and `F` return to the version selector; `Space` leaves the black screen unchanged. A no-input wait longer than two minutes did not advance.

## Isolation

The behavior reproduces in three launch paths:

1. official Reloaded-II `--launch` with the isolated mod set;
2. direct `FFT_enhanced.exe` launch without Reloaded injection;
3. Steam `steam://rungameid/1004640` launch without Reloaded injection.

Therefore the live result is **blocked before probe execution** and is not evidence against LT14 or the migrated hooks. The updated executable reports game version 1.5.1. Its local Denuvo `.psol` cache predates the executable update, but that is only a launch hypothesis; the cache was not modified.

`FFT_enhanced.exe` was closed with `Alt+F4` after evidence capture. Reloaded-II remains available. The deployed LT14 profile remains installed for the next launch attempt.

## Resume gate

Resume LT14 only after the unmodified base game reaches the Enhanced title/menu. Then relaunch through Reloaded-II, follow Load > Manual Saves > save 05, and execute the add/resist/duration sequence from `1783974808-lt14-dcl-status-live-plan.md`.
