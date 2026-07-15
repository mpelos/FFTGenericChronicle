# DCL atomic HP/MP result flags checkpoint

## Question

Determine how `unit+0x1E5` represents simultaneous HP/MP changes and whether a managed partial Mana
Shield result can own its numeric channels and presentation atomically.

## Offline evidence

- Current-build literal writers establish the numeric high-nibble mapping: HP debit `0x80`, HP
  credit `0x40`, MP debit `0x20`, and MP credit `0x10`.
- Native actions emit combined `0x50` and `0x90`, proving the field is a bitwise bundle rather than an
  exclusive result enum.
- The real-code selector checks HP debit first, MP debit second, low-bit effects next, and the shared
  HP/MP credit route last.
- A partial HP+MP debit is therefore `0xA0`; its primary selector route is HP damage.
- No verified literal native `0xA0` writer exists, so exact popup plus Mana Shield reaction
  composition cannot be promoted beyond a live presentation gate.

## Implementation

`DclResultFlagsControlEnabled` derives the numeric high nibble from the final four staged channels.
`DclResultFlagsPreserveMask` accepts only `0x00..0x0F`, preventing stale native numeric bits from being
preserved. The flag is written after HP credit, MP debit, and MP credit in the existing pre-clamp
transaction. A failure restores all already-written channels and the old flag before failing open.

Pure smoke tests cover all four bits, stale-bit clearing, low-bit preservation, `0xA0`, native
`0x50`/`0x90` presentation priority, and validator gates. The integrated physical+magic scaffold now
enables result-flag ownership.

The complete offline gate passes in 36.5 seconds: Release build has zero warnings/errors, all C# and
Python smoke tests pass, all executable anchors pass, and all 27 runtime settings files validate.

## Remaining live proof

The bounded fixture rewrites only a natural Mana Shield redirect on character `0x1F` into a temporary
half split. One controlled hit must confirm exact one-time HP+MP deltas, `flags=0x20->0xA0`, HP damage
as the primary popup, and whether the native Mana Shield presentation still plays. The test does not
ratify the half-split ratio.
