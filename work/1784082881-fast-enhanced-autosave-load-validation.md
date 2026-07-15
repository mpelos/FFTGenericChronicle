# Fast Enhanced autosave load validation

## Objective

Remove the fragile repeated navigation through **Load Game > save tab > save row**, especially the
failure mode where the title-menu intro restarts while the automation is inspecting the screen.

## Validated mechanism

- Enhanced autosaves are stored in
  `C:\Users\mmpel\OneDrive\Documentos\My Games\FINAL FANTASY TACTICS - The Ivalice Chronicles\Steam\76561198044337912\autoenhanced.png`.
- `tools/manage_fft_enhanced_autosave.ps1` snapshots and restores that container only while the game
  is stopped, backs up the displaced container, and verifies SHA-256 after restore.
- The validation fixture SHA-256 was
  `449D00E21EEE656B9022F90042ECA7F45079A0C96F68D66EE51785A24616D550`.
- The restored fixture was launched through Reloaded-II twice and **Continue** returned to Ramza's
  turn in the same chocobo battle both times.

## Reliable unattended timing

1. Choose **Enhanced > Start Game**.
2. Wait about 4.2 seconds for the opening movie, then press `Enter`.
3. Wait about 1.6 seconds without requesting a screenshot.
4. Click **Continue** directly. For the 1280x720 game window, Computer Use input `(640, 578)` hits
   the visible Continue row.
5. Wait about 22 seconds before inspecting the loaded state.

The measured timed segment from the Enhanced click through the loaded-state screenshot was 27,982
ms. Keyboard confirmation was inconsistent on this particular title menu; the direct Continue
click is the verified path.

## Operational consequence

Manual save 05 remains the starting point for creating a new fixture. Once a protocol reaches its
exact pre-battle or mid-battle state, capture `autoenhanced.png` once and restore it before every
baseline/forced repetition. This removes random encounter rerolls and makes probe comparisons start
from an identical game state.
