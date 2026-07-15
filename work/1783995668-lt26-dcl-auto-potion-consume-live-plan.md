# LT26 DCL Auto-Potion consumption live plan

## Purpose

Connect native Auto-Potion order `441` to the real-code inventory decrement at RVA `0x2816B2`.
The probe is observe-only and records ordinary item uses as negative controls.

## Safety contract

- The hook runs before the native `sub cl,bl` and inventory write.
- It preserves registers and flags and writes only to the mod-owned ring buffer.
- Expected bytes must match `2A CB 43 88 8C 05 00 7C 1A 01`; otherwise installation fails closed.
- No DCL formula, reaction synthesis, action replacement, or inventory control is enabled.

## Test sequence

1. Start through Reloaded-II and select Enhanced.
2. Press Enter to skip the intro.
3. Choose Load, Manual Saves, first entry (`05`).
4. In a controlled battle, spend one ordinary Potion-family item and retain its log as a control.
5. Trigger native Auto-Potion `441` on a living reactor that owns at least one eligible item.
6. Close the game through its normal UI after the capture.

## Required evidence

- Startup contains `[DCL-AUTOPOTION-CONSUME-HOOK]`, never `-SKIP` or `-FAILED`.
- Ordinary item use reaches the site with `autoPotion=False`.
- Auto-Potion reaches it with `actionId=441`, matching `itemId=selectedItemId`, and item id `240..242`.
- `inventory=old->old-1` and `decrement=1` for the Auto-Potion event.
- The reaction survives damage and executes after the triggering damage; KO behavior is recorded separately.

## Interpretation

- A matching `autoPotion=True` event proves the VM/order path reaches the native inventory decrement.
- No event does not refute consumption until LT23 also confirms that reaction `441` committed in the same test.
- A committed `441` with no matching consume event requires tracing the VM side-effect path before implementation.
