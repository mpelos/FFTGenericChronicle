# DCL Auto-Potion checkpoint

## Static mechanism

- Native Reaction id is `441`.
- The eligible-item table at RVA `0x7154B8` is Potion `240`, Hi-Potion `241`, X-Potion `242`.
- Native selection scans in that order and picks the first owned item with nonzero quantity.
- The selected item is staged at reactor `+0x1A8`; the order uses type `6`; action id `441` remains
  at `+0x1A2`.
- A special `+0x1EE & 0x30` path selects Potion `240` without the normal inventory scan.
- The real-code item executor has a guarded inventory decrement at `0x2816B2..0x2816B4`.
- The final Chemist design does not author Item Lore, so the DCL does not add an external healing
  multiplier.

## Prepared runtime evidence

- LT23 observes all three accepted-Reaction commit passes and remains the installed first gate.
- LT26 observes the shared item decrement before the native subtraction. It records item id, old
  count, decrement, context/action/selected item, source index, and battle state.
- LT26 classifies an event as Auto-Potion only for action `441` plus a matching item id `240..242`.
- Build, settings validation, smoke tests, anchor audit, and launch-only assembler installation pass.

## Remaining live gates

1. Use LT23 to classify Auto-Potion's queue pass and confirm its accepted commit count.
2. Use LT26 with an ordinary item negative control and a native Auto-Potion trigger.
3. Prove `actionId=441`, matching selected item, and `inventory=old->old-1` at the decrement site.
4. Establish post-damage ordering, survivor-only behavior, and KO behavior.
5. Bind once-per-own-cycle cadence to an execution/effect commit, never chance or forecast.
