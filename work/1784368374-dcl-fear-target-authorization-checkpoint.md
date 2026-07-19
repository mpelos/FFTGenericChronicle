# DCL Fear target-authorization checkpoint

## Scope

This checkpoint covers only the job-free DCL Fear combat mechanism. It does not assign Fear to a
job, select final balance values, or treat the draft job layer as implementation authority.

## Offline findings

The native Chicken dispatcher is not a complete Fear implementation. Its handled return suppresses
the ordinary planning path, so using Chicken unchanged would remove the self/ally/item/defensive
action that the DCL specification preserves.

The final affected-target list is available at RVA `0x281EC8`, after native unit/tile/AoE expansion
and before per-target calculation. Native target `0xFF` is skipped by both the calculation loop and
the affected-target output count. Battle state `0x19` reaches a single voluntary confirmation call
at `0x20C55F`; Reaction delivery state `0x2C` does not use that call.

## Implemented observe-only transaction

- `DclFearPolicy` classifies opposition with the same broad-foe-or-team split used by Approach.
- Any mixed AoE containing an opponent rejects the whole candidate.
- Self, ally, empty-tile and defensive target sets remain valid.
- AI state `0x05` and execution states `0x2A/0x2F` have an all-`0xFF` fail-closed path.
- Reaction state `0x2C` is explicitly excluded.
- Player state `0x19` records the forecast decision; the call-site hook can reject only the matching
  current turn owner's confirmation.
- Only the finite DCL status-duration owner named `dcl-fear` can bypass native Chicken control.
- Basic Attack `0` is the fixture's common successful-result transaction; its named rule authors
  byte 2 mask `0x04`. Chicken ability `242` is excluded because it changes Brave and produces no
  native status packet.

The settings validator requires `DclFearLogOnly=true`. The target-list, confirmation and Chicken
dispatcher hooks therefore log `would-*` decisions and perform no writes. Non-log-only activation is
rejected until forced movement is armed.

## Offline gates

- `tools/analyze_dcl_fear_boundaries.py --check-only`: PASS.
- `tools/analyze_dcl_fear_mechanism.py`: PASS.
- `tools/test_dcl_fear_mechanism.py`: PASS.
- C# runtime smoke tests, including target policy, Reaction preservation, carrier ownership and the
  live-write rejection gate: PASS.
- Complete `codemod/run-offline-checks.ps1` suite: PASS after 507.2 seconds.

The first complete run exposed and rejected an invalid carrier hypothesis: ability `242` has formula
`0x61`, an empty native status packet, and changes Brave instead. Adding it to the retained carrier
allowlist broke the exact status-action authority partition. The hypothesis was removed, the fixture
was moved to a common basic-Attack result transaction, the authority gate returned to its exact
22-action retained set, and the complete suite then passed.

## Remaining forced-flee boundary

Player movement acceptance is battle state `0x0D`, which calls `0x20B270`. That handler reads the
selected destination globals, calls the native route resolver `0x27C7B4`, copies its 128-byte route
record to actor `+0xA8..+0x127`, and enters the ordinary movement pipeline. State `0x11` consumes the
route; completion advances to `0x12`.

The native Chicken selector behind `0x38E11C` searches the flee destination, but its successful tail
writes the chosen coordinates into the active unit as part of the AI planning transaction. The next
offline task is to separate its chosen destination from that temporary planning write and connect it
to the ordinary accepted-route transaction. The remaining live invariant is whether this injected
route returns a player-controlled unit from state `0x12` to the normal post-move action menu without
ending or replanning the turn.
