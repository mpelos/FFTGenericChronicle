# LT32 — managed native status-packet proof

## Objective

Prove that the ordinary DCL pre-clamp callback can stage one authored status bit in the game's
paired native packet while preserving vanilla HP damage, and that the later native
validator/committer produces the real durable/effective status and presentation.

This is a single-action vertical slice. It does not test status-only carriers, RandomFire,
data-neutralized native riders, or duration expiry.

## Offline basis

- The current executable's packet finalizer, immunity validator, ordinary apply call, commit, and
  add/remove consumers pass all guarded static checks.
- The pre-clamp hook occurs before the ordinary apply path calls the native status commit.
- `DclStatusPacket.Compose` preserves unrelated packet bits, removes conflicting ownership from the
  opposite half, and derives result bit `0x08` from packet non-emptiness.
- Numeric and packet writes share one rollback boundary.
- Basic Attack has an independent HP result and no native status rider, so no NXD mutation is
  required for this first proof.

Profile: `work/1784019532-battle-runtime-settings.lt32-dcl-native-status-packet.json`.

## Startup and fixture

1. Launch FFT through Reloaded-II and select Reloaded/Enhanced.
2. Press Enter to skip the intro.
3. Choose Load > Manual Saves > first entry, save 05.
4. Confirm the DCL pre-clamp hook installs with no `SKIP` or `FAILED` line.
5. Choose a target that is alive, not already Blind, and can survive one basic Attack when practical.

## Single write test

1. Land exactly one basic Attack on the chosen target.
2. Preserve the natural HP damage and normal hit presentation.
3. Require one status log with:

   ```text
   ability=0 byte=1 mask=0x20 resistance=10 roll=18
   outcome=packet-add-staged packetAdd=0x20 packetRemove=0x00
   ```

   `packetAdd` may contain unrelated bits in addition to `0x20`; the owned bit must be set and the
   same bit must be absent from `packetRemove`. Result flags must retain the numeric HP-damage bit
   and include status bit `0x08`.
4. Confirm Blind becomes active through native behavior/presentation after the hit.
5. Close the game with `Alt+F4` and verify `FFT_enhanced.exe` stops.

## Pass gates

- No crash, callback exception, guarded-hook failure, or staged-write error.
- Exactly one connected Attack produces exactly one `packet-add-staged` line for the target.
- Vanilla Attack damage remains nonzero and is applied once.
- The final packet has add bit `byte 1 / 0x20`, no matching remove bit, and result bit `0x08`.
- Blind is present after the native commit; the runtime never writes the durable/effective arrays
  during action delivery.

## Failure interpretation

- DCL damage log but no status line: rule/action identity did not match.
- Packet log is correct but Blind is absent: the mapped ordering or downstream native commit
  assumption is wrong; capture the ordinary apply/commit boundary before any new write.
- Blind appears but HP damage disappears or repeats: packet production disturbed the ordinary
  numeric transaction or apply cardinality.
- More than one status log for one hit: pre-clamp cardinality needs an idempotence key before this
  carrier can be promoted.

Do not deploy or run this profile without a working privileged Computer Use channel or an awake
operator. Blind keyboard injection is not an acceptable substitute.
