# DCL common-unarmed mechanism

## Scope

This closes the job-free common-fist route only. Martial Arts skill growth, its job-derived damage
modifier, unarmed Parry, techniques, and job assignment remain excluded.

## Previous mismatch

Item id zero already mapped to weapon family zero and crush damage, but the generic physical spine
selected the swing table whenever a weapon was not Knife/Polearm. An empty hand therefore received
`sw(PA)` with zero weapon power. The DCL requires common unarmed to use thrust-like base PA, subtract
a small untrained-fist penalty, and then continue through crush DR with no wound multiplier.

## Implemented route

The physical mechanism profile now owns:

```text
dcl.isUnarmed = action.weapon.itemId == 0
dcl.base = dcl.isThrust || dcl.isUnarmed ? gurpsThr[ST] : gurpsSw[ST]
dcl.unarmedPenalty = dcl.isUnarmed * const.dclUntrainedFistPenalty
dcl.gross = max(0, dcl.base + dcl.wmod + dcl.overcapRaw - dcl.unarmedPenalty)
```

The fixture penalty is `1`; it is replaceable calibration, not a final balance value. Item zero has
weapon power zero, remains damage-type index `2` (crush), uses the armor's crush DR, and keeps wound
ratio `1/1`. The normal native-result-kind gate, Brave/Zodiac terms, penetration floor, execution,
and preview paths remain unchanged.

## Offline falsifiers

The formula smoke suite proves that item zero:

- sets `dcl.isUnarmed` without claiming thrust or missile family metadata;
- uses `gurpsThr` and differs from `gurpsSw` for the fixture ST;
- has zero weapon modifier and exactly one point of untrained penalty;
- traverses heavy armor crush DR with no cutting/impaling wound multiplier;
- survives both composition stages and produces the same model in execution and preview.

Validation passed:

- C# build: zero warnings/errors;
- compiled C# formula smoke executable: passed;
- integration and unified composition checks: current;
- unified settings/data pair: passed;
- unified settings SHA-256:
  `7FE4AC73D9843C3709EC7DFED16802F5C932BE4C6786E5BFCFBBC21BE0EF89FC`;
- whole-DCL coverage schema: 40 mechanisms, passed.

## Remaining gate

A live item-zero basic Attack should confirm that the native active-weapon carrier reaches formula
context as item id zero and that applied damage matches preview. This is an integration regression,
not an offline implementation blocker. Martial Arts remains outside this mechanism.
