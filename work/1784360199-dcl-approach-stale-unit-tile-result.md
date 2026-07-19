# DCL Approach stale unit-tile result

## Capture

- Runtime log: `work/1784360171-dcl-approach-stale-unit-tile-live.log`
- SHA-256: `DE12FDFCA6C763E1E8DC7A87F6EF7E85E0899C35787DFFB8C77DD36D25F48CE4`
- Fixture: `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`
- Runtime profile: `work/1784344222-battle-runtime-settings.dcl-approach-live.json`

## Visible protocol

1. Rion used `Auto-battle > Attack Enemy > Wenyld`.
2. Rion moved and used Throw/Shuriken on Wenyld.
3. Wenyld moved and used basic Attack on Rion.
4. Janus followed the three-step route that enters Rion's configured Approach reach.

The battle continued normally after the rejected delivery. No Counter animation, effect, movement freeze, or crash occurred.

## Exact result

The entered-reach decision remained correct:

```text
[DCL-APPROACH-DECISION] event=15 cursor=3/3 from=4,3,0 entered=5,3,0 candidates=1 mask=0x10000 delivery=442 mailbox=source-mailbox-0:442-selector-excluded command=queue-pass2
```

The native bridge rejected before touching the tile map:

```text
[DCL-APPROACH-QUEUE] event=15 accepted=0 outcome=-2 targetMark=unavailable bridgeStage=1 unitTile=2,3,0/raw51=0x03 map=unavailable
```

`bridgeStage=1` means actor identity, unit identity, route identity, and the actor's entered-tile snapshot all still agreed, but the first actor/unit tile comparison failed. The movement actor was already at `5,3,0`, while the source battle-unit record still exposed `2,3,0`, the route origin. The later poll diff `unit+0x4F: 02 -> 05` occurred only after the rejected bridge released native movement.

## Static reconciliation

Counter's typed helper reads the source battle-unit table directly at RVAs `0x2832D2..0x2832FC`, computes the tile index from `unit+0x4F/+0x50/+0x51`, tests tile mark `+5 & 0x40` at `0x2832FE`, and only then copies the same source index and coordinates into the order at `0x28331B..0x283349`.

Therefore the current assumption that the battle-unit tile is already synchronized at boundary `0x1FE793` is live-refuted. Lending the mark on the actor's entered tile cannot satisfy the helper while the unit record is stale. Lending it on the stale origin would satisfy the wrong coordinate tuple, and mutating unit coordinates around the queue call would be an unproven state forgery. Neither is an acceptable implementation shortcut.

## Next offline target

Locate the native synchronization that writes the movement actor's entered coordinates into battle-unit `+0x4F/+0x50/+0x51`, then identify the earliest boundary that is simultaneously:

- after that synchronization;
- before the next route byte is consumed or the next step begins;
- before terminal-route completion destroys movement ownership;
- safe for one bounded pass-2 Reaction queue call and later resume.

The queue-stage instrumentation remains in source to distinguish every bridge gate in the next focused falsifier.
