# DCL Fear enemy-AI live result

## Artifacts

- Frozen runtime log: `work/1784440534-dcl-fear-enemy-ai-live.log`
- SHA-256: `8E6E0E20B86DD028AA8912CD63189949227EEA4F0C39882438BA3D730CAD813E`
- Runtime DLL SHA-256: `9A2B231E9C01797F7CDED8835A06950D92C36355ECCACD1A3F163BEFEE583E1B`
- Starting fixture: `work/1784418805-dcl-fear-josephine-fervor-arthur-999hp-fixture-fixture.png`
- Pre-test autosave backup: `work/1784440069-fft-autoenhanced-before-enemy-fear-test.png`

## Procedure and observation

Josephine moved into range and cast Fervor 53 on enemy Chocobo Kleobis (slot 7, 366 HP). Forecast
showed 100% connection. The DCL contest rolled 15 against resistance 6 and staged Chicken/Fear at
byte 2 mask `0x04`. Kleobis visibly changed into a Chicken.

On Kleobis's AI turn, the forced-control dispatcher emitted `decision=route`. The ordinary planner
considered Choco Beak 265 against an opposing unit and invalidated the candidate. The coordinator
selected one flee record, the ordinary planner published the same record, and the shim restored the
original `(7,3,1)` coordinate tuple plus Chicken `0x04` and Don't Move `0x00` byte-exactly with
`plannerResult=0` and `statusRestored=1`.

The native movement transaction then committed a new position (`+0x4F:07->08`, `+0x50:03->08`,
`+0x51:01->02`). No hostile action executed. The one-target-turn duration expired immediately after
the forced movement, clearing the master and effective Fear bit.

## Conclusion

Enemy-team Fear uses the same guarded coordinator as player-team Fear. It completes the forced route,
rejects an illegal hostile action candidate, falls through to the legal Wait outcome when no
self/ally/item/defensive action exists, and expires on the intended target-turn boundary.

The remaining authorization closure is the voluntary player-confirm mutation plus a representative
state-`0x2C` Reaction delivery under Fear ownership, followed by the complete Fear-profile regression.
