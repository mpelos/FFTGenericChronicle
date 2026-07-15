# DCL reaction queue / carrier checkpoint

## Scope

This checkpoint continues the reaction-mechanism track after the accepted-commit candidate and the
first action-replacement vertical slice. It separates static proof, prepared runtime controls, and
live evidence that is still pending.

## Static results

- The queue function `0x206344` has one direct caller (`0x2121F7`) and three passes.
- Pass 0 calls VM-owned `0x282BDC`, creates an actor in `rbx`, mirrors the selected action id to
  `+0x18C/+0x142`, and reaches post-store boundary `0x2066AE`.
- Pass 1 calls VM-owned `0x28198C`, reuses the actor in `rdi`, mirrors its existing `+0x17A` order
  payload to `+0x18C/+0x142`, and reaches post-store boundary `0x206743`.
- Pass 2 calls real-code `0x282E38`, consumes the selected unit's exact Reaction id from
  `unit+0x1CE`, creates an actor in `rbx`, mirrors the id, and reaches `0x206421`.
- Current executable guards pass 21/21 in `work/1783994199-runtime-hook-anchor-audit.md`.
- Full queue/call-graph analysis passes in `work/1783994325-dcl-reaction-queue-analysis.md`.

## Counter carrier result

Pass 2 has exact-id branches for the counter-style native records. The typed-order helper at
`0x283280` reads source-index global RVA `0x186AFF4` and derives target coordinates from that unit.
Counter `442` and Bonecrusher `434` invoke it with order type `1`, payload/action `0`, and
source-target validation. This is the native basic-attack counter shape.

The leading trigger-producer hypothesis is therefore:

1. retain a DCL execution decision for a target/reactor;
2. immediately before pass-2 selection, stage carrier `442` at that reactor's `unit+0x1CE`;
3. let the native selector author the basic-attack order against the source global;
4. consume cadence only at the resulting tagged commit.

No staging write is implemented. Source lifetime, reactor ownership, status-field coexistence, and
cleanup remain live gates.

## Runtime preparation

- The LT23 probe now installs guarded hooks at all three commit boundaries and logs
  `[DCL-REACTION-COMMIT] ... pass=0/1/2 ...`.
- A failed byte guard prevents the three-hook set from installing.
- The LT24 action-replacement control remains restricted to mapped pass 2.
- Release build and smoke tests pass with zero warnings/errors.
- LT23 and LT24 validate with zero errors.
- The updated LT23 DLL and settings are deployed to Reloaded-II.
- Source and installed DLL SHA-256 both equal
  `8222FB5A40911395D8C1EBC43583FBB5E58109B11CECA881AEAFB7130F7D8B54`.

## LT25 pre-selector preparation

- An observe-only pass-2 pre-selector probe is implemented at `0x2063A9`.
- It snapshots source/evaluated-Reaction globals, incoming actor/record identity, all 21
  `unit+0x1CE` words, and active markers before selector consumption.
- It has no carrier setting and no write path.
- Profile: `work/1783994716-battle-runtime-settings.lt25-dcl-reaction-preselector.json`.
- Protocol: `work/1783994716-lt25-dcl-reaction-preselector-live-plan.md`.
- Release build, smoke tests, and profile validation pass; runtime anchors pass 22/22 in
  `work/1783994762-runtime-hook-anchor-audit.md`.
- LT25 remains behind LT23 in the live gate order and is not deployed over the installed LT23.
- A launch-only smoke check proved that its AOB guard and assembly shim install successfully; see
  `work/1783994922-lt25-preselector-hook-launch-check.md`. LT23 was restored afterward.

## Live status

The user confirmed that launching the game through Reloaded-II works normally after selecting the
Reloaded launch choice. This clears the game-launch anomaly. It does not yet pass LT23: the prior
attempt proved only the original hook installation and did not execute a battle.

The current autonomous UI channel still cannot inject input into the elevated Reloaded/game
process. No blind input is sent, no save is changed, and no behavioral result is claimed.

The revised three-pass LT23 received a launch-only smoke check after deployment. Reloaded installed
all three hooks (`pass=2`, `pass=0`, `pass=1`) without an assembler or AOB failure, the game remained
responsive, and `CloseMainWindow` exited it cleanly. Evidence is in
`work/1783994850-lt23-three-pass-hook-launch-check.md`. This proves installation only, not behavior.

## Gate order

1. Run revised LT23 and require three hook-install rows plus one tagged commit per visible Reaction.
2. Establish which queue pass owns Counter and at least one distinct reaction family.
3. Run LT24 log-only on pass 2; only then prepare a one-write live replacement profile.
4. Prepare a separate log-only pre-selector producer probe/plan for carrier `442`.
5. Keep LoS and execution-provenance work offline until the carrier/source gates are exhausted.
