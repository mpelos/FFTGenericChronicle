# DCL Fear: first safe-builder live result and Chicken slowdown diagnosis

## Observed battle

Josephine (table slot 17, character `0x81`) used Basic Attack against Arthur (slot 16, character
`0x80`). The game did not crash. Arthur visibly changed into a chicken. Performance then degraded
progressively until the game had to be closed manually.

The relevant runtime evidence is:

```text
[CALC] n=0 rec=0x141856080 casterSlot=17 casterIdx=17 type=0x01 abilityId=0 targetIdx=16 turnOwner=17
[DCL-FEAR-CONFIRM] casterIdx=0 turnOwner=17 type=0x00 ability=0 state=0x19 decision=allow
[CALC] n=1 rec=0x141856080 casterSlot=17 casterIdx=17 type=0x01 abilityId=0 targetIdx=16 turnOwner=17
[DCL-FEAR-TARGET] casterIdx=17 type=0x11 ability=0 state=0x2A owned=0 affected=1 opposing=0 decision=allow
[DCL-STATUS] rule=dcl-fear target=0x80 ability=0 byte=2 mask=0x04 resistance=9 roll=18 outcome=packet-add-staged packetAdd=0x04 packetRemove=0x00 flags=0x88
[DCL] caster=0x81 target=0x80 abilityId=0 actionType=0x01 result=14 debit=14 flags=0x80->0x88
[DAMAGE ptr=0x141855CE0 id=0x80] 199 -> 185 = 14
```

The process remained alive after action execution and emitted no WER crash event. The log contained
30 lines and only one valid target callback; logging volume was not the slowdown source.

## Proven results

- The exact relative target-builder hook at `0x281EC3` survives player confirmation and result
  execution.
- The target callback resolves the correct caster slot and completed one-target list.
- The ordinary status transaction stages and visually applies Chicken byte 2/mask `0x04` without
  ability 242 or a job assignment.
- The calculation/order record stores caster index at `+0`, action type at `+1`, and ability id at
  `+2`. The logged `type=0x11` was the caster index misread as action type; runtime reading now uses
  `+1`.
- The expanded-target builder does not supply the tested direct-Attack forecast before confirmation.
  The calculation-entry ring fires before `[DCL-FEAR-CONFIRM]`, while the target callback fires only
  later in execution state `0x2A`. The confirmation cache therefore remains default/fail-open.

## Slowdown root cause

The installed Chicken dispatcher hook at `0x38BC37` had two independent native-contract defects.

First, the native boundary is six bytes:

```text
38BC37  F6 47 63 04  test byte [rdi+63h],04h
38BC3B  74 47        je 0x38BC84
38BC3D  E8 ...       Chicken selector
```

The hook was created with default length at the four-byte test. Its custom code replayed the test but
did not reproduce the conditional branch. Regardless of the trampoline span chosen by Reloaded,
normal mode could fall through to the Chicken successor instead of routing clear predicates to
`0x38BC84`. This allows non-Chicken units to enter Chicken planning and explains progressive battle
degradation as more units are processed.

Second, function `0x38BBFC..0x38BF2D` leaves `rsp` 16-byte aligned at `0x38BC37`. Eight wrapper saves
preserve that alignment, but the old `sub rsp,0x88` misaligned every managed ownership callback by
eight bytes.

## Corrected dispatcher contract

The dispatcher now uses an exact six-byte relative `DoNotExecuteOriginal` hook. Callback mode zero
replays the predicate and routes explicitly to native Chicken successor `0x38BC3D` or non-Chicken
successor `0x38BC84`. Owned forced-route success returns through `0x38BF14`. The managed wrapper uses
an aligned `0x90` frame and keeps its result at `+0x80`.

Smoke tests enforce hook length/options, the two native successors, the handled successor, three
explicit trampolines, and absence of a `0x88` frame. The runtime byte guard is now the complete pair
`F6 47 63 04 74 47`.

## Offline validation

- Fear flee-route analyzer: PASS
- Fear mechanism analyzer: PASS
- Release build: PASS, zero warnings/errors
- codemod smoke tests: PASS
- complete `codemod/run-offline-checks.ps1`: PASS
- corrected build SHA-256: `56D7F41C6082BA2E98F0D2ED80E4F44C8A72338E248BEDB11E19223BFBC9954B`

## Next gates

1. Install only after the previous game process is fully closed.
2. Run an A/B stability slice long enough for several non-Chicken unit turns before applying Fear.
3. Repeat Josephine-to-Arthur application and verify stable frame pacing after the visual Chicken
   transition.
4. Do not attempt voluntary-target rejection until the pre-confirm calculation-entry forecast is
   converted into an exact, freshness-bounded Fear decision.
