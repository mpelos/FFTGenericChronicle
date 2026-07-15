# LT20 — DCL instant KO through engine-owned lethal HP

Date: 2026-07-13

## Do not start yet

The unmodified Enhanced game must first reach the main menu. A black screen before the menu is a
base-game/startup blocker, not a combat result. Save 05 must expose ability id 30 (`Death`) on a
controllable unit; if it does not, stop and author a deterministic test loadout instead of testing a
different multi-status ability.

## Required data/runtime pair

1. Generate the action-data override with only Death neutralized:
   `python tools/build_neuter_data.py --dcl-instant-ko-neuter 30 --build-nxd`.
2. Deploy that data together with the matching code-mod build and
   `work/1783986506-battle-runtime-settings.lt20-dcl-instant-ko.json`.
3. Do not enable the profile without the data override. A native Dead rider surviving the DCL resist
   roll invalidates safety.

## Game route

Enhanced → Start Game → press Enter to skip the intro → Load → Manual Saves → first row, save 05.

## A — forced resistance

- Keep `DclStatusForcedRoll=3`.
- Cast Death on a living, non-KO-immune target.
- Required log: `[DCL-KO ... roll=3 outcome=resisted debit=0]`.
- Required game result: target remains alive, takes no damage, receives no partial Dead state, keeps
  normal CT/turn ownership, and can act afterward.
- Any death, untargetability, standing 0-HP zombie, or turn-list corruption is a hard fail.

## B — forced failure to resist

- Exit the battle cleanly, change only `DclStatusForcedRoll` to `18`, and reload save 05.
- Use a target whose inverse-Faith resistance is below 18.
- Required log: `[DCL-KO ... roll=18 outcome=engine-owned-ko debit=<current HP>]` (plus any same-hit
  credit if present).
- Required game result: coherent native KO — HP 0, prone/dead presentation, CT no longer grants a
  normal turn, targeting/immunity behavior matches vanilla death, and no zombie state.

## Evidence to retain

Copy the bounded runtime log and screenshots into new timestamp-prefixed `work/` files. Record the
target's HP, Faith, KO immunity, staged credit/debit, DCL resistance/roll, and post-action turn state.
