# DCL Fear turn-owner caster authority correction

## Contradiction in the live evidence

The proven pre-confirm row for Josephine attacking Arthur reported `turnOwner=17` but the current
actor's linked battle-unit pointer resolved to slot `0`. Later execution identified Josephine slot
`17` as the caster and Arthur slot `16` as the target. The private builder still returned the correct
target list `[16]`.

The first managed-authority refactor derived the caster from `actor+0x148` and then required that
derived index to equal the turn owner. On the proven fixture it would therefore mark the list
non-authoritative and fail open even though the builder output was correct. This was a silent gate,
not a crash or target-list failure.

## Correction

Voluntary confirmation now accepts caster authority only when battle state is `0x19` and the live
turn-owner index is within the 64-slot battle table. It derives the caster pointer and action record
from that slot. `actor+0x148` remains diagnostic and is logged as `actorUnitIdx`; it cannot override
the turn owner. Reaction state `0x2C` and invalid turn-owner indices fail open.

The next live row must show `casterSource=turn-owner`, `casterIdx=17`, and may continue to show
`actorUnitIdx=0`. AoE completeness remains the only gate before enabling voluntary rejection.

## Offline verification

- codemod smoke tests: PASS;
- Fear pre-confirm binary/source analyzer: PASS;
- Fear native-boundary analyzer: PASS;
- Fear forced-flee route analyzer: PASS;
- timeless documentation gate: PASS.

