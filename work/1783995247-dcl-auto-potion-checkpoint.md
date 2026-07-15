# DCL Auto-Potion checkpoint

## Static facts

- Native Reaction id: `441`.
- The pass-2 selector's eligible-item table at RVA `0x7154B8` contains exactly item ids
  `240/241/242`: Potion, Hi-Potion, X-Potion.
- Selection is first available in that order, based on the inventory count byte.
- The selected item id is stored at reactor `unit+0x1A8`.
- The typed order uses type `6`; the Reaction/action id remains `441` at `unit+0x1A2`.
- `unit+0x1EE & 0x30` selects Potion `240` through a special no-inventory-scan branch.
- A Strong real-code inventory mutation candidate reads the selected item count at `0x281692` and
  contains a one-unit decrement/write at `0x2816B2..0x2816B4`.
- Full anchors/table checks pass in `work/1783995093-dcl-auto-potion-analysis.md`.

## DCL implication

Potion-line restriction is already native and excludes Elixir, Phoenix Down, Remedy, and every
other item id. X-Potion eligibility is a tuning choice, not a missing technical mechanism. The final
Chemist design does not author an Item Lore support, so no DCL multiplier should be added.

## Remaining gates

- Classify Auto-Potion's queue pass with LT23.
- Prove post-damage timing and survivor-only behavior.
- Connect the VM-entry order helper to the real-code decrement candidate at runtime.
- Consume once-per-own-cycle cadence at the execution commit, never at chance/forecast evaluation.
- Confirm one item is consumed and the heal amount is the selected item's native amount without an
  external multiplier.
