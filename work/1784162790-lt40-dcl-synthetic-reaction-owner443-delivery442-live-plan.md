# LT40 synthetic-Reaction owner-443 / delivery-442 live plan

## Question

Can an exact equipped blank owner `443` reserve one successful-hit reaction while a separately
configured native delivery `442` materializes and executes one source-target basic retaliation?

## Why this gate differs from the failed gate

- Owner `443` supplies only equipped ownership and the configured taxonomy rule.
- The producer stages delivery `442`, not `443`.
- Counter `442` is already proven to traverse special materialization RVA `0x2831BD` with native
  order type `1`, payload `0`, and incoming-source target coordinates.
- No order rewrite is enabled. The test exercises native delivery semantics rather than trying to
  turn generic self-directed `443` into Counter by editing its order.

## Offline prerequisites

- Static selector analysis proves generic `443` skips `0x2831BD` via `0x283003 -> 0x2831CC` and
  special delivery `442` traverses `0x2831BD`.
- Runtime validation rejects generic delivery ids at this hook and requires owner/delivery separation.
- Build, smoke tests, Python analyzers, source gates, profile validation, and the complete offline
  suite pass.
- Game, Reloaded-II profile, DLL/PDB/settings/log, and autosave are independently backed up while
  both processes are stopped.

## Bounded setup

- Profile: `work/1784162789-battle-runtime-settings.synthetic-reaction-owner443-delivery442-live.json`.
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`.
- One live producer write maximum; no order-rewrite controller.
- Load through the verified Continue path and use **Auto-battle > Attack Enemy** against Wenyld.
- The forecast must show Throw Shuriken and no native Counter indicator.

## Required positive chain

1. Startup owner has equipped `443`, reaction set `00000400`, and empty candidate `unit+0x1CE`.
2. One survived committed hit logs `carrier=443 delivery=442`, source `N`, accepted mailbox.
3. One pre-selector event reports `producer=synthetic-staged` and the same owner/delivery pair.
4. One materialization row reports Reaction `442`, selected reactor `16`, source `N`, native action
   type `1`, payload `0`, target mode `5`, target `N`, `rewrite=none`, and
   `syntheticDelivery=owned`.
5. One exact pass-2 commit reports Reaction `442`, agreeing ids, and source `N`.
6. One managed commit reports owner `443`, delivery `442`, `cadence=consumed`, and
   `ownership=materialized-delivery-owned`.
7. State `0x2C` reports presentation Reaction `442`, executable action `0`, source `N`, and
   `targets=[N]`.

## Immediate stop conditions

Close without saving after the first matching state-`0x2C` row. Also stop on any hook
skip/failure/loss, native candidate collision, wrong owner/delivery/source, missing materialization
ownership, duplicate stage/commit, non-`1/0` materialized order, self-target effect, second write, or
unexpected native Counter before the accepted synthetic mailbox. Archive the full log, run all
strict analyzers, close Reloaded-II, and restore every external artifact to its exact independent
backup hash before interpretation.
