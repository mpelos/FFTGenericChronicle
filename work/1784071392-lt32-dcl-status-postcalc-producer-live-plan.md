# LT32 DCL post-calc status-producer live plan

## Purpose

Validate one formula-`0x0B` status-only action whose native handler can skip packet production.
The runtime must replace that conditional native result only on confirmed execution and feed the
game's ordinary status validator/committer with one complete managed packet.

## Profile and setup

- Profile: `work/1784071391-battle-runtime-settings.lt32-dcl-status-postcalc-producer.json`.
- Save route: Enhanced, `Enter` to skip the intro, Load, Manual Saves, first entry `05`.
- Test action: Wall (`abilityId=13`) against a living target without Protect/Shell immunity.
- The profile forces the managed 3d6 roll to `18` and uses resistance `0`, so the DCL contest must
  succeed. It performs no data override: native forecast/AI behavior remains available.

## Required evidence

1. Startup reports `[POST-CALC-HOOK] ... statusProducer=1` with RVA `0x281F12`.
2. Opening/canceling the forecast produces no `[DCL-STATUS-PRODUCER]` row and consumes no status roll.
3. Confirmed execution produces exactly one row with `origin=outer-sweep`, `battleState=0x2A`,
   `writes=2`, `carriesResult=1`, and `suppressedByHit=0`.
4. Exactly one Protect and one Shell packet row share the forced roll `18` and show staged add bits
   byte 3 masks `0x20` and `0x10`.
5. The target visibly receives both Protect and Shell through native application. No duplicate
   animation, second roll, or second producer row is accepted.

## Failure interpretation

- Missing hook: expected-byte/wrapper installation failure; do not test the action.
- Forecast producer row: execution gate failure; close without saving and disable the profile.
- Producer row but no pre-clamp/status rows: packet result does not reopen the apply carrier.
- Two rolls or duplicated status rows: prepared-plan reuse failed.
- Correct packet rows but no visible statuses: native validator/committer integration failed.

Close the game with `Alt+F4` without saving after evidence capture.
