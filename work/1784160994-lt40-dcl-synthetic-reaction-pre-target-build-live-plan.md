# LT40 synthetic-Reaction pre-target-build live plan

## Question

Does rewriting carrier `443` at accepted selector RVA `0x2831BD`, before the target-list builder at
`0x2831C0`, deliver rewritten action `1/0` to the exact incoming source rather than retaining the
generic carrier's native self target?

## Offline prerequisites

- Static materialization analysis proves the order is complete at `0x2831BD` and the target-list
  builder is the immediately following call.
- Runtime validation requires exact RVA `0x2831BD` and exact bytes
  `48 8B CB E8 6F F0 FF FF 0F B7 35 24 7E 5E 01`.
- The complete offline suite, build, smoke tests, static hook scan, and profile validation pass.
- Game, Reloaded-II profile, deployed DLL/PDB/settings/log, and autosave are independently backed up
  while both processes are stopped.

## Bounded setup

- Profile: `work/1784160993-battle-runtime-settings.synthetic-reaction-pre-target-build-live.json`.
- Corrected carrier fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
- One live synthetic stage maximum; one live accepted-order rewrite maximum.
- Target Wenyld through **Auto-battle > Attack Enemy** using the verified runbook route. The forecast
  must show Throw Shuriken against Wenyld and no native Counter indicator.

## Required positive chain

1. Startup owner has equipped `443`, reaction-set `00000400`, and empty candidate slot.
2. One survived committed hit arms one mailbox for source `N`.
3. One pre-selector event reports `producer=synthetic-staged`.
4. The pre-target-build hook reports carrier `443`, original `0/443`, final `1/0`, target mode `5`,
   `targetIdx=N`, `rewrite=wrote`, and cumulative `rewriteWrites=1`.
5. One exact pass-2 native commit reports carrier `443`, agreeing ids, and source `N`.
6. One managed commit reports `cadence=consumed delivery=accepted-order-owned`.
7. State `0x2C` reports Reaction `443`, executable action `0`, source `N`, and `targets=[N]`.

## Immediate stop conditions

Close the game without saving after the first matching state-`0x2C` row. Also close immediately on
any hook skip/failure/loss, wrong native original, invalid source, capped/duplicate write, missing
rewrite ownership, second write, self-target effect, or mismatched source. Archive the complete log,
run all three strict analyzers, close Reloaded-II, and restore every backed-up external artifact to
its exact original SHA-256 before interpreting the result.
