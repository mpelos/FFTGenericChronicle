# LT21 — DCL Courage/Caution/Neutral reaction taxonomy

## Do not start yet

The unmodified Enhanced game must first reach the main menu. The profile is prepared but not deployed.
Save 05 must provide controllable/targetable units with Shirahadori, Counter, and Mana Shield for the
corresponding phases; skip an unavailable phase instead of silently substituting another reaction.

## Game route

Enhanced → Start Game → press Enter to skip the intro → Load → Manual Saves → first row, save 05.

## Preflight

- Validate the current executable anchors with `python tools/analyze_dcl_reaction_scope.py`.
- Validate the profile with the settings validator.
- Required install logs: calc-entry hook, reaction-taxonomy tail, and all four exact-id reaction gates.
- Record each test unit's visible Brave before acting. Any persistent Brave change is a hard fail.

## A — Caution / VM-internal Shirahadori

1. Attack the Shirahadori unit with a compatible direct physical attack.
2. At Brave `B`, the required virtualization log reports chance `100-B` for reaction id `451`.
3. Cancel/reopen forecast several times and verify visible Brave remains `B` after every calculation.
4. Execute several attacks. The stochastic outcomes must be compatible with the logged inverse chance.
5. For a deterministic A/B, restart once with only `ChanceFormula: "0"` on rule 451 (never catches),
   then once with only `ChanceFormula: "100"` (always catches). Restore the default afterward.

Hard fail: stuck/changed Brave, chicken state, an unbalanced entry/tail pattern, or unrelated forecast
amount changes caused by the temporary byte.

## B — Courage / exact real-code Counter gate

1. Provoke Counter with a normal connected attack.
2. Required log: `[DCL-REACTION-TAXONOMY-GATE ... reaction=442 mode=courage brave=B chance=...->B]`.
3. Confirm the engine still owns the counter animation/effect and the mod changed only the chance.

## C — Neutral diagnostic / exact real-code Mana Shield gate

This phase validates the Neutral flat-chance code path only. It is not the final Time Mage policy;
the final job specification classifies Mana Shield as Caution.

1. Damage the Mana Shield unit with a compatible attack/spell.
2. Required log: reaction id `445`, `mode=neutral`, `chance=...->50`.
3. Confirm successful triggers still redirect the native staged HP debit to MP exactly as before.

## D — DCL miss composition

1. Exit cleanly and change only `DclHitChanceFormula` from `100` to `0`.
2. Reload save 05 and attack the Counter unit.
3. The taxonomy gate may compute Courage first, but `[DCL-REACTION-GATE]` must then report
   `chance=<authored>->0 decision=miss`; Counter must not arm.
4. Restore the profile to `100` after capture.

## Evidence to retain

Copy the bounded runtime log and screenshots into new timestamp-prefixed `work/` files. Record the
reaction id, mode, real Brave, authored chance, DCL hit/miss decision, visible reaction outcome, and
post-action Brave. Do not promote the hybrid path from Strong until A–D pass without persistent state.
