# LT41D visible fast Dual Wield provenance result

## Outcome

The bounded live gate stopped before the required Choco Beak transaction. The corrected attack CT
aliases made Janus the next actor at CT 100, but Janus ended the turn without attacking Rion. The
game returned to Rion's command menu at full HP `277/277`; the raw log contains no Janus action,
calculation, HP event, synthetic-Reaction gate, or `442` row.

## Status evidence

The status-clear fixture did work at the intended memory surface. Rion's startup dump has effective
status byte `unit+0x63 = 0x20`, so the Invisible mask `0x10` is absent while Reraise remains present.
This refutes the explanation that a stale effective/master Invisible bit made Janus ignore Rion.

The narrower conclusion is that advancing Janus immediately after a passive Rion Wait does not
reproduce the AI decision reached in LT40 after Rion selected Auto-battle, threw a shuriken at
Wenyld, and Wenyld took her turn. A fast fixture must preserve that action history or checkpoint the
battle after Rion's actual Throw rather than approximate it with CT/status edits.

## Evidence and restoration

- Raw capture: `work/1784172968-lt41d-dcl-dual-wield-visible-fixture-no-attack-live.log`
- SHA-256: `24160DC3C22F4909A892E6B46E16C85BB11BBD84BA9F88406C8EF158E962A27C`
- Size: `17,953` bytes
- All six external test files were restored byte-for-byte from
  `C:\Users\mmpel\AppData\Local\Temp\fftgc-lt41d-dual-wield-1784172555`.

## Next bounded gate

Recreate the proven LT40 Auto-battle route against Wenyld, then close at the first actionable turn
boundary after Rion's Throw and snapshot the resulting autosave. Use that checkpoint for the
calc-provenance build so the live diagnostic begins with the decisive Wenyld/Janus sequence already
established.
