# LT41B DCL Dual Wield fast provenance live plan

## Question

Why does the managed DCL pre-clamp pipeline rewrite the first native Dual Wield strike but fail open
to vanilla on the second?

## Changes from interrupted LT41

- Runtime profile remains
  `work/1784169839-battle-runtime-settings.dcl-dual-wield-provenance-live.json`.
- The build adds provenance-only `[DCL-PRECLAMP]`, `[DCL-PRECLAMP-GUARD]`, and
  `[DCL-PRECLAMP-ERR]` rows. They are emitted only while the calc-provenance probe is enabled.
- Autosave becomes `work/1784171084-dcl-dual-wield-fast-ct-order-fixture.png`, SHA-256
  `BBBD6A9AAEC5F0436C6EF0FE840392AEBB38A7191E3F43BAF7AD2D69C29C5DAE`.
- The audited fixture changes only live `unit+0x41` CT bytes: adjacent Choco `77 -> 100` and distant
  Wenyld `84 -> 0`. This is intended to put Choco first after Rion ends his already-open turn.

## Bounded action

1. Use the atomic Enhanced-to-Continue fast path.
2. On Rion's already-open command menu, choose **Wait**, confirm, and confirm facing.
3. Allow only the next enemy action. The fixture passes its speed gate only if Choco acts before
   Wenyld and attacks Rion.
4. Stop immediately after either two Reaction `442` state-`0x2C` effects or the first contrary
   action. Do not continue the battle to compensate for a failed turn-order hypothesis.

## Required diagnosis

For each of the two Counter strikes, correlate:

1. `[DCL-CALC-PROVENANCE]` return RVA, battle state, caster `16`, target `0`, action `1/0`, payload;
2. `[DCL-HIT]` identity;
3. `[DCL-PRECLAMP]` entry or an explicit guard/callback error;
4. `[DCL]` rewrite or `[DCL-MISS]` reason;
5. HP delta and state-`0x2C` effect.

The run is diagnostic only. Do not alter the action-context cache until the second strike has one
unambiguous failure cause.

## Restoration

Before deployment, back up DLL, PDB, runtime settings, Reloaded AppConfig, battle log, and autosave.
After the bounded stop, close FFT and Reloaded-II, archive the log, restore all six files, and verify
their pre-test SHA-256 hashes before interpreting the capture.
