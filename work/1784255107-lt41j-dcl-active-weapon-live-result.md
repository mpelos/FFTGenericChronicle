# LT41J DCL active-weapon live result

## Outcome

The managed active-weapon capture passes. The first report failed because its gate assumed every
state-`0x2F` calculation had to be an applied off-hand strike. Static timing and the rest of the same
live capture refute that assumption.

## Authoritative evidence

- Raw log: `work/1784254397-lt41j-dcl-active-weapon-falsifier-live.log`
- Raw-log SHA-256: `0F43A3DB151E66EC86DA19D3F42A085B7F3449893CEF294B7FDB240259814FB0`
- Corrected live report: `work/1784255052-dcl-active-weapon-live-analysis.md`
- Corrected live-report SHA-256: `66B649CD6BCCC657FFFDAA21AA935EAEBFACA8922D30EA766B79DF0A67CE10B6`
- Static timing/routing report: `work/1784255052-dcl-active-weapon-routing-analysis.md`
- Static report SHA-256: `7C40EB3F69A2CB12BC0D887AB1C90BFE665D1643D5B15F333560DCDECFAB6ABF`

The corrected gate requires at least one ordinary-owner pair with positive native staged debits on
both transactions. It checks each captured active item against that row's native repeat index rather
than inferring a hand from battle state.

| Pair | Ownership | Native debit | Repeat carrier | Active item | Interpretation |
| --- | --- | --- | --- | --- | --- |
| `19 -> 20` | ordinary | `273 -> 0` | `0/2 -> 0/2` | `17 -> 17` | non-completing follow-up |
| `25 -> 26` | Reaction delivery | `189 -> 189` | `0/2 -> 1/2` | `17 -> 18` | completed right/left pair |
| `37 -> 38` | ordinary | `189 -> 189` | `0/2 -> 1/2` | `17 -> 18` | completed right/left pair |

The first pair's target position changes from `(8,10)` before the first result to `(8,11)` before
the follow-up, and the engine stages zero native debit on that follow-up. This is consistent with a
position-changing first result followed by a canceled or reinitialized second attempt. It is not an
applied off-hand result.

## Timing proof

The executable contains no direct reference to repeat index RVA `0x7B0763` between calc entry
`0x3099AC` and active-weapon selector `0x309AB5`. The only pre-selector helper path that can return
to the selector also contains no reference. The outer result producer writes the index at
`0x282113` or `0x2821FA`, after its calculation sweep. Moving the managed capture later would not
repair the zero-debit row; the entry value already equals the value the native selector consumes on
that invocation.

## Separate finding

Although the engine staged native debit zero for row `20`, the one-point proof formula rewrote it to
one and applied one HP damage. This is not an active-weapon failure. It opens a distinct DCL output
question: numeric rewriting must distinguish an authored hit that legitimately overcomes zero
vanilla damage from a native cancellation caused by target legality or positional change. Resolve
that boundary offline before using native-zero preservation as a runtime rule.

## Remaining gate

Action-cache and formula-context propagation are covered offline. A later job-free regression can
close the final visible formula-surface gate by making the authored numeric result depend directly
on `action.weaponItemId` or active-weapon metadata and observing different applied right/left values.
No additional live test is needed to relocate or repair the calc-entry carrier capture.
