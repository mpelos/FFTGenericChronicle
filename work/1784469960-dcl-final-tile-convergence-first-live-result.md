# DCL final-tile convergence first live result

## Inputs

- Runtime profile: `work/1784469176-battle-runtime-settings.dcl-final-tile-convergence-observe.json`.
- Runtime log: `D:\SteamLibrary\steamapps\common\FINAL FANTASY TACTICS - The Ivalice Chronicles\battleprobe_log.txt`.
- Runtime-log SHA-256: `D2553B7E04E940D23ACB3C856D164EC51F38E301430579C9F5A3EB60046E9C4C`.
- Hook: `ExecuteFirst`, RVA `0xD45A2A2`, guard `48 8B 8C 24 A8 00 00 00`.

## Controlled observation

The game reached battle normally. One AI unit completed movement, then the controlled allied unit
confirmed a two-tile movement and completed its walking animation. The game remained responsive
past the confirmation point that crashed the rejected five-byte call hook.

The hook published exactly two snapshots:

| Event | Unit | Cursor/length | Actor/target/unit tile | State | Initial classification |
| ---: | --- | ---: | --- | ---: | --- |
| 1 | foe index 1, character 130 | `6/0` | `5,8,0` | `0x11` | `no-movement` |
| 2 | ally index 17, character 129 | `2/0` | `9,0,0` | `0x11` | `no-movement` |

Both actors were idle and all three coordinate authorities agreed. There was no hook failure, ring
loss, duplicate publication, crash, or per-tile event.

## Conclusion

The convergence hook is live-safe for the observed AI and player routes and emits once after each
completed walking animation. The finalizer clears route length `+0xA8` before convergence while
preserving consumed cursor `+0xA4`. The initial classifier was therefore wrong: `cursor > 0` with
`length = 0` is the ordinary completed-route representation at this boundary. Only `0/0` is a
zero-length settlement.

The managed validator now treats consumed cursor as authoritative after length cleanup. A second
live run must show `accepted=1 reason=completed-movement` for one player and one AI movement.
