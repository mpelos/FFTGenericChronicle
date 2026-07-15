# LT27 DCL weapon line-of-fire live plan

## Purpose

Observe the native Arc and Direct projectile resolver results at their existing call sites. The key
contract is `candidateIdx == resultIdx`: true means the intended unit is the unit actually reached;
false means failure or interception.

## Safety contract

- Both hooks run after the native resolver returns and before the game's equality gate.
- They copy EAX/ESI and surrounding actor/order context into a mod-owned ring buffer.
- They never call Arc or Direct, write a target, change a candidate, or alter game flags/registers.
- Both sites require the same 17-byte expected sequence and fail closed independently.

## Minimal test sequence

1. Start through Reloaded-II, select Enhanced, press Enter to skip intro.
2. Load Manual Saves, first entry (`05`).
3. In battle, preview one unobstructed bow shot and one obstructed or intercepted bow shot.
4. Preview one unobstructed crossbow/gun shot and one obstructed or intercepted Direct shot.
5. Cancel each preview where possible; no save write is required.
6. Close the game normally.

## Required evidence

- Startup logs both `[DCL-WEAPON-LOF-HOOK] kind=Arc` and `kind=Direct` without skip/failure.
- Clear shots produce the intended target index with `included=True`.
- A blocked/intercepted case produces a negative/different result with `included=False`, or proves
  that the UI excludes the target before this resolver site and therefore needs a different fixture.
- Actor index, weapon ids, target coordinates, and action identity remain coherent across events.

## Interpretation boundary

This probe establishes the runtime meaning and cadence of the existing resolver return. It does not
yet wire LoS into Reaction `ConditionFormula`, synthesize Countershot, or prove that a reaction-
delivered basic attack traverses the same evaluator.
