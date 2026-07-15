# LT14 — DCL status add, resistance, and duration

## Objective

Validate the first formula-configured DCL status rule on the current Steam executable without changing native Attack data. The test separates four claims:

1. a connected ability can author a native status through the successful apply callback;
2. the mod-owned 3d6 resistance result gates that write;
3. equipment immunity rejects the write independently of the formula roll;
4. a one-target-turn duration expires on the target's turn-end signal and not earlier.

## Offline basis

- Current executable hook audit: 18/18 guarded anchors match.
- The successful outcome category `0x300` reaches the apply routine and its pre-clamp callback. This is Strong static evidence that successful status-only outcomes share the window; Attack is used first because its damage guarantees an observable traversal.
- Direct status authority is the durable master array `+0x1EF..+0x1F3` mirrored into effective `+0x61..+0x65`. Equipment/source `+0x57..+0x5B` is preserved on removal. Immunity is `+0x5C..+0x60`.
- Duration ownership counts the proven `+0x1B8` active-marker falling edge. Offline transition tests cover application while inactive, application during the target's active turn, multiple turns, and expiration.

## Profile and sequence

Profile: `1783974808-battle-runtime-settings.lt14-dcl-status-add.json`.

1. Launch Enhanced, skip the intro with Enter, choose Load, Manual Saves, and the first entry (save 05).
2. Confirm every required hook installs and no guarded anchor reports SKIP/FAILED.
3. Land a basic Attack on a target that is not already Blind. Forced status roll 18 vs resistance 10 must log `outcome=added`, set Blind, and preserve vanilla damage.
4. Verify Blind remains until the target completes its next turn, then look for `outcome=expired` and the native behavior/indicator disappearing.
5. Repeat with `DclStatusForcedRoll=3`. Roll 3 is at or below resistance 10, so it must log `outcome=resisted` and write neither status array.
6. If a convenient target has native Blind immunity, repeat the roll-18 case against it. It must log `outcome=immune` and perform no write.

## Pass gates

- No crash and no DCL/status callback errors.
- Connected Attack retains its natural HP damage.
- Roll 18 produces exactly one authored Blind application.
- The state does not expire during the attacker's/application turn.
- Duration 1 expires after exactly one completed turn belonging to the target.
- Roll 3 resists with no Blind state.
- Native cure/removal before expiry retires mod ownership rather than reapplying the state.

## Failure interpretation

- No `[DCL]` line: action-context/apply traversal is not reaching the managed pipeline on the updated executable.
- `[DCL]` but no `[DCL-STATUS]`: rule identity or validation mismatch.
- Logged add but no visible/native behavior: the effective/master write is too early or a downstream native stage overwrites it.
- Immediate expiration: the active-marker edge semantics or application-turn skip is wrong.
- Never expires: poll registration or active-marker observation is incomplete.
- Status survives logged expiration: another source/master owns the same bit and must not be cleared blindly.
