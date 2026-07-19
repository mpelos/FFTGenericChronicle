# DCL Fear forced-flee live attempt — invalid vanilla session

## Scope

The minimal Fear profile and the native selector/route coordinator were prepared for a first live
probe using the restored LT32 Josephine/Arthur autosave fixture.

## What happened

- A Reloaded-II launch installed the Fear hook successfully but its first Enhanced start returned
  to the version selector.
- A retry through `steam://rungameid/1004640` opened a playable Enhanced session and loaded the
  restored autosave.
- Josephine's basic Attack dealt the expected native 14 damage to Arthur and preserved Auto-Potion.
- Arthur's status-effects screen reported `None`; his turn opened normally without a forced move.

## Classification

This is **not** evidence against the Fear carrier or coordinator. The Steam-launched process did not
append a fresh runtime-harness header, hook-install line, status transaction, or coordinator event.
The copied log `work/1784374888-dcl-fear-forced-flee-live.log` contains only the earlier modded
process initialization. The playable retry was therefore vanilla and the probe was invalid.

## Operational conclusion

Use Reloaded-II's own **Launch Application** path and require a fresh runtime log header from the
current process before entering the battle. The Steam protocol shortcut is prohibited for modded
live probes. Multiple paced `F` pulses are a reliable fallback for frame-sampled confirmation when
menu navigation works but a single confirmation pulse is missed.
