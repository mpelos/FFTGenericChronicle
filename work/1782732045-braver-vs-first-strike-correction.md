# Braver vs First Strike correction

## Corrected live result

The user clarified that the executed test was:

- Cloud -> Ninja with Braver.
- Preview: 100% hit chance, 182 HP damage.
- First Strike did not trigger before the action.
- Ninja took 182 damage and ended at 95/277 HP.
- The named action resolved normally.
- No critical/status/special effect was observed.

## Interpretation

The previous `first-strike-named-action-mismatch` note treated the log as inconsistent with the intended test. With the corrected manual result, the stabilized log actually matches the live outcome:

- Source/caster: Cloud-like unit `id=0x32`.
- Target: Ninja-like unit `id=0x80`.
- Action id: `257`, identified as Braver.
- Final HP event: Ninja `277 -> 95`, 182 damage.

This is valid evidence for named action identity and damage resolution, but it is not evidence about First Strike interception.

## Reaction test implication

Braver/Limit is not an appropriate First Strike trigger candidate for this investigation. A reaction/interruption test must use an incoming action that the game can actually react to, such as the basic Attack command for First Strike/Shirahadori-style tests, or a known reaction-compatible named action if one is identified later.
