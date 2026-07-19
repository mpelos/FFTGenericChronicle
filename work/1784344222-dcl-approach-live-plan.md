# DCL Approach native bridge live plan

## Fixture and scope

- Restore `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png` only while
  `FFT_enhanced.exe` is stopped.
- The fixture equips Rion with reserved owner carrier `443`. Delivery `442` is the live-proven native
  basic weapon order; the profile keeps all damage at one and hit delivery deterministic.
- Let an opposing unit enter Rion's existing weapon reach during ordinary movement. The expected
  shortest natural case is the Chocobo approaching Rion.
- This test validates movement control only. It does not validate a job, balance value, final owner
  assignment, or final Approach visual treatment.

## Required install evidence

- Boundary hook: `0x1FE793`.
- Final-step completion guard: `0x211E09`.
- Terminal resume hook: `0xD7D0A81`.
- Pass-2 commit, delivery-validation, accepted-materialization, and state-`0x2C` effect hooks.
- No hook install failure, expected-byte mismatch, or competing Landmark hook.

## Pass criteria

1. Ordinary route boundaries establish a baseline without queueing on the origin or repeated tile.
2. Exactly one outside-to-inside edge identifies Rion as the equipped reactor and stages delivery
   `442` in an otherwise empty mailbox set.
3. The direct pass-2 queue returns accepted and produces an exact commit bit for Rion/source mover.
4. Native Reaction execution completes, then the terminal shim writes `0x11` exactly once.
5. Managed resume audit reports `pass`; the same mover and unchanged route/cursor continue normally.
6. The moving enemy completes its intended movement/turn after the Stop-hit rather than stalling,
   teleporting, repeating a tile, or yielding control to Rion permanently.
7. Commit/materialization/effect cardinality agrees with one native Reaction transaction.

## Hard stops

- Game crash, frozen battle state, route discontinuity, or wrong moving unit after resume.
- `[DCL-APPROACH-ABORT]`, negative native outcome, `audit=mismatch`, commit mask zero, or more than one
  continuation write.
- Any delivery or effect without exact producer ownership.
- Any unexpected job/data behavior; this profile changes no job definition or item SKU.

After capture, close the game, archive the log, restore the installed DLL/PDB/settings and autosave
from the independent pre-live backup, and write a dated result before promoting any fact to `docs/`.
