# DCL final-tile corrected-classifier live pass

## Inputs

- Runtime profile: `work/1784469176-battle-runtime-settings.dcl-final-tile-convergence-observe.json`.
- Runtime-log SHA-256: `2165EA063627306B5397B4E23CC14AB9FB30BC308CDBEF3CF7F2DF6D28F45540`.
- Hook: `ExecuteFirst`, RVA `0xD45A2A2`, guard `48 8B 8C 24 A8 00 00 00`.

## Result

The fresh battle generation published one AI movement and one controlled-player movement:

| Event | Unit | Cursor/length | Final tile | Classification |
| ---: | --- | ---: | --- | --- |
| 2 | foe index 1, character 130 | `6/0` | `5,8,0` | `accepted=1 reason=completed-movement` |
| 3 | ally index 17, character 129 | `2/0` | `9,0,0` | `accepted=1 reason=completed-movement` |

Both snapshots recorded idle actor state, battle state `0x11`, and identical actor-current,
actor-target, and battle-unit coordinates. No hook failure, skip, ring loss, duplicate, per-tile
publication, crash, or visible movement slowdown occurred.

## Conclusion

The finalizer convergence point and the consumed-cursor classifier are Proven live for ordinary AI
and player movement. This producer observes the completed movement only; it does not pause or alter
the route and does not invoke an effect.

The remaining producer gate is negative-case validation: preview/cancel, a turn without movement,
and zero-length settlement must publish no accepted event.
