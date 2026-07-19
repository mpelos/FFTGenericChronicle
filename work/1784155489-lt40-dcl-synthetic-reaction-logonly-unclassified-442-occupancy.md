# LT40 synthetic-Reaction log-only: accepted gate and uncommitted 442 occupancy

## Scope

This was the third LT40 live attempt. It tested only the generic combat mechanism. No managed
effect, status, stat, balance value, or synthetic live write was enabled.

## Live evidence

Archived log: `work/1784155441-lt40-dcl-synthetic-reaction-logonly-live-third.log`.

Wenyld's ordinary Attack produced an exact successful, nonlethal transaction against Rion:

- source table index `6`, target table index `16`;
- action type `0x01`, ability `0`;
- Rion HP `277 -> 169`, debit `108`;
- carrier `443`, chance `100`, roll `0`;
- `accepted=1`, `replay=0`, `mailbox=armed`.

The following pre-selector did not reach `synthetic-would-stage`. It reported
`candidates=[16:442:active=True]`, `syntheticStates=[16:3]:carrier=443`, and `producer=none`.
Candidate `442` was first observed at the preceding pre-selector, immediately after Rion's Throw
Shuriken result. The capture contains zero Reaction materializations, zero native commits, zero
managed commits, and zero Reaction effects. No Counter was accepted or executed.

The later Choco Beak reduced Rion from `169` to `0`. This does not change the classification of the
candidate word.

## Offline correction

The startup unit dump bounds the candidate's lifetime:

- equipped Reaction `unit+0x14 = 443`;
- reaction-set bytes `unit+0x94..0x97 = 00 00 08 00`;
- candidate `unit+0x1CE = 0`.

The obsolete fixture therefore changed the equipped word but retained Counter's derived bit.
Current-build code proves the mapping: `0x30B837` loads mask `0x08`, `0x30B958` tests it against
`unit+0x96`, and `0x30B977` selects id `442`. The exact write during the first action was not hooked,
so its dynamic instruction is not directly captured; the inconsistent bitfield is the strong causal
explanation for the native `442` staging.

The pass-2 selector also explains persistence without execution. At `0x282E87`, the source/excluded
index branches directly to loop increment `0x283184`, before candidate read `0x282E9A` and clear
`0x282F29`. A candidate on that excluded index survives the pass without being consumed. Static
anchors are recorded in `work/1784157052-dcl-reaction-queue-analysis.md`.

## Fixture resolution

`tools/build_fft_autosave_reaction_fixture.py` now updates the equipped word and live reaction-set
bitfield atomically, fails closed when the expected source bit is absent, and audits both surfaces
after pack/unpack. Its integration test covers the Counter `0x08` to carrier `0x04` transition and
rejects an inconsistent source.

Corrected non-deployed artifact:

- `work/1784157011-synthetic-reaction-carrier443-consistent-fixture.png`;
- SHA-256 `415050EACDA681E5C24C3FF29AD41EA5E1D6FA6992A96F32499319D8BEE8EFE3`;
- live transition `unit+0x14: 442 -> 443`, `unit+0x96: 0x08 -> 0x04`;
- unrelated bitfield bits and stale autosave members remain unchanged.

## Next bounded gate

If a new live test is required, first confirm the startup tuple `+0x14=443`, `+0x96=0x04`, and
`+0x1CE=0`. Then one successful nonlethal incoming hit must produce the accepted synthetic gate and
`synthetic-would-stage`/state `4` without any preceding `442`. No synthetic live write is permitted
before that log-only row passes.
