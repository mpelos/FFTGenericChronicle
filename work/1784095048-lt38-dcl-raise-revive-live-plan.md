# LT38 DCL Raise/revive compute-point live plan

## Question

Can the unified compute-point writer replace a native revive action's staged HP credit while the
native KO-target packet still clears `Dead`, restores the unit, preserves turn ownership, and shows
the authored amount?

## Fixture

- Runtime profile: `work/1784095048-battle-runtime-settings.lt38-raise-revive.json`.
- Action data: `work/1784090960-lt37-death-neutralized.nxd`, required because the profile also owns
  Death ability `30`.
- Manual save: `work/1784095048-lt38-save05-death-raise-fixture.png`.
- Save 05 unit `6` has Death learned; unit `1` Arthur has Raise learned through two separately
  audited one-bit fixture steps.
- Death is forced to roll `18` only to construct a deterministic KO target. Raise ability `5`
  authors exactly `111` HP credit in execution and forecast.

## Procedure

1. With the game stopped, snapshot installed DLL, settings, action NXD, manual save, autosave, and
   AppConfig; record SHA-256 for every source.
2. Install the current Release DLL, LT38 settings, Death-neutralized NXD, and combined manual save.
   Keep only the mod loader and code mod active.
3. Load Manual Save 05 once and enter the same random encounter used by LT37 with Josephine and
   Arthur deployed. Reach Josephine's first actionable turn, close the game, and snapshot the
   resulting autosave as the repeated fixture.
4. Continue from that autosave. Josephine casts Death on herself and waits for the charged action.
   Require visible KO and HP `0` before proceeding.
5. On Arthur's turn, cast White Magicks > Raise on Josephine. Wait for resolution.
6. Require visible HP `111`, no KO state, normal standing presentation, and a later actionable unit
   turn. Capture screenshots before and after Raise.
7. Close the game, copy the complete runtime log to a timestamped `work/` file, run
   `tools/analyze_dcl_raise_revive.py`, then restore every pre-test artifact and verify hashes.

## Machine checks

The analyzer requires:

- forced Death execution and native `HP -> 0` for Josephine `0x81`;
- confirmed Raise execution from Arthur `0x80` with `hp=...->0/111`, `cached=1`;
- the DCL result row reporting `credit=111` and `computePoint=1`;
- poll evidence `HEALING ... 0 -> 111 = 111`;
- effective status transition `+0x61:20->00`;
- no legacy `[DCL-KO]`, compute-point rollback, or DCL error.

## Stop conditions

Stop without interpreting the result if the installed NXD is not the audited Death-neutralized
hash, the combined save does not expose both abilities, the expected hook line is absent, a target
other than Josephine is killed/raised, or any rollback/error line appears.
