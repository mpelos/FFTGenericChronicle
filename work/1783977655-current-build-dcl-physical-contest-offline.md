# DCL physical contest — offline implementation checkpoint

## Implemented slice

The code mod now contains a formula-configured physical contest with two independent 3d6 rolls:

1. the attack connects when the attack roll is at or below skill, except for the authored critical/fumble edges;
2. a connected non-critical attack is stopped when the defense roll is at or below the best available defense;
3. critical rolls 3/4 always, 5 at skill 15+, and 6 at skill 16+ bypass defense;
4. fumble 18 always, and 17 at skill 15 or lower, automatically misses;
5. Block wins defense-value ties, then Parry, then non-depleting Dodge;
6. finite Block/Parry uses spend once in the successful apply window and refresh on the defender's `+0x1B8` own-turn rising edge;
7. forecast hit percent is enumerated exactly over all 3d6 attack and defense outcomes.

The physical contest has its own applicability formula. Actions outside that formula use the existing `DclHitChanceFormula` fallback, preventing the physical defense model from being applied indiscriminately to magic/status action families. The first live profile scopes the slice to native basic Attack (`action.type == 1`).

The formula context now exposes raw position/facing, turn/action-owner state, all five source/immunity/effective/master status bytes, named native-status aliases, and elemental absorb/null/halve/weak/strengthen masks. Guard-state variables are available to defense-value and defense-policy formulas after the finite pool has been initialized:

- `guard.parryRemaining`, `guard.parryMax`
- `guard.blockRemaining`, `guard.blockMax`

Capacity formulas deliberately cannot read `guard.*`, avoiding a recursive definition of pool size.

## Offline gates passed

- Release build: zero warnings, zero errors.
- Full formula/runtime smoke suite: passed.
- Outcome tests cover ordinary miss, fumble, defended hit, failed defense, and critical defense bypass.
- Probability tests establish the expected monotonic ordering: no defense > Dodge > stronger finite guard.
- Skill 12 versus Dodge 8 remains within the DCL attacker-favored baseline band.
- Guard tests cover one-use spending, no double-spend, own-turn refresh, repeated active samples, and unit-pointer ownership replacement.
- Settings validation covers every physical formula, forced-roll ranges, hit/miss/reaction dependencies, applicability scoping, and non-recursive guard capacities.

## Important unresolved boundaries

These are not treated as proven by the offline implementation:

- the `+0x1B8` rising edge must be confirmed live as the correct full-guard refresh moment;
- the successful apply callback must be confirmed to spend one guard use and never spend during preview or AI evaluation;
- multiple cached previews or charged actions can observe a guard charge before another action spends it; stale availability at eventual execution needs a live architecture decision;
- multi-hit actions need one independent contest and one guard spend per strike, but the present per-target action-key cache may reuse or retire the decision at the wrong granularity;
- Parry/Block need distinct presentation/animation rather than the generic authored miss output;
- facing byte compass mapping and front/side/back classification remain unproven;
- the final physical-action taxonomy must replace the basic-Attack-only applicability formula.

LT14 status validation remains the next live test. LT15 is prepared but must not replace LT14 until the unmodified Enhanced game can reach its title/menu and LT14 has been completed.
