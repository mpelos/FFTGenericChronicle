# LT28 DCL calc-provenance live result

## Test boundary

- Runtime profile: `work/1783997612-battle-runtime-settings.lt28-dcl-calc-provenance.json`.
- Save: Manual Saves, first entry, `05`.
- Battle: random encounter at Mandalia Plain.
- Action pair: Ramza `Mettle > Focus` (`type=0x19`, ability id `146`, self target).
- Mutation policy: observe-only; the game was closed with `Alt+F4` without saving.
- Raw log: `work/1784070150-lt28-dcl-calc-provenance-live.log`.

## Proven sequence

The first Focus forecast was opened and canceled. The second was opened and confirmed through its
visible execution animation. The log records exactly the expected two forecast calculations followed
by the execution calculation:

```text
n=0 returnRva=0xEF53F14 battleState=0x19 sourceIdx=0  forecastPtr=0x0
n=1 returnRva=0xEF53F14 battleState=0x19 sourceIdx=0  forecastPtr=0x141855E9E
n=2 returnRva=0x281F12  battleState=0x2A sourceIdx=16 forecastPtr=0x141855E9E
```

All three rows carry `turnOwner=16`, `casterIdx=16`, `type=0x19`, `abilityId=146`, and
`targetIdx=16`. The action then changes Ramza PA from 20 to 21, proving that row 2 belongs to the
confirmed execution rather than another forecast refresh.

## Corrected caller map

The former offline scan stopped at the aligned real-code boundary and therefore omitted executable
`.trace` code. A complete direct-`call rel32` scan of every executable PE section finds exactly three
call sites into calc entry `0x3099AC`:

- `0x281F0D`: ordinary affected-target sweep; returns at `0x281F12`;
- `0x307ED0`: nested formula-`0x25` Rend fallback; returns at `0x307ED5`;
- `0xEF53F0F`: player-forecast path in `.trace`; returns at `0xEF53F14`.

The corrected offline report is `work/1784070400-dcl-calc-provenance-analysis.md` and passes every
anchor and caller check. Runtime logging now names the third origin `forecast-trace`.

## Verdict

- **Player phase PASS — Proven:** caller provenance alone distinguishes forecast
  (`forecast-trace`) from confirmed execution (`outer-sweep`); battle states `0x19` and `0x2A`
  independently agree.
- **Forecast-pointer classifier Refuted:** it is zero on the first forecast, nonzero on the second,
  and remains nonzero during execution. It is useful presentation state, not phase authority.
- **Ordinary execution producer gate — Proven:** `(returnRva=0x281F12, battleState=0x2A)` is the
  execution-only calc completion surface for the observed action and can gate managed packet
  production without mutating forecast evaluation.
- **AI row not observed:** the enemy turns before Ramza did not calculate an action in the captured
  interval. This does not block an execution-only producer because AI scoring cannot satisfy the
  proven player execution pair, but it remains a presentation/scoring coverage row.
- **Nested Rend remains pending:** no loaded unit exposed a Rend action. Do not collapse or suppress
  `nested-rend-attack` until a separate Rend live row establishes the native fallback dependency.

## Next implementation step

Carry caller provenance and fire-time battle state into the calc cache, accept only the proven
ordinary execution pair for managed conditional status producers, and preserve the outer Rend entry
when a nested `0x307ED5` row appears. Forecast presentation and AI scoring remain read-only paths.
