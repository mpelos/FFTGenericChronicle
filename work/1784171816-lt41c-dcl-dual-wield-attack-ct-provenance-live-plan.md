# LT41C DCL Dual Wield attack-alias CT provenance live plan

## Question

Why does the managed DCL pre-clamp pipeline rewrite the first native Dual Wield strike but fail open
to vanilla on the second?

## Corrected fast fixture

- Runtime profile:
  `work/1784169839-battle-runtime-settings.dcl-dual-wield-provenance-live.json`.
- Autosave: `work/1784171803-dcl-dual-wield-fast-attack-ct-order-fixture.png`, SHA-256
  `02700D2FA95B00EAA55262FE88BFD77FF2ED8610F0ECB57C4D1BFD59C099D5F2`.
- Main/fturn live aliases retain the LT41B edits: Choco `77 -> 100`, Wenyld `84 -> 0`.
- The two attack aliases now also set Choco `0 -> 100`. LT41B showed that Continue loaded Choco CT
  `0`, matching those previously untouched attack members.
- Round-trip audit permits only the eight enumerated `unit+0x41` bytes and the CRC fields changed by
  FF16Tools; every other member and byte is preserved.

## Bounded action and stop rule

Use the atomic Continue path. End Rion's already-open turn with **Wait** and confirm facing. The speed
gate passes only if adjacent Choco receives the next action and uses Choco Beak against Rion. Stop on
the first contrary active unit or action. If Choco Beak connects, allow the owned `442` transaction
to finish and stop immediately after its second state-`0x2C` effect.

## Required diagnosis

For both Counter strikes correlate calc provenance, hit identity, `[DCL-PRECLAMP]` entry or explicit
guard/callback error, `[DCL]` rewrite or `[DCL-MISS]`, HP delta, and state-`0x2C` effect. Do not alter
the action-context cache unless the second strike has one unambiguous failure cause.

Back up and restore DLL, PDB, runtime settings, AppConfig, battle log, and autosave by verified hash.
