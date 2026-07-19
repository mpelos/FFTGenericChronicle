# DCL Fear v7 plan-composition live result

## Artifacts

- Frozen runtime log: `work/1784439496-dcl-fear-v7-plan-composed-live.log`
- SHA-256: `2752E78FAAB4A135A3B04230EEA148A6E785B441D89D1609FDC133A6CBB9AA3E`
- Runtime DLL SHA-256: `9A2B231E9C01797F7CDED8835A06950D92C36355ECCACD1A3F163BEFEE583E1B`
- Fixture: `work/1784418805-dcl-fear-josephine-fervor-arthur-999hp-fixture-fixture.png`

## Transaction

Josephine used Fervor 53 on Arthur at 100% forecast. The DCL status contest rolled 10 against
resistance 9 and staged effective Chicken/Fear at byte 2, mask `0x04`.

Arthur's forced-control dispatcher entered `decision=route` from `(6,0,2)`. Event 1 selected flee
tile `(9,0,0)`, the ordinary planner published `(9,0,0)`, and the shim restored `(6,0,2)`, Chicken
`0x04`, and Don't Move `0x00` with `plannerResult=0` and `statusRestored=1`. During that planning pass,
opposing candidates were invalidated and Arthur's self candidate remained allowed. The selected
action was self-targeted Wall 13.

The engine invoked the forced planner a second time in the same pipeline. Event 2 repeated the same
selected and published tile and the same exact restoration. The native movement transaction then
changed Arthur's coordinates from X 6 / layer 2 to X 9 / layer 3, and Wall 13 subsequently reached
the execution path against Arthur himself.

## Conclusion

The guarded plan-composition transaction is live-proven. It no longer stops at the Chicken flee
selector: it preserves the chosen route, composes a legal non-opposing action at the destination,
restores borrowed unit state before returning, and lets the native movement/action pipeline execute
both parts of the turn.

The remaining live closure is narrower: voluntary opposing confirmation must be rejected while
self/ally confirmation and state-`0x2C` Reaction delivery remain allowed, followed by an enemy-team
forced-route regression and a complete Fear-profile regression.
