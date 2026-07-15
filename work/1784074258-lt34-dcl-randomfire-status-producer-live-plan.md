# LT34 DCL RandomFire status-producer live plan

## Purpose

Confirm the statically mapped RandomFire integration boundary with Celestial Void. The native engine
selects one target and calculates one result per repeat. DCL retains one Magic Evade decision per
target for the whole spell and produces one fresh seven-member random-one status contest per repeat.

## Profile and route

- Profile: `work/1784074257-battle-runtime-settings.lt34-dcl-randomfire-status-producer.json`.
- Start Reloaded-II and choose the Reloaded launch option.
- Choose Enhanced, press `Enter` during the intro, then Load, Manual Saves, first entry `05`.
- Use Celestial Void (`abilityId=173`) if the save exposes it naturally. Do not edit the save merely
  to force availability; defer the live observation if the action is absent.
- Forced status roll `18` and resistance `0` make the one selected packet member succeed unless
  native immunity blocks it.

## Required evidence

1. Startup reports the post-calc status producer at `0x281F12`.
2. Forecast produces no managed status decision.
3. Execution produces one RandomFire selection, one outer calculation, and at most one target apply
   for each native repeat, with continuation driven by repeat index/count.
4. Each repeated result produces one status plan containing all seven owned bits and exactly one
   member not marked `not-selected`.
5. The chosen packet member is reused at pre-clamp without a second status roll.
6. If one target is selected twice, its DCL Magic Evade decision is reused; a different target gets
   its own decision. The status packet remains fresh on both repeats.
7. HP/status presentation occurs once per repeated result, with no aggregate duplication.

## Failure interpretation

- More than one calculation or apply for one selector event refutes the mapped integration cadence.
- A new Magic Evade roll for the same target during the same spell means repeat retention failed.
- Reusing the same status choice/roll merely because the target repeats means status-plan retirement
  is too broad.
- Missing or duplicate packet commits mean the post-calc/pre-clamp boundary ordering is wrong.

Close with `Alt+F4` without saving after preserving the complete log span.
